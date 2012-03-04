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
using System.Threading;

namespace MongoDB.Driver.Internal
{
    /// <summary>
    /// Represents a pool of connections to a MongoDB server.
    /// </summary>
    public class MongoConnectionPool
    {
        // private fields
        private object _connectionPoolLock = new object();
        private MongoServer _server;
        private MongoServerInstance _serverInstance;
        private int _poolSize;
        private List<MongoConnection> _availableConnections = new List<MongoConnection>();
        private int _generationId; // whenever the pool is cleared the generationId is incremented
        private int _waitQueueSize;
        private Timer _timer;
        private bool _inTimerCallback;
        private bool _inEnsureMinConnectionPoolSizeWorkItem;
        private int _connectionsRemovedSinceLastTimerTick;

        // constructors
        internal MongoConnectionPool(MongoServerInstance serverInstance)
        {
            _server = serverInstance.Server;
            _serverInstance = serverInstance;
            _poolSize = 0;

            var dueTime = TimeSpan.FromSeconds(0);
            var period = TimeSpan.FromSeconds(10);
            _timer = new Timer(TimerCallback, null, dueTime, period);
        }

        // public properties
        /// <summary>
        /// Gets the number of available connections (connections that are open but not currently in use).
        /// </summary>
        public int AvailableConnectionsCount
        {
            get { return _availableConnections.Count; }
        }

        /// <summary>
        /// Gets the number of connections in the connection pool (includes both available and in use connections).
        /// </summary>
        public int CurrentPoolSize
        {
            get { return _poolSize; }
        }

        /// <summary>
        /// Gets the current generation Id of the connection pool.
        /// </summary>
        public int GenerationId
        {
            get { return _generationId; }
        }

        /// <summary>
        /// Gets the server instance.
        /// </summary>
        public MongoServerInstance ServerInstance
        {
            get { return _serverInstance; }
        }

        // internal methods
        internal MongoConnection AcquireConnection(MongoDatabase database)
        {
            if (database != null && database.Server != _server)
            {
                throw new ArgumentException("This connection pool is for a different server.", "database");
            }

            lock (_connectionPoolLock)
            {
                if (_waitQueueSize >= _server.Settings.WaitQueueSize)
                {
                    throw new MongoConnectionException("Too many threads are already waiting for a connection.");
                }

                _waitQueueSize += 1;
                try
                {
                    DateTime timeoutAt = DateTime.UtcNow + _server.Settings.WaitQueueTimeout;
                    while (true)
                    {
                        if (_availableConnections.Count > 0)
                        {
                            // first try to find the most recently used connection that is already authenticated for this database
                            for (int i = _availableConnections.Count - 1; i >= 0; i--)
                            {
                                if (_availableConnections[i].IsAuthenticated(database))
                                {
                                    var connection = _availableConnections[i];
                                    _availableConnections.RemoveAt(i);
                                    return connection;
                                }
                            }

                            // otherwise find the most recently used connection that can be authenticated for this database
                            for (int i = _availableConnections.Count - 1; i >= 0; i--)
                            {
                                if (_availableConnections[i].CanAuthenticate(database))
                                {
                                    var connection = _availableConnections[i];
                                    _availableConnections.RemoveAt(i);
                                    return connection;
                                }
                            }

                            // otherwise replace the least recently used connection with a brand new one
                            // if this happens a lot the connection pool size should be increased
                            _availableConnections[0].Close();
                            _availableConnections.RemoveAt(0);
                            return new MongoConnection(this);
                        }

                        // create a new connection if maximum pool size has not been reached
                        if (_poolSize < _server.Settings.MaxConnectionPoolSize)
                        {
                            // make sure connection is created successfully before incrementing poolSize
                            // connection will be opened later outside of the lock
                            var connection = new MongoConnection(this);
                            _poolSize += 1;
                            return connection;
                        }

                        // wait for a connection to be released
                        var timeRemaining = timeoutAt - DateTime.UtcNow;
                        if (timeRemaining > TimeSpan.Zero)
                        {
                            Monitor.Wait(_connectionPoolLock, timeRemaining);
                        }
                        else
                        {
                            throw new TimeoutException("Timeout waiting for a MongoConnection.");
                        }
                    }
                }
                finally
                {
                    _waitQueueSize -= 1;
                }
            }
        }

        internal void Clear()
        {
            lock (_connectionPoolLock)
            {
                foreach (var connection in _availableConnections)
                {
                    connection.Close();
                }
                _availableConnections.Clear();
                _poolSize = 0;
                _generationId += 1;
                Monitor.Pulse(_connectionPoolLock);
            }
        }

        internal void EnsureMinConnectionPoolSizeWorkItem(object state)
        {
            // make sure only one instance of EnsureMinConnectionPoolSizeWorkItem is running at a time
            if (_inEnsureMinConnectionPoolSizeWorkItem)
            {
                return;
            }

            _inEnsureMinConnectionPoolSizeWorkItem = true;
            try
            {
                // keep creating connections one at a time until MinConnectionPoolSize is reached
                var forGenerationId = (int)state;
                while (true)
                {
                    lock (_connectionPoolLock)
                    {
                        // stop if the connection pool generationId has changed or we have already reached MinConnectionPoolSize
                        if (_generationId != forGenerationId || _poolSize >= _server.Settings.MinConnectionPoolSize)
                        {
                            return;
                        }
                    }

                    var connection = new MongoConnection(this);
                    try
                    {
                        connection.Open();

                        // compare against MaxConnectionPoolSize instead of MinConnectionPoolSize
                        // because while we were opening this connection many others may have already been created
                        // and we don't want to throw this one away unless we would exceed MaxConnectionPoolSize
                        var added = false;
                        lock (_connectionPoolLock)
                        {
                            if (_generationId == forGenerationId && _poolSize < _server.Settings.MaxConnectionPoolSize)
                            {
                                _availableConnections.Add(connection);
                                _poolSize++;
                                added = true;
                                Monitor.Pulse(_connectionPoolLock);
                            }
                        }

                        if (!added)
                        {
                            // turns out we couldn't use the connection after all
                            connection.Close();
                        }
                    }
                    catch
                    {
                        // TODO: log exception?
                        // wait a bit before trying again
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                    }
                }
            }
            catch
            {
                // don't let unhandled exceptions leave EnsureMinConnectionPoolSizeWorkItem
                // if the minimum connection pool size was not achieved a new work item will be queued shortly
                // TODO: log exception?
            }
            finally
            {
                _inEnsureMinConnectionPoolSizeWorkItem = false;
            }
        }

        internal void ReleaseConnection(MongoConnection connection)
        {
            if (connection.ConnectionPool != this)
            {
                throw new ArgumentException("The connection being released does not belong to this connection pool.", "connection");
            }

            // if the connection is no longer open remove it from the pool
            if (connection.State != MongoConnectionState.Open)
            {
                RemoveConnection(connection);
                return;
            }

            // don't put connections that have reached their maximum lifetime back in the pool
            // but only remove one connection at most per timer tick to avoid connection storms
            if (_connectionsRemovedSinceLastTimerTick == 0)
            {
                if (DateTime.UtcNow - connection.CreatedAt > _server.Settings.MaxConnectionLifeTime)
                {
                    RemoveConnection(connection);
                    return;
                }
            }

            var connectionIsFromAnotherGeneration = false;
            lock (_connectionPoolLock)
            {
                if (connection.GenerationId == _generationId)
                {
                    connection.LastUsedAt = DateTime.UtcNow;
                    _availableConnections.Add(connection);
                    Monitor.Pulse(_connectionPoolLock);
                }
                else
                {
                    connectionIsFromAnotherGeneration = true;
                }
            }

            // if connection is from another generation of the pool just close it
            if (connectionIsFromAnotherGeneration)
            {
                connection.Close();
            }
        }

        // private methods
        private void RemoveConnection(MongoConnection connection)
        {
            lock (_connectionPoolLock)
            {
                // even though we may have checked the GenerationId once before getting here it might have changed since
                if (connection.GenerationId == _generationId)
                {
                    _availableConnections.Remove(connection); // it might or might not be in availableConnections (but remove it if it is)
                    _poolSize -= 1;
                    _connectionsRemovedSinceLastTimerTick += 1;
                    Monitor.Pulse(_connectionPoolLock);
                }
            }

            // close connection outside of lock
            connection.Close();
        }

        private void TimerCallback(object state)
        {
            // make sure only one instance of TimerCallback is running at a time
            if (_inTimerCallback)
            {
                // Console.WriteLine("MongoConnectionPool[{0}] TimerCallback skipped because previous callback has not completed.", serverInstance.SequentialId);
                return;
            }

            // Console.WriteLine("MongoConnectionPool[{0}]: TimerCallback called.", serverInstance.SequentialId);
            _inTimerCallback = true;
            try
            {
                var server = _serverInstance.Server;
                if (server.State == MongoServerState.Disconnected || server.State == MongoServerState.Disconnecting)
                {
                    return;
                }

                // on every timer callback verify the state of the server instance because it might have changed
                // we do this even if this one instance is currently Disconnected so we can discover when a disconnected instance comes back online
                _serverInstance.VerifyState();

                // note: the state could have changed to Disconnected when VerifyState was called
                if (_serverInstance.State == MongoServerState.Disconnected)
                {
                    return;
                }

                MongoConnection connectionToRemove = null;
                lock (_connectionPoolLock)
                {
                    // only remove one connection per timer tick to avoid reconnection storms
                    if (_connectionsRemovedSinceLastTimerTick == 0)
                    {
                        MongoConnection oldestConnection = null;
                        MongoConnection lruConnection = null;
                        foreach (var connection in _availableConnections)
                        {
                            if (oldestConnection == null || connection.CreatedAt < oldestConnection.CreatedAt)
                            {
                                oldestConnection = connection;
                            }
                            if (lruConnection == null || connection.LastUsedAt < lruConnection.LastUsedAt)
                            {
                                lruConnection = connection;
                            }
                        }

                        // remove old connections before idle connections
                        var now = DateTime.UtcNow;
                        if (oldestConnection != null && now > oldestConnection.CreatedAt + server.Settings.MaxConnectionLifeTime)
                        {
                            connectionToRemove = oldestConnection;
                        }
                        else if (_poolSize > server.Settings.MinConnectionPoolSize && lruConnection != null && now > lruConnection.LastUsedAt + server.Settings.MaxConnectionIdleTime)
                        {
                            connectionToRemove = lruConnection;
                        }
                    }
                    _connectionsRemovedSinceLastTimerTick = 0;
                }

                // remove connection (if any) outside of lock
                if (connectionToRemove != null)
                {
                    RemoveConnection(connectionToRemove);
                }

                if (_poolSize < server.Settings.MinConnectionPoolSize)
                {
                    ThreadPool.QueueUserWorkItem(EnsureMinConnectionPoolSizeWorkItem, _generationId);
                }
            }
            catch
            {
                // don't let any unhandled exceptions leave TimerCallback
                // server state will already have been change by earlier exception handling
                // TODO: log exception?
            }
            finally
            {
                _inTimerCallback = false;
            }
        }
    }
}
