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

namespace MongoDB.Driver.Internal {
    /// <summary>
    /// Represents a pool of connections to a MongoDB server.
    /// </summary>
    public class MongoConnectionPool {
        #region private fields
        private object connectionPoolLock = new object();
        private MongoServer server;
        private MongoServerInstance serverInstance;
        private int poolSize;
        private List<MongoConnection> availableConnections = new List<MongoConnection>();
        private int generationId; // whenever the pool is cleared the generationId is incremented
        private int waitQueueSize;
        private Timer timer;
        private bool inTimerCallback;
        private bool inEnsureMinConnectionPoolSizeWorkItem;
        private int connectionsRemovedSinceLastTimerTick;
        #endregion

        #region constructors
        internal MongoConnectionPool(
            MongoServerInstance serverInstance
        ) {
            this.server = serverInstance.Server;
            this.serverInstance = serverInstance;
            poolSize = 0;

            var dueTime = TimeSpan.FromSeconds(0);
            var period = TimeSpan.FromSeconds(10);
            timer = new Timer(TimerCallback, null, dueTime, period);
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the number of available connections (connections that are open but not currently in use).
        /// </summary>
        public int AvailableConnectionsCount {
            get { return availableConnections.Count; }
        }

        /// <summary>
        /// Gets the number of connections in the connection pool (includes both available and in use connections).
        /// </summary>
        public int CurrentPoolSize {
            get { return poolSize; }
        }

        /// <summary>
        /// Gets the current generation Id of the connection pool.
        /// </summary>
        public int GenerationId {
            get { return generationId; }
        }

        /// <summary>
        /// Gets the server instance.
        /// </summary>
        public MongoServerInstance ServerInstance {
            get { return serverInstance; }
        }
        #endregion

        #region internal methods
        internal MongoConnection AcquireConnection(
            MongoDatabase database
        ) {
            if (database != null && database.Server != server) {
                throw new ArgumentException("This connection pool is for a different server.", "database");
            }

            lock (connectionPoolLock) {
                if (waitQueueSize >= server.Settings.WaitQueueSize) {
                    throw new MongoConnectionException("Too many threads are already waiting for a connection.");
                }

                waitQueueSize += 1;
                try {
                    DateTime timeoutAt = DateTime.UtcNow + server.Settings.WaitQueueTimeout;
                    while (true) {
                        if (availableConnections.Count > 0) {
                            // first try to find the most recently used connection that is already authenticated for this database
                            for (int i = availableConnections.Count - 1; i >= 0; i--) {
                                if (availableConnections[i].IsAuthenticated(database)) {
                                    var connection = availableConnections[i];
                                    availableConnections.RemoveAt(i);
                                    return connection;
                                }
                            }

                            // otherwise find the most recently used connection that can be authenticated for this database
                            for (int i = availableConnections.Count - 1; i >= 0; i--) {
                                if (availableConnections[i].CanAuthenticate(database)) {
                                    var connection = availableConnections[i];
                                    availableConnections.RemoveAt(i);
                                    return connection;
                                }
                            }

                            // otherwise replace the least recently used connection with a brand new one
                            // if this happens a lot the connection pool size should be increased
                            availableConnections[0].Close();
                            availableConnections.RemoveAt(0);
                            return new MongoConnection(this);
                        }

                        // create a new connection if maximum pool size has not been reached
                        if (poolSize < server.Settings.MaxConnectionPoolSize) {
                            // make sure connection is created successfully before incrementing poolSize
                            var connection = new MongoConnection(this);
                            poolSize += 1;
                            return connection;
                        }

                        // wait for a connection to be released
                        var timeRemaining = timeoutAt - DateTime.UtcNow;
                        if (timeRemaining > TimeSpan.Zero) {
                            Monitor.Wait(connectionPoolLock, timeRemaining);
                        } else {
                            throw new TimeoutException("Timeout waiting for a MongoConnection.");
                        }
                    }
                } finally {
                    waitQueueSize -= 1;
                }
            }
        }

        internal void Clear() {
            lock (connectionPoolLock) {
                foreach (var connection in availableConnections) {
                    connection.Close();
                }
                availableConnections.Clear();
                poolSize = 0;
                generationId += 1;
                Monitor.Pulse(connectionPoolLock);
            }
        }

        internal void EnsureMinConnectionPoolSizeWorkItem(
            object state // forGenerationId
        ) {
            // make sure only one instance of EnsureMinConnectionPoolSizeWorkItem is running at a time
            if (inEnsureMinConnectionPoolSizeWorkItem) {
                return;
            }

            inEnsureMinConnectionPoolSizeWorkItem = true;
            try {
                // keep creating connections one at a time until MinConnectionPoolSize is reached
                var forGenerationId = (int) state;
                while (true) {
                    lock (connectionPoolLock) {
                        // stop if the connection pool generationId has changed or we have already reached MinConnectionPoolSize
                        if (generationId != forGenerationId || poolSize >= server.Settings.MinConnectionPoolSize) {
                            return;
                        }
                    }

                    var connection = new MongoConnection(this);
                    try {
                        connection.Open();

                        // compare against MaxConnectionPoolSize instead of MinConnectionPoolSize
                        // because while we were opening this connection many others may have already been created
                        // and we don't want to throw this one away unless we would exceed MaxConnectionPoolSize
                        var added = false;
                        lock (connectionPoolLock) {
                            if (generationId == forGenerationId && poolSize < server.Settings.MaxConnectionPoolSize) {
                                availableConnections.Add(connection);
                                poolSize++;
                                added = true;
                            }
                        }

                        if (!added) {
                            // turns out we couldn't use the connection after all
                            connection.Close();
                        }
                    } catch {
                        // TODO: log exception?
                        // wait a bit before trying again
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                    }
                }
            } catch {
                // don't let unhandled exceptions leave EnsureMinConnectionPoolSizeWorkItem
                // if the minimum connection pool size was not achieved a new work item will be queued shortly
                // TODO: log exception?
            } finally {
                inEnsureMinConnectionPoolSizeWorkItem = false;
            }
        }

        internal void ReleaseConnection(
            MongoConnection connection
        ) {
            if (connection.ConnectionPool != this) {
                throw new ArgumentException("The connection being released does not belong to this connection pool.", "connection");
            }

            lock (connectionPoolLock) {
                // if connection is from another generation of the pool just close it
                if (connection.GenerationId != generationId) {
                    connection.Close();
                    return;
                }

                // if the connection is no longer open don't remove it from the pool
                if (connection.State != MongoConnectionState.Open) {
                    RemoveConnection(connection);
                    return;
                }

                // don't put connections that have reached their maximum lifetime back in the pool
                // but only remove one connection at most per timer tick to avoid connection storms
                if (connectionsRemovedSinceLastTimerTick == 0) {
                    if (DateTime.UtcNow - connection.CreatedAt > server.Settings.MaxConnectionLifeTime) {
                        RemoveConnection(connection);
                        return;
                    }
                }

                connection.LastUsedAt = DateTime.UtcNow;
                availableConnections.Add(connection);
                Monitor.Pulse(connectionPoolLock);
            }
        }
        #endregion

        #region private methods
        private void RemoveConnection(
            MongoConnection connection
        ) {
            availableConnections.Remove(connection); // it might or might not be in availableConnections (but remove it if it is)
            poolSize -= 1;
            connectionsRemovedSinceLastTimerTick += 1;
            connection.Close();
            Monitor.Pulse(connectionPoolLock);
        }

        private void TimerCallback(
            object state // not used
        ) {
            // make sure only one instance of TimerCallback is running at a time
            if (inTimerCallback) {
                // Console.WriteLine("MongoConnectionPool[{0}] TimerCallback skipped because previous callback has not completed.", serverInstance.SequentialId);
                return;
            }

            // Console.WriteLine("MongoConnectionPool[{0}]: TimerCallback called.", serverInstance.SequentialId);
            inTimerCallback = true;
            try {
                var server = serverInstance.Server;
                if (server.State == MongoServerState.Disconnected || server.State == MongoServerState.Disconnecting) {
                    return;
                }

                // on every timer callback verify the state of the server instance because it might have changed
                // we do this even if this one instance is currently Disconnected so we can discover when a disconnected instance comes back online
                serverInstance.VerifyState();

                lock (connectionPoolLock) {
                    // note: the state could have changed to Disconnected when VerifyState was called
                    if (serverInstance.State == MongoServerState.Disconnected) {
                        return;
                    }

                    // only remove one connection per timer tick to avoid reconnection storms
                    if (connectionsRemovedSinceLastTimerTick == 0) {
                        MongoConnection oldestConnection = null;
                        MongoConnection lruConnection = null;
                        foreach (var connection in availableConnections) {
                            if (oldestConnection == null || connection.CreatedAt < oldestConnection.CreatedAt) {
                                oldestConnection = connection;
                            }
                            if (lruConnection == null || connection.LastUsedAt < lruConnection.LastUsedAt) {
                                lruConnection = connection;
                            }
                        }

                        // remove old connections before idle connections
                        var now = DateTime.UtcNow;
                        if (oldestConnection != null && now > oldestConnection.CreatedAt + server.Settings.MaxConnectionLifeTime) {
                            RemoveConnection(oldestConnection);
                        } else if (poolSize > server.Settings.MinConnectionPoolSize && lruConnection != null && now > lruConnection.LastUsedAt + server.Settings.MaxConnectionIdleTime) {
                            RemoveConnection(lruConnection);
                        }
                    }
                    connectionsRemovedSinceLastTimerTick = 0;
                }

                if (poolSize < server.Settings.MinConnectionPoolSize) {
                    ThreadPool.QueueUserWorkItem(EnsureMinConnectionPoolSizeWorkItem, generationId);
                }
            } catch {
                // don't let any unhandled exceptions leave TimerCallback
                // server state will already have been change by earlier exception handling
                // TODO: log exception?
            } finally {
                inTimerCallback = false;
            }
        }
        #endregion
    }
}
