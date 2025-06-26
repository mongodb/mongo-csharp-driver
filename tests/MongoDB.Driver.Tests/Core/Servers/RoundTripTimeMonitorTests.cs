/* Copyright 2020-present MongoDB Inc.
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
using System.Net;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers;
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

namespace MongoDB.Driver.Core.Tests.Core.Servers
{
    public class RoundTripTimeMonitorTests
    {
        private static EndPoint __endPoint = new DnsEndPoint("localhost", 27017);
        private static ServerId __serverId = new ServerId(new ClusterId(), __endPoint);

        [Fact]
        public void Constructor_should_throw_connection_endpoint_is_null()
        {
            var exception = Record.Exception(() => new RoundTripTimeMonitor(Mock.Of<IConnectionFactory>(), __serverId, endpoint: null, TimeSpan.Zero, serverApi: null, logger: null));
            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("endpoint");
        }

        [Fact]
        public void Constructor_should_throw_connection_factory_is_null()
        {
            var exception = Record.Exception(() => new RoundTripTimeMonitor(connectionFactory: null, __serverId, __endPoint, TimeSpan.Zero, serverApi: null, logger: null));
            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("connectionFactory");
        }

        [Fact]
        public void Constructor_should_throw_connection_serverId_is_null()
        {
            var exception = Record.Exception(() => new RoundTripTimeMonitor(Mock.Of<IConnectionFactory>(), serverId: null, __endPoint, TimeSpan.Zero, serverApi: null, logger: null));
            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("serverId");
        }

        [Fact]
        public void Dispose_should_close_connection()
        {
            var mockConnection = new Mock<IConnection>();

            var subject = CreateSubject(TimeSpan.FromMilliseconds(10), mockConnection);

            subject.Start();
            SpinWait.SpinUntil(() => subject._roundTripTimeConnection() != null, TimeSpan.FromSeconds(2)).Should().BeTrue();

            subject.Dispose();

            mockConnection.Verify(c => c.Dispose(), Times.Once);
            subject._disposed().Should().BeTrue();

            SpinWait.SpinUntil(() => subject._roundTripTimeMonitorThread().ThreadState == ThreadState.Stopped, TimeSpan.FromMilliseconds(500)).Should().BeTrue();
        }

        [Fact]
        public void Round_trip_time_monitor_should_work_as_expected()
        {
            var frequency = TimeSpan.FromMilliseconds(10);
            var mockConnection = new Mock<IConnection>();
            var mockConnectionFactory = new Mock<IConnectionFactory>();

            ConcurrentQueue<(TimeSpan, IConnection)> steps = new ConcurrentQueue<(TimeSpan, IConnection)>();

            var subject = CreateSubject(
                frequency,
                mockConnection,
                mockConnectionFactory);

            mockConnectionFactory.Setup(f => f.ConnectionSettings).Returns(() => new ConnectionSettings());
            mockConnectionFactory
                .Setup(f => f.CreateConnection(__serverId, __endPoint))
                .Returns(
                    () =>
                    {
                        steps.Enqueue((subject.Average, subject._roundTripTimeConnection()));
                        return mockConnection.Object;
                    });

            mockConnection
                .SetupSequence(c => c.ReceiveMessage(It.IsAny<OperationContext>(), It.IsAny<int>(), It.IsAny<IMessageEncoderSelector>(), It.IsAny<MessageEncoderSettings>()))
                .Returns(
                    () =>
                    {
                        steps.Enqueue((subject.Average, subject._roundTripTimeConnection()));
                        return CreateResponseMessage();
                    })
                .Throws(new Exception("TestMessage"))
                .Returns(
                    () =>
                    {
                        subject.Dispose();
                        steps.Enqueue((subject.Average, subject._roundTripTimeConnection()));
                        return CreateResponseMessage();
                    });

            subject.Start();

            var thread = subject._roundTripTimeMonitorThread();
            if (!thread.Join(TimeSpan.FromSeconds(5)))
            {
                throw new Exception("Rtt monitor has not been stopped.");
            }

            // initialize connection
            steps.TryDequeue(out (TimeSpan Average, IConnection RttConnection) step).Should().BeTrue();
            step.Average.Should().Be(default);
            step.RttConnection.Should().BeNull();

            // legacy hello call
            steps.TryDequeue(out step).Should().BeTrue();
            step.Average.Should().NotBe(default);
            step.RttConnection.Should().NotBeNull();

            // initialize connection after exception
            steps.TryDequeue(out step).Should().BeTrue();
            step.Average.Should().NotBe(default);
            step.RttConnection.Should().BeNull();

            // legacy hello call
            steps.TryDequeue(out step).Should().BeTrue();
            step.Average.Should().NotBe(default);
            step.RttConnection.Should().NotBeNull();

            steps.TryDequeue(out _).Should().BeFalse();

            mockConnection.Verify(c => c.Dispose(), Times.Exactly(2)); // 1 - exception handling, 2 - monitor disposing
            mockConnectionFactory.Verify(c => c.CreateConnection(__serverId, __endPoint), Times.Exactly(2));
            subject._disposed().Should().BeTrue();
        }

        [Fact]
        public void Start_should_use_serverApi()
        {
            var serverApi = new ServerApi(ServerApiVersion.V1);
            var connection = new MockConnection(__serverId);

            var mockConnectionFactory = new Mock<IConnectionFactory>();
            mockConnectionFactory.Setup(f => f.ConnectionSettings).Returns(() => new ConnectionSettings());
            mockConnectionFactory
                .Setup(x => x.CreateConnection(__serverId, __endPoint))
                .Returns(connection);

            Thread thread;
            using (var subject = new RoundTripTimeMonitor(
                mockConnectionFactory.Object,
                __serverId,
                __endPoint,
                TimeSpan.FromMilliseconds(10),
                serverApi,
                logger: null))
            {
                subject.Start();

                thread = subject._roundTripTimeMonitorThread();

                SpinWait.SpinUntil(() => connection.GetSentMessages().Count >= 1, TimeSpan.FromSeconds(5)).Should().BeTrue();
            }

            SpinWait.SpinUntil(() => thread.ThreadState == ThreadState.Stopped, TimeSpan.FromMilliseconds(500)).Should().BeTrue();

            var sentMessages = MessageHelper.TranslateMessagesToBsonDocuments(connection.GetSentMessages());
            sentMessages.Count.Should().BeInRange(1, 2);

            var requestId = sentMessages[0]["requestId"].AsInt32;
            sentMessages[0].Should().Be($"{{ \"opcode\" : \"opmsg\", \"requestId\" : {requestId}, \"responseTo\" : 0, \"sections\" : [{{ \"payloadType\" : 0, \"document\" : {{ \"hello\" : 1, \"helloOk\" : true, \"$db\" : \"admin\", \"$readPreference\" : {{ \"mode\" : \"primaryPreferred\" }}, \"apiVersion\" : \"1\" }} }}] }}");
            if (sentMessages.Count > 1)
            {
                requestId = sentMessages[1]["requestId"].AsInt32;
                sentMessages[1].Should().Be($"{{ \"opcode\" : \"opmsg\", \"requestId\" : {requestId}, \"responseTo\" : 0, \"sections\" : [{{ \"payloadType\" : 0, \"document\" : {{ \"hello\" : 1, \"helloOk\" : true, \"$db\" : \"admin\", \"$readPreference\" : {{ \"mode\" : \"primaryPreferred\" }}, \"apiVersion\" : \"1\" }} }}] }}");
            }
        }

        [Fact]
        public void RoundTripTimeMonitor_without_serverApi_but_with_loadBalancedConnection_should_use_hello_command_to_set_up_monitoring()
        {
            var connection = new MockConnection(__serverId, new ConnectionSettings(loadBalanced:true), null);

            var mockConnectionFactory = new Mock<IConnectionFactory>();
            mockConnectionFactory.Setup(f => f.ConnectionSettings).Returns(() => new ConnectionSettings());
            mockConnectionFactory
                .Setup(x => x.CreateConnection(__serverId, __endPoint))
                .Returns(connection);

            Thread thread;
            using (var subject = new RoundTripTimeMonitor(
                mockConnectionFactory.Object,
                __serverId,
                __endPoint,
                TimeSpan.FromMilliseconds(10),
                null,
                logger: null))
            {
                subject.Start();

                thread = subject._roundTripTimeMonitorThread();

                SpinWait.SpinUntil(() => connection.GetSentMessages().Count >= 1, TimeSpan.FromSeconds(5)).Should().BeTrue();
            }

            SpinWait.SpinUntil(() => thread.ThreadState == ThreadState.Stopped, TimeSpan.FromMilliseconds(500)).Should().BeTrue();

            var sentMessages = MessageHelper.TranslateMessagesToBsonDocuments(connection.GetSentMessages());
            sentMessages.Count.Should().BeInRange(1, 2);

            var requestId = sentMessages[0]["requestId"].AsInt32;
            sentMessages[0].Should().Be($"{{ \"opcode\" : \"opmsg\", \"requestId\" : {requestId}, \"responseTo\" : 0, \"sections\" : [{{ \"payloadType\" : 0, \"document\" : {{ \"hello\" : 1, \"helloOk\" : true, \"loadBalanced\" : true, \"$db\" : \"admin\", \"$readPreference\" : {{ \"mode\" : \"primaryPreferred\" }} }} }}] }}");
            if (sentMessages.Count > 1)
            {
                requestId = sentMessages[1]["requestId"].AsInt32;
                sentMessages[1].Should().Be($"{{ \"opcode\" : \"opmsg\", \"requestId\" : {requestId}, \"responseTo\" : 0, \"sections\" : [{{ \"payloadType\" : 0, \"document\" : {{ \"hello\" : 1, \"helloOk\" : true, \"loadBalanced\" : true, \"$db\" : \"admin\", \"$readPreference\" : {{ \"mode\" : \"primaryPreferred\" }} }} }}] }}");
            }
        }

        // private methods
        private RoundTripTimeMonitor CreateSubject(
            TimeSpan frequency,
            Mock<IConnection> mockConnection,
            Mock<IConnectionFactory> mockConnectionFactory)
        {
            var connectionDescription = CreateConnectionDescription();
            mockConnection
                .SetupGet(c => c.Description)
                .Returns(connectionDescription);
            mockConnection.Setup(f => f.Settings).Returns(() => new ConnectionSettings());

            mockConnection
               .SetupGet(c => c.ConnectionId)
               .Returns(connectionDescription.ConnectionId);

            return new RoundTripTimeMonitor(
                mockConnectionFactory.Object,
                __serverId,
                __endPoint,
                frequency,
                serverApi: null,
                logger: null);
        }

        private ConnectionDescription CreateConnectionDescription()
        {
            var helloDocument = new BsonDocument
            {
                { "ok", 1 },
                { "maxWireVersion", WireVersion.Server44 }
            };
            return new ConnectionDescription(
                    new ConnectionId(__serverId, 0),
                    new HelloResult(helloDocument));
        }

        private RoundTripTimeMonitor CreateSubject(TimeSpan frequency, Mock<IConnection> mockConnection)
        {
            mockConnection
                .Setup(c => c.ReceiveMessage(It.IsAny<OperationContext>(), It.IsAny<int>(), It.IsAny<IMessageEncoderSelector>(), It.IsAny<MessageEncoderSettings>()))
                .Returns(() => CreateResponseMessage());

            var mockConnectionFactory = new Mock<IConnectionFactory>();
            mockConnectionFactory.Setup(f => f.ConnectionSettings).Returns(() => new ConnectionSettings());
            mockConnectionFactory
                .Setup(f => f.CreateConnection(__serverId, __endPoint))
                .Returns(mockConnection.Object);

            return CreateSubject(frequency, mockConnection, mockConnectionFactory);
        }

        private ResponseMessage CreateResponseMessage()
        {
            var section0Document = $"{{ {OppressiveLanguageConstants.LegacyHelloResponseIsWritablePrimaryFieldName} : true, topologyVersion : {{ processId : ObjectId('5ee3f0963109d4fe5e71dd28'), counter : NumberLong(0) }}, ok : 1.0 }}";
            var section0 = new Type0CommandMessageSection<RawBsonDocument>(
                new RawBsonDocument(BsonDocument.Parse(section0Document).ToBson()),
                RawBsonDocumentSerializer.Instance);
            return new CommandResponseMessage(new CommandMessage(1, 1, new[] { section0 }, false));
        }
    }

    internal static class RoundTripTimeMonitorReflector
    {
        public static bool _disposed(this RoundTripTimeMonitor roundTripTimeMonitor)
        {
            return (bool)Reflector.GetFieldValue(roundTripTimeMonitor, nameof(_disposed));
        }

        public static IConnection _roundTripTimeConnection(this RoundTripTimeMonitor roundTripTimeMonitor)
        {
            return (IConnection)Reflector.GetFieldValue(roundTripTimeMonitor, nameof(_roundTripTimeConnection));
        }

        public static Thread _roundTripTimeMonitorThread(this RoundTripTimeMonitor roundTripTimeMonitor)
        {
            return (Thread)Reflector.GetFieldValue(roundTripTimeMonitor, nameof(_roundTripTimeMonitorThread));
        }
    }
}
