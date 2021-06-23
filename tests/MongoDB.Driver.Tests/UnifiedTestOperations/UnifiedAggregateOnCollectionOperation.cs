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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedAggregateOnCollectionOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly AggregateOptions _options;
        private readonly PipelineDefinition<BsonDocument, BsonDocument> _pipeline;

        public UnifiedAggregateOnCollectionOperation(
            IMongoCollection<BsonDocument> collection,
            PipelineDefinition<BsonDocument, BsonDocument> pipeline,
            AggregateOptions options)
        {
            _collection = collection;
            _pipeline = pipeline;
            _options = options;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                var cursor = _collection.Aggregate(_pipeline, _options, cancellationToken);
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
                var cursor = await _collection.AggregateAsync(_pipeline, _options, cancellationToken);
                var result = await cursor.ToListAsync();

                return OperationResult.FromResult(new BsonArray(result));
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedAggregateOnCollectionOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedAggregateOnCollectionOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public IUnifiedEntityTestOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            var collection = _entityMap.GetCollection(targetCollectionId);

            var options = new AggregateOptions();
            PipelineDefinition<BsonDocument, BsonDocument> pipeline = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "pipeline":
                        var stages = argument.Value.AsBsonArray.Cast<BsonDocument>();
                        pipeline = new BsonDocumentStagePipelineDefinition<BsonDocument, BsonDocument>(stages);
                        break;
                    case "batchSize":
                        options.BatchSize = argument.Value.ToInt32();
                        break;
                    default:
                        throw new FormatException($"Invalid AggregateOperation argument name: '{argument.Name}'.");
                }
            }

            if (pipeline.Stages.LastOrDefault()?.OperatorName == "$out")
            {
                return new UnifiedAggregateToCollectionOperation(collection, pipeline, options);
            }
            else
            {
                return new UnifiedAggregateOnCollectionOperation(collection, pipeline, options);
            }
        }
    }
}
