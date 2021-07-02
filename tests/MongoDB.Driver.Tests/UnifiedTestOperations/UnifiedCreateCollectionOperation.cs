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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedCreateCollectionOperation : IUnifiedEntityTestOperation
    {
        private readonly string _collectionName;
        private readonly CreateCollectionOptions _options;
        private readonly IMongoDatabase _database;
        private readonly IClientSessionHandle _session;

        public UnifiedCreateCollectionOperation(
            IClientSessionHandle session,
            IMongoDatabase database,
            string collectionName,
            CreateCollectionOptions options)
        {
            _session = session;
            _database = database;
            _collectionName = collectionName;
            _options = options;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                if (_session == null)
                {
                    _database.CreateCollection(_collectionName, _options, cancellationToken);
                }
                else
                {
                    _database.CreateCollection(_session, _collectionName, _options, cancellationToken);
                }

                return OperationResult.FromResult(null);
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
                if (_session == null)
                {
                    await _database.CreateCollectionAsync(_collectionName, _options, cancellationToken);
                }
                else
                {
                    await _database.CreateCollectionAsync(_session, _collectionName, _options, cancellationToken);
                }

                return OperationResult.FromResult(null);
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedCreateCollectionOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedCreateCollectionOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedCreateCollectionOperation Build(string targetDatabaseId, BsonDocument arguments)
        {
            var database = _entityMap.GetDatabase(targetDatabaseId);

            string collectionName = null;
            CreateCollectionOptions createCollectionOptions = null;
            IClientSessionHandle session = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "collection":
                        collectionName = argument.Value.AsString;
                        break;
                    case "expireAfterSeconds":
                        createCollectionOptions ??= new CreateCollectionOptions();
                        createCollectionOptions.ExpireAfter = TimeSpan.FromSeconds(argument.Value.ToInt64());
                        break;
                    case "session":
                        var sessionId = argument.Value.AsString;
                        session = _entityMap.GetSession(sessionId);
                        break;
                    case "timeseries":
                        var timeseries = argument.Value.AsBsonDocument;
                        var timeField = timeseries["timeField"].AsString;
                        var metaField = timeseries.TryGetValue("metaField", out var metaFieldValue) ? metaFieldValue.AsString : null;
                        TimeSeriesGranularity? granularity = null;
                        if (timeseries.TryGetValue("granularity", out var granularityValue))
                        {
                            granularity = (TimeSeriesGranularity)Enum.Parse(typeof(TimeSeriesGranularity), granularityValue.AsString, true);
                        }
                        createCollectionOptions ??= new CreateCollectionOptions();
                        createCollectionOptions.TimeSeriesOptions = new TimeSeriesOptions(timeField, metaField, granularity);
                        break;
                    default:
                        throw new FormatException($"Invalid CreateCollectionOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedCreateCollectionOperation(session, database, collectionName, createCollectionOptions);
        }
    }
}
