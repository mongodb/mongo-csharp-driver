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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public abstract class UnifiedAggregateOperation : IUnifiedEntityTestOperation
    {
        protected readonly IMongoDatabase _database;
        protected readonly AggregateOptions _options;
        protected readonly List<BsonDocument> _pipeline;
        protected readonly IClientSessionHandle _session;

        protected UnifiedAggregateOperation(
            IClientSessionHandle session,
            IMongoDatabase database,
            List<BsonDocument> pipeline,
            AggregateOptions options)
        {
            _session = session;
            _database = database;
            _pipeline = pipeline;
            _options = options;
        }

        public abstract OperationResult Execute(CancellationToken cancellationToken);
        public abstract Task<OperationResult> ExecuteAsync(CancellationToken cancellationToken);
    }

    public class UnifiedDatabaseAggregateOperation : UnifiedAggregateOperation
    {
        public UnifiedDatabaseAggregateOperation(
            IClientSessionHandle session,
            IMongoDatabase database,
            List<BsonDocument> pipeline,
            AggregateOptions options)
            : base(session, database, pipeline, options)
        {
        }

        public override OperationResult Execute(CancellationToken cancellationToken)
        {
            var pipelineDefinition = new BsonDocumentStagePipelineDefinition<NoPipelineInput, BsonDocument>(_pipeline, BsonDocumentSerializer.Instance);
            try
            {
                using var cursor = _session == null
                    ? _database.Aggregate(pipelineDefinition, _options, cancellationToken)
                    : _database.Aggregate(_session, pipelineDefinition, _options, cancellationToken);

                var result = cursor.ToList(cancellationToken);

                return OperationResult.FromResult(new BsonArray(result));
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }

        public override async Task<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            var pipelineDefinition = new BsonDocumentStagePipelineDefinition<NoPipelineInput, BsonDocument>(_pipeline, BsonDocumentSerializer.Instance);
            try
            {
                using var cursor = _session == null
                    ? await _database.AggregateAsync(pipelineDefinition, _options, cancellationToken)
                    : await _database.AggregateAsync(_session, pipelineDefinition, _options, cancellationToken);

                var result = await cursor.ToListAsync(cancellationToken);

                return OperationResult.FromResult(new BsonArray(result));
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedCollectionAggregateOperation : UnifiedAggregateOperation
    {
        private readonly IMongoCollection<BsonDocument> _collection;

        public UnifiedCollectionAggregateOperation(
            IClientSessionHandle session,
            IMongoCollection<BsonDocument> collection,
            List<BsonDocument> pipeline,
            AggregateOptions options)
            : base(session, collection.Database, pipeline, options)
        {
            _collection = collection;
        }

        public override OperationResult Execute(CancellationToken cancellationToken)
        {
            var pipelineDefinition = new BsonDocumentStagePipelineDefinition<BsonDocument, BsonDocument>(_pipeline, BsonDocumentSerializer.Instance);
            try
            {
                using var cursor = _session == null
                    ? _collection.Aggregate(pipelineDefinition, _options, cancellationToken)
                    : _collection.Aggregate(_session, pipelineDefinition, _options, cancellationToken);

                var result = cursor.ToList(cancellationToken);

                return OperationResult.FromResult(new BsonArray(result));
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }

        public override async Task<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            var pipelineDefinition = new BsonDocumentStagePipelineDefinition<BsonDocument, BsonDocument>(_pipeline, BsonDocumentSerializer.Instance);
            try
            {
                using var cursor = _session == null
                    ? await _collection.AggregateAsync(pipelineDefinition, _options, cancellationToken)
                    : await _collection.AggregateAsync(_session, pipelineDefinition, _options, cancellationToken);

                var result = await cursor.ToListAsync(cancellationToken);

                return OperationResult.FromResult(new BsonArray(result));
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedAggregateOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedAggregateOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public IUnifiedEntityTestOperation BuildDatabaseOperation(string targetDatabaseId, BsonDocument arguments)
        {
            var database = _entityMap.Databases[targetDatabaseId];
            return Build(database, collection: null, arguments);
        }

        public IUnifiedEntityTestOperation BuildCollectionOperation(string targetCollectionId, BsonDocument arguments)
        {
            var collection = _entityMap.Collections[targetCollectionId];
            return Build(collection.Database, collection, arguments);
        }

        private IUnifiedEntityTestOperation Build(IMongoDatabase database, IMongoCollection<BsonDocument> collection, BsonDocument arguments)
        {
            AggregateOptions options = null;
            List<BsonDocument> pipeline = null;
            IClientSessionHandle session = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "allowDiskUse":
                        options ??= new AggregateOptions();
                        options.AllowDiskUse = argument.Value.AsBoolean;
                        break;
                    case "batchSize":
                        options ??= new AggregateOptions();
                        options.BatchSize = argument.Value.ToInt32();
                        break;
                    case "comment":
                        options ??= new AggregateOptions();
                        options.Comment = argument.Value;
                        break;
                    case "let":
                        options ??= new AggregateOptions();
                        options.Let = argument.Value.AsBsonDocument;
                        break;
                    case "pipeline":
                        pipeline = argument.Value.AsBsonArray.Cast<BsonDocument>().ToList();
                        break;
                    case "session":
                        session = _entityMap.Sessions[argument.Value.AsString];
                        break;
                    default:
                        throw new FormatException($"Invalid AggregateOperation argument name: '{argument.Name}'.");
                }
            }

            var lastStageName = pipeline.LastOrDefault()?.GetElement(0).Name;
            if (lastStageName == "$out" || lastStageName == "$merge")
            {
                return collection == null ?
                    new UnifiedDatabaseAggregateToCollectionOperation(session, database, pipeline, options) :
                    new UnifiedCollectionAggregateToCollectionOperation(session, collection, pipeline, options);
            }
            else
            {
                return collection == null ?
                    new UnifiedDatabaseAggregateOperation(session, database, pipeline, options) :
                    new UnifiedCollectionAggregateOperation(session, collection, pipeline, options);
            }
        }
    }
}
