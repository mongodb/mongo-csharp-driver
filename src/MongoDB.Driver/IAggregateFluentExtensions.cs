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
        public static IAggregateFluent<BsonDocument> Group<TResult>(this IAggregateFluent<TResult> aggregate, Projection<TResult, BsonDocument> group)
        {
            Ensure.IsNotNull(aggregate, "aggregate");
            Ensure.IsNotNull(group, "group");

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
            Ensure.IsNotNull(aggregate, "aggregate");
            Ensure.IsNotNull(id, "id");
            Ensure.IsNotNull(group, "group");

            return aggregate.Group<TNewResult>(new GroupExpressionProjection<TResult, TKey, TNewResult>(id, group));
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
            Ensure.IsNotNull(aggregate, "aggregate");
            Ensure.IsNotNull(filter, "filter");

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
        public static IAggregateFluent<BsonDocument> Project<TResult>(this IAggregateFluent<TResult> aggregate, Projection<TResult, BsonDocument> projection)
        {
            Ensure.IsNotNull(aggregate, "aggregate");
            Ensure.IsNotNull(projection, "projection");

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
            Ensure.IsNotNull(aggregate, "aggregate");
            Ensure.IsNotNull(projection, "projection");

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
            Ensure.IsNotNull(aggregate, "aggregate");
            Ensure.IsNotNull(field, "field");

            return (IOrderedAggregateFluent<TResult>)aggregate.Sort(
                new DirectionalSort<TResult>(new ExpressionFieldName<TResult>(field), SortDirection.Ascending));
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
            Ensure.IsNotNull(aggregate, "aggregate");
            Ensure.IsNotNull(field, "field");

            return (IOrderedAggregateFluent<TResult>)aggregate.Sort(
                new DirectionalSort<TResult>(new ExpressionFieldName<TResult>(field), SortDirection.Descending));
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
            Ensure.IsNotNull(aggregate, "aggregate");
            Ensure.IsNotNull(field, "field");

            // this looks sketchy, but if we get here and this isn't true, then
            // someone is being a bad citizen.
            var lastStage = aggregate.Stages.Last();
            aggregate.Stages.RemoveAt(aggregate.Stages.Count - 1); // remove it so we can add it back

            var stage = new DelegatedPipelineStageDefinition<TResult, TResult>(
                "$sort",
                (s, sr) =>
                {
                    var lastSort = lastStage.Render(s, sr).Document["$sort"].AsBsonDocument;
                    var newSort = new DirectionalSort<TResult>(new ExpressionFieldName<TResult>(field), SortDirection.Ascending).Render(s, sr);
                    return new RenderedPipelineStage<TResult>("$sort", new BsonDocument("$sort", lastSort.Merge(newSort)), s);
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
            Ensure.IsNotNull(aggregate, "aggregate");
            Ensure.IsNotNull(field, "field");

            // this looks sketchy, but if we get here and this isn't true, then
            // someone is being a bad citizen.
            var lastStage = aggregate.Stages.Last();
            aggregate.Stages.RemoveAt(aggregate.Stages.Count - 1); // remove it so we can add it back

            var stage = new DelegatedPipelineStageDefinition<TResult, TResult>(
                "$sort",
                (s, sr) =>
                {
                    var lastSort = lastStage.Render(s, sr).Document["$sort"].AsBsonDocument;
                    var newSort = new DirectionalSort<TResult>(new ExpressionFieldName<TResult>(field), SortDirection.Descending).Render(s, sr);
                    return new RenderedPipelineStage<TResult>("$sort", new BsonDocument("$sort", lastSort.Merge(newSort)), s);
                });

            return (IOrderedAggregateFluent<TResult>)aggregate.AppendStage(stage);
        }

        /// <summary>
        /// Appends an unwind stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="fieldName">The name of the field to unwind.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static IAggregateFluent<BsonDocument> Unwind<TResult>(this IAggregateFluent<TResult> aggregate, FieldName<TResult> fieldName)
        {
            Ensure.IsNotNull(aggregate, "aggregate");
            Ensure.IsNotNull(fieldName, "fieldName");

            return aggregate.Unwind<BsonDocument>(fieldName);
        }

        /// <summary>
        /// Appends an unwind stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="fieldName">The field to unwind.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static IAggregateFluent<BsonDocument> Unwind<TResult>(this IAggregateFluent<TResult> aggregate, Expression<Func<TResult, object>> fieldName)
        {
            Ensure.IsNotNull(aggregate, "aggregate");
            Ensure.IsNotNull(fieldName, "field");

            return aggregate.Unwind<BsonDocument>(new ExpressionFieldName<TResult>(fieldName));
        }

        /// <summary>
        /// Appends an unwind stage to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="fieldName">The field to unwind.</param>
        /// <param name="newResultSerializer">The new result serializer.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static IAggregateFluent<TNewResult> Unwind<TResult, TNewResult>(this IAggregateFluent<TResult> aggregate, Expression<Func<TResult, object>> fieldName, IBsonSerializer<TNewResult> newResultSerializer = null)
        {
            Ensure.IsNotNull(aggregate, "aggregate");
            Ensure.IsNotNull(fieldName, "field");
            Ensure.IsNotNull(newResultSerializer, "newResultSerializer");

            return aggregate.Unwind<TNewResult>(new ExpressionFieldName<TResult>(fieldName), newResultSerializer);
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
        /// <exception cref="System.InvalidOperationException">The aggregate sequence is empty.</exception>
        public async static Task<TResult> FirstAsync<TResult>(this IAggregateFluent<TResult> aggregate, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(aggregate, "aggregate");

            using (var cursor = await aggregate.Limit(1).ToCursorAsync(cancellationToken).ConfigureAwait(false))
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
        /// Returns the first document of the aggregate result, or the default value if the result set is empty.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public async static Task<TResult> FirstOrDefaultAsync<TResult>(this IAggregateFluent<TResult> aggregate, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(aggregate, "aggregate");

            using (var cursor = await aggregate.Limit(1).ToCursorAsync(cancellationToken).ConfigureAwait(false))
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
        /// Returns the only document of the aggregate result. Throws an exception if the result set does not contain exactly one document.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">The aggregate sequence is empty.</exception>
        public async static Task<TResult> SingleAsync<TResult>(this IAggregateFluent<TResult> aggregate, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(aggregate, "aggregate");

            using (var cursor = await aggregate.Limit(2).ToCursorAsync(cancellationToken).ConfigureAwait(false))
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
        /// Returns the only document of the aggregate result, or the default value if the result set is empty. Throws an exception if the result set contains more than one document.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public async static Task<TResult> SingleOrDefaultAsync<TResult>(this IAggregateFluent<TResult> aggregate, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(aggregate, "aggregate");

            using (var cursor = await aggregate.Limit(2).ToCursorAsync(cancellationToken).ConfigureAwait(false))
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

        private sealed class ProjectExpressionProjection<TResult, TNewResult> : Projection<TResult, TNewResult>
        {
            private readonly Expression<Func<TResult, TNewResult>> _expression;

            public ProjectExpressionProjection(Expression<Func<TResult, TNewResult>> expression)
            {
                _expression = Ensure.IsNotNull(expression, "expression");
            }

            public Expression<Func<TResult, TNewResult>> Expression
            {
                get { return _expression; }
            }

            public override RenderedProjection<TNewResult> Render(IBsonSerializer<TResult> documentSerializer, IBsonSerializerRegistry serializerRegistry)
            {
                return AggregateProjectionTranslator.TranslateProject<TResult, TNewResult>(_expression, documentSerializer, serializerRegistry);
            }
        }

        private sealed class GroupExpressionProjection<TResult, TKey, TNewResult> : Projection<TResult, TNewResult>
        {
            private readonly Expression<Func<TResult, TKey>> _idExpression;
            private readonly Expression<Func<IGrouping<TKey, TResult>, TNewResult>> _groupExpression;

            public GroupExpressionProjection(Expression<Func<TResult, TKey>> idExpression, Expression<Func<IGrouping<TKey, TResult>, TNewResult>> groupExpression)
            {
                _idExpression = Ensure.IsNotNull(idExpression, "idExpression");
                _groupExpression = Ensure.IsNotNull(groupExpression, "groupExpression");
            }

            public Expression<Func<TResult, TKey>> IdExpression
            {
                get { return _idExpression; }
            }

            public Expression<Func<IGrouping<TKey, TResult>, TNewResult>> GroupExpression
            {
                get { return _groupExpression; }
            }

            public override RenderedProjection<TNewResult> Render(IBsonSerializer<TResult> documentSerializer, IBsonSerializerRegistry serializerRegistry)
            {
                return AggregateProjectionTranslator.TranslateGroup<TKey, TResult, TNewResult>(_idExpression, _groupExpression, documentSerializer, serializerRegistry);
            }
        }
    }
}
