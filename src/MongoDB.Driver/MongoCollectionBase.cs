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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq;

namespace MongoDB.Driver
{
    /// <summary>
    /// Base class for implementors of <see cref="IMongoCollection{TDocument}"/>.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public abstract class MongoCollectionBase<TDocument> : IMongoCollection<TDocument>
    {
        /// <inheritdoc />
        public abstract CollectionNamespace CollectionNamespace { get; }

        /// <inheritdoc />
        public abstract IMongoDatabase Database { get; }

        /// <inheritdoc />
        public abstract IBsonSerializer<TDocument> DocumentSerializer { get; }

        /// <inheritdoc />
        public abstract IMongoIndexManager<TDocument> Indexes { get; }

        /// <inheritdoc />
        public abstract MongoCollectionSettings Settings { get; }

        /// <inheritdoc />
        public abstract Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <inheritdoc />
        public abstract Task<BulkWriteResult<TDocument>> BulkWriteAsync(IEnumerable<WriteModel<TDocument>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <inheritdoc />
        public abstract Task<long> CountAsync(FilterDefinition<TDocument> filter, CountOptions options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <inheritdoc />
        public virtual async Task<DeleteResult> DeleteManyAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(filter, "filter");

            var model = new DeleteManyModel<TDocument>(filter);
            try
            {
                var result = await BulkWriteAsync(new[] { model }, null, cancellationToken).ConfigureAwait(false);
                return DeleteResult.FromCore(result);
            }
            catch (MongoBulkWriteException<TDocument> ex)
            {
                throw MongoWriteException.FromBulkWriteException(ex);
            }
        }

        /// <inheritdoc />
        public virtual async Task<DeleteResult> DeleteOneAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(filter, "filter");

            var model = new DeleteOneModel<TDocument>(filter);
            try
            {
                var result = await BulkWriteAsync(new[] { model }, null, cancellationToken).ConfigureAwait(false);
                return DeleteResult.FromCore(result);
            }
            catch (MongoBulkWriteException<TDocument> ex)
            {
                throw MongoWriteException.FromBulkWriteException(ex);
            }
        }

        /// <inheritdoc />
        public abstract Task<IAsyncCursor<TField>> DistinctAsync<TField>(FieldDefinition<TDocument, TField> field, FilterDefinition<TDocument> filter, DistinctOptions options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <inheritdoc />
        public abstract Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(FilterDefinition<TDocument> filter, FindOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <inheritdoc />
        public abstract Task<TProjection> FindOneAndDeleteAsync<TProjection>(FilterDefinition<TDocument> filter, FindOneAndDeleteOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <inheritdoc />
        public abstract Task<TProjection> FindOneAndReplaceAsync<TProjection>(FilterDefinition<TDocument> filter, TDocument replacement, FindOneAndReplaceOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <inheritdoc />
        public abstract Task<TProjection> FindOneAndUpdateAsync<TProjection>(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, FindOneAndUpdateOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <inheritdoc />
        public virtual async Task InsertOneAsync(TDocument document, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull((object)document, "document");

            var model = new InsertOneModel<TDocument>(document);
            try
            {
                await BulkWriteAsync(new[] { model }, null, cancellationToken).ConfigureAwait(false);
            }
            catch (MongoBulkWriteException<TDocument> ex)
            {
                throw MongoWriteException.FromBulkWriteException(ex);
            }
        }

        /// <inheritdoc />
        public virtual Task InsertManyAsync(IEnumerable<TDocument> documents, InsertManyOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(documents, "documents");

            var models = documents.Select(x => new InsertOneModel<TDocument>(x));
            BulkWriteOptions bulkWriteOptions = options == null ? null : new BulkWriteOptions { IsOrdered = options.IsOrdered };
            return BulkWriteAsync(models, bulkWriteOptions, cancellationToken);
        }

        /// <inheritdoc />
        public abstract Task<IAsyncCursor<TResult>> MapReduceAsync<TResult>(BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TDocument, TResult> options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <inheritdoc />
        public virtual async Task<ReplaceOneResult> ReplaceOneAsync(FilterDefinition<TDocument> filter, TDocument replacement, UpdateOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(filter, "filter");
            Ensure.IsNotNull((object)replacement, "replacement");

            options = options ?? new UpdateOptions();
            var model = new ReplaceOneModel<TDocument>(filter, replacement)
            {
                IsUpsert = options.IsUpsert
            };

            try
            {
                var result = await BulkWriteAsync(new[] { model }, null, cancellationToken).ConfigureAwait(false);
                return ReplaceOneResult.FromCore(result);
            }
            catch (MongoBulkWriteException<TDocument> ex)
            {
                throw MongoWriteException.FromBulkWriteException(ex);
            }
        }

        /// <inheritdoc />
        public virtual async Task<UpdateResult> UpdateManyAsync(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(filter, "filter");
            Ensure.IsNotNull(update, "update");

            options = options ?? new UpdateOptions();
            var model = new UpdateManyModel<TDocument>(filter, update)
            {
                IsUpsert = options.IsUpsert
            };

            try
            {
                var result = await BulkWriteAsync(new[] { model }, null, cancellationToken).ConfigureAwait(false);
                return UpdateResult.FromCore(result);
            }
            catch (MongoBulkWriteException<TDocument> ex)
            {
                throw MongoWriteException.FromBulkWriteException(ex);
            }
        }

        /// <inheritdoc />
        public virtual async Task<UpdateResult> UpdateOneAsync(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(filter, "filter");
            Ensure.IsNotNull(update, "update");

            options = options ?? new UpdateOptions();
            var model = new UpdateOneModel<TDocument>(filter, update)
            {
                IsUpsert = options.IsUpsert
            };

            try
            {
                var result = await BulkWriteAsync(new[] { model }, null, cancellationToken).ConfigureAwait(false);
                return UpdateResult.FromCore(result);
            }
            catch (MongoBulkWriteException<TDocument> ex)
            {
                throw MongoWriteException.FromBulkWriteException(ex);
            }
        }

        /// <inheritdoc />
        public abstract IMongoCollection<TDocument> WithReadPreference(ReadPreference readPreference);

        /// <inheritdoc />
        public abstract IMongoCollection<TDocument> WithWriteConcern(WriteConcern writeConcern);
    }
}
