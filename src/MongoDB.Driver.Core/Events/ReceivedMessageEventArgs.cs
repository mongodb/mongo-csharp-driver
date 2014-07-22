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
    public class ReceivedMessageEventArgs
    {
        // fields
        private readonly ConnectionId _connectionId;
        private readonly DnsEndPoint _endPoint;
        private readonly Exception _exception;
        private readonly ReplyMessage _reply;
        private ReplyMessage _substituteReply;

        // constructors
        public ReceivedMessageEventArgs(DnsEndPoint endPoint, ConnectionId connectionId, ReplyMessage reply)
        {
            _endPoint = Ensure.IsNotNull(endPoint, "endPoint");
            _connectionId = Ensure.IsNotNull(connectionId, "connectionId");
            _reply = Ensure.IsNotNull(reply, "reply");
        }

        public ReceivedMessageEventArgs(DnsEndPoint endPoint, ConnectionId connectionId, Exception exception)
        {
            _endPoint = Ensure.IsNotNull(endPoint, "endPoint");
            _connectionId = connectionId;
            _exception = Ensure.IsNotNull(exception, "exception");
        }

        // properties
        public ConnectionId ConnectionId
        {
            get { return _connectionId; }
        }

        public DnsEndPoint EndPoint
        {
            get { return _endPoint; }
        }

        public Exception Exception
        {
            get { return _exception; }
        }

        public ReplyMessage Reply
        {
            get { return _reply; }
        }

        public ReplyMessage SubstituteReply
        {
            get { return _substituteReply; }
            set { _substituteReply = value; }
        }
    }
}
