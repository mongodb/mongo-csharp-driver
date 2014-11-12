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
            _settings = Ensure.IsNotNull(settings, "settings");
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
        public AggregateFluent<TDocument, TDocument> Aggregate(AggregateOptions options)
        {
            options = options ?? new AggregateOptions();
            return new AggregateFluent<TDocument, TDocument>(this, ConvertToBsonDocument, new List<object>(), options, _serializer);
        }

        public async Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(IEnumerable<object> pipeline, AggregateOptions<TResult> options,  CancellationToken cancellationToken)
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
                return await Task.FromResult<IAsyncCursor<TResult>>(new DeferredAsyncCursor<TResult>(ct => ExecuteReadOperation(findOperation,  ct))).ConfigureAwait(false);
            }
            else
            {
                var operation = CreateAggregateOperation<TResult>(options, pipelineDocuments);
                return await ExecuteReadOperation(operation,  cancellationToken);
            }
        }

        public async Task<BulkWriteResult<TDocument>> BulkWriteAsync(IEnumerable<WriteModel<TDocument>> requests, BulkWriteOptions options,  CancellationToken cancellationToken)
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
                var result = await ExecuteWriteOperation(operation,  cancellationToken).ConfigureAwait(false);
                return BulkWriteResult<TDocument>.FromCore(result, requests);
            }
            catch (BulkWriteOperationException ex)
            {
                throw BulkWriteException<TDocument>.FromCore(ex, requests.ToList());
            }
        }

        public Task<long> CountAsync(object criteria, CountOptions options,  CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(criteria, "criteria");

            options = options ?? new CountOptions();

            var operation = new CountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Criteria = ConvertToBsonDocument(criteria),
                Hint = options.Hint is string ? BsonValue.Create((string)options.Hint) : ConvertToBsonDocument(options.Hint),
                Limit = options.Limit,
                MaxTime = options.MaxTime,
                Skip = options.Skip
            };

            return ExecuteReadOperation(operation, cancellationToken);
        }

        public async Task<DeleteResult> DeleteManyAsync(object criteria,  CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(criteria, "criteria");

            var model = new DeleteManyModel<TDocument>(ConvertToBsonDocument(criteria));
            try
            {
                var result = await BulkWriteAsync(new[] { model }, null, cancellationToken).ConfigureAwait(false);
                return DeleteResult.FromCore(result);
            }
            catch (BulkWriteException<TDocument> ex)
            {
                throw WriteException.FromBulkWriteException(ex);
            }
        }

        public async Task<DeleteResult> DeleteOneAsync(object criteria,  CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(criteria, "criteria");

            var model = new DeleteOneModel<TDocument>(ConvertToBsonDocument(criteria));
            try
            {
                var result = await BulkWriteAsync(new[] { model }, null, cancellationToken).ConfigureAwait(false);
                return DeleteResult.FromCore(result);
            }
            catch (BulkWriteException<TDocument> ex)
            {
                throw WriteException.FromBulkWriteException(ex);
            }
        }

        public Task<IReadOnlyList<TResult>> DistinctAsync<TResult>(string fieldName, object criteria, DistinctOptions<TResult> options,  CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(fieldName, "fieldName");
            Ensure.IsNotNull(criteria, "criteria");

            options = options ?? new DistinctOptions<TResult>();
            var resultSerializer = ResolveResultSerializer(options.ResultSerializer);

            var operation = new DistinctOperation<TResult>(
                _collectionNamespace,
                resultSerializer,
                fieldName,
                _messageEncoderSettings)
            {
                Criteria = ConvertToBsonDocument(criteria),
                MaxTime = options.MaxTime
            };

            return ExecuteReadOperation(operation, cancellationToken);
        }

        public FindFluent<TDocument, TDocument> Find(object criteria)
        {
            var options = new FindOptions<TDocument>();
            return new FindFluent<TDocument, TDocument>(this, criteria, options);
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

        public Task<IAsyncCursor<TResult>> FindAsync<TResult>(object criteria, FindOptions<TResult> options,  CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(criteria, "criteria");

            options = options ?? new FindOptions<TResult>();
            var operation = CreateFindOperation<TResult>(criteria, options);
            return ExecuteReadOperation(operation, cancellationToken);
        }

        public Task<TResult> FindOneAndDeleteAsync<TResult>(object criteria, FindOneAndDeleteOptions<TResult> options,  CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(criteria, "criteria");

            options = options ?? new FindOneAndDeleteOptions<TResult>();
            var resultSerializer = ResolveResultSerializer(options.ResultSerializer);

            var operation = new FindOneAndDeleteOperation<TResult>(
                _collectionNamespace,
                ConvertToBsonDocument(criteria),
                new FindAndModifyValueDeserializer<TResult>(resultSerializer),
                _messageEncoderSettings)
            {
                MaxTime = options.MaxTime,
                Projection = ConvertToBsonDocument(options.Projection),
                Sort = ConvertToBsonDocument(options.Sort)
            };

            return ExecuteWriteOperation(operation, cancellationToken);
        }

        public Task<TResult> FindOneAndReplaceAsync<TResult>(object criteria, TDocument replacement, FindOneAndReplaceOptions<TResult> options,  CancellationToken cancellationToken)
        {
            var replacementObject = (object)replacement; // only box once if it's a struct
            Ensure.IsNotNull(criteria, "criteria");
            Ensure.IsNotNull(replacementObject, "replacement");

            options = options ?? new FindOneAndReplaceOptions<TResult>();
            var resultSerializer = ResolveResultSerializer(options.ResultSerializer);

            var operation = new FindOneAndReplaceOperation<TResult>(
                _collectionNamespace,
                ConvertToBsonDocument(criteria),
                ConvertToBsonDocument(replacementObject),
                new FindAndModifyValueDeserializer<TResult>(resultSerializer),
                _messageEncoderSettings)
            {
                IsUpsert = options.IsUpsert,
                MaxTime = options.MaxTime,
                Projection = ConvertToBsonDocument(options.Projection),
                ReturnOriginal = options.ReturnOriginal,
                Sort = ConvertToBsonDocument(options.Sort)
            };

            return ExecuteWriteOperation(operation,  cancellationToken);
        }

        public Task<TResult> FindOneAndUpdateAsync<TResult>(object criteria, object update, FindOneAndUpdateOptions<TResult> options,  CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(criteria, "criteria");
            Ensure.IsNotNull(update, "update");

            options = options ?? new FindOneAndUpdateOptions<TResult>();
            var resultSerializer = ResolveResultSerializer(options.ResultSerializer);

            var operation = new FindOneAndUpdateOperation<TResult>(
                _collectionNamespace,
                ConvertToBsonDocument(criteria),
                ConvertToBsonDocument(update),
                new FindAndModifyValueDeserializer<TResult>(resultSerializer),
                _messageEncoderSettings)
            {
                IsUpsert = options.IsUpsert,
                MaxTime = options.MaxTime,
                Projection = ConvertToBsonDocument(options.Projection),
                ReturnOriginal = options.ReturnOriginal,
                Sort = ConvertToBsonDocument(options.Sort)
            };

            return ExecuteWriteOperation(operation,  cancellationToken);
        }

        public Task<IReadOnlyList<BsonDocument>> GetIndexesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var op = new ListIndexesOperation(_collectionNamespace, _messageEncoderSettings);
            return ExecuteReadOperation(op, cancellationToken);
        }

        public async Task InsertOneAsync(TDocument document,  CancellationToken cancellationToken)
        {
            Ensure.IsNotNull((object)document, "document");

            var model = new InsertOneModel<TDocument>(document);
            try
            {
                await BulkWriteAsync(new [] { model }, null,  cancellationToken).ConfigureAwait(false);
            }
            catch (BulkWriteException<TDocument> ex)
            {
                throw WriteException.FromBulkWriteException(ex);
            }
        }

        public async Task<ReplaceOneResult> ReplaceOneAsync(object criteria, TDocument replacement, UpdateOptions options,  CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(criteria, "criteria");
            Ensure.IsNotNull((object)replacement, "replacement");

            options = options ?? new UpdateOptions();
            var model = new ReplaceOneModel<TDocument>(criteria, replacement)
            {
                IsUpsert = options.IsUpsert
            };

            try
            {
                var result = await BulkWriteAsync(new [] { model }, null,  cancellationToken).ConfigureAwait(false);
                return ReplaceOneResult.FromCore(result);
            }
            catch (BulkWriteException<TDocument> ex)
            {
                throw WriteException.FromBulkWriteException(ex);
            }
        }

        public async Task<UpdateResult> UpdateManyAsync(object criteria, object update, UpdateOptions options,  CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(criteria, "criteria");
            Ensure.IsNotNull(update, "update");

            options = options ?? new UpdateOptions();
            var model = new UpdateManyModel<TDocument>(criteria, update)
            {
                IsUpsert = options.IsUpsert
            };

            try
            {
                var result = await BulkWriteAsync(new [] { model }, null,  cancellationToken).ConfigureAwait(false);
                return UpdateResult.FromCore(result);
            }
            catch (BulkWriteException<TDocument> ex)
            {
                throw WriteException.FromBulkWriteException(ex);
            }
        }

        public async Task<UpdateResult> UpdateOneAsync(object criteria, object update, UpdateOptions options,  CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(criteria, "criteria");
            Ensure.IsNotNull(update, "update");

            options = options ?? new UpdateOptions();
            var model = new UpdateOneModel<TDocument>(criteria, update)
            {
                IsUpsert = options.IsUpsert
            };

            try
            {
                var result = await BulkWriteAsync(new [] { model }, null,  cancellationToken).ConfigureAwait(false);
                return UpdateResult.FromCore(result);
            }
            catch (BulkWriteException<TDocument> ex)
            {
                throw WriteException.FromBulkWriteException(ex);
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
                    return new DeleteRequest(ConvertToBsonDocument(removeManyModel.Criteria))
                    {
                        CorrelationId = index,
                        Limit = 0
                    };
                case WriteModelType.DeleteOne:
                    var removeOneModel = (DeleteOneModel<TDocument>)model;
                    return new DeleteRequest(ConvertToBsonDocument(removeOneModel.Criteria))
                    {
                        CorrelationId = index,
                        Limit = 1
                    };
                case WriteModelType.ReplaceOne:
                    var replaceOneModel = (ReplaceOneModel<TDocument>)model;
                    return new UpdateRequest(
                        UpdateType.Replacement,
                        ConvertToBsonDocument(replaceOneModel.Criteria),
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
                        ConvertToBsonDocument(updateManyModel.Criteria),
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
                        ConvertToBsonDocument(updateOneModel.Criteria),
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
            if (document == null)
            {
                return null;
            }

            var bsonDocument = document as BsonDocument;
            if (bsonDocument != null)
            {
                return bsonDocument;
            }

            if (document is string)
            {
                return BsonDocument.Parse((string)document);
            }

            var serializer = _settings.SerializerRegistry.GetSerializer(document.GetType());
            return new BsonDocumentWrapper(document, serializer);
        }

        private async Task<TResult> ExecuteReadOperation<TResult>(IReadOperation<TResult> operation,  CancellationToken cancellationToken)
        {
            using (var binding = new ReadPreferenceBinding(_cluster, _settings.ReadPreference))
            {
                return await _operationExecutor.ExecuteReadOperationAsync(binding, operation, _settings.OperationTimeout, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<TResult> ExecuteWriteOperation<TResult>(IWriteOperation<TResult> operation,  CancellationToken cancellationToken)
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

        private FindOperation<TResult> CreateFindOperation<TResult>(object criteria, FindOptions<TResult> options)
        {
            var resultSerializer = ResolveResultSerializer(options.ResultSerializer);

            return new FindOperation<TResult>(
                _collectionNamespace,
                resultSerializer,
                _messageEncoderSettings)
            {
                AwaitData = options.AwaitData,
                BatchSize = options.BatchSize,
                Comment = options.Comment,
                Criteria = ConvertToBsonDocument(criteria),
                Limit = options.Limit,
                MaxTime = options.MaxTime,
                Modifiers = options.Modifiers,
                NoCursorTimeout = options.NoCursorTimeout,
                Partial = options.Partial,
                Projection = ConvertToBsonDocument(options.Projection),
                Skip = options.Skip,
                Sort = ConvertToBsonDocument(options.Sort),
                Tailable = options.Tailable
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
