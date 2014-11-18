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
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Utils;

namespace MongoDB.Driver
{
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
        /// <typeparam name="TField">The type of the result.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="field">The field.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The distinct values for the specified field.
        /// </returns>
        public static Task<IReadOnlyList<TField>> DistinctAsync<TDocument, TField>(this IMongoCollection<TDocument> collection, Expression<Func<TDocument, TField>> field, Expression<Func<TDocument, bool>> filter, DistinctOptions<TField> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(collection, "collection");
            Ensure.IsNotNull(filter, "filter");

            var helper = new BsonSerializationInfoHelper();
            helper.RegisterExpressionSerializer(field.Parameters[0], collection.Settings.SerializerRegistry.GetSerializer<TDocument>());

            var serializationInfo = helper.GetSerializationInfo(field.Body);
            var filterDocument = CreateFilterDocument(collection, filter);
            options = options ?? new DistinctOptions<TField>();
            if (options.ResultSerializer == null)
            {
                options.ResultSerializer = (IBsonSerializer<TField>)serializationInfo.Serializer;
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
