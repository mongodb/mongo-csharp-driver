/* Copyright 2010-2015 MongoDB Inc.
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

            return aggregate.Group<TNewResult>(new GroupExpressionProjection<TResult, TKey, TNewResult>(id, group));
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
        /// <typeparam name="TForeignCollection">The type of the foreign collection.</typeparam>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="foreignCollection">The foreign collection.</param>
        /// <param name="localField">The local field.</param>
        /// <param name="foreignField">The foreign field.</param>
        /// <param name="as">The field in the result to place the foreign matches.</param>
        /// <param name="options">The options.</param>
        /// <returns>The fluent aggregate interface.</returns>
        public static IAggregateFluent<TNewResult> Lookup<TResult, TForeignCollection, TNewResult>(this IAggregateFluent<TResult> aggregate,
            IMongoCollection<TForeignCollection> foreignCollection,
            Expression<Func<TResult, object>> localField,
            Expression<Func<TForeignCollection, object>> foreignField,
            Expression<Func<TNewResult, object>> @as,
            AggregateLookupOptions<TForeignCollection, TNewResult> options = null)
        {
            Ensure.IsNotNull(aggregate, nameof(aggregate));
            Ensure.IsNotNull(foreignCollection, nameof(foreignCollection));
            Ensure.IsNotNull(localField, nameof(localField));
            Ensure.IsNotNull(foreignField, nameof(foreignField));
            Ensure.IsNotNull(@as, nameof(@as));

            options = options ?? new AggregateLookupOptions<TForeignCollection, TNewResult>();
            if (options.ForeignSerializer == null)
            {
                options.ForeignSerializer = foreignCollection.DocumentSerializer;
            }

            return aggregate.Lookup(
                foreignCollection.CollectionNamespace.CollectionName,
                new ExpressionFieldDefinition<TResult>(localField),
                new ExpressionFieldDefinition<TForeignCollection>(foreignField),
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

            return aggregate.Project<TNewResult>(new ProjectExpressionProjection<TResult, TNewResult>(projection));
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

            public ProjectExpressionProjection(Expression<Func<TResult, TNewResult>> expression)
            {
                _expression = Ensure.IsNotNull(expression, nameof(expression));
            }

            public Expression<Func<TResult, TNewResult>> Expression
            {
                get { return _expression; }
            }

            public override RenderedProjectionDefinition<TNewResult> Render(IBsonSerializer<TResult> documentSerializer, IBsonSerializerRegistry serializerRegistry)
            {
                return AggregateProjectTranslator.Translate<TResult, TNewResult>(_expression, documentSerializer, serializerRegistry);
            }
        }

        private sealed class GroupExpressionProjection<TResult, TKey, TNewResult> : ProjectionDefinition<TResult, TNewResult>
        {
            private readonly Expression<Func<TResult, TKey>> _idExpression;
            private readonly Expression<Func<IGrouping<TKey, TResult>, TNewResult>> _groupExpression;

            public GroupExpressionProjection(Expression<Func<TResult, TKey>> idExpression, Expression<Func<IGrouping<TKey, TResult>, TNewResult>> groupExpression)
            {
                _idExpression = Ensure.IsNotNull(idExpression, nameof(idExpression));
                _groupExpression = Ensure.IsNotNull(groupExpression, nameof(groupExpression));
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
                return AggregateGroupTranslator.Translate<TKey, TResult, TNewResult>(_idExpression, _groupExpression, documentSerializer, serializerRegistry);
            }
        }
    }
}
