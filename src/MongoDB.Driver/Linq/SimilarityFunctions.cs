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
 */

using System;
using System.Collections.Generic;

namespace MongoDB.Driver.Linq;

/// <summary>
/// Methods to use for similarity functions in LINQ queries. These methods can only be used when translating
/// C# to Mongo Query Language (MQL) for execution in the MongoDB database. Calling the method directly is
/// not supported.
/// </summary>
public static class SimilarityFunctions
{
    /// <summary>
    /// Translated to the "$similarityDotProduct" operator in MQL to measure the similarity between two vectors.
    /// </summary>
    /// <param name="vector1">The first vector to compare. Must have the same length as the second vector.</param>
    /// <param name="vector2">The second vector to compare. Must have the same length as the first vector.</param>
    /// <param name="normalizeScore">Whether to normalize the result for use as a vector search score.</param>
    /// <typeparam name="TElement">The vector element type</typeparam>
    /// <returns>The dot-product measure between the two vectors.</returns>
    /// <exception cref="InvalidOperationException">if executed.</exception>
    public static double DotProduct<TElement>(
        IEnumerable<TElement> vector1,
        IEnumerable<TElement> vector2,
        bool normalizeScore)
        => Throw(nameof(DotProduct));

    /// <summary>
    /// Translated to the "$similarityDotProduct" operator in MQL to measure the similarity between two vectors.
    /// </summary>
    /// <param name="vector1">The first vector to compare. Must have the same length as the second vector.</param>
    /// <param name="vector2">The second vector to compare. Must have the same length as the first vector.</param>
    /// <param name="normalizeScore">Whether to normalize the result for use as a vector search score.</param>
    /// <typeparam name="TElement">The vector element type</typeparam>
    /// <returns>The dot-product measure between the two vectors.</returns>
    /// <exception cref="InvalidOperationException">if executed.</exception>
    public static double DotProduct<TElement>(
        ReadOnlyMemory<TElement> vector1,
        ReadOnlyMemory<TElement> vector2,
        bool normalizeScore)
        => Throw(nameof(DotProduct));

    /// <summary>
    /// Translated to the "$similarityCosine" operator in MQL to measure the similarity between two vectors.
    /// </summary>
    /// <param name="vector1">The first vector to compare. Must have the same length as the second vector.</param>
    /// <param name="vector2">The second vector to compare. Must have the same length as the first vector.</param>
    /// <param name="normalizeScore">Whether to normalize the result for use as a vector search score.</param>
    /// <typeparam name="TElement">The vector element type</typeparam>
    /// <returns>The cosine measure between the two vectors.</returns>
    /// <exception cref="InvalidOperationException">if executed.</exception>
    public static double Cosine<TElement>(
        IEnumerable<TElement> vector1,
        IEnumerable<TElement> vector2,
        bool normalizeScore)
        => Throw(nameof(Cosine));

    /// <summary>
    /// Translated to the "$similarityCosine" operator in MQL to measure the similarity between two vectors.
    /// </summary>
    /// <param name="vector1">The first vector to compare. Must have the same length as the second vector.</param>
    /// <param name="vector2">The second vector to compare. Must have the same length as the first vector.</param>
    /// <param name="normalizeScore">Whether to normalize the result for use as a vector search score.</param>
    /// <typeparam name="TElement">The vector element type</typeparam>
    /// <returns>The cosine measure between the two vectors.</returns>
    /// <exception cref="InvalidOperationException">if executed.</exception>
    public static double Cosine<TElement>(
        ReadOnlyMemory<TElement> vector1,
        ReadOnlyMemory<TElement> vector2,
        bool normalizeScore)
        => Throw(nameof(Cosine));

    /// <summary>
    /// Translated to the "$similarityEuclidean" operator in MQL to measure the similarity between two vectors.
    /// </summary>
    /// <param name="vector1">The first vector to compare. Must have the same length as the second vector.</param>
    /// <param name="vector2">The second vector to compare. Must have the same length as the first vector.</param>
    /// <param name="normalizeScore">Whether to normalize the result for use as a vector search score.</param>
    /// <typeparam name="TElement">The vector element type</typeparam>
    /// <returns>The Euclidean measure between the two vectors.</returns>
    /// <exception cref="InvalidOperationException">if executed.</exception>
    public static double Euclidean<TElement>(
        IEnumerable<TElement> vector1,
        IEnumerable<TElement> vector2,
        bool normalizeScore)
        => Throw(nameof(Euclidean));

    /// <summary>
    /// Translated to the "$similarityEuclidean" operator in MQL to measure the similarity between two vectors.
    /// </summary>
    /// <param name="vector1">The first vector to compare. Must have the same length as the second vector.</param>
    /// <param name="vector2">The second vector to compare. Must have the same length as the first vector.</param>
    /// <param name="normalizeScore">Whether to normalize the result for use as a vector search score.</param>
    /// <typeparam name="TElement">The vector element type</typeparam>
    /// <returns>The Euclidean measure between the two vectors.</returns>
    /// <exception cref="InvalidOperationException">if executed.</exception>
    public static double Euclidean<TElement>(
        ReadOnlyMemory<TElement> vector1,
        ReadOnlyMemory<TElement> vector2,
        bool normalizeScore)
        => Throw(nameof(Euclidean));

    private static double Throw(string methodName)
        => throw new NotSupportedException(
            $"Local evaluation of '{nameof(SimilarityFunctions)}.{methodName}' is not supported. " +
            "This method can only be used when translating C# to Mongo Query Language (MQL) for execution in the MongoDB database.");
}
