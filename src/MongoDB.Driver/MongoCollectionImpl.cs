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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.Driver.Search;

namespace MongoDB.Driver
{
    internal sealed class MongoCollectionImpl<TDocument> : MongoCollectionBase<TDocument>, IMongoCollection
    {
        // fields
        private readonly IClusterInternal _cluster;
        private readonly CollectionNamespace _collectionNamespace;
        private readonly IMongoDatabase _database;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly IOperationExecutor _operationExecutor;
        private readonly IBsonSerializer<TDocument> _documentSerializer;
        private readonly MongoCollectionSettings _settings;

        // constructors
        public MongoCollectionImpl(IMongoDatabase database, CollectionNamespace collectionNamespace, MongoCollectionSettings settings, IClusterInternal cluster, IOperationExecutor operationExecutor)
            : this(database, collectionNamespace, settings, cluster, operationExecutor, Ensure.IsNotNull(settings, "settings").SerializerRegistry.GetSerializer<TDocument>())
        {
        }

        private MongoCollectionImpl(IMongoDatabase database, CollectionNamespace collectionNamespace, MongoCollectionSettings settings, IClusterInternal cluster, IOperationExecutor operationExecutor, IBsonSerializer<TDocument> documentSerializer)
        {
            _database = Ensure.IsNotNull(database, nameof(database));
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
            _settings = Ensure.IsNotNull(settings, nameof(settings)).Freeze();
            _cluster = Ensure.IsNotNull(cluster, nameof(cluster));
            _operationExecutor = Ensure.IsNotNull(operationExecutor, nameof(operationExecutor));
            _documentSerializer = Ensure.IsNotNull(documentSerializer, nameof(documentSerializer));

            _messageEncoderSettings = GetMessageEncoderSettings();
        }

        // properties
        public override CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        public override IMongoDatabase Database
        {
            get { return _database; }
        }

        public override IBsonSerializer<TDocument> DocumentSerializer
        {
            get { return _documentSerializer; }
        }

        IBsonSerializer IMongoCollection.DocumentSerializer => _documentSerializer;

        public override IMongoIndexManager<TDocument> Indexes
        {
            get { return new MongoIndexManager(this); }
        }

        public override IMongoSearchIndexManager SearchIndexes
        {
            get { return new MongoSearchIndexManager(this); }
        }

        public override MongoCollectionSettings Settings
        {
            get { return _settings; }
        }

        // public methods
        public override IAsyncCursor<TResult> Aggregate<TResult>(PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UsingImplicitSession(session => Aggregate(session, pipeline, options, cancellationToken), cancellationToken);
        }

        public override IAsyncCursor<TResult> Aggregate<TResult>(IClientSessionHandle session, PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(pipeline, nameof(pipeline));
            options = options ?? new AggregateOptions();

            var renderArgs = GetRenderArgs(options.TranslationOptions);
            var renderedPipeline = pipeline.Render(renderArgs);

            var lastStage = renderedPipeline.Documents.LastOrDefault();
            var lastStageName = lastStage?.GetElement(0).Name;
            if (lastStage != null && (lastStageName == "$out" || lastStageName == "$merge"))
            {
                var aggregateOperation = CreateAggregateToCollectionOperation(renderedPipeline, options);
                ExecuteWriteOperation(session, aggregateOperation, cancellationToken);

                // we want to delay execution of the find because the user may
                // not want to iterate the results at all...
                var findOperation = CreateAggregateToCollectionFindOperation(lastStage, renderedPipeline.OutputSerializer, options);
                var forkedSession = session.Fork();
                var deferredCursor = new DeferredAsyncCursor<TResult>(
                    () => forkedSession.Dispose(),
                    ct => ExecuteReadOperation(forkedSession, findOperation, ReadPreference.Primary, ct),
                    ct => ExecuteReadOperationAsync(forkedSession, findOperation, ReadPreference.Primary, ct));
                return deferredCursor;
            }
            else
            {
                var aggregateOperation = CreateAggregateOperation(renderedPipeline, options);
                return ExecuteReadOperation(session, aggregateOperation, cancellationToken);
            }
        }

        public override Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UsingImplicitSessionAsync(session => AggregateAsync(session, pipeline, options, cancellationToken), cancellationToken);
        }

        public override async Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(IClientSessionHandle session, PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(pipeline, nameof(pipeline));
            options = options ?? new AggregateOptions();

            var renderArgs = GetRenderArgs(options.TranslationOptions);
            var renderedPipeline = pipeline.Render(renderArgs);

            var lastStage = renderedPipeline.Documents.LastOrDefault();
            var lastStageName = lastStage?.GetElement(0).Name;
            if (lastStage != null && (lastStageName == "$out" || lastStageName == "$merge"))
            {
                var aggregateOperation = CreateAggregateToCollectionOperation(renderedPipeline, options);
                await ExecuteWriteOperationAsync(session, aggregateOperation, cancellationToken).ConfigureAwait(false);

                // we want to delay execution of the find because the user may
                // not want to iterate the results at all...
                var findOperation = CreateAggregateToCollectionFindOperation(lastStage, renderedPipeline.OutputSerializer, options);
                var forkedSession = session.Fork();
                var deferredCursor = new DeferredAsyncCursor<TResult>(
                    () => forkedSession.Dispose(),
                    ct => ExecuteReadOperation(forkedSession, findOperation, ReadPreference.Primary, ct),
                    ct => ExecuteReadOperationAsync(forkedSession, findOperation, ReadPreference.Primary, ct));
                return await Task.FromResult<IAsyncCursor<TResult>>(deferredCursor).ConfigureAwait(false);
            }
            else
            {
                var aggregateOperation = CreateAggregateOperation(renderedPipeline, options);
                return await ExecuteReadOperationAsync(session, aggregateOperation, cancellationToken).ConfigureAwait(false);
            }
        }

        public override void AggregateToCollection<TResult>(PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            UsingImplicitSession(session => AggregateToCollection(session, pipeline, options, cancellationToken), cancellationToken);
        }

        public override void AggregateToCollection<TResult>(IClientSessionHandle session, PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(pipeline, nameof(pipeline));
            options = options ?? new AggregateOptions();

            var renderArgs = GetRenderArgs(options.TranslationOptions);
            var renderedPipeline = pipeline.Render(renderArgs);

            var lastStage = renderedPipeline.Documents.LastOrDefault();
            var lastStageName = lastStage?.GetElement(0).Name;
            if (lastStage == null || (lastStageName != "$out" && lastStageName != "$merge"))
            {
                throw new InvalidOperationException("AggregateToCollection requires that the last stage be $out or $merge.");
            }

            var aggregateOperation = CreateAggregateToCollectionOperation(renderedPipeline, options);
            ExecuteWriteOperation(session, aggregateOperation, cancellationToken);
        }

        public override Task AggregateToCollectionAsync<TResult>(PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UsingImplicitSessionAsync(session => AggregateToCollectionAsync(session, pipeline, options, cancellationToken), cancellationToken);
        }

        public override async Task AggregateToCollectionAsync<TResult>(IClientSessionHandle session, PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(pipeline, nameof(pipeline));
            options = options ?? new AggregateOptions();

            var renderArgs = GetRenderArgs(options.TranslationOptions);
            var renderedPipeline = pipeline.Render(renderArgs);

            var lastStage = renderedPipeline.Documents.LastOrDefault();
            var lastStageName = lastStage?.GetElement(0).Name;
            if (lastStage == null || (lastStageName != "$out" && lastStageName != "$merge"))
            {
                throw new InvalidOperationException("AggregateToCollectionAsync requires that the last stage be $out or $merge.");
            }

            var aggregateOperation = CreateAggregateToCollectionOperation(renderedPipeline, options);
            await ExecuteWriteOperationAsync(session, aggregateOperation, cancellationToken).ConfigureAwait(false);
        }

        public override BulkWriteResult<TDocument> BulkWrite(IEnumerable<WriteModel<TDocument>> requests, BulkWriteOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UsingImplicitSession(session => BulkWrite(session, requests, options, cancellationToken), cancellationToken);
        }

        public override BulkWriteResult<TDocument> BulkWrite(IClientSessionHandle session, IEnumerable<WriteModel<TDocument>> requests, BulkWriteOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull((object)requests, nameof(requests));

            var requestsArray = requests.ToArray();
            if (requestsArray.Length == 0)
            {
                throw new ArgumentException("Must contain at least 1 request.", nameof(requests));
            }

            foreach (var request in requestsArray)
            {
                request.ThrowIfNotValid();
            }

            options = options ?? new BulkWriteOptions();

            var renderArgs = GetRenderArgs();
            var operation = CreateBulkWriteOperation(session, requestsArray, options, renderArgs);
            try
            {
                var result = ExecuteWriteOperation(session, operation, cancellationToken);
                return BulkWriteResult<TDocument>.FromCore(result, requestsArray);
            }
            catch (MongoBulkWriteOperationException ex)
            {
                throw MongoBulkWriteException<TDocument>.FromCore(ex, requestsArray);
            }
        }

        public override Task<BulkWriteResult<TDocument>> BulkWriteAsync(IEnumerable<WriteModel<TDocument>> requests, BulkWriteOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UsingImplicitSessionAsync(session => BulkWriteAsync(session, requests, options, cancellationToken), cancellationToken);
        }

        public override async Task<BulkWriteResult<TDocument>> BulkWriteAsync(IClientSessionHandle session, IEnumerable<WriteModel<TDocument>> requests, BulkWriteOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull((object)requests, nameof(requests));

            var requestsArray = requests.ToArray();
            if (requestsArray.Length == 0)
            {
                throw new ArgumentException("Must contain at least 1 request.", nameof(requests));
            }

            foreach (var request in requestsArray)
            {
                request.ThrowIfNotValid();
            }

            options = options ?? new BulkWriteOptions();

            var renderArgs = GetRenderArgs();
            var operation = CreateBulkWriteOperation(session, requestsArray, options, renderArgs);
            try
            {
                var result = await ExecuteWriteOperationAsync(session, operation, cancellationToken).ConfigureAwait(false);
                return BulkWriteResult<TDocument>.FromCore(result, requestsArray);
            }
            catch (MongoBulkWriteOperationException ex)
            {
                throw MongoBulkWriteException<TDocument>.FromCore(ex, requestsArray);
            }
        }

        [Obsolete("Use CountDocuments or EstimatedDocumentCount instead.")]
        public override long Count(FilterDefinition<TDocument> filter, CountOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UsingImplicitSession(session => Count(session, filter, options, cancellationToken), cancellationToken);
        }

        [Obsolete("Use CountDocuments or EstimatedDocumentCount instead.")]
        public override long Count(IClientSessionHandle session, FilterDefinition<TDocument> filter, CountOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(filter, nameof(filter));
            options = options ?? new CountOptions();

            var renderArgs = GetRenderArgs();
            var operation = CreateCountOperation(filter, options, renderArgs);
            return ExecuteReadOperation(session, operation, cancellationToken);
        }

        [Obsolete("Use CountDocumentsAsync or EstimatedDocumentCountAsync instead.")]
        public override Task<long> CountAsync(FilterDefinition<TDocument> filter, CountOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UsingImplicitSessionAsync(session => CountAsync(session, filter, options, cancellationToken), cancellationToken);
        }

        [Obsolete("Use CountDocumentsAsync or EstimatedDocumentCountAsync instead.")]
        public override Task<long> CountAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, CountOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(filter, nameof(filter));
            options = options ?? new CountOptions();

            var renderArgs = GetRenderArgs();
            var operation = CreateCountOperation(filter, options, renderArgs);
            return ExecuteReadOperationAsync(session, operation, cancellationToken);
        }

        public override long CountDocuments(FilterDefinition<TDocument> filter, CountOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UsingImplicitSession(session => CountDocuments(session, filter, options, cancellationToken), cancellationToken);
        }

        public override long CountDocuments(IClientSessionHandle session, FilterDefinition<TDocument> filter, CountOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(filter, nameof(filter));
            options = options ?? new CountOptions();

            var renderArgs = GetRenderArgs();
            var operation = CreateCountDocumentsOperation(filter, options, renderArgs);
            return ExecuteReadOperation(session, operation, cancellationToken);
        }

        public override Task<long> CountDocumentsAsync(FilterDefinition<TDocument> filter, CountOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UsingImplicitSessionAsync(session => CountDocumentsAsync(session, filter, options, cancellationToken), cancellationToken);
        }

        public override Task<long> CountDocumentsAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, CountOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(filter, nameof(filter));
            options = options ?? new CountOptions();

            var renderArgs = GetRenderArgs();
            var operation = CreateCountDocumentsOperation(filter, options, renderArgs);
            return ExecuteReadOperationAsync(session, operation, cancellationToken);
        }

        public override IAsyncCursor<TField> Distinct<TField>(FieldDefinition<TDocument, TField> field, FilterDefinition<TDocument> filter, DistinctOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UsingImplicitSession(session => Distinct(session, field, filter, options, cancellationToken), cancellationToken);
        }

        public override IAsyncCursor<TField> Distinct<TField>(IClientSessionHandle session, FieldDefinition<TDocument, TField> field, FilterDefinition<TDocument> filter, DistinctOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(field, nameof(field));
            Ensure.IsNotNull(filter, nameof(filter));
            options = options ?? new DistinctOptions();

            var renderArgs = GetRenderArgs();
            var operation = CreateDistinctOperation(field, filter, options, renderArgs);
            return ExecuteReadOperation(session, operation, cancellationToken);
        }

        public override Task<IAsyncCursor<TField>> DistinctAsync<TField>(FieldDefinition<TDocument, TField> field, FilterDefinition<TDocument> filter, DistinctOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UsingImplicitSessionAsync(session => DistinctAsync(session, field, filter, options, cancellationToken), cancellationToken);
        }

        public override Task<IAsyncCursor<TField>> DistinctAsync<TField>(IClientSessionHandle session, FieldDefinition<TDocument, TField> field, FilterDefinition<TDocument> filter, DistinctOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(field, nameof(field));
            Ensure.IsNotNull(filter, nameof(filter));
            options = options ?? new DistinctOptions();

            var renderArgs = GetRenderArgs();
            var operation = CreateDistinctOperation(field, filter, options, renderArgs);
            return ExecuteReadOperationAsync(session, operation, cancellationToken);
        }

        public override IAsyncCursor<TItem> DistinctMany<TItem>(FieldDefinition<TDocument, IEnumerable<TItem>> field, FilterDefinition<TDocument> filter, DistinctOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UsingImplicitSession(session => DistinctMany(session, field, filter, options, cancellationToken), cancellationToken);
        }

        public override IAsyncCursor<TItem> DistinctMany<TItem>(IClientSessionHandle session, FieldDefinition<TDocument, IEnumerable<TItem>> field, FilterDefinition<TDocument> filter, DistinctOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(field, nameof(field));
            Ensure.IsNotNull(filter, nameof(filter));
            options = options ?? new DistinctOptions();

            var renderArgs = GetRenderArgs();
            var operation = CreateDistinctManyOperation(field, filter, options, renderArgs);
            return ExecuteReadOperation(session, operation, cancellationToken);
        }

        public override Task<IAsyncCursor<TItem>> DistinctManyAsync<TItem>(FieldDefinition<TDocument, IEnumerable<TItem>> field, FilterDefinition<TDocument> filter, DistinctOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UsingImplicitSessionAsync(session => DistinctManyAsync(session, field, filter, options, cancellationToken), cancellationToken);
        }

        public override Task<IAsyncCursor<TItem>> DistinctManyAsync<TItem>(IClientSessionHandle session, FieldDefinition<TDocument, IEnumerable<TItem>> field, FilterDefinition<TDocument> filter, DistinctOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(field, nameof(field));
            Ensure.IsNotNull(filter, nameof(filter));
            options = options ?? new DistinctOptions();

            var renderArgs = GetRenderArgs();
            var operation = CreateDistinctManyOperation(field, filter, options, renderArgs);
            return ExecuteReadOperationAsync(session, operation, cancellationToken);
        }

        public override long EstimatedDocumentCount(EstimatedDocumentCountOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UsingImplicitSession(session =>
            {
                var operation = CreateEstimatedDocumentCountOperation(options);
                return ExecuteReadOperation(session, operation, cancellationToken);
            });
        }

        public override Task<long> EstimatedDocumentCountAsync(EstimatedDocumentCountOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UsingImplicitSessionAsync(session =>
            {
                var operation = CreateEstimatedDocumentCountOperation(options);
                return ExecuteReadOperationAsync(session, operation, cancellationToken);
            });
        }

        public override IAsyncCursor<TProjection> FindSync<TProjection>(FilterDefinition<TDocument> filter, FindOptions<TDocument, TProjection> options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UsingImplicitSession(session => FindSync(session, filter, options, cancellationToken), cancellationToken);
        }

        public override IAsyncCursor<TProjection> FindSync<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, FindOptions<TDocument, TProjection> options, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(filter, nameof(filter));
            options = options ?? new FindOptions<TDocument, TProjection>();

            var renderArgs = GetRenderArgs(options.TranslationOptions);
            var operation = CreateFindOperation<TProjection>(filter, options, renderArgs);
            return ExecuteReadOperation(session, operation, cancellationToken);
        }

        public override Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(FilterDefinition<TDocument> filter, FindOptions<TDocument, TProjection> options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UsingImplicitSessionAsync(session => FindAsync(session, filter, options, cancellationToken), cancellationToken);
        }

        public override Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, FindOptions<TDocument, TProjection> options, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(filter, nameof(filter));
            options = options ?? new FindOptions<TDocument, TProjection>();

            var renderArgs = GetRenderArgs(options.TranslationOptions);
            var operation = CreateFindOperation<TProjection>(filter, options, renderArgs);
            return ExecuteReadOperationAsync(session, operation, cancellationToken);
        }

        public override TProjection FindOneAndDelete<TProjection>(FilterDefinition<TDocument> filter, FindOneAndDeleteOptions<TDocument, TProjection> options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UsingImplicitSession(session => FindOneAndDelete(session, filter, options, cancellationToken), cancellationToken);
        }

        public override TProjection FindOneAndDelete<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, FindOneAndDeleteOptions<TDocument, TProjection> options, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(filter, nameof(filter));
            options = options ?? new FindOneAndDeleteOptions<TDocument, TProjection>();

            var renderArgs = GetRenderArgs();
            var operation = CreateFindOneAndDeleteOperation<TProjection>(filter, options, renderArgs);
            return ExecuteWriteOperation(session, operation, cancellationToken);
        }

        public override Task<TProjection> FindOneAndDeleteAsync<TProjection>(FilterDefinition<TDocument> filter, FindOneAndDeleteOptions<TDocument, TProjection> options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UsingImplicitSessionAsync(session => FindOneAndDeleteAsync(session, filter, options, cancellationToken), cancellationToken);
        }

        public override Task<TProjection> FindOneAndDeleteAsync<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, FindOneAndDeleteOptions<TDocument, TProjection> options, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(filter, nameof(filter));
            options = options ?? new FindOneAndDeleteOptions<TDocument, TProjection>();

            var renderArgs = GetRenderArgs();
            var operation = CreateFindOneAndDeleteOperation<TProjection>(filter, options, renderArgs);
            return ExecuteWriteOperationAsync(session, operation, cancellationToken);
        }

        public override TProjection FindOneAndReplace<TProjection>(FilterDefinition<TDocument> filter, TDocument replacement, FindOneAndReplaceOptions<TDocument, TProjection> options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UsingImplicitSession(session => FindOneAndReplace(session, filter, replacement, options, cancellationToken), cancellationToken);
        }

        public override TProjection FindOneAndReplace<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, TDocument replacement, FindOneAndReplaceOptions<TDocument, TProjection> options, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(filter, nameof(filter));
            var replacementObject = Ensure.IsNotNull((object)replacement, nameof(replacement)); // only box once if it's a struct
            options = options ?? new FindOneAndReplaceOptions<TDocument, TProjection>();

            var renderArgs = GetRenderArgs();
            var operation = CreateFindOneAndReplaceOperation(filter, replacementObject, options, renderArgs);
            return ExecuteWriteOperation(session, operation, cancellationToken);
        }

        public override Task<TProjection> FindOneAndReplaceAsync<TProjection>(FilterDefinition<TDocument> filter, TDocument replacement, FindOneAndReplaceOptions<TDocument, TProjection> options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UsingImplicitSessionAsync(session => FindOneAndReplaceAsync(session, filter, replacement, options, cancellationToken), cancellationToken);
        }

        public override Task<TProjection> FindOneAndReplaceAsync<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, TDocument replacement, FindOneAndReplaceOptions<TDocument, TProjection> options, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(filter, nameof(filter));
            var replacementObject = Ensure.IsNotNull((object)replacement, nameof(replacement)); // only box once if it's a struct
            options = options ?? new FindOneAndReplaceOptions<TDocument, TProjection>();

            var renderArgs = GetRenderArgs();
            var operation = CreateFindOneAndReplaceOperation(filter, replacementObject, options, renderArgs);
            return ExecuteWriteOperationAsync(session, operation, cancellationToken);
        }

        public override TProjection FindOneAndUpdate<TProjection>(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, FindOneAndUpdateOptions<TDocument, TProjection> options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UsingImplicitSession(session => FindOneAndUpdate(session, filter, update, options, cancellationToken), cancellationToken);
        }

        public override TProjection FindOneAndUpdate<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, FindOneAndUpdateOptions<TDocument, TProjection> options, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(filter, nameof(filter));
            Ensure.IsNotNull(update, nameof(update));
            options = options ?? new FindOneAndUpdateOptions<TDocument, TProjection>();

            if (update is PipelineUpdateDefinition<TDocument> && (options.ArrayFilters != null && options.ArrayFilters.Any()))
            {
                throw new NotSupportedException("An arrayfilter is not supported in the pipeline-style update.");
            }

            var renderArgs = GetRenderArgs();
            var operation = CreateFindOneAndUpdateOperation(filter, update, options, renderArgs);
            return ExecuteWriteOperation(session, operation, cancellationToken);
        }

        public override Task<TProjection> FindOneAndUpdateAsync<TProjection>(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, FindOneAndUpdateOptions<TDocument, TProjection> options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UsingImplicitSessionAsync(session => FindOneAndUpdateAsync(session, filter, update, options, cancellationToken), cancellationToken);
        }

        public override Task<TProjection> FindOneAndUpdateAsync<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, FindOneAndUpdateOptions<TDocument, TProjection> options, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(filter, nameof(filter));
            Ensure.IsNotNull(update, nameof(update));
            options = options ?? new FindOneAndUpdateOptions<TDocument, TProjection>();

            if (update is PipelineUpdateDefinition<TDocument> && (options.ArrayFilters != null && options.ArrayFilters.Any()))
            {
                throw new NotSupportedException("An arrayfilter is not supported in the pipeline-style update.");
            }

            var renderArgs = GetRenderArgs();
            var operation = CreateFindOneAndUpdateOperation(filter, update, options, renderArgs);
            return ExecuteWriteOperationAsync(session, operation, cancellationToken);
        }

        [Obsolete("Use Aggregation pipeline instead.")]
        public override IAsyncCursor<TResult> MapReduce<TResult>(BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TDocument, TResult> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UsingImplicitSession(session => MapReduce(session, map, reduce, options, cancellationToken), cancellationToken);
        }

        [Obsolete("Use Aggregation pipeline instead.")]
        public override IAsyncCursor<TResult> MapReduce<TResult>(IClientSessionHandle session, BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TDocument, TResult> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(map, nameof(map));
            Ensure.IsNotNull(reduce, nameof(reduce));
            options = options ?? new MapReduceOptions<TDocument, TResult>();

            var outputOptions = options.OutputOptions ?? MapReduceOutputOptions.Inline;
            var resultSerializer = ResolveResultSerializer<TResult>(options.ResultSerializer);

            var renderArgs = GetRenderArgs();
            if (outputOptions == MapReduceOutputOptions.Inline)
            {
                var operation = CreateMapReduceOperation(map, reduce, options, resultSerializer, renderArgs);
                return ExecuteReadOperation(session, operation, cancellationToken);
            }
            else
            {
                var mapReduceOperation = CreateMapReduceOutputToCollectionOperation(map, reduce, options, outputOptions, renderArgs);
                ExecuteWriteOperation(session, mapReduceOperation, cancellationToken);

                // we want to delay execution of the find because the user may
                // not want to iterate the results at all...
                var findOperation = CreateMapReduceOutputToCollectionFindOperation<TResult>(options, mapReduceOperation.OutputCollectionNamespace, resultSerializer);
                var forkedSession = session.Fork();
                var deferredCursor = new DeferredAsyncCursor<TResult>(
                    () => forkedSession.Dispose(),
                    ct => ExecuteReadOperation(forkedSession, findOperation, ReadPreference.Primary, ct),
                    ct => ExecuteReadOperationAsync(forkedSession, findOperation, ReadPreference.Primary, ct));
                return deferredCursor;
            }
        }

        [Obsolete("Use Aggregation pipeline instead.")]
        public override Task<IAsyncCursor<TResult>> MapReduceAsync<TResult>(BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TDocument, TResult> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UsingImplicitSessionAsync(session => MapReduceAsync(session, map, reduce, options, cancellationToken), cancellationToken);
        }

        [Obsolete("Use Aggregation pipeline instead.")]
        public override async Task<IAsyncCursor<TResult>> MapReduceAsync<TResult>(IClientSessionHandle session, BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TDocument, TResult> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(map, nameof(map));
            Ensure.IsNotNull(reduce, nameof(reduce));
            options = options ?? new MapReduceOptions<TDocument, TResult>();

            var outputOptions = options.OutputOptions ?? MapReduceOutputOptions.Inline;
            var resultSerializer = ResolveResultSerializer<TResult>(options.ResultSerializer);

            var renderArgs = GetRenderArgs();
            if (outputOptions == MapReduceOutputOptions.Inline)
            {
                var operation = CreateMapReduceOperation(map, reduce, options, resultSerializer, renderArgs);
                return await ExecuteReadOperationAsync(session, operation, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var mapReduceOperation = CreateMapReduceOutputToCollectionOperation(map, reduce, options, outputOptions, renderArgs);
                await ExecuteWriteOperationAsync(session, mapReduceOperation, cancellationToken).ConfigureAwait(false);

                // we want to delay execution of the find because the user may
                // not want to iterate the results at all...
                var findOperation = CreateMapReduceOutputToCollectionFindOperation<TResult>(options, mapReduceOperation.OutputCollectionNamespace, resultSerializer);
                var forkedSession = session.Fork();
                var deferredCursor = new DeferredAsyncCursor<TResult>(
                    () => forkedSession.Dispose(),
                    ct => ExecuteReadOperation(forkedSession, findOperation, ReadPreference.Primary, ct),
                    ct => ExecuteReadOperationAsync(forkedSession, findOperation, ReadPreference.Primary, ct));
                return await Task.FromResult(deferredCursor).ConfigureAwait(false);
            }
        }

        public override IFilteredMongoCollection<TDerivedDocument> OfType<TDerivedDocument>()
        {
            var derivedDocumentSerializer = _settings.SerializerRegistry.GetSerializer<TDerivedDocument>();
            var ofTypeSerializer = new OfTypeSerializer<TDocument, TDerivedDocument>(derivedDocumentSerializer);
            var derivedDocumentCollection = new MongoCollectionImpl<TDerivedDocument>(_database, _collectionNamespace, _settings, _cluster, _operationExecutor, ofTypeSerializer);

            var rootOfTypeFilter = Builders<TDocument>.Filter.OfType<TDerivedDocument>();
            var renderArgs = GetRenderArgs();
            var renderedOfTypeFilter = rootOfTypeFilter.Render(renderArgs);
            var ofTypeFilter = new BsonDocumentFilterDefinition<TDerivedDocument>(renderedOfTypeFilter);

            return new OfTypeMongoCollection<TDocument, TDerivedDocument>(this, derivedDocumentCollection, ofTypeFilter);
        }

        public override IChangeStreamCursor<TResult> Watch<TResult>(
            PipelineDefinition<ChangeStreamDocument<TDocument>, TResult> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return UsingImplicitSession(session => Watch(session, pipeline, options, cancellationToken), cancellationToken);
        }

        public override IChangeStreamCursor<TResult> Watch<TResult>(
            IClientSessionHandle session,
            PipelineDefinition<ChangeStreamDocument<TDocument>, TResult> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(pipeline, nameof(pipeline));
            var translationOptions = _database.Client.Settings.TranslationOptions;
            var operation = CreateChangeStreamOperation(pipeline, options, translationOptions);
            return ExecuteReadOperation(session, operation, cancellationToken);
        }

        public override Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(
            PipelineDefinition<ChangeStreamDocument<TDocument>, TResult> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return UsingImplicitSessionAsync(session => WatchAsync(session, pipeline, options, cancellationToken), cancellationToken);
        }

        public override Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(
            IClientSessionHandle session,
            PipelineDefinition<ChangeStreamDocument<TDocument>, TResult> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(session, nameof(session));
            Ensure.IsNotNull(pipeline, nameof(pipeline));
            var translationOptions = _database.Client.Settings.TranslationOptions;
            var operation = CreateChangeStreamOperation(pipeline, options, translationOptions);
            return ExecuteReadOperationAsync(session, operation, cancellationToken);
        }

        public override IMongoCollection<TDocument> WithReadConcern(ReadConcern readConcern)
        {
            var newSettings = _settings.Clone();
            newSettings.ReadConcern = readConcern;
            return new MongoCollectionImpl<TDocument>(_database, _collectionNamespace, newSettings, _cluster, _operationExecutor);
        }

        public override IMongoCollection<TDocument> WithReadPreference(ReadPreference readPreference)
        {
            var newSettings = _settings.Clone();
            newSettings.ReadPreference = readPreference;
            return new MongoCollectionImpl<TDocument>(_database, _collectionNamespace, newSettings, _cluster, _operationExecutor);
        }

        public override IMongoCollection<TDocument> WithWriteConcern(WriteConcern writeConcern)
        {
            var newSettings = _settings.Clone();
            newSettings.WriteConcern = writeConcern;
            return new MongoCollectionImpl<TDocument>(_database, _collectionNamespace, newSettings, _cluster, _operationExecutor);
        }

        // private methods
        private WriteRequest ConvertWriteModelToWriteRequest(WriteModel<TDocument> model, int index, RenderArgs<TDocument> renderArgs)
        {
            switch (model.ModelType)
            {
                case WriteModelType.InsertOne:
                    var insertOneModel = (InsertOneModel<TDocument>)model;
                    if (_settings.AssignIdOnInsert)
                    {
                        _documentSerializer.SetDocumentIdIfMissing(this, insertOneModel.Document);
                    }
                    return new InsertRequest(new BsonDocumentWrapper(insertOneModel.Document, _documentSerializer))
                    {
                        CorrelationId = index
                    };
                case WriteModelType.DeleteMany:
                    var deleteManyModel = (DeleteManyModel<TDocument>)model;
                    return new DeleteRequest(deleteManyModel.Filter.Render(renderArgs))
                    {
                        CorrelationId = index,
                        Collation = deleteManyModel.Collation,
                        Hint = deleteManyModel.Hint,
                        Limit = 0
                    };
                case WriteModelType.DeleteOne:
                    var deleteOneModel = (DeleteOneModel<TDocument>)model;
                    return new DeleteRequest(deleteOneModel.Filter.Render(renderArgs))
                    {
                        CorrelationId = index,
                        Collation = deleteOneModel.Collation,
                        Hint = deleteOneModel.Hint,
                        Limit = 1
                    };
                case WriteModelType.ReplaceOne:
                    var replaceOneModel = (ReplaceOneModel<TDocument>)model;
                    return new UpdateRequest(
                        UpdateType.Replacement,
                        replaceOneModel.Filter.Render(renderArgs),
                        new BsonDocumentWrapper(replaceOneModel.Replacement, _documentSerializer))
                    {
                        Collation = replaceOneModel.Collation,
                        CorrelationId = index,
                        Hint = replaceOneModel.Hint,
                        IsMulti = false,
                        IsUpsert = replaceOneModel.IsUpsert,
                        Sort = replaceOneModel.Sort?.Render(renderArgs)
                    };
                case WriteModelType.UpdateMany:
                    var updateManyModel = (UpdateManyModel<TDocument>)model;
                    return new UpdateRequest(
                        UpdateType.Update,
                        updateManyModel.Filter.Render(renderArgs),
                        updateManyModel.Update.Render(renderArgs))
                    {
                        ArrayFilters = RenderArrayFilters(updateManyModel.ArrayFilters),
                        Collation = updateManyModel.Collation,
                        CorrelationId = index,
                        Hint = updateManyModel.Hint,
                        IsMulti = true,
                        IsUpsert = updateManyModel.IsUpsert
                    };
                case WriteModelType.UpdateOne:
                    var updateOneModel = (UpdateOneModel<TDocument>)model;
                    return new UpdateRequest(
                        UpdateType.Update,
                        updateOneModel.Filter.Render(renderArgs),
                        updateOneModel.Update.Render(renderArgs))
                    {
                        ArrayFilters = RenderArrayFilters(updateOneModel.ArrayFilters),
                        Collation = updateOneModel.Collation,
                        CorrelationId = index,
                        Hint = updateOneModel.Hint,
                        IsMulti = false,
                        IsUpsert = updateOneModel.IsUpsert,
                        Sort = updateOneModel.Sort?.Render(renderArgs)
                    };
                default:
                    throw new InvalidOperationException("Unknown type of WriteModel provided.");
            }
        }

        private AggregateOperation<TResult> CreateAggregateOperation<TResult>(RenderedPipelineDefinition<TResult> renderedPipeline, AggregateOptions options)
        {
            return new AggregateOperation<TResult>(
                _collectionNamespace,
                renderedPipeline.Documents,
                renderedPipeline.OutputSerializer,
                _messageEncoderSettings)
            {
                AllowDiskUse = options.AllowDiskUse,
                BatchSize = options.BatchSize,
                Collation = options.Collation,
                Comment = options.Comment,
                Hint = options.Hint,
                Let = options.Let,
                MaxAwaitTime = options.MaxAwaitTime,
                MaxTime = options.MaxTime,
                ReadConcern = _settings.ReadConcern,
                RetryRequested = _database.Client.Settings.RetryReads,
#pragma warning disable 618
                UseCursor = options.UseCursor
#pragma warning restore 618
            };
        }

        private FindOperation<TResult> CreateAggregateToCollectionFindOperation<TResult>(BsonDocument outStage, IBsonSerializer<TResult> resultSerializer, AggregateOptions options)
        {
            CollectionNamespace outputCollectionNamespace;
            var stageName = outStage.GetElement(0).Name;
            switch (stageName)
            {
                case "$out":
                    {
                        var outValue = outStage[0];
                        DatabaseNamespace outputDatabaseNamespace;
                        string outputCollectionName;
                        if (outValue.IsString)
                        {
                            outputDatabaseNamespace = _collectionNamespace.DatabaseNamespace;
                            outputCollectionName = outValue.AsString;
                        }
                        else
                        {
                            outputDatabaseNamespace = new DatabaseNamespace(outValue["db"].AsString);
                            outputCollectionName = outValue["coll"].AsString;
                        }
                        outputCollectionNamespace = new CollectionNamespace(outputDatabaseNamespace, outputCollectionName);
                    }
                    break;
                case "$merge":
                    {
                        var mergeArguments = outStage[0];
                        DatabaseNamespace outputDatabaseNamespace;
                        string outputCollectionName;
                        if (mergeArguments.IsString)
                        {
                            outputDatabaseNamespace = _collectionNamespace.DatabaseNamespace;
                            outputCollectionName = mergeArguments.AsString;
                        }
                        else
                        {
                            var into = mergeArguments.AsBsonDocument["into"];
                            if (into.IsString)
                            {
                                outputDatabaseNamespace = _collectionNamespace.DatabaseNamespace;
                                outputCollectionName = into.AsString;
                            }
                            else
                            {
                                if (into.AsBsonDocument.Contains("db"))
                                {
                                    outputDatabaseNamespace = new DatabaseNamespace(into["db"].AsString);
                                }
                                else
                                {
                                    outputDatabaseNamespace = _collectionNamespace.DatabaseNamespace;
                                }
                                outputCollectionName = into["coll"].AsString;
                            }
                        }
                        outputCollectionNamespace = new CollectionNamespace(outputDatabaseNamespace, outputCollectionName);
                    }
                    break;
                default:
                    throw new ArgumentException($"Unexpected stage name: {stageName}.");
            }

            return new FindOperation<TResult>(
                outputCollectionNamespace,
                resultSerializer,
                _messageEncoderSettings)
            {
                BatchSize = options.BatchSize,
                Collation = options.Collation,
                MaxTime = options.MaxTime,
                ReadConcern = _settings.ReadConcern,
                RetryRequested = _database.Client.Settings.RetryReads
            };
        }

        private AggregateToCollectionOperation CreateAggregateToCollectionOperation<TResult>(RenderedPipelineDefinition<TResult> renderedPipeline, AggregateOptions options)
        {
            return new AggregateToCollectionOperation(
                _collectionNamespace,
                renderedPipeline.Documents,
                _messageEncoderSettings)
            {
                AllowDiskUse = options.AllowDiskUse,
                BypassDocumentValidation = options.BypassDocumentValidation,
                Collation = options.Collation,
                Comment = options.Comment,
                Hint = options.Hint,
                Let = options.Let,
                MaxTime = options.MaxTime,
                ReadConcern = _settings.ReadConcern,
                ReadPreference = _settings.ReadPreference,
                WriteConcern = _settings.WriteConcern
            };
        }

        private BulkMixedWriteOperation CreateBulkWriteOperation(
            IClientSessionHandle session,
            IEnumerable<WriteModel<TDocument>> requests,
            BulkWriteOptions options,
            RenderArgs<TDocument> renderArgs)
        {
            var effectiveWriteConcern = session.IsInTransaction ? WriteConcern.Acknowledged : _settings.WriteConcern;

            return new BulkMixedWriteOperation(
                _collectionNamespace,
                requests.Select((model, index) => ConvertWriteModelToWriteRequest(model, index, renderArgs)),
                _messageEncoderSettings)
            {
                BypassDocumentValidation = options.BypassDocumentValidation,
                Comment = options.Comment,
                IsOrdered = options.IsOrdered,
                Let = options.Let,
                RetryRequested = _database.Client.Settings.RetryWrites,
                WriteConcern = effectiveWriteConcern
            };
        }

        private ChangeStreamOperation<TResult> CreateChangeStreamOperation<TResult>(
            PipelineDefinition<ChangeStreamDocument<TDocument>, TResult> pipeline,
            ChangeStreamOptions options,
            ExpressionTranslationOptions translationOptions)
        {
            return ChangeStreamHelper.CreateChangeStreamOperation(
                this,
                pipeline,
                _documentSerializer,
                options,
                _settings.ReadConcern, messageEncoderSettings: _messageEncoderSettings,
                _database.Client.Settings.RetryReads,
                translationOptions);
        }

        private CountDocumentsOperation CreateCountDocumentsOperation(
            FilterDefinition<TDocument> filter,
            CountOptions options,
            RenderArgs<TDocument> renderArgs)
        {
            return new CountDocumentsOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Collation = options.Collation,
                Comment = options.Comment,
                Filter = filter.Render(renderArgs),
                Hint = options.Hint,
                Limit = options.Limit,
                MaxTime = options.MaxTime,
                ReadConcern = _settings.ReadConcern,
                RetryRequested = _database.Client.Settings.RetryReads,
                Skip = options.Skip
            };
        }

        private CountOperation CreateCountOperation(
            FilterDefinition<TDocument> filter,
            CountOptions options,
            RenderArgs<TDocument> renderArgs)
        {
            return new CountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Collation = options.Collation,
                Comment = options.Comment,
                Filter = filter.Render(renderArgs),
                Hint = options.Hint,
                Limit = options.Limit,
                MaxTime = options.MaxTime,
                ReadConcern = _settings.ReadConcern,
                RetryRequested = _database.Client.Settings.RetryReads,
                Skip = options.Skip
            };
        }

        private DistinctOperation<TField> CreateDistinctOperation<TField>(
            FieldDefinition<TDocument, TField> field,
            FilterDefinition<TDocument> filter,
            DistinctOptions options,
            RenderArgs<TDocument> renderArgs)
        {
            var renderedField = field.Render(renderArgs);
            var valueSerializer = GetValueSerializerForDistinct(renderedField, _settings.SerializerRegistry);

            return new DistinctOperation<TField>(
                _collectionNamespace,
                valueSerializer,
                renderedField.FieldName,
                _messageEncoderSettings)
            {
                Collation = options.Collation,
                Comment = options.Comment,
                Filter = filter.Render(renderArgs),
                MaxTime = options.MaxTime,
                ReadConcern = _settings.ReadConcern,
                RetryRequested = _database.Client.Settings.RetryReads,
            };
        }

        private DistinctOperation<TItem> CreateDistinctManyOperation<TItem>(
            FieldDefinition<TDocument, IEnumerable<TItem>> field,
            FilterDefinition<TDocument> filter,
            DistinctOptions options,
            RenderArgs<TDocument> renderArgs)
        {
            var renderedField = field.Render(renderArgs);
            var itemSerializer = GetItemSerializerForDistinctMany(renderedField, _settings.SerializerRegistry);

            return new DistinctOperation<TItem>(
                _collectionNamespace,
                itemSerializer,
                renderedField.FieldName,
                _messageEncoderSettings)
            {
                Collation = options.Collation,
                Comment = options.Comment,
                Filter = filter.Render(renderArgs),
                MaxTime = options.MaxTime,
                ReadConcern = _settings.ReadConcern,
                RetryRequested = _database.Client.Settings.RetryReads,
            };
        }

        private EstimatedDocumentCountOperation CreateEstimatedDocumentCountOperation(EstimatedDocumentCountOptions options)
        {
            return new EstimatedDocumentCountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Comment = options?.Comment,
                MaxTime = options?.MaxTime,
                RetryRequested = _database.Client.Settings.RetryReads
            };
        }

        private FindOneAndDeleteOperation<TProjection> CreateFindOneAndDeleteOperation<TProjection>(
            FilterDefinition<TDocument> filter,
            FindOneAndDeleteOptions<TDocument, TProjection> options,
            RenderArgs<TDocument> renderArgs)
        {
            var projection = options.Projection ?? new ClientSideDeserializationProjectionDefinition<TDocument, TProjection>();
            var renderedProjection = projection.Render(renderArgs with { RenderForFind = true });

            return new FindOneAndDeleteOperation<TProjection>(
                _collectionNamespace,
                filter.Render(renderArgs),
                new FindAndModifyValueDeserializer<TProjection>(renderedProjection.ProjectionSerializer),
                _messageEncoderSettings)
            {
                Collation = options.Collation,
                Comment = options.Comment,
                Hint = options.Hint,
                Let = options.Let,
                MaxTime = options.MaxTime,
                Projection = renderedProjection.Document,
                Sort = options.Sort?.Render(renderArgs),
                WriteConcern = _settings.WriteConcern,
                RetryRequested = _database.Client.Settings.RetryWrites
            };
        }

        private FindOneAndReplaceOperation<TProjection> CreateFindOneAndReplaceOperation<TProjection>(
            FilterDefinition<TDocument> filter,
            object replacementObject,
            FindOneAndReplaceOptions<TDocument, TProjection> options,
            RenderArgs<TDocument> renderArgs)
        {
            var projection = options.Projection ?? new ClientSideDeserializationProjectionDefinition<TDocument, TProjection>();
            var renderedProjection = projection.Render(renderArgs with { RenderForFind = true });

            return new FindOneAndReplaceOperation<TProjection>(
                _collectionNamespace,
                filter.Render(renderArgs),
                new BsonDocumentWrapper(replacementObject, _documentSerializer),
                new FindAndModifyValueDeserializer<TProjection>(renderedProjection.ProjectionSerializer),
                _messageEncoderSettings)
            {
                BypassDocumentValidation = options.BypassDocumentValidation,
                Collation = options.Collation,
                Comment = options.Comment,
                Hint = options.Hint,
                IsUpsert = options.IsUpsert,
                Let = options.Let,
                MaxTime = options.MaxTime,
                Projection = renderedProjection.Document,
                ReturnDocument = options.ReturnDocument,
                Sort = options.Sort?.Render(renderArgs),
                WriteConcern = _settings.WriteConcern,
                RetryRequested = _database.Client.Settings.RetryWrites
            };
        }

        private FindOneAndUpdateOperation<TProjection> CreateFindOneAndUpdateOperation<TProjection>(
            FilterDefinition<TDocument> filter,
            UpdateDefinition<TDocument> update,
            FindOneAndUpdateOptions<TDocument, TProjection> options,
            RenderArgs<TDocument> renderArgs)
        {
            var projection = options.Projection ?? new ClientSideDeserializationProjectionDefinition<TDocument, TProjection>();
            var renderedProjection = projection.Render(renderArgs with { RenderForFind = true });

            return new FindOneAndUpdateOperation<TProjection>(
                _collectionNamespace,
                filter.Render(renderArgs),
                update.Render(renderArgs),
                new FindAndModifyValueDeserializer<TProjection>(renderedProjection.ProjectionSerializer),
                _messageEncoderSettings)
            {
                ArrayFilters = RenderArrayFilters(options.ArrayFilters),
                BypassDocumentValidation = options.BypassDocumentValidation,
                Collation = options.Collation,
                Comment = options.Comment,
                Hint = options.Hint,
                IsUpsert = options.IsUpsert,
                Let = options.Let,
                MaxTime = options.MaxTime,
                Projection = renderedProjection.Document,
                ReturnDocument = options.ReturnDocument,
                Sort = options.Sort?.Render(renderArgs),
                WriteConcern = _settings.WriteConcern,
                RetryRequested = _database.Client.Settings.RetryWrites
            };
        }

        private FindOperation<TProjection> CreateFindOperation<TProjection>(
            FilterDefinition<TDocument> filter,
            FindOptions<TDocument, TProjection> options,
            RenderArgs<TDocument> renderArgs)
        {
            var projection = options.Projection ?? new ClientSideDeserializationProjectionDefinition<TDocument, TProjection>();
            var renderedProjection = projection.Render(renderArgs with { RenderForFind = true });

            return new FindOperation<TProjection>(
                _collectionNamespace,
                renderedProjection.ProjectionSerializer,
                _messageEncoderSettings)
            {
                AllowDiskUse = options.AllowDiskUse,
                AllowPartialResults = options.AllowPartialResults,
                BatchSize = options.BatchSize,
                Collation = options.Collation,
                Comment = options.Comment,
                CursorType = options.CursorType,
                Filter = filter.Render(renderArgs),
                Hint = options.Hint,
                Let = options.Let,
                Limit = options.Limit,
                Max = options.Max,
                MaxAwaitTime = options.MaxAwaitTime,
                MaxTime = options.MaxTime,
                Min = options.Min,
                NoCursorTimeout = options.NoCursorTimeout,
#pragma warning disable 618
                OplogReplay = options.OplogReplay,
#pragma warning restore 618
                Projection = renderedProjection.Document,
                ReadConcern = _settings.ReadConcern,
                RetryRequested = _database.Client.Settings.RetryReads,
                ReturnKey = options.ReturnKey,
                ShowRecordId = options.ShowRecordId,
                Skip = options.Skip,
                Sort = options.Sort?.Render(renderArgs)
            };
        }

#pragma warning disable CS0618 // Type or member is obsolete
        private MapReduceOperation<TResult> CreateMapReduceOperation<TResult>(
            BsonJavaScript map,
            BsonJavaScript reduce,
            MapReduceOptions<TDocument, TResult> options,
            IBsonSerializer<TResult> resultSerializer,
            RenderArgs<TDocument> renderArgs)
        {
            return new MapReduceOperation<TResult>(
#pragma warning restore CS0618 // Type or member is obsolete
                _collectionNamespace,
                map,
                reduce,
                resultSerializer,
                _messageEncoderSettings)
            {
                Collation = options.Collation,
                Filter = options.Filter?.Render(renderArgs),
                FinalizeFunction = options.Finalize,
#pragma warning disable 618
                JavaScriptMode = options.JavaScriptMode,
#pragma warning restore 618
                Limit = options.Limit,
                MaxTime = options.MaxTime,
                ReadConcern = _settings.ReadConcern,
                Scope = options.Scope,
                Sort = options.Sort?.Render(renderArgs),
                Verbose = options.Verbose
            };
        }

#pragma warning disable CS0618 // Type or member is obsolete
        private MapReduceOutputToCollectionOperation CreateMapReduceOutputToCollectionOperation<TResult>(
            BsonJavaScript map,
            BsonJavaScript reduce,
            MapReduceOptions<TDocument, TResult> options,
            MapReduceOutputOptions outputOptions,
            RenderArgs<TDocument> renderArgs)
        {
            var collectionOutputOptions = (MapReduceOutputOptions.CollectionOutput)outputOptions;
            var databaseNamespace = collectionOutputOptions.DatabaseName == null ?
                _collectionNamespace.DatabaseNamespace :
                new DatabaseNamespace(collectionOutputOptions.DatabaseName);
            var outputCollectionNamespace = new CollectionNamespace(databaseNamespace, collectionOutputOptions.CollectionName);

            return new MapReduceOutputToCollectionOperation(
#pragma warning restore CS0618 // Type or member is obsolete
                _collectionNamespace,
                outputCollectionNamespace,
                map,
                reduce,
                _messageEncoderSettings)
            {
                BypassDocumentValidation = options.BypassDocumentValidation,
                Collation = options.Collation,
                Filter = options.Filter?.Render(renderArgs),
                FinalizeFunction = options.Finalize,
#pragma warning disable 618
                JavaScriptMode = options.JavaScriptMode,
#pragma warning restore 618
                Limit = options.Limit,
                MaxTime = options.MaxTime,
#pragma warning disable 618
                NonAtomicOutput = collectionOutputOptions.NonAtomic,
#pragma warning restore 618
                Scope = options.Scope,
                OutputMode = collectionOutputOptions.OutputMode,
#pragma warning disable 618
                ShardedOutput = collectionOutputOptions.Sharded,
#pragma warning restore 618
                Sort = options.Sort?.Render(renderArgs),
                Verbose = options.Verbose,
                WriteConcern = _settings.WriteConcern
            };
        }

#pragma warning disable CS0618 // Type or member is obsolete
        private FindOperation<TResult> CreateMapReduceOutputToCollectionFindOperation<TResult>(MapReduceOptions<TDocument, TResult> options, CollectionNamespace outputCollectionNamespace, IBsonSerializer<TResult> resultSerializer)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            return new FindOperation<TResult>(
                outputCollectionNamespace,
                resultSerializer,
                _messageEncoderSettings)
            {
                Collation = options.Collation,
                MaxTime = options.MaxTime,
                ReadConcern = _settings.ReadConcern,
                RetryRequested = _database.Client.Settings.RetryReads
            };
        }

        private IReadBindingHandle CreateReadBinding(IClientSessionHandle session, ReadPreference readPreference)
        {
            if (session.IsInTransaction && readPreference.ReadPreferenceMode != ReadPreferenceMode.Primary)
            {
                throw new InvalidOperationException("Read preference in a transaction must be primary.");
            }

            return ChannelPinningHelper.CreateReadBinding(_cluster, session.WrappedCoreSession.Fork(), readPreference);
        }

        private IWriteBindingHandle CreateReadWriteBinding(IClientSessionHandle session)
        {
            return ChannelPinningHelper.CreateReadWriteBinding(_cluster, session.WrappedCoreSession.Fork());
        }

        private MessageEncoderSettings GetMessageEncoderSettings()
        {
            var messageEncoderSettings = new MessageEncoderSettings
            {
                { MessageEncoderSettingsName.ReadEncoding, _settings.ReadEncoding ?? Utf8Encodings.Strict },
                { MessageEncoderSettingsName.WriteEncoding, _settings.WriteEncoding ?? Utf8Encodings.Strict },
                { MessageEncoderSettingsName.SerializationDomain, _settings.SerializationDomain }
            };

            if (_database.Client is MongoClient mongoClient)
            {
                mongoClient.ConfigureAutoEncryptionMessageEncoderSettings(messageEncoderSettings);
            }

            return messageEncoderSettings;
        }

        private IBsonSerializer<TField> GetValueSerializerForDistinct<TField>(RenderedFieldDefinition<TField> renderedField, IBsonSerializerRegistry serializerRegistry)
        {
            if (renderedField.UnderlyingSerializer != null)
            {
                if (renderedField.UnderlyingSerializer.ValueType == typeof(TField))
                {
                    return (IBsonSerializer<TField>)renderedField.UnderlyingSerializer;
                }

                var arraySerializer = renderedField.UnderlyingSerializer as IBsonArraySerializer;
                if (arraySerializer != null)
                {
                    BsonSerializationInfo itemSerializationInfo;
                    if (arraySerializer.TryGetItemSerializationInfo(out itemSerializationInfo))
                    {
                        if (itemSerializationInfo.Serializer.ValueType == typeof(TField))
                        {
                            return (IBsonSerializer<TField>)itemSerializationInfo.Serializer;
                        }
                    }
                }
            }

            return serializerRegistry.GetSerializer<TField>();
        }

        private IBsonSerializer<TItem> GetItemSerializerForDistinctMany<TItem>(RenderedFieldDefinition<IEnumerable<TItem>> renderedField, IBsonSerializerRegistry serializerRegistry)
        {
            if (renderedField.UnderlyingSerializer != null)
            {
                if (renderedField.UnderlyingSerializer is IBsonArraySerializer arraySerializer)
                {
                    BsonSerializationInfo itemSerializationInfo;
                    if (arraySerializer.TryGetItemSerializationInfo(out itemSerializationInfo))
                    {
                        if (itemSerializationInfo.Serializer.ValueType == typeof(TItem))
                        {
                            return (IBsonSerializer<TItem>)itemSerializationInfo.Serializer;
                        }
                    }
                }
            }

            return serializerRegistry.GetSerializer<TItem>();
        }

        private RenderArgs<TDocument> GetRenderArgs()
        {
            var translationOptions = _database.Client.Settings.TranslationOptions;
            return new RenderArgs<TDocument>(_documentSerializer, _settings.SerializerRegistry, translationOptions: translationOptions);
        }

        private RenderArgs<TDocument> GetRenderArgs(ExpressionTranslationOptions translationOptions)
        {
            translationOptions = translationOptions.AddMissingOptionsFrom(_database.Client.Settings.TranslationOptions);
            return new RenderArgs<TDocument>(_documentSerializer, _settings.SerializerRegistry, translationOptions: translationOptions);
        }

        private TResult ExecuteReadOperation<TResult>(IClientSessionHandle session, IReadOperation<TResult> operation, CancellationToken cancellationToken = default(CancellationToken))
        {
            var effectiveReadPreference = ReadPreferenceResolver.GetEffectiveReadPreference(session, null, _settings.ReadPreference);
            return ExecuteReadOperation(session, operation, effectiveReadPreference, cancellationToken);
        }

        private TResult ExecuteReadOperation<TResult>(IClientSessionHandle session, IReadOperation<TResult> operation, ReadPreference readPreference, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var binding = CreateReadBinding(session, readPreference))
            {
                return _operationExecutor.ExecuteReadOperation(binding, operation, cancellationToken);
            }
        }

        private Task<TResult> ExecuteReadOperationAsync<TResult>(IClientSessionHandle session, IReadOperation<TResult> operation, CancellationToken cancellationToken = default(CancellationToken))
        {
            var effectiveReadPreference = ReadPreferenceResolver.GetEffectiveReadPreference(session, null, _settings.ReadPreference);
            return ExecuteReadOperationAsync(session, operation, effectiveReadPreference, cancellationToken);
        }

        private async Task<TResult> ExecuteReadOperationAsync<TResult>(IClientSessionHandle session, IReadOperation<TResult> operation, ReadPreference readPreference, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var binding = CreateReadBinding(session, readPreference))
            {
                return await _operationExecutor.ExecuteReadOperationAsync(binding, operation, cancellationToken).ConfigureAwait(false);
            }
        }

        private TResult ExecuteWriteOperation<TResult>(IClientSessionHandle session, IWriteOperation<TResult> operation, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var binding = CreateReadWriteBinding(session))
            {
                return _operationExecutor.ExecuteWriteOperation(binding, operation, cancellationToken);
            }
        }

        private async Task<TResult> ExecuteWriteOperationAsync<TResult>(IClientSessionHandle session, IWriteOperation<TResult> operation, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var binding = CreateReadWriteBinding(session))
            {
                return await _operationExecutor.ExecuteWriteOperationAsync(binding, operation, cancellationToken).ConfigureAwait(false);
            }
        }

        private IEnumerable<BsonDocument> RenderArrayFilters(IEnumerable<ArrayFilterDefinition> arrayFilters)
        {
            if (arrayFilters == null)
            {
                return null;
            }

            var renderedArrayFilters = new List<BsonDocument>();
            foreach (var arrayFilter in arrayFilters)
            {
                var renderedArrayFilter = arrayFilter.Render(null, _settings.SerializerRegistry);
                renderedArrayFilters.Add(renderedArrayFilter);
            }

            return renderedArrayFilters;
        }

        private IBsonSerializer<TResult> ResolveResultSerializer<TResult>(IBsonSerializer<TResult> resultSerializer)
        {
            if (resultSerializer != null)
            {
                return resultSerializer;
            }

            if (typeof(TResult) == typeof(TDocument) && _documentSerializer != null)
            {
                return (IBsonSerializer<TResult>)_documentSerializer;
            }

            return _settings.SerializerRegistry.GetSerializer<TResult>();
        }

        private void UsingImplicitSession(Action<IClientSessionHandle> func, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var session = _operationExecutor.StartImplicitSession(cancellationToken))
            {
                func(session);
            }
        }

        private TResult UsingImplicitSession<TResult>(Func<IClientSessionHandle, TResult> func, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var session = _operationExecutor.StartImplicitSession(cancellationToken))
            {
                return func(session);
            }
        }

        private async Task UsingImplicitSessionAsync(Func<IClientSessionHandle, Task> funcAsync, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var session = await _operationExecutor.StartImplicitSessionAsync(cancellationToken).ConfigureAwait(false))
            {
                await funcAsync(session).ConfigureAwait(false);
            }
        }

        private async Task<TResult> UsingImplicitSessionAsync<TResult>(Func<IClientSessionHandle, Task<TResult>> funcAsync, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var session = await _operationExecutor.StartImplicitSessionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await funcAsync(session).ConfigureAwait(false);
            }
        }

        // nested types
        private class MongoIndexManager : MongoIndexManagerBase<TDocument>
        {
            // private fields
            private readonly MongoCollectionImpl<TDocument> _collection;

            // constructors
            public MongoIndexManager(MongoCollectionImpl<TDocument> collection)
            {
                _collection = collection;
            }

            // public properties
            public override CollectionNamespace CollectionNamespace
            {
                get { return _collection.CollectionNamespace; }
            }

            public override IBsonSerializer<TDocument> DocumentSerializer
            {
                get { return _collection.DocumentSerializer; }
            }

            public override MongoCollectionSettings Settings
            {
                get { return _collection._settings; }
            }

            // public methods
            public override IEnumerable<string> CreateMany(IEnumerable<CreateIndexModel<TDocument>> models, CancellationToken cancellationToken = default(CancellationToken))
            {
                return CreateMany(models, null, cancellationToken);
            }

            public override IEnumerable<string> CreateMany(
                IEnumerable<CreateIndexModel<TDocument>> models,
                CreateManyIndexesOptions options,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                return _collection.UsingImplicitSession(session => CreateMany(session, models, options, cancellationToken), cancellationToken);
            }

            public override IEnumerable<string> CreateMany(IClientSessionHandle session, IEnumerable<CreateIndexModel<TDocument>> models, CancellationToken cancellationToken = default(CancellationToken))
            {
                return CreateMany(session, models, null, cancellationToken);
            }

            public override IEnumerable<string> CreateMany(
                IClientSessionHandle session,
                IEnumerable<CreateIndexModel<TDocument>> models,
                CreateManyIndexesOptions options,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                Ensure.IsNotNull(session, nameof(session));
                Ensure.IsNotNull(models, nameof(models));

                var renderArgs = _collection.GetRenderArgs();
                var requests = CreateCreateIndexRequests(models, renderArgs);
                var operation = CreateCreateIndexesOperation(requests, options);
                _collection.ExecuteWriteOperation(session, operation, cancellationToken);

                return requests.Select(x => x.GetIndexName());
            }

            public override Task<IEnumerable<string>> CreateManyAsync(IEnumerable<CreateIndexModel<TDocument>> models, CancellationToken cancellationToken = default(CancellationToken))
            {
                return CreateManyAsync(models, null, cancellationToken);
            }

            public override Task<IEnumerable<string>> CreateManyAsync(
                IEnumerable<CreateIndexModel<TDocument>> models,
                CreateManyIndexesOptions options,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                return _collection.UsingImplicitSessionAsync(session => CreateManyAsync(session, models, options, cancellationToken), cancellationToken);
            }

            public override Task<IEnumerable<string>> CreateManyAsync(IClientSessionHandle session, IEnumerable<CreateIndexModel<TDocument>> models, CancellationToken cancellationToken = default(CancellationToken))
            {
                return CreateManyAsync(session, models, null, cancellationToken);
            }

            public override async Task<IEnumerable<string>> CreateManyAsync(
                IClientSessionHandle session,
                IEnumerable<CreateIndexModel<TDocument>> models,
                CreateManyIndexesOptions options,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                Ensure.IsNotNull(session, nameof(session));
                Ensure.IsNotNull(models, nameof(models));

                var renderArgs = _collection.GetRenderArgs();
                var requests = CreateCreateIndexRequests(models, renderArgs);
                var operation = CreateCreateIndexesOperation(requests, options);
                await _collection.ExecuteWriteOperationAsync(session, operation, cancellationToken).ConfigureAwait(false);

                return requests.Select(x => x.GetIndexName());
            }

            public override void DropAll(CancellationToken cancellationToken)
            {
                _collection.UsingImplicitSession(session => DropAll(session, cancellationToken), cancellationToken);
            }

            public override void DropAll(DropIndexOptions options, CancellationToken cancellationToken = default(CancellationToken))
            {
                _collection.UsingImplicitSession(session => DropAll(session, options, cancellationToken), cancellationToken);
            }

            public override void DropAll(IClientSessionHandle session, CancellationToken cancellationToken = default(CancellationToken))
            {
                DropAll(session, null, cancellationToken);
            }

            public override void DropAll(IClientSessionHandle session, DropIndexOptions options, CancellationToken cancellationToken = default(CancellationToken))
            {
                Ensure.IsNotNull(session, nameof(session));
                var operation = CreateDropAllOperation(options);
                _collection.ExecuteWriteOperation(session, operation, cancellationToken);
            }

            public override Task DropAllAsync(CancellationToken cancellationToken)
            {
                return _collection.UsingImplicitSessionAsync(session => DropAllAsync(session, cancellationToken), cancellationToken);
            }

            public override Task DropAllAsync(DropIndexOptions options, CancellationToken cancellationToken = default(CancellationToken))
            {
                return _collection.UsingImplicitSessionAsync(session => DropAllAsync(session, options, cancellationToken), cancellationToken);
            }

            public override Task DropAllAsync(IClientSessionHandle session, CancellationToken cancellationToken = default(CancellationToken))
            {
                return DropAllAsync(session, null, cancellationToken);
            }

            public override Task DropAllAsync(IClientSessionHandle session, DropIndexOptions options, CancellationToken cancellationToken = default(CancellationToken))
            {
                Ensure.IsNotNull(session, nameof(session));
                var operation = CreateDropAllOperation(options);
                return _collection.ExecuteWriteOperationAsync(session, operation, cancellationToken);
            }

            public override void DropOne(string name, CancellationToken cancellationToken = default(CancellationToken))
            {
                _collection.UsingImplicitSession(session => DropOne(session, name, cancellationToken), cancellationToken);
            }

            public override void DropOne(string name, DropIndexOptions options, CancellationToken cancellationToken = default(CancellationToken))
            {
                _collection.UsingImplicitSession(session => DropOne(session, name, options, cancellationToken), cancellationToken);
            }

            public override void DropOne(IClientSessionHandle session, string name, CancellationToken cancellationToken = default(CancellationToken))
            {
                DropOne(session, name, null, cancellationToken);
            }

            public override void DropOne(
                IClientSessionHandle session,
                string name,
                DropIndexOptions options,
                CancellationToken cancellationToken)
            {
                Ensure.IsNotNull(session, nameof(session));
                Ensure.IsNotNullOrEmpty(name, nameof(name));
                if (name == "*")
                {
                    throw new ArgumentException("Cannot specify '*' for the index name. Use DropAllAsync to drop all indexes.", "name");
                }

                var operation = CreateDropOneOperation(name, options);
                _collection.ExecuteWriteOperation(session, operation, cancellationToken);
            }

            public override Task DropOneAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
            {
                return _collection.UsingImplicitSessionAsync(session => DropOneAsync(session, name, cancellationToken), cancellationToken);
            }

            public override Task DropOneAsync(string name, DropIndexOptions options, CancellationToken cancellationToken = default(CancellationToken))
            {
                return _collection.UsingImplicitSessionAsync(session => DropOneAsync(session, name, options, cancellationToken), cancellationToken);
            }

            public override Task DropOneAsync(IClientSessionHandle session, string name, CancellationToken cancellationToken = default(CancellationToken))
            {
                return DropOneAsync(session, name, null, cancellationToken);
            }

            public override Task DropOneAsync(
                IClientSessionHandle session,
                string name,
                DropIndexOptions options,
                CancellationToken cancellationToken)
            {
                Ensure.IsNotNull(session, nameof(session));
                Ensure.IsNotNullOrEmpty(name, nameof(name));
                if (name == "*")
                {
                    throw new ArgumentException("Cannot specify '*' for the index name. Use DropAllAsync to drop all indexes.", "name");
                }

                var operation = CreateDropOneOperation(name, options);
                return _collection.ExecuteWriteOperationAsync(session, operation, cancellationToken);
            }

            public override IAsyncCursor<BsonDocument> List(CancellationToken cancellationToken = default(CancellationToken))
            {
                return List(options: null, cancellationToken);
            }

            public override IAsyncCursor<BsonDocument> List(ListIndexesOptions options, CancellationToken cancellationToken = default(CancellationToken))
            {
                return _collection.UsingImplicitSession(session => List(session, options, cancellationToken), cancellationToken);
            }

            public override IAsyncCursor<BsonDocument> List(IClientSessionHandle session, CancellationToken cancellationToken = default)
            {
                return List(session, options: null, cancellationToken);
            }

            public override IAsyncCursor<BsonDocument> List(IClientSessionHandle session, ListIndexesOptions options, CancellationToken cancellationToken = default(CancellationToken))
            {
                Ensure.IsNotNull(session, nameof(session));
                var operation = CreateListIndexesOperation(options);
                return _collection.ExecuteReadOperation(session, operation, ReadPreference.Primary, cancellationToken);
            }

            public override Task<IAsyncCursor<BsonDocument>> ListAsync(CancellationToken cancellationToken = default(CancellationToken))
            {
                return ListAsync(options: null, cancellationToken);
            }

            public override Task<IAsyncCursor<BsonDocument>> ListAsync(ListIndexesOptions options, CancellationToken cancellationToken = default(CancellationToken))
            {
                return _collection.UsingImplicitSessionAsync(session => ListAsync(session, options, cancellationToken), cancellationToken);
            }

            public override Task<IAsyncCursor<BsonDocument>> ListAsync(IClientSessionHandle session, CancellationToken cancellationToken = default)
            {
                return ListAsync(session, options: null, cancellationToken);
            }

            public override Task<IAsyncCursor<BsonDocument>> ListAsync(IClientSessionHandle session, ListIndexesOptions options, CancellationToken cancellationToken = default(CancellationToken))
            {
                Ensure.IsNotNull(session, nameof(session));
                var operation = CreateListIndexesOperation(options);
                return _collection.ExecuteReadOperationAsync(session, operation, ReadPreference.Primary, cancellationToken);
            }

            // private methods
            private CreateIndexesOperation CreateCreateIndexesOperation(IEnumerable<CreateIndexRequest> requests, CreateManyIndexesOptions options)
            {
                return new CreateIndexesOperation(_collection._collectionNamespace, requests, _collection._messageEncoderSettings)
                {
                    Comment = options?.Comment,
                    CommitQuorum = options?.CommitQuorum,
                    MaxTime = options?.MaxTime,
                    WriteConcern = _collection.Settings.WriteConcern
                };
            }

            private IEnumerable<CreateIndexRequest> CreateCreateIndexRequests(IEnumerable<CreateIndexModel<TDocument>> models, RenderArgs<TDocument> renderArgs)
            {
                return models.Select(m =>
                {
                    var options = m.Options ?? new CreateIndexOptions<TDocument>();
                    var keysDocument = m.Keys.Render(renderArgs);
                    var renderedPartialFilterExpression = options.PartialFilterExpression == null ? null : options.PartialFilterExpression.Render(renderArgs);
                    var renderedWildcardProjection = options.WildcardProjection?.Render(renderArgs);

                    return new CreateIndexRequest(keysDocument)
                    {
                        Name = options.Name,
                        Background = options.Background,
                        Bits = options.Bits,
#pragma warning disable 618
                        BucketSize = options.BucketSize,
#pragma warning restore 618
                        Collation = options.Collation,
                        DefaultLanguage = options.DefaultLanguage,
                        ExpireAfter = options.ExpireAfter,
                        Hidden = options.Hidden,
                        LanguageOverride = options.LanguageOverride,
                        Max = options.Max,
                        Min = options.Min,
                        PartialFilterExpression = renderedPartialFilterExpression,
                        Sparse = options.Sparse,
                        SphereIndexVersion = options.SphereIndexVersion,
                        StorageEngine = options.StorageEngine,
                        TextIndexVersion = options.TextIndexVersion,
                        Unique = options.Unique,
                        Version = options.Version,
                        Weights = options.Weights,
                        WildcardProjection = renderedWildcardProjection
                    };
                });
            }

            private DropIndexOperation CreateDropAllOperation(DropIndexOptions options)
            {
                return new DropIndexOperation(_collection._collectionNamespace, "*", _collection._messageEncoderSettings)
                {
                    Comment = options?.Comment,
                    MaxTime = options?.MaxTime,
                    WriteConcern = _collection.Settings.WriteConcern
                };
            }

            private DropIndexOperation CreateDropOneOperation(string name, DropIndexOptions options)
            {
                return new DropIndexOperation(_collection._collectionNamespace, name, _collection._messageEncoderSettings)
                {
                    Comment = options?.Comment,
                    MaxTime = options?.MaxTime,
                    WriteConcern = _collection.Settings.WriteConcern
                };
            }

            private ListIndexesOperation CreateListIndexesOperation(ListIndexesOptions options)
            {
                return new ListIndexesOperation(_collection._collectionNamespace, _collection._messageEncoderSettings)
                {
                    BatchSize = options?.BatchSize,
                    Comment = options?.Comment,
                    RetryRequested = _collection.Database.Client.Settings.RetryReads
                };
            }
        }

        internal sealed class MongoSearchIndexManager : IMongoSearchIndexManager
        {
            // private fields
            private readonly MongoCollectionImpl<TDocument> _collection;

            // constructors
            public MongoSearchIndexManager(MongoCollectionImpl<TDocument> collection)
            {
                _collection = Ensure.IsNotNull(collection, nameof(collection));
            }

            public IEnumerable<string> CreateMany(IEnumerable<CreateSearchIndexModel> models, CancellationToken cancellationToken = default)
            {
                var operation = CreateCreateIndexesOperation(models);
                var result = _collection.UsingImplicitSession(session => _collection.ExecuteWriteOperation(session, operation, cancellationToken), cancellationToken);
                var indexNames = GetIndexNames(result);

                return indexNames;
            }

            public async Task<IEnumerable<string>> CreateManyAsync(IEnumerable<CreateSearchIndexModel> models, CancellationToken cancellationToken = default)
            {
                var operation = CreateCreateIndexesOperation(models);
                var result = await _collection.UsingImplicitSessionAsync(session => _collection.ExecuteWriteOperationAsync(session, operation, cancellationToken), cancellationToken).ConfigureAwait(false);
                var indexNames = GetIndexNames(result);

                return indexNames;
            }

            public string CreateOne(BsonDocument definition, string name = null, CancellationToken cancellationToken = default) =>
                CreateOne(new CreateSearchIndexModel(name, definition), cancellationToken);

            public string CreateOne(CreateSearchIndexModel model, CancellationToken cancellationToken = default)
            {
                var result = CreateMany(new[] { model }, cancellationToken);
                return result.Single();
            }

            public Task<string> CreateOneAsync(BsonDocument definition, string name = null, CancellationToken cancellationToken = default) =>
                CreateOneAsync(new CreateSearchIndexModel(name, definition), cancellationToken);

            public async Task<string> CreateOneAsync(CreateSearchIndexModel model, CancellationToken cancellationToken = default)
            {
                var result = await CreateManyAsync(new[] { model }, cancellationToken).ConfigureAwait(false);
                return result.Single();
            }

            public void DropOne(string indexName, CancellationToken cancellationToken = default)
            {
                var operation = new DropSearchIndexOperation(_collection.CollectionNamespace, indexName, _collection._messageEncoderSettings);
                _collection.UsingImplicitSession(session => _collection.ExecuteWriteOperation(session, operation, cancellationToken), cancellationToken);
            }

            public Task DropOneAsync(string indexName, CancellationToken cancellationToken = default)
            {
                var operation = new DropSearchIndexOperation(_collection.CollectionNamespace, indexName, _collection._messageEncoderSettings);
                return _collection.UsingImplicitSessionAsync(session => _collection.ExecuteWriteOperationAsync(session, operation, cancellationToken), cancellationToken);
            }

            public IAsyncCursor<BsonDocument> List(string indexName, AggregateOptions aggregateOptions = null, CancellationToken cancellationToken = default)
            {
                return _collection.WithReadConcern(ReadConcern.Default).Aggregate(CreateListIndexesStage(indexName), aggregateOptions, cancellationToken);
            }

            public Task<IAsyncCursor<BsonDocument>> ListAsync(string indexName, AggregateOptions aggregateOptions = null, CancellationToken cancellationToken = default)
            {
                return _collection.WithReadConcern(ReadConcern.Default).AggregateAsync(CreateListIndexesStage(indexName), aggregateOptions, cancellationToken);
            }

            public void Update(string indexName, BsonDocument definition, CancellationToken cancellationToken = default)
            {
                var operation = new UpdateSearchIndexOperation(_collection.CollectionNamespace, indexName, definition, _collection._messageEncoderSettings);

                _collection.UsingImplicitSession(session => _collection.ExecuteWriteOperation(session, operation, cancellationToken), cancellationToken);
            }

            public async Task UpdateAsync(string indexName, BsonDocument definition, CancellationToken cancellationToken = default)
            {
                var operation = new UpdateSearchIndexOperation(_collection.CollectionNamespace, indexName, definition, _collection._messageEncoderSettings);

                await _collection.UsingImplicitSessionAsync(session => _collection.ExecuteWriteOperationAsync(session, operation, cancellationToken), cancellationToken).ConfigureAwait(false);
            }

            // private methods
            private PipelineDefinition<TDocument, BsonDocument> CreateListIndexesStage(string indexName)
            {
                var nameDocument = new BsonDocument() { { "name", indexName, indexName != null } };
                var stage = new BsonDocument("$listSearchIndexes", nameDocument);
                return new BsonDocumentStagePipelineDefinition<TDocument, BsonDocument>(new[] { stage });
            }

            private CreateSearchIndexesOperation CreateCreateIndexesOperation(IEnumerable<CreateSearchIndexModel> models) =>
                new(_collection._collectionNamespace,
                    models.Select(m => new CreateSearchIndexRequest(m.Name, m.Type, m.Definition)),
                    _collection._messageEncoderSettings);

            private string[] GetIndexNames(BsonDocument createSearchIndexesResponse) =>
                createSearchIndexesResponse["indexesCreated"]
                    .AsBsonArray
                    .Select(bsonValue => bsonValue["name"].AsString)
                    .ToArray();
        }
    }
}
