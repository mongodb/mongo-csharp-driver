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
using System.Threading;

using MongoDB.Bson;

namespace MongoDB.Driver.Internal {
    internal class DirectConnector {
        #region private fields
        private MongoServer server;
        private MongoConnection connection;
        private bool isPrimary;
        private int maxDocumentSize;
        private int maxMessageLength;
        #endregion

        #region constructors
        public DirectConnector(
            MongoServer server
        ) {
            this.server = server;
        }
        #endregion

        #region public properties
        public MongoConnection Connection {
            get { return connection; }
        }

        public int MaxDocumentSize {
            get { return maxDocumentSize; }
        }

        public int MaxMessageLength {
            get { return maxMessageLength; }
        }

        public bool IsPrimary {
            get { return isPrimary; }
        }
        #endregion

        #region public methods
        public void Connect(
            TimeSpan timeout
        ) {
            var exceptions = new List<Exception>();
            foreach (var endPoint in server.EndPoints) {
                try {
                    Connect(endPoint, timeout);
                    return;
                } catch (Exception ex) {
                    exceptions.Add(ex);
                }
            }

            var innerException = exceptions.FirstOrDefault();
            var connectionException = new MongoConnectionException("Unable to connect to server", innerException);
            if (exceptions.Count > 1) {
                connectionException.Data.Add("exceptions", exceptions);
            }
            throw connectionException;
        }
        #endregion

        #region private methods
        private void Connect(
            IPEndPoint endPoint,
            TimeSpan timeout
        ) {
            var connection = new MongoConnection(null, endPoint); // no connection pool
            bool isPrimary;

            try {
                var isMasterCommand = new CommandDocument("ismaster", 1);
                var isMasterResult = connection.RunCommand(server, "admin.$cmd", QueryFlags.SlaveOk, isMasterCommand);

                isPrimary = isMasterResult.Response["ismaster", false].ToBoolean();
                if (!isPrimary && !server.Settings.SlaveOk) {
                    throw new MongoConnectionException("Server is not a primary and SlaveOk is false");
                }

                maxDocumentSize = isMasterResult.Response["maxBsonObjectSize", server.MaxDocumentSize].ToInt32();
                maxMessageLength = Math.Max(MongoDefaults.MaxMessageLength, maxDocumentSize + 1024); // derived from maxDocumentSize
            } catch {
                try { connection.Close(); } catch { } // ignore exceptions
                throw;
            }

            this.connection = connection;
            this.isPrimary = isPrimary;
        }
        #endregion
    }
}
