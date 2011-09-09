﻿/* Copyright 2010-2011 10gen Inc.
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

namespace MongoDB.Driver {
    /// <summary>
    /// Represents an instance of a MongoDB server host (in the case of a replica set a MongoServer uses multiple MongoServerInstances).
    /// </summary>
    public class MongoServerInstance {
        #region private static fields
        private static int nextSequentialId;
        #endregion

        #region public events
        /// <summary>
        /// Occurs when the value of the State property changes.
        /// </summary>
        public event EventHandler StateChanged;
        #endregion

        #region private fields
        private object serverInstanceLock = new object();
        private MongoServerAddress address;
        private MongoServerBuildInfo buildInfo;
        private Exception connectException;
        private MongoConnectionPool connectionPool;
        private IPEndPoint endPoint;
        private bool isArbiter;
        private CommandResult isMasterResult;
        private bool isPassive;
        private bool isPrimary;
        private bool isSecondary;
        private int maxDocumentSize;
        private int maxMessageLength;
        private int sequentialId;
        private MongoServer server;
        private MongoServerState state; // always use property to set value so event gets raised
        #endregion

        #region constructors
        internal MongoServerInstance(
            MongoServer server,
            MongoServerAddress address
        ) {
            this.server = server;
            this.address = address;
            this.sequentialId = Interlocked.Increment(ref nextSequentialId);
            this.maxDocumentSize = MongoDefaults.MaxDocumentSize;
            this.maxMessageLength = MongoDefaults.MaxMessageLength;
            this.state = MongoServerState.Disconnected;
            this.connectionPool = new MongoConnectionPool(this);
            // Console.WriteLine("MongoServerInstance[{0}]: {1}", sequentialId, address);
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the address of this server instance.
        /// </summary>
        public MongoServerAddress Address {
            get { return address; }
            internal set {
                lock (serverInstanceLock) {
                    if (state != MongoServerState.Disconnected) {
                        throw new MongoInternalException("MongoServerInstance Address can only be set when State is Disconnected.");
                    }
                    address = value;
                }
            }
        }

        /// <summary>
        /// Gets the version of this server instance.
        /// </summary>
        public MongoServerBuildInfo BuildInfo {
            get { return buildInfo; }
        }

        /// <summary>
        /// Gets the exception thrown the last time Connect was called (null if Connect did not throw an exception).
        /// </summary>
        public Exception ConnectException {
            get { return connectException; }
            internal set { connectException = value; }
        }

        /// <summary>
        /// Gets the connection pool for this server instance.
        /// </summary>
        public MongoConnectionPool ConnectionPool {
            get { return connectionPool; }
        }

        /// <summary>
        /// Gets the IP end point of this server instance.
        /// </summary>
        public IPEndPoint EndPoint {
            get { return endPoint; }
        }

        /// <summary>
        /// Gets whether this server instance is an arbiter instance.
        /// </summary>
        public bool IsArbiter {
            get { return isArbiter; }
        }

        /// <summary>
        /// Gets the result of the most recent ismaster command sent to this server instance.
        /// </summary>
        public CommandResult IsMasterResult {
            get { return isMasterResult; }
        }

        /// <summary>
        /// Gets whether this server instance is a passive instance.
        /// </summary>
        public bool IsPassive {
            get { return isPassive; }
        }

        /// <summary>
        /// Gets whether this server instance is a primary.
        /// </summary>
        public bool IsPrimary {
            get { return isPrimary; }
        }

        /// <summary>
        /// Gets whether this server instance is a secondary.
        /// </summary>
        public bool IsSecondary {
            get { return isSecondary; }
        }

        /// <summary>
        /// Gets the max document size for this server instance.
        /// </summary>
        public int MaxDocumentSize {
            get { return maxDocumentSize; }
        }

        /// <summary>
        /// Gets the max message length for this server instance.
        /// </summary>
        public int MaxMessageLength {
            get { return maxMessageLength; }
        }

        /// <summary>
        /// Gets the unique sequential Id for this server instance.
        /// </summary>
        public int SequentialId {
            get { return sequentialId; }
        }

        /// <summary>
        /// Gets the server for this server instance.
        /// </summary>
        public MongoServer Server {
            get { return server; }
        }

        /// <summary>
        /// Gets the state of this server instance.
        /// </summary>
        public MongoServerState State {
            get { return state; }
            internal set {
                lock (serverInstanceLock) {
                    if (state != value) {
                        // Console.WriteLine("MongoServerInstance[{0}]: State changed: state={1}{2}", sequentialId, value, isPrimary ? " (Primary)" : "");
                        state = value;
                        if (StateChanged != null) {
                            try { StateChanged(this, null); } catch { } // ignore exceptions
                        }
                    }
                }
            }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Checks whether the server is alive (throws an exception if not).
        /// </summary>
        public void Ping() {
            var connection = connectionPool.AcquireConnection(null);
            try {
                var pingCommand = new CommandDocument("ping", 1);
                connection.RunCommand("admin.$cmd", QueryFlags.SlaveOk, pingCommand);
            } finally {
                connectionPool.ReleaseConnection(connection);
            }
        }

        /// <summary>
        /// Verifies the state of the server instance.
        /// </summary>
        public void VerifyState() {
            lock (serverInstanceLock) {
                // Console.WriteLine("MongoServerInstance[{0}]: VerifyState called.", sequentialId);
                // if ping fails assume all connections in the connection pool are doomed
                try {
                    Ping();
                } catch (Exception ex) {
                    // Console.WriteLine("MongoServerInstance[{0}]: Ping failed: {1}.", sequentialId, ex.Message);
                    connectionPool.Clear();
                }

                var connection = connectionPool.AcquireConnection(null);
                try {
                    var previousState = state;
                    try {
                        VerifyState(connection);
                    } catch (Exception ex) {
                        // ignore exceptions (if any occured state will already be set to Disconnected)
                        // Console.WriteLine("MongoServerInstance[{0}]: VerifyState failed: {1}.", sequentialId, ex.Message);
                    }
                    if (state != previousState && state == MongoServerState.Disconnected) {
                        connectionPool.Clear();
                    }
                } finally {
                    ReleaseConnection(connection);
                }
            }
        }
        #endregion

        #region internal methods
        internal MongoConnection AcquireConnection(
            MongoDatabase database
        ) {
            MongoConnection connection;
            lock (serverInstanceLock) {
                if (state != MongoServerState.Connected) {
                    var message = string.Format("Server instance {0} is no longer connected.", address);
                    throw new InvalidOperationException(message);
                }
                connection = connectionPool.AcquireConnection(database);
            }

            // check authentication outside the lock because it might involve a round trip to the server
            try {
                connection.CheckAuthentication(database); // will authenticate if necessary
            } catch (MongoAuthenticationException) {
                // don't let the connection go to waste just because authentication failed
                ReleaseConnection(connection); // ReleaseConnection will reacquire the lock
                throw;
            }

            return connection;
        }

        internal void Connect(
            bool slaveOk
        ) {
            // Console.WriteLine("MongoServerInstance[{0}]: Connect(slaveOk={1}) called.", sequentialId, slaveOk);
            lock (serverInstanceLock) {
                // note: don't check that state is Disconnected here
                // when reconnecting to a replica set state can transition from Connected -> Connecting -> Connected

                State = MongoServerState.Connecting;
                connectException = null;
                try {
                    endPoint = address.ToIPEndPoint(server.Settings.AddressFamily);

                    try {
                        var connection = connectionPool.AcquireConnection(null);
                        try {
                            VerifyState(connection);
                            if (!isPrimary && !slaveOk) {
                                throw new MongoConnectionException("Server is not a primary and SlaveOk is false.");
                            }
                        } finally {
                            connectionPool.ReleaseConnection(connection);
                        }
                    } catch {
                        connectionPool.Clear();
                        throw;
                    }

                    State = MongoServerState.Connected;
                } catch (Exception ex) {
                    State = MongoServerState.Disconnected;
                    connectException = ex;
                    throw;
                }
            }
        }

        internal void Disconnect() {
            // Console.WriteLine("MongoServerInstance[{0}]: Disconnect called.", sequentialId);
            lock (serverInstanceLock) {
                if (state == MongoServerState.Disconnecting) {
                    throw new MongoInternalException("Disconnect called while disconnecting.");
                }
                if (state != MongoServerState.Disconnected) {
                    try {
                        State = MongoServerState.Disconnecting;
                        connectionPool.Clear();
                    } finally {
                        State = MongoServerState.Disconnected;
                    }
                }
            }
        }

        internal void ReleaseConnection(
            MongoConnection connection
        ) {
            lock (serverInstanceLock) {
                connectionPool.ReleaseConnection(connection);
            }
        }

        internal void VerifyState(
            MongoConnection connection
        ) {
            CommandResult isMasterResult = null;
            try {
                try {
                    var isMasterCommand = new CommandDocument("ismaster", 1);
                    isMasterResult = connection.RunCommand("admin.$cmd", QueryFlags.SlaveOk, isMasterCommand);
                } catch (MongoCommandException ex) {
                    isMasterResult = ex.CommandResult;
                    throw;
                }

                var isPrimary = isMasterResult.Response["ismaster", false].ToBoolean();
                var isSecondary = isMasterResult.Response["secondary", false].ToBoolean();
                var isPassive = isMasterResult.Response["passive", false].ToBoolean();
                var isArbiter = isMasterResult.Response["arbiterOnly", false].ToBoolean();
                // workaround for CSHARP-273
                if (isPassive && isArbiter) { isPassive = false; }

                var maxDocumentSize = isMasterResult.Response["maxBsonObjectSize", MongoDefaults.MaxDocumentSize].ToInt32();
                var maxMessageLength = Math.Max(MongoDefaults.MaxMessageLength, maxDocumentSize + 1024); // derived from maxDocumentSize

                var buildInfoCommand = new CommandDocument("buildinfo", 1);
                var buildInfoResult = connection.RunCommand("admin.$cmd", QueryFlags.SlaveOk, buildInfoCommand);
                var buildInfo = new MongoServerBuildInfo(
                    buildInfoResult.Response["bits"].ToInt32(), // bits
                    buildInfoResult.Response["gitVersion"].AsString, // gitVersion
                    buildInfoResult.Response["sysInfo"].AsString, // sysInfo
                    buildInfoResult.Response["version"].AsString // versionString
                );

                this.isMasterResult = isMasterResult;
                this.isPrimary = isPrimary;
                this.isSecondary = isSecondary;
                this.isPassive = isPassive;
                this.isArbiter = isArbiter;
                this.maxDocumentSize = maxDocumentSize;
                this.maxMessageLength = maxMessageLength;
                this.buildInfo = buildInfo;
                this.State = MongoServerState.Connected;
            } catch {
                this.isMasterResult = isMasterResult;
                this.isPrimary = false;
                this.isSecondary = false;
                this.isPassive = false;
                this.isArbiter = false;
                this.maxDocumentSize = MongoDefaults.MaxDocumentSize;
                this.maxMessageLength = MongoDefaults.MaxMessageLength;
                this.buildInfo = null;
                this.State = MongoServerState.Disconnected;
                throw;
            }
        }
        #endregion
    }
}
