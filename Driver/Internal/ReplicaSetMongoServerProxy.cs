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
    /// Proxy for connecting to a replica set.
    /// </summary>
    internal class ReplicaSetMongoServerProxy : IMongoServerProxy
    {
        // private fields
        private readonly object _lock = new object();
        private readonly MongoServer _server;
        private readonly List<MongoServerInstance> _instances;
        private readonly ConnectedInstanceCollection _connectedInstances;
        private readonly BlockingQueue<MongoServerInstance> _stateChangeQueue;
        private readonly Thread _stateChangeThread;
        private int _connectionAttempt;
        private MongoServerInstance _primary;
        private string _replicaSetName;
        private MongoServerState _state;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ReplicaSetMongoServerProxy"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        public ReplicaSetMongoServerProxy(MongoServer server)
        {
            _server = server;
            _replicaSetName = server.Settings.ReplicaSetName;
            _connectedInstances = new ConnectedInstanceCollection();
            _instances = new List<MongoServerInstance>();
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
        /// Initializes a new instance of the <see cref="ReplicaSetMongoServerProxy"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="instances">The instances.</param>
        /// <param name="stateChangeQueue">The state change queue.</param>
        /// <param name="connectionAttempt">The connection attempt.</param>
        public ReplicaSetMongoServerProxy(MongoServer server, IEnumerable<MongoServerInstance> instances, BlockingQueue<MongoServerInstance> stateChangeQueue, int connectionAttempt)
        {
            _server = server;
            _replicaSetName = server.Settings.ReplicaSetName;
            _connectedInstances = new ConnectedInstanceCollection();
            _instances = new List<MongoServerInstance>();
            _stateChangeQueue = stateChangeQueue;
            _connectionAttempt = connectionAttempt;

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
                    if (_primary != null)
                    {
                        return _primary.BuildInfo;
                    }
                    return null;
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
        /// Gets the name of the replica set.
        /// </summary>
        /// <value>
        /// The name of the replica set.
        /// </value>
        public string ReplicaSetName
        {
            get
            {
                lock (_lock)
                {
                    return _replicaSetName;
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
            for (int attempt = 1; attempt <= 2; attempt++)
            {
                lock (_lock)
                {
                    if (_connectedInstances.Count > 0)
                    {
                        var instance = _connectedInstances.ChooseServerInstance(readPreference);
                        if (instance != null)
                        {
                            return instance;
                        }
                    }
                }

                if (attempt == 1)
                {
                    Connect(_server.Settings.ConnectTimeout, readPreference);
                }
            }

            throw new MongoConnectionException("Unable to choose a server instance.");
        }

        /// <summary>
        /// Connects to the instances respecting the timeout and waitFor parameters.
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        /// <param name="waitFor">The wait for.</param>
        public void Connect(TimeSpan timeout, ReadPreference readPreference)
        {
            var timeoutAt = DateTime.UtcNow + timeout;
            lock (_lock)
            {
                while (DateTime.UtcNow < timeoutAt)
                {
                    if (_connectedInstances.Count > 0 && _connectedInstances.ChooseServerInstance(readPreference) != null)
                    {
                        return;
                    }

                    if (_instances.Count == 0)
                    {
                        Monitor.PulseAll(_lock);
                        string message;
                        if (_replicaSetName == null)
                        {
                            message = "There were no replica set members provided or discoverable. " + 
                                "Ensure that the hosts listed in the connection string are available and are replica set members.";
                            throw new MongoConnectionException(message);
                        }

                        message = "There were no replica set members provided or discoverable with the name '{0}'. " +
                                "Ensure that the hosts listed in the connection string are available and are members of the replica set '{0}'.";
                        throw new MongoConnectionException(string.Format(message, _replicaSetName));
                    }

                    if (_state == MongoServerState.Connecting)
                    {
                        Monitor.Wait(_lock, TimeSpan.FromMilliseconds(20));
                        continue;
                    }

                    SetState(MongoServerState.Connecting);
                    _connectionAttempt++;
                    foreach (var instance in _instances.Where(x => x.State == MongoServerState.Disconnected || x.State == MongoServerState.Unknown))
                    {
                        ConnectInstanceAsynchronously(instance);
                    }
                }
            }

            ThrowConnectionException(readPreference);
        }

        /// <summary>
        /// Disconnects the server.
        /// </summary>
        public void Disconnect()
        {
            lock (_lock)
            {
                if (_state != MongoServerState.Disconnected && _state != MongoServerState.Disconnecting)
                {
                    SetState(MongoServerState.Disconnecting);
                    try
                    {
                        foreach (var instance in _instances)
                        {
                            try
                            {
                                instance.Disconnect();
                            }
                            catch { } // ignore disconnection errors
                        }
                    }
                    finally
                    {
                        SetState(MongoServerState.Disconnected);
                    }
                }
            }
        }

        /// <summary>
        /// Checks whether the server is alive (throws an exception if not).
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

            List<Exception> exceptions = new List<Exception>();
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

            if (exceptions.Count == _instances.Count)
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
            _stateChangeQueue.EnqueuWorkItem(() =>
            {
                try
                {
                    instance.Connect();
                }
                catch
                {
                    // instance is keeping it's last ConnectionException
                }
                return instance;
            });
        }

        private MongoServerState DetermineServerState()
        {
            lock (_lock)
            {
                // the order of the tests is significant
                // and resolves ambiguities when more than one state might match
                if (_state == MongoServerState.Disconnecting)
                {
                    if (_instances.All(i => i.State == MongoServerState.Disconnected))
                    {
                        return MongoServerState.Disconnected;
                    }

                    return _state;
                }
                else
                {
                    if (_instances.All(i => i.State == MongoServerState.Disconnected))
                    {
                        return MongoServerState.Disconnected;
                    }
                    else if (_instances.All(i => i.State == MongoServerState.Connected))
                    {
                        return MongoServerState.Connected;
                    }
                    else if (_instances.Any(i => i.State == MongoServerState.Connecting))
                    {
                        return MongoServerState.Connecting;
                    }
                    else if (_instances.Any(i => i.State == MongoServerState.Unknown))
                    {
                        return MongoServerState.Unknown;
                    }
                    else if (_instances.Any(i => i.State == MongoServerState.Connected))
                    {
                        return MongoServerState.ConnectedToSubset;
                    }

                    return _state;
                }
            }
        }

        private void InstanceStateChanged(object sender, EventArgs e)
        {
            _stateChangeQueue.Enqueue((MongoServerInstance)sender);
        }

        private void ProcessConnectedPrimaryStateChange(MongoServerInstance instance)
        {
            lock (_lock)
            {
                if (!_connectedInstances.Contains(instance))
                {
                    _connectedInstances.Add(instance);
                }

                if (_primary != instance)
                {
                    _primary = instance;
                }

                if (_replicaSetName == null)
                {
                    _replicaSetName = instance.ReplicaSetInformation.Name;
                }

                if (instance.ReplicaSetInformation.Members.Any())
                {
                    // remove instances the primary doesn't know about and add instances we don't know about
                    for (int i = _instances.Count - 1; i >= 0; i--)
                    {
                        if (!instance.ReplicaSetInformation.Members.Any(x => x == _instances[i].Address))
                        {
                            RemoveInstance(_instances[i]);
                        }
                    }

                    _instances.RemoveAll(x => !instance.ReplicaSetInformation.Members.Contains(x.Address));
                    foreach (var address in instance.ReplicaSetInformation.Members)
                    {
                        if (!_instances.Any(i => i.Address == address))
                        {
                            var missingInstance = new MongoServerInstance(_server, address);
                            AddInstance(missingInstance);
                            ConnectInstanceAsynchronously(missingInstance);
                        }
                    }
                }
            }
        }

        private void ProcessConnectedSecondaryStateChange(MongoServerInstance instance)
        {
            lock (_lock)
            {
                if (!_connectedInstances.Contains(instance))
                {
                    _connectedInstances.Add(instance);
                }

                // if the secondary is reporting a primary server that doesn't exist in our instances, add it
                if (instance.ReplicaSetInformation.Primary != null && !_instances.Any(x => x.Address == instance.ReplicaSetInformation.Primary))
                {
                    var missingInstance = new MongoServerInstance(_server, instance.ReplicaSetInformation.Primary);
                    AddInstance(missingInstance);
                    ConnectInstanceAsynchronously(missingInstance);
                }
            }
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
                        if (instance.IsMasterResult.MyAddress != null && instance.Address != instance.IsMasterResult.MyAddress)
                        {
                            // TODO: if this gets set inside the MongoServerInstance, then there is a race condition that could cause
                            // the instance to get added more than once to the list because the changes occur under different locks.
                            // I don't like this and would rather it be in the MongoServerInstance, but haven't figured out how to do
                            // it yet.
                            // One solution is for every instance change to check to see if two instances exist in the list and remove
                            // the other one, as the current one is more up-to-date.
                            instance.Address = instance.IsMasterResult.MyAddress;
                        }

                        if (instance.Type == MongoServerInstanceType.ReplicaSetMember)
                        {
                            if (_replicaSetName != null && _replicaSetName != instance.ReplicaSetInformation.Name)
                            {
                                RemoveInstance(instance);
                                // TODO: log!!!
                            }
                            else if (instance.IsPrimary)
                            {
                                ProcessConnectedPrimaryStateChange(instance);
                            }
                            else
                            {
                                ProcessConnectedSecondaryStateChange(instance);
                            }
                        }
                        else
                        {
                            RemoveInstance(instance);
                            // TODO: log!!!
                        }
                    }
                    else
                    {
                        _connectedInstances.Remove(instance);

                        if (_primary == instance)
                        {
                            _primary = null;
                        }
                    }

                    SetState(DetermineServerState());
                }
            }
        }

        private void RemoveInstance(MongoServerInstance instance)
        {
            lock (_lock)
            {
                _instances.Remove(instance);
                _connectedInstances.Remove(instance);
                instance.StateChanged -= InstanceStateChanged;
                instance.Disconnect();
            }
        }

        private void SetState(MongoServerState state)
        {
            lock (_lock)
            {
                //Console.WriteLine(state);
                _state = state;
            }
        }

        private void ThrowConnectionException(ReadPreference readPreference)
        {
            List<Exception> exceptions;
            lock (_lock)
            {
                exceptions = _instances.Select(x => x.ConnectException).Where(x => x != null).ToList();
            }
            var firstException = exceptions.FirstOrDefault();
            string message;
            if (firstException == null)
            {
                message = string.Format("Unable to connect to a member of the replica set matching the read preference {0}", readPreference);
            }
            else
            {
                message = string.Format("Unable to connect to a member of the replica set matching the read preference {0}: {1}.", readPreference, firstException.Message);
            }
            var connectionException = new MongoConnectionException(message, firstException);
            connectionException.Data.Add("InnerExceptions", exceptions); // useful when there is more than one
            throw connectionException;
        }
    }
}
