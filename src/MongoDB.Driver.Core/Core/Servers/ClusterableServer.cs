/* Copyright 2013-2015 MongoDB Inc.
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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Servers
{
    /// <summary>
    /// Represents a server in a MongoDB cluster.
    /// </summary>
    internal sealed class ClusterableServer : IClusterableServer
    {
        #region static
        // static fields
        private static readonly TimeSpan __minHeartbeatInterval = TimeSpan.FromMilliseconds(500);
        private static readonly List<Type> __invalidatingExceptions = new List<Type>
        {
            typeof(MongoNotPrimaryException),
            typeof(MongoConnectionException),
            typeof(SocketException),
            typeof(EndOfStreamException),
            typeof(IOException),
        };
        #endregion

        // fields
        private readonly ExponentiallyWeightedMovingAverage _averageRoundTripTimeCalculator = new ExponentiallyWeightedMovingAverage(0.2);
        private readonly ServerDescription _baseDescription;
        private readonly ClusterConnectionMode _clusterConnectionMode;
        private IConnectionPool _connectionPool;
        private ServerDescription _currentDescription;
        private readonly EndPoint _endPoint;
        private readonly CancellationTokenSource _heartbeatCancellationTokenSource = new CancellationTokenSource();
        private readonly IConnectionFactory _heartbeatConnectionFactory;
        private IConnection _heartbeatConnection;
        private HeartbeatDelay _heartbeatDelay;
        private readonly ServerId _serverId;
        private readonly ServerSettings _settings;
        private readonly InterlockedInt32 _state;

        private readonly Action<ServerOpeningEvent> _openingEventHandler;
        private readonly Action<ServerOpenedEvent> _openedEventHandler;
        private readonly Action<ServerClosingEvent> _closingEventHandler;
        private readonly Action<ServerClosedEvent> _closedEventHandler;
        private readonly Action<ServerHeartbeatStartedEvent> _heartbeatStartedEventHandler;
        private readonly Action<ServerHeartbeatSucceededEvent> _heartbeatSucceededEventHandler;
        private readonly Action<ServerHeartbeatFailedEvent> _heartbeatFailedEventHandler;
        private readonly Action<ServerDescriptionChangedEvent> _descriptionChangedEventHandler;

        // events
        public event EventHandler<ServerDescriptionChangedEventArgs> DescriptionChanged;

        // constructors
        public ClusterableServer(ClusterId clusterId, ClusterConnectionMode clusterConnectionMode, ServerSettings settings, EndPoint endPoint, IConnectionPoolFactory connectionPoolFactory, IConnectionFactory heartbeatConnectionFactory, IEventSubscriber eventSubscriber)
        {
            Ensure.IsNotNull(clusterId, nameof(clusterId));
            _clusterConnectionMode = clusterConnectionMode;
            _settings = Ensure.IsNotNull(settings, nameof(settings)); ;
            _endPoint = Ensure.IsNotNull(endPoint, nameof(endPoint));
            Ensure.IsNotNull(connectionPoolFactory, nameof(connectionPoolFactory));
            _heartbeatConnectionFactory = Ensure.IsNotNull(heartbeatConnectionFactory, nameof(heartbeatConnectionFactory));
            Ensure.IsNotNull(eventSubscriber, nameof(eventSubscriber));

            _serverId = new ServerId(clusterId, endPoint);
            _baseDescription = _currentDescription = new ServerDescription(_serverId, endPoint);
            _connectionPool = connectionPoolFactory.CreateConnectionPool(_serverId, endPoint);
            _state = new InterlockedInt32(State.Initial);

            eventSubscriber.TryGetEventHandler(out _openingEventHandler);
            eventSubscriber.TryGetEventHandler(out _openedEventHandler);
            eventSubscriber.TryGetEventHandler(out _closingEventHandler);
            eventSubscriber.TryGetEventHandler(out _closedEventHandler);
            eventSubscriber.TryGetEventHandler(out _heartbeatStartedEventHandler);
            eventSubscriber.TryGetEventHandler(out _heartbeatSucceededEventHandler);
            eventSubscriber.TryGetEventHandler(out _heartbeatFailedEventHandler);
            eventSubscriber.TryGetEventHandler(out _descriptionChangedEventHandler);
        }

        // properties
        public ServerDescription Description
        {
            get
            {
                return Interlocked.CompareExchange(ref _currentDescription, null, null);
            }
        }

        public EndPoint EndPoint
        {
            get { return _endPoint; }
        }

        public bool IsInitialized
        {
            get { return _state.Value != State.Initial; }
        }

        public ServerId ServerId
        {
            get { return _serverId; }
        }

        // methods
        public void Initialize()
        {
            if (_state.TryChange(State.Initial, State.Open))
            {
                if (_openingEventHandler != null)
                {
                    _openingEventHandler(new ServerOpeningEvent(_serverId, _settings));
                }

                var stopwatch = Stopwatch.StartNew();
                _connectionPool.Initialize();
                MonitorServerAsync().ConfigureAwait(false);
                stopwatch.Stop();

                if (_openedEventHandler != null)
                {
                    _openedEventHandler(new ServerOpenedEvent(_serverId, _settings, stopwatch.Elapsed));
                }
            }
        }

        public void Invalidate()
        {
            ThrowIfNotOpen();
            _connectionPool.Clear();
            OnDescriptionChanged(_baseDescription);
            RequestHeartbeat();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_state.TryChange(State.Disposed))
            {
                if (disposing)
                {
                    if (_closingEventHandler != null)
                    {
                        _closingEventHandler(new ServerClosingEvent(_serverId));
                    }

                    var stopwatch = Stopwatch.StartNew();
                    _heartbeatCancellationTokenSource.Cancel();
                    _heartbeatCancellationTokenSource.Dispose();
                    _connectionPool.Dispose();
                    if (_heartbeatConnection != null)
                    {
                        _heartbeatConnection.Dispose();
                    }
                    stopwatch.Stop();

                    if (_closedEventHandler != null)
                    {
                        _closedEventHandler(new ServerClosedEvent(_serverId, stopwatch.Elapsed));
                    }
                }
            }
        }

        public IChannelHandle GetChannel(CancellationToken cancellationToken)
        {
            ThrowIfNotOpen();

            var connection = _connectionPool.AcquireConnection(cancellationToken);
            try
            {
                // ignoring the user's cancellation token here because we don't
                // want to throw this connection away simply because the user
                // wanted to cancel their operation. It will be better for the
                // collective to complete opening the connection than the throw
                // it away.
                connection.Open(CancellationToken.None);
                return new ServerChannel(this, connection);
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        public async Task<IChannelHandle> GetChannelAsync(CancellationToken cancellationToken)
        {
            ThrowIfNotOpen();

            var connection = await _connectionPool.AcquireConnectionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // ignoring the user's cancellation token here because we don't
                // want to throw this connection away simply because the user
                // wanted to cancel their operation. It will be better for the
                // collective to complete opening the connection than the throw
                // it away.
                await connection.OpenAsync(CancellationToken.None).ConfigureAwait(false);
                return new ServerChannel(this, connection);
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        private async Task MonitorServerAsync()
        {
            var metronome = new Metronome(_settings.HeartbeatInterval);
            var heartbeatCancellationToken = _heartbeatCancellationTokenSource.Token;
            while (!heartbeatCancellationToken.IsCancellationRequested)
            {
                try
                {
                    await HeartbeatAsync(heartbeatCancellationToken).ConfigureAwait(false);
                    var newHeartbeatDelay = new HeartbeatDelay(metronome.GetNextTickDelay(), __minHeartbeatInterval);
                    var oldHeartbeatDelay = Interlocked.Exchange(ref _heartbeatDelay, newHeartbeatDelay);
                    if (oldHeartbeatDelay != null)
                    {
                        oldHeartbeatDelay.Dispose();
                    }
                    await newHeartbeatDelay.Task.ConfigureAwait(false);
                }
                catch
                {
                    // ignore these exceptions
                }
            }
        }

        private async Task<bool> HeartbeatAsync(CancellationToken cancellationToken)
        {
            const int maxRetryCount = 2;
            HeartbeatInfo heartbeatInfo = null;
            Exception heartbeatException = null;
            for (var attempt = 1; attempt <= maxRetryCount; attempt++)
            {
                try
                {
                    if (_heartbeatConnection == null)
                    {
                        _heartbeatConnection = _heartbeatConnectionFactory.CreateConnection(_serverId, _endPoint);
                        // if we are cancelling, it's because the server has
                        // been shut down and we really don't need to wait.
                        await _heartbeatConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                    }

                    heartbeatInfo = await GetHeartbeatInfoAsync(_heartbeatConnection, cancellationToken).ConfigureAwait(false);
                    heartbeatException = null;
                    break;
                }
                catch (Exception ex)
                {
                    heartbeatException = ex;
                    _heartbeatConnection.Dispose();
                    _heartbeatConnection = null;

                    if (attempt == maxRetryCount)
                    {
                        _connectionPool.Clear();
                    }
                }
            }

            ServerDescription newDescription;
            if (heartbeatInfo != null)
            {
                var averageRoundTripTime = _averageRoundTripTimeCalculator.AddSample(heartbeatInfo.RoundTripTime);
                var averageRoundTripTimeRounded = TimeSpan.FromMilliseconds(Math.Round(averageRoundTripTime.TotalMilliseconds));
                var isMasterResult = heartbeatInfo.IsMasterResult;
                var buildInfoResult = heartbeatInfo.BuildInfoResult;

                newDescription = _baseDescription.With(
                    averageRoundTripTime: averageRoundTripTimeRounded,
                    canonicalEndPoint: isMasterResult.Me,
                    electionId: isMasterResult.ElectionId,
                    maxBatchCount: isMasterResult.MaxBatchCount,
                    maxDocumentSize: isMasterResult.MaxDocumentSize,
                    maxMessageSize: isMasterResult.MaxMessageSize,
                    replicaSetConfig: isMasterResult.GetReplicaSetConfig(),
                    state: ServerState.Connected,
                    tags: isMasterResult.Tags,
                    type: isMasterResult.ServerType,
                    version: buildInfoResult.ServerVersion,
                    wireVersionRange: new Range<int>(isMasterResult.MinWireVersion, isMasterResult.MaxWireVersion));
            }
            else
            {
                newDescription = _baseDescription;
            }

            if (heartbeatException != null)
            {
                newDescription = newDescription.With(heartbeatException: heartbeatException);
            }

            OnDescriptionChanged(newDescription);

            return true;
        }

        private async Task<HeartbeatInfo> GetHeartbeatInfoAsync(IConnection connection, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_heartbeatStartedEventHandler != null)
            {
                _heartbeatStartedEventHandler(new ServerHeartbeatStartedEvent(connection.ConnectionId));
            }

            try
            {
                var isMasterCommand = new CommandWireProtocol<BsonDocument>(
                    DatabaseNamespace.Admin,
                    new BsonDocument("isMaster", 1),
                    true,
                    BsonDocumentSerializer.Instance,
                    null);

                var stopwatch = Stopwatch.StartNew();
                var isMasterResultDocument = await isMasterCommand.ExecuteAsync(connection, cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();
                var isMasterResult = new IsMasterResult(isMasterResultDocument);

                var buildInfoCommand = new CommandWireProtocol<BsonDocument>(
                    DatabaseNamespace.Admin,
                    new BsonDocument("buildInfo", 1),
                    true,
                    BsonDocumentSerializer.Instance,
                    null);

                var buildInfoResultRocument = await buildInfoCommand.ExecuteAsync(connection, cancellationToken).ConfigureAwait(false);
                var buildInfoResult = new BuildInfoResult(buildInfoResultRocument);

                if (_heartbeatSucceededEventHandler != null)
                {
                    _heartbeatSucceededEventHandler(new ServerHeartbeatSucceededEvent(connection.ConnectionId, stopwatch.Elapsed));
                }

                return new HeartbeatInfo
                {
                    RoundTripTime = stopwatch.Elapsed,
                    IsMasterResult = isMasterResult,
                    BuildInfoResult = buildInfoResult
                };
            }
            catch (Exception ex)
            {
                if (_heartbeatFailedEventHandler != null)
                {
                    _heartbeatFailedEventHandler(new ServerHeartbeatFailedEvent(connection.ConnectionId, ex));
                }
                throw;
            }
        }

        private void OnDescriptionChanged(ServerDescription newDescription)
        {
            var oldDescription = Interlocked.CompareExchange(ref _currentDescription, null, null);
            if (oldDescription.Equals(newDescription))
            {
                return;
            }
            Interlocked.Exchange(ref _currentDescription, newDescription);

            var args = new ServerDescriptionChangedEventArgs(oldDescription, newDescription);

            if (_descriptionChangedEventHandler != null)
            {
                _descriptionChangedEventHandler(new ServerDescriptionChangedEvent(oldDescription, newDescription));
            }

            var handler = DescriptionChanged;
            if (handler != null)
            {
                try { handler(this, args); }
                catch { } // ignore exceptions
            }
        }

        private void HandleChannelException(IConnection connection, Exception ex)
        {
            if (_state.Value != State.Open)
            {
                return;
            }

            var aggregateException = ex as AggregateException;
            if (aggregateException != null && aggregateException.InnerExceptions.Count == 1)
            {
                ex = aggregateException.InnerException;
            }

            // For most connection exceptions, we are going to immediately
            // invalidate the server. However, we aren't going to invalidate
            // because of OperationCanceledExceptions. We trust that the
            // implementations of connection don't leave themselves in a state
            // where they can't be used based on user cancellation.
            if (ex.GetType() == typeof(OperationCanceledException))
            {
                return;
            }

            if (__invalidatingExceptions.Contains(ex.GetType()))
            {
                Invalidate();
            }
            else
            {
                RequestHeartbeat();
            }
        }

        public void RequestHeartbeat()
        {
            ThrowIfNotOpen();
            var heartbeatDelay = Interlocked.CompareExchange(ref _heartbeatDelay, null, null);
            if (heartbeatDelay != null)
            {
                heartbeatDelay.RequestHeartbeat();
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

        private class HeartbeatInfo
        {
            public TimeSpan RoundTripTime;
            public IsMasterResult IsMasterResult;
            public BuildInfoResult BuildInfoResult;
        }

        private sealed class ServerChannel : IChannelHandle
        {
            // fields
            private readonly IConnectionHandle _connection;
            private bool _disposed;
            private readonly ClusterableServer _server;

            // constructors
            public ServerChannel(ClusterableServer server, IConnectionHandle connection)
            {
                _server = server;
                _connection = connection;
            }

            // properties
            public ConnectionDescription ConnectionDescription
            {
                get { return _connection.Description; }
            }

            // methods
            public TResult Command<TResult>(
                DatabaseNamespace databaseNamespace,
                BsonDocument command,
                IElementNameValidator commandValidator,
                bool slaveOk,
                IBsonSerializer<TResult> resultSerializer,
                MessageEncoderSettings messageEncoderSettings,
                CancellationToken cancellationToken)
            {
                return Command(databaseNamespace,
                    command,
                    commandValidator,
                    () => CommandResponseHandling.Return,
                    slaveOk,
                    resultSerializer,
                    messageEncoderSettings,
                    cancellationToken);
            }

            // methods
            public TResult Command<TResult>(
                DatabaseNamespace databaseNamespace,
                BsonDocument command,
                IElementNameValidator commandValidator,
                Func<CommandResponseHandling> responseHandling,
                bool slaveOk,
                IBsonSerializer<TResult> resultSerializer,
                MessageEncoderSettings messageEncoderSettings,
                CancellationToken cancellationToken)
            {
                slaveOk = GetEffectiveSlaveOk(slaveOk);
                var protocol = new CommandWireProtocol<TResult>(
                    databaseNamespace,
                    command,
                    commandValidator,
                    responseHandling,
                    slaveOk,
                    resultSerializer,
                    messageEncoderSettings);

                return ExecuteProtocol(protocol, cancellationToken);
            }

            public Task<TResult> CommandAsync<TResult>(
                DatabaseNamespace databaseNamespace,
                BsonDocument command,
                IElementNameValidator commandValidator,
                bool slaveOk,
                IBsonSerializer<TResult> resultSerializer,
                MessageEncoderSettings messageEncoderSettings,
                CancellationToken cancellationToken)
            {
                return CommandAsync(databaseNamespace,
                    command,
                    commandValidator,
                    () => CommandResponseHandling.Return,
                    slaveOk,
                    resultSerializer,
                    messageEncoderSettings,
                    cancellationToken);
            }

            public Task<TResult> CommandAsync<TResult>(
                DatabaseNamespace databaseNamespace,
                BsonDocument command,
                IElementNameValidator commandValidator,
                Func<CommandResponseHandling> responseHandling,
                bool slaveOk,
                IBsonSerializer<TResult> resultSerializer,
                MessageEncoderSettings messageEncoderSettings,
                CancellationToken cancellationToken)
            {
                slaveOk = GetEffectiveSlaveOk(slaveOk);
                var protocol = new CommandWireProtocol<TResult>(
                    databaseNamespace,
                    command,
                    commandValidator,
                    responseHandling,
                    slaveOk,
                    resultSerializer,
                    messageEncoderSettings);

                return ExecuteProtocolAsync(protocol, cancellationToken);
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _connection.Dispose();
                    _disposed = true;
                }
            }

            public WriteConcernResult Delete(
                CollectionNamespace collectionNamespace,
                BsonDocument query,
                bool isMulti,
                MessageEncoderSettings messageEncoderSettings,
                WriteConcern writeConcern,
                CancellationToken cancellationToken)
            {
                var protocol = new DeleteWireProtocol(
                    collectionNamespace,
                    query,
                    isMulti,
                    messageEncoderSettings,
                    writeConcern);

                return ExecuteProtocol(protocol, cancellationToken);
            }

            public Task<WriteConcernResult> DeleteAsync(
                CollectionNamespace collectionNamespace,
                BsonDocument query,
                bool isMulti,
                MessageEncoderSettings messageEncoderSettings,
                WriteConcern writeConcern,
                CancellationToken cancellationToken)
            {
                var protocol = new DeleteWireProtocol(
                    collectionNamespace,
                    query,
                    isMulti,
                    messageEncoderSettings,
                    writeConcern);

                return ExecuteProtocolAsync(protocol, cancellationToken);
            }

            public CursorBatch<TDocument> GetMore<TDocument>(
                CollectionNamespace collectionNamespace,
                BsonDocument query,
                long cursorId,
                int batchSize,
                IBsonSerializer<TDocument> serializer,
                MessageEncoderSettings messageEncoderSettings,
                CancellationToken cancellationToken)
            {
                var protocol = new GetMoreWireProtocol<TDocument>(
                    collectionNamespace,
                    query,
                    cursorId,
                    batchSize,
                    serializer,
                    messageEncoderSettings);

                return ExecuteProtocol(protocol, cancellationToken);
            }

            public Task<CursorBatch<TDocument>> GetMoreAsync<TDocument>(
              CollectionNamespace collectionNamespace,
              BsonDocument query,
              long cursorId,
              int batchSize,
              IBsonSerializer<TDocument> serializer,
              MessageEncoderSettings messageEncoderSettings,
              CancellationToken cancellationToken)
            {
                var protocol = new GetMoreWireProtocol<TDocument>(
                    collectionNamespace,
                    query,
                    cursorId,
                    batchSize,
                    serializer,
                    messageEncoderSettings);

                return ExecuteProtocolAsync(protocol, cancellationToken);
            }

            public WriteConcernResult Insert<TDocument>(
                CollectionNamespace collectionNamespace,
                WriteConcern writeConcern,
                IBsonSerializer<TDocument> serializer,
                MessageEncoderSettings messageEncoderSettings,
                BatchableSource<TDocument> documentSource,
                int? maxBatchCount,
                int? maxMessageSize,
                bool continueOnError,
                Func<bool> shouldSendGetLastError,
                CancellationToken cancellationToken)
            {
                var protocol = new InsertWireProtocol<TDocument>(
                    collectionNamespace,
                    writeConcern,
                    serializer,
                    messageEncoderSettings,
                    documentSource,
                    maxBatchCount,
                    maxMessageSize,
                    continueOnError,
                    shouldSendGetLastError);

                return ExecuteProtocol(protocol, cancellationToken);
            }

            public Task<WriteConcernResult> InsertAsync<TDocument>(
               CollectionNamespace collectionNamespace,
               WriteConcern writeConcern,
               IBsonSerializer<TDocument> serializer,
               MessageEncoderSettings messageEncoderSettings,
               BatchableSource<TDocument> documentSource,
               int? maxBatchCount,
               int? maxMessageSize,
               bool continueOnError,
               Func<bool> shouldSendGetLastError,
               CancellationToken cancellationToken)
            {
                var protocol = new InsertWireProtocol<TDocument>(
                    collectionNamespace,
                    writeConcern,
                    serializer,
                    messageEncoderSettings,
                    documentSource,
                    maxBatchCount,
                    maxMessageSize,
                    continueOnError,
                    shouldSendGetLastError);

                return ExecuteProtocolAsync(protocol, cancellationToken);
            }

            public void KillCursors(
                IEnumerable<long> cursorIds,
                MessageEncoderSettings messageEncoderSettings,
                CancellationToken cancellationToken)
            {
                var protocol = new KillCursorsWireProtocol(
                    cursorIds,
                    messageEncoderSettings);

                ExecuteProtocol(protocol, cancellationToken);
            }

            public Task KillCursorsAsync(
              IEnumerable<long> cursorIds,
              MessageEncoderSettings messageEncoderSettings,
              CancellationToken cancellationToken)
            {
                var protocol = new KillCursorsWireProtocol(
                    cursorIds,
                    messageEncoderSettings);

                return ExecuteProtocolAsync(protocol, cancellationToken);
            }

            public CursorBatch<TDocument> Query<TDocument>(
                CollectionNamespace collectionNamespace,
                BsonDocument query,
                BsonDocument fields,
                IElementNameValidator queryValidator,
                int skip,
                int batchSize,
                bool slaveOk,
                bool partialOk,
                bool noCursorTimeout,
                bool oplogReplay,
                bool tailableCursor,
                bool awaitData,
                IBsonSerializer<TDocument> serializer,
                MessageEncoderSettings messageEncoderSettings,
                CancellationToken cancellationToken)
            {
                slaveOk = GetEffectiveSlaveOk(slaveOk);
                var protocol = new QueryWireProtocol<TDocument>(
                    collectionNamespace,
                    query,
                    fields,
                    queryValidator,
                    skip,
                    batchSize,
                    slaveOk,
                    partialOk,
                    noCursorTimeout,
                    oplogReplay,
                    tailableCursor,
                    awaitData,
                    serializer,
                    messageEncoderSettings);

                return ExecuteProtocol(protocol, cancellationToken);
            }

            public Task<CursorBatch<TDocument>> QueryAsync<TDocument>(
             CollectionNamespace collectionNamespace,
             BsonDocument query,
             BsonDocument fields,
             IElementNameValidator queryValidator,
             int skip,
             int batchSize,
             bool slaveOk,
             bool partialOk,
             bool noCursorTimeout,
             bool oplogReplay,
             bool tailableCursor,
             bool awaitData,
             IBsonSerializer<TDocument> serializer,
             MessageEncoderSettings messageEncoderSettings,
             CancellationToken cancellationToken)
            {
                slaveOk = GetEffectiveSlaveOk(slaveOk);
                var protocol = new QueryWireProtocol<TDocument>(
                    collectionNamespace,
                    query,
                    fields,
                    queryValidator,
                    skip,
                    batchSize,
                    slaveOk,
                    partialOk,
                    noCursorTimeout,
                    oplogReplay,
                    tailableCursor,
                    awaitData,
                    serializer,
                    messageEncoderSettings);

                return ExecuteProtocolAsync(protocol, cancellationToken);
            }

            public WriteConcernResult Update(
                CollectionNamespace collectionNamespace,
                MessageEncoderSettings messageEncoderSettings,
                WriteConcern writeConcern,
                BsonDocument query,
                BsonDocument update,
                IElementNameValidator updateValidator,
                bool isMulti,
                bool isUpsert,
                CancellationToken cancellationToken)
            {
                var protocol = new UpdateWireProtocol(
                    collectionNamespace,
                    messageEncoderSettings,
                    writeConcern,
                    query,
                    update,
                    updateValidator,
                    isMulti,
                    isUpsert);

                return ExecuteProtocol(protocol, cancellationToken);
            }

            public Task<WriteConcernResult> UpdateAsync(
               CollectionNamespace collectionNamespace,
               MessageEncoderSettings messageEncoderSettings,
               WriteConcern writeConcern,
               BsonDocument query,
               BsonDocument update,
               IElementNameValidator updateValidator,
               bool isMulti,
               bool isUpsert,
               CancellationToken cancellationToken)
            {
                var protocol = new UpdateWireProtocol(
                    collectionNamespace,
                    messageEncoderSettings,
                    writeConcern,
                    query,
                    update,
                    updateValidator,
                    isMulti,
                    isUpsert);

                return ExecuteProtocolAsync(protocol, cancellationToken);
            }

            private void ExecuteProtocol(IWireProtocol protocol, CancellationToken cancellationToken)
            {
                try
                {
                    protocol.Execute(_connection, cancellationToken);
                }
                catch (Exception ex)
                {
                    _server.HandleChannelException(_connection, ex);
                    throw;
                }
            }

            private TResult ExecuteProtocol<TResult>(IWireProtocol<TResult> protocol, CancellationToken cancellationToken)
            {
                try
                {
                    return protocol.Execute(_connection, cancellationToken);
                }
                catch (Exception ex)
                {
                    _server.HandleChannelException(_connection, ex);
                    throw;
                }
            }

            private async Task ExecuteProtocolAsync(IWireProtocol protocol, CancellationToken cancellationToken)
            {
                try
                {
                    await protocol.ExecuteAsync(_connection, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _server.HandleChannelException(_connection, ex);
                    throw;
                }
            }

            private async Task<TResult> ExecuteProtocolAsync<TResult>(IWireProtocol<TResult> protocol, CancellationToken cancellationToken)
            {
                try
                {
                    return await protocol.ExecuteAsync(_connection, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _server.HandleChannelException(_connection, ex);
                    throw;
                }
            }

            public IChannelHandle Fork()
            {
                ThrowIfDisposed();
                return new ServerChannel(_server, _connection.Fork());
            }

            private bool GetEffectiveSlaveOk(bool slaveOk)
            {
                if (_server._clusterConnectionMode == ClusterConnectionMode.Direct && _server.Description.Type != ServerType.ShardRouter)
                {
                    return true;
                }

                return slaveOk;
            }

            private void ThrowIfDisposed()
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }
            }
        }
    }
}