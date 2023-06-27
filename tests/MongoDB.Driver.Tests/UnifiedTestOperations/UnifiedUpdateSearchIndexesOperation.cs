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
    public sealed class UnifiedUpdateSearchIndexesOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly BsonDocument _definition;
        private readonly string _indexName;

        public UnifiedUpdateSearchIndexesOperation(
            IMongoCollection<BsonDocument> collection,
            string indexName,
            BsonDocument definition)
        {
            _collection = Ensure.IsNotNull(collection, nameof(collection));
            _definition = Ensure.IsNotNull(definition, nameof(definition));
            _indexName = Ensure.IsNotNull(indexName, nameof(indexName));
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                _collection.SearchIndexes.Update(_indexName, _definition, cancellationToken);

                return OperationResult.Empty();
            }
            catch (Exception ex)
            {
                return OperationResult.FromException(ex);
            }
        }

        public async Task<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _collection.SearchIndexes.UpdateAsync(_indexName, _definition, cancellationToken);

                return OperationResult.Empty();
            }
            catch (Exception ex)
            {
                return OperationResult.FromException(ex);
            }
        }
    }

    public sealed class UnifiedUpdateSearchIndexesOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedUpdateSearchIndexesOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedUpdateSearchIndexesOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            var collection = _entityMap.Collections[targetCollectionId];
            string indexName = null;
            BsonDocument definition = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "name":
                        indexName = argument.Value.ToString();
                        break;
                    case "definition":
                        definition = argument.Value.AsBsonDocument;
                        break;
                    default:
                        throw new FormatException($"Invalid {nameof(UnifiedUpdateSearchIndexesOperation)} argument name: '{argument.Name}'.");
                }
            }

            return new(collection, indexName, definition);
        }
    }
}
