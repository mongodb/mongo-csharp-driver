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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.Driver.Linq.Utils;

namespace MongoDB.Driver
{
    internal sealed class MongoCollectionImpl<TDocument> : IMongoCollection<TDocument>, IMongoIndexManager<TDocument>
    {
        // fields
        private readonly ICluster _cluster;
        private readonly CollectionNamespace _collectionNamespace;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly IOperationExecutor _operationExecutor;
        private readonly IBsonSerializer<TDocument> _serializer;
        private readonly MongoCollectionSettings _settings;

        // constructors
        public MongoCollectionImpl(CollectionNamespace collectionNamespace, MongoCollectionSettings settings, ICluster cluster, IOperationExecutor operationExecutor)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _settings = Ensure.IsNotNull(settings, "settings").Freeze();
            _cluster = Ensure.IsNotNull(cluster, "cluster");
            _operationExecutor = Ensure.IsNotNull(operationExecutor, "operationExecutor");

            _serializer = _settings.SerializerRegistry.GetSerializer<TDocument>();
            _messageEncoderSettings = new MessageEncoderSettings
            {
                { MessageEncoderSettingsName.GuidRepresentation, _settings.GuidRepresentation },
                { MessageEncoderSettingsName.ReadEncoding, _settings.ReadEncoding ?? Utf8Encodings.Strict },
                { MessageEncoderSettingsName.WriteEncoding, _settings.WriteEncoding ?? Utf8Encodings.Strict }
            };
        }

        // properties
        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        public IMongoIndexManager<TDocument> IndexManager
        {
            get { return this; }
        }

        public MongoCollectionSettings Settings
        {
            get { return _settings; }
        }

        // methods
        public IAggregateFluent<TDocument, TDocument> Aggregate(AggregateOptions options)
        {
            options = options ?? new AggregateOptions();
            return new AggregateFluent<TDocument, TDocument>(this, new List<object>(), options, _serializer);
        }

        public async Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(IEnumerable<object> pipeline, AggregateOptions<TResult> options, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(pipeline, "pipeline");

            options = options ?? new AggregateOptions<TResult>();
            var pipelineDocuments = pipeline.Select(x => ConvertToBsonDocument(x)).ToList();

            var last = pipelineDocuments.LastOrDefault();
            if (last != null && last.GetElement(0).Name == "$out")
            {
                var operation = new AggregateToCollectionOperation(
                    _collectionNamespace,
                    pipelineDocuments,
                    _messageEncoderSettings)
                {
                    AllowDiskUse = options.AllowDiskUse,
                    MaxTime = options.MaxTime
                };

                await ExecuteWriteOperation(operation, cancellationToken).ConfigureAwait(false);

                var outputCollectionName = last.GetElement(0).Value.AsString;
                var resultSerializer = ResolveResultSerializer(options.ResultSerializer);

                var findOperation = new FindOperation<TResult>(
                    new CollectionNamespace(_collectionNamespace.DatabaseNamespace, outputCollectionName),
                    resultSerializer,
                    _messageEncoderSettings)
                {
                    BatchSize = options.BatchSize,
                    MaxTime = options.MaxTime
                };

                // we want to delay execution of the find because the user may
                // not want to iterate the results at all...
                return await Task.FromResult<IAsyncCursor<TResult>>(new DeferredAsyncCursor<TResult>(ct => ExecuteReadOperation(findOperation, ct))).ConfigureAwait(false);
            }
            else
            {
                var operation = CreateAggregateOperation<TResult>(options, pipelineDocuments);
                return await ExecuteReadOperation(operation, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<BulkWriteResult<TDocument>> BulkWriteAsync(IEnumerable<WriteModel<TDocument>> requests, BulkWriteOptions options, CancellationToken cancellationToken)
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

        public Task<long> CountAsync(object filter, CountOptions options, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(filter, "filter");

            options = options ?? new CountOptions();

            var operation = new CountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Filter = ConvertFilterToBsonDocument(filter),
                Hint = options.Hint is string ? BsonValue.Create((string)options.Hint) : ConvertToBsonDocument(options.Hint),
                Limit = options.Limit,
                MaxTime = options.MaxTime,
                Skip = options.Skip
            };

            return ExecuteReadOperation(operation, cancellationToken);
        }

        public Task CreateIndexAsync(object keys, CreateIndexOptions options, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(keys, "keys");

            var keysDocument = ConvertToBsonDocument(keys);

            options = options ?? new CreateIndexOptions();
            var request = new CreateIndexRequest(keysDocument)
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
                StorageEngine = ConvertToBsonDocument(options.StorageEngine),
                TextIndexVersion = options.TextIndexVersion,
                Unique = options.Unique,
                Version = options.Version,
                Weights = ConvertToBsonDocument(options.Weights)
            };

            var operation = new CreateIndexesOperation(_collectionNamespace, new[] { request }, _messageEncoderSettings);
            return ExecuteWriteOperation(operation, cancellationToken);
        }

        public async Task<DeleteResult> DeleteManyAsync(object filter, CancellationToken cancellationToken)
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

        public async Task<DeleteResult> DeleteOneAsync(object filter, CancellationToken cancellationToken)
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

        public Task<IReadOnlyList<TResult>> DistinctAsync<TResult>(string fieldName, object filter, DistinctOptions<TResult> options, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(fieldName, "fieldName");
            Ensure.IsNotNull(filter, "filter");

            options = options ?? new DistinctOptions<TResult>();
            var resultSerializer = ResolveResultSerializer(options.ResultSerializer);

            var operation = new DistinctOperation<TResult>(
                _collectionNamespace,
                resultSerializer,
                fieldName,
                _messageEncoderSettings)
            {
                Filter = ConvertFilterToBsonDocument(filter),
                MaxTime = options.MaxTime
            };

            return ExecuteReadOperation(operation, cancellationToken);
        }

        public Task DropIndexAsync(string name, CancellationToken cancellationToken)
        {
            Ensure.IsNotNullOrEmpty(name, "name");

            var operation = new DropIndexOperation(_collectionNamespace, name, _messageEncoderSettings);

            return ExecuteWriteOperation(operation, cancellationToken);
        }

        public Task DropIndexAsync(object keys, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(keys, "keys");

            var keysDocument = ConvertToBsonDocument(keys);
            var operation = new DropIndexOperation(_collectionNamespace, keysDocument, _messageEncoderSettings);

            return ExecuteWriteOperation(operation, cancellationToken);
        }

        public FindFluent<TDocument, TDocument> Find(object filter)
        {
            var options = new FindOptions<TDocument>();
            return new FindFluent<TDocument, TDocument>(this, filter, options);
        }

        public Task<IAsyncCursor<TResult>> FindAsync<TResult>(object filter, FindOptions<TResult> options, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(filter, "filter");

            options = options ?? new FindOptions<TResult>();
            var resultSerializer = ResolveResultSerializer(options.ResultSerializer);

            var operation = new FindOperation<TResult>(
                _collectionNamespace,
                resultSerializer,
                _messageEncoderSettings)
            {
                AllowPartialResults = options.AllowPartialResults,
                BatchSize = options.BatchSize,
                Comment = options.Comment,
                CursorType = options.CursorType.ToCore(),
                Filter = ConvertFilterToBsonDocument(filter),
                Limit = options.Limit,
                MaxTime = options.MaxTime,
                Modifiers = options.Modifiers,
                NoCursorTimeout = options.NoCursorTimeout,
                Projection = ConvertToBsonDocument(options.Projection),
                Skip = options.Skip,
                Sort = ConvertToBsonDocument(options.Sort),
            };

            return ExecuteReadOperation(operation, cancellationToken);
        }

        public Task<TDocument> FindOneAndDeleteAsync(object filter, FindOneAndDeleteOptions<TDocument> options, CancellationToken cancellationToken)
        {
            return FindOneAndDeleteAsync<TDocument>(filter, options, cancellationToken);
        }

        public Task<TResult> FindOneAndDeleteAsync<TResult>(object filter, FindOneAndDeleteOptions<TResult> options, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(filter, "filter");

            options = options ?? new FindOneAndDeleteOptions<TResult>();
            var resultSerializer = ResolveResultSerializer(options.ResultSerializer);

            var operation = new FindOneAndDeleteOperation<TResult>(
                _collectionNamespace,
                ConvertFilterToBsonDocument(filter),
                new FindAndModifyValueDeserializer<TResult>(resultSerializer),
                _messageEncoderSettings)
            {
                MaxTime = options.MaxTime,
                Projection = ConvertToBsonDocument(options.Projection),
                Sort = ConvertToBsonDocument(options.Sort)
            };

            return ExecuteWriteOperation(operation, cancellationToken);
        }

        public Task<TDocument> FindOneAndReplaceAsync(object filter, TDocument replacement, FindOneAndReplaceOptions<TDocument> options, CancellationToken cancellationToken)
        {
            return FindOneAndReplaceAsync<TDocument>(filter, replacement, options, cancellationToken);
        }

        public Task<TResult> FindOneAndReplaceAsync<TResult>(object filter, TDocument replacement, FindOneAndReplaceOptions<TResult> options, CancellationToken cancellationToken)
        {
            var replacementObject = (object)replacement; // only box once if it's a struct
            Ensure.IsNotNull(filter, "filter");
            Ensure.IsNotNull(replacementObject, "replacement");

            options = options ?? new FindOneAndReplaceOptions<TResult>();
            var resultSerializer = ResolveResultSerializer(options.ResultSerializer);

            var operation = new FindOneAndReplaceOperation<TResult>(
                _collectionNamespace,
                ConvertFilterToBsonDocument(filter),
                ConvertToBsonDocument(replacementObject),
                new FindAndModifyValueDeserializer<TResult>(resultSerializer),
                _messageEncoderSettings)
            {
                IsUpsert = options.IsUpsert,
                MaxTime = options.MaxTime,
                Projection = ConvertToBsonDocument(options.Projection),
                ReturnDocument = options.ReturnDocument.ToCore(),
                Sort = ConvertToBsonDocument(options.Sort)
            };

            return ExecuteWriteOperation(operation, cancellationToken);
        }

        public Task<TDocument> FindOneAndUpdateAsync(object filter, object update, FindOneAndUpdateOptions<TDocument> options, CancellationToken cancellationToken)
        {
            return FindOneAndUpdateAsync<TDocument>(filter, update, options, cancellationToken);
        }

        public Task<TResult> FindOneAndUpdateAsync<TResult>(object filter, object update, FindOneAndUpdateOptions<TResult> options, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(filter, "filter");
            Ensure.IsNotNull(update, "update");

            options = options ?? new FindOneAndUpdateOptions<TResult>();
            var resultSerializer = ResolveResultSerializer(options.ResultSerializer);

            var operation = new FindOneAndUpdateOperation<TResult>(
                _collectionNamespace,
                ConvertFilterToBsonDocument(filter),
                ConvertToBsonDocument(update),
                new FindAndModifyValueDeserializer<TResult>(resultSerializer),
                _messageEncoderSettings)
            {
                IsUpsert = options.IsUpsert,
                MaxTime = options.MaxTime,
                Projection = ConvertToBsonDocument(options.Projection),
                ReturnDocument = options.ReturnDocument.ToCore(),
                Sort = ConvertToBsonDocument(options.Sort)
            };

            return ExecuteWriteOperation(operation, cancellationToken);
        }

        public Task<IAsyncCursor<BsonDocument>> GetIndexesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var op = new ListIndexesOperation(_collectionNamespace, _messageEncoderSettings);
            return ExecuteReadOperation(op, cancellationToken);
        }

        public async Task InsertOneAsync(TDocument document, CancellationToken cancellationToken)
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

        public async Task<ReplaceOneResult> ReplaceOneAsync(object filter, TDocument replacement, UpdateOptions options, CancellationToken cancellationToken)
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

        public async Task<UpdateResult> UpdateManyAsync(object filter, object update, UpdateOptions options, CancellationToken cancellationToken)
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

        public async Task<UpdateResult> UpdateOneAsync(object filter, object update, UpdateOptions options, CancellationToken cancellationToken)
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

        private void AssignId(TDocument document)
        {
            var idProvider = _serializer as IBsonIdProvider;
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
                    return new InsertRequest(new BsonDocumentWrapper(insertOneModel.Document, _serializer))
                    {
                        CorrelationId = index
                    };
                case WriteModelType.DeleteMany:
                    var removeManyModel = (DeleteManyModel<TDocument>)model;
                    return new DeleteRequest(ConvertFilterToBsonDocument(removeManyModel.Filter))
                    {
                        CorrelationId = index,
                        Limit = 0
                    };
                case WriteModelType.DeleteOne:
                    var removeOneModel = (DeleteOneModel<TDocument>)model;
                    return new DeleteRequest(ConvertFilterToBsonDocument(removeOneModel.Filter))
                    {
                        CorrelationId = index,
                        Limit = 1
                    };
                case WriteModelType.ReplaceOne:
                    var replaceOneModel = (ReplaceOneModel<TDocument>)model;
                    return new UpdateRequest(
                        UpdateType.Replacement,
                        ConvertFilterToBsonDocument(replaceOneModel.Filter),
                        new BsonDocumentWrapper(replaceOneModel.Replacement, _serializer))
                    {
                        CorrelationId = index,
                        IsMulti = false,
                        IsUpsert = replaceOneModel.IsUpsert
                    };
                case WriteModelType.UpdateMany:
                    var updateManyModel = (UpdateManyModel<TDocument>)model;
                    return new UpdateRequest(
                        UpdateType.Update,
                        ConvertFilterToBsonDocument(updateManyModel.Filter),
                        ConvertToBsonDocument(updateManyModel.Update))
                    {
                        CorrelationId = index,
                        IsMulti = true,
                        IsUpsert = updateManyModel.IsUpsert
                    };
                case WriteModelType.UpdateOne:
                    var updateOneModel = (UpdateOneModel<TDocument>)model;
                    return new UpdateRequest(
                        UpdateType.Update,
                        ConvertFilterToBsonDocument(updateOneModel.Filter),
                        ConvertToBsonDocument(updateOneModel.Update))
                    {
                        CorrelationId = index,
                        IsMulti = false,
                        IsUpsert = updateOneModel.IsUpsert
                    };
                default:
                    throw new InvalidOperationException("Unknown type of WriteModel provided.");
            }
        }

        private BsonDocument ConvertToBsonDocument(object document)
        {
            return BsonDocumentHelper.ToBsonDocument(_settings.SerializerRegistry, document);
        }

        private BsonDocument ConvertFilterToBsonDocument(object filter)
        {
            return BsonDocumentHelper.FilterToBsonDocument<TDocument>(_settings.SerializerRegistry, filter);
        }

        private async Task<TResult> ExecuteReadOperation<TResult>(IReadOperation<TResult> operation, CancellationToken cancellationToken)
        {
            using (var binding = new ReadPreferenceBinding(_cluster, _settings.ReadPreference))
            {
                return await _operationExecutor.ExecuteReadOperationAsync(binding, operation, _settings.OperationTimeout, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<TResult> ExecuteWriteOperation<TResult>(IWriteOperation<TResult> operation, CancellationToken cancellationToken)
        {
            using (var binding = new WritableServerBinding(_cluster))
            {
                return await _operationExecutor.ExecuteWriteOperationAsync(binding, operation, _settings.OperationTimeout, cancellationToken).ConfigureAwait(false);
            }
        }

        private AggregateOperation<TResult> CreateAggregateOperation<TResult>(AggregateOptions<TResult> options, List<BsonDocument> pipeline)
        {
            var resultSerializer = ResolveResultSerializer(options.ResultSerializer);

            return new AggregateOperation<TResult>(
                _collectionNamespace,
                pipeline,
                resultSerializer,
                _messageEncoderSettings)
            {
                AllowDiskUse = options.AllowDiskUse,
                BatchSize = options.BatchSize,
                MaxTime = options.MaxTime,
                UseCursor = options.UseCursor
            };
        }

        private IBsonSerializer<TResult> ResolveResultSerializer<TResult>(IBsonSerializer<TResult> resultSerializer)
        {
            if (resultSerializer != null)
            {
                return resultSerializer;
            }

            if (typeof(TResult) == typeof(TDocument) && _serializer != null)
            {
                return (IBsonSerializer<TResult>)_serializer;
            }

            return _settings.SerializerRegistry.GetSerializer<TResult>();
        }
    }
}
