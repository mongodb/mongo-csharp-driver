/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Bson;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// Extension methods that represent operations for SetWindowFields.
    /// </summary>
    public static class ISetWindowFieldsPartitionExtensions
    {
        /// <summary>
        /// Returns a set.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <typeparam name="TValue">The type of the selected values.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The set of the selected values.</returns>
        public static IEnumerable<TValue> AddToSet<TInput, TValue>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, TValue> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the average value of the numeric values. Average ignores non-numeric values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The average of the selected values.</returns>
        public static decimal Average<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, decimal> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the average value of the numeric values. Average ignores non-numeric values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The average of the selected values.</returns>
        public static decimal? Average<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, decimal?> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the average value of the numeric values. Average ignores non-numeric values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The average of the selected values.</returns>
        public static double Average<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, double> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the average value of the numeric values. Average ignores non-numeric values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The average of the selected values.</returns>
        public static double? Average<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, double?> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the average value of the numeric values. Average ignores non-numeric values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The average of the selected values.</returns>
        public static float Average<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, float> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the average value of the numeric values. Average ignores non-numeric values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The average of the selected values.</returns>
        public static float? Average<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, float?> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the average value of the numeric values. Average ignores non-numeric values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The average of the selected values.</returns>
        public static double Average<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, int> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the average value of the numeric values. Average ignores non-numeric values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The average of the selected values.</returns>
        public static double? Average<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, int?> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the average value of the numeric values. Average ignores non-numeric values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The average of the selected values.</returns>
        public static double Average<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, long> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the average value of the numeric values. Average ignores non-numeric values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The average of the selected values.</returns>
        public static double? Average<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, long?> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the number of documents in the window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The maximum of the selected values.</returns>
        public static long Count<TInput>(this ISetWindowFieldsPartition<TInput> partition, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the population covariance of two numeric expressions that are evaluated using documents in the partition window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector1">The selector that defines the first expression.</param>
        /// <param name="selector2">The selector that defines the second expression.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The maximum of the selected values.</returns>
        public static decimal CovariancePopulation<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, decimal> selector1, Func<TInput, decimal> selector2, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the population covariance of two numeric expressions that are evaluated using documents in the partition window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector1">The selector that defines the first expression.</param>
        /// <param name="selector2">The selector that defines the second expression.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The maximum of the selected values.</returns>
        public static decimal? CovariancePopulation<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, decimal?> selector1, Func<TInput, decimal?> selector2, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the population covariance of two numeric expressions that are evaluated using documents in the partition window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector1">The selector that defines the first expression.</param>
        /// <param name="selector2">The selector that defines the second expression.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The maximum of the selected values.</returns>
        public static double CovariancePopulation<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, double> selector1, Func<TInput, double> selector2, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the population covariance of two numeric expressions that are evaluated using documents in the partition window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector1">The selector that defines the first expression.</param>
        /// <param name="selector2">The selector that defines the second expression.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The maximum of the selected values.</returns>
        public static double? CovariancePopulation<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, double?> selector1, Func<TInput, double?> selector2, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the population covariance of two numeric expressions that are evaluated using documents in the partition window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector1">The selector that defines the first expression.</param>
        /// <param name="selector2">The selector that defines the second expression.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The maximum of the selected values.</returns>
        public static double CovariancePopulation<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, int> selector1, Func<TInput, int> selector2, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the population covariance of two numeric expressions that are evaluated using documents in the partition window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector1">The selector that defines the first expression.</param>
        /// <param name="selector2">The selector that defines the second expression.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The maximum of the selected values.</returns>
        public static double? CovariancePopulation<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, int?> selector1, Func<TInput, int?> selector2, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the population covariance of two numeric expressions that are evaluated using documents in the partition window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector1">The selector that defines the first expression.</param>
        /// <param name="selector2">The selector that defines the second expression.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The maximum of the selected values.</returns>
        public static double CovariancePopulation<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, long> selector1, Func<TInput, long> selector2, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the population covariance of two numeric expressions that are evaluated using documents in the partition window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector1">The selector that defines the first expression.</param>
        /// <param name="selector2">The selector that defines the second expression.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The maximum of the selected values.</returns>
        public static double? CovariancePopulation<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, long?> selector1, Func<TInput, long?> selector2, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the population covariance of two numeric expressions that are evaluated using documents in the partition window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector1">The selector that defines the first expression.</param>
        /// <param name="selector2">The selector that defines the second expression.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The maximum of the selected values.</returns>
        public static float CovariancePopulation<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, float> selector1, Func<TInput, float> selector2, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the population covariance of two numeric expressions that are evaluated using documents in the partition window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector1">The selector that defines the first expression.</param>
        /// <param name="selector2">The selector that defines the second expression.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The maximum of the selected values.</returns>
        public static float? CovariancePopulation<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, float?> selector1, Func<TInput, float?> selector2, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the sample covariance of two numeric expressions that are evaluated using documents in the partition window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector1">The selector that defines the first expression.</param>
        /// <param name="selector2">The selector that defines the second expression.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The maximum of the selected values.</returns>
        public static decimal CovarianceSample<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, decimal> selector1, Func<TInput, decimal> selector2, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the sample covariance of two numeric expressions that are evaluated using documents in the partition window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector1">The selector that defines the first expression.</param>
        /// <param name="selector2">The selector that defines the second expression.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The maximum of the selected values.</returns>
        public static decimal? CovarianceSample<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, decimal?> selector1, Func<TInput, decimal?> selector2, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the sample covariance of two numeric expressions that are evaluated using documents in the partition window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector1">The selector that defines the first expression.</param>
        /// <param name="selector2">The selector that defines the second expression.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The maximum of the selected values.</returns>
        public static double CovarianceSample<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, double> selector1, Func<TInput, double> selector2, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the sample covariance of two numeric expressions that are evaluated using documents in the partition window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector1">The selector that defines the first expression.</param>
        /// <param name="selector2">The selector that defines the second expression.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The maximum of the selected values.</returns>
        public static double? CovarianceSample<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, double?> selector1, Func<TInput, double?> selector2, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the sample covariance of two numeric expressions that are evaluated using documents in the partition window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector1">The selector that defines the first expression.</param>
        /// <param name="selector2">The selector that defines the second expression.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The maximum of the selected values.</returns>
        public static double CovarianceSample<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, int> selector1, Func<TInput, int> selector2, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the sample covariance of two numeric expressions that are evaluated using documents in the partition window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector1">The selector that defines the first expression.</param>
        /// <param name="selector2">The selector that defines the second expression.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The maximum of the selected values.</returns>
        public static double? CovarianceSample<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, int?> selector1, Func<TInput, int?> selector2, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the sample covariance of two numeric expressions that are evaluated using documents in the partition window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector1">The selector that defines the first expression.</param>
        /// <param name="selector2">The selector that defines the second expression.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The maximum of the selected values.</returns>
        public static double CovarianceSample<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, long> selector1, Func<TInput, long> selector2, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the sample covariance of two numeric expressions that are evaluated using documents in the partition window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector1">The selector that defines the first expression.</param>
        /// <param name="selector2">The selector that defines the second expression.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The maximum of the selected values.</returns>
        public static double? CovarianceSample<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, long?> selector1, Func<TInput, long?> selector2, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the sample covariance of two numeric expressions that are evaluated using documents in the partition window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector1">The selector that defines the first expression.</param>
        /// <param name="selector2">The selector that defines the second expression.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The maximum of the selected values.</returns>
        public static float CovarianceSample<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, float> selector1, Func<TInput, float> selector2, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the sample covariance of two numeric expressions that are evaluated using documents in the partition window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector1">The selector that defines the first expression.</param>
        /// <param name="selector2">The selector that defines the second expression.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The maximum of the selected values.</returns>
        public static float? CovarianceSample<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, float?> selector1, Func<TInput, float?> selector2, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the document position (known as the rank) relative to other documents in the $setWindowFields stage partition.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <returns>The document position.</returns>
        public static decimal DenseRank<TInput>(this ISetWindowFieldsPartition<TInput> partition)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the average rate of change within the specified window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The average rate of change within the specified window.</returns>
        public static decimal Derivative<TInput> (this ISetWindowFieldsPartition<TInput> partition, Func<TInput, decimal> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the average rate of change within the specified window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="unit">The unit for time based derivatives.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The average rate of change within the specified window.</returns>
        public static decimal Derivative<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, decimal> selector, WindowTimeUnit unit, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the average rate of change within the specified window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The average rate of change within the specified window.</returns>
        public static double Derivative<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, double> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the average rate of change within the specified window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="unit">The unit for time based derivatives.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The average rate of change within the specified window.</returns>
        public static double Derivative<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, double> selector, WindowTimeUnit unit, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the average rate of change within the specified window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The average rate of change within the specified window.</returns>
        public static double Derivative<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, float> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the average rate of change within the specified window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="unit">The unit for time based derivatives.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The average rate of change within the specified window.</returns>
        public static double Derivative<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, float> selector, WindowTimeUnit unit, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the average rate of change within the specified window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The average rate of change within the specified window.</returns>
        public static double Derivative<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, int> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the average rate of change within the specified window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="unit">The unit for time based derivatives.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The average rate of change within the specified window.</returns>
        public static double Derivative<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, int> selector, WindowTimeUnit unit, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the average rate of change within the specified window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The average rate of change within the specified window.</returns>
        public static double Derivative<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, long> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the average rate of change within the specified window.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="unit">The unit for time based derivatives.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The average rate of change within the specified window.</returns>
        public static double Derivative<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, long> selector, WindowTimeUnit unit, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the position of a document (known as the document number) in the $setWindowFields stage partition.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <returns>The document position.</returns>
        public static decimal DocumentNumber<TInput>(this ISetWindowFieldsPartition<TInput> partition)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the exponential moving average value of the numeric values. Ignores non-numeric values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="weighting">How to weigh the values when computing the exponential moving average.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The exponential moving average of the selected values.</returns>
        public static decimal ExponentialMovingAverage<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, decimal> selector, ExponentialMovingAverageWeighting weighting, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the exponential moving average value of the numeric values. Ignores non-numeric values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="weighting">How to weigh the values when computing the exponential moving average.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The exponential moving average of the selected values.</returns>
        public static double ExponentialMovingAverage<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, double> selector, ExponentialMovingAverageWeighting weighting, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the exponential moving average value of the numeric values. Ignores non-numeric values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="weighting">How to weigh the values when computing the exponential moving average.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The exponential moving average of the selected values.</returns>
        public static double ExponentialMovingAverage<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, int> selector, ExponentialMovingAverageWeighting weighting, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the exponential moving average value of the numeric values. Ignores non-numeric values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="weighting">How to weigh the values when computing the exponential moving average.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The exponential moving average of the selected values.</returns>
        public static double ExponentialMovingAverage<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, long> selector, ExponentialMovingAverageWeighting weighting, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the exponential moving average value of the numeric values. Ignores non-numeric values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="weighting">How to weigh the values when computing the exponential moving average.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The exponential moving average of the selected values.</returns>
        public static float ExponentialMovingAverage<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, float> selector, ExponentialMovingAverageWeighting weighting, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the first value.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <typeparam name="TValue">The type of the selected values.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The first of the selected values.</returns>
        public static TValue First<TInput, TValue>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, TValue> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the approximation of the area under a curve.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The approximation of the area under a curve.</returns>
        public static decimal Integral<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, decimal> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the approximation of the area under a curve.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="unit">The unit for time based integrals.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The approximation of the area under a curve.</returns>
        public static decimal Integral<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, decimal> selector, WindowTimeUnit unit, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the approximation of the area under a curve.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The approximation of the area under a curve.</returns>
        public static double Integral<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, double> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the approximation of the area under a curve.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="unit">The unit for time based integrals.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The approximation of the area under a curve.</returns>
        public static double Integral<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, double> selector, WindowTimeUnit unit, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the approximation of the area under a curve.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The approximation of the area under a curve.</returns>
        public static double Integral<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, float> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the approximation of the area under a curve.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="unit">The unit for time based integrals.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The approximation of the area under a curve.</returns>
        public static double Integral<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, float> selector, WindowTimeUnit unit, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the approximation of the area under a curve.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The approximation of the area under a curve.</returns>
        public static double Integral<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, int> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the approximation of the area under a curve.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="unit">The unit for time based integrals.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The approximation of the area under a curve.</returns>
        public static double Integral<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, int> selector, WindowTimeUnit unit, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the approximation of the area under a curve.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The approximation of the area under a curve.</returns>
        public static double Integral<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, long> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the approximation of the area under a curve.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="unit">The unit for time based integrals.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The approximation of the area under a curve.</returns>
        public static double Integral<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, long> selector, WindowTimeUnit unit, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the last value.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <typeparam name="TValue">The type of the selected values.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The last of the selected values.</returns>
        public static TValue Last<TInput, TValue>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, TValue> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the last observation carried forward.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <typeparam name="TValue">The type of the selected values.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The last observation carried forward.</returns>
        public static TValue Locf<TInput, TValue>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, TValue> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the maximum value.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <typeparam name="TValue">The type of the selected values.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The maximum of the selected values.</returns>
        public static TValue Max<TInput, TValue>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, TValue> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the minimum value.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <typeparam name="TValue">The type of the selected values.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The minimum of the selected values.</returns>
        public static TValue Min<TInput, TValue>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, TValue> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns a sequence of values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <typeparam name="TValue">The type of the selected values.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>A sequence of the selected values.</returns>
        public static IEnumerable<TValue> Push<TInput, TValue>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, TValue> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the document position (known as the rank) relative to other documents in the $setWindowFields stage partition.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <returns>The document position.</returns>
        public static decimal Rank<TInput>(this ISetWindowFieldsPartition<TInput> partition)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the value from an expression applied to a document in a specified position relative to the current document.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <typeparam name="TValue">The type of the selected values.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="by">The relative position of the document to evaluate the selector on to shift a value to this document.</param>
        /// <returns>The value from an expression applied to a document in a specified position relative to the current document.</returns>
        public static TValue Shift<TInput, TValue>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, TValue> selector, int by)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the value from an expression applied to a document in a specified position relative to the current document.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <typeparam name="TValue">The type of the selected values.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="by">The relative position of the document to evaluate the selector on to shift a value to this document.</param>
        /// <param name="defaultValue">The default value to use if the document position is outside the partition.</param>
        /// <returns>The value from an expression applied to a document in a specified position relative to the current document.</returns>
        public static TValue Shift<TInput, TValue>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, TValue> selector, int by, TValue defaultValue)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Calculates the population standard deviation of the input values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The population standard deviation of the selected values.</returns>
        public static decimal StandardDeviationPopulation<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, decimal> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Calculates the population standard deviation of the input values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The population standard deviation of the selected values.</returns>
        public static decimal? StandardDeviationPopulation<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, decimal?> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Calculates the population standard deviation of the input values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The population standard deviation of the selected values.</returns>
        public static double StandardDeviationPopulation<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, double> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Calculates the population standard deviation of the input values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The population standard deviation of the selected values.</returns>
        public static double? StandardDeviationPopulation<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, double?> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Calculates the population standard deviation of the input values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The population standard deviation of the selected values.</returns>
        public static double StandardDeviationPopulation<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, int> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Calculates the population standard deviation of the input values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The population standard deviation of the selected values.</returns>
        public static double? StandardDeviationPopulation<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, int?> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Calculates the population standard deviation of the input values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The population standard deviation of the selected values.</returns>
        public static double StandardDeviationPopulation<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, long> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Calculates the population standard deviation of the input values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The population standard deviation of the selected values.</returns>
        public static double? StandardDeviationPopulation<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, long?> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Calculates the population standard deviation of the input values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The population standard deviation of the selected values.</returns>
        public static float StandardDeviationPopulation<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, float> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Calculates the population standard deviation of the input values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The population standard deviation of the selected values.</returns>
        public static float? StandardDeviationPopulation<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, float?> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Calculates the sample standard deviation of the input values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The sample standard deviation of the selected values.</returns>
        public static decimal StandardDeviationSample<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, decimal> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Calculates the sample standard deviation of the input values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The sample standard deviation of the selected values.</returns>
        public static decimal? StandardDeviationSample<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, decimal?> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Calculates the sample standard deviation of the input values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The sample standard deviation of the selected values.</returns>
        public static double StandardDeviationSample<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, double> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Calculates the sample standard deviation of the input values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The sample standard deviation of the selected values.</returns>
        public static double? StandardDeviationSample<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, double?> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Calculates the sample standard deviation of the input values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The sample standard deviation of the selected values.</returns>
        public static double StandardDeviationSample<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, int> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Calculates the sample standard deviation of the input values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The sample standard deviation of the selected values.</returns>
        public static double? StandardDeviationSample<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, int?> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Calculates the sample standard deviation of the input values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The sample standard deviation of the selected values.</returns>
        public static double StandardDeviationSample<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, long> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Calculates the sample standard deviation of the input values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The sample standard deviation of the selected values.</returns>
        public static double? StandardDeviationSample<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, long?> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Calculates the sample standard deviation of the input values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The sample standard deviation of the selected values.</returns>
        public static float StandardDeviationSample<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, float> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Calculates the sample standard deviation of the input values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The sample standard deviation of the selected values.</returns>
        public static float? StandardDeviationSample<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, float?> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the sum of numeric values. $sum ignores non-numeric values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The sum of the values.</returns>
        public static decimal Sum<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, decimal> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the sum of numeric values. $sum ignores non-numeric values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The sum of the values.</returns>
        public static decimal? Sum<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, decimal?> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the sum of numeric values. $sum ignores non-numeric values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The sum of the values.</returns>
        public static double Sum<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, double> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the sum of numeric values. $sum ignores non-numeric values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The sum of the values.</returns>
        public static double? Sum<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, double?> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the sum of numeric values. $sum ignores non-numeric values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The sum of the values.</returns>
        public static float Sum<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, float> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the sum of numeric values. $sum ignores non-numeric values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The sum of the values.</returns>
        public static float? Sum<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, float?> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the sum of numeric values. $sum ignores non-numeric values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The sum of the values.</returns>
        public static long Sum<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, int> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the sum of numeric values. $sum ignores non-numeric values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The sum of the values.</returns>
        public static long? Sum<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, int?> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the sum of numeric values. $sum ignores non-numeric values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The sum of the values.</returns>
        public static long Sum<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, long> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }

        /// <summary>
        /// Returns the sum of numeric values. $sum ignores non-numeric values.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents in the partition.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="selector">The selector that selects a value from the input document.</param>
        /// <param name="window">The window boundaries.</param>
        /// <returns>The sum of the values.</returns>
        public static long? Sum<TInput>(this ISetWindowFieldsPartition<TInput> partition, Func<TInput, long?> selector, SetWindowFieldsWindow window = null)
        {
            throw new InvalidOperationException("This method is only intended to be used with SetWindowFields.");
        }
    }
}
