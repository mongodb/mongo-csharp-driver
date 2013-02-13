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
using System.Collections.ObjectModel;
using System.Linq;

namespace MongoDB.Driver.Internal
{
    /// <summary>
    /// A proxy that dynamically discovers the type of server it is connecting to.
    /// </summary>
    internal sealed class DiscoveringMongoServerProxy : IMongoServerProxy
    {
        private readonly object _lock = new object();
        private readonly int _sequentialId;
        private readonly MongoServerProxySettings _settings;
        private readonly ReadOnlyCollection<MongoServerInstance> _instances;

        // volatile will ensure that our reads are not reordered such one could get placed before a write.  This 
        // isn't strictly required for > .NET 2.0 systems, but Mono does not offer the same memory model guarantees,
        // so we code to the ECMA standard.
        private volatile IMongoServerProxy _serverProxy;
        private volatile MongoServerState _state;
        private volatile int _connectionAttempt;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveringMongoServerProxy"/> class.
        /// </summary>
        /// <param name="sequentialId">The sequential id.</param>
        /// <param name="settings">The settings.</param>
        public DiscoveringMongoServerProxy(int sequentialId, MongoServerProxySettings settings)
        {
            _state = MongoServerState.Disconnected;
            _sequentialId = sequentialId;
            _settings = settings;
            _instances = settings.Servers.Select(a => new MongoServerInstance(settings, a)).ToList().AsReadOnly();
        }

        // public properties
        /// <summary>
        /// Gets the build info.
        /// </summary>
        public MongoServerBuildInfo BuildInfo
        {
            get
            {
                if (_serverProxy == null)
                {
                    return null;
                }

                return _serverProxy.BuildInfo;
            }
        }

        /// <summary>
        /// Gets the connection attempt.
        /// </summary>
        public int ConnectionAttempt
        {
            get
            {
                if (_serverProxy == null)
                {
                    return _connectionAttempt;
                }

                return _serverProxy.ConnectionAttempt;
            }
        }

        /// <summary>
        /// Gets the instances.
        /// </summary>
        public ReadOnlyCollection<MongoServerInstance> Instances
        {
            get
            {
                if (_serverProxy == null)
                {
                    return _instances;
                }

                return _serverProxy.Instances;
            }
        }

        /// <summary>
        /// Gets the sequential id assigned to this proxy.
        /// </summary>
        public int SequentialId
        {
            get { return _sequentialId; }
        }
        
        /// <summary>
        /// Gets the state.
        /// </summary>
        public MongoServerState State
        {
            get
            {
                if (_serverProxy == null)
                {
                    return _state;
                }

                return _serverProxy.State;
            }
        }

        // public methods
        /// <summary>
        /// Chooses the server instance.
        /// </summary>
        /// <param name="readPreference">The read preference.</param>
        /// <returns>A MongoServerInstance</returns>
        public MongoServerInstance ChooseServerInstance(ReadPreference readPreference)
        {
            EnsureInstanceManager(_settings.ConnectTimeout);
            return _serverProxy.ChooseServerInstance(readPreference);
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
                EnsureInstanceManager(timeout);
            }
            catch
            {
                _state = MongoServerState.Disconnected;
                throw;
            }
            _serverProxy.Connect(timeout, readPreference);
        }

        /// <summary>
        /// Disconnects this instance.
        /// </summary>
        public void Disconnect()
        {
            if (_serverProxy != null)
            {
                _serverProxy.Disconnect();
            }
        }

        /// <summary>
        /// Pings this instance.
        /// </summary>
        public void Ping()
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
        }

        /// <summary>
        /// Verifies the state.
        /// </summary>
        public void VerifyState()
        {
            // if we have never connected, then our state is correct...
            if (_serverProxy == null)
            {
                return;
            }

            _serverProxy.VerifyState();
        }

        // private methods
        private void EnsureInstanceManager(TimeSpan timeout)
        {
            if (_serverProxy == null)
            {
                lock (_lock)
                {
                    if (_serverProxy == null)
                    {
                        _connectionAttempt++;
                        _state = MongoServerState.Connecting;
                        Discover(timeout);
                    }
                }
            }
        }

        private void Discover(TimeSpan timeout)
        {
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
            lock (_lock)
            {
                if (instance.InstanceType == MongoServerInstanceType.ReplicaSetMember)
                {
                    _serverProxy = new ReplicaSetMongoServerProxy(_sequentialId, _settings, _instances, connectionQueue, _connectionAttempt);
                }
                else if (instance.InstanceType == MongoServerInstanceType.ShardRouter)
                {
                    _serverProxy = new ShardedMongoServerProxy(_sequentialId, _settings, _instances, connectionQueue, _connectionAttempt);
                }
                else if (instance.InstanceType == MongoServerInstanceType.StandAlone)
                {
                    var otherInstances = _instances.Where(x => x != instance).ToList();
                    foreach (var otherInstance in otherInstances)
                    {
                        otherInstance.Disconnect();
                    }

                    _serverProxy = new DirectMongoServerProxy(_sequentialId, _settings, instance, _connectionAttempt);
                }
                else
                {
                    throw new MongoConnectionException("The type of servers in the host list could not be determined.");
                }
            }
        }
    }
}