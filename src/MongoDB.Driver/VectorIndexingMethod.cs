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
/// Indexing algorithm used for a vector field. See
/// <see href="https://www.mongodb.com/docs/atlas/atlas-vector-search/vector-search-type/">How to Index Fields for
/// Vector Search</see> for more information.
/// </summary>
public enum VectorIndexingMethod
{
    /// <summary>
    /// Hierarchical Navigable Small Worlds graph-based indexing. If omitted, this is the default value.
    /// </summary>
    Hnsw,

    /// <summary>
    /// Full-scan indexing. More efficient than HNSW for indexes containing many small tenants
    /// (approximately 1–10k vectors each) where searches are highly selective.
    /// </summary>
    Flat,
}
