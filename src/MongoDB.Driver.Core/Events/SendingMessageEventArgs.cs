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

using System.Net;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;

namespace MongoDB.Driver.Core.Events
{
    public class SendingMessageEventArgs
    {
        // fields
        private readonly ConnectionId _connectionId;
        private readonly EndPoint _endPoint;
        private readonly RequestMessage _message;
        private RequestMessage _substituteMessage;
        private ReplyMessage _substituteReply;

        // constructors
        public SendingMessageEventArgs(EndPoint endPoint, ConnectionId connectionId, RequestMessage message)
        {
            _endPoint = Ensure.IsNotNull(endPoint, "endPoint");
            _connectionId = Ensure.IsNotNull(connectionId, "connectionId");
            _message = Ensure.IsNotNull(message, "message");
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

        public RequestMessage Message
        {
            get { return _message; }
        }

        public RequestMessage SubstituteMessage
        {
            get { return _substituteMessage; }
            set { _substituteMessage = value; }
        }

        public ReplyMessage SubstituteReply
        {
            get { return _substituteReply; }
            set { _substituteReply = value; }
        }
    }
}
