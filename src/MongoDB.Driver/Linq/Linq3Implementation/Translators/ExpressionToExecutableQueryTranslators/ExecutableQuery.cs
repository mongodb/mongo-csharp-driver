﻿/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Optimizers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToExecutableQueryTranslators
{
    internal static class ExecutableQueryExtensions
    {
        public static ExecutableQuery<TDocument, TResult> AsExecutableQuery<TDocument, TResult>(this ExecutableQuery<TDocument> executableQuery)
        {
            return (ExecutableQuery<TDocument, TResult>)executableQuery;
        }
    }

    internal static class ExecutableQuery
    {
        public static ExecutableQuery<TDocument, TOutput, TResult> Create<TDocument, TOutput, TResult>(
            MongoQueryProvider<TDocument> provider,
            AstPipeline unoptimizedPipeline,
            IExecutableQueryFinalizer<TOutput, TResult> finalizer)
        {
            var optimizedPipeline = AstPipelineOptimizer.Optimize(unoptimizedPipeline);
            var executedStagesLogger = provider.ExecutedStagesLogger;
            return provider.Collection == null ?
                new ExecutableQuery<TDocument, TOutput, TResult>(provider.Database, provider.Options, optimizedPipeline, finalizer, executedStagesLogger) :
                new ExecutableQuery<TDocument, TOutput, TResult>(provider.Collection, provider.Options, optimizedPipeline, finalizer, executedStagesLogger);
        }
    }

    internal abstract class ExecutableQuery<TDocument>
    {
        public abstract BsonDocument[] ExecutedStages { get; }
        public abstract AstPipeline Pipeline { get; }
    }

    internal abstract class ExecutableQuery<TDocument, TResult> : ExecutableQuery<TDocument>
    {
        public abstract TResult Execute(IClientSessionHandle session, CancellationToken cancellation);
        public abstract Task<TResult> ExecuteAsync(IClientSessionHandle session, CancellationToken cancellation);
    }

    internal class ExecutableQuery<TDocument, TOutput, TResult> : ExecutableQuery<TDocument, TResult>
    {
        // private fields
        private readonly IMongoCollection<TDocument> _collection;
        private readonly IMongoDatabase _database;
        private BsonDocument[] _executedStages = null;
        private readonly Action<BsonDocument[]> _executedStagesLogger;
        private readonly IExecutableQueryFinalizer<TOutput, TResult> _finalizer;
        private readonly AggregateOptions _options;
        private readonly AstPipeline _pipeline;

        // constructors
        public ExecutableQuery(
            IMongoCollection<TDocument> collection,
            AggregateOptions options,
            AstPipeline pipeline,
            IExecutableQueryFinalizer<TOutput, TResult> finalizer,
            Action<BsonDocument[]> executedStagesLogger)
            : this(options, pipeline, finalizer, executedStagesLogger)
        {
            _collection = Ensure.IsNotNull(collection, nameof(collection));
        }

        public ExecutableQuery(
            IMongoDatabase database,
            AggregateOptions options,
            AstPipeline pipeline,
            IExecutableQueryFinalizer<TOutput, TResult> finalizer,
            Action<BsonDocument[]> executedStagesLogger)
            : this(options, pipeline, finalizer, executedStagesLogger)
        {
            _database = Ensure.IsNotNull(database, nameof(database));
        }

        private ExecutableQuery(
            AggregateOptions options,
            AstPipeline pipeline,
            IExecutableQueryFinalizer<TOutput, TResult> finalizer,
            Action<BsonDocument[]> executedStagesLogger)
        {
            _options = options;
            _pipeline = pipeline;
            _finalizer = finalizer;
            _executedStagesLogger = executedStagesLogger;
        }

        // public properties
        public override BsonDocument[] ExecutedStages => _executedStages;
        public override AstPipeline Pipeline => _pipeline;

        // public methods
        public override TResult Execute(IClientSessionHandle session, CancellationToken cancellationToken)
        {
            var cursor = (_collection, session) switch
            {
                (null, null) => _database.Aggregate(CreateDatabasePipelineDefinition(), _options, cancellationToken),
                (null, _) => _database.Aggregate(session, CreateDatabasePipelineDefinition(), _options, cancellationToken),
                (_, null) => _collection.Aggregate(CreateCollectionPipelineDefinition(), _options, cancellationToken),
                (_, _) => _collection.Aggregate(session, CreateCollectionPipelineDefinition(), _options, cancellationToken)
            };

            return _finalizer.Finalize(cursor, cancellationToken);
        }

        public override async Task<TResult> ExecuteAsync(IClientSessionHandle session, CancellationToken cancellationToken)
        {
            var cursor = (_collection, session) switch
            {
                (null, null) => await _database.AggregateAsync(CreateDatabasePipelineDefinition(), _options, cancellationToken).ConfigureAwait(false),
                (null, _) => await _database.AggregateAsync(session, CreateDatabasePipelineDefinition(), _options, cancellationToken).ConfigureAwait(false),
                (_, null) => await _collection.AggregateAsync(CreateCollectionPipelineDefinition(), _options, cancellationToken).ConfigureAwait(false),
                (_, _) => await _collection.AggregateAsync(session, CreateCollectionPipelineDefinition(), _options, cancellationToken).ConfigureAwait(false)
            };

            return await _finalizer.FinalizeAsync(cursor, cancellationToken).ConfigureAwait(false);
        }

        public override string ToString()
        {
            var x = (object)_database?.DatabaseNamespace ?? _collection.CollectionNamespace;
            return $"{(_collection == null ? _database.DatabaseNamespace : _collection.CollectionNamespace)}.Aggregate({_pipeline})";
        }

        // private methods
        private BsonDocumentStagePipelineDefinition<TDocument, TOutput> CreateCollectionPipelineDefinition()
        {
            _executedStages = _pipeline.Render().AsBsonArray.Cast<BsonDocument>().ToArray();
            if (_executedStagesLogger != null)
            {
                _executedStagesLogger(_executedStages);
            }
            return new BsonDocumentStagePipelineDefinition<TDocument, TOutput>(_executedStages, (IBsonSerializer<TOutput>)_pipeline.OutputSerializer);
        }

        private BsonDocumentStagePipelineDefinition<NoPipelineInput, TOutput> CreateDatabasePipelineDefinition()
        {
            _executedStages = _pipeline.Render().AsBsonArray.Cast<BsonDocument>().ToArray();
            if (_executedStagesLogger != null)
            {
                _executedStagesLogger(_executedStages);
            }
            return new BsonDocumentStagePipelineDefinition<NoPipelineInput, TOutput>(_executedStages, (IBsonSerializer<TOutput>)_pipeline.OutputSerializer);
        }
    }
}
