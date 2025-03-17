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
*
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// Enumerable Extensions for MongoDB.
    /// </summary>
    public static class MongoEnumerable
    {
        internal static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            Ensure.IsNotNull(source, nameof(source));

            return new HashSet<T>(source);
        }

        /// <summary>
        /// Represents all elements in an array (corresponds to the server's "$[]" update operator).
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A source of values.</param>
        /// <returns>Only meant to be used in Update specifications.</returns>
        public static TSource AllElements<TSource>(this IEnumerable<TSource> source)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Represents all matching elements in an array when using an array filter (corresponds to the server's "$[identifier]" update operator).
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A source of values.</param>
        /// <param name="identifier">The name of the identifier in the corresponding array filter.</param>
        /// <returns>Only meant to be used in Update specifications.</returns>
        public static TSource AllMatchingElements<TSource>(this IEnumerable<TSource> source, string identifier)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Returns the bottom result.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="sortBy">The sort order.</param>
        /// <param name="selector">The selector.</param>
        /// <returns>The bottom result.</returns>
        public static TResult Bottom<TSource, TResult>(
            this IEnumerable<TSource> source,
            SortDefinition<TSource> sortBy,
            Func<TSource, TResult> selector)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Returns the bottom n results.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="sortBy">The sort order.</param>
        /// <param name="selector">The selector.</param>
        /// <param name="n">The number of results to return.</param>
        /// <returns>The bottom n results.</returns>
        public static IEnumerable<TResult> BottomN<TSource, TResult>(
            this IEnumerable<TSource> source,
            SortDefinition<TSource> sortBy,
            Func<TSource, TResult> selector,
            int n)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Returns the bottom n results.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="sortBy">The sort order.</param>
        /// <param name="selector">The selector.</param>
        /// <param name="key">The key (needed to infer the key type and to determine the key serializer).</param>
        /// <param name="n">The number of results to return.</param>
        /// <returns>The bottom n results.</returns>
        public static IEnumerable<TResult> BottomN<TSource, TKey, TResult>(
            this IEnumerable<TSource> source,
            SortDefinition<TSource> sortBy,
            Func<TSource, TResult> selector,
            TKey key,
            Func<TKey, int> n)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Represents the first matching element in an array used in a query (corresponds to the server's "$" update operator).
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A source of values.</param>
        /// <returns>Only meant to be used in Update specifications.</returns>
        public static TSource FirstMatchingElement<TSource>(this IEnumerable<TSource> source)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Returns the first n results.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="selector">The selector.</param>
        /// <param name="n">The number of results to return.</param>
        /// <returns>The first n results.</returns>
        public static IEnumerable<TResult> FirstN<TSource, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, TResult> selector,
            int n)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Returns the first n results.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="selector">The selector.</param>
        /// <param name="key">The key (needed to infer the key type and to determine the key serializer).</param>
        /// <param name="n">The number of results to return.</param>
        /// <returns>The first n results.</returns>
        public static IEnumerable<TResult> FirstN<TSource, TKey, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, TResult> selector,
            TKey key,
            Func<TKey, int> n)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Returns the last n results.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="selector">The selector.</param>
        /// <param name="n">The number of results to return.</param>
        /// <returns>The last n results.</returns>
        public static IEnumerable<TResult> LastN<TSource, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, TResult> selector,
            int n)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Returns the last n results.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="selector">The selector.</param>
        /// <param name="key">The key (needed to infer the key type and to determine the key serializer).</param>
        /// <param name="n">The number of results to return.</param>
        /// <returns>The last n results.</returns>
        public static IEnumerable<TResult> LastN<TSource, TKey, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, TResult> selector,
            TKey key,
            Func<TKey, int> n)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Returns the max n results.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="selector">The selector.</param>
        /// <param name="n">The number of results to return.</param>
        /// <returns>The max n results.</returns>
        public static IEnumerable<TResult> MaxN<TSource, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, TResult> selector,
            int n)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Returns the max n results.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="selector">The selector.</param>
        /// <param name="key">The key (needed to infer the key type and to determine the key serializer).</param>
        /// <param name="n">The number of results to return.</param>
        /// <returns>The max n results.</returns>
        public static IEnumerable<TResult> MaxN<TSource, TKey, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, TResult> selector,
            TKey key,
            Func<TKey, int> n)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Returns the min n results.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="selector">The selector.</param>
        /// <param name="n">The number of results to return.</param>
        /// <returns>The min n results.</returns>
        public static IEnumerable<TResult> MinN<TSource, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, TResult> selector,
            int n)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Returns the min n results.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="selector">The selector.</param>
        /// <param name="key">The key (needed to infer the key type and to determine the key serializer).</param>
        /// <param name="n">The number of results to return.</param>
        /// <returns>The min n results.</returns>
        public static IEnumerable<TResult> MinN<TSource, TKey, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, TResult> selector,
            TKey key,
            Func<TKey, int> n)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double StandardDeviationPopulation(this IEnumerable<int> source)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double? StandardDeviationPopulation(this IEnumerable<int?> source)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double StandardDeviationPopulation(this IEnumerable<long> source)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double? StandardDeviationPopulation(this IEnumerable<long?> source)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static float StandardDeviationPopulation(this IEnumerable<float> source)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static float? StandardDeviationPopulation(this IEnumerable<float?> source)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double StandardDeviationPopulation(this IEnumerable<double> source)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double? StandardDeviationPopulation(this IEnumerable<double?> source)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static decimal StandardDeviationPopulation(this IEnumerable<decimal> source)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Computes the population standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static decimal? StandardDeviationPopulation(this IEnumerable<decimal?> source)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
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
        public static double StandardDeviationPopulation<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
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
        public static double? StandardDeviationPopulation<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
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
        public static double StandardDeviationPopulation<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
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
        public static double? StandardDeviationPopulation<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
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
        public static float StandardDeviationPopulation<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
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
        public static float? StandardDeviationPopulation<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
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
        public static double StandardDeviationPopulation<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
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
        public static double? StandardDeviationPopulation<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
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
        public static decimal StandardDeviationPopulation<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
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
        public static decimal? StandardDeviationPopulation<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double StandardDeviationSample(this IEnumerable<int> source)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double? StandardDeviationSample(this IEnumerable<int?> source)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double StandardDeviationSample(this IEnumerable<long> source)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double? StandardDeviationSample(this IEnumerable<long?> source)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static float StandardDeviationSample(this IEnumerable<float> source)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static float? StandardDeviationSample(this IEnumerable<float?> source)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double StandardDeviationSample(this IEnumerable<double> source)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static double? StandardDeviationSample(this IEnumerable<double?> source)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static decimal StandardDeviationSample(this IEnumerable<decimal> source)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Computes the sample standard deviation of a sequence of values.
        /// </summary>
        /// <param name="source">A sequence of values to calculate the population standard deviation of.</param>
        /// <returns>
        /// The population standard deviation of the sequence of values.
        /// </returns>
        public static decimal? StandardDeviationSample(this IEnumerable<decimal?> source)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
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
        public static double StandardDeviationSample<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
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
        public static double? StandardDeviationSample<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
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
        public static double StandardDeviationSample<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
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
        public static double? StandardDeviationSample<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
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
        public static float StandardDeviationSample<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
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
        public static float? StandardDeviationSample<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
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
        public static double StandardDeviationSample<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
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
        public static double? StandardDeviationSample<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
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
        public static decimal StandardDeviationSample<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
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
        public static decimal? StandardDeviationSample<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Returns the top n results.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="sortBy">The sort order.</param>
        /// <param name="selector">The selector.</param>
        /// <returns>The top n results.</returns>
        public static TResult Top<TSource, TResult>(
            this IEnumerable<TSource> source,
            SortDefinition<TSource> sortBy,
            Func<TSource, TResult> selector)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Returns the top n results.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="sortBy">The sort order.</param>
        /// <param name="selector">The selector.</param>
        /// <param name="n">The number of results to return.</param>
        /// <returns>The top n results.</returns>
        public static IEnumerable<TResult> TopN<TSource, TResult>(
            this IEnumerable<TSource> source,
            SortDefinition<TSource> sortBy,
            Func<TSource, TResult> selector,
            int n)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Returns the top n results.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="sortBy">The sort order.</param>
        /// <param name="selector">The selector.</param>
        /// <param name="key">The key (needed to infer the key type and to determine the key serializer).</param>
        /// <param name="n">The number of results to return.</param>
        /// <returns>The top n results.</returns>
        public static IEnumerable<TResult> TopN<TSource, TKey, TResult>(
            this IEnumerable<TSource> source,
            SortDefinition<TSource> sortBy,
            Func<TSource, TResult> selector,
            TKey key,
            Func<TKey, int> n)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Filters a sequence of values based on a predicate and limits the number of results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The source values.</param>
        /// <param name="predicate">The predicate.</param>
        /// <param name="limit">The limit.</param>
        /// <returns>The filtered results.</returns>
        public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, int limit)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }
    }
}
