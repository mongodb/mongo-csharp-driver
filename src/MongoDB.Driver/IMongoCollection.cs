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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

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
        /// Gets the settings.
        /// </summary>
        MongoCollectionSettings Settings { get; }

        /// <summary>
        /// Begins an aggregation pipeline.
        /// </summary>
        /// <returns></returns>
        AggregateFluent<TDocument, TDocument> Aggregate();

        /// <summary>
        /// Runs an aggregation pipeline asynchronously.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="model">The model.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<IAsyncEnumerable<TResult>> AggregateAsync<TResult>(AggregateOptions<TResult> model, TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Performs multiple write operations at the same time.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of writing.</returns>
        Task<BulkWriteResult<TDocument>> BulkWriteAsync(BulkWriteModel<TDocument> model, TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Counts the number of documents in the collection.
        /// </summary>
        /// <param name="criteria">The criteria.</param>
        /// <param name="options">The options.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The number of documents in the collection
        /// </returns>
        Task<long> CountAsync(object criteria = null, CountOptions options = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Deletes multiple documents.
        /// </summary>
        /// <param name="criteria">The criteria.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the delete operation.
        /// </returns>
        Task<DeleteResult> DeleteManyAsync(object criteria, TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Deletes a single document.
        /// </summary>
        /// <param name="criteria">The criteria.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the delete operation.
        /// </returns>
        Task<DeleteResult> DeleteOneAsync(object criteria, TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the distinct values for a specified field.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="options">The options.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The distinct values for the specified field.
        /// </returns>
        Task<IReadOnlyList<TResult>> DistinctAsync<TResult>(string fieldName, DistinctOptions<TResult> options = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Begins a fluent find interface.
        /// </summary>
        /// <param name="criteria">The criteria.</param>
        /// <returns>A fluent interface.</returns>
        FindFluent<TDocument, TDocument> Find(object criteria);

        /// <summary>
        /// Finds the documents matching the model.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="options">The options.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The results of the query.
        /// </returns>
        Task<IAsyncEnumerable<TResult>> FindAsync<TResult>(FindOptions<TResult> options, TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Finds a single document and deletes it atomically.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="criteria">The criteria.</param>
        /// <param name="options">The options.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The deleted document if one was deleted.
        /// </returns>
        Task<TResult> FindOneAndDeleteAsync<TResult>(object criteria, FindOneAndDeleteOptions<TResult> options = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Finds a single document and replaces it atomically.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="criteria">The criteria.</param>
        /// <param name="replacement">The replacement.</param>
        /// <param name="options">The options.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the operation.
        /// </returns>
        Task<TResult> FindOneAndReplaceAsync<TResult>(object criteria, TDocument replacement, FindOneAndReplaceOptions<TResult> options = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Finds a single document and updates it atomically.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="criteria">The criteria.</param>
        /// <param name="update">The update.</param>
        /// <param name="options">The model.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the operation.
        /// </returns>
        Task<TResult> FindOneAndUpdateAsync<TResult>(object criteria, object update, FindOneAndUpdateOptions<TResult> options = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Inserts a single document.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the insert operation.
        /// </returns>
        Task InsertOneAsync(TDocument document, TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Replaces a single document.
        /// </summary>
        /// <param name="criteria">The criteria.</param>
        /// <param name="replacement">The replacement.</param>
        /// <param name="options">The options.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the replacement.
        /// </returns>
        Task<ReplaceOneResult> ReplaceOneAsync(object criteria, TDocument replacement, UpdateOptions options = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Updates many documents.
        /// </summary>
        /// <param name="criteria">The criteria.</param>
        /// <param name="update">The update.</param>
        /// <param name="options">The options.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the update operation.
        /// </returns>
        Task<UpdateResult> UpdateManyAsync(object criteria, object update, UpdateOptions options = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Updates a single document.
        /// </summary>
        /// <param name="criteria">The criteria.</param>
        /// <param name="update">The update.</param>
        /// <param name="options">The options.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the update operation.
        /// </returns>
        Task<UpdateResult> UpdateOneAsync(object criteria, object update, UpdateOptions options = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken));
    }
}
