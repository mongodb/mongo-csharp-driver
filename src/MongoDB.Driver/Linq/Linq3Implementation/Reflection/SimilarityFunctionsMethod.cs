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

namespace MongoDB.Driver.Linq.Linq3Implementation.Reflection;

internal static class SimilarityFunctionsMethod
{
    // sets of methods
    private static readonly IReadOnlyMethodInfoSet __similarityFunctionOverloads;

    // static constructor
    static SimilarityFunctionsMethod()
    {
        // initialize methods before sets of methods
        var dotProductEnumerable = ReflectionInfo.Method((IEnumerable<object> vectors1, IEnumerable<object> vectors2, bool normalizeScore) => SimilarityFunctions.DotProduct(vectors1, vectors2, normalizeScore));
        var dotProductMemory = ReflectionInfo.Method((ReadOnlyMemory<object> vectors1, ReadOnlyMemory<object> vectors2, bool normalizeScore) => SimilarityFunctions.DotProduct(vectors1, vectors2, normalizeScore));
        var euclideanEnumerable = ReflectionInfo.Method((IEnumerable<object> vectors1, IEnumerable<object> vectors2, bool normalizeScore) => SimilarityFunctions.Euclidean(vectors1, vectors2, normalizeScore));
        var euclideanMemory = ReflectionInfo.Method((ReadOnlyMemory<object> vectors1, ReadOnlyMemory<object> vectors2, bool normalizeScore) => SimilarityFunctions.Euclidean(vectors1, vectors2, normalizeScore));
        var cosineEnumerable = ReflectionInfo.Method((IEnumerable<object> vectors1, IEnumerable<object> vectors2, bool normalizeScore) => SimilarityFunctions.Cosine(vectors1, vectors2, normalizeScore));
        var cosineMemory = ReflectionInfo.Method((ReadOnlyMemory<object> vectors1, ReadOnlyMemory<object> vectors2, bool normalizeScore) => SimilarityFunctions.Cosine(vectors1, vectors2, normalizeScore));

        // initialize sets of methods after methods
        __similarityFunctionOverloads = MethodInfoSet.Create(
        [
            dotProductEnumerable,
            dotProductMemory,
            euclideanEnumerable,
            euclideanMemory,
            cosineEnumerable,
            cosineMemory,
        ]);
    }

    // sets of methods
    public static IReadOnlyMethodInfoSet SimilarityFunctionOverloads => __similarityFunctionOverloads;
}
