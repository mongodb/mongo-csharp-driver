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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.WireProtocol.Messages;

namespace MongoDB.Driver.Core.Events
{
    public interface IConnectionListener : IListener
    {
        void ConnectionFailed(ConnectionId connectionId, Exception exception);

        void ConnectionBeforeClosing(ConnectionId connectionId);
        void ConnectionAfterClosing(ConnectionId connectionId);
        
        void ConnectionBeforeOpening(ConnectionId connectionId, ConnectionSettings settings);
        void ConnectionAfterOpening(ConnectionId connectionId, ConnectionSettings settings, TimeSpan elapsed);
        void ConnectionErrorOpening(ConnectionId connectionId, Exception exception);

        void ConnectionBeforeReceivingMessage(ConnectionId connectionId, int responseTo);
        void ConnectionAfterReceivingMessage<T>(ConnectionId connectionId, ReplyMessage<T> message, int length, TimeSpan elapsed);
        void ConnectionErrorReceivingMessage(ConnectionId connectionId, int responseTo, Exception exception);
        
        void ConnectionBeforeSendingMessages(ConnectionId connectionId, IReadOnlyList<RequestMessage> messages);
        void ConnectionAfterSendingMessages(ConnectionId connectionId, IReadOnlyList<RequestMessage> messages, int length, TimeSpan elapsed);
        void ConnectionErrorSendingMessages(ConnectionId connectionId, IReadOnlyList<RequestMessage> messages, Exception exception);
    }
}