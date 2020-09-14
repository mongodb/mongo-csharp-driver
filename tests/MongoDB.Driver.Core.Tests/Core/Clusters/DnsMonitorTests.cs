/* Copyright 2019-present MongoDB Inc.
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
using System.Net;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Clusters
{
    public class DnsMonitorTests
    {
        [Theory]
        [InlineData("a.b.com")]
        [InlineData("a.b.c.com")]
        public void EnsureLookupDomainNameIsValid_should_return_expected_result(string lookupDomainName)
        {
            var result = DnsMonitorReflector.EnsureLookupDomainNameIsValid(lookupDomainName);

            result.Should().Be(lookupDomainName);
        }

        [Fact]
        public void EnsureLookupDomainNameIsValid_should_throw_when_lookupDomainName_is_null()
        {
            var exception = Record.Exception(() => DnsMonitorReflector.EnsureLookupDomainNameIsValid(null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("lookupDomainName");
        }

        [Theory]
        [InlineData("")]
        [InlineData("com")]
        [InlineData("a.com")]
        public void EnsureLookupDomainNameIsValid_should_throw_when_lookupDomainName_is_invalid(string lookupDomainName)
        {
            var exception = Record.Exception(() => DnsMonitorReflector.EnsureLookupDomainNameIsValid(lookupDomainName));

            var e = exception.Should().BeOfType<ArgumentException>().Subject;
            e.ParamName.Should().Be("lookupDomainName");
        }

        [Fact]
        public void constructor_should_initialize_instance()
        {
            var cluster = Mock.Of<IDnsMonitoringCluster>();
            var dnsResolver = Mock.Of<IDnsResolver>();
            var lookupDomainName = "a.b.com";
            var mockEventSubscriber = new Mock<IEventSubscriber>();
            var sdamInformationEventHandler = (Action<SdamInformationEvent>)(e => { });
            mockEventSubscriber
                .Setup(m => m.TryGetEventHandler<SdamInformationEvent>(out sdamInformationEventHandler))
                .Returns(true);
            var cancellationToken = new CancellationTokenSource().Token;

            var subject = new DnsMonitor(cluster, dnsResolver, lookupDomainName, mockEventSubscriber.Object, cancellationToken);

            subject.State.Should().Be(DnsMonitorState.Created);
            subject._cancellationToken().Should().Be(cancellationToken);
            subject._cluster().Should().BeSameAs(cluster);
            subject._dnsResolver().Should().BeSameAs(dnsResolver);
            subject._lookupDomainName().Should().Be("a.b.com");
            subject._processDnsResultHasEverBeenCalled().Should().BeFalse();
            subject._sdamInformationEventHandler().Should().Be(sdamInformationEventHandler);
            subject._service().Should().Be("_mongodb._tcp.a.b.com");
            subject._unhandledException().Should().BeNull();
        }

        [Fact]
        public void constructor_should_throw_when_cluster_is_null()
        {
            var dnsResolver = Mock.Of<IDnsResolver>();
            var lookupDomainName = "a.b.com";
            var cancellationToken = new CancellationTokenSource().Token;

            var exception = Record.Exception(() => new DnsMonitor(null, dnsResolver, lookupDomainName, null, cancellationToken));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("cluster");
        }

        [Fact]
        public void constructor_should_throw_when_dnsResolver_is_null()
        {
            var cluster = Mock.Of<IDnsMonitoringCluster>();
            var lookupDomainName = "a.b.com";
            var cancellationToken = new CancellationTokenSource().Token;

            var exception = Record.Exception(() => new DnsMonitor(cluster, null, lookupDomainName, null, cancellationToken));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("dnsResolver");
        }

        [Fact]
        public void constructor_should_throw_when_lookupDomainName_is_null()
        {
            var cluster = Mock.Of<IDnsMonitoringCluster>();
            var dnsResolver = Mock.Of<IDnsResolver>();
            var cancellationToken = new CancellationTokenSource().Token;

            var exception = Record.Exception(() => new DnsMonitor(cluster, dnsResolver, null, null, cancellationToken));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("lookupDomainName");
        }

        [Theory]
        [InlineData("")]
        [InlineData("com")]
        [InlineData("a.com")]
        public void constructor_should_throw_when_lookupDomainName_is_invalid(string lookupDomainName)
        {
            var cluster = Mock.Of<IDnsMonitoringCluster>();
            var dnsResolver = Mock.Of<IDnsResolver>();
            var cancellationToken = new CancellationTokenSource().Token;

            var exception = Record.Exception(() => new DnsMonitor(cluster, dnsResolver, lookupDomainName, null, cancellationToken));

            var e = exception.Should().BeOfType<ArgumentException>().Subject;
            e.ParamName.Should().Be("lookupDomainName");
        }

        [Fact]
        public void Start_should_set_state_to_Running_when_starting()
        {
            var mockCluster = new Mock<IDnsMonitoringCluster>();
            var subject = CreateSubject(cluster: mockCluster.Object);
            DnsMonitorState actualState = (DnsMonitorState)(-1);
            mockCluster
                .Setup(m => m.ShouldDnsMonitorStop())
                .Returns(() => { actualState = subject.State; return true; });

            var thread = subject.Start();
            thread.Join();

            actualState.Should().Be(DnsMonitorState.Running);
        }

        [Fact]
        public void Start_should_set_call_Monitor()
        {
            var mockCluster = new Mock<IDnsMonitoringCluster>();
            mockCluster
                .Setup(m => m.ShouldDnsMonitorStop())
                .Returns(true);
            var subject = CreateSubject(cluster: mockCluster.Object);

            var thread = subject.Start();
            thread.Join();

            subject.State.Should().Be(DnsMonitorState.Stopped);
        }

        [Fact]
        public void Start_should_ignore_OperationCanceledException()
        {
            var mockCluster = new Mock<IDnsMonitoringCluster>();
            mockCluster
                .Setup(m => m.ShouldDnsMonitorStop())
                .Throws(new OperationCanceledException());
            var subject = CreateSubject(cluster: mockCluster.Object);

            var thread = subject.Start();
            thread.Join();

            subject.State.Should().Be(DnsMonitorState.Stopped);
        }

        [Fact]
        public void Start_should_set_unhandledException()
        {
            var mockCluster = new Mock<IDnsMonitoringCluster>();
            var unhandledException = new Exception();
            mockCluster
                .Setup(m => m.ShouldDnsMonitorStop())
                .Throws(unhandledException);
            var subject = CreateSubject(cluster: mockCluster.Object);

            var thread = subject.Start();
            thread.Join();

            subject.State.Should().Be(DnsMonitorState.Failed);
            subject.UnhandledException.Should().BeSameAs(unhandledException);
        }

        [Fact]
        public void Start_should_raise_SdamInformationEvent_when_there_is_an_unhandled_exception()
        {
            var mockCluster = new Mock<IDnsMonitoringCluster>();
            var unhandledException = new Exception("fake exception");
            mockCluster
                .Setup(m => m.ShouldDnsMonitorStop())
                .Throws(unhandledException);
            var mockEventSubscriber = new Mock<IEventSubscriber>();
            var actualEvent = default(SdamInformationEvent);
            var sdamInformationEventHandler = (Action<SdamInformationEvent>)(sie => actualEvent = sie);
            mockEventSubscriber
                .Setup(m => m.TryGetEventHandler<SdamInformationEvent>(out sdamInformationEventHandler));
            var subject = CreateSubject(cluster: mockCluster.Object, eventSubscriber: mockEventSubscriber.Object);

            var thread = subject.Start();
            thread.Join();

            actualEvent.Message.Should().Contain("fake exception");
        }

        [Fact]
        public void Start_should_set_state_to_Stopped_when_stopping()
        {
            var mockCluster = new Mock<IDnsMonitoringCluster>();
            mockCluster
                .Setup(m => m.ShouldDnsMonitorStop())
                .Returns(true);
            var subject = CreateSubject(cluster: mockCluster.Object);

            var thread = subject.Start();
            thread.Join();

            subject.State.Should().Be(DnsMonitorState.Stopped);
        }

        [Theory]
        [InlineData(new int[0], 60)]
        [InlineData(new[] { 30 }, 60)]
        [InlineData(new[] { 60 }, 60)]
        [InlineData(new[] { 61 }, 61)]
        [InlineData(new[] { 15, 30 }, 60)]
        [InlineData(new[] { 30, 60 }, 60)]
        [InlineData(new[] { 61, 90 }, 61)]
        public void ComputeRescanDelay_should_return_expected_result(int[] ttls, int expectedResult)
        {
            var subject = CreateSubject();
            var srvRecords = CreateSrvRecords(ttls);

            var result = subject.ComputeRescanDelay(srvRecords);

            result.Should().Be(TimeSpan.FromSeconds(expectedResult));
        }

        [Theory]
        [InlineData(new string[0], new string[0])]
        [InlineData(new[] { "x.b.com" }, new[] { "x.b.com" })]
        [InlineData(new[] { "x.b.com", "y.b.com" }, new[] { "x.b.com", "y.b.com" })]
        [InlineData(new[] { "x.c.com" }, new string[0])]
        [InlineData(new[] { "x.b.com", "y.c.com" }, new[] { "x.b.com" })]
        [InlineData(new[] { "x.c.com", "y.b.com" }, new[] { "y.b.com" })]
        [InlineData(new[] { "x.c.com", "y.c.com" }, new string[0])]
        public void GetValidEndPoints_should_return_expected_results(string[] srvEndPoints, string[] validEndPoints)
        {
            var lookupDomainName = "a.b.com";
            var subject = CreateSubject(lookupDomainName: lookupDomainName);
            var srvRecords = CreateSrvRecords(srvEndPoints);

            var result = subject.GetValidEndPoints(srvRecords);

            var expectedResult = validEndPoints.Select(x => (DnsEndPoint)EndPointHelper.Parse(x)).ToList();
            result.Should().Equal(expectedResult);
        }

        [Theory]
        [InlineData(new string[0], new string[0])]
        [InlineData(new[] { "x.b.com:27017" }, new string[0])]
        [InlineData(new[] { "x.b.com:27017", "y.b.com:27017" }, new string[0])]
        [InlineData(new[] { "x.q.com:27017", "y.b.com:27017" }, new[] { "x.q.com" })]
        [InlineData(new[] { "x.b.com:27017", "y.q.com:27017" }, new[] { "y.q.com" })]
        [InlineData(new[] { "x.q.com:27017" }, new[] { "x.q.com" })]
        [InlineData(new[] { "x.q.com:27017", "y.q.com:27017" }, new[] { "x.q.com", "y.q.com" })]
        public void GetValidEndPoints_should_raise_sdamInformationEvent_for_each_invalid_host(string[] srvEndPoints, string[] invalidHosts)
        {
            var lookupDomainName = "a.b.com";
            var mockEventSubscriber = new Mock<IEventSubscriber>();
            var actualEvents = new List<SdamInformationEvent>();
            var sdamInformationEventHandler = (Action<SdamInformationEvent>)(raisedEvent => actualEvents.Add(raisedEvent));
            mockEventSubscriber
                .Setup(m => m.TryGetEventHandler<SdamInformationEvent>(out sdamInformationEventHandler));
            var subject = CreateSubject(lookupDomainName: lookupDomainName, eventSubscriber: mockEventSubscriber.Object);
            var srvRecords = CreateSrvRecords(srvEndPoints);

            var result = subject.GetValidEndPoints(srvRecords);

            actualEvents.Should().HaveCount(invalidHosts.Length);
            for (var i = 0; i < actualEvents.Count; i++)
            {
                var actualEvent = actualEvents[i];
                var invalidHost = invalidHosts[i];
                actualEvent.Message.Should().Contain(invalidHost);
            }
        }

        [Theory]
        [InlineData("a.b.com", "x.b.com", true)]
        [InlineData("a.b.com", "x.com", false)]
        [InlineData("a.b.com", "x.c.com", false)]
        [InlineData("a.b.c.com", "x.b.c.com", true)]
        [InlineData("a.b.c.com", "x.com", false)]
        [InlineData("a.b.c.com", "x.b.com", false)]
        [InlineData("a.b.c.com", "x.c.com", false)]
        [InlineData("a.b.c.com", "x.d.com", false)]
        [InlineData("a.b.c.com", "x.b.d.com", false)]
        [InlineData("a.b.c.com", "x.d.c.com", false)]
        [InlineData("a.b.com", "x.bb.com", false)]
        public void IsValidHost_should_return_expected_result(string lookupDomainName, string host, bool expectedResult)
        {
            var subject = CreateSubject(lookupDomainName: lookupDomainName);
            var endPoint = (DnsEndPoint)EndPointHelper.Parse(host);

            var result = subject.IsValidHost(endPoint);

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void Monitor_should_return_when_ShouldDnsMonitorStop_returns_true()
        {
            var mockDnsResolver = new Mock<IDnsResolver>();
            var srvRecords = CreateSrvRecords(new[] { "oneserver.test.com" });
            mockDnsResolver
                .Setup(m => m.ResolveSrvRecords(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(srvRecords);
            var mockCluster = new Mock<IDnsMonitoringCluster>();
            mockCluster
                .Setup(m => m.ShouldDnsMonitorStop())
                .Returns(true);
            var subject = CreateSubject(cluster: mockCluster.Object, dnsResolver: mockDnsResolver.Object);

            subject.Monitor();
            mockDnsResolver.Verify(c => c.ResolveSrvRecords(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            mockCluster.Verify(c => c.ShouldDnsMonitorStop(), Times.Once);
        }

        [Fact]
        public void Monitor_should_call_ProcessDnsException_when_an_exception_is_thrown_during_dns_resolution()
        {
            var mockCluster = new Mock<IDnsMonitoringCluster>();
            var mockDnsResolver = new Mock<IDnsResolver>();
            var exception = new Exception();
            mockCluster
                .SetupSequence(m => m.ShouldDnsMonitorStop())
                .Returns(true);
            mockDnsResolver
                .Setup(m => m.ResolveSrvRecords(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(exception);
            var subject = CreateSubject(cluster: mockCluster.Object, dnsResolver: mockDnsResolver.Object);

            subject.Monitor();

            mockCluster.Verify(m => m.ProcessDnsException(exception), Times.Once);
        }

        [Theory]
        [InlineData("oneserver.test.com", new[] { "host1.test.com:27017" })]
        [InlineData("twoservers.test.com", new[] { "host1.test.com:27017", "host2.test.com:27017" })]
        public void Monitor_should_call_ProcessDnsResults_with_expected_endPoints(string lookupDomainName, string[] expectedEndPointStrings)
        {
            var mockCluster = new Mock<IDnsMonitoringCluster>();
            mockCluster
                .SetupSequence(x => x.ShouldDnsMonitorStop())
                .Returns(true);
            List<DnsEndPoint> actualEndPoints = null;
            var cts = new CancellationTokenSource();
            mockCluster
                .Setup(x => x.ProcessDnsResults(It.IsAny<List<DnsEndPoint>>()))
                .Callback((List<DnsEndPoint> endPoints) =>
                {
                    actualEndPoints = endPoints;
                    cts.Cancel();
                });
            var mockDnsResolver = new Mock<IDnsResolver>();
            var service = "_mongodb._tcp." + lookupDomainName;
            var srvRecords = CreateSrvRecords(expectedEndPointStrings);
            mockDnsResolver
                .Setup(m => m.ResolveSrvRecords(service, cts.Token))
                .Returns(srvRecords);
            var subject = CreateSubject(cluster: mockCluster.Object, dnsResolver: mockDnsResolver.Object, lookupDomainName: lookupDomainName, cancellationToken: cts.Token);

            subject.Monitor();

            var expectedEndPoints = expectedEndPointStrings.Select(e => (DnsEndPoint)EndPointHelper.Parse(e)).ToList();
            actualEndPoints.Should().Equal(expectedEndPoints);
        }

        [Fact]
        public void Monitor_should_not_call_ProcessDnsResults_when_there_are_no_valid_hosts()
        {
            var mockCluster = new Mock<IDnsMonitoringCluster>();
            mockCluster
                .SetupSequence(x => x.ShouldDnsMonitorStop())
                .Returns(true);
            var cts = new CancellationTokenSource();
            var mockDnsResolver = new Mock<IDnsResolver>();
            var lookupDomainName = "a.b.com";
            var service = "_mongodb._tcp." + lookupDomainName;
            var noSrvRecords = new List<SrvRecord>();
            mockDnsResolver
                .Setup(m => m.ResolveSrvRecords(service, cts.Token))
                .Returns(noSrvRecords);
            var mockEventSubscriber = new Mock<IEventSubscriber>();
            var actualEvents = new List<SdamInformationEvent>();
            var sdamInformationEventHandler = (Action<SdamInformationEvent>)(raisedEvent => actualEvents.Add(raisedEvent));
            mockEventSubscriber
                .Setup(m => m.TryGetEventHandler<SdamInformationEvent>(out sdamInformationEventHandler));
            var subject = CreateSubject(
                cluster: mockCluster.Object,
                dnsResolver: mockDnsResolver.Object,
                lookupDomainName: lookupDomainName,
                eventSubscriber: mockEventSubscriber.Object,
                cancellationToken: cts.Token);

            subject.Monitor();

            mockCluster.Verify(m => m.ProcessDnsResults(It.IsAny<List<DnsEndPoint>>()), Times.Never);
            var actualEvent = actualEvents.OfType<SdamInformationEvent>().Single();
            var expectedMessage = "A DNS SRV query on \"_mongodb._tcp.a.b.com\" returned no valid hosts.";
            actualEvent.Message.Should().Be(expectedMessage);
        }

        [Fact]
        public void Monitor_should_throw_when_cancellation_is_requested()
        {
            var cts = new CancellationTokenSource();
            var mockCluster = new Mock<IDnsMonitoringCluster>();
            mockCluster
                .SetupSequence(x => x.ShouldDnsMonitorStop())
                .Returns(() => { cts.Cancel(); return false; });
            var lookupDomainName = "a.b.com";
            var subject = CreateSubject(cluster: mockCluster.Object, lookupDomainName: lookupDomainName, cancellationToken: cts.Token);
            var exception = Record.Exception(() => subject.Monitor());

            exception.Should().BeOfType<OperationCanceledException>();
        }

        // private methods
        private ClusterDescription CreateClusterDescription(ClusterType type)
        {
            var clusterId = new ClusterId(1);
            var servers = new ServerDescription[0];
#pragma warning disable CS0618 // Type or member is obsolete
            return new ClusterDescription(clusterId, ClusterConnectionMode.Automatic, type, servers);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        private List<SrvRecord> CreateSrvRecords(int[] ttls)
        {
            var srvRecords = new List<SrvRecord>();

            for (var i = 0; i < ttls.Length; i++)
            {
                var host = $"host{i + 1}.b.c.com";
                var endPoint = new DnsEndPoint(host, 27017);
                var timeToLive = TimeSpan.FromSeconds(ttls[i]);
                var srvRecord = new SrvRecord(endPoint, timeToLive);
                srvRecords.Add(srvRecord);
            }

            return srvRecords;
        }

        private List<SrvRecord> CreateSrvRecords(string[] domainNames)
        {
            var srvRecords = new List<SrvRecord>();

            for (var i = 0; i < domainNames.Length; i++)
            {
                var endPoint = (DnsEndPoint)EndPointHelper.Parse(domainNames[i]);
                var timeToLive = TimeSpan.FromHours(24);
                var srvRecord = new SrvRecord(endPoint, timeToLive);
                srvRecords.Add(srvRecord);
            }

            return srvRecords;
        }

        private DnsMonitor CreateSubject(
            IDnsMonitoringCluster cluster = null,
            IDnsResolver dnsResolver = null,
            string lookupDomainName = null,
            IEventSubscriber eventSubscriber = null,
            CancellationToken cancellationToken = default)
        {
            cluster = cluster ?? Mock.Of<IDnsMonitoringCluster>();
            dnsResolver = dnsResolver ?? Mock.Of<IDnsResolver>();
            lookupDomainName = lookupDomainName ?? "a.b.c.com";
            return new DnsMonitor(cluster, dnsResolver, lookupDomainName, eventSubscriber, cancellationToken);
        }
    }

    internal static class DnsMonitorReflector
    {
        public static string EnsureLookupDomainNameIsValid(string lookupDomainName) => (string)Reflector.InvokeStatic(typeof(DnsMonitor), nameof(EnsureLookupDomainNameIsValid), lookupDomainName);

        public static CancellationToken _cancellationToken(this DnsMonitor obj) => (CancellationToken)Reflector.GetFieldValue(obj, nameof(_cancellationToken));
        public static IDnsMonitoringCluster _cluster(this DnsMonitor obj) => (IDnsMonitoringCluster)Reflector.GetFieldValue(obj, nameof(_cluster));
        public static IDnsResolver _dnsResolver(this DnsMonitor obj) => (IDnsResolver)Reflector.GetFieldValue(obj, nameof(_dnsResolver));
        public static string _lookupDomainName(this DnsMonitor obj) => (string)Reflector.GetFieldValue(obj, nameof(_lookupDomainName));
        public static bool _processDnsResultHasEverBeenCalled(this DnsMonitor obj) => (bool)Reflector.GetFieldValue(obj, nameof(_processDnsResultHasEverBeenCalled));
        public static Action<SdamInformationEvent> _sdamInformationEventHandler(this DnsMonitor obj) => (Action<SdamInformationEvent>)Reflector.GetFieldValue(obj, nameof(_sdamInformationEventHandler));
        public static string _service(this DnsMonitor obj) => (string)Reflector.GetFieldValue(obj, nameof(_service));
        public static Exception _unhandledException(this DnsMonitor obj) => (Exception)Reflector.GetFieldValue(obj, nameof(_unhandledException));

        public static TimeSpan ComputeRescanDelay(this DnsMonitor obj, List<SrvRecord> srvRecords) => (TimeSpan)Reflector.Invoke(obj, nameof(ComputeRescanDelay), srvRecords);
        public static List<DnsEndPoint> GetValidEndPoints(this DnsMonitor obj, List<SrvRecord> srvRecords) => (List<DnsEndPoint>)Reflector.Invoke(obj, nameof(GetValidEndPoints), srvRecords);
        public static bool IsValidHost(this DnsMonitor obj, DnsEndPoint endPoint) => (bool)Reflector.Invoke(obj, nameof(IsValidHost), endPoint);
        public static void Monitor(this DnsMonitor obj) => Reflector.Invoke(obj, nameof(Monitor));
    }
}
