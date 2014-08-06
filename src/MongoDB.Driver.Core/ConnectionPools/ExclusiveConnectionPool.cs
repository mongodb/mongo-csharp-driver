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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Async;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
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
            IConnectionFactory connectionFactory)
        {
            _serverId = Ensure.IsNotNull(serverId, "serverId");
            _endPoint = Ensure.IsNotNull(endPoint, "endPoint");
            _settings = Ensure.IsNotNull(settings, "settings");
            _connectionFactory = Ensure.IsNotNull(connectionFactory, "connectionFactory");

            _connectionHolder = new ListConnectionHolder();
            _poolQueue = new WaitQueue(settings.MaxConnections);
            _waitQueue = new SemaphoreSlim(settings.MaxWaitQueueSize);
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
        public async Task<IConnectionHandle> AcquireConnectionAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            ThrowIfNotOpen();

            var slidingTimeout = new SlidingTimeout(timeout);

            bool enteredWaitQueue = false;
            bool enteredPool = false;

            try
            {
                enteredWaitQueue = _waitQueue.Wait(0); // don't wait...
                if (!enteredWaitQueue)
                {
                    throw new MongoDBException("Too many waiters in the connection pool.");
                }

                var waitQueueTimeout = (int)Math.Min(slidingTimeout.ToTimeout().TotalMilliseconds, timeout.TotalMilliseconds);
                if (waitQueueTimeout == Timeout.Infinite)
                {
                    // if one of these is infinite (-1), then we don't timeout properly
                    waitQueueTimeout = (int)Math.Max(slidingTimeout.ToTimeout().TotalMilliseconds, timeout.TotalMilliseconds);
                }
                enteredPool = await _poolQueue.WaitAsync(TimeSpan.FromMilliseconds(waitQueueTimeout), cancellationToken);

                if (enteredPool)
                {
                    return AcquireConnection();
                }

                throw new TimeoutException("Timed out waiting for a connection.");
            }
            catch
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
                connection = CreateNewConnection();
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
                AsyncBackgroundTask.Start(
                    ct => MaintainSizeAsync(ct),
                    _settings.MaintenanceInterval,
                    _maintenanceCancellationTokenSource.Token)
                    .LogUnobservedExceptions();
            }
        }

        public void Dispose()
        {
            if (_state.TryChange(State.Disposed))
            {
                // TODO: dispose all connections in the pool
                _maintenanceCancellationTokenSource.Cancel();
                _maintenanceCancellationTokenSource.Dispose();
                _poolQueue.Dispose();
                _waitQueue.Dispose();
            }
        }

        private async Task<bool> MaintainSizeAsync(CancellationToken cancellationToken)
        {
            try
            {
                await PrunePoolAsync(cancellationToken);
                await EnsureMinSizeAsync(cancellationToken);
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
                enteredPool = await _poolQueue.WaitAsync(TimeSpan.FromMilliseconds(20), cancellationToken);
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
                    enteredPool = await _poolQueue.WaitAsync(TimeSpan.FromMilliseconds(20), cancellationToken);
                    if (!enteredPool)
                    {
                        return;
                    }

                    var connection = CreateNewConnection();
                    // when adding in a connection, we need to open it because 
                    // the whole point of having a min pool size is to have
                    // them available and ready...
                    await connection.OpenAsync(Timeout.InfiniteTimeSpan, cancellationToken);
                    _connectionHolder.Return(connection);
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

            _connectionHolder.Return(connection);
            _poolQueue.Release();
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

            public ListConnectionHolder()
            {
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

            public void Prune()
            {
                lock (_lock)
                {
                    for (int i = 0; i < _connections.Count; i++)
                    {
                        if (_connections[i].IsExpired)
                        {
                            _connections[i].Dispose();
                            _connections.RemoveAt(i);
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
                        if (!connection.IsExpired)
                        {
                            _connections.RemoveAt(_connections.Count - 1);
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
                    connection.Dispose();
                    return;
                }

                lock (_lock)
                {
                    _connections.Add(connection);
                }
            }

        }
    }
}
