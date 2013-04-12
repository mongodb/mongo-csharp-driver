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
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Internal;
using MongoDB.Driver.Operations;

namespace MongoDB.Driver
{
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
        private readonly MongoServerSettings _settings;
        private readonly MongoConnectionPool _connectionPool;
        private readonly PingTimeAggregator _pingTimeAggregator;
        private MongoServerAddress _address;
        private Exception _connectException;
        private bool _inStateVerification;
        private ServerInformation _serverInfo;
        private IPEndPoint _ipEndPoint;
        private bool _permanentlyDisconnected;
        private int _sequentialId;
        private MongoServerState _state;
        private Timer _stateVerificationTimer;
        private MongoConnectionPool.AcquireConnectionOptions _stateVerificationAcquireConnectionOptions;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoServerInstance"/> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="address">The address.</param>
        internal MongoServerInstance(MongoServerSettings settings, MongoServerAddress address)
        {
            _settings = settings;
            _address = address;
            _sequentialId = Interlocked.Increment(ref __nextSequentialId);
            _state = MongoServerState.Disconnected;
            _serverInfo = new ServerInformation
            {
                MaxDocumentSize = MongoDefaults.MaxDocumentSize,
                MaxMessageLength = MongoDefaults.MaxMessageLength,
                InstanceType = MongoServerInstanceType.Unknown
            };
            _connectionPool = new MongoConnectionPool(this);
            _pingTimeAggregator = new PingTimeAggregator(5);
            _permanentlyDisconnected = false;
            // Console.WriteLine("MongoServerInstance[{0}]: {1}", sequentialId, address);

            _stateVerificationAcquireConnectionOptions = new MongoConnectionPool.AcquireConnectionOptions
            {
                OkToAvoidWaitingByCreatingNewConnection = false,
                OkToExceedMaxConnectionPoolSize = true,
                OkToExceedWaitQueueSize = true,
                WaitQueueTimeout = TimeSpan.FromSeconds(2)
            };
        }

        // internal properties
        /// <summary>
        /// Gets the average ping time.
        /// </summary>
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
                    return _serverInfo.ReplicaSetInformation;
                }
            }
        }

        /// <summary>
        /// Gets the instance type.
        /// </summary>
        public MongoServerInstanceType InstanceType
        {
            get
            {
                lock (_serverInstanceLock)
                {
                    return _serverInfo.InstanceType;
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
                    return _serverInfo.BuildInfo;
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
                    return _serverInfo.IsArbiter;
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
                    return _serverInfo.IsMasterResult;
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
                    return _serverInfo.IsPassive;
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
                    return _serverInfo.IsPrimary;
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
                    return _serverInfo.IsSecondary;
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
                    return _serverInfo.MaxDocumentSize;
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
                    return _serverInfo.MaxMessageLength;
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
        public MongoServerSettings Settings
        {
            get { return _settings; }
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
            var ipEndPoint = Interlocked.CompareExchange(ref _ipEndPoint, null, null);
            if (ipEndPoint == null)
            {
                var addressFamily = _settings.IPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;
                ipEndPoint = _address.ToIPEndPoint(addressFamily);
                Interlocked.CompareExchange(ref _ipEndPoint, _ipEndPoint, null);
            }
            return ipEndPoint;
        }

        /// <summary>
        /// Checks whether the server is alive (throws an exception if not).
        /// </summary>
        public void Ping()
        {
            var connection = _connectionPool.AcquireConnection(_stateVerificationAcquireConnectionOptions);
            try
            {
                Ping(connection);
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
            var connection = _connectionPool.AcquireConnection(_stateVerificationAcquireConnectionOptions);
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
                _connectionPool.ReleaseConnection(connection);
            }
        }

        // internal methods
        /// <summary>
        /// Acquires the connection.
        /// </summary>
        /// <returns>A MongoConnection.</returns>
        internal MongoConnection AcquireConnection()
        {
            lock (_serverInstanceLock)
            {
                if (_state != MongoServerState.Connected)
                {
                    var message = string.Format("Server instance {0} is no longer connected.", _address);
                    throw new InvalidOperationException(message);
                }
            }

            return _connectionPool.AcquireConnection();
        }

        /// <summary>
        /// Connects this instance.
        /// </summary>
        internal void Connect()
        {
            // Console.WriteLine("MongoServerInstance[{0}]: Connect() called.", sequentialId);
            lock (_serverInstanceLock)
            {
                if (_permanentlyDisconnected || _state == MongoServerState.Connecting || _state == MongoServerState.Connected)
                {
                    return;
                }

                _connectException = null;

                // set the state manually here because SetState raises an event that shouldn't be raised
                // while holding a lock.
                _state = MongoServerState.Connecting;
            }

            // We know for certain that the state just changed
            OnStateChanged();

            try
            {
                var connection = _connectionPool.AcquireConnection();
                try
                {
                    Ping(connection);
                    LookupServerInformation(connection);
                }
                finally
                {
                    _connectionPool.ReleaseConnection(connection);
                }
                SetState(MongoServerState.Connected);
            }
            catch (Exception ex)
            {
                lock (_serverInstanceLock)
                {
                    _connectException = ex;
                }
                _connectionPool.Clear();
                Interlocked.Exchange(ref _connectException, ex);
                SetState(MongoServerState.Disconnected);
                throw;
            }
            finally
            {
                lock (_serverInstanceLock)
                {
                    if (_stateVerificationTimer == null)
                    {
                        _stateVerificationTimer = new Timer(o => StateVerificationTimerCallback(), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
                    }
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
                if (_stateVerificationTimer != null)
                {
                    _stateVerificationTimer.Dispose();
                    _stateVerificationTimer = null;
                }

                if (_state == MongoServerState.Disconnecting || _state == MongoServerState.Disconnected)
                {
                    return;
                }

                // set the state here because SetState raises an event that should not be raised while holding a lock
                _state = MongoServerState.Disconnecting;
            }

            // we know for certain state has just changed.
            OnStateChanged(); 

            try
            {
                _connectionPool.Clear();
            }
            finally
            {
                SetState(MongoServerState.Disconnected);
            }
        }

        /// <summary>
        /// Disconnects this instance permanently.
        /// </summary>
        internal void DisconnectPermanently()
        {
            lock (_serverInstanceLock)
            {
                _permanentlyDisconnected = true;
            }

            Disconnect();
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
            lock (_serverInstanceLock)
            {
                if (_state == state)
                {
                    return;
                }

                _state = state;
            }

            OnStateChanged();
        }

        // private methods
        private void LookupServerInformation(MongoConnection connection)
        {
            IsMasterResult isMasterResult = null;
            bool ok = false;
            try
            {
                var isMasterCommand = new CommandDocument("ismaster", 1);
                isMasterResult = RunCommandAs<IsMasterResult>(connection, "admin", isMasterCommand);

                MongoServerBuildInfo buildInfo;
                try
                {
                    var buildInfoCommand = new CommandDocument("buildinfo", 1);
                    var buildInfoResult = RunCommandAs<CommandResult>(connection, "admin", buildInfoCommand);
                    buildInfo = MongoServerBuildInfo.FromCommandResult(buildInfoResult);
                }
                catch (MongoCommandException ex)
                {
                    // short term fix: if buildInfo fails due to auth we don't know the server version; see CSHARP-324
                    if (ex.CommandResult.ErrorMessage != "need to login")
                    {
                        throw;
                    }
                    buildInfo = null;
                }

                ReplicaSetInformation replicaSetInformation = null;
                MongoServerInstanceType instanceType = MongoServerInstanceType.StandAlone;
                if (isMasterResult.IsReplicaSet)
                {
                    var peers = isMasterResult.Hosts.Concat(isMasterResult.Passives).Concat(isMasterResult.Arbiters).ToList();
                    replicaSetInformation = new ReplicaSetInformation(isMasterResult.ReplicaSetName, isMasterResult.Primary, peers, isMasterResult.Tags);
                    instanceType = MongoServerInstanceType.ReplicaSetMember;
                }
                else if (isMasterResult.Message != null && isMasterResult.Message == "isdbgrid")
                {
                    instanceType = MongoServerInstanceType.ShardRouter;
                }

                var newServerInfo = new ServerInformation
                {
                    BuildInfo = buildInfo,
                    InstanceType = instanceType,
                    IsArbiter = isMasterResult.IsArbiterOnly,
                    IsMasterResult = isMasterResult,
                    IsPassive = isMasterResult.IsPassive,
                    IsPrimary = isMasterResult.IsPrimary,
                    IsSecondary = isMasterResult.IsSecondary,
                    MaxDocumentSize = isMasterResult.MaxBsonObjectSize,
                    MaxMessageLength = isMasterResult.MaxMessageLength,
                    ReplicaSetInformation = replicaSetInformation
                };
                MongoServerState currentState;
                lock (_serverInstanceLock)
                {
                    currentState = _state;
                }
                SetState(currentState, newServerInfo);
                ok = true;
            }
            finally
            {
                if (!ok)
                {
                    ServerInformation currentServerInfo;
                    lock (_serverInstanceLock)
                    {
                        currentServerInfo = _serverInfo;
                    }

                    // keep the current instance type, build info, and replica set info
                    // as these aren't relevent to state and are likely still correct.
                    var newServerInfo = new ServerInformation
                    {
                        BuildInfo = currentServerInfo.BuildInfo,
                        InstanceType = currentServerInfo.InstanceType,
                        IsArbiter = false,
                        IsMasterResult = isMasterResult,
                        IsPassive = false,
                        IsPrimary = false,
                        IsSecondary = false,
                        MaxDocumentSize = currentServerInfo.MaxDocumentSize,
                        MaxMessageLength = currentServerInfo.MaxMessageLength,
                        ReplicaSetInformation = currentServerInfo.ReplicaSetInformation
                    };

                    SetState(MongoServerState.Disconnected, newServerInfo);
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
                var pingCommand = new CommandDocument("ping", 1);
                Stopwatch stopwatch = Stopwatch.StartNew();
                RunCommandAs<CommandResult>(connection, "admin", pingCommand);
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

        private TCommandResult RunCommandAs<TCommandResult>(MongoConnection connection, string databaseName, IMongoCommand command)
            where TCommandResult : CommandResult
        {
            var readerSettings = new BsonBinaryReaderSettings();
            var writerSettings = new BsonBinaryWriterSettings();
            var resultSerializer = BsonSerializer.LookupSerializer(typeof(TCommandResult));

            var commandOperation = new CommandOperation<TCommandResult>(
                databaseName,
                readerSettings,
                writerSettings,
                command,
                QueryFlags.SlaveOk,
                null, // options
                null, // readPreference
                null, // serializationOptions
                resultSerializer);

            return commandOperation.Execute(connection);
        }

        private void StateVerificationTimerCallback()
        {
            if (_inStateVerification)
            {
                return;
            }

            _inStateVerification = true;
            try
            {
                var connection = _connectionPool.AcquireConnection(_stateVerificationAcquireConnectionOptions);
                try
                {
                    Ping(connection);
                    LookupServerInformation(connection);
                    ThreadPool.QueueUserWorkItem(o => _connectionPool.MaintainPoolSize());
                    SetState(MongoServerState.Connected);
                }
                finally
                {
                    _connectionPool.ReleaseConnection(connection);
                }
            }
            catch { } // this is called in a timer thread and we don't want any exceptions escaping
            finally
            {
                _inStateVerification = false;
            }
        }

        /// <remarks>This method must be called outside of a lock.</remarks>
        private void SetState(MongoServerState newState, ServerInformation newServerInfo)
        {
            bool raiseChangedEvent = false;
            lock (_serverInstanceLock)
            {
                if (_state != newState)
                {
                    _state = newState;
                    raiseChangedEvent = true;
                }

                if (newState == MongoServerState.Disconnected)
                {
                    _connectionPool.Clear();
                }

                if (_serverInfo != newServerInfo && _serverInfo.IsDifferentFrom(newServerInfo))
                {
                    _serverInfo = newServerInfo;
                    raiseChangedEvent = true;
                }
            }

            if (raiseChangedEvent)
            {
                OnStateChanged();
            }
        }

        // NOTE: while all these properties are mutable, it is purely for ease of use.  This class is used as an immutable class.
        private class ServerInformation
        {
            public MongoServerBuildInfo BuildInfo { get; set; }

            public MongoServerInstanceType InstanceType { get; set; }

            public bool IsArbiter { get; set; }

            public IsMasterResult IsMasterResult { get; set; }

            public bool IsPassive { get; set; }

            public bool IsPrimary { get; set; }

            public bool IsSecondary { get; set; }

            public int MaxDocumentSize { get; set; }

            public int MaxMessageLength { get; set; }

            public ReplicaSetInformation ReplicaSetInformation { get; set; }

            public bool IsDifferentFrom(ServerInformation other)
            {
                if (InstanceType != other.InstanceType)
                {
                    return true;
                }

                if (IsPrimary != other.IsPrimary)
                {
                    return true;
                }

                if (IsSecondary != other.IsSecondary)
                {
                    return true;
                }

                if (IsPassive != other.IsPassive)
                {
                    return true;
                }

                if (IsArbiter != other.IsArbiter)
                {
                    return true;
                }

                if (MaxDocumentSize != other.MaxDocumentSize)
                {
                    return true;
                }

                if (MaxMessageLength != other.MaxMessageLength)
                {
                    return true;
                }

                if ((ReplicaSetInformation == null && other.ReplicaSetInformation != null) || (ReplicaSetInformation != other.ReplicaSetInformation))
                {
                    return true;
                }

                return false;
            }
        }
    }
}
