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
    public class UnifiedAggregateToDatabaseOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoDatabase _database;
        private readonly AggregateOptions _options;
        private readonly PipelineDefinition<NoPipelineInput, BsonDocument> _pipeline;

        public UnifiedAggregateToDatabaseOperation(
            IMongoDatabase database,
            PipelineDefinition<NoPipelineInput, BsonDocument> pipeline,
            AggregateOptions options)
        {
            _database = Ensure.IsNotNull(database, nameof(database));
            _pipeline = Ensure.IsNotNull(pipeline, nameof(pipeline));
            _options = options; // can be null
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                _database.AggregateToCollection(_pipeline, _options, cancellationToken);

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
                await _database.AggregateToCollectionAsync(_pipeline, _options, cancellationToken);

                return OperationResult.Empty();
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }
}
