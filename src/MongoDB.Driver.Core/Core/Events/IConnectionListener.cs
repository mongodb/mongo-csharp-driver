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
    /// <preliminary/>
    /// <summary>
    /// Represents a listener to connection events.
    /// </summary>
    public interface IConnectionListener : IListener
    {
        /// <summary>
        /// An event that occurs when a connection has failed.
        /// </summary>
        /// <param name="event">The event.</param>
        void ConnectionFailed(ConnectionFailedEvent @event);

        /// <summary>
        /// An event that occurs before closing a connection. 
        /// </summary>
        /// <param name="event">The event.</param>
        void ConnectionBeforeClosing(ConnectionBeforeClosingEvent @event);

        /// <summary>
        /// An event that occurs after a connection has been closed. 
        /// </summary>
        /// <param name="event">The event.</param>
        void ConnectionAfterClosing(ConnectionAfterClosingEvent @event);

        /// <summary>
        /// An event that occurs before opening a connection. 
        /// </summary>
        /// <param name="event">The event.</param>
        void ConnectionBeforeOpening(ConnectionBeforeOpeningEvent @event);

        /// <summary>
        /// An event that occurs after a connection has been opened. 
        /// </summary>
        /// <param name="event">The event.</param>
        void ConnectionAfterOpening(ConnectionAfterOpeningEvent @event);

        /// <summary>
        /// An event that occurs when there is an error while opening a connection.
        /// </summary>
        /// <param name="event">The event.</param>
        void ConnectionErrorOpening(ConnectionErrorOpeningEvent @event);

        /// <summary>
        /// An event that occurs before receiving a message on a connection. 
        /// </summary>
        /// <param name="event">The event.</param>
        void ConnectionBeforeReceivingMessage(ConnectionBeforeReceivingMessageEvent @event);

        /// <summary>
        /// An event that occurs after a message has been received on a connection.
        /// </summary>
        /// <param name="event">The event.</param>
        void ConnectionAfterReceivingMessage(ConnectionAfterReceivingMessageEvent @event);

        /// <summary>
        /// An event that occurs when there is an an error while receiving a message.
        /// </summary>
        /// <param name="event">The event.</param>
        void ConnectionErrorReceivingMessage(ConnectionErrorReceivingMessageEvent @event);

        /// <summary>
        /// An event that occurs before sending a set of messages. 
        /// </summary>
        /// <param name="event">The event.</param>
        void ConnectionBeforeSendingMessages(ConnectionBeforeSendingMessagesEvent @event);

        /// <summary>
        /// An event that occurs after a set of message has been sent. 
        /// </summary>
        /// <param name="event">The event.</param>
        void ConnectionAfterSendingMessages(ConnectionAfterSendingMessagesEvent @event);

        /// <summary>
        /// An event that occurs when there is an error while sending a set of messages.
        /// </summary>
        /// <param name="event">The event.</param>
        void ConnectionErrorSendingMessages(ConnectionErrorSendingMessagesEvent @event);
    }
}