/* Copyright 2010-2011 10gen Inc.
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

namespace MongoDB.Driver.Internal {
    internal class DirectConnector {
        #region private fields
        private MongoServer server;
        #endregion

        #region constructors
        internal DirectConnector(
            MongoServer server
        ) {
            this.server = server;
        }
        #endregion

        #region internal methods
        internal void Connect(
            TimeSpan timeout
        ) {
            server.ClearInstances();

            var exceptions = new List<Exception>();
            foreach (var address in server.Settings.Servers) {
                try {
                    var serverInstance = new MongoServerInstance(server, address);
                    server.AddInstance(serverInstance);
                    try {
                        serverInstance.Connect(server.Settings.SlaveOk); // TODO: what about timeout?
                    } catch {
                        server.RemoveInstance(serverInstance);
                        throw;
                    }

                    return;
                } catch (Exception ex) {
                    exceptions.Add(ex);
                }
            }

            var innerException = exceptions.FirstOrDefault();
            var connectionException = new MongoConnectionException("Unable to connect to server.", innerException);
            if (exceptions.Count > 1) {
                connectionException.Data.Add("exceptions", exceptions);
            }
            throw connectionException;
        }
        #endregion
    }
}
