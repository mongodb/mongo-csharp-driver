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
    public class UnifiedFindOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly FilterDefinition<BsonDocument> _filter;
        private readonly FindOptions<BsonDocument> _options;

        public UnifiedFindOperation(
            IMongoCollection<BsonDocument> collection,
            FilterDefinition<BsonDocument> filter,
            FindOptions<BsonDocument> options)
        {
            _collection = collection;
            _filter = filter;
            _options = options;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                var cursor = _collection.FindSync(_filter, _options, cancellationToken);
                var result = cursor.ToList();

                return OperationResult.FromResult(new BsonArray(result));
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
                var cursor = await _collection.FindAsync(_filter, _options, cancellationToken);
                var result = await cursor.ToListAsync();

                return OperationResult.FromResult(new BsonArray(result));
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedFindOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedFindOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedFindOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            var collection = _entityMap.GetCollection(targetCollectionId);

            FilterDefinition<BsonDocument> filter = null;
            FindOptions<BsonDocument> options = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "batchSize":
                        options = options ?? new FindOptions<BsonDocument>();
                        options.BatchSize = argument.Value.AsInt32;
                        break;
                    case "filter":
                        filter = new BsonDocumentFilterDefinition<BsonDocument>(argument.Value.AsBsonDocument);
                        break;
                    case "limit":
                        options = options ?? new FindOptions<BsonDocument>();
                        options.Limit = argument.Value.AsInt32;
                        break;
                    case "sort":
                        options = options ?? new FindOptions<BsonDocument>();
                        options.Sort = new BsonDocumentSortDefinition<BsonDocument>(argument.Value.AsBsonDocument);
                        break;
                    default:
                        throw new FormatException($"Invalid FindOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedFindOperation(collection, filter, options);
        }
    }
}
