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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedReplaceOneOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly FilterDefinition<BsonDocument> _filter;
        private readonly ReplaceOptions<BsonDocument> _options;
        private readonly BsonDocument _replacement;
        private readonly IClientSessionHandle _session;

        public UnifiedReplaceOneOperation(
            IMongoCollection<BsonDocument> collection,
            FilterDefinition<BsonDocument> filter,
            BsonDocument replacement,
            ReplaceOptions<BsonDocument> options,
            IClientSessionHandle session)
        {
            _collection = collection;
            _filter = filter;
            _replacement = replacement;
            _options = options;
            _session = session;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                var result = _session == null
                    ? _collection.ReplaceOne(_filter, _replacement, _options, cancellationToken)
                    : _collection.ReplaceOne(_session, _filter, _replacement, _options, cancellationToken);

                return new UnifiedReplaceOneOperationResultConverter().Convert(result);
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
                var result = _session == null
                    ? await _collection.ReplaceOneAsync(_filter, _replacement, _options, cancellationToken)
                    : await _collection.ReplaceOneAsync(_session, _filter, _replacement, _options, cancellationToken);

                return new UnifiedReplaceOneOperationResultConverter().Convert(result);
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedReplaceOneOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedReplaceOneOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedReplaceOneOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            var collection = _entityMap.Collections[targetCollectionId];

            FilterDefinition<BsonDocument> filter = null;
            ReplaceOptions<BsonDocument> options = null;
            BsonDocument replacement = null;
            IClientSessionHandle session = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "comment":
                        options ??= new ReplaceOptions<BsonDocument>();
                        options.Comment = argument.Value;
                        break;
                    case "filter":
                        filter = new BsonDocumentFilterDefinition<BsonDocument>(argument.Value.AsBsonDocument);
                        break;
                    case "hint":
                        options ??= new ReplaceOptions<BsonDocument>();
                        options.Hint = argument.Value;
                        break;
                    case "let":
                        options ??= new ReplaceOptions<BsonDocument>();
                        options.Let = argument.Value.AsBsonDocument;
                        break;
                    case "replacement":
                        replacement = argument.Value.AsBsonDocument;
                        break;
                    case "session":
                        session = _entityMap.Sessions[argument.Value.AsString];
                        break;
                    case "sort":
                        options ??= new ReplaceOptions<BsonDocument>();
                        options.Sort = argument.Value.AsBsonDocument;
                        break;
                    case "upsert":
                        options ??= new ReplaceOptions<BsonDocument>();
                        options.IsUpsert = argument.Value.AsBoolean;
                        break;
                    default:
                        throw new FormatException($"Invalid ReplaceOneOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedReplaceOneOperation(collection, filter, replacement, options, session);
        }
    }

    public class UnifiedReplaceOneOperationResultConverter
    {
        public OperationResult Convert(ReplaceOneResult result)
        {
            var document = new BsonDocument
            {
                { "matchedCount", () => result.MatchedCount, result.IsAcknowledged },
                { "modifiedCount", () => result.ModifiedCount, result.IsModifiedCountAvailable },
                { "upsertedCount", () => result.UpsertedId == null ? 0 : 1, result.IsAcknowledged },
            };

            return OperationResult.FromResult(document);
        }
    }
}
