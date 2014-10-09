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
using System.Reflection;
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
    internal sealed class MongoCollectionImpl<TDocument> : IMongoCollection<TDocument>
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

        public MongoCollectionSettings Settings
        {
            get { return _settings; }
        }

        // methods
        public async Task<IAsyncEnumerable<TResult>> AggregateAsync<TResult>(AggregateModel<TResult> model, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(model, "model");

            var pipeline = model.Pipeline.Select(x => ConvertToBsonDocument(x)).ToList();

            var last = pipeline.LastOrDefault();
            if (last != null && last.GetElement(0).Name == "$out")
            {
                var operation = new AggregateToCollectionOperation(
                    _collectionNamespace,
                    pipeline,
                    _messageEncoderSettings)
                {
                    AllowDiskUse = model.AllowDiskUse,
                    MaxTime = model.MaxTime
                };

                await ExecuteWriteOperation(operation, timeout, cancellationToken).ConfigureAwait(false);

                var outputCollectionName = last.GetElement(0).Value.AsString;
                var findOperation = new FindOperation<TResult>(
                    new CollectionNamespace(_collectionNamespace.DatabaseNamespace, outputCollectionName),
                    model.ResultSerializer ?? _settings.SerializerRegistry.GetSerializer<TResult>(),
                    _messageEncoderSettings)
                {
                    BatchSize = model.BatchSize,
                    MaxTime = model.MaxTime
                };

                return await Task.FromResult<IAsyncEnumerable<TResult>>(new AsyncCursorAsyncEnumerable<TResult>(
                    () => ExecuteReadOperation(findOperation, timeout, cancellationToken),
                    null)).ConfigureAwait(false);
            }
            else
            {
                var operation = CreateAggregateOperation<TResult>(model, pipeline);

                return await Task.FromResult<IAsyncEnumerable<TResult>>(new AsyncCursorAsyncEnumerable<TResult>(
                    () => ExecuteReadOperation(operation, timeout, cancellationToken),
                    null)).ConfigureAwait(false);
            }
        }

        public async Task<BulkWriteResult<TDocument>> BulkWriteAsync(BulkWriteModel<TDocument> model, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(model, "model");

            var operation = new BulkMixedWriteOperation(
                _collectionNamespace,
                model.Requests.Select(ConvertWriteModelToWriteRequest),
                _messageEncoderSettings)
            {
                IsOrdered = model.IsOrdered,
                WriteConcern = _settings.WriteConcern
            };

            try
            {
                var result = await ExecuteWriteOperation(operation, timeout, cancellationToken).ConfigureAwait(false);
                return BulkWriteResult<TDocument>.FromCore(result, model.Requests);
            }
            catch (BulkWriteOperationException ex)
            {
                throw BulkWriteException<TDocument>.FromCore(ex, model.Requests);
            }
        }

        public Task<long> CountAsync(object criteria, CountOptions options, TimeSpan? timeout, CancellationToken cancellationToken)
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

            return ExecuteReadOperation(operation, timeout, cancellationToken);
        }

        public async Task<DeleteResult> DeleteManyAsync(object criteria, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(criteria, "criteria");

            var model = new DeleteManyModel<TDocument>(ConvertToBsonDocument(criteria));
            try
            {
                var bulkModel = new BulkWriteModel<TDocument>(new[] { model });
                var result = await BulkWriteAsync(bulkModel, timeout, cancellationToken).ConfigureAwait(false);
                return DeleteResult.FromCore(result);
            }
            catch (BulkWriteException<TDocument> ex)
            {
                throw WriteException.FromBulkWriteException(ex);
            }
        }

        public async Task<DeleteResult> DeleteOneAsync(object criteria, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(criteria, "criteria");

            var model = new DeleteOneModel<TDocument>(ConvertToBsonDocument(criteria));
            try
            {
                var bulkModel = new BulkWriteModel<TDocument>(new[] { model });
                var result = await BulkWriteAsync(bulkModel, timeout, cancellationToken).ConfigureAwait(false);
                return DeleteResult.FromCore(result);
            }
            catch (BulkWriteException<TDocument> ex)
            {
                throw WriteException.FromBulkWriteException(ex);
            }
        }

        public Task<IReadOnlyList<TResult>> DistinctAsync<TResult>(string fieldName, DistinctOptions<TResult> options, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(fieldName, "fieldName");

            options = options ?? new DistinctOptions<TResult>();
            var resultSerializer = options.ResultSerializer ?? _settings.SerializerRegistry.GetSerializer<TResult>();
            var operation = new DistinctOperation<TResult>(
                _collectionNamespace,
                resultSerializer,
                fieldName,
                _messageEncoderSettings)
            {
                Criteria = ConvertToBsonDocument(options.Criteria),
                MaxTime = options.MaxTime
            };

            return ExecuteReadOperation(operation, timeout, cancellationToken);
        }

        public FindFluent<TDocument, TDocument> Find(object criteria)
        {
            var find = new FindFluent<TDocument, TDocument>(this, criteria);
            return find;
        }

        public Task<IAsyncEnumerable<TResult>> FindAsync<TResult>(FindOptions<TResult> model, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(model, "model");

            var operation = CreateFindOperation<TResult>(model);

            return Task.FromResult<IAsyncEnumerable<TResult>>(new AsyncCursorAsyncEnumerable<TResult>(
                () => ExecuteReadOperation(operation, timeout, cancellationToken),
                model.Limit));
        }

        public Task<TResult> FindOneAndDeleteAsync<TResult>(object criteria, FindOneAndDeleteOptions<TResult> options, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(criteria, "criteria");

            options = options ?? new FindOneAndDeleteOptions<TResult>();
            var resultSerializer = options.ResultSerializer ?? _settings.SerializerRegistry.GetSerializer<TResult>();
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

            return ExecuteWriteOperation(operation, timeout, cancellationToken);
        }

        public Task<TResult> FindOneAndReplaceAsync<TResult>(object criteria, TDocument replacement, FindOneAndReplaceOptions<TResult> options, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(criteria, "criteria");
            // how to check replacement - it could be a struct

            options = options ?? new FindOneAndReplaceOptions<TResult>();
            var resultSerializer = options.ResultSerializer ?? _settings.SerializerRegistry.GetSerializer<TResult>();
            var operation = new FindOneAndReplaceOperation<TResult>(
                _collectionNamespace,
                ConvertToBsonDocument(criteria),
                ConvertToBsonDocument(replacement),
                new FindAndModifyValueDeserializer<TResult>(resultSerializer),
                _messageEncoderSettings)
            {
                IsUpsert = options.IsUpsert,
                MaxTime = options.MaxTime,
                Projection = ConvertToBsonDocument(options.Projection),
                ReturnOriginal = options.ReturnOriginal,
                Sort = ConvertToBsonDocument(options.Sort)
            };

            return ExecuteWriteOperation(operation, timeout, cancellationToken);
        }

        public Task<TResult> FindOneAndUpdateAsync<TResult>(object criteria, object update, FindOneAndUpdateOptions<TResult> options, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(criteria, "criteria");
            Ensure.IsNotNull(update, "update");

            options = options ?? new FindOneAndUpdateOptions<TResult>();
            var operation = new FindOneAndUpdateOperation<TResult>(
                _collectionNamespace,
                ConvertToBsonDocument(criteria),
                ConvertToBsonDocument(update),
                new FindAndModifyValueDeserializer<TResult>(_settings.SerializerRegistry.GetSerializer<TResult>()),
                _messageEncoderSettings)
            {
                IsUpsert = options.IsUpsert,
                MaxTime = options.MaxTime,
                Projection = ConvertToBsonDocument(options.Projection),
                ReturnOriginal = options.ReturnOriginal,
                Sort = ConvertToBsonDocument(options.Sort)
            };

            return ExecuteWriteOperation(operation, timeout, cancellationToken);
        }

        public async Task InsertOneAsync(TDocument document, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            // how to check replacement - it could be a struct

            var model = new InsertOneModel<TDocument>(document);
            try
            {
                var bulkModel = new BulkWriteModel<TDocument>(new[] { model });
                await BulkWriteAsync(bulkModel, timeout, cancellationToken).ConfigureAwait(false);
            }
            catch (BulkWriteException<TDocument> ex)
            {
                throw WriteException.FromBulkWriteException(ex);
            }
        }

        public async Task<ReplaceOneResult> ReplaceOneAsync(object criteria, TDocument replacement, UpdateOptions options, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(criteria, "criteria");
            // how to validate replacement - it might be a struct

            options = options ?? new UpdateOptions();
            var model = new ReplaceOneModel<TDocument>(criteria, replacement)
            {
                IsUpsert = options.IsUpsert
            };
            try
            {
                var bulkModel = new BulkWriteModel<TDocument>(new[] { model });
                var result = await BulkWriteAsync(bulkModel, timeout, cancellationToken).ConfigureAwait(false);
                return ReplaceOneResult.FromCore(result);
            }
            catch (BulkWriteException<TDocument> ex)
            {
                throw WriteException.FromBulkWriteException(ex);
            }
        }

        public async Task<UpdateResult> UpdateManyAsync(object criteria, object update, UpdateOptions options, TimeSpan? timeout, CancellationToken cancellationToken)
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
                var bulkModel = new BulkWriteModel<TDocument>(new[] { model });
                var result = await BulkWriteAsync(bulkModel, timeout, cancellationToken).ConfigureAwait(false);
                return UpdateResult.FromCore(result);
            }
            catch (BulkWriteException<TDocument> ex)
            {
                throw WriteException.FromBulkWriteException(ex);
            }
        }

        public async Task<UpdateResult> UpdateOneAsync(object criteria, object update, UpdateOptions options, TimeSpan? timeout, CancellationToken cancellationToken)
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
                var bulkModel = new BulkWriteModel<TDocument>(new[] { model });
                var result = await BulkWriteAsync(bulkModel, timeout, cancellationToken).ConfigureAwait(false);
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

        private async Task<TResult> ExecuteReadOperation<TResult>(IReadOperation<TResult> operation, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            using (var binding = new ReadPreferenceBinding(_cluster, _settings.ReadPreference))
            {
                return await _operationExecutor.ExecuteReadOperationAsync(binding, operation, timeout ?? _settings.OperationTimeout, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<TResult> ExecuteWriteOperation<TResult>(IWriteOperation<TResult> operation, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            using (var binding = new WritableServerBinding(_cluster))
            {
                return await _operationExecutor.ExecuteWriteOperationAsync(binding, operation, timeout ?? _settings.OperationTimeout, cancellationToken).ConfigureAwait(false);
            }
        }

        private AggregateOperation<TResult> CreateAggregateOperation<TResult>(AggregateModel<TResult> model, List<BsonDocument> pipeline)
        {
            var resultSerializer = model.ResultSerializer ?? _settings.SerializerRegistry.GetSerializer<TResult>();

            return new AggregateOperation<TResult>(
                _collectionNamespace,
                pipeline,
                resultSerializer,
                _messageEncoderSettings)
            {
                AllowDiskUse = model.AllowDiskUse,
                BatchSize = model.BatchSize,
                MaxTime = model.MaxTime,
                UseCursor = model.UseCursor
            };
        }

        private FindOperation<TResult> CreateFindOperation<TResult>(FindOptions<TResult> model)
        {
            var resultSerializer = model.ResultSerializer ?? _settings.SerializerRegistry.GetSerializer<TResult>();

            return new FindOperation<TResult>(
                _collectionNamespace,
                resultSerializer,
                _messageEncoderSettings)
            {
                AwaitData = model.AwaitData,
                BatchSize = model.BatchSize,
                Comment = model.Comment,
                Criteria = ConvertToBsonDocument(model.Criteria),
                Limit = model.Limit,
                MaxTime = model.MaxTime,
                Modifiers = model.Modifiers,
                NoCursorTimeout = model.NoCursorTimeout,
                Partial = model.Partial,
                Projection = ConvertToBsonDocument(model.Projection),
                Skip = model.Skip,
                Sort = ConvertToBsonDocument(model.Sort),
                Tailable = model.Tailable
            };
        }
    }
}
