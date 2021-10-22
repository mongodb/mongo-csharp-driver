/* Copyright 2013-present MongoDB Inc.
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

using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Clusters.ServerSelectors
{
    /// <summary>
    /// Represents a server selector that selects writable servers.
    /// </summary>
    public class WritableServerSelector : IServerSelector
    {
        #region static
        // static fields
        private readonly static WritableServerSelector __instance = new WritableServerSelector();

        // static properties
        /// <summary>
        /// Gets a WritableServerSelector.
        /// </summary>
        /// <value>
        /// A server selector.
        /// </value>
        public static WritableServerSelector Instance
        {
            get { return __instance; }
        }
        #endregion

        private readonly ReadPreference _readPreference;
        private readonly ServerVersion _minServerVersionToUseSecondary;

        // constructors
        private WritableServerSelector()
        {
        }

        internal WritableServerSelector(ReadPreference readPreference, ServerVersion minServerVersionToUseSecondary)
        {
            _readPreference = Ensure.IsNotNull(readPreference, nameof(readPreference));
            _minServerVersionToUseSecondary = Ensure.IsNotNull(minServerVersionToUseSecondary, nameof(minServerVersionToUseSecondary));
        }

        // methods
        /// <inheritdoc/>
        public IEnumerable<ServerDescription> SelectServers(ClusterDescription cluster, IEnumerable<Servers.ServerDescription> servers)
        {
            if (cluster.IsDirectConnection)
            {
                return servers;
            }

            // modify this method to use _readPreference and _minServerVersionToUseSecondary (if not null)
            // possibly leveraging an enhanced ReadPreferenceServerSelector as a helper

            return servers.Where(x => x.Type.IsWritable());
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "WritableServerSelector";
        }
    }
}
