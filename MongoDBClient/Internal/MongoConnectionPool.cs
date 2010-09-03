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
        #region private fields
        private object connectionPoolLock = new object();
        private bool closed = false;
        private MongoServer server;
        private MongoServerAddress address;
        private List<MongoConnection> pool = new List<MongoConnection>();
        private int maxPoolSize = 10; // TODO: make configurable?
        private TimeSpan maxIdleTime = TimeSpan.FromMinutes(10); // TODO: make configurable?
        #endregion

        #region constructors
        internal MongoConnectionPool(
            MongoServer server,
            MongoServerAddress address,
            MongoConnection firstConnection
        ) {
            this.server = server;
            this.address = address;

            pool.Add(firstConnection);
            firstConnection.JoinConnectionPool(this);
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
        internal void Close() {
            lock (connectionPoolLock) {
                ThreadPool.QueueUserWorkItem(CloseAllConnectionsWorkItem, pool);
                closed = true;
                pool = null;
            }
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

            MongoConnection connection = null;
            lock (connectionPoolLock) {
                // look for the most recently used connection that has the right credentials
                for (int i = pool.Count - 1; i >= 0; i--) {
                    if (pool[i].Credentials == database.Credentials) {
                        connection = pool[i];
                        pool.RemoveAt(i);
                        break;
                    }
                }

                // if no connection with the right credentials was found create a new one
                if (connection == null) {
                    connection = new MongoConnection(this, address, database.Credentials);
                }
            }

            // if we need to authenticate do so only after the connectionPoolLock has been released
            if (database.Credentials != null && !connection.IsAuthenticated(database)) {
                try {
                    connection.Authenticate(database);
                } catch (MongoException) {
                    // don't let the connection go to waste just because one authentication failed
                    ReleaseConnection(connection);
                    throw;
                }
            }

            return connection;
        }

        internal void ReleaseConnection(
            MongoConnection connection
        ) {
            if (connection.ConnectionPool != this) {
                throw new MongoException("The connection being released does not belong to this connection pool.");
            }

            lock (connectionPoolLock) {
                if (!closed) {
                    // close connections that haven't been used for 10 minutes or more (should this be on a timer?)
                    DateTime cutoff = DateTime.UtcNow - maxIdleTime;
                    foreach (var idleConnection in pool.Where(c => c.LastUsed < cutoff).ToList()) {
                        ThreadPool.QueueUserWorkItem(CloseConnectionWorkItem, idleConnection);
                        pool.Remove(idleConnection);
                    }

                    if (pool.Count == maxPoolSize) {
                        ThreadPool.QueueUserWorkItem(CloseConnectionWorkItem, pool[0]); // close oldest connection
                        pool.RemoveAt(0);
                    }

                    connection.LastUsed = DateTime.UtcNow;
                    pool.Add(connection);
                } else {
                    connection.Dispose();
                }
            }
        }
        #endregion

        #region private methods
        // note: this method runs on a thread from the ThreadPool
        private void CloseAllConnectionsWorkItem(
            object parameters
        ) {
            try {
                var pool = (List<MongoConnection>) parameters;
                foreach (var connection in pool) {
                    connection.Close();
                }
            } catch { } // ignore exceptions
        }

        // note: this method runs on a thread from the ThreadPool
        private void CloseConnectionWorkItem(
            object parameters
        ) {
            try {
                var connection = (MongoConnection) parameters;
                connection.Close();
            } catch { } // ignore exceptions
        }
        #endregion
    }
}
