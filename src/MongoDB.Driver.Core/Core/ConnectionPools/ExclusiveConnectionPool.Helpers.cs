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
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.ConnectionPools
{
    internal sealed partial class ExclusiveConnectionPool : IConnectionPool
    {
        // nested classes
        private static class State
        {
            public const int Initial = 0;
            public const int Open = 1;
            public const int Disposed = 2;
        }

        private sealed class AcquireConnectionHelper
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
                    var timeSpentInWaitQueue = _stopwatch.Elapsed;
                    using (var connectionCreator = new ConnectionCreator(_pool, _pool._settings.WaitQueueTimeout - timeSpentInWaitQueue))
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
                    var timeSpentInWaitQueue = _stopwatch.Elapsed;
                    using (var connectionCreator = new ConnectionCreator(_pool, _pool._settings.WaitQueueTimeout - timeSpentInWaitQueue))
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
            private int _generation;
            private bool _disposed;

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

            public bool IsDisposed
            {
                get { return _disposed; }
            }

            public bool IsExpired
            {
                get { return _disposed || _generation < _connectionPool.GetGeneration(_connection.Description?.ServiceId) || _connection.IsExpired; }
            }

            public ConnectionSettings Settings
            {
                get { return _connection.Settings; }
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _connection.Dispose();
                    _disposed = true;
                }
            }

            public void Open(CancellationToken cancellationToken)
            {
                try
                {
                    _connection.Open(cancellationToken);
                    SetEffectiveGenerationIfRequired(_connection.Description);
                }
                catch (MongoConnectionException ex)
                {
                    SetEffectiveGenerationIfRequired(_connection.Description);
                    EnrichExceptionDetails(ex);
                    throw;
                }
            }

            public async Task OpenAsync(CancellationToken cancellationToken)
            {
                try
                {
                    await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                    SetEffectiveGenerationIfRequired(_connection.Description);
                }
                catch (MongoConnectionException ex)
                {
                    SetEffectiveGenerationIfRequired(_connection.Description);
                    EnrichExceptionDetails(ex);
                    throw;
                }
            }

            public ResponseMessage ReceiveMessage(int responseTo, IMessageEncoderSelector encoderSelector, MessageEncoderSettings messageEncoderSettings, CancellationToken cancellationToken)
            {
                try
                {
                    return _connection.ReceiveMessage(responseTo, encoderSelector, messageEncoderSettings, cancellationToken);
                }
                catch (MongoConnectionException ex)
                {
                    EnrichExceptionDetails(ex);
                    throw;
                }
            }

            public async Task<ResponseMessage> ReceiveMessageAsync(int responseTo, IMessageEncoderSelector encoderSelector, MessageEncoderSettings messageEncoderSettings, CancellationToken cancellationToken)
            {
                try
                {
                    return await _connection.ReceiveMessageAsync(responseTo, encoderSelector, messageEncoderSettings, cancellationToken).ConfigureAwait(false);
                }
                catch (MongoConnectionException ex)
                {
                    EnrichExceptionDetails(ex);
                    throw;
                }
            }

            public void SendMessages(IEnumerable<RequestMessage> messages, MessageEncoderSettings messageEncoderSettings, CancellationToken cancellationToken)
            {
                try
                {
                    _connection.SendMessages(messages, messageEncoderSettings, cancellationToken);
                }
                catch (MongoConnectionException ex)
                {
                    EnrichExceptionDetails(ex);
                    throw;
                }
            }

            public async Task SendMessagesAsync(IEnumerable<RequestMessage> messages, MessageEncoderSettings messageEncoderSettings, CancellationToken cancellationToken)
            {
                try
                {
                    await _connection.SendMessagesAsync(messages, messageEncoderSettings, cancellationToken).ConfigureAwait(false);
                }
                catch (MongoConnectionException ex)
                {
                    EnrichExceptionDetails(ex);
                    throw;
                }
            }

            public void SetReadTimeout(TimeSpan timeout)
            {
                _connection.SetReadTimeout(timeout);
            }

            // private methods
            private void EnrichExceptionDetails(MongoConnectionException ex)
            {
                // should be refactored in CSHARP-3720
                ex.Generation = _generation;
                ex.ServiceId = _connection?.Description?.ServiceId;
            }

            private void SetEffectiveGenerationIfRequired(ConnectionDescription description)
            {
                if (_connectionPool._serviceStates.TryGetGeneration(description?.ServiceId, out var effectiveGeneration))
                {
                    _generation = effectiveGeneration;
                }
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

        private sealed class ListConnectionHolder
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
                PooledConnection[] expiredConnections;
                lock (_lock)
                {
                    expiredConnections = _connections.Where(c => c.IsExpired).ToArray();
                }

                foreach (var connection in expiredConnections)
                {
                    lock (_lock)
                    {
                        // At this point connection is always expired and might be disposed
                        // If connection is already disposed the removal logic was already executed
                        if (connection.IsDisposed)
                        {
                            continue;
                        }

                        RemoveConnection(connection);
                        _connections.Remove(connection);
                        SignalOrReset();
                    }
                }
            }

            public PooledConnection Acquire()
            {
                PooledConnection result = null;

                lock (_lock)
                {
                    while (_connections.Count > 0 && result == null)
                    {
                        var lastIndex = _connections.Count - 1;
                        var connection = _connections[lastIndex];
                        _connections.RemoveAt(lastIndex);
                        if (connection.IsExpired)
                        {
                            RemoveConnection(connection);
                        }
                        else
                        {
                            result = connection;
                        }
                    }

                    SignalOrReset();
                }
                return result;
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
            private readonly TimeSpan _connectingTimeout;

            private PooledConnection _connection;
            private bool _disposeConnection;

            private SemaphoreSlimSignalable.SemaphoreWaitResult _connectingWaitStatus;

            private Stopwatch _stopwatch;

            public ConnectionCreator(ExclusiveConnectionPool pool, TimeSpan connectingTimeout)
            {
                _pool = pool;
                _connectingTimeout = connectingTimeout;
                _connectingWaitStatus = SemaphoreSlimSignalable.SemaphoreWaitResult.None;
                _connection = null;
                _disposeConnection = true;
                _stopwatch = null;
            }

            public async Task<PooledConnection> CreateOpenedAsync(CancellationToken cancellationToken)
            {
                var stopwatch = Stopwatch.StartNew();
                _connectingWaitStatus = await _pool._connectingQueue.WaitAsync(_connectingTimeout, cancellationToken).ConfigureAwait(false);
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
                var waitTimeout = _connectingTimeout;
                var stopwatch = Stopwatch.StartNew();

                while (connection == null)
                {
                    // Try to acquire connecting semaphore. Possible operation results:
                    // Entered: The request was successfully fulfilled, and a connection establishment can start
                    // Signaled: The request was interrupted because Connection was return to pool and can be reused
                    // Timeout: The request was timed out after WaitQueueTimeout period.
                    _connectingWaitStatus = _pool._connectingQueue.WaitSignaled(waitTimeout, cancellationToken);

                    connection = _connectingWaitStatus switch
                    {
                        SemaphoreSlimSignalable.SemaphoreWaitResult.Signaled => _pool._connectionHolder.Acquire(),
                        SemaphoreSlimSignalable.SemaphoreWaitResult.Entered => CreateOpenedInternal(cancellationToken),
                        SemaphoreSlimSignalable.SemaphoreWaitResult.TimedOut => throw new TimeoutException($"Timed out waiting in connecting queue after {stopwatch.ElapsedMilliseconds}ms."),
                        _ => throw new InvalidOperationException($"Invalid wait result {_connectingWaitStatus}")
                    };

                    waitTimeout = _connectingTimeout - stopwatch.Elapsed;

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

                var waitTimeout = _connectingTimeout;
                var stopwatch = Stopwatch.StartNew();

                while (connection == null)
                {
                    // Try to acquire connecting semaphore. Possible operation results:
                    // Entered: The request was successfully fulfilled, and a connection establishment can start
                    // Signaled: The request was interrupted because Connection was return to pool and can be reused
                    // Timeout: The request was timed out after WaitQueueTimeout period.
                    _connectingWaitStatus = await _pool._connectingQueue.WaitSignaledAsync(waitTimeout, cancellationToken).ConfigureAwait(false);

                    connection = _connectingWaitStatus switch
                    {
                        SemaphoreSlimSignalable.SemaphoreWaitResult.Signaled => _pool._connectionHolder.Acquire(),
                        SemaphoreSlimSignalable.SemaphoreWaitResult.Entered => await CreateOpenedInternalAsync(cancellationToken).ConfigureAwait(false),
                        SemaphoreSlimSignalable.SemaphoreWaitResult.TimedOut => throw TimoutException(stopwatch),
                        _ => throw new InvalidOperationException($"Invalid wait result {_connectingWaitStatus}")
                    };

                    waitTimeout = _connectingTimeout - stopwatch.Elapsed;

                    if (connection == null && waitTimeout <= TimeSpan.Zero)
                    {
                        throw TimoutException(stopwatch);
                    }
                }

                return connection;
            }

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

            // private methods
            private PooledConnection CreateOpenedInternal(CancellationToken cancellationToken)
            {
                StartCreating(cancellationToken);

                _connection.Open(cancellationToken);

                FinishCreating(_connection.Description);

                return _connection;
            }

            private async Task<PooledConnection> CreateOpenedInternalAsync(CancellationToken cancellationToken)
            {
                StartCreating(cancellationToken);

                await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                FinishCreating(_connection.Description);

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

            private void FinishCreating(ConnectionDescription description)
            {
                _stopwatch.Stop();

                var connectionAddedEvent = new ConnectionPoolAddedConnectionEvent(_connection.ConnectionId, _stopwatch.Elapsed, EventContext.OperationId);
                _pool._addedConnectionEventHandler?.Invoke(connectionAddedEvent);

                // Only if reached this stage, connection should not be disposed
                _disposeConnection = false;
                _pool._serviceStates.IncrementConnectionCount(description?.ServiceId);
            }

            private Exception TimoutException(Stopwatch stopwatch) =>
                new TimeoutException($"Timed out waiting in connecting queue after {stopwatch.ElapsedMilliseconds}ms.");
        }
    }
}
