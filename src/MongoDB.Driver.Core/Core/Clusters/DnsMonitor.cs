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
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Clusters
{
    internal class DnsMonitor : IDnsMonitor
    {
        #region static
        private static string EnsureLookupDomainNameIsValid(string lookupDomainName)
        {
            Ensure.IsNotNull(lookupDomainName, nameof(lookupDomainName));
            Ensure.That(lookupDomainName.Count(c => c == '.') >= 2, "LookupDomainName must have at least three components.", nameof(lookupDomainName));
            return lookupDomainName;
        }
        #endregion

        // private fields
        private readonly CancellationToken _cancellationToken;
        private readonly IDnsMonitoringCluster _cluster;
        private readonly IDnsResolver _dnsResolver;
        private readonly string _lookupDomainName;
        private bool _processDnsResultHasEverBeenCalled;
        private readonly string _service;
        private DnsMonitorState _state;
        private Exception _unhandledException;

        private readonly Action<SdamInformationEvent> _sdamInformationEventHandler;

        // constructors
        public DnsMonitor(IDnsMonitoringCluster cluster, IDnsResolver dnsResolver, string lookupDomainName, IEventSubscriber eventSubscriber, CancellationToken cancellationToken)
        {
            _cluster = Ensure.IsNotNull(cluster, nameof(cluster));
            _dnsResolver = Ensure.IsNotNull(dnsResolver, nameof(dnsResolver));
            _lookupDomainName = EnsureLookupDomainNameIsValid(lookupDomainName);
            _cancellationToken = cancellationToken;
            _service = "_mongodb._tcp." + _lookupDomainName;
            _state = DnsMonitorState.Created;

            eventSubscriber?.TryGetEventHandler(out _sdamInformationEventHandler);
        }

        // public properties
        public DnsMonitorState State => _state;

        public Exception UnhandledException => _unhandledException;

        // public methods
        public Thread Start()
        {
            var thread = new Thread(ThreadStart);
            thread.Start();
            return thread;
        }

        // private methods
        private void ThreadStart()
        {
            _state = DnsMonitorState.Running;

            try
            {
                Monitor();
            }
            catch (OperationCanceledException)
            {
                // ignore OperationCanceledException
            }
            catch (Exception exception)
            {
                _unhandledException = exception;

                if (_sdamInformationEventHandler != null)
                {
                    var message = $"Unhandled exception in DnsMonitor: {exception}.";
                    var sdamInformationEvent = new SdamInformationEvent(() => message);
                    _sdamInformationEventHandler(sdamInformationEvent);
                }

                _state = DnsMonitorState.Failed;
                return;
            }

            _state = DnsMonitorState.Stopped;
        }

        private TimeSpan ComputeRescanDelay(List<SrvRecord> srvRecords)
        {
            var delay = TimeSpan.FromSeconds(60);

            if (srvRecords.Count > 0)
            {
                var minTimeToLive = srvRecords.Select(s => s.TimeToLive).Min();
                if (minTimeToLive > delay)
                {
                    delay = minTimeToLive;
                }
            }

            return delay;
        }

        private List<DnsEndPoint> GetValidEndPoints(List<SrvRecord> srvRecords)
        {
            var validEndPoints = new List<DnsEndPoint>();

            foreach (var srvRecord in srvRecords)
            {
                var endPoint = srvRecord.EndPoint;
                var host = endPoint.Host;
                if (host.EndsWith(".", StringComparison.Ordinal))
                {
                    host = host.Substring(0, host.Length - 1);
                    endPoint = new DnsEndPoint(host, endPoint.Port);
                }

                if (IsValidHost(endPoint))
                {
                    validEndPoints.Add(endPoint);
                }
                else
                {
                    if (_sdamInformationEventHandler != null)
                    {
                        var message = $"Invalid host returned by DNS SRV lookup: {host}.";
                        var sdamInformationEvent = new SdamInformationEvent(() => message);
                        _sdamInformationEventHandler(sdamInformationEvent);
                    }
                }
            }

            return validEndPoints;
        }

        private bool IsValidHost(DnsEndPoint endPoint)
        {
            return ConnectionString.HasValidParentDomain(_lookupDomainName, endPoint);
        }

        private void Monitor()
        {
            while (true)
            {
                if (_processDnsResultHasEverBeenCalled && _cluster.ShouldDnsMonitorStop())
                {
                    return;
                }

                List<SrvRecord> srvRecords = null;
                try
                {
                    srvRecords = _dnsResolver.ResolveSrvRecords(_service, _cancellationToken);
                }
                catch (Exception exception)
                {
                    if (!_processDnsResultHasEverBeenCalled)
                    {
                        _cluster.ProcessDnsException(exception);
                    }
                }

                if (srvRecords != null)
                {
                    var endPoints = GetValidEndPoints(srvRecords);
                    if (endPoints.Count > 0)
                    {
                        _cluster.ProcessDnsResults(endPoints);
                        _processDnsResultHasEverBeenCalled = true;
                    }
                    else
                    {
                        if (_sdamInformationEventHandler != null)
                        {
                            var message = $"A DNS SRV query on \"{_service}\" returned no valid hosts.";
                            var sdamInformationEvent = new SdamInformationEvent(() => message);
                            _sdamInformationEventHandler(sdamInformationEvent);
                        }
                    }
                }

                if (_cluster.ShouldDnsMonitorStop())
                {
                    return;
                }

                _cancellationToken.ThrowIfCancellationRequested();
                var delay = ComputeRescanDelay(srvRecords);
                Thread.Sleep(delay);
            }
        }
    }
}
