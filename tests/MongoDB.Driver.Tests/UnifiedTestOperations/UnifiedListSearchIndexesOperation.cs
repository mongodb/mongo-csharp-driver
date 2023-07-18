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
    public class UnifiedListSearchIndexesOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly AggregateOptions _aggregateOptions;
        private readonly string _indexName;

        public UnifiedListSearchIndexesOperation(
            IMongoCollection<BsonDocument> collection,
            AggregateOptions aggregateOptions,
            string indexName)
        {
            _collection = Ensure.IsNotNull(collection, nameof(collection));
            _aggregateOptions = aggregateOptions;
            _indexName = indexName;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                using var cursor = _collection.SearchIndexes.List(_indexName, _aggregateOptions, cancellationToken);
                var indexes = cursor.ToList(cancellationToken);

                return OperationResult.FromResult(new BsonArray(indexes));
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
                using var cursor = await _collection.SearchIndexes.ListAsync(_indexName, _aggregateOptions, cancellationToken).ConfigureAwait(false);
                var indexes = await cursor.ToListAsync(cancellationToken);

                return OperationResult.FromResult(new BsonArray(indexes));
            }
            catch (Exception ex)
            {
                return OperationResult.FromException(ex);
            }
        }
    }

    public class UnifiedListSearchIndexesOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedListSearchIndexesOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedListSearchIndexesOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            var collection = _entityMap.Collections[targetCollectionId];
            AggregateOptions options = null;
            string indexName = null;

            if (arguments != null)
            {
                foreach (var argument in arguments)
                {
                    switch (argument.Name)
                    {
                        case "name":
                            indexName = argument.Value.ToString();
                            break;
                        case "aggregationOptions":
                            foreach (var option in argument.Value.AsBsonDocument)
                            {
                                switch (option.Name)
                                {
                                    case "allowDiskUse":
                                        options ??= new AggregateOptions();
                                        options.AllowDiskUse = option.Value.AsBoolean;
                                        break;
                                    case "batchSize":
                                        options ??= new AggregateOptions();
                                        options.BatchSize = option.Value.ToInt32();
                                        break;
                                    case "comment":
                                        options ??= new AggregateOptions();
                                        options.Comment = option.Value;
                                        break;
                                    case "let":
                                        options ??= new AggregateOptions();
                                        options.Let = option.Value.AsBsonDocument;
                                        break;
                                    default:
                                        throw new FormatException($"Invalid AggregateOperation argument name: '{option.Name}'.");
                                }
                            }
                            break;
                        default:
                            throw new FormatException($"Invalid {nameof(UnifiedListSearchIndexesOperation)} argument name: '{argument.Name}'.");
                    }
                }
            }

            return new(collection, options, indexName);
        }
    }
}
