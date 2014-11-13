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
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Events
{
    internal class ConnectionPoolListenerPair : IConnectionPoolListener
    {
        // static
        public static IConnectionPoolListener Create(IConnectionPoolListener first, IConnectionPoolListener second)
        {
            if (first == null)
            {
                return second;
            }

            if (second == null)
            {
                return first;
            }

            return new ConnectionPoolListenerPair(first, second);
        }

        // fields
        private readonly IConnectionPoolListener _first;
        private readonly IConnectionPoolListener _second;

        // constructors
        public ConnectionPoolListenerPair(IConnectionPoolListener first, IConnectionPoolListener second)
        {
            _first = Ensure.IsNotNull(first, "first");
            _second = Ensure.IsNotNull(second, "second");
        }

        // methods
        public void ConnectionPoolBeforeClosing(ServerId serverId)
        {
            _first.ConnectionPoolBeforeClosing(serverId);
            _second.ConnectionPoolBeforeClosing(serverId);
        }

        public void ConnectionPoolAfterClosing(ServerId serverId)
        {
            _first.ConnectionPoolAfterClosing(serverId);
            _second.ConnectionPoolAfterClosing(serverId);
        }

        public void ConnectionPoolBeforeOpening(ServerId serverId, ConnectionPoolSettings settings)
        {
            _first.ConnectionPoolBeforeOpening(serverId, settings);
            _second.ConnectionPoolBeforeOpening(serverId, settings);
        }

        public void ConnectionPoolAfterOpening(ServerId serverId, ConnectionPoolSettings settings)
        {
            _first.ConnectionPoolAfterOpening(serverId, settings);
            _second.ConnectionPoolAfterOpening(serverId, settings);
        }

        public void ConnectionPoolBeforeAddingAConnection(ServerId serverId)
        {
            _first.ConnectionPoolBeforeAddingAConnection(serverId);
            _second.ConnectionPoolBeforeAddingAConnection(serverId);
        }

        public void ConnectionPoolAfterAddingAConnection(ConnectionId connectionId, TimeSpan elapsed)
        {
            _first.ConnectionPoolAfterAddingAConnection(connectionId, elapsed);
            _second.ConnectionPoolAfterAddingAConnection(connectionId, elapsed);
        }

        public void ConnectionPoolBeforeRemovingAConnection(ConnectionId connectionId)
        {
            _first.ConnectionPoolBeforeRemovingAConnection(connectionId);
            _second.ConnectionPoolBeforeRemovingAConnection(connectionId);
        }

        public void ConnectionPoolAfterRemovingAConnection(ConnectionId connectionId, TimeSpan elapsed)
        {
            _first.ConnectionPoolAfterRemovingAConnection(connectionId, elapsed);
            _second.ConnectionPoolAfterRemovingAConnection(connectionId, elapsed);
        }

        public void ConnectionPoolBeforeEnteringWaitQueue(ServerId serverId)
        {
            _first.ConnectionPoolBeforeEnteringWaitQueue(serverId);
            _second.ConnectionPoolBeforeEnteringWaitQueue(serverId);
        }

        public void ConnectionPoolAfterEnteringWaitQueue(ServerId serverId, TimeSpan elapsed)
        {
            _first.ConnectionPoolAfterEnteringWaitQueue(serverId, elapsed);
            _second.ConnectionPoolAfterEnteringWaitQueue(serverId, elapsed);
        }

        public void ConnectionPoolErrorEnteringWaitQueue(ServerId serverId, TimeSpan elapsed, Exception exception)
        {
            _first.ConnectionPoolErrorEnteringWaitQueue(serverId, elapsed, exception);
            _second.ConnectionPoolErrorEnteringWaitQueue(serverId, elapsed, exception);
        }

        public void ConnectionPoolBeforeCheckingOutAConnection(ServerId serverId)
        {
            _first.ConnectionPoolBeforeCheckingOutAConnection(serverId);
            _second.ConnectionPoolBeforeCheckingOutAConnection(serverId);
        }

        public void ConnectionPoolAfterCheckingOutAConnection(ConnectionId connectionId, TimeSpan elapsed)
        {
            _first.ConnectionPoolAfterCheckingOutAConnection(connectionId, elapsed);
            _second.ConnectionPoolAfterCheckingOutAConnection(connectionId, elapsed);
        }

        public void ConnectionPoolErrorCheckingOutAConnection(ServerId serverId, TimeSpan elapsed, Exception ex)
        {
            _first.ConnectionPoolErrorCheckingOutAConnection(serverId, elapsed, ex);
            _second.ConnectionPoolErrorCheckingOutAConnection(serverId, elapsed, ex);
        }

        public void ConnectionPoolBeforeCheckingInAConnection(ConnectionId connectionId)
        {
            _first.ConnectionPoolBeforeCheckingInAConnection(connectionId);
            _second.ConnectionPoolBeforeCheckingInAConnection(connectionId);
        }

        public void ConnectionPoolAfterCheckingInAConnection(ConnectionId connectionId, TimeSpan elapsed)
        {
            _first.ConnectionPoolAfterCheckingInAConnection(connectionId, elapsed);
            _second.ConnectionPoolAfterCheckingInAConnection(connectionId, elapsed);
        }
    }
}