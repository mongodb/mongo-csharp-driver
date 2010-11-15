/* Copyright 2010 10gen Inc.
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
using System.Text;
using System.Threading;

using MongoDB.Bson;

namespace MongoDB.Driver.Internal {
    internal class DirectConnector {
        #region private fields
        private MongoUrl url;
        private MongoServerAddress address;
        private MongoConnection connection;
        private bool isPrimary;
        #endregion

        #region constructors
        public DirectConnector(
            MongoUrl url
        ) {
            this.url = url;
        }
        #endregion

        #region public properties
        public MongoServerAddress Address {
            get { return address; }
        }

        public MongoConnection Connection {
            get { return connection; }
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
            foreach (var address in url.Servers) {
                try {
                    Connect(address, timeout);
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
            MongoServerAddress address,
            TimeSpan timeout
        ) {
            var connection = new MongoConnection(null, address); // no connection pool
            bool isPrimary;

            try {
                var command = new BsonDocument("ismaster", 1);
                using (
                    var message = new MongoQueryMessage<BsonDocument>(
                        "admin.$cmd",
                        QueryFlags.SlaveOk,
                        0, // numberToSkip
                        1, // numberToReturn
                        command,
                        null // fields
                    )
                ) {
                    connection.SendMessage(message, SafeMode.False);
                }
                var reply = connection.ReceiveMessage<BsonDocument>();
                var commandResult = reply.Documents[0];
                isPrimary =
                    commandResult["ok", false].ToBoolean() &&
                    commandResult["ismaster", false].ToBoolean();
                if (!isPrimary && !url.SlaveOk) {
                    throw new MongoConnectionException("Server is not a primary and SlaveOk is false");
                }
            } catch {
                try { connection.Close(); } catch { } // ignore exceptions
                throw;
            }

            this.address = address;
            this.connection = connection;
            this.isPrimary = isPrimary;
        }
        #endregion
    }
}
