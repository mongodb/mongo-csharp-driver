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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Linq.Utils;

namespace MongoDB.Driver
{
    /// <summary>
    /// Logical representation of a collection in MongoDB.
    /// </summary>
    /// <typeparam name="TDocument">The document type.</typeparam>
    public interface IMongoCollection<TDocument>
    {
        /// <summary>
        /// Gets the name of the collection.
        /// </summary>
        CollectionNamespace CollectionNamespace { get; }

        /// <summary>
        /// Gets the index manager.
        /// </summary>
        IMongoIndexManager<TDocument> IndexManager { get; }

        /// <summary>
        /// Gets the settings.
        /// </summary>
        MongoCollectionSettings Settings { get; }

        /// <summary>
        /// Begins an aggregation pipeline.
        /// </summary>
        /// <returns></returns>
        AggregateFluent<TDocument, TDocument> Aggregate(AggregateOptions options = null);

        /// <summary>
        /// Runs an aggregation pipeline asynchronously.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="options">The model.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(IEnumerable<object> pipeline, AggregateOptions<TResult> options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Performs multiple write operations at the same time.
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
        /// The number of documents in the collection
        /// </returns>
        Task<long> CountAsync(object filter, CountOptions options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Deletes multiple documents.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the delete operation.
        /// </returns>
        Task<DeleteResult> DeleteManyAsync(object filter, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Deletes a single document.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the delete operation.
        /// </returns>
        Task<DeleteResult> DeleteOneAsync(object filter, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the distinct values for a specified field.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The distinct values for the specified field.
        /// </returns>
        Task<IReadOnlyList<TResult>> DistinctAsync<TResult>(string fieldName, object filter, DistinctOptions<TResult> options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Begins a fluent find interface.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <returns>A fluent interface.</returns>
        FindFluent<TDocument, TDocument> Find(object filter);

        /// <summary>
        /// Finds the documents matching the model.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="filter">The filter.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The results of the query.
        /// </returns>
        Task<IAsyncCursor<TResult>> FindAsync<TResult>(object filter, FindOptions<TResult> options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Finds a single document and deletes it atomically.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The deleted document if one was deleted.
        /// </returns>
        Task<TDocument> FindOneAndDeleteAsync(object filter, FindOneAndDeleteOptions<TDocument> options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Finds a single document and deletes it atomically.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="filter">The filter.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The deleted document if one was deleted.
        /// </returns>
        Task<TResult> FindOneAndDeleteAsync<TResult>(object filter, FindOneAndDeleteOptions<TResult> options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Finds a single document and replaces it atomically.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="replacement">The replacement.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the operation.
        /// </returns>
        Task<TDocument> FindOneAndReplaceAsync(object filter, TDocument replacement, FindOneAndReplaceOptions<TDocument> options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Finds a single document and replaces it atomically.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="filter">The filter.</param>
        /// <param name="replacement">The replacement.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the operation.
        /// </returns>
        Task<TResult> FindOneAndReplaceAsync<TResult>(object filter, TDocument replacement, FindOneAndReplaceOptions<TResult> options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Finds a single document and updates it atomically.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="update">The update.</param>
        /// <param name="options">The model.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the operation.
        /// </returns>
        Task<TDocument> FindOneAndUpdateAsync(object filter, object update, FindOneAndUpdateOptions<TDocument> options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Finds a single document and updates it atomically.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="filter">The filter.</param>
        /// <param name="update">The update.</param>
        /// <param name="options">The model.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the operation.
        /// </returns>
        Task<TResult> FindOneAndUpdateAsync<TResult>(object filter, object update, FindOneAndUpdateOptions<TResult> options = null, CancellationToken cancellationToken = default(CancellationToken));

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
        /// Replaces a single document.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="replacement">The replacement.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the replacement.
        /// </returns>
        Task<ReplaceOneResult> ReplaceOneAsync(object filter, TDocument replacement, UpdateOptions options = null, CancellationToken cancellationToken = default(CancellationToken));

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
        Task<UpdateResult> UpdateManyAsync(object filter, object update, UpdateOptions options = null, CancellationToken cancellationToken = default(CancellationToken));

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
        Task<UpdateResult> UpdateOneAsync(object filter, object update, UpdateOptions options = null, CancellationToken cancellationToken = default(CancellationToken));
    }

    /// <summary>
    /// Extensions for <see cref="IMongoCollection{T}"/>.
    /// </summary>
    public static class IMongoCollectionExtensions
    {
        /// <summary>
        /// Counts the number of documents in the collection.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The number of documents in the collection
        /// </returns>
        public static Task<long> CountAsync<TDocument>(this IMongoCollection<TDocument> collection, Expression<Func<TDocument, bool>> filter, CountOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(collection, "collection");
            Ensure.IsNotNull(filter, "filter");

            var filterDocument = CreateFilterDocument(collection, filter);
            return collection.CountAsync(filterDocument, options, cancellationToken);
        }

        /// <summary>
        /// Deletes multiple documents.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the delete operation.
        /// </returns>
        public static Task<DeleteResult> DeleteManyAsync<TDocument>(this IMongoCollection<TDocument> collection, Expression<Func<TDocument, bool>> filter, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(collection, "collection");
            Ensure.IsNotNull(filter, "filter");

            var filterDocument = CreateFilterDocument(collection, filter);
            return collection.DeleteManyAsync(filterDocument, cancellationToken);
        }

        /// <summary>
        /// Deletes a single document.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the delete operation.
        /// </returns>
        public static Task<DeleteResult> DeleteOneAsync<TDocument>(this IMongoCollection<TDocument> collection, Expression<Func<TDocument, bool>> filter, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(collection, "collection");
            Ensure.IsNotNull(filter, "filter");

            var filterDocument = CreateFilterDocument(collection, filter);
            return collection.DeleteOneAsync(filterDocument, cancellationToken);
        }

        /// <summary>
        /// Gets the distinct values for a specified field.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="field">The field.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The distinct values for the specified field.
        /// </returns>
        public static Task<IReadOnlyList<TResult>> DistinctAsync<TDocument, TResult>(this IMongoCollection<TDocument> collection, Expression<Func<TDocument, TResult>> field, Expression<Func<TDocument, bool>> filter, DistinctOptions<TResult> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(collection, "collection");
            Ensure.IsNotNull(filter, "filter");

            var helper = new BsonSerializationInfoHelper();
            helper.RegisterExpressionSerializer(field.Parameters[0], collection.Settings.SerializerRegistry.GetSerializer<TDocument>());

            var serializationInfo = helper.GetSerializationInfo(field.Body);
            var filterDocument = CreateFilterDocument(collection, filter);
            options = options ?? new DistinctOptions<TResult>();
            if (options.ResultSerializer == null)
            {
                options.ResultSerializer = (IBsonSerializer<TResult>)serializationInfo.Serializer;
            }
            return collection.DistinctAsync(serializationInfo.ElementName, filterDocument, options, cancellationToken);
        }

        /// <summary>
        /// Begins a fluent find interface.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>
        /// A fluent interface.
        /// </returns>
        public static FindFluent<TDocument, TDocument> Find<TDocument>(this IMongoCollection<TDocument> collection, Expression<Func<TDocument, bool>> filter)
        {
            Ensure.IsNotNull(collection, "collection");
            Ensure.IsNotNull(filter, "filter");

            var filterDocument = CreateFilterDocument(collection, filter);
            return new FindFluent<TDocument, TDocument>(collection, filterDocument, new FindOptions<TDocument>());
        }

        /// <summary>
        /// Finds a single document and deletes it atomically.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The deleted document if one was deleted.
        /// </returns>
        public static Task<TDocument> FindOneAndDeleteAsync<TDocument>(this IMongoCollection<TDocument> collection, Expression<Func<TDocument, bool>> filter, FindOneAndDeleteOptions<TDocument> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(collection, "collection");
            Ensure.IsNotNull(filter, "filter");

            var filterDocument = CreateFilterDocument(collection, filter);
            return collection.FindOneAndDeleteAsync(filterDocument, options, cancellationToken);
        }

        /// <summary>
        /// Finds a single document and deletes it atomically.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The deleted document if one was deleted.
        /// </returns>
        public static Task<TResult> FindOneAndDeleteAsync<TDocument, TResult>(this IMongoCollection<TDocument> collection, Expression<Func<TDocument, bool>> filter, FindOneAndDeleteOptions<TResult> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(collection, "collection");
            Ensure.IsNotNull(filter, "filter");

            var filterDocument = CreateFilterDocument(collection, filter);
            return collection.FindOneAndDeleteAsync(filterDocument, options, cancellationToken);
        }

        /// <summary>
        /// Finds a single document and replaces it atomically.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="replacement">The replacement.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the operation.
        /// </returns>
        public static Task<TDocument> FindOneAndReplaceAsync<TDocument>(this IMongoCollection<TDocument> collection, Expression<Func<TDocument, bool>> filter, TDocument replacement, FindOneAndReplaceOptions<TDocument> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(collection, "collection");
            Ensure.IsNotNull(filter, "filter");

            var filterDocument = CreateFilterDocument(collection, filter);
            return collection.FindOneAndReplaceAsync(filterDocument, replacement, options, cancellationToken);
        }

        /// <summary>
        /// Finds a single document and replaces it atomically.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="replacement">The replacement.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the operation.
        /// </returns>
        public static Task<TResult> FindOneAndReplaceAsync<TDocument, TResult>(this IMongoCollection<TDocument> collection, Expression<Func<TDocument, bool>> filter, TDocument replacement, FindOneAndReplaceOptions<TResult> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(collection, "collection");
            Ensure.IsNotNull(filter, "filter");

            var filterDocument = CreateFilterDocument(collection, filter);
            return collection.FindOneAndReplaceAsync(filterDocument, replacement, options, cancellationToken);
        }

        /// <summary>
        /// Finds a single document and updates it atomically.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="update">The update.</param>
        /// <param name="options">The model.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the operation.
        /// </returns>
        public static Task<TDocument> FindOneAndUpdateAsync<TDocument>(this IMongoCollection<TDocument> collection, Expression<Func<TDocument, bool>> filter, object update, FindOneAndUpdateOptions<TDocument> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(collection, "collection");
            Ensure.IsNotNull(collection, "filter");

            var filterDocument = CreateFilterDocument(collection, filter);
            return collection.FindOneAndUpdateAsync(filterDocument, update, options, cancellationToken);
        }

        /// <summary>
        /// Finds a single document and updates it atomically.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="update">The update.</param>
        /// <param name="options">The model.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the operation.
        /// </returns>
        public static Task<TResult> FindOneAndUpdateAsync<TDocument, TResult>(this IMongoCollection<TDocument> collection, Expression<Func<TDocument, bool>> filter, object update, FindOneAndUpdateOptions<TResult> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(collection, "collection");
            Ensure.IsNotNull(filter, "filter");

            var filterDocument = CreateFilterDocument(collection, filter);
            return collection.FindOneAndUpdateAsync(filterDocument, update, options, cancellationToken);
        }

        /// <summary>
        /// Replaces a single document.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="replacement">The replacement.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the replacement.
        /// </returns>
        public static Task<ReplaceOneResult> ReplaceOneAsync<TDocument>(this IMongoCollection<TDocument> collection, Expression<Func<TDocument, bool>> filter, TDocument replacement, UpdateOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(collection, "collection");
            Ensure.IsNotNull(filter, "filter");

            var filterDocument = CreateFilterDocument(collection, filter);
            return collection.ReplaceOneAsync(filterDocument, replacement, options, cancellationToken);
        }

        /// <summary>
        /// Updates many documents.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="update">The update.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the update operation.
        /// </returns>
        public static Task<UpdateResult> UpdateManyAsync<TDocument>(this IMongoCollection<TDocument> collection, Expression<Func<TDocument, bool>> filter, object update, UpdateOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(collection, "collection");
            Ensure.IsNotNull(filter, "filter");

            var filterDocument = CreateFilterDocument(collection, filter);
            return collection.UpdateManyAsync(filterDocument, update, options, cancellationToken);
        }

        /// <summary>
        /// Updates a single document.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="update">The update.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the update operation.
        /// </returns>
        public static Task<UpdateResult> UpdateOneAsync<TDocument>(this IMongoCollection<TDocument> collection, Expression<Func<TDocument, bool>> filter, object update, UpdateOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(collection, "collection");
            Ensure.IsNotNull(filter, "filter");

            var filterDocument = CreateFilterDocument(collection, filter);
            return collection.UpdateOneAsync(filterDocument, update, options, cancellationToken);
        }

        private static BsonDocument CreateFilterDocument<TDocument>(IMongoCollection<TDocument> collection, Expression<Func<TDocument, bool>> filter)
        {
            var helper = new BsonSerializationInfoHelper();
            helper.RegisterExpressionSerializer(filter.Parameters[0], collection.Settings.SerializerRegistry.GetSerializer<TDocument>());
            return new QueryBuilder<TDocument>(helper).Where(filter).ToBsonDocument();
        }
    }
}
