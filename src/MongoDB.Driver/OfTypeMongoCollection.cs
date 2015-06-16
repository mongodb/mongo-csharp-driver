using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver
{
    internal class OfTypeMongoCollection<TRootDocument, TDocument> : MongoCollectionBase<TDocument>, IFilteredMongoCollection<TDocument>
        where TDocument : TRootDocument
    {
        private readonly IMongoCollection<TRootDocument> _rootCollection;
        private readonly IMongoCollection<TDocument> _wrappedCollection;
        private readonly FilterDefinition<TDocument> _filter;

        public OfTypeMongoCollection(IMongoCollection<TRootDocument> rootCollection, IMongoCollection<TDocument> wrappedCollection, FilterDefinition<TDocument> filter)
        {
            _rootCollection = rootCollection;
            _wrappedCollection = wrappedCollection;
            _filter = filter;
        }

        public override CollectionNamespace CollectionNamespace
        {
            get { return _wrappedCollection.CollectionNamespace; }
        }

        public override IMongoDatabase Database
        {
            get { return _wrappedCollection.Database; }
        }

        public override IBsonSerializer<TDocument> DocumentSerializer
        {
            get { return _wrappedCollection.DocumentSerializer; }
        }

        public FilterDefinition<TDocument> Filter
        {
            get { return _filter; }
        }

        public override IMongoIndexManager<TDocument> Indexes
        {
            get { return _wrappedCollection.Indexes; }
        }

        public override MongoCollectionSettings Settings
        {
            get { return _wrappedCollection.Settings; }
        }

        public override Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            const string operatorName = "$match";

            var newStage = new DelegatedPipelineStageDefinition<TDocument, TDocument>(
                operatorName,
                (s, sr) =>
                {
                    var renderedFilter = _filter.Render(s, sr);
                    return new RenderedPipelineStageDefinition<TDocument>(operatorName, new BsonDocument(operatorName, renderedFilter), s);
                });

            var firstPipeline = new PipelineStagePipelineDefinition<TDocument, TDocument>(new[] { newStage });
            var combinedPipeline = new CombinedPipelineDefinition<TDocument, TDocument, TResult>(
                firstPipeline,
                pipeline);

            var optimizedPipeline = new OptimizingPipelineDefinition<TDocument, TResult>(combinedPipeline);

            return _wrappedCollection.AggregateAsync(optimizedPipeline, options, cancellationToken);
        }

        public override Task<BulkWriteResult<TDocument>> BulkWriteAsync(IEnumerable<WriteModel<TDocument>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var models = ConvertModels(requests);
            return _wrappedCollection.BulkWriteAsync(models, options, cancellationToken);
        }

        private IEnumerable<WriteModel<TDocument>> ConvertModels(IEnumerable<WriteModel<TDocument>> models)
        {
            return models.Select<WriteModel<TDocument>, WriteModel<TDocument>>(x =>
            {
                switch (x.ModelType)
                {
                    case WriteModelType.DeleteMany:
                        var deleteManyModel = (DeleteManyModel<TDocument>)x;
                        return new DeleteManyModel<TDocument>(CombineFilters(deleteManyModel.Filter));
                    case WriteModelType.DeleteOne:
                        var deleteOneModel = (DeleteOneModel<TDocument>)x;
                        return new DeleteOneModel<TDocument>(CombineFilters(deleteOneModel.Filter));
                    case WriteModelType.InsertOne:
                        var insertOneModel = (InsertOneModel<TDocument>)x;
                        return new InsertOneModel<TDocument>(insertOneModel.Document);
                    case WriteModelType.ReplaceOne:
                        var replaceOneModel = (ReplaceOneModel<TDocument>)x;
                        return new ReplaceOneModel<TDocument>(CombineFilters(replaceOneModel.Filter), replaceOneModel.Replacement) { IsUpsert = replaceOneModel.IsUpsert };
                    case WriteModelType.UpdateMany:
                        var updateManyModel = (UpdateManyModel<TDocument>)x;
                        return new UpdateManyModel<TDocument>(CombineFilters(updateManyModel.Filter), updateManyModel.Update) { IsUpsert = updateManyModel.IsUpsert };
                    case WriteModelType.UpdateOne:
                        var updateOneModel = (UpdateOneModel<TDocument>)x;
                        return new UpdateOneModel<TDocument>(CombineFilters(updateOneModel.Filter), updateOneModel.Update) { IsUpsert = updateOneModel.IsUpsert };
                    default:
                        throw new MongoInternalException("Request type is invalid.");
                }
            });
        }

        public override Task<long> CountAsync(FilterDefinition<TDocument> filter, CountOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _wrappedCollection.CountAsync(CombineFilters(filter), options, cancellationToken);
        }

        public override Task<IAsyncCursor<TField>> DistinctAsync<TField>(FieldDefinition<TDocument, TField> field, FilterDefinition<TDocument> filter, DistinctOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _wrappedCollection.DistinctAsync(field, CombineFilters(filter), options, cancellationToken);
        }

        public override Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(FilterDefinition<TDocument> filter, FindOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _wrappedCollection.FindAsync(CombineFilters(filter), options, cancellationToken);
        }

        public override Task<TProjection> FindOneAndDeleteAsync<TProjection>(FilterDefinition<TDocument> filter, FindOneAndDeleteOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _wrappedCollection.FindOneAndDeleteAsync(CombineFilters(filter), options, cancellationToken);
        }

        public override Task<TProjection> FindOneAndReplaceAsync<TProjection>(FilterDefinition<TDocument> filter, TDocument replacement, FindOneAndReplaceOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _wrappedCollection.FindOneAndReplaceAsync(CombineFilters(filter), replacement, options, cancellationToken);
        }

        public override Task<TProjection> FindOneAndUpdateAsync<TProjection>(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, FindOneAndUpdateOptions<TDocument, TProjection> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _wrappedCollection.FindOneAndUpdateAsync(CombineFilters(filter), update, options, cancellationToken);
        }

        public override Task<IAsyncCursor<TResult>> MapReduceAsync<TResult>(BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TDocument, TResult> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            options = options ?? new MapReduceOptions<TDocument, TResult>();
            options.Filter = CombineFilters(options.Filter);
            return _wrappedCollection.MapReduceAsync(map, reduce, options, cancellationToken);
        }

        public override IFilteredMongoCollection<TNewDocument> OfType<TNewDocument>()
        {
            return _rootCollection.OfType<TNewDocument>();
        }

        public override IMongoCollection<TDocument> WithReadPreference(ReadPreference readPreference)
        {
            return new OfTypeMongoCollection<TRootDocument, TDocument>(_rootCollection, _wrappedCollection.WithReadPreference(readPreference), _filter);
        }

        public override IMongoCollection<TDocument> WithWriteConcern(WriteConcern writeConcern)
        {
            return new OfTypeMongoCollection<TRootDocument, TDocument>(_rootCollection, _wrappedCollection.WithWriteConcern(writeConcern), _filter);
        }

        private FilterDefinition<TDocument> CombineFilters(FilterDefinition<TDocument> newFilter)
        {
            if (newFilter == null)
            {
                return _filter;
            }
            if (_filter == null)
            {
                return newFilter;
            }

            return _filter & newFilter;
        }
    }
}
