/* Copyright 2010-2012 10gen Inc.
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

using MongoDB.Bson;

namespace MongoDB.Driver.Internal
{
    internal class DirectConnector
    {
        // private fields
        private MongoServer _server;
        private int _connectionAttempt;

        // constructors
        internal DirectConnector(MongoServer server, int connectionAttempt)
        {
            _server = server;
            _connectionAttempt = connectionAttempt;
        }

        // internal methods
        internal void Connect(TimeSpan timeout)
        {
            var exceptions = new List<Exception>();
            foreach (var address in _server.Settings.Servers)
            {
                try
                {
                    var serverInstance = _server.Instance;
                    if (serverInstance.Address != address)
                    {
                        serverInstance.Address = address;
                    }
                    serverInstance.Connect(_server.Settings.SlaveOk); // TODO: what about timeout?
                    return;
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            var firstAddress = _server.Settings.Servers.First();
            var firstException = exceptions.First();
            var message = string.Format("Unable to connect to server {0}: {1}.", firstAddress, firstException.Message);
            var connectionException = new MongoConnectionException(message, firstException);
            connectionException.Data.Add("InnerExceptions", exceptions); // useful when there is more than one
            throw connectionException;
        }
    }
}
