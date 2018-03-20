/* Copyright 2018-present MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.WireProtocol
{
    internal class CommandUsingCommandMessageWireProtocol<TCommandResult> : IWireProtocol<TCommandResult>
    {
        // private fields
        private readonly BsonDocument _additionalOptions; // TODO: can these be supported when using CommandMessage?
        private readonly BsonDocument _command;
        private readonly Func<CommandResponseHandling> _responseHandling; // TODO: does this make any sense when using CommandMessage?
        private readonly IElementNameValidator _commandValidator; // TODO: how can this be supported when using CommandMessage?
        private readonly DatabaseNamespace _databaseNamespace;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly ReadPreference _readPreference;
        private readonly IBsonSerializer<TCommandResult> _resultSerializer;
        private readonly ICoreSession _session;

        // constructors
        public CommandUsingCommandMessageWireProtocol(
            ICoreSession session,
            ReadPreference readPreference,
            DatabaseNamespace databaseNamespace,
            BsonDocument command,
            IElementNameValidator commandValidator,
            BsonDocument additionalOptions,
            Func<CommandResponseHandling> responseHandling,
            IBsonSerializer<TCommandResult> resultSerializer,
            MessageEncoderSettings messageEncoderSettings)
        {
            _session = Ensure.IsNotNull(session, nameof(session));
            _readPreference = readPreference;
            _databaseNamespace = Ensure.IsNotNull(databaseNamespace, nameof(databaseNamespace));
            _command = Ensure.IsNotNull(command, nameof(command));
            _commandValidator = Ensure.IsNotNull(commandValidator, nameof(commandValidator));
            _additionalOptions = additionalOptions; // can be null
            _responseHandling = responseHandling;
            _resultSerializer = Ensure.IsNotNull(resultSerializer, nameof(resultSerializer));
            _messageEncoderSettings = messageEncoderSettings;
        }

        // public methods
        public TCommandResult Execute(IConnection connection, CancellationToken cancellationToken)
        {
            var message = CreateCommandMessage(connection.Description);
            connection.SendMessage(message, _messageEncoderSettings, cancellationToken);
            MessageWasSent();

            var encoderSelector = new CommandResponseMessageEncoderSelector();
            var response = (CommandResponseMessage)connection.ReceiveMessage(message.RequestId, encoderSelector, _messageEncoderSettings, cancellationToken);
            return ProcessResponse(connection.ConnectionId, response.WrappedMessage);
        }

        public async Task<TCommandResult> ExecuteAsync(IConnection connection, CancellationToken cancellationToken)
        {
            var message = CreateCommandMessage(connection.Description);
            await connection.SendMessageAsync(message, _messageEncoderSettings, cancellationToken).ConfigureAwait(false);
            MessageWasSent();

            var encoderSelector = new CommandResponseMessageEncoderSelector();
            var response = (CommandResponseMessage)await connection.ReceiveMessageAsync(message.RequestId, encoderSelector, _messageEncoderSettings, cancellationToken).ConfigureAwait(false);
            return ProcessResponse(connection.ConnectionId, response.WrappedMessage);
        }

        // private methods
        private CommandRequestMessage CreateCommandMessage(ConnectionDescription connectionDescription)
        {
            var requestId = RequestMessage.GetNextRequestId();
            var responseTo = 0;
            var sections = CreateSections(connectionDescription);
            var moreToCome = false;
            var wrappedMessage = new CommandMessage(requestId, responseTo, sections, moreToCome);
            var shouldBeSent = (Func<bool>)(() => true);

            return new CommandRequestMessage(wrappedMessage, shouldBeSent);
        }

        private IEnumerable<CommandMessageSection> CreateSections(ConnectionDescription connectionDescription)
        {
            var type0Section = CreateType0Section(connectionDescription);
            return new[] { type0Section };
        }

        private Type0CommandMessageSection<BsonDocument> CreateType0Section(ConnectionDescription connectionDescription)
        {
            var extraElements = new List<BsonElement>();
            var dbElement = new BsonElement("$db", _databaseNamespace.DatabaseName);
            extraElements.Add(dbElement);
            if (_readPreference != null && _readPreference != ReadPreference.Primary)
            {
                var readPreferenceDocument = QueryHelper.CreateReadPreferenceDocument(_readPreference);
                var readPreferenceElement = new BsonElement("$readPreference", readPreferenceDocument);
                extraElements.Add(readPreferenceElement);
            }
            if (_session.Id != null)
            {
                var lsidElement = new BsonElement("lsid", _session.Id);
                extraElements.Add(lsidElement);
            }
            if (_session.ClusterTime != null)
            {
                var clusterTimeElement = new BsonElement("$clusterTime", _session.ClusterTime);
                extraElements.Add(clusterTimeElement);
            }

            var elementAppendingSerializer = new ElementAppendingSerializer<BsonDocument>(BsonDocumentSerializer.Instance, extraElements);
            return new Type0CommandMessageSection<BsonDocument>(_command, elementAppendingSerializer);
        }

        private void MessageWasSent()
        {
            if (_session.Id != null)
            {
                _session.WasUsed();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        private TCommandResult ProcessResponse(ConnectionId connectionId, CommandMessage responseMessage)
        {
            using (new CommandMessageDisposer(responseMessage))
            {
                var rawDocument = responseMessage.Sections.OfType<Type0CommandMessageSection<RawBsonDocument>>().Single().Document;

                var binaryReaderSettings = new BsonBinaryReaderSettings();
                if (_messageEncoderSettings != null)
                {
                    binaryReaderSettings.Encoding = _messageEncoderSettings.GetOrDefault<UTF8Encoding>(MessageEncoderSettingsName.ReadEncoding, Utf8Encodings.Strict);
                    binaryReaderSettings.GuidRepresentation = _messageEncoderSettings.GetOrDefault<GuidRepresentation>(MessageEncoderSettingsName.GuidRepresentation, GuidRepresentation.CSharpLegacy);
                };

                BsonValue clusterTime;
                if (rawDocument.TryGetValue("$clusterTime", out clusterTime))
                {
                    // note: we are assuming that _session is an instance of ClusterClockAdvancingClusterTime
                    // and that calling _session.AdvanceClusterTime will have the side effect of advancing the cluster's ClusterTime also
                    var materializedClusterTime = ((RawBsonDocument)clusterTime).Materialize(binaryReaderSettings);
                    _session.AdvanceClusterTime(materializedClusterTime);
                }

                BsonValue operationTime;
                if (rawDocument.TryGetValue("operationTime", out operationTime))
                {
                    _session.AdvanceOperationTime(operationTime.AsBsonTimestamp);
                }

                if (!rawDocument.GetValue("ok", false).ToBoolean())
                {
                    var materializedDocument = rawDocument.Materialize(binaryReaderSettings);

                    var commandName = _command.GetElement(0).Name;
                    if (commandName == "$query")
                    {
                        commandName = _command["$query"].AsBsonDocument.GetElement(0).Name;
                    }

                    var notPrimaryOrNodeIsRecoveringException = ExceptionMapper.MapNotPrimaryOrNodeIsRecovering(connectionId, materializedDocument, "errmsg");
                    if (notPrimaryOrNodeIsRecoveringException != null)
                    {
                        throw notPrimaryOrNodeIsRecoveringException;
                    }

                    var mappedException = ExceptionMapper.Map(connectionId, materializedDocument);
                    if (mappedException != null)
                    {
                        throw mappedException;
                    }

                    string message;
                    BsonValue errmsgBsonValue;
                    if (materializedDocument.TryGetValue("errmsg", out errmsgBsonValue) && errmsgBsonValue.IsString)
                    {
                        var errmsg = errmsgBsonValue.ToString();
                        message = string.Format("Command {0} failed: {1}.", commandName, errmsg);
                    }
                    else
                    {
                        message = string.Format("Command {0} failed.", commandName);
                    }

                    throw new MongoCommandException(connectionId, message, _command, materializedDocument);
                }

                using (var stream = new ByteBufferStream(rawDocument.Slice, ownsBuffer: false))
                {
                    using (var reader = new BsonBinaryReader(stream, binaryReaderSettings))
                    {
                        var context = BsonDeserializationContext.CreateRoot(reader);
                        return _resultSerializer.Deserialize(context);
                    }
                }
            }
        }
    }
}
