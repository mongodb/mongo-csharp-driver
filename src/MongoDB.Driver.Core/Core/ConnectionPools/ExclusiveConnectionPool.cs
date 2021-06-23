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
        private readonly IConnectionFactory _connectionFactory;
        private readonly ListConnectionHolder _connectionHolder;
        private readonly EndPoint _endPoint;
        private int _generation;
        private readonly CancellationTokenSource _maintenanceCancellationTokenSource;
        private readonly WaitQueue _poolQueue;
        private readonly ServerId _serverId;
        private readonly ConnectionPoolSettings _settings;
        private readonly InterlockedInt32 _state;
        private readonly SemaphoreSlim _waitQueue;
        private readonly SemaphoreSlimSignalable _connectingQueue;

        private readonly Action<ConnectionPoolCheckingOutConnectionEvent> _checkingOutConnectionEventHandler;
        private readonly Action<ConnectionPoolCheckedOutConnectionEvent> _checkedOutConnectionEventHandler;
        private readonly Action<ConnectionPoolCheckingOutConnectionFailedEvent> _checkingOutConnectionFailedEventHandler;
        private readonly Action<ConnectionPoolCheckingInConnectionEvent> _checkingInConnectionEventHandler;
        private readonly Action<ConnectionPoolCheckedInConnectionEvent> _checkedInConnectionEventHandler;
        private readonly Action<ConnectionPoolAddingConnectionEvent> _addingConnectionEventHandler;
        private readonly Action<ConnectionPoolAddedConnectionEvent> _addedConnectionEventHandler;
        private readonly Action<ConnectionPoolOpeningEvent> _openingEventHandler;
        private readonly Action<ConnectionPoolOpenedEvent> _openedEventHandler;
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
            IEventSubscriber eventSubscriber)
        {
            _serverId = Ensure.IsNotNull(serverId, nameof(serverId));
            _endPoint = Ensure.IsNotNull(endPoint, nameof(endPoint));
            _settings = Ensure.IsNotNull(settings, nameof(settings));
            _connectionFactory = Ensure.IsNotNull(connectionFactory, nameof(connectionFactory));
            Ensure.IsNotNull(eventSubscriber, nameof(eventSubscriber));

            _connectingQueue = new SemaphoreSlimSignalable(MongoInternalDefaults.ConnectionPool.MaxConnecting);
            _connectionHolder = new ListConnectionHolder(eventSubscriber, _connectingQueue);
            _poolQueue = new WaitQueue(settings.MaxConnections);
#pragma warning disable 618
            _waitQueue = new SemaphoreSlim(settings.WaitQueueSize);
#pragma warning restore 618
            _maintenanceCancellationTokenSource = new CancellationTokenSource();
            _state = new InterlockedInt32(State.Initial);

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
                ThrowIfDisposed();
                return _poolQueue.CurrentCount;
            }
        }

        public int CreatedCount
        {
            get
            {
                ThrowIfDisposed();
                return UsedCount + DormantCount;
            }
        }

        public int DormantCount
        {
            get
            {
                ThrowIfDisposed();
                return _connectionHolder.Count;
            }
        }

        public int Generation
        {
            get { return Interlocked.CompareExchange(ref _generation, 0, 0); }
        }

        public int PendingCount
        {
            get
            {
                ThrowIfDisposed();
                return MongoInternalDefaults.ConnectionPool.MaxConnecting - _connectingQueue.Count;
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
                ThrowIfDisposed();
                return _settings.MaxConnections - AvailableCount;
            }
        }

        // public methods
        public IConnectionHandle AcquireConnection(CancellationToken cancellationToken)
        {
            var helper = new AcquireConnectionHelper(this);
            try
            {
                helper.CheckingOutConnection();
                ThrowIfNotOpen();
                helper.EnterWaitQueue();
                var enteredPool = _poolQueue.Wait(_settings.WaitQueueTimeout, cancellationToken);
                return helper.EnteredPool(enteredPool, cancellationToken);
            }
            catch (Exception ex)
            {
                helper.HandleException(ex);
                throw;
            }
            finally
            {
                helper.Finally();
            }
        }

        public async Task<IConnectionHandle> AcquireConnectionAsync(CancellationToken cancellationToken)
        {
            var helper = new AcquireConnectionHelper(this);
            try
            {
                helper.CheckingOutConnection();
                ThrowIfNotOpen();
                helper.EnterWaitQueue();
                var enteredPool = await _poolQueue.WaitAsync(_settings.WaitQueueTimeout, cancellationToken).ConfigureAwait(false);

                var connectionHandle = await helper.EnteredPoolAsync(enteredPool, cancellationToken).ConfigureAwait(false);
                return connectionHandle;
            }
            catch (Exception ex)
            {
                helper.HandleException(ex);
                throw;
            }
            finally
            {
                helper.Finally();
            }
        }

        public void Clear()
        {
            ThrowIfNotOpen();

            _clearingEventHandler?.Invoke(new ConnectionPoolClearingEvent(_serverId, _settings));

            Interlocked.Increment(ref _generation);

            _clearedEventHandler?.Invoke(new ConnectionPoolClearedEvent(_serverId, _settings));
        }

        public void Clear(ObjectId serviceId)
        {
            ThrowIfNotOpen();

            _clearingEventHandler?.Invoke(new ConnectionPoolClearingEvent(_serverId, _settings));

            // TODO

            _clearedEventHandler?.Invoke(new ConnectionPoolClearedEvent(_serverId, _settings));
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
            ThrowIfDisposed();
            if (_state.TryChange(State.Initial, State.Open))
            {
                if (_openingEventHandler != null)
                {
                    _openingEventHandler(new ConnectionPoolOpeningEvent(_serverId, _settings));
                }

                if (_openedEventHandler != null)
                {
                    _openedEventHandler(new ConnectionPoolOpenedEvent(_serverId, _settings));
                }

                MaintainSizeAsync().ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            if (_state.TryChange(State.Disposed))
            {
                if (_closingEventHandler != null)
                {
                    _closingEventHandler(new ConnectionPoolClosingEvent(_serverId));
                }

                _connectionHolder.Clear();
                _maintenanceCancellationTokenSource.Cancel();
                _maintenanceCancellationTokenSource.Dispose();
                _poolQueue.Dispose();
                _waitQueue.Dispose();
                _connectingQueue.Dispose();
                if (_closedEventHandler != null)
                {
                    _closedEventHandler(new ConnectionPoolClosedEvent(_serverId));
                }
            }
        }

        private async Task MaintainSizeAsync()
        {
            var maintenanceCancellationToken = _maintenanceCancellationTokenSource.Token;
            while (!maintenanceCancellationToken.IsCancellationRequested)
            {
                try
                {
                    await PrunePoolAsync(maintenanceCancellationToken).ConfigureAwait(false);
                    await EnsureMinSizeAsync(maintenanceCancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    // ignore exceptions
                }
                await Task.Delay(_settings.MaintenanceInterval, maintenanceCancellationToken).ConfigureAwait(false);
            }
        }

        private async Task PrunePoolAsync(CancellationToken cancellationToken)
        {
            bool enteredPool = false;
            try
            {
                // if it takes too long to enter the pool, then the pool is fully utilized
                // and we don't want to mess with it.
                enteredPool = await _poolQueue.WaitAsync(TimeSpan.FromMilliseconds(20), cancellationToken).ConfigureAwait(false);
                if (!enteredPool)
                {
                    return;
                }

                _connectionHolder.Prune();
            }
            finally
            {
                if (enteredPool)
                {
                    try
                    {
                        _poolQueue.Release();
                    }
                    catch
                    {
                        // log this... it's a bug
                    }
                }
            }
        }

        private async Task EnsureMinSizeAsync(CancellationToken cancellationToken)
        {
            var minTimeout = TimeSpan.FromMilliseconds(20);

            while (CreatedCount < _settings.MinConnections)
            {
                bool enteredPool = false;
                try
                {
                    enteredPool = await _poolQueue.WaitAsync(minTimeout, cancellationToken).ConfigureAwait(false);
                    if (!enteredPool)
                    {
                        return;
                    }

                    using (var connectionCreator = new ConnectionCreator(this, minTimeout))
                    {
                        var connection = await connectionCreator.CreateOpenedAsync(cancellationToken).ConfigureAwait(false);
                        _connectionHolder.Return(connection);
                    }
                }
                finally
                {
                    if (enteredPool)
                    {
                        try
                        {
                            _poolQueue.Release();
                        }
                        catch
                        {
                            // log this... it's a bug
                        }
                    }
                }
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

            if (!connection.IsExpired && _state.Value != State.Disposed)
            {
                _connectionHolder.Return(connection);
            }
            else
            {
                _connectionHolder.RemoveConnection(connection);
            }

            if (_state.Value != State.Disposed)
            {
                _poolQueue.Release();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_state.Value == State.Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private void ThrowIfNotOpen()
        {
            if (_state.Value != State.Open)
            {
                ThrowIfDisposed();
                throw new InvalidOperationException("ConnectionPool must be initialized.");
            }
        }
    }
}
