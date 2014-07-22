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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.WireProtocol.Messages;

namespace MongoDB.Driver.Core.Connections
{
    public class ConnectionId
    {
        #region static
        // static fields
        private static int __lastDriverConnectionId;

        // static methods
        public static ConnectionId CreateConnectionId()
        {
            var driverConnectionId = Interlocked.Increment(ref __lastDriverConnectionId);
            return new ConnectionId(driverConnectionId, 0);
        }
        #endregion

        // fields
        private readonly int _driverConnectionId;
        private int _serverConnectionId;

        // constructors
        public ConnectionId(int driverConnectionId, int serverConnectionId)
        {
            _driverConnectionId = driverConnectionId;
            _serverConnectionId = serverConnectionId;
        }

        // properties
        public int DriverConnectionId
        {
            get { return _driverConnectionId; }
        }

        public int ServerConnectionId
        {
            get { return _serverConnectionId; }
        }

        // methods
        public ConnectionId WithServerConnectionId(int serverConnectionId)
        {
            return new ConnectionId(_driverConnectionId, serverConnectionId);
        }
    }
}
