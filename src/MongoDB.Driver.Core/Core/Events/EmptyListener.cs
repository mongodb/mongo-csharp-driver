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
        public virtual void BeforeClosing(ClusterBeforeClosingEvent @event)
        {
        }

        public virtual void AfterClosing(ClusterAfterClosingEvent @event)
        {
        }

        public virtual void BeforeOpening(ClusterBeforeOpeningEvent @event)
        {
        }

        public virtual void AfterOpening(ClusterAfterOpeningEvent @event)
        {
        }

        public virtual void BeforeAddingServer(ClusterBeforeAddingServerEvent @event)
        {
        }

        public virtual void AfterAddingServer(ClusterAfterAddingServerEvent @event)
        {
        }

        public virtual void BeforeRemovingServer(ClusterBeforeRemovingServerEvent @event)
        {
        }

        public virtual void AfterRemovingServer(ClusterAfterRemovingServerEvent @event)
        {
        }

        public virtual void AfterDescriptionChanged(ClusterAfterDescriptionChangedEvent @event)
        {
        }
        #endregion

        #region Server
        public virtual void BeforeClosing(ServerBeforeClosingEvent @event)
        {
        }

        public virtual void AfterClosing(ServerAfterClosingEvent @event)
        {
        }

        public virtual void BeforeOpening(ServerBeforeOpeningEvent @event)
        {
        }

        public virtual void AfterOpening(ServerAfterOpeningEvent @event)
        {
        }

        public virtual void BeforeHeartbeating(ServerBeforeHeartbeatingEvent @event)
        {
        }

        public virtual void AfterHeartbeating(ServerAfterHeartbeatingEvent @event)
        {
        }

        public virtual void ErrorHeartbeating(ServerErrorHeartbeatingEvent @event)
        {
        }

        public virtual void AfterDescriptionChanged(ServerAfterDescriptionChangedEvent @event)
        {
        }
        #endregion

        #region Connection Pools
        public virtual void BeforeClosing(ConnectionPoolBeforeClosingEvent @event)
        {
        }

        public virtual void AfterClosing(ConnectionPoolAfterClosingEvent @event)
        {
        }

        public virtual void BeforeOpening(ConnectionPoolBeforeOpeningEvent @event)
        {
        }

        public virtual void AfterOpening(ConnectionPoolAfterOpeningEvent @event)
        {
        }

        public virtual void BeforeAddingAConnection(ConnectionPoolBeforeAddingAConnectionEvent @event)
        {
        }

        public virtual void AfterAddingAConnection(ConnectionPoolAfterAddingAConnectionEvent @event)
        {
        }

        public virtual void BeforeRemovingAConnection(ConnectionPoolBeforeRemovingAConnectionEvent @event)
        {
        }

        public virtual void AfterRemovingAConnection(ConnectionPoolAfterRemovingAConnectionEvent @event)
        {
        }

        public virtual void BeforeEnteringWaitQueue(ConnectionPoolBeforeEnteringWaitQueueEvent @event)
        {
        }

        public virtual void AfterEnteringWaitQueue(ConnectionPoolAfterEnteringWaitQueueEvent @event)
        {
        }

        public virtual void ErrorEnteringWaitQueue(ConnectionPoolErrorEnteringWaitQueueEvent @event)
        {
        }

        public virtual void BeforeCheckingOutAConnection(ConnectionPoolBeforeCheckingOutAConnectionEvent @event)
        {
        }

        public virtual void AfterCheckingOutAConnection(ConnectionPoolAfterCheckingOutAConnectionEvent @event)
        {
        }

        public virtual void ErrorCheckingOutAConnection(ConnectionPoolErrorCheckingOutAConnectionEvent @event)
        {
        }

        public virtual void BeforeCheckingInAConnection(ConnectionPoolBeforeCheckingInAConnectionEvent @event)
        {
        }

        public virtual void AfterCheckingInAConnection(ConnectionPoolAfterCheckingInAConnectionEvent @event)
        {
        }

        #endregion

        #region Connections
        public virtual void Failed(ConnectionFailedEvent @event)
        {
        }

        public virtual void BeforeClosing(ConnectionBeforeClosingEvent @event)
        {
        }

        public virtual void AfterClosing(ConnectionAfterClosingEvent @event)
        {
        }

        public virtual void BeforeOpening(ConnectionBeforeOpeningEvent @event)
        {
        }

        public virtual void AfterOpening(ConnectionAfterOpeningEvent @event)
        {
        }

        public virtual void ErrorOpening(ConnectionErrorOpeningEvent @event)
        {
        }

        public virtual void BeforeReceivingMessage(ConnectionBeforeReceivingMessageEvent @event)
        {
        }

        public virtual void AfterReceivingMessage<T>(ConnectionAfterReceivingMessageEvent<T> @event)
        {
        }

        public virtual void ErrorReceivingMessage(ConnectionErrorReceivingMessageEvent@event)
        {
        }

        public virtual void BeforeSendingMessages(ConnectionBeforeSendingMessagesEvent @event)
        {
        }

        public virtual void AfterSendingMessages(ConnectionAfterSendingMessagesEvent @event)
        {
        }

        public virtual void ErrorSendingMessages(ConnectionErrorSendingMessagesEvent @event)
        {
        }

        #endregion
    }
}