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
using System.Net;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;

namespace MongoDB.Driver.Core.Events
{
    public class SentMessageEventArgs
    {
        // fields
        private readonly ConnectionId _connectionId;
        private readonly EndPoint _endPoint;
        private readonly Exception _exception;
        private readonly RequestMessage _message;

        // constructors
        public SentMessageEventArgs(EndPoint endPoint, ConnectionId connectionId, RequestMessage message, Exception exception)
        {
            _endPoint = Ensure.IsNotNull(endPoint, "endPoint");
            _connectionId = Ensure.IsNotNull(connectionId, "connectionId");
            _message = Ensure.IsNotNull(message, "message");
            _exception = exception; // can be null
        }

        // properties
        public ConnectionId ConnectionId
        {
            get { return _connectionId; }
        }

        public EndPoint EndPoint
        {
            get { return _endPoint; }
        }

        public Exception Exception
        {
            get { return _exception; }
        }

        public RequestMessage Message
        {
            get { return _message; }
        }
    }
}
