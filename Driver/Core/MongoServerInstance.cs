/* Copyright 2010-2011 10gen Inc.
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
        #region public events
        /// <summary>
        /// Occurs when the value of the State property changes.
        /// </summary>
        public event EventHandler StateChanged;
        #endregion

        #region private fields
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
            this.maxDocumentSize = MongoDefaults.MaxDocumentSize;
            this.maxMessageLength = MongoDefaults.MaxMessageLength;
            this.state = MongoServerState.Disconnected;
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the address of this server instance.
        /// </summary>
        public MongoServerAddress Address {
            get { return address; }
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
            set {
                if (state != value) {
                    // Console.WriteLine("{0} state: {1}{2}", address, value, isPrimary ? " (Primary)" : "");
                    state = value;
                    if (StateChanged != null) {
                        try { StateChanged(this, null); } catch { } // ignore exceptions
                    }
                }
            }
        }
        #endregion

        #region internal methods
        internal MongoConnection AcquireConnection(
            MongoDatabase database
        ) {
            var connection = connectionPool.AcquireConnection(database);
            try {
                connection.CheckAuthentication(database); // will authenticate if necessary
            } catch (MongoAuthenticationException) {
                // don't let the connection go to waste just because authentication failed
                connectionPool.ReleaseConnection(connection);
                throw;
            }

            return connection;
        }

        internal void Connect(
            bool slaveOk
        ) {
            if (state != MongoServerState.Disconnected) {
                var message = string.Format("MongoServerInstance.Connect can only be called when state is Disconnected, not when state is {0}.", state);
                throw new InvalidOperationException(message);
            }

            State = MongoServerState.Connecting;
            connectException = null;
            try {
                endPoint = address.ToIPEndPoint(server.Settings.AddressFamily);

                var connectionPool = new MongoConnectionPool(this);
                try {
                    var connection = connectionPool.AcquireConnection(null);
                    try {
                        try {
                            var isMasterCommand = new CommandDocument("ismaster", 1);
                            isMasterResult = connection.RunCommand("admin.$cmd", QueryFlags.SlaveOk, isMasterCommand);
                        } catch (MongoCommandException ex) {
                            isMasterResult = ex.CommandResult;
                            throw;
                        }

                        isPrimary = isMasterResult.Response["ismaster", false].ToBoolean();
                        isSecondary = isMasterResult.Response["secondary", false].ToBoolean();
                        isPassive = isMasterResult.Response["passive", false].ToBoolean();
                        isArbiter = isMasterResult.Response["arbiterOnly", false].ToBoolean();
                        if (!isPrimary && !slaveOk) {
                            throw new MongoConnectionException("Server is not a primary and SlaveOk is false.");
                        }

                        maxDocumentSize = isMasterResult.Response["maxBsonObjectSize", MongoDefaults.MaxDocumentSize].ToInt32();
                        maxMessageLength = Math.Max(MongoDefaults.MaxMessageLength, maxDocumentSize + 1024); // derived from maxDocumentSize

                        var buildInfoCommand = new CommandDocument("buildinfo", 1);
                        var buildInfoResult = connection.RunCommand("admin.$cmd", QueryFlags.SlaveOk, buildInfoCommand);
                        buildInfo = new MongoServerBuildInfo(
                            buildInfoResult.Response["bits"].ToInt32(), // bits
                            buildInfoResult.Response["gitVersion"].AsString, // gitVersion
                            buildInfoResult.Response["sysInfo"].AsString, // sysInfo
                            buildInfoResult.Response["version"].AsString // versionString
                        );
                    } finally {
                        connectionPool.ReleaseConnection(connection);
                    }
                } catch {
                    connectionPool.Close();
                    throw;
                }

                State = MongoServerState.Connected;
                this.connectionPool = connectionPool;
            } catch (Exception ex) {
                State = MongoServerState.Disconnected;
                connectException = ex;
                throw;
            }
        }

        internal void Disconnect() {
            if (state != MongoServerState.Disconnected) {
                try {
                    // if we fail during Connect the connectionPool field will still be null
                    if (connectionPool != null) {
                        connectionPool.Close();
                        connectionPool = null;
                    }
                } finally {
                    State = MongoServerState.Disconnected;
                }
            }
        }
        #endregion
    }
}
