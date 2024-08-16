/* Copyright 2015-present MongoDB Inc.
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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Search;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// LINQ extension methods for <see cref="IQueryable" />.
    /// </summary>
    public static class MongoQueryable
    {
        /// <summary>
        /// Determines whether a sequence contains any elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence to check for being empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// true if the source sequence contains any elements; otherwise, false.
        /// </returns>
        public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<bool>(
                Expression.Call(
                    GetMethodInfo(Queryable.Any, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Determines whether any element of a sequence satisfies a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence whose elements to test for a condition.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// true if any elements in the source sequence pass the test in the specified predicate; otherwise, false.
        /// </returns>
        public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(predicate, nameof(predicate));

            return source.GetMongoQueryProvider().ExecuteAsync<bool>(
                Expression.Call(
                    GetMethodInfo(Queryable.Any, source, predicate),
                    source.Expression,
                    Expression.Quote(predicate)),
                cancellationToken);
        }

        /// <summary>
        /// Appends an arbitrary stage to the LINQ pipeline.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TResult">The type of the result values returned by the appended stage.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="stage">The stage to append.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        /// <returns>The queryable with a new stage appended.</returns>
        public static IQueryable<TResult> AppendStage<TSource, TResult>(
            this IQueryable<TSource> source,
            PipelineStageDefinition<TSource, TResult> stage,
            IBsonSerializer<TResult> resultSerializer = null)
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(stage, nameof(stage));

            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    GetMethodInfo(AppendStage, source, stage, resultSerializer),
                    source.Expression,
                    Expression.Constant(stage),
                    Expression.Constant(resultSerializer, typeof(IBsonSerializer<TResult>))));
        }

        /// <summary>
        /// Allows the results to be interpreted as a different type. It is up to the caller
        /// to determine that the new result type is compatible with the actual results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TResult">The new result type for the results.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="resultSerializer">The new serializer (optional, will be looked up if null).</param>
        /// <returns>
        /// A new IQueryable with a new result type.
        /// </returns>
        public static IQueryable<TResult> As<TSource, TResult>(
            this IQueryable<TSource> source,
            IBsonSerializer<TResult> resultSerializer = null)
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    GetMethodInfo(As, source, resultSerializer),
                    source.Expression,
                    Expression.Constant(resultSerializer, typeof(IBsonSerializer<TResult>))));
        }

        /// <summary>
        /// Computes the average of a sequence of <see cref="Decimal"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The average of the values in the sequence.</returns>
        public static Task<decimal> AverageAsync(this IQueryable<decimal> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<decimal>(
                Expression.Call(
                    GetMethodInfo(Queryable.Average, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the average of a sequence of <see cref="Nullable{Decimal}"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The average of the values in the sequence.</returns>
        public static Task<decimal?> AverageAsync(this IQueryable<decimal?> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<decimal?>(
                Expression.Call(
                    GetMethodInfo(Queryable.Average, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the average of a sequence of <see cref="Double"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The average of the values in the sequence.</returns>
        public static Task<double> AverageAsync(this IQueryable<double> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<double>(
                Expression.Call(
                    GetMethodInfo(Queryable.Average, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the average of a sequence of <see cref="Nullable{Double}"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The average of the values in the sequence.</returns>
        public static Task<double?> AverageAsync(this IQueryable<double?> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<double?>(
                Expression.Call(
                    GetMethodInfo(Queryable.Average, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the average of a sequence of <see cref="Single"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The average of the values in the sequence.</returns>
        public static Task<float> AverageAsync(this IQueryable<float> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<float>(
                Expression.Call(
                    GetMethodInfo(Queryable.Average, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the average of a sequence of <see cref="Nullable{Single}"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The average of the values in the sequence.</returns>
        public static Task<float?> AverageAsync(this IQueryable<float?> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<float?>(
                Expression.Call(
                    GetMethodInfo(Queryable.Average, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the average of a sequence of <see cref="Int32"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The average of the values in the sequence.</returns>
        public static Task<double> AverageAsync(this IQueryable<int> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<double>(
                Expression.Call(
                    GetMethodInfo(Queryable.Average, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the average of a sequence of <see cref="Nullable{Int32}"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The average of the values in the sequence.</returns>
        public static Task<double?> AverageAsync(this IQueryable<int?> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<double?>(
                Expression.Call(
                    GetMethodInfo(Queryable.Average, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the average of a sequence of <see cref="Int64"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The average of the values in the sequence.</returns>
        public static Task<double> AverageAsync(this IQueryable<long> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<double>(
                Expression.Call(
                    GetMethodInfo(Queryable.Average, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the average of a sequence of <see cref="Nullable{Int64}"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The average of the values in the sequence.</returns>
        public static Task<double?> AverageAsync(this IQueryable<long?> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<double?>(
                Expression.Call(
                    GetMethodInfo(Queryable.Average, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the average of the sequence of <see cref="Decimal" /> values that is obtained
        /// by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The average of the projected values.
        /// </returns>
        public static Task<decimal> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<decimal>(
                Expression.Call(
                    GetMethodInfo(Queryable.Average, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the average of the sequence of <see cref="Nullable{Decimal}" /> values that is obtained
        /// by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The average of the projected values.
        /// </returns>
        public static Task<decimal?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<decimal?>(
                Expression.Call(
                    GetMethodInfo(Queryable.Average, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the average of the sequence of <see cref="Double" /> values that is obtained
        /// by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The average of the projected values.
        /// </returns>
        public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<double>(
                Expression.Call(
                    GetMethodInfo(Queryable.Average, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the average of the sequence of <see cref="Nullable{Double}" /> values that is obtained
        /// by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The average of the projected values.
        /// </returns>
        public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<double?>(
                Expression.Call(
                    GetMethodInfo(Queryable.Average, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the average of the sequence of <see cref="Single" /> values that is obtained
        /// by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The average of the projected values.
        /// </returns>
        public static Task<float> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<float>(
                Expression.Call(
                    GetMethodInfo(Queryable.Average, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the average of the sequence of <see cref="Nullable{Single}" /> values that is obtained
        /// by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The average of the projected values.
        /// </returns>
        public static Task<float?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<float?>(
                Expression.Call(
                    GetMethodInfo(Queryable.Average, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the average of the sequence of <see cref="Int32" /> values that is obtained
        /// by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The average of the projected values.
        /// </returns>
        public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<double>(
                Expression.Call(
                    GetMethodInfo(Queryable.Average, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the average of the sequence of <see cref="Nullable{Int32}" /> values that is obtained
        /// by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The average of the projected values.
        /// </returns>
        public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<double?>(
                Expression.Call(
                    GetMethodInfo(Queryable.Average, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the average of the sequence of <see cref="Int64" /> values that is obtained
        /// by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The average of the projected values.
        /// </returns>
        public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<double>(
                Expression.Call(
                    GetMethodInfo(Queryable.Average, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the average of the sequence of <see cref="Nullable{Int64}" /> values that is obtained
        /// by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The average of the projected values.
        /// </returns>
        public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<double?>(
                Expression.Call(
                    GetMethodInfo(Queryable.Average, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Returns the number of elements in a sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The <see cref="IQueryable{TSource}" /> that contains the elements to be counted.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The number of elements in the input sequence.
        /// </returns>
        public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<int>(
                Expression.Call(
                    GetMethodInfo(Queryable.Count, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Returns the number of elements in the specified sequence that satisfies a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IQueryable{TSource}" /> that contains the elements to be counted.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The number of elements in the sequence that satisfies the condition in the predicate function.
        /// </returns>
        public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(predicate, nameof(predicate));

            return source.GetMongoQueryProvider().ExecuteAsync<int>(
                Expression.Call(
                    GetMethodInfo(Queryable.Count, source, predicate),
                    source.Expression,
                    Expression.Quote(predicate)),
                cancellationToken);
        }

        /// <summary>
        /// Adds a $densify stage to the LINQ pipeline.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="field">The field.</param>
        /// <param name="range">The range.</param>
        /// <param name="partitionByFields">The partition by fields.</param>
        /// <returns>The densified sequence of values.</returns>
        public static IQueryable<TSource> Densify<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, object>> field,
            DensifyRange range,
            IEnumerable<Expression<Func<TSource, object>>> partitionByFields = null)
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(field, nameof(field));
            Ensure.IsNotNull(range, nameof(range));

            return Densify(source, field, range, partitionByFields?.ToArray());
        }

        /// <summary>
        /// Adds a $densify stage to the LINQ pipeline.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="field">The field.</param>
        /// <param name="range">The range.</param>
        /// <param name="partitionByFields">The partition by fields.</param>
        /// <returns>The densified sequence of values.</returns>
        public static IQueryable<TSource> Densify<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, object>> field,
            DensifyRange range,
            params Expression<Func<TSource, object>>[] partitionByFields)
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(field, nameof(field));
            Ensure.IsNotNull(range, nameof(range));

            Expression quotedPartitionByFields;
            if (partitionByFields?.Length > 0)
            {
                quotedPartitionByFields = Expression.NewArrayInit(typeof(Expression<Func<TSource, object>>), partitionByFields.Select(f => Expression.Quote(f)));
            }
            else
            {
                quotedPartitionByFields = Expression.Constant(null, typeof(Expression<Func<TSource, object>>[]));
            }

            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    GetMethodInfo(Densify, source, field, range, partitionByFields),
                    source.Expression,
                    Expression.Quote(field),
                    Expression.Constant(range),
                    quotedPartitionByFields));
        }

        /// <summary>
        /// Injects a sequence of documents at the beginning of a pipeline.
        /// </summary>
        /// <typeparam name="TDocument"> The type of the documents.</typeparam>
        /// <param name="source">An IQueryable with no other input.</param>
        /// <param name="documents">The documents.</param>
        /// <returns>
        /// An <see cref="IQueryable{TDocument}"/> whose elements are the documents.
        /// </returns>
        public static IQueryable<TDocument> Documents<TDocument>(this IQueryable<NoPipelineInput> source, params TDocument[] documents)
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(documents, nameof(documents));

            return source.Provider.CreateQuery<TDocument>(
                Expression.Call(
                    GetMethodInfo(Documents, source, documents),
                    source.Expression,
                    Expression.Constant(documents, typeof(TDocument[]))));
        }

        /// <summary>
        /// Injects a sequence of documents at the beginning of a pipeline.
        /// </summary>
        /// <typeparam name="TDocument"> The type of the documents.</typeparam>
        /// <param name="source">An IQueryable with no other input.</param>
        /// <param name="documents">The documents.</param>
        /// <param name="documentSerializer">The document serializer.</param>
        /// <returns>
        /// An <see cref="IQueryable{TDocument}"/> whose elements are the documents.
        /// </returns>
        public static IQueryable<TDocument> Documents<TDocument>(this IQueryable<NoPipelineInput> source, IEnumerable<TDocument> documents, IBsonSerializer<TDocument> documentSerializer)
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(documents, nameof(documents));
            Ensure.IsNotNull(documentSerializer, nameof(documentSerializer));

            return source.Provider.CreateQuery<TDocument>(
                Expression.Call(
                    GetMethodInfo(Documents, source, documents, documentSerializer),
                    source.Expression,
                    Expression.Constant(documents, typeof(IEnumerable<TDocument>)),
                    Expression.Constant(documentSerializer, typeof(IBsonSerializer<TDocument>))));
        }

        /// <summary>
        /// Returns the first element of a sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The <see cref="IQueryable{TSource}" /> to return the first element of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The first element in <paramref name="source" />.
        /// </returns>
        public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<TSource>(
                Expression.Call(
                    GetMethodInfo(Queryable.First, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Returns the first element of a sequence that satisfies a specified condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IQueryable{TSource}" /> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The first element in <paramref name="source" /> that passes the test in <paramref name="predicate" />.
        /// </returns>
        public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(predicate, nameof(predicate));

            return source.GetMongoQueryProvider().ExecuteAsync<TSource>(
                Expression.Call(
                    GetMethodInfo(Queryable.First, source, predicate),
                    source.Expression,
                    Expression.Quote(predicate)),
                cancellationToken);
        }

        /// <summary>
        /// Returns the first element of a sequence, or a default value if the sequence contains no elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The <see cref="IQueryable{TSource}" /> to return the first element of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// default(<typeparamref name="TSource" />) if <paramref name="source" /> is empty; otherwise, the first element in <paramref name="source" />.
        /// </returns>
        public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<TSource>(
                Expression.Call(
                    GetMethodInfo(Queryable.FirstOrDefault, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Returns the first element of a sequence that satisfies a specified condition or a default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IQueryable{TSource}" /> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// default(<typeparamref name="TSource" />) if <paramref name="source" /> is empty or if no element passes the test specified by <paramref name="predicate" />; otherwise, the first element in <paramref name="source" /> that passes the test specified by <paramref name="predicate" />.
        /// </returns>
        public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(predicate, nameof(predicate));

            return source.GetMongoQueryProvider().ExecuteAsync<TSource>(
                Expression.Call(
                    GetMethodInfo(Queryable.FirstOrDefault, source, predicate),
                    source.Expression,
                    Expression.Quote(predicate)),
                cancellationToken);
        }

        /// <summary>
        /// Correlates the elements of two sequences based on key equality and groups the results.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TResult">The type of the result elements.</typeparam>
        /// <param name="outer">The first sequence to join.</param>
        /// <param name="inner">The collection to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <param name="resultSelector">A function to create a result element from an element from the first sequence and a collection of matching elements from the second sequence.</param>
        /// <returns>
        /// An <see cref="IQueryable{TResult}" /> that contains elements of type <typeparamref name="TResult" /> obtained by performing a grouped join on two sequences.
        /// </returns>
        public static IQueryable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IQueryable<TOuter> outer, IMongoCollection<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, IEnumerable<TInner>, TResult>> resultSelector)
        {
            Ensure.IsNotNull(outer, nameof(outer));
            Ensure.IsNotNull(inner, nameof(inner));
            Ensure.IsNotNull(outerKeySelector, nameof(outerKeySelector));
            Ensure.IsNotNull(innerKeySelector, nameof(innerKeySelector));
            Ensure.IsNotNull(resultSelector, nameof(resultSelector));

            return Queryable.GroupJoin(outer, inner.AsQueryable(), outerKeySelector, innerKeySelector, resultSelector);
        }

        /// <summary>
        /// Correlates the elements of two sequences based on matching keys.
        /// </summary>
        /// <typeparam name="TOuter">The type of the elements of the first sequence.</typeparam>
        /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
        /// <typeparam name="TResult">The type of the result elements.</typeparam>
        /// <param name="outer">The first sequence to join.</param>
        /// <param name="inner">The sequence to join to the first sequence.</param>
        /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
        /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
        /// <param name="resultSelector">A function to create a result element from two matching elements.</param>
        /// <returns>
        /// An <see cref="T:System.Linq.IQueryable`1" /> that has elements of type <typeparamref name="TResult" /> obtained by performing an inner join on two sequences.
        /// </returns>
        public static IQueryable<TResult> Join<TOuter, TInner, TKey, TResult>(this IQueryable<TOuter> outer, IMongoCollection<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, TInner, TResult>> resultSelector)
        {
            Ensure.IsNotNull(outer, nameof(outer));
            Ensure.IsNotNull(inner, nameof(inner));
            Ensure.IsNotNull(outerKeySelector, nameof(outerKeySelector));
            Ensure.IsNotNull(innerKeySelector, nameof(innerKeySelector));
            Ensure.IsNotNull(resultSelector, nameof(resultSelector));

            return Queryable.Join(outer, inner.AsQueryable(), outerKeySelector, innerKeySelector, resultSelector);
        }

        /// <summary>
        /// Returns the number of elements in a sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The <see cref="IQueryable{TSource}" /> that contains the elements to be counted.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The number of elements in the input sequence.
        /// </returns>
        public static Task<long> LongCountAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<long>(
                Expression.Call(
                    GetMethodInfo(Queryable.LongCount, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Returns the number of elements in the specified sequence that satisfies a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IQueryable{TSource}" /> that contains the elements to be counted.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The number of elements in the sequence that satisfies the condition in the predicate function.
        /// </returns>
        public static Task<long> LongCountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(predicate, nameof(predicate));

            return source.GetMongoQueryProvider().ExecuteAsync<long>(
                Expression.Call(
                    GetMethodInfo(Queryable.LongCount, source, predicate),
                    source.Expression,
                    Expression.Quote(predicate)),
                cancellationToken);
        }

        /// <summary>
        /// Returns the maximum value in a generic <see cref="IQueryable{TSource}" />.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The maximum value in the sequence.
        /// </returns>
        public static Task<TSource> MaxAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<TSource>(
                Expression.Call(
                    GetMethodInfo(Queryable.Max, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Invokes a projection function on each element of a generic <see cref="IQueryable{TSource}" /> and returns the maximum resulting value.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TResult">The type of the value returned by the function represented by <paramref name="selector" />.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum of.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The maximum value in the sequence.
        /// </returns>
        public static Task<TResult> MaxAsync<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<TResult>(
                Expression.Call(
                    GetMethodInfo(Queryable.Max, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Returns the minimum value in a generic <see cref="IQueryable{TSource}" />.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to determine the minimum of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The minimum value in the sequence.
        /// </returns>
        public static Task<TSource> MinAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<TSource>(
                Expression.Call(
                    GetMethodInfo(Queryable.Min, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Invokes a projection function on each element of a generic <see cref="IQueryable{TSource}" /> and returns the minimum resulting value.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TResult">The type of the value returned by the function represented by <paramref name="selector" />.</typeparam>
        /// <param name="source">A sequence of values to determine the minimum of.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The minimum value in the sequence.
        /// </returns>
        public static Task<TResult> MinAsync<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<TResult>(
                Expression.Call(
                    GetMethodInfo<IQueryable<TSource>, Expression<Func<TSource, TResult>>, TResult>(Queryable.Min, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Returns a sample of the elements in the <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IQueryable{TSource}" /> to return a sample of.</param>
        /// <param name="count">The number of elements in the sample.</param>
        /// <returns>
        /// A sample of the elements in the <paramref name="source"/>.
        /// </returns>
        public static IQueryable<TSource> Sample<TSource>(this IQueryable<TSource> source, long count)
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfo(Sample, source, count),
                    source.Expression,
                    Expression.Constant(count)));
        }

        /// <summary>
        /// Appends a $search stage to the LINQ pipeline.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="searchDefinition">The search definition.</param>
        /// <param name="highlight">The highlight options.</param>
        /// <param name="indexName">The index name.</param>
        /// <param name="count">The count options.</param>
        /// <param name="returnStoredSource">
        /// Flag that specifies whether to perform a full document lookup on the backend database
        /// or return only stored source fields directly from Atlas Search.
        /// </param>
        /// <param name="scoreDetails">
        /// Flag that specifies whether to return a detailed breakdown
        /// of the score for each document in the result. 
        /// </param>
        /// <returns>The queryable with a new stage appended.</returns>
        public static IQueryable<TSource> Search<TSource>(
            this IQueryable<TSource> source,
            SearchDefinition<TSource> searchDefinition,
            SearchHighlightOptions<TSource> highlight = null,
            string indexName = null,
            SearchCountOptions count = null,
            bool returnStoredSource = false,
            bool scoreDetails = false)
        {
            var searchOptions = new SearchOptions<TSource>()
            {
                CountOptions = count,
                Highlight = highlight,
                IndexName = indexName,
                ReturnStoredSource = returnStoredSource,
                ScoreDetails = scoreDetails
            };

            return Search(source, searchDefinition, searchOptions);
        }

        /// <summary>
        /// Appends a $search stage to the LINQ pipeline.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="searchDefinition">The search definition.</param>
        /// <param name="searchOptions">The search options.</param>
        /// <returns>The queryable with a new stage appended.</returns>
        public static IQueryable<TSource> Search<TSource>(
            this IQueryable<TSource> source,
            SearchDefinition<TSource> searchDefinition,
            SearchOptions<TSource> searchOptions)
        {
            return AppendStage(
                source,
                PipelineStageDefinitionBuilder.Search(searchDefinition, searchOptions));
        }

        /// <summary>
        /// Appends a $searchMeta stage to the LINQ pipeline.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="searchDefinition">The search definition.</param>
        /// <param name="indexName">The index name.</param>
        /// <param name="count">The count options.</param>
        /// <returns>The queryable with a new stage appended.</returns>
        public static IQueryable<SearchMetaResult> SearchMeta<TSource>(
            this IQueryable<TSource> source,
            SearchDefinition<TSource> searchDefinition,
            string indexName = null,
            SearchCountOptions count = null)
        {
            return AppendStage(
                source,
                PipelineStageDefinitionBuilder.SearchMeta(searchDefinition, indexName, count));
        }

        /// <summary>
        /// Returns the only element of a sequence, and throws an exception if there is not exactly one element in the sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IQueryable{TSource}" /> to return the single element of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The single element of the input sequence.
        /// </returns>
        public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<TSource>(
                Expression.Call(
                    GetMethodInfo(Queryable.Single, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Returns the only element of a sequence that satisfies a specified condition, and throws an exception if more than one such element exists.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IQueryable{TSource}" /> to return a single element from.</param>
        /// <param name="predicate">A function to test an element for a condition.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The single element of the input sequence that satisfies the condition in <paramref name="predicate" />.
        /// </returns>
        public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(predicate, nameof(predicate));

            return source.GetMongoQueryProvider().ExecuteAsync<TSource>(
                Expression.Call(
                    GetMethodInfo(Queryable.Single, source, predicate),
                    source.Expression,
                    Expression.Quote(predicate)),
                cancellationToken);
        }

        /// <summary>
        /// Returns the only element of a sequence, or a default value if the sequence is empty; this method throws an exception if there is more than one element in the sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IQueryable{TSource}" /> to return the single element of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The single element of the input sequence, or default(<typeparamref name="TSource" />) if the sequence contains no elements.
        /// </returns>
        public static Task<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<TSource>(
                Expression.Call(
                    GetMethodInfo(Queryable.SingleOrDefault, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Returns the only element of a sequence that satisfies a specified condition or a default value if no such element exists; this method throws an exception if more than one element satisfies the condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IQueryable{TSource}" /> to return a single element from.</param>
        /// <param name="predicate">A function to test an element for a condition.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The single element of the input sequence that satisfies the condition in <paramref name="predicate" />, or default(<typeparamref name="TSource" />) if no such element is found.
        /// </returns>
        public static Task<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(predicate, nameof(predicate));

            return source.GetMongoQueryProvider().ExecuteAsync<TSource>(
                Expression.Call(
                    GetMethodInfo(Queryable.SingleOrDefault, source, predicate),
                    source.Expression,
                    Expression.Quote(predicate)),
                cancellationToken);
        }

        /// <summary>
        /// Bypasses a specified number of elements in a sequence and then returns the
        /// remaining elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source</typeparam>
        /// <param name="source">An <see cref="IQueryable{TSource}"/> to return elements from.</param>
        /// <param name="count">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>
        /// An <see cref="IQueryable{TSource}"/> that contains elements that occur after the
        /// specified index in the input sequence.
        /// </returns>
        public static IQueryable<TSource> Skip<TSource>(this IQueryable<TSource> source, long count)
        {
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    GetMethodInfo(Skip, source, count),
                    source.Expression,
                    Expression.Constant(count)));
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double StandardDeviationPopulation(this IQueryable<int> source)
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.Provider.Execute<double>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source),
                    source.Expression));
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double? StandardDeviationPopulation(this IQueryable<int?> source)
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.Provider.Execute<double?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source),
                    source.Expression));
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double StandardDeviationPopulation(this IQueryable<long> source)
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.Provider.Execute<double>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source),
                    source.Expression));
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double? StandardDeviationPopulation(this IQueryable<long?> source)
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.Provider.Execute<double?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source),
                    source.Expression));
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static float StandardDeviationPopulation(this IQueryable<float> source)
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.Provider.Execute<float>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source),
                    source.Expression));
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static float? StandardDeviationPopulation(this IQueryable<float?> source)
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.Provider.Execute<float?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source),
                    source.Expression));
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double StandardDeviationPopulation(this IQueryable<double> source)
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.Provider.Execute<double>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source),
                    source.Expression));
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double? StandardDeviationPopulation(this IQueryable<double?> source)
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.Provider.Execute<double?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source),
                    source.Expression));
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static decimal StandardDeviationPopulation(this IQueryable<decimal> source)
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.Provider.Execute<decimal>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source),
                    source.Expression));
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static decimal? StandardDeviationPopulation(this IQueryable<decimal?> source)
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.Provider.Execute<decimal?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source),
                    source.Expression));
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double StandardDeviationPopulation<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector)
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.Provider.Execute<double>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source, selector),
                    source.Expression,
                    Expression.Quote(selector)));
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double? StandardDeviationPopulation<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector)
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.Provider.Execute<double?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source, selector),
                    source.Expression,
                    Expression.Quote(selector)));
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double StandardDeviationPopulation<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector)
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.Provider.Execute<double>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source, selector),
                    source.Expression,
                    Expression.Quote(selector)));
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double? StandardDeviationPopulation<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector)
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.Provider.Execute<double?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source, selector),
                    source.Expression,
                    Expression.Quote(selector)));
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static float StandardDeviationPopulation<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector)
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.Provider.Execute<float>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source, selector),
                    source.Expression,
                    Expression.Quote(selector)));
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static float? StandardDeviationPopulation<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector)
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.Provider.Execute<float?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source, selector),
                    source.Expression,
                    Expression.Quote(selector)));
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double StandardDeviationPopulation<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector)
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.Provider.Execute<double>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source, selector),
                    source.Expression,
                    Expression.Quote(selector)));
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double? StandardDeviationPopulation<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector)
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.Provider.Execute<double?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source, selector),
                    source.Expression,
                    Expression.Quote(selector)));
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static decimal StandardDeviationPopulation<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector)
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.Provider.Execute<decimal>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source, selector),
                    source.Expression,
                    Expression.Quote(selector)));
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static decimal? StandardDeviationPopulation<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector)
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.Provider.Execute<decimal?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source, selector),
                    source.Expression,
                    Expression.Quote(selector)));
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<double> StandardDeviationPopulationAsync(this IQueryable<int> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<double>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<double?> StandardDeviationPopulationAsync(this IQueryable<int?> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<double?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<double> StandardDeviationPopulationAsync(this IQueryable<long> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<double>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<double?> StandardDeviationPopulationAsync(this IQueryable<long?> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<double?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<float> StandardDeviationPopulationAsync(this IQueryable<float> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<float>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<float?> StandardDeviationPopulationAsync(this IQueryable<float?> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<float?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<double> StandardDeviationPopulationAsync(this IQueryable<double> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<double>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<double?> StandardDeviationPopulationAsync(this IQueryable<double?> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<double?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<decimal> StandardDeviationPopulationAsync(this IQueryable<decimal> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<decimal>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<decimal?> StandardDeviationPopulationAsync(this IQueryable<decimal?> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<decimal?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<double> StandardDeviationPopulationAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<double>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<double?> StandardDeviationPopulationAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<double?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<double> StandardDeviationPopulationAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<double>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<double?> StandardDeviationPopulationAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<double?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<float> StandardDeviationPopulationAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<float>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<float?> StandardDeviationPopulationAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<float?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<double> StandardDeviationPopulationAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<double>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<double?> StandardDeviationPopulationAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<double?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<decimal> StandardDeviationPopulationAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<decimal>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<decimal?> StandardDeviationPopulationAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<decimal?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationPopulation, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double StandardDeviationSample(this IQueryable<int> source)
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.Provider.Execute<double>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source),
                    source.Expression));
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double? StandardDeviationSample(this IQueryable<int?> source)
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.Provider.Execute<double?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source),
                    source.Expression));
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double StandardDeviationSample(this IQueryable<long> source)
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.Provider.Execute<double>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source),
                    source.Expression));
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double? StandardDeviationSample(this IQueryable<long?> source)
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.Provider.Execute<double?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source),
                    source.Expression));
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static float StandardDeviationSample(this IQueryable<float> source)
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.Provider.Execute<float>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source),
                    source.Expression));
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static float? StandardDeviationSample(this IQueryable<float?> source)
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.Provider.Execute<float?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source),
                    source.Expression));
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double StandardDeviationSample(this IQueryable<double> source)
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.Provider.Execute<double>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source),
                    source.Expression));
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double? StandardDeviationSample(this IQueryable<double?> source)
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.Provider.Execute<double?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source),
                    source.Expression));
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static decimal StandardDeviationSample(this IQueryable<decimal> source)
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.Provider.Execute<decimal>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source),
                    source.Expression));
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static decimal? StandardDeviationSample(this IQueryable<decimal?> source)
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.Provider.Execute<decimal?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source),
                    source.Expression));
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double StandardDeviationSample<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector)
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.Provider.Execute<double>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source, selector),
                    source.Expression,
                    Expression.Quote(selector)));
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double? StandardDeviationSample<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector)
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.Provider.Execute<double?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source, selector),
                    source.Expression,
                    Expression.Quote(selector)));
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double StandardDeviationSample<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector)
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.Provider.Execute<double>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source, selector),
                    source.Expression,
                    Expression.Quote(selector)));
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double? StandardDeviationSample<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector)
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.Provider.Execute<double?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source, selector),
                    source.Expression,
                    Expression.Quote(selector)));
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static float StandardDeviationSample<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector)
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.Provider.Execute<float>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source, selector),
                    source.Expression,
                    Expression.Quote(selector)));
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static float? StandardDeviationSample<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector)
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.Provider.Execute<float?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source, selector),
                    source.Expression,
                    Expression.Quote(selector)));
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double StandardDeviationSample<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector)
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.Provider.Execute<double>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source, selector),
                    source.Expression,
                    Expression.Quote(selector)));
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double? StandardDeviationSample<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector)
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.Provider.Execute<double?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source, selector),
                    source.Expression,
                    Expression.Quote(selector)));
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static decimal StandardDeviationSample<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector)
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.Provider.Execute<decimal>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source, selector),
                    source.Expression,
                    Expression.Quote(selector)));
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static decimal? StandardDeviationSample<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector)
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.Provider.Execute<decimal?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source, selector),
                    source.Expression,
                    Expression.Quote(selector)));
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<double> StandardDeviationSampleAsync(this IQueryable<int> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<double>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<double?> StandardDeviationSampleAsync(this IQueryable<int?> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<double?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<double> StandardDeviationSampleAsync(this IQueryable<long> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<double>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<double?> StandardDeviationSampleAsync(this IQueryable<long?> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<double?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<float> StandardDeviationSampleAsync(this IQueryable<float> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<float>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<float?> StandardDeviationSampleAsync(this IQueryable<float?> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<float?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<double> StandardDeviationSampleAsync(this IQueryable<double> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<double>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<double?> StandardDeviationSampleAsync(this IQueryable<double?> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<double?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<decimal> StandardDeviationSampleAsync(this IQueryable<decimal> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<decimal>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<decimal?> StandardDeviationSampleAsync(this IQueryable<decimal?> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<decimal?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<double> StandardDeviationSampleAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<double>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<double?> StandardDeviationSampleAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<double?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<double> StandardDeviationSampleAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<double>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<double?> StandardDeviationSampleAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<double?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<float> StandardDeviationSampleAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<float>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<float?> StandardDeviationSampleAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<float?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<double> StandardDeviationSampleAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<double>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<double?> StandardDeviationSampleAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<double?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<decimal> StandardDeviationSampleAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<decimal>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static Task<decimal?> StandardDeviationSampleAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<decimal?>(
                Expression.Call(
                    GetMethodInfo(StandardDeviationSample, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sum of a sequence of <see cref="Decimal"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        public static Task<decimal> SumAsync(this IQueryable<decimal> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<decimal>(
                Expression.Call(
                    GetMethodInfo(Queryable.Sum, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sum of a sequence of <see cref="Nullable{Decimal}"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        public static Task<decimal?> SumAsync(this IQueryable<decimal?> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<decimal?>(
                Expression.Call(
                    GetMethodInfo(Queryable.Sum, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sum of a sequence of <see cref="Double"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        public static Task<double> SumAsync(this IQueryable<double> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<double>(
                Expression.Call(
                    GetMethodInfo(Queryable.Sum, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sum of a sequence of <see cref="Nullable{Double}"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        public static Task<double?> SumAsync(this IQueryable<double?> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<double?>(
                Expression.Call(
                    GetMethodInfo(Queryable.Sum, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sum of a sequence of <see cref="Single"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        public static Task<float> SumAsync(this IQueryable<float> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<float>(
                Expression.Call(
                    GetMethodInfo(Queryable.Sum, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sum of a sequence of <see cref="Nullable{Single}"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        public static Task<float?> SumAsync(this IQueryable<float?> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<float?>(
                Expression.Call(
                    GetMethodInfo(Queryable.Sum, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sum of a sequence of <see cref="Int32"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        public static Task<int> SumAsync(this IQueryable<int> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<int>(
                Expression.Call(
                    GetMethodInfo(Queryable.Sum, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sum of a sequence of <see cref="Nullable{Int32}"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        public static Task<int?> SumAsync(this IQueryable<int?> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<int?>(
                Expression.Call(
                    GetMethodInfo(Queryable.Sum, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sum of a sequence of <see cref="Int64"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        public static Task<long> SumAsync(this IQueryable<long> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<long>(
                Expression.Call(
                    GetMethodInfo(Queryable.Sum, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sum of a sequence of <see cref="Nullable{Int64}"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        public static Task<long?> SumAsync(this IQueryable<long?> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));

            return source.GetMongoQueryProvider().ExecuteAsync<long?>(
                Expression.Call(
                    GetMethodInfo(Queryable.Sum, source),
                    source.Expression),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sum of the sequence of <see cref="Decimal" /> values that is obtained
        /// by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The sum of the projected values.
        /// </returns>
        public static Task<decimal> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<decimal>(
                Expression.Call(
                    GetMethodInfo(Queryable.Sum, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sum of the sequence of <see cref="Nullable{Decimal}" /> values that is obtained
        /// by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The sum of the projected values.
        /// </returns>
        public static Task<decimal?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<decimal?>(
                Expression.Call(
                    GetMethodInfo(Queryable.Sum, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sum of the sequence of <see cref="Double" /> values that is obtained
        /// by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The sum of the projected values.
        /// </returns>
        public static Task<double> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<double>(
                Expression.Call(
                    GetMethodInfo(Queryable.Sum, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sum of the sequence of <see cref="Nullable{Double}" /> values that is obtained
        /// by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The sum of the projected values.
        /// </returns>
        public static Task<double?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<double?>(
                Expression.Call(
                    GetMethodInfo(Queryable.Sum, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sum of the sequence of <see cref="Single" /> values that is obtained
        /// by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The sum of the projected values.
        /// </returns>
        public static Task<float> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<float>(
                Expression.Call(
                    GetMethodInfo(Queryable.Sum, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sum of the sequence of <see cref="Nullable{Single}" /> values that is obtained
        /// by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The sum of the projected values.
        /// </returns>
        public static Task<float?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<float?>(
                Expression.Call(
                    GetMethodInfo(Queryable.Sum, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sum of the sequence of <see cref="Int32" /> values that is obtained
        /// by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The sum of the projected values.
        /// </returns>
        public static Task<int> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<int>(
                Expression.Call(
                    GetMethodInfo(Queryable.Sum, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sum of the sequence of <see cref="Nullable{Int32}" /> values that is obtained
        /// by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The sum of the projected values.
        /// </returns>
        public static Task<int?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<int?>(
                Expression.Call(
                    GetMethodInfo(Queryable.Sum, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sum of the sequence of <see cref="Int64" /> values that is obtained
        /// by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The sum of the projected values.
        /// </returns>
        public static Task<long> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<long>(
                Expression.Call(
                    GetMethodInfo(Queryable.Sum, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the sum of the sequence of <see cref="Nullable{Int64}" /> values that is obtained
        /// by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The sum of the projected values.
        /// </returns>
        public static Task<long?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            return source.GetMongoQueryProvider().ExecuteAsync<long?>(
                Expression.Call(
                    GetMethodInfo(Queryable.Sum, source, selector),
                    source.Expression,
                    Expression.Quote(selector)),
                cancellationToken);
        }

        /// <summary>
        /// Returns a specified number of contiguous elements from the start of a sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The sequence to return elements from.</param>
        /// <param name="count">The number of elements to return.</param>
        /// <returns>
        /// An <see cref="IQueryable{TSource}"/> that contains the specified number of elements
        /// from the start of source.
        /// </returns>
        public static IQueryable<TSource> Take<TSource>(this IQueryable<TSource> source, long count)
        {
            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    GetMethodInfo(Take, source, count),
                    source.Expression,
                    Expression.Constant(count)));
        }

        /// <summary>
        /// Executes the LINQ query and returns a cursor to the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The sequence to return elements from.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A cursor to the results of executing the LINQ query.</returns>
        public static IAsyncCursor<TSource> ToCursor<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            var cursorSource = GetCursorSource(source);
            return cursorSource.ToCursor(cancellationToken);
        }

        /// <summary>
        /// Executes the LINQ query and returns a cursor to the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The sequence to return elements from.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A cursor to the results of executing the LINQ query.</returns>
        public static Task<IAsyncCursor<TSource>> ToCursorAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            var cursorSource = GetCursorSource(source);
            return cursorSource.ToCursorAsync(cancellationToken);
        }

        /// <summary>
        /// Executes the LINQ query and returns a list of the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The sequence to return elements from.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of the results of executing the LINQ query.</returns>
        public static Task<List<TSource>> ToListAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            var cursorSource = GetCursorSource(source);
            return cursorSource.ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Appends a $vectorSearch stage to the LINQ pipeline.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="field">The field.</param>
        /// <param name="queryVector">The query vector.</param>
        /// <param name="limit">The limit.</param>
        /// <param name="options">The options.</param>
        /// <returns>
        /// The queryable with a new stage appended.
        /// </returns>
        public static IQueryable<TSource> VectorSearch<TSource, TField>(
            this IQueryable<TSource> source,
            FieldDefinition<TSource> field,
            QueryVector queryVector,
            int limit,
            VectorSearchOptions<TSource> options = null)
        {
            return AppendStage(
                source,
                PipelineStageDefinitionBuilder.VectorSearch(field, queryVector, limit, options));
        }

        /// <summary>
        /// Appends a $vectorSearch stage to the LINQ pipeline.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="field">The field.</param>
        /// <param name="queryVector">The query vector.</param>
        /// <param name="limit">The limit.</param>
        /// <param name="options">The options.</param>
        /// <returns>
        /// The queryable with a new stage appended.
        /// </returns>
        public static IQueryable<TSource> VectorSearch<TSource, TField>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, TField>> field,
            QueryVector queryVector,
            int limit,
            VectorSearchOptions<TSource> options = null)
        {
            return AppendStage(
                source,
                PipelineStageDefinitionBuilder.VectorSearch(field, queryVector, limit, options));
        }

        private static IAsyncCursorSource<TDocument> GetCursorSource<TDocument>(IQueryable<TDocument> source)
        {
            Ensure.IsNotNull(source, nameof(source));
            var cursorSource = source as IAsyncCursorSource<TDocument>;
            if (cursorSource == null)
            {
                throw new ArgumentException("The source argument must be a MongoDB IQueryable.", nameof(source));
            }

            return cursorSource;
        }

        private static MethodInfo GetMethodInfo<T1, T2>(Func<T1, T2> f, T1 unused)
        {
            return f.GetMethodInfo();
        }

        private static MethodInfo GetMethodInfo<T1, T2, T3>(Func<T1, T2, T3> f, T1 unused1, T2 unused2)
        {
            return f.GetMethodInfo();
        }

        private static MethodInfo GetMethodInfo<T1, T2, T3, T4>(Func<T1, T2, T3, T4> f, T1 unused1, T2 unused2, T3 unused3)
        {
            return f.GetMethodInfo();
        }

        private static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5> f, T1 unused1, T2 unused2, T3 unused3, T4 unused4)
        {
            return f.GetMethodInfo();
        }
    }
}
