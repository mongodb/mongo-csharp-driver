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
using MongoDB.Driver.Core.WireProtocol.Messages;

namespace MongoDB.Driver.Core.Events
{
    /// <preliminary/>
    /// <summary>
    /// Represents information about a  ConnectionErrorSendingMessages event.
    /// </summary>
    public struct ConnectionErrorSendingMessagesEvent
    {
        private readonly ConnectionId _connectionId;
        private readonly Exception _exception;
        private readonly IReadOnlyList<RequestMessage> _messages;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionErrorSendingMessagesEvent"/> struct.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="messages">The messages.</param>
        /// <param name="exception">The exception.</param>
        public ConnectionErrorSendingMessagesEvent(ConnectionId connectionId, IReadOnlyList<RequestMessage> messages, Exception exception)
        {
            _connectionId = connectionId;
            _messages = messages;
            _exception = exception;
        }

        /// <summary>
        /// Gets the connection identifier.
        /// </summary>
        /// <value>
        /// The connection identifier.
        /// </value>
        public ConnectionId ConnectionId
        {
            get { return _connectionId; }
        }

        /// <summary>
        /// Gets the exception.
        /// </summary>
        /// <value>
        /// The exception.
        /// </value>
        public Exception Exception
        {
            get { return _exception; }
        }

        /// <summary>
        /// Gets the messages.
        /// </summary>
        /// <value>
        /// The messages.
        /// </value>
        public IReadOnlyList<RequestMessage> Messages
        {
            get { return _messages; }
        }
    }
}
