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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.Driver.Linq;

namespace MongoDB.Driver
{
    internal sealed class MongoCollectionImpl<TDocument> : MongoCollectionBase<TDocument>
    {
        // fields
        private readonly ICluster _cluster;
        private readonly CollectionNamespace _collectionNamespace;
        private readonly IMongoDatabase _database;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly IOperationExecutor _operationExecutor;
        private readonly IBsonSerializer<TDocument> _documentSerializer;
        private readonly MongoCollectionSettings _settings;

        // constructors
        public MongoCollectionImpl(IMongoDatabase database, CollectionNamespace collectionNamespace, MongoCollectionSettings settings, ICluster cluster, IOperationExecutor operationExecutor)
        {
            _database = Ensure.IsNotNull(database, "database");
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _settings = Ensure.IsNotNull(settings, "settings").Freeze();
            _cluster = Ensure.IsNotNull(cluster, "cluster");
            _operationExecutor = Ensure.IsNotNull(operationExecutor, "operationExecutor");

            _documentSerializer = _settings.SerializerRegistry.GetSerializer<TDocument>();
            _messageEncoderSettings = new MessageEncoderSettings
            {
                { MessageEncoderSettingsName.GuidRepresentation, _settings.GuidRepresentation },
                { MessageEncoderSettingsName.ReadEncoding, _settings.ReadEncoding ?? Utf8Encodings.Strict },
                { MessageEncoderSettingsName.WriteEncoding, _settings.WriteEncoding ?? Utf8Encodings.Strict }
            };
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

        public override IMongoIndexManager<TDocument> Indexes
        {
            get { return new MongoIndexManager(this); }
        }

        public override MongoCollectionSettings Settings
        {
            get { return _settings; }
        }

        // methods
        public override async Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions options, CancellationToken cancellationToken)
        {
            var renderedPipeline = Ensure.IsNotNull(pipeline, "pipeline").Render(_documentSerializer, _settings.SerializerRegistry);
            options = options ?? new AggregateOptions();

            var last = renderedPipeline.Documents.LastOrDefault();
            if (last != null && last.GetElement(0).Name == "$out")
            {
                var operation = new AggregateToCollectionOperation(
                    _collectionNamespace,
                    renderedPipeline.Documents,
                    _messageEncoderSettings)
                {
                    AllowDiskUse = options.AllowDiskUse,
                    MaxTime = options.MaxTime
                };

                await ExecuteWriteOperation(operation, cancellationToken).ConfigureAwait(false);

                var outputCollectionName = last.GetElement(0).Value.AsString;

                var findOperation = new FindOperation<TResult>(
                    new CollectionNamespace(_collectionNamespace.DatabaseNamespace, outputCollectionName),
                    renderedPipeline.OutputSerializer,
                    _messageEncoderSettings)
                {
                    BatchSize = options.BatchSize,
                    MaxTime = options.MaxTime
                };

                // we want to delay execution of the find because the user may
                // not want to iterate the results at all...
                return await Task.FromResult<IAsyncCursor<TResult>>(new DeferredAsyncCursor<TResult>(ct => ExecuteReadOperation(findOperation, ReadPreference.Primary, ct))).ConfigureAwait(false);
            }
            else
            {
                var aggregateOperation = new AggregateOperation<TResult>(
                    _collectionNamespace,
                    renderedPipeline.Documents,
                    renderedPipeline.OutputSerializer,
                    _messageEncoderSettings)
                {
                    AllowDiskUse = options.AllowDiskUse,
                    BatchSize = options.BatchSize,
                    MaxTime = options.MaxTime,
                    UseCursor = options.UseCursor
                };
                return await ExecuteReadOperation(aggregateOperation, cancellationToken).ConfigureAwait(false);
            }
        }

        public override async Task<BulkWriteResult<TDocument>> BulkWriteAsync(IEnumerable<WriteModel<TDocument>> requests, BulkWriteOptions options, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(requests, "requests");
            if (!requests.Any())
            {
                throw new ArgumentException("Must contain at least 1 request.", "requests");
            }

            options = options ?? new BulkWriteOptions();

            var operation = new BulkMixedWriteOperation(
                _collectionNamespace,
                requests.Select(ConvertWriteModelToWriteRequest),
                _messageEncoderSettings)
            {
                IsOrdered = options.IsOrdered,
                WriteConcern = _settings.WriteConcern
            };

            try
            {
                var result = await ExecuteWriteOperation(operation, cancellationToken).ConfigureAwait(false);
                return BulkWriteResult<TDocument>.FromCore(result, requests);
            }
            catch (MongoBulkWriteOperationException ex)
            {
                throw MongoBulkWriteException<TDocument>.FromCore(ex, requests.ToList());
            }
        }

        public override Task<long> CountAsync(FilterDefinition<TDocument> filter, CountOptions options, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(filter, "filter");

            options = options ?? new CountOptions();

            var operation = new CountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Filter = filter.Render(_documentSerializer, _settings.SerializerRegistry),
                Hint = options.Hint,
                Limit = options.Limit,
                MaxTime = options.MaxTime,
                Skip = options.Skip
            };

            return ExecuteReadOperation(operation, cancellationToken);
        }

        public override Task<IAsyncCursor<TField>> DistinctAsync<TField>(FieldDefinition<TDocument, TField> field, FilterDefinition<TDocument> filter, DistinctOptions options, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(field, "field");
            Ensure.IsNotNull(filter, "filter");

            options = options ?? new DistinctOptions();
            var renderedField = field.Render(_documentSerializer, _settings.SerializerRegistry);

            var operation = new DistinctOperation<TField>(
                _collectionNamespace,
                renderedField.FieldSerializer,
                renderedField.FieldName,
                _messageEncoderSettings)
            {
                Filter = filter.Render(_documentSerializer, _settings.SerializerRegistry),
                MaxTime = options.MaxTime
            };

            return ExecuteReadOperation(operation, cancellationToken);
        }

        public override Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(FilterDefinition<TDocument> filter, FindOptions<TDocument, TProjection> options, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(filter, "filter");

            options = options ?? new FindOptions<TDocument, TProjection>();
            var projection = options.Projection ?? new EntireDocumentProjectionDefinition<TDocument, TProjection>();
            var renderedProjection = projection.Render(_documentSerializer, _settings.SerializerRegistry);

            var operation = new FindOperation<TProjection>(
                _collectionNamespace,
                renderedProjection.ProjectionSerializer,
                _messageEncoderSettings)
            {
                AllowPartialResults = options.AllowPartialResults,
                BatchSize = options.BatchSize,
                Comment = options.Comment,
                CursorType = options.CursorType.ToCore(),
                Filter = filter.Render(_documentSerializer, _settings.SerializerRegistry),
                Limit = options.Limit,
                MaxTime = options.MaxTime,
                Modifiers = options.Modifiers,
                NoCursorTimeout = options.NoCursorTimeout,
                Projection = renderedProjection.Document,
                Skip = options.Skip,
                Sort = options.Sort == null ? null : options.Sort.Render(_documentSerializer, _settings.SerializerRegistry)
            };

            return ExecuteReadOperation(operation, cancellationToken);
        }

        public override Task<TProjection> FindOneAndDeleteAsync<TProjection>(FilterDefinition<TDocument> filter, FindOneAndDeleteOptions<TDocument, TProjection> options, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(filter, "filter");

            options = options ?? new FindOneAndDeleteOptions<TDocument, TProjection>();
            var projection = options.Projection ?? new EntireDocumentProjectionDefinition<TDocument, TProjection>();
            var renderedProjection = projection.Render(_documentSerializer, _settings.SerializerRegistry);

            var operation = new FindOneAndDeleteOperation<TProjection>(
                _collectionNamespace,
                filter.Render(_documentSerializer, _settings.SerializerRegistry),
                new FindAndModifyValueDeserializer<TProjection>(renderedProjection.ProjectionSerializer),
                _messageEncoderSettings)
            {
                MaxTime = options.MaxTime,
                Projection = renderedProjection.Document,
                Sort = options.Sort == null ? null : options.Sort.Render(_documentSerializer, _settings.SerializerRegistry)
            };

            return ExecuteWriteOperation(operation, cancellationToken);
        }

        public override Task<TProjection> FindOneAndReplaceAsync<TProjection>(FilterDefinition<TDocument> filter, TDocument replacement, FindOneAndReplaceOptions<TDocument, TProjection> options, CancellationToken cancellationToken)
        {
            var replacementObject = (object)replacement; // only box once if it's a struct
            Ensure.IsNotNull(filter, "filter");
            Ensure.IsNotNull(replacementObject, "replacement");

            options = options ?? new FindOneAndReplaceOptions<TDocument, TProjection>();
            var projection = options.Projection ?? new EntireDocumentProjectionDefinition<TDocument, TProjection>();
            var renderedProjection = projection.Render(_documentSerializer, _settings.SerializerRegistry);

            var operation = new FindOneAndReplaceOperation<TProjection>(
                _collectionNamespace,
                filter.Render(_documentSerializer, _settings.SerializerRegistry),
                new BsonDocumentWrapper(replacementObject, _documentSerializer),
                new FindAndModifyValueDeserializer<TProjection>(renderedProjection.ProjectionSerializer),
                _messageEncoderSettings)
            {
                IsUpsert = options.IsUpsert,
                MaxTime = options.MaxTime,
                Projection = renderedProjection.Document,
                ReturnDocument = options.ReturnDocument.ToCore(),
                Sort = options.Sort == null ? null : options.Sort.Render(_documentSerializer, _settings.SerializerRegistry)
            };

            return ExecuteWriteOperation(operation, cancellationToken);
        }

        public override Task<TProjection> FindOneAndUpdateAsync<TProjection>(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, FindOneAndUpdateOptions<TDocument, TProjection> options, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(filter, "filter");
            Ensure.IsNotNull(update, "update");

            options = options ?? new FindOneAndUpdateOptions<TDocument, TProjection>();
            var projection = options.Projection ?? new EntireDocumentProjectionDefinition<TDocument, TProjection>();
            var renderedProjection = projection.Render(_documentSerializer, _settings.SerializerRegistry);

            var operation = new FindOneAndUpdateOperation<TProjection>(
                _collectionNamespace,
                filter.Render(_documentSerializer, _settings.SerializerRegistry),
                update.Render(_documentSerializer, _settings.SerializerRegistry),
                new FindAndModifyValueDeserializer<TProjection>(renderedProjection.ProjectionSerializer),
                _messageEncoderSettings)
            {
                IsUpsert = options.IsUpsert,
                MaxTime = options.MaxTime,
                Projection = renderedProjection.Document,
                ReturnDocument = options.ReturnDocument.ToCore(),
                Sort = options.Sort == null ? null : options.Sort.Render(_documentSerializer, _settings.SerializerRegistry)
            };

            return ExecuteWriteOperation(operation, cancellationToken);
        }

        public override async Task<IAsyncCursor<TResult>> MapReduceAsync<TResult>(BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TDocument, TResult> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(map, "map");
            Ensure.IsNotNull(reduce, "reduce");

            options = options ?? new MapReduceOptions<TDocument, TResult>();
            var outputOptions = options.OutputOptions ?? MapReduceOutputOptions.Inline;
            var resultSerializer = ResolveResultSerializer<TResult>(options.ResultSerializer);

            if (outputOptions == MapReduceOutputOptions.Inline)
            {
                var operation = new MapReduceOperation<TResult>(
                    _collectionNamespace,
                    map,
                    reduce,
                    resultSerializer,
                    _messageEncoderSettings)
                {
                    Filter = options.Filter == null ? null : options.Filter.Render(_documentSerializer, _settings.SerializerRegistry),
                    FinalizeFunction = options.Finalize,
                    JavaScriptMode = options.JavaScriptMode,
                    Limit = options.Limit,
                    MaxTime = options.MaxTime,
                    Scope = options.Scope,
                    Sort = options.Sort == null ? null : options.Sort.Render(_documentSerializer, _settings.SerializerRegistry),
                    Verbose = options.Verbose
                };

                return await ExecuteReadOperation(operation, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var collectionOutputOptions = (MapReduceOutputOptions.CollectionOutput)outputOptions;
                var databaseNamespace = collectionOutputOptions.DatabaseName == null ?
                    _collectionNamespace.DatabaseNamespace :
                    new DatabaseNamespace(collectionOutputOptions.DatabaseName);
                var outputCollectionNamespace = new CollectionNamespace(databaseNamespace, collectionOutputOptions.CollectionName);

                var operation = new MapReduceOutputToCollectionOperation(
                    _collectionNamespace,
                    outputCollectionNamespace,
                    map,
                    reduce,
                    _messageEncoderSettings)
                {
                    Filter = options.Filter == null ? null : options.Filter.Render(_documentSerializer, _settings.SerializerRegistry),
                    FinalizeFunction = options.Finalize,
                    JavaScriptMode = options.JavaScriptMode,
                    Limit = options.Limit,
                    MaxTime = options.MaxTime,
                    NonAtomicOutput = collectionOutputOptions.NonAtomic,
                    Scope = options.Scope,
                    OutputMode = collectionOutputOptions.OutputMode,
                    ShardedOutput = collectionOutputOptions.Sharded,
                    Sort = options.Sort == null ? null : options.Sort.Render(_documentSerializer, _settings.SerializerRegistry),
                    Verbose = options.Verbose
                };

                await ExecuteWriteOperation(operation, cancellationToken).ConfigureAwait(false);

                var findOperation = new FindOperation<TResult>(
                    outputCollectionNamespace,
                    resultSerializer,
                    _messageEncoderSettings)
                {
                    MaxTime = options.MaxTime
                };

                // we want to delay execution of the find because the user may
                // not want to iterate the results at all...
                var deferredCursor = new DeferredAsyncCursor<TResult>(ct => ExecuteReadOperation(findOperation, ReadPreference.Primary, ct));
                return await Task.FromResult(deferredCursor).ConfigureAwait(false);
            }
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

        private void AssignId(TDocument document)
        {
            var idProvider = _documentSerializer as IBsonIdProvider;
            if (idProvider != null)
            {
                object id;
                Type idNominalType;
                IIdGenerator idGenerator;
                if (idProvider.GetDocumentId(document, out id, out idNominalType, out idGenerator))
                {
                    if (idGenerator != null && idGenerator.IsEmpty(id))
                    {
                        id = idGenerator.GenerateId(this, document);
                        idProvider.SetDocumentId(document, id);
                    }
                }
            }
        }

        private WriteRequest ConvertWriteModelToWriteRequest(WriteModel<TDocument> model, int index)
        {
            switch (model.ModelType)
            {
                case WriteModelType.InsertOne:
                    var insertOneModel = (InsertOneModel<TDocument>)model;
                    AssignId(insertOneModel.Document);
                    return new InsertRequest(new BsonDocumentWrapper(insertOneModel.Document, _documentSerializer))
                    {
                        CorrelationId = index
                    };
                case WriteModelType.DeleteMany:
                    var deleteManyModel = (DeleteManyModel<TDocument>)model;
                    return new DeleteRequest(deleteManyModel.Filter.Render(_documentSerializer, _settings.SerializerRegistry))
                    {
                        CorrelationId = index,
                        Limit = 0
                    };
                case WriteModelType.DeleteOne:
                    var deleteOneModel = (DeleteOneModel<TDocument>)model;
                    return new DeleteRequest(deleteOneModel.Filter.Render(_documentSerializer, _settings.SerializerRegistry))
                    {
                        CorrelationId = index,
                        Limit = 1
                    };
                case WriteModelType.ReplaceOne:
                    var replaceOneModel = (ReplaceOneModel<TDocument>)model;
                    return new UpdateRequest(
                        UpdateType.Replacement,
                        replaceOneModel.Filter.Render(_documentSerializer, _settings.SerializerRegistry),
                        new BsonDocumentWrapper(replaceOneModel.Replacement, _documentSerializer))
                    {
                        CorrelationId = index,
                        IsMulti = false,
                        IsUpsert = replaceOneModel.IsUpsert
                    };
                case WriteModelType.UpdateMany:
                    var updateManyModel = (UpdateManyModel<TDocument>)model;
                    return new UpdateRequest(
                        UpdateType.Update,
                        updateManyModel.Filter.Render(_documentSerializer, _settings.SerializerRegistry),
                        updateManyModel.Update.Render(_documentSerializer, _settings.SerializerRegistry))
                    {
                        CorrelationId = index,
                        IsMulti = true,
                        IsUpsert = updateManyModel.IsUpsert
                    };
                case WriteModelType.UpdateOne:
                    var updateOneModel = (UpdateOneModel<TDocument>)model;
                    return new UpdateRequest(
                        UpdateType.Update,
                        updateOneModel.Filter.Render(_documentSerializer, _settings.SerializerRegistry),
                        updateOneModel.Update.Render(_documentSerializer, _settings.SerializerRegistry))
                    {
                        CorrelationId = index,
                        IsMulti = false,
                        IsUpsert = updateOneModel.IsUpsert
                    };
                default:
                    throw new InvalidOperationException("Unknown type of WriteModel provided.");
            }
        }

        private Task<TResult> ExecuteReadOperation<TResult>(IReadOperation<TResult> operation, CancellationToken cancellationToken)
        {
            return ExecuteReadOperation(operation, _settings.ReadPreference, cancellationToken);
        }

        private async Task<TResult> ExecuteReadOperation<TResult>(IReadOperation<TResult> operation, ReadPreference readPreference, CancellationToken cancellationToken)
        {
            using (var binding = new ReadPreferenceBinding(_cluster, readPreference))
            {
                return await _operationExecutor.ExecuteReadOperationAsync(binding, operation, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<TResult> ExecuteWriteOperation<TResult>(IWriteOperation<TResult> operation, CancellationToken cancellationToken)
        {
            using (var binding = new WritableServerBinding(_cluster))
            {
                return await _operationExecutor.ExecuteWriteOperationAsync(binding, operation, cancellationToken).ConfigureAwait(false);
            }
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

        private class MongoIndexManager : MongoIndexManagerBase<TDocument>
        {
            private readonly MongoCollectionImpl<TDocument> _collection;

            public MongoIndexManager(MongoCollectionImpl<TDocument> collection)
            {
                _collection = collection;
            }

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

            public async override Task<IEnumerable<string>> CreateManyAsync(IEnumerable<CreateIndexModel<TDocument>> models, CancellationToken cancellationToken = default(CancellationToken))
            {
                Ensure.IsNotNull(models, "models");

                var requests = models.Select(m =>
                {
                    var keysDocument = m.Keys.Render(_collection._documentSerializer, _collection._settings.SerializerRegistry);
                    var options = m.Options ?? new CreateIndexOptions();
                    return new CreateIndexRequest(keysDocument)
                    {
                        Name = options.Name,
                        Background = options.Background,
                        Bits = options.Bits,
                        BucketSize = options.BucketSize,
                        DefaultLanguage = options.DefaultLanguage,
                        ExpireAfter = options.ExpireAfter,
                        LanguageOverride = options.LanguageOverride,
                        Max = options.Max,
                        Min = options.Min,
                        Sparse = options.Sparse,
                        SphereIndexVersion = options.SphereIndexVersion,
                        StorageEngine = options.StorageEngine,
                        TextIndexVersion = options.TextIndexVersion,
                        Unique = options.Unique,
                        Version = options.Version,
                        Weights = options.Weights
                    };
                });

                var operation = new CreateIndexesOperation(_collection._collectionNamespace, requests, _collection._messageEncoderSettings);
                await _collection.ExecuteWriteOperation(operation, cancellationToken).ConfigureAwait(false);

                return requests.Select(x => x.GetIndexName());
            }

            public override Task DropAllAsync(CancellationToken cancellationToken)
            {
                var operation = new DropIndexOperation(_collection._collectionNamespace, "*", _collection._messageEncoderSettings);

                return _collection.ExecuteWriteOperation(operation, cancellationToken);
            }

            public override Task DropOneAsync(string name, CancellationToken cancellationToken)
            {
                Ensure.IsNotNullOrEmpty(name, "name");
                if (name == "*")
                {
                    throw new ArgumentException("Cannot specify '*' for the index name. Use DropAllAsync to drop all indexes.", "name");
                }

                var operation = new DropIndexOperation(_collection._collectionNamespace, name, _collection._messageEncoderSettings);

                return _collection.ExecuteWriteOperation(operation, cancellationToken);
            }

            public override Task<IAsyncCursor<BsonDocument>> ListAsync(CancellationToken cancellationToken = default(CancellationToken))
            {
                var op = new ListIndexesOperation(_collection._collectionNamespace, _collection._messageEncoderSettings);
                return _collection.ExecuteReadOperation(op, ReadPreference.Primary, cancellationToken);
            }
        }

    }
}
