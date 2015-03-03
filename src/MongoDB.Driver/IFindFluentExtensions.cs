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
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Extension methods for <see cref="IFindFluent{TDocument, TResult}"/>
    /// </summary>
    public static class IFindFluentExtensions
    {
        /// <summary>
        /// Projects the result.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="find">The fluent find.</param>
        /// <param name="projection">The projection.</param>
        /// <returns>The fluent find interface.</returns>
        public static IFindFluent<TDocument, BsonDocument> Project<TDocument, TResult>(this IFindFluent<TDocument, TResult> find, ProjectionDefinition<TDocument, BsonDocument> projection)
        {
            Ensure.IsNotNull(find, "find");
            Ensure.IsNotNull(projection, "projection");

            return find.Project<BsonDocument>(projection);
        }

        /// <summary>
        /// Projects the result.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="find">The fluent find.</param>
        /// <param name="projection">The projection.</param>
        /// <returns>The fluent find interface.</returns>
        public static IFindFluent<TDocument, TNewResult> Project<TDocument, TResult, TNewResult>(this IFindFluent<TDocument, TResult> find, Expression<Func<TDocument, TNewResult>> projection)
        {
            Ensure.IsNotNull(find, "find");
            Ensure.IsNotNull(projection, "projection");

            return find.Project<TNewResult>(new FindExpressionProjectionDefinition<TDocument, TNewResult>(projection));
        }

        /// <summary>
        /// Sorts the results by an ascending field.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="find">The fluent find.</param>
        /// <param name="field">The field.</param>
        /// <returns>The fluent find interface.</returns>
        public static IOrderedFindFluent<TDocument, TResult> SortBy<TDocument, TResult>(this IFindFluent<TDocument, TResult> find, Expression<Func<TDocument, object>> field)
        {
            Ensure.IsNotNull(find, "find");
            Ensure.IsNotNull(field, "field");

            // We require an implementation of IFindFluent<TDocument, TResult> 
            // to also implement IOrderedFindFluent<TDocument, TResult>
            return (IOrderedFindFluent<TDocument, TResult>)find.Sort(
                new DirectionalSort<TDocument>(new ExpressionFieldName<TDocument>(field), SortDirection.Ascending));
        }

        /// <summary>
        /// Sorts the results by a descending field.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="find">The fluent find.</param>
        /// <param name="field">The field.</param>
        /// <returns>The fluent find interface.</returns>
        public static IOrderedFindFluent<TDocument, TResult> SortByDescending<TDocument, TResult>(this IFindFluent<TDocument, TResult> find, Expression<Func<TDocument, object>> field)
        {
            Ensure.IsNotNull(find, "find");
            Ensure.IsNotNull(field, "field");

            // We require an implementation of IFindFluent<TDocument, TResult> 
            // to also implement IOrderedFindFluent<TDocument, TResult>
            return (IOrderedFindFluent<TDocument, TResult>)find.Sort(
                new DirectionalSort<TDocument>(new ExpressionFieldName<TDocument>(field), SortDirection.Descending));
        }

        /// <summary>
        /// Adds an ascending field to the existing sort.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="find">The fluent find.</param>
        /// <param name="field">The field.</param>
        /// <returns>The fluent find interface.</returns>
        public static IOrderedFindFluent<TDocument, TResult> ThenBy<TDocument, TResult>(this IOrderedFindFluent<TDocument, TResult> find, Expression<Func<TDocument, object>> field)
        {
            Ensure.IsNotNull(find, "find");
            Ensure.IsNotNull(field, "field");

            find.Options.Sort = new SortBuilder<TDocument>().Combine(
                find.Options.Sort,
                new DirectionalSort<TDocument>(new ExpressionFieldName<TDocument>(field), SortDirection.Ascending));

            return find;
        }

        /// <summary>
        /// Adds a descending field to the existing sort.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="find">The fluent find.</param>
        /// <param name="field">The field.</param>
        /// <returns>The fluent find interface.</returns>
        public static IOrderedFindFluent<TDocument, TResult> ThenByDescending<TDocument, TResult>(this IOrderedFindFluent<TDocument, TResult> find, Expression<Func<TDocument, object>> field)
        {
            Ensure.IsNotNull(find, "find");
            Ensure.IsNotNull(field, "field");

            find.Options.Sort = new SortBuilder<TDocument>().Combine(
                find.Options.Sort,
                new DirectionalSort<TDocument>(new ExpressionFieldName<TDocument>(field), SortDirection.Descending));

            return find;
        }

        /// <summary>
        /// Get the first result.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="find">The fluent find.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is the first result.</returns>
        public async static Task<TResult> FirstAsync<TDocument, TResult>(this IFindFluent<TDocument, TResult> find, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(find, "find");

            using (var cursor = await find.Limit(1).ToCursorAsync(cancellationToken).ConfigureAwait(false))
            {
                if (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    return cursor.Current.First();
                }
                else
                {
                    throw new InvalidOperationException("The source sequence is empty.");
                }
            }
        }

        /// <summary>
        /// Get the first result or null.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="find">The fluent find.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is the first result or null.</returns>
        public async static Task<TResult> FirstOrDefaultAsync<TDocument, TResult>(this IFindFluent<TDocument, TResult> find, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(find, "find");

            using (var cursor = await find.Limit(1).ToCursorAsync(cancellationToken).ConfigureAwait(false))
            {
                if (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    return cursor.Current.FirstOrDefault();
                }
                else
                {
                    return default(TResult);
                }
            }
        }

        /// <summary>
        /// Gets a single result.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="find">The fluent find.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is the single result.</returns>
        public async static Task<TResult> SingleAsync<TDocument, TResult>(this IFindFluent<TDocument, TResult> find, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(find, "find");

            using (var cursor = await find.Limit(2).ToCursorAsync(cancellationToken).ConfigureAwait(false))
            {
                if (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    return cursor.Current.Single();
                }
                else
                {
                    throw new InvalidOperationException("The source sequence is empty.");
                }
            }
        }

        /// <summary>
        /// Gets a single result or null.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="find">The fluent find.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is the single result or null.</returns>
        public async static Task<TResult> SingleOrDefaultAsync<TDocument, TResult>(this IFindFluent<TDocument, TResult> find, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(find, "find");

            using (var cursor = await find.Limit(2).ToCursorAsync(cancellationToken).ConfigureAwait(false))
            {
                if (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    return cursor.Current.SingleOrDefault();
                }
                else
                {
                    return default(TResult);
                }
            }
        }
    }
}
