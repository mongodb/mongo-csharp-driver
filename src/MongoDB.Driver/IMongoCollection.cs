/* Copyright 2010-2014 MongoDB Inc.
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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a typed collection in MongoDB.
    /// </summary>
    /// <remarks>
    /// This interface is not guaranteed to remain stable. Implementors should use
    /// <see cref="MongoCollectionBase{TDocument}"/>.
    /// </remarks>
    /// <typeparam name="TDocument">The type of the documents stored in the collection.</typeparam>
    public interface IMongoCollection<TDocument>
    {
        /// <summary>
        /// Gets the namespace of the collection.
        /// </summary>
        CollectionNamespace CollectionNamespace { get; }

        /// <summary>
        /// Gets the database.
        /// </summary>
        IMongoDatabase Database { get; }

        /// <summary>
        /// Gets the document serializer.
        /// </summary>
        IBsonSerializer<TDocument> DocumentSerializer { get; }

        /// <summary>
        /// Gets the index manager.
        /// </summary>
        IMongoIndexManager<TDocument> Indexes { get; }

        /// <summary>
        /// Gets the settings.
        /// </summary>
        MongoCollectionSettings Settings { get; }

        /// <summary>
        /// Runs an aggregation pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is a cursor.</returns>
        Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Performs multiple write operations.
        /// </summary>
        /// <param name="requests">The requests.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of writing.</returns>
        Task<BulkWriteResult<TDocument>> BulkWriteAsync(IEnumerable<WriteModel<TDocument>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Counts the number of documents in the collection.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The number of documents in the collection.
        /// </returns>
        Task<long> CountAsync(FilterDefinition<TDocument> filter, CountOptions options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Deletes multiple documents.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the delete operation.
        /// </returns>
        Task<DeleteResult> DeleteManyAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Deletes a single document.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the delete operation.
        /// </returns>
        Task<DeleteResult> DeleteOneAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the distinct values for a specified field.
        /// </summary>
        /// <typeparam name="TField">The type of the result.</typeparam>
        /// <param name="field">The field.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is a cursor.</returns>
        Task<IAsyncCursor<TField>> DistinctAsync<TField>(FieldDefinition<TDocument, TField> field, FilterDefinition<TDocument> filter, DistinctOptions options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Finds the documents matching the filter.
        /// </summary>
        /// <typeparam name="TProjection">The type of the projection (same as TDocument if there is no projection).</typeparam>
        /// <param name="filter">The filter.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is a cursor.</returns>
        Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(FilterDefinition<TDocument> filter, FindOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Finds a single document and deletes it atomically.
        /// </summary>
        /// <typeparam name="TProjection">The type of the projection (same as TDocument if there is no projection).</typeparam>
        /// <param name="filter">The filter.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The returned document.
        /// </returns>
        Task<TProjection> FindOneAndDeleteAsync<TProjection>(FilterDefinition<TDocument> filter, FindOneAndDeleteOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Finds a single document and replaces it atomically.
        /// </summary>
        /// <typeparam name="TProjection">The type of the projection (same as TDocument if there is no projection).</typeparam>
        /// <param name="filter">The filter.</param>
        /// <param name="replacement">The replacement.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The returned document.
        /// </returns>
        Task<TProjection> FindOneAndReplaceAsync<TProjection>(FilterDefinition<TDocument> filter, TDocument replacement, FindOneAndReplaceOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Finds a single document and updates it atomically.
        /// </summary>
        /// <typeparam name="TProjection">The type of the projection (same as TDocument if there is no projection).</typeparam>
        /// <param name="filter">The filter.</param>
        /// <param name="update">The update.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The returned document.
        /// </returns>
        Task<TProjection> FindOneAndUpdateAsync<TProjection>(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, FindOneAndUpdateOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Inserts a single document.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the insert operation.
        /// </returns>
        Task InsertOneAsync(TDocument document, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Inserts many documents.
        /// </summary>
        /// <param name="documents">The documents.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the insert operation.
        /// </returns>
        Task InsertManyAsync(IEnumerable<TDocument> documents, InsertManyOptions options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Executes a map-reduce command.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="map">The map function.</param>
        /// <param name="reduce">The reduce function.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is a cursor.</returns>
        Task<IAsyncCursor<TResult>> MapReduceAsync<TResult>(BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TDocument, TResult> options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Replaces a single document.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="replacement">The replacement.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the replacement.
        /// </returns>
        Task<ReplaceOneResult> ReplaceOneAsync(FilterDefinition<TDocument> filter, TDocument replacement, UpdateOptions options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Updates many documents.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="update">The update.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the update operation.
        /// </returns>
        Task<UpdateResult> UpdateManyAsync(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Updates a single document.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="update">The update.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the update operation.
        /// </returns>
        Task<UpdateResult> UpdateOneAsync(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns a new collection with a different read preference.
        /// </summary>
        /// <param name="readPreference">The read preference.</param>
        /// <returns>A new collection.</returns>
        IMongoCollection<TDocument> WithReadPreference(ReadPreference readPreference);

        /// <summary>
        /// Returns a new collection with a different write concern.
        /// </summary>
        /// <param name="writeConcern">The write concern.</param>
        /// <returns>A new collection.</returns>
        IMongoCollection<TDocument> WithWriteConcern(WriteConcern writeConcern);
    }
}
