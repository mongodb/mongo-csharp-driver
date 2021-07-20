/* Copyright 2021-present MongoDB Inc.
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
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core
{
    /// <summary>
    /// Connection pinning helper.
    /// </summary>
    public static class ChannelPinningHelper
    {
        /// <summary>
        /// Create a read binding handle.
        /// </summary>
        /// <param name="cluster">The cluster,</param>
        /// <param name="session">The session.</param>
        /// <param name="readPreference">The read preference.</param>
        /// <returns>An effective read binging.</returns>
        public static IReadBindingHandle CreateReadBinding(ICluster cluster, ICoreSessionHandle session, ReadPreference readPreference)
        {
            IReadBinding readBinding;
            if (session.IsInTransaction &&
                IsChannelPinned(session.CurrentTransaction) &&
                session.CurrentTransaction.State != CoreTransactionState.Starting)
            {
                readBinding = new ChannelReadWriteBinding(
                    session.CurrentTransaction.PinnedServer,
                    session.CurrentTransaction.PinnedChannel.Fork(),
                    session);
            }
            else
            {
                if (IsInLoadBalancedMode(cluster.Description) && IsChannelPinned(session.CurrentTransaction))
                {
                    // unpin if the next operation is not under transaction
                    session.CurrentTransaction.UnpinAll();
                }
                readBinding = new ReadPreferenceBinding(cluster, readPreference, session);
            }

            return new ReadBindingHandle(readBinding);
        }

        /// <summary>
        /// Create a readwrite binding handle.
        /// </summary>
        /// <param name="cluster">The cluster.</param>
        /// <param name="session">The session.</param>
        /// <returns>An effective read write binging.</returns>
        public static IReadWriteBindingHandle CreateReadWriteBinding(ICluster cluster, ICoreSessionHandle session)
        {
            IReadWriteBinding readWriteBinding;
            if (session.IsInTransaction &&
                IsChannelPinned(session.CurrentTransaction) &&
                session.CurrentTransaction.State != CoreTransactionState.Starting)
            {
                readWriteBinding = new ChannelReadWriteBinding(
                    session.CurrentTransaction.PinnedServer,
                    session.CurrentTransaction.PinnedChannel.Fork(),
                    session);
            }
            else
            {
                if (IsInLoadBalancedMode(cluster.Description) && IsChannelPinned(session.CurrentTransaction))
                {
                    // unpin if the next operation is not under transaction
                    session.CurrentTransaction.UnpinAll();
                }
                readWriteBinding = new WritableServerBinding(cluster, session);
            }

            return new ReadWriteBindingHandle(readWriteBinding);
        }

        internal static IChannelSourceHandle CreateGetMoreChannelSource(IChannelSourceHandle channelSource, long cursorId)
        {
            IChannelSource effectiveChannelSource;
            if (IsInLoadBalancedMode(channelSource.ServerDescription) && cursorId != 0)
            {
                var getMoreChannel = channelSource.GetChannel(CancellationToken.None); // no need for cancellation token since we already have channel in the source
                var getMoreSession = channelSource.Session.Fork();
                if (getMoreChannel.Connection is ITrackedPinningReason trackedConnection)
                {
                    trackedConnection.SetPinningCheckoutReasonIfNotAlreadySet(CheckedOutReason.Cursor);
                }

                effectiveChannelSource = new ChannelChannelSource(
                    channelSource.Server,
                    getMoreChannel,
                    getMoreSession);
            }
            else
            {
                effectiveChannelSource = new ServerChannelSource(channelSource.Server, channelSource.Session.Fork());
            }

            return new ChannelSourceHandle(effectiveChannelSource);
        }

        internal static bool PinChannelSourceAndChannelIfRequired(
            IChannelSourceHandle channelSource,
            IChannelHandle channel,
            ICoreSessionHandle session,
            out IChannelSourceHandle pinnedChannelSource,
            out IChannelHandle pinnedChannel)
        {
            if (IsInLoadBalancedMode(channel.ConnectionDescription))
            {
                var server = channelSource.Server;

                pinnedChannelSource = new ChannelSourceHandle(
                    new ChannelChannelSource(
                        server,
                        channel.Fork(),
                        session.Fork()));

                if (session.IsInTransaction && !IsChannelPinned(session.CurrentTransaction))
                {
                    if (channel.Connection is ITrackedPinningReason trackedConnection)
                    {
                        trackedConnection.SetPinningCheckoutReasonIfNotAlreadySet(CheckedOutReason.Transaction);
                    }
                    session.CurrentTransaction.PinChannel(channel.Fork());
                    session.CurrentTransaction.PinnedServer = server;
                }

                pinnedChannel = channel.Fork();

                return true;
            }

            pinnedChannelSource = null;
            pinnedChannel = null;
            return false;
        }

        // private methods
        private static bool IsInLoadBalancedMode(ConnectionDescription connectionDescription) => connectionDescription?.ServiceId.HasValue ?? false;
        private static bool IsInLoadBalancedMode(ServerDescription serverDescription) => serverDescription?.Type == ServerType.LoadBalanced;
        private static bool IsInLoadBalancedMode(ClusterDescription clusterDescription) => clusterDescription?.Type == ClusterType.LoadBalanced;
        private static bool IsChannelPinned(CoreTransaction coreTransaction) => coreTransaction?.PinnedChannel != null;
    }
}
