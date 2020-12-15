/* Copyright 2020-present MongoDB Inc.
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
using MongoDB.Bson.TestHelpers.JsonDrivenTests;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedBulkWriteOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly BulkWriteOptions _options;
        private readonly List<WriteModel<BsonDocument>> _requests;
        private readonly IClientSessionHandle _session;

        public UnifiedBulkWriteOperation(
            IClientSessionHandle session,
            IMongoCollection<BsonDocument> collection,
            List<WriteModel<BsonDocument>> requests,
            BulkWriteOptions options)
        {
            _session = session;
            _collection = collection;
            _requests = requests;
            _options = options;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                BulkWriteResult<BsonDocument> result;
                if (_session == null)
                {
                    result = _collection.BulkWrite(_requests, _options);
                }
                else
                {
                    result = _collection.BulkWrite(_session, _requests, _options);
                }

                return new UnifiedBulkWriteOperationResultConverter().Convert(result);
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }

        public async Task<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                BulkWriteResult<BsonDocument> result;
                if (_session == null)
                {
                    result = await _collection.BulkWriteAsync(_requests, _options);
                }
                else
                {
                    result = await _collection.BulkWriteAsync(_session, _requests, _options);
                }

                return new UnifiedBulkWriteOperationResultConverter().Convert(result);
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedBulkWriteOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedBulkWriteOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedBulkWriteOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            var collection = _entityMap.GetCollection(targetCollectionId);

            BulkWriteOptions options = null;
            List<WriteModel<BsonDocument>> requests = null;
            IClientSessionHandle session = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "ordered":
                        options = options ?? new BulkWriteOptions();
                        options.IsOrdered = argument.Value.AsBoolean;
                        break;
                    case "requests":
                        requests = ParseWriteModels(argument.Value.AsBsonArray.Cast<BsonDocument>());
                        break;
                    case "session":
                        session = _entityMap.GetSession(argument.Value.AsString);
                        break;
                    default:
                        throw new FormatException($"Invalid BulkWriteOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedBulkWriteOperation(session, collection, requests, options);
        }

        private DeleteManyModel<BsonDocument> ParseDeleteManyModel(BsonDocument model)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(model, "filter");

            var filter = new BsonDocumentFilterDefinition<BsonDocument>(model["filter"].AsBsonDocument);

            return new DeleteManyModel<BsonDocument>(filter);
        }

        private DeleteOneModel<BsonDocument> ParseDeleteOneModel(BsonDocument model)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(model, "filter");

            var filter = new BsonDocumentFilterDefinition<BsonDocument>(model["filter"].AsBsonDocument);

            return new DeleteOneModel<BsonDocument>(filter);
        }

        private InsertOneModel<BsonDocument> ParseInsertOneModel(BsonDocument model)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(model, "document");

            var document = model["document"].AsBsonDocument;

            return new InsertOneModel<BsonDocument>(document);
        }

        private ReplaceOneModel<BsonDocument> ParseReplaceOneModel(BsonDocument model)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(model, "filter", "replacement", "upsert");

            var filter = new BsonDocumentFilterDefinition<BsonDocument>(model["filter"].AsBsonDocument);
            var replacement = model["replacement"].AsBsonDocument;
            var isUpsert = model.GetValue("upsert", false).ToBoolean();

            return new ReplaceOneModel<BsonDocument>(filter, replacement)
            {
                IsUpsert = isUpsert
            };
        }

        private UpdateManyModel<BsonDocument> ParseUpdateManyModel(BsonDocument model)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(model, "filter", "update", "upsert");

            var filter = new BsonDocumentFilterDefinition<BsonDocument>(model["filter"].AsBsonDocument);
            var update = new BsonDocumentUpdateDefinition<BsonDocument>(model["update"].AsBsonDocument);
            var isUpsert = model.GetValue("upsert", false).ToBoolean();

            return new UpdateManyModel<BsonDocument>(filter, update)
            {
                IsUpsert = isUpsert
            };
        }

        private UpdateOneModel<BsonDocument> ParseUpdateOneModel(BsonDocument model)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(model, "filter", "update", "upsert");

            var filter = new BsonDocumentFilterDefinition<BsonDocument>(model["filter"].AsBsonDocument);
            var update = new BsonDocumentUpdateDefinition<BsonDocument>(model["update"].AsBsonDocument);
            var isUpsert = model.GetValue("upsert", false).ToBoolean();

            return new UpdateOneModel<BsonDocument>(filter, update)
            {
                IsUpsert = isUpsert
            };
        }

        private WriteModel<BsonDocument> ParseWriteModel(BsonDocument modelItem)
        {
            if (modelItem.ElementCount != 1)
            {
                throw new FormatException("BulkWrite request model must contain a single element.");
            }

            var modelName = modelItem.GetElement(0).Name;
            var model = modelItem[0].AsBsonDocument;
            switch (modelName)
            {
                case "deleteMany":
                    return ParseDeleteManyModel(model);
                case "deleteOne":
                    return ParseDeleteOneModel(model);
                case "insertOne":
                    return ParseInsertOneModel(model);
                case "replaceOne":
                    return ParseReplaceOneModel(model);
                case "updateMany":
                    return ParseUpdateManyModel(model);
                case "updateOne":
                    return ParseUpdateOneModel(model);
                default:
                    throw new FormatException($"Invalid write model name: '{modelName}'.");
            }
        }

        private List<WriteModel<BsonDocument>> ParseWriteModels(IEnumerable<BsonDocument> models)
        {
            var result = new List<WriteModel<BsonDocument>>();
            foreach (var model in models)
            {
                result.Add(ParseWriteModel(model));
            }

            return result;
        }
    }

    public class UnifiedBulkWriteOperationResultConverter
    {
        public OperationResult Convert(BulkWriteResult<BsonDocument> result)
        {
            var document = new BsonDocument
            {
                { "deletedCount", result.DeletedCount },
                { "insertedCount", result.InsertedCount },
                { "matchedCount", result.MatchedCount },
                { "modifiedCount", result.ModifiedCount },
                { "upsertedCount", result.Upserts.Count },
                { "insertedIds", PrepareInsertedIds(result.ProcessedRequests) },
                { "upsertedIds", PrepareUpsertedIds(result.Upserts) }
            };

            return OperationResult.FromResult(document);
        }

        private BsonDocument PrepareInsertedIds(IReadOnlyList<WriteModel<BsonDocument>> processedRequests)
        {
            var result = new BsonDocument();

            for (int i = 0; i < processedRequests.Count; i++)
            {
                if (processedRequests[i] is InsertOneModel<BsonDocument> insertOneModel)
                {
                    result.Add(i.ToString(), insertOneModel.Document["_id"]);
                }
            }

            return result;
        }

        private BsonDocument PrepareUpsertedIds(IReadOnlyList<BulkWriteUpsert> upserts)
        {
            return new BsonDocument(upserts.Select(x => new BsonElement(x.Index.ToString(), x.Id)));
        }
    }
}
