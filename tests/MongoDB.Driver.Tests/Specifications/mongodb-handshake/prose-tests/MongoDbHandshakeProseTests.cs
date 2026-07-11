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
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Specifications.mongodb_handshake.prose_tests
{
    [Trait("Category", "Integration")]
    public class MongoDbHandshakeProseTests : LoggableTestClass
    {
        public MongoDbHandshakeProseTests(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [ParameterAttributeData]
        // https://github.com/mongodb/specifications/blob/75027a8e91ff50778aed2ad5a67c005f2694705f/source/mongodb-handshake/tests/README.md?plain=1#L77
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

            using var operationContext = new OperationContext(NoCoreSession.NewHandle());
            if (async)
            {
                await subject.OpenAsync(operationContext);
            }
            else
            {
                subject.Open(operationContext);
            }

            subject._state().Should().Be(3); // 3 - open.
        }

        [Fact]
        // https://github.com/mongodb/specifications/blob/7039e69945d463a14b1b727d16db063e21f48f53/source/mongodb-handshake/tests/README.md#test-9-handshake-documents-include-backpressure-true
        public async Task HandshakeDocumentsIncludeBackpressureTrue()
        {
            RequireServer.Check().Authentication(authentication: false); // speculative authentication makes events asserting hard

            var eventCapturer = new EventCapturer()
                .Capture<CommandStartedEvent>(e => e.CommandName is "hello" or OppressiveLanguageConstants.LegacyHelloCommandName);

            using var client = DriverTestConfiguration.CreateMongoClient(eventCapturer);

            var database = client.GetDatabase("admin");
            await database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));

            var commandStartedEvents = eventCapturer.Events.OfType<CommandStartedEvent>().ToList();
            commandStartedEvents.Should().NotBeEmpty();
            foreach (var doc in commandStartedEvents.Select(ev => ev.Command))
            {
                doc.Contains("backpressure").Should().BeTrue();
                doc["backpressure"].AsBoolean.Should().BeTrue();
            }
        }

        [Theory]
        [ParameterAttributeData]
        // https://github.com/mongodb/specifications/blob/290ee48973ed0dc8a7c54668e35b19cb012e94ed/source/mongodb-handshake/tests/README.md#test-1-test-that-the-driver-updates-metadata
        public async Task ClientMetadataUpdate_Test1_driver_updates_metadata(
            [Values("2.0", null)] string version,
            [Values("Framework Platform", null)] string platform)
        {
            RequireServer.Check().Authentication(authentication: false); // speculative authentication makes events asserting hard

            var eventCapturer = CreateHelloEventCapturer();
            using var client = CreateClient(eventCapturer, new LibraryInfo("library", "1.2", "Library Platform"));

            await Ping(client);
            var initialClientMetadata = HelloClientDocument(eventCapturer);
            await WaitForIdleConnection();

            client.AppendMetadata(new LibraryInfo("framework", version, platform));
            await Ping(client);

            HelloClientDocument(eventCapturer).Should().Be(WithAppended(initialClientMetadata, "framework", version, platform));
        }

        [Theory]
        [ParameterAttributeData]
        // https://github.com/mongodb/specifications/blob/290ee48973ed0dc8a7c54668e35b19cb012e94ed/source/mongodb-handshake/tests/README.md#test-2-multiple-successive-metadata-updates
        public async Task ClientMetadataUpdate_Test2_multiple_successive_metadata_updates(
            [Values("2.0", null)] string version,
            [Values("Framework Platform", null)] string platform)
        {
            RequireServer.Check().Authentication(authentication: false);

            var eventCapturer = CreateHelloEventCapturer();
            using var client = CreateClient(eventCapturer, libraryInfo: null);

            client.AppendMetadata(new LibraryInfo("library", "1.2", "Library Platform"));
            await Ping(client);
            var clientMetadata = HelloClientDocument(eventCapturer);
            await WaitForIdleConnection();

            client.AppendMetadata(new LibraryInfo("framework", version, platform));
            await Ping(client);

            HelloClientDocument(eventCapturer).Should().Be(WithAppended(clientMetadata, "framework", version, platform));
        }

        [Theory]
        [ParameterAttributeData]
        // https://github.com/mongodb/specifications/blob/290ee48973ed0dc8a7c54668e35b19cb012e94ed/source/mongodb-handshake/tests/README.md#test-3-multiple-successive-metadata-updates-with-duplicate-data
        public async Task ClientMetadataUpdate_Test3_multiple_successive_metadata_updates_with_duplicate_data(
            [Values(1, 2, 3, 4, 5, 6, 7)] int testCase)
        {
            RequireServer.Check().Authentication(authentication: false);

            var (name, version, platform) = testCase switch
            {
                1 => ("library", "1.2", "Library Platform"),
                2 => ("framework", "1.2", "Library Platform"),
                3 => ("library", "2.0", "Library Platform"),
                4 => ("library", "1.2", "Framework Platform"),
                5 => ("framework", "2.0", "Library Platform"),
                6 => ("framework", "1.2", "Framework Platform"),
                7 => ("library", "2.0", "Framework Platform"),
                _ => throw new ArgumentOutOfRangeException(nameof(testCase))
            };

            var eventCapturer = CreateHelloEventCapturer();
            using var client = CreateClient(eventCapturer, libraryInfo: null);

            client.AppendMetadata(new LibraryInfo("library", "1.2", "Library Platform"));
            await Ping(client);
            var updatedClientMetadata = HelloClientDocument(eventCapturer);
            await WaitForIdleConnection();

            client.AppendMetadata(new LibraryInfo(name, version, platform));
            await Ping(client);

            var isIdenticalToSetup = name == "library" && version == "1.2" && platform == "Library Platform";
            var expected = isIdenticalToSetup ? updatedClientMetadata : WithAppended(updatedClientMetadata, name, version, platform);
            HelloClientDocument(eventCapturer).Should().Be(expected);
        }

        [Fact]
        // https://github.com/mongodb/specifications/blob/290ee48973ed0dc8a7c54668e35b19cb012e94ed/source/mongodb-handshake/tests/README.md#test-4-multiple-metadata-updates-with-duplicate-data
        public async Task ClientMetadataUpdate_Test4_multiple_metadata_updates_with_duplicate_data()
        {
            RequireServer.Check().Authentication(authentication: false);

            var eventCapturer = CreateHelloEventCapturer();
            using var client = CreateClient(eventCapturer, libraryInfo: null);

            client.AppendMetadata(new LibraryInfo("library", "1.2", "Library Platform"));
            await Ping(client);
            await WaitForIdleConnection();

            client.AppendMetadata(new LibraryInfo("framework", "2.0", "Framework Platform"));
            await Ping(client);
            var clientMetadata = HelloClientDocument(eventCapturer);
            await WaitForIdleConnection();

            client.AppendMetadata(new LibraryInfo("library", "1.2", "Library Platform"));
            await Ping(client);

            HelloClientDocument(eventCapturer).Should().Be(clientMetadata);
        }

        [Fact]
        // https://github.com/mongodb/specifications/blob/290ee48973ed0dc8a7c54668e35b19cb012e94ed/source/mongodb-handshake/tests/README.md#test-5-metadata-is-not-appended-if-identical-to-initial-metadata
        public async Task ClientMetadataUpdate_Test5_metadata_is_not_appended_if_identical_to_initial_metadata()
        {
            RequireServer.Check().Authentication(authentication: false);

            var eventCapturer = CreateHelloEventCapturer();
            using var client = CreateClient(eventCapturer, new LibraryInfo("library", "1.2", "Library Platform"));

            await Ping(client);
            var clientMetadata = HelloClientDocument(eventCapturer);
            await WaitForIdleConnection();

            client.AppendMetadata(new LibraryInfo("library", "1.2", "Library Platform"));
            await Ping(client);

            HelloClientDocument(eventCapturer).Should().Be(clientMetadata);
        }

        [Fact]
        // https://github.com/mongodb/specifications/blob/290ee48973ed0dc8a7c54668e35b19cb012e94ed/source/mongodb-handshake/tests/README.md#test-6-metadata-is-not-appended-if-identical-to-initial-metadata-separated-by-non-identical-metadata
        public async Task ClientMetadataUpdate_Test6_metadata_is_not_appended_if_identical_to_initial_metadata_separated_by_non_identical_metadata()
        {
            RequireServer.Check().Authentication(authentication: false);

            var eventCapturer = CreateHelloEventCapturer();
            using var client = CreateClient(eventCapturer, new LibraryInfo("library", "1.2", "Library Platform"));

            await Ping(client);
            await WaitForIdleConnection();

            client.AppendMetadata(new LibraryInfo("framework", "1.2", "Library Platform"));
            await Ping(client);
            var clientMetadata = HelloClientDocument(eventCapturer);
            await WaitForIdleConnection();

            client.AppendMetadata(new LibraryInfo("library", "1.2", "Library Platform"));
            await Ping(client);

            HelloClientDocument(eventCapturer).Should().Be(clientMetadata);
        }

        [Theory]
        [ParameterAttributeData]
        // https://github.com/mongodb/specifications/blob/290ee48973ed0dc8a7c54668e35b19cb012e94ed/source/mongodb-handshake/tests/README.md#test-7-empty-strings-are-considered-unset-when-appending-duplicate-metadata
        public async Task ClientMetadataUpdate_Test7_empty_strings_are_unset_when_appending_duplicate_metadata(
            [Values(2, 3)] int testCase) // case 1 (null name) is not applicable: the driver requires a non-null library name
        {
            RequireServer.Check().Authentication(authentication: false);

            var (appended, duplicate) = testCase switch
            {
                2 => (new LibraryInfo("library", null, "Library Platform"), new LibraryInfo("library", "", "Library Platform")),
                3 => (new LibraryInfo("library", "1.2", null), new LibraryInfo("library", "1.2", "")),
                _ => throw new ArgumentOutOfRangeException(nameof(testCase))
            };

            var eventCapturer = CreateHelloEventCapturer();
            using var client = CreateClient(eventCapturer, libraryInfo: null);

            client.AppendMetadata(appended);
            await Ping(client);
            var clientMetadata = HelloClientDocument(eventCapturer);
            await WaitForIdleConnection();

            client.AppendMetadata(duplicate);
            await Ping(client);

            HelloClientDocument(eventCapturer).Should().Be(clientMetadata);
        }

        [Theory]
        [ParameterAttributeData]
        // https://github.com/mongodb/specifications/blob/290ee48973ed0dc8a7c54668e35b19cb012e94ed/source/mongodb-handshake/tests/README.md#test-8-empty-strings-are-considered-unset-when-appending-metadata-identical-to-initial-metadata
        public async Task ClientMetadataUpdate_Test8_empty_strings_are_unset_when_appending_metadata_identical_to_initial_metadata(
            [Values(2, 3)] int testCase) // case 1 (null name) is not applicable: the driver requires a non-null library name
        {
            RequireServer.Check().Authentication(authentication: false);

            var (initial, appended) = testCase switch
            {
                2 => (new LibraryInfo("library", null, "Library Platform"), new LibraryInfo("library", "", "Library Platform")),
                3 => (new LibraryInfo("library", "1.2", null), new LibraryInfo("library", "1.2", "")),
                _ => throw new ArgumentOutOfRangeException(nameof(testCase))
            };

            var eventCapturer = CreateHelloEventCapturer();
            using var client = CreateClient(eventCapturer, initial);

            await Ping(client);
            var initialClientMetadata = HelloClientDocument(eventCapturer);
            await WaitForIdleConnection();

            client.AppendMetadata(appended);
            await Ping(client);

            HelloClientDocument(eventCapturer).Should().Be(initialClientMetadata);
        }

        // private methods
        private static EventCapturer CreateHelloEventCapturer() =>
            new EventCapturer().Capture<CommandStartedEvent>(e => e.CommandName is "hello" or OppressiveLanguageConstants.LegacyHelloCommandName);

        private static MongoClient CreateClient(EventCapturer eventCapturer, LibraryInfo libraryInfo) =>
            (MongoClient)DriverTestConfiguration.CreateMongoClient(settings =>
            {
                settings.LibraryInfo = libraryInfo;
                settings.MaxConnectionIdleTime = TimeSpan.FromMilliseconds(1);
                var clusterConfigurator = settings.ClusterConfigurator;
                settings.ClusterConfigurator = builder => { clusterConfigurator?.Invoke(builder); builder.Subscribe(eventCapturer); };
            });

        private static BsonDocument HelloClientDocument(EventCapturer eventCapturer) =>
            eventCapturer.Events
                .OfType<CommandStartedEvent>()
                .Last(e => e.Command.Contains("client"))
                .Command["client"].AsBsonDocument.DeepClone().AsBsonDocument;

        private static BsonDocument WithAppended(BsonDocument clientDocument, string name, string version, string platform)
        {
            var expected = clientDocument.DeepClone().AsBsonDocument;
            if (name != null)
            {
                expected["driver"].AsBsonDocument["name"] = $"{expected["driver"]["name"].AsString}|{name}";
            }
            if (version != null)
            {
                expected["driver"].AsBsonDocument["version"] = $"{expected["driver"]["version"].AsString}|{version}";
            }
            if (platform != null)
            {
                expected["platform"] = $"{expected["platform"].AsString}|{platform}";
            }
            return expected;
        }

        private static async Task Ping(IMongoClient client)
        {
            var database = client.GetDatabase("admin");
            var ping = new BsonDocument("ping", 1);
            await database.RunCommandAsync<BsonDocument>(ping);
        }

        // wait long enough for the pooled connection to exceed maxConnectionIdleTime (1ms) so the next ping handshakes a new connection
        private static Task WaitForIdleConnection() => Task.Delay(TimeSpan.FromMilliseconds(10));
    }

    internal static class BinaryConnectionReflector
    {
        public static int _state(this BinaryConnection subject)
            => ((InterlockedInt32)Reflector.GetFieldValue(subject, nameof(_state))).Value;
    }
}
