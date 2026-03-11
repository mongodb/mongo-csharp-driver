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
/// Type of automatic vector quantization for your vectors. Use this setting only if your embeddings are float
/// or double vectors. See <see href="https://www.mongodb.com/docs/atlas/atlas-vector-search/vector-quantization/">
/// Vector Quantization</see> for more information.
/// </summary>
public enum VectorQuantization
{
    /// <summary>
    /// Indicates no automatic quantization for the vector embeddings. Use this setting if you have pre-quantized
    /// vectors for ingestion. If omitted, this is the default value.
    /// </summary>
    None,

    /// <summary>
    /// Indicates scalar quantization, which transforms values to 1 byte integers.
    /// </summary>
    Scalar,

    /// <summary>
    /// Indicates binary quantization, which transforms values to a single bit.
    /// To use this value, numDimensions must be a multiple of 8.
    /// If precision is critical, select <see cref="None"/> or <see cref="Scalar"/> instead of <see cref="Binary"/>.
    /// </summary>
    Binary,
}
