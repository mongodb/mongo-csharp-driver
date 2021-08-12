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

        private readonly Action<ConnectionPoolCheckingOutConnectionEvent> _checkingOutConnectionEventHandler;
        private readonly Action<ConnectionPoolCheckedOutConnectionEvent> _checkedOutConnectionEventHandler;
        private readonly Action<ConnectionPoolCheckingOutConnectionFailedEvent> _checkingOutConnectionFailedEventHandler;
        private readonly Action<ConnectionPoolCheckingInConnectionEvent> _checkingInConnectionEventHandler;
        private readonly Action<ConnectionPoolCheckedInConnectionEvent> _checkedInConnectionEventHandler;
        private readonly Action<ConnectionPoolAddingConnectionEvent> _addingConnectionEventHandler;
        private readonly Action<ConnectionPoolAddedConnectionEvent> _addedConnectionEventHandler;
        private readonly Action<ConnectionPoolOpeningEvent> _openingEventHandler;
        private readonly Action<ConnectionPoolOpenedEvent> _openedEventHandler;
        private readonly Action<ConnectionPoolReadyEvent> _readyEventHandler;
        private readonly Action<ConnectionPoolClosingEvent> _closingEventHandler;
        private readonly Action<ConnectionPoolClosedEvent> _closedEventHandler;
        private readonly Action<ConnectionPoolClearingEvent> _clearingEventHandler;
        private readonly Action<ConnectionPoolClearedEvent> _clearedEventHandler;
        private readonly Action<ConnectionCreatedEvent> _connectionCreatedEventHandler;

        // constructors
        public ExclusiveConnectionPool(
            ServerId serverId,
            EndPoint endPoint,
            ConnectionPoolSettings settings,
            IConnectionFactory connectionFactory,
            IEventSubscriber eventSubscriber,
            IConnectionExceptionHandler connectionExceptionHandler)
        {
            _serverId = Ensure.IsNotNull(serverId, nameof(serverId));
            _endPoint = Ensure.IsNotNull(endPoint, nameof(endPoint));
            _settings = Ensure.IsNotNull(settings, nameof(settings));
            _connectionFactory = Ensure.IsNotNull(connectionFactory, nameof(connectionFactory));
            _connectionExceptionHandler = Ensure.IsNotNull(connectionExceptionHandler, nameof(connectionExceptionHandler));
            Ensure.IsNotNull(eventSubscriber, nameof(eventSubscriber));

            _maintenanceHelper = new MaintenanceHelper(token => MaintainSizeAsync(token));
            _poolState = new PoolState(EndPointHelper.ToString(_endPoint));
            _checkOutReasonCounter = new CheckOutReasonCounter();

            _maxConnectingQueue = new SemaphoreSlimSignalable(settings.MaxConnecting);
            _connectionHolder = new ListConnectionHolder(eventSubscriber, _maxConnectingQueue);
            _maxConnectionsQueue = new SemaphoreSlimSignalable(settings.MaxConnections);

            _serviceStates = new ServiceStates();
#pragma warning disable 618
            _waitQueueFreeSlots = settings.WaitQueueSize;
#pragma warning restore 618

            eventSubscriber.TryGetEventHandler(out _checkingOutConnectionEventHandler);
            eventSubscriber.TryGetEventHandler(out _checkedOutConnectionEventHandler);
            eventSubscriber.TryGetEventHandler(out _checkingOutConnectionFailedEventHandler);
            eventSubscriber.TryGetEventHandler(out _checkingInConnectionEventHandler);
            eventSubscriber.TryGetEventHandler(out _checkedInConnectionEventHandler);
            eventSubscriber.TryGetEventHandler(out _addingConnectionEventHandler);
            eventSubscriber.TryGetEventHandler(out _addedConnectionEventHandler);
            eventSubscriber.TryGetEventHandler(out _openingEventHandler);
            eventSubscriber.TryGetEventHandler(out _openedEventHandler);
            eventSubscriber.TryGetEventHandler(out _closingEventHandler);
            eventSubscriber.TryGetEventHandler(out _closedEventHandler);
            eventSubscriber.TryGetEventHandler(out _readyEventHandler);
            eventSubscriber.TryGetEventHandler(out _addingConnectionEventHandler);
            eventSubscriber.TryGetEventHandler(out _addedConnectionEventHandler);
            eventSubscriber.TryGetEventHandler(out _clearingEventHandler);
            eventSubscriber.TryGetEventHandler(out _clearedEventHandler);
            eventSubscriber.TryGetEventHandler(out _connectionCreatedEventHandler);
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

        public int UsedCount
        {
            get
            {
                _poolState.ThrowIfDisposed();
                return _settings.MaxConnections - AvailableCount;
            }
        }

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

        public void Clear()
        {
            lock (_poolState)
            {
                _poolState.ThrowIfNotInitialized();

                if (_poolState.TransitionState(State.Paused))
                {
                    _clearingEventHandler?.Invoke(new ConnectionPoolClearingEvent(_serverId, _settings));

                    _maintenanceHelper.Cancel();
                    _generation++;

                    _maxConnectionsQueue.Signal();
                    _maxConnectingQueue.Signal();

                    _clearedEventHandler?.Invoke(new ConnectionPoolClearedEvent(_serverId, _settings));
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
            _clearingEventHandler?.Invoke(new ConnectionPoolClearingEvent(_serverId, _settings, serviceId));

            _serviceStates.IncrementGeneration(serviceId);

            _clearedEventHandler?.Invoke(new ConnectionPoolClearedEvent(_serverId, _settings, serviceId));
        }

        private PooledConnection CreateNewConnection()
        {
            var connection = _connectionFactory.CreateConnection(_serverId, _endPoint);
            var pooledConnection = new PooledConnection(this, connection);
            _connectionCreatedEventHandler?.Invoke(new ConnectionCreatedEvent(connection.ConnectionId, connection.Settings, EventContext.OperationId));
            return pooledConnection;
        }

        public void Initialize()
        {
            lock (_poolState)
            {
                if (_poolState.TransitionState(State.Paused))
                {
                    _openingEventHandler?.Invoke(new ConnectionPoolOpeningEvent(_serverId, _settings));
                    _openedEventHandler?.Invoke(new ConnectionPoolOpenedEvent(_serverId, _settings));
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
                    _readyEventHandler?.Invoke(new ConnectionPoolReadyEvent(_serverId, _settings));

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
                if (_closingEventHandler != null)
                {
                    _closingEventHandler(new ConnectionPoolClosingEvent(_serverId));
                }

                _connectionHolder.Clear();
                _maintenanceHelper.Dispose();
                _maxConnectionsQueue.Dispose();
                _maxConnectingQueue.Dispose();
                if (_closedEventHandler != null)
                {
                    _closedEventHandler(new ConnectionPoolClosedEvent(_serverId));
                }
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

        // private methods
        private async Task MaintainSizeAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await PrunePoolAsync(cancellationToken).ConfigureAwait(false);
                        await EnsureMinSizeAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch (MongoConnectionException exception)
                    {
                        _connectionExceptionHandler.HandleExceptionOnOpen(exception);
                    }
                    catch
                    {
                        // ignore exceptions
                    }
                    await Task.Delay(_settings.MaintenanceInterval, cancellationToken).ConfigureAwait(false);
                }
            }
            catch
            {
                // ignore exceptions
            }
        }

        private async Task PrunePoolAsync(CancellationToken cancellationToken)
        {
            using (var poolAwaiter = _maxConnectionsQueue.CreateAwaiter())
            {
                var entered = await poolAwaiter.WaitSignaledAsync(TimeSpan.FromMilliseconds(20), cancellationToken).ConfigureAwait(false);
                if (!entered)
                {
                    return;
                }

                _connectionHolder.Prune();
            }
        }

        private async Task EnsureMinSizeAsync(CancellationToken cancellationToken)
        {
            var minTimeout = TimeSpan.FromMilliseconds(20);

            while (CreatedCount < _settings.MinConnections && !cancellationToken.IsCancellationRequested)
            {
                using (var poolAwaiter = _maxConnectionsQueue.CreateAwaiter())
                {
                    var entered = await poolAwaiter.WaitSignaledAsync(minTimeout, cancellationToken).ConfigureAwait(false);
                    if (!entered)
                    {
                        return;
                    }

                    using (var connectionCreator = new ConnectionCreator(this, minTimeout))
                    {
                        var connection = await connectionCreator.CreateOpenedAsync(cancellationToken).ConfigureAwait(false);
                        _connectionHolder.Return(connection);
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        private void ReleaseConnection(PooledConnection connection)
        {
            if (_checkingInConnectionEventHandler != null)
            {
                _checkingInConnectionEventHandler(new ConnectionPoolCheckingInConnectionEvent(connection.ConnectionId, EventContext.OperationId));
            }

            if (_checkedInConnectionEventHandler != null)
            {
                _checkedInConnectionEventHandler(new ConnectionPoolCheckedInConnectionEvent(connection.ConnectionId, TimeSpan.Zero, EventContext.OperationId));
            }

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
