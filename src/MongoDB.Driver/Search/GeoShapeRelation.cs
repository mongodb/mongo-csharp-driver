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

namespace MongoDB.Driver.Search
{
    /// <summary>
    /// The relation of the query shape geometry to the indexed field geometry in a
    /// geo shape search definition.
    /// </summary>
    public enum GeoShapeRelation
    {
        /// <summary>
        /// Indicates that the indexed geometry contains the query geometry.
        /// </summary>
        Contains,

        /// <summary>
        /// Indicates that both the query and indexed geometries have nothing in common.
        /// </summary>
        Disjoint,

        /// <summary>
        /// Indicates that both the query and indexed geometries intersect.
        /// </summary>
        Intersects,

        /// <summary>
        /// Indicates that the indexed geometry is within the query geometry.
        /// </summary>
        Within
    }
}
