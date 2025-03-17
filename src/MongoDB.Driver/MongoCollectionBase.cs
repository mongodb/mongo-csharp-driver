/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Driver.Search;

namespace MongoDB.Driver
{
    internal abstract class MongoCollectionBase<TDocument> : IMongoCollection<TDocument>
    {
        public abstract CollectionNamespace CollectionNamespace { get; }

        public abstract IMongoDatabase Database { get; }

        public abstract IBsonSerializer<TDocument> DocumentSerializer { get; }

        public abstract IMongoIndexManager<TDocument> Indexes { get; }

        public abstract IMongoSearchIndexManager SearchIndexes { get; }

        public abstract MongoCollectionSettings Settings { get; }

        public virtual IAsyncCursor<TResult> Aggregate<TResult>(PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public virtual IAsyncCursor<TResult> Aggregate<TResult>(IClientSessionHandle session, PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public abstract Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default(CancellationToken));

        public virtual Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(IClientSessionHandle session, PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public virtual void AggregateToCollection<TResult>(PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public virtual void AggregateToCollection<TResult>(IClientSessionHandle session, PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public virtual Task AggregateToCollectionAsync<TResult>(PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public virtual Task AggregateToCollectionAsync<TResult>(IClientSessionHandle session, PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public virtual BulkWriteResult<TDocument> BulkWrite(IEnumerable<WriteModel<TDocument>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public virtual BulkWriteResult<TDocument> BulkWrite(IClientSessionHandle session, IEnumerable<WriteModel<TDocument>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public abstract Task<BulkWriteResult<TDocument>> BulkWriteAsync(IEnumerable<WriteModel<TDocument>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = default(CancellationToken));

        public virtual Task<BulkWriteResult<TDocument>> BulkWriteAsync(IClientSessionHandle session, IEnumerable<WriteModel<TDocument>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        [Obsolete("Use CountDocuments or EstimatedDocumentCount instead.")]
        public virtual long Count(FilterDefinition<TDocument> filter, CountOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        [Obsolete("Use CountDocuments or EstimatedDocumentCount instead.")]
        public virtual long Count(IClientSessionHandle session, FilterDefinition<TDocument> filter, CountOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        [Obsolete("Use CountDocumentsAsync or EstimatedDocumentCountAsync instead.")]
        public abstract Task<long> CountAsync(FilterDefinition<TDocument> filter, CountOptions options = null, CancellationToken cancellationToken = default(CancellationToken));

        [Obsolete("Use CountDocumentsAsync or EstimatedDocumentCountAsync instead.")]
        public virtual Task<long> CountAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, CountOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public virtual long CountDocuments(FilterDefinition<TDocument> filter, CountOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public virtual long CountDocuments(IClientSessionHandle session, FilterDefinition<TDocument> filter, CountOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public virtual Task<long> CountDocumentsAsync(FilterDefinition<TDocument> filter, CountOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public virtual Task<long> CountDocumentsAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, CountOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public virtual DeleteResult DeleteMany(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DeleteMany(filter, null, cancellationToken);
        }

        public virtual DeleteResult DeleteMany(FilterDefinition<TDocument> filter, DeleteOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DeleteMany(filter, options, (requests, bulkWriteOptions) => BulkWrite(requests, bulkWriteOptions, cancellationToken));
        }

        public virtual DeleteResult DeleteMany(IClientSessionHandle session, FilterDefinition<TDocument> filter, DeleteOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DeleteMany(filter, options, (requests, bulkWriteOptions) => BulkWrite(session, requests, bulkWriteOptions, cancellationToken));
        }

        private DeleteResult DeleteMany(FilterDefinition<TDocument> filter, DeleteOptions options, Func<IEnumerable<WriteModel<TDocument>>, BulkWriteOptions, BulkWriteResult> bulkWriteFunc)
        {
            Ensure.IsNotNull(filter, nameof(filter));
            options = options ?? new DeleteOptions();

            var model = new DeleteManyModel<TDocument>(filter)
            {
                Collation = options.Collation,
                Hint = options.Hint
            };
            try
            {
                var bulkWriteOptions = new BulkWriteOptions
                {
                    Comment = options.Comment,
                    Let = options.Let
                };
                var result = bulkWriteFunc(new[] { model }, bulkWriteOptions);
                return DeleteResult.FromCore(result);
            }
            catch (MongoBulkWriteException<TDocument> ex)
            {
                throw MongoWriteException.FromBulkWriteException(ex);
            }
        }

        public virtual Task<DeleteResult> DeleteManyAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DeleteManyAsync(filter, null, cancellationToken);
        }

        public virtual Task<DeleteResult> DeleteManyAsync(FilterDefinition<TDocument> filter, DeleteOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DeleteManyAsync(filter, options, (requests, bulkWriteOptions) => BulkWriteAsync(requests, bulkWriteOptions, cancellationToken));
        }

        public virtual Task<DeleteResult> DeleteManyAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, DeleteOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DeleteManyAsync(filter, options, (requests, bulkWriteOptions) => BulkWriteAsync(session, requests, bulkWriteOptions, cancellationToken));
        }

        private async Task<DeleteResult> DeleteManyAsync(FilterDefinition<TDocument> filter, DeleteOptions options, Func<IEnumerable<WriteModel<TDocument>>, BulkWriteOptions, Task<BulkWriteResult<TDocument>>> bulkWriteFuncAsync)
        {
            Ensure.IsNotNull(filter, nameof(filter));
            options = options ?? new DeleteOptions();

            var model = new DeleteManyModel<TDocument>(filter)
            {
                Collation = options.Collation,
                Hint = options.Hint
            };
            try
            {
                var bulkWriteOptions = new BulkWriteOptions
                {
                    Comment = options.Comment,
                    Let = options.Let
                };
                var result = await bulkWriteFuncAsync(new[] { model }, bulkWriteOptions).ConfigureAwait(false);
                return DeleteResult.FromCore(result);
            }
            catch (MongoBulkWriteException<TDocument> ex)
            {
                throw MongoWriteException.FromBulkWriteException(ex);
            }
        }

        public virtual DeleteResult DeleteOne(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DeleteOne(filter, null, cancellationToken);
        }

        public virtual DeleteResult DeleteOne(FilterDefinition<TDocument> filter, DeleteOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DeleteOne(filter, options, (requests, bulkWriteOptions) => BulkWrite(requests, bulkWriteOptions, cancellationToken));
        }

        public virtual DeleteResult DeleteOne(IClientSessionHandle session, FilterDefinition<TDocument> filter, DeleteOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DeleteOne(filter, options, (requests, bulkWriteOptions) => BulkWrite(session, requests, bulkWriteOptions, cancellationToken));
        }

        private DeleteResult DeleteOne(FilterDefinition<TDocument> filter, DeleteOptions options, Func<IEnumerable<WriteModel<TDocument>>, BulkWriteOptions, BulkWriteResult> bulkWrite)
        {
            Ensure.IsNotNull(filter, nameof(filter));
            options = options ?? new DeleteOptions();

            var model = new DeleteOneModel<TDocument>(filter)
            {
                Collation = options.Collation,
                Hint = options.Hint
            };
            try
            {
                var bulkWriteOptions = new BulkWriteOptions
                {
                    Comment = options.Comment,
                    Let = options.Let
                };
                var result = bulkWrite(new[] { model }, bulkWriteOptions);
                return DeleteResult.FromCore(result);
            }
            catch (MongoBulkWriteException<TDocument> ex)
            {
                throw MongoWriteException.FromBulkWriteException(ex);
            }
        }

        public virtual Task<DeleteResult> DeleteOneAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DeleteOneAsync(filter, null, cancellationToken);
        }

        public virtual Task<DeleteResult> DeleteOneAsync(FilterDefinition<TDocument> filter, DeleteOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DeleteOneAsync(filter, options, (requests, bulkWriteOptions) => BulkWriteAsync(requests, bulkWriteOptions, cancellationToken));
        }

        public virtual Task<DeleteResult> DeleteOneAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, DeleteOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DeleteOneAsync(filter, options, (requests, bulkWriteOptions) => BulkWriteAsync(session, requests, bulkWriteOptions, cancellationToken));
        }

        private async Task<DeleteResult> DeleteOneAsync(FilterDefinition<TDocument> filter, DeleteOptions options, Func<IEnumerable<WriteModel<TDocument>>, BulkWriteOptions, Task<BulkWriteResult<TDocument>>> bulkWriteAsync)
        {
            Ensure.IsNotNull(filter, nameof(filter));
            options = options ?? new DeleteOptions();

            var model = new DeleteOneModel<TDocument>(filter)
            {
                Collation = options.Collation,
                Hint = options.Hint
            };
            try
            {
                var bulkWriteOptions = new BulkWriteOptions
                {
                    Comment = options.Comment,
                    Let = options.Let
                };
                var result = await bulkWriteAsync(new[] { model }, bulkWriteOptions).ConfigureAwait(false);
                return DeleteResult.FromCore(result);
            }
            catch (MongoBulkWriteException<TDocument> ex)
            {
                throw MongoWriteException.FromBulkWriteException(ex);
            }
        }

        public virtual IAsyncCursor<TField> Distinct<TField>(FieldDefinition<TDocument, TField> field, FilterDefinition<TDocument> filter, DistinctOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public virtual IAsyncCursor<TField> Distinct<TField>(IClientSessionHandle session, FieldDefinition<TDocument, TField> field, FilterDefinition<TDocument> filter, DistinctOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public abstract Task<IAsyncCursor<TField>> DistinctAsync<TField>(FieldDefinition<TDocument, TField> field, FilterDefinition<TDocument> filter, DistinctOptions options = null, CancellationToken cancellationToken = default(CancellationToken));

        public virtual Task<IAsyncCursor<TField>> DistinctAsync<TField>(IClientSessionHandle session, FieldDefinition<TDocument, TField> field, FilterDefinition<TDocument> filter, DistinctOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public virtual IAsyncCursor<TItem> DistinctMany<TItem>(FieldDefinition<TDocument, IEnumerable<TItem>> field, FilterDefinition<TDocument> filter, DistinctOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public virtual IAsyncCursor<TItem> DistinctMany<TItem>(IClientSessionHandle session, FieldDefinition<TDocument, IEnumerable<TItem>> field, FilterDefinition<TDocument> filter, DistinctOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public virtual Task<IAsyncCursor<TItem>> DistinctManyAsync<TItem>(FieldDefinition<TDocument, IEnumerable<TItem>> field, FilterDefinition<TDocument> filter, DistinctOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public virtual Task<IAsyncCursor<TItem>> DistinctManyAsync<TItem>(IClientSessionHandle session, FieldDefinition<TDocument, IEnumerable<TItem>> field, FilterDefinition<TDocument> filter, DistinctOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public virtual long EstimatedDocumentCount(EstimatedDocumentCountOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public virtual Task<long> EstimatedDocumentCountAsync(EstimatedDocumentCountOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public virtual IAsyncCursor<TProjection> FindSync<TProjection>(FilterDefinition<TDocument> filter, FindOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public virtual IAsyncCursor<TProjection> FindSync<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, FindOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public abstract Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(FilterDefinition<TDocument> filter, FindOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default(CancellationToken));

        public virtual Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, FindOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public virtual TProjection FindOneAndDelete<TProjection>(FilterDefinition<TDocument> filter, FindOneAndDeleteOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public virtual TProjection FindOneAndDelete<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, FindOneAndDeleteOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public abstract Task<TProjection> FindOneAndDeleteAsync<TProjection>(FilterDefinition<TDocument> filter, FindOneAndDeleteOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default(CancellationToken));

        public virtual Task<TProjection> FindOneAndDeleteAsync<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, FindOneAndDeleteOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public virtual TProjection FindOneAndReplace<TProjection>(FilterDefinition<TDocument> filter, TDocument replacement, FindOneAndReplaceOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default(CancellationToken))

        {
            throw new NotImplementedException();
        }

        public virtual TProjection FindOneAndReplace<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, TDocument replacement, FindOneAndReplaceOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default(CancellationToken))

        {
            throw new NotImplementedException();
        }

        public abstract Task<TProjection> FindOneAndReplaceAsync<TProjection>(FilterDefinition<TDocument> filter, TDocument replacement, FindOneAndReplaceOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default(CancellationToken));

        public virtual Task<TProjection> FindOneAndReplaceAsync<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, TDocument replacement, FindOneAndReplaceOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public virtual TProjection FindOneAndUpdate<TProjection>(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, FindOneAndUpdateOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public virtual TProjection FindOneAndUpdate<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, FindOneAndUpdateOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public abstract Task<TProjection> FindOneAndUpdateAsync<TProjection>(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, FindOneAndUpdateOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default(CancellationToken));

        public virtual Task<TProjection> FindOneAndUpdateAsync<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, FindOneAndUpdateOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public virtual void InsertOne(TDocument document, InsertOneOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            InsertOne(document, options, (requests, bulkWriteOptions) => BulkWrite(requests, bulkWriteOptions, cancellationToken));
        }

        public virtual void InsertOne(IClientSessionHandle session, TDocument document, InsertOneOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            InsertOne(document, options, (requests, bulkWriteOptions) => BulkWrite(session, requests, bulkWriteOptions, cancellationToken));
        }

        private void InsertOne(TDocument document, InsertOneOptions options, Action<IEnumerable<WriteModel<TDocument>>, BulkWriteOptions> bulkWrite)
        {
            Ensure.IsNotNull((object)document, "document");

            var model = new InsertOneModel<TDocument>(document);
            try
            {
                var bulkWriteOptions = options == null ? null : new BulkWriteOptions
                {
                    BypassDocumentValidation = options.BypassDocumentValidation,
                    Comment = options.Comment
                };
                bulkWrite(new[] { model }, bulkWriteOptions);
            }
            catch (MongoBulkWriteException<TDocument> ex)
            {
                throw MongoWriteException.FromBulkWriteException(ex);
            }
        }

        [Obsolete("Use the new overload of InsertOneAsync with an InsertOneOptions parameter instead.")]
        public virtual Task InsertOneAsync(TDocument document, CancellationToken _cancellationToken)
        {
            return InsertOneAsync(document, null, _cancellationToken);
        }

        public virtual Task InsertOneAsync(TDocument document, InsertOneOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return InsertOneAsync(document, options, (requests, bulkWriteOptions) => BulkWriteAsync(requests, bulkWriteOptions, cancellationToken));
        }

        public virtual Task InsertOneAsync(IClientSessionHandle session, TDocument document, InsertOneOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return InsertOneAsync(document, options, (requests, bulkWriteOptions) => BulkWriteAsync(session, requests, bulkWriteOptions, cancellationToken));
        }

        private async Task InsertOneAsync(TDocument document, InsertOneOptions options, Func<IEnumerable<WriteModel<TDocument>>, BulkWriteOptions, Task> bulkWriteAsync)
        {
            Ensure.IsNotNull((object)document, "document");

            var model = new InsertOneModel<TDocument>(document);
            try
            {
                var bulkWriteOptions = options == null ? null : new BulkWriteOptions
                {
                    BypassDocumentValidation = options.BypassDocumentValidation,
                    Comment = options.Comment
                };
                await bulkWriteAsync(new[] { model }, bulkWriteOptions).ConfigureAwait(false);
            }
            catch (MongoBulkWriteException<TDocument> ex)
            {
                throw MongoWriteException.FromBulkWriteException(ex);
            }
        }

        public virtual void InsertMany(IEnumerable<TDocument> documents, InsertManyOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            InsertMany(documents, options, (requests, bulkWriteOptions) => BulkWrite(requests, bulkWriteOptions, cancellationToken));
        }

        public virtual void InsertMany(IClientSessionHandle session, IEnumerable<TDocument> documents, InsertManyOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            InsertMany(documents, options, (requests, bulkWriteOptions) => BulkWrite(session, requests, bulkWriteOptions, cancellationToken));
        }

        private void InsertMany(IEnumerable<TDocument> documents, InsertManyOptions options, Action<IEnumerable<WriteModel<TDocument>>, BulkWriteOptions> bulkWrite)
        {
            Ensure.IsNotNull(documents, nameof(documents));

            var models = documents.Select(x => new InsertOneModel<TDocument>(x));
            BulkWriteOptions bulkWriteOptions = options == null ? null : new BulkWriteOptions
            {
                BypassDocumentValidation = options.BypassDocumentValidation,
                Comment = options.Comment,
                IsOrdered = options.IsOrdered
            };
            bulkWrite(models, bulkWriteOptions);
        }

        public virtual Task InsertManyAsync(IEnumerable<TDocument> documents, InsertManyOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return InsertManyAsync(documents, options, (requests, bulkWriteOptions) => BulkWriteAsync(requests, bulkWriteOptions, cancellationToken));
        }

        public virtual Task InsertManyAsync(IClientSessionHandle session, IEnumerable<TDocument> documents, InsertManyOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return InsertManyAsync(documents, options, (requests, bulkWriteOptions) => BulkWriteAsync(session, requests, bulkWriteOptions, cancellationToken));
        }

        private Task InsertManyAsync(IEnumerable<TDocument> documents, InsertManyOptions options, Func<IEnumerable<WriteModel<TDocument>>, BulkWriteOptions, Task> bulkWriteAsync)
        {
            Ensure.IsNotNull(documents, nameof(documents));

            var models = documents.Select(x => new InsertOneModel<TDocument>(x));
            var bulkWriteOptions = options == null ? null : new BulkWriteOptions
            {
                BypassDocumentValidation = options.BypassDocumentValidation,
                Comment = options.Comment,
                IsOrdered = options.IsOrdered
            };
            return bulkWriteAsync(models, bulkWriteOptions);
        }

        [Obsolete("Use Aggregation pipeline instead.")]
        public virtual IAsyncCursor<TResult> MapReduce<TResult>(BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TDocument, TResult> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        [Obsolete("Use Aggregation pipeline instead.")]
        public virtual IAsyncCursor<TResult> MapReduce<TResult>(IClientSessionHandle session, BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TDocument, TResult> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        [Obsolete("Use Aggregation pipeline instead.")]
        public abstract Task<IAsyncCursor<TResult>> MapReduceAsync<TResult>(BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TDocument, TResult> options = null, CancellationToken cancellationToken = default(CancellationToken));

        [Obsolete("Use Aggregation pipeline instead.")]
        public virtual Task<IAsyncCursor<TResult>> MapReduceAsync<TResult>(IClientSessionHandle session, BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TDocument, TResult> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public abstract IFilteredMongoCollection<TDerivedDocument> OfType<TDerivedDocument>() where TDerivedDocument : TDocument;

        public virtual ReplaceOneResult ReplaceOne(FilterDefinition<TDocument> filter, TDocument replacement, ReplaceOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ReplaceOne(filter, replacement, options, (requests, bulkWriteOptions) => BulkWrite(requests, bulkWriteOptions, cancellationToken));
        }

        [Obsolete("Use the overload that takes a ReplaceOptions instead of an UpdateOptions.")]
        public virtual ReplaceOneResult ReplaceOne(FilterDefinition<TDocument> filter, TDocument replacement, UpdateOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ReplaceOne(filter, replacement, ReplaceOptions.From(options), (requests, bulkWriteOptions) => BulkWrite(requests, bulkWriteOptions, cancellationToken));
        }

        public virtual ReplaceOneResult ReplaceOne(IClientSessionHandle session, FilterDefinition<TDocument> filter, TDocument replacement, ReplaceOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ReplaceOne(filter, replacement, options, (requests, bulkWriteOptions) => BulkWrite(session, requests, bulkWriteOptions, cancellationToken));
        }

        [Obsolete("Use the overload that takes a ReplaceOptions instead of an UpdateOptions.")]
        public virtual ReplaceOneResult ReplaceOne(IClientSessionHandle session, FilterDefinition<TDocument> filter, TDocument replacement, UpdateOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ReplaceOne(filter, replacement, ReplaceOptions.From(options), (requests, bulkWriteOptions) => BulkWrite(session, requests, bulkWriteOptions, cancellationToken));
        }

        private ReplaceOneResult ReplaceOne(FilterDefinition<TDocument> filter, TDocument replacement, ReplaceOptions options, Func<IEnumerable<WriteModel<TDocument>>, BulkWriteOptions, BulkWriteResult<TDocument>> bulkWrite)
        {
            Ensure.IsNotNull(filter, nameof(filter));
            Ensure.IsNotNull((object)replacement, "replacement");

            options ??= new ReplaceOptions();
            var model = new ReplaceOneModel<TDocument>(filter, replacement)
            {
                Collation = options.Collation,
                Hint = options.Hint,
                IsUpsert = options.IsUpsert
            };

            if (options is ReplaceOptions<TDocument> re)
            {
                model.Sort = re.Sort;
            }

            try
            {
                var bulkWriteOptions = new BulkWriteOptions
                {
                    BypassDocumentValidation = options.BypassDocumentValidation,
                    Comment = options.Comment,
                    Let = options.Let
                };
                var result = bulkWrite(new[] { model }, bulkWriteOptions);
                return ReplaceOneResult.FromCore(result);
            }
            catch (MongoBulkWriteException<TDocument> ex)
            {
                throw MongoWriteException.FromBulkWriteException(ex);
            }
        }

        public virtual Task<ReplaceOneResult> ReplaceOneAsync(FilterDefinition<TDocument> filter, TDocument replacement, ReplaceOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ReplaceOneAsync(filter, replacement, options, (requests, bulkWriteOptions) => BulkWriteAsync(requests, bulkWriteOptions, cancellationToken));
        }

        [Obsolete("Use the overload that takes a ReplaceOptions instead of an UpdateOptions.")]
        public virtual Task<ReplaceOneResult> ReplaceOneAsync(FilterDefinition<TDocument> filter, TDocument replacement, UpdateOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ReplaceOneAsync(filter, replacement, ReplaceOptions.From(options), (requests, bulkWriteOptions) => BulkWriteAsync(requests, bulkWriteOptions, cancellationToken));
        }

        public virtual Task<ReplaceOneResult> ReplaceOneAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, TDocument replacement, ReplaceOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ReplaceOneAsync(filter, replacement, options, (requests, bulkWriteOptions) => BulkWriteAsync(session, requests, bulkWriteOptions, cancellationToken));
        }

        [Obsolete("Use the overload that takes a ReplaceOptions instead of an UpdateOptions.")]
        public virtual Task<ReplaceOneResult> ReplaceOneAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, TDocument replacement, UpdateOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ReplaceOneAsync(filter, replacement, ReplaceOptions.From(options), (requests, bulkWriteOptions) => BulkWriteAsync(session, requests, bulkWriteOptions, cancellationToken));
        }

        private async Task<ReplaceOneResult> ReplaceOneAsync(FilterDefinition<TDocument> filter, TDocument replacement, ReplaceOptions options,
            Func<IEnumerable<WriteModel<TDocument>>, BulkWriteOptions, Task<BulkWriteResult<TDocument>>> bulkWriteAsync)
        {
            Ensure.IsNotNull(filter, nameof(filter));
            Ensure.IsNotNull((object)replacement, "replacement");

            options ??= new ReplaceOptions();
            var model = new ReplaceOneModel<TDocument>(filter, replacement)
            {
                Collation = options.Collation,
                Hint = options.Hint,
                IsUpsert = options.IsUpsert
            };

            if (options is ReplaceOptions<TDocument> re)
            {
                model.Sort = re.Sort;
            }

            try
            {
                var bulkWriteOptions = new BulkWriteOptions
                {
                    BypassDocumentValidation = options.BypassDocumentValidation,
                    Comment = options.Comment,
                    Let = options.Let
                };
                var result = await bulkWriteAsync(new[] { model }, bulkWriteOptions).ConfigureAwait(false);
                return ReplaceOneResult.FromCore(result);
            }
            catch (MongoBulkWriteException<TDocument> ex)
            {
                throw MongoWriteException.FromBulkWriteException(ex);
            }
        }

        public virtual UpdateResult UpdateMany(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UpdateMany(filter, update, options, (requests, bulkWriteOptions) => BulkWrite(requests, bulkWriteOptions, cancellationToken));
        }

        public virtual UpdateResult UpdateMany(IClientSessionHandle session, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UpdateMany(filter, update, options, (requests, bulkWriteOptions) => BulkWrite(session, requests, bulkWriteOptions, cancellationToken));
        }

        private UpdateResult UpdateMany(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions options, Func<IEnumerable<WriteModel<TDocument>>, BulkWriteOptions, BulkWriteResult<TDocument>> bulkWrite)
        {
            Ensure.IsNotNull(filter, nameof(filter));
            Ensure.IsNotNull(update, nameof(update));

            options = options ?? new UpdateOptions();
            var model = new UpdateManyModel<TDocument>(filter, update)
            {
                ArrayFilters = options.ArrayFilters,
                Collation = options.Collation,
                Hint = options.Hint,
                IsUpsert = options.IsUpsert
            };

            try
            {
                var bulkWriteOptions = new BulkWriteOptions
                {
                    BypassDocumentValidation = options.BypassDocumentValidation,
                    Comment = options.Comment,
                    Let = options.Let
                };
                var result = bulkWrite(new[] { model }, bulkWriteOptions);
                return UpdateResult.FromCore(result);
            }
            catch (MongoBulkWriteException<TDocument> ex)
            {
                throw MongoWriteException.FromBulkWriteException(ex);
            }
        }

        public virtual Task<UpdateResult> UpdateManyAsync(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UpdateManyAsync(filter, update, options, (requests, bulkWriteOptions) => BulkWriteAsync(requests, bulkWriteOptions, cancellationToken));
        }

        public virtual Task<UpdateResult> UpdateManyAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UpdateManyAsync(filter, update, options, (requests, bulkWriteOptions) => BulkWriteAsync(session, requests, bulkWriteOptions, cancellationToken));
        }

        private async Task<UpdateResult> UpdateManyAsync(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions options, Func<IEnumerable<WriteModel<TDocument>>, BulkWriteOptions, Task<BulkWriteResult<TDocument>>> bulkWriteAsync)
        {
            Ensure.IsNotNull(filter, nameof(filter));
            Ensure.IsNotNull(update, nameof(update));

            options = options ?? new UpdateOptions();
            var model = new UpdateManyModel<TDocument>(filter, update)
            {
                ArrayFilters = options.ArrayFilters,
                Collation = options.Collation,
                Hint = options.Hint,
                IsUpsert = options.IsUpsert
            };

            try
            {
                var bulkWriteOptions = new BulkWriteOptions
                {
                    BypassDocumentValidation = options.BypassDocumentValidation,
                    Comment = options.Comment,
                    Let = options.Let
                };
                var result = await bulkWriteAsync(new[] { model }, bulkWriteOptions).ConfigureAwait(false);
                return UpdateResult.FromCore(result);
            }
            catch (MongoBulkWriteException<TDocument> ex)
            {
                throw MongoWriteException.FromBulkWriteException(ex);
            }
        }

        public virtual UpdateResult UpdateOne(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UpdateOne(filter, update, options, (requests, bulkWriteOptions) => BulkWrite(requests, bulkWriteOptions, cancellationToken));
        }

        public virtual UpdateResult UpdateOne(IClientSessionHandle session, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UpdateOne(filter, update, options, (requests, bulkWriteOptions) => BulkWrite(session, requests, bulkWriteOptions, cancellationToken));
        }

        private UpdateResult UpdateOne(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions options, Func<IEnumerable<WriteModel<TDocument>>, BulkWriteOptions, BulkWriteResult<TDocument>> bulkWrite)
        {
            Ensure.IsNotNull(filter, nameof(filter));
            Ensure.IsNotNull(update, nameof(update));

            options ??= new UpdateOptions();
            var model = new UpdateOneModel<TDocument>(filter, update)
            {
                ArrayFilters = options.ArrayFilters,
                Collation = options.Collation,
                Hint = options.Hint,
                IsUpsert = options.IsUpsert
            };

            if (options is UpdateOptions<TDocument> uo)
            {
                model.Sort = uo.Sort;
            }

            try
            {
                var bulkWriteOptions = new BulkWriteOptions
                {
                    BypassDocumentValidation = options.BypassDocumentValidation,
                    Comment = options.Comment,
                    Let = options.Let
                };
                var result = bulkWrite(new[] { model }, bulkWriteOptions);
                return UpdateResult.FromCore(result);
            }
            catch (MongoBulkWriteException<TDocument> ex)
            {
                throw MongoWriteException.FromBulkWriteException(ex);
            }
        }

        public virtual Task<UpdateResult> UpdateOneAsync(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UpdateOneAsync(filter, update, options, (requests, bulkWriteOptions) => BulkWriteAsync(requests, bulkWriteOptions, cancellationToken));
        }

        public virtual Task<UpdateResult> UpdateOneAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UpdateOneAsync(filter, update, options, (requests, bulkWriteOptions) => BulkWriteAsync(session, requests, bulkWriteOptions, cancellationToken));
        }

        private async Task<UpdateResult> UpdateOneAsync(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions options, Func<IEnumerable<WriteModel<TDocument>>, BulkWriteOptions, Task<BulkWriteResult<TDocument>>> bulkWriteAsync)
        {
            Ensure.IsNotNull(filter, nameof(filter));
            Ensure.IsNotNull(update, nameof(update));

            options ??= new UpdateOptions();
            var model = new UpdateOneModel<TDocument>(filter, update)
            {
                ArrayFilters = options.ArrayFilters,
                Collation = options.Collation,
                Hint = options.Hint,
                IsUpsert = options.IsUpsert
            };

            if (options is UpdateOptions<TDocument> uo)
            {
                model.Sort = uo.Sort;
            }

            try
            {
                var bulkWriteOptions = new BulkWriteOptions
                {
                    BypassDocumentValidation = options.BypassDocumentValidation,
                    Comment = options.Comment,
                    Let = options.Let
                };
                var result = await bulkWriteAsync(new[] { model }, bulkWriteOptions).ConfigureAwait(false);
                return UpdateResult.FromCore(result);
            }
            catch (MongoBulkWriteException<TDocument> ex)
            {
                throw MongoWriteException.FromBulkWriteException(ex);
            }
        }

        public virtual IChangeStreamCursor<TResult> Watch<TResult>(
            PipelineDefinition<ChangeStreamDocument<TDocument>, TResult> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); // implemented by subclasses
        }

        public virtual IChangeStreamCursor<TResult> Watch<TResult>(
            IClientSessionHandle session,
            PipelineDefinition<ChangeStreamDocument<TDocument>, TResult> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); // implemented by subclasses
        }

        public virtual Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(
            PipelineDefinition<ChangeStreamDocument<TDocument>, TResult> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); // implemented by subclasses
        }

        public virtual Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(
            IClientSessionHandle session,
            PipelineDefinition<ChangeStreamDocument<TDocument>, TResult> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); // implemented by subclasses
        }

        public virtual IMongoCollection<TDocument> WithReadConcern(ReadConcern readConcern)
        {
            throw new NotImplementedException();
        }

        public abstract IMongoCollection<TDocument> WithReadPreference(ReadPreference readPreference);

        public abstract IMongoCollection<TDocument> WithWriteConcern(WriteConcern writeConcern);
    }
}
