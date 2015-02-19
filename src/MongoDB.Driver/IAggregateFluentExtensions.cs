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
    /// Extension methods for <see cref="IAggregateFluent{TDocument}"/>
    /// </summary>
    public static class IAggregateFluentExtensions
    {
        /// <summary>
        /// Appends a group stage to the pipeline.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="group">The group expressions.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static IAggregateFluent<BsonDocument> Group<TDocument>(this IAggregateFluent<TDocument> source, Projection<TDocument, BsonDocument> group)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(group, "group");

            return source.Group<BsonDocument>(group);
        }

        /// <summary>
        /// Appends a group stage to the pipeline.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the new result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="idProjector">The identifier projector.</param>
        /// <param name="groupProjector">The group projector.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static IAggregateFluent<TResult> Group<TKey, TDocument, TResult>(this IAggregateFluent<TDocument> source, Expression<Func<TDocument, TKey>> idProjector, Expression<Func<IGrouping<TKey, TDocument>, TResult>> groupProjector)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(idProjector, "idProjector");
            Ensure.IsNotNull(groupProjector, "groupProjector");

            return source.Group<TResult>(new GroupExpressionProjection<TKey, TDocument, TResult>(idProjector, groupProjector));
        }

        /// <summary>
        /// Appends a match stage to the pipeline.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static IAggregateFluent<TDocument> Match<TDocument>(this IAggregateFluent<TDocument> source, Expression<Func<TDocument, bool>> filter)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(filter, "filter");

            return source.Match(new ExpressionFilter<TDocument>(filter));
        }

        /// <summary>
        /// Appends a project stage to the pipeline.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="project">The project specifications.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static IAggregateFluent<BsonDocument> Project<TDocument>(this IAggregateFluent<TDocument> source, Projection<TDocument, BsonDocument> project)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(project, "project");

            return source.Project<BsonDocument>(project);
        }

        /// <summary>
        /// Appends a project stage to the pipeline.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the new result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="project">The project specifications.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static IAggregateFluent<TResult> Project<TDocument, TResult>(this IAggregateFluent<TDocument> source, Expression<Func<TDocument, TResult>> project)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(project, "projector");

            return source.Project<TResult>(new ProjectExpressionProjection<TDocument, TResult>(project));
        }

        /// <summary>
        /// Appends an ascending sort stage to the pipeline.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="field">The field to sort by.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static IOrderedAggregateFluent<TDocument> SortBy<TDocument>(this IAggregateFluent<TDocument> source, Expression<Func<TDocument, object>> field)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(field, "field");

            return (IOrderedAggregateFluent<TDocument>)source.Sort(new ExpressionSort<TDocument>(field, ExpressionSort<TDocument>.Direction.Ascending));
        }

        /// <summary>
        /// Appends a descending sort stage to the pipeline.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="field">The field to sort by.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static IOrderedAggregateFluent<TDocument> SortByDescending<TDocument>(this IAggregateFluent<TDocument> source, Expression<Func<TDocument, object>> field)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(field, "field");

            return (IOrderedAggregateFluent<TDocument>)source.Sort(new ExpressionSort<TDocument>(field, ExpressionSort<TDocument>.Direction.Descending));
        }

        /// <summary>
        /// Modifies the current sort stage by appending an ascending field specification to it.
        /// </summary>
        /// <typeparam name="TDocument">The type of the result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="field">The field to sort by.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static IOrderedAggregateFluent<TDocument> ThenBy<TDocument>(this IOrderedAggregateFluent<TDocument> source, Expression<Func<TDocument, object>> field)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(field, "field");

            // this looks sketchy, but if we get here and this isn't true, then
            // someone is being a bad citizen.
            var currentSort = new BsonDocumentSort<TDocument>(source.Pipeline.Last()["$sort"].AsBsonDocument);
            source.Pipeline.RemoveAt(source.Pipeline.Count - 1); // remove it so we can add it back

            return (IOrderedAggregateFluent<TDocument>)source.Sort(new PairSort<TDocument>(
                currentSort,
                new ExpressionSort<TDocument>(field, ExpressionSort<TDocument>.Direction.Ascending)));
        }

        /// <summary>
        /// Modifies the current sort stage by appending a descending field specification to it.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="field">The field to sort by.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static IOrderedAggregateFluent<TDocument> ThenByDescending<TDocument>(this IOrderedAggregateFluent<TDocument> source, Expression<Func<TDocument, object>> field)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(field, "field");

            // this looks sketchy, but if we get here and this isn't true, then
            // someone is being a bad citizen.
            var currentSort = new BsonDocumentSort<TDocument>(source.Pipeline.Last()["$sort"].AsBsonDocument);
            source.Pipeline.RemoveAt(source.Pipeline.Count - 1); // remove it so we can add it back

            return (IOrderedAggregateFluent<TDocument>)source.Sort(new PairSort<TDocument>(
                currentSort,
                new ExpressionSort<TDocument>(field, ExpressionSort<TDocument>.Direction.Descending)));
        }

        /// <summary>
        /// Appends an unwind stage to the pipeline.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="fieldName">The name of the field to unwind.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static IAggregateFluent<BsonDocument> Unwind<TDocument>(this IAggregateFluent<TDocument> source, FieldName<TDocument> fieldName)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(fieldName, "fieldName");

            return source.Unwind<BsonDocument>(fieldName);
        }

        /// <summary>
        /// Appends an unwind stage to the pipeline.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="fieldName">The field to unwind.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static IAggregateFluent<BsonDocument> Unwind<TDocument>(this IAggregateFluent<TDocument> source, Expression<Func<TDocument, object>> fieldName)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(fieldName, "field");

            return source.Unwind<BsonDocument>(new ExpressionFieldName<TDocument>(fieldName));
        }

        /// <summary>
        /// Appends an unwind stage to the pipeline.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the new result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="fieldName">The field to unwind.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public static IAggregateFluent<TResult> Unwind<TDocument, TResult>(this IAggregateFluent<TDocument> source, Expression<Func<TDocument, object>> fieldName, IBsonSerializer<TResult> resultSerializer = null)
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(fieldName, "field");
            Ensure.IsNotNull(resultSerializer, "resultSerializer");

            return source.Unwind<TResult>(new ExpressionFieldName<TDocument>(fieldName), resultSerializer);
        }

        /// <summary>
        /// Returns the first document of the aggregate result.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">The source sequence is empty.</exception>
        public async static Task<TDocument> FirstAsync<TDocument>(this IAggregateFluent<TDocument> source, CancellationToken cancellationToken = default(CancellationToken))
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
        /// Returns the first document of the aggregate result, or the default value if the result set is empty.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public async static Task<TDocument> FirstOrDefaultAsync<TDocument>(this IAggregateFluent<TDocument> source, CancellationToken cancellationToken = default(CancellationToken))
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
                    return default(TDocument);
                }
            }
        }

        /// <summary>
        /// Returns the only document of the aggregate result. Throws an exception if the result set does not contain exactly one document.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">The source sequence is empty.</exception>
        public async static Task<TDocument> SingleAsync<TDocument>(this IAggregateFluent<TDocument> source, CancellationToken cancellationToken = default(CancellationToken))
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
        /// Returns the only document of the aggregate result, or the default value if the result set is empty. Throws an exception if the result set contains more than one document.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The fluent aggregate interface.
        /// </returns>
        public async static Task<TDocument> SingleOrDefaultAsync<TDocument>(this IAggregateFluent<TDocument> source, CancellationToken cancellationToken = default(CancellationToken))
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
                    return default(TDocument);
                }
            }
        }

        private sealed class ProjectExpressionProjection<TDocument, TResult> : Projection<TDocument, TResult>
        {
            private readonly Expression<Func<TDocument, TResult>> _expression;

            public ProjectExpressionProjection(Expression<Func<TDocument, TResult>> expression)
            {
                _expression = Ensure.IsNotNull(expression, "expression");
            }

            public Expression<Func<TDocument, TResult>> Expression
            {
                get { return _expression; }
            }

            public override RenderedProjection<TResult> Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
            {
                return AggregateProjectionTranslator.TranslateProject<TDocument, TResult>(_expression, documentSerializer, serializerRegistry);
            }
        }

        private sealed class GroupExpressionProjection<TKey, TDocument, TResult> : Projection<TDocument, TResult>
        {
            private readonly Expression<Func<TDocument, TKey>> _idExpression;
            private readonly Expression<Func<IGrouping<TKey, TDocument>, TResult>> _groupExpression;

            public GroupExpressionProjection(Expression<Func<TDocument, TKey>> idExpression, Expression<Func<IGrouping<TKey, TDocument>, TResult>> groupExpression)
            {
                _idExpression = Ensure.IsNotNull(idExpression, "idExpression");
                _groupExpression = Ensure.IsNotNull(groupExpression, "groupExpression");
            }

            public Expression<Func<TDocument, TKey>> IdExpression
            {
                get { return _idExpression; }
            }

            public Expression<Func<IGrouping<TKey, TDocument>, TResult>> GroupExpression
            {
                get { return _groupExpression; }
            }

            public override RenderedProjection<TResult> Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
            {
                return AggregateProjectionTranslator.TranslateGroup<TKey, TDocument, TResult>(_idExpression, _groupExpression, documentSerializer, serializerRegistry);
            }
        }
    }
}
