/* Copyright 2017-present MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver.TestHelpers
{
    public class DisposableMongoClient : IMongoClient, IDisposable
    {
        private readonly IMongoClient wrapped;

        public DisposableMongoClient(IMongoClient wrapped)
        {
            this.wrapped = wrapped;
        }

        public ICluster Cluster => wrapped.Cluster;

        public MongoClientSettings Settings => wrapped.Settings;

        public IMongoClient Wrapped => wrapped;

        public void DropDatabase(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            wrapped.DropDatabase(name, cancellationToken);
        }

        public void DropDatabase(IClientSessionHandle session, string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            wrapped.DropDatabase(session, name, cancellationToken);
        }

        public Task DropDatabaseAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.DropDatabaseAsync(name, cancellationToken);
        }

        public Task DropDatabaseAsync(IClientSessionHandle session, string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.DropDatabaseAsync(session, name, cancellationToken);
        }

        public IMongoDatabase GetDatabase(string name, MongoDatabaseSettings settings = null)
        {
            return wrapped.GetDatabase(name, settings);
        }

        public IAsyncCursor<string> ListDatabaseNames(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.ListDatabaseNames(cancellationToken);
        }

        public IAsyncCursor<string> ListDatabaseNames(
            IClientSessionHandle session,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.ListDatabaseNames(session, cancellationToken);
        }

        public Task<IAsyncCursor<string>> ListDatabaseNamesAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.ListDatabaseNamesAsync(cancellationToken);
        }

        public Task<IAsyncCursor<string>> ListDatabaseNamesAsync(
            IClientSessionHandle session,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.ListDatabaseNamesAsync(session, cancellationToken);
        }

        public IAsyncCursor<BsonDocument> ListDatabases(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.ListDatabases(cancellationToken);
        }

        public IAsyncCursor<BsonDocument> ListDatabases(
            ListDatabasesOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.ListDatabases(options, cancellationToken);
        }

        public IAsyncCursor<BsonDocument> ListDatabases(
            IClientSessionHandle session,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.ListDatabases(session, cancellationToken);
        }

        public IAsyncCursor<BsonDocument> ListDatabases(
            IClientSessionHandle session,
            ListDatabasesOptions options,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.ListDatabases(session, options, cancellationToken);
        }
        

        public Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.ListDatabasesAsync(cancellationToken);
        }

        public Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(
            ListDatabasesOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.ListDatabasesAsync(options, cancellationToken);
        }

        public Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(IClientSessionHandle session, CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.ListDatabasesAsync(session, cancellationToken);
        }

        public Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(
            IClientSessionHandle session,
            ListDatabasesOptions options,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.ListDatabasesAsync(session, options, cancellationToken);
        }

        public IClientSessionHandle StartSession(ClientSessionOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.StartSession(options, cancellationToken);
        }

        public Task<IClientSessionHandle> StartSessionAsync(ClientSessionOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.StartSessionAsync(options, cancellationToken);
        }

        /// <inheritdoc />
        public virtual IChangeStreamCursor<TResult> Watch<TResult>(
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.Watch(pipeline, options, cancellationToken);
        }

        /// <inheritdoc />
        public virtual IChangeStreamCursor<TResult> Watch<TResult>(
            IClientSessionHandle session,
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.Watch(session, pipeline, options, cancellationToken);
        }

        /// <inheritdoc />
        public virtual Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.WatchAsync(pipeline, options, cancellationToken);
        }

        /// <inheritdoc />
        public virtual Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(
            IClientSessionHandle session,
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.WatchAsync(session, pipeline, options, cancellationToken);
        }

        public IMongoClient WithReadConcern(ReadConcern readConcern)
        {
            return wrapped.WithReadConcern(readConcern);
        }

        public IMongoClient WithReadPreference(ReadPreference readPreference)
        {
            return wrapped.WithReadPreference(readPreference);
        }

        public IMongoClient WithWriteConcern(WriteConcern writeConcern)
        {
            return wrapped.WithWriteConcern(writeConcern);
        }

        public void Dispose()
        {
            ClusterRegistry.Instance.UnregisterAndDisposeCluster(wrapped.Cluster);
        }
    }
}
