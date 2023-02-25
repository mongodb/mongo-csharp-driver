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
            var collection = _entityMap.Collections[targetCollectionId];

            BulkWriteOptions options = null;
            List<WriteModel<BsonDocument>> requests = null;
            IClientSessionHandle session = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "comment":
                        options ??= new BulkWriteOptions();
                        options.Comment = argument.Value;
                        break;
                    case "let":
                        options ??= new BulkWriteOptions();
                        options.Let = argument.Value.AsBsonDocument;
                        break;
                    case "ordered":
                        options ??= new BulkWriteOptions();
                        options.IsOrdered = argument.Value.AsBoolean;
                        break;
                    case "requests":
                        requests = argument.Value.AsBsonArray.Cast<BsonDocument>().Select(ParseWriteModel).ToList();
                        break;
                    case "session":
                        session = _entityMap.Sessions[argument.Value.AsString];
                        break;
                    default:
                        throw new FormatException($"Invalid BulkWriteOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedBulkWriteOperation(session, collection, requests, options);
        }

        // private methods
        private void ParseDeleteModel(
            BsonDocument model,
            out FilterDefinition<BsonDocument> filter,
            out BsonValue hint)
        {
            filter = null;
            hint = null;

            foreach (BsonElement argument in model.Elements)
            {
                switch (argument.Name)
                {
                    case "hint":
                        hint = argument.Value;
                        break;
                    case "filter":
                        filter = new BsonDocumentFilterDefinition<BsonDocument>(argument.Value.AsBsonDocument);
                        break;
                    default:
                        throw new FormatException($"Invalid BulkWrite Delete model argument name: '{argument.Name}'.");
                }
            }
        }

        private void ParseInsertModel(
            BsonDocument model,
            out BsonDocument document)
        {
            document = null;

            foreach (BsonElement argument in model.Elements)
            {
                switch (argument.Name)
                {
                    case "document":
                        document = argument.Value.AsBsonDocument;
                        break;
                    default:
                        throw new FormatException($"Invalid BulkWrite Insert model argument name: '{argument.Name}'.");
                }
            }
        }

        private void ParseReplaceModel(
            BsonDocument model,
            out FilterDefinition<BsonDocument> filter,
            out BsonDocument replacement,
            out BsonValue hint,
            out bool isUpsert)
        {
            filter = null;
            replacement = null;
            hint = null;
            isUpsert = false;

            foreach (BsonElement argument in model.Elements)
            {
                switch (argument.Name)
                {
                    case "filter":
                        filter = new BsonDocumentFilterDefinition<BsonDocument>(argument.Value.AsBsonDocument);
                        break;
                    case "hint":
                        hint = argument.Value;
                        break;
                    case "replacement":
                        replacement = argument.Value.AsBsonDocument;
                        break;
                    case "upsert":
                        isUpsert = argument.Value.ToBoolean();
                        break;
                    default:
                        throw new FormatException($"Invalid BulkWrite Replace model argument name: '{argument.Name}'.");
                }
            }
        }

        private void ParseUpdateModel(
            BsonDocument model,
            out FilterDefinition<BsonDocument> filter,
            out UpdateDefinition<BsonDocument> update,
            out List<ArrayFilterDefinition> arrayFilters,
            out BsonValue hint,
            out bool isUpsert)
        {
            arrayFilters = null;
            filter = null;
            update = null;
            hint = null;
            isUpsert = false;

            foreach (BsonElement argument in model.Elements)
            {
                switch (argument.Name)
                {
                    case "arrayFilters":
                        arrayFilters = argument
                            .Value
                            .AsBsonArray
                            .Cast<BsonDocument>()
                            .Select(x => new BsonDocumentArrayFilterDefinition<BsonValue>(x))
                            .ToList<ArrayFilterDefinition>();
                        break;
                    case "filter":
                        filter = new BsonDocumentFilterDefinition<BsonDocument>(argument.Value.AsBsonDocument);
                        break;
                    case "hint":
                        hint = argument.Value;
                        break;
                    case "update":
                        switch (argument.Value)
                        {
                            case BsonDocument:
                                update = argument.Value.AsBsonDocument;
                                break;
                            case BsonArray:
                                update = PipelineDefinition<BsonDocument, BsonDocument>.Create(argument.Value.AsBsonArray.Cast<BsonDocument>());
                                break;
                            default:
                                throw new FormatException($"Invalid BulkWrite Update model update argument: '{argument.Value}'.");
                        }
                        break;
                    case "upsert":
                        isUpsert = argument.Value.ToBoolean();
                        break;
                    default:
                        throw new FormatException($"Invalid BulkWrite Update model argument name: '{argument.Name}'.");
                }
            }
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
                    {
                        ParseDeleteModel(model, out var filter, out var hint);

                        return new DeleteManyModel<BsonDocument>(filter)
                        {
                            Hint = hint
                        };
                    }
                case "deleteOne":
                    {
                        ParseDeleteModel(model, out var filter, out var hint);

                        return new DeleteOneModel<BsonDocument>(filter)
                        {
                            Hint = hint
                        };
                    }
                case "insertOne":
                    {
                        ParseInsertModel(model, out var document);

                        return new InsertOneModel<BsonDocument>(document);
                    }
                case "replaceOne":
                    {
                        ParseReplaceModel(model, out var filter, out var replacement, out var hint, out bool isUpsert);

                        return new ReplaceOneModel<BsonDocument>(filter, replacement)
                        {
                            Hint = hint,
                            IsUpsert = isUpsert
                        };
                    }
                case "updateMany":
                    {
                        ParseUpdateModel(model, out var filter, out var update, out var arrayFilters, out var hint, out var isUpsert);

                        return new UpdateManyModel<BsonDocument>(filter, update)
                        {
                            ArrayFilters = arrayFilters,
                            Hint = hint,
                            IsUpsert = isUpsert
                        };
                    }
                case "updateOne":
                    {
                        ParseUpdateModel(model, out var filter, out var update, out var arrayFilters, out var hint, out var isUpsert);

                        return new UpdateOneModel<BsonDocument>(filter, update)
                        {
                            ArrayFilters = arrayFilters,
                            Hint = hint,
                            IsUpsert = isUpsert
                        };
                    }
                default:
                    throw new FormatException($"Invalid BulkWrite model name: '{modelName}'.");
            }
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
