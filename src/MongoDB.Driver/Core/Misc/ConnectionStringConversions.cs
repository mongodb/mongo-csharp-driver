/* Copyright 2020-present MongoDB Inc.
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

namespace MongoDB.Driver.Core.Misc
{
    internal static class ConnectionStringConversions
    {
        /// <summary>
        /// Gets the effective max connections.
        /// </summary>
        /// <param name="maxConnections">The max connections (0 means no max).</param>
        /// <returns>The effective max connections.</returns>
        public static int GetEffectiveMaxConnections(int maxConnections)
        {
            return maxConnections == 0 ? int.MaxValue : maxConnections;
        }

        /// <summary>
        /// Gets the effective max connections.
        /// </summary>
        /// <param name="maxConnections">The max connections (0 means no max).</param>
        /// <returns>The effective max connections (or null if maxConnections is null).</returns>
        public static int? GetEffectiveMaxConnections(int? maxConnections)
        {
            return maxConnections.HasValue ? GetEffectiveMaxConnections(maxConnections.Value) : (int?)null;
        }
    }
}
