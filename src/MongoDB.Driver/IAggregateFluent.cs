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
        /// Appends the stage.
        /// </summary>
        /// <param name="stage">The stage.</param>
        /// <returns></returns>
        IAggregateFluent<TDocument, TResult> AppendStage(object stage);

        /// <summary>
        /// Geoes the near.
        /// </summary>
        /// <param name="geoNear">The geo near.</param>
        /// <returns></returns>
        IAggregateFluent<TDocument, TResult> GeoNear(object geoNear);

        /// <summary>
        /// Groups the specified group.
        /// </summary>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="group">The group.</param>
        /// <returns></returns>
        IAggregateFluent<TDocument, TNewResult> Group<TNewResult>(object group);

        /// <summary>
        /// Groups the specified group.
        /// </summary>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="group">The group.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        /// <returns></returns>
        IAggregateFluent<TDocument, TNewResult> Group<TNewResult>(object group, IBsonSerializer<TNewResult> resultSerializer);

        /// <summary>
        /// Limits the specified limit.
        /// </summary>
        /// <param name="limit">The limit.</param>
        /// <returns></returns>
        IAggregateFluent<TDocument, TResult> Limit(int limit);

        /// <summary>
        /// Matches the specified filter.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <returns></returns>
        IAggregateFluent<TDocument, TResult> Match(object filter);

        /// <summary>
        /// Outs the specified collection name.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <returns></returns>
        Task<IAsyncCursor<TResult>> OutAsync(string collectionName);

        /// <summary>
        /// Projects the specified project.
        /// </summary>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="project">The project.</param>
        /// <returns></returns>
        IAggregateFluent<TDocument, TNewResult> Project<TNewResult>(object project);

        /// <summary>
        /// Projects the specified project.
        /// </summary>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="project">The project.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        /// <returns></returns>
        IAggregateFluent<TDocument, TNewResult> Project<TNewResult>(object project, IBsonSerializer<TNewResult> resultSerializer);

        /// <summary>
        /// Redacts the specified redact.
        /// </summary>
        /// <param name="redact">The redact.</param>
        /// <returns></returns>
        IAggregateFluent<TDocument, TResult> Redact(object redact);

        /// <summary>
        /// Skips the specified skip.
        /// </summary>
        /// <param name="skip">The skip.</param>
        /// <returns></returns>
        IAggregateFluent<TDocument, TResult> Skip(int skip);

        /// <summary>
        /// Sorts the specified sort.
        /// </summary>
        /// <param name="sort">The sort.</param>
        /// <returns></returns>
        IAggregateFluent<TDocument, TResult> Sort(object sort);

        /// <summary>
        /// Unwinds the specified field name.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns></returns>
        IAggregateFluent<TDocument, TNewResult> Unwind<TNewResult>(string fieldName);

        /// <summary>
        /// Unwinds the specified field name.
        /// </summary>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        /// <returns></returns>
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
