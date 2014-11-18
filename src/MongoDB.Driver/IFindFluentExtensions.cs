using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Utils;

namespace MongoDB.Driver
{
    /// <summary>
    /// Extension methods for <see cref="IFindFluent{TDocument, TResult}"/>
    /// </summary>
    public static class FindFluentExtensions
    {
        /// <summary>
        /// Sorts the by.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="field">The field.</param>
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">The source sequence is empty.</exception>
        public async static Task<TResult> FirstAsync<TDocument, TResult>(this FindFluent<TDocument, TResult> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, "source");

            using (var cursor = await source.Limit(1).ToCursorAsync(cancellationToken))
            {
                if (await cursor.MoveNextAsync(cancellationToken))
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
        /// <returns></returns>
        public async static Task<TResult> FirstOrDefaultAsync<TDocument, TResult>(this FindFluent<TDocument, TResult> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, "source");

            using (var cursor = await source.Limit(1).ToCursorAsync(cancellationToken))
            {
                if (await cursor.MoveNextAsync(cancellationToken))
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
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">The source sequence is empty.</exception>
        public async static Task<TResult> SingleAsync<TDocument, TResult>(this FindFluent<TDocument, TResult> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, "source");

            using (var cursor = await source.Limit(2).ToCursorAsync(cancellationToken))
            {
                if (await cursor.MoveNextAsync(cancellationToken))
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
        /// <returns></returns>
        public async static Task<TResult> SingleOrDefaultAsync<TDocument, TResult>(this FindFluent<TDocument, TResult> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, "source");

            using (var cursor = await source.Limit(2).ToCursorAsync(cancellationToken))
            {
                if (await cursor.MoveNextAsync(cancellationToken))
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
