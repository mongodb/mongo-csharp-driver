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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Async;
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
                    _listener.ConnectionPoolBeforeEnteringWaitQueue(new ConnectionPoolBeforeEnteringWaitQueueEvent(_serverId));
                }

                stopwatch.Start();
                enteredWaitQueue = _waitQueue.Wait(0); // don't wait...
                if (!enteredWaitQueue)
                {
                    throw MongoWaitQueueFullException.ForConnectionPool(_endPoint);
                }
                stopwatch.Stop();

                if (_listener != null)
                {
                    _listener.ConnectionPoolAfterEnteringWaitQueue(new ConnectionPoolAfterEnteringWaitQueueEvent(_serverId, stopwatch.Elapsed));
                    _listener.ConnectionPoolBeforeCheckingOutAConnection(new ConnectionPoolBeforeCheckingOutAConnectionEvent(_serverId));
                }

                stopwatch.Restart();
                enteredPool = await _poolQueue.WaitAsync(_settings.WaitQueueTimeout, cancellationToken).ConfigureAwait(false);

                if (enteredPool)
                {
                    var acquired = AcquireConnection();
                    stopwatch.Stop();
                    if (_listener != null)
                    {
                        _listener.ConnectionPoolAfterCheckingOutAConnection(new ConnectionPoolAfterCheckingOutAConnectionEvent(acquired.ConnectionId, stopwatch.Elapsed));
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
                        _listener.ConnectionPoolErrorEnteringWaitQueue(new ConnectionPoolErrorEnteringWaitQueueEvent(_serverId, ex));
                    }
                    else
                    {
                        _listener.ConnectionPoolErrorCheckingOutAConnection(new ConnectionPoolErrorCheckingOutAConnectionEvent(_serverId, ex));
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
                if (_listener != null)
                {
                    _listener.ConnectionPoolBeforeAddingAConnection(new ConnectionPoolBeforeAddingAConnectionEvent(_serverId));
                }
                var stopwatch = Stopwatch.StartNew();
                connection = CreateNewConnection();
                stopwatch.Stop();
                if (_listener != null)
                {
                    _listener.ConnectionPoolAfterAddingAConnection(new ConnectionPoolAfterAddingAConnectionEvent(connection.ConnectionId, stopwatch.Elapsed));
                }
            }

            var reference = new ReferenceCounted<PooledConnection>(connection, x => ReleaseConnection(x));
            return new AcquiredConnection(this, reference);
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
                    _listener.ConnectionPoolBeforeOpening(new ConnectionPoolBeforeOpeningEvent(_serverId, _settings));
                }

                MaintainSize().ConfigureAwait(false);

                if (_listener != null)
                {
                    _listener.ConnectionPoolAfterOpening(new ConnectionPoolAfterOpeningEvent(_serverId, _settings));
                }
            }
        }

        public void Dispose()
        {
            if (_state.TryChange(State.Disposed))
            {
                if (_listener != null)
                {
                    _listener.ConnectionPoolBeforeClosing(new ConnectionPoolBeforeClosingEvent(_serverId));
                }

                _connectionHolder.Clear();
                _maintenanceCancellationTokenSource.Cancel();
                _maintenanceCancellationTokenSource.Dispose();
                _poolQueue.Dispose();
                _waitQueue.Dispose();
                if (_listener != null)
                {
                    _listener.ConnectionPoolAfterClosing(new ConnectionPoolAfterClosingEvent(_serverId));
                }
            }
        }

        private async Task MaintainSize()
        {
            var maintenanceCancellationToken = _maintenanceCancellationTokenSource.Token;
            while (!maintenanceCancellationToken.IsCancellationRequested)
            {
                try
                {
                    await PrunePoolAsync(maintenanceCancellationToken).ConfigureAwait(false);
                    await EnsureMinSizeAsync(maintenanceCancellationToken).ConfigureAwait(false);
                    await Task.Delay(_settings.MaintenanceInterval, maintenanceCancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    // do nothing, this is called in the background and, quite frankly, should never
                    // result in an error
                }
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

                    if (_listener != null)
                    {
                        _listener.ConnectionPoolBeforeAddingAConnection(new ConnectionPoolBeforeAddingAConnectionEvent(_serverId));
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
                        _listener.ConnectionPoolAfterAddingAConnection(new ConnectionPoolAfterAddingAConnectionEvent(connection.ConnectionId, stopwatch.Elapsed));
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
                _listener.ConnectionPoolBeforeCheckingInAConnection(new ConnectionPoolBeforeCheckingInAConnectionEvent(connection.ConnectionId));
            }

            var stopwatch = Stopwatch.StartNew();
            _connectionHolder.Return(connection);
            _poolQueue.Release();
            stopwatch.Stop();

            if (_listener != null)
            {
                _listener.ConnectionPoolAfterCheckingInAConnection(new ConnectionPoolAfterCheckingInAConnectionEvent(connection.ConnectionId, stopwatch.Elapsed));
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

            public Task OpenAsync(CancellationToken cancellationToken)
            {
                return _connection.OpenAsync(cancellationToken);
            }

            public Task<ResponseMessage> ReceiveMessageAsync(int responseTo, IMessageEncoderSelector encoderSelector, MessageEncoderSettings messageEncoderSettings, CancellationToken cancellationToken)
            {
                return _connection.ReceiveMessageAsync(responseTo, encoderSelector, messageEncoderSettings, cancellationToken);
            }

            public Task SendMessagesAsync(IEnumerable<RequestMessage> messages, MessageEncoderSettings messageEncoderSettings, CancellationToken cancellationToken)
            {
                return _connection.SendMessagesAsync(messages, messageEncoderSettings, cancellationToken);
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

            public Task SendMessagesAsync(IEnumerable<RequestMessage> messages, MessageEncoderSettings messageEncoderSettings, CancellationToken cancellationToken)
            {
                ThrowIfDisposed();
                return _reference.Instance.SendMessagesAsync(messages, messageEncoderSettings, cancellationToken);
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
                    _listener.ConnectionPoolBeforeRemovingAConnection(new ConnectionPoolBeforeRemovingAConnectionEvent(connection.ConnectionId));
                }

                var stopwatch = Stopwatch.StartNew();
                connection.Dispose();
                stopwatch.Stop();

                if (_listener != null)
                {
                    _listener.ConnectionPoolAfterRemovingAConnection(new ConnectionPoolAfterRemovingAConnectionEvent(connection.ConnectionId, stopwatch.Elapsed));
                }
            }
        }
    }
}
