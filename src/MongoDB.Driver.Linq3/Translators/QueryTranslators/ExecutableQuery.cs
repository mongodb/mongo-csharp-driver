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

namespace MongoDB.Driver.Linq3.Translators.QueryTranslators
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
        public abstract BsonDocument[] Stages { get; }
        public abstract TResult Execute(IClientSessionHandle session, CancellationToken cancellation);
        public abstract Task<TResult> ExecuteAsync(IClientSessionHandle session, CancellationToken cancellation);
    }

    public class ExecutableQuery<TDocument, TOutput, TResult> : ExecutableQuery<TDocument, TResult>
    {
        // private fields
        private readonly IMongoCollection<TDocument> _collection;
        private readonly IExecutableQueryFinalizer<TOutput, TResult> _finalizer;
        private readonly AggregateOptions _options;
        private readonly BsonDocumentStagePipelineDefinition<TDocument, TOutput> _pipeline;

        // constructors
        public ExecutableQuery(
            IMongoCollection<TDocument> collection,
            AggregateOptions options,
            BsonDocumentStagePipelineDefinition<TDocument, TOutput> pipeline,
            IExecutableQueryFinalizer<TOutput, TResult> finalizer)
        {
            _collection = collection;
            _options = options;
            _pipeline = pipeline;
            _finalizer = finalizer;
        }

        // public properties
        public override BsonDocument[] Stages => _pipeline.Documents.ToArray();

        // public methods
        public override TResult Execute(IClientSessionHandle session, CancellationToken cancellationToken)
        {
            IAsyncCursor<TOutput> cursor;
            if (session == null)
            {
                cursor = _collection.Aggregate(_pipeline, _options, cancellationToken);
            }
            else
            {
                cursor = _collection.Aggregate(session, _pipeline, _options, cancellationToken);
            }
            return _finalizer.Finalize(cursor, cancellationToken);
        }

        public override async Task<TResult> ExecuteAsync(IClientSessionHandle session, CancellationToken cancellationToken)
        {
            IAsyncCursor<TOutput> cursor;
            if (session == null)
            {
                cursor = await _collection.AggregateAsync(_pipeline, _options, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                cursor = await _collection.AggregateAsync(session, _pipeline, _options, cancellationToken).ConfigureAwait(false);
            }
            return await _finalizer.FinalizeAsync(cursor, cancellationToken).ConfigureAwait(false);
        }
    }
}
