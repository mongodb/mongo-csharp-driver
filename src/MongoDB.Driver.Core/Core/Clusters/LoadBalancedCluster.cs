/* Copyright 2021-present MongoDB Inc.
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
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Libmongocrypt;

namespace MongoDB.Driver.Core.Clusters
{
    /// <summary>
    /// Represents the cluster that use load balancing.
    /// </summary>
    internal class LoadBalancedCluster : ICluster, IDnsMonitoringCluster
    {
        private readonly IClusterClock _clusterClock;
        private readonly ClusterId _clusterId;
        private readonly ClusterType _clusterType = ClusterType.LoadBalanced;
        private CryptClient _cryptClient = null;
        private ClusterDescription _description;
        private readonly IDnsMonitorFactory _dnsMonitorFactory;
        private Thread _dnsMonitorThread;
        private readonly CancellationTokenSource _dnsMonitorCancellationTokenSource;
        private readonly IEventSubscriber _eventSubscriber;
        private IClusterableServer _server;
        private readonly IClusterableServerFactory _serverFactory;
        private readonly TaskCompletionSource<bool> _serverReadyTaskCompletionSource;
        private readonly ICoreServerSessionPool _serverSessionPool;
        private readonly ClusterSettings _settings;
        private readonly InterlockedInt32 _state;

        private readonly Action<ClusterClosingEvent> _closingEventHandler;
        private readonly Action<ClusterClosedEvent> _closedEventHandler;
        private readonly Action<ClusterOpeningEvent> _openingEventHandler;
        private readonly Action<ClusterOpenedEvent> _openedEventHandler;
        private readonly Action<ClusterDescriptionChangedEvent> _descriptionChangedEventHandler;

        public LoadBalancedCluster(
            ClusterSettings settings,
            IClusterableServerFactory serverFactory,
            IEventSubscriber eventSubscriber)
            : this(
                  settings,
                  serverFactory,
                  eventSubscriber,
                  dnsMonitorFactory: new DnsMonitorFactory(new EventAggregator())) // should not trigger any events
        {
        }

        public LoadBalancedCluster(
            ClusterSettings settings,
            IClusterableServerFactory serverFactory,
            IEventSubscriber eventSubscriber,
            IDnsMonitorFactory dnsMonitorFactory)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            Ensure.That(settings.ConnectionModeSwitch != ConnectionModeSwitch.UseConnectionMode, $"{nameof(ConnectionModeSwitch.UseConnectionMode)} must not be used for a {nameof(LoadBalancedCluster)}.");
            if (settings.ConnectionModeSwitch == ConnectionModeSwitch.UseDirectConnection)
            {
                Ensure.That(!settings.DirectConnection.GetValueOrDefault(), $"DirectConnection mode is not supported for {nameof(LoadBalancedCluster)}.");
            }
#pragma warning restore CS0618 // Type or member is obsolete
            Ensure.That(settings.LoadBalanced, $"Only Load balanced mode is supported for a {nameof(LoadBalancedCluster)}.");

            Ensure.IsEqualTo(settings.EndPoints.Count, 1, nameof(settings.EndPoints.Count));
            Ensure.IsEqualTo(settings.LoadBalanced, true, nameof(settings.LoadBalanced));
            Ensure.That(settings.ReplicaSetName == null, nameof(settings.ReplicaSetName));

            _clusterClock = new ClusterClock();
            _clusterId = new ClusterId();

            _dnsMonitorCancellationTokenSource = new CancellationTokenSource();
            _dnsMonitorFactory = Ensure.IsNotNull(dnsMonitorFactory, nameof(dnsMonitorFactory));
            _settings = Ensure.IsNotNull(settings, nameof(settings));

            _serverFactory = Ensure.IsNotNull(serverFactory, nameof(serverFactory));
            _serverReadyTaskCompletionSource = new TaskCompletionSource<bool>();

            _serverSessionPool = new CoreServerSessionPool(this);

            _state = new InterlockedInt32(State.Initial);

            _description = ClusterDescription.CreateInitial(
                _clusterId,
#pragma warning disable CS0618 // Type or member is obsolete
                ClusterConnectionMode.Automatic,
                ConnectionModeSwitch.UseConnectionMode,
#pragma warning restore CS0618 // Type or member is obsolete
                null,
                loadBalanced: true);

            _eventSubscriber = eventSubscriber;
            eventSubscriber.TryGetEventHandler(out _closingEventHandler);
            eventSubscriber.TryGetEventHandler(out _closedEventHandler);
            eventSubscriber.TryGetEventHandler(out _openingEventHandler);
            eventSubscriber.TryGetEventHandler(out _openedEventHandler);
            eventSubscriber.TryGetEventHandler(out _descriptionChangedEventHandler);
        }

        public ClusterId ClusterId => _clusterId;
        public CryptClient CryptClient => _cryptClient;

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
                    _closingEventHandler?.Invoke(new ClusterClosingEvent(ClusterId));
                    var stopwatch = Stopwatch.StartNew();
                    if (_server != null)
                    {
                        _server.DescriptionChanged -= ServerDescriptionChangedHandler;
                        _server.Dispose();
                    }
                    _closedEventHandler?.Invoke(new ClusterClosedEvent(ClusterId, stopwatch.Elapsed));
                }
            }
        }

        public void Initialize()
        {
            ThrowIfDisposed();

            if (_state.TryChange(State.Initial, State.Open))
            {
                var stopwatch = Stopwatch.StartNew();
                _openingEventHandler?.Invoke(new ClusterOpeningEvent(ClusterId, Settings));

                if (_settings.KmsProviders != null || _settings.SchemaMap != null)
                {
                    _cryptClient = CryptClientCreator.CreateCryptClient(_settings.KmsProviders, _settings.SchemaMap);
                }

                var endPoint = _settings.EndPoints.Single();
                if (_settings.Scheme != ConnectionStringScheme.MongoDBPlusSrv)
                {
                    _server = CreateInitializedServer(endPoint);
                }
                else
                {
                    // _server will be created after srv resolving
                    var dnsEndPoint = (DnsEndPoint)endPoint;
                    var lookupDomainName = dnsEndPoint.Host;
                    var monitor = _dnsMonitorFactory.CreateDnsMonitor(this, lookupDomainName, _dnsMonitorCancellationTokenSource.Token);
                    _dnsMonitorThread = monitor.Start();
                }

                _openedEventHandler?.Invoke(new ClusterOpenedEvent(ClusterId, Settings, stopwatch.Elapsed));
            }
        }

        public IServer SelectServer(IServerSelector _, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            var timeoutTask = Task.Delay(_settings.ServerSelectionTimeout, cancellationToken);
            var index = Task.WaitAny(_serverReadyTaskCompletionSource.Task, timeoutTask);
            if (index != 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw CreateTimeoutException(_description); // _description will contain dnsException
            }

            return
                _server ??
                throw new InvalidOperationException("The server must be created before usage."); // should not be reached
        }

        public async Task<IServer> SelectServerAsync(IServerSelector _, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            var timeoutTask = Task.Delay(_settings.ServerSelectionTimeout, cancellationToken);
            var triggeredTask = await Task.WhenAny(_serverReadyTaskCompletionSource.Task, timeoutTask).ConfigureAwait(false);
            if (triggeredTask == timeoutTask)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw CreateTimeoutException(_description); // _description will contain dnsException
            }

            return
                _server ??
                throw new InvalidOperationException("The server must be created before usage."); // should not be reached
        }

        public ICoreSessionHandle StartSession(CoreSessionOptions options = null)
        {
            ThrowIfDisposed();

            options = options ?? new CoreSessionOptions();
            var serverSession = AcquireServerSession();
            var session = new CoreSession(this, serverSession, options);
            return new CoreSessionHandle(session);
        }

        // private method
        private IClusterableServer CreateInitializedServer(EndPoint endPoint)
        {
            var server = _serverFactory.CreateServer(_clusterType, _clusterId, _clusterClock, endPoint);

            var newClusterDescription = _description
                .WithType(ClusterType.LoadBalanced)
                .WithServerDescription(server.Description)
                .WithDnsMonitorException(null);
            UpdateClusterDescription(newClusterDescription);

            server.DescriptionChanged += ServerDescriptionChangedHandler;
            server.Initialize();
            return server;
        }

        private Exception CreateTimeoutException(ClusterDescription description)
        {
            var ms = (int)Math.Round(_settings.ServerSelectionTimeout.TotalMilliseconds);
            var message = string.Format(
                "A timeout occurred after {0}ms selecting a server. Client view of cluster state is {1}.",
                ms.ToString(),
                description.ToString());
            return new TimeoutException(message);
        }

        private void UpdateClusterDescription(ClusterDescription newClusterDescription)
        {
            var oldClusterDescription = Interlocked.CompareExchange(ref _description, newClusterDescription, _description);
            OnClusterDescriptionChanged(oldClusterDescription, newClusterDescription, true);

            void OnClusterDescriptionChanged(ClusterDescription oldDescription, ClusterDescription newDescription, bool shouldSdamClusterDescriptionChangedEventBePublished)
            {
                if (shouldSdamClusterDescriptionChangedEventBePublished && _descriptionChangedEventHandler != null)
                {
                    _descriptionChangedEventHandler(new ClusterDescriptionChangedEvent(oldDescription, newDescription));
                }

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
            _server = CreateInitializedServer(resolvedEndpoint);
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
