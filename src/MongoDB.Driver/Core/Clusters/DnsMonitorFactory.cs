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

using System.Threading;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Logging;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Clusters
{
    internal class DnsMonitorFactory : IDnsMonitorFactory
    {
        private readonly IEventSubscriber _eventSubscriber;
        private readonly ILoggerFactory _loggerFactory;

        public DnsMonitorFactory(IEventSubscriber eventSubscriber, ILoggerFactory loggerFactory)
        {
            _eventSubscriber = Ensure.IsNotNull(eventSubscriber, nameof(eventSubscriber));
            _loggerFactory = loggerFactory;
        }

        public IDnsMonitor CreateDnsMonitor(IDnsMonitoringCluster cluster, string srvServiceName, string lookupDomainName, CancellationToken cancellationToken)
        {
            var dnsResolver = DnsClientWrapper.Instance;
            return new DnsMonitor(cluster, dnsResolver, srvServiceName, lookupDomainName, _eventSubscriber, _loggerFactory?.CreateLogger<LogCategories.SDAM>(), cancellationToken);
        }
    }
}
