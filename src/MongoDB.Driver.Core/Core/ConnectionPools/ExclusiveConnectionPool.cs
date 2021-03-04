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
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.ConnectionPools
{
    internal sealed class ExclusiveConnectionPool : IConnectionPool
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

            _connectingQueue = new SemaphoreSlimSignalable(MongoCoreDefaults.ConnectionPool.MaxConnecting);
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
            while (CreatedCount < _settings.MinConnections)
            {
                bool enteredPool = false;
                try
                {
                    enteredPool = await _poolQueue.WaitAsync(TimeSpan.FromMilliseconds(20), cancellationToken).ConfigureAwait(false);
                    if (!enteredPool)
                    {
                        return;
                    }

                    using (var connectionCreator = new ConnectionCreator(this))
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

        // nested classes
        private static class State
        {
            public const int Initial = 0;
            public const int Open = 1;
            public const int Disposed = 2;
        }

        private class AcquireConnectionHelper
        {
            // private fields
            private readonly ExclusiveConnectionPool _pool;
            private bool _enteredPool;
            private bool _enteredWaitQueue;
            private Stopwatch _stopwatch;

            // constructors
            public AcquireConnectionHelper(ExclusiveConnectionPool pool)
            {
                _pool = pool;
            }

            // public methods
            public void CheckingOutConnection()
            {
                var handler = _pool._checkingOutConnectionEventHandler;
                if (handler != null)
                {
                    handler(new ConnectionPoolCheckingOutConnectionEvent(_pool._serverId, EventContext.OperationId));
                }
            }

            public void EnterWaitQueue()
            {
                _enteredWaitQueue = _pool._waitQueue.Wait(0); // don't wait...
                if (!_enteredWaitQueue)
                {
                    throw MongoWaitQueueFullException.ForConnectionPool(_pool._endPoint);
                }

                _stopwatch = Stopwatch.StartNew();
            }

            public IConnectionHandle EnteredPool(bool enteredPool, CancellationToken cancellationToken)
            {
                _enteredPool = enteredPool;
                PooledConnection connection = null;

                if (enteredPool)
                {
                    using (var connectionCreator = new ConnectionCreator(_pool))
                    {
                        connection = connectionCreator.CreateOpenedOrReuse(cancellationToken);
                    }
                }

                return FinalizePoolEnterance(connection);
            }

            public async Task<IConnectionHandle> EnteredPoolAsync(bool enteredPool, CancellationToken cancellationToken)
            {
                _enteredPool = enteredPool;
                PooledConnection connection = null;

                if (enteredPool)
                {
                    using (var connectionCreator = new ConnectionCreator(_pool))
                    {
                        connection = await connectionCreator.CreateOpenedOrReuseAsync(cancellationToken).ConfigureAwait(false);
                    }
                }

                return FinalizePoolEnterance(connection);
            }

            private AcquiredConnection FinalizePoolEnterance(PooledConnection pooledConnection)
            {
                if (pooledConnection != null)
                {
                    var reference = new ReferenceCounted<PooledConnection>(pooledConnection, _pool.ReleaseConnection);
                    var connectionHandle = new AcquiredConnection(_pool, reference);

                    var checkedOutConnectionEvent = new ConnectionPoolCheckedOutConnectionEvent(connectionHandle.ConnectionId, _stopwatch.Elapsed, EventContext.OperationId);
                    _pool._checkedOutConnectionEventHandler?.Invoke(checkedOutConnectionEvent);

                    return connectionHandle;
                }
                else
                {
                    _stopwatch.Stop();

                    var message = $"Timed out waiting for a connection after {_stopwatch.ElapsedMilliseconds}ms.";
                    throw new TimeoutException(message);
                }
            }

            public void Finally()
            {
                if (_enteredWaitQueue)
                {
                    try
                    {
                        _pool._waitQueue.Release();
                    }
                    catch
                    {
                        // TODO: log this, but don't throw... it's a bug if we get here
                    }
                }
            }

            public void HandleException(Exception ex)
            {
                if (_enteredPool)
                {
                    try
                    {
                        _pool._poolQueue.Release();
                    }
                    catch
                    {
                        // TODO: log this, but don't throw... it's a bug if we get here
                    }
                }

                var handler = _pool._checkingOutConnectionFailedEventHandler;
                if (handler != null)
                {
                    ConnectionCheckOutFailedReason reason;
                    switch (ex)
                    {
                        case ObjectDisposedException _: reason = ConnectionCheckOutFailedReason.PoolClosed; break;
                        case TimeoutException _: reason = ConnectionCheckOutFailedReason.Timeout; break;
                        default: reason = ConnectionCheckOutFailedReason.ConnectionError; break;
                    }
                    handler(new ConnectionPoolCheckingOutConnectionFailedEvent(_pool._serverId, ex, EventContext.OperationId, reason));
                }
            }
        }

        private sealed class PooledConnection : IConnection
        {
            private readonly IConnection _connection;
            private readonly ExclusiveConnectionPool _connectionPool;
            private readonly int _generation;

            public PooledConnection(ExclusiveConnectionPool connectionPool, IConnection connection)
            {
                _connectionPool = connectionPool;
                _connection = connection;
                _generation = connectionPool._generation;
            }

            public ConnectionId ConnectionId
            {
                get { return _connection.ConnectionId; }
            }

            public ConnectionDescription Description
            {
                get { return _connection.Description; }
            }

            public EndPoint EndPoint
            {
                get { return _connection.EndPoint; }
            }

            public int Generation
            {
                get { return _generation; }
            }

            public bool IsExpired
            {
                get { return _generation < _connectionPool.Generation || _connection.IsExpired; }
            }

            public ConnectionSettings Settings
            {
                get { return _connection.Settings; }
            }

            public void Dispose()
            {
                _connection.Dispose();
            }

            public void Open(CancellationToken cancellationToken)
            {
                try
                {
                    _connection.Open(cancellationToken);
                }
                catch (MongoConnectionException ex)
                {
                    // TODO temporary workaround for propagating exception generation to server
                    // Will be reconsider after SDAM spec error handling adjustments
                    ex.Generation = Generation;
                    throw;
                }
            }

            public async Task OpenAsync(CancellationToken cancellationToken)
            {
                try
                {
                    await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (MongoConnectionException ex)
                {
                    // TODO temporary workaround for propagating exception generation to server
                    // Will be reconsider after SDAM spec error handling adjustments
                    ex.Generation = Generation;
                    throw;
                }
            }

            public ResponseMessage ReceiveMessage(int responseTo, IMessageEncoderSelector encoderSelector, MessageEncoderSettings messageEncoderSettings, CancellationToken cancellationToken)
            {
                return _connection.ReceiveMessage(responseTo, encoderSelector, messageEncoderSettings, cancellationToken);
            }

            public Task<ResponseMessage> ReceiveMessageAsync(int responseTo, IMessageEncoderSelector encoderSelector, MessageEncoderSettings messageEncoderSettings, CancellationToken cancellationToken)
            {
                return _connection.ReceiveMessageAsync(responseTo, encoderSelector, messageEncoderSettings, cancellationToken);
            }

            public void SendMessages(IEnumerable<RequestMessage> messages, MessageEncoderSettings messageEncoderSettings, CancellationToken cancellationToken)
            {
                _connection.SendMessages(messages, messageEncoderSettings, cancellationToken);
            }

            public Task SendMessagesAsync(IEnumerable<RequestMessage> messages, MessageEncoderSettings messageEncoderSettings, CancellationToken cancellationToken)
            {
                return _connection.SendMessagesAsync(messages, messageEncoderSettings, cancellationToken);
            }

            public void SetReadTimeout(TimeSpan timeout)
            {
                _connection.SetReadTimeout(timeout);
            }
        }

        private sealed class AcquiredConnection : IConnectionHandle
        {
            private ExclusiveConnectionPool _connectionPool;
            private bool _disposed;
            private ReferenceCounted<PooledConnection> _reference;

            public AcquiredConnection(ExclusiveConnectionPool connectionPool, ReferenceCounted<PooledConnection> reference)
            {
                _connectionPool = connectionPool;
                _reference = reference;
            }

            public ConnectionId ConnectionId
            {
                get { return _reference.Instance.ConnectionId; }
            }

            public ConnectionDescription Description
            {
                get { return _reference.Instance.Description; }
            }

            public EndPoint EndPoint
            {
                get { return _reference.Instance.EndPoint; }
            }

            public int Generation
            {
                get { return _reference.Instance.Generation; }
            }

            public bool IsExpired
            {
                get
                {
                    return _connectionPool._state.Value == State.Disposed || _reference.Instance.IsExpired;
                }
            }

            public ConnectionSettings Settings
            {
                get { return _reference.Instance.Settings; }
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _reference.DecrementReferenceCount();
                    _disposed = true;
                }
            }

            public IConnectionHandle Fork()
            {
                ThrowIfDisposed();
                _reference.IncrementReferenceCount();
                return new AcquiredConnection(_connectionPool, _reference);
            }

            public void Open(CancellationToken cancellationToken)
            {
                ThrowIfDisposed();
                _reference.Instance.Open(cancellationToken);
            }

            public Task OpenAsync(CancellationToken cancellationToken)
            {
                ThrowIfDisposed();
                return _reference.Instance.OpenAsync(cancellationToken);
            }

            public Task<ResponseMessage> ReceiveMessageAsync(int responseTo, IMessageEncoderSelector encoderSelector, MessageEncoderSettings messageEncoderSettings, CancellationToken cancellationToken)
            {
                ThrowIfDisposed();
                return _reference.Instance.ReceiveMessageAsync(responseTo, encoderSelector, messageEncoderSettings, cancellationToken);
            }

            public ResponseMessage ReceiveMessage(int responseTo, IMessageEncoderSelector encoderSelector, MessageEncoderSettings messageEncoderSettings, CancellationToken cancellationToken)
            {
                ThrowIfDisposed();
                return _reference.Instance.ReceiveMessage(responseTo, encoderSelector, messageEncoderSettings, cancellationToken);
            }

            public void SendMessages(IEnumerable<RequestMessage> messages, MessageEncoderSettings messageEncoderSettings, CancellationToken cancellationToken)
            {
                ThrowIfDisposed();
                _reference.Instance.SendMessages(messages, messageEncoderSettings, cancellationToken);
            }

            public Task SendMessagesAsync(IEnumerable<RequestMessage> messages, MessageEncoderSettings messageEncoderSettings, CancellationToken cancellationToken)
            {
                ThrowIfDisposed();
                return _reference.Instance.SendMessagesAsync(messages, messageEncoderSettings, cancellationToken);
            }

            public void SetReadTimeout(TimeSpan timeout)
            {
                ThrowIfDisposed();
                _reference.Instance.SetReadTimeout(timeout);
            }

            private void ThrowIfDisposed()
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }
            }
        }

        private sealed class WaitQueue : IDisposable
        {
            private SemaphoreSlim _semaphore;

            public WaitQueue(int count)
            {
                _semaphore = new SemaphoreSlim(count);
            }

            public int CurrentCount
            {
                get { return _semaphore.CurrentCount; }
            }

            public void Release()
            {
                _semaphore.Release();
            }

            public bool Wait(TimeSpan timeout, CancellationToken cancellationToken)
            {
                return _semaphore.Wait(timeout, cancellationToken);
            }

            public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
            {
                return _semaphore.WaitAsync(timeout, cancellationToken);
            }

            public void Dispose()
            {
                _semaphore.Dispose();
            }
        }

        private class ListConnectionHolder : IDisposable
        {
            private readonly SemaphoreSlimSignalable _semaphoreSlimSignalable;
            private readonly object _lock = new object();
            private readonly List<PooledConnection> _connections;

            private readonly Action<ConnectionPoolRemovingConnectionEvent> _removingConnectionEventHandler;
            private readonly Action<ConnectionPoolRemovedConnectionEvent> _removedConnectionEventHandler;

            public ListConnectionHolder(IEventSubscriber eventSubscriber, SemaphoreSlimSignalable semaphoreSlimSignalable)
            {
                _semaphoreSlimSignalable = semaphoreSlimSignalable;
                _connections = new List<PooledConnection>();

                eventSubscriber.TryGetEventHandler(out _removingConnectionEventHandler);
                eventSubscriber.TryGetEventHandler(out _removedConnectionEventHandler);
            }

            public int Count
            {
                get
                {
                    lock (_lock)
                    {
                        return _connections.Count;
                    }
                }
            }

            public void Clear()
            {
                lock (_lock)
                {
                    foreach (var connection in _connections)
                    {
                        RemoveConnection(connection);
                    }
                    _connections.Clear();

                    SignalOrReset();
                }
            }

            public void Prune()
            {
                lock (_lock)
                {
                    for (int i = 0; i < _connections.Count; i++)
                    {
                        if (_connections[i].IsExpired)
                        {
                            RemoveConnection(_connections[i]);
                            _connections.RemoveAt(i);
                            break;
                        }
                    }

                    SignalOrReset();
                }
            }

            public PooledConnection Acquire()
            {
                lock (_lock)
                {
                    if (_connections.Count > 0)
                    {
                        var connection = _connections[_connections.Count - 1];
                        _connections.RemoveAt(_connections.Count - 1);
                        if (connection.IsExpired)
                        {
                            RemoveConnection(connection);
                        }
                        else
                        {
                            return connection;
                        }
                    }

                    SignalOrReset();
                }
                return null;
            }

            public void Return(PooledConnection connection)
            {
                lock (_lock)
                {
                    _connections.Add(connection);
                    SignalOrReset();
                }
            }

            public void RemoveConnection(PooledConnection connection)
            {
                if (_removingConnectionEventHandler != null)
                {
                    _removingConnectionEventHandler(new ConnectionPoolRemovingConnectionEvent(connection.ConnectionId, EventContext.OperationId));
                }

                var stopwatch = Stopwatch.StartNew();
                connection.Dispose();
                stopwatch.Stop();

                if (_removedConnectionEventHandler != null)
                {
                    _removedConnectionEventHandler(new ConnectionPoolRemovedConnectionEvent(connection.ConnectionId, stopwatch.Elapsed, EventContext.OperationId));
                }
            }

            public void Dispose() => throw new NotImplementedException();

            private void SignalOrReset()
            {
                // Should be invoked under lock only
                if (_connections.Count == 0)
                {
                    // no connections are available, clear the signal flag
                    _semaphoreSlimSignalable.Reset();
                }
                else
                {
                    // signal that connections are available
                    _semaphoreSlimSignalable.Signal();
                }
            }
        }

        private sealed class ConnectionCreator : IDisposable
        {
            private readonly ExclusiveConnectionPool _pool;

            private PooledConnection _connection;
            private bool _disposeConnection;

            private SemaphoreSlimSignalable.SemaphoreWaitResult _connectingWaitStatus;

            private Stopwatch _stopwatch;

            public ConnectionCreator(ExclusiveConnectionPool pool)
            {
                _pool = pool;
                _connectingWaitStatus = SemaphoreSlimSignalable.SemaphoreWaitResult.None;
                _connection = null;
                _disposeConnection = true;
                _stopwatch = null;
            }

            public async Task<PooledConnection> CreateOpenedAsync(CancellationToken cancellationToken)
            {
                var stopwatch = Stopwatch.StartNew();
                _connectingWaitStatus = await _pool._connectingQueue.WaitAsync(_pool._settings.WaitQueueTimeout, cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();

                if (_connectingWaitStatus == SemaphoreSlimSignalable.SemaphoreWaitResult.TimedOut)
                {
                    throw new TimeoutException($"Timed out waiting for in connecting queue after {stopwatch.ElapsedMilliseconds}ms.");
                }

                var connection = await CreateOpenedInternalAsync(cancellationToken).ConfigureAwait(false);
                return connection;
            }

            public PooledConnection CreateOpenedOrReuse(CancellationToken cancellationToken)
            {
                var connection = _pool._connectionHolder.Acquire();
                var waitTimeout = _pool._settings.WaitQueueTimeout;
                var stopwatch = Stopwatch.StartNew();

                while (connection == null)
                {
                    _connectingWaitStatus = _pool._connectingQueue.WaitSignaled(waitTimeout, cancellationToken);

                    connection = _connectingWaitStatus switch
                    {
                        SemaphoreSlimSignalable.SemaphoreWaitResult.Signaled => _pool._connectionHolder.Acquire(),
                        SemaphoreSlimSignalable.SemaphoreWaitResult.Entered => CreateOpenedInternal(cancellationToken),
                        SemaphoreSlimSignalable.SemaphoreWaitResult.TimedOut => throw new TimeoutException($"Timed out waiting in connecting queue after {stopwatch.ElapsedMilliseconds}ms."),
                        _ => throw new ArgumentOutOfRangeException(nameof(_connectingWaitStatus))
                    };

                    waitTimeout = _pool._settings.WaitQueueTimeout - stopwatch.Elapsed;

                    if (connection == null && waitTimeout <= TimeSpan.Zero)
                    {
                        throw TimoutException(stopwatch);
                    }
                }

                return connection;
            }

            public async Task<PooledConnection> CreateOpenedOrReuseAsync(CancellationToken cancellationToken)
            {
                var connection = _pool._connectionHolder.Acquire();

                var waitTimeout = _pool._settings.WaitQueueTimeout;
                var stopwatch = Stopwatch.StartNew();

                while (connection == null)
                {
                    _connectingWaitStatus = await _pool._connectingQueue.WaitSignaledAsync(waitTimeout, cancellationToken).ConfigureAwait(false);

                    connection = _connectingWaitStatus switch
                    {
                        SemaphoreSlimSignalable.SemaphoreWaitResult.Signaled => _pool._connectionHolder.Acquire(),
                        SemaphoreSlimSignalable.SemaphoreWaitResult.Entered => await CreateOpenedInternalAsync(cancellationToken).ConfigureAwait(false),
                        SemaphoreSlimSignalable.SemaphoreWaitResult.TimedOut => throw TimoutException(stopwatch),
                        _ => throw new ArgumentOutOfRangeException(nameof(_connectingWaitStatus))
                    };

                    waitTimeout = _pool._settings.WaitQueueTimeout - stopwatch.Elapsed;

                    if (connection == null && waitTimeout <= TimeSpan.Zero)
                    {
                        throw TimoutException(stopwatch);
                    }
                }

                return connection;
            }

            // private methods
            private PooledConnection CreateOpenedInternal(CancellationToken cancellationToken)
            {
                StartCreating(cancellationToken);

                _connection.Open(cancellationToken);

                FinishCreating();

                return _connection;
            }

            private async Task<PooledConnection> CreateOpenedInternalAsync(CancellationToken cancellationToken)
            {
                StartCreating(cancellationToken);

                await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                FinishCreating();

                return _connection;
            }

            private void StartCreating(CancellationToken cancellationToken)
            {
                var addingConnectionEvent = new ConnectionPoolAddingConnectionEvent(_pool._serverId, EventContext.OperationId);
                _pool._addingConnectionEventHandler?.Invoke(addingConnectionEvent);

                cancellationToken.ThrowIfCancellationRequested();

                _stopwatch = Stopwatch.StartNew();
                _connection = _pool.CreateNewConnection();
            }

            private void FinishCreating()
            {
                _stopwatch.Stop();

                var connectionAddedEvent = new ConnectionPoolAddedConnectionEvent(_connection.ConnectionId, _stopwatch.Elapsed, EventContext.OperationId);
                _pool._addedConnectionEventHandler?.Invoke(connectionAddedEvent);

                // Only if reached this stage, connection should not be disposed
                _disposeConnection = false;
            }

            private Exception TimoutException(Stopwatch stopwatch) =>
                new TimeoutException($"Timed out waiting in connecting queue after {stopwatch.ElapsedMilliseconds}ms.");

            public void Dispose()
            {
                if (_connectingWaitStatus == SemaphoreSlimSignalable.SemaphoreWaitResult.Entered)
                {
                    _pool._connectingQueue.Release();
                }

                if (_disposeConnection)
                {
                    // TODO SDAM spec: topology.handle_pre_handshake_error(error) # if possible, defer error handling to SDAM
                    _connection?.Dispose();
                }
            }
        }
    }
}
