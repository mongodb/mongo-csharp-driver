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
/// Indicates the type of data that will be auto-embedded by an auto-embedding vector index defined by
/// <see cref="CreateVectorSearchIndexModel{TDocument}"/>. See
/// <see href="https://www.mongodb.com/docs/atlas/atlas-vector-search/vector-search-type/">How to Index Fields for
/// Vector Search</see> for more information.
/// </summary>
/// <remarks>
/// Note that more entries will be added to this enum in the future as different modalities are supported. Do
/// not assume a fixed set of values for this enum.
/// </remarks>
public enum VectorEmbeddingModality
{
    /// <summary>
    /// The indexed fields contain text.
    /// </summary>
    Text,
}
