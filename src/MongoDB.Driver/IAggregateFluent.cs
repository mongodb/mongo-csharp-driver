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
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver
{
    /// <summary>
    /// Fluent interface for aggregate.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public interface IAggregateFluent<TDocument, TResult> : IAsyncCursorSource<TResult>
    {
        /// <summary>
        /// Gets the collection.
        /// </summary>
        IMongoCollection<TDocument> Collection { get; }

        /// <summary>
        /// Gets the options.
        /// </summary>
        AggregateOptions Options { get; }

        /// <summary>
        /// Gets the pipeline.
        /// </summary>
        IList<object> Pipeline { get; }

        /// <summary>
        /// Gets the result serializer.
        /// </summary>
        IBsonSerializer<TResult> ResultSerializer { get; }

        /// <summary>
        /// Appends a stage to the pipeline.
        /// </summary>
        /// <param name="stage">The stage.</param>
        /// <returns>The fluent aggregate interface.</returns>
        IAggregateFluent<TDocument, TResult> AppendStage(object stage);

        /// <summary>
        /// Appends a geoNear stage to the pipeline.
        /// </summary>
        /// <param name="geoNear">The geo near options.</param>
        /// <returns>The fluent aggregate interface.</returns>
        IAggregateFluent<TDocument, TResult> GeoNear(object geoNear);

        /// <summary>
        /// Appends a group stage to the pipeline.
        /// </summary>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="group">The group expressions.</param>
        /// <returns>The fluent aggregate interface.</returns>
        IAggregateFluent<TDocument, TNewResult> Group<TNewResult>(object group);

        /// <summary>
        /// Appends a group stage to the pipeline.
        /// </summary>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="group">The group expressions.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        /// <returns>The fluent aggregate interface.</returns>
        IAggregateFluent<TDocument, TNewResult> Group<TNewResult>(object group, IBsonSerializer<TNewResult> resultSerializer);

        /// <summary>
        /// Appends a limit stage to the pipeline.
        /// </summary>
        /// <param name="limit">The limit.</param>
        /// <returns>The fluent aggregate interface.</returns>
        IAggregateFluent<TDocument, TResult> Limit(int limit);

        /// <summary>
        /// Appends a match stage to the pipeline.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <returns>The fluent aggregate interface.</returns>
        IAggregateFluent<TDocument, TResult> Match(object filter);

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
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="project">The project specifications.</param>
        /// <returns>The fluent aggregate interface.</returns>
        IAggregateFluent<TDocument, TNewResult> Project<TNewResult>(object project);

        /// <summary>
        /// Appends a project stage to the pipeline.
        /// </summary>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="project">The project specifications.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        /// <returns>The fluent aggregate interface.</returns>
        IAggregateFluent<TDocument, TNewResult> Project<TNewResult>(object project, IBsonSerializer<TNewResult> resultSerializer);

        /// <summary>
        /// Appends a redact stage to the pipeline.
        /// </summary>
        /// <param name="redact">The redact expression.</param>
        /// <returns>The fluent aggregate interface.</returns>
        IAggregateFluent<TDocument, TResult> Redact(object redact);

        /// <summary>
        /// Appends a skip stage to the pipeline.
        /// </summary>
        /// <param name="skip">The number of documents to skip.</param>
        /// <returns>The fluent aggregate interface.</returns>
        IAggregateFluent<TDocument, TResult> Skip(int skip);

        /// <summary>
        /// Appends a sort stage to the pipeline.
        /// </summary>
        /// <param name="sort">The sort specification.</param>
        /// <returns>The fluent aggregate interface.</returns>
        IAggregateFluent<TDocument, TResult> Sort(object sort);

        /// <summary>
        /// Appends an unwind stage to the pipeline.
        /// </summary>
        /// <param name="fieldName">The name of the field to unwind.</param>
        /// <returns>The fluent aggregate interface.</returns>
        IAggregateFluent<TDocument, TNewResult> Unwind<TNewResult>(string fieldName);

        /// <summary>
        /// Appends an unwind stage to the pipeline.
        /// </summary>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="fieldName">The name of the field to unwind.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        /// <returns>The fluent aggregate interface.</returns>
        IAggregateFluent<TDocument, TNewResult> Unwind<TNewResult>(string fieldName, IBsonSerializer<TNewResult> resultSerializer);
    }

    /// <summary>
    /// Fluent interface for aggregate.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public interface IOrderedAggregateFluent<TDocument, TResult> : IAggregateFluent<TDocument, TResult>
    {
    }
}
