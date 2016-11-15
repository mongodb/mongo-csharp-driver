/* Copyright 2010-2016 MongoDB Inc.
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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Translators;

namespace MongoDB.Driver
{
    /// <summary>
    /// Extension methods for <see cref="IAggregateFluent{TResult}"/>
    /// </summary>
    public static class IAggregateFluentExtensions
    {
        /// <summary>
        /// Appends a $bucket stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="groupBy">The expression providing the value to group by.</param>
        /// <param name="boundaries">The bucket boundaries.</param>
        /// <param name="options">The options.</param>
        /// <returns>The fluent aggregate interface.</returns>
        public static IAggregateFluent<AggregateBucketResult<TValue>> Bucket<TResult, TValue>(
            this IAggregateFluent<TResult> aggregate,
            Expression<Func<TResult, TValue>> groupBy,
            IEnumerable<TValue> boundaries,
            AggregateBucketOptions<TValue> options = null)
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));
            Ensure.IsNotNull(groupBy, nameof(groupBy));
            Ensure.IsNotNull(boundaries, nameof(boundaries));

            var groupByDefinition = new ExpressionAggregateExpressionDefinition<TResult, TValue>(groupBy, aggregate.Options.TranslationOptions);
            return aggregate.Bucket(groupByDefinition, boundaries, options);
        }

        /// <summary>
        /// Appends a $bucket stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="groupBy">The expression providing the value to group by.</param>
        /// <param name="boundaries">The bucket boundaries.</param>
        /// <param name="output">The output projection.</param>
        /// <param name="options">The options.</param>
        /// <returns>The fluent aggregate interface.</returns>
        public static IAggregateFluent<TNewResult> Bucket<TResult, TValue, TNewResult>(
            this IAggregateFluent<TResult> aggregate,
            Expression<Func<TResult, TValue>> groupBy,
            IEnumerable<TValue> boundaries,
            Expression<Func<IGrouping<TValue, TResult>, TNewResult>> output,
            AggregateBucketOptions<TValue> options = null)
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));
            Ensure.IsNotNull(groupBy, nameof(groupBy));
            Ensure.IsNotNull(boundaries, nameof(boundaries));
            Ensure.IsNotNull(output, nameof(output));

            var groupByDefinition = new ExpressionAggregateExpressionDefinition<TResult, TValue>(groupBy, aggregate.Options.TranslationOptions);
            var outputDefinition = new ExpressionBucketOutputProjection<TResult, TValue, TNewResult>(x => default(TValue), output, aggregate.Options.TranslationOptions);
            return aggregate.Bucket(groupByDefinition, boundaries, outputDefinition, options);
        }

        /// <summary>
        /// Appends a $bucketAuto stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="groupBy">The expression providing the value to group by.</param>
        /// <param name="buckets">The number of buckets.</param>
        /// <param name="options">The options (optional).</param>
        /// <returns>The fluent aggregate interface.</returns>
        public static IAggregateFluent<AggregateBucketAutoResult<TValue>> BucketAuto<TResult, TValue>(
            this IAggregateFluent<TResult> aggregate,
            Expression<Func<TResult, TValue>> groupBy,
            int buckets,
            AggregateBucketAutoOptions options = null)
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));
            Ensure.IsNotNull(groupBy, nameof(groupBy));

            var groupByDefinition = new ExpressionAggregateExpressionDefinition<TResult, TValue>(groupBy, aggregate.Options.TranslationOptions);
            return aggregate.BucketAuto(groupByDefinition, buckets, options);
        }

        /// <summary>
        /// Appends a $bucketAuto stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="groupBy">The expression providing the value to group by.</param>
        /// <param name="buckets">The number of buckets.</param>
        /// <param name="output">The output projection.</param>
        /// <param name="options">The options (optional).</param>
        /// <returns>The fluent aggregate interface.</returns>
        public static IAggregateFluent<TNewResult> BucketAuto<TResult, TValue, TNewResult>(
            this IAggregateFluent<TResult> aggregate,
            Expression<Func<TResult, TValue>> groupBy,
            int buckets,
            Expression<Func<IGrouping<TValue, TResult>, TNewResult>> output,
            AggregateBucketAutoOptions options = null)
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));
            Ensure.IsNotNull(groupBy, nameof(groupBy));
            Ensure.IsNotNull(output, nameof(output));

            var groupByDefinition = new ExpressionAggregateExpressionDefinition<TResult, TValue>(groupBy, aggregate.Options.TranslationOptions);
            var outputDefinition = new ExpressionBucketOutputProjection<TResult, TValue, TNewResult>(x => default(TValue), output, aggregate.Options.TranslationOptions);
            return aggregate.BucketAuto(groupByDefinition, buckets, outputDefinition, options);
        }

        /// <summary>
        /// Appends a $facet stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="facets">The facets.</param>
        /// <returns>The fluent aggregate interface.</returns>
        public static IAggregateFluent<AggregateFacetResults> Facet<TResult>(
            this IAggregateFluent<TResult> aggregate,
            IEnumerable<AggregateFacet<TResult>> facets)
        {
            var newResultSerializer = new AggregateFacetResultsSerializer(
                facets.Select(f => f.Name),
                facets.Select(f => f.OutputSerializer ?? BsonSerializer.SerializerRegistry.GetSerializer(f.OutputType)));
            var options = new AggregateFacetOptions<AggregateFacetResults> { NewResultSerializer = newResultSerializer };
            return aggregate.Facet(facets, options);
        }

        /// <summary>
        /// Appends a $facet stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="facets">The facets.</param>
        /// <returns>The fluent aggregate interface.</returns>
        public static IAggregateFluent<AggregateFacetResults> Facet<TResult>(
            this IAggregateFluent<TResult> aggregate,
            params AggregateFacet<TResult>[] facets)
        {
            return aggregate.Facet((IEnumerable<AggregateFacet<TResult>>)facets);
        }

        /// <summary>
        /// Appends a $facet stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="facets">The facets.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static IAggregateFluent<TNewResult> Facet<TResult, TNewResult>(
            this IAggregateFluent<TResult> aggregate,
            params AggregateFacet<TResult>[] facets)
        {
            return aggregate.Facet<TNewResult>((IEnumerable<AggregateFacet<TResult>>)facets);
        }
        
        /// <summary>
        /// Appends a $graphLookup stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <typeparam name="TNewResult">The type of the new result (must be same as TResult with an additional as field).</typeparam>
        /// <typeparam name="TFrom">The type of the from documents.</typeparam>
        /// <typeparam name="TConnect">The type of the connect field.</typeparam>
        /// <typeparam name="TConnectFrom">The type of the connect from field (must be either TConnect or a type that implements IEnumerable{TConnect}).</typeparam>
        /// <typeparam name="TStartWith">The type of the start with expression (must be either TConnect or a type that implements IEnumerable{TConnect}).</typeparam>
        /// <typeparam name="TAsEnumerable">The type of the enumerable as field.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="from">The from collection.</param>
        /// <param name="connectFromField">The connect from field.</param>
        /// <param name="connectToField">The connect to field.</param>
        /// <param name="startWith">The start with value.</param>
        /// <param name="as">The as field.</param>
        /// <param name="options">The options.</param>
        /// <returns>The fluent aggregate interface.</returns>
        public static IAggregateFluent<TNewResult> GraphLookup<TResult, TNewResult, TFrom, TConnect, TConnectFrom, TStartWith, TAsEnumerable>(
            this IAggregateFluent<TResult> aggregate,
            IMongoCollection<TFrom> from,
            FieldDefinition<TFrom, TConnectFrom> connectFromField,
            FieldDefinition<TFrom, TConnect> connectToField,
            AggregateExpressionDefinition<TResult, TStartWith> startWith,
            FieldDefinition<TNewResult, TAsEnumerable> @as,
            AggregateGraphLookupOptions<TNewResult, TFrom, TConnect, TConnectFrom, TStartWith, TFrom, TAsEnumerable> options = null)
                where TAsEnumerable : IEnumerable<TFrom>
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));
            Ensure.IsNotNull(from, nameof(from));
            Ensure.IsNotNull(connectFromField, nameof(connectFromField));
            Ensure.IsNotNull(connectToField, nameof(connectToField));
            Ensure.IsNotNull(startWith, nameof(startWith));
            Ensure.IsNotNull(@as, nameof(@as));
            var depthField = (FieldDefinition<TFrom, int>)null;
            return aggregate.GraphLookup(from, connectFromField, connectToField, startWith, @as, depthField, options);
        }

        /// <summary>
        /// Appends a $graphLookup stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <typeparam name="TFrom">The type of the from documents.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="from">The from collection.</param>
        /// <param name="connectFromField">The connect from field.</param>
        /// <param name="connectToField">The connect to field.</param>
        /// <param name="startWith">The start with value.</param>
        /// <param name="as">The as field.</param>
        /// <param name="depthField">The depth field.</param>
        /// <returns>The fluent aggregate interface.</returns>
        public static IAggregateFluent<BsonDocument> GraphLookup<TResult, TFrom>(
            this IAggregateFluent<TResult> aggregate,
            IMongoCollection<TFrom> from,
            FieldDefinition<TFrom, BsonValue> connectFromField,
            FieldDefinition<TFrom, BsonValue> connectToField,
            AggregateExpressionDefinition<TResult, BsonValue> startWith,
            FieldDefinition<BsonDocument, IEnumerable<BsonDocument>> @as,
            FieldDefinition<BsonDocument, int> depthField = null)
        {
            return aggregate.GraphLookup<BsonDocument, TFrom, BsonValue, BsonValue, BsonValue, BsonDocument, IEnumerable<BsonDocument>>(
                from, connectFromField, connectToField, startWith, @as, depthField, null);
        }

        /// <summary>
        /// Appends a $graphLookup stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <typeparam name="TNewResult">The type of the new result (must be same as TResult with an additional as field).</typeparam>
        /// <typeparam name="TFrom">The type of the from documents.</typeparam>
        /// <typeparam name="TConnect">The type of the connect field.</typeparam>
        /// <typeparam name="TConnectFrom">The type of the connect from field (must be either TConnect or a type that implements IEnumerable{TConnect}).</typeparam>
        /// <typeparam name="TStartWith">The type of the start with expression (must be either TConnect or a type that implements IEnumerable{TConnect}).</typeparam>
        /// <typeparam name="TAsEnumerable">The type of the enumerable as field.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="from">The from collection.</param>
        /// <param name="connectFromField">The connect from field.</param>
        /// <param name="connectToField">The connect to field.</param>
        /// <param name="startWith">The start with value.</param>
        /// <param name="as">The as field.</param>
        /// <param name="options">The options.</param>
        /// <returns>The fluent aggregate interface.</returns>
        public static IAggregateFluent<TNewResult> GraphLookup<TResult, TNewResult, TFrom, TConnect, TConnectFrom, TStartWith, TAsEnumerable>(
            this IAggregateFluent<TResult> aggregate,
            IMongoCollection<TFrom> from,
            Expression<Func<TFrom, TConnectFrom>> connectFromField,
            Expression<Func<TFrom, TConnect>> connectToField,
            Expression<Func<TResult, TStartWith>> startWith,
            Expression<Func<TNewResult, TAsEnumerable>> @as,
            AggregateGraphLookupOptions<TNewResult, TFrom, TConnect, TConnectFrom, TStartWith, TFrom, TAsEnumerable> options = null)
                where TAsEnumerable : IEnumerable<TFrom>
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));
            Ensure.IsNotNull(from, nameof(from));
            Ensure.IsNotNull(connectFromField, nameof(connectFromField));
            Ensure.IsNotNull(connectToField, nameof(connectToField));
            Ensure.IsNotNull(startWith, nameof(startWith));
            Ensure.IsNotNull(@as, nameof(@as));
            var connectFromFieldDefinition = new ExpressionFieldDefinition<TFrom, TConnectFrom>(connectFromField);
            var connectToFieldDefinition = new ExpressionFieldDefinition<TFrom, TConnect>(connectToField);
            var startWithDefinition = new ExpressionAggregateExpressionDefinition<TResult, TStartWith>(startWith, aggregate.Options.TranslationOptions);
            var asDefinition = new ExpressionFieldDefinition<TNewResult, TAsEnumerable>(@as);
            return aggregate.GraphLookup(from, connectFromFieldDefinition, connectToFieldDefinition, startWithDefinition, asDefinition, options);
        }

        /// <summary>
        /// Appends a $graphLookup stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <typeparam name="TNewResult">The type of the new result (must be same as TResult with an additional as field).</typeparam>
        /// <typeparam name="TFrom">The type of the from documents.</typeparam>
        /// <typeparam name="TConnect">The type of the connect field.</typeparam>
        /// <typeparam name="TConnectFrom">The type of the connect from field (must be either TConnect or a type that implements IEnumerable{TConnect}).</typeparam>
        /// <typeparam name="TStartWith">The type of the start with expression (must be either TConnect or a type that implements IEnumerable{TConnect}).</typeparam>
        /// <typeparam name="TAs">The type of the documents in the as field.</typeparam>
        /// <typeparam name="TAsEnumerable">The type of the enumerable as field.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="from">The from collection.</param>
        /// <param name="connectFromField">The connect from field.</param>
        /// <param name="connectToField">The connect to field.</param>
        /// <param name="startWith">The start with value.</param>
        /// <param name="as">The as field.</param>
        /// <param name="depthField">The depth field.</param>
        /// <param name="options">The options.</param>
        /// <returns>The fluent aggregate interface.</returns>
        public static IAggregateFluent<TNewResult> GraphLookup<TResult, TNewResult, TFrom, TConnect, TConnectFrom, TStartWith, TAs, TAsEnumerable>(
            this IAggregateFluent<TResult> aggregate,
            IMongoCollection<TFrom> from,
            Expression<Func<TFrom, TConnectFrom>> connectFromField,
            Expression<Func<TFrom, TConnect>> connectToField,
            Expression<Func<TResult, TStartWith>> startWith,
            Expression<Func<TNewResult, TAsEnumerable>> @as,
            Expression<Func<TAs, int>> depthField,
            AggregateGraphLookupOptions<TNewResult, TFrom, TConnect, TConnectFrom, TStartWith, TAs, TAsEnumerable> options = null)
                where TAsEnumerable : IEnumerable<TAs>
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));
            Ensure.IsNotNull(from, nameof(from));
            Ensure.IsNotNull(connectFromField, nameof(connectFromField));
            Ensure.IsNotNull(connectToField, nameof(connectToField));
            Ensure.IsNotNull(@as, nameof(@as));
            var connectFromFieldDefinition = new ExpressionFieldDefinition<TFrom, TConnectFrom>(connectFromField);
            var connectToFieldDefinition = new ExpressionFieldDefinition<TFrom, TConnect>(connectToField);
            var startWithDefinition = new ExpressionAggregateExpressionDefinition<TResult, TStartWith>(startWith, aggregate.Options.TranslationOptions);
            var asDefinition = new ExpressionFieldDefinition<TNewResult, TAsEnumerable>(@as);
            var depthFieldDefinition = depthField == null ? null : new ExpressionFieldDefinition<TAs, int>(depthField);
            return aggregate.GraphLookup(from, connectFromFieldDefinition, connectToFieldDefinition, startWithDefinition, asDefinition, depthFieldDefinition, options);
        }

        /// <summary>
        /// Appends a group stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="group">The group projection.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static IAggregateFluent<BsonDocument> Group<TResult>(this IAggregateFluent<TResult> aggregate, ProjectionDefinition<TResult, BsonDocument> group)
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));
            Ensure.IsNotNull(group, nameof(group));

            return aggregate.Group<BsonDocument>(group);
        }

        /// <summary>
        /// Appends a group stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="id">The id.</param>
        /// <param name="group">The group projection.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static IAggregateFluent<TNewResult> Group<TResult, TKey, TNewResult>(this IAggregateFluent<TResult> aggregate, Expression<Func<TResult, TKey>> id, Expression<Func<IGrouping<TKey, TResult>, TNewResult>> group)
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));
            Ensure.IsNotNull(id, nameof(id));
            Ensure.IsNotNull(group, nameof(group));

            return aggregate.Group<TNewResult>(new GroupExpressionProjection<TResult, TKey, TNewResult>(id, group, aggregate.Options.TranslationOptions));
        }

        /// <summary>
        /// Appends a lookup stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="foreignCollectionName">Name of the foreign collection.</param>
        /// <param name="localField">The local field.</param>
        /// <param name="foreignField">The foreign field.</param>
        /// <param name="as">The field in the result to place the foreign matches.</param>
        /// <returns>The fluent aggregate interface.</returns>
        public static IAggregateFluent<BsonDocument> Lookup<TResult>(this IAggregateFluent<TResult> aggregate,
            string foreignCollectionName,
            FieldDefinition<TResult> localField,
            FieldDefinition<BsonDocument> foreignField,
            FieldDefinition<BsonDocument> @as)
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));
            Ensure.IsNotNull(foreignCollectionName, nameof(foreignCollectionName));
            Ensure.IsNotNull(localField, nameof(localField));
            Ensure.IsNotNull(foreignField, nameof(foreignField));
            Ensure.IsNotNull(@as, nameof(@as));

            return aggregate.Lookup(
                foreignCollectionName,
                localField,
                foreignField,
                @as,
                new AggregateLookupOptions<BsonDocument, BsonDocument>());
        }

        /// <summary>
        /// Appends a lookup stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <typeparam name="TForeignDocument">The type of the foreign collection.</typeparam>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="foreignCollection">The foreign collection.</param>
        /// <param name="localField">The local field.</param>
        /// <param name="foreignField">The foreign field.</param>
        /// <param name="as">The field in the result to place the foreign matches.</param>
        /// <param name="options">The options.</param>
        /// <returns>The fluent aggregate interface.</returns>
        public static IAggregateFluent<TNewResult> Lookup<TResult, TForeignDocument, TNewResult>(this IAggregateFluent<TResult> aggregate,
            IMongoCollection<TForeignDocument> foreignCollection,
            Expression<Func<TResult, object>> localField,
            Expression<Func<TForeignDocument, object>> foreignField,
            Expression<Func<TNewResult, object>> @as,
            AggregateLookupOptions<TForeignDocument, TNewResult> options = null)
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));
            Ensure.IsNotNull(foreignCollection, nameof(foreignCollection));
            Ensure.IsNotNull(localField, nameof(localField));
            Ensure.IsNotNull(foreignField, nameof(foreignField));
            Ensure.IsNotNull(@as, nameof(@as));

            options = options ?? new AggregateLookupOptions<TForeignDocument, TNewResult>();
            if (options.ForeignSerializer == null)
            {
                options.ForeignSerializer = foreignCollection.DocumentSerializer;
            }

            return aggregate.Lookup(
                foreignCollection.CollectionNamespace.CollectionName,
                new ExpressionFieldDefinition<TResult>(localField),
                new ExpressionFieldDefinition<TForeignDocument>(foreignField),
                new ExpressionFieldDefinition<TNewResult>(@as),
                options);
        }

        /// <summary>
        /// Appends a match stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static IAggregateFluent<TResult> Match<TResult>(this IAggregateFluent<TResult> aggregate, Expression<Func<TResult, bool>> filter)
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));
            Ensure.IsNotNull(filter, nameof(filter));

            return aggregate.Match(new ExpressionFilterDefinition<TResult>(filter));
        }

        /// <summary>
        /// Appends a project stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="projection">The projection.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static IAggregateFluent<BsonDocument> Project<TResult>(this IAggregateFluent<TResult> aggregate, ProjectionDefinition<TResult, BsonDocument> projection)
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));
            Ensure.IsNotNull(projection, nameof(projection));

            return aggregate.Project<BsonDocument>(projection);
        }

        /// <summary>
        /// Appends a project stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="projection">The projection.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static IAggregateFluent<TNewResult> Project<TResult, TNewResult>(this IAggregateFluent<TResult> aggregate, Expression<Func<TResult, TNewResult>> projection)
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));
            Ensure.IsNotNull(projection, nameof(projection));

            return aggregate.Project<TNewResult>(new ProjectExpressionProjection<TResult, TNewResult>(projection, aggregate.Options.TranslationOptions));
        }

        /// <summary>
        /// Appends a $replaceRoot stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="newRoot">The new root.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static IAggregateFluent<TNewResult> ReplaceRoot<TResult, TNewResult>(
            this IAggregateFluent<TResult> aggregate,
            Expression<Func<TResult, TNewResult>> newRoot)
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));
            Ensure.IsNotNull(newRoot, nameof(newRoot));

            return aggregate.ReplaceRoot<TNewResult>(new ExpressionAggregateExpressionDefinition<TResult, TNewResult>(newRoot, aggregate.Options.TranslationOptions));
        }

        /// <summary>
        /// Appends an ascending sort stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="field">The field to sort by.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static IOrderedAggregateFluent<TResult> SortBy<TResult>(this IAggregateFluent<TResult> aggregate, Expression<Func<TResult, object>> field)
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));
            Ensure.IsNotNull(field, nameof(field));

            return (IOrderedAggregateFluent<TResult>)aggregate.Sort(
                new DirectionalSortDefinition<TResult>(new ExpressionFieldDefinition<TResult>(field), SortDirection.Ascending));
        }

        /// <summary>
        /// Appends a sortByCount stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="id">The id.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static IAggregateFluent<AggregateSortByCountResult<TKey>> SortByCount<TResult, TKey>(
            this IAggregateFluent<TResult> aggregate,
            Expression<Func<TResult, TKey>> id)
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));
            Ensure.IsNotNull(id, nameof(id));

            return aggregate.SortByCount<TKey>(new ExpressionAggregateExpressionDefinition<TResult, TKey>(id, aggregate.Options.TranslationOptions));
        }

        /// <summary>
        /// Appends a descending sort stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="field">The field to sort by.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static IOrderedAggregateFluent<TResult> SortByDescending<TResult>(this IAggregateFluent<TResult> aggregate, Expression<Func<TResult, object>> field)
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));
            Ensure.IsNotNull(field, nameof(field));

            return (IOrderedAggregateFluent<TResult>)aggregate.Sort(
                new DirectionalSortDefinition<TResult>(new ExpressionFieldDefinition<TResult>(field), SortDirection.Descending));
        }

        /// <summary>
        /// Modifies the current sort stage by appending an ascending field specification to it.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="field">The field to sort by.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static IOrderedAggregateFluent<TResult> ThenBy<TResult>(this IOrderedAggregateFluent<TResult> aggregate, Expression<Func<TResult, object>> field)
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));
            Ensure.IsNotNull(field, nameof(field));

            // this looks sketchy, but if we get here and this isn't true, then
            // someone is being a bad citizen.
            var lastStage = aggregate.Stages.Last();
            aggregate.Stages.RemoveAt(aggregate.Stages.Count - 1); // remove it so we can add it back

            var stage = new DelegatedPipelineStageDefinition<TResult, TResult>(
                "$sort",
                (s, sr) =>
                {
                    var lastSort = lastStage.Render(s, sr).Document["$sort"].AsBsonDocument;
                    var newSort = new DirectionalSortDefinition<TResult>(new ExpressionFieldDefinition<TResult>(field), SortDirection.Ascending).Render(s, sr);
                    return new RenderedPipelineStageDefinition<TResult>("$sort", new BsonDocument("$sort", lastSort.Merge(newSort)), s);
                });

            return (IOrderedAggregateFluent<TResult>)aggregate.AppendStage(stage);
        }

        /// <summary>
        /// Modifies the current sort stage by appending a descending field specification to it.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="field">The field to sort by.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static IOrderedAggregateFluent<TResult> ThenByDescending<TResult>(this IOrderedAggregateFluent<TResult> aggregate, Expression<Func<TResult, object>> field)
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));
            Ensure.IsNotNull(field, nameof(field));

            // this looks sketchy, but if we get here and this isn't true, then
            // someone is being a bad citizen.
            var lastStage = aggregate.Stages.Last();
            aggregate.Stages.RemoveAt(aggregate.Stages.Count - 1); // remove it so we can add it back

            var stage = new DelegatedPipelineStageDefinition<TResult, TResult>(
                "$sort",
                (s, sr) =>
                {
                    var lastSort = lastStage.Render(s, sr).Document["$sort"].AsBsonDocument;
                    var newSort = new DirectionalSortDefinition<TResult>(new ExpressionFieldDefinition<TResult>(field), SortDirection.Descending).Render(s, sr);
                    return new RenderedPipelineStageDefinition<TResult>("$sort", new BsonDocument("$sort", lastSort.Merge(newSort)), s);
                });

            return (IOrderedAggregateFluent<TResult>)aggregate.AppendStage(stage);
        }

        /// <summary>
        /// Appends an unwind stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="field">The field to unwind.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static IAggregateFluent<BsonDocument> Unwind<TResult>(this IAggregateFluent<TResult> aggregate, FieldDefinition<TResult> field)
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));
            Ensure.IsNotNull(field, nameof(field));

            return aggregate.Unwind(
                field,
                new AggregateUnwindOptions<BsonDocument>());
        }

        /// <summary>
        /// Appends an unwind stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="field">The field to unwind.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static IAggregateFluent<BsonDocument> Unwind<TResult>(this IAggregateFluent<TResult> aggregate, Expression<Func<TResult, object>> field)
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));
            Ensure.IsNotNull(field, nameof(field));

            return aggregate.Unwind(
                new ExpressionFieldDefinition<TResult>(field),
                new AggregateUnwindOptions<BsonDocument>());
        }

        /// <summary>
        /// Appends an unwind stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="field">The field to unwind.</param>
        /// <param name="newResultSerializer">The new result serializer.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        [Obsolete("Use the Unwind overload which takes an options parameter.")]
        public static IAggregateFluent<TNewResult> Unwind<TResult, TNewResult>(this IAggregateFluent<TResult> aggregate, Expression<Func<TResult, object>> field, IBsonSerializer<TNewResult> newResultSerializer)
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));
            Ensure.IsNotNull(field, nameof(field));

            return aggregate.Unwind(
                new ExpressionFieldDefinition<TResult>(field),
                new AggregateUnwindOptions<TNewResult> { ResultSerializer = newResultSerializer });
        }

        /// <summary>
        /// Appends an unwind stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="field">The field to unwind.</param>
        /// <param name="options">The options.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static IAggregateFluent<TNewResult> Unwind<TResult, TNewResult>(this IAggregateFluent<TResult> aggregate, Expression<Func<TResult, object>> field, AggregateUnwindOptions<TNewResult> options = null)
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));
            Ensure.IsNotNull(field, nameof(field));

            return aggregate.Unwind(
                new ExpressionFieldDefinition<TResult>(field),
                options);
        }

        /// <summary>
        /// Returns the first document of the aggregate result.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static TResult First<TResult>(this IAggregateFluent<TResult> aggregate, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));

            return IAsyncCursorSourceExtensions.First(aggregate.Limit(1), cancellationToken);
        }

        /// <summary>
        /// Returns the first document of the aggregate result.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static Task<TResult> FirstAsync<TResult>(this IAggregateFluent<TResult> aggregate, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));

            return IAsyncCursorSourceExtensions.FirstAsync(aggregate.Limit(1), cancellationToken);
        }

        /// <summary>
        /// Returns the first document of the aggregate result, or the default value if the result set is empty.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static TResult FirstOrDefault<TResult>(this IAggregateFluent<TResult> aggregate, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));

            return IAsyncCursorSourceExtensions.FirstOrDefault(aggregate.Limit(1), cancellationToken);
        }

        /// <summary>
        /// Returns the first document of the aggregate result, or the default value if the result set is empty.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static Task<TResult> FirstOrDefaultAsync<TResult>(this IAggregateFluent<TResult> aggregate, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));

            return IAsyncCursorSourceExtensions.FirstOrDefaultAsync(aggregate.Limit(1), cancellationToken);
        }

        /// <summary>
        /// Returns the only document of the aggregate result. Throws an exception if the result set does not contain exactly one document.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static TResult Single<TResult>(this IAggregateFluent<TResult> aggregate, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));

            return IAsyncCursorSourceExtensions.Single(aggregate.Limit(2), cancellationToken);
        }

        /// <summary>
        /// Returns the only document of the aggregate result. Throws an exception if the result set does not contain exactly one document.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static Task<TResult> SingleAsync<TResult>(this IAggregateFluent<TResult> aggregate, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));

            return IAsyncCursorSourceExtensions.SingleAsync(aggregate.Limit(2), cancellationToken);
        }

        /// <summary>
        /// Returns the only document of the aggregate result, or the default value if the result set is empty. Throws an exception if the result set contains more than one document.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static TResult SingleOrDefault<TResult>(this IAggregateFluent<TResult> aggregate, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));

            return IAsyncCursorSourceExtensions.SingleOrDefault(aggregate.Limit(2), cancellationToken);
        }

        /// <summary>
        /// Returns the only document of the aggregate result, or the default value if the result set is empty. Throws an exception if the result set contains more than one document.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static Task<TResult> SingleOrDefaultAsync<TResult>(this IAggregateFluent<TResult> aggregate, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));

            return IAsyncCursorSourceExtensions.SingleOrDefaultAsync(aggregate.Limit(2), cancellationToken);
        }

        private sealed class ProjectExpressionProjection<TResult, TNewResult> : ProjectionDefinition<TResult, TNewResult>
        {
            private readonly Expression<Func<TResult, TNewResult>> _expression;
            private readonly ExpressionTranslationOptions _translationOptions;

            public ProjectExpressionProjection(Expression<Func<TResult, TNewResult>> expression, ExpressionTranslationOptions translationOptions)
            {
                _expression = Ensure.IsNotNull(expression, nameof(expression));
                _translationOptions = translationOptions; // can be null
            }

            public Expression<Func<TResult, TNewResult>> Expression
            {
                get { return _expression; }
            }

            public override RenderedProjectionDefinition<TNewResult> Render(IBsonSerializer<TResult> documentSerializer, IBsonSerializerRegistry serializerRegistry)
            {
                return AggregateProjectTranslator.Translate<TResult, TNewResult>(_expression, documentSerializer, serializerRegistry, _translationOptions);
            }
        }

        private sealed class GroupExpressionProjection<TResult, TKey, TNewResult> : ProjectionDefinition<TResult, TNewResult>
        {
            private readonly Expression<Func<TResult, TKey>> _idExpression;
            private readonly Expression<Func<IGrouping<TKey, TResult>, TNewResult>> _groupExpression;
            private readonly ExpressionTranslationOptions _translationOptions;

            public GroupExpressionProjection(Expression<Func<TResult, TKey>> idExpression, Expression<Func<IGrouping<TKey, TResult>, TNewResult>> groupExpression, ExpressionTranslationOptions translationOptions)
            {
                _idExpression = Ensure.IsNotNull(idExpression, nameof(idExpression));
                _groupExpression = Ensure.IsNotNull(groupExpression, nameof(groupExpression));
                _translationOptions = translationOptions; // can be null
            }

            public Expression<Func<TResult, TKey>> IdExpression
            {
                get { return _idExpression; }
            }

            public Expression<Func<IGrouping<TKey, TResult>, TNewResult>> GroupExpression
            {
                get { return _groupExpression; }
            }

            public override RenderedProjectionDefinition<TNewResult> Render(IBsonSerializer<TResult> documentSerializer, IBsonSerializerRegistry serializerRegistry)
            {
                return AggregateGroupTranslator.Translate<TKey, TResult, TNewResult>(_idExpression, _groupExpression, documentSerializer, serializerRegistry, _translationOptions);
            }
        }

        private sealed class ExpressionBucketOutputProjection<TResult, TValue, TNewResult> : ProjectionDefinition<TResult, TNewResult>
        {
            private readonly Expression<Func<IGrouping<TValue, TResult>, TNewResult>> _outputExpression;
            private readonly ExpressionTranslationOptions _translationOptions;
            private readonly Expression<Func<TResult, TValue>> _valueExpression;

            public ExpressionBucketOutputProjection(
                Expression<Func<TResult, TValue>> valueExpression,
                Expression<Func<IGrouping<TValue, TResult>, TNewResult>> outputExpression,
                ExpressionTranslationOptions translationOptions)
            {
                _valueExpression = Ensure.IsNotNull(valueExpression, nameof(valueExpression));
                _outputExpression = Ensure.IsNotNull(outputExpression, nameof(outputExpression));
                _translationOptions = translationOptions; // can be null

            }

            public Expression<Func<IGrouping<TValue, TResult>, TNewResult>> OutputExpression
            {
                get { return _outputExpression; }
            }

            public override RenderedProjectionDefinition<TNewResult> Render(IBsonSerializer<TResult> documentSerializer, IBsonSerializerRegistry serializerRegistry)
            {
                var renderedOutput = AggregateGroupTranslator.Translate<TValue, TResult, TNewResult>(_valueExpression, _outputExpression, documentSerializer, serializerRegistry, _translationOptions);
                var document = renderedOutput.Document;
                document.Remove("_id");
                return new RenderedProjectionDefinition<TNewResult>(document, renderedOutput.ProjectionSerializer);
            }
        }
    }
}
