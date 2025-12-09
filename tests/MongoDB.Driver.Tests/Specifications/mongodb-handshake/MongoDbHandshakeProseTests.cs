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

using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Specifications.mongodb_handshake
{
    public class MongoDbHandshakeProseTests : LoggableTestClass
    {
        // https://github.com/mongodb/specifications/blob/75027a8e91ff50778aed2ad5a67c005f2694705f/source/mongodb-handshake/tests/README.md?plain=1#L77
        public MongoDbHandshakeProseTests(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [ParameterAttributeData]
        public async Task DriverAcceptsArbitraryAuthMechanism([Values(false, true)] bool async)
        {
            var capturedEvents = new EventCapturer();
            var mockStreamFactory = new Mock<IStreamFactory>();
            using var stream = new MemoryStream();
            mockStreamFactory
                .Setup(s => s.CreateStream(It.IsAny<EndPoint>(), It.IsAny<CancellationToken>()))
                .Returns(stream);
            mockStreamFactory
                .Setup(s => s.CreateStreamAsync(It.IsAny<EndPoint>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(stream);
            var endPoint = new DnsEndPoint("localhost", 27017);
            var serverId = new ServerId(new ClusterId(), endPoint);
            var connectionId = new ConnectionId(serverId);
            var helloResult = new HelloResult(BsonDocument.Parse("{ ok: 1, saslSupportedMechs : ['arbitrary string'] }"));
            var connectionDescription = new ConnectionDescription(connectionId, helloResult);
            var connectionInitializerContext = new ConnectionInitializerContext(connectionDescription, null);
            var connectionInitializerContextAfterAuthentication = new ConnectionInitializerContext(connectionDescription, null);

            var mockConnectionInitializer = new Mock<IConnectionInitializer>();
            mockConnectionInitializer
                .Setup(i => i.SendHello(It.IsAny<OperationContext>(), It.IsAny<IConnection>()))
                .Returns(connectionInitializerContext);
            mockConnectionInitializer
                .Setup(i => i.Authenticate(It.IsAny<OperationContext>(), It.IsAny<IConnection>(), It.IsAny<ConnectionInitializerContext>()))
                .Returns(connectionInitializerContextAfterAuthentication);
            mockConnectionInitializer
                .Setup(i => i.SendHelloAsync(It.IsAny<OperationContext>(), It.IsAny<IConnection>()))
                .ReturnsAsync(connectionInitializerContext);
            mockConnectionInitializer
                .Setup(i => i.AuthenticateAsync(It.IsAny<OperationContext>(), It.IsAny<IConnection>(), It.IsAny<ConnectionInitializerContext>()))
                .ReturnsAsync(connectionInitializerContextAfterAuthentication);

            using var subject = new BinaryConnection(
                serverId: serverId,
                endPoint: endPoint,
                settings: new ConnectionSettings(),
                streamFactory: mockStreamFactory.Object,
                connectionInitializer: mockConnectionInitializer.Object,
                eventSubscriber: capturedEvents,
                LoggerFactory,
                null,
                socketReadTimeout: Timeout.InfiniteTimeSpan,
                socketWriteTimeout: Timeout.InfiniteTimeSpan);

            if (async)
            {
                await subject.OpenAsync(OperationContext.NoTimeout);
            }
            else
            {
                subject.Open(OperationContext.NoTimeout);
            }

            subject._state().Should().Be(3); // 3 - open.
        }
    }

    internal static class BinaryConnectionReflector
    {
        public static int _state(this BinaryConnection subject)
            => ((InterlockedInt32)Reflector.GetFieldValue(subject, nameof(_state))).Value;
    }
}
