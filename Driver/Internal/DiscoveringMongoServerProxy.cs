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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace MongoDB.Driver.Internal
{
    /// <summary>
    /// A proxy that dynamically discovers the type of server it is connecting to.
    /// </summary>
    internal class DiscoveringMongoServerProxy : IMongoServerProxy
    {
        private readonly MongoServer _server;
        private readonly List<MongoServerInstance> _instances;
        private readonly ReaderWriterLockSlim _lock;
        private IMongoServerProxy _serverProxy;
        private MongoServerState _state;
        private int _connectionAttempt;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveringMongoServerProxy"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        public DiscoveringMongoServerProxy(MongoServer server)
        {
            _lock = new ReaderWriterLockSlim();
            _state = MongoServerState.Disconnected;
            _server = server;
            _instances = server.Settings.Servers.Select(a => new MongoServerInstance(server, a)).ToList();
        }

        // public properties
        /// <summary>
        /// Gets the build info.
        /// </summary>
        public MongoServerBuildInfo BuildInfo
        {
            get
            {
                return WithReadLock(() =>
                {
                    if (_serverProxy == null)
                    {
                        return null;
                    }

                    return _serverProxy.BuildInfo;
                });
            }
        }

        /// <summary>
        /// Gets the connection attempt.
        /// </summary>
        public int ConnectionAttempt
        {
            get
            {
                return WithReadLock(() =>
                {
                    if (_serverProxy == null)
                    {
                        return _connectionAttempt;
                    }

                    return _serverProxy.ConnectionAttempt;
                });
            }
        }

        /// <summary>
        /// Gets the instances.
        /// </summary>
        public ReadOnlyCollection<MongoServerInstance> Instances
        {
            get
            {
                return WithReadLock(() =>
                {
                    if (_serverProxy == null)
                    {
                        return _instances.AsReadOnly();
                    }

                    return _serverProxy.Instances;
                });
            }
        }

        /// <summary>
        /// Gets the state.
        /// </summary>
        public MongoServerState State
        {
            get
            {
                return WithReadLock(() =>
                {
                    if (_serverProxy == null)
                    {
                        return _state;
                    }

                    return _serverProxy.State;
                });
            }
        }

        // public methods
        /// <summary>
        /// Chooses the server instance.
        /// </summary>
        /// <param name="slaveOk">if set to <c>true</c> [slave ok].</param>
        /// <returns></returns>
        public MongoServerInstance ChooseServerInstance(ReadPreference readPreference)
        {
            EnsureInstanceManager(_server.Settings.ConnectTimeout);
            return WithReadLock(() => _serverProxy.ChooseServerInstance(readPreference));
        }

        /// <summary>
        /// Connects to the instances respecting the timeout and readPreference.
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        /// <param name="readPreference">The read preference.</param>
        public void Connect(TimeSpan timeout, ReadPreference readPreference)
        {
            try
            {
                WithReadLock(() =>
                {
                    _state = MongoServerState.Connecting;
                });
                EnsureInstanceManager(timeout);
            }
            catch
            {
                WithReadLock(() => _state = MongoServerState.Disconnected);
            }
            WithReadLock(() => _serverProxy.Connect(timeout, readPreference));
        }

        /// <summary>
        /// Disconnects this instance.
        /// </summary>
        public void Disconnect()
        {
            WithReadLock(() =>
            {
                if (_serverProxy != null)
                {
                    _serverProxy.Disconnect();
                }
            });
        }

        /// <summary>
        /// Pings this instance.
        /// </summary>
        public void Ping()
        {
            WithReadLock(() =>
            {
                if (_serverProxy == null)
                {
                    foreach (var instance in _instances)
                    {
                        instance.Ping();
                    }
                }
                else
                {
                    _serverProxy.Ping();
                }
            });
        }

        /// <summary>
        /// Verifies the state.
        /// </summary>
        public void VerifyState()
        {
            WithReadLock(() =>
            {
                // if we have never connected, then our state is correct...
                if (_serverProxy == null)
                {
                    return;
                }

                _serverProxy.VerifyState();
            });
        }

        // private methods
        private T WithReadLock<T>(Func<T> reader)
        {
            _lock.EnterReadLock();
            try
            {
                return reader();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        private void WithReadLock(Action action)
        {
            _lock.EnterReadLock();
            try
            {
                action();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        private void EnsureInstanceManager(TimeSpan timeout)
        {
            _lock.EnterReadLock();
            try
            {
                if (_serverProxy != null)
                {
                    return;
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }

            _lock.EnterWriteLock();
            try
            {
                if (_serverProxy != null)
                {
                    return;
                }
                _connectionAttempt++;
                Discover(timeout);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        private void Discover(TimeSpan timeout)
        {
            // Note: we are already in a write lock here...
            var connectionQueue = new BlockingQueue<MongoServerInstance>();

            for (int i = 0; i < _instances.Count; i++)
            {
                var local = _instances[i];
                connectionQueue.EnqueuWorkItem(() =>
                {
                    try
                    {
                        local.Connect();
                    }
                    catch
                    {
                        // instance is keeping it's last ConnectionException
                    }
                    return local;
                });
            }

            MongoServerInstance instance = null;
            var timeoutAt = DateTime.UtcNow;
            while ((instance = connectionQueue.Dequeue(timeout)) != null)
            {
                if (instance.ConnectException == null)
                {
                    CreateActualProxy(instance, connectionQueue);
                    return;
                }

                timeout = DateTime.UtcNow - timeoutAt;
            }

            throw new MongoConnectionException(string.Format("Unable to connect in the specified timeframe of '{0}'.", timeout));
        }

        private void CreateActualProxy(MongoServerInstance instance, BlockingQueue<MongoServerInstance> connectionQueue)
        {
            // we are already in a write lock here...
            if (instance.Type == MongoServerInstanceType.ReplicaSetMember)
            {
                _serverProxy = new ReplicaSetMongoServerProxy(_server, _instances, connectionQueue, _connectionAttempt);
            }
            else if (instance.Type == MongoServerInstanceType.ShardRouter)
            {
                _serverProxy = new ShardedMongoServerProxy(_server, _instances, connectionQueue, _connectionAttempt);
            }
            else if (instance.Type == MongoServerInstanceType.StandAlone)
            {
                var otherInstances = _instances.Where(x => x != instance).ToList();
                foreach (var otherInstance in otherInstances)
                {
                    otherInstance.Disconnect();
                }

                _serverProxy = new DirectMongoServerProxy(_server, instance, _connectionAttempt);
            }
            else
            {
                throw new MongoConnectionException("The type of servers in the host list could not be determined.");
            }
        }
    }
}