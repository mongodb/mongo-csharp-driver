/* Copyright 2013-2014 MongoDB Inc.
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
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol.Messages;

namespace MongoDB.Driver.Core.Events
{
    public class EmptyListener : IClusterListener, IServerListener, IConnectionPoolListener, IConnectionListener
    {
        #region static
        // static fields
        private static readonly EmptyListener __instance = new EmptyListener();

        // static properties
        public static EmptyListener Instance
        {
            get { return __instance; }
        }
        #endregion static

        #region Clusters
        public virtual void ClusterBeforeClosing(ClusterId clusterId)
        {
        }

        public virtual void ClusterAfterClosing(ClusterId clusterId, TimeSpan elapsed)
        {
        }

        public virtual void ClusterBeforeOpening(ClusterId clusterId, ClusterSettings settings)
        {
        }

        public virtual void ClusterAfterOpening(ClusterId clusterId, ClusterSettings settings, TimeSpan elapsed)
        {
        }

        public virtual void ClusterBeforeAddingServer(ClusterId clusterId, System.Net.EndPoint endPoint)
        {
        }

        public virtual void ClusterAfterAddingServer(ServerId serverId, TimeSpan elapsed)
        {
        }

        public virtual void ClusterBeforeRemovingServer(ServerId serverId, string reason)
        {
        }

        public virtual void ClusterAfterRemovingServer(ServerId serverId, string reason, TimeSpan elapsed)
        {
        }

        public virtual void ClusterDescriptionChanged(Clusters.ClusterDescription oldClusterDescription, Clusters.ClusterDescription newClusterDescription)
        {
        }
        #endregion

        #region Server
        public virtual void ServerBeforeClosing(ServerId serverId)
        {
        }

        public virtual void ServerAfterClosing(ServerId serverId)
        {
        }

        public virtual void ServerBeforeOpening(ServerId serverId, ServerSettings settings)
        {
        }

        public virtual void ServerAfterOpening(ServerId serverId, ServerSettings settings, TimeSpan elapsed)
        {
        }

        public virtual void ServerBeforeHeartbeating(ConnectionId connectionId)
        {
        }

        public virtual void ServerAfterHeartbeating(ConnectionId connectionId, TimeSpan elapsed)
        {
        }

        public virtual void ServerErrorHeartbeating(ConnectionId connectionId, Exception exception)
        {
        }

        public virtual void ServerAfterDescriptionChanged(Servers.ServerDescription oldDescription, Servers.ServerDescription newDescription)
        {
        }
        #endregion

        #region Connection Pools
        public virtual void ConnectionPoolBeforeClosing(ServerId serverId)
        {
        }

        public virtual void ConnectionPoolAfterClosing(ServerId serverId)
        {
        }

        public virtual void ConnectionPoolBeforeOpening(ServerId serverId, ConnectionPoolSettings settings)
        {
        }

        public virtual void ConnectionPoolAfterOpening(ServerId serverId, ConnectionPoolSettings settings)
        {
        }

        public virtual void ConnectionPoolBeforeAddingAConnection(ServerId serverId)
        {
        }

        public virtual void ConnectionPoolAfterAddingAConnection(ConnectionId connectionId, TimeSpan elapsed)
        {
        }

        public virtual void ConnectionPoolBeforeRemovingAConnection(ConnectionId connectionId)
        {
        }

        public virtual void ConnectionPoolAfterRemovingAConnection(ConnectionId connectionId, TimeSpan elapsed)
        {
        }

        public virtual void ConnectionPoolBeforeEnteringWaitQueue(ServerId serverId)
        {
        }

        public virtual void ConnectionPoolAfterEnteringWaitQueue(ServerId serverId, TimeSpan elapsed)
        {
        }

        public virtual void ConnectionPoolErrorEnteringWaitQueue(ServerId serverId, TimeSpan elapsed, Exception ex)
        {
        }

        public virtual void ConnectionPoolBeforeCheckingOutAConnection(ServerId serverId)
        {
        }

        public virtual void ConnectionPoolAfterCheckingOutAConnection(ConnectionId connectionId, TimeSpan elapsed)
        {
        }

        public virtual void ConnectionPoolErrorCheckingOutAConnection(ServerId serverId, TimeSpan elapsed, Exception ex)
        {
        }

        public virtual void ConnectionPoolBeforeCheckingInAConnection(ConnectionId connectionId)
        {
        }

        public virtual void ConnectionPoolAfterCheckingInAConnection(ConnectionId connectionId, TimeSpan elapsed)
        {
        }

        #endregion

        #region Connections
        public virtual void ConnectionFailed(ConnectionId connectionId, Exception exception)
        {
        }

        public virtual void ConnectionBeforeClosing(ConnectionId connectionId)
        {
        }

        public virtual void ConnectionAfterClosing(ConnectionId connectionId)
        {
        }

        public virtual void ConnectionBeforeOpening(ConnectionId connectionId, ConnectionSettings settings)
        {
        }

        public virtual void ConnectionAfterOpening(ConnectionId connectionId, ConnectionSettings settings, TimeSpan elapsed)
        {
        }

        public virtual void ConnectionErrorOpening(ConnectionId connectionId, Exception exception)
        {
        }

        public virtual void ConnectionBeforeReceivingMessage(ConnectionId connectionId, int responseTo)
        {
        }

        public virtual void ConnectionAfterReceivingMessage<T>(ConnectionId connectionId, ReplyMessage<T> message, int length, TimeSpan elapsed)
        {
        }

        public virtual void ConnectionErrorReceivingMessage(ConnectionId connectionId, int responseTo, Exception exception)
        {
        }

        public virtual void ConnectionBeforeSendingMessages(ConnectionId connectionId, IReadOnlyList<RequestMessage> messages)
        {
        }

        public virtual void ConnectionAfterSendingMessages(ConnectionId connectionId, IReadOnlyList<RequestMessage> messages, int length, TimeSpan elapsed)
        {
        }

        public virtual void ConnectionErrorSendingMessages(ConnectionId connectionId, IReadOnlyList<RequestMessage> messages, Exception exception)
        {
        }
        #endregion
    }
}