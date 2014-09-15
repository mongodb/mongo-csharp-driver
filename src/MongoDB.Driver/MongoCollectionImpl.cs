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
using MongoDB.Bson.Serialization.Serializers;
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
                { MessageEncoderSettingsName.ReadEncoding, _settings.ReadEncoding ?? Utf8Helper.StrictUtf8Encoding },
                { MessageEncoderSettingsName.WriteEncoding, _settings.WriteEncoding ?? Utf8Helper.StrictUtf8Encoding }
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

                await ExecuteWriteOperation(operation, timeout, cancellationToken);

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
                    null));
            }
            else
            {
                var operation = CreateAggregateOperation<TResult>(model, pipeline);

                return await Task.FromResult<IAsyncEnumerable<TResult>>(new AsyncCursorAsyncEnumerable<TResult>(
                    () => ExecuteReadOperation(operation, timeout, cancellationToken),
                    null));
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
                var result = await ExecuteWriteOperation(operation, timeout, cancellationToken);
                return BulkWriteResult<TDocument>.FromCore(result, model.Requests);
            }
            catch (BulkWriteOperationException ex)
            {
                throw BulkWriteException<TDocument>.FromCore(ex, model.Requests);
            }
        }

        public Task<long> CountAsync(CountModel model, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(model, "model");

            var operation = CreateCountOperation(model);

            return ExecuteReadOperation(operation, timeout, cancellationToken);
        }


        public async Task<DeleteResult> DeleteManyAsync(DeleteManyModel<TDocument> model, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(model, "model");

            try
            {
                var bulkModel = new BulkWriteModel<TDocument>(new[] { model });
                var result = await BulkWriteAsync(bulkModel, timeout, cancellationToken);
                return DeleteResult.FromCore(result);
            }
            catch (BulkWriteException<TDocument> ex)
            {
                throw WriteException.FromBulkWriteException(ex);
            }
        }

        public async Task<DeleteResult> DeleteOneAsync(DeleteOneModel<TDocument> model, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(model, "model");

            try
            {
                var bulkModel = new BulkWriteModel<TDocument>(new[] { model });
                var result = await BulkWriteAsync(bulkModel, timeout, cancellationToken);
                return DeleteResult.FromCore(result);
            }
            catch (BulkWriteException<TDocument> ex)
            {
                throw WriteException.FromBulkWriteException(ex);
            }
        }

        public Task<IReadOnlyList<TResult>> DistinctAsync<TResult>(DistinctModel<TResult> model, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(model, "model");

            var resultSerializer = model.ResultSerializer ?? _settings.SerializerRegistry.GetSerializer<TResult>();
            var operation = new DistinctOperation<TResult>(
                _collectionNamespace,
                resultSerializer,
                model.FieldName,
                _messageEncoderSettings)
            {
                Criteria = ConvertToBsonDocument(model.Criteria),
                MaxTime = model.MaxTime
            };

            return ExecuteReadOperation(operation, timeout, cancellationToken);
        }

        public Task<BsonDocument> ExplainAsync(ExplainModel model, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(model, "model");

            var commandType = model.Command.GetType();
            if (commandType == typeof(CountModel))
            {
                return ExplainCountAsync((CountModel)model.Command, model.Verbosity, timeout, cancellationToken);
            }

            if (commandType.IsGenericType)
            {
                var genericCommandDefinition = commandType.GetGenericTypeDefinition();
                var genericArgument = commandType.GetGenericArguments()[0];
                if (genericCommandDefinition == typeof(AggregateModel<>))
                {
                    var explainAggregateMethod =
                        typeof(MongoCollectionImpl<TDocument>)
                        .GetMethod("ExplainAggregateAsync", BindingFlags.Instance | BindingFlags.NonPublic)
                        .MakeGenericMethod(genericArgument);

                    return (Task<BsonDocument>)explainAggregateMethod.Invoke(this, new object[] { model.Command, model.Verbosity, timeout, cancellationToken });
                }
                if (genericCommandDefinition == typeof(FindModel<>))
                {
                    var explainFindMethod =
                        typeof(MongoCollectionImpl<TDocument>)
                        .GetMethod("ExplainFindAsync", BindingFlags.Instance | BindingFlags.NonPublic)
                        .MakeGenericMethod(genericArgument);

                    return (Task<BsonDocument>)explainFindMethod.Invoke(this, new object[] { model.Command, model.Verbosity, timeout, cancellationToken });
                }
            }

            var message = string.Format("No explanation for {0} is defined.", model.Command.GetType().ToString());
            throw new ArgumentException(message, "model.Command");
        }

        public Task<IAsyncEnumerable<TResult>> FindAsync<TResult>(FindModel<TResult> model, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(model, "model");

            var operation = CreateFindOperation<TResult>(model);

            return Task.FromResult<IAsyncEnumerable<TResult>>(new AsyncCursorAsyncEnumerable<TResult>(
                () => ExecuteReadOperation(operation, timeout, cancellationToken),
                model.Limit));
        }

        public Task<TResult> FindOneAndDeleteAsync<TResult>(FindOneAndDeleteModel model, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(model, "model");

            var operation = new FindOneAndDeleteOperation<TResult>(
                _collectionNamespace,
                ConvertToBsonDocument(model.Criteria),
                _settings.SerializerRegistry.GetSerializer<TResult>(),
                _messageEncoderSettings)
            {
                MaxTime = model.MaxTime,
                Projection = ConvertToBsonDocument(model.Projection),
                Sort = ConvertToBsonDocument(model.Sort)
            };

            return ExecuteWriteOperation(operation, timeout, cancellationToken);

        }

        public Task<TResult> FindOneAndReplaceAsync<TResult>(FindOneAndReplaceModel<TDocument> model, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(model, "model");

            var operation = new FindOneAndReplaceOperation<TResult>(
                _collectionNamespace,
                ConvertToBsonDocument(model.Criteria),
                ConvertToBsonDocument(model.Replacement),
                _settings.SerializerRegistry.GetSerializer<TResult>(),
                _messageEncoderSettings)
            {
                IsUpsert = model.IsUpsert,
                MaxTime = model.MaxTime,
                Projection = ConvertToBsonDocument(model.Projection),
                ReturnOriginal = model.ReturnOriginal,
                Sort = ConvertToBsonDocument(model.Sort)
            };

            return ExecuteWriteOperation(operation, timeout, cancellationToken);
        }

        public Task<TResult> FindOneAndUpdateAsync<TResult>(FindOneAndUpdateModel model, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(model, "model");

            var operation = new FindOneAndUpdateOperation<TResult>(
                _collectionNamespace,
                ConvertToBsonDocument(model.Criteria),
                ConvertToBsonDocument(model.Update),
                _settings.SerializerRegistry.GetSerializer<TResult>(),
                _messageEncoderSettings)
            {
                IsUpsert = model.IsUpsert,
                MaxTime = model.MaxTime,
                Projection = ConvertToBsonDocument(model.Projection),
                ReturnOriginal = model.ReturnOriginal,
                Sort = ConvertToBsonDocument(model.Sort)
            };

            return ExecuteWriteOperation(operation, timeout, cancellationToken);
        }

        public async Task InsertOneAsync(InsertOneModel<TDocument> model, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(model, "model");

            try
            {
                var bulkModel = new BulkWriteModel<TDocument>(new[] { model });
                await BulkWriteAsync(bulkModel, timeout, cancellationToken);
            }
            catch (BulkWriteException<TDocument> ex)
            {
                throw WriteException.FromBulkWriteException(ex);
            }
        }

        public async Task<ReplaceOneResult> ReplaceOneAsync(ReplaceOneModel<TDocument> model, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(model, "model");

            try
            {
                var bulkModel = new BulkWriteModel<TDocument>(new[] { model });
                var result = await BulkWriteAsync(bulkModel, timeout, cancellationToken);
                return ReplaceOneResult.FromCore(result);
            }
            catch (BulkWriteException<TDocument> ex)
            {
                throw WriteException.FromBulkWriteException(ex);
            }
        }

        public async Task<UpdateResult> UpdateManyAsync(UpdateManyModel<TDocument> model, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(model, "model");

            try
            {
                var bulkModel = new BulkWriteModel<TDocument>(new[] { model });
                var result = await BulkWriteAsync(bulkModel, timeout, cancellationToken);
                return UpdateResult.FromCore(result);
            }
            catch (BulkWriteException<TDocument> ex)
            {
                throw WriteException.FromBulkWriteException(ex);
            }
        }

        public async Task<UpdateResult> UpdateOneAsync(UpdateOneModel<TDocument> model, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(model, "model");

            try
            {
                var bulkModel = new BulkWriteModel<TDocument>(new[] { model });
                var result = await BulkWriteAsync(bulkModel, timeout, cancellationToken);
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
                return await _operationExecutor.ExecuteReadOperationAsync(binding, operation, timeout ?? _settings.OperationTimeout, cancellationToken);
            }
        }

        private async Task<TResult> ExecuteWriteOperation<TResult>(IWriteOperation<TResult> operation, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            using (var binding = new WritableServerBinding(_cluster))
            {
                return await _operationExecutor.ExecuteWriteOperationAsync(binding, operation, timeout ?? _settings.OperationTimeout, cancellationToken);
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

        private CountOperation CreateCountOperation(CountModel model)
        {
            return new CountOperation(
                _collectionNamespace,
                _messageEncoderSettings)
            {
                Criteria = ConvertToBsonDocument(model.Criteria),
                Hint = model.Hint is string ? BsonValue.Create((string)model.Hint) : ConvertToBsonDocument(model.Hint),
                Limit = model.Limit,
                MaxTime = model.MaxTime,
                Skip = model.Skip
            };
        }

        private FindOperation<TResult> CreateFindOperation<TResult>(FindModel<TResult> model)
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

        private Task<BsonDocument> ExplainAggregateAsync<TResult>(AggregateModel<TResult> model, ExplainVerbosity verbosity, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            var pipeline = model.Pipeline.Select(x => ConvertToBsonDocument(x)).ToList();
            var aggregateOperation = CreateAggregateOperation<TResult>(model, pipeline);
            var explainOperation = aggregateOperation.ToExplainOperation(verbosity.ToCore());
            return ExecuteReadOperation(explainOperation, timeout, cancellationToken);
        }

        private Task<BsonDocument> ExplainCountAsync(CountModel model, ExplainVerbosity verbosity, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            var countOperation = CreateCountOperation(model);
            var explainOperation = countOperation.ToExplainOperation(verbosity.ToCore());
            return ExecuteReadOperation(explainOperation, timeout, cancellationToken);
        }

        private Task<BsonDocument> ExplainFindAsync<TResult>(FindModel<TResult> model, ExplainVerbosity verbosity, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            var findOperation = CreateFindOperation<TResult>(model);
            var explainOperation = findOperation.ToExplainOperation(verbosity.ToCore());
            return ExecuteReadOperation(explainOperation, timeout, cancellationToken);
        }
    }
}
