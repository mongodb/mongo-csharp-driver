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
        void Failed(ConnectionFailedEvent @event);

        void BeforeClosing(ConnectionBeforeClosingEvent @event);
        void AfterClosing(ConnectionAfterClosingEvent @event);
        
        void BeforeOpening(ConnectionBeforeOpeningEvent @event);
        void AfterOpening(ConnectionAfterOpeningEvent @event);
        void ErrorOpening(ConnectionErrorOpeningEvent @event);

        void BeforeReceivingMessage(ConnectionBeforeReceivingMessageEvent @event);
        void AfterReceivingMessage<T>(ConnectionAfterReceivingMessageEvent<T> @event);
        void ErrorReceivingMessage(ConnectionErrorReceivingMessageEvent @event);
        
        void BeforeSendingMessages(ConnectionBeforeSendingMessagesEvent @event);
        void AfterSendingMessages(ConnectionAfterSendingMessagesEvent @event);
        void ErrorSendingMessages(ConnectionErrorSendingMessagesEvent @event);
    }
}