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
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public interface IAggregateFluent<TDocument> : IAsyncCursorSource<TDocument>
    {
        /// <summary>
        /// Gets the options.
        /// </summary>
        AggregateOptions<TDocument> Options { get; }

        /// <summary>
        /// Gets the pipeline.
        /// </summary>
        IList<BsonDocument> Pipeline { get; }

        /// <summary>
        /// Appends a geoNear stage to the pipeline.
        /// </summary>
        /// <param name="geoNear">The geo near options.</param>
        /// <returns>The fluent aggregate interface.</returns>
        IAggregateFluent<TDocument> GeoNear(object geoNear);

        /// <summary>
        /// Appends a group stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="group">The group expressions.</param>
        /// <returns>The fluent aggregate interface.</returns>
        IAggregateFluent<TResult> Group<TResult>(Projection<TDocument, TResult> group);

        /// <summary>
        /// Appends a limit stage to the pipeline.
        /// </summary>
        /// <param name="limit">The limit.</param>
        /// <returns>The fluent aggregate interface.</returns>
        IAggregateFluent<TDocument> Limit(int limit);

        /// <summary>
        /// Appends a match stage to the pipeline.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <returns>The fluent aggregate interface.</returns>
        IAggregateFluent<TDocument> Match(Filter<TDocument> filter);

        /// <summary>
        /// Appends an out stage to the pipeline and executes it, and then returns a cursor to read the contents of the output collection.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The fluent aggregate interface.</returns>
        Task<IAsyncCursor<TDocument>> OutAsync(string collectionName, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Appends a project stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="project">The project specifications.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        IAggregateFluent<TResult> Project<TResult>(Projection<TDocument, TResult> project);

        /// <summary>
        /// Appends a redact stage to the pipeline.
        /// </summary>
        /// <param name="redact">The redact expression.</param>
        /// <returns>The fluent aggregate interface.</returns>
        IAggregateFluent<TDocument> Redact(object redact);

        /// <summary>
        /// Appends a skip stage to the pipeline.
        /// </summary>
        /// <param name="skip">The number of documents to skip.</param>
        /// <returns>The fluent aggregate interface.</returns>
        IAggregateFluent<TDocument> Skip(int skip);

        /// <summary>
        /// Appends a sort stage to the pipeline.
        /// </summary>
        /// <param name="sort">The sort specification.</param>
        /// <returns>The fluent aggregate interface.</returns>
        IAggregateFluent<TDocument> Sort(Sort<TDocument> sort);

        /// <summary>
        /// Appends an unwind stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        IAggregateFluent<TResult> Unwind<TResult>(FieldName<TDocument> fieldName, IBsonSerializer<TResult> resultSerializer = null);
    }

    /// <summary>
    /// Fluent interface for aggregate.
    /// </summary>
    /// <typeparam name="TDocument">The type of the result.</typeparam>
    public interface IOrderedAggregateFluent<TDocument> : IAggregateFluent<TDocument>
    {
    }
}
