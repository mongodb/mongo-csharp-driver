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
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents an instance of a MongoDB server host (in the case of a replica set a MongoServer uses multiple MongoServerInstances).
    /// </summary>
    internal enum MongoServerInstanceType
    {
        /// <summary>
        /// The server instance type is unknown.  This is the default.
        /// </summary>
        Unknown,
        /// <summary>
        /// The server is a standalone instance.
        /// </summary>
        StandAlone,
        /// <summary>
        /// The server is a replica set member.
        /// </summary>
        ReplicaSetMember,
        /// <summary>
        /// The server is a shard router (mongos).
        /// </summary>
        ShardRouter
    }

    /// <summary>
    /// Represents an instance of a MongoDB server host.
    /// </summary>
    public sealed class MongoServerInstance
    {
        // private static fields
        private static int __nextSequentialId;

        // public events
        /// <summary>
        /// Occurs when the value of the State property changes.
        /// </summary>
        public event EventHandler StateChanged;

        //internal events
        internal event EventHandler AveragePingTimeChanged;

        // private fields
        private readonly object _serverInstanceLock = new object();
        private readonly MongoServer _server;
        private readonly MongoConnectionPool _connectionPool;
        private readonly PingTimeAggregator _pingTimeAggregator;
        private readonly Timer _stateVerificationTimer;
        private MongoServerAddress _address;
        private MongoServerBuildInfo _buildInfo;
        private Exception _connectException;
        private bool _inStateVerification;
        private IPEndPoint _ipEndPoint;
        private bool _isArbiter;
        private IsMasterResult _isMasterResult;
        private bool _isPassive;
        private bool _isPrimary;
        private bool _isSecondary;
        private int _maxDocumentSize;
        private int _maxMessageLength;
        private ReplicaSetInformation _replicaSetInformation;
        private int _sequentialId;
        private MongoServerState _state;
        private MongoServerInstanceType _type;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoServerInstance"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="address">The address.</param>
        internal MongoServerInstance(MongoServer server, MongoServerAddress address)
        {
            _server = server;
            _address = address;
            _sequentialId = Interlocked.Increment(ref __nextSequentialId);
            _maxDocumentSize = MongoDefaults.MaxDocumentSize;
            _maxMessageLength = MongoDefaults.MaxMessageLength;
            _state = MongoServerState.Disconnected;
            _type = MongoServerInstanceType.Unknown;
            _connectionPool = new MongoConnectionPool(this);
            _pingTimeAggregator = new PingTimeAggregator(5);
            _stateVerificationTimer = new Timer(o => StateVerificationTimerCallback(), null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
            // Console.WriteLine("MongoServerInstance[{0}]: {1}", sequentialId, address);
        }

        // internal properties
        internal TimeSpan AveragePingTime
        {
            get { return _pingTimeAggregator.Average; }
        }

        /// <summary>
        /// Gets the replica set information.
        /// </summary>
        internal ReplicaSetInformation ReplicaSetInformation
        {
            get
            {
                lock (_serverInstanceLock)
                {
                    return _replicaSetInformation;
                }
            }
        }

        /// <summary>
        /// Gets the type.
        /// </summary>
        internal MongoServerInstanceType Type
        {
            get 
            {
                lock (_serverInstanceLock)
                {
                    return _type;
                }
            }
        }

        // public properties
        /// <summary>
        /// Gets the address of this server instance.
        /// </summary>
        public MongoServerAddress Address
        {
            get 
            {
                lock (_serverInstanceLock)
                {
                    return _address;
                }
            }
            internal set 
            {
                lock (_serverInstanceLock)
                {
                    _address = value;
                }
            }
        }

        /// <summary>
        /// Gets the version of this server instance.
        /// </summary>
        public MongoServerBuildInfo BuildInfo
        {
            get 
            {
                lock (_serverInstanceLock)
                {
                    return _buildInfo;
                }
            }
        }

        /// <summary>
        /// Gets the exception thrown the last time Connect was called (null if Connect did not throw an exception).
        /// </summary>
        public Exception ConnectException
        {
            get 
            {
                lock (_serverInstanceLock)
                {
                    return _connectException;
                }
            }
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
            get
            {
                lock (_serverInstanceLock)
                {
                    return _isArbiter;
                }
            }
        }

        /// <summary>
        /// Gets the result of the most recent ismaster command sent to this server instance.
        /// </summary>
        public IsMasterResult IsMasterResult
        {
            get
            {
                lock (_serverInstanceLock)
                { 
                    return _isMasterResult; 
                }
            }
        }

        /// <summary>
        /// Gets whether this server instance is a passive instance.
        /// </summary>
        public bool IsPassive
        {
            get
            {
                lock (_serverInstanceLock)
                {
                    return _isPassive;
                }
            }
        }

        /// <summary>
        /// Gets whether this server instance is a primary.
        /// </summary>
        public bool IsPrimary
        {
            get
            {
                lock (_serverInstanceLock)
                {
                    return _isPrimary;
                }
            }
        }

        /// <summary>
        /// Gets whether this server instance is a secondary.
        /// </summary>
        public bool IsSecondary
        {
            get
            {
                lock (_serverInstanceLock)
                {
                    return _isSecondary;
                }
            }
        }

        /// <summary>
        /// Gets the max document size for this server instance.
        /// </summary>
        public int MaxDocumentSize
        {
            get
            {
                lock (_serverInstanceLock)
                {
                    return _maxDocumentSize;
                }
            }
        }

        /// <summary>
        /// Gets the max message length for this server instance.
        /// </summary>
        public int MaxMessageLength
        {
            get
            {
                lock (_serverInstanceLock)
                {
                    return _maxMessageLength;
                }
            }
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
            get
            {
                lock (_serverInstanceLock)
                {
                    return _state;
                }
            }
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
            // use a new connection instead of one from the connection pool
            var connection = new MongoConnection(this);
            try
            {
                Ping(connection);
            }
            finally
            {
                connection.Close();
            }
        }

        /// <summary>
        /// Verifies the state of the server instance.
        /// </summary>
        public void VerifyState()
        {
            if (!ShouldVerifyState())
            {
                return;
            }

            // use a new connection instead of one from the connection pool
            var connection = new MongoConnection(this);
            try
            {
                try
                {
                    Ping(connection);
                    LookupServerInformation(connection);
                }
                catch
                {
                    // ignore exceptions (if any occured state will already be set to Disconnected)
                    // Console.WriteLine("MongoServerInstance[{0}]: VerifyState failed: {1}.", sequentialId, ex.Message);
                }
            }
            finally
            {
                connection.Close();
            }
        }

        // internal methods
        /// <summary>
        /// Acquires the connection.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <returns>A MongoConnection.</returns>
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

        /// <summary>
        /// Connects this instance.
        /// </summary>
        internal void Connect()
        {
            // Console.WriteLine("MongoServerInstance[{0}]: Connect() called.", sequentialId);
            lock (_serverInstanceLock)
            {
                if (_state != MongoServerState.Connected)
                {
                    SetState(MongoServerState.Connecting);
                    _connectException = null;
                    try
                    {
                        var connection = _connectionPool.AcquireConnection(null);
                        try
                        {
                            Ping(connection);
                            LookupServerInformation(connection);
                        }
                        finally
                        {
                            _connectionPool.ReleaseConnection(connection);
                        }
                    }
                    catch(Exception ex)
                    {
                        _connectionPool.Clear();
                        _connectException = ex;
                        SetState(MongoServerState.Disconnected);
                        throw;
                    }

                    SetState(MongoServerState.Connected);
                }
            }
        }

        /// <summary>
        /// Disconnects this instance.
        /// </summary>
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

        /// <summary>
        /// Releases the connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        internal void ReleaseConnection(MongoConnection connection)
        {
            _connectionPool.ReleaseConnection(connection);
        }

        /// <summary>
        /// Sets the state.
        /// </summary>
        /// <param name="state">The state.</param>
        internal void SetState(MongoServerState state)
        {
            lock(_serverInstanceLock)
            {
                SetState(state, _type, _isPrimary, _isSecondary, _isPassive, _isArbiter, _replicaSetInformation);
            }
        }

        // private methods
        private void LookupServerInformation(MongoConnection connection)
        {
            IsMasterResult isMasterResult = null;
            bool ok = false;
            try
            {
                var isMasterCommand = new CommandDocument("ismaster", 1);
                var tempResult = connection.RunCommand("admin", QueryFlags.SlaveOk, isMasterCommand, false);
                isMasterResult = new IsMasterResult();
                isMasterResult.Initialize(isMasterCommand, tempResult.Response);
                if (!isMasterResult.Ok)
                {
                    throw new MongoCommandException(isMasterResult);
                }

                MongoServerBuildInfo buildInfo;
                var buildInfoCommand = new CommandDocument("buildinfo", 1);
                var buildInfoResult = connection.RunCommand("admin", QueryFlags.SlaveOk, buildInfoCommand, false);
                if (buildInfoResult.Ok)
                {
                    buildInfo = MongoServerBuildInfo.FromCommandResult(buildInfoResult);
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

                ReplicaSetInformation replicaSetInformation = null;
                MongoServerInstanceType type = MongoServerInstanceType.StandAlone;
                if (isMasterResult.ReplicaSetName != null)
                {
                    var tagSet = new ReplicaSetTagSet();
                    var peers = isMasterResult.Hosts.Concat(isMasterResult.Passives).Concat(isMasterResult.Arbiters).ToList();
                    replicaSetInformation = new ReplicaSetInformation(isMasterResult.ReplicaSetName, isMasterResult.Primary, peers, tagSet);
                    type = MongoServerInstanceType.ReplicaSetMember;
                }
                else if (isMasterResult.Message != null && isMasterResult.Message == "isdbgrid")
                {
                    type = MongoServerInstanceType.ShardRouter;
                }

                lock (_serverInstanceLock)
                {
                    _isMasterResult = isMasterResult;
                    _maxDocumentSize = isMasterResult.MaxBsonObjectSize;
                    _maxMessageLength = isMasterResult.MaxMessageLength;
                    _buildInfo = buildInfo;
                    this.SetState(MongoServerState.Connected,
                        type,
                        isMasterResult.IsPrimary,
                        isMasterResult.IsSecondary,
                        isMasterResult.IsPassive,
                        isMasterResult.IsArbiterOnly,
                        replicaSetInformation);
                }
                ok = true;
            }
            finally
            {
                if (!ok)
                {
                    lock (_serverInstanceLock)
                    {
                        _isMasterResult = isMasterResult;
                        _maxDocumentSize = MongoDefaults.MaxDocumentSize;
                        _maxMessageLength = MongoDefaults.MaxMessageLength;
                        _buildInfo = null;
                        this.SetState(MongoServerState.Disconnected, _type, false, false, false, false, null);
                    }
                }
            }
        }

        private void OnAveragePingTimeChanged()
        {
            if (AveragePingTimeChanged != null)
            {
                try { AveragePingTimeChanged(this, EventArgs.Empty); }
                catch { } // ignore exceptions
            }
        }

        private void OnStateChanged()
        {
            if (StateChanged != null)
            {
                try { StateChanged(this, null); }
                catch { } // ignore exceptions
            }
        }

        private void Ping(MongoConnection connection)
        {
            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                var pingCommand = new CommandDocument("ping", 1);
                connection.RunCommand("admin", QueryFlags.SlaveOk, pingCommand, true);
                stopwatch.Stop();
                var currentAverage = _pingTimeAggregator.Average;
                _pingTimeAggregator.Include(stopwatch.Elapsed);
                var newAverage = _pingTimeAggregator.Average;
                if (currentAverage != newAverage)
                {
                    OnAveragePingTimeChanged();
                }
            }
            catch
            {
                _pingTimeAggregator.Clear();
                SetState(MongoServerState.Disconnected);
                throw;
            }
        }

        internal void StateVerificationTimerCallback()
        {
            if (_inStateVerification)
            {
                return;
            }

            _inStateVerification = true;
            try
            {
                if (!ShouldVerifyState())
                {
                    return;
                }

                var connection = new MongoConnection(this);

                Ping(connection);
                LookupServerInformation(connection);
                ThreadPool.QueueUserWorkItem(o => _connectionPool.MaintainPoolSize());
            }
            catch { }
            finally
            {
                _inStateVerification = false;
            }
        }

        private void SetState(
            MongoServerState state,
            MongoServerInstanceType type,
            bool isPrimary,
            bool isSecondary,
            bool isPassive,
            bool isArbiter,
            ReplicaSetInformation replicaSetInformation)
        {
            lock (_serverInstanceLock)
            {
                bool changed = false;
                bool replicaSetInformationIsDifferent = false;
                if ((_replicaSetInformation == null && replicaSetInformation != null) || (_replicaSetInformation != replicaSetInformation))
                {
                    replicaSetInformationIsDifferent = true;
                }
                if (_state != state || _type != type || replicaSetInformationIsDifferent || _isPrimary != isPrimary || _isSecondary != isSecondary || _isPassive != isPassive || _isArbiter != isArbiter)
                {
                    changed = true;
                    _state = state;
                    _type = type;
                    if (_replicaSetInformation != replicaSetInformation)
                    {
                        _replicaSetInformation = replicaSetInformation;
                    }
                    _isPrimary = isPrimary;
                    _isSecondary = isSecondary;
                    _isPassive = isPassive;
                    _isArbiter = isArbiter;
                }

                if (changed)
                {
                    if (_state == MongoServerState.Disconnected)
                    {
                        _connectionPool.Clear();
                    }
                    OnStateChanged();
                }
            }
        }

        private bool ShouldVerifyState()
        {
            MongoServerState currentState;
            lock (_serverInstanceLock)
            {
                currentState = _state;
            }
            return currentState == MongoServerState.Unknown || currentState == MongoServerState.Connected;
        }
    }
}
