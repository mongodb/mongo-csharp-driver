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
    internal sealed class MongoCollectionImpl<T> : IMongoCollection<T>
    {
        // fields
        private readonly ICluster _cluster;
        private readonly CollectionNamespace _collectionNamespace;
        private readonly IOperationExecutor _operationExecutor;
        private readonly IBsonSerializer<T> _serializer;
        private readonly MongoCollectionSettings _settings;

        // constructors
        public MongoCollectionImpl(CollectionNamespace collectionNamespace, MongoCollectionSettings settings, ICluster cluster, IOperationExecutor operationExecutor)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _settings = Ensure.IsNotNull(settings, "settings");
            _cluster = Ensure.IsNotNull(cluster, "cluster");
            _operationExecutor = Ensure.IsNotNull(operationExecutor, "operationExecutor");

            _serializer = _settings.SerializerRegistry.GetSerializer<T>();
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
        public async Task<BulkWriteResult<T>> BulkWriteAsync(BulkWriteModel<T> model, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(model, "model");

            var operation = new BulkMixedWriteOperation(
                _collectionNamespace,
                model.Requests.Select(ConvertWriteModelToWriteRequest),
                GetMessageEncoderSettings())
            {
                IsOrdered = model.IsOrdered,
                WriteConcern = _settings.WriteConcern
            };

            try
            {
                var result = await ExecuteWriteOperation(operation, timeout, cancellationToken);
                return BulkWriteResult<T>.FromCore(result, model.Requests);
            }
            catch(BulkWriteOperationException ex)
            {
                throw BulkWriteException<T>.FromCore(ex, model.Requests);
            }
        }

        public Task<long> CountAsync(CountModel model, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(model, "model");

            var operation = new CountOperation(
                _collectionNamespace,
                GetMessageEncoderSettings())
            {
                Criteria = ConvertToBsonDocument(model.Criteria),
                Hint = model.Hint is string ? BsonValue.Create((string)model.Hint) : ConvertToBsonDocument(model.Hint),
                Limit = model.Limit,
                MaxTime = model.MaxTime,
                Skip = model.Skip
            };

            return ExecuteReadOperation(operation, timeout, cancellationToken);
        }

        public Task<IReadOnlyList<TValue>> DistinctAsync<TValue>(DistinctModel<TValue> model, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(model, "model");

            var operation = new DistinctOperation<TValue>(
                _collectionNamespace,
                model.ValueSerializer ?? _settings.SerializerRegistry.GetSerializer<TValue>(),
                model.FieldName,
                GetMessageEncoderSettings())
            {
                Criteria = ConvertToBsonDocument(model.Criteria),
                MaxTime = model.MaxTime
            };

            return ExecuteReadOperation(operation, timeout, cancellationToken);
        }

        private void AssignId(T document)
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
        
        private WriteRequest ConvertWriteModelToWriteRequest(WriteModel<T> model, int index)
        {
            switch (model.ModelType)
            {
                case WriteModelType.InsertOne:
                    var insertOneModel = (InsertOneModel<T>)model;
                    AssignId(insertOneModel.Document);
                    return new InsertRequest(new BsonDocumentWrapper(insertOneModel.Document, _serializer))
                    {
                        CorrelationId = index
                    };
                case WriteModelType.RemoveMany:
                    var removeManyModel = (RemoveManyModel<T>)model;
                    return new DeleteRequest(ConvertToBsonDocument(removeManyModel.Criteria))
                    {
                        CorrelationId = index,
                        Limit = 0
                    };
                case WriteModelType.RemoveOne:
                    var removeOneModel = (RemoveOneModel<T>)model;
                    return new DeleteRequest(ConvertToBsonDocument(removeOneModel.Criteria))
                    {
                        CorrelationId = index,
                        Limit = 1
                    };
                case WriteModelType.ReplaceOne:
                    var replaceOneModel = (ReplaceOneModel<T>)model;
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
                    var updateManyModel = (UpdateManyModel<T>)model;
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
                    var updateOneModel = (UpdateOneModel<T>)model;
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
            if(document == null)
            {
                return null;
            }

            var bsonDocument = document as BsonDocument;
            if(bsonDocument != null)
            {
                return bsonDocument;
            }

            if(document is string)
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

        private MessageEncoderSettings GetMessageEncoderSettings()
        {
            return new MessageEncoderSettings
            {
                { MessageEncoderSettingsName.GuidRepresentation, _settings.GuidRepresentation },
                { MessageEncoderSettingsName.ReadEncoding, _settings.ReadEncoding ?? Utf8Helper.StrictUtf8Encoding },
                { MessageEncoderSettingsName.WriteEncoding, _settings.WriteEncoding ?? Utf8Helper.StrictUtf8Encoding }
            };
        }
    }
}
