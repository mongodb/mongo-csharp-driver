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
        public virtual void ClusterBeforeClosing(ClusterBeforeClosingEvent @event)
        {
        }

        public virtual void ClusterAfterClosing(ClusterAfterClosingEvent @event)
        {
        }

        public virtual void ClusterBeforeOpening(ClusterBeforeOpeningEvent @event)
        {
        }

        public virtual void ClusterAfterOpening(ClusterAfterOpeningEvent @event)
        {
        }

        public virtual void ClusterBeforeAddingServer(ClusterBeforeAddingServerEvent @event)
        {
        }

        public virtual void ClusterAfterAddingServer(ClusterAfterAddingServerEvent @event)
        {
        }

        public virtual void ClusterBeforeRemovingServer(ClusterBeforeRemovingServerEvent @event)
        {
        }

        public virtual void ClusterAfterRemovingServer(ClusterAfterRemovingServerEvent @event)
        {
        }

        public virtual void ClusterAfterDescriptionChanged(ClusterAfterDescriptionChangedEvent @event)
        {
        }
        #endregion

        #region Server
        public virtual void ServerBeforeClosing(ServerBeforeClosingEvent @event)
        {
        }

        public virtual void ServerAfterClosing(ServerAfterClosingEvent @event)
        {
        }

        public virtual void ServerBeforeOpening(ServerBeforeOpeningEvent @event)
        {
        }

        public virtual void ServerAfterOpening(ServerAfterOpeningEvent @event)
        {
        }

        public virtual void ServerBeforeHeartbeating(ServerBeforeHeartbeatingEvent @event)
        {
        }

        public virtual void ServerAfterHeartbeating(ServerAfterHeartbeatingEvent @event)
        {
        }

        public virtual void ServerErrorHeartbeating(ServerErrorHeartbeatingEvent @event)
        {
        }

        public virtual void ServerAfterDescriptionChanged(ServerAfterDescriptionChangedEvent @event)
        {
        }
        #endregion

        #region Connection Pools
        public virtual void ConnectionPoolBeforeClosing(ConnectionPoolBeforeClosingEvent @event)
        {
        }

        public virtual void ConnectionPoolAfterClosing(ConnectionPoolAfterClosingEvent @event)
        {
        }

        public virtual void ConnectionPoolBeforeOpening(ConnectionPoolBeforeOpeningEvent @event)
        {
        }

        public virtual void ConnectionPoolAfterOpening(ConnectionPoolAfterOpeningEvent @event)
        {
        }

        public virtual void ConnectionPoolBeforeAddingAConnection(ConnectionPoolBeforeAddingAConnectionEvent @event)
        {
        }

        public virtual void ConnectionPoolAfterAddingAConnection(ConnectionPoolAfterAddingAConnectionEvent @event)
        {
        }

        public virtual void ConnectionPoolBeforeRemovingAConnection(ConnectionPoolBeforeRemovingAConnectionEvent @event)
        {
        }

        public virtual void ConnectionPoolAfterRemovingAConnection(ConnectionPoolAfterRemovingAConnectionEvent @event)
        {
        }

        public virtual void ConnectionPoolBeforeEnteringWaitQueue(ConnectionPoolBeforeEnteringWaitQueueEvent @event)
        {
        }

        public virtual void ConnectionPoolAfterEnteringWaitQueue(ConnectionPoolAfterEnteringWaitQueueEvent @event)
        {
        }

        public virtual void ConnectionPoolErrorEnteringWaitQueue(ConnectionPoolErrorEnteringWaitQueueEvent @event)
        {
        }

        public virtual void ConnectionPoolBeforeCheckingOutAConnection(ConnectionPoolBeforeCheckingOutAConnectionEvent @event)
        {
        }

        public virtual void ConnectionPoolAfterCheckingOutAConnection(ConnectionPoolAfterCheckingOutAConnectionEvent @event)
        {
        }

        public virtual void ConnectionPoolErrorCheckingOutAConnection(ConnectionPoolErrorCheckingOutAConnectionEvent @event)
        {
        }

        public virtual void ConnectionPoolBeforeCheckingInAConnection(ConnectionPoolBeforeCheckingInAConnectionEvent @event)
        {
        }

        public virtual void ConnectionPoolAfterCheckingInAConnection(ConnectionPoolAfterCheckingInAConnectionEvent @event)
        {
        }

        #endregion

        #region Connections
        public virtual void ConnectionFailed(ConnectionFailedEvent @event)
        {
        }

        public virtual void ConnectionBeforeClosing(ConnectionBeforeClosingEvent @event)
        {
        }

        public virtual void ConnectionAfterClosing(ConnectionAfterClosingEvent @event)
        {
        }

        public virtual void ConnectionBeforeOpening(ConnectionBeforeOpeningEvent @event)
        {
        }

        public virtual void ConnectionAfterOpening(ConnectionAfterOpeningEvent @event)
        {
        }

        public virtual void ConnectionErrorOpening(ConnectionErrorOpeningEvent @event)
        {
        }

        public virtual void ConnectionBeforeReceivingMessage(ConnectionBeforeReceivingMessageEvent @event)
        {
        }

        public virtual void ConnectionAfterReceivingMessage<T>(ConnectionAfterReceivingMessageEvent<T> @event)
        {
        }

        public virtual void ConnectionErrorReceivingMessage(ConnectionErrorReceivingMessageEvent@event)
        {
        }

        public virtual void ConnectionBeforeSendingMessages(ConnectionBeforeSendingMessagesEvent @event)
        {
        }

        public virtual void ConnectionAfterSendingMessages(ConnectionAfterSendingMessagesEvent @event)
        {
        }

        public virtual void ConnectionErrorSendingMessages(ConnectionErrorSendingMessagesEvent @event)
        {
        }

        #endregion
    }
}