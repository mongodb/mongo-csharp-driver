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
using System.Net;
using System.Text;
using System.Threading;

namespace MongoDB.Driver.Internal {
    internal class MongoConnectionPool {
        #region private fields
        private object connectionPoolLock = new object();
        private bool closed = false;
        private MongoServer server;
        private IPEndPoint endPoint;
        private List<MongoConnection> pool = new List<MongoConnection>();
        private MongoConnectionPoolSettings settings;
        #endregion

        #region constructors
        internal MongoConnectionPool(
            MongoServer server,
            MongoConnection firstConnection
        ) {
            this.server = server;
            this.settings = server.ConnectionPoolSettings;
            this.endPoint = firstConnection.EndPoint;

            pool.Add(firstConnection);
            firstConnection.JoinConnectionPool(this);
        }
        #endregion

        #region internal properties
        internal MongoServer Server {
            get { return server; }
        }

        internal IPEndPoint EndPoint {
            get { return endPoint; }
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
            if (database.Server != server) {
                throw new ArgumentException("This connection pool is for a different server", "database");
            }
            if (closed) {
                throw new InvalidOperationException("Attempt to get a connection from a closed connection pool");
            }

            MongoConnection connection = null;
            lock (connectionPoolLock) {
                if (connection == null) {
                    for (int i = pool.Count - 1; i >= 0; i--) {
                        if (pool[i].IsAuthenticated(database)) {
                            connection = pool[i];
                            pool.RemoveAt(i);
                            break;
                        }
                    }
                }

                // otherwise find the most recently used connection that can be authenticated for this database
                if (connection == null) {
                    for (int i = pool.Count - 1; i >= 0; i--) {
                        if (pool[i].CanAuthenticate(database)) {
                            connection = pool[i];
                            pool.RemoveAt(i);
                            break;
                        }
                    }
                }
            }

            // if we have to create a new connection do it after releasing the connectionPoolLock
            // because it is a slow operation (it opens a TCP connection to the server)
            if (connection == null) {
                connection = new MongoConnection(this, endPoint);
            }

            return connection;
        }

        internal void ReleaseConnection(
            MongoConnection connection
        ) {
            if (connection.ConnectionPool != this) {
                throw new ArgumentException("The connection being released does not belong to this connection pool.", "connection");
            }

            // don't put closed connections back in the connection pool
            if (connection.Closed) {
                return;
            }

            lock (connectionPoolLock) {
                if (!closed) {
                    // close connections that haven't been used for 10 minutes or more (should this be on a timer?)
                    DateTime cutoff = DateTime.UtcNow - settings.MaxConnectionIdleTime;
                    foreach (var idleConnection in pool.Where(c => c.LastUsed < cutoff).ToList()) {
                        ThreadPool.QueueUserWorkItem(CloseConnectionWorkItem, idleConnection);
                        pool.Remove(idleConnection);
                    }

                    if (pool.Count == settings.MaxConnectionPoolSize) {
                        ThreadPool.QueueUserWorkItem(CloseConnectionWorkItem, pool[0]); // close oldest connection
                        pool.RemoveAt(0);
                    }

                    connection.LastUsed = DateTime.UtcNow;
                    pool.Add(connection);
                } else {
                    connection.Close();
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
