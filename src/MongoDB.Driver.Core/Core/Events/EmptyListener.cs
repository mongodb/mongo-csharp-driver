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
    /// <preliminary/>
    /// <summary>
    /// Represents an empty listener that ignores all events.
    /// </summary>
    public class EmptyListener : IClusterListener, IServerListener, IConnectionPoolListener, IConnectionListener
    {
        #region static
        // static fields
        private static readonly EmptyListener __instance = new EmptyListener();

        // static properties
        /// <summary>
        /// Gets an instance of an EmptyListener.
        /// </summary>
        /// <value>
        /// An instance of an EmptyListener.
        /// </value>
        public static EmptyListener Instance
        {
            get { return __instance; }
        }
        #endregion static

        #region Clusters
        /// <inheritdoc/>
        public virtual void ClusterBeforeClosing(ClusterBeforeClosingEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ClusterAfterClosing(ClusterAfterClosingEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ClusterBeforeOpening(ClusterBeforeOpeningEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ClusterAfterOpening(ClusterAfterOpeningEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ClusterBeforeAddingServer(ClusterBeforeAddingServerEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ClusterAfterAddingServer(ClusterAfterAddingServerEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ClusterBeforeRemovingServer(ClusterBeforeRemovingServerEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ClusterAfterRemovingServer(ClusterAfterRemovingServerEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ClusterAfterDescriptionChanged(ClusterAfterDescriptionChangedEvent @event)
        {
        }
        #endregion

        #region Server
        /// <inheritdoc/>
        public virtual void ServerBeforeClosing(ServerBeforeClosingEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ServerAfterClosing(ServerAfterClosingEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ServerBeforeOpening(ServerBeforeOpeningEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ServerAfterOpening(ServerAfterOpeningEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ServerBeforeHeartbeating(ServerBeforeHeartbeatingEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ServerAfterHeartbeating(ServerAfterHeartbeatingEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ServerErrorHeartbeating(ServerErrorHeartbeatingEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ServerAfterDescriptionChanged(ServerAfterDescriptionChangedEvent @event)
        {
        }
        #endregion

        #region Connection Pools
        /// <inheritdoc/>
        public virtual void ConnectionPoolBeforeClosing(ConnectionPoolBeforeClosingEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ConnectionPoolAfterClosing(ConnectionPoolAfterClosingEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ConnectionPoolBeforeOpening(ConnectionPoolBeforeOpeningEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ConnectionPoolAfterOpening(ConnectionPoolAfterOpeningEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ConnectionPoolBeforeAddingAConnection(ConnectionPoolBeforeAddingAConnectionEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ConnectionPoolAfterAddingAConnection(ConnectionPoolAfterAddingAConnectionEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ConnectionPoolBeforeRemovingAConnection(ConnectionPoolBeforeRemovingAConnectionEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ConnectionPoolAfterRemovingAConnection(ConnectionPoolAfterRemovingAConnectionEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ConnectionPoolBeforeEnteringWaitQueue(ConnectionPoolBeforeEnteringWaitQueueEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ConnectionPoolAfterEnteringWaitQueue(ConnectionPoolAfterEnteringWaitQueueEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ConnectionPoolErrorEnteringWaitQueue(ConnectionPoolErrorEnteringWaitQueueEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ConnectionPoolBeforeCheckingOutAConnection(ConnectionPoolBeforeCheckingOutAConnectionEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ConnectionPoolAfterCheckingOutAConnection(ConnectionPoolAfterCheckingOutAConnectionEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ConnectionPoolErrorCheckingOutAConnection(ConnectionPoolErrorCheckingOutAConnectionEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ConnectionPoolBeforeCheckingInAConnection(ConnectionPoolBeforeCheckingInAConnectionEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ConnectionPoolAfterCheckingInAConnection(ConnectionPoolAfterCheckingInAConnectionEvent @event)
        {
        }

        #endregion

        #region Connections
        /// <inheritdoc/>
        public virtual void ConnectionFailed(ConnectionFailedEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ConnectionBeforeClosing(ConnectionBeforeClosingEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ConnectionAfterClosing(ConnectionAfterClosingEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ConnectionBeforeOpening(ConnectionBeforeOpeningEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ConnectionAfterOpening(ConnectionAfterOpeningEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ConnectionErrorOpening(ConnectionErrorOpeningEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ConnectionBeforeReceivingMessage(ConnectionBeforeReceivingMessageEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ConnectionAfterReceivingMessage(ConnectionAfterReceivingMessageEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ConnectionErrorReceivingMessage(ConnectionErrorReceivingMessageEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ConnectionBeforeSendingMessages(ConnectionBeforeSendingMessagesEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ConnectionAfterSendingMessages(ConnectionAfterSendingMessagesEvent @event)
        {
        }

        /// <inheritdoc/>
        public virtual void ConnectionErrorSendingMessages(ConnectionErrorSendingMessagesEvent @event)
        {
        }

        #endregion
    }
}