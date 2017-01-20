/* Copyright 2013-2016 MongoDB Inc.
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
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Connections
{
    public class BinaryConnection_CommandEventTests : IDisposable
    {
        private Mock<IConnectionInitializer> _mockConnectionInitializer;
        private DnsEndPoint _endPoint;
        private EventCapturer _capturedEvents;
        private MessageEncoderSettings _messageEncoderSettings = new MessageEncoderSettings();
        private BlockingMemoryStream _stream;
        private Mock<IStreamFactory> _mockStreamFactory;
        private BinaryConnection _subject;
        private IDisposable _operationIdDisposer;

        public BinaryConnection_CommandEventTests()
        {
            _capturedEvents = new EventCapturer()
                .Capture<CommandStartedEvent>()
                .Capture<CommandSucceededEvent>()
                .Capture<CommandFailedEvent>();

            _mockStreamFactory = new Mock<IStreamFactory>();

            _endPoint = new DnsEndPoint("localhost", 27017);
            var serverId = new ServerId(new ClusterId(), _endPoint);

            _mockConnectionInitializer = new Mock<IConnectionInitializer>();
            _mockConnectionInitializer.Setup(i => i.InitializeConnectionAsync(It.IsAny<IConnection>(), CancellationToken.None))
                .Returns(() => Task.FromResult(new ConnectionDescription(
                    new ConnectionId(serverId),
                    new IsMasterResult(new BsonDocument()),
                    new BuildInfoResult(new BsonDocument("version", "2.6.3")))));

            _subject = new BinaryConnection(
                serverId: serverId,
                endPoint: _endPoint,
                settings: new ConnectionSettings(),
                streamFactory: _mockStreamFactory.Object,
                connectionInitializer: _mockConnectionInitializer.Object,
                eventSubscriber: _capturedEvents);

            _stream = new BlockingMemoryStream();
            _mockStreamFactory.Setup(f => f.CreateStreamAsync(_endPoint, CancellationToken.None))
                .Returns(Task.FromResult<Stream>(_stream));
            _subject.OpenAsync(CancellationToken.None).Wait();
            _capturedEvents.Clear();

            _operationIdDisposer = EventContext.BeginOperation();
        }

        public void Dispose()
        {
            _stream.Dispose();
            _operationIdDisposer.Dispose();
        }

        [Fact]
        public void Should_process_a_command()
        {
            var expectedCommand = BsonDocument.Parse("{ ismaster: 1 }");
            var expectedReply = BsonDocument.Parse("{ ok: 1 }");

            var requestMessage = MessageHelper.BuildCommand(
                expectedCommand,
                requestId: 10);
            SendMessages(requestMessage);

            var replyMessage = MessageHelper.BuildReply<BsonDocument>(
                expectedReply,
                BsonDocumentSerializer.Instance,
                responseTo: requestMessage.RequestId);
            ReceiveMessages(replyMessage);

            var commandStartedEvent = (CommandStartedEvent)_capturedEvents.Next();
            var commandSucceededEvent = (CommandSucceededEvent)_capturedEvents.Next();

            commandStartedEvent.CommandName.Should().Be(expectedCommand.GetElement(0).Name);
            commandStartedEvent.Command.Should().Be(expectedCommand);
            commandStartedEvent.ConnectionId.Should().Be(_subject.ConnectionId);
            commandStartedEvent.DatabaseNamespace.Should().Be(MessageHelper.DefaultDatabaseNamespace);
            commandStartedEvent.OperationId.Should().Be(EventContext.OperationId);
            commandStartedEvent.RequestId.Should().Be(requestMessage.RequestId);

            commandSucceededEvent.CommandName.Should().Be(commandStartedEvent.CommandName);
            commandSucceededEvent.ConnectionId.Should().Be(commandStartedEvent.ConnectionId);
            commandSucceededEvent.Duration.Should().BeGreaterThan(TimeSpan.Zero);
            commandSucceededEvent.OperationId.Should().Be(commandStartedEvent.OperationId);
            commandSucceededEvent.Reply.Should().Be(expectedReply);
            commandSucceededEvent.RequestId.Should().Be(commandStartedEvent.RequestId);
        }

        [Theory]
        [MemberData("GetRedactedCommands")]
        public void Should_process_a_redacted_command(string commandName)
        {
            var command = BsonDocument.Parse($"{{ {commandName}: 1, extra: true }}");
            var reply = BsonDocument.Parse("{ ok: 1, extra: true }");

            var requestMessage = MessageHelper.BuildCommand(
                command,
                requestId: 10);
            SendMessages(requestMessage);

            var replyMessage = MessageHelper.BuildReply<BsonDocument>(
                reply,
                BsonDocumentSerializer.Instance,
                responseTo: requestMessage.RequestId);
            ReceiveMessages(replyMessage);

            var commandStartedEvent = (CommandStartedEvent)_capturedEvents.Next();
            var commandSucceededEvent = (CommandSucceededEvent)_capturedEvents.Next();

            commandStartedEvent.CommandName.Should().Be(command.GetElement(0).Name);
            commandStartedEvent.Command.ElementCount.Should().Be(0);
            commandStartedEvent.ConnectionId.Should().Be(_subject.ConnectionId);
            commandStartedEvent.DatabaseNamespace.Should().Be(MessageHelper.DefaultDatabaseNamespace);
            commandStartedEvent.OperationId.Should().Be(EventContext.OperationId);
            commandStartedEvent.RequestId.Should().Be(requestMessage.RequestId);

            commandSucceededEvent.CommandName.Should().Be(commandStartedEvent.CommandName);
            commandSucceededEvent.ConnectionId.Should().Be(commandStartedEvent.ConnectionId);
            commandSucceededEvent.Duration.Should().BeGreaterThan(TimeSpan.Zero);
            commandSucceededEvent.OperationId.Should().Be(commandStartedEvent.OperationId);
            commandSucceededEvent.Reply.ElementCount.Should().Be(0);
            commandSucceededEvent.RequestId.Should().Be(commandStartedEvent.RequestId);
        }

        [Fact]
        public void Should_process_a_failed_command()
        {
            var expectedCommand = BsonDocument.Parse("{ ismaster: 1 }");
            var expectedReply = BsonDocument.Parse("{ ok: 0 }");

            var requestMessage = MessageHelper.BuildCommand(
                expectedCommand,
                requestId: 10);
            SendMessages(requestMessage);

            var replyMessage = MessageHelper.BuildReply<BsonDocument>(
                expectedReply,
                BsonDocumentSerializer.Instance,
                responseTo: requestMessage.RequestId);
            ReceiveMessages(replyMessage);

            var commandStartedEvent = (CommandStartedEvent)_capturedEvents.Next();
            var commandFailedEvent = (CommandFailedEvent)_capturedEvents.Next();

            commandStartedEvent.CommandName.Should().Be(expectedCommand.GetElement(0).Name);
            commandStartedEvent.Command.Should().Be(expectedCommand);
            commandStartedEvent.ConnectionId.Should().Be(_subject.ConnectionId);
            commandStartedEvent.DatabaseNamespace.Should().Be(MessageHelper.DefaultDatabaseNamespace);
            commandStartedEvent.OperationId.Should().Be(EventContext.OperationId);
            commandStartedEvent.RequestId.Should().Be(requestMessage.RequestId);

            commandFailedEvent.CommandName.Should().Be(commandStartedEvent.CommandName);
            commandFailedEvent.ConnectionId.Should().Be(commandStartedEvent.ConnectionId);
            commandFailedEvent.Duration.Should().BeGreaterThan(TimeSpan.Zero);
            commandFailedEvent.OperationId.Should().Be(commandStartedEvent.OperationId);
            commandFailedEvent.RequestId.Should().Be(commandStartedEvent.RequestId);
            commandFailedEvent.Failure.Should().BeOfType<MongoCommandException>();
        }

        [Theory]
        [MemberData("GetRedactedCommands")]
        public void Should_process_a_redacted_failed_command(string commandName)
        {
            var command = BsonDocument.Parse($"{{ {commandName}: 1, extra: true }}");
            var reply = BsonDocument.Parse("{ ok: 0, extra: true }");

            var requestMessage = MessageHelper.BuildCommand(
                command,
                requestId: 10);
            SendMessages(requestMessage);

            var replyMessage = MessageHelper.BuildReply<BsonDocument>(
                reply,
                BsonDocumentSerializer.Instance,
                responseTo: requestMessage.RequestId);
            ReceiveMessages(replyMessage);

            var commandStartedEvent = (CommandStartedEvent)_capturedEvents.Next();
            var commandFailedEvent = (CommandFailedEvent)_capturedEvents.Next();

            commandStartedEvent.CommandName.Should().Be(command.GetElement(0).Name);
            commandStartedEvent.Command.ElementCount.Should().Be(0);
            commandStartedEvent.ConnectionId.Should().Be(_subject.ConnectionId);
            commandStartedEvent.DatabaseNamespace.Should().Be(MessageHelper.DefaultDatabaseNamespace);
            commandStartedEvent.OperationId.Should().Be(EventContext.OperationId);
            commandStartedEvent.RequestId.Should().Be(requestMessage.RequestId);

            commandFailedEvent.CommandName.Should().Be(commandStartedEvent.CommandName);
            commandFailedEvent.ConnectionId.Should().Be(commandStartedEvent.ConnectionId);
            commandFailedEvent.Duration.Should().BeGreaterThan(TimeSpan.Zero);
            commandFailedEvent.OperationId.Should().Be(commandStartedEvent.OperationId);
            commandFailedEvent.RequestId.Should().Be(commandStartedEvent.RequestId);
            commandFailedEvent.Failure.Should().BeOfType<MongoCommandException>();
            ((MongoCommandException)commandFailedEvent.Failure).Result.ElementCount.Should().Be(0);
        }

        [Fact]
        public void Should_process_a_delete_without_gle()
        {
            var delete = BsonDocument.Parse("{ q: { x: 1 }, limit: 0 }");
            var expectedCommand = new BsonDocument
            {
                { "delete", MessageHelper.DefaultCollectionNamespace.CollectionName },
                { "deletes", new BsonArray(new [] { delete }) },
                { "writeConcern", WriteConcern.Unacknowledged.ToBsonDocument() }
            };
            var expectedReply = BsonDocument.Parse("{ ok: 1 }");

            var requestMessage = MessageHelper.BuildDelete(
                (BsonDocument)delete["q"],
                requestId: 10,
                isMulti: true);
            SendMessages(requestMessage);

            var commandStartedEvent = (CommandStartedEvent)_capturedEvents.Next();
            var commandSucceededEvent = (CommandSucceededEvent)_capturedEvents.Next();

            commandStartedEvent.CommandName.Should().Be(expectedCommand.GetElement(0).Name);
            commandStartedEvent.Command.Should().Be(expectedCommand);
            commandStartedEvent.ConnectionId.Should().Be(_subject.ConnectionId);
            commandStartedEvent.DatabaseNamespace.Should().Be(MessageHelper.DefaultDatabaseNamespace);
            commandStartedEvent.OperationId.Should().Be(EventContext.OperationId);
            commandStartedEvent.RequestId.Should().Be(requestMessage.RequestId);

            commandSucceededEvent.CommandName.Should().Be(commandStartedEvent.CommandName);
            commandSucceededEvent.ConnectionId.Should().Be(commandStartedEvent.ConnectionId);
            commandSucceededEvent.Duration.Should().BeGreaterThan(TimeSpan.Zero);
            commandSucceededEvent.OperationId.Should().Be(commandStartedEvent.OperationId);
            commandSucceededEvent.Reply.Should().Be(expectedReply);
            commandSucceededEvent.RequestId.Should().Be(commandStartedEvent.RequestId);
        }

        [Fact]
        public void Should_process_a_delete_with_a_gle()
        {
            var delete = BsonDocument.Parse("{ q: { x: 1 }, limit: 0 }");
            var expectedCommand = new BsonDocument
            {
                { "delete", MessageHelper.DefaultCollectionNamespace.CollectionName },
                { "deletes", new BsonArray(new [] { delete }) }
            };
            var expectedReply = BsonDocument.Parse("{ ok: 1, n: 0 }");

            var requestMessage = MessageHelper.BuildDelete(
                (BsonDocument)delete["q"],
                requestId: 10,
                isMulti: true);
            var gleMessage = MessageHelper.BuildCommand(
                BsonDocument.Parse("{ getLastError: 1 }"),
                requestId: requestMessage.RequestId + 1);
            SendMessages(requestMessage, gleMessage);

            var replyMessage = MessageHelper.BuildReply(
                BsonDocument.Parse("{ ok: 1, n: 0 }"),
                serializer: BsonDocumentSerializer.Instance,
                responseTo: gleMessage.RequestId);
            ReceiveMessages(replyMessage);

            var commandStartedEvent = (CommandStartedEvent)_capturedEvents.Next();
            var commandSucceededEvent = (CommandSucceededEvent)_capturedEvents.Next();

            commandStartedEvent.CommandName.Should().Be(expectedCommand.GetElement(0).Name);
            commandStartedEvent.Command.Should().Be(expectedCommand);
            commandStartedEvent.ConnectionId.Should().Be(_subject.ConnectionId);
            commandStartedEvent.DatabaseNamespace.Should().Be(MessageHelper.DefaultDatabaseNamespace);
            commandStartedEvent.OperationId.Should().Be(EventContext.OperationId);
            commandStartedEvent.RequestId.Should().Be(gleMessage.RequestId);

            commandSucceededEvent.CommandName.Should().Be(commandStartedEvent.CommandName);
            commandSucceededEvent.ConnectionId.Should().Be(commandStartedEvent.ConnectionId);
            commandSucceededEvent.Duration.Should().BeGreaterThan(TimeSpan.Zero);
            commandSucceededEvent.OperationId.Should().Be(commandStartedEvent.OperationId);
            commandSucceededEvent.Reply.Should().Be(expectedReply);
            commandSucceededEvent.RequestId.Should().Be(commandStartedEvent.RequestId);
        }

        [Fact]
        public void Should_process_a_get_more()
        {
            var expectedCommand = new BsonDocument
            {
                { "getMore", 20L },
                { "collection", MessageHelper.DefaultCollectionNamespace.CollectionName },
                { "batchSize", 40 }
            };
            var expectedReplyDocuments = new[]
            {
                new BsonDocument("first", 1),
                new BsonDocument("second", 2)
            };
            var expectedReply = new BsonDocument
            {
                { "cursor", new BsonDocument
                            {
                                { "id", expectedCommand["getMore"].ToInt64() },
                                { "ns", MessageHelper.DefaultCollectionNamespace.FullName },
                                { "nextBatch", new BsonArray(expectedReplyDocuments) }
                            }},
                { "ok", 1 }
            };

            var requestMessage = MessageHelper.BuildGetMore(
                requestId: 10,
                cursorId: expectedCommand["getMore"].ToInt64(),
                batchSize: 40);
            SendMessages(requestMessage);

            var replyMessage = MessageHelper.BuildReply<BsonDocument>(
                expectedReplyDocuments,
                BsonDocumentSerializer.Instance,
                responseTo: requestMessage.RequestId,
                cursorId: expectedReply["cursor"]["id"].ToInt64());
            ReceiveMessages(replyMessage);

            var commandStartedEvent = (CommandStartedEvent)_capturedEvents.Next();
            var commandSucceededEvent = (CommandSucceededEvent)_capturedEvents.Next();

            commandStartedEvent.CommandName.Should().Be(expectedCommand.GetElement(0).Name);
            commandStartedEvent.Command.Should().Be(expectedCommand);
            commandStartedEvent.ConnectionId.Should().Be(_subject.ConnectionId);
            commandStartedEvent.DatabaseNamespace.Should().Be(MessageHelper.DefaultDatabaseNamespace);
            commandStartedEvent.OperationId.Should().Be(EventContext.OperationId);
            commandStartedEvent.RequestId.Should().Be(requestMessage.RequestId);

            commandSucceededEvent.CommandName.Should().Be(commandStartedEvent.CommandName);
            commandSucceededEvent.ConnectionId.Should().Be(commandStartedEvent.ConnectionId);
            commandSucceededEvent.Duration.Should().BeGreaterThan(TimeSpan.Zero);
            commandSucceededEvent.OperationId.Should().Be(commandStartedEvent.OperationId);
            commandSucceededEvent.Reply.Should().Be(expectedReply);
            commandSucceededEvent.RequestId.Should().Be(commandStartedEvent.RequestId);
        }

        [Fact]
        public void Should_process_an_insert_without_gle()
        {
            var documents = new[]
            {
                new BsonDocument("x", 1),
                new BsonDocument("x", 2)
            };
            var expectedCommand = new BsonDocument
            {
                { "insert", MessageHelper.DefaultCollectionNamespace.CollectionName },
                { "documents", new BsonArray(documents) },
                { "ordered", true },
                { "writeConcern", WriteConcern.Unacknowledged.ToBsonDocument() },
            };
            var expectedReply = BsonDocument.Parse("{ ok: 1 }");

            var requestMessage = MessageHelper.BuildInsert(
                documents,
                requestId: 10);
            SendMessages(requestMessage);

            var commandStartedEvent = (CommandStartedEvent)_capturedEvents.Next();
            var commandSucceededEvent = (CommandSucceededEvent)_capturedEvents.Next();

            commandStartedEvent.CommandName.Should().Be(expectedCommand.GetElement(0).Name);
            commandStartedEvent.Command.Should().Be(expectedCommand);
            commandStartedEvent.ConnectionId.Should().Be(_subject.ConnectionId);
            commandStartedEvent.DatabaseNamespace.Should().Be(MessageHelper.DefaultDatabaseNamespace);
            commandStartedEvent.OperationId.Should().Be(EventContext.OperationId);
            commandStartedEvent.RequestId.Should().Be(requestMessage.RequestId);

            commandSucceededEvent.CommandName.Should().Be(commandStartedEvent.CommandName);
            commandSucceededEvent.ConnectionId.Should().Be(commandStartedEvent.ConnectionId);
            commandSucceededEvent.Duration.Should().BeGreaterThan(TimeSpan.Zero);
            commandSucceededEvent.OperationId.Should().Be(commandStartedEvent.OperationId);
            commandSucceededEvent.Reply.Should().Be(expectedReply);
            commandSucceededEvent.RequestId.Should().Be(commandStartedEvent.RequestId);
        }

        [Fact]
        public void Should_process_an_insert_with_a_gle()
        {
            var documents = new[]
            {
                new BsonDocument("x", 1),
                new BsonDocument("x", 2)
            };
            var writeConcern = WriteConcern.W2;
            var expectedCommand = new BsonDocument
            {
                { "insert", MessageHelper.DefaultCollectionNamespace.CollectionName },
                { "documents", new BsonArray(documents) },
                { "ordered", true },
                { "writeConcern", BsonDocument.Parse("{ w: 2, wtimeout: 17.0, fsync: true, j: true }") }
            };
            var expectedReply = BsonDocument.Parse("{ ok: 1, n: 2 }");

            var requestMessage = MessageHelper.BuildInsert(
                documents,
                requestId: 10);
            var gleMessage = MessageHelper.BuildCommand(
                BsonDocument.Parse("{ getLastError: 1, w: 2, wtimeout: 17.0, fsync: true, j: true }"),
                requestId: requestMessage.RequestId + 1);
            SendMessages(requestMessage, gleMessage);

            var replyMessage = MessageHelper.BuildReply(
                BsonDocument.Parse("{ ok: 1, n: 0 }"),
                serializer: BsonDocumentSerializer.Instance,
                responseTo: gleMessage.RequestId);
            ReceiveMessages(replyMessage);

            var commandStartedEvent = (CommandStartedEvent)_capturedEvents.Next();
            var commandSucceededEvent = (CommandSucceededEvent)_capturedEvents.Next();

            commandStartedEvent.CommandName.Should().Be(expectedCommand.GetElement(0).Name);
            commandStartedEvent.Command.Should().Be(expectedCommand);
            commandStartedEvent.ConnectionId.Should().Be(_subject.ConnectionId);
            commandStartedEvent.DatabaseNamespace.Should().Be(MessageHelper.DefaultDatabaseNamespace);
            commandStartedEvent.OperationId.Should().Be(EventContext.OperationId);
            commandStartedEvent.RequestId.Should().Be(gleMessage.RequestId);

            commandSucceededEvent.CommandName.Should().Be(commandStartedEvent.CommandName);
            commandSucceededEvent.ConnectionId.Should().Be(commandStartedEvent.ConnectionId);
            commandSucceededEvent.Duration.Should().BeGreaterThan(TimeSpan.Zero);
            commandSucceededEvent.OperationId.Should().Be(commandStartedEvent.OperationId);
            commandSucceededEvent.Reply.Should().Be(expectedReply);
            commandSucceededEvent.RequestId.Should().Be(commandStartedEvent.RequestId);
        }

        [Fact]
        public void Should_process_kill_cursors()
        {
            var expectedCommand = BsonDocument.Parse("{ killCursors: 'bar', cursors: [NumberLong(20)] }");
            var expectedReply = BsonDocument.Parse("{ ok: 1, cursorsUnknown: [NumberLong(20)] }");

            var requestMessage = MessageHelper.BuildKillCursors(10, 20);
            using (EventContext.BeginKillCursors(MessageHelper.DefaultCollectionNamespace))
            {
                SendMessages(requestMessage);
            }

            var commandStartedEvent = (CommandStartedEvent)_capturedEvents.Next();
            var commandSucceededEvent = (CommandSucceededEvent)_capturedEvents.Next();

            commandStartedEvent.CommandName.Should().Be(expectedCommand.GetElement(0).Name);
            commandStartedEvent.Command.Should().Be(expectedCommand);
            commandStartedEvent.ConnectionId.Should().Be(_subject.ConnectionId);
            commandStartedEvent.DatabaseNamespace.Should().Be(MessageHelper.DefaultDatabaseNamespace);
            commandStartedEvent.OperationId.Should().Be(EventContext.OperationId);
            commandStartedEvent.RequestId.Should().Be(requestMessage.RequestId);

            commandSucceededEvent.CommandName.Should().Be(commandStartedEvent.CommandName);
            commandSucceededEvent.ConnectionId.Should().Be(commandStartedEvent.ConnectionId);
            commandSucceededEvent.Duration.Should().BeGreaterThan(TimeSpan.Zero);
            commandSucceededEvent.OperationId.Should().Be(commandStartedEvent.OperationId);
            commandSucceededEvent.Reply.Should().Be(expectedReply);
            commandSucceededEvent.RequestId.Should().Be(commandStartedEvent.RequestId);
        }

        [Fact]
        public void Should_process_a_query_without_modifiers()
        {
            var expectedCommand = new BsonDocument
            {
                { "find", MessageHelper.DefaultCollectionNamespace.CollectionName },
                { "filter", new BsonDocument("x", 1) },
            };
            var expectedReplyDocuments = new[]
            {
                new BsonDocument("first", 1),
                new BsonDocument("second", 2)
            };
            var expectedReply = new BsonDocument
            {
                { "cursor", new BsonDocument
                            {
                                { "id", 20L },
                                { "ns", MessageHelper.DefaultCollectionNamespace.FullName },
                                { "firstBatch", new BsonArray(expectedReplyDocuments) }
                            }},
                { "ok", 1 }
            };

            var requestMessage = MessageHelper.BuildQuery(
                (BsonDocument)expectedCommand["filter"],
                requestId: 10);
            SendMessages(requestMessage);


            var replyMessage = MessageHelper.BuildReply<BsonDocument>(
                expectedReplyDocuments,
                BsonDocumentSerializer.Instance,
                responseTo: requestMessage.RequestId,
                cursorId: expectedReply["cursor"]["id"].ToInt64());
            ReceiveMessages(replyMessage);

            var commandStartedEvent = (CommandStartedEvent)_capturedEvents.Next();
            var commandSucceededEvent = (CommandSucceededEvent)_capturedEvents.Next();

            commandStartedEvent.CommandName.Should().Be(expectedCommand.GetElement(0).Name);
            commandStartedEvent.Command.Should().Be(expectedCommand);
            commandStartedEvent.ConnectionId.Should().Be(_subject.ConnectionId);
            commandStartedEvent.DatabaseNamespace.Should().Be(MessageHelper.DefaultDatabaseNamespace);
            commandStartedEvent.OperationId.Should().Be(EventContext.OperationId);
            commandStartedEvent.RequestId.Should().Be(requestMessage.RequestId);

            commandSucceededEvent.CommandName.Should().Be(commandStartedEvent.CommandName);
            commandSucceededEvent.ConnectionId.Should().Be(commandStartedEvent.ConnectionId);
            commandSucceededEvent.Duration.Should().BeGreaterThan(TimeSpan.Zero);
            commandSucceededEvent.OperationId.Should().Be(commandStartedEvent.OperationId);
            commandSucceededEvent.Reply.Should().Be(expectedReply);
            commandSucceededEvent.RequestId.Should().Be(commandStartedEvent.RequestId);
        }

        [Fact]
        public void Should_process_a_query_with_modifiers()
        {
            var expectedCommand = new BsonDocument
            {
                { "find", MessageHelper.DefaultCollectionNamespace.CollectionName },
                { "projection", new BsonDocument("b", 1) },
                { "skip", 1 },
                { "batchSize", 2 },
                { "limit", 100 },
                { "awaitData", true },
                { "noCursorTimeout", true },
                { "allowPartialResults", true },
                { "tailable", true },
                { "oplogReplay", true },
                { "filter", new BsonDocument("x", 1) },
                { "sort", new BsonDocument("a", -1) },
                { "comment", "funny" },
                { "maxTimeMS", 20 },
                { "showRecordId", true },
            };
            var expectedReplyDocuments = new[]
            {
                new BsonDocument("first", 1),
                new BsonDocument("second", 2)
            };
            var expectedReply = new BsonDocument
            {
                { "cursor", new BsonDocument
                            {
                                { "id", 20L },
                                { "ns", MessageHelper.DefaultCollectionNamespace.FullName },
                                { "firstBatch", new BsonArray(expectedReplyDocuments) }
                            }},
                { "ok", 1 }
            };
            var query = new BsonDocument
            {
                { "$query", expectedCommand["filter"] },
                { "$orderby", expectedCommand["sort"] },
                { "$comment", expectedCommand["comment"] },
                { "$maxTimeMS", expectedCommand["maxTimeMS"] },
                { "$showDiskLoc", expectedCommand["showRecordId"] },
            };
            var requestMessage = MessageHelper.BuildQuery(
                query,
                fields: (BsonDocument)expectedCommand["projection"],
                requestId: 10,
                skip: expectedCommand["skip"].ToInt32(),
                batchSize: expectedCommand["batchSize"].ToInt32(),
                noCursorTimeout: expectedCommand["noCursorTimeout"].ToBoolean(),
                awaitData: expectedCommand["awaitData"].ToBoolean(),
                partialOk: expectedCommand["allowPartialResults"].ToBoolean(),
                tailableCursor: expectedCommand["tailable"].ToBoolean(),
                oplogReplay: expectedCommand["oplogReplay"].ToBoolean());

            using (EventContext.BeginFind(expectedCommand["batchSize"].ToInt32(), expectedCommand["limit"].ToInt32()))
            {
                SendMessages(requestMessage);
            }

            var replyMessage = MessageHelper.BuildReply<BsonDocument>(
                expectedReplyDocuments,
                BsonDocumentSerializer.Instance,
                responseTo: requestMessage.RequestId,
                cursorId: expectedReply["cursor"]["id"].ToInt64());
            ReceiveMessages(replyMessage);

            var commandStartedEvent = (CommandStartedEvent)_capturedEvents.Next();
            var commandSucceededEvent = (CommandSucceededEvent)_capturedEvents.Next();

            commandStartedEvent.CommandName.Should().Be(expectedCommand.GetElement(0).Name);
            commandStartedEvent.Command.Should().Be(expectedCommand);
            commandStartedEvent.ConnectionId.Should().Be(_subject.ConnectionId);
            commandStartedEvent.DatabaseNamespace.Should().Be(MessageHelper.DefaultDatabaseNamespace);
            commandStartedEvent.OperationId.Should().Be(EventContext.OperationId);
            commandStartedEvent.RequestId.Should().Be(requestMessage.RequestId);

            commandSucceededEvent.CommandName.Should().Be(commandStartedEvent.CommandName);
            commandSucceededEvent.ConnectionId.Should().Be(commandStartedEvent.ConnectionId);
            commandSucceededEvent.Duration.Should().BeGreaterThan(TimeSpan.Zero);
            commandSucceededEvent.OperationId.Should().Be(commandStartedEvent.OperationId);
            commandSucceededEvent.Reply.Should().Be(expectedReply);
            commandSucceededEvent.RequestId.Should().Be(commandStartedEvent.RequestId);
        }

        [Fact]
        public void Should_process_a_query_with_the_explain_modifier()
        {
            var expectedCommand = new BsonDocument("explain", new BsonDocument
            {
                { "find", MessageHelper.DefaultCollectionNamespace.CollectionName },
                { "filter", new BsonDocument("x", 1) },
            });
            var expectedReply = BsonDocument.Parse("{ ok: 1, somethingExplainish: 'yeah' }");

            var query = new BsonDocument
            {
                { "$query", expectedCommand["explain"]["filter"] },
                { "$explain", 1 },
            };
            var requestMessage = MessageHelper.BuildQuery(
                query,
                requestId: 10);
            SendMessages(requestMessage);

            var replyMessage = MessageHelper.BuildReply(
                expectedReply,
                BsonDocumentSerializer.Instance,
                responseTo: requestMessage.RequestId);
            ReceiveMessages(replyMessage);

            var commandStartedEvent = (CommandStartedEvent)_capturedEvents.Next();
            var commandSucceededEvent = (CommandSucceededEvent)_capturedEvents.Next();

            commandStartedEvent.CommandName.Should().Be(expectedCommand.GetElement(0).Name);
            commandStartedEvent.Command.Should().Be(expectedCommand);
            commandStartedEvent.ConnectionId.Should().Be(_subject.ConnectionId);
            commandStartedEvent.DatabaseNamespace.Should().Be(MessageHelper.DefaultDatabaseNamespace);
            commandStartedEvent.OperationId.Should().Be(EventContext.OperationId);
            commandStartedEvent.RequestId.Should().Be(requestMessage.RequestId);

            commandSucceededEvent.CommandName.Should().Be(commandStartedEvent.CommandName);
            commandSucceededEvent.ConnectionId.Should().Be(commandStartedEvent.ConnectionId);
            commandSucceededEvent.Duration.Should().BeGreaterThan(TimeSpan.Zero);
            commandSucceededEvent.OperationId.Should().Be(commandStartedEvent.OperationId);
            commandSucceededEvent.Reply.Should().Be(expectedReply);
            commandSucceededEvent.RequestId.Should().Be(commandStartedEvent.RequestId);
        }

        [Fact]
        public void Should_process_a_failed_query()
        {
            var expectedCommand = new BsonDocument
            {
                { "find", MessageHelper.DefaultCollectionNamespace.CollectionName },
                { "filter", new BsonDocument("x", 1) },
            };
            var queryFailureDocument = BsonDocument.Parse("{ $err: \"Can't canonicalize query: BadValue $or needs an array\", code: 17287 }");

            var requestMessage = MessageHelper.BuildQuery(
                (BsonDocument)expectedCommand["filter"],
                requestId: 10);
            SendMessages(requestMessage);


            var replyMessage = MessageHelper.BuildQueryFailedReply<BsonDocument>(
                queryFailureDocument,
                requestMessage.RequestId);
            ReceiveMessages(replyMessage);

            var commandStartedEvent = (CommandStartedEvent)_capturedEvents.Next();
            var commandFailedEvent = (CommandFailedEvent)_capturedEvents.Next();

            commandStartedEvent.CommandName.Should().Be(expectedCommand.GetElement(0).Name);
            commandStartedEvent.Command.Should().Be(expectedCommand);
            commandStartedEvent.ConnectionId.Should().Be(_subject.ConnectionId);
            commandStartedEvent.DatabaseNamespace.Should().Be(MessageHelper.DefaultDatabaseNamespace);
            commandStartedEvent.OperationId.Should().Be(EventContext.OperationId);
            commandStartedEvent.RequestId.Should().Be(requestMessage.RequestId);

            commandFailedEvent.CommandName.Should().Be(commandStartedEvent.CommandName);
            commandFailedEvent.ConnectionId.Should().Be(commandStartedEvent.ConnectionId);
            commandFailedEvent.Duration.Should().BeGreaterThan(TimeSpan.Zero);
            commandFailedEvent.OperationId.Should().Be(commandStartedEvent.OperationId);
            ((MongoCommandException)commandFailedEvent.Failure).Result.Should().Be(queryFailureDocument);
            commandFailedEvent.RequestId.Should().Be(commandStartedEvent.RequestId);
        }

        [Fact]
        public void Should_process_an_update_without_gle()
        {
            var update = BsonDocument.Parse("{ q: { x: 1 }, u: { $set: { x: 2 } }, upsert: false, multi: false }");
            var expectedCommand = new BsonDocument
            {
                { "update", MessageHelper.DefaultCollectionNamespace.CollectionName },
                { "updates", new BsonArray(new [] { update }) },
                { "writeConcern", WriteConcern.Unacknowledged.ToBsonDocument() }
            };
            var expectedReply = BsonDocument.Parse("{ ok: 1 }");

            var requestMessage = MessageHelper.BuildUpdate(
                (BsonDocument)update["q"],
                (BsonDocument)update["u"],
                requestId: 10,
                isMulti: update.GetValue("multi", false).ToBoolean(),
                isUpsert: update.GetValue("upsert", false).ToBoolean());
            SendMessages(requestMessage);

            var commandStartedEvent = (CommandStartedEvent)_capturedEvents.Next();
            var commandSucceededEvent = (CommandSucceededEvent)_capturedEvents.Next();

            commandStartedEvent.CommandName.Should().Be(expectedCommand.GetElement(0).Name);
            commandStartedEvent.Command.Should().Be(expectedCommand);
            commandStartedEvent.ConnectionId.Should().Be(_subject.ConnectionId);
            commandStartedEvent.DatabaseNamespace.Should().Be(MessageHelper.DefaultDatabaseNamespace);
            commandStartedEvent.OperationId.Should().Be(EventContext.OperationId);
            commandStartedEvent.RequestId.Should().Be(requestMessage.RequestId);

            commandSucceededEvent.CommandName.Should().Be(commandStartedEvent.CommandName);
            commandSucceededEvent.ConnectionId.Should().Be(commandStartedEvent.ConnectionId);
            commandSucceededEvent.Duration.Should().BeGreaterThan(TimeSpan.Zero);
            commandSucceededEvent.OperationId.Should().Be(commandStartedEvent.OperationId);
            commandSucceededEvent.Reply.Should().Be(expectedReply);
            commandSucceededEvent.RequestId.Should().Be(commandStartedEvent.RequestId);
        }

        [Fact]
        public void Should_process_an_update_with_a_gle()
        {
            var update = BsonDocument.Parse("{ q: { x: 1 }, u: { $set: { x: 2 } }, upsert: true, multi: true }");
            var expectedCommand = new BsonDocument
            {
                { "update", MessageHelper.DefaultCollectionNamespace.CollectionName },
                { "updates", new BsonArray(new [] { update }) },
                { "writeConcern", new BsonDocument("w", 2)}
            };
            var expectedReply = BsonDocument.Parse("{ ok: 1, n: 1, upserted: [ { index: 0, _id: undefined } ] }");

            var requestMessage = MessageHelper.BuildUpdate(
                (BsonDocument)update["q"],
                (BsonDocument)update["u"],
                requestId: 10,
                isMulti: update.GetValue("multi", false).ToBoolean(),
                isUpsert: update.GetValue("upsert", false).ToBoolean());
            var gleMessage = MessageHelper.BuildCommand(
                BsonDocument.Parse("{ getLastError: 1, w: 2 }"),
                requestId: requestMessage.RequestId + 1);
            SendMessages(requestMessage, gleMessage);

            var replyMessage = MessageHelper.BuildReply(
                BsonDocument.Parse("{ ok: 1, n: 1 }"),
                serializer: BsonDocumentSerializer.Instance,
                responseTo: gleMessage.RequestId);
            ReceiveMessages(replyMessage);

            var commandStartedEvent = (CommandStartedEvent)_capturedEvents.Next();
            var commandSucceededEvent = (CommandSucceededEvent)_capturedEvents.Next();

            commandStartedEvent.CommandName.Should().Be(expectedCommand.GetElement(0).Name);
            commandStartedEvent.Command.Should().Be(expectedCommand);
            commandStartedEvent.ConnectionId.Should().Be(_subject.ConnectionId);
            commandStartedEvent.DatabaseNamespace.Should().Be(MessageHelper.DefaultDatabaseNamespace);
            commandStartedEvent.OperationId.Should().Be(EventContext.OperationId);
            commandStartedEvent.RequestId.Should().Be(gleMessage.RequestId);

            commandSucceededEvent.CommandName.Should().Be(commandStartedEvent.CommandName);
            commandSucceededEvent.ConnectionId.Should().Be(commandStartedEvent.ConnectionId);
            commandSucceededEvent.Duration.Should().BeGreaterThan(TimeSpan.Zero);
            commandSucceededEvent.OperationId.Should().Be(commandStartedEvent.OperationId);
            commandSucceededEvent.Reply.Should().Be(expectedReply);
            commandSucceededEvent.RequestId.Should().Be(commandStartedEvent.RequestId);
        }

        [Fact]
        public void Should_process_an_upsert_with_a_gle_where_the_server_does_not_return_the_upserted_id()
        {
            var update = BsonDocument.Parse("{ q: { _id: 10, x: 1 }, u: { $set: { x: 2 } }, upsert: true, multi: true }");
            var expectedCommand = new BsonDocument
            {
                { "update", MessageHelper.DefaultCollectionNamespace.CollectionName },
                { "updates", new BsonArray(new [] { update }) },
                { "writeConcern", new BsonDocument("w", 2)}
            };
            var expectedReply = BsonDocument.Parse("{ ok: 1, n: 1, upserted: [ { index: 0, _id: 10 } ] }");

            var requestMessage = MessageHelper.BuildUpdate(
                (BsonDocument)update["q"],
                (BsonDocument)update["u"],
                requestId: 10,
                isMulti: update.GetValue("multi", false).ToBoolean(),
                isUpsert: update.GetValue("upsert", false).ToBoolean());
            var gleMessage = MessageHelper.BuildCommand(
                BsonDocument.Parse("{ getLastError: 1, w: 2 }"),
                requestId: requestMessage.RequestId + 1);
            SendMessages(requestMessage, gleMessage);

            var replyMessage = MessageHelper.BuildReply(
                BsonDocument.Parse("{ ok: 1, n: 1 }"),
                serializer: BsonDocumentSerializer.Instance,
                responseTo: gleMessage.RequestId);
            ReceiveMessages(replyMessage);

            var commandStartedEvent = (CommandStartedEvent)_capturedEvents.Next();
            var commandSucceededEvent = (CommandSucceededEvent)_capturedEvents.Next();

            commandStartedEvent.CommandName.Should().Be(expectedCommand.GetElement(0).Name);
            commandStartedEvent.Command.Should().Be(expectedCommand);
            commandStartedEvent.ConnectionId.Should().Be(_subject.ConnectionId);
            commandStartedEvent.DatabaseNamespace.Should().Be(MessageHelper.DefaultDatabaseNamespace);
            commandStartedEvent.OperationId.Should().Be(EventContext.OperationId);
            commandStartedEvent.RequestId.Should().Be(gleMessage.RequestId);

            commandSucceededEvent.CommandName.Should().Be(commandStartedEvent.CommandName);
            commandSucceededEvent.ConnectionId.Should().Be(commandStartedEvent.ConnectionId);
            commandSucceededEvent.Duration.Should().BeGreaterThan(TimeSpan.Zero);
            commandSucceededEvent.OperationId.Should().Be(commandStartedEvent.OperationId);
            commandSucceededEvent.Reply.Should().Be(expectedReply);
            commandSucceededEvent.RequestId.Should().Be(commandStartedEvent.RequestId);
        }

        [Fact]
        public void Should_process_a_write_with_a_write_error()
        {
            var delete = BsonDocument.Parse("{ q: { x: 1 }, limit: 0 }");
            var expectedCommand = new BsonDocument
            {
                { "delete", MessageHelper.DefaultCollectionNamespace.CollectionName },
                { "deletes", new BsonArray(new [] { delete }) }
            };
            var expectedReply = BsonDocument.Parse("{ ok: 1, n: 0, writeErrors: [{ index: 0, code: 10, errmsg: 'oops' }] }");

            var requestMessage = MessageHelper.BuildDelete(
                (BsonDocument)delete["q"],
                requestId: 10,
                isMulti: true);
            var gleMessage = MessageHelper.BuildCommand(
                BsonDocument.Parse("{ getLastError: 1 }"),
                requestId: requestMessage.RequestId + 1);
            SendMessages(requestMessage, gleMessage);

            var replyMessage = MessageHelper.BuildReply(
                BsonDocument.Parse("{ ok: 1, n: 0, code: 10, err: 'oops' }"),
                serializer: BsonDocumentSerializer.Instance,
                responseTo: gleMessage.RequestId);
            ReceiveMessages(replyMessage);

            var commandStartedEvent = (CommandStartedEvent)_capturedEvents.Next();
            var commandSucceededEvent = (CommandSucceededEvent)_capturedEvents.Next();

            commandStartedEvent.CommandName.Should().Be(expectedCommand.GetElement(0).Name);
            commandStartedEvent.Command.Should().Be(expectedCommand);
            commandStartedEvent.ConnectionId.Should().Be(_subject.ConnectionId);
            commandStartedEvent.DatabaseNamespace.Should().Be(MessageHelper.DefaultDatabaseNamespace);
            commandStartedEvent.OperationId.Should().Be(EventContext.OperationId);
            commandStartedEvent.RequestId.Should().Be(gleMessage.RequestId);

            commandSucceededEvent.CommandName.Should().Be(commandStartedEvent.CommandName);
            commandSucceededEvent.ConnectionId.Should().Be(commandStartedEvent.ConnectionId);
            commandSucceededEvent.Duration.Should().BeGreaterThan(TimeSpan.Zero);
            commandSucceededEvent.OperationId.Should().Be(commandStartedEvent.OperationId);
            commandSucceededEvent.Reply.Should().Be(expectedReply);
            commandSucceededEvent.RequestId.Should().Be(commandStartedEvent.RequestId);
        }

        [Fact]
        public void Should_process_a_write_with_a_write_concern_error()
        {
            var delete = BsonDocument.Parse("{ q: { x: 1 }, limit: 0 }");
            var expectedCommand = new BsonDocument
            {
                { "delete", MessageHelper.DefaultCollectionNamespace.CollectionName },
                { "deletes", new BsonArray(new [] { delete }) }
            };
            var expectedReply = BsonDocument.Parse("{ ok: 1, n: 0, writeConcernError: { code: 10, errmsg: 'wnote indicates ...' } }");

            var requestMessage = MessageHelper.BuildDelete(
                (BsonDocument)delete["q"],
                requestId: 10,
                isMulti: true);
            var gleMessage = MessageHelper.BuildCommand(
                BsonDocument.Parse("{ getLastError: 1 }"),
                requestId: requestMessage.RequestId + 1);
            SendMessages(requestMessage, gleMessage);

            var replyMessage = MessageHelper.BuildReply(
                BsonDocument.Parse("{ ok: 1, n: 0, code: 10, err: 'wnote indicates ...' }"),
                serializer: BsonDocumentSerializer.Instance,
                responseTo: gleMessage.RequestId);
            ReceiveMessages(replyMessage);

            var commandStartedEvent = (CommandStartedEvent)_capturedEvents.Next();
            var commandSucceededEvent = (CommandSucceededEvent)_capturedEvents.Next();

            commandStartedEvent.CommandName.Should().Be(expectedCommand.GetElement(0).Name);
            commandStartedEvent.Command.Should().Be(expectedCommand);
            commandStartedEvent.ConnectionId.Should().Be(_subject.ConnectionId);
            commandStartedEvent.DatabaseNamespace.Should().Be(MessageHelper.DefaultDatabaseNamespace);
            commandStartedEvent.OperationId.Should().Be(EventContext.OperationId);
            commandStartedEvent.RequestId.Should().Be(gleMessage.RequestId);

            commandSucceededEvent.CommandName.Should().Be(commandStartedEvent.CommandName);
            commandSucceededEvent.ConnectionId.Should().Be(commandStartedEvent.ConnectionId);
            commandSucceededEvent.Duration.Should().BeGreaterThan(TimeSpan.Zero);
            commandSucceededEvent.OperationId.Should().Be(commandStartedEvent.OperationId);
            commandSucceededEvent.Reply.Should().Be(expectedReply);
            commandSucceededEvent.RequestId.Should().Be(commandStartedEvent.RequestId);
        }

        private void SendMessages(params RequestMessage[] messages)
        {
            _subject.SendMessagesAsync(messages, _messageEncoderSettings, CancellationToken.None).Wait();
        }

        private void ReceiveMessages(params ReplyMessage<BsonDocument>[] messages)
        {
            MessageHelper.WriteResponsesToStream(_stream, messages);
            var encoderSelector = new ReplyMessageEncoderSelector<BsonDocument>(BsonDocumentSerializer.Instance);
            foreach (var message in messages)
            {
                _subject.ReceiveMessageAsync(message.ResponseTo, encoderSelector, _messageEncoderSettings, CancellationToken.None).Wait();
            }
        }

        private static IEnumerable<object[]> GetRedactedCommands()
        {
            var commands = (IEnumerable<string>)typeof(CommandEventHelper)
                .GetField("__securitySensitiveCommands", BindingFlags.Static | BindingFlags.NonPublic)
                .GetValue(null);
            return commands.Select(c => new object[] { c });
        }
    }
}