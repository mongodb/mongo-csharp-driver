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
        /// Projections the specified source.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="projection">The projection.</param>
        /// <returns>The fluent find interface.</returns>
        public static IFindFluent<TDocument, BsonDocument> Projection<TDocument, TResult>(this IFindFluent<TDocument, TResult> source, Projection<TDocument, BsonDocument> projection)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(projection, "projection");

            return source.Projection<BsonDocument>(projection);
        }

        /// <summary>
        /// Projections the specified source.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="projection">The projection.</param>
        /// <returns>The fluent find interface.</returns>
        public static IFindFluent<TDocument, TNewResult> Projection<TDocument, TResult, TNewResult>(this IFindFluent<TDocument, TResult> source, Expression<Func<TDocument, TNewResult>> projection)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(projection, "projection");

            return source.Projection<TNewResult>(new ClientSideExpressionProjection<TDocument, TNewResult>(projection));
        }

        /// <summary>
        /// Sorts the by.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="field">The field.</param>
        /// <returns>The fluent find interface.</returns>
        public static IOrderedFindFluent<TDocument, TResult> SortBy<TDocument, TResult>(this IFindFluent<TDocument, TResult> source, Expression<Func<TDocument, object>> field)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(field, "field");

            // We require an implementation of IFindFluent<TDocument, TResult> 
            // to also implement IOrderedFindFluent<TDocument, TResult>
            return (IOrderedFindFluent<TDocument, TResult>)source.Sort(
                new DirectionalSort<TDocument>(new ExpressionFieldName<TDocument>(field), SortDirection.Ascending));
        }

        /// <summary>
        /// Sorts the by descending.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="field">The field.</param>
        /// <returns>The fluent find interface.</returns>
        public static IOrderedFindFluent<TDocument, TResult> SortByDescending<TDocument, TResult>(this IFindFluent<TDocument, TResult> source, Expression<Func<TDocument, object>> field)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(field, "field");

            // We require an implementation of IFindFluent<TDocument, TResult> 
            // to also implement IOrderedFindFluent<TDocument, TResult>
            return (IOrderedFindFluent<TDocument, TResult>)source.Sort(
                new DirectionalSort<TDocument>(new ExpressionFieldName<TDocument>(field), SortDirection.Descending));
        }

        /// <summary>
        /// Thens the by.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="field">The field.</param>
        /// <returns>The fluent find interface.</returns>
        public static IOrderedFindFluent<TDocument, TResult> ThenBy<TDocument, TResult>(this IOrderedFindFluent<TDocument, TResult> source, Expression<Func<TDocument, object>> field)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(field, "field");

            source.Options.Sort = new SortBuilder<TDocument>().Combine(
                source.Options.Sort,
                new DirectionalSort<TDocument>(new ExpressionFieldName<TDocument>(field), SortDirection.Ascending));

            return source;
        }

        /// <summary>
        /// Thens the by descending.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="field">The field.</param>
        /// <returns>The fluent find interface.</returns>
        public static IOrderedFindFluent<TDocument, TResult> ThenByDescending<TDocument, TResult>(this IOrderedFindFluent<TDocument, TResult> source, Expression<Func<TDocument, object>> field)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(field, "field");

            source.Options.Sort = new SortBuilder<TDocument>().Combine(
                source.Options.Sort,
                new DirectionalSort<TDocument>(new ExpressionFieldName<TDocument>(field), SortDirection.Descending));

            return source;
        }

        /// <summary>
        /// Firsts the asynchronous.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The fluent find interface.</returns>
        public async static Task<TResult> FirstAsync<TDocument, TResult>(this IFindFluent<TDocument, TResult> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, "source");

            using (var cursor = await source.Limit(1).ToCursorAsync(cancellationToken).ConfigureAwait(false))
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
        /// Firsts the or default asynchronous.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The fluent find interface.</returns>
        public async static Task<TResult> FirstOrDefaultAsync<TDocument, TResult>(this IFindFluent<TDocument, TResult> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, "source");

            using (var cursor = await source.Limit(1).ToCursorAsync(cancellationToken).ConfigureAwait(false))
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
        /// Singles the asynchronous.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The fluent find interface.</returns>
        public async static Task<TResult> SingleAsync<TDocument, TResult>(this IFindFluent<TDocument, TResult> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, "source");

            using (var cursor = await source.Limit(2).ToCursorAsync(cancellationToken).ConfigureAwait(false))
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
        /// Singles the or default asynchronous.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The fluent find interface.</returns>
        public async static Task<TResult> SingleOrDefaultAsync<TDocument, TResult>(this IFindFluent<TDocument, TResult> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, "source");

            using (var cursor = await source.Limit(2).ToCursorAsync(cancellationToken).ConfigureAwait(false))
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
