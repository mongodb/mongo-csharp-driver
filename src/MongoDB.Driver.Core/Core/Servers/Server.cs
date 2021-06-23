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
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Servers
{
    /// <summary>
    /// Represents a server in a MongoDB cluster.
    /// </summary>
    internal abstract class Server : IClusterableServer
    {
        // fields
        private readonly IClusterClock _clusterClock;
#pragma warning disable CS0618 // Type or member is obsolete
        private readonly ClusterConnectionMode _clusterConnectionMode;
        private readonly ConnectionModeSwitch _connectionModeSwitch;
#pragma warning restore CS0618 // Type or member is obsolete
        private readonly IConnectionPool _connectionPool;
        private readonly bool? _directConnection;
        private readonly EndPoint _endPoint;
        private readonly ServerId _serverId;
        private readonly ServerSettings _settings;
        private readonly InterlockedInt32 _state;
        private readonly ServerApi _serverApi;

        private int _outstandingOperationsCount;

        private readonly Action<ServerOpeningEvent> _openingEventHandler;
        private readonly Action<ServerOpenedEvent> _openedEventHandler;
        private readonly Action<ServerClosingEvent> _closingEventHandler;
        private readonly Action<ServerClosedEvent> _closedEventHandler;
        private readonly Action<ServerDescriptionChangedEvent> _descriptionChangedEventHandler;

        // constructors
        public Server(
            ClusterId clusterId,
            IClusterClock clusterClock,
#pragma warning disable CS0618 // Type or member is obsolete
            ClusterConnectionMode clusterConnectionMode,
            ConnectionModeSwitch connectionModeSwitch,
#pragma warning restore CS0618 // Type or member is obsolete
            bool? directConnection,
            ServerSettings settings,
            EndPoint endPoint,
            IConnectionPoolFactory connectionPoolFactory,
            IEventSubscriber eventSubscriber,
            ServerApi serverApi)
        {
            ClusterConnectionModeHelper.EnsureConnectionModeValuesAreValid(clusterConnectionMode, connectionModeSwitch, directConnection);

            _clusterClock = Ensure.IsNotNull(clusterClock, nameof(clusterClock));
            _clusterConnectionMode = clusterConnectionMode;
            _connectionModeSwitch = connectionModeSwitch;
            _directConnection = directConnection;
            _settings = Ensure.IsNotNull(settings, nameof(settings));
            _endPoint = Ensure.IsNotNull(endPoint, nameof(endPoint));
            Ensure.IsNotNull(eventSubscriber, nameof(eventSubscriber));

            _serverId = new ServerId(clusterId, endPoint);
            _connectionPool = Ensure.IsNotNull(connectionPoolFactory, nameof(connectionPoolFactory)).CreateConnectionPool(_serverId, endPoint);
            _state = new InterlockedInt32(State.Initial);
            _serverApi = serverApi;
            _outstandingOperationsCount = 0;

            eventSubscriber.TryGetEventHandler(out _openingEventHandler);
            eventSubscriber.TryGetEventHandler(out _openedEventHandler);
            eventSubscriber.TryGetEventHandler(out _closingEventHandler);
            eventSubscriber.TryGetEventHandler(out _closedEventHandler);
            eventSubscriber.TryGetEventHandler(out _descriptionChangedEventHandler);
        }

        // events
        public event EventHandler<ServerDescriptionChangedEventArgs> DescriptionChanged;

        // properties
        public IClusterClock ClusterClock => _clusterClock;
        public IConnectionPool ConnectionPool => _connectionPool;
        public abstract ServerDescription Description { get; }
        public EndPoint EndPoint => _endPoint;
        public bool IsInitialized => _state.Value != State.Initial;
        public ServerId ServerId => _serverId;

        int IClusterableServer.OutstandingOperationsCount => Interlocked.CompareExchange(ref _outstandingOperationsCount, 0, 0);

        // public methods
        public void Dispose()
        {
            if (_state.TryChange(State.Disposed))
            {
                if (_closingEventHandler != null)
                {
                    _closingEventHandler(new ServerClosingEvent(_serverId));
                }

                var stopwatch = Stopwatch.StartNew();

                Dispose(disposing: true);

                _connectionPool.Dispose();
                stopwatch.Stop();

                if (_closedEventHandler != null)
                {
                    _closedEventHandler(new ServerClosedEvent(_serverId, stopwatch.Elapsed));
                }
            }
        }

        protected abstract void Dispose(bool disposing);

        protected abstract void HandleBeforeHandshakeCompletesException(Exception ex);
        protected abstract void HandleAfterHandshakeCompletesException(IConnection connection, Exception ex);

        public IChannelHandle GetChannel(CancellationToken cancellationToken)
        {
            ThrowIfNotOpen();

            try
            {
                Interlocked.Increment(ref _outstandingOperationsCount);
                var connection = _connectionPool.AcquireConnection(cancellationToken);
                return new ServerChannel(this, connection);
            }
            catch (Exception ex)
            {
                Interlocked.Decrement(ref _outstandingOperationsCount);

                HandleBeforeHandshakeCompletesException(ex);
                throw;
            }
        }
        public async Task<IChannelHandle> GetChannelAsync(CancellationToken cancellationToken)
        {
            ThrowIfNotOpen();

            try
            {
                Interlocked.Increment(ref _outstandingOperationsCount);
                var connection = await _connectionPool.AcquireConnectionAsync(cancellationToken).ConfigureAwait(false);
                return new ServerChannel(this, connection);
            }
            catch (Exception ex)
            {
                Interlocked.Decrement(ref _outstandingOperationsCount);

                HandleBeforeHandshakeCompletesException(ex);
                throw;
            }
        }

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
                Initializing();
                stopwatch.Stop();

                if (_openedEventHandler != null)
                {
                    _openedEventHandler(new ServerOpenedEvent(_serverId, _settings, stopwatch.Elapsed));
                }
            }
        }

        public abstract void Initializing();

        [Obsolete("Use Invalidate with TopologyDescription instead.")]
        public void Invalidate(string reasonInvalidated)
        {
            Invalidate(reasonInvalidated, responseTopologyDescription: null);
        }

        public void Invalidate(string reasonInvalidated, TopologyVersion responseTopologyDescription)
        {
            Invalidate(reasonInvalidated, clearConnectionPool: true, responseTopologyDescription);
        }

        public abstract void Invalidate(string reasonInvalidated, bool clearConnectionPool, TopologyVersion responseTopologyDescription);

        public abstract void RequestHeartbeat();

        // protected methods
        protected bool IsStateChangeException(Exception ex) => ex is MongoNotPrimaryException || ex is MongoNodeIsRecoveringException;

        protected bool IsShutdownException(Exception ex) => ex is MongoNodeIsRecoveringException mongoNodeIsRecoveringException && mongoNodeIsRecoveringException.IsShutdownError;

        protected void TriggerServerDescriptionChanged(object sender, ServerDescriptionChangedEventArgs e)
        {
            var shouldServerDescriptionChangedEventBePublished = !e.OldServerDescription.SdamEquals(e.NewServerDescription);
            if (shouldServerDescriptionChangedEventBePublished && _descriptionChangedEventHandler != null)
            {
                _descriptionChangedEventHandler(new ServerDescriptionChangedEvent(e.OldServerDescription, e.NewServerDescription));
            }

            var handler = DescriptionChanged;
            if (handler != null)
            {
                try { handler(this, e); }
                catch { } // ignore exceptions
            }
        }

        protected bool ShouldClearConnectionPoolForChannelException(Exception ex, SemanticVersion serverVersion)
        {
            if (ex is MongoConnectionException mongoCommandException &&
                mongoCommandException.IsNetworkException &&
                !mongoCommandException.ContainsTimeoutException)
            {
                return true;
            }
            if (IsStateChangeException(ex))
            {
                return
                    IsShutdownException(ex) ||
                    !Feature.KeepConnectionPoolWhenNotMasterConnectionException.IsSupported(serverVersion); // i.e. serverVersion < 4.1.10
            }
            return false;
        }

        // private methods
        private void HandleChannelException(IConnection connection, Exception ex)
        {
            if (!IsOpened() || ShouldIgnoreException(ex))
            {
                return;
            }

            ex = GetEffectiveException(ex);

            HandleAfterHandshakeCompletesException(connection, ex);

            bool ShouldIgnoreException(Exception ex)
            {
                // For most connection exceptions, we are going to immediately
                // invalidate the server. However, we aren't going to invalidate
                // because of OperationCanceledExceptions. We trust that the
                // implementations of connection don't leave themselves in a state
                // where they can't be used based on user cancellation.
                return ex is OperationCanceledException;
            }

            Exception GetEffectiveException(Exception ex) =>
                ex is AggregateException aggregateException && aggregateException.InnerExceptions.Count == 1
                    ? aggregateException.InnerException
                    : ex;
        }

        private bool IsOpened() => _state.Value == State.Open;

        private void ThrowIfDisposed()
        {
            if (_state.Value == State.Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private void ThrowIfNotOpen()
        {
            if (!IsOpened())
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

        private sealed class ServerChannel : IChannelHandle
        {
            // fields
            private readonly IConnectionHandle _connection;
            private readonly Server _server;

            private readonly InterlockedInt32 _state;
            private readonly bool _decrementOperationsCount;

            // constructors
            public ServerChannel(Server server, IConnectionHandle connection, bool decrementOperationsCount = true)
            {
                _server = server;
                _connection = connection;

                _state = new InterlockedInt32(ChannelState.Initial);
                _decrementOperationsCount = decrementOperationsCount;
            }

            // properties
            public IConnectionHandle Connection => _connection;

            public ConnectionDescription ConnectionDescription
            {
                get { return _connection.Description; }
            }

            // methods
            [Obsolete("Use the newest overload instead.")]
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
                var readPreference = GetEffectiveReadPreference(slaveOk, null);
                var result = Command(
                    NoCoreSession.Instance,
                    readPreference,
                    databaseNamespace,
                    command,
                    null, // commandPayloads
                    commandValidator,
                    null, // additionalOptions
                    null, // postWriteAction
                    CommandResponseHandling.Return,
                    resultSerializer,
                    messageEncoderSettings,
                    cancellationToken);

                if (responseHandling != null && responseHandling() != CommandResponseHandling.Return)
                {
                    throw new NotSupportedException("This overload requires responseHandling to be: Return.");
                }

                return result;
            }

            [Obsolete("Use the newest overload instead.")]
            public TResult Command<TResult>(
                ICoreSession session,
                ReadPreference readPreference,
                DatabaseNamespace databaseNamespace,
                BsonDocument command,
                IElementNameValidator commandValidator,
                BsonDocument additionalOptions,
                Func<CommandResponseHandling> responseHandling,
                bool slaveOk,
                IBsonSerializer<TResult> resultSerializer,
                MessageEncoderSettings messageEncoderSettings,
                CancellationToken cancellationToken)
            {
                readPreference = GetEffectiveReadPreference(slaveOk, readPreference);
                var result = Command(
                    session,
                    readPreference,
                    databaseNamespace,
                    command,
                    null, // commandPayloads
                    commandValidator,
                    additionalOptions,
                    null, // postWriteActions
                    CommandResponseHandling.Return,
                    resultSerializer,
                    messageEncoderSettings,
                    cancellationToken);

                if (responseHandling != null && responseHandling() != CommandResponseHandling.Return)
                {
                    throw new NotSupportedException("This overload requires responseHandling to be: Return.");
                }

                return result;
            }

            public TResult Command<TResult>(
                ICoreSession session,
                ReadPreference readPreference,
                DatabaseNamespace databaseNamespace,
                BsonDocument command,
                IEnumerable<Type1CommandMessageSection> commandPayloads,
                IElementNameValidator commandValidator,
                BsonDocument additionalOptions,
                Action<IMessageEncoderPostProcessor> postWriteAction,
                CommandResponseHandling responseHandling,
                IBsonSerializer<TResult> resultSerializer,
                MessageEncoderSettings messageEncoderSettings,
                CancellationToken cancellationToken)
            {
                var protocol = new CommandWireProtocol<TResult>(
                    CreateClusterClockAdvancingCoreSession(session),
                    readPreference,
                    databaseNamespace,
                    command,
                    commandPayloads,
                    commandValidator,
                    additionalOptions,
                    postWriteAction,
                    responseHandling,
                    resultSerializer,
                    messageEncoderSettings,
                    _server._serverApi);

                return ExecuteProtocol(protocol, session, cancellationToken);
            }

            [Obsolete("Use the newest overload instead.")]
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
                var readPreference = GetEffectiveReadPreference(slaveOk, null);
                var result = CommandAsync(
                    NoCoreSession.Instance,
                    readPreference,
                    databaseNamespace,
                    command,
                    null, // commandPayloads
                    commandValidator,
                    null, // additionalOptions
                    null, // postWriteAction
                    CommandResponseHandling.Return,
                    resultSerializer,
                    messageEncoderSettings,
                    cancellationToken);

                if (responseHandling != null && responseHandling() != CommandResponseHandling.Return)
                {
                    throw new NotSupportedException("This overload requires responseHandling to be 'Return'.");
                }

                return result;
            }

            [Obsolete("Use the newest overload instead.")]
            public Task<TResult> CommandAsync<TResult>(
                ICoreSession session,
                ReadPreference readPreference,
                DatabaseNamespace databaseNamespace,
                BsonDocument command,
                IElementNameValidator commandValidator,
                BsonDocument additionalOptions,
                Func<CommandResponseHandling> responseHandling,
                bool slaveOk,
                IBsonSerializer<TResult> resultSerializer,
                MessageEncoderSettings messageEncoderSettings,
                CancellationToken cancellationToken)
            {
                readPreference = GetEffectiveReadPreference(slaveOk, readPreference);
                var result = CommandAsync(
                    session,
                    readPreference,
                    databaseNamespace,
                    command,
                    null, // commandPayloads
                    commandValidator,
                    additionalOptions,
                    null, // postWriteAction
                    CommandResponseHandling.Return,
                    resultSerializer,
                    messageEncoderSettings,
                    cancellationToken);

                if (responseHandling != null && responseHandling() != CommandResponseHandling.Return)
                {
                    throw new NotSupportedException("This overload requires responseHandling to be 'Return'.");
                }

                return result;
            }

            public Task<TResult> CommandAsync<TResult>(
                ICoreSession session,
                ReadPreference readPreference,
                DatabaseNamespace databaseNamespace,
                BsonDocument command,
                IEnumerable<Type1CommandMessageSection> commandPayloads,
                IElementNameValidator commandValidator,
                BsonDocument additionalOptions,
                Action<IMessageEncoderPostProcessor> postWriteAction,
                CommandResponseHandling responseHandling,
                IBsonSerializer<TResult> resultSerializer,
                MessageEncoderSettings messageEncoderSettings,
                CancellationToken cancellationToken)
            {
                var protocol = new CommandWireProtocol<TResult>(
                    CreateClusterClockAdvancingCoreSession(session),
                    readPreference,
                    databaseNamespace,
                    command,
                    commandPayloads,
                    commandValidator,
                    additionalOptions,
                    postWriteAction,
                    responseHandling,
                    resultSerializer,
                    messageEncoderSettings,
                    _server._serverApi);

                return ExecuteProtocolAsync(protocol, session, cancellationToken);
            }

            public void Dispose()
            {
                if (_state.TryChange(ChannelState.Initial, ChannelState.Disposed))
                {
                    if (_decrementOperationsCount)
                    {
                        Interlocked.Decrement(ref _server._outstandingOperationsCount);
                    }

                    _connection.Dispose();
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
                bool tailableCursor,
                bool awaitData,
                IBsonSerializer<TDocument> serializer,
                MessageEncoderSettings messageEncoderSettings,
                CancellationToken cancellationToken)
            {
#pragma warning disable 618
                return Query(
                    collectionNamespace,
                    query,
                    fields,
                    queryValidator,
                    skip,
                    batchSize,
                    slaveOk,
                    partialOk,
                    noCursorTimeout,
                    oplogReplay: false,
                    tailableCursor,
                    awaitData,
                    serializer,
                    messageEncoderSettings,
                    cancellationToken);
#pragma warning restore 618
            }

            [Obsolete("Use the newest overload instead.")]
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
#pragma warning disable 618
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
#pragma warning restore 618

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
                bool tailableCursor,
                bool awaitData,
                IBsonSerializer<TDocument> serializer,
                MessageEncoderSettings messageEncoderSettings,
                CancellationToken cancellationToken)
            {
#pragma warning disable 618
                return QueryAsync(
                    collectionNamespace,
                    query,
                    fields,
                    queryValidator,
                    skip,
                    batchSize,
                    slaveOk,
                    partialOk,
                    noCursorTimeout,
                    oplogReplay: false,
                    tailableCursor,
                    awaitData,
                    serializer,
                    messageEncoderSettings,
                    cancellationToken);
#pragma warning restore 618
            }

            [Obsolete("Use the newest overload instead.")]
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
#pragma warning disable 618
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
#pragma warning restore 618

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

            private ICoreSession CreateClusterClockAdvancingCoreSession(ICoreSession session)
            {
                return new ClusterClockAdvancingCoreSession(session, _server.ClusterClock);
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

            private TResult ExecuteProtocol<TResult>(IWireProtocol<TResult> protocol, ICoreSession session, CancellationToken cancellationToken)
            {
                try
                {
                    return protocol.Execute(_connection, cancellationToken);
                }
                catch (Exception ex)
                {
                    MarkSessionDirtyIfNeeded(session, ex);
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

            private async Task<TResult> ExecuteProtocolAsync<TResult>(IWireProtocol<TResult> protocol, ICoreSession session, CancellationToken cancellationToken)
            {
                try
                {
                    return await protocol.ExecuteAsync(_connection, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    MarkSessionDirtyIfNeeded(session, ex);
                    _server.HandleChannelException(_connection, ex);
                    throw;
                }
            }

            public IChannelHandle Fork()
            {
                ThrowIfDisposed();

                return new ServerChannel(_server, _connection.Fork(), false);
            }

            private ReadPreference GetEffectiveReadPreference(bool slaveOk, ReadPreference readPreference)
            {
                if (IsDirectConnection() && _server.Description.Type != ServerType.ShardRouter)
                {
                    return ReadPreference.PrimaryPreferred;
                }

                if (readPreference == null)
                {
                    return slaveOk ? ReadPreference.SecondaryPreferred : ReadPreference.Primary;
                }

                var impliedSlaveOk = readPreference.ReadPreferenceMode != ReadPreferenceMode.Primary;
                if (slaveOk != impliedSlaveOk)
                {
                    throw new ArgumentException($"slaveOk {slaveOk} is inconsistent with read preference mode: {readPreference.ReadPreferenceMode}.");
                }

                return readPreference;
            }

            private bool GetEffectiveSlaveOk(bool slaveOk)
            {
                if (IsDirectConnection() && _server.Description.Type != ServerType.ShardRouter)
                {
                    return true;
                }

                return slaveOk;
            }

            private bool IsDirectConnection()
            {
#pragma warning disable CS0618 // Type or member is obsolete
                if (_server._connectionModeSwitch == ConnectionModeSwitch.UseDirectConnection)
                {
                    return _server._directConnection.GetValueOrDefault();
                }
                else
                {
                    return _server._clusterConnectionMode == ClusterConnectionMode.Direct;
                }
#pragma warning restore CS0618 // Type or member is obsolete
            }

            private void MarkSessionDirtyIfNeeded(ICoreSession session, Exception ex)
            {
                if (ex is MongoConnectionException)
                {
                    session.MarkDirty();
                }
            }

            private void ThrowIfDisposed()
            {
                if (_state.Value == ChannelState.Disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }
            }

            // nested types
            private static class ChannelState
            {
                public const int Initial = 0;
                public const int Disposed = 1;
            }
        }
    }
}
