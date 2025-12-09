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
using System.IO;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Serializers;
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
        private readonly ConcurrentDictionary<int, CommandState> _state;
        private readonly TracingOptions _tracingOptions;

        private readonly bool _shouldProcessRequestMessages;
        private readonly bool _shouldTrackState;
        private readonly bool _shouldTrackFailed;
        private readonly bool _shouldTrackSucceeded;
        private readonly bool _shouldTrace;

        public CommandEventHelper(EventLogger<LogCategories.Command> eventLogger, TracingOptions tracingOptions = null)
        {
            _eventLogger = eventLogger;
            _tracingOptions = tracingOptions;

            _shouldTrackSucceeded = _eventLogger.IsEventTracked<CommandSucceededEvent>();
            _shouldTrackFailed = _eventLogger.IsEventTracked<CommandFailedEvent>();

            // Check if tracing is disabled for this client
            _shouldTrace = _tracingOptions?.Disabled != true;

            _shouldTrackState = _shouldTrackSucceeded || _shouldTrackFailed || _shouldTrace;
            _shouldProcessRequestMessages = _eventLogger.IsEventTracked<CommandStartedEvent>() || _shouldTrackState;

            if (_shouldTrackState)
            {
                // we only need to track state if we have to raise
                // a succeeded or failed event or for tracing
                _state = new ConcurrentDictionary<int, CommandState>();
            }
        }

        public bool ShouldCallBeforeSending
        {
            get { return _shouldProcessRequestMessages; }
        }

        public bool ShouldCallAfterSending
        {
            get { return _shouldTrackState; }
        }

        public bool ShouldCallErrorSending
        {
            get { return _shouldTrackState; }
        }

        public bool ShouldCallAfterReceiving
        {
            get { return _shouldTrackState; }
        }

        public bool ShouldCallErrorReceiving
        {
            get { return _shouldTrackState; }
        }

        public void BeforeSending(
            RequestMessage message,
            ConnectionId connectionId,
            ObjectId? serviceId,
            IByteBuffer buffer,
            MessageEncoderSettings encoderSettings,
            Stopwatch stopwatch,
            bool skipLogging)
        {
            using (var stream = new ByteBufferStream(buffer, ownsBuffer: false))
            {
                ProcessRequestMessage(message, connectionId, serviceId, stream, encoderSettings, stopwatch, skipLogging);
            }
        }

        public void AfterSending(RequestMessage message, ConnectionId connectionId, ObjectId? serviceId, bool skipLogging)
        {
            CommandState state;
            if (_state.TryGetValue(message.RequestId, out state) &&
                state.ExpectedResponseType == ExpectedResponseType.None)
            {
                state.Stopwatch.Stop();

                if (state.CommandActivity != null)
                {
                    state.CommandActivity.SetStatus(ActivityStatusCode.Ok);
                    state.CommandActivity.Stop();
                }

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

        public void ErrorSending(RequestMessage message, ConnectionId connectionId, ObjectId? serviceId, Exception exception, bool skipLogging)
        {
            CommandState state;
            if (_state.TryRemove(message.RequestId, out state))
            {
                state.Stopwatch.Stop();

                if (state.CommandActivity != null)
                {
                    MongoTelemetry.RecordException(state.CommandActivity, exception);
                    state.CommandActivity.Stop();
                }

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

        public void AfterReceiving(ResponseMessage message, IByteBuffer buffer, ConnectionId connectionId, ObjectId? serviceId, MessageEncoderSettings encoderSettings, bool skipLogging)
        {
            CommandState state;
            if (!_state.TryRemove(message.ResponseTo, out state))
            {
                // this indicates a bug in the sending portion...
                return;
            }

            if (message is CommandResponseMessage)
            {
                ProcessCommandResponseMessage(state, (CommandResponseMessage)message, buffer, connectionId, serviceId, encoderSettings, skipLogging);
            }
            else
            {
                ProcessReplyMessage(state, message, buffer, connectionId, encoderSettings, skipLogging);
            }
        }

        public void ErrorReceiving(int responseTo, ConnectionId connectionId, ObjectId? serviceId, Exception exception, bool skipLogging)
        {
            CommandState state;
            if (!_state.TryRemove(responseTo, out state))
            {
                // this indicates a bug in the sending portion...
                return;
            }

            state.Stopwatch.Stop();

            if (state.CommandActivity != null)
            {
                MongoTelemetry.RecordException(state.CommandActivity, exception);
                state.CommandActivity.Stop();
            }

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
            if (!_shouldTrackFailed)
            {
                return;
            }

            var requestIds = _state.Keys;
            foreach (var requestId in requestIds)
            {
                CommandState state;
                if (_state.TryRemove(requestId, out state))
                {
                    state.Stopwatch.Stop();
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

        private void ProcessRequestMessage(RequestMessage message, ConnectionId connectionId, ObjectId? serviceId, Stream stream, MessageEncoderSettings encoderSettings, Stopwatch stopwatch, bool skipLogging)
        {
            switch (message.MessageType)
            {
                case MongoDBMessageType.Command:
                    ProcessCommandRequestMessage((CommandRequestMessage)message, connectionId, serviceId, new CommandMessageBinaryEncoder(stream, encoderSettings), stopwatch, skipLogging);
                    break;
                case MongoDBMessageType.Query:
                    ProcessQueryMessage((QueryMessage)message, connectionId, new QueryMessageBinaryEncoder(stream, encoderSettings), stopwatch, skipLogging);
                    break;
                default:
                    throw new MongoInternalException("Invalid message type.");
            }
        }

        private void ProcessCommandRequestMessage(CommandRequestMessage message, ConnectionId connectionId, ObjectId? serviceId, CommandMessageBinaryEncoder encoder, Stopwatch stopwatch, bool skipLogging)
        {
            var requestId = message.RequestId;
            var operationId = EventContext.OperationId;

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
                var commandName = command.GetElement(0).Name;
                var databaseName = command["$db"].AsString;
                var databaseNamespace = new DatabaseNamespace(databaseName);
                var shouldRedactCommand = ShouldRedactCommand(command);
                if (shouldRedactCommand)
                {
                    command = new BsonDocument();
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

                if (_shouldTrackState)
                {
                    var commandState = new CommandState
                    {
                        CommandName = commandName,
                        OperationId = operationId,
                        Stopwatch = stopwatch,
                        QueryNamespace = new CollectionNamespace(databaseNamespace, "$cmd"),
                        ExpectedResponseType = decodedMessage.MoreToCome ? ExpectedResponseType.None : ExpectedResponseType.Command,
                        ShouldRedactReply = shouldRedactCommand
                    };

                    if (_shouldTrace && !ShouldRedactCommand(command))
                    {
                        commandState.CommandActivity = MongoTelemetry.StartCommandActivity(
                            commandName,
                            command,
                            databaseNamespace,
                            connectionId,
                            _tracingOptions?.QueryTextMaxLength ?? 0);
                    }

                    _state.TryAdd(requestId, commandState);
                }
            }
        }

        private void ProcessCommandResponseMessage(CommandState state, CommandResponseMessage message, IByteBuffer buffer, ConnectionId connectionId, ObjectId? serviceId, MessageEncoderSettings encoderSettings, bool skipLogging)
        {
            var wrappedMessage = message.WrappedMessage;
            var type0Section = wrappedMessage.Sections.OfType<Type0CommandMessageSection<RawBsonDocument>>().Single();
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
                CompleteCommandActivityWithSuccess(state.CommandActivity, reply);

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

        private void ProcessQueryMessage(QueryMessage originalMessage, ConnectionId connectionId, QueryMessageBinaryEncoder encoder, Stopwatch stopwatch, bool skipLogging)
        {
            var requestId = originalMessage.RequestId;
            var operationId = EventContext.OperationId;

            var decodedMessage = encoder.ReadMessage(RawBsonDocumentSerializer.Instance);
            try
            {
                var isCommand = IsCommand(decodedMessage.CollectionNamespace);
                string commandName;
                BsonDocument command;
                var shouldRedactCommand = false;
                if (isCommand)
                {
                    command = decodedMessage.Query;
                    var firstElement = command.GetElement(0);
                    commandName = firstElement.Name;
                    shouldRedactCommand = ShouldRedactCommand(command);
                    if (shouldRedactCommand)
                    {
                        command = new BsonDocument();
                    }
                }
                else
                {
                    commandName = "find";
                    command = BuildFindCommandFromQuery(decodedMessage);
                    if (decodedMessage.Query.GetValue("$explain", false).ToBoolean())
                    {
                        commandName = "explain";
                        command = new BsonDocument("explain", command);
                    }
                }

                _eventLogger.LogAndPublish(new CommandStartedEvent(
                    commandName,
                    command,
                    decodedMessage.CollectionNamespace.DatabaseNamespace,
                    operationId,
                    requestId,
                    connectionId),
                    skipLogging);

                if (_shouldTrackState)
                {
                    var commandState = new CommandState
                    {
                        CommandName = commandName,
                        OperationId = operationId,
                        Stopwatch = stopwatch,
                        QueryNamespace = decodedMessage.CollectionNamespace,
                        ExpectedResponseType = isCommand ? ExpectedResponseType.Command : ExpectedResponseType.Query,
                        ShouldRedactReply = shouldRedactCommand
                    };

                    if (_shouldTrace && !ShouldRedactCommand(command))
                    {
                        commandState.CommandActivity = MongoTelemetry.StartCommandActivity(
                            commandName,
                            command,
                            decodedMessage.CollectionNamespace.DatabaseNamespace,
                            connectionId,
                            _tracingOptions?.QueryTextMaxLength ?? 0);
                    }

                    _state.TryAdd(requestId, commandState);
                }
            }
            finally
            {
                var disposable = decodedMessage.Query as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
                disposable = decodedMessage.Fields as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
        }

        private void ProcessReplyMessage(CommandState state, ResponseMessage message, IByteBuffer buffer, ConnectionId connectionId, MessageEncoderSettings encoderSettings, bool skipLogging)
        {
            state.Stopwatch.Stop();
            bool disposeOfDocuments = false;
            var replyMessage = message as ReplyMessage<RawBsonDocument>;
            if (replyMessage == null)
            {
                // ReplyMessage is generic, which means that we can't use it here, so, we need to use a different one...
                using (var stream = new ByteBufferStream(buffer, ownsBuffer: false))
                {
                    var encoderFactory = new BinaryMessageEncoderFactory(stream, encoderSettings);
                    replyMessage = (ReplyMessage<RawBsonDocument>)encoderFactory
                        .GetReplyMessageEncoder(RawBsonDocumentSerializer.Instance)
                        .ReadMessage();
                    disposeOfDocuments = true;
                }
            }

            try
            {
                if (replyMessage.CursorNotFound ||
                    replyMessage.QueryFailure ||
                    (state.ExpectedResponseType != ExpectedResponseType.Query && replyMessage.Documents.Count == 0))
                {
                    var queryFailureDocument = replyMessage.QueryFailureDocument;
                    if (state.ShouldRedactReply)
                    {
                        queryFailureDocument = new BsonDocument();
                    }

                    if (_shouldTrackFailed)
                    {
                        _eventLogger.LogAndPublish(new CommandFailedEvent(
                            state.CommandName,
                            state.QueryNamespace.DatabaseNamespace,
                            new MongoCommandException(
                                connectionId,
                                string.Format("{0} command failed", state.CommandName),
                                null,
                                queryFailureDocument),
                            state.OperationId,
                            replyMessage.ResponseTo,
                            connectionId,
                            state.Stopwatch.Elapsed),
                            skipLogging);
                    }
                }
                else
                {
                    switch (state.ExpectedResponseType)
                    {
                        case ExpectedResponseType.Command:
                            ProcessCommandReplyMessage(state, replyMessage, connectionId, skipLogging);
                            break;
                        case ExpectedResponseType.Query:
                            ProcessQueryReplyMessage(state, replyMessage, connectionId, skipLogging);
                            break;
                    }
                }
            }
            finally
            {
                if (disposeOfDocuments && replyMessage.Documents != null)
                {
                    replyMessage.Documents.ForEach(d => d.Dispose());
                }
            }
        }

        private void ProcessCommandReplyMessage(CommandState state, ReplyMessage<RawBsonDocument> replyMessage, ConnectionId connectionId, bool skipLogging)
        {
            BsonDocument reply = replyMessage.Documents[0];
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

            if (!ok.ToBoolean())
            {
                HandleCommandFailure(state, reply, connectionId, serviceId: null, replyMessage.ResponseTo, skipLogging);
            }
            else
            {
                CompleteCommandActivityWithSuccess(state.CommandActivity, reply);

                _eventLogger.LogAndPublish(new CommandSucceededEvent(
                    state.CommandName,
                    reply,
                    state.QueryNamespace.DatabaseNamespace,
                    state.OperationId,
                    replyMessage.ResponseTo,
                    connectionId,
                    state.Stopwatch.Elapsed),
                    skipLogging);
            }
        }

        private void ProcessQueryReplyMessage(CommandState state, ReplyMessage<RawBsonDocument> replyMessage, ConnectionId connectionId, bool skipLogging)
        {
            if (_shouldTrackSucceeded)
            {
                BsonDocument reply;
                if (state.CommandName == "explain")
                {
                    reply = new BsonDocument("ok", 1);
                    reply.Merge(replyMessage.Documents[0]);
                }
                else
                {
                    var batchName = state.CommandName == "find" ? "firstBatch" : "nextBatch";
                    reply = new BsonDocument
                    {
                        { "cursor", new BsonDocument
                                    {
                                        { "id", replyMessage.CursorId },
                                        { "ns", state.QueryNamespace.FullName },
                                        { batchName, new BsonArray(replyMessage.Documents) }
                                    }},
                        { "ok", 1 }
                    };
                }

                CompleteCommandActivityWithSuccess(state.CommandActivity, reply);

                _eventLogger.LogAndPublish(new CommandSucceededEvent(
                    state.CommandName,
                    reply,
                    state.QueryNamespace.DatabaseNamespace,
                    state.OperationId,
                    replyMessage.ResponseTo,
                    connectionId,
                    state.Stopwatch.Elapsed),
                    skipLogging);
            }
        }

        private BsonDocument BuildFindCommandFromQuery(QueryMessage message)
        {
            var batchSize = EventContext.FindOperationBatchSize ?? 0;
            var limit = EventContext.FindOperationLimit ?? 0;
            var command = new BsonDocument
            {
                { "find", message.CollectionNamespace.CollectionName },
                { "projection", message.Fields, message.Fields != null },
                { "skip", message.Skip, message.Skip > 0 },
                { "batchSize", batchSize, batchSize > 0 },
                { "limit", limit, limit != 0 },
                { "awaitData", message.AwaitData, message.AwaitData },
                { "noCursorTimeout", message.NoCursorTimeout, message.NoCursorTimeout },
                { "allowPartialResults", message.PartialOk, message.PartialOk },
                { "tailable", message.TailableCursor, message.TailableCursor },
#pragma warning disable 618
                { "oplogReplay", message.OplogReplay, message.OplogReplay }
#pragma warning restore 618
            };

            var query = message.Query;
            if (query.ElementCount <= 1 && !query.Contains("$query"))
            {
                command["filter"] = query; // can be empty
            }
            else
            {
                foreach (var element in query)
                {
                    switch (element.Name)
                    {
                        case "$query":
                            command["filter"] = element.Value;
                            break;
                        case "$orderby":
                            command["sort"] = element.Value;
                            break;
                        case "$showDiskLoc":
                            command["showRecordId"] = element.Value;
                            break;
                        case "$explain":
                            // explain is special and gets handled elsewhere
                            break;
                        default:
                            if (element.Name.StartsWith("$", StringComparison.Ordinal))
                            {
                                // should we actually remove the $ or not?
                                command[element.Name.Substring(1)] = element.Value;
                            }
                            else
                            {
                                // theoretically, this should never happen and is illegal.
                                // however, we'll push this up anyways and a command-failure
                                // event will likely get thrown.
                                command[element.Name] = element.Value;
                            }
                            break;
                    }
                }
            }

            return command;
        }

        private static bool IsCommand(CollectionNamespace collectionNamespace)
        {
            return collectionNamespace.Equals(collectionNamespace.DatabaseNamespace.CommandCollection);
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

        private void CompleteCommandActivityWithSuccess(Activity activity, BsonDocument reply)
        {
            if (activity == null)
            {
                return;
            }

            if (TryGetCursorId(reply, out var cursorId))
            {
                activity.SetTag("db.mongodb.cursor_id", cursorId);
            }
            activity.SetStatus(ActivityStatusCode.Ok);
            activity.Stop();
        }

        private void HandleCommandFailure(
            CommandState state,
            BsonDocument reply,
            ConnectionId connectionId,
            ObjectId? serviceId,
            int responseTo,
            bool skipLogging)
        {
            MongoCommandException exception = null;

            // Only create exception if needed for span or event
            if (state.CommandActivity != null || _shouldTrackFailed)
            {
                exception = new MongoCommandException(
                    connectionId,
                    $"{state.CommandName} command failed",
                    null,
                    reply);
            }

            if (state.CommandActivity != null)
            {
                MongoTelemetry.RecordException(state.CommandActivity, exception);

                // Add error code if present in reply (spec requires string type)
                if (reply.Contains("code"))
                {
                    state.CommandActivity.SetTag("db.response.status_code", reply["code"].ToString());
                }

                state.CommandActivity.SetTag("exception.stacktrace", "");

                state.CommandActivity.Stop();
            }

            if (_shouldTrackFailed)
            {
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
        }

        private bool TryGetCursorId(BsonDocument reply, out long cursorId)
        {
            cursorId = 0;
            if (reply == null) return false;

            if (reply.TryGetValue("cursor", out var cursorValue) && cursorValue.IsBsonDocument)
            {
                var cursorDoc = cursorValue.AsBsonDocument;
                if (cursorDoc.TryGetValue("id", out var idValue) && idValue.IsInt64)
                {
                    cursorId = idValue.AsInt64;
                    return cursorId != 0;
                }
            }

            return false;
        }

        private enum ExpectedResponseType
        {
            None,
            Query,
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
            public Activity CommandActivity;
        }
    }
}
