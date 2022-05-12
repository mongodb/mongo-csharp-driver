﻿/* Copyright 2015-present MongoDB Inc.
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
using System.Linq.Expressions;
using MongoDB.Driver.Core.Misc;

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
            throw new NotSupportedException("This method is not functional. It is only usable in conjunction with MongoDB.");
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
            throw new NotSupportedException("This method is not functional. It is only usable in conjunction with MongoDB.");
        }

        /// <summary>
        /// Represents the first matching element in an array used in a query (corresponds to the server's "$" update operator).
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A source of values.</param>
        /// <returns>Only meant to be used in Update specifications.</returns>
        public static TSource FirstMatchingElement<TSource>(this IEnumerable<TSource> source)
        {
            throw new NotSupportedException("This method is not functional. It is only usable in conjunction with MongoDB.");
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
            throw new NotSupportedException("This method is not functional. It is only usable in conjunction with MongoDB.");
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
            throw new NotSupportedException("This method is not functional. It is only usable in conjunction with MongoDB.");
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
            throw new NotSupportedException("This method is not functional. It is only usable in conjunction with MongoDB.");
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
            throw new NotSupportedException("This method is not functional. It is only usable in conjunction with MongoDB.");
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
            throw new NotSupportedException("This method is not functional. It is only usable in conjunction with MongoDB.");
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
            throw new NotSupportedException("This method is not functional. It is only usable in conjunction with MongoDB.");
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
            throw new NotSupportedException("This method is not functional. It is only usable in conjunction with MongoDB.");
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
            throw new NotSupportedException("This method is not functional. It is only usable in conjunction with MongoDB.");
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
            throw new NotSupportedException("This method is not functional. It is only usable in conjunction with MongoDB.");
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
            throw new NotSupportedException("This method is not functional. It is only usable in conjunction with MongoDB.");
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
            return source.Select(selector).StandardDeviationPopulation();
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
            return source.Select(selector).StandardDeviationPopulation();
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
            return source.Select(selector).StandardDeviationPopulation();
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
            return source.Select(selector).StandardDeviationPopulation();
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
            return source.Select(selector).StandardDeviationPopulation();
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
            return source.Select(selector).StandardDeviationPopulation();
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
            return source.Select(selector).StandardDeviationPopulation();
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
            return source.Select(selector).StandardDeviationPopulation();
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
            return source.Select(selector).StandardDeviationPopulation();
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
            return source.Select(selector).StandardDeviationPopulation();
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
            throw new NotSupportedException("This method is not functional. It is only usable in conjunction with MongoDB.");
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
            throw new NotSupportedException("This method is not functional. It is only usable in conjunction with MongoDB.");
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
            throw new NotSupportedException("This method is not functional. It is only usable in conjunction with MongoDB.");
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
            throw new NotSupportedException("This method is not functional. It is only usable in conjunction with MongoDB.");
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
            throw new NotSupportedException("This method is not functional. It is only usable in conjunction with MongoDB.");
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
            throw new NotSupportedException("This method is not functional. It is only usable in conjunction with MongoDB.");
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
            throw new NotSupportedException("This method is not functional. It is only usable in conjunction with MongoDB.");
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
            throw new NotSupportedException("This method is not functional. It is only usable in conjunction with MongoDB.");
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
            throw new NotSupportedException("This method is not functional. It is only usable in conjunction with MongoDB.");
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
            throw new NotSupportedException("This method is not functional. It is only usable in conjunction with MongoDB.");
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
            return source.Select(selector).StandardDeviationSample();
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
            return source.Select(selector).StandardDeviationSample();
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
            return source.Select(selector).StandardDeviationSample();
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
            return source.Select(selector).StandardDeviationSample();
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
            return source.Select(selector).StandardDeviationSample();
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
            return source.Select(selector).StandardDeviationSample();
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
            return source.Select(selector).StandardDeviationSample();
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
            return source.Select(selector).StandardDeviationSample();
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
            return source.Select(selector).StandardDeviationSample();
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
            return source.Select(selector).StandardDeviationSample();
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
            throw new NotSupportedException("This method is not functional. It is only usable in conjunction with MongoDB.");
        }
    }
}
