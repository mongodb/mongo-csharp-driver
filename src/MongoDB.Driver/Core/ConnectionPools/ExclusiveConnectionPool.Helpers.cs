/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Driver.Core.Logging;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.ConnectionPools
{
    internal sealed partial class ExclusiveConnectionPool
    {
        // private methods
        private TimeSpan CalculateRemainingTimeout(TimeSpan timeout, Stopwatch stopwatch)
        {
            if (timeout == Timeout.InfiniteTimeSpan)
            {
                return Timeout.InfiniteTimeSpan;
            }

            var elapsed = stopwatch.Elapsed;
            var remainingTimeout = timeout - elapsed;

            if (remainingTimeout < TimeSpan.Zero)
            {
                throw CreateTimeoutException(elapsed, $"Timed out waiting for a connection after {elapsed.TotalMilliseconds}ms.");
            }

            return remainingTimeout;
        }

        private Exception CreateTimeoutException(TimeSpan elapsed, string message)
        {
            var checkOutsForCursorCount = _checkOutReasonCounter.GetCheckOutsCount(CheckOutReason.Cursor);
            var checkOutsForTransactionCount = _checkOutReasonCounter.GetCheckOutsCount(CheckOutReason.Transaction);

            // only use the expanded message format when connected to a load balancer
            if (checkOutsForCursorCount != 0 || checkOutsForTransactionCount != 0)
            {
                var maxPoolSize = _settings.MaxConnections;
                var availableConnectionsCount = AvailableCount;
                var checkOutsCount = maxPoolSize - availableConnectionsCount;
                var checkOutsForOtherCount = checkOutsCount - checkOutsForCursorCount - checkOutsForTransactionCount;

                message =
                    $"Timed out after {elapsed.TotalMilliseconds}ms waiting for a connection from the connection pool. " +
                    $"maxPoolSize: {maxPoolSize}, " +
                    $"connections in use by cursors: {checkOutsForCursorCount}, " +
                    $"connections in use by transactions: {checkOutsForTransactionCount}, " +
                    $"connections in use by other operations: {checkOutsForOtherCount}.";
            }

            return new TimeoutException(message);
        }

        // nested classes
        internal enum State
        {
            Uninitialized,
            Paused,
            Ready,
            ReadyNonPausable,
            Disposed
        }

        // Not thread safe
        internal sealed class PoolState
        {
            private static readonly bool[,] __transitions;
            private State _state;
            private string _poolIdentifier;

            static PoolState()
            {
                __transitions = new bool[5, 5];
                __transitions[(int)State.Paused, (int)State.Paused] = true;
                __transitions[(int)State.Paused, (int)State.Ready] = true;
                __transitions[(int)State.Paused, (int)State.ReadyNonPausable] = true;
                __transitions[(int)State.Ready, (int)State.Ready] = true;
                __transitions[(int)State.Ready, (int)State.Paused] = true;
                __transitions[(int)State.ReadyNonPausable, (int)State.ReadyNonPausable] = true;
                __transitions[(int)State.Uninitialized, (int)State.Paused] = true;

                __transitions[(int)State.Disposed, (int)State.Disposed] = true;
                __transitions[(int)State.Paused, (int)State.Disposed] = true;
                __transitions[(int)State.Ready, (int)State.Disposed] = true;
                __transitions[(int)State.ReadyNonPausable, (int)State.Disposed] = true;
                __transitions[(int)State.Uninitialized, (int)State.Disposed] = true;
            }

            public PoolState(string poolIdentifier)
            {
                _state = State.Uninitialized;
                _poolIdentifier = poolIdentifier;
            }

            public bool IsDisposed => _state == State.Disposed;
            public State State => _state;

            // returns whether the current transition is not self-transition
            public bool TransitionState(State newState)
            {
                var previousState = _state;
                if (!__transitions[(int)_state, (int)newState])
                {
                    ThrowIfDisposed();

                    throw new InvalidOperationException($"Invalid transition {_state} to {newState}.");
                }

                _state = newState;

                return previousState != newState;
            }

            public void ThrowIfDisposed()
            {
                if (_state == State.Disposed)
                {
                    throw new ObjectDisposedException(nameof(ExclusiveConnectionPool));
                }
            }

            public void ThrowIfNotReady()
            {
                ThrowIfDisposed();

                var state = _state;
                if (state == State.Paused)
                {
                    throw MongoConnectionPoolPausedException.ForConnectionPool(_poolIdentifier);
                }
                else if (state != State.Ready && state != State.ReadyNonPausable)
                {
                    throw new InvalidOperationException($"ConnectionPool must be ready, but is in {state} state.");
                }
            }

            public void ThrowIfNotReadyNonPausable()
            {
                ThrowIfDisposed();

                var state = _state;
                if (state != State.ReadyNonPausable)
                {
                    throw new InvalidOperationException($"ConnectionPool must be ready non pausable, but is in {state} state.");
                }
            }

            public void ThrowIfNotInitialized()
            {
                if (_state == State.Uninitialized)
                {
                    throw new InvalidOperationException("ConnectionPool must be initialized.");
                }
            }

            public override string ToString() => State.ToString();
        }

        private sealed class AcquireConnectionHelper : IDisposable
        {
            // private fields
            private readonly ExclusiveConnectionPool _pool;

            private bool _enteredWaitQueue;
            private SemaphoreSlimSignalable.SemaphoreWaitResult _poolQueueWaitResult;

            // constructors
            public AcquireConnectionHelper(ExclusiveConnectionPool pool)
            {
                _pool = pool;
            }

            public IConnectionHandle AcquireConnection(OperationContext operationContext)
            {
                var stopwatch = new Stopwatch();
                try
                {
                    StartCheckingOut(stopwatch);
                    var waitQueueTimeout = operationContext.RemainingTimeoutOrDefault(_pool.Settings.WaitQueueTimeout);
                    _poolQueueWaitResult = _pool._maxConnectionsQueue.WaitSignaled(waitQueueTimeout, operationContext.CancellationToken);

                    if (_poolQueueWaitResult == SemaphoreSlimSignalable.SemaphoreWaitResult.Entered)
                    {
                        PooledConnection pooledConnection;
                        ThrowIfTimedOut(operationContext, stopwatch);

                        using (var connectionCreator = new ConnectionCreator(_pool))
                        {
                            waitQueueTimeout = _pool.CalculateRemainingTimeout(waitQueueTimeout, stopwatch);
                            pooledConnection = connectionCreator.CreateOpenedOrReuse(operationContext, waitQueueTimeout);
                        }

                        return EndCheckingOut(pooledConnection, stopwatch);
                    }

                    stopwatch.Stop();
                    throw CreateException(stopwatch.Elapsed);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    HandleException(ex, stopwatch.Elapsed);
                    throw;
                }
            }

            public async Task<IConnectionHandle> AcquireConnectionAsync(OperationContext operationContext)
            {
                var stopwatch = new Stopwatch();
                try
                {
                    StartCheckingOut(stopwatch);
                    var waitQueueTimeout = operationContext.RemainingTimeoutOrDefault(_pool.Settings.WaitQueueTimeout);
                    _poolQueueWaitResult = await _pool._maxConnectionsQueue.WaitSignaledAsync(waitQueueTimeout, operationContext.CancellationToken).ConfigureAwait(false);

                    if (_poolQueueWaitResult == SemaphoreSlimSignalable.SemaphoreWaitResult.Entered)
                    {
                        PooledConnection pooledConnection;
                        ThrowIfTimedOut(operationContext, stopwatch);

                        using (var connectionCreator = new ConnectionCreator(_pool))
                        {
                            waitQueueTimeout = _pool.CalculateRemainingTimeout(waitQueueTimeout, stopwatch);
                            pooledConnection = await connectionCreator.CreateOpenedOrReuseAsync(operationContext, waitQueueTimeout).ConfigureAwait(false);
                        }

                        return EndCheckingOut(pooledConnection, stopwatch);
                    }

                    stopwatch.Stop();
                    throw CreateException(stopwatch.Elapsed);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    HandleException(ex, stopwatch.Elapsed);
                    throw;
                }
            }

            public void Dispose()
            {
                if (_enteredWaitQueue)
                {
                    Interlocked.Increment(ref _pool._waitQueueFreeSlots);
                }

                if (_poolQueueWaitResult == SemaphoreSlimSignalable.SemaphoreWaitResult.Entered)
                {
                    try
                    {
                        _pool._maxConnectionsQueue.Release();
                    }
                    catch
                    {
                        // TODO: log this, but don't throw... it's a bug if we get here
                    }
                }
            }

            // private methods
            private void AcquireWaitQueueSlot()
            {
                // enter the wait-queue, deprecated feature
                int freeSlots;
                do
                {
                    freeSlots = _pool._waitQueueFreeSlots;

                    if (freeSlots == 0)
                    {
                        throw MongoWaitQueueFullException.ForConnectionPool(_pool._endPoint);
                    }
                }
                while (Interlocked.CompareExchange(ref _pool._waitQueueFreeSlots, freeSlots - 1, freeSlots) != freeSlots);

                _enteredWaitQueue = true;
            }

            private void ThrowIfTimedOut(OperationContext operationContext, Stopwatch stopwatch)
            {
                if (operationContext.IsTimedOut())
                {
                    stopwatch.Stop();
                    throw _pool.CreateTimeoutException(stopwatch.Elapsed, $"Timed out waiting for a connection after {stopwatch.ElapsedMilliseconds}ms.");
                }
            }

            private void StartCheckingOut(Stopwatch stopwatch)
            {
                _pool._eventLogger.LogAndPublish(new ConnectionPoolCheckingOutConnectionEvent(_pool._serverId, EventContext.OperationId));
                stopwatch.Start();

                _pool._poolState.ThrowIfNotReady();
                AcquireWaitQueueSlot();
            }

            private IConnectionHandle EndCheckingOut(PooledConnection pooledConnection, Stopwatch stopwatch)
            {
                var reference = new ReferenceCounted<PooledConnection>(pooledConnection, _pool.ReleaseConnection);
                var connectionHandle = new AcquiredConnection(_pool, reference);

                _pool._eventLogger.LogAndPublish(new ConnectionPoolCheckedOutConnectionEvent(connectionHandle.ConnectionId, stopwatch.Elapsed, EventContext.OperationId));

                // no need to release the semaphore
                _poolQueueWaitResult = SemaphoreSlimSignalable.SemaphoreWaitResult.None;

                return connectionHandle;
            }

            private Exception CreateException(TimeSpan elapsed) =>
                _poolQueueWaitResult switch
                {
                    SemaphoreSlimSignalable.SemaphoreWaitResult.Signaled =>
                        MongoConnectionPoolPausedException.ForConnectionPool(_pool._endPoint),
                    SemaphoreSlimSignalable.SemaphoreWaitResult.TimedOut =>
                        _pool.CreateTimeoutException(elapsed, $"Timed out waiting for a connection after {elapsed.TotalMilliseconds}ms."),
                    // should not be reached
                    _ => new InvalidOperationException($"Invalid {_poolQueueWaitResult}.")
                };

            private void HandleException(Exception ex, TimeSpan elapsedTime)
            {
                var reason = ex switch
                {
                    ObjectDisposedException => ConnectionCheckOutFailedReason.PoolClosed,
                    TimeoutException => ConnectionCheckOutFailedReason.Timeout,
                    _ => ConnectionCheckOutFailedReason.ConnectionError
                };

                _pool._eventLogger.LogAndPublish(new ConnectionPoolCheckingOutConnectionFailedEvent(_pool._serverId, ex, EventContext.OperationId, elapsedTime, reason));
            }
        }

        internal sealed class PooledConnection : IConnection, ICheckOutReasonTracker
        {
            private CheckOutReason? _checkOutReason;
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

            public CheckOutReason? CheckOutReason
            {
                get
                {
                    return _checkOutReason;
                }
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

            public void Open(OperationContext operationContext)
            {
                try
                {
                    _connection.Open(operationContext);
                    SetEffectiveGenerationIfRequired(_connection.Description);
                }
                catch (MongoConnectionException ex)
                {
                    SetEffectiveGenerationIfRequired(_connection.Description);
                    EnrichExceptionDetails(ex);
                    throw;
                }
            }

            public async Task OpenAsync(OperationContext operationContext)
            {
                try
                {
                    await _connection.OpenAsync(operationContext).ConfigureAwait(false);
                    SetEffectiveGenerationIfRequired(_connection.Description);
                }
                catch (MongoConnectionException ex)
                {
                    SetEffectiveGenerationIfRequired(_connection.Description);
                    EnrichExceptionDetails(ex);
                    throw;
                }
            }

            public void Reauthenticate(OperationContext operationContext) => _connection.Reauthenticate(operationContext);

            public Task ReauthenticateAsync(OperationContext operationContext) => _connection.ReauthenticateAsync(operationContext);

            public ResponseMessage ReceiveMessage(OperationContext operationContext, int responseTo, IMessageEncoderSelector encoderSelector, MessageEncoderSettings messageEncoderSettings)
            {
                try
                {
                    return _connection.ReceiveMessage(operationContext, responseTo, encoderSelector, messageEncoderSettings);
                }
                catch (MongoConnectionException ex)
                {
                    EnrichExceptionDetails(ex);
                    throw;
                }
            }

            public async Task<ResponseMessage> ReceiveMessageAsync(OperationContext operationContext, int responseTo, IMessageEncoderSelector encoderSelector, MessageEncoderSettings messageEncoderSettings)
            {
                try
                {
                    return await _connection.ReceiveMessageAsync(operationContext, responseTo, encoderSelector, messageEncoderSettings).ConfigureAwait(false);
                }
                catch (MongoConnectionException ex)
                {
                    EnrichExceptionDetails(ex);
                    throw;
                }
            }

            public void SendMessage(OperationContext operationContext, RequestMessage message, MessageEncoderSettings messageEncoderSettings)
            {
                try
                {
                    _connection.SendMessage(operationContext, message, messageEncoderSettings);
                }
                catch (MongoConnectionException ex)
                {
                    EnrichExceptionDetails(ex);
                    throw;
                }
            }

            public async Task SendMessageAsync(OperationContext operationContext, RequestMessage message, MessageEncoderSettings messageEncoderSettings)
            {
                try
                {
                    await _connection.SendMessageAsync(operationContext, message, messageEncoderSettings).ConfigureAwait(false);
                }
                catch (MongoConnectionException ex)
                {
                    EnrichExceptionDetails(ex);
                    throw;
                }
            }

            public void CompleteCommandWithException(Exception exception)
            {
                _connection.CompleteCommandWithException(exception);
            }

            public void SetCheckOutReasonIfNotAlreadySet(CheckOutReason reason)
            {
                if (_checkOutReason == null)
                {
                    _checkOutReason = reason;
                    _connectionPool._checkOutReasonCounter.Increment(reason);
                }
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

        private sealed class AcquiredConnection : IConnectionHandle, ICheckOutReasonTracker
        {
            private ExclusiveConnectionPool _connectionPool;
            private bool _disposed;
            private ReferenceCounted<PooledConnection> _reference;

            public AcquiredConnection(ExclusiveConnectionPool connectionPool, ReferenceCounted<PooledConnection> reference)
            {
                _connectionPool = connectionPool;
                _reference = reference;
            }

            public CheckOutReason? CheckOutReason
            {
                get
                {
                    return _reference.Instance.CheckOutReason;
                }
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
                    return _connectionPool._poolState.IsDisposed || _reference.Instance.IsExpired;
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

            public void Open(OperationContext operationContext)
            {
                ThrowIfDisposed();
                _reference.Instance.Open(operationContext);
            }

            public Task OpenAsync(OperationContext operationContext)
            {
                ThrowIfDisposed();
                return _reference.Instance.OpenAsync(operationContext);
            }

            public void Reauthenticate(OperationContext operationContext)
            {
                ThrowIfDisposed();
                _reference.Instance.Reauthenticate(operationContext);
            }

            public Task ReauthenticateAsync(OperationContext operationContext)
            {
                ThrowIfDisposed();
                return _reference.Instance.ReauthenticateAsync(operationContext);
            }

            public Task<ResponseMessage> ReceiveMessageAsync(OperationContext operationContext, int responseTo, IMessageEncoderSelector encoderSelector, MessageEncoderSettings messageEncoderSettings)
            {
                ThrowIfDisposed();
                return _reference.Instance.ReceiveMessageAsync(operationContext, responseTo, encoderSelector, messageEncoderSettings);
            }

            public ResponseMessage ReceiveMessage(OperationContext operationContext, int responseTo, IMessageEncoderSelector encoderSelector, MessageEncoderSettings messageEncoderSettings)
            {
                ThrowIfDisposed();
                return _reference.Instance.ReceiveMessage(operationContext, responseTo, encoderSelector, messageEncoderSettings);
            }

            public void SendMessage(OperationContext operationContext, RequestMessage message, MessageEncoderSettings messageEncoderSettings)
            {
                ThrowIfDisposed();
                _reference.Instance.SendMessage(operationContext, message, messageEncoderSettings);
            }

            public Task SendMessageAsync(OperationContext operationContext, RequestMessage message, MessageEncoderSettings messageEncoderSettings)
            {
                ThrowIfDisposed();
                return _reference.Instance.SendMessageAsync(operationContext, message, messageEncoderSettings);
            }

            public void CompleteCommandWithException(Exception exception)
            {
                ThrowIfDisposed();
                _reference.Instance.CompleteCommandWithException(exception);
            }

            public void SetCheckOutReasonIfNotAlreadySet(CheckOutReason reason)
            {
                ThrowIfDisposed();
                _reference.Instance.SetCheckOutReasonIfNotAlreadySet(reason);
            }

            private void ThrowIfDisposed()
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }
            }
        }

        internal sealed class ListConnectionHolder
        {
            private readonly SemaphoreSlimSignalable _semaphoreSlimSignalable;
            private readonly object _lock = new object();
            private readonly object _lockInUse = new object();
            private readonly List<PooledConnection> _connections;
            private readonly List<PooledConnection> _connectionsInUse;
            private readonly EventLogger<LogCategories.Connection> _eventLogger;

            public ListConnectionHolder(EventLogger<LogCategories.Connection> eventLogger, SemaphoreSlimSignalable semaphoreSlimSignalable)
            {
                _semaphoreSlimSignalable = semaphoreSlimSignalable;
                _connections = new List<PooledConnection>();
                _connectionsInUse = new List<PooledConnection>();
                _eventLogger = eventLogger;
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
                    // In use Connections MUST be closed when they are checked in to the closed pool.
                    foreach (var connection in _connections)
                    {
                        RemoveConnection(connection);
                    }
                    _connections.Clear();

                    SignalOrReset();
                }
            }

            public void Prune(int? maxExpiredGenerationInUse, CancellationToken cancellationToken)
            {
                RemoveExpiredConnections(_connections, generation: null, _lock, signal: true);

                if (maxExpiredGenerationInUse.HasValue)
                {
                    RemoveExpiredConnections(_connectionsInUse, generation: maxExpiredGenerationInUse.Value, _lockInUse, signal: false);
                }

                void RemoveExpiredConnections(List<PooledConnection> connections, int? generation, object @lock, bool signal)
                {
                    PooledConnection[] expiredConnections;
                    lock (@lock)
                    {
                        expiredConnections = connections.Where(c => c.IsExpired && (generation == null || c.Generation <= generation)).ToArray();
                    }

                    foreach (var connection in expiredConnections)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        lock (@lock)
                        {
                            // At this point connection is always expired and might be disposed
                            // If connection is already disposed the removal logic was already executed
                            if (connection.IsDisposed)
                            {
                                continue;
                            }

                            RemoveConnection(connection);
                            connections.Remove(connection);

                            if (signal)
                            {
                                SignalOrReset();
                            }
                        }
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

                if (result != null)
                {
                    TrackInUseConnection(result);

                    // This connection can be expired and not disposed by Prune. Dispose if needed
                    if (result.IsExpired)
                    {
                        RemoveConnection(result);
                        result = null;
                    }
                }

                return result;
            }

            public void Return(PooledConnection connection)
            {
                UntrackInUseConnection(connection);

                lock (_lock)
                {
                    _connections.Add(connection);
                    SignalOrReset();
                }
            }

            public void RemoveConnection(PooledConnection connection)
            {
                _eventLogger.LogAndPublish(new ConnectionPoolRemovingConnectionEvent(connection.ConnectionId, EventContext.OperationId));

                var stopwatch = Stopwatch.StartNew();
                UntrackInUseConnection(connection); // no op if connection is not in use
                connection.Dispose();
                stopwatch.Stop();

                _eventLogger.LogAndPublish(new ConnectionPoolRemovedConnectionEvent(connection.ConnectionId, stopwatch.Elapsed, EventContext.OperationId));
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

            public void TrackInUseConnection(PooledConnection connection)
            {
                lock (_lockInUse)
                {
                    _connectionsInUse.Add(connection);
                }
            }

            public void UntrackInUseConnection(PooledConnection connection)
            {
                lock (_lockInUse)
                {
                    _connectionsInUse.Remove(connection);
                }
            }
        }

        internal sealed class ConnectionCreator : IDisposable
        {
            private readonly ExclusiveConnectionPool _pool;

            private PooledConnection _connection;
            private bool _disposeConnection;

            private SemaphoreSlimSignalable.SemaphoreWaitResult _connectingWaitStatus;

            public ConnectionCreator(ExclusiveConnectionPool pool)
            {
                _pool = pool;
                _connectingWaitStatus = SemaphoreSlimSignalable.SemaphoreWaitResult.None;
                _connection = null;
                _disposeConnection = true;
            }

            public PooledConnection CreateOpened(TimeSpan maxConnectingQueueTimeout, CancellationToken cancellationToken)
            {
                try
                {
                    var stopwatch = Stopwatch.StartNew();
                    _connectingWaitStatus = _pool._maxConnectingQueue.Wait(maxConnectingQueueTimeout, cancellationToken);
                    stopwatch.Stop();
                    _pool._poolState.ThrowIfNotReady();

                    if (_connectingWaitStatus == SemaphoreSlimSignalable.SemaphoreWaitResult.TimedOut)
                    {
                        throw _pool.CreateTimeoutException(stopwatch.Elapsed, $"Timed out waiting for in connecting queue after {stopwatch.ElapsedMilliseconds}ms.");
                    }

                    return CreateOpenedInternal(new(Timeout.InfiniteTimeSpan, cancellationToken));
                }
                catch (Exception ex)
                {
                    _pool._connectionExceptionHandler.HandleExceptionOnOpen(ex);
                    throw;
                }
            }

            public PooledConnection CreateOpenedOrReuse(OperationContext operationContext, TimeSpan waitQueueTimeout)
            {
                try
                {
                    var connection = _pool._connectionHolder.Acquire();
                    var stopwatch = Stopwatch.StartNew();

                    while (connection == null)
                    {
                        _pool._poolState.ThrowIfNotReady();
                        var waitTimeout = _pool.CalculateRemainingTimeout(waitQueueTimeout, stopwatch);

                        // Try to acquire connecting semaphore. Possible operation results:
                        // Entered: The request was successfully fulfilled, and a connection establishment can start
                        // Signaled: The request was interrupted because Connection was return to pool and can be reused
                        // Timeout: The request was timed out after WaitQueueTimeout period.
                        _connectingWaitStatus = _pool._maxConnectingQueue.WaitSignaled(waitTimeout, operationContext.CancellationToken);

                        connection = _connectingWaitStatus switch
                        {
                            SemaphoreSlimSignalable.SemaphoreWaitResult.Signaled => _pool._connectionHolder.Acquire(),
                            SemaphoreSlimSignalable.SemaphoreWaitResult.Entered => CreateOpenedInternal(operationContext),
                            SemaphoreSlimSignalable.SemaphoreWaitResult.TimedOut => throw CreateTimeoutException(stopwatch.Elapsed),
                            _ => throw new InvalidOperationException($"Invalid wait result {_connectingWaitStatus}")
                        };
                    }

                    return connection;
                }
                catch (Exception ex)
                {
                    _pool._connectionExceptionHandler.HandleExceptionOnOpen(ex);
                    throw;
                }
            }

            public async Task<PooledConnection> CreateOpenedOrReuseAsync(OperationContext operationContext, TimeSpan waitQueueTimeout)
            {
                try
                {
                    var connection = _pool._connectionHolder.Acquire();
                    var stopwatch = Stopwatch.StartNew();

                    while (connection == null)
                    {
                        _pool._poolState.ThrowIfNotReady();

                        var waitTimeout = _pool.CalculateRemainingTimeout(waitQueueTimeout, stopwatch);
                        // Try to acquire connecting semaphore. Possible operation results:
                        // Entered: The request was successfully fulfilled, and a connection establishment can start
                        // Signaled: The request was interrupted because Connection was return to pool and can be reused
                        // Timeout: The request was timed out after WaitQueueTimeout period.
                        _connectingWaitStatus = await _pool._maxConnectingQueue.WaitSignaledAsync(waitTimeout, operationContext.CancellationToken).ConfigureAwait(false);

                        connection = _connectingWaitStatus switch
                        {
                            SemaphoreSlimSignalable.SemaphoreWaitResult.Signaled => _pool._connectionHolder.Acquire(),
                            SemaphoreSlimSignalable.SemaphoreWaitResult.Entered => await CreateOpenedInternalAsync(operationContext).ConfigureAwait(false),
                            SemaphoreSlimSignalable.SemaphoreWaitResult.TimedOut => throw CreateTimeoutException(stopwatch.Elapsed),
                            _ => throw new InvalidOperationException($"Invalid wait result {_connectingWaitStatus}")
                        };
                    }

                    return connection;
                }
                catch (Exception ex)
                {
                    _pool._connectionExceptionHandler.HandleExceptionOnOpen(ex);
                    throw;
                }
            }

            public void Dispose()
            {
                if (_connectingWaitStatus == SemaphoreSlimSignalable.SemaphoreWaitResult.Entered)
                {
                    _pool._maxConnectingQueue.Release();
                }

                if (_disposeConnection && _connection != null)
                {
                    _pool.ConnectionHolder.UntrackInUseConnection(_connection);
                    _connection.Dispose();
                }
            }

            // private methods
            private PooledConnection CreateOpenedInternal(OperationContext operationContext)
            {
                var stopwatch = StartCreating(operationContext);

                _connection.Open(operationContext);

                FinishCreating(_connection.Description, stopwatch);

                return _connection;
            }

            private async Task<PooledConnection> CreateOpenedInternalAsync(OperationContext operationContext)
            {
                var stopwatch = StartCreating(operationContext);

                await _connection.OpenAsync(operationContext).ConfigureAwait(false);

                FinishCreating(_connection.Description, stopwatch);

                return _connection;
            }

            private Stopwatch StartCreating(OperationContext operationContext)
            {
                _pool._eventLogger.LogAndPublish(new ConnectionPoolAddingConnectionEvent(_pool._serverId, EventContext.OperationId));

                operationContext.ThrowIfTimedOutOrCanceled();

                var stopwatch = Stopwatch.StartNew();
                _connection = _pool.CreateNewConnection();
                return stopwatch;
            }

            private void FinishCreating(ConnectionDescription description, Stopwatch stopwatch)
            {
                stopwatch.Stop();
                _pool._eventLogger.LogAndPublish(new ConnectionPoolAddedConnectionEvent(_connection.ConnectionId, stopwatch.Elapsed, EventContext.OperationId));

                // Only if reached this stage, connection should not be disposed
                _disposeConnection = false;
                _pool._serviceStates.IncrementConnectionCount(description?.ServiceId);
            }

            private Exception CreateTimeoutException(TimeSpan elapsed)
            {
                var message = $"Timed out waiting in connecting queue after {elapsed.TotalMilliseconds}ms.";
                return _pool.CreateTimeoutException(elapsed, message);
            }
        }
    }
}
