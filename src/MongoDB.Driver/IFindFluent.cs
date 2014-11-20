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

using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver
{
    /// <summary>
    /// Fluent interface for find.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public interface IFindFluent<TDocument, TResult> : IAsyncCursorSource<TResult>
    {
        /// <summary>
        /// Gets the collection.
        /// </summary>
        IMongoCollection<TDocument> Collection { get; }

        /// <summary>
        /// Gets or sets the filter.
        /// </summary>
        object Filter { get; set; }

        /// <summary>
        /// Gets the options.
        /// </summary>
        FindOptions<TResult> Options { get; }

        /// <summary>
        /// Allows partial results from shards.
        /// </summary>
        /// <param name="allowPartialResults">if set to <c>true</c> [allow partial results].</param>
        /// <returns></returns>
        IFindFluent<TDocument, TResult> AllowPartialResults(bool allowPartialResults);

        /// <summary>
        /// Batches the size.
        /// </summary>
        /// <param name="size">The size.</param>
        /// <returns></returns>
        IFindFluent<TDocument, TResult> BatchSize(int? size);

        /// <summary>
        /// Comments the specified comment.
        /// </summary>
        /// <param name="comment">The comment.</param>
        /// <returns></returns>
        IFindFluent<TDocument, TResult> Comment(string comment);

        /// <summary>
        /// Sets the cursor type.
        /// </summary>
        /// <param name="cursorType">Type of the cursor.</param>
        /// <returns></returns>
        IFindFluent<TDocument, TResult> CursorType(CursorType cursorType);

        /// <summary>
        /// Limits the specified limit.
        /// </summary>
        /// <param name="limit">The limit.</param>
        /// <returns></returns>
        IFindFluent<TDocument, TResult> Limit(int? limit);

        /// <summary>
        /// Maximums the time.
        /// </summary>
        /// <param name="maxTime">The maximum time.</param>
        /// <returns></returns>
        IFindFluent<TDocument, TResult> MaxTime(TimeSpan? maxTime);

        /// <summary>
        /// Modifierses the specified modifiers.
        /// </summary>
        /// <param name="modifiers">The modifiers.</param>
        /// <returns></returns>
        IFindFluent<TDocument, TResult> Modifiers(BsonDocument modifiers);

        /// <summary>
        /// Noes the cursor timeout.
        /// </summary>
        /// <param name="noCursorTimeout">if set to <c>true</c> [no cursor timeout].</param>
        /// <returns></returns>
        IFindFluent<TDocument, TResult> NoCursorTimeout(bool noCursorTimeout);

        /// <summary>
        /// Projections the specified projection.
        /// </summary>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="projection">The projection.</param>
        /// <returns></returns>
        IFindFluent<TDocument, TNewResult> Projection<TNewResult>(object projection);

        /// <summary>
        /// Projections the specified projection.
        /// </summary>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="projection">The projection.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        /// <returns></returns>
        IFindFluent<TDocument, TNewResult> Projection<TNewResult>(object projection, IBsonSerializer<TNewResult> resultSerializer);

        /// <summary>
        /// Skips the specified skip.
        /// </summary>
        /// <param name="skip">The skip.</param>
        /// <returns></returns>
        IFindFluent<TDocument, TResult> Skip(int? skip);

        /// <summary>
        /// Sorts the specified sort.
        /// </summary>
        /// <param name="sort">The sort.</param>
        /// <returns></returns>
        IFindFluent<TDocument, TResult> Sort(object sort);
    }

    /// <summary>
    /// Fluent interface for find.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public interface IOrderedFindFluent<TDocument, TResult> : IFindFluent<TDocument, TResult>
    {
    }
}
