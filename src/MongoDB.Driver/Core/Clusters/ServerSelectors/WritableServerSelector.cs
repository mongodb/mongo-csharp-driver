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
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Clusters.ServerSelectors
{
    /// <summary>
    /// Represents a server selector that selects writable servers.
    /// </summary>
    internal sealed class WritableServerSelector : IServerSelector
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

        // constructors
        private readonly IMayUseSecondaryCriteria _mayUseSecondary;

        /// <summary>
        /// Initializes an instance of the WritableServerSelector class.
        /// </summary>
        public WritableServerSelector()
        {
        }

        /// <summary>
        /// Initializes an instance of the WritableServerSelector class.
        /// </summary>
        /// <param name="mayUseSecondary">The may use secondary criteria.</param>
        public WritableServerSelector(IMayUseSecondaryCriteria mayUseSecondary)
        {
            _mayUseSecondary = mayUseSecondary; // can be null
        }

        // properties
        /// <summary>
        /// Returns the may use secondary criteria.
        /// </summary>
        public IMayUseSecondaryCriteria MayUseSecondary => _mayUseSecondary;

        // methods
        /// <inheritdoc/>
        public IEnumerable<ServerDescription> SelectServers(ClusterDescription cluster, IEnumerable<ServerDescription> servers)
        {
            if (cluster.DirectConnection)
            {
                return servers;
            }

            var serversList = servers.ToList(); // avoid multiple enumeration
            if (CanUseSecondaries(cluster, serversList))
            {
                var readPreferenceSelector = new ReadPreferenceServerSelector(_mayUseSecondary.ReadPreference);
                return readPreferenceSelector.SelectServers(cluster, serversList);
            }

            if (_mayUseSecondary != null)
            {
                _mayUseSecondary.EffectiveReadPreference = ReadPreference.Primary; // fallback to primary
            }
            return serversList.Where(x => x.Type.IsWritable());
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "WritableServerSelector";
        }

        private bool CanUseSecondaries(ClusterDescription cluster, List<ServerDescription> servers)
        {
            if (_mayUseSecondary?.ReadPreference == null)
            {
                return false;
            }

            switch (cluster.Type)
            {
                case ClusterType.ReplicaSet:
                case ClusterType.Sharded:
                    if (servers.Count == 0)
                    {
                        return true;
                    }

                    return servers.All(s => _mayUseSecondary.CanUseSecondary(s));

                case ClusterType.LoadBalanced:
                    return true;

                default:
                    return false;
            }
        }
    }
}
