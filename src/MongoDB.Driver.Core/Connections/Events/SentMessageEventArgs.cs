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
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;

namespace MongoDB.Driver.Core.Connections.Events
{
    public class SentMessageEventArgs
    {
        // fields
        private readonly ConnectionDescription _connectionDescription;
        private readonly DnsEndPoint _endPoint;
        private readonly Exception _exception;
        private readonly RequestMessage _message;

        // constructors
        public SentMessageEventArgs(DnsEndPoint endPoint, ConnectionDescription connectionDescription, RequestMessage message, Exception exception)
        {
            _endPoint = Ensure.IsNotNull(endPoint, "endPoint");
            _connectionDescription = Ensure.IsNotNull(connectionDescription, "connectionDescription");
            _message = Ensure.IsNotNull(message, "message");
            _exception = exception; // can be null
        }

        // properties
        public ConnectionDescription ConnectionDescription
        {
            get { return _connectionDescription; }
        }

        public DnsEndPoint EndPoint
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
