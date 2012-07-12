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

namespace MongoDB.Driver.Internal
{
    /// <summary>
    /// Connects directly to a specified instance, failing over to other addresses as necessary.
    /// </summary>
    internal sealed class DirectMongoServerProxy : IMongoServerProxy
    {
        // private fields
        private readonly object _stateLock = new object();
        private readonly MongoServer _server;
        private readonly MongoServerInstance _instance;
        private int _connectionAttempt;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DirectMongoServerProxy"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        public DirectMongoServerProxy(MongoServer server)
        {
            _server = server;
            _instance = new MongoServerInstance(server, server.Settings.Servers.First());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectMongoServerProxy"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="instance">The instance.</param>
        /// <param name="connectionAttempt">The connection attempt.</param>
        public DirectMongoServerProxy(MongoServer server, MongoServerInstance instance, int connectionAttempt)
        {
            _server = server;
            _instance = instance;
            _connectionAttempt = connectionAttempt;
        }

        // public properties
        /// <summary>
        /// Gets the build info.
        /// </summary>
        public MongoServerBuildInfo BuildInfo
        {
            get { return _instance.BuildInfo; }
        }

        /// <summary>
        /// Gets the connection attempt.
        /// </summary>
        public int ConnectionAttempt
        {
            get 
            {
                lock (_stateLock)
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
            get { return new List<MongoServerInstance> { _instance }.AsReadOnly(); }
        }

        /// <summary>
        /// Gets the state.
        /// </summary>
        public MongoServerState State
        {
            get 
            {
                lock (_stateLock)
                {
                    return _instance.State;
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
            if (_instance.State != MongoServerState.Connected)
            {
                lock (_stateLock)
                {
                    Connect(_server.Settings.ConnectTimeout, readPreference);
                }
            }

            if (_instance.State == MongoServerState.Connected)
            {
                if (readPreference.MatchesInstance(_instance))
                {
                    return _instance;
                }
                else
                {
                    string message = string.Format("Server {0} does not meet the criteria of the specified read preference {1}.",
                        _instance.Address,
                        readPreference);
                    throw new MongoConnectionException(message);
                }
            }

            throw new MongoConnectionException(string.Format("Unable to connect to the server {0}.", _instance.Address));
        }

        /// <summary>
        /// Connects to the server respecting the timeout and readPreference.
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        /// <param name="readPreference">The read preference.</param>
        public void Connect(TimeSpan timeout, ReadPreference readPreference)
        {
            if (_instance.State != MongoServerState.Connected)
            {
                lock (_stateLock)
                {
                    _connectionAttempt++;
                    var exceptions = new List<Exception>();
                    foreach (var address in _server.Settings.Servers)
                    {
                        try
                        {
                            _instance.Address = address;
                            _instance.Connect(); // TODO: what about timeout?
                            if (!readPreference.MatchesInstance(_instance))
                            {
                                exceptions.Add(new MongoConnectionException(string.Format("The server '{0}' does not match the read preference {1}.", address, _server.Settings.ReadPreference)));
                                continue;
                            }

                            if (_server.Settings.ReplicaSetName != null && 
                                (_instance.Type != MongoServerInstanceType.ReplicaSetMember || _instance.ReplicaSetInformation.Name != _server.Settings.ReplicaSetName))
                            {
                                exceptions.Add(new MongoConnectionException(string.Format("The server '{0}' is not a member of replica set '{1}'.", address, _server.Settings.ReplicaSetName)));
                                continue;
                            }

                            if (_instance.IsMasterResult.MyAddress != null)
                            {
                                _instance.Address = _instance.IsMasterResult.MyAddress;
                            }
                            return;
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(ex);
                        }
                    }

                    var firstAddress = _server.Settings.Servers.First();
                    var firstException = exceptions.First();
                    var message = string.Format("Unable to connect to server {0}: {1}.", firstAddress, firstException.Message);
                    var connectionException = new MongoConnectionException(message, firstException);
                    connectionException.Data.Add("InnerExceptions", exceptions); // useful when there is more than one
                    throw connectionException;
                }
            }
        }

        /// <summary>
        /// Disconnects from the server.
        /// </summary>
        public void Disconnect()
        {
            lock (_stateLock)
            {
                _instance.Disconnect();
            }
        }

        /// <summary>
        /// Pings the server.
        /// </summary>
        public void Ping()
        {
            _instance.Ping();
        }

        /// <summary>
        /// Verifies the state of the server.
        /// </summary>
        public void VerifyState()
        {
            var state = _instance.State;
            // if we are disconnected or disconnecting, then our state is correct...
            if (state == MongoServerState.Disconnected || state == MongoServerState.Disconnecting)
            {
                return;
            }
            
            lock (_stateLock)
            {
                _instance.VerifyState();
            }
        }
    }
}