/* Copyright 2010-present MongoDB Inc.
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
    internal sealed class CommandUsingCommandMessageWireProtocol<TCommandResult> : IWireProtocol<TCommandResult>
    {
        // private fields
        private readonly BsonDocument _additionalOptions; // TODO: can these be supported when using CommandMessage?
        private readonly BsonDocument _command;
        private readonly List<BatchableCommandMessageSection> _commandPayloads;
        private readonly IElementNameValidator _commandValidator; // TODO: how can this be supported when using CommandMessage?
        private readonly DatabaseNamespace _databaseNamespace;
        private readonly IBinaryDocumentFieldDecryptor _documentFieldDecryptor;
        private readonly IBinaryCommandFieldEncryptor _documentFieldEncryptor;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly Action<IMessageEncoderPostProcessor> _postWriteAction;
        private readonly ReadPreference _readPreference;
        private readonly CommandResponseHandling _responseHandling;
        private readonly IBsonSerializer<TCommandResult> _resultSerializer;
        private readonly ServerApi _serverApi;
        private readonly TimeSpan _roundTripTime;
        private readonly ICoreSession _session;
        // streamable fields
        private bool _moreToCome = false; // MoreToCome from the previous response
        private int _previousRequestId; // RequestId from the previous response

        // constructors
        public CommandUsingCommandMessageWireProtocol(
            ICoreSession session,
            ReadPreference readPreference,
            DatabaseNamespace databaseNamespace,
            BsonDocument command,
            IEnumerable<BatchableCommandMessageSection> commandPayloads,
            IElementNameValidator commandValidator,
            BsonDocument additionalOptions,
            CommandResponseHandling responseHandling,
            IBsonSerializer<TCommandResult> resultSerializer,
            MessageEncoderSettings messageEncoderSettings,
            Action<IMessageEncoderPostProcessor> postWriteAction,
            ServerApi serverApi,
            TimeSpan roundTripTime)
        {
            if (responseHandling != CommandResponseHandling.Return &&
                responseHandling != CommandResponseHandling.NoResponseExpected &&
                responseHandling != CommandResponseHandling.ExhaustAllowed)
            {
                throw new ArgumentException("CommandResponseHandling must be Return, NoneExpected or ExhaustAllowed.", nameof(responseHandling));
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
            _serverApi = serverApi; // can be null
            _roundTripTime = roundTripTime;

            if (messageEncoderSettings != null)
            {
                _documentFieldDecryptor = messageEncoderSettings.GetOrDefault<IBinaryDocumentFieldDecryptor>(MessageEncoderSettingsName.BinaryDocumentFieldDecryptor, null);
                _documentFieldEncryptor = messageEncoderSettings.GetOrDefault<IBinaryCommandFieldEncryptor>(MessageEncoderSettingsName.BinaryDocumentFieldEncryptor, null);
            }
        }

        // public properties
        public bool MoreToCome => _moreToCome;

        // public methods
        public TCommandResult Execute(OperationContext operationContext, IConnection connection)
        {
            try
            {
                int responseTo;
                CommandRequestMessage message = null;

                if (_moreToCome)
                {
                    responseTo = _previousRequestId;
                }
                else
                {
                    message = CreateCommandMessage(operationContext, connection.Description);
                    // TODO: CSOT: Propagate operationContext into Encryption
                    message = AutoEncryptFieldsIfNecessary(message, connection, operationContext.CancellationToken);
                    responseTo = message.WrappedMessage.RequestId;
                }

                try
                {
                    return SendMessageAndProcessResponse(operationContext, message, responseTo, connection);
                }
                catch (MongoCommandException commandException) when (RetryabilityHelper.IsReauthenticationRequested(commandException, _command))
                {
                    connection.Reauthenticate(operationContext);
                    return SendMessageAndProcessResponse(operationContext, message, responseTo, connection);
                }
            }
            catch (Exception exception)
            {
                AddErrorLabelIfRequired(exception, connection.Description);

                TransactionHelper.UnpinServerIfNeededOnCommandException(_session, exception);
                throw;
            }
        }

        public async Task<TCommandResult> ExecuteAsync(OperationContext operationContext, IConnection connection)
        {
            try
            {
                int responseTo;
                CommandRequestMessage message = null;

                if (_moreToCome)
                {
                    responseTo = _previousRequestId;
                }
                else
                {
                    message = CreateCommandMessage(operationContext, connection.Description);
                    // TODO: CSOT: Propagate operationContext into Encryption
                    message = await AutoEncryptFieldsIfNecessaryAsync(message, connection, operationContext.CancellationToken).ConfigureAwait(false);
                    responseTo = message.WrappedMessage.RequestId;
                }

                try
                {
                    return await SendMessageAndProcessResponseAsync(operationContext, message, responseTo, connection).ConfigureAwait(false);
                }
                catch (MongoCommandException commandException) when (RetryabilityHelper.IsReauthenticationRequested(commandException, _command))
                {
                    await connection.ReauthenticateAsync(operationContext).ConfigureAwait(false);
                    return await SendMessageAndProcessResponseAsync(operationContext, message, responseTo, connection).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                AddErrorLabelIfRequired(exception, connection.Description);

                TransactionHelper.UnpinServerIfNeededOnCommandException(_session, exception);
                throw;
            }
        }

        // private methods
        private void AddErrorLabelIfRequired(Exception exception, ConnectionDescription connectionDescription)
        {
            if (exception is MongoException mongoException)
            {
                if (ShouldAddTransientTransactionError(mongoException))
                {
                    mongoException.AddErrorLabel("TransientTransactionError");
                }

                if (RetryabilityHelper.IsCommandRetryable(_command) && connectionDescription != null)
                {
                    RetryabilityHelper.AddRetryableWriteErrorLabelIfRequired(mongoException, connectionDescription);
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
                var messageFieldDecryptor = new CommandMessageFieldDecryptor(_documentFieldDecryptor);
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
                var messageFieldDecryptor = new CommandMessageFieldDecryptor(_documentFieldDecryptor);
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
                if (connection.Description.HelloResult.MaxWireVersion < 8)
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
                if (connection.Description.HelloResult.MaxWireVersion < 8)
                {
                    throw new NotSupportedException("Auto-encryption requires a minimum MongoDB version of 4.2.");
                }

                var helper = new CommandMessageFieldEncryptor(_documentFieldEncryptor, _messageEncoderSettings);
                return await helper.EncryptFieldsAsync(_databaseNamespace.DatabaseName, unencryptedRequestMessage, cancellationToken).ConfigureAwait(false);
            }
        }

        private CommandRequestMessage CreateCommandMessage(OperationContext operationContext, ConnectionDescription connectionDescription)
        {
            var requestId = RequestMessage.GetNextRequestId();
            var responseTo = 0;
            var sections = CreateSections(operationContext, connectionDescription);

            var moreToComeRequest = _responseHandling == CommandResponseHandling.NoResponseExpected;

            var wrappedMessage = new CommandMessage(requestId, responseTo, sections, moreToComeRequest)
            {
                PostWriteAction = _postWriteAction,
                ExhaustAllowed = _responseHandling == CommandResponseHandling.ExhaustAllowed,
            };

            return new CommandRequestMessage(wrappedMessage);
        }

        private IEnumerable<CommandMessageSection> CreateSections(OperationContext operationContext, ConnectionDescription connectionDescription)
        {
            var type0Section = CreateType0Section(operationContext, connectionDescription);
            if (_commandPayloads == null)
            {
                return new[] { type0Section };
            }
            else
            {
                return new CommandMessageSection[] { type0Section }.Concat(_commandPayloads);
            }
        }

        private Type0CommandMessageSection<BsonDocument> CreateType0Section(OperationContext operationContext, ConnectionDescription connectionDescription)
        {
            var extraElements = new List<BsonElement>();

            AddIfNotAlreadyAdded("$db", _databaseNamespace.DatabaseName);

            if (connectionDescription?.HelloResult.ServerType != ServerType.Standalone
                && _readPreference != null
                && _readPreference != ReadPreference.Primary)
            {
                var readPreferenceDocument = QueryHelper.CreateReadPreferenceDocument(_readPreference);
                AddIfNotAlreadyAdded("$readPreference", readPreferenceDocument);
            }

            if (_session.Id != null)
            {
                var areSessionsSupported = connectionDescription.HelloResult.LogicalSessionTimeout.HasValue;

                if (!areSessionsSupported)
                {
                    if (_session.IsImplicit)
                    {
                        // do not set sessionId if session is implicit and sessions are not supported
                    }
                    else
                    {
                        throw new MongoClientException("Sessions are not supported.");
                    }
                }
                else if (IsSessionAcknowledged())
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

            var snapshotReadConcernDocument = ReadConcernHelper.GetReadConcernForSnapshotSession(_session, connectionDescription);
            if (snapshotReadConcernDocument != null)
            {
                extraElements.Add(new BsonElement("readConcern", snapshotReadConcernDocument));
            }

            if (_session.ClusterTime != null)
            {
                AddIfNotAlreadyAdded("$clusterTime", _session.ClusterTime);
            }

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
            if (_serverApi != null)
            {
                AddIfNotAlreadyAdded("apiVersion", _serverApi.Version.ToString());
                if (_serverApi.Strict.HasValue)
                {
                    AddIfNotAlreadyAdded("apiStrict", _serverApi.Strict.Value);
                }
                if (_serverApi.DeprecationErrors.HasValue)
                {
                    AddIfNotAlreadyAdded("apiDeprecationErrors", _serverApi.DeprecationErrors.Value);
                }
            }

            if (operationContext.IsRootContextTimeoutConfigured() && _roundTripTime > TimeSpan.Zero)
            {
                var serverTimeout = operationContext.RemainingTimeout;
                if (serverTimeout != Timeout.InfiniteTimeSpan)
                {
                    serverTimeout -= _roundTripTime;
                    // Server expects maxTimeMS as an integer, we should truncate it to give server a chance to reply with Timeout.
                    // Do not want to use MaxTimeHelper here, because it has different logic (rounds up, allow zero value and throw ArgumentException on negative values instead of TimeoutException).
                    var maxtimeMs = (int)serverTimeout.TotalMilliseconds;
                    if (maxtimeMs <= 0)
                    {
                        throw new TimeoutException();
                    }

                    AddIfNotAlreadyAdded("maxTimeMS", maxtimeMs);
                }
            }

            var elementAppendingSerializer = new ElementAppendingSerializer<BsonDocument>(BsonDocumentSerializer.Instance, extraElements);
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
                errmsg.AsString.StartsWith("Transaction numbers", StringComparison.Ordinal);
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

        private void SaveResponseInfo(CommandResponseMessage response)
        {
            _previousRequestId = response.RequestId;
            _moreToCome = response.WrappedMessage.MoreToCome;
        }

        private TCommandResult SendMessageAndProcessResponse(OperationContext operationContext, CommandRequestMessage message, int responseTo, IConnection connection)
        {
            var responseExpected = true;
            if (message != null)
            {
                try
                {
                    ThrowIfRemainingTimeoutLessThenRoundTripTime(operationContext);
                    connection.SendMessage(operationContext, message, _messageEncoderSettings);
                }
                finally
                {
                    if (message.WasSent)
                    {
                        MessageWasProbablySent(message);
                    }
                }

                responseExpected = message.WrappedMessage.ResponseExpected; // mutable, read after sending
            }

            if (responseExpected)
            {
                var encoderSelector = new CommandResponseMessageEncoderSelector();
                var response = (CommandResponseMessage)connection.ReceiveMessage(operationContext, responseTo, encoderSelector, _messageEncoderSettings);
                // TODO: CSOT: Propagate operationContext into Encryption
                response = AutoDecryptFieldsIfNecessary(response, operationContext.CancellationToken);
                try
                {
                    var result = ProcessResponse(connection.ConnectionId, response.WrappedMessage);
                    SaveResponseInfo(response);
                    return result;
                }
                catch (MongoServerException ex)
                {
                    connection.CompleteCommandWithException(ex);
                    throw;
                }
            }
            else
            {
                return default;
            }
        }

        private async Task<TCommandResult> SendMessageAndProcessResponseAsync(OperationContext operationContext, CommandRequestMessage message, int responseTo, IConnection connection)
        {
            var responseExpected = true;
            if (message != null)
            {
                try
                {
                    ThrowIfRemainingTimeoutLessThenRoundTripTime(operationContext);
                    await connection.SendMessageAsync(operationContext, message, _messageEncoderSettings).ConfigureAwait(false);
                }
                finally
                {
                    if (message.WasSent)
                    {
                        MessageWasProbablySent(message);
                    }
                }
                responseExpected = message.WrappedMessage.ResponseExpected; // mutable, read after sending
            }

            if (responseExpected)
            {
                var encoderSelector = new CommandResponseMessageEncoderSelector();
                var response = (CommandResponseMessage)await connection.ReceiveMessageAsync(operationContext, responseTo, encoderSelector, _messageEncoderSettings).ConfigureAwait(false);
                // TODO: CSOT: Propagate operationContext into Encryption
                response = await AutoDecryptFieldsIfNecessaryAsync(response, operationContext.CancellationToken).ConfigureAwait(false);
                try
                {
                    var result = ProcessResponse(connection.ConnectionId, response.WrappedMessage);
                    SaveResponseInfo(response);
                    return result;
                }
                catch (MongoServerException ex)
                {
                    connection.CompleteCommandWithException(ex);
                    throw;
                }
            }
            else
            {
                return default;
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

        private void ThrowIfRemainingTimeoutLessThenRoundTripTime(OperationContext operationContext)
        {
            if (operationContext.RemainingTimeout == Timeout.InfiniteTimeSpan ||
                _roundTripTime == TimeSpan.Zero ||
                operationContext.RemainingTimeout > _roundTripTime)
            {
                return;
            }

            throw new TimeoutException();
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
