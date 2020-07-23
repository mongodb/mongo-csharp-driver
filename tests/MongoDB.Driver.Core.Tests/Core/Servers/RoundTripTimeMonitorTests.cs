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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
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
            var exception = Record.Exception(() => new RoundTripTimeMonitor(Mock.Of<IConnectionFactory>(), __serverId, null, TimeSpan.Zero));
            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("endpoint");
        }

        [Fact]
        public void Constructor_should_throw_connection_factory_is_null()
        {
            var exception = Record.Exception(() => new RoundTripTimeMonitor(null, __serverId, __endPoint, TimeSpan.Zero));
            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("connectionFactory");
        }

        [Fact]
        public void Constructor_should_throw_connection_serverId_is_null()
        {
            var exception = Record.Exception(() => new RoundTripTimeMonitor(Mock.Of<IConnectionFactory>(), null, __endPoint, TimeSpan.Zero));
            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("serverId");
        }

        [Fact]
        public void Dispose_should_close_connection()
        {
            var mockConnection = new Mock<IConnection>();

            var subject = CreateSubject(TimeSpan.FromMilliseconds(10), mockConnection);

            subject.RunAsync().ConfigureAwait(false);
            SpinWait.SpinUntil(() => subject._roundTripTimeConnection() != null, TimeSpan.FromSeconds(2)).Should().BeTrue();

            subject.Dispose();

            mockConnection.Verify(c => c.Dispose(), Times.Once);
            subject._disposed().Should().BeTrue();
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

            mockConnectionFactory
                .Setup(f => f.CreateConnection(__serverId, __endPoint))
                .Returns(
                    () =>
                    {
                        steps.Enqueue((subject.Average, subject._roundTripTimeConnection()));
                        return mockConnection.Object;
                    });

            mockConnection
                .SetupSequence(c => c.ReceiveMessageAsync(It.IsAny<int>(), It.IsAny<IMessageEncoderSelector>(), It.IsAny<MessageEncoderSettings>(), It.IsAny<CancellationToken>()))
                .Returns(
                    () =>
                    {
                        steps.Enqueue((subject.Average, subject._roundTripTimeConnection()));
                        return Task.FromResult(CreateResponseMessage());
                    })
                .Throws(new Exception("TestMessage"))
                .Returns(
                    () =>
                    {
                        subject.Dispose();
                        steps.Enqueue((subject.Average, subject._roundTripTimeConnection()));
                        return Task.FromResult(CreateResponseMessage());
                    });

            var exception = Record.Exception(() => subject.RunAsync().GetAwaiter().GetResult());
            exception.Should().BeOfType<TaskCanceledException>(); // Task.Delay has been cancelled

            // initialize connection
            steps.TryDequeue(out (TimeSpan Average, IConnection RttConnection) step).Should().BeTrue();
            step.Average.Should().Be(default);
            step.RttConnection.Should().BeNull();

            // isMaster call
            steps.TryDequeue(out step).Should().BeTrue();
            step.Average.Should().NotBe(default);
            step.RttConnection.Should().NotBeNull();

            // initialize connection after exception
            steps.TryDequeue(out step).Should().BeTrue();
            step.Average.Should().NotBe(default);
            step.RttConnection.Should().BeNull();

            // isMaster call
            steps.TryDequeue(out step).Should().BeTrue();
            step.Average.Should().NotBe(default);
            step.RttConnection.Should().NotBeNull();

            steps.TryDequeue(out _).Should().BeFalse();

            mockConnection.Verify(c => c.Dispose(), Times.Exactly(2)); // 1 - exception handling, 2 - monitor disposing
            mockConnectionFactory.Verify(c => c.CreateConnection(__serverId, __endPoint), Times.Exactly(2));
            subject._disposed().Should().BeTrue();
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

            mockConnection
               .SetupGet(c => c.ConnectionId)
               .Returns(connectionDescription.ConnectionId);

            return new RoundTripTimeMonitor(
                mockConnectionFactory.Object,
                __serverId,
                __endPoint,
                frequency);
        }

        private ConnectionDescription CreateConnectionDescription()
        {
            var isMasterDocument = new BsonDocument
            {
                { "ok", 1 },
            };
            return new ConnectionDescription(
                    new ConnectionId(__serverId, 0),
                    new IsMasterResult(isMasterDocument),
                    new BuildInfoResult(BsonDocument.Parse("{ ok : 1, version : '4.4.0' }")));
        }

        private RoundTripTimeMonitor CreateSubject(TimeSpan frequency, Mock<IConnection> mockConnection)
        {
            mockConnection
                .Setup(c => c.ReceiveMessageAsync(It.IsAny<int>(), It.IsAny<IMessageEncoderSelector>(), It.IsAny<MessageEncoderSettings>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => CreateResponseMessage());

            var mockConnectionFactory = new Mock<IConnectionFactory>();
            mockConnectionFactory
                .Setup(f => f.CreateConnection(__serverId, __endPoint))
                .Returns(mockConnection.Object);

            return CreateSubject(frequency, mockConnection, mockConnectionFactory);
        }

        private ResponseMessage CreateResponseMessage()
        {
            var section0Document = "{ ismaster : true, topologyVersion : { processId : ObjectId('5ee3f0963109d4fe5e71dd28'), counter : NumberLong(0) }, ok : 1.0 }";
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
    }
}
