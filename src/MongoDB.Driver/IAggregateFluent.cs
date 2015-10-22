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
        /// Appends a project stage to the pipeline.
        /// </summary>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="newResultSerializer">The new result serializer.</param>
        /// <returns>The fluent aggregate interface.</returns>
        IAggregateFluent<TNewResult> As<TNewResult>(IBsonSerializer<TNewResult> newResultSerializer = null);

        /// <summary>
        /// Appends a group stage to the pipeline.
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
        /// Appends a lookup stage to the pipeline.
        /// </summary>
        /// <typeparam name="TForeignDocument">The type of the foreign document.</typeparam>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="foreignCollectionName">Name of the other collection.</param>
        /// <param name="localField">The local field.</param>
        /// <param name="foreignField">The foreign field.</param>
        /// <param name="as">The field in <typeparamref name="TNewResult" /> to place the foreign results.</param>
        /// <param name="options">The options.</param>
        /// <returns>The fluent aggregate interface.</returns>
        IAggregateFluent<TNewResult> Lookup<TForeignDocument, TNewResult>(string foreignCollectionName, FieldDefinition<TResult> localField, FieldDefinition<TForeignDocument> foreignField, FieldDefinition<TNewResult> @as, AggregateLookupOptions<TForeignDocument, TNewResult> options = null);

        /// <summary>
        /// Appends a match stage to the pipeline.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <returns>The fluent aggregate interface.</returns>
        IAggregateFluent<TResult> Match(FilterDefinition<TResult> filter);

        /// <summary>
        /// Appends a match stage to the pipeline that matches derived documents and changes the result type to the derived type.
        /// </summary>
        /// <typeparam name="TNewResult">The type of the derived documents.</typeparam>
        /// <param name="newResultSerializer">The new result serializer.</param>
        /// <returns>The fluent aggregate interface.</returns>
        IAggregateFluent<TNewResult> OfType<TNewResult>(IBsonSerializer<TNewResult> newResultSerializer = null) where TNewResult : TResult;

        /// <summary>
        /// Appends an out stage to the pipeline and executes it, and then returns a cursor to read the contents of the output collection.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A cursor.</returns>
        IAsyncCursor<TResult> Out(string collectionName, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Appends an out stage to the pipeline and executes it, and then returns a cursor to read the contents of the output collection.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is a cursor.</returns>
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
        [Obsolete("Use the Unwind overload which takes an options parameter.")]
        IAggregateFluent<TNewResult> Unwind<TNewResult>(FieldDefinition<TResult> field, IBsonSerializer<TNewResult> newResultSerializer);

        /// <summary>
        /// Appends an unwind stage to the pipeline.
        /// </summary>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="field">The field.</param>
        /// <param name="options">The options.</param>
        /// The fluent aggregate interface.
        IAggregateFluent<TNewResult> Unwind<TNewResult>(FieldDefinition<TResult> field, AggregateUnwindOptions<TNewResult> options = null);
    }


    /// <summary>
    /// Fluent interface for aggregate.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public interface IOrderedAggregateFluent<TResult> : IAggregateFluent<TResult>
    {
    }
}
