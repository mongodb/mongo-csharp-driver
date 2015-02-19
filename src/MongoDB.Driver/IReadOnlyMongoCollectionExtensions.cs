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
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Utils;

namespace MongoDB.Driver
{
    /// <summary>
    /// Extension methods for <see cref="IReadOnlyMongoCollection{T}"/>.
    /// </summary>
    public static class IReadOnlyMongoCollectionExtensions
    {
        /// <summary>
        /// Begins a fluent aggregation interface.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="options">The options.</param>
        /// <returns>
        /// A fluent aggregate interface.
        /// </returns>
        public static IAggregateFluent<TDocument> Aggregate<TDocument>(this IReadOnlyMongoCollection<TDocument> collection, AggregateOptions options = null)
        {
            AggregateOptions<TDocument> newOptions;
            if (options == null)
            {
                newOptions = new AggregateOptions<TDocument>
                {
                    ResultSerializer = collection.DocumentSerializer
                };
            }
            else
            {
                newOptions = new AggregateOptions<TDocument>
                {
                    AllowDiskUse = options.AllowDiskUse,
                    BatchSize = options.BatchSize,
                    MaxTime = options.MaxTime,
                    ResultSerializer = collection.DocumentSerializer,
                    UseCursor = options.UseCursor
                };
            }
            return new AggregateFluent<TDocument, TDocument>(collection, new List<object>(), newOptions);
        }

        /// <summary>
        /// Counts the number of documents in the collection.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The number of documents in the collection.
        /// </returns>
        public static Task<long> CountAsync<TDocument>(this IReadOnlyMongoCollection<TDocument> collection, IMongoQuery filter, CountOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(collection, "collection");
            Ensure.IsNotNull(filter, "filter");

            return collection.CountAsync(new ObjectFilter<TDocument>(filter), options, cancellationToken);
        }

        /// <summary>
        /// Counts the number of documents in the collection.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The number of documents in the collection.
        /// </returns>
        public static Task<long> CountAsync<TDocument>(this IReadOnlyMongoCollection<TDocument> collection, Expression<Func<TDocument, bool>> filter, CountOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(collection, "collection");
            Ensure.IsNotNull(filter, "filter");

            return collection.CountAsync(new ExpressionFilter<TDocument>(filter), options, cancellationToken);
        }

        /// <summary>
        /// Gets the distinct values for a specified field.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the result.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="fieldName">The field.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The distinct values for the specified field.
        /// </returns>
        public static Task<IAsyncCursor<TField>> DistinctAsync<TDocument, TField>(this IReadOnlyMongoCollection<TDocument> collection, Expression<Func<TDocument, TField>> fieldName, Filter<TDocument> filter, DistinctOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(collection, "collection");
            Ensure.IsNotNull(fieldName, "fieldName");
            Ensure.IsNotNull(filter, "filter");

            return collection.DistinctAsync<TField>(
                new ExpressionFieldName<TDocument, TField>(fieldName),
                filter,
                options,
                cancellationToken);
        }

        /// <summary>
        /// Gets the distinct values for a specified field.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the result.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="fieldName">The field.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The distinct values for the specified field.
        /// </returns>
        public static Task<IAsyncCursor<TField>> DistinctAsync<TDocument, TField>(this IReadOnlyMongoCollection<TDocument> collection, FieldName<TDocument, TField> fieldName, IMongoQuery filter, DistinctOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(collection, "collection");
            Ensure.IsNotNull(fieldName, "fieldName");
            Ensure.IsNotNull(filter, "filter");

            return collection.DistinctAsync<TField>(
                fieldName,
                new ObjectFilter<TDocument>(filter),
                options,
                cancellationToken);
        }

        /// <summary>
        /// Gets the distinct values for a specified field.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the result.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="fieldName">The field.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The distinct values for the specified field.
        /// </returns>
        public static Task<IAsyncCursor<TField>> DistinctAsync<TDocument, TField>(this IReadOnlyMongoCollection<TDocument> collection, Expression<Func<TDocument, TField>> fieldName, IMongoQuery filter, DistinctOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(collection, "collection");
            Ensure.IsNotNull(fieldName, "fieldName");
            Ensure.IsNotNull(filter, "filter");

            return collection.DistinctAsync<TField>(
                new ExpressionFieldName<TDocument, TField>(fieldName),
                new ObjectFilter<TDocument>(filter),
                options,
                cancellationToken);
        }

        /// <summary>
        /// Gets the distinct values for a specified field.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the result.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="fieldName">The field.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The distinct values for the specified field.
        /// </returns>
        public static Task<IAsyncCursor<TField>> DistinctAsync<TDocument, TField>(this IReadOnlyMongoCollection<TDocument> collection, FieldName<TDocument, TField> fieldName, Expression<Func<TDocument, bool>> filter, DistinctOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(collection, "collection");
            Ensure.IsNotNull(fieldName, "fieldName");
            Ensure.IsNotNull(filter, "filter");

            return collection.DistinctAsync<TField>(
                fieldName,
                new ExpressionFilter<TDocument>(filter),
                options,
                cancellationToken);
        }

        /// <summary>
        /// Gets the distinct values for a specified field.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the result.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="fieldName">The field.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The distinct values for the specified field.
        /// </returns>
        public static Task<IAsyncCursor<TField>> DistinctAsync<TDocument, TField>(this IReadOnlyMongoCollection<TDocument> collection, Expression<Func<TDocument, TField>> fieldName, Expression<Func<TDocument, bool>> filter, DistinctOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(collection, "collection");
            Ensure.IsNotNull(fieldName, "fieldName");
            Ensure.IsNotNull(filter, "filter");

            return collection.DistinctAsync<TField>(
                new ExpressionFieldName<TDocument, TField>(fieldName),
                new ExpressionFilter<TDocument>(filter),
                options,
                cancellationToken);
        }

        /// <summary>
        /// Begins a fluent find interface.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="options">The options.</param>
        /// <returns>
        /// A fluent find interface.
        /// </returns>
        public static IFindFluent<TDocument, TDocument> Find<TDocument>(this IReadOnlyMongoCollection<TDocument> collection, Filter<TDocument> filter, FindOptions options = null)
        {
            FindOptions<TDocument, TDocument> genericOptions;
            if (options == null)
            {
                genericOptions = new FindOptions<TDocument, TDocument>();
            }
            else
            {
                genericOptions = new FindOptions<TDocument, TDocument>
                {
                    AllowPartialResults = options.AllowPartialResults,
                    BatchSize = options.BatchSize,
                    Comment = options.Comment,
                    CursorType = options.CursorType,
                    MaxTime = options.MaxTime,
                    Modifiers = options.Modifiers,
                    NoCursorTimeout = options.NoCursorTimeout
                };
            }

            return new FindFluent<TDocument, TDocument>(collection, filter, genericOptions);
        }

        /// <summary>
        /// Begins a fluent find interface.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="options">The options.</param>
        /// <returns>
        /// A fluent interface.
        /// </returns>
        public static IFindFluent<TDocument, TDocument> Find<TDocument>(this IMongoCollection<TDocument> collection, IMongoQuery filter, FindOptions options = null)
        {
            Ensure.IsNotNull(collection, "collection");
            Ensure.IsNotNull(filter, "filter");

            return Find(collection, new ObjectFilter<TDocument>(filter), options);
        }

        /// <summary>
        /// Begins a fluent find interface.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="options">The options.</param>
        /// <returns>
        /// A fluent interface.
        /// </returns>
        public static IFindFluent<TDocument, TDocument> Find<TDocument>(this IMongoCollection<TDocument> collection, Expression<Func<TDocument, bool>> filter, FindOptions options = null)
        {
            Ensure.IsNotNull(collection, "collection");
            Ensure.IsNotNull(filter, "filter");

            return Find(collection, new ExpressionFilter<TDocument>(filter), options);
        }
    }
}
