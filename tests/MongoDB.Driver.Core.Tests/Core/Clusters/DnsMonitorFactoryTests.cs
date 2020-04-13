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
using System.Threading;
using FluentAssertions;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.Clusters
{
    public class DnsMonitorFactoryTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var eventSubscriber = Mock.Of<IEventSubscriber>();

            var result = new DnsMonitorFactory(eventSubscriber);

            result._eventSubscriber().Should().BeSameAs(eventSubscriber);
        }

        [Fact]
        public void constructor_should_throw_when_eventSubscriber_is_null()
        {
            var exception = Record.Exception(() => new DnsMonitorFactory(eventSubscriber: null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("eventSubscriber");
        }

        [Fact]
        public void CreateDnsMonitor_should_return_expected_result()
        {
            var mockEventSubscriber = new Mock<IEventSubscriber>();
            var subject = new DnsMonitorFactory(mockEventSubscriber.Object);
            var cluster = Mock.Of<IDnsMonitoringCluster>();
            var lookupDomainName = "a.b.com";
            var cancellationToken = new CancellationTokenSource().Token;

            var result = subject.CreateDnsMonitor(cluster, lookupDomainName, cancellationToken);

            var dnsMonitor = result.Should().BeOfType<DnsMonitor>().Subject;
            dnsMonitor._cluster().Should().BeSameAs(cluster);
            dnsMonitor._lookupDomainName().Should().Be(lookupDomainName);
            dnsMonitor._cancellationToken().Should().Be(cancellationToken);

            Action<SdamInformationEvent> sdamInformationEventHandler;
            mockEventSubscriber.Verify(m => m.TryGetEventHandler<SdamInformationEvent>(out sdamInformationEventHandler), Times.Once);
        }
    }

    internal static class DnsMonitorFactoryReflector
    {
        public static IDnsResolver _dnsResolver(this DnsMonitorFactory obj) => (IDnsResolver)Reflector.GetFieldValue(obj, nameof(_dnsResolver));
        public static IEventSubscriber _eventSubscriber(this DnsMonitorFactory obj) => (IEventSubscriber)Reflector.GetFieldValue(obj, nameof(_eventSubscriber));
    }
}
