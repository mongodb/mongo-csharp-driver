/* Copyright 2010-2013 10gen Inc.
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
using System.Threading;
using MongoDB.Driver.Communication;

namespace MongoDB.Driver.Internal
{
    /// <summary>
    /// Represents a pool of connections to a MongoDB server.
    /// </summary>
    public class MongoConnectionPool
    {
        // private fields
        private object _connectionPoolLock = new object();
        private MongoServerProxySettings _settings;
        private MongoServerInstance _serverInstance;
        private int _poolSize;
        private List<MongoConnection> _availableConnections = new List<MongoConnection>();
        private AcquireConnectionOptions _defaultAcquireConnectionOptions;
        private int _generationId; // whenever the pool is cleared the generationId is incremented
        private int _waitQueueSize;
        private bool _inMaintainPoolSize;
        private bool _inEnsureMinConnectionPoolSizeWorkItem;

        // constructors
        internal MongoConnectionPool(MongoServerInstance serverInstance)
        {
            _settings = serverInstance.Settings;
            _serverInstance = serverInstance;
            _poolSize = 0;

            _defaultAcquireConnectionOptions = new AcquireConnectionOptions
            {
                OkToAvoidWaitingByCreatingNewConnection = true,
                OkToExceedMaxConnectionPoolSize = false,
                OkToExceedWaitQueueSize = false,
                WaitQueueTimeout = _settings.WaitQueueTimeout
            };
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
        internal MongoConnection AcquireConnection()
        {
            return AcquireConnection(_defaultAcquireConnectionOptions);
        }

        internal MongoConnection AcquireConnection(AcquireConnectionOptions options)
        {
            MongoConnection connectionToClose = null;
            try
            {
                DateTime timeoutAt = DateTime.UtcNow + options.WaitQueueTimeout;

                lock (_connectionPoolLock)
                {
                    if (_waitQueueSize >= _settings.WaitQueueSize && !options.OkToExceedWaitQueueSize)
                    {
                        throw new MongoConnectionException("Too many threads are already waiting for a connection.");
                    }

                    _waitQueueSize += 1;
                    try
                    {
                        while (true)
                        {
                            if (_availableConnections.Count > 0)
                            {
                                var connection = _availableConnections[_availableConnections.Count - 1];
                                if (connection.IsExpired())
                                {
                                    connectionToClose = connection;
                                    connection = new MongoConnection(this);
                                }

                                _availableConnections.RemoveAt(_availableConnections.Count - 1);
                                return connection;
                            }

                            // avoid waiting by creating a new connection if options allow it
                            if (options.OkToAvoidWaitingByCreatingNewConnection)
                            {
                                if (_poolSize < _settings.MaxConnectionPoolSize || options.OkToExceedMaxConnectionPoolSize)
                                {
                                    // make sure connection is created successfully before incrementing poolSize
                                    // connection will be opened later outside of the lock
                                    var connection = new MongoConnection(this);
                                    _poolSize += 1;
                                    return connection;
                                }
                            }

                            // wait for a connection to be released
                            var timeRemaining = timeoutAt - DateTime.UtcNow;
                            if (timeRemaining > TimeSpan.Zero)
                            {
                                // other methods should call Monitor.Pulse whenever:
                                // 1. an available connection is added _availableConnections
                                // 2. the _poolSize changes
                                Monitor.Wait(_connectionPoolLock, timeRemaining);
                            }
                            else
                            {
                                if (options.OkToExceedMaxConnectionPoolSize)
                                {
                                    // make sure connection is created successfully before incrementing poolSize
                                    // connection will be opened later outside of the lock
                                    var connection = new MongoConnection(this);
                                    _poolSize += 1;
                                    return connection;
                                }
                                else
                                {
                                    throw new TimeoutException("Timeout waiting for a MongoConnection.");
                                }
                            }
                        }
                    }
                    finally
                    {
                        _waitQueueSize -= 1;
                    }
                }
            }
            finally
            {
                if (connectionToClose != null)
                {
                    try
                    {
                        connectionToClose.Close();
                    }
                    catch
                    {
                        // ignore exceptions
                    }
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

        internal void MaintainPoolSize()
        {
            if (_inMaintainPoolSize)
            {
                return;
            }

            _inMaintainPoolSize = true;
            try
            {
                MongoConnection connectionToClose = null;
                lock (_connectionPoolLock)
                {
                    for (int i = 0; i < _availableConnections.Count; i++)
                    {
                        var connection = _availableConnections[i];
                        if (connection.IsExpired())
                        {
                            _availableConnections.RemoveAt(i);
                            _poolSize -= 1;
                            connectionToClose = connection;
                            Monitor.Pulse(_connectionPoolLock);
                            break;
                        }
                    }
                }

                if (connectionToClose != null)
                {
                    try
                    {
                        connectionToClose.Close();
                    }
                    catch
                    {
                        // ignore exceptions
                    }
                }

                if (_poolSize < _settings.MinConnectionPoolSize)
                {
                    ThreadPool.QueueUserWorkItem(EnsureMinConnectionPoolSizeWorkItem, _generationId);
                }
            }
            finally
            {
                _inMaintainPoolSize = false;
            }
        }

        internal void ReleaseConnection(MongoConnection connection)
        {
            if (connection.ConnectionPool != this)
            {
                throw new ArgumentException("The connection being released does not belong to this connection pool.", "connection");
            }

            // if the connection is no longer open remove it from the pool
            if (connection.State != MongoConnectionState.Open || connection.IsExpired())
            {
                RemoveConnection(connection);
                return;
            }

            var closeConnection = false;
            lock (_connectionPoolLock)
            {
                if (connection.GenerationId == _generationId)
                {
                    if (_poolSize <= _settings.MaxConnectionPoolSize)
                    {
                        _availableConnections.Add(connection);
                    }
                    else
                    {
                        _poolSize -= 1;
                        closeConnection = true;
                    }
                    Monitor.Pulse(_connectionPoolLock);
                }
                else
                {
                    closeConnection = true;
                }
            }

            // if connection is from another generation of the pool just close it
            if (closeConnection)
            {
                connection.Close();
            }
        }

        // private methods
        private void EnsureMinConnectionPoolSizeWorkItem(object state)
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
                        if (_generationId != forGenerationId || _poolSize >= _settings.MinConnectionPoolSize)
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
                            if (_generationId == forGenerationId && _poolSize < _settings.MaxConnectionPoolSize)
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

        private void RemoveConnection(MongoConnection connection)
        {
            lock (_connectionPoolLock)
            {
                // even though we may have checked the GenerationId once before getting here it might have changed since
                if (connection.GenerationId == _generationId)
                {
                    _availableConnections.Remove(connection); // it might or might not be in availableConnections (but remove it if it is)
                    _poolSize -= 1;
                    Monitor.Pulse(_connectionPoolLock);
                }
            }

            // close connection outside of lock
            connection.Close();
        }

        // internal classes
        internal class AcquireConnectionOptions
        {
            public bool OkToAvoidWaitingByCreatingNewConnection;
            public bool OkToExceedMaxConnectionPoolSize;
            public bool OkToExceedWaitQueueSize;
            public TimeSpan WaitQueueTimeout;
        }
    }
}
