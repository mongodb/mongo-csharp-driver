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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Events
{
    internal class ServerListenerPair : IServerListener
    {
        // static
        public static IServerListener Create(IServerListener first, IServerListener second)
        {
            if (first == null)
            {
                return second;
            }

            if (second == null)
            {
                return first;
            }

            return new ServerListenerPair(first, second);
        }

        // fields
        private readonly IServerListener _first;
        private readonly IServerListener _second;

        // constructors
        public ServerListenerPair(IServerListener first, IServerListener second)
        {
            _first = Ensure.IsNotNull(first, "first");
            _second = Ensure.IsNotNull(second, "second");
        }

        // methods
        public void ServerBeforeClosing(ServerId serverId)
        {
            _first.ServerBeforeClosing(serverId);
            _second.ServerBeforeClosing(serverId);
        }

        public void ServerAfterClosing(ServerId serverId)
        {
            _first.ServerAfterClosing(serverId);
            _second.ServerAfterClosing(serverId);
        }

        public void ServerBeforeOpening(ServerId serverId, ServerSettings settings)
        {
            _first.ServerBeforeOpening(serverId, settings);
            _second.ServerBeforeOpening(serverId, settings);
        }

        public void ServerAfterOpening(ServerId serverId, ServerSettings settings, TimeSpan elapsed)
        {
            _first.ServerAfterOpening(serverId, settings, elapsed);
            _second.ServerAfterOpening(serverId, settings, elapsed);
        }

        public void ServerBeforeHeartbeating(ConnectionId connectionId)
        {
            _first.ServerBeforeHeartbeating(connectionId);
            _second.ServerBeforeHeartbeating(connectionId);
        }

        public void ServerAfterHeartbeating(ConnectionId connectionId, TimeSpan elapsed)
        {
            _first.ServerAfterHeartbeating(connectionId, elapsed);
            _second.ServerAfterHeartbeating(connectionId, elapsed);
        }

        public void ServerErrorHeartbeating(ConnectionId connectionId, Exception exception)
        {
            _first.ServerErrorHeartbeating(connectionId, exception);
            _second.ServerErrorHeartbeating(connectionId, exception);
        }

        public void ServerAfterDescriptionChanged(ServerDescription oldDescription, ServerDescription newDescription)
        {
            _first.ServerAfterDescriptionChanged(oldDescription, newDescription);
            _second.ServerAfterDescriptionChanged(oldDescription, newDescription);
        }
    }
}
