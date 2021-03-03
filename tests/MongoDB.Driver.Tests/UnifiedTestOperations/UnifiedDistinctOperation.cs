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

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedDistinctOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly string _fieldName;
        private readonly FilterDefinition<BsonDocument> _filter;

        public UnifiedDistinctOperation(
            IMongoCollection<BsonDocument> collection,
            string fieldName,
            FilterDefinition<BsonDocument> filter)
        {
            _collection = collection;
            _fieldName = fieldName;
            _filter = filter;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                var cursor = _collection.Distinct<BsonValue>(_fieldName, _filter, cancellationToken: cancellationToken);
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
                var cursor = await _collection.DistinctAsync<BsonValue>(_fieldName, _filter, cancellationToken: cancellationToken);
                var result = cursor.ToList();

                return OperationResult.FromResult(new BsonArray(result));
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedDistinctOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedDistinctOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedDistinctOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            var collection = _entityMap.GetCollection(targetCollectionId);

            string fieldName = null;
            FilterDefinition<BsonDocument> filter = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "fieldName":
                        fieldName = argument.Value.AsString;
                        break;
                    case "filter":
                        filter = argument.Value.AsBsonDocument;
                        break;
                    default:
                        throw new FormatException($"Invalid DistinctOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedDistinctOperation(collection, fieldName, filter);
        }
    }
}
