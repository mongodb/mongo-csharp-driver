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
using MongoDB.Driver.Core.Operations;
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

        private readonly IMayUseSecondaryCriteria _mayUseSecondary;
        private readonly IServerSelector _readPreferenceServerSelector;

        // constructors
        private WritableServerSelector()
            : this(mayUseSecondary: null)
        {
        }

        internal WritableServerSelector(IMayUseSecondaryCriteria mayUseSecondary)
        {
            _mayUseSecondary = mayUseSecondary; // can be null

            var readPreference = mayUseSecondary?.ReadPreference;
            _readPreferenceServerSelector = readPreference == null ? null : new ReadPreferenceServerSelector(readPreference);
        }

        // methods
        /// <inheritdoc/>
        public IEnumerable<ServerDescription> SelectServers(ClusterDescription cluster, IEnumerable<Servers.ServerDescription> servers)
        {
            var serversList = servers.ToList(); // avoid multiple enumeration

            if (cluster.IsDirectConnection)
            {
                return serversList;
            }

            if (ShouldUseReadPreference())
            {
                return _readPreferenceServerSelector.SelectServers(cluster, serversList);
            }
            else
            {
                return serversList.Where(x => x.Type.IsWritable());
            }

            bool ShouldUseReadPreference()
            {
                if (_readPreferenceServerSelector == null)
                {
                    return false; // no ReadPreference server selector available to use
                }

                if (cluster.Type != ClusterType.ReplicaSet)
                {
                    return false; // ReadPreference server selector is only applicable to replica sets
                }

                // ReadPreference should be used if there is at least one available server and all available servers are suitable
                return
                    serversList.Count > 0 &&
                    serversList.All(s => s.Type == ServerType.ReplicaSetPrimary || _mayUseSecondary.CanUseSecondary(s));
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "WritableServerSelector";
        }
    }
}
