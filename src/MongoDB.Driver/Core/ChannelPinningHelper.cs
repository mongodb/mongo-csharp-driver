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

using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core
{
    internal static class ChannelPinningHelper
    {
        public static IReadBindingHandle CreateReadBinding(IClusterInternal cluster, ICoreSessionHandle session, ReadPreference readPreference)
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

        public static IReadWriteBindingHandle CreateReadWriteBinding(IClusterInternal cluster, ICoreSessionHandle session)
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

        internal static IChannelSourceHandle CreateGetMoreChannelSource(IChannelSourceHandle channelSource, IChannelHandle channel, long cursorId)
        {
            IChannelSource effectiveChannelSource;
            if (IsInLoadBalancedMode(channelSource.ServerDescription) && cursorId != 0)
            {
                if (channel.Connection is ICheckOutReasonTracker checkOutReasonTracker)
                {
                    checkOutReasonTracker.SetCheckOutReasonIfNotAlreadySet(CheckOutReason.Cursor);
                }

                effectiveChannelSource = new ChannelChannelSource(
                    channelSource.Server,
                    channel.Fork(),
                    channelSource.Session.Fork());
            }
            else
            {
                effectiveChannelSource = new ServerChannelSource(channelSource.Server, channelSource.Session.Fork());
            }

            return new ChannelSourceHandle(effectiveChannelSource);
        }

        internal static void PinChannellIfRequired(
            IChannelSourceHandle channelSource,
            IChannelHandle channel,
            ICoreSessionHandle session)
        {
            if (IsInLoadBalancedMode(channel.ConnectionDescription) &&
                session.IsInTransaction &&
                !IsChannelPinned(session.CurrentTransaction))
            {
                if (channel.Connection is ICheckOutReasonTracker checkOutReasonTracker)
                {
                    checkOutReasonTracker.SetCheckOutReasonIfNotAlreadySet(CheckOutReason.Transaction);
                }
                session.CurrentTransaction.PinChannel(channel.Fork());
                session.CurrentTransaction.PinnedServer = channelSource.Server;
            }
        }

        // private methods
        private static bool IsInLoadBalancedMode(ConnectionDescription connectionDescription) => connectionDescription?.ServiceId.HasValue ?? false;
        private static bool IsInLoadBalancedMode(ServerDescription serverDescription) => serverDescription?.Type == ServerType.LoadBalanced;
        private static bool IsInLoadBalancedMode(ClusterDescription clusterDescription) => clusterDescription?.Type == ClusterType.LoadBalanced;
        private static bool IsChannelPinned(CoreTransaction coreTransaction) => coreTransaction?.PinnedChannel != null;
    }
}
