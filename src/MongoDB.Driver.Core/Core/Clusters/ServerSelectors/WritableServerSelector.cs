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

using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver.Core.Misc;
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

        private readonly IWriteOperation _operation;

        // constructors
        private WritableServerSelector()
        {
        }

        internal WritableServerSelector(IWriteOperation operation)
        {
            _operation = operation; // can be null
        }

        // methods
        /// <inheritdoc/>
        public IEnumerable<ServerDescription> SelectServers(ClusterDescription cluster, IEnumerable<Servers.ServerDescription> servers)
        {
            if (cluster.IsDirectConnection)
            {
                return servers;
            }

            if (_operation is IMayUseSecondaryWriteOperation mayUseSecondaryOperation)
            {
                var readPreference = mayUseSecondaryOperation.ReadPreference;
                var minServerVersionToUseSecondary = mayUseSecondaryOperation.MinServerVersionToUseSecondary;
                return SelectServersUsingReadPreference(cluster, servers, readPreference, minServerVersionToUseSecondary);
            }

            return servers.Where(x => x.Type.IsWritable());
        }

        private IEnumerable<ServerDescription> SelectServersUsingReadPreference(
            ClusterDescription cluster,
            IEnumerable<Servers.ServerDescription> servers,
            ReadPreference readPreference,
            ServerVersion minServerVersionToUseSecondary)
        {
            throw new NotImplementedException(); // implement the new server selection logic here
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "WritableServerSelector";
        }
    }
}
