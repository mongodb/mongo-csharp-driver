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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Logging;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;

namespace MongoDB.Driver.Core.Connections
{
    internal class CommandEventHelper
    {
        private readonly EventLogger<LogCategories.Command> _eventLogger;
        private ConcurrentDictionary<int, CommandState> _state;

        private readonly bool _eventsNeedBeforeSending;
        private readonly bool _shouldTrackStarted;
        private readonly bool _eventsNeedState;
        private readonly bool _shouldTrackFailed;
        private readonly bool _shouldTrackSucceeded;
        private readonly bool _tracingDisabled;
        private readonly int _queryTextMaxLength;

        private Activity _currentCommandActivity;

        public CommandEventHelper(EventLogger<LogCategories.Command> eventLogger, TracingOptions tracingOptions = null)
        {
            _eventLogger = eventLogger;

            _shouldTrackSucceeded = _eventLogger.IsEventTracked<CommandSucceededEvent>();
            _shouldTrackFailed = _eventLogger.IsEventTracked<CommandFailedEvent>();
            _shouldTrackStarted = _eventLogger.IsEventTracked<CommandStartedEvent>();

            _tracingDisabled = tracingOptions?.Disabled == true;
            _queryTextMaxLength = tracingOptions?.QueryTextMaxLength ?? 0;

            _eventsNeedState = _shouldTrackSucceeded || _shouldTrackFailed;
            _eventsNeedBeforeSending = _shouldTrackStarted || _eventsNeedState;

            if (_eventsNeedState)
            {
                // we only need to track state if we have to raise
                // a succeeded or failed event or for tracing
                _state = new ConcurrentDictionary<int, CommandState>();
            }
        }

        public bool ShouldCallBeforeSending => _eventsNeedBeforeSending || ShouldTraceWithActivityListener();

        public bool ShouldCallAfterSending => _eventsNeedState || ShouldTraceWithActivityListener();

        public bool ShouldCallErrorSending => _eventsNeedState || ShouldTraceWithActivityListener();

        public bool ShouldCallAfterReceiving => _eventsNeedState || ShouldTraceWithActivityListener();

        public bool ShouldCallErrorReceiving => _eventsNeedState || ShouldTraceWithActivityListener();

        public bool ShouldCallConnectionFailed => (_shouldTrackFailed || ShouldTraceWithActivityListener()) && _state != null;

        private bool ShouldTraceWithActivityListener()
            => !_tracingDisabled && MongoTelemetry.ActivitySource.HasListeners();

        public void CompleteFailedCommandActivity(Exception exception)
        {
            if (_currentCommandActivity is not null)
            {
                MongoTelemetry.RecordException(_currentCommandActivity, exception);
                _currentCommandActivity.Dispose();
                _currentCommandActivity = null;
            }
        }

        public void EnsureCommandActivityCompleted()
        {
            if (_currentCommandActivity is not null)
            {
                _currentCommandActivity.Dispose();
                _currentCommandActivity = null;
            }
        }

        public void BeforeSending(
            RequestCommandMessage message,
            ConnectionId connectionId,
            ObjectId? serviceId,
            IByteBuffer buffer,
            MessageEncoderSettings encoderSettings,
            Stopwatch stopwatch,
            bool skipLogging)
        {
            using (var stream = new ByteBufferStream(buffer, ownsBuffer: false))
            {
                ProcessCommandRequestMessage(message, connectionId, serviceId, new CommandMessageBinaryEncoder(stream, encoderSettings), stopwatch, skipLogging);
            }
        }

        public void AfterSending(RequestCommandMessage message, ConnectionId connectionId, ObjectId? serviceId, bool skipLogging)
        {
            if (_state == null)
            {
                return;
            }

            if (_state.TryGetValue(message.RequestId, out var state) &&
                state.ExpectedResponseType == ExpectedResponseType.None)
            {
                state.Stopwatch.Stop();

                CompleteCommandActivityWithSuccess();

                if (_shouldTrackSucceeded)
                {
                    _eventLogger.LogAndPublish(new CommandSucceededEvent(
                            state.CommandName,
                            new BsonDocument("ok", 1),
                            state.QueryNamespace.DatabaseNamespace,
                            state.OperationId,
                            message.RequestId,
                            connectionId,
                            serviceId,
                            state.Stopwatch.Elapsed),
                        skipLogging);
                }

                _state.TryRemove(message.RequestId, out state);
            }
        }

        public void ErrorSending(RequestCommandMessage message, ConnectionId connectionId, ObjectId? serviceId, Exception exception, bool skipLogging)
        {
            if (_state == null)
            {
                return;
            }

            CompleteFailedCommandActivity(exception);

            if (_state.TryRemove(message.RequestId, out var state))
            {
                state.Stopwatch.Stop();

                _eventLogger.LogAndPublish(new CommandFailedEvent(
                        state.CommandName,
                        state.QueryNamespace.DatabaseNamespace,
                        exception,
                        state.OperationId,
                        message.RequestId,
                        connectionId,
                        serviceId,
                        state.Stopwatch.Elapsed),
                    skipLogging);
            }
        }

        public void AfterReceiving(ResponseCommandMessage message, IByteBuffer buffer, ConnectionId connectionId, ObjectId? serviceId, MessageEncoderSettings encoderSettings, bool skipLogging)
        {
            if (_state == null)
            {
                return;
            }

            if (!_state.TryRemove(message.ResponseTo, out var state))
            {
                // this indicates a bug in the sending portion...
                return;
            }

            ProcessCommandResponseMessage(state, message, buffer, connectionId, serviceId, encoderSettings, skipLogging);
        }

        public void ErrorReceiving(int responseTo, ConnectionId connectionId, ObjectId? serviceId, Exception exception, bool skipLogging)
        {
            if (_state == null)
            {
                return;
            }

            CompleteFailedCommandActivity(exception);

            if (!_state.TryRemove(responseTo, out var state))
            {
                // this indicates a bug in the sending portion...
                return;
            }

            state.Stopwatch.Stop();

            _eventLogger.LogAndPublish(new CommandFailedEvent(
                state.CommandName,
                state.QueryNamespace.DatabaseNamespace,
                exception,
                state.OperationId,
                responseTo,
                connectionId,
                serviceId,
                state.Stopwatch.Elapsed),
                skipLogging);
        }

        public void ConnectionFailed(ConnectionId connectionId, ObjectId? serviceId, Exception exception, bool skipLogging)
        {
            CompleteFailedCommandActivity(exception);

            var requestIds = _state.Keys;
            foreach (var requestId in requestIds)
            {
                CommandState state;
                if (_state.TryRemove(requestId, out state))
                {
                    state.Stopwatch.Stop();

                    if (_shouldTrackFailed)
                    {
                        _eventLogger.LogAndPublish(new CommandFailedEvent(
                            state.CommandName,
                            state.QueryNamespace.DatabaseNamespace,
                            exception,
                            state.OperationId,
                            requestId,
                            connectionId,
                            serviceId,
                            state.Stopwatch.Elapsed),
                            skipLogging);
                    }
                }
            }
        }

        private void ProcessCommandRequestMessage(RequestCommandMessage message, ConnectionId connectionId, ObjectId? serviceId, CommandMessageBinaryEncoder encoder, Stopwatch stopwatch, bool skipLogging)
        {
            var requestId = message.RequestId;
            var operationId = EventContext.OperationId;

            var originalType0Section = message.Sections.OfType<Type0CommandMessageSection>().Single();
            var originalCommand = (BsonDocument)originalType0Section.Document;
            var databaseNamespace = originalType0Section.DatabaseNamespace;
            var sessionId = originalType0Section.SessionId;
            var transactionNumber = originalType0Section.TransactionNumber;

            var commandName = originalCommand.GetElement(0).Name;
            var shouldRedactCommand = ShouldRedactCommand(originalCommand);

            // Only decode when we need the full command for CommandStartedEvent or db.query.text
            if ((_shouldTrackStarted || _queryTextMaxLength > 0) && !shouldRedactCommand)
            {
                var decodedMessage = encoder.ReadMessage();
                using (new CommandMessageDisposer(decodedMessage))
                {
                    var type0Section = decodedMessage.Sections.OfType<Type0CommandMessageSection>().Single();
                    var command = (BsonDocument)type0Section.Document;
                    var type1Sections = decodedMessage.Sections.OfType<Type1CommandMessageSection>().ToList();
                    if (type1Sections.Count > 0)
                    {
                        command = new BsonDocument(command); // materialize the top level of the command RawBsonDocument
                        foreach (var type1Section in type1Sections)
                        {
                            var name = type1Section.Identifier;
                            var items = new BsonArray(type1Section.Documents.GetBatchItems().Cast<RawBsonDocument>());
                            command[name] = items;
                        }
                    }

                    _eventLogger.LogAndPublish(new CommandStartedEvent(
                        commandName,
                        command,
                        databaseNamespace,
                        operationId,
                        requestId,
                        connectionId,
                        serviceId),
                        skipLogging);

                    TrackCommandState(
                        requestId,
                        operationId,
                        commandName,
                        command,
                        stopwatch,
                        new CollectionNamespace(databaseNamespace, "$cmd"),
                        decodedMessage.MoreToCome ? ExpectedResponseType.None : ExpectedResponseType.Command,
                        shouldRedactCommand,
                        databaseNamespace,
                        connectionId,
                        skipLogging,
                        sessionId,
                        transactionNumber);
                }
            }
            else
            {
                if (_shouldTrackStarted)
                {
                    _eventLogger.LogAndPublish(new CommandStartedEvent(
                        commandName,
                        new BsonDocument(),
                        databaseNamespace,
                        operationId,
                        requestId,
                        connectionId,
                        serviceId),
                        skipLogging);
                }

                TrackCommandState(
                    requestId,
                    operationId,
                    commandName,
                    originalCommand,
                    stopwatch,
                    new CollectionNamespace(databaseNamespace, "$cmd"),
                    message.MoreToCome ? ExpectedResponseType.None : ExpectedResponseType.Command,
                    shouldRedactCommand,
                    databaseNamespace,
                    connectionId,
                    skipLogging,
                    sessionId,
                    transactionNumber);
            }
        }

        private void ProcessCommandResponseMessage(CommandState state, ResponseCommandMessage message, IByteBuffer buffer, ConnectionId connectionId, ObjectId? serviceId, MessageEncoderSettings encoderSettings, bool skipLogging)
        {
            var type0Section = message.Sections.OfType<Type0CommandMessageSection<RawBsonDocument>>().Single();
            var reply = (BsonDocument)type0Section.Document;

            BsonValue ok;
            if (!reply.TryGetValue("ok", out ok))
            {
                // this is a degenerate case with the server and
                // we don't really know what to do here...
                return;
            }

            if (state.ShouldRedactReply)
            {
                reply = new BsonDocument();
            }

            if (ok.ToBoolean())
            {
                CompleteCommandActivityWithSuccess(reply);

                _eventLogger.LogAndPublish(new CommandSucceededEvent(
                    state.CommandName,
                    reply,
                    state.QueryNamespace.DatabaseNamespace,
                    state.OperationId,
                    message.ResponseTo,
                    connectionId,
                    serviceId,
                    state.Stopwatch.Elapsed),
                    skipLogging);
            }
            else
            {
                HandleCommandFailure(state, reply, connectionId, serviceId, message.ResponseTo, skipLogging);
            }
        }

        private static bool ShouldRedactCommand(BsonDocument command)
        {
            var commandName = command.GetElement(0).Name;
            switch (commandName.ToLowerInvariant())
            {
                // string constants MUST all be lowercase for the case-insensitive comparison to work
                case "authenticate":
                case "saslstart":
                case "saslcontinue":
                case "getnonce":
                case "createuser":
                case "updateuser":
                case "copydbsaslstart":
                case "copydb":
                    return true;

                case "hello":
                case OppressiveLanguageConstants.LegacyHelloCommandNameLowerCase:
                    return command.Names.Any(n => n.ToLowerInvariant() == "speculativeauthenticate");

                default:
                    return false;
            }
        }

        private void TrackCommandState(
            int requestId,
            long? operationId,
            string commandName,
            BsonDocument command,
            Stopwatch stopwatch,
            CollectionNamespace queryNamespace,
            ExpectedResponseType expectedResponseType,
            bool shouldRedactCommand,
            DatabaseNamespace databaseNamespace,
            ConnectionId connectionId,
            bool skipLogging,
            BsonDocument sessionId,
            long? transactionNumber)
        {
            var shouldTraceCommand = ShouldTraceWithActivityListener() && !shouldRedactCommand && !skipLogging;

            if (!_eventsNeedState && !shouldTraceCommand)
            {
                return;
            }

            _state ??= new ConcurrentDictionary<int, CommandState>();

            var commandState = new CommandState
            {
                CommandName = commandName,
                OperationId = operationId,
                Stopwatch = stopwatch,
                QueryNamespace = queryNamespace,
                ExpectedResponseType = expectedResponseType,
                ShouldRedactReply = shouldRedactCommand
            };

            if (shouldTraceCommand)
            {
                _currentCommandActivity = MongoTelemetry.StartCommandActivity(
                    commandName,
                    command,
                    databaseNamespace,
                    connectionId,
                    sessionId,
                    transactionNumber,
                    _queryTextMaxLength);
            }

            _state.TryAdd(requestId, commandState);
        }

        private void CompleteCommandActivityWithSuccess(BsonDocument reply = null)
        {
            if (_currentCommandActivity is not null)
            {
                MongoTelemetry.CompleteCommandActivity(_currentCommandActivity, reply);
                _currentCommandActivity = null;
            }
        }

        private void HandleCommandFailure(
            CommandState state,
            BsonDocument reply,
            ConnectionId connectionId,
            ObjectId? serviceId,
            int responseTo,
            bool skipLogging)
        {
            // Activity is not completed here. WireProtocol will create the real exception with a
            // meaningful stacktrace, and CompleteCommandActivityWithException will be called there.

            if (!_shouldTrackFailed)
            {
                return;
            }

            var exception = new MongoCommandException(
                connectionId,
                $"{state.CommandName} command failed",
                null,
                reply);

            _eventLogger.LogAndPublish(new CommandFailedEvent(
                state.CommandName,
                state.QueryNamespace.DatabaseNamespace,
                exception,
                state.OperationId,
                responseTo,
                connectionId,
                serviceId,
                state.Stopwatch.Elapsed),
                skipLogging);
        }

        private enum ExpectedResponseType
        {
            None,
            Command
        }

        private class CommandState
        {
            public string CommandName;
            public long? OperationId;
            public Stopwatch Stopwatch;
            public CollectionNamespace QueryNamespace;
            public ExpectedResponseType ExpectedResponseType;
            public bool ShouldRedactReply;
        }
    }
}
