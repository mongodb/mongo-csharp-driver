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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Translators;
using MongoDB.Driver.Linq.Utils;

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
        public static IFindFluent<TDocument, BsonDocument> Projection<TDocument, TResult>(this IFindFluent<TDocument, TResult> source, object projection)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(projection, "projection");

            return source.Projection<BsonDocument>(projection, BsonDocumentSerializer.Instance);
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

            var parameterSerializer = source.Collection.Settings.SerializerRegistry.GetSerializer<TDocument>();
            var projectionInfo = FindProjectionTranslator.Translate<TDocument, TNewResult>(projection, parameterSerializer);

            return source.Projection<TNewResult>(projectionInfo.Projection, projectionInfo.Serializer);
        }

        /// <summary>
        /// Sorts the by.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="field">The field.</param>
        /// <returns>The fluent find interface.</returns>
        public static IOrderedFindFluent<TDocument, TResult> SortBy<TDocument, TResult>(this IFindFluent<TDocument, TResult> source, Expression<Func<TResult, object>> field)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(field, "field");

            var helper = new BsonSerializationInfoHelper();
            helper.RegisterExpressionSerializer(field.Parameters[0], source.Collection.Settings.SerializerRegistry.GetSerializer<TResult>());
            var sortDocument = new SortByBuilder<TResult>(helper).Ascending(field).ToBsonDocument();

            source = source.Sort(sortDocument);

            return new FindFluent<TDocument, TResult>(source.Collection, source.Filter, source.Options);
        }

        /// <summary>
        /// Sorts the by descending.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="field">The field.</param>
        /// <returns>The fluent find interface.</returns>
        public static IOrderedFindFluent<TDocument, TResult> SortByDescending<TDocument, TResult>(this IFindFluent<TDocument, TResult> source, Expression<Func<TResult, object>> field)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(field, "field");

            var helper = new BsonSerializationInfoHelper();
            helper.RegisterExpressionSerializer(field.Parameters[0], source.Collection.Settings.SerializerRegistry.GetSerializer<TResult>());
            var sortDocument = new SortByBuilder<TResult>(helper).Descending(field).ToBsonDocument();

            source = source.Sort(sortDocument);

            return new FindFluent<TDocument, TResult>(source.Collection, source.Filter, source.Options);
        }

        /// <summary>
        /// Thens the by.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="field">The field.</param>
        /// <returns>The fluent find interface.</returns>
        public static IOrderedFindFluent<TDocument, TResult> ThenBy<TDocument, TResult>(this IOrderedFindFluent<TDocument, TResult> source, Expression<Func<TResult, object>> field)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(field, "field");

            var helper = new BsonSerializationInfoHelper();
            helper.RegisterExpressionSerializer(field.Parameters[0], source.Collection.Settings.SerializerRegistry.GetSerializer<TResult>());
            var sortDocument = new SortByBuilder<TResult>(helper).Ascending(field).ToBsonDocument();

            // this looks sketchy, but if we get here and this isn't true, then
            // someone is being a bad citizen.
            var currentSort = (BsonDocument)source.Options.Sort;

            currentSort.AddRange(sortDocument);

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
        public static IOrderedFindFluent<TDocument, TResult> ThenByDescending<TDocument, TResult>(this IOrderedFindFluent<TDocument, TResult> source, Expression<Func<TResult, object>> field)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(field, "field");

            var helper = new BsonSerializationInfoHelper();
            helper.RegisterExpressionSerializer(field.Parameters[0], source.Collection.Settings.SerializerRegistry.GetSerializer<TResult>());
            var sortDocument = new SortByBuilder<TResult>(helper).Descending(field).ToBsonDocument();

            // this looks sketchy, but if we get here and this isn't true, then
            // someone is being a bad citizen.
            var currentSort = (BsonDocument)source.Options.Sort;

            currentSort.AddRange(sortDocument);

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
