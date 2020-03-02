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
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.WireProtocol
{
    internal class CommandUsingCommandMessageWireProtocol<TCommandResult> : IWireProtocol<TCommandResult>
    {
        // private fields
        private readonly BsonDocument _additionalOptions; // TODO: can these be supported when using CommandMessage?
        private readonly BsonDocument _command;
        private readonly List<Type1CommandMessageSection> _commandPayloads;
        private readonly IElementNameValidator _commandValidator; // TODO: how can this be supported when using CommandMessage?
        private readonly DatabaseNamespace _databaseNamespace;
        private readonly IBinaryDocumentFieldDecryptor _documentFieldDecryptor;
        private readonly IBinaryCommandFieldEncryptor _documentFieldEncryptor;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly Action<IMessageEncoderPostProcessor> _postWriteAction;
        private readonly ReadPreference _readPreference;
        private readonly CommandResponseHandling _responseHandling;
        private readonly IBsonSerializer<TCommandResult> _resultSerializer;
        private readonly ICoreSession _session;

        // constructors
        public CommandUsingCommandMessageWireProtocol(
            ICoreSession session,
            ReadPreference readPreference,
            DatabaseNamespace databaseNamespace,
            BsonDocument command,
            IEnumerable<Type1CommandMessageSection> commandPayloads,
            IElementNameValidator commandValidator,
            BsonDocument additionalOptions,
            CommandResponseHandling responseHandling,
            IBsonSerializer<TCommandResult> resultSerializer,
            MessageEncoderSettings messageEncoderSettings,
            Action<IMessageEncoderPostProcessor> postWriteAction)
        {
            if (responseHandling != CommandResponseHandling.Return && responseHandling != CommandResponseHandling.NoResponseExpected)
            {
                throw new ArgumentException("CommandResponseHandling must be Return or NoneExpected.", nameof(responseHandling));
            }

            _session = Ensure.IsNotNull(session, nameof(session));
            _readPreference = readPreference;
            _databaseNamespace = Ensure.IsNotNull(databaseNamespace, nameof(databaseNamespace));
            _command = Ensure.IsNotNull(command, nameof(command));
            _commandPayloads = commandPayloads?.ToList(); // can be null
            _commandValidator = Ensure.IsNotNull(commandValidator, nameof(commandValidator));
            _additionalOptions = additionalOptions; // can be null
            _responseHandling = responseHandling;
            _resultSerializer = Ensure.IsNotNull(resultSerializer, nameof(resultSerializer));
            _messageEncoderSettings = messageEncoderSettings;
            _postWriteAction = postWriteAction; // can be null

            if (messageEncoderSettings != null)
            {
                _documentFieldDecryptor = messageEncoderSettings.GetOrDefault<IBinaryDocumentFieldDecryptor>(MessageEncoderSettingsName.BinaryDocumentFieldDecryptor, null);
                _documentFieldEncryptor = messageEncoderSettings.GetOrDefault<IBinaryCommandFieldEncryptor>(MessageEncoderSettingsName.BinaryDocumentFieldEncryptor, null);
            }
        }

        // public methods
        public TCommandResult Execute(IConnection connection, CancellationToken cancellationToken)
        {
            try
            {
                var message = CreateCommandMessage(connection.Description);
                message = AutoEncryptFieldsIfNecessary(message, connection, cancellationToken);

                try
                {
                    connection.SendMessage(message, _messageEncoderSettings, cancellationToken);
                }
                finally
                {
                    if (message.WasSent)
                    {
                        MessageWasProbablySent(message);
                    }
                }

                if (message.WrappedMessage.ResponseExpected)
                {
                    var encoderSelector = new CommandResponseMessageEncoderSelector();
                    var response = (CommandResponseMessage)connection.ReceiveMessage(message.RequestId, encoderSelector, _messageEncoderSettings, cancellationToken);
                    response = AutoDecryptFieldsIfNecessary(response, cancellationToken);
                    return ProcessResponse(connection.ConnectionId, response.WrappedMessage);
                }
                else
                {
                    return default(TCommandResult);
                }
            }
            catch (Exception exception)
            {
                AddErrorLabelIfRequired(exception, connection.Description.ServerVersion);

                TransactionHelper.UnpinServerIfNeededOnCommandException(_session, exception);
                throw;
            }
        }

        public async Task<TCommandResult> ExecuteAsync(IConnection connection, CancellationToken cancellationToken)
        {
            try
            {
                var message = CreateCommandMessage(connection.Description);
                message = await AutoEncryptFieldsIfNecessaryAsync(message, connection, cancellationToken).ConfigureAwait(false);

                try
                {
                    await connection.SendMessageAsync(message, _messageEncoderSettings, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    if (message.WasSent)
                    {
                        MessageWasProbablySent(message);
                    }
                }

                if (message.WrappedMessage.ResponseExpected)
                {
                    var encoderSelector = new CommandResponseMessageEncoderSelector();
                    var response = (CommandResponseMessage)await connection.ReceiveMessageAsync(message.RequestId, encoderSelector, _messageEncoderSettings, cancellationToken).ConfigureAwait(false);
                    response = await AutoDecryptFieldsIfNecessaryAsync(response, cancellationToken).ConfigureAwait(false);
                    return ProcessResponse(connection.ConnectionId, response.WrappedMessage);
                }
                else
                {
                    return default(TCommandResult);
                }
            }
            catch (Exception exception)
            {
                AddErrorLabelIfRequired(exception, connection.Description.ServerVersion);

                TransactionHelper.UnpinServerIfNeededOnCommandException(_session, exception);
                throw;
            }
        }

        // private methods
        private void AddErrorLabelIfRequired(Exception exception, SemanticVersion serverVersion)
        {
            if (exception is MongoException mongoException)
            {
                if (ShouldAddTransientTransactionError(mongoException))
                {
                    mongoException.AddErrorLabel("TransientTransactionError");
                }

                if ((exception is MongoConnectionException) || // network error
                    (Feature.RetryableWrites.IsSupported(serverVersion) &&
                    !Feature.ServerReturnsRetryableWriteErrorLabel.IsSupported(serverVersion)))
                {
                    RetryabilityHelper.AddRetryableWriteErrorLabelIfRequired(mongoException);
                }
            }
        }

        private CommandResponseMessage AutoDecryptFieldsIfNecessary(CommandResponseMessage encryptedResponseMessage, CancellationToken cancellationToken)
        {
            if (_documentFieldDecryptor == null)
            {
                return encryptedResponseMessage;
            }
            else
            {
                var messageFieldDecryptor = new CommandMessageFieldDecryptor(_documentFieldDecryptor, _messageEncoderSettings);
                return messageFieldDecryptor.DecryptFields(encryptedResponseMessage, cancellationToken);
            }
        }

        private async Task<CommandResponseMessage> AutoDecryptFieldsIfNecessaryAsync(CommandResponseMessage encryptedResponseMessage, CancellationToken cancellationToken)
        {
            if (_documentFieldDecryptor == null)
            {
                return encryptedResponseMessage;
            }
            else
            {
                var messageFieldDecryptor = new CommandMessageFieldDecryptor(_documentFieldDecryptor, _messageEncoderSettings);
                return await messageFieldDecryptor.DecryptFieldsAsync(encryptedResponseMessage, cancellationToken).ConfigureAwait(false);
            }
        }

        private CommandRequestMessage AutoEncryptFieldsIfNecessary(CommandRequestMessage unencryptedRequestMessage, IConnection connection, CancellationToken cancellationToken)
        {
            if (_documentFieldEncryptor == null)
            {
                return unencryptedRequestMessage;
            }
            else
            {
                if (connection.Description.IsMasterResult.MaxWireVersion < 8)
                {
                    throw new NotSupportedException("Auto-encryption requires a minimum MongoDB version of 4.2.");
                }

                var helper = new CommandMessageFieldEncryptor(_documentFieldEncryptor, _messageEncoderSettings);
                return helper.EncryptFields(_databaseNamespace.DatabaseName, unencryptedRequestMessage, cancellationToken);
            }
        }

        private async Task<CommandRequestMessage> AutoEncryptFieldsIfNecessaryAsync(CommandRequestMessage unencryptedRequestMessage, IConnection connection, CancellationToken cancellationToken)
        {
            if (_documentFieldEncryptor == null)
            {
                return unencryptedRequestMessage;
            }
            else
            {
                if (connection.Description.IsMasterResult.MaxWireVersion < 8)
                {
                    throw new NotSupportedException("Auto-encryption requires a minimum MongoDB version of 4.2.");
                }

                var helper = new CommandMessageFieldEncryptor(_documentFieldEncryptor, _messageEncoderSettings);
                return await helper.EncryptFieldsAsync(_databaseNamespace.DatabaseName, unencryptedRequestMessage, cancellationToken).ConfigureAwait(false);
            }
        }

        private CommandRequestMessage CreateCommandMessage(ConnectionDescription connectionDescription)
        {
            var requestId = RequestMessage.GetNextRequestId();
            var responseTo = 0;
            var sections = CreateSections(connectionDescription);
            var moreToCome = _responseHandling == CommandResponseHandling.NoResponseExpected;
            var wrappedMessage = new CommandMessage(requestId, responseTo, sections, moreToCome)
            {
                PostWriteAction = _postWriteAction
            };
            var shouldBeSent = (Func<bool>)(() => true);

            return new CommandRequestMessage(wrappedMessage, shouldBeSent);
        }

        private IEnumerable<CommandMessageSection> CreateSections(ConnectionDescription connectionDescription)
        {
            var type0Section = CreateType0Section(connectionDescription);
            if (_commandPayloads == null)
            {
                return new[] { type0Section };
            }
            else
            {
                return new CommandMessageSection[] { type0Section }.Concat(_commandPayloads);
            }
        }

        private Type0CommandMessageSection<BsonDocument> CreateType0Section(ConnectionDescription connectionDescription)
        {
            var extraElements = new List<BsonElement>();

            AddIfNotAlreadyAdded("$db", _databaseNamespace.DatabaseName);

            if (connectionDescription.IsMasterResult.ServerType != ServerType.Standalone
                && _readPreference != null
                && _readPreference != ReadPreference.Primary)
            {
                var readPreferenceDocument = QueryHelper.CreateReadPreferenceDocument(_readPreference);
                AddIfNotAlreadyAdded("$readPreference", readPreferenceDocument);
            }

            if (_session.Id != null)
            {
                if (IsSessionAcknowledged())
                {
                    AddIfNotAlreadyAdded("lsid", _session.Id);
                }
                else
                {
                    if (_session.IsImplicit)
                    {
                        // do not set sessionId if session is implicit and write is unacknowledged
                    }
                    else
                    {
                        throw new InvalidOperationException("Explicit session must not be used with unacknowledged writes.");
                    }
                }
            }

            if (_session.ClusterTime != null)
            {
                AddIfNotAlreadyAdded("$clusterTime", _session.ClusterTime);
            }
#pragma warning disable 618
            Action<BsonWriterSettings> writerSettingsConfigurator = null;
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                writerSettingsConfigurator = s => s.GuidRepresentation = GuidRepresentation.Unspecified;
            }
#pragma warning restore 618

            _session.AboutToSendCommand();
            if (_session.IsInTransaction)
            {
                var transaction = _session.CurrentTransaction;
                AddIfNotAlreadyAdded("txnNumber", transaction.TransactionNumber);
                if (transaction.State == CoreTransactionState.Starting)
                {
                    AddIfNotAlreadyAdded("startTransaction", true);
                    var readConcern = ReadConcernHelper.GetReadConcernForFirstCommandInTransaction(_session, connectionDescription);
                    if (readConcern != null)
                    {
                        AddIfNotAlreadyAdded("readConcern", readConcern);
                    }
                }
                AddIfNotAlreadyAdded("autocommit", false);
            }

            var elementAppendingSerializer = new ElementAppendingSerializer<BsonDocument>(BsonDocumentSerializer.Instance, extraElements, writerSettingsConfigurator);
            return new Type0CommandMessageSection<BsonDocument>(_command, elementAppendingSerializer);

            void AddIfNotAlreadyAdded(string key, BsonValue value)
            {
                if (!_command.Contains(key))
                {
                    extraElements.Add(new BsonElement(key, value));
                }
            }

            bool IsSessionAcknowledged()
            {
                if (_command.TryGetValue("writeConcern", out var writeConcernDocument))
                {
                    var writeConcern = WriteConcern.FromBsonDocument(writeConcernDocument.AsBsonDocument);
                    return writeConcern.IsAcknowledged;
                }
                else
                {
                    return true;
                }
            }
        }

        private bool IsRetryableWriteExceptionAndDeploymentDoesNotSupportRetryableWrites(MongoCommandException exception)
        {
            return
                exception.Result.TryGetValue("code", out var errorCode) &&
                errorCode.ToInt32() == 20 &&
                exception.Result.TryGetValue("errmsg", out var errmsg) &&
                errmsg.AsString.StartsWith("Transaction numbers");
        }


        private void MessageWasProbablySent(CommandRequestMessage message)
        {
            if (_session.Id != null)
            {
                _session.WasUsed();
            }

            var transaction = _session.CurrentTransaction;
            if (transaction != null && transaction.State == CoreTransactionState.Starting)
            {
                transaction.SetState(CoreTransactionState.InProgress);
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
#pragma warning disable 618
                    if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
                    {
                        binaryReaderSettings.GuidRepresentation = _messageEncoderSettings.GetOrDefault<GuidRepresentation>(MessageEncoderSettingsName.GuidRepresentation, GuidRepresentation.CSharpLegacy);
                    }
#pragma warning restore 618
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

                if (rawDocument.GetValue("ok", false).ToBoolean())
                {
                    if (rawDocument.TryGetValue("recoveryToken", out var rawRecoveryToken))
                    {
                        var recoveryToken = ((RawBsonDocument)rawRecoveryToken).Materialize(binaryReaderSettings);
                        _session.CurrentTransaction.RecoveryToken = recoveryToken;
                    }
                }
                else
                {
                    var materializedDocument = rawDocument.Materialize(binaryReaderSettings);

                    var commandName = _command.GetElement(0).Name;
                    if (commandName == "$query")
                    {
                        commandName = _command["$query"].AsBsonDocument.GetElement(0).Name;
                    }

                    var notPrimaryOrNodeIsRecoveringException = ExceptionMapper.MapNotPrimaryOrNodeIsRecovering(connectionId, _command, materializedDocument, "errmsg");
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

                    var exception = new MongoCommandException(connectionId, message, _command, materializedDocument);

                    // https://jira.mongodb.org/browse/CSHARP-2678
                    if (IsRetryableWriteExceptionAndDeploymentDoesNotSupportRetryableWrites(exception))
                    {
                        throw WrapNotSupportedRetryableWriteException(exception);
                    }
                    else
                    {
                        throw exception;
                    }
                }

                if (rawDocument.Contains("writeConcernError"))
                {
                    var materializedDocument = rawDocument.Materialize(binaryReaderSettings);
                    var writeConcernError = materializedDocument["writeConcernError"].AsBsonDocument;
                    var message = writeConcernError.AsBsonDocument.GetValue("errmsg", null)?.AsString;
                    var writeConcernResult = new WriteConcernResult(materializedDocument);
                    throw new MongoWriteConcernException(connectionId, message, writeConcernResult);
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

        private bool ShouldAddTransientTransactionError(MongoException exception)
        {
            if (_session.IsInTransaction)
            {
                if (exception is MongoConnectionException)
                {
                    return true;
                }
            }

            return false;
        }

        private MongoException WrapNotSupportedRetryableWriteException(MongoCommandException exception)
        {
            const string friendlyErrorMessage =
                "This MongoDB deployment does not support retryable writes. " +
                "Please add retryWrites=false to your connection string.";
            return new MongoCommandException(
                exception.ConnectionId,
                friendlyErrorMessage,
                exception.Command,
                exception.Result,
                innerException: exception);
        }
    }
}
