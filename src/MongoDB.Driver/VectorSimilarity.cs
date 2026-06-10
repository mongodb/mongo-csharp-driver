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

namespace MongoDB.Driver;

/// <summary>
/// Vector similarity function to use to search for top K-nearest neighbors.
/// See <see href="https://www.mongodb.com/docs/atlas/atlas-vector-search/vector-search-type/">How to Index Fields for
/// Vector Search</see> for more information.
/// </summary>
public enum VectorSimilarity
{
    /// <summary>
    /// Measures the distance between ends of vectors.
    /// </summary>
    Euclidean,

    /// <summary>
    /// Measures similarity based on the angle between vectors.
    /// </summary>
    Cosine,

    /// <summary>
    /// Measures similarity like cosine, but takes into account the magnitude of the vector.
    /// </summary>
    DotProduct,
}
