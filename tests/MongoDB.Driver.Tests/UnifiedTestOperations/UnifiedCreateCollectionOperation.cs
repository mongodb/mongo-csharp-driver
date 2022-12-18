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

    public class UnifiedCreateViewOperation : IUnifiedEntityTestOperation
    {
        private readonly string _viewName;
        private readonly string _viewOn;
        private readonly PipelineDefinition<BsonDocument, BsonDocument> _pipeline;
        private readonly CreateViewOptions<BsonDocument> _options;
        private readonly IMongoDatabase _database;
        private readonly IClientSessionHandle _session;

        public UnifiedCreateViewOperation(
            IClientSessionHandle session,
            IMongoDatabase database,
            string viewName,
            string viewOn,
            PipelineDefinition<BsonDocument, BsonDocument> pipeline,
            CreateViewOptions<BsonDocument> options)
        {
            _session = session;
            _database = database;
            _viewName = viewName;
            _viewOn = viewOn;
            _pipeline = pipeline;
            _options = options;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                if (_session == null)
                {
                    _database.CreateView(_viewName, _viewOn, _pipeline, _options, cancellationToken);
                }
                else
                {
                    _database.CreateView(_session, _viewName, _viewOn, _pipeline, _options, cancellationToken);
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
                    await _database.CreateViewAsync(_viewName, _viewOn, _pipeline, _options, cancellationToken);
                }
                else
                {
                    await _database.CreateViewAsync(_session, _viewName, _viewOn, _pipeline, _options, cancellationToken);
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

        public IUnifiedEntityTestOperation Build(string targetDatabaseId, BsonDocument arguments)
        {
            var database = _entityMap.Databases[targetDatabaseId];

            string name = null;
            string viewOn = null;
            PipelineDefinition<BsonDocument, BsonDocument> pipeline = null;
            IClientSessionHandle session = null;
            TimeSpan? expireAfter = null;
            TimeSeriesOptions timeSeriesOptions = null;
            ClusteredIndexOptions<BsonDocument> clusteredIndex = null;
            ChangeStreamPreAndPostImagesOptions changeStreamPreAndPostImageOptions = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "changeStreamPreAndPostImages":
                        changeStreamPreAndPostImageOptions = new ChangeStreamPreAndPostImagesOptions(argument.Value.AsBsonDocument);
                        break;
                    case "clusteredIndex":
                        var clusteredIndexSpecification = argument.Value.AsBsonDocument;
                        clusteredIndex = new ClusteredIndexOptions<BsonDocument>
                        {
                            Key = clusteredIndexSpecification["key"].AsBsonDocument,
                            Unique = clusteredIndexSpecification["unique"].AsBoolean,
                            Name = clusteredIndexSpecification["name"].AsString
                        };
                        break;
                    case "collection":
                        name = argument.Value.AsString;
                        break;
                    case "expireAfterSeconds":
                        expireAfter = TimeSpan.FromSeconds(argument.Value.ToInt64());
                        break;
                    case "pipeline":
                        pipeline = new EmptyPipelineDefinition<BsonDocument>();
                        foreach (var stage in argument.Value.AsBsonArray)
                        {
                            pipeline = pipeline.AppendStage<BsonDocument, BsonDocument, BsonDocument>(stage.AsBsonDocument);
                        }
                        break;
                    case "session":
                        var sessionId = argument.Value.AsString;
                        session = _entityMap.Sessions[sessionId];
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
                        timeSeriesOptions = new TimeSeriesOptions(timeField, metaField, granularity);
                        break;
                    case "viewOn":
                        viewOn = argument.Value.AsString;
                        break;
                    default:
                        throw new FormatException($"Invalid CreateCollectionOperation argument name: '{argument.Name}'.");
                }
            }

            if (viewOn == null && pipeline == null)
            {
                var options = new CreateCollectionOptions<BsonDocument> { ExpireAfter = expireAfter, TimeSeriesOptions = timeSeriesOptions, ClusteredIndex = clusteredIndex, ChangeStreamPreAndPostImagesOptions = changeStreamPreAndPostImageOptions };
                return new UnifiedCreateCollectionOperation(session, database, name, options);
            }
            if (viewOn != null && expireAfter == null && timeSeriesOptions == null && clusteredIndex == null)
            {
                var options = new CreateViewOptions<BsonDocument>();
                pipeline ??= new EmptyPipelineDefinition<BsonDocument>();
                return new UnifiedCreateViewOperation(session, database, name, viewOn, pipeline, options);
            }

            var invalidArguments = string.Join(",", arguments.Elements.Select(x => x.Name));
            throw new InvalidOperationException($"Invalid combination of CreateCollectionOperation arguments: {invalidArguments}");
        }
    }
}
