/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedDropSearchIndexOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly string _indexName;

        public UnifiedDropSearchIndexOperation(IMongoCollection<BsonDocument> collection, string indexName)
        {
            _collection = Ensure.IsNotNull(collection, nameof(collection));
            _indexName = Ensure.IsNotNullOrEmpty(indexName, nameof(indexName));
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                _collection.SearchIndexes.DropOne(_indexName, cancellationToken);

                return OperationResult.Empty();
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
                await _collection.SearchIndexes.DropOneAsync(_indexName, cancellationToken);

                return OperationResult.Empty();
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedDropSearchIndexOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedDropSearchIndexOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedDropSearchIndexOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            var collection = _entityMap.Collections[targetCollectionId];
            string indexName = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "name":
                        indexName = argument.Value.AsString;
                        break;
                    default:
                        throw new FormatException($"Invalid DropSearchIndexOperation argument name: '{argument.Name}'.");
                }
            }

            return new(collection, indexName);
        }
    }
}
