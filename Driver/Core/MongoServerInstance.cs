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

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents an instance of a MongoDB server host (in the case of a replica set a MongoServer uses multiple MongoServerInstances).
    /// </summary>
    public class MongoServerInstance
    {
        // private static fields
        private static int __nextSequentialId;

        // public events
        /// <summary>
        /// Occurs when the value of the State property changes.
        /// </summary>
        public event EventHandler StateChanged;

        // private fields
        private object _serverInstanceLock = new object();
        private MongoServerAddress _address;
        private MongoServerBuildInfo _buildInfo;
        private Exception _connectException;
        private MongoConnectionPool _connectionPool;
        private IPEndPoint _ipEndPoint;
        private bool _isArbiter;
        private CommandResult _isMasterResult;
        private bool _isPassive;
        private bool _isPrimary;
        private bool _isSecondary;
        private int _maxDocumentSize;
        private int _maxMessageLength;
        private int _sequentialId;
        private MongoServer _server;
        private MongoServerState _state; // always use property to set value so event gets raised

        // constructors
        internal MongoServerInstance(MongoServer server, MongoServerAddress address)
        {
            _server = server;
            _address = address;
            _sequentialId = Interlocked.Increment(ref __nextSequentialId);
            _maxDocumentSize = MongoDefaults.MaxDocumentSize;
            _maxMessageLength = MongoDefaults.MaxMessageLength;
            _state = MongoServerState.Disconnected;
            _connectionPool = new MongoConnectionPool(this);
            // Console.WriteLine("MongoServerInstance[{0}]: {1}", sequentialId, address);
        }

        // public properties
        /// <summary>
        /// Gets the address of this server instance.
        /// </summary>
        public MongoServerAddress Address
        {
            get { return _address; }
            internal set
            {
                lock (_serverInstanceLock)
                {
                    if (_state != MongoServerState.Disconnected)
                    {
                        throw new MongoInternalException("MongoServerInstance Address can only be set when State is Disconnected.");
                    }
                    _address = value;
                }
            }
        }

        /// <summary>
        /// Gets the version of this server instance.
        /// </summary>
        public MongoServerBuildInfo BuildInfo
        {
            get { return _buildInfo; }
        }

        /// <summary>
        /// Gets the exception thrown the last time Connect was called (null if Connect did not throw an exception).
        /// </summary>
        public Exception ConnectException
        {
            get { return _connectException; }
            internal set { _connectException = value; }
        }

        /// <summary>
        /// Gets the connection pool for this server instance.
        /// </summary>
        public MongoConnectionPool ConnectionPool
        {
            get { return _connectionPool; }
        }

        /// <summary>
        /// Gets whether this server instance is an arbiter instance.
        /// </summary>
        public bool IsArbiter
        {
            get { return _isArbiter; }
        }

        /// <summary>
        /// Gets the result of the most recent ismaster command sent to this server instance.
        /// </summary>
        public CommandResult IsMasterResult
        {
            get { return _isMasterResult; }
        }

        /// <summary>
        /// Gets whether this server instance is a passive instance.
        /// </summary>
        public bool IsPassive
        {
            get { return _isPassive; }
        }

        /// <summary>
        /// Gets whether this server instance is a primary.
        /// </summary>
        public bool IsPrimary
        {
            get { return _isPrimary; }
        }

        /// <summary>
        /// Gets whether this server instance is a secondary.
        /// </summary>
        public bool IsSecondary
        {
            get { return _isSecondary; }
        }

        /// <summary>
        /// Gets the max document size for this server instance.
        /// </summary>
        public int MaxDocumentSize
        {
            get { return _maxDocumentSize; }
        }

        /// <summary>
        /// Gets the max message length for this server instance.
        /// </summary>
        public int MaxMessageLength
        {
            get { return _maxMessageLength; }
        }

        /// <summary>
        /// Gets the unique sequential Id for this server instance.
        /// </summary>
        public int SequentialId
        {
            get { return _sequentialId; }
        }

        /// <summary>
        /// Gets the server for this server instance.
        /// </summary>
        public MongoServer Server
        {
            get { return _server; }
        }

        /// <summary>
        /// Gets the state of this server instance.
        /// </summary>
        public MongoServerState State
        {
            get { return _state; }
        }

        // public methods
        /// <summary>
        /// Gets the IP end point of this server instance.
        /// </summary>
        /// <returns>The IP end point of this server instance.</returns>
        public IPEndPoint GetIPEndPoint()
        {
            // use a lock free algorithm because DNS lookups are rare and concurrent lookups are tolerable
            // the intermediate variable is important to avoid race conditions
            var ipEndPoint = _ipEndPoint;
            if (ipEndPoint == null)
            {
                ipEndPoint = _address.ToIPEndPoint(_server.Settings.AddressFamily);
                _ipEndPoint = ipEndPoint;
            }
            return ipEndPoint;
        }

        /// <summary>
        /// Checks whether the server is alive (throws an exception if not).
        /// </summary>
        public void Ping()
        {
            var connection = _connectionPool.AcquireConnection(null);
            try
            {
                var pingCommand = new CommandDocument("ping", 1);
                connection.RunCommand("admin.$cmd", QueryFlags.SlaveOk, pingCommand, true);
            }
            finally
            {
                _connectionPool.ReleaseConnection(connection);
            }
        }

        /// <summary>
        /// Verifies the state of the server instance.
        /// </summary>
        public void VerifyState()
        {
            lock (_serverInstanceLock)
            {
                // Console.WriteLine("MongoServerInstance[{0}]: VerifyState called.", sequentialId);
                // if ping fails assume all connections in the connection pool are doomed
                try
                {
                    Ping();
                }
                catch
                {
                    // Console.WriteLine("MongoServerInstance[{0}]: Ping failed: {1}.", sequentialId, ex.Message);
                    _connectionPool.Clear();
                }

                var connection = _connectionPool.AcquireConnection(null);
                try
                {
                    var previousState = _state;
                    try
                    {
                        VerifyState(connection);
                    }
                    catch
                    {
                        // ignore exceptions (if any occured state will already be set to Disconnected)
                        // Console.WriteLine("MongoServerInstance[{0}]: VerifyState failed: {1}.", sequentialId, ex.Message);
                    }
                    if (_state != previousState && _state == MongoServerState.Disconnected)
                    {
                        _connectionPool.Clear();
                    }
                }
                finally
                {
                    _connectionPool.ReleaseConnection(connection);
                }
            }
        }

        // internal methods
        internal MongoConnection AcquireConnection(MongoDatabase database)
        {
            MongoConnection connection;
            lock (_serverInstanceLock)
            {
                if (_state != MongoServerState.Connected)
                {
                    var message = string.Format("Server instance {0} is no longer connected.", _address);
                    throw new InvalidOperationException(message);
                }
            }
            connection = _connectionPool.AcquireConnection(database);

            // check authentication outside the lock because it might involve a round trip to the server
            try
            {
                connection.CheckAuthentication(database); // will authenticate if necessary
            }
            catch (MongoAuthenticationException)
            {
                // don't let the connection go to waste just because authentication failed
                _connectionPool.ReleaseConnection(connection);
                throw;
            }

            return connection;
        }

        internal void Connect(bool slaveOk)
        {
            // Console.WriteLine("MongoServerInstance[{0}]: Connect(slaveOk={1}) called.", sequentialId, slaveOk);
            lock (_serverInstanceLock)
            {
                // note: don't check that state is Disconnected here
                // when reconnecting to a replica set state can transition from Connected -> Connecting -> Connected

                SetState(MongoServerState.Connecting);
                _connectException = null;
                try
                {
                    try
                    {
                        var connection = _connectionPool.AcquireConnection(null);
                        try
                        {
                            VerifyState(connection);
                            if (!_isPrimary && !slaveOk)
                            {
                                throw new MongoConnectionException("Server is not a primary and SlaveOk is false.");
                            }
                        }
                        finally
                        {
                            _connectionPool.ReleaseConnection(connection);
                        }
                    }
                    catch
                    {
                        _connectionPool.Clear();
                        throw;
                    }

                    SetState(MongoServerState.Connected);
                }
                catch (Exception ex)
                {
                    SetState(MongoServerState.Disconnected);
                    _connectException = ex;
                    throw;
                }
            }
        }

        internal void Disconnect()
        {
            // Console.WriteLine("MongoServerInstance[{0}]: Disconnect called.", sequentialId);
            lock (_serverInstanceLock)
            {
                if (_state == MongoServerState.Disconnecting)
                {
                    throw new MongoInternalException("Disconnect called while disconnecting.");
                }
                if (_state != MongoServerState.Disconnected)
                {
                    try
                    {
                        SetState(MongoServerState.Disconnecting);
                        _connectionPool.Clear();
                    }
                    finally
                    {
                        SetState(MongoServerState.Disconnected);
                    }
                }
            }
        }

        internal void ReleaseConnection(MongoConnection connection)
        {
            _connectionPool.ReleaseConnection(connection);
        }

        internal void SetState(MongoServerState state)
        {
            lock (_serverInstanceLock)
            {
                if (_state != state)
                {
                    _state = state;
                    OnStateChanged();
                }
            }
        }

        internal void SetState(
            MongoServerState state,
            bool isPrimary,
            bool isSecondary,
            bool isPassive,
            bool isArbiter)
        {
            lock (_serverInstanceLock)
            {
                if (_state != state || _isPrimary != isPrimary || _isSecondary != isSecondary || _isPassive != isPassive || _isArbiter != isArbiter)
                {
                    _state = state;
                    _isPrimary = isPrimary;
                    _isSecondary = isSecondary;
                    _isPassive = isPassive;
                    _isArbiter = isArbiter;
                    OnStateChanged();
                }
            }
        }

        internal void VerifyState(MongoConnection connection)
        {
            CommandResult isMasterResult = null;
            bool ok = false;
            try
            {
                var isMasterCommand = new CommandDocument("ismaster", 1);
                isMasterResult = connection.RunCommand("admin.$cmd", QueryFlags.SlaveOk, isMasterCommand, false);
                if (!isMasterResult.Ok)
                {
                    throw new MongoCommandException(isMasterResult);
                }

                var isPrimary = isMasterResult.Response["ismaster", false].ToBoolean();
                var isSecondary = isMasterResult.Response["secondary", false].ToBoolean();
                var isPassive = isMasterResult.Response["passive", false].ToBoolean();
                var isArbiter = isMasterResult.Response["arbiterOnly", false].ToBoolean();
                // workaround for CSHARP-273
                if (isPassive && isArbiter) { isPassive = false; }

                var maxDocumentSize = isMasterResult.Response["maxBsonObjectSize", MongoDefaults.MaxDocumentSize].ToInt32();
                var maxMessageLength = Math.Max(MongoDefaults.MaxMessageLength, maxDocumentSize + 1024); // derived from maxDocumentSize

                MongoServerBuildInfo buildInfo;
                var buildInfoCommand = new CommandDocument("buildinfo", 1);
                var buildInfoResult = connection.RunCommand("admin.$cmd", QueryFlags.SlaveOk, buildInfoCommand, false);
                if (buildInfoResult.Ok)
                {
                    buildInfo = new MongoServerBuildInfo(
                        buildInfoResult.Response["bits"].ToInt32(), // bits
                        buildInfoResult.Response["gitVersion"].AsString, // gitVersion
                        buildInfoResult.Response["sysInfo"].AsString, // sysInfo
                        buildInfoResult.Response["version"].AsString // versionString
                    );
                }
                else
                {
                    // short term fix: if buildInfo fails due to auth we don't know the server version; see CSHARP-324
                    if (buildInfoResult.ErrorMessage != "need to login")
                    {
                        throw new MongoCommandException(buildInfoResult);
                    }
                    buildInfo = null;
                }

                _isMasterResult = isMasterResult;
                _maxDocumentSize = maxDocumentSize;
                _maxMessageLength = maxMessageLength;
                _buildInfo = buildInfo;
                this.SetState(MongoServerState.Connected, isPrimary, isSecondary, isPassive, isArbiter);

                // if this is the primary of a replica set check to see if any instances have been added or removed
                if (isPrimary && _server.Settings.ConnectionMode == ConnectionMode.ReplicaSet)
                {
                    var instanceAddresses = new List<MongoServerAddress>();
                    if (isMasterResult.Response.Contains("hosts"))
                    {
                        foreach (var hostName in isMasterResult.Response["hosts"].AsBsonArray)
                        {
                            var address = MongoServerAddress.Parse(hostName.AsString);
                            instanceAddresses.Add(address);
                        }
                    }
                    if (isMasterResult.Response.Contains("passives"))
                    {
                        foreach (var hostName in isMasterResult.Response["passives"].AsBsonArray)
                        {
                            var address = MongoServerAddress.Parse(hostName.AsString);
                            instanceAddresses.Add(address);
                        }
                    }
                    if (isMasterResult.Response.Contains("arbiters"))
                    {
                        foreach (var hostName in isMasterResult.Response["arbiters"].AsBsonArray)
                        {
                            var address = MongoServerAddress.Parse(hostName.AsString);
                            instanceAddresses.Add(address);
                        }
                    }
                    _server.VerifyInstances(instanceAddresses);
                }

                ok = true;
            }
            finally
            {
                if (!ok)
                {
                    _isMasterResult = isMasterResult;
                    _maxDocumentSize = MongoDefaults.MaxDocumentSize;
                    _maxMessageLength = MongoDefaults.MaxMessageLength;
                    _buildInfo = null;
                    this.SetState(MongoServerState.Disconnected, false, false, false, false);
                }
            }
        }

        // private methods
        private void OnStateChanged()
        {
            if (StateChanged != null)
            {
                try { StateChanged(this, null); }
                catch { } // ignore exceptions
            }
        }
    }
}
