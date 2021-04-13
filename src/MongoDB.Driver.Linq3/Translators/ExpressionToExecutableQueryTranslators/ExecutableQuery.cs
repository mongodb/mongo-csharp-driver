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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq3.Ast;
using MongoDB.Driver.Linq3.Ast.PipelineOptimizer;
using MongoDB.Driver.Linq3.Translators.ExpressionToPipelineTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToExecutableQueryTranslators
{
    public static class ExecutableQueryExtensions
    {
        public static ExecutableQuery<TDocument, TResult> AsExecutableQuery<TDocument, TResult>(this ExecutableQuery<TDocument> executableQuery)
        {
            return (ExecutableQuery<TDocument, TResult>)executableQuery;
        }
    }

    public abstract class ExecutableQuery<TDocument>
    {
    }

    public abstract class ExecutableQuery<TDocument, TResult> : ExecutableQuery<TDocument>
    {
        public abstract AstPipeline Pipeline { get; }
        public abstract AstPipeline UnoptimizedPipeline { get; }

        public abstract TResult Execute(IClientSessionHandle session, CancellationToken cancellation);
        public abstract Task<TResult> ExecuteAsync(IClientSessionHandle session, CancellationToken cancellation);
    }

    public class ExecutableQuery<TDocument, TOutput, TResult> : ExecutableQuery<TDocument, TResult>
    {
        // private fields
        private readonly IMongoCollection<TDocument> _collection;
        private readonly IExecutableQueryFinalizer<TOutput, TResult> _finalizer;
        private readonly AggregateOptions _options;
        private readonly IBsonSerializer<TOutput> _outputSerializer;
        private readonly AstPipeline _pipeline;
        private readonly AstPipeline _unoptimizedPipeline;

        // constructors
        public ExecutableQuery(
            IMongoCollection<TDocument> collection,
            AggregateOptions options,
            Pipeline pipeline,
            IExecutableQueryFinalizer<TOutput, TResult> finalizer)
        {
            _collection = collection;
            _options = options;
            _unoptimizedPipeline = new AstPipeline(pipeline.Stages);
            _pipeline = AstPipelineOptimizer.Optimize(_unoptimizedPipeline);
            _outputSerializer = (IBsonSerializer<TOutput>)pipeline.OutputSerializer;
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

        // private methods
        private BsonDocumentStagePipelineDefinition<TDocument, TOutput> CreatePipelineDefinition()
        {
            var stages = _pipeline.Stages.Select(s => (BsonDocument)s.Render());
            return new BsonDocumentStagePipelineDefinition<TDocument, TOutput>(stages, _outputSerializer);
        }
    }
}
