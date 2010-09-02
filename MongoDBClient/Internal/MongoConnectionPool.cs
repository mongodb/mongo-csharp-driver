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

namespace MongoDB.MongoDBClient.Internal {
    internal class MongoConnectionPool {
        // TODO: implement a real connection pool
        #region private fields
        private MongoServer server;
        private MongoServerAddress address;
        private List<MongoConnection> pool = new List<MongoConnection>();
        private bool closed;
        #endregion

        #region constructors
        internal MongoConnectionPool(
            MongoServer server,
            MongoServerAddress address
        ) {
            this.server = server;
            this.address = address;
        }
        #endregion

        #region internal properties
        internal MongoServer Server {
            get { return server; }
        }

        internal MongoServerAddress Address {
            get { return address; }
        }
        #endregion

        #region internal methods
        // used only to add the first connection to the pool
        // we don't want to waste the connection made by FindPrimary
        internal void AddConnection(
            MongoConnection connection
        ) {
            connection.ConnectionPool = this;
            pool.Add(connection);
        }

        internal void Close() {
            ThreadPool.QueueUserWorkItem(CloseAllConnections);
            closed = true;
        }

        internal MongoConnection GetConnection(
            MongoDatabase database
        ) {
            if (!object.ReferenceEquals(database.Server, server)) {
                throw new MongoException("This connection pool is for a different server");
            }
            if (closed) {
                throw new MongoException("Attempt to get a connection from a closed connection pool");
            }

            MongoConnection connection;
            if (pool.Count == 0) {
                connection = new MongoConnection(address);
                connection.ConnectionPool = this;
            } else {
                connection = pool[0];
                pool.RemoveAt(0);
            }

            connection.Database = database;
            return connection;
        }

        internal void ReleaseConnection(
            MongoConnection connection
        ) {
            if (connection.ConnectionPool != this) {
                throw new MongoException("The connection being released does not belong to this connection pool.");
            }

            connection.Database = null;
            if (pool.Count < 10 && !closed) {
                pool.Add(connection);
            } else {
                connection.Dispose();
            }
        }
        #endregion

        #region private methods
        // note: this method runs on a thread from the ThreadPool
        private void CloseAllConnections(
            object parameters
        ) {
            try {
                foreach (var connection in pool) {
                    connection.Close();
                }
            } catch { } // ignore exceptions
        }
        #endregion
    }
}
