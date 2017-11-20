/* Copyright 2013-2017 MongoDB Inc.
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;

namespace MongoDB.Driver.Core.WireProtocol
{
    internal class CommandWireProtocol<TCommandResult> : IWireProtocol<TCommandResult>
    {
        // fields
        private readonly BsonDocument _additionalOptions;
        private readonly BsonDocument _command;
        private readonly Func<CommandResponseHandling> _responseHandling;
        private readonly IElementNameValidator _commandValidator;
        private readonly DatabaseNamespace _databaseNamespace;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly ReadPreference _readPreference;
        private readonly IBsonSerializer<TCommandResult> _resultSerializer;
        private readonly ICoreSession _session;
        private readonly bool _slaveOk;

        // constructors
        public CommandWireProtocol(
            DatabaseNamespace databaseNamespace,
            BsonDocument command,
            bool slaveOk,
            IBsonSerializer<TCommandResult> resultSerializer,
            MessageEncoderSettings messageEncoderSettings)
            : this(
                databaseNamespace,
                command,
                NoOpElementNameValidator.Instance,
                () => CommandResponseHandling.Return,
                slaveOk,
                resultSerializer,
                messageEncoderSettings)
        {
        }

        public CommandWireProtocol(
            DatabaseNamespace databaseNamespace,
            BsonDocument command,
            IElementNameValidator commandValidator,
            Func<CommandResponseHandling> responseHandling,
            bool slaveOk,
            IBsonSerializer<TCommandResult> resultSerializer,
            MessageEncoderSettings messageEncoderSettings)
            : this(
                  NoCoreSession.Instance,
                  null, // readPreference
                  databaseNamespace,
                  command,
                  commandValidator,
                  null, // aditionalOptions
                  responseHandling,
                  slaveOk,
                  resultSerializer,
                  messageEncoderSettings)
        {
        }

        public CommandWireProtocol(
            ICoreSession session,
            ReadPreference readPreference,
            DatabaseNamespace databaseNamespace,
            BsonDocument command,
            IElementNameValidator commandValidator,
            BsonDocument additionalOptions,
            Func<CommandResponseHandling> responseHandling,
            bool slaveOk,
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
            _slaveOk = slaveOk;
            _resultSerializer = Ensure.IsNotNull(resultSerializer, nameof(resultSerializer));
            _messageEncoderSettings = messageEncoderSettings;
        }

        // methods
        private QueryMessage CreateMessage(ConnectionDescription connectionDescription, out bool messageContainsSessionId)
        {
            var wrappedCommand = WrapCommandForQueryMessage(connectionDescription, out messageContainsSessionId);

            return new QueryMessage(
                RequestMessage.GetNextRequestId(),
                _databaseNamespace.CommandCollection,
                wrappedCommand,
                null,
                _commandValidator,
                0,
                -1,
                _slaveOk,
                false,
                false,
                false,
                false,
                false);
        }

        public TCommandResult Execute(IConnection connection, CancellationToken cancellationToken)
        {
            bool messageContainsSessionId;
            var message = CreateMessage(connection.Description, out messageContainsSessionId);
            connection.SendMessage(message, _messageEncoderSettings, cancellationToken);
            if (messageContainsSessionId)
            {
                _session.WasUsed();
            }

            switch (_responseHandling())
            {
                case CommandResponseHandling.Ignore:
                    IgnoreResponse(connection, message, cancellationToken);
                    return default(TCommandResult);
                default:
                    var encoderSelector = new ReplyMessageEncoderSelector<RawBsonDocument>(RawBsonDocumentSerializer.Instance);
                    var reply = connection.ReceiveMessage(message.RequestId, encoderSelector, _messageEncoderSettings, cancellationToken);
                    return ProcessReply(connection.ConnectionId, (ReplyMessage<RawBsonDocument>)reply);
            }
        }

        public async Task<TCommandResult> ExecuteAsync(IConnection connection, CancellationToken cancellationToken)
        {
            bool messageContainsSessionId;
            var message = CreateMessage(connection.Description, out messageContainsSessionId);
            await connection.SendMessageAsync(message, _messageEncoderSettings, cancellationToken).ConfigureAwait(false);
            if (messageContainsSessionId)
            {
                _session.WasUsed();
            }

            switch (_responseHandling())
            {
                case CommandResponseHandling.Ignore:
                    IgnoreResponse(connection, message, cancellationToken);
                    return default(TCommandResult);
                default:
                    var encoderSelector = new ReplyMessageEncoderSelector<RawBsonDocument>(RawBsonDocumentSerializer.Instance);
                    var reply = await connection.ReceiveMessageAsync(message.RequestId, encoderSelector, _messageEncoderSettings, cancellationToken).ConfigureAwait(false);
                    return ProcessReply(connection.ConnectionId, (ReplyMessage<RawBsonDocument>)reply);
            }
        }

        private void IgnoreResponse(IConnection connection, QueryMessage message, CancellationToken cancellationToken)
        {
            var encoderSelector = new ReplyMessageEncoderSelector<IgnoredReply>(IgnoredReplySerializer.Instance);
            connection.ReceiveMessageAsync(message.RequestId, encoderSelector, _messageEncoderSettings, cancellationToken).IgnoreExceptions();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        private TCommandResult ProcessReply(ConnectionId connectionId, ReplyMessage<RawBsonDocument> reply)
        {
            if (reply.NumberReturned == 0)
            {
                throw new MongoCommandException(connectionId, "Command returned no documents.", _command);
            }
            if (reply.NumberReturned > 1)
            {
                throw new MongoCommandException(connectionId, "Command returned multiple documents.", _command);
            }
            if (reply.QueryFailure)
            {
                var failureDocument = reply.QueryFailureDocument;
                throw ExceptionMapper.Map(connectionId, failureDocument) ?? new MongoCommandException(connectionId, "Command failed.", _command, failureDocument);
            }

            using (var rawDocument = reply.Documents[0])
            {
                var binaryReaderSettings = new BsonBinaryReaderSettings();
                if (_messageEncoderSettings != null)
                {
                    binaryReaderSettings.Encoding = _messageEncoderSettings.GetOrDefault<UTF8Encoding>(MessageEncoderSettingsName.ReadEncoding, Utf8Encodings.Strict);
                    binaryReaderSettings.GuidRepresentation = _messageEncoderSettings.GetOrDefault<GuidRepresentation>(MessageEncoderSettingsName.GuidRepresentation, GuidRepresentation.CSharpLegacy);
                };

                BsonValue clusterTime;
                if (rawDocument.TryGetValue("$clusterTime", out clusterTime))
                {
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

                    var mappedException = ExceptionMapper.Map(connectionId, materializedDocument);
                    if (mappedException != null)
                    {
                        throw mappedException;
                    }

                    throw new MongoCommandException(connectionId, message, _command, materializedDocument);
                }

                using (var stream = new ByteBufferStream(rawDocument.Slice, ownsBuffer: false))
                {
                    var encoderFactory = new BinaryMessageEncoderFactory(stream, _messageEncoderSettings);
                    var encoder = (ReplyMessageBinaryEncoder<TCommandResult>)encoderFactory.GetReplyMessageEncoder<TCommandResult>(_resultSerializer);
                    using (var reader = encoder.CreateBinaryReader())
                    {
                        var context = BsonDeserializationContext.CreateRoot(reader);
                        return _resultSerializer.Deserialize(context);
                    }
                }
            }
        }

        private BsonDocument WrapCommandForQueryMessage(ConnectionDescription connectionDescription, out bool messageContainsSessionId)
        {
            messageContainsSessionId = false;
            var extraElements = new List<BsonElement>();
            if (_session.Id != null)
            {
                var areSessionsSupported = connectionDescription.IsMasterResult.LogicalSessionTimeout.HasValue;
                if (areSessionsSupported)
                {
                    var lsid = new BsonElement("lsid", _session.Id);
                    extraElements.Add(lsid);
                    messageContainsSessionId = true;
                }
                else
                {
                    if (!_session.IsImplicit)
                    {
                        throw new MongoClientException("Sessions are not supported.");
                    }
                }
            }
            if (_session.ClusterTime != null)
            {
                var clusterTime = new BsonElement("$clusterTime", _session.ClusterTime);
                extraElements.Add(clusterTime);
            }
            var appendExtraElementsSerializer = new ElementAppendingSerializer<BsonDocument>(BsonDocumentSerializer.Instance, extraElements);
            var commandWithExtraElements = new BsonDocumentWrapper(_command, appendExtraElementsSerializer);

            BsonDocument readPreferenceDocument = null;
            if (connectionDescription != null)
            {
                var serverType = connectionDescription.IsMasterResult.ServerType;
                readPreferenceDocument = QueryHelper.CreateReadPreferenceDocument(serverType, _readPreference);
            }

            var wrappedCommand = new BsonDocument
            {
                { "$query", commandWithExtraElements },
                { "$readPreference", readPreferenceDocument, readPreferenceDocument != null }
            };
            if (_additionalOptions != null)
            {
                wrappedCommand.Merge(_additionalOptions, overwriteExistingElements: false);
            }

            if (wrappedCommand.ElementCount == 1)
            {
                return wrappedCommand["$query"].AsBsonDocument;
            }
            else
            {
                return wrappedCommand;
            }
        }

        // nested types
        private class IgnoredReply
        {
            public static IgnoredReply Instance = new IgnoredReply();
        }

        private class IgnoredReplySerializer : SerializerBase<IgnoredReply>
        {
            public static IgnoredReplySerializer Instance = new IgnoredReplySerializer();

            public override IgnoredReply Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            {
                context.Reader.ReadStartDocument();
                while (context.Reader.ReadBsonType() != BsonType.EndOfDocument)
                {
                    context.Reader.SkipName();
                    context.Reader.SkipValue();
                }
                context.Reader.ReadEndDocument();
                return IgnoredReply.Instance;
            }
        }
    }
}
