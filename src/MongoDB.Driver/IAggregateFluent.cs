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
    /// Fluent interface for aggregate.
    /// </summary>
    /// <remarks>
    /// This interface is not guaranteed to remain stable. Implementors should use
    /// <see cref="AggregateFluentBase{TResult}" />.
    /// </remarks>
    /// <typeparam name="TResult">The type of the result of the pipeline.</typeparam>
    public interface IAggregateFluent<TResult> : IAsyncCursorSource<TResult>
    {
        /// <summary>
        /// Gets the options.
        /// </summary>
        AggregateOptions Options { get; }

        /// <summary>
        /// Gets the stages.
        /// </summary>
        IList<IPipelineStageDefinition> Stages { get; }

        /// <summary>
        /// Appends the stage to the pipeline.
        /// </summary>
        /// <typeparam name="TNewResult">The type of the result of the stage.</typeparam>
        /// <param name="stage">The stage.</param>
        /// <returns>The fluent aggregate interface.</returns>
        IAggregateFluent<TNewResult> AppendStage<TNewResult>(PipelineStageDefinition<TResult, TNewResult> stage);

        /// <summary>
        /// Appends a group stage to the stages.
        /// </summary>
        /// <typeparam name="TNewResult">The type of the result of the stage.</typeparam>
        /// <param name="group">The group projection.</param>
        /// <returns>The fluent aggregate interface.</returns>
        IAggregateFluent<TNewResult> Group<TNewResult>(ProjectionDefinition<TResult, TNewResult> group);

        /// <summary>
        /// Appends a limit stage to the pipeline.
        /// </summary>
        /// <param name="limit">The limit.</param>
        /// <returns>The fluent aggregate interface.</returns>
        IAggregateFluent<TResult> Limit(int limit);

        /// <summary>
        /// Appends a match stage to the pipeline.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <returns>The fluent aggregate interface.</returns>
        IAggregateFluent<TResult> Match(FilterDefinition<TResult> filter);

        /// <summary>
        /// Appends an out stage to the pipeline and executes it, and then returns a cursor to read the contents of the output collection.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The fluent aggregate interface.</returns>
        Task<IAsyncCursor<TResult>> OutAsync(string collectionName, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Appends a project stage to the pipeline.
        /// </summary>
        /// <typeparam name="TNewResult">The type of the result of the stage.</typeparam>
        /// <param name="projection">The projection.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        IAggregateFluent<TNewResult> Project<TNewResult>(ProjectionDefinition<TResult, TNewResult> projection);

        /// <summary>
        /// Appends a skip stage to the pipeline.
        /// </summary>
        /// <param name="skip">The number of documents to skip.</param>
        /// <returns>The fluent aggregate interface.</returns>
        IAggregateFluent<TResult> Skip(int skip);

        /// <summary>
        /// Appends a sort stage to the pipeline.
        /// </summary>
        /// <param name="sort">The sort specification.</param>
        /// <returns>The fluent aggregate interface.</returns>
        IAggregateFluent<TResult> Sort(SortDefinition<TResult> sort);

        /// <summary>
        /// Appends an unwind stage to the pipeline.
        /// </summary>
        /// <typeparam name="TNewResult">The type of the result of the stage.</typeparam>
        /// <param name="field">The field.</param>
        /// <param name="newResultSerializer">The new result serializer.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        IAggregateFluent<TNewResult> Unwind<TNewResult>(FieldDefinition<TResult> field, IBsonSerializer<TNewResult> newResultSerializer = null);
    }

    /// <summary>
    /// Fluent interface for aggregate.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public interface IOrderedAggregateFluent<TResult> : IAggregateFluent<TResult>
    {
    }
}
