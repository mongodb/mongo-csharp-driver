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
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Core.Tests.Core.Clusters
{
    public class LoadBalancedClusterTests : LoggableTestClass
    {
        private EventCapturer _capturedEvents;
        private readonly MockClusterableServerFactory _mockServerFactory;
        private ClusterSettings _settings;
        private readonly EndPoint _endPoint = new DnsEndPoint("localhost", 27017);

        public LoadBalancedClusterTests(ITestOutputHelper output) : base(output)
        {
            _settings = new ClusterSettings().With(loadBalanced: true);
            _mockServerFactory = new MockClusterableServerFactory(LoggerFactory);
            _capturedEvents = new EventCapturer();
        }

        [Fact]
        public void Constructor_should_initialize_instance()
        {
            var settings = new ClusterSettings(loadBalanced: true);
            var serverFactory = Mock.Of<IClusterableServerFactory>();
            var mockEventSubscriber = new Mock<IEventSubscriber>();
            var dnsMonitorFactory = Mock.Of<IDnsMonitorFactory>();

            var result = new LoadBalancedCluster(settings, serverFactory, mockEventSubscriber.Object, null, dnsMonitorFactory);

            result._dnsMonitorFactory().Should().BeSameAs(dnsMonitorFactory);
            result._state().Value.Should().Be(0); // State.Initial
        }

        [Theory]
        [ParameterAttributeData]
        public void Constructor_should_handle_directConnection_correctly([Values(false, true)]bool directConnection)
        {
            _settings = _settings.With(directConnection: directConnection);

            var exception = Record.Exception(() => new LoadBalancedCluster(_settings, _mockServerFactory, _capturedEvents, null));

            if (directConnection)
            {
                exception.Should().BeOfType<ArgumentException>();
            }
            else
            {
                exception.Should().BeNull();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Constructor_should_handle_loadBalanced_correctly([Values(false, true)] bool loadBalanced)
        {
            _settings = _settings.With(loadBalanced: loadBalanced);

            var exception = Record.Exception(() => new LoadBalancedCluster(_settings, _mockServerFactory, _capturedEvents, LoggerFactory));

            if (!loadBalanced)
            {
                exception.Should().BeOfType<ArgumentException>();
            }
            else
            {
                exception.Should().BeNull();
            }
        }

        [Fact]
        public void Constructor_should_throw_when_more_than_one_endpoint_is_specified()
        {
            _settings = _settings.With(endPoints: new[] { _endPoint, new DnsEndPoint("localhost", 27018) });

            var exception = Record.Exception(() => new LoadBalancedCluster(_settings, _mockServerFactory, _capturedEvents, loggerFactory: null));

            exception.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void Constructor_should_throw_when_replicaSetName_is_specified()
        {
            _settings = _settings.With(replicaSetName: "rs");

            var exception = Record.Exception(() => new LoadBalancedCluster(_settings, _mockServerFactory, _capturedEvents, loggerFactory: null));

            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_if_srvMaxHosts_is_greater_than_zero()
        {
            _settings = _settings.With(srvMaxHosts: 2);

            var exception = Record.Exception(() => new LoadBalancedCluster(_settings, _mockServerFactory, _capturedEvents, loggerFactory: null));

            exception.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void InitializeServer_should_throw_if_cluster_disposed()
        {
            var subject = CreateSubject();
            subject.Dispose();

            var exception = Record.Exception(() => subject.InitializeServer(Mock.Of<IClusterableServer>()));

            exception.Should().BeOfType<ObjectDisposedException>();
        }

        [Fact]
        public void Description_should_contain_expected_server_description()
        {
            var subject = CreateSubject();
            subject.Description.Servers.Should().BeEmpty();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(_endPoint);

            var description = subject.Description;
            description.Servers.Should().ContainSingle(s => EndPointHelper.Equals(s.EndPoint, _endPoint) && s.State == ServerState.Connected);

            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Dispose_should_dispose_of_the_server()
        {
            var subject = CreateSubject();
            subject._state().Value.Should().Be(0); // initial
            subject.Initialize();
            subject._state().Value.Should().Be(1); // opened

            _capturedEvents.Clear();

            subject.Dispose();
            subject._state().Value.Should().Be(2); // disposed

            var mockServer = Mock.Get(_mockServerFactory.GetServer(_endPoint));
            mockServer.Verify(s => s.Dispose(), Times.Once);

            _capturedEvents.Next().Should().BeOfType<ClusterClosingEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterClosedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }


        [Fact]
        public void Initialize_should_create_and_initialize_the_server()
        {
            var subject = CreateSubject();
            subject._state().Value.Should().Be(0); // initial
            subject.Initialize();
            subject._state().Value.Should().Be(1); // opened

            var mockServer = Mock.Get(_mockServerFactory.GetServer(_endPoint));
            mockServer.Verify(s => s.Initialize(), Times.Once);

            _capturedEvents.Next().Should().BeOfType<ClusterOpeningEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterOpenedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Theory]
        [InlineData(ConnectionStringScheme.MongoDB, false)]
        [InlineData(ConnectionStringScheme.MongoDBPlusSrv, true)]
        public void Initialize_should_not_start_dns_monitor_thread_when_scheme_is_MongoDB(ConnectionStringScheme connectionStringScheme, bool shouldStartDnsResolving)
        {
            var settings = _settings.With(scheme: connectionStringScheme, endPoints: new[] { new DnsEndPoint("a.b.com", 53) });
            using (var subject = CreateSubject(settings))
            {
                subject.Initialize();
                if (connectionStringScheme == ConnectionStringScheme.MongoDBPlusSrv)
                {
                    PublishDnsResults(subject, _endPoint);
                }

                if (shouldStartDnsResolving)
                {
                    subject._dnsMonitorThread().Should().NotBeNull();
                }
                else
                {
                    subject._dnsMonitorThread().Should().BeNull();
                }
            }

            _capturedEvents.Next().Should().BeOfType<ClusterOpeningEvent>();
            if (connectionStringScheme == ConnectionStringScheme.MongoDB)
            {
                _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
                _capturedEvents.Next().Should().BeOfType<ClusterOpenedEvent>();
            }
            else
            {
                // the events are in reverse order because async mode for srv resolving
                _capturedEvents.Next().Should().BeOfType<ClusterOpenedEvent>();
                _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            }
            _capturedEvents.Next().Should().BeOfType<ClusterClosingEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterClosedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Initialize_should_throw_when_disposed()
        {
            var subject = CreateSubject();
            subject.Dispose();

            var exception = Record.Exception(() => subject.Initialize());

            exception.Should().BeOfType<ObjectDisposedException>();
        }

        [Fact]
        public void ProcessDnsResults_should_call_Initialize_on_added_servers()
        {
            var settings = _settings.With(scheme: ConnectionStringScheme.MongoDBPlusSrv, endPoints: new[] { new DnsEndPoint("a.b.com", 53) });
            var mockDnsMonitorFactory = CreateMockDnsMonitorFactory();
            using (var subject = CreateSubject(settings: settings, dnsMonitorFactory: mockDnsMonitorFactory.Object))
            {
                subject.Initialize();
                var endPoints = new[] { _endPoint }.ToArray();

                PublishDnsResults(subject, endPoints);

                var server = subject._server();
                var mockServer = Mock.Get(server);
                mockServer.Verify(m => m.Initialize(), Times.Once);
            }
        }

        [Fact]
        public void ProcessDnsResults_should_clear_dns_monitor_exception()
        {
            var settings = _settings.With(scheme: ConnectionStringScheme.MongoDBPlusSrv, endPoints: new[] { new DnsEndPoint("a.b.com", 53) });
            var mockDnsMonitorFactory = CreateMockDnsMonitorFactory();
            using (var subject = CreateSubject(settings: settings, dnsMonitorFactory: mockDnsMonitorFactory.Object))
            {
                subject.Initialize();
                var exception = new Exception("Dns exception");
                PublishDnsException(subject, exception);
                subject.Description.DnsMonitorException.Should().BeSameAs(exception);

                PublishDnsResults(subject, _endPoint);

                subject.Description.DnsMonitorException.Should().BeNull();
            }
        }

        [Theory]
        [InlineData(0, "No srv records were resolved.")]
        [InlineData(2, "Load balanced mode cannot be used with multiple host names.")]
        [InlineData(3, "Load balanced mode cannot be used with multiple host names.")]
        public void ProcessDnsResults_should_throw_when_srv_records_number_is_unexpected(int numberOfRecords, string expectedErrorMessage)
        {
            var settings = _settings.With(scheme: ConnectionStringScheme.MongoDBPlusSrv, endPoints: new[] { new DnsEndPoint("a.b.com", 53) });
            var mockDnsMonitorFactory = CreateMockDnsMonitorFactory();
            using (var subject = CreateSubject(settings: settings, dnsMonitorFactory: mockDnsMonitorFactory.Object))
            {
                subject.Initialize();
                var endPoints = Enumerable.Repeat(_endPoint, numberOfRecords).ToArray();
                var originalDescription = subject.Description;

                var exception = Record.Exception(() => PublishDnsResults(subject, endPoints));

                exception
                    .Should().BeOfType<InvalidOperationException>().Subject
                    .Message.Should().Be(expectedErrorMessage);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public async Task SelectServer_should_return_expected_server(
            [Values(ConnectionStringScheme.MongoDB, ConnectionStringScheme.MongoDBPlusSrv)] ConnectionStringScheme connectionStringScheme,
            [Values(false, true)] bool async)
        {
            var settings = _settings.With(scheme: connectionStringScheme);
            var mockDnsMonitorFactory = CreateMockDnsMonitorFactory();
            using (var subject = CreateSubject(settings: settings, dnsMonitorFactory: mockDnsMonitorFactory.Object))
            {
                subject.Initialize();

                if (connectionStringScheme == ConnectionStringScheme.MongoDBPlusSrv)
                {
                    PublishDnsResults(subject, _endPoint);
                }

                PublishDescription(_endPoint);

                var (server, _) = async ?
                    await subject.SelectServerAsync(OperationContext.NoTimeout, Mock.Of<IServerSelector>()) :
                    subject.SelectServer(OperationContext.NoTimeout, Mock.Of<IServerSelector>());

                server.EndPoint.Should().Be(_endPoint);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public async Task SelectServer_should_throw_server_selection_timeout_if_server_has_not_been_created_in_time(
            [Values(ConnectionStringScheme.MongoDB, ConnectionStringScheme.MongoDBPlusSrv)] ConnectionStringScheme connectionStringScheme,
            [Values(false, true)] bool async)
        {
            var serverSelectionTimeout = TimeSpan.FromMilliseconds(500);
            var settings = _settings.With(scheme: connectionStringScheme, serverSelectionTimeout: serverSelectionTimeout);
            var mockDnsMonitorFactory = CreateMockDnsMonitorFactory();
            using (var subject = CreateSubject(settings: settings, dnsMonitorFactory: mockDnsMonitorFactory.Object))
            {
                subject.Initialize();

                var dnsException = new Exception("Dns exception");
                if (connectionStringScheme == ConnectionStringScheme.MongoDBPlusSrv)
                {
                    // it has an effect only on srv mode
                    PublishDnsException(subject, dnsException);
                }

                var exception = async ?
                    await Record.ExceptionAsync(() => subject.SelectServerAsync(OperationContext.NoTimeout, Mock.Of<IServerSelector>())) :
                    Record.Exception(() => subject.SelectServer(OperationContext.NoTimeout, Mock.Of<IServerSelector>()));

                var ex = exception.Should().BeOfType<TimeoutException>().Subject;
                ex.Message.Should().StartWith($"A timeout occurred after {serverSelectionTimeout.TotalMilliseconds}ms selecting a server. Client view of cluster state is ");
                if (connectionStringScheme == ConnectionStringScheme.MongoDBPlusSrv)
                {
                    ex.Message.Contains(dnsException.ToString());
                }
                else
                {
                    ex.Message.Should().NotContain(dnsException.ToString());
                }
            }
        }

        [Theory]
        [ParameterAttributeData]
        public async Task SelectServer_should_be_cancelled_by_cancellationToken(
            [Values(ConnectionStringScheme.MongoDB, ConnectionStringScheme.MongoDBPlusSrv)] ConnectionStringScheme connectionStringScheme,
            [Values(false, true)] bool async)
        {
            var settings = _settings.With(scheme: connectionStringScheme);
            var mockDnsMonitorFactory = CreateMockDnsMonitorFactory();
            using (var subject = CreateSubject(settings: settings, dnsMonitorFactory: mockDnsMonitorFactory.Object))
            {
                subject.Initialize();

                Exception exception;
                using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(100)))
                {
                    var operationContext = new OperationContext(Timeout.InfiniteTimeSpan, cancellationTokenSource.Token);
                    exception = async ?
                        await Record.ExceptionAsync(() => subject.SelectServerAsync(operationContext, Mock.Of<IServerSelector>())) :
                        Record.Exception(() => subject.SelectServer(operationContext, Mock.Of<IServerSelector>()));
                }

                exception.Should().BeOfType<OperationCanceledException>();
            }
        }

        [Fact]
        public void Should_call_dispose_on_server_when_the_cluster_is_disposed()
        {
            _settings = _settings.With(endPoints: new[] { _endPoint });

            using (var subject = CreateSubject())
            {
                subject.Initialize();
                _capturedEvents.Clear();
                subject.Dispose();

                var mockServer = Mock.Get(_mockServerFactory.GetServer(_endPoint));
                mockServer.Verify(s => s.Dispose(), Times.Once);

                _capturedEvents.Next().Should().BeOfType<ClusterClosingEvent>();
                _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
                _capturedEvents.Next().Should().BeOfType<ClusterClosedEvent>();
                _capturedEvents.Any().Should().BeFalse();
            }
        }

        [Fact]
        public void ShouldMonitorStop_should_always_be_true()
        {
            var settings = _settings.With(scheme: ConnectionStringScheme.MongoDBPlusSrv, endPoints: new[] { new DnsEndPoint("a.b.com", 53) });
            var mockDnsMonitorFactory = CreateMockDnsMonitorFactory();
            using (var subject = CreateSubject(settings: settings, dnsMonitorFactory: mockDnsMonitorFactory.Object))
            {
                var result = ((IDnsMonitoringCluster)subject).ShouldDnsMonitorStop();
                result.Should().BeTrue();
            }
        }

        [Fact]
        public void Server_should_be_assigned_immidiately_after_dns_response()
        {
            var settings = _settings.With(scheme: ConnectionStringScheme.MongoDBPlusSrv, endPoints: new[] { new DnsEndPoint("a.b.com", 53) });
            var mockDnsMonitorFactory = CreateMockDnsMonitorFactory();
            using (var subject = CreateSubject(settings: settings, dnsMonitorFactory: mockDnsMonitorFactory.Object))
            {
                subject._server().Should().BeNull();
                PublishDnsResults(subject, _endPoint);
                subject._server().Should().NotBeNull();
            }
        }

        // private methods
        private Mock<IDnsMonitorFactory> CreateMockDnsMonitorFactory()
        {
            var mockDnsMonitorFactory = new Mock<IDnsMonitorFactory>();
            mockDnsMonitorFactory
                .Setup(m => m.CreateDnsMonitor(It.IsAny<IDnsMonitoringCluster>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Mock.Of<IDnsMonitor>());
            return mockDnsMonitorFactory;
        }

        private LoadBalancedCluster CreateSubject(ClusterSettings settings = null, IDnsMonitorFactory dnsMonitorFactory = null)
        {
            return dnsMonitorFactory != null
                ? new LoadBalancedCluster(settings ?? _settings, _mockServerFactory, _capturedEvents, LoggerFactory, dnsMonitorFactory)
                : new LoadBalancedCluster(settings ?? _settings, _mockServerFactory, _capturedEvents, LoggerFactory);
        }

        private void PublishDnsException(IDnsMonitoringCluster cluster, Exception exception)
        {
            cluster.ProcessDnsException(exception);
        }

        private void PublishDnsResults(IDnsMonitoringCluster cluster, params EndPoint[] endPoints)
        {
            cluster.ProcessDnsResults(endPoints.Cast<DnsEndPoint>().ToList());
        }

        private void PublishDescription(EndPoint endPoint)
        {
            var current = _mockServerFactory.GetServerDescription(endPoint);

            var description = current.With(
                state: ServerState.Connected,
                type: ServerType.LoadBalanced);

            _mockServerFactory.PublishDescription(description);
        }
    }

    internal static class LoadBalancedClusterReflector
    {
        public static IClusterableServer InitializeServer(this LoadBalancedCluster cluster, IClusterableServer server) => (IClusterableServer)Reflector.Invoke(cluster, nameof(InitializeServer), server);
        public static IDnsMonitorFactory _dnsMonitorFactory(this LoadBalancedCluster cluster) => (IDnsMonitorFactory)Reflector.GetFieldValue(cluster, nameof(_dnsMonitorFactory));
        public static Thread _dnsMonitorThread(this LoadBalancedCluster cluster) => (Thread)Reflector.GetFieldValue(cluster, nameof(_dnsMonitorThread));
        public static IClusterableServer _server(this LoadBalancedCluster cluster) => (IClusterableServer)Reflector.GetFieldValue(cluster, nameof(_server));
        public static InterlockedInt32 _state(this LoadBalancedCluster cluster) => (InterlockedInt32)Reflector.GetFieldValue(cluster, nameof(_state));
    }
}
