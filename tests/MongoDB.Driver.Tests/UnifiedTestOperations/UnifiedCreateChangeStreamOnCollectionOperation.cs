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
    public class UnifiedCreateChangeStreamOnCollectionOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly ChangeStreamOptions _options;
        private readonly BsonDocumentStagePipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>> _pipeline;

        public UnifiedCreateChangeStreamOnCollectionOperation(
            IMongoCollection<BsonDocument> collection,
            BsonDocumentStagePipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>> pipeline,
            ChangeStreamOptions options)
        {
            _collection = collection;
            _pipeline = pipeline;
            _options = options;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                var cursor = _collection.Watch(_pipeline, _options, cancellationToken);
                var changeStream = cursor.ToEnumerable().GetEnumerator();

                return OperationResult.FromChangeStream(changeStream);
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
                var cursor = await _collection.WatchAsync(_pipeline, _options, cancellationToken);
                var changeStream = cursor.ToEnumerable().GetEnumerator();

                return OperationResult.FromChangeStream(changeStream);
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedCreateChangeStreamOnCollectionOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedCreateChangeStreamOnCollectionOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedCreateChangeStreamOnCollectionOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            var collection = _entityMap.Collections[targetCollectionId];

            ChangeStreamOptions options = null;
            BsonDocumentStagePipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>> pipeline = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "batchSize":
                        options ??= new ChangeStreamOptions();
                        options.BatchSize = argument.Value.AsInt32;
                        break;
                    case "comment":
                        options ??= new ChangeStreamOptions();
                        options.Comment = argument.Value;
                        break;
                    case "fullDocument":
                        options ??= new ChangeStreamOptions();
                        options.FullDocument = (ChangeStreamFullDocumentOption)Enum.Parse(typeof(ChangeStreamFullDocumentOption), argument.Value.AsString, true);
                        break;
                    case "fullDocumentBeforeChange":
                        options ??= new ChangeStreamOptions();
                        options.FullDocumentBeforeChange = (ChangeStreamFullDocumentBeforeChangeOption)Enum.Parse(typeof(ChangeStreamFullDocumentBeforeChangeOption), argument.Value.AsString, true);
                        break;
                    case "pipeline":
                        var stages = argument.Value.AsBsonArray.Cast<BsonDocument>();
                        pipeline = new BsonDocumentStagePipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>(stages);
                        break;
                    case "showExpandedEvents":
                        options ??= new ChangeStreamOptions();
                        options.ShowExpandedEvents = argument.Value.AsNullableBoolean;
                        break;
                    default:
                        throw new FormatException($"Invalid CreateChangeStreamOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedCreateChangeStreamOnCollectionOperation(collection, pipeline, options);
        }
    }
}
