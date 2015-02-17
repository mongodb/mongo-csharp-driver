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
using System.Threading;
using System.Threading.Tasks;
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
        /// Gets the document serializer.
        /// </summary>
        IBsonSerializer<TDocument> DocumentSerializer { get; }

        /// <summary>
        /// Gets or sets the filter.
        /// </summary>
        object Filter { get; set; }

        /// <summary>
        /// Gets the options.
        /// </summary>
        FindOptions<TResult> Options { get; }

        /// <summary>
        /// Counts the number of documents.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The fluent find interface.</returns>
        Task<long> CountAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Limits the number of documents.
        /// </summary>
        /// <param name="limit">The limit.</param>
        /// <returns>The fluent find interface.</returns>
        IFindFluent<TDocument, TResult> Limit(int? limit);

        /// <summary>
        /// Projects the the documents.
        /// </summary>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="projection">The projection.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        /// <returns>The fluent find interface.</returns>
        IFindFluent<TDocument, TNewResult> Projection<TNewResult>(object projection, IBsonSerializer<TNewResult> resultSerializer = null);

        /// <summary>
        /// Skips the the specified number of documents.
        /// </summary>
        /// <param name="skip">The skip.</param>
        /// <returns>The fluent find interface.</returns>
        IFindFluent<TDocument, TResult> Skip(int? skip);

        /// <summary>
        /// Sorts the the documents.
        /// </summary>
        /// <param name="sort">The sort.</param>
        /// <returns>The fluent find interface.</returns>
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
