/* Copyright 2010-2015 MongoDB Inc.
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
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver
{
    /// <summary>
    /// Base class for implementors of <see cref="IAggregateFluent{TResult}" />.
    /// </summary>
    /// <typeparam name="TResult">The type of the document.</typeparam>
    public abstract class AggregateFluentBase<TResult> : IOrderedAggregateFluent<TResult>
    {
        /// <inheritdoc />
        public abstract AggregateOptions Options { get; }

        /// <inheritdoc />
        public abstract IList<IPipelineStageDefinition> Stages { get; }

        /// <inheritdoc />
        public abstract IAggregateFluent<TNewResult> AppendStage<TNewResult>(PipelineStageDefinition<TResult, TNewResult> stage);

        /// <inheritdoc />
        public abstract IAggregateFluent<TNewResult> As<TNewResult>(IBsonSerializer<TNewResult> newResultSerializer);

        /// <inheritdoc />
        public abstract IAggregateFluent<TNewResult> Group<TNewResult>(ProjectionDefinition<TResult, TNewResult> group);

        /// <inheritdoc />
        public abstract IAggregateFluent<TResult> Limit(int limit);

        /// <inheritdoc />
        public virtual IAggregateFluent<TNewResult> Lookup<TForeignCollection, TNewResult>(string otherCollectionName, FieldDefinition<TResult> localField, FieldDefinition<TForeignCollection> foreignField, FieldDefinition<TNewResult> @as, AggregateLookupOptions<TForeignCollection, TNewResult> options)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public abstract IAggregateFluent<TResult> Match(FilterDefinition<TResult> filter);

        /// <inheritdoc />
        public abstract IAggregateFluent<TNewResult> OfType<TNewResult>(IBsonSerializer<TNewResult> newResultSerializer) where TNewResult : TResult;

        /// <inheritdoc />
        public virtual IAsyncCursor<TResult> Out(string collectionName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public abstract Task<IAsyncCursor<TResult>> OutAsync(string collectionName, CancellationToken cancellationToken);

        /// <inheritdoc />
        public abstract IAggregateFluent<TNewResult> Project<TNewResult>(ProjectionDefinition<TResult, TNewResult> projection);

        /// <inheritdoc />
        public abstract IAggregateFluent<TResult> Skip(int skip);

        /// <inheritdoc />
        public abstract IAggregateFluent<TResult> Sort(SortDefinition<TResult> sort);

        /// <inheritdoc />
        public abstract IAggregateFluent<TNewResult> Unwind<TNewResult>(FieldDefinition<TResult> field, IBsonSerializer<TNewResult> newResultSerializer);

        /// <inheritdoc />
        public virtual IAggregateFluent<TNewResult> Unwind<TNewResult>(FieldDefinition<TResult> field, AggregateUnwindOptions<TNewResult> options)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public virtual IAsyncCursor<TResult> ToCursor(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public abstract Task<IAsyncCursor<TResult>> ToCursorAsync(CancellationToken cancellationToken);
    }
}
