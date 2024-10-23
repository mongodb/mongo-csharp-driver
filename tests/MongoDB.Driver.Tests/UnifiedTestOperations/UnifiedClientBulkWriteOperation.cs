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
    public class UnifiedClientBulkWriteOperation : IUnifiedEntityTestOperation
    {
        private readonly IClientSessionHandle _session;
        private readonly IMongoClient _mongoClient;
        private readonly IReadOnlyList<BulkWriteModel> _models;
        private readonly ClientBulkWriteOptions _options;

        public UnifiedClientBulkWriteOperation(IMongoClient mongoClient, IClientSessionHandle session, IReadOnlyList<BulkWriteModel> models, ClientBulkWriteOptions options)
        {
            _mongoClient = mongoClient;
            _session = session;
            _models = models;
            _options = options;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                ClientBulkWriteResult result;
                if (_session == null)
                {
                    result = _mongoClient.BulkWrite(_models, _options);
                }
                else
                {
                    result = _mongoClient.BulkWrite(_session, _models, _options);
                }

                return OperationResult.FromResult(ConvertClientBulkWriteResult(result));
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
                ClientBulkWriteResult result;
                if (_session == null)
                {
                    result = await _mongoClient.BulkWriteAsync(_models, _options, cancellationToken);
                }
                else
                {
                    result = await _mongoClient.BulkWriteAsync(_session, _models, _options, cancellationToken);
                }

                return OperationResult.FromResult(ConvertClientBulkWriteResult(result));
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }

        public static BsonDocument ConvertClientBulkWriteResult(ClientBulkWriteResult result)
        {
            if (result == null)
            {
                return null;
            }

            if (!result.Acknowledged)
            {
                return new BsonDocument();
            }

            return new BsonDocument
            {
                { "insertedCount", (int)result.InsertedCount },
                { "upsertedCount", (int)result.UpsertedCount },
                { "matchedCount", (int)result.MatchedCount },
                { "modifiedCount", (int)result.ModifiedCount },
                { "deletedCount", (int)result.DeletedCount },
                {
                    "insertResults", ConvertResults(result.InsertResults,
                        item => new() { { "insertedId", item.InsertedId } })
                },
                {
                    "updateResults", ConvertResults(result.UpdateResults,
                        item => new() { { "matchedCount", (int)item.MatchedCount }, { "modifiedCount", (int)item.ModifiedCount }, { "upsertedId", item.UpsertedId, item.UpsertedId != null } })
                },
                {
                    "deleteResults", ConvertResults(result.DeleteResults,
                        item => new() { { "deletedCount", (int)item.DeletedCount } })
                }
            };

            BsonDocument ConvertResults<TResultModel>(IReadOnlyDictionary<int, TResultModel> results, Func<TResultModel, BsonDocument> converter)
                => new BsonDocument(results.ToDictionary(
                    i => i.Key.ToString(),
                    i => converter(i.Value)));
        }
    }

    public class UnifiedClientBulkWriteOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedClientBulkWriteOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedClientBulkWriteOperation Build(string targetClientId, BsonDocument arguments)
        {
            var client = _entityMap.Clients[targetClientId];
            ClientBulkWriteOptions options = null;
            BulkWriteModel[] models = null;
            IClientSessionHandle session = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "bypassDocumentValidation":
                        options ??= new ClientBulkWriteOptions();
                        options.BypassDocumentValidation = argument.Value.AsBoolean;
                        break;
                    case "comment":
                        options ??= new ClientBulkWriteOptions();
                        options.Comment = argument.Value;
                        break;
                    case "let":
                        options ??= new ClientBulkWriteOptions();
                        options.Let = argument.Value.AsBsonDocument;
                        break;
                    case "ordered":
                        options ??= new ClientBulkWriteOptions();
                        options.IsOrdered = argument.Value.AsBoolean;
                        break;
                    case "models":
                        models = argument.Value.AsBsonArray.Cast<BsonDocument>().Select(ParseWriteModel).ToArray();
                        break;
                    case "session":
                        session = _entityMap.Sessions[argument.Value.AsString];
                        break;
                    case "verboseResults":
                        options ??= new ClientBulkWriteOptions();
                        options.VerboseResult = argument.Value.AsBoolean;
                        break;
                    case "writeConcern":
                        options ??= new ClientBulkWriteOptions();
                        options.WriteConcern = WriteConcern.FromBsonDocument(argument.Value.AsBsonDocument);
                        break;
                    default:
                        throw new FormatException($"Invalid BulkWriteOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedClientBulkWriteOperation(client, session, models, options);
        }

        private void ParseDeleteModel(
            BsonDocument model,
            out string ns,
            out FilterDefinition<BsonDocument> filter,
            out BsonValue hint,
            out Collation collation)
        {
            ns = null;
            filter = null;
            hint = null;
            collation = null;

            foreach (BsonElement argument in model.Elements)
            {
                switch (argument.Name)
                {
                    case "collation":
                        collation = Collation.FromBsonDocument(argument.Value.AsBsonDocument);
                        break;
                    case "namespace":
                        ns = argument.Value.AsString;
                        break;
                    case "hint":
                        hint = argument.Value;
                        break;
                    case "filter":
                        filter = new BsonDocumentFilterDefinition<BsonDocument>(argument.Value.AsBsonDocument);
                        break;
                    default:
                        throw new FormatException($"Invalid ClientBulkWrite Delete model argument name: '{argument.Name}'.");
                }
            }
        }

        private void ParseInsertModel(
            BsonDocument model,
            out string ns,
            out BsonDocument document)
        {
            document = null;
            ns = null;

            foreach (BsonElement argument in model.Elements)
            {
                switch (argument.Name)
                {
                    case "document":
                        document = argument.Value.AsBsonDocument;
                        break;
                    case "namespace":
                        ns = argument.Value.AsString;
                        break;
                    default:
                        throw new FormatException($"Invalid BulkWrite Insert model argument name: '{argument.Name}'.");
                }
            }
        }

        private void ParseReplaceModel(
            BsonDocument model,
            out string ns,
            out FilterDefinition<BsonDocument> filter,
            out BsonDocument replacement,
            out BsonValue hint,
            out Collation collation,
            out bool isUpsert)
        {
            ns = null;
            filter = null;
            replacement = null;
            hint = null;
            collation = null;
            isUpsert = false;

            foreach (BsonElement argument in model.Elements)
            {
                switch (argument.Name)
                {
                    case "collation":
                        collation = Collation.FromBsonDocument(argument.Value.AsBsonDocument);
                        break;
                    case "filter":
                        filter = new BsonDocumentFilterDefinition<BsonDocument>(argument.Value.AsBsonDocument);
                        break;
                    case "hint":
                        hint = argument.Value;
                        break;
                    case "namespace":
                        ns = argument.Value.AsString;
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
            out string ns,
            out FilterDefinition<BsonDocument> filter,
            out UpdateDefinition<BsonDocument> update,
            out List<ArrayFilterDefinition> arrayFilters,
            out BsonValue hint,
            out Collation collation,
            out bool isUpsert)
        {
            ns = null;
            arrayFilters = null;
            filter = null;
            update = null;
            hint = null;
            collation = null;
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
                    case "collation":
                        collation = Collation.FromBsonDocument(argument.Value.AsBsonDocument);
                        break;
                    case "filter":
                        filter = new BsonDocumentFilterDefinition<BsonDocument>(argument.Value.AsBsonDocument);
                        break;
                    case "hint":
                        hint = argument.Value;
                        break;
                    case "namespace":
                        ns = argument.Value.AsString;
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

        private BulkWriteModel ParseWriteModel(BsonDocument modelItem)
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
                        ParseDeleteModel(model, out var ns, out var filter, out var hint, out var collation);

                        return new BulkWriteDeleteManyModel<BsonDocument>(ns, filter)
                        {
                            Collation = collation,
                            Hint = hint
                        };
                    }
                case "deleteOne":
                    {
                        ParseDeleteModel(model, out var ns, out var filter, out var hint, out var collation);

                        return new BulkWriteDeleteOneModel<BsonDocument>(ns, filter)
                        {
                            Collation = collation,
                            Hint = hint
                        };
                    }
                case "insertOne":
                    {
                        ParseInsertModel(model, out var ns, out var document);

                        return new BulkWriteInsertOneModel<BsonDocument>(ns, document);
                    }
                case "replaceOne":
                    {
                        ParseReplaceModel(model, out var ns, out var filter, out var replacement, out var hint, out var collation, out bool isUpsert);

                        return new BulkWriteReplaceOneModel<BsonDocument>(ns, filter, replacement)
                        {
                            Collation = collation,
                            Hint = hint,
                            IsUpsert = isUpsert
                        };
                    }
                case "updateMany":
                    {
                        ParseUpdateModel(model, out var ns, out var filter, out var update, out var arrayFilters, out var hint, out var collation, out var isUpsert);

                        return new BulkWriteUpdateManyModel<BsonDocument>(ns, filter, update)
                        {
                            ArrayFilters = arrayFilters,
                            Collation = collation,
                            Hint = hint,
                            IsUpsert = isUpsert
                        };
                    }
                case "updateOne":
                    {
                        ParseUpdateModel(model, out var ns, out var filter, out var update, out var arrayFilters, out var hint, out var collation, out var isUpsert);

                        return new BulkWriteUpdateOneModel<BsonDocument>(ns, filter, update)
                        {
                            ArrayFilters = arrayFilters,
                            Collation = collation,
                            Hint = hint,
                            IsUpsert = isUpsert
                        };
                    }
                default:
                    throw new FormatException($"Invalid BulkWrite model name: '{modelName}'.");
            }
        }
    }
}
