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
        public void Failed(ConnectionFailedEvent @event)
        {
            _first.Failed(@event);
            _second.Failed(@event);
        }

        public void BeforeClosing(ConnectionBeforeClosingEvent @event)
        {
            _first.BeforeClosing(@event);
            _second.BeforeClosing(@event);
        }

        public void AfterClosing(ConnectionAfterClosingEvent @event)
        {
            _first.AfterClosing(@event);
            _second.AfterClosing(@event);
        }

        public void BeforeOpening(ConnectionBeforeOpeningEvent @event)
        {
            _first.BeforeOpening(@event);
            _second.BeforeOpening(@event);
        }

        public void AfterOpening(ConnectionAfterOpeningEvent @event)
        {
            _first.AfterOpening(@event);
            _second.AfterOpening(@event);
        }

        public void ErrorOpening(ConnectionErrorOpeningEvent @event)
        {
            _first.ErrorOpening(@event);
            _second.ErrorOpening(@event);
        }

        public void BeforeReceivingMessage(ConnectionBeforeReceivingMessageEvent @event)
        {
            _first.BeforeReceivingMessage(@event);
            _second.BeforeReceivingMessage(@event);
        }

        public void AfterReceivingMessage<T>(ConnectionAfterReceivingMessageEvent<T> @event)
        {
            _first.AfterReceivingMessage<T>(@event);
            _second.AfterReceivingMessage<T>(@event);
        }

        public void ErrorReceivingMessage(ConnectionErrorReceivingMessageEvent @event)
        {
            _first.ErrorReceivingMessage(@event);
            _second.ErrorReceivingMessage(@event);
        }

        public void BeforeSendingMessages(ConnectionBeforeSendingMessagesEvent @event)
        {
            _first.BeforeSendingMessages(@event);
            _second.BeforeSendingMessages(@event);
        }

        public void AfterSendingMessages(ConnectionAfterSendingMessagesEvent @event)
        {
            _first.AfterSendingMessages(@event);
            _second.AfterSendingMessages(@event);
        }

        public void ErrorSendingMessages(ConnectionErrorSendingMessagesEvent @event)
        {
            _first.ErrorSendingMessages(@event);
            _second.ErrorSendingMessages(@event);
        }
    }
}
