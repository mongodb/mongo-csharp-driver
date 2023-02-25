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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Logging;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.ConnectionPools
{
    internal sealed partial class ExclusiveConnectionPool : IConnectionPool
    {
        // fields
        private readonly CheckOutReasonCounter _checkOutReasonCounter;
        private readonly IConnectionFactory _connectionFactory;
        private readonly ListConnectionHolder _connectionHolder;
        private readonly EndPoint _endPoint;
        private int _generation;
        private readonly MaintenanceHelper _maintenanceHelper;
        private readonly ServerId _serverId;
        private readonly ServiceStates _serviceStates;
        private readonly ConnectionPoolSettings _settings;
        private readonly PoolState _poolState;
        private int _waitQueueFreeSlots;
        private readonly SemaphoreSlimSignalable _maxConnectionsQueue;
        private readonly SemaphoreSlimSignalable _maxConnectingQueue;
        private readonly IConnectionExceptionHandler _connectionExceptionHandler;

        private readonly EventLogger<LogCategories.Connection> _eventLogger;

        // constructors
        public ExclusiveConnectionPool(
            ServerId serverId,
            EndPoint endPoint,
            ConnectionPoolSettings settings,
            IConnectionFactory connectionFactory,
            IConnectionExceptionHandler connectionExceptionHandler,
            EventLogger<LogCategories.Connection> eventLogger)
        {
            _serverId = Ensure.IsNotNull(serverId, nameof(serverId));
            _endPoint = Ensure.IsNotNull(endPoint, nameof(endPoint));
            _settings = Ensure.IsNotNull(settings, nameof(settings));
            _connectionFactory = Ensure.IsNotNull(connectionFactory, nameof(connectionFactory));
            _connectionExceptionHandler = Ensure.IsNotNull(connectionExceptionHandler, nameof(connectionExceptionHandler));

            _eventLogger = Ensure.IsNotNull(eventLogger, nameof(eventLogger));

            _maintenanceHelper = new MaintenanceHelper(this, _settings.MaintenanceInterval);
            _poolState = new PoolState(EndPointHelper.ToString(_endPoint));
            _checkOutReasonCounter = new CheckOutReasonCounter();

            _maxConnectingQueue = new SemaphoreSlimSignalable(settings.MaxConnecting);
            _connectionHolder = new ListConnectionHolder(_eventLogger, _maxConnectingQueue);
            _maxConnectionsQueue = new SemaphoreSlimSignalable(settings.MaxConnections);

            _serviceStates = new ServiceStates();
#pragma warning disable 618
            _waitQueueFreeSlots = settings.WaitQueueSize;
#pragma warning restore 618
        }

        // properties
        public int AvailableCount
        {
            get
            {
                _poolState.ThrowIfDisposed();
                return _maxConnectionsQueue.Count;
            }
        }

        public int CreatedCount
        {
            get
            {
                _poolState.ThrowIfDisposed();
                return UsedCount + DormantCount;
            }
        }

        public int DormantCount
        {
            get
            {
                _poolState.ThrowIfDisposed();
                return _connectionHolder.Count;
            }
        }

        public int Generation
        {
            get { return _generation; }
        }

        public int PendingCount
        {
            get
            {
                _poolState.ThrowIfDisposed();
                return _settings.MaxConnecting - _maxConnectingQueue.Count;
            }
        }

        public ServerId ServerId
        {
            get { return _serverId; }
        }

        public ConnectionPoolSettings Settings => _settings;

        public int UsedCount
        {
            get
            {
                _poolState.ThrowIfDisposed();
                return _settings.MaxConnections - AvailableCount;
            }
        }

        // internal properties
        internal ListConnectionHolder ConnectionHolder => _connectionHolder;

        // public methods
        public IConnectionHandle AcquireConnection(CancellationToken cancellationToken)
        {
            using var helper = new AcquireConnectionHelper(this);
            return helper.AcquireConnection(cancellationToken);
        }

        public async Task<IConnectionHandle> AcquireConnectionAsync(CancellationToken cancellationToken)
        {
            using var helper = new AcquireConnectionHelper(this);
            return await helper.AcquireConnectionAsync(cancellationToken).ConfigureAwait(false);
        }

        public void Clear(bool closeInUseConnections = false)
        {
            lock (_poolState)
            {
                _poolState.ThrowIfNotInitialized();

                if (_poolState.TransitionState(State.Paused))
                {
                    _eventLogger.LogAndPublish(new ConnectionPoolClearingEvent(_serverId, _settings, closeInUseConnections));

                    int? maxGenerationToReap = closeInUseConnections ? _generation : null;
                    _generation++;

                    _maxConnectionsQueue.Signal();
                    _maxConnectingQueue.Signal();

                    _eventLogger.LogAndPublish(new ConnectionPoolClearedEvent(_serverId, _settings, closeInUseConnections));
                    _maintenanceHelper.Stop(maxGenerationToReap);
                }
            }
        }

        public void Clear(ObjectId serviceId)
        {
            lock (_poolState)
            {
                // access to _poolState should be synchronized
                _poolState.ThrowIfNotReadyNonPausable();
            }

            // generation increment can happen outside lock, as _serviceStates is threadsafe
            // and currently we allow dispose to start during generation increment.
            _eventLogger.LogAndPublish(new ConnectionPoolClearingEvent(_serverId, _settings, serviceId));

            _serviceStates.IncrementGeneration(serviceId);

            _eventLogger.LogAndPublish(new ConnectionPoolClearedEvent(_serverId, _settings, serviceId));
        }

        private PooledConnection CreateNewConnection()
        {
            var connection = _connectionFactory.CreateConnection(_serverId, _endPoint);
            var pooledConnection = new PooledConnection(this, connection);

            ConnectionHolder.TrackInUseConnection(pooledConnection);
            _eventLogger.LogAndPublish(new ConnectionCreatedEvent(connection.ConnectionId, connection.Settings, EventContext.OperationId));

            return pooledConnection;
        }

        public void Initialize()
        {
            lock (_poolState)
            {
                if (_poolState.TransitionState(State.Paused))
                {
                    _eventLogger.LogAndPublish(new ConnectionPoolOpeningEvent(_serverId, _settings), _connectionFactory.ConnectionSettings);
                    _eventLogger.LogAndPublish(new ConnectionPoolOpenedEvent(_serverId, _settings), _connectionFactory.ConnectionSettings);
                }
            }
        }

        public void SetReady()
        {
            lock (_poolState)
            {
                var targetState = _settings.IsPausable ? State.Ready : State.ReadyNonPausable;

                if (_poolState.TransitionState(targetState))
                {
                    _maxConnectionsQueue.Reset();

                    _eventLogger.LogAndPublish(new ConnectionPoolReadyEvent(_serverId, _settings));

                    _maintenanceHelper.Start();
                }
            }
        }

        public void Dispose()
        {
            var dispose = false;

            lock (_poolState)
            {
                dispose = _poolState.TransitionState(State.Disposed);
            }

            if (dispose)
            {
                _eventLogger.LogAndPublish(new ConnectionPoolClosingEvent(_serverId));

                _maintenanceHelper.Dispose();
                _connectionHolder.Clear();
                _maxConnectionsQueue.Dispose();
                _maxConnectingQueue.Dispose();
                _eventLogger.LogAndPublish(new ConnectionPoolClosedEvent(_serverId));
            }
        }

        public int GetGeneration(ObjectId? serviceId)
        {
            // if serviceId is supported, a generation for connection should be initialized on the previous handshake step
            if (_serviceStates.TryGetGeneration(serviceId, out var generation))
            {
                return generation;
            }

            // fall back to not serviceId path
            return Generation;
        }

        // internal methods
        internal SemaphoreSlimSignalable.SemaphoreSlimSignalableAwaiter CreateMaxConnectionsAwaiter()
        {
            return _maxConnectionsQueue.CreateAwaiter();
        }

        // private methods
        private void ReleaseConnection(PooledConnection connection)
        {
            _eventLogger.LogAndPublish(new ConnectionPoolCheckingInConnectionEvent(connection.ConnectionId, EventContext.OperationId));
            _eventLogger.LogAndPublish(new ConnectionPoolCheckedInConnectionEvent(connection.ConnectionId, TimeSpan.Zero, EventContext.OperationId));

            _checkOutReasonCounter.Decrement(connection.CheckOutReason);

            if (!connection.IsExpired && !_poolState.IsDisposed)
            {
                _connectionHolder.Return(connection);
            }
            else
            {
                _serviceStates.DecrementConnectionCount(connection.Description?.ServiceId);

                _connectionHolder.RemoveConnection(connection);
            }

            if (!_poolState.IsDisposed)
            {
                _maxConnectionsQueue.Release();
            }
        }
    }
}
