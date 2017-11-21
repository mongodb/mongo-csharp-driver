/* Copyright 2010-2017 MongoDB Inc.
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver
{
    /// <inheritdoc/>
    public class MongoClient : MongoClientBase
    {
        // private fields
        private readonly ICluster _cluster;
        private readonly IOperationExecutor _operationExecutor;
        private readonly IServerSessionPool _serverSessionPool;
        private readonly MongoClientSettings _settings;

        // constructors
        /// <summary>
        /// Initializes a new instance of the MongoClient class.
        /// </summary>
        public MongoClient()
            : this(new MongoClientSettings())
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoClient class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public MongoClient(MongoClientSettings settings)
        {
            _settings = Ensure.IsNotNull(settings, nameof(settings)).FrozenCopy();
            _cluster = ClusterRegistry.Instance.GetOrCreateCluster(_settings.ToClusterKey());
            _operationExecutor = new OperationExecutor(this);
            _serverSessionPool = new ServerSessionPool(this);
        }

        /// <summary>
        /// Initializes a new instance of the MongoClient class.
        /// </summary>
        /// <param name="url">The URL.</param>
        public MongoClient(MongoUrl url)
            : this(MongoClientSettings.FromUrl(url))
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoClient class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        public MongoClient(string connectionString)
            : this(ParseConnectionString(connectionString))
        {
        }

        internal MongoClient(IOperationExecutor operationExecutor, MongoClientSettings settings)
            : this(settings)
        {
            _operationExecutor = operationExecutor;
        }

        // public properties
        /// <summary>
        /// Gets the cluster.
        /// </summary>
        public override ICluster Cluster
        {
            get { return _cluster; }
        }

        /// <inheritdoc/>
        public sealed override MongoClientSettings Settings
        {
            get { return _settings; }
        }

        // internal properties
        internal IOperationExecutor OperationExecutor => _operationExecutor;

        // private static methods
        private static MongoClientSettings ParseConnectionString(string connectionString)
        {
            var url = new MongoUrl(connectionString);
            return MongoClientSettings.FromUrl(url);
        }

        // public methods
        /// <inheritdoc/>
        public sealed override void DropDatabase(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            UsingImplicitSession(session => DropDatabase(session, name, cancellationToken), cancellationToken);
        }

        /// <inheritdoc/>
        public sealed override void DropDatabase(IClientSessionHandle session, string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            var messageEncoderSettings = GetMessageEncoderSettings();
            var operation = new DropDatabaseOperation(new DatabaseNamespace(name), messageEncoderSettings)
            {
                WriteConcern = _settings.WriteConcern
            };
            ExecuteWriteOperation(session, operation, cancellationToken);
        }

        /// <inheritdoc/>
        public sealed override Task DropDatabaseAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UsingImplicitSessionAsync(session => DropDatabaseAsync(session, name, cancellationToken), cancellationToken);
        }

        /// <inheritdoc/>
        public sealed override Task DropDatabaseAsync(IClientSessionHandle session, string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            var messageEncoderSettings = GetMessageEncoderSettings();
            var operation = new DropDatabaseOperation(new DatabaseNamespace(name), messageEncoderSettings)
            {
                WriteConcern = _settings.WriteConcern
            };
            return ExecuteWriteOperationAsync(session, operation, cancellationToken);
        }

        /// <inheritdoc/>
        public sealed override IMongoDatabase GetDatabase(string name, MongoDatabaseSettings settings = null)
        {
            settings = settings == null ?
                new MongoDatabaseSettings() :
                settings.Clone();

            settings.ApplyDefaultValues(_settings);

            return new MongoDatabaseImpl(this, new DatabaseNamespace(name), settings, _cluster, _operationExecutor);
        }

        /// <inheritdoc/>
        public sealed override IAsyncCursor<BsonDocument> ListDatabases(CancellationToken cancellationToken = default(CancellationToken))
        {
            return UsingImplicitSession(session => ListDatabases(session, cancellationToken), cancellationToken);
        }

        /// <inheritdoc/>
        public sealed override IAsyncCursor<BsonDocument> ListDatabases(IClientSessionHandle session, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            var messageEncoderSettings = GetMessageEncoderSettings();
            var operation = new ListDatabasesOperation(messageEncoderSettings);
            return ExecuteReadOperation(session, operation, cancellationToken);
        }

        /// <inheritdoc/>
        public sealed override Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return UsingImplicitSessionAsync(session => ListDatabasesAsync(session, cancellationToken), cancellationToken);
        }

        /// <inheritdoc/>
        public sealed override Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(IClientSessionHandle session, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            var messageEncoderSettings = GetMessageEncoderSettings();
            var operation = new ListDatabasesOperation(messageEncoderSettings);
            return ExecuteReadOperationAsync(session, operation, cancellationToken);
        }

        /// <summary>
        /// Starts an implicit session.
        /// </summary>
        /// <returns>A session.</returns>
        internal IClientSessionHandle StartImplicitSession(CancellationToken cancellationToken)
        {
            var areSessionsSupported = AreSessionsSupported(cancellationToken);
            return StartImplicitSession(areSessionsSupported);
        }

        /// <summary>
        /// Starts an implicit session.
        /// </summary>
        /// <returns>A Task whose result is a session.</returns>
        internal async Task<IClientSessionHandle> StartImplicitSessionAsync(CancellationToken cancellationToken)
        {
            var areSessionsSupported = await AreSessionsSupportedAsync(cancellationToken).ConfigureAwait(false);
            return StartImplicitSession(areSessionsSupported);
        }

        /// <inheritdoc/>
        public sealed override IClientSessionHandle StartSession(ClientSessionOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var areSessionsSupported = AreSessionsSupported(cancellationToken);
            return StartSession(options, areSessionsSupported);
        }

        /// <inheritdoc/>
        public sealed override async Task<IClientSessionHandle> StartSessionAsync(ClientSessionOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var areSessionsSupported = await AreSessionsSupportedAsync(cancellationToken).ConfigureAwait(false);
            return StartSession(options, areSessionsSupported);
        }

        /// <inheritdoc/>
        public override IMongoClient WithReadConcern(ReadConcern readConcern)
        {
            Ensure.IsNotNull(readConcern, nameof(readConcern));
            var newSettings = Settings.Clone();
            newSettings.ReadConcern = readConcern;
            return new MongoClient(_operationExecutor, newSettings);
        }

        /// <inheritdoc/>
        public override IMongoClient WithReadPreference(ReadPreference readPreference)
        {
            Ensure.IsNotNull(readPreference, nameof(readPreference));
            var newSettings = Settings.Clone();
            newSettings.ReadPreference = readPreference;
            return new MongoClient(_operationExecutor, newSettings);
        }

        /// <inheritdoc/>
        public override IMongoClient WithWriteConcern(WriteConcern writeConcern)
        {
            Ensure.IsNotNull(writeConcern, nameof(writeConcern));
            var newSettings = Settings.Clone();
            newSettings.WriteConcern = writeConcern;
            return new MongoClient(_operationExecutor, newSettings);
        }

        // private methods
        private IServerSession AcquireServerSession()
        {
            return _serverSessionPool.AcquireSession();
        }

        private bool AreSessionsSupported(CancellationToken cancellationToken)
        {
            return AreSessionsSupported(_cluster.Description) ?? AreSessionsSupportedAfterServerSelection(cancellationToken);
        }

        private async Task<bool> AreSessionsSupportedAsync(CancellationToken cancellationToken)
        {
            return AreSessionsSupported(_cluster.Description) ?? await AreSessionsSupportedAfterSeverSelctionAsync(cancellationToken).ConfigureAwait(false);
        }

        private bool? AreSessionsSupported(ClusterDescription clusterDescription)
        {
            if (clusterDescription.Servers.Any(s => s.IsDataBearing))
            {
                return clusterDescription.LogicalSessionTimeout.HasValue;
            }
            else
            {
                return null;
            }
        }

        private bool AreSessionsSupportedAfterServerSelection(CancellationToken cancellationToken)
        {
            var selector = new AreSessionsSupportedServerSelector();
            var selectedServer = _cluster.SelectServer(selector, cancellationToken);
            return AreSessionsSupported(selector.ClusterDescription) ?? false;
        }

        private async Task<bool> AreSessionsSupportedAfterSeverSelctionAsync(CancellationToken cancellationToken)
        {
            var selector = new AreSessionsSupportedServerSelector();
            var selectedServer = await _cluster.SelectServerAsync(selector, cancellationToken).ConfigureAwait(false);
            return AreSessionsSupported(selector.ClusterDescription) ?? false;
        }

        private TResult ExecuteReadOperation<TResult>(IClientSessionHandle session, IReadOperation<TResult> operation, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var binding = new ReadPreferenceBinding(_cluster, _settings.ReadPreference, session.ToCoreSession()))
            {
                return _operationExecutor.ExecuteReadOperation(binding, operation, cancellationToken);
            }
        }

        private async Task<TResult> ExecuteReadOperationAsync<TResult>(IClientSessionHandle session, IReadOperation<TResult> operation, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var binding = new ReadPreferenceBinding(_cluster, _settings.ReadPreference, session.ToCoreSession()))
            {
                return await _operationExecutor.ExecuteReadOperationAsync(binding, operation, cancellationToken).ConfigureAwait(false);
            }
        }

        private TResult ExecuteWriteOperation<TResult>(IClientSessionHandle session, IWriteOperation<TResult> operation, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var binding = new WritableServerBinding(_cluster, session.ToCoreSession()))
            {
                return _operationExecutor.ExecuteWriteOperation(binding, operation, cancellationToken);
            }
        }

        private async Task<TResult> ExecuteWriteOperationAsync<TResult>(IClientSessionHandle session, IWriteOperation<TResult> operation, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var binding = new WritableServerBinding(_cluster, session.ToCoreSession()))
            {
                return await _operationExecutor.ExecuteWriteOperationAsync(binding, operation, cancellationToken).ConfigureAwait(false);
            }
        }

        private MessageEncoderSettings GetMessageEncoderSettings()
        {
            return new MessageEncoderSettings
            {
                { MessageEncoderSettingsName.GuidRepresentation, _settings.GuidRepresentation },
                { MessageEncoderSettingsName.ReadEncoding, _settings.ReadEncoding ?? Utf8Encodings.Strict },
                { MessageEncoderSettingsName.WriteEncoding, _settings.WriteEncoding ?? Utf8Encodings.Strict }
            };
        }

        private IClientSessionHandle StartImplicitSession(bool areSessionsSupported)
        {
            var options = new ClientSessionOptions();

            IServerSession serverSession;
#pragma warning disable 618
            var areMultipleUsersAuthenticated = _settings.Credentials.Count() > 1;
#pragma warning restore
            if (areSessionsSupported && !areMultipleUsersAuthenticated)
            {
                serverSession = AcquireServerSession();
            }
            else
            {
                serverSession = NoServerSession.Instance;
            }

            var session = new ClientSession(this, options, serverSession, isImplicit: true);
            return new ClientSessionHandle(session);
        }

        private IClientSessionHandle StartSession(ClientSessionOptions options, bool areSessionsSupported)
        {
            if (!areSessionsSupported)
            {
                throw new NotSupportedException("Sessions are not supported by this version of the server.");
            }
            options = options ?? new ClientSessionOptions();
            var serverSession = AcquireServerSession();
            var session = new ClientSession(this, options, serverSession, isImplicit: false);
            var handle = new ClientSessionHandle(session);
            return handle;
        }

        private void UsingImplicitSession(Action<IClientSessionHandle> func, CancellationToken cancellationToken)
        {
            using (var session = StartImplicitSession(cancellationToken))
            {
                func(session);
            }
        }

        private TResult UsingImplicitSession<TResult>(Func<IClientSessionHandle, TResult> func, CancellationToken cancellationToken)
        {
            using (var session = StartImplicitSession(cancellationToken))
            {
                return func(session);
            }
        }

        private async Task UsingImplicitSessionAsync(Func<IClientSessionHandle, Task> funcAsync, CancellationToken cancellationToken)
        {
            using (var session = await StartImplicitSessionAsync(cancellationToken).ConfigureAwait(false))
            {
                await funcAsync(session).ConfigureAwait(false);
            }
        }

        private async Task<TResult> UsingImplicitSessionAsync<TResult>(Func<IClientSessionHandle, Task<TResult>> funcAsync, CancellationToken cancellationToken)
        {
            using (var session = await StartImplicitSessionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await funcAsync(session).ConfigureAwait(false);
            }
        }

        // nested types
        private class AreSessionsSupportedServerSelector : IServerSelector
        {
            public ClusterDescription ClusterDescription;

            public IEnumerable<ServerDescription> SelectServers(ClusterDescription cluster, IEnumerable<ServerDescription> servers)
            {
                ClusterDescription = cluster;
                return servers.Where(s => s.IsDataBearing);
            }
        }
    }
}
