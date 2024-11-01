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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Logging;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Clusters
{
    internal sealed class LoadBalancedCluster : IClusterInternal, IDnsMonitoringCluster
    {
        private readonly IClusterClock _clusterClock;
        private readonly ClusterId _clusterId;
        private readonly ClusterType _clusterType = ClusterType.LoadBalanced;
        private ClusterDescription _description;
        private readonly IDnsMonitorFactory _dnsMonitorFactory;
        private Thread _dnsMonitorThread;
        private readonly CancellationTokenSource _dnsMonitorCancellationTokenSource;
        private IClusterableServer _server;
        private readonly IClusterableServerFactory _serverFactory;
        private readonly TaskCompletionSource<bool> _serverReadyTaskCompletionSource;
        private readonly ICoreServerSessionPool _serverSessionPool;
        private readonly ClusterSettings _settings;
        private readonly InterlockedInt32 _state;
        private readonly EventLogger<LogCategories.SDAM> _eventLogger;
        private readonly EventLogger<LogCategories.ServerSelection> _serverSelectionEventLogger;

        public LoadBalancedCluster(
            ClusterSettings settings,
            IClusterableServerFactory serverFactory,
            IEventSubscriber eventSubscriber,
            ILoggerFactory loggerFactory)
            : this(
                  settings,
                  serverFactory,
                  eventSubscriber,
                  loggerFactory,
                  dnsMonitorFactory: new DnsMonitorFactory(new EventAggregator(), loggerFactory)) // should not trigger any events
        {
        }

        public LoadBalancedCluster(
            ClusterSettings settings,
            IClusterableServerFactory serverFactory,
            IEventSubscriber eventSubscriber,
            ILoggerFactory loggerFactory,
            IDnsMonitorFactory dnsMonitorFactory)
        {
            Ensure.That(!settings.DirectConnection, $"DirectConnection mode is not supported for {nameof(LoadBalancedCluster)}.");
            Ensure.That(settings.LoadBalanced, $"Only Load balanced mode is supported for a {nameof(LoadBalancedCluster)}.");
            Ensure.IsEqualTo(settings.EndPoints.Count, 1, nameof(settings.EndPoints.Count));
            Ensure.IsNull(settings.ReplicaSetName, nameof(settings.ReplicaSetName));
            Ensure.That(settings.SrvMaxHosts == 0, "srvMaxHosts cannot be used with load balanced mode.");

            _clusterClock = new ClusterClock();
            _clusterId = new ClusterId();

            _dnsMonitorCancellationTokenSource = new CancellationTokenSource();
            _dnsMonitorFactory = Ensure.IsNotNull(dnsMonitorFactory, nameof(dnsMonitorFactory));
            _settings = Ensure.IsNotNull(settings, nameof(settings));

            _serverFactory = Ensure.IsNotNull(serverFactory, nameof(serverFactory));
            _serverReadyTaskCompletionSource = new TaskCompletionSource<bool>();

            _serverSessionPool = new CoreServerSessionPool(this);

            _state = new InterlockedInt32(State.Initial);

            _description = ClusterDescription.CreateInitial(_clusterId, directConnection: false);

            _eventLogger = loggerFactory.CreateEventLogger<LogCategories.SDAM>(eventSubscriber);
            _serverSelectionEventLogger = loggerFactory.CreateEventLogger<LogCategories.ServerSelection>(eventSubscriber);
        }

        public ClusterId ClusterId => _clusterId;

        public ClusterDescription Description => _description;

        public ClusterSettings Settings => _settings;

        public event EventHandler<ClusterDescriptionChangedEventArgs> DescriptionChanged;

        // public methods
        public ICoreServerSession AcquireServerSession()
        {
            ThrowIfDisposed();
            return _serverSessionPool.AcquireSession();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_state.TryChange(State.Disposed))
            {
                if (disposing)
                {
                    _dnsMonitorCancellationTokenSource.Cancel();
                    _dnsMonitorCancellationTokenSource.Dispose();

                    _eventLogger.LogAndPublish(new ClusterClosingEvent(ClusterId));

                    var stopwatch = Stopwatch.StartNew();
                    if (_server != null)
                    {
                        _server.DescriptionChanged -= ServerDescriptionChangedHandler;
                        _server.Dispose();
                    }

                    UpdateClusterDescription(Description.WithType(ClusterType.Unknown));

                    _eventLogger.LogAndPublish(new ClusterClosedEvent(ClusterId, stopwatch.Elapsed));
                }
            }
        }

        public void Initialize()
        {
            ThrowIfDisposed();

            if (_state.TryChange(State.Initial, State.Open))
            {
                var stopwatch = Stopwatch.StartNew();
                _eventLogger.LogAndPublish(new ClusterOpeningEvent(ClusterId, Settings));

                var endPoint = _settings.EndPoints.Single();
                if (_settings.Scheme != ConnectionStringScheme.MongoDBPlusSrv)
                {
                    _server = _serverFactory.CreateServer(_clusterType, _clusterId, _clusterClock, endPoint);
                    InitializeServer(_server);
                }
                else
                {
                    // _server will be created after srv resolving
                    var dnsEndPoint = (DnsEndPoint)endPoint;
                    var lookupDomainName = dnsEndPoint.Host;
                    var monitor = _dnsMonitorFactory.CreateDnsMonitor(this, _settings.SrvServiceName, lookupDomainName, _dnsMonitorCancellationTokenSource.Token);
                    _dnsMonitorThread = monitor.Start();
                }

                _eventLogger.LogAndPublish(new ClusterOpenedEvent(ClusterId, Settings, stopwatch.Elapsed));
            }
        }

        public IServer SelectServer(IServerSelector selector, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            _serverSelectionEventLogger.LogAndPublish(new ClusterSelectingServerEvent(
                _description,
                selector,
                null,
                EventContext.OperationName));

            var index = Task.WaitAny(new[] { _serverReadyTaskCompletionSource.Task }, (int)_settings.ServerSelectionTimeout.TotalMilliseconds, cancellationToken);
            if (index != 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw CreateTimeoutException(_description); // _description will contain dnsException
            }

            if (_server != null)
            {
                _serverSelectionEventLogger.LogAndPublish(new ClusterSelectedServerEvent(
                   _description,
                   selector,
                   _server.Description,
                   TimeSpan.FromSeconds(1),
                   null,
                   EventContext.OperationName));
            }

            return _server ??
                throw new InvalidOperationException("The server must be created before usage."); // should not be reached
        }

        public async Task<IServer> SelectServerAsync(IServerSelector selector, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            _serverSelectionEventLogger.LogAndPublish(new ClusterSelectingServerEvent(
                _description,
                selector,
                null,
                EventContext.OperationName));

            var timeoutTask = Task.Delay(_settings.ServerSelectionTimeout, cancellationToken);
            var triggeredTask = await Task.WhenAny(_serverReadyTaskCompletionSource.Task, timeoutTask).ConfigureAwait(false);
            if (triggeredTask == timeoutTask)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw CreateTimeoutException(_description); // _description will contain dnsException
            }

            if (_server != null)
            {
                _serverSelectionEventLogger.LogAndPublish(new ClusterSelectedServerEvent(
                   _description,
                   selector,
                   _server.Description,
                   TimeSpan.FromSeconds(1),
                   null,
                   EventContext.OperationName));
            }

            return _server ??
                throw new InvalidOperationException("The server must be created before usage."); // should not be reached
        }

        public ICoreSessionHandle StartSession(CoreSessionOptions options = null)
        {
            ThrowIfDisposed();

            options = options ?? new CoreSessionOptions();
            var session = new CoreSession(this, _serverSessionPool, options);
            return new CoreSessionHandle(session);
        }

        // private method
        private void InitializeServer(IClusterableServer server)
        {
            ThrowIfDisposed();

            var newClusterDescription = _description
                .WithType(ClusterType.LoadBalanced)
                .WithServerDescription(server.Description)
                .WithDnsMonitorException(null);
            UpdateClusterDescription(newClusterDescription);

            server.DescriptionChanged += ServerDescriptionChangedHandler;
            server.Initialize();
        }

        private Exception CreateTimeoutException(ClusterDescription description)
        {
            var ms = (int)Math.Round(_settings.ServerSelectionTimeout.TotalMilliseconds);
            var message = $"A timeout occurred after {ms}ms selecting a server. Client view of cluster state is {description}.";
            return new TimeoutException(message);
        }

        private void UpdateClusterDescription(ClusterDescription newClusterDescription)
        {
            var oldClusterDescription = Interlocked.CompareExchange(ref _description, newClusterDescription, _description);
            OnClusterDescriptionChanged(oldClusterDescription, newClusterDescription);

            void OnClusterDescriptionChanged(ClusterDescription oldDescription, ClusterDescription newDescription)
            {
                _eventLogger.LogAndPublish(new ClusterDescriptionChangedEvent(oldDescription, newDescription));

                // used only in tests and legacy
                var handler = DescriptionChanged;
                if (handler != null)
                {
                    var args = new ClusterDescriptionChangedEventArgs(oldDescription, newDescription);
                    handler(this, args);
                }
            }
        }

        private void ServerDescriptionChangedHandler(object sender, ServerDescriptionChangedEventArgs e)
        {
            var newClusterDescription = _description.WithServerDescription(e.NewServerDescription);
            UpdateClusterDescription(newClusterDescription);

            _serverReadyTaskCompletionSource.TrySetResult(true); // the server is ready
        }

        private void ThrowIfDisposed()
        {
            if (_state.Value == State.Disposed)
            {
                throw new ObjectDisposedException(nameof(LoadBalancedCluster));
            }
        }

        void IDnsMonitoringCluster.ProcessDnsException(Exception exception)
        {
            var newDescription = _description.WithDnsMonitorException(exception);
            UpdateClusterDescription(newDescription);
        }

        void IDnsMonitoringCluster.ProcessDnsResults(List<DnsEndPoint> endPoints)
        {
            switch (endPoints.Count)
            {
                case < 1:
                    throw new InvalidOperationException("No srv records were resolved.");
                case > 1:
                    throw new InvalidOperationException("Load balanced mode cannot be used with multiple host names.");
            }

            var resolvedEndpoint = endPoints.Single();
            _server = _serverFactory.CreateServer(_clusterType, _clusterId, _clusterClock, resolvedEndpoint);
            InitializeServer(_server);
        }

        bool IDnsMonitoringCluster.ShouldDnsMonitorStop() => true;  // we need only one successful attempt

        // nested type
        private static class State
        {
            public const int Initial = 0;
            public const int Open = 1;
            public const int Disposed = 2;
        }
    }
}
