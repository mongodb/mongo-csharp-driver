/* Copyright 2021-present MongoDB Inc.
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
    public class UnifiedListIndexesOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly ListIndexesOptions _listIndexesOptions;

        public UnifiedListIndexesOperation(IMongoCollection<BsonDocument> collection, ListIndexesOptions listIndexesOptions)
        {
            _collection = Ensure.IsNotNull(collection, nameof(collection));
            _listIndexesOptions = listIndexesOptions; // can be null
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                _ = _collection.Indexes.List(_listIndexesOptions, cancellationToken).ToList(); //TODO: fully iterate
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
                var cursor = await _collection.Indexes.ListAsync(_listIndexesOptions, cancellationToken).ConfigureAwait(false);
                _ = await cursor.ToListAsync().ConfigureAwait(false); //TODO: fully iterate
                return OperationResult.Empty();
            }
            catch (Exception ex)
            {
                return OperationResult.FromException(ex);
            }
        }
    }

    public class UnifiedListIndexesOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedListIndexesOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedListIndexesOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            var collection = _entityMap.GetCollection(targetCollectionId);

            var listIndexesOptions = new ListIndexesOptions();
            if (arguments != null)
            {
                foreach (var argument in arguments)
                {
                    switch (argument.Name)
                    {
                        case "batchSize":
                            listIndexesOptions.BatchSize = argument.Value.ToInt32();
                            break;
                        default:
                            throw new FormatException($"Invalid {nameof(UnifiedListIndexesOperation)} argument name: '{argument.Name}'.");
                    }
                }
            }

            return new UnifiedListIndexesOperation(collection, listIndexesOptions);
        }
    }
}
