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
    /// Connects to a number of mongos' and distributes load based on ping times.
    /// </summary>
    internal class ShardedMongoServerProxy : IMongoServerProxy
    {
        // private fields
        private readonly object _lock = new object();
        private readonly MongoServer _server;
        private readonly List<MongoServerInstance> _instances;
        private readonly ConnectedInstanceCollection _connectedInstances;
        private readonly BlockingQueue<MongoServerInstance> _stateChangeQueue;
        private readonly Thread _stateChangeThread;
        private int _connectionAttempt;
        private MongoServerState _state;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ShardedMongoServerProxy"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        public ShardedMongoServerProxy(MongoServer server)
        {
            _server = server;
            _instances = new List<MongoServerInstance>();
            _connectedInstances = new ConnectedInstanceCollection();
            _stateChangeQueue = new BlockingQueue<MongoServerInstance>();

            foreach (var address in server.Settings.Servers)
            {
                AddInstance(new MongoServerInstance(server, address));
            }

            _stateChangeThread = new Thread(ProcessStateChanges);
            _stateChangeThread.IsBackground = true;
            _stateChangeThread.Start();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShardedMongoServerProxy"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="instances">The instances.</param>
        /// <param name="connectionAttempt">The connection attempt.</param>
        public ShardedMongoServerProxy(MongoServer server, IEnumerable<MongoServerInstance> instances, BlockingQueue<MongoServerInstance> stateChangedQueue, int connectionAttempt)
        {
            _server = server;
            _instances = new List<MongoServerInstance>();
            _connectedInstances = new ConnectedInstanceCollection();
            _connectionAttempt = connectionAttempt;
            _stateChangeQueue = stateChangedQueue;

            foreach (var instance in instances)
            {
                AddInstance(instance);
            }

            _stateChangeThread = new Thread(ProcessStateChanges);
            _stateChangeThread.IsBackground = true;
            _stateChangeThread.Start();
        }

        // public properties
        /// <summary>
        /// Gets the build info.
        /// </summary>
        public MongoServerBuildInfo BuildInfo
        {
            get
            {
                lock (_lock)
                {
                    return _connectedInstances.ChooseServerInstance(ReadPreference.Primary).BuildInfo;
                }
            }
        }

        /// <summary>
        /// Gets the connection attempt.
        /// </summary>
        public int ConnectionAttempt
        {
            get
            {
                lock (_lock)
                {
                    return _connectionAttempt;
                }
            }
        }

        /// <summary>
        /// Gets the instances.
        /// </summary>
        public ReadOnlyCollection<MongoServerInstance> Instances
        {
            get
            {
                lock (_lock)
                {
                    return _instances.ToList().AsReadOnly();
                }
            }
        }

        /// <summary>
        /// Gets the state.
        /// </summary>
        public MongoServerState State
        {
            get
            {
                lock (_lock)
                {
                    return _state;
                }
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
            lock (_lock)
            {
                if (_connectedInstances.Count == 0)
                {
                    Connect(_server.Settings.ConnectTimeout, readPreference);
                }

                return _connectedInstances.ChooseServerInstance(readPreference);
            }
        }

        /// <summary>
        /// Connects to the server respecting the timeout and readPreference.
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        /// <param name="readPreference">The read preference.</param>
        public void Connect(TimeSpan timeout, ReadPreference readPreference)
        {
            DateTime timeoutAt = DateTime.UtcNow + timeout;
            lock (_lock)
            {
                while (DateTime.UtcNow < timeoutAt)
                {
                    if (_connectedInstances.Count > 0)
                    {
                        return;
                    }

                    if (_instances.Count == 0)
                    {
                        Monitor.PulseAll(_lock);
                        throw new MongoConnectionException("There were no mongos servers available. Ensure that the hosts listed in the connection string are available are are mongos'.");
                    }

                    if (_state == MongoServerState.Connecting)
                    {
                        Monitor.Wait(_lock, TimeSpan.FromMilliseconds(20));
                        continue;
                    }

                    _state = MongoServerState.Connecting;
                    _connectionAttempt++;
                    foreach (var instance in _instances.Where(x => x.State == MongoServerState.Disconnected || x.State == MongoServerState.Unknown))
                    {
                        ConnectInstanceAsynchronously(instance);
                    }
                }
            }

            throw new MongoConnectionException("Unable to connect to a mongos before the connection timed out.");
        }

        /// <summary>
        /// Disconnects from the server.
        /// </summary>
        public void Disconnect()
        {
            lock (_lock)
            {
                _state = MongoServerState.Disconnecting;
                foreach (var instance in _instances)
                {
                    _connectedInstances.Remove(instance);
                    instance.Disconnect();
                }
            }
        }

        /// <summary>
        /// Pings the server.
        /// </summary>
        public void Ping()
        {
            List<MongoServerInstance> instances;
            lock (_lock)
            {
                instances = _instances.ToList();
            }

            if (instances.Count == 0)
            {
                return;
            }

            foreach (var instance in instances)
            {
                instance.Ping();
            }
        }

        /// <summary>
        /// Verifies the state of the server.
        /// </summary>
        public void VerifyState()
        {
            List<MongoServerInstance> instances;
            lock (_lock)
            {
                // if we are disconnected or disconnecting, then our state is correct...
                if (_state == MongoServerState.Disconnected || _state == MongoServerState.Disconnecting)
                {
                    return;
                }

                instances = _instances.ToList();
            }

            if (instances.Count == 0)
            {
                return;
            }

            var exceptions = new List<Exception>();
            foreach (var instance in instances)
            {
                try
                {
                    instance.VerifyState();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Count == instances.Count)
            {
                throw exceptions[0];
            }
        }

        // private methods
        private void AddInstance(MongoServerInstance instance)
        {
            lock (_lock)
            {
                _instances.Add(instance);
                if (instance.State == MongoServerState.Connected)
                {
                    _connectedInstances.Add(instance);
                }

                instance.StateChanged += InstanceStateChanged;
            }
        }

        private void ConnectInstanceAsynchronously(MongoServerInstance instance)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    instance.Connect();
                }
                catch
                {
                    // instance is keeping it's last ConnectionException
                }
            });
        }

        private void InstanceStateChanged(object sender, EventArgs e)
        {
            _stateChangeQueue.Enqueue((MongoServerInstance)sender);
        }

        private void ProcessStateChanges()
        {
            while (true)
            {
                var instance = _stateChangeQueue.Dequeue();
                lock (_lock)
                {
                    if (instance.State == MongoServerState.Connected && _instances.Contains(instance))
                    {
                        if (instance.Type == MongoServerInstanceType.ShardRouter)
                        {

                            if (instance.IsMasterResult.MyAddress != null && instance.Address != instance.IsMasterResult.MyAddress)
                            {
                                instance.Address = instance.IsMasterResult.MyAddress;
                            }

                            if (!_connectedInstances.Contains(instance))
                            {
                                _connectedInstances.Add(instance);
                            }
                        }
                        else
                        {
                            RemoveInstance(instance);
                            // TODO: log!!!
                        }
                    }

                    if (_instances.Count == 0)
                    {
                        _state = MongoServerState.Disconnected;
                        return;
                    }

                    // the order of the tests is significant
                    // and resolves ambiguities when more than one state might match
                    if (_state == MongoServerState.Disconnecting)
                    {
                        if (_instances.All(i => i.State == MongoServerState.Disconnected))
                        {
                            _state = MongoServerState.Disconnected;
                        }
                    }
                    else
                    {
                        if (_instances.Any(i => i.State == MongoServerState.Connected))
                        {
                            _state = MongoServerState.Connected;
                        }
                        else if (_instances.All(i => i.State == MongoServerState.Disconnected))
                        {
                            _state = MongoServerState.Disconnected;
                        }
                        else if (_instances.All(i => i.State == MongoServerState.Connecting))
                        {
                            _state = MongoServerState.Connecting;
                        }
                        else if (_instances.Any(i => i.State == MongoServerState.Unknown))
                        {
                            _state = MongoServerState.Unknown;
                        }
                        else
                        {
                            throw new MongoInternalException("Unexpected server instance states.");
                        }
                    }
                }
            }
        }

        private void RemoveInstance(MongoServerInstance instance)
        {
            lock (_lock)
            {
                _connectedInstances.Remove(instance);
                _instances.Remove(instance);
                instance.StateChanged -= InstanceStateChanged;
                instance.Disconnect();
            }
        }
    }
}