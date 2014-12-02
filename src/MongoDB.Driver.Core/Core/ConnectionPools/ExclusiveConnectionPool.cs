/* Copyright 2013-2014 MongoDB Inc.
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
using MongoDB.Driver.Core.Async;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.ConnectionPools
{
    internal sealed class ExclusiveConnectionPool : IConnectionPool
    {
        // fields
        private readonly IConnectionFactory _connectionFactory;
        private readonly ListConnectionHolder _connectionHolder;
        private readonly EndPoint _endPoint;
        private int _generation;
        private readonly IConnectionPoolListener _listener;
        private readonly CancellationTokenSource _maintenanceCancellationTokenSource;
        private readonly WaitQueue _poolQueue;
        private readonly ServerId _serverId;
        private readonly ConnectionPoolSettings _settings;
        private readonly InterlockedInt32 _state;
        private readonly SemaphoreSlim _waitQueue;

        // constructors
        public ExclusiveConnectionPool(
            ServerId serverId,
            EndPoint endPoint,
            ConnectionPoolSettings settings,
            IConnectionFactory connectionFactory,
            IConnectionPoolListener listener)
        {
            _serverId = Ensure.IsNotNull(serverId, "serverId");
            _endPoint = Ensure.IsNotNull(endPoint, "endPoint");
            _settings = Ensure.IsNotNull(settings, "settings");
            _connectionFactory = Ensure.IsNotNull(connectionFactory, "connectionFactory");
            _listener = listener;

            _connectionHolder = new ListConnectionHolder(_listener);
            _poolQueue = new WaitQueue(settings.MaxConnections);
            _waitQueue = new SemaphoreSlim(settings.WaitQueueSize);
            _maintenanceCancellationTokenSource = new CancellationTokenSource();
            _state = new InterlockedInt32(State.Initial);
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
        public async Task<IConnectionHandle> AcquireConnectionAsync(CancellationToken cancellationToken)
        {
            ThrowIfNotOpen();

            bool enteredWaitQueue = false;
            bool enteredPool = false;

            var stopwatch = new Stopwatch();
            try
            {
                if (_listener != null)
                {
                    _listener.BeforeEnteringWaitQueue(new ConnectionPoolBeforeEnteringWaitQueueEvent(_serverId));
                }

                stopwatch.Start();
                enteredWaitQueue = _waitQueue.Wait(0); // don't wait...
                if (!enteredWaitQueue)
                {
                    throw MongoWaitQueueFullException.ForConnectionPool(_endPoint);
                }
                stopwatch.Stop();

                if(_listener != null)
                {
                    _listener.AfterEnteringWaitQueue(new ConnectionPoolAfterEnteringWaitQueueEvent(_serverId, stopwatch.Elapsed));
                    _listener.BeforeCheckingOutAConnection(new ConnectionPoolBeforeCheckingOutAConnectionEvent(_serverId));
                }

                stopwatch.Restart();
                enteredPool = await _poolQueue.WaitAsync(_settings.WaitQueueTimeout, cancellationToken).ConfigureAwait(false);

                if (enteredPool)
                {
                    var acquired = AcquireConnection();
                    stopwatch.Stop();
                    if (_listener != null)
                    {
                        _listener.AfterCheckingOutAConnection(new ConnectionPoolAfterCheckingOutAConnectionEvent(acquired.ConnectionId, stopwatch.Elapsed));
                    }
                    return acquired;
                }

                stopwatch.Stop();
                var message = string.Format("Timed out waiting for a connection after {0}ms.", stopwatch.ElapsedMilliseconds);
                throw new TimeoutException(message);
            }
            catch (Exception ex)
            {
                if (enteredPool)
                {
                    try
                    {
                        _poolQueue.Release();
                    }
                    catch
                    {
                        // TODO: log this, but don't throw... it's a bug if we get here
                    }
                }

                if (_listener != null)
                {
                    if (!enteredWaitQueue)
                    {
                        _listener.ErrorEnteringWaitQueue(new ConnectionPoolErrorEnteringWaitQueueEvent(_serverId, ex));
                    }
                    else
                    {
                        _listener.ErrorCheckingOutAConnection(new ConnectionPoolErrorCheckingOutAConnectionEvent(_serverId, ex));
                    }
                }
                throw;
            }
            finally
            {
                if (enteredWaitQueue)
                {
                    try
                    {
                        _waitQueue.Release();
                    }
                    catch
                    {
                        // TODO: log this, but don't throw... it's a bug if we get here
                    }
                }
            }
        }

        private IConnectionHandle AcquireConnection()
        {
            PooledConnection connection = _connectionHolder.Acquire();
            if (connection == null)
            {
                if(_listener != null)
                {
                    _listener.BeforeAddingAConnection(new ConnectionPoolBeforeAddingAConnectionEvent(_serverId));
                }
                var stopwatch = Stopwatch.StartNew();
                connection = CreateNewConnection();
                stopwatch.Stop();
                if (_listener != null)
                {
                    _listener.AfterAddingAConnection(new ConnectionPoolAfterAddingAConnectionEvent(connection.ConnectionId, stopwatch.Elapsed));
                }
            }

            return new AcquiredConnection(this, connection);
        }

        public void Clear()
        {
            ThrowIfNotOpen();
            Interlocked.Increment(ref _generation);
        }

        private PooledConnection CreateNewConnection()
        {
            var connection = _connectionFactory.CreateConnection(_serverId, _endPoint);
            return new PooledConnection(this, connection);
        }

        public void Initialize()
        {
            ThrowIfDisposed();
            if (_state.TryChange(State.Initial, State.Open))
            {
                if (_listener != null)
                {
                    _listener.BeforeOpening(new ConnectionPoolBeforeOpeningEvent(_serverId, _settings));
                }
                AsyncBackgroundTask.Start(
                    ct => MaintainSizeAsync(ct),
                    _settings.MaintenanceInterval,
                    _maintenanceCancellationTokenSource.Token)
                    .HandleUnobservedException(ex => { }); // TODO: do we need to handle any error here?

                if (_listener != null)
                {
                    _listener.AfterOpening(new ConnectionPoolAfterOpeningEvent(_serverId, _settings));
                }
            }
        }

        public void Dispose()
        {
            if (_state.TryChange(State.Disposed))
            {
                if (_listener != null)
                {
                    _listener.BeforeClosing(new ConnectionPoolBeforeClosingEvent(_serverId));
                }

                _connectionHolder.Clear();
                _maintenanceCancellationTokenSource.Cancel();
                _maintenanceCancellationTokenSource.Dispose();
                _poolQueue.Dispose();
                _waitQueue.Dispose();
                if (_listener != null)
                {
                    _listener.AfterClosing(new ConnectionPoolAfterClosingEvent(_serverId));
                }
            }
        }

        private async Task<bool> MaintainSizeAsync(CancellationToken cancellationToken)
        {
            try
            {
                await PrunePoolAsync(cancellationToken).ConfigureAwait(false);
                await EnsureMinSizeAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // do nothing, this is called in the background
            }

            return true;
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

                    if (_listener != null)
                    {
                        _listener.BeforeAddingAConnection(new ConnectionPoolBeforeAddingAConnectionEvent(_serverId));
                    }

                    var stopwatch = Stopwatch.StartNew();
                    var connection = CreateNewConnection();
                    // when adding in a connection, we need to open it because 
                    // the whole point of having a min pool size is to have
                    // them available and ready...
                    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                    _connectionHolder.Return(connection);
                    stopwatch.Stop();

                    if (_listener != null)
                    {
                        _listener.AfterAddingAConnection(new ConnectionPoolAfterAddingAConnectionEvent(connection.ConnectionId, stopwatch.Elapsed));
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
            if (_state.Value == State.Disposed)
            {
                connection.Dispose();
                return;
            }

            if (_listener != null)
            {
                _listener.BeforeCheckingInAConnection(new ConnectionPoolBeforeCheckingInAConnectionEvent(connection.ConnectionId));
            }

            var stopwatch = Stopwatch.StartNew();
            _connectionHolder.Return(connection);
            _poolQueue.Release();
            stopwatch.Stop();

            if (_listener != null)
            {
                _listener.AfterCheckingInAConnection(new ConnectionPoolAfterCheckingInAConnectionEvent(connection.ConnectionId, stopwatch.Elapsed));
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

        private class PooledConnection : ConnectionWrapper
        {
            // fields
            private readonly ExclusiveConnectionPool _connectionPool;
            private readonly int _generation;
            private int _referenceCount;

            // constructors
            public PooledConnection(ExclusiveConnectionPool connectionPool, IConnection connection)
                : base(connection)
            {
                _connectionPool = connectionPool;
                _generation = _connectionPool.Generation;
            }

            // properties
            public override bool IsExpired
            {
                get
                {
                    return base.IsExpired || _generation < _connectionPool.Generation;
                }
            }

            public int ReferenceCount
            {
                get
                {
                    return Interlocked.CompareExchange(ref _referenceCount, 0, 0);
                }
            }

            // methods
            public void DecrementReferenceCount()
            {
                Interlocked.Decrement(ref _referenceCount);
            }

            public void IncrementReferenceCount()
            {
                Interlocked.Increment(ref _referenceCount);
            }
        }

        private class AcquiredConnection : ConnectionWrapper, IConnectionHandle
        {
            private ExclusiveConnectionPool _connectionPool;
            private PooledConnection _pooledConnection;

            public AcquiredConnection(ExclusiveConnectionPool connectionPool, PooledConnection pooledConnection)
                : base(pooledConnection)
            {
                _connectionPool = connectionPool;
                _pooledConnection = pooledConnection;
                _pooledConnection.IncrementReferenceCount();
            }

            public override bool IsExpired
            {
                get
                {
                    ThrowIfDisposed();
                    return base.IsExpired || _connectionPool._state.Value == State.Disposed;
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (!Disposed)
                    {
                        _pooledConnection.DecrementReferenceCount();
                        if (_pooledConnection.ReferenceCount == 0)
                        {
                            _connectionPool.ReleaseConnection(_pooledConnection);
                        }
                    }
                    Disposed = true;
                    _pooledConnection = null;
                    _connectionPool = null;
                }
                // don't call base.Dispose here because we don't want the underlying 
                // connection to get disposed...
            }

            public IConnectionHandle Fork()
            {
                ThrowIfDisposed();
                return new AcquiredConnection(_connectionPool, _pooledConnection);
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

        private class ListConnectionHolder
        {
            private readonly object _lock = new object();
            private readonly List<PooledConnection> _connections;
            private readonly IConnectionPoolListener _listener;

            public ListConnectionHolder(IConnectionPoolListener listener)
            {
                _listener = listener;
                _connections = new List<PooledConnection>();
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
                }
                return null;
            }

            public void Return(PooledConnection connection)
            {
                if (connection.IsExpired)
                {
                    RemoveConnection(connection);
                    return;
                }

                lock (_lock)
                {
                    _connections.Add(connection);
                }
            }

            private void RemoveConnection(PooledConnection connection)
            {
                if (_listener != null)
                {
                    _listener.BeforeRemovingAConnection(new ConnectionPoolBeforeRemovingAConnectionEvent(connection.ConnectionId));
                }

                var stopwatch = Stopwatch.StartNew();
                connection.Dispose();
                stopwatch.Stop();

                if (_listener != null)
                {
                    _listener.AfterRemovingAConnection(new ConnectionPoolAfterRemovingAConnectionEvent(connection.ConnectionId, stopwatch.Elapsed));
                }
            }
        }
    }
}
