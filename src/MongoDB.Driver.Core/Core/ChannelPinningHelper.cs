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

using System;
using System.Threading;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
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
        /// Create effective read binding handle.
        /// </summary>
        /// <param name="cluster">The cluster,</param>
        /// <param name="session">The session.</param>
        /// <param name="readPreference">The read preference.</param>
        /// <returns>An effective read binging.</returns>
        public static IReadBindingHandle CreateEffectiveReadBinding(ICluster cluster, ICoreSessionHandle session, ReadPreference readPreference)
        {
            IReadBinding readBinding;
            if (session.IsInTransaction &&
                IsConnectionPinned(session.CurrentTransaction) &&
                session.CurrentTransaction.State != CoreTransactionState.Starting)
            {
                readBinding = new ChannelReadWriteBinding(
                    session.CurrentTransaction.PinnedServer,
                    session.CurrentTransaction.PinnedChannel,
                    session);
            }
            else
            {
                if (IsInLoadBalancedMode(cluster.Description) && IsConnectionPinned(session.CurrentTransaction))
                {
                    // unpin if the next operation is not under transaction
                    session.CurrentTransaction.UnpinAll();
                }
                readBinding = new ReadPreferenceBinding(cluster, readPreference, session);
            }

            return new ReadBindingHandle(readBinding);
        }

        /// <summary>
        /// Create effective readwrite binding handle.
        /// </summary>
        /// <param name="cluster">The cluster.</param>
        /// <param name="session">The session.</param>
        /// <returns></returns>
        public static IReadWriteBindingHandle CreateEffectiveReadWriteBinding(ICluster cluster, ICoreSessionHandle session)
        {
            IReadWriteBinding readWriteBinding;
            if (session.IsInTransaction &&
                IsConnectionPinned(session.CurrentTransaction) &&
                session.CurrentTransaction.State != CoreTransactionState.Starting)
            {
                readWriteBinding = new ChannelReadWriteBinding(
                    session.CurrentTransaction.PinnedServer,
                    session.CurrentTransaction.PinnedChannel,
                    session);
            }
            else
            {
                if (IsInLoadBalancedMode(cluster.Description) && IsConnectionPinned(session.CurrentTransaction))
                {
                    // unpin if the next operation is not under transaction
                    session.CurrentTransaction.UnpinAll();
                }
                readWriteBinding = new WritableServerBinding(cluster, session);
            }

            return new ReadWriteBindingHandle(readWriteBinding);
        }

        internal static IChannelSourceHandle CreateEffectiveGetMoreChannelSource(IChannelSourceHandle channelSource, long cursorId)
        {
            IChannelSource effectiveChannelSource;
            if (IsInLoadBalancedMode(channelSource.ServerDescription) && cursorId != 0)
            {
                var getMoreChannel = channelSource.GetChannel(CancellationToken.None); // no need for cancellation token since we already have channel in the source
                var getMoreSession = channelSource.Session.Fork();

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

        internal static bool TryCreatePinnedChannelSourceAndPinChannel(
            IChannelSourceHandle channelSource,
            IChannelHandle channel,
            ICoreSessionHandle session,
            out (IChannelSourceHandle PinnedChannelSource, IChannelHandle Channel) pinnedChannel)
        {
            pinnedChannel = default;
            if (IsInLoadBalancedMode(channel.ConnectionDescription))
            {
                var server = channelSource.Server;
                var forkedChannel = channel.Fork();
                var forkedSession = session.Fork();

                var pinnedChannelSource = new ChannelSourceHandle(
                    new ChannelChannelSource(
                        server,
                        forkedChannel, 
                        forkedSession));

                if (session.IsInTransaction)
                {
                    PinToTheSession(forkedChannel.Fork(), server, session);
                }

                pinnedChannel = (pinnedChannelSource, forkedChannel);

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Pin the channel and the server to the session if required.
        /// </summary>
        /// <param name="getChannelFunc">The protected channel getter.</param>
        /// <param name="server">The server.</param>
        /// <param name="session">The session.</param>
        internal static void PinToTheSessionIfAlreadyNotUnpinned(Func<IChannelHandle> getChannelFunc, IServer server, ICoreSessionHandle session)
        {
            if (IsInLoadBalancedMode(server.Description) && IsConnectionPinned(session.CurrentTransaction))
            {
                PinToTheSession(getChannelFunc(), server, session);
            }
        }

        // private methods
        private static bool IsInLoadBalancedMode(ConnectionDescription connectionDescription) => connectionDescription?.ServiceId.HasValue ?? false;
        private static bool IsInLoadBalancedMode(ServerDescription serverDescription) => serverDescription?.Type == ServerType.LoadBalanced;
        private static bool IsInLoadBalancedMode(ClusterDescription clusterDescription) => clusterDescription?.Type == ClusterType.LoadBalanced;
        private static bool IsConnectionPinned(CoreTransaction coreTransaction) => coreTransaction?.IsConnectionPinned ?? false;

        private static void PinToTheSession(IChannelHandle channel, IServer server, ICoreSessionHandle session)
        {
            session.CurrentTransaction.PinConnection(channel);
            session.CurrentTransaction.PinnedServer = server;
        }
    }
}
