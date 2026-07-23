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
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.WireProtocol
{
    public class CommandWriteProtocolTests
    {
        private static readonly ClusterId __clusterId = new ClusterId();
        private static readonly ServerId __serverId = new ServerId(__clusterId, new DnsEndPoint("localhost", 27017));
        private static readonly ConnectionDescription __connectionDescription = new ConnectionDescription(
            new ConnectionId(__serverId),
            new HelloResult(
                new BsonDocument("ok", 1)
                .Add(OppressiveLanguageConstants.LegacyHelloResponseIsWritablePrimaryFieldName, 1)
                .Add("maxWireVersion", WireVersion.Server49)));

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_reset_streamable_state_when_connection_changes([Values(false, true)] bool withSameConnection)
        {
            var messageEncoderSettings = new MessageEncoderSettings();
            var subject = new CommandWireProtocol<BsonDocument>(
                NoCoreSession.Instance,
                null,
                ReadPreference.Primary,
                new DatabaseNamespace("test"),
                new BsonDocument("cmd", 1),
                null, // commandPayloads
                null, // postWriteAction
                CommandResponseHandling.Return,
                BsonDocumentSerializer.Instance,
                messageEncoderSettings,
                null, // serverApi
                TimeSpan.Zero);

            var connectionId1 = new ConnectionId(__serverId);
            var connection1 = CreateConnection(connectionId1);

            using var operationContext = new OperationContext(NoCoreSession.NewHandle());
            subject.Execute(operationContext, connection1);

            connection1.GetSentMessages().Count.Should().Be(1);
            subject._lastConnectionId().Should().BeSameAs(connectionId1);
            subject._moreToCome().Should().BeFalse();

            // Simulate that the previous response asked us to keep streaming on the same connection.
            subject._moreToCome(true);

            if (withSameConnection)
            {
                connection1.EnqueueCommandResponseMessage(MessageHelper.BuildCommandResponse(CreateRawBsonDocument(new BsonDocument("ok", 1))));

                subject.Execute(operationContext, connection1);

                // streaming continues on the same connection: no new command message is sent, only a response is received
                connection1.GetSentMessages().Count.Should().Be(1);
                subject._lastConnectionId().Should().BeSameAs(connectionId1);
            }
            else
            {
                var connectionId2 = new ConnectionId(new ServerId(new ClusterId(IdGenerator<ClusterId>.GetNextId()), new DnsEndPoint("localhost", 27017)));
                var connection2 = CreateConnection(connectionId2);

                subject.Execute(operationContext, connection2);

                // streaming state is reset for the new connection: a fresh command message is sent
                connection2.GetSentMessages().Count.Should().Be(1);
                subject._lastConnectionId().Should().BeSameAs(connectionId2);
                subject._moreToCome().Should().BeFalse();
            }

            MockConnection CreateConnection(ConnectionId connectionId)
            {
                var connection = new MockConnection(connectionId, new ConnectionSettings(), null);
                connection.Description = new ConnectionDescription(
                    connectionId,
                    new HelloResult(new BsonDocument { { "ok", 1 }, { "maxWireVersion", WireVersion.Server44 } }));
                connection.EnqueueCommandResponseMessage(MessageHelper.BuildCommandResponse(CreateRawBsonDocument(new BsonDocument("ok", 1))));
                return connection;
            }
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Execute_should_gossip_the_greater_of_the_session_and_cluster_clock_cluster_time(
            [Values(false, true)] bool async,
            [Values("sessionHasNoClusterTime", "sessionClusterTimeIsLower", "sessionClusterTimeIsHigher")] string scenario)
        {
            var lowerClusterTime = new BsonDocument("clusterTime", new BsonTimestamp(1L));
            var higherClusterTime = new BsonDocument("clusterTime", new BsonTimestamp(2L));

            // In every scenario the greater of the session's and the cluster clock's cluster time (higherClusterTime) must be gossiped.
            var clusterClock = new ClusterClock();
            ICoreSessionHandle session;
            switch (scenario)
            {
                case "sessionHasNoClusterTime": // fresh implicit session that hasn't seen a cluster time yet
                    clusterClock.AdvanceClusterTime(higherClusterTime);
                    session = NoCoreSession.NewHandle(); // ClusterTime is null
                    break;
                case "sessionClusterTimeIsLower":
                    clusterClock.AdvanceClusterTime(higherClusterTime);
                    session = CreateSessionWithClusterTime(lowerClusterTime);
                    break;
                case "sessionClusterTimeIsHigher":
                    clusterClock.AdvanceClusterTime(lowerClusterTime);
                    session = CreateSessionWithClusterTime(higherClusterTime);
                    break;
                default:
                    throw new NotSupportedException($"scenario {scenario} is not supported");
            }

            var connection = new MockConnection();
            connection.Description = __connectionDescription;
            connection.EnqueueCommandResponseMessage(MessageHelper.BuildCommandResponse(CreateRawBsonDocument(new BsonDocument("ok", 1))));

            var subject = new CommandWireProtocol<BsonDocument>(
                session,
                clusterClock,
                ReadPreference.Primary,
                new DatabaseNamespace("test"),
                new BsonDocument("cmd", 1),
                commandPayloads: null,
                postWriteAction: null,
                CommandResponseHandling.Return,
                BsonDocumentSerializer.Instance,
                new MessageEncoderSettings(),
                null, // serverApi
                TimeSpan.Zero);

            using var operationContext = new OperationContext(session);
            if (async)
            {
                await subject.ExecuteAsync(operationContext, connection);
            }
            else
            {
                subject.Execute(operationContext, connection);
            }

            SpinWait.SpinUntil(() => connection.GetSentMessages().Count >= 1, TimeSpan.FromSeconds(4)).Should().BeTrue();

            var command = MessageHelper.ToCommandPayload(connection.GetSentMessages()[0]);
            command["$clusterTime"].Should().Be(higherClusterTime);

            ICoreSessionHandle CreateSessionWithClusterTime(BsonDocument clusterTime)
            {
                var mockSession = new Mock<ICoreSessionHandle>();
                mockSession.SetupGet(m => m.ClusterTime).Returns(clusterTime);
                return mockSession.Object;
            }
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Execute_should_use_serverApi_with_getMoreCommand(
            [Values(false, true)] bool useServerApi,
            [Values(false, true)] bool async)
        {
            var serverApi = useServerApi ? new ServerApi(ServerApiVersion.V1, true, true) : null;

            var connection = new MockConnection();
            connection.Description = __connectionDescription;
            var commandResponse = MessageHelper.BuildCommandResponse(CreateRawBsonDocument(new BsonDocument("ok", 1)));
            connection.EnqueueCommandResponseMessage(commandResponse);
            var subject = new CommandWireProtocol<BsonDocument>(
                NoCoreSession.Instance,
                null,
                ReadPreference.Primary,
                new DatabaseNamespace("test"),
                new BsonDocument("getMore", 1),
                commandPayloads: null,
                postWriteAction: null,
                CommandResponseHandling.Return,
                BsonDocumentSerializer.Instance,
                new MessageEncoderSettings(),
                serverApi,
                TimeSpan.FromMilliseconds(42));

            using var operationContext = new OperationContext(NoCoreSession.NewHandle());
            if (async)
            {
                await subject.ExecuteAsync(operationContext, connection);
            }
            else
            {
                subject.Execute(operationContext, connection);
            }

            SpinWait.SpinUntil(() => connection.GetSentMessages().Count >= 1, TimeSpan.FromSeconds(4)).Should().BeTrue();

            var sentMessages = connection.GetSentMessages();
            sentMessages.Count.Should().Be(1);
            var expectedServerApiString = useServerApi ? ", apiVersion : '1', apiStrict : true, apiDeprecationErrors : true" : "";
            MessageHelper.ToCommandPayload(sentMessages[0]).Should().Be($"{{ getMore : 1, $db : 'test'{expectedServerApiString} }}");
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Execute_should_use_serverApi_in_transaction(
            [Values(false, true)] bool useServerApi,
            [Values(false, true)] bool async)
        {
            var serverApi = useServerApi ? new ServerApi(ServerApiVersion.V1, true, true) : null;

            var connection = new MockConnection();
            connection.Description = __connectionDescription;
            var commandResponse = MessageHelper.BuildCommandResponse(CreateRawBsonDocument(new BsonDocument("ok", 1)));
            connection.EnqueueCommandResponseMessage(commandResponse);
            var session = CreateMockSessionInTransaction();
            var subject = new CommandWireProtocol<BsonDocument>(
                session,
                null,
                ReadPreference.Primary,
                new DatabaseNamespace("test"),
                new BsonDocument("moreGet", 1),
                commandPayloads: null,
                postWriteAction: null,
                CommandResponseHandling.Return,
                BsonDocumentSerializer.Instance,
                new MessageEncoderSettings(),
                serverApi,
                TimeSpan.FromMilliseconds(42));

            using var operationContext = new OperationContext(session);
            if (async)
            {
                await subject.ExecuteAsync(operationContext, connection);
            }
            else
            {
                subject.Execute(operationContext, connection);
            }

            SpinWait.SpinUntil(() => connection.GetSentMessages().Count >= 1, TimeSpan.FromSeconds(4)).Should().BeTrue();

            var sentMessages = connection.GetSentMessages();
            sentMessages.Count.Should().Be(1);
            var expectedServerApiString = useServerApi ? ", apiVersion : '1', apiStrict : true, apiDeprecationErrors : true" : "";
            MessageHelper.ToCommandPayload(sentMessages[0]).Should().Be($"{{ moreGet : 1, $db : 'test', txnNumber : NumberLong(1), autocommit : false{expectedServerApiString} }}");

            ICoreSessionHandle CreateMockSessionInTransaction()
            {
                var transaction = new CoreTransaction(1, new TransactionOptions());
                transaction.SetState(CoreTransactionState.InProgress);

                var mockSession = new Mock<ICoreSessionHandle>();
                mockSession.SetupGet(m => m.CurrentTransaction).Returns(transaction);
                mockSession.SetupGet(m => m.IsInTransaction).Returns(true);

                return mockSession.Object;
            }
        }

        [Fact]
        public void Execute_should_wait_for_response_when_CommandResponseHandling_is_Return()
        {
            var messageEncoderSettings = new MessageEncoderSettings();
            var subject = new CommandWireProtocol<BsonDocument>(
                NoCoreSession.Instance,
                null,
                ReadPreference.Primary,
                new DatabaseNamespace("test"),
                new BsonDocument("cmd", 1),
                null, // commandPayloads
                null, // postWriteAction
                CommandResponseHandling.Return,
                BsonDocumentSerializer.Instance,
                messageEncoderSettings,
                null, // serverApi
                TimeSpan.FromMilliseconds(42));

            var mockConnection = new Mock<IConnection>();
            mockConnection.Setup(c => c.Settings).Returns(() => new ConnectionSettings());

            var commandResponse = MessageHelper.BuildCommandResponse(CreateRawBsonDocument(new BsonDocument("ok", 1)));
            mockConnection
                .Setup(c => c.ReceiveMessage(It.IsAny<OperationContext>(), It.IsAny<int>(), messageEncoderSettings))
                .Returns(commandResponse);

            using var operationContext = new OperationContext(NoCoreSession.NewHandle());
            var result = subject.Execute(operationContext, mockConnection.Object);
            result.Should().Be("{ok: 1}");
        }

        [Fact]
        public void Execute_should_not_wait_for_response_when_CommandResponseHandling_is_NoResponseExpected()
        {
            var messageEncoderSettings = new MessageEncoderSettings();
            var subject = new CommandWireProtocol<BsonDocument>(
                NoCoreSession.Instance,
                null,
                ReadPreference.Primary,
                new DatabaseNamespace("test"),
                new BsonDocument("cmd", 1),
                null, // commandPayloads
                null, // postWriteAction
                CommandResponseHandling.NoResponseExpected,
                BsonDocumentSerializer.Instance,
                messageEncoderSettings,
                null, // serverApi
                TimeSpan.FromMilliseconds(42));

            var mockConnection = new Mock<IConnection>();
            mockConnection.Setup(c => c.Settings).Returns(() => new ConnectionSettings());
            using var operationContext = new OperationContext(NoCoreSession.NewHandle());
            var result = subject.Execute(operationContext, mockConnection.Object);
            result.Should().BeNull();

            mockConnection.Verify(
                c => c.ReceiveMessageAsync(It.IsAny<OperationContext>(), It.IsAny<int>(), messageEncoderSettings),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_should_wait_for_response_when_CommandResponseHandling_is_Return()
        {
            var messageEncoderSettings = new MessageEncoderSettings();
            var subject = new CommandWireProtocol<BsonDocument>(
                NoCoreSession.Instance,
                null,
                ReadPreference.Primary,
                new DatabaseNamespace("test"),
                new BsonDocument("cmd", 1),
                null, // commandPayloads
                null, // postWriteAction
                CommandResponseHandling.Return,
                BsonDocumentSerializer.Instance,
                messageEncoderSettings,
                null, // serverApi
                TimeSpan.FromMilliseconds(42));

            var mockConnection = new Mock<IConnection>();
            mockConnection.Setup(c => c.Settings).Returns(() => new ConnectionSettings());

            var commandResponse = MessageHelper.BuildCommandResponse(CreateRawBsonDocument(new BsonDocument("ok", 1)));
            mockConnection
                .Setup(c => c.ReceiveMessageAsync(It.IsAny<OperationContext>(), It.IsAny<int>(), messageEncoderSettings))
                .Returns(Task.FromResult<ResponseCommandMessage>(commandResponse));

            using var operationContext = new OperationContext(NoCoreSession.NewHandle());
            var result = await subject.ExecuteAsync(operationContext, mockConnection.Object);
            result.Should().Be("{ok: 1}");
        }

        [Fact]
        public async Task ExecuteAsync_should_not_wait_for_response_when_CommandResponseHandling_is_NoResponseExpected()
        {
            var messageEncoderSettings = new MessageEncoderSettings();
            var subject = new CommandWireProtocol<BsonDocument>(
                NoCoreSession.Instance,
                null,
                ReadPreference.Primary,
                new DatabaseNamespace("test"),
                new BsonDocument("cmd", 1),
                null, // commandPayloads
                null, // postWriteAction
                CommandResponseHandling.NoResponseExpected,
                BsonDocumentSerializer.Instance,
                messageEncoderSettings,
                null, // serverApi
                TimeSpan.FromMilliseconds(42));

            var mockConnection = new Mock<IConnection>();
            mockConnection.Setup(c => c.Settings).Returns(() => new ConnectionSettings());

            using var operationContext = new OperationContext(NoCoreSession.NewHandle());
            var result = await subject.ExecuteAsync(operationContext, mockConnection.Object);
            result.Should().BeNull();

            mockConnection.Verify(c => c.ReceiveMessageAsync(It.IsAny<OperationContext>(), It.IsAny<int>(), messageEncoderSettings), Times.Never);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Execute_should_send_only_one_readConcern_for_snapshot_session_with_non_default_readConcern(
            [Values(false, true)] bool async)
        {
            var snapshotConnectionDescription = new ConnectionDescription(
                new ConnectionId(__serverId),
                new HelloResult(
                    new BsonDocument("ok", 1)
                    .Add(OppressiveLanguageConstants.LegacyHelloResponseIsWritablePrimaryFieldName, 1)
                    .Add("logicalSessionTimeoutMinutes", 30)
                    .Add("maxWireVersion", WireVersion.Server50)));

            var connection = new MockConnection();
            connection.Description = snapshotConnectionDescription;
            connection.EnqueueCommandResponseMessage(
                MessageHelper.BuildCommandResponse(CreateRawBsonDocument(new BsonDocument("ok", 1))));

            var command = new BsonDocument
            {
                { "find", "testCollection" }
            };

            var session = CreateSnapshotSession();
            var subject = new CommandWireProtocol<BsonDocument>(
                session,
                null,
                ReadPreference.Primary,
                new DatabaseNamespace("test"),
                command,
                commandPayloads: null,
                postWriteAction: null,
                CommandResponseHandling.Return,
                BsonDocumentSerializer.Instance,
                new MessageEncoderSettings(),
                null,
                TimeSpan.FromMilliseconds(42));

            using var operationContext = new OperationContext(session);
            if (async)
            {
                await subject.ExecuteAsync(operationContext, connection);
            }
            else
            {
                subject.Execute(operationContext, connection);
            }

            SpinWait.SpinUntil(() => connection.GetSentMessages().Count >= 1, TimeSpan.FromSeconds(4))
                .Should().BeTrue();

            var sentMessages = connection.GetSentMessages();
            var document = MessageHelper.ToCommandPayload(sentMessages[0]);

            var readConcernElements = document.Elements.Where(e => e.Name == "readConcern").ToList();
            readConcernElements.Should().HaveCount(1,
                "readConcern should appear exactly once");
            readConcernElements[0].Value.AsBsonDocument["level"].AsString
                .Should().Be("snapshot");

            ICoreSessionHandle CreateSnapshotSession()
            {
                var mockSession = new Mock<ICoreSessionHandle>();
                mockSession.SetupGet(m => m.IsSnapshot).Returns(true);
                mockSession.SetupGet(m => m.Id).Returns(
                    new BsonDocument("id", new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard)));
                return mockSession.Object;
            }
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Execute_should_not_duplicate_readConcern_for_transaction_with_non_default_readConcern_in_command(
            [Values(false, true)] bool async)
        {
            var transactionConnectionDescription = new ConnectionDescription(
                new ConnectionId(__serverId),
                new HelloResult(
                    new BsonDocument("ok", 1)
                    .Add(OppressiveLanguageConstants.LegacyHelloResponseIsWritablePrimaryFieldName, 1)
                    .Add("logicalSessionTimeoutMinutes", 30)
                    .Add("maxWireVersion", WireVersion.Server50)));

            var connection = new MockConnection();
            connection.Description = transactionConnectionDescription;
            connection.EnqueueCommandResponseMessage(
                MessageHelper.BuildCommandResponse(CreateRawBsonDocument(new BsonDocument("ok", 1))));

            var command = new BsonDocument
            {
                { "find", "testCollection" },
                { "readConcern", new BsonDocument("level", "local") }
            };

            var session = CreateTransactionSession();
            var subject = new CommandWireProtocol<BsonDocument>(
                session,
                null,
                ReadPreference.Primary,
                new DatabaseNamespace("test"),
                command,
                commandPayloads: null,
                postWriteAction: null,
                CommandResponseHandling.Return,
                BsonDocumentSerializer.Instance,
                new MessageEncoderSettings(),
                null,
                TimeSpan.FromMilliseconds(42));

            using var operationContext2 = new OperationContext(session);
            if (async)
            {
                await subject.ExecuteAsync(operationContext2, connection);
            }
            else
            {
                subject.Execute(operationContext2, connection);
            }

            var sentMessages = connection.GetSentMessages();
            var document = MessageHelper.ToCommandPayload(sentMessages[0]);

            var readConcernElements = document.Elements.Where(e => e.Name == "readConcern").ToList();
            readConcernElements.Should().HaveCount(1,
                "readConcern should appear exactly once even when command already contains it");

            ICoreSessionHandle CreateTransactionSession()
            {
                var transaction = new CoreTransaction(1, new TransactionOptions(ReadConcern.Majority));
                transaction.SetState(CoreTransactionState.Starting);

                var mockSession = new Mock<ICoreSessionHandle>();
                mockSession.SetupGet(m => m.CurrentTransaction).Returns(transaction);
                mockSession.SetupGet(m => m.IsInTransaction).Returns(true);
                mockSession.SetupGet(m => m.Id).Returns(
                    new BsonDocument("id", new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard)));
                return mockSession.Object;
            }
        }

        // private methods
        private RawBsonDocument CreateRawBsonDocument(BsonDocument doc)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var bsonWriter = new BsonBinaryWriter(memoryStream, BsonBinaryWriterSettings.Defaults))
                {
                    var context = BsonSerializationContext.CreateRoot(bsonWriter);
                    BsonDocumentSerializer.Instance.Serialize(context, doc);
                }

                return new RawBsonDocument(memoryStream.ToArray());
            }
        }
    }

    internal static class CommandWireProtocolReflector
    {
        public static ConnectionId _lastConnectionId(this CommandWireProtocol<BsonDocument> commandWireProtocol)
        {
            return (ConnectionId)Reflector.GetFieldValue(commandWireProtocol, nameof(_lastConnectionId));
        }

        public static bool _moreToCome(this CommandWireProtocol<BsonDocument> commandWireProtocol)
        {
            return (bool)Reflector.GetFieldValue(commandWireProtocol, nameof(_moreToCome));
        }

        public static void _moreToCome(this CommandWireProtocol<BsonDocument> commandWireProtocol, bool moreToCome)
        {
            Reflector.SetFieldValue(commandWireProtocol, nameof(_moreToCome), moreToCome);
        }

        public static BsonDocument _command(this CommandWireProtocol<BsonDocument> commandWireProtocol)
        {
            return (BsonDocument)Reflector.GetFieldValue(commandWireProtocol, nameof(_command));
        }

        public static void _responseHandling(this CommandWireProtocol<BsonDocument> commandWireProtocol, CommandResponseHandling commandResponseHandling)
        {
            Reflector.SetFieldValue(commandWireProtocol, nameof(_responseHandling), commandResponseHandling);
        }

        public static CommandResponseHandling _responseHandling(this CommandWireProtocol<BsonDocument> commandWireProtocol)
        {
            return (CommandResponseHandling)Reflector.GetFieldValue(commandWireProtocol, nameof(_responseHandling));
        }
    }
}
