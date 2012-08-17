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
using System.Threading;
using System.Collections.ObjectModel;

namespace MongoDB.Driver.Internal
{
    /// <summary>
    /// Base class for proxies that maintain multiple server instance connections.
    /// </summary>
    internal abstract class MultipleConnectionMongoServerProxy : IMongoServerProxy
    {
        // private fields
        private readonly object _lock = new object();
        private readonly ConnectedInstanceCollection _connectedInstances;
        private readonly List<MongoServerInstance> _instances;
        private readonly MongoServer _server;
        private int _connectionAttempt;
        private int _outstandingInstanceConnections;
        private MongoServerState _state;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShardedMongoServerProxy"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        protected MultipleConnectionMongoServerProxy(MongoServer server)
        {
            _server = server;
            _connectedInstances = new ConnectedInstanceCollection();
            _instances = new List<MongoServerInstance>();

            MakeInstancesMatchAddresses(server.Settings.Servers);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShardedMongoServerProxy"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="instances">The instances.</param>
        /// <param name="connectionQueue">The state change queue.</param>
        /// <param name="connectionAttempt">The connection attempt.</param>
        protected MultipleConnectionMongoServerProxy(MongoServer server, IEnumerable<MongoServerInstance> instances, BlockingQueue<MongoServerInstance> connectionQueue, int connectionAttempt)
        {
            _server = server;
            _connectedInstances = new ConnectedInstanceCollection();
            _connectionAttempt = connectionAttempt;

            // This constructor is used when the instances are already connecting, so 
            // we set this value here to reflect that.
            _state = MongoServerState.Connecting;

            _outstandingInstanceConnections = connectionQueue.Count;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                while (connectionQueue.Count > 0)
                {
                    var instance = connectionQueue.Dequeue();
                    Interlocked.Decrement(ref _outstandingInstanceConnections);
                }
            });

            // It's important to have our own copy of this list because it might get modified during iteration. 
            _instances = instances.ToList();
            foreach (var instance in instances)
            {
                instance.StateChanged += InstanceStateChanged;
                ProcessInstanceStateChange(instance);
            }
        }

        // public methods
        /// <summary>
        /// Gets the build info.
        /// </summary>
        public MongoServerBuildInfo BuildInfo
        {
            get
            {
                var instance = _connectedInstances.ChooseServerInstance(ReadPreference.Primary);
                return instance == null
                    ? null
                    : instance.BuildInfo;
            }
        }

        /// <summary>
        /// Gets the connection attempt.
        /// </summary>
        public int ConnectionAttempt
        {
            get
            {
                return Interlocked.CompareExchange(ref _connectionAttempt, 0, 0);
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
        /// <param name="readPreference">The read preference.</param>
        /// <returns>A MongoServerInstance.</returns>
        public MongoServerInstance ChooseServerInstance(ReadPreference readPreference)
        {
            for (int attempt = 1; attempt <= 2; attempt++)
            {
                var instance = ChooseServerInstance(_connectedInstances, readPreference);
                if (instance != null)
                {
                    return instance;
                }
                if (attempt == 1)
                {
                    Connect(_server.Settings.ConnectTimeout, readPreference);
                }
            }

            throw new MongoConnectionException("Unable to choose a server instance.");
        }

        /// <summary>
        /// Connects to the instances respecting the timeout and readPreference.
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        /// <param name="readPreference">The read preference.</param>
        public void Connect(TimeSpan timeout, ReadPreference readPreference)
        {
            var timeoutAt = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < timeoutAt)
            {
                if (ChooseServerInstance(_connectedInstances, readPreference) != null)
                {
                    return;
                }

                if (Interlocked.CompareExchange(ref _outstandingInstanceConnections, 0, 0) > 0)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(20));
                    continue;
                }

                lock (_lock)
                {
                    // if we are already fully connected and an instance still isn't chosen,
                    // then one simply doesn't exist, so we'll break immediately and throw a
                    // connection exception.
                    if (_state == MongoServerState.Connected)
                    {
                        break;
                    }

                    SetState(MongoServerState.Connecting);
                    Interlocked.Increment(ref _connectionAttempt);

                    foreach (var instance in _instances)
                    {
                        ConnectInstance(instance);
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
            List<MongoServerInstance> currentInstances;
            lock (_lock)
            {
                if (_state == MongoServerState.Disconnected || _state == MongoServerState.Disconnecting)
                {
                    return;
                }

                currentInstances = _instances.ToList();

                SetState(MongoServerState.Disconnecting);
            }

            try
            {
                _connectedInstances.Clear();
                foreach (var instance in currentInstances)
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

        // protected methods
        /// <summary>
        /// Chooses the server instance.
        /// </summary>
        /// <param name="connectedInstances">The connected instances.</param>
        /// <param name="readPreference">The read preference.</param>
        /// <returns>A MongoServerInstance.</returns>
        protected abstract MongoServerInstance ChooseServerInstance(ConnectedInstanceCollection connectedInstances, ReadPreference readPreference);

        /// <summary>
        /// Determines the state of the server.
        /// </summary>
        /// <param name="currentState">State of the current.</param>
        /// <param name="instances">The instances.</param>
        /// <returns>The state of the server.</returns>
        protected abstract MongoServerState DetermineServerState(MongoServerState currentState, IEnumerable<MongoServerInstance> instances);

        /// <summary>
        /// Ensures that an instance with the address exists.
        /// </summary>
        /// <param name="address">The address.</param>
        protected void EnsureInstanceWithAddress(MongoServerAddress address)
        {
            lock (_lock)
            {
                if (!_instances.Any(x => x.Address == address))
                {
                    var instance = new MongoServerInstance(_server, address);
                    AddInstance(instance);
                    if (_state != MongoServerState.Disconnecting && _state != MongoServerState.Disconnected)
                    {
                        SetState(MongoServerState.Connecting);
                        ConnectInstance(instance);
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether the instance is a valid.  If not, the instance is removed.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns>
        ///   <c>true</c> if the instance is valid; otherwise, <c>false</c>.
        /// </returns>
        protected abstract bool IsValidInstance(MongoServerInstance instance);

        /// <summary>
        /// Ensures that the current instance list has all the addresses provided and does not contain any not provided.
        /// </summary>
        /// <param name="addresses">The addresses.</param>
        protected void MakeInstancesMatchAddresses(IEnumerable<MongoServerAddress> addresses)
        {
            lock (_lock)
            {
                for (int i = _instances.Count - 1; i >= 0; i--)
                {
                    if (!addresses.Any(x => x == _instances[i].Address))
                    {
                        RemoveInstance(_instances[i]);
                    }
                }

                foreach (var address in addresses)
                {
                    EnsureInstanceWithAddress(address);
                }
            }
        }

        /// <summary>
        /// Processes the connected instance state change.
        /// </summary>
        /// <param name="instance">The instance.</param>
        protected virtual void ProcessConnectedInstanceStateChange(MongoServerInstance instance)
        { }

        // private methods
        private void AddInstance(MongoServerInstance instance)
        {
            lock (_lock)
            {
                _instances.Add(instance);
            }

            instance.StateChanged += InstanceStateChanged;
            ProcessInstanceStateChange(instance);
        }

        private void ConnectInstance(MongoServerInstance instance)
        {
            var instanceState = instance.State;
            if (instanceState == MongoServerState.Connecting || instanceState == MongoServerState.Connected)
            {
                return;
            }

            Interlocked.Increment(ref _outstandingInstanceConnections);
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    instance.Connect();
                }
                catch
                {
                    // instance is keeping it's last ConnectionException
                }
                finally
                {
                    Interlocked.Decrement(ref _outstandingInstanceConnections);
                }
            });
        }

        private void InstanceStateChanged(object sender, EventArgs e)
        {
            ProcessInstanceStateChange((MongoServerInstance)sender);
        }

        private void ProcessInstanceStateChange(MongoServerInstance instance)
        {
            List<MongoServerInstance> currentInstances;
            MongoServerState currentState;

            lock (_lock)
            {
                currentInstances = _instances;
                currentState = _state;
            }

            if (currentInstances.Contains(instance))
            {
                if (instance.State == MongoServerState.Connected)
                {
                    if (!IsValidInstance(instance))
                    {
                        RemoveInstance(instance);
                        return;
                    }

                    if (currentState != MongoServerState.Disconnecting && currentState != MongoServerState.Disconnected)
                    {
                        _connectedInstances.EnsureContains(instance);
                        ProcessConnectedInstanceStateChange(instance);
                    }
                }
                else
                {
                    _connectedInstances.Remove(instance);
                }
            }

            SetState(DetermineServerState(currentState, currentInstances));
        }

        private void RemoveInstance(MongoServerInstance instance)
        {
            _connectedInstances.Remove(instance);
            lock (_lock)
            {
                _instances.Remove(instance);
            }

            instance.StateChanged -= InstanceStateChanged;
            instance.Disconnect();
            ProcessInstanceStateChange(instance);
        }

        private void SetState(MongoServerState state)
        {
            lock (_lock)
            {
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
