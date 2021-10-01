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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
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
            IMongoCollection<TDocument> collection,
            AggregateOptions options,
            AstPipeline unoptimizedPipeline,
            IExecutableQueryFinalizer<TOutput, TResult> finalizer)
        {
            var pipeline = AstPipelineOptimizer.Optimize(unoptimizedPipeline);
            return new ExecutableQuery<TDocument, TOutput, TResult>(collection, options, unoptimizedPipeline, pipeline, finalizer);
        }
    }

    internal abstract class ExecutableQuery<TDocument>
    {
    }

    internal abstract class ExecutableQuery<TDocument, TResult> : ExecutableQuery<TDocument>
    {
        public abstract AstPipeline Pipeline { get; }
        public abstract AstPipeline UnoptimizedPipeline { get; }

        public abstract TResult Execute(IClientSessionHandle session, CancellationToken cancellation);
        public abstract Task<TResult> ExecuteAsync(IClientSessionHandle session, CancellationToken cancellation);
    }

    internal class ExecutableQuery<TDocument, TOutput, TResult> : ExecutableQuery<TDocument, TResult>
    {
        // private fields
        private readonly IMongoCollection<TDocument> _collection;
        private readonly IExecutableQueryFinalizer<TOutput, TResult> _finalizer;
        private readonly AggregateOptions _options;
        private readonly AstPipeline _pipeline;
        private readonly AstPipeline _unoptimizedPipeline;

        // constructors
        public ExecutableQuery(
            IMongoCollection<TDocument> collection,
            AggregateOptions options,
            AstPipeline unoptimizedPipeline,
            AstPipeline pipeline,
            IExecutableQueryFinalizer<TOutput, TResult> finalizer)
        {
            _collection = collection;
            _options = options;
            _unoptimizedPipeline = unoptimizedPipeline;
            _pipeline = pipeline;
            _finalizer = finalizer;
        }

        // public properties
        public override AstPipeline Pipeline => _pipeline;
        public override AstPipeline UnoptimizedPipeline => _unoptimizedPipeline;

        // public methods
        public override TResult Execute(IClientSessionHandle session, CancellationToken cancellationToken)
        {
            var pipelineDefinition = CreatePipelineDefinition();
            IAsyncCursor<TOutput> cursor;
            if (session == null)
            {
                cursor = _collection.Aggregate(pipelineDefinition, _options, cancellationToken);
            }
            else
            {
                cursor = _collection.Aggregate(session, pipelineDefinition, _options, cancellationToken);
            }
            return _finalizer.Finalize(cursor, cancellationToken);
        }

        public override async Task<TResult> ExecuteAsync(IClientSessionHandle session, CancellationToken cancellationToken)
        {
            var pipelineDefinition = CreatePipelineDefinition();
            IAsyncCursor<TOutput> cursor;
            if (session == null)
            {
                cursor = await _collection.AggregateAsync(pipelineDefinition, _options, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                cursor = await _collection.AggregateAsync(session, pipelineDefinition, _options, cancellationToken).ConfigureAwait(false);
            }
            return await _finalizer.FinalizeAsync(cursor, cancellationToken).ConfigureAwait(false);
        }

        public override string ToString()
        {
            return $"{_collection.CollectionNamespace}.Aggregate({_pipeline})";
        }

        // private methods
        private BsonDocumentStagePipelineDefinition<TDocument, TOutput> CreatePipelineDefinition()
        {
            var stages = _pipeline.Stages.Select(s => (BsonDocument)s.Render());
            return new BsonDocumentStagePipelineDefinition<TDocument, TOutput>(stages, (IBsonSerializer<TOutput>)_pipeline.OutputSerializer);
        }
    }
}
