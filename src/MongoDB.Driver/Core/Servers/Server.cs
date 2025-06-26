/* Copyright 2013-present MongoDB Inc.
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
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Logging;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Servers
{
    internal abstract class Server : IClusterableServer, IConnectionExceptionHandler
    {
        private readonly IClusterClock _clusterClock;
        private readonly IConnectionPool _connectionPool;
        private readonly bool _directConnection;
        private readonly EndPoint _endPoint;
        private readonly ServerId _serverId;
        private readonly ServerSettings _settings;
        private readonly InterlockedInt32 _state;
        private readonly ServerApi _serverApi;
        private readonly EventLogger<LogCategories.SDAM> _eventLogger;

        private int _outstandingOperationsCount;

        public Server(
            ClusterId clusterId,
            IClusterClock clusterClock,
            bool directConnection,
            ServerSettings settings,
            EndPoint endPoint,
            IConnectionPoolFactory connectionPoolFactory,
            ServerApi serverApi,
            EventLogger<LogCategories.SDAM> eventLogger)
        {

            _clusterClock = Ensure.IsNotNull(clusterClock, nameof(clusterClock));
            _directConnection = directConnection;
            _settings = Ensure.IsNotNull(settings, nameof(settings));
            _endPoint = Ensure.IsNotNull(endPoint, nameof(endPoint));

            _serverId = new ServerId(clusterId, endPoint);
            _connectionPool = Ensure.IsNotNull(connectionPoolFactory, nameof(connectionPoolFactory)).CreateConnectionPool(_serverId, endPoint, this);
            _state = new InterlockedInt32(State.Initial);
            _serverApi = serverApi;
            _outstandingOperationsCount = 0;

            _eventLogger = Ensure.IsNotNull(eventLogger, nameof(eventLogger));
        }

        public event EventHandler<ServerDescriptionChangedEventArgs> DescriptionChanged;

        public IClusterClock ClusterClock => _clusterClock;
        public IConnectionPool ConnectionPool => _connectionPool;
        public abstract ServerDescription Description { get; }
        public EndPoint EndPoint => _endPoint;
        public bool IsInitialized => _state.Value != State.Initial;
        public ServerApi ServerApi => _serverApi;
        public ServerId ServerId => _serverId;
        protected EventLogger<LogCategories.SDAM> EventLogger => _eventLogger;

        int IClusterableServer.OutstandingOperationsCount => Interlocked.CompareExchange(ref _outstandingOperationsCount, 0, 0);

        public void Dispose()
        {
            if (_state.TryChange(State.Disposed))
            {
                _eventLogger.LogAndPublish(new ServerClosingEvent(_serverId));

                var stopwatch = Stopwatch.StartNew();

                Dispose(disposing: true);

                _connectionPool.Dispose();
                stopwatch.Stop();

                _eventLogger.LogAndPublish(new ServerClosedEvent(_serverId, stopwatch.Elapsed));
            }
        }

        public void HandleChannelException(IConnection connection, Exception ex)
        {
            if (!IsOpen() || ShouldIgnoreException(ex))
            {
                return;
            }

            ex = GetEffectiveException(ex);

            HandleAfterHandshakeCompletesException(connection, ex);

            bool ShouldIgnoreException(Exception ex)
            {
                // For most connection exceptions, we are going to immediately
                // invalidate the server. However, we aren't going to invalidate
                // because of OperationCanceledExceptions. We trust that the
                // implementations of connection don't leave themselves in a state
                // where they can't be used based on user cancellation.
                return ex is OperationCanceledException;
            }

            Exception GetEffectiveException(Exception ex) =>
                ex is AggregateException aggregateException && aggregateException.InnerExceptions.Count == 1
                    ? aggregateException.InnerException
                    : ex;
        }

        public void HandleExceptionOnOpen(Exception exception) =>
            HandleBeforeHandshakeCompletesException(exception);

        public IConnectionHandle GetConnection(OperationContext operationContext)
        {
            ThrowIfNotOpen();

            try
            {
                Interlocked.Increment(ref _outstandingOperationsCount);

                return _connectionPool.AcquireConnection(operationContext);
            }
            catch
            {
                Interlocked.Decrement(ref _outstandingOperationsCount);

                throw;
            }
        }

        public async Task<IConnectionHandle> GetConnectionAsync(OperationContext operationContext)
        {
            ThrowIfNotOpen();

            try
            {
                Interlocked.Increment(ref _outstandingOperationsCount);
                return await _connectionPool.AcquireConnectionAsync(operationContext).ConfigureAwait(false);
            }
            catch
            {
                Interlocked.Decrement(ref _outstandingOperationsCount);

                throw;
            }
        }

        public void Initialize()
        {
            if (_state.TryChange(State.Initial, State.Open))
            {
                _eventLogger.LogAndPublish(new ServerOpeningEvent(_serverId, _settings));

                var stopwatch = Stopwatch.StartNew();
                _connectionPool.Initialize();
                InitializeSubClass();
                stopwatch.Stop();

                _eventLogger.LogAndPublish(new ServerOpenedEvent(_serverId, _settings, stopwatch.Elapsed));
            }
        }

        [Obsolete("Use Invalidate with TopologyDescription instead.")]
        public void Invalidate(string reasonInvalidated)
        {
            Invalidate(reasonInvalidated, responseTopologyDescription: null);
        }

        public void Invalidate(string reasonInvalidated, TopologyVersion responseTopologyDescription)
        {
            Invalidate(reasonInvalidated, clearConnectionPool: true, responseTopologyDescription);
        }

        public abstract void RequestHeartbeat();

        public void ReturnConnection(IConnectionHandle connection)
        {
            Interlocked.Decrement(ref _outstandingOperationsCount);
        }

        // protected methods

        protected abstract void Invalidate(string reasonInvalidated, bool clearConnectionPool, TopologyVersion responseTopologyDescription);

        protected abstract void Dispose(bool disposing);

        protected abstract void HandleBeforeHandshakeCompletesException(Exception ex);
        protected abstract void HandleAfterHandshakeCompletesException(IConnection connection, Exception ex);

        protected abstract void InitializeSubClass();

        protected bool DirectConnection => _directConnection;

        protected bool IsStateChangeException(Exception ex) => ex is MongoNotPrimaryException || ex is MongoNodeIsRecoveringException;

        protected bool IsShutdownException(Exception ex) => ex is MongoNodeIsRecoveringException mongoNodeIsRecoveringException && mongoNodeIsRecoveringException.IsShutdownError;

        protected void TriggerServerDescriptionChanged(object sender, ServerDescriptionChangedEventArgs e)
        {
            if (!e.OldServerDescription.SdamEquals(e.NewServerDescription))
            {
                _eventLogger.LogAndPublish(new ServerDescriptionChangedEvent(e.OldServerDescription, e.NewServerDescription));
            }

            var handler = DescriptionChanged;
            if (handler != null)
            {
                try { handler(this, e); }
                catch { } // ignore exceptions
            }
        }

        protected bool ShouldClearConnectionPoolForChannelException(Exception ex, int maxWireVersion)
        {
            if (ex is MongoConnectionException mongoCommandException &&
                mongoCommandException.IsNetworkException &&
                !mongoCommandException.ContainsTimeoutException)
            {
                return true;
            }
            if (IsStateChangeException(ex))
            {
                return
                    IsShutdownException(ex) ||
                    !Feature.KeepConnectionPoolWhenNotPrimaryConnectionException.IsSupported(maxWireVersion);
            }
            return false;
        }

        // private methods
        private bool IsOpen() => _state.Value == State.Open;

        private void ThrowIfDisposed()
        {
            if (_state.Value == State.Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private void ThrowIfNotOpen()
        {
            if (!IsOpen())
            {
                ThrowIfDisposed();
                throw new InvalidOperationException("Server must be initialized.");
            }
        }

        // nested types
        private static class State
        {
            public const int Initial = 0;
            public const int Open = 1;
            public const int Disposed = 2;
        }
    }
}
