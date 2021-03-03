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
        private readonly ReplaceOptions _options;
        private readonly BsonDocument _replacement;

        public UnifiedReplaceOneOperation(
            IMongoCollection<BsonDocument> collection,
            FilterDefinition<BsonDocument> filter,
            BsonDocument replacement,
            ReplaceOptions options)
        {
            _collection = collection;
            _filter = filter;
            _replacement = replacement;
            _options = options;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                var result = _collection.ReplaceOne(_filter, _replacement, _options, cancellationToken);

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
                var result = await _collection.ReplaceOneAsync(_filter, _replacement, _options, cancellationToken);

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
            var collection = _entityMap.GetCollection(targetCollectionId);

            FilterDefinition<BsonDocument> filter = null;
            ReplaceOptions options = null;
            BsonDocument replacement = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "filter":
                        filter = new BsonDocumentFilterDefinition<BsonDocument>(argument.Value.AsBsonDocument);
                        break;
                    case "replacement":
                        replacement = argument.Value.AsBsonDocument;
                        break;
                    case "upsert":
                        options = options ?? new ReplaceOptions();
                        options.IsUpsert = argument.Value.AsBoolean;
                        break;
                    default:
                        throw new FormatException($"Invalid ReplaceOneOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedReplaceOneOperation(collection, filter, replacement, options);
        }
    }

    public class UnifiedReplaceOneOperationResultConverter
    {
        public OperationResult Convert(ReplaceOneResult result)
        {
            var document = new BsonDocument
            {
                { "acknowledged", result.IsAcknowledged },
                { "matchedCount", result.MatchedCount },
                { "modifiedCount", result.ModifiedCount }
            };

            return OperationResult.FromResult(document);
        }
    }
}
