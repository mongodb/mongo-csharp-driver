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
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Events
{
    public interface IConnectionPoolListener : IListener
    {
        void ConnectionPoolBeforeClosing(ServerId serverId);
        void ConnectionPoolAfterClosing(ServerId serverId);

        void ConnectionPoolBeforeOpening(ServerId serverId, ConnectionPoolSettings settings);
        void ConnectionPoolAfterOpening(ServerId serverId, ConnectionPoolSettings settings);

        void ConnectionPoolBeforeAddingAConnection(ServerId serverId);
        void ConnectionPoolAfterAddingAConnection(ConnectionId connectionId, TimeSpan elapsed);

        void ConnectionPoolBeforeRemovingAConnection(ConnectionId connectionId);
        void ConnectionPoolAfterRemovingAConnection(ConnectionId connectionId, TimeSpan elapsed);

        void ConnectionPoolBeforeEnteringWaitQueue(ServerId serverId);
        void ConnectionPoolAfterEnteringWaitQueue(ServerId serverId, TimeSpan elapsed);
        void ConnectionPoolErrorEnteringWaitQueue(ServerId serverId, TimeSpan elapsed, Exception exception);

        void ConnectionPoolBeforeCheckingOutAConnection(ServerId serverId);
        void ConnectionPoolAfterCheckingOutAConnection(ConnectionId connectionId, TimeSpan elapsed);
        void ConnectionPoolErrorCheckingOutAConnection(ServerId serverId, TimeSpan elapsed, Exception ex);

        void ConnectionPoolBeforeCheckingInAConnection(ConnectionId connectionId);
        void ConnectionPoolAfterCheckingInAConnection(ConnectionId connectionId, TimeSpan elapsed);
    }
}
