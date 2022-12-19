﻿/* Copyright 2018-present MongoDB Inc.
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

using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq;

namespace MongoDB.Driver
{
    /// <summary>
    /// Extension methods on IMongoDatabase.
    /// </summary>
    public static class IMongoDatabaseExtensions
    {
        /// <summary>
        /// Begins a fluent aggregation interface.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="options">The options.</param>
        /// <returns>
        /// A fluent aggregate interface.
        /// </returns>
        public static IAggregateFluent<NoPipelineInput> Aggregate(this IMongoDatabase database, AggregateOptions options = null)
        {
            var emptyPipeline = new EmptyPipelineDefinition<NoPipelineInput>(NoPipelineInputSerializer.Instance);
            return new DatabaseAggregateFluent<NoPipelineInput>(null, database, emptyPipeline, options ?? new AggregateOptions());
        }

        /// <summary>
        /// Begins a fluent aggregation interface.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="session">The session.</param>
        /// <param name="options">The options.</param>
        /// <returns>
        /// A fluent aggregate interface.
        /// </returns>
        public static IAggregateFluent<NoPipelineInput> Aggregate(this IMongoDatabase database, IClientSessionHandle session, AggregateOptions options = null)
        {
            Ensure.IsNotNull(session, nameof(session));
            var emptyPipeline = new EmptyPipelineDefinition<NoPipelineInput>(NoPipelineInputSerializer.Instance);
            return new DatabaseAggregateFluent<NoPipelineInput>(session, database, emptyPipeline, options ?? new AggregateOptions());
        }

        /// <summary>
        /// Creates a queryable source of documents.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="aggregateOptions">The aggregate options</param>
        /// <returns>A queryable source of documents.</returns>
        public static IMongoQueryable<NoPipelineInput> AsQueryable(this IMongoDatabase database, AggregateOptions aggregateOptions = null)
        {
            Ensure.IsNotNull(database, nameof(database));

            return AsQueryableHelper(database, session: null, aggregateOptions);
        }

        /// <summary>
        /// Creates a queryable source of documents.
        /// </summary>
        /// <param name="database">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="aggregateOptions">The aggregate options</param>
        /// <returns>A queryable source of documents.</returns>
        public static IMongoQueryable<NoPipelineInput> AsQueryable(this IMongoDatabase database, IClientSessionHandle session, AggregateOptions aggregateOptions = null)
        {
            Ensure.IsNotNull(database, nameof(database));
            Ensure.IsNotNull(session, nameof(session));

            return AsQueryableHelper(database, session, aggregateOptions);
        }

        /// <summary>
        /// Watches changes on all collection in a database.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A change stream.
        /// </returns>
        public static IChangeStreamCursor<ChangeStreamDocument<BsonDocument>> Watch(
            this IMongoDatabase database,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(database, nameof(database));
            var emptyPipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>();
            return database.Watch(emptyPipeline, options, cancellationToken);
        }

        /// <summary>
        /// Watches changes on all collection in a database.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="session">The session.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A change stream.
        /// </returns>
        public static IChangeStreamCursor<ChangeStreamDocument<BsonDocument>> Watch(
            this IMongoDatabase database,
            IClientSessionHandle session,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(database, nameof(database));
            Ensure.IsNotNull(session, nameof(session));
            var emptyPipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>();
            return database.Watch(session, emptyPipeline, options, cancellationToken);
        }

        /// <summary>
        /// Watches changes on all collection in a database.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A change stream.
        /// </returns>
        public static Task<IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>> WatchAsync(
            this IMongoDatabase database,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(database, nameof(database));
            var emptyPipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>();
            return database.WatchAsync(emptyPipeline, options, cancellationToken);
        }

        /// <summary>
        /// Watches changes on all collection in a database.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="session">The session.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A change stream.
        /// </returns>
        public static Task<IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>> WatchAsync(
            this IMongoDatabase database,
            IClientSessionHandle session,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(database, nameof(database));
            Ensure.IsNotNull(session, nameof(session));
            var emptyPipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>();
            return database.WatchAsync(session, emptyPipeline, options, cancellationToken);
        }

        // private static methods
        private static IMongoQueryable<NoPipelineInput> AsQueryableHelper(IMongoDatabase database, IClientSessionHandle session, AggregateOptions aggregateOptions)
        {
            var linqProvider = database.Client.Settings.LinqProvider;
            aggregateOptions = aggregateOptions ?? new AggregateOptions();
            return linqProvider.GetAdapter().AsQueryable(database, session, aggregateOptions);
        }
    }
}
