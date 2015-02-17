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
    /// Extension methods for <see cref="IReadableMongoCollection{T}"/>.
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
        public static IAggregateFluent<TDocument> Aggregate<TDocument>(this IReadableMongoCollection<TDocument> collection, AggregateOptions options = null)
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
        public static Task<long> CountAsync<TDocument>(this IReadableMongoCollection<TDocument> collection, Expression<Func<TDocument, bool>> filter, CountOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(collection, "collection");
            Ensure.IsNotNull(filter, "filter");

            return collection.CountAsync(filter, options, cancellationToken);
        }

        /// <summary>
        /// Gets the distinct values for a specified field.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the result.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="field">The field.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The distinct values for the specified field.
        /// </returns>
        public static Task<IAsyncCursor<TField>> DistinctAsync<TDocument, TField>(this IReadableMongoCollection<TDocument> collection, Expression<Func<TDocument, TField>> field, Expression<Func<TDocument, bool>> filter, DistinctOptions<TField> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(collection, "collection");
            Ensure.IsNotNull(filter, "filter");

            var helper = new BsonSerializationInfoHelper();
            helper.RegisterExpressionSerializer(field.Parameters[0], collection.Settings.SerializerRegistry.GetSerializer<TDocument>());

            var serializationInfo = helper.GetSerializationInfo(field.Body);
            options = options ?? new DistinctOptions<TField>();
            if (options.ResultSerializer == null)
            {
                options.ResultSerializer = (IBsonSerializer<TField>)serializationInfo.Serializer;
            }
            return collection.DistinctAsync(serializationInfo.ElementName, filter, options, cancellationToken);
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
        public static IFindFluent<TDocument, TDocument> Find<TDocument>(this IReadableMongoCollection<TDocument> collection, object filter, FindOptions options = null)
        {
            FindOptions<TDocument> genericOptions;
            if (options == null)
            {
                genericOptions = new FindOptions<TDocument>
                {
                    ResultSerializer = collection.DocumentSerializer
                };
            }
            else
            {
                genericOptions = new FindOptions<TDocument>
                {
                    AllowPartialResults = options.AllowPartialResults,
                    BatchSize = options.BatchSize,
                    Comment = options.Comment,
                    CursorType = options.CursorType,
                    MaxTime = options.MaxTime,
                    Modifiers = options.Modifiers,
                    NoCursorTimeout = options.NoCursorTimeout,
                    ResultSerializer = collection.DocumentSerializer
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
        public static IFindFluent<TDocument, TDocument> Find<TDocument>(this IMongoCollection<TDocument> collection, Expression<Func<TDocument, bool>> filter, FindOptions options = null)
        {
            Ensure.IsNotNull(collection, "collection");
            Ensure.IsNotNull(filter, "filter");

            return Find(collection, (object)filter, options);
        }
    }
}
