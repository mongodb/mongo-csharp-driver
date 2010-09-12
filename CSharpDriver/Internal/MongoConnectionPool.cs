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

namespace MongoDB.CSharpDriver.Internal {
    internal class MongoConnectionPool {
        #region private fields
        private object connectionPoolLock = new object();
        private bool closed = false;
        private MongoServer server;
        private MongoServerAddress address;
        private List<MongoConnection> pool = new List<MongoConnection>();
        private Dictionary<int, Request> requests = new Dictionary<int, Request>(); // tracks threads that have called RequestStart
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

        internal int RequestNestingLevel {
            get {
                int threadId = Thread.CurrentThread.ManagedThreadId;
                Request request;
                if (requests.TryGetValue(threadId, out request)) {
                    return request.NestingLevel;
                } else {
                    return 0;
                }
            }
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
                throw new MongoException("This connection pool is for a different server");
            }
            if (closed) {
                throw new MongoException("Attempt to get a connection from a closed connection pool");
            }

            MongoConnection connection = null;
            lock (connectionPoolLock) {
                // if a thread has called RequestStart it wants all operations to take place on the same connection
                int threadId = Thread.CurrentThread.ManagedThreadId;
                Request request;
                if (requests.TryGetValue(threadId, out request)) {
                    connection = request.Connection;
                }

                // otherwise find the most recently used connection that is already authenticated for this database
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
                connection = new MongoConnection(this, address);
            }

            // be sure connectionPoolLock has been released before calling CheckAuthentication
            try {
                connection.CheckAuthentication(database); // will authenticate if necessary
            } catch (MongoException) {
                // don't let the connection go to waste just because authentication failed
                ReleaseConnection(connection);
                throw;
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
                    // if the thread has called RequestStart just verify that the connection it is releasing is the right one
                    int threadId = Thread.CurrentThread.ManagedThreadId;
                    Request request;
                    if (requests.TryGetValue(threadId, out request)) {
                        if (connection != request.Connection) {
                            throw new MongoException("Connection being released is not the one assigned to the thread by RequestStart");
                        }
                        return;
                    }
                    
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
                    connection.Close();
                }
            }
        }

        internal void RequestDone() {
            lock (connectionPoolLock) {
                int threadId = Thread.CurrentThread.ManagedThreadId;
                Request request;
                if (requests.TryGetValue(threadId, out request)) {
                    if (--request.NestingLevel == 0) {
                        requests.Remove(threadId);
                        ReleaseConnection(request.Connection); // MUST be after request has been removed from requests
                    }
                } else {
                    throw new MongoException("Thread is not in a request (did you call RequestStart?)");
                }
            }
        }

        internal void RequestStart(
            MongoDatabase database
        ) {
            lock (connectionPoolLock) {
                int threadId = Thread.CurrentThread.ManagedThreadId;
                Request request;
                if (requests.TryGetValue(threadId, out request)) {
                    request.NestingLevel++;
                } else {
                    var connection = GetConnection(database);
                    request = new Request(connection);
                    requests.Add(threadId, request);
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

        #region private nested classes
        private class Request {
            #region private fields
            private int nestingLevel;
            private MongoConnection connection;
            #endregion

            #region constructors
            public Request(
                MongoConnection connection
            ) {
                this.nestingLevel = 1;
                this.connection = connection;
            }
            #endregion

            #region public properties
            public int NestingLevel {
                get { return nestingLevel; }
                set { nestingLevel = value; }
            }

            public MongoConnection Connection {
                get { return connection; }
                set { connection = value; }
            }
            #endregion
        }
        #endregion
    }
}
