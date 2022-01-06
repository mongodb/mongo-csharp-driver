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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public abstract class UnifiedAggregateToCollectionOperation : IUnifiedEntityTestOperation
    {
        protected readonly IMongoDatabase _database;
        protected readonly AggregateOptions _options;
        protected readonly List<BsonDocument> _pipeline;
        protected readonly IClientSessionHandle _session;

        public UnifiedAggregateToCollectionOperation(
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

    public class UnifiedDatabaseAggregateToCollectionOperation : UnifiedAggregateToCollectionOperation
    {
        public UnifiedDatabaseAggregateToCollectionOperation(
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
                if (_session == null)
                {
                    _database.AggregateToCollection(pipelineDefinition, _options, cancellationToken);
                }
                else
                {
                    _database.AggregateToCollection(_session, pipelineDefinition, _options, cancellationToken);
                }

                return OperationResult.FromResult(null);
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
                if (_session == null)
                {
                    await _database.AggregateToCollectionAsync(pipelineDefinition, _options, cancellationToken);
                }
                else
                {
                    await _database.AggregateToCollectionAsync(_session, pipelineDefinition, _options, cancellationToken);
                }

                return OperationResult.FromResult(null);
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedCollectionAggregateToCollectionOperation : UnifiedAggregateToCollectionOperation
    {
        private readonly IMongoCollection<BsonDocument> _collection;

        public UnifiedCollectionAggregateToCollectionOperation(
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
                if (_session == null)
                {
                    _collection.AggregateToCollection(pipelineDefinition, _options, cancellationToken);
                }
                else
                {
                    _collection.AggregateToCollection(_session, pipelineDefinition, _options, cancellationToken);
                }

                return OperationResult.FromResult(null);
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
                if (_session == null)
                {
                    await _collection.AggregateToCollectionAsync(pipelineDefinition, _options, cancellationToken);
                }
                else
                {
                    await _collection.AggregateToCollectionAsync(_session, pipelineDefinition, _options, cancellationToken);
                }

                return OperationResult.FromResult(null);
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }
}
