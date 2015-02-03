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
    /// <preliminary/>
    /// <summary>
    /// Represents a pair of server listeners. All events will be forwarded to both listeners.
    /// </summary>
    public class ServerListenerPair : IServerListener
    {
        // static
        /// <summary>
        /// Combines two server listeners.
        /// </summary>
        /// <param name="first">The first server listener, or null.</param>
        /// <param name="second">The second server listener, or null.</param>
        /// <returns>A combined server listener.</returns>
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
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerListenerPair"/> class.
        /// </summary>
        /// <param name="first">The first server listener.</param>
        /// <param name="second">The second server listener.</param>
        public ServerListenerPair(IServerListener first, IServerListener second)
        {
            _first = Ensure.IsNotNull(first, "first");
            _second = Ensure.IsNotNull(second, "second");
        }

        // methods
        /// <inheritdoc/>
        public void ServerBeforeClosing(ServerBeforeClosingEvent @event)
        {
            _first.ServerBeforeClosing(@event);
            _second.ServerBeforeClosing(@event);
        }

        /// <inheritdoc/>
        public void ServerAfterClosing(ServerAfterClosingEvent @event)
        {
            _first.ServerAfterClosing(@event);
            _second.ServerAfterClosing(@event);
        }

        /// <inheritdoc/>
        public void ServerBeforeOpening(ServerBeforeOpeningEvent @event)
        {
            _first.ServerBeforeOpening(@event);
            _second.ServerBeforeOpening(@event);
        }

        /// <inheritdoc/>
        public void ServerAfterOpening(ServerAfterOpeningEvent @event)
        {
            _first.ServerAfterOpening(@event);
            _second.ServerAfterOpening(@event);
        }

        /// <inheritdoc/>
        public void ServerBeforeHeartbeating(ServerBeforeHeartbeatingEvent @event)
        {
            _first.ServerBeforeHeartbeating(@event);
            _second.ServerBeforeHeartbeating(@event);
        }

        /// <inheritdoc/>
        public void ServerAfterHeartbeating(ServerAfterHeartbeatingEvent @event)
        {
            _first.ServerAfterHeartbeating(@event);
            _second.ServerAfterHeartbeating(@event);
        }

        /// <inheritdoc/>
        public void ServerErrorHeartbeating(ServerErrorHeartbeatingEvent @event)
        {
            _first.ServerErrorHeartbeating(@event);
            _second.ServerErrorHeartbeating(@event);
        }

        /// <inheritdoc/>
        public void ServerAfterDescriptionChanged(ServerAfterDescriptionChangedEvent @event)
        {
            _first.ServerAfterDescriptionChanged(@event);
            _second.ServerAfterDescriptionChanged(@event);
        }
    }
}
