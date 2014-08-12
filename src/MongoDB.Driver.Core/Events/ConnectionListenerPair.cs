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
using MongoDB.Driver.Core.WireProtocol.Messages;

namespace MongoDB.Driver.Core.Events
{
    internal class ConnectionListenerPair : IConnectionListener
    {
        // static
        public static IConnectionListener Create(IConnectionListener first, IConnectionListener second)
        {
            if(first == null)
            {
                return second;
            }

            if(second == null)
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
        public void ConnectionFailed(ConnectionId connectionId, Exception exception)
        {
            _first.ConnectionFailed(connectionId, exception);
            _second.ConnectionFailed(connectionId, exception);
        }

        public void ConnectionBeforeClosing(ConnectionId connectionId)
        {
            _first.ConnectionBeforeClosing(connectionId);
            _second.ConnectionBeforeClosing(connectionId);
        }

        public void ConnectionAfterClosing(ConnectionId connectionId)
        {
            _first.ConnectionAfterClosing(connectionId);
            _second.ConnectionAfterClosing(connectionId);
        }

        public void ConnectionBeforeOpening(ConnectionId connectionId, ConnectionSettings settings)
        {
            _first.ConnectionBeforeOpening(connectionId, settings);
            _second.ConnectionBeforeOpening(connectionId, settings);
        }

        public void ConnectionAfterOpening(ConnectionId connectionId, ConnectionSettings settings, TimeSpan elapsed)
        {
            _first.ConnectionAfterOpening(connectionId, settings, elapsed);
            _second.ConnectionAfterOpening(connectionId, settings, elapsed);
        }

        public void ConnectionErrorOpening(ConnectionId connectionId, Exception exception)
        {
            _first.ConnectionErrorOpening(connectionId, exception);
            _second.ConnectionErrorOpening(connectionId, exception);
        }

        public void ConnectionBeforeReceivingMessage(ConnectionId connectionId, int responseTo)
        {
            _first.ConnectionBeforeReceivingMessage(connectionId, responseTo);
            _second.ConnectionBeforeReceivingMessage(connectionId, responseTo);
        }

        public void ConnectionAfterReceivingMessage<T>(ConnectionId connectionId, ReplyMessage<T> message, int length, TimeSpan elapsed)
        {
            _first.ConnectionAfterReceivingMessage<T>(connectionId, message, length, elapsed);
            _second.ConnectionAfterReceivingMessage<T>(connectionId, message, length, elapsed);
        }

        public void ConnectionErrorReceivingMessage(ConnectionId connectionId, int responseTo, Exception exception)
        {
            _first.ConnectionErrorReceivingMessage(connectionId, responseTo, exception);
            _second.ConnectionErrorReceivingMessage(connectionId, responseTo, exception);
        }

        public void ConnectionBeforeSendingMessages(ConnectionId connectionId, IReadOnlyList<RequestMessage> messages)
        {
            _first.ConnectionBeforeSendingMessages(connectionId, messages);
            _second.ConnectionBeforeSendingMessages(connectionId, messages);
        }

        public void ConnectionAfterSendingMessages(ConnectionId connectionId, IReadOnlyList<RequestMessage> messages, int length, TimeSpan elapsed)
        {
            _first.ConnectionAfterSendingMessages(connectionId, messages, length, elapsed);
            _second.ConnectionAfterSendingMessages(connectionId, messages, length, elapsed);
        }

        public void ConnectionErrorSendingMessages(ConnectionId connectionId, IReadOnlyList<RequestMessage> messages, Exception exception)
        {
            _first.ConnectionErrorSendingMessages(connectionId, messages, exception);
            _second.ConnectionErrorSendingMessages(connectionId, messages, exception);
        }
    }
}
