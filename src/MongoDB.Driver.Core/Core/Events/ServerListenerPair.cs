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
        public void BeforeClosing(ServerBeforeClosingEvent @event)
        {
            _first.BeforeClosing(@event);
            _second.BeforeClosing(@event);
        }

        public void AfterClosing(ServerAfterClosingEvent @event)
        {
            _first.AfterClosing(@event);
            _second.AfterClosing(@event);
        }

        public void BeforeOpening(ServerBeforeOpeningEvent @event)
        {
            _first.BeforeOpening(@event);
            _second.BeforeOpening(@event);
        }

        public void AfterOpening(ServerAfterOpeningEvent @event)
        {
            _first.AfterOpening(@event);
            _second.AfterOpening(@event);
        }

        public void BeforeHeartbeating(ServerBeforeHeartbeatingEvent @event)
        {
            _first.BeforeHeartbeating(@event);
            _second.BeforeHeartbeating(@event);
        }

        public void AfterHeartbeating(ServerAfterHeartbeatingEvent @event)
        {
            _first.AfterHeartbeating(@event);
            _second.AfterHeartbeating(@event);
        }

        public void ErrorHeartbeating(ServerErrorHeartbeatingEvent @event)
        {
            _first.ErrorHeartbeating(@event);
            _second.ErrorHeartbeating(@event);
        }

        public void AfterDescriptionChanged(ServerAfterDescriptionChangedEvent @event)
        {
            _first.AfterDescriptionChanged(@event);
            _second.AfterDescriptionChanged(@event);
        }
    }
}
