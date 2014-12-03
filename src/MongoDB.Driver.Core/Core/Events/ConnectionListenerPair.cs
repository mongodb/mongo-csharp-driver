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
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;

namespace MongoDB.Driver.Core.Events
{
    internal class ConnectionListenerPair : IConnectionListener
    {
        // static
        public static IConnectionListener Create(IConnectionListener first, IConnectionListener second)
        {
            if (first == null)
            {
                return second;
            }

            if (second == null)
            {
                return first;
            }

            return new ConnectionListenerPair(first, second);
        }

        // fields
        private readonly IConnectionListener _first;
        private readonly IConnectionListener _second;

        // constructors
        public ConnectionListenerPair(IConnectionListener first, IConnectionListener second)
        {
            _first = Ensure.IsNotNull(first, "first");
            _second = Ensure.IsNotNull(second, "second");
        }

        // methods
        public void ConnectionFailed(ConnectionFailedEvent @event)
        {
            _first.ConnectionFailed(@event);
            _second.ConnectionFailed(@event);
        }

        public void ConnectionBeforeClosing(ConnectionBeforeClosingEvent @event)
        {
            _first.ConnectionBeforeClosing(@event);
            _second.ConnectionBeforeClosing(@event);
        }

        public void ConnectionAfterClosing(ConnectionAfterClosingEvent @event)
        {
            _first.ConnectionAfterClosing(@event);
            _second.ConnectionAfterClosing(@event);
        }

        public void ConnectionBeforeOpening(ConnectionBeforeOpeningEvent @event)
        {
            _first.ConnectionBeforeOpening(@event);
            _second.ConnectionBeforeOpening(@event);
        }

        public void ConnectionAfterOpening(ConnectionAfterOpeningEvent @event)
        {
            _first.ConnectionAfterOpening(@event);
            _second.ConnectionAfterOpening(@event);
        }

        public void ConnectionErrorOpening(ConnectionErrorOpeningEvent @event)
        {
            _first.ConnectionErrorOpening(@event);
            _second.ConnectionErrorOpening(@event);
        }

        public void ConnectionBeforeReceivingMessage(ConnectionBeforeReceivingMessageEvent @event)
        {
            _first.ConnectionBeforeReceivingMessage(@event);
            _second.ConnectionBeforeReceivingMessage(@event);
        }

        public void ConnectionAfterReceivingMessage<T>(ConnectionAfterReceivingMessageEvent<T> @event)
        {
            _first.ConnectionAfterReceivingMessage<T>(@event);
            _second.ConnectionAfterReceivingMessage<T>(@event);
        }

        public void ConnectionErrorReceivingMessage(ConnectionErrorReceivingMessageEvent @event)
        {
            _first.ConnectionErrorReceivingMessage(@event);
            _second.ConnectionErrorReceivingMessage(@event);
        }

        public void ConnectionBeforeSendingMessages(ConnectionBeforeSendingMessagesEvent @event)
        {
            _first.ConnectionBeforeSendingMessages(@event);
            _second.ConnectionBeforeSendingMessages(@event);
        }

        public void ConnectionAfterSendingMessages(ConnectionAfterSendingMessagesEvent @event)
        {
            _first.ConnectionAfterSendingMessages(@event);
            _second.ConnectionAfterSendingMessages(@event);
        }

        public void ConnectionErrorSendingMessages(ConnectionErrorSendingMessagesEvent @event)
        {
            _first.ConnectionErrorSendingMessages(@event);
            _second.ConnectionErrorSendingMessages(@event);
        }
    }
}
