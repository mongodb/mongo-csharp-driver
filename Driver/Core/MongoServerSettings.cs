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
using System.Text;

using MongoDB.Bson;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver {
    public class MongoServerSettings {
        #region private fields
        private ConnectionMode connectionMode;
        private TimeSpan connectTimeout;
        private MongoCredentials defaultCredentials;
        private TimeSpan maxConnectionIdleTime;
        private TimeSpan maxConnectionLifeTime;
        private int maxConnectionPoolSize;
        private int minConnectionPoolSize;
        private string replicaSetName;
        private SafeMode safeMode;
        private IEnumerable<MongoServerAddress> servers;
        private bool slaveOk;
        private TimeSpan socketTimeout;
        private int waitQueueSize;
        private TimeSpan waitQueueTimeout;
        // the following fields are set when Freeze is called
        private bool isFrozen;
        private int frozenHashCode;
        private string frozenStringRepresentation;
        #endregion

        #region constructors
        public MongoServerSettings() {
            connectionMode = ConnectionMode.Direct;
            connectTimeout = MongoDefaults.ConnectTimeout;
            defaultCredentials = null;
            maxConnectionIdleTime = MongoDefaults.MaxConnectionIdleTime;
            maxConnectionLifeTime = MongoDefaults.MaxConnectionLifeTime;
            maxConnectionPoolSize = MongoDefaults.MaxConnectionPoolSize;
            minConnectionPoolSize = MongoDefaults.MinConnectionPoolSize;
            replicaSetName = null;
            safeMode = MongoDefaults.SafeMode;
            servers = null;
            slaveOk = false;
            socketTimeout = MongoDefaults.SocketTimeout;
            waitQueueSize = MongoDefaults.ComputedWaitQueueSize;
            waitQueueTimeout = MongoDefaults.WaitQueueTimeout;
        }

        public MongoServerSettings(
            ConnectionMode connectionMode,
            TimeSpan connectTimeout,
            MongoCredentials defaultCredentials,
            TimeSpan maxConnectionIdleTime,
            TimeSpan maxConnectionLifeTime,
            int maxConnectionPoolSize,
            int minConnectionPoolSize,
            string replicaSetName,
            SafeMode safeMode,
            IEnumerable<MongoServerAddress> servers,
            bool slaveOk,
            TimeSpan socketTimeout,
            int waitQueueSize,
            TimeSpan waitQueueTimeout
        ) {
            this.connectionMode = connectionMode;
            this.connectTimeout = connectTimeout;
            this.defaultCredentials = defaultCredentials;
            this.maxConnectionIdleTime = maxConnectionIdleTime;
            this.maxConnectionLifeTime = maxConnectionLifeTime;
            this.maxConnectionPoolSize = maxConnectionPoolSize;
            this.minConnectionPoolSize = minConnectionPoolSize;
            this.replicaSetName = replicaSetName;
            this.safeMode = safeMode;
            this.servers = servers;
            this.slaveOk = slaveOk;
            this.socketTimeout = socketTimeout;
            this.waitQueueSize = waitQueueSize;
            this.waitQueueTimeout = waitQueueTimeout;
        }
        #endregion

        #region public properties
        public ConnectionMode ConnectionMode {
            get { return connectionMode; }
            set {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen"); }
                connectionMode = value;
            }
        }

        public TimeSpan ConnectTimeout {
            get { return connectTimeout; }
            set {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen"); }
                connectTimeout = value;
            }
        }

        public MongoCredentials DefaultCredentials {
            get { return defaultCredentials; }
            set {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen"); }
                defaultCredentials = value;
            }
        }

        public bool IsFrozen {
            get { return isFrozen; }
        }

        public TimeSpan MaxConnectionIdleTime {
            get { return maxConnectionIdleTime; }
            set {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen"); }
                maxConnectionIdleTime = value;
            }
        }

        public TimeSpan MaxConnectionLifeTime {
            get { return maxConnectionLifeTime; }
            set {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen"); }
                maxConnectionLifeTime = value;
            }
        }

        public int MaxConnectionPoolSize {
            get { return maxConnectionPoolSize; }
            set {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen"); }
                maxConnectionPoolSize = value;
            }
        }

        public int MinConnectionPoolSize {
            get { return minConnectionPoolSize; }
            set {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen"); }
                minConnectionPoolSize = value;
            }
        }

        public string ReplicaSetName {
            get { return replicaSetName; }
            set {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen"); }
                replicaSetName = value;
            }
        }

        public SafeMode SafeMode {
            get { return safeMode; }
            set {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen"); }
                safeMode = value;
            }
        }

        public MongoServerAddress Server {
            get { return (servers == null) ? null : servers.Single(); }
            set {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen"); }
                servers = new MongoServerAddress[] { value };
            }
        }

        public IEnumerable<MongoServerAddress> Servers {
            get { return servers; }
            set {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen"); }
                servers = value;
            }
        }

        public bool SlaveOk {
            get { return slaveOk; }
            set {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen"); }
                slaveOk = value;
            }
        }

        public TimeSpan SocketTimeout {
            get { return socketTimeout; }
            set {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen"); }
                socketTimeout = value;
            }
        }

        public int WaitQueueSize {
            get { return waitQueueSize; }
            set {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen"); }
                waitQueueSize = value;
            }
        }

        public TimeSpan WaitQueueTimeout {
            get { return waitQueueTimeout; }
            set {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen"); }
                waitQueueTimeout = value;
            }
        }
        #endregion

        #region public methods
        public void Freeze() {
            if (!isFrozen) {
                frozenHashCode = GetHashCodeHelper();
                frozenStringRepresentation = ToStringHelper();
                isFrozen = true;
            }
        }

        public override bool Equals(object obj) {
            var rhs = obj as MongoServerSettings;
            if (rhs == null) {
                return false;
            } else {
                if (this.isFrozen && rhs.isFrozen) {
                    return this.frozenStringRepresentation == rhs.frozenStringRepresentation;
                } else {
                    return
                        this.connectionMode == rhs.connectionMode &&
                        this.connectTimeout == rhs.connectTimeout &&
                        this.defaultCredentials == rhs.defaultCredentials &&
                        this.maxConnectionIdleTime == rhs.maxConnectionIdleTime &&
                        this.maxConnectionLifeTime == rhs.maxConnectionLifeTime &&
                        this.maxConnectionPoolSize == rhs.maxConnectionPoolSize &&
                        this.minConnectionPoolSize == rhs.minConnectionPoolSize &&
                        this.replicaSetName == rhs.replicaSetName &&
                        this.safeMode == rhs.safeMode &&
                        (this.servers == null && rhs.servers == null || this.servers.SequenceEqual(rhs.servers)) &&
                        this.slaveOk == rhs.slaveOk &&
                        this.socketTimeout == rhs.socketTimeout &&
                        this.waitQueueSize == rhs.waitQueueSize &&
                        this.waitQueueTimeout == rhs.waitQueueTimeout;
                }
            }
        }

        public override int GetHashCode() {
            if (isFrozen) {
                return frozenHashCode;
            } else {
                return GetHashCodeHelper();
            }
        }

        public override string ToString() {
            if (isFrozen) {
                return frozenStringRepresentation;
            } else {
                return ToStringHelper();
            }
        }
        #endregion

        #region private methods
        private int GetHashCodeHelper() {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + connectionMode.GetHashCode();
            hash = 37 * hash + connectTimeout.GetHashCode();
            hash = 37 * hash + (defaultCredentials == null ? 0 : defaultCredentials.GetHashCode());
            hash = 37 * hash + maxConnectionIdleTime.GetHashCode();
            hash = 37 * hash + maxConnectionLifeTime.GetHashCode();
            hash = 37 * hash + maxConnectionPoolSize.GetHashCode();
            hash = 37 * hash + minConnectionPoolSize.GetHashCode();
            hash = 37 * hash + (replicaSetName == null ? 0 : replicaSetName.GetHashCode());
            hash = 37 * hash + (safeMode == null ? 0 : safeMode.GetHashCode());
            hash = 37 * hash + (servers == null ? 0 : servers.GetHashCode());
            hash = 37 * hash + slaveOk.GetHashCode();
            hash = 37 * hash + socketTimeout.GetHashCode();
            hash = 37 * hash + waitQueueSize.GetHashCode();
            hash = 37 * hash + waitQueueTimeout.GetHashCode();
            return hash;
        }

        private string ToStringHelper() {
            var sb = new StringBuilder();
            string serversString = null;
            if (servers != null) {
                serversString = string.Join(",", servers.Select(s => s.ToString()).ToArray());
            }
            sb.AppendFormat("ConnectionMode={0};", connectionMode);
            sb.AppendFormat("ConnectTimeout={0};", connectTimeout);
            sb.AppendFormat("DefaultCredentials={0};", defaultCredentials);
            sb.AppendFormat("MaxConnectionIdleTime={0};", maxConnectionIdleTime);
            sb.AppendFormat("MaxConnectionLifeTime={0};", maxConnectionLifeTime);
            sb.AppendFormat("MaxConnectionPoolSize={0};", maxConnectionPoolSize);
            sb.AppendFormat("MinConnectionPoolSize={0};", minConnectionPoolSize);
            sb.AppendFormat("ReplicaSetName={0};", replicaSetName);
            sb.AppendFormat("SafeMode={0};", safeMode);
            sb.AppendFormat("Servers={0};", serversString);
            sb.AppendFormat("SlaveOk={0};", slaveOk);
            sb.AppendFormat("SocketTimeout={0};", socketTimeout);
            sb.AppendFormat("WaitQueueSize={0};", waitQueueSize);
            sb.AppendFormat("WaitQueueTimeout={0}", waitQueueTimeout);
            return sb.ToString();
        }
        #endregion
    }
}
