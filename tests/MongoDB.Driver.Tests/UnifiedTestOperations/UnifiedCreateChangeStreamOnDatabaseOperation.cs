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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedCreateChangeStreamOnDatabaseOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoDatabase _database;
        private readonly ChangeStreamOptions _options;
        private readonly BsonDocumentStagePipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>> _pipeline;

        public UnifiedCreateChangeStreamOnDatabaseOperation(
            IMongoDatabase database,
            BsonDocumentStagePipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>> pipeline,
            ChangeStreamOptions options)
        {
            _database = database;
            _pipeline = pipeline;
            _options = options;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                var cursor = _database.Watch(_pipeline, _options, cancellationToken);
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
                var cursor = await _database.WatchAsync(_pipeline, _options, cancellationToken);
                var changeStream = cursor.ToEnumerable().GetEnumerator();

                return OperationResult.FromChangeStream(changeStream);
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedCreateChangeStreamOnDatabaseOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedCreateChangeStreamOnDatabaseOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedCreateChangeStreamOnDatabaseOperation Build(string targetDatabaseId, BsonDocument arguments)
        {
            var database = _entityMap.Databases[targetDatabaseId];

            ChangeStreamOptions options = null;
            BsonDocumentStagePipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>> pipeline = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "batchSize":
                        options = options ?? new ChangeStreamOptions();
                        options.BatchSize = argument.Value.AsInt32;
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

            return new UnifiedCreateChangeStreamOnDatabaseOperation(database, pipeline, options);
        }
    }
}
