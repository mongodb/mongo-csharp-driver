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
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Utils;

namespace MongoDB.Driver
{
    /// <summary>
    /// Extension methods for <see cref="IAggregateFluent{TDocument, TResult}"/>
    /// </summary>
    public static class IAggregateFluentExtensions
    {
        /// <summary>
        /// Groups the specified source.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="group">The group.</param>
        /// <returns></returns>
        public static IAggregateFluent<TDocument, BsonDocument> Group<TDocument, TResult>(this IAggregateFluent<TDocument, TResult> source, object group)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(group, "group");

            return source.Group<BsonDocument>(group, BsonDocumentSerializer.Instance);
        }

        /// <summary>
        /// Matches the specified match.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="filter">The filter.</param>
        /// <returns></returns>
        public static IAggregateFluent<TDocument, TResult> Match<TDocument, TResult>(this IAggregateFluent<TDocument, TResult> source, Expression<Func<TResult, bool>> filter)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(filter, "filter");

            return source.Match(filter);
        }

        /// <summary>
        /// Projects the specified source.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="project">The project.</param>
        /// <returns></returns>
        public static IAggregateFluent<TDocument, BsonDocument> Project<TDocument, TResult>(this IAggregateFluent<TDocument, TResult> source, object project)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(project, "project");

            return source.Project<BsonDocument>(project, BsonDocumentSerializer.Instance);
        }

        /// <summary>
        /// Sorts the by.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="field">The field.</param>
        /// <returns></returns>
        public static IOrderedAggregateFluent<TDocument, TResult> SortBy<TDocument, TResult>(this IAggregateFluent<TDocument, TResult> source, Expression<Func<TResult, object>> field)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(field, "field");

            var helper = new BsonSerializationInfoHelper();
            helper.RegisterExpressionSerializer(field.Parameters[0], source.Collection.Settings.SerializerRegistry.GetSerializer<TResult>());
            var sortDocument = new SortByBuilder<TResult>(helper).Ascending(field).ToBsonDocument();

            source = source.Sort(sortDocument);

            return new AggregateFluent<TDocument, TResult>(source.Collection, source.Pipeline, source.Options, source.ResultSerializer);
        }

        /// <summary>
        /// Sorts the by descending.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="field">The field.</param>
        /// <returns></returns>
        public static IOrderedAggregateFluent<TDocument, TResult> SortByDescending<TDocument, TResult>(this IAggregateFluent<TDocument, TResult> source, Expression<Func<TResult, object>> field)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(field, "field");

            var helper = new BsonSerializationInfoHelper();
            helper.RegisterExpressionSerializer(field.Parameters[0], source.Collection.Settings.SerializerRegistry.GetSerializer<TResult>());
            var sortDocument = new SortByBuilder<TResult>(helper).Descending(field).ToBsonDocument();

            source = source.Sort(sortDocument);

            return new AggregateFluent<TDocument, TResult>(source.Collection, source.Pipeline, source.Options, source.ResultSerializer);
        }

        /// <summary>
        /// Thens the by.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="field">The field.</param>
        /// <returns></returns>
        public static IOrderedAggregateFluent<TDocument, TResult> ThenBy<TDocument, TResult>(this IOrderedAggregateFluent<TDocument, TResult> source, Expression<Func<TResult, object>> field)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(field, "field");

            var helper = new BsonSerializationInfoHelper();
            helper.RegisterExpressionSerializer(field.Parameters[0], source.Collection.Settings.SerializerRegistry.GetSerializer<TResult>());
            var sortDocument = new SortByBuilder<TResult>(helper).Ascending(field).ToBsonDocument();

            // this looks sketchy, but if we get here and this isn't true, then
            // someone is being a bad citizen.
            var currentSortStage = (BsonDocument)source.Pipeline.Last();

            currentSortStage["$sort"].AsBsonDocument.AddRange(sortDocument);

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
        public static IOrderedAggregateFluent<TDocument, TResult> ThenByDescending<TDocument, TResult>(this IOrderedAggregateFluent<TDocument, TResult> source, Expression<Func<TResult, object>> field)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(field, "field");

            var helper = new BsonSerializationInfoHelper();
            helper.RegisterExpressionSerializer(field.Parameters[0], source.Collection.Settings.SerializerRegistry.GetSerializer<TResult>());
            var sortDocument = new SortByBuilder<TResult>(helper).Descending(field).ToBsonDocument();

            // this looks sketchy, but if we get here and this isn't true, then
            // someone is being a bad citizen.
            var currentSortStage = (BsonDocument)source.Pipeline.Last();

            currentSortStage["$sort"].AsBsonDocument.AddRange(sortDocument);

            return source;
        }

        /// <summary>
        /// Unwinds the specified source.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns></returns>
        public static IAggregateFluent<TDocument, BsonDocument> Unwind<TDocument, TResult>(this IAggregateFluent<TDocument, TResult> source, string fieldName)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(fieldName, "fieldName");

            return source.Unwind<BsonDocument>(fieldName);
        }

        /// <summary>
        /// Unwinds the specified source.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="field">The field.</param>
        /// <returns></returns>
        public static IAggregateFluent<TDocument, BsonDocument> Unwind<TDocument, TResult>(this IAggregateFluent<TDocument, TResult> source, Expression<Func<TResult, object>> field)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(field, "field");

            var helper = new BsonSerializationInfoHelper();
            helper.RegisterExpressionSerializer(field.Parameters[0], source.Collection.Settings.SerializerRegistry.GetSerializer<TResult>());
            var serialiationInfo = helper.GetSerializationInfo(field.Body);

            return source.Unwind<BsonDocument>("$" + serialiationInfo.ElementName);
        }

        /// <summary>
        /// Unwinds the specified source.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="field">The field.</param>
        /// <returns></returns>
        public static IAggregateFluent<TDocument, TNewResult> Unwind<TDocument, TResult, TNewResult>(this IAggregateFluent<TDocument, TResult> source, Expression<Func<TResult, object>> field)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(field, "field");

            var helper = new BsonSerializationInfoHelper();
            helper.RegisterExpressionSerializer(field.Parameters[0], source.Collection.Settings.SerializerRegistry.GetSerializer<TResult>());
            var serialiationInfo = helper.GetSerializationInfo(field.Body);

            return source.Unwind<TNewResult>("$" + serialiationInfo.ElementName);
        }

        /// <summary>
        /// Unwinds the specified source.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="field">The field.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        /// <returns></returns>
        public static IAggregateFluent<TDocument, TNewResult> Unwind<TDocument, TResult, TNewResult>(this IAggregateFluent<TDocument, TResult> source, Expression<Func<TResult, object>> field, IBsonSerializer<TNewResult> resultSerializer)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(field, "field");
            Ensure.IsNotNull(resultSerializer, "resultSerializer");

            var helper = new BsonSerializationInfoHelper();
            helper.RegisterExpressionSerializer(field.Parameters[0], source.Collection.Settings.SerializerRegistry.GetSerializer<TResult>());
            var serialiationInfo = helper.GetSerializationInfo(field.Body);

            return source.Unwind<TNewResult>("$" + serialiationInfo.ElementName, resultSerializer);
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
        public async static Task<TResult> FirstAsync<TDocument, TResult>(this IAggregateFluent<TDocument, TResult> source, CancellationToken cancellationToken = default(CancellationToken))
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
        /// <returns></returns>
        public async static Task<TResult> FirstOrDefaultAsync<TDocument, TResult>(this IAggregateFluent<TDocument, TResult> source, CancellationToken cancellationToken = default(CancellationToken))
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
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">The source sequence is empty.</exception>
        public async static Task<TResult> SingleAsync<TDocument, TResult>(this IAggregateFluent<TDocument, TResult> source, CancellationToken cancellationToken = default(CancellationToken))
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
        /// <returns></returns>
        public async static Task<TResult> SingleOrDefaultAsync<TDocument, TResult>(this IAggregateFluent<TDocument, TResult> source, CancellationToken cancellationToken = default(CancellationToken))
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
