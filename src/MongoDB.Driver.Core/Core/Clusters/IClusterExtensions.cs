/* Copyright 2019–present MongoDB Inc.
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

using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Clusters
{
    /// <summary>
    /// This class contains extension methods for ICluster used for server selection with sharded transactions.
    /// </summary>
    internal static class IClusterExtensions
    {
        public static IServer SelectServerAndPinIfNeeded(
            this ICluster cluster,
            ICoreSessionHandle session,
            IServerSelector selector,
            CancellationToken cancellationToken)
        {
            var pinnedServer = GetPinnedServerIfValid(cluster, session);
            if (pinnedServer != null)
            {
                return pinnedServer;
            }

            // Server selection also updates the cluster type, allowing us to to determine if the server
            // should be pinned.
            var server = cluster.SelectServer(selector, cancellationToken);
            PinServerIfNeeded(cluster, session, server);
            return server;
        }
        
        public static async Task<IServer> SelectServerAndPinIfNeededAsync(
            this ICluster cluster,
            ICoreSessionHandle session,
            IServerSelector selector,
            CancellationToken cancellationToken)
        {
            var pinnedServer = GetPinnedServerIfValid(cluster, session);
            if (pinnedServer != null)
            {
                return pinnedServer;
            }

            // Server selection also updates the cluster type, allowing us to to determine if the server
            // should be pinned.
            var server = await cluster.SelectServerAsync(selector, cancellationToken).ConfigureAwait(false);
            PinServerIfNeeded(cluster, session, server);

            return server;
        }
        
        private static void PinServerIfNeeded(ICluster cluster, ICoreSessionHandle session, IServer server)
        {
            if (cluster.Description.Type == ClusterType.Sharded && session.IsInTransaction)
            {
                session.CurrentTransaction.PinnedServer = server;
            }
        }

        private static IServer GetPinnedServerIfValid(ICluster cluster, ICoreSessionHandle session)
        {
            if (cluster.Description.Type == ClusterType.Sharded 
                && session.IsInTransaction
                && session.CurrentTransaction.State != CoreTransactionState.Starting)
            {
                return session.CurrentTransaction.PinnedServer;
            }
            else
            {
                return null;
            }
        }
    }
}
