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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// Extension for IMongoQueryable.
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
        public static Task<bool> AnyAsync<TSource>(this IMongoQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IMongoQueryProvider)source.Provider).ExecuteAsync<bool>(
                Expression.Call(
                    null,
                    ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource) }),
                    Expression.Convert(source.Expression, typeof(IMongoQueryable<TSource>)),
                    Expression.Constant(cancellationToken)),
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
        public static Task<bool> AnyAsync<TSource>(this IMongoQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IMongoQueryProvider)source.Provider).ExecuteAsync<bool>(
                Expression.Call(
                    null,
                    ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource) }),
                    Expression.Convert(source.Expression, typeof(IMongoQueryable<TSource>)),
                    Expression.Quote(predicate),
                    Expression.Constant(cancellationToken)),
                cancellationToken);
        }

        /// <summary>
        /// Computes the average of a sequence of <see cref="Decimal"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The average of the values in the sequence.</returns>
        public static Task<decimal> AverageAsync(this IMongoQueryable<decimal> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            return AverageAsync<decimal, decimal>(source, cancellationToken);
        }

        /// <summary>
        /// Computes the average of a sequence of <see cref="Nullable{Decimal}"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The average of the values in the sequence.</returns>
        public static Task<decimal?> AverageAsync(this IMongoQueryable<decimal?> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            return AverageAsync<decimal?, decimal?>(source, cancellationToken);
        }

        /// <summary>
        /// Computes the average of a sequence of <see cref="Double"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The average of the values in the sequence.</returns>
        public static Task<double> AverageAsync(this IMongoQueryable<double> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            return AverageAsync<double, double>(source, cancellationToken);
        }

        /// <summary>
        /// Computes the average of a sequence of <see cref="Nullable{Double}"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The average of the values in the sequence.</returns>
        public static Task<double?> AverageAsync(this IMongoQueryable<double?> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            return AverageAsync<double?, double?>(source, cancellationToken);
        }

        /// <summary>
        /// Computes the average of a sequence of <see cref="Single"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The average of the values in the sequence.</returns>
        public static Task<float> AverageAsync(this IMongoQueryable<float> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            return AverageAsync<float, float>(source, cancellationToken);
        }

        /// <summary>
        /// Computes the average of a sequence of <see cref="Nullable{Single}"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The average of the values in the sequence.</returns>
        public static Task<float?> AverageAsync(this IMongoQueryable<float?> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            return AverageAsync<float?, float?>(source, cancellationToken);
        }

        /// <summary>
        /// Computes the average of a sequence of <see cref="Int32"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The average of the values in the sequence.</returns>
        public static Task<double> AverageAsync(this IMongoQueryable<int> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            return AverageAsync<int, double>(source, cancellationToken);
        }

        /// <summary>
        /// Computes the average of a sequence of <see cref="Nullable{Int32}"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The average of the values in the sequence.</returns>
        public static Task<double?> AverageAsync(this IMongoQueryable<int?> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            return AverageAsync<int?, double?>(source, cancellationToken);
        }

        /// <summary>
        /// Computes the average of a sequence of <see cref="Int64"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The average of the values in the sequence.</returns>
        public static Task<double> AverageAsync(this IMongoQueryable<long> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            return AverageAsync<long, double>(source, cancellationToken);
        }

        /// <summary>
        /// Computes the average of a sequence of <see cref="Nullable{Int64}"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The average of the values in the sequence.</returns>
        public static Task<double?> AverageAsync(this IMongoQueryable<long?> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            return AverageAsync<long?, double?>(source, cancellationToken);
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
        public static Task<decimal> AverageAsync<TSource>(this IMongoQueryable<TSource> source, Expression<Func<TSource, decimal>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            return AverageAsync<TSource, decimal, decimal>(source, selector, cancellationToken);
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
        public static Task<decimal?> AverageAsync<TSource>(this IMongoQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            return AverageAsync<TSource, decimal?, decimal?>(source, selector, cancellationToken);
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
        public static Task<double> AverageAsync<TSource>(this IMongoQueryable<TSource> source, Expression<Func<TSource, double>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            return AverageAsync<TSource, double, double>(source, selector, cancellationToken);
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
        public static Task<double?> AverageAsync<TSource>(this IMongoQueryable<TSource> source, Expression<Func<TSource, double?>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            return AverageAsync<TSource, double?, double?>(source, selector, cancellationToken);
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
        public static Task<float> AverageAsync<TSource>(this IMongoQueryable<TSource> source, Expression<Func<TSource, float>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            return AverageAsync<TSource, float, float>(source, selector, cancellationToken);
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
        public static Task<float?> AverageAsync<TSource>(this IMongoQueryable<TSource> source, Expression<Func<TSource, float?>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            return AverageAsync<TSource, float?, float?>(source, selector, cancellationToken);
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
        public static Task<double> AverageAsync<TSource>(this IMongoQueryable<TSource> source, Expression<Func<TSource, int>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            return AverageAsync<TSource, int, double>(source, selector, cancellationToken);
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
        public static Task<double?> AverageAsync<TSource>(this IMongoQueryable<TSource> source, Expression<Func<TSource, int?>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            return AverageAsync<TSource, int?, double?>(source, selector, cancellationToken);
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
        public static Task<double> AverageAsync<TSource>(this IMongoQueryable<TSource> source, Expression<Func<TSource, long>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            return AverageAsync<TSource, long, double>(source, selector, cancellationToken);
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
        public static Task<double?> AverageAsync<TSource>(this IMongoQueryable<TSource> source, Expression<Func<TSource, long?>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            return AverageAsync<TSource, long?, double?>(source, selector, cancellationToken);
        }

        /// <summary>
        /// Returns the number of elements in a sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The <see cref="IMongoQueryable{TSource}" /> that contains the elements to be counted.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The number of elements in the input sequence.
        /// </returns>
        public static Task<int> CountAsync<TSource>(this IMongoQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IMongoQueryProvider)source.Provider).ExecuteAsync<int>(
                Expression.Call(
                    null,
                    ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource) }),
                    Expression.Convert(source.Expression, typeof(IMongoQueryable<TSource>)),
                    Expression.Constant(cancellationToken)),
                cancellationToken);
        }

        /// <summary>
        /// Returns the number of elements in the specified sequence that satisfies a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IMongoQueryable{TSource}" /> that contains the elements to be counted.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The number of elements in the sequence that satisfies the condition in the predicate function.
        /// </returns>
        public static Task<int> CountAsync<TSource>(this IMongoQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IMongoQueryProvider)source.Provider).ExecuteAsync<int>(
                Expression.Call(
                    null,
                    ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource) }),
                    Expression.Convert(source.Expression, typeof(IMongoQueryable<TSource>)),
                    Expression.Quote(predicate),
                    Expression.Constant(cancellationToken)),
                cancellationToken);
        }

        /// <summary>
        /// Returns distinct elements from a sequence by using the default equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The <see cref="IMongoQueryable{TSource}" /> to remove duplicates from.</param>
        /// <returns>
        /// An <see cref="IMongoQueryable{TSource}" /> that contains distinct elements from <paramref name="source" />.
        /// </returns>
        public static IMongoQueryable<TSource> Distinct<TSource>(this IMongoQueryable<TSource> source)
        {
            return (IMongoQueryable<TSource>)Queryable.Distinct(source);
        }

        /// <summary>
        /// Returns the first element of a sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The <see cref="IMongoQueryable{TSource}" /> to return the first element of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The first element in <paramref name="source" />.
        /// </returns>
        public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IMongoQueryProvider)source.Provider).ExecuteAsync<TSource>(
                Expression.Call(
                    null,
                    ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource) }),
                    Expression.Convert(source.Expression, typeof(IMongoQueryable<TSource>)),
                    Expression.Constant(cancellationToken)),
                cancellationToken);
        }

        /// <summary>
        /// Returns the first element of a sequence that satisfies a specified condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IMongoQueryable{TSource}" /> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The first element in <paramref name="source" /> that passes the test in <paramref name="predicate" />.
        /// </returns>
        public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IMongoQueryProvider)source.Provider).ExecuteAsync<TSource>(
                Expression.Call(
                    null,
                    ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource) }),
                    Expression.Convert(source.Expression, typeof(IMongoQueryable<TSource>)),
                    Expression.Quote(predicate),
                    Expression.Constant(cancellationToken)),
                cancellationToken);
        }

        /// <summary>
        /// Returns the first element of a sequence, or a default value if the sequence contains no elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The <see cref="IMongoQueryable{TSource}" /> to return the first element of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// default(<typeparamref name="TSource" />) if <paramref name="source" /> is empty; otherwise, the first element in <paramref name="source" />.
        /// </returns>
        public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IMongoQueryProvider)source.Provider).ExecuteAsync<TSource>(
                Expression.Call(
                    null,
                    ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource) }),
                    Expression.Convert(source.Expression, typeof(IMongoQueryable<TSource>)),
                    Expression.Constant(cancellationToken)),
                cancellationToken);
        }

        /// <summary>
        /// Returns the first element of a sequence that satisfies a specified condition or a default value if no such element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IMongoQueryable{TSource}" /> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// default(<typeparamref name="TSource" />) if <paramref name="source" /> is empty or if no element passes the test specified by <paramref name="predicate" />; otherwise, the first element in <paramref name="source" /> that passes the test specified by <paramref name="predicate" />.
        /// </returns>
        public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IMongoQueryProvider)source.Provider).ExecuteAsync<TSource>(
                Expression.Call(
                    null,
                    ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource) }),
                    Expression.Convert(source.Expression, typeof(IMongoQueryable<TSource>)),
                    Expression.Quote(predicate),
                    Expression.Constant(cancellationToken)),
                cancellationToken);
        }

        /// <summary>
        /// Groups the elements of a sequence according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the function represented in keySelector.</typeparam>
        /// <param name="source">An <see cref="IMongoQueryable{TSource}" /> whose elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>
        /// An <see cref="IMongoQueryable{T}" /> that has a type argument of <see cref="IGrouping{TKey, TSource}"/> 
        /// and where each <see cref="IGrouping{TKey, TSource}"/> object contains a sequence of objects 
        /// and a key.
        /// </returns>
        public static IMongoQueryable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IMongoQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            return (IMongoQueryable<IGrouping<TKey, TSource>>)Queryable.GroupBy(source, keySelector);
        }

        /// <summary>
        /// Groups the elements of a sequence according to a specified key selector function
        /// and creates a result value from each group and its key.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the function represented in keySelector.</typeparam>
        /// <typeparam name="TResult">The type of the result value returned by resultSelector.</typeparam>
        /// <param name="source">An <see cref="IMongoQueryable{TSource}" /> whose elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="resultSelector">A function to create a result value from each group.</param>
        /// <returns>
        /// An <see cref="IMongoQueryable{T}" /> that has a type argument of TResult and where
        /// each element represents a projection over a group and its key.
        /// </returns>
        public static IMongoQueryable<TResult> GroupBy<TSource, TKey, TResult>(this IMongoQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TKey, IEnumerable<TSource>, TResult>> resultSelector)
        {
            return (IMongoQueryable<TResult>)Queryable.GroupBy(source, keySelector, resultSelector);
        }

        /// <summary>
        /// Returns the maximum value in a generic <see cref="IMongoQueryable{TSource}" />.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The maximum value in the sequence.
        /// </returns>
        public static Task<TSource> MaxAsync<TSource>(this IMongoQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            var method = ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource) });
            return ((IMongoQueryProvider)source.Provider).ExecuteAsync<TSource>(
                Expression.Call(
                    null,
                    method,
                    Expression.Convert(source.Expression, typeof(IMongoQueryable<TSource>)),
                    Expression.Constant(cancellationToken)),
                cancellationToken);
        }

        /// <summary>
        /// Invokes a projection function on each element of a generic <see cref="IMongoQueryable{TSource}" /> and returns the maximum resulting value.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TResult">The type of the value returned by the function represented by <paramref name="selector" />.</typeparam>
        /// <param name="source">A sequence of values to determine the maximum of.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The maximum value in the sequence.
        /// </returns>
        public static Task<TResult> MaxAsync<TSource, TResult>(this IMongoQueryable<TSource> source, Expression<Func<TSource, TResult>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IMongoQueryProvider)source.Provider).ExecuteAsync<TResult>(
                Expression.Call(
                    null,
                    ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource), typeof(TResult) }),
                    Expression.Convert(source.Expression, typeof(IMongoQueryable<TSource>)),
                    Expression.Quote(selector),
                    Expression.Constant(cancellationToken)),
                cancellationToken);
        }

        /// <summary>
        /// Returns the minimum value in a generic <see cref="IMongoQueryable{TSource}" />.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to determine the minimum of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The minimum value in the sequence.
        /// </returns>
        public static Task<TSource> MinAsync<TSource>(this IMongoQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IMongoQueryProvider)source.Provider).ExecuteAsync<TSource>(
                Expression.Call(
                    null,
                    ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource) }),
                    Expression.Convert(source.Expression, typeof(IMongoQueryable<TSource>)),
                    Expression.Constant(cancellationToken)),
                cancellationToken);
        }

        /// <summary>
        /// Invokes a projection function on each element of a generic <see cref="IMongoQueryable{TSource}" /> and returns the minimum resulting value.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TResult">The type of the value returned by the function represented by <paramref name="selector" />.</typeparam>
        /// <param name="source">A sequence of values to determine the minimum of.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The minimum value in the sequence.
        /// </returns>
        public static Task<TResult> MinAsync<TSource, TResult>(this IMongoQueryable<TSource> source, Expression<Func<TSource, TResult>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IMongoQueryProvider)source.Provider).ExecuteAsync<TResult>(
                Expression.Call(
                    null,
                    ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource), typeof(TResult) }),
                    Expression.Convert(source.Expression, typeof(IMongoQueryable<TSource>)),
                    Expression.Quote(selector),
                    Expression.Constant(cancellationToken)),
                cancellationToken);
        }

        /// <summary>
        /// Filters the elements of an <see cref="IMongoQueryable" /> based on a specified type.
        /// </summary>
        /// <typeparam name="TResult">The type to filter the elements of the sequence on.</typeparam>
        /// <param name="source">An <see cref="IMongoQueryable" /> whose elements to filter.</param>
        /// <returns>
        /// A collection that contains the elements from <paramref name="source" /> that have type <typeparamref name="TResult" />.
        /// </returns>
        public static IMongoQueryable<TResult> OfType<TResult>(this IMongoQueryable source)
        {
            return (IMongoQueryable<TResult>)Queryable.OfType<TResult>(source);
        }

        /// <summary>
        /// Sorts the elements of a sequence in ascending order according to a key.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the function that is represented by keySelector.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>
        /// An <see cref="IOrderedMongoQueryable{TSource}"/> whose elements are sorted according to a key.
        /// </returns>
        public static IOrderedMongoQueryable<TSource> OrderBy<TSource, TKey>(this IMongoQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            return (IOrderedMongoQueryable<TSource>)Queryable.OrderBy(source, keySelector);
        }

        /// <summary>
        /// Sorts the elements of a sequence in descending order according to a key.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the function that is represented by keySelector.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>
        /// An <see cref="IOrderedMongoQueryable{TSource}"/> whose elements are sorted in descending order according to a key.
        /// </returns>
        public static IOrderedMongoQueryable<TSource> OrderByDescending<TSource, TKey>(this IMongoQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            return (IOrderedMongoQueryable<TSource>)Queryable.OrderByDescending(source, keySelector);
        }

        /// <summary>
        /// Projects each element of a sequence into a new form by incorporating the
        /// element's index.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TResult"> The type of the value returned by the function represented by selector.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>
        /// An <see cref="IMongoQueryable{TResult}"/> whose elements are the result of invoking a
        /// projection function on each element of source.
        /// </returns>
        public static IMongoQueryable<TResult> Select<TSource, TResult>(this IMongoQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
        {
            return (IMongoQueryable<TResult>)Queryable.Select(source, selector);
        }

        /// <summary>
        /// Projects each element of a sequence to an <see cref="IEnumerable{TResult}" /> and combines the resulting sequences into one sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the sequence returned by the function represented by <paramref name="selector" />.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>
        /// An <see cref="IMongoQueryable{TResult}" /> whose elements are the result of invoking a one-to-many projection function on each element of the input sequence.
        /// </returns>
        public static IMongoQueryable<TResult> SelectMany<TSource, TResult>(this IMongoQueryable<TSource> source, Expression<Func<TSource, IEnumerable<TResult>>> selector)
        {
            return (IMongoQueryable<TResult>)Queryable.SelectMany(source, selector);
        }

        /// <summary>
        /// Projects each element of a sequence to an <see cref="IEnumerable{TCollection}" /> and 
        /// invokes a result selector function on each element therein. The resulting values from 
        /// each intermediate sequence are combined into a single, one-dimensional sequence and returned.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by the function represented by <paramref name="collectionSelector" />.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="collectionSelector">A projection function to apply to each element of the input sequence.</param>
        /// <param name="resultSelector">A projection function to apply to each element of each intermediate sequence.</param>
        /// <returns>
        /// An <see cref="IMongoQueryable{TResult}" /> whose elements are the result of invoking the one-to-many projection function <paramref name="collectionSelector" /> on each element of <paramref name="source" /> and then mapping each of those sequence elements and their corresponding <paramref name="source" /> element to a result element.
        /// </returns>
        public static IMongoQueryable<TResult> SelectMany<TSource, TCollection, TResult>(this IMongoQueryable<TSource> source, Expression<Func<TSource, IEnumerable<TCollection>>> collectionSelector, Expression<Func<TSource, TCollection, TResult>> resultSelector)
        {
            return (IMongoQueryable<TResult>)Queryable.SelectMany(source, collectionSelector, resultSelector);
        }

        /// <summary>
        /// Returns the only element of a sequence, and throws an exception if there is not exactly one element in the sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IMongoQueryable{TSource}" /> to return the single element of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The single element of the input sequence.
        /// </returns>
        public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IMongoQueryProvider)source.Provider).ExecuteAsync<TSource>(
                Expression.Call(
                    null,
                    ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource) }),
                    Expression.Convert(source.Expression, typeof(IMongoQueryable<TSource>)),
                    Expression.Constant(cancellationToken)),
                cancellationToken);
        }

        /// <summary>
        /// Returns the only element of a sequence that satisfies a specified condition, and throws an exception if more than one such element exists.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IMongoQueryable{TSource}" /> to return a single element from.</param>
        /// <param name="predicate">A function to test an element for a condition.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The single element of the input sequence that satisfies the condition in <paramref name="predicate" />.
        /// </returns>
        public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IMongoQueryProvider)source.Provider).ExecuteAsync<TSource>(
                Expression.Call(
                    null,
                    ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource) }),
                    Expression.Convert(source.Expression, typeof(IMongoQueryable<TSource>)),
                    Expression.Quote(predicate),
                    Expression.Constant(cancellationToken)),
                cancellationToken);
        }

        /// <summary>
        /// Returns the only element of a sequence, or a default value if the sequence is empty; this method throws an exception if there is more than one element in the sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IMongoQueryable{TSource}" /> to return the single element of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The single element of the input sequence, or default(<typeparamref name="TSource" />) if the sequence contains no elements.
        /// </returns>
        public static Task<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IMongoQueryProvider)source.Provider).ExecuteAsync<TSource>(
                Expression.Call(
                    null,
                    ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource) }),
                    Expression.Convert(source.Expression, typeof(IMongoQueryable<TSource>)),
                    Expression.Constant(cancellationToken)),
                cancellationToken);
        }

        /// <summary>
        /// Returns the only element of a sequence that satisfies a specified condition or a default value if no such element exists; this method throws an exception if more than one element satisfies the condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IMongoQueryable{TSource}" /> to return a single element from.</param>
        /// <param name="predicate">A function to test an element for a condition.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The single element of the input sequence that satisfies the condition in <paramref name="predicate" />, or default(<typeparamref name="TSource" />) if no such element is found.
        /// </returns>
        public static Task<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IMongoQueryProvider)source.Provider).ExecuteAsync<TSource>(
                Expression.Call(
                    null,
                    ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource) }),
                    Expression.Convert(source.Expression, typeof(IMongoQueryable<TSource>)),
                    Expression.Quote(predicate),
                    Expression.Constant(cancellationToken)),
                cancellationToken);
        }

        /// <summary>
        /// Bypasses a specified number of elements in a sequence and then returns the
        /// remaining elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source</typeparam>
        /// <param name="source">An <see cref="IMongoQueryable{TSource}"/> to return elements from.</param>
        /// <param name="count">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>
        /// An <see cref="IMongoQueryable{TSource}"/> that contains elements that occur after the
        /// specified index in the input sequence.
        /// </returns>
        public static IMongoQueryable<TSource> Skip<TSource>(this IMongoQueryable<TSource> source, int count)
        {
            return (IMongoQueryable<TSource>)Queryable.Skip(source, count);
        }

        /// <summary>
        /// Computes the sum of a sequence of <see cref="Decimal"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        public static Task<decimal> SumAsync(this IMongoQueryable<decimal> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SumAsync<decimal>(source, cancellationToken);
        }

        /// <summary>
        /// Computes the sum of a sequence of <see cref="Nullable{Decimal}"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        public static Task<decimal?> SumAsync(this IMongoQueryable<decimal?> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SumAsync<decimal?>(source, cancellationToken);
        }

        /// <summary>
        /// Computes the sum of a sequence of <see cref="Double"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        public static Task<double> SumAsync(this IMongoQueryable<double> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SumAsync<double>(source, cancellationToken);
        }

        /// <summary>
        /// Computes the sum of a sequence of <see cref="Nullable{Double}"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        public static Task<double?> SumAsync(this IMongoQueryable<double?> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SumAsync<double?>(source, cancellationToken);
        }

        /// <summary>
        /// Computes the sum of a sequence of <see cref="Single"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        public static Task<float> SumAsync(this IMongoQueryable<float> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SumAsync<float>(source, cancellationToken);
        }

        /// <summary>
        /// Computes the sum of a sequence of <see cref="Nullable{Single}"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        public static Task<float?> SumAsync(this IMongoQueryable<float?> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SumAsync<float?>(source, cancellationToken);
        }

        /// <summary>
        /// Computes the sum of a sequence of <see cref="Int32"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        public static Task<int> SumAsync(this IMongoQueryable<int> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SumAsync<int>(source, cancellationToken);
        }

        /// <summary>
        /// Computes the sum of a sequence of <see cref="Nullable{Int32}"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        public static Task<int?> SumAsync(this IMongoQueryable<int?> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SumAsync<int?>(source, cancellationToken);
        }

        /// <summary>
        /// Computes the sum of a sequence of <see cref="Int64"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        public static Task<long> SumAsync(this IMongoQueryable<long> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SumAsync<long>(source, cancellationToken);
        }

        /// <summary>
        /// Computes the sum of a sequence of <see cref="Nullable{Int64}"/> values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the sum of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        public static Task<long?> SumAsync(this IMongoQueryable<long?> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SumAsync<long?>(source, cancellationToken);
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
        public static Task<decimal> SumAsync<TSource>(this IMongoQueryable<TSource> source, Expression<Func<TSource, decimal>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SumAsync<TSource, decimal>(source, selector, cancellationToken);
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
        public static Task<decimal?> SumAsync<TSource>(this IMongoQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SumAsync<TSource, decimal?>(source, selector, cancellationToken);
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
        public static Task<double> SumAsync<TSource>(this IMongoQueryable<TSource> source, Expression<Func<TSource, double>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SumAsync<TSource, double>(source, selector, cancellationToken);
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
        public static Task<double?> SumAsync<TSource>(this IMongoQueryable<TSource> source, Expression<Func<TSource, double?>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SumAsync<TSource, double?>(source, selector, cancellationToken);
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
        public static Task<float> SumAsync<TSource>(this IMongoQueryable<TSource> source, Expression<Func<TSource, float>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SumAsync<TSource, float>(source, selector, cancellationToken);
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
        public static Task<float?> SumAsync<TSource>(this IMongoQueryable<TSource> source, Expression<Func<TSource, float?>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SumAsync<TSource, float?>(source, selector, cancellationToken);
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
        public static Task<int> SumAsync<TSource>(this IMongoQueryable<TSource> source, Expression<Func<TSource, int>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SumAsync<TSource, int>(source, selector, cancellationToken);
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
        public static Task<int?> SumAsync<TSource>(this IMongoQueryable<TSource> source, Expression<Func<TSource, int?>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SumAsync<TSource, int?>(source, selector, cancellationToken);
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
        public static Task<long> SumAsync<TSource>(this IMongoQueryable<TSource> source, Expression<Func<TSource, long>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SumAsync<TSource, long>(source, selector, cancellationToken);
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
        public static Task<long?> SumAsync<TSource>(this IMongoQueryable<TSource> source, Expression<Func<TSource, long?>> selector, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SumAsync<TSource, long?>(source, selector, cancellationToken);
        }

        /// <summary>
        /// Returns a specified number of contiguous elements from the start of a sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The sequence to return elements from.</param>
        /// <param name="count">The number of elements to return.</param>
        /// <returns>
        /// An <see cref="IMongoQueryable{TSource}"/> that contains the specified number of elements
        /// from the start of source.
        /// </returns>
        public static IMongoQueryable<TSource> Take<TSource>(this IMongoQueryable<TSource> source, int count)
        {
            return (IMongoQueryable<TSource>)Queryable.Take(source, count);
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in a sequence in ascending
        /// order according to a key.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the function that is represented by keySelector.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>
        /// An <see cref="IOrderedMongoQueryable{TSource}"/> whose elements are sorted according to a key.
        /// </returns>
        public static IOrderedMongoQueryable<TSource> ThenBy<TSource, TKey>(this IOrderedMongoQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            return (IOrderedMongoQueryable<TSource>)Queryable.OrderBy(source, keySelector);
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in a sequence in descending
        /// order according to a key.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the function that is represented by keySelector.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>
        /// An <see cref="IOrderedMongoQueryable{TSource}"/> whose elements are sorted in descending order according to a key.
        /// </returns>
        public static IOrderedMongoQueryable<TSource> ThenByDescending<TSource, TKey>(this IOrderedMongoQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            return (IOrderedMongoQueryable<TSource>)Queryable.OrderByDescending(source, keySelector);
        }

        /// <summary>
        /// Filters a sequence of values based on a predicate.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IMongoQueryable{TSource}"/> to return elements from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>
        /// An <see cref="IMongoQueryable{TSource}"/> that contains elements from the input sequence
        /// that satisfy the condition specified by predicate.
        /// </returns>
        public static IMongoQueryable<TSource> Where<TSource>(this IMongoQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            return (IMongoQueryable<TSource>)Queryable.Where(source, predicate);
        }

        private static Task<TResult> AverageAsync<TSource, TResult>(this IMongoQueryable<TSource> source, CancellationToken cancellationToken)
        {
            return ((IMongoQueryProvider)source.Provider).ExecuteAsync<TResult>(
                Expression.Call(
                    null,
                    ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource), typeof(TResult) }),
                    Expression.Convert(source.Expression, typeof(IMongoQueryable<TSource>)),
                    Expression.Constant(cancellationToken)),
                cancellationToken);
        }

        private static Task<TResult> AverageAsync<TSource, TValue, TResult>(this IMongoQueryable<TSource> source, Expression<Func<TSource, TValue>> selector, CancellationToken cancellationToken)
        {
            return ((IMongoQueryProvider)source.Provider).ExecuteAsync<TResult>(
                Expression.Call(
                    null,
                    ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource), typeof(TValue), typeof(TResult) }),
                    Expression.Convert(source.Expression, typeof(IMongoQueryable<TSource>)),
                    Expression.Quote(selector),
                    Expression.Constant(cancellationToken)),
                cancellationToken);
        }

        private static Task<TSource> SumAsync<TSource>(this IMongoQueryable<TSource> source, CancellationToken cancellationToken)
        {
            return ((IMongoQueryProvider)source.Provider).ExecuteAsync<TSource>(
                Expression.Call(
                    null,
                    ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource) }),
                    Expression.Convert(source.Expression, typeof(IMongoQueryable<TSource>)),
                    Expression.Constant(cancellationToken)),
                cancellationToken);
        }

        private static Task<TValue> SumAsync<TSource, TValue>(this IMongoQueryable<TSource> source, Expression<Func<TSource, TValue>> selector, CancellationToken cancellationToken)
        {
            return ((IMongoQueryProvider)source.Provider).ExecuteAsync<TValue>(
                Expression.Call(
                    null,
                    ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource), typeof(TValue) }),
                    Expression.Convert(source.Expression, typeof(IMongoQueryable<TSource>)),
                    Expression.Quote(selector),
                    Expression.Constant(cancellationToken)),
                cancellationToken);
        }
    }
}
