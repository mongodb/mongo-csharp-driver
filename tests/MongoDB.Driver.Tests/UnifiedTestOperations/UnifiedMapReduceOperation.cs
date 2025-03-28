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
    public class UnifiedMapReduceOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly BsonJavaScript _map;
        private readonly BsonJavaScript _reduce;

        public UnifiedMapReduceOperation(
            IMongoCollection<BsonDocument> collection,
            BsonJavaScript map,
            BsonJavaScript reduce)
        {
            _collection = collection;
            _map = Ensure.IsNotNull(map, nameof(map));
            _reduce = Ensure.IsNotNull(reduce, nameof(reduce));
        }

        /// <summary>
        /// Executes the specified cancellation token.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
#pragma warning disable CS0618 // Type or member is obsolete
                var cursor = _collection.MapReduce<BsonDocument>(_map, _reduce);
#pragma warning restore CS0618 // Type or member is obsolete

                var result = cursor.ToList(cancellationToken);
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
#pragma warning disable CS0618 // Type or member is obsolete
                var cursor = await _collection.MapReduceAsync<BsonDocument>(_map, _reduce);
#pragma warning restore CS0618 // Type or member is obsolete

                var result = await cursor.ToListAsync(cancellationToken);
                return OperationResult.FromResult(new BsonArray(result));
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedMapReduceOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedMapReduceOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedMapReduceOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            var collection = _entityMap.Collections[targetCollectionId];

            BsonJavaScript map = null, reduce = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "map":
                        map = argument.Value.AsBsonJavaScript;
                        break;
                    case "reduce":
                        reduce = argument.Value.AsBsonJavaScript;
                        break;
                    case "out":
                        var outDocument = argument.Value.AsBsonDocument;
                        if (!outDocument.Equals(new("inline", 1)))
                        {
                            throw new FormatException($"Invalid out setting '{argument.Value}'.");
                        }
                        break;
                    default:
                        throw new FormatException($"Invalid CountOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedMapReduceOperation(collection, map, reduce);
        }
    }
}
