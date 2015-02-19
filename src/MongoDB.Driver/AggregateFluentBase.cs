/* Copyright 2010-2014 MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver
{
    /// <summary>
    /// Base class for implementors of <see cref="IAggregateFluent{TDocument}" />.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public abstract class AggregateFluentBase<TDocument> : IOrderedAggregateFluent<TDocument>
    {
        /// <inheritdoc />
        public abstract AggregateOptions Options { get; }

        /// <inheritdoc />
        public abstract IList<AggregateStage> Pipeline { get; }

        /// <inheritdoc />
        public abstract IAggregateFluent<TDocument> AppendStage(AggregateStage stage);

        /// <inheritdoc />
        public abstract IAggregateFluent<TResult> AppendStage<TResult>(AggregateStage stage);

        /// <inheritdoc />
        public abstract IAggregateFluent<TResult> Group<TResult>(Projection<TDocument, TResult> group);

        /// <inheritdoc />
        public abstract IAggregateFluent<TDocument> Limit(int limit);

        /// <inheritdoc />
        public abstract IAggregateFluent<TDocument> Match(Filter<TDocument> filter);

        /// <inheritdoc />
        public abstract Task<IAsyncCursor<TDocument>> OutAsync(string collectionName, CancellationToken cancellationToken = default(CancellationToken));

        /// <inheritdoc />
        public abstract IAggregateFluent<TResult> Project<TResult>(Projection<TDocument, TResult> project);

        /// <inheritdoc />
        public abstract IAggregateFluent<TDocument> Skip(int skip);

        /// <inheritdoc />
        public abstract IAggregateFluent<TDocument> Sort(Sort<TDocument> sort);

        /// <inheritdoc />
        public abstract IAggregateFluent<TResult> Unwind<TResult>(FieldName<TDocument> fieldName, IBsonSerializer<TResult> resultSerializer = null);

        /// <inheritdoc />
        public abstract Task<IAsyncCursor<TDocument>> ToCursorAsync(CancellationToken cancellationToken);
    }
}
