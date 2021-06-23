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
    public class UnifiedListCollectionsOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoDatabase _database;
        private readonly ListCollectionsOptions _options;

        public UnifiedListCollectionsOperation(IMongoDatabase database, ListCollectionsOptions options)
        {
            _database = Ensure.IsNotNull(database, nameof(database));
            _options = options; // can be null
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                _database.ListCollections(_options, cancellationToken).ToList(); //TODO: fully iterate
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
                var cursor = await _database.ListCollectionsAsync(_options, cancellationToken).ConfigureAwait(false);
                _ = await cursor.ToListAsync().ConfigureAwait(false); //TODO: fully iterate
                return OperationResult.Empty();
            }
            catch (Exception ex)
            {
                return OperationResult.FromException(ex);
            }
        }
    }

    public class UnifiedListCollectionsOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedListCollectionsOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedListCollectionsOperation Build(string targetDatabaseId, BsonDocument arguments)
        {
            var database = _entityMap.GetDatabase(targetDatabaseId);

            var listCollectionsOptions = new ListCollectionsOptions();
            if (arguments != null)
            {
                foreach (var argument in arguments)
                {
                    switch (argument.Name)
                    {
                        case "filter":
                            listCollectionsOptions.Filter = argument.Value.AsBsonDocument;
                            break;
                        case "batchSize":
                            listCollectionsOptions.BatchSize = argument.Value.ToInt32();
                            break;
                        default:
                            throw new FormatException($"Invalid AssertIndexNotExistsOperation argument name: '{argument.Name}'.");
                    }
                }
            }

            return new UnifiedListCollectionsOperation(database, listCollectionsOptions);
        }
    }
}
