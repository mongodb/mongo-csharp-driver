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
using System.Net.Sockets;
using System.Text;

using MongoDB.Bson;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver
{
    /// <summary>
    /// The settings used to access a MongoDB server.
    /// </summary>
    public class MongoServerSettings
    {
        // private fields
        private ConnectionMode connectionMode;
        private TimeSpan connectTimeout;
        private MongoCredentials defaultCredentials;
        private GuidRepresentation guidRepresentation;
        private bool ipv6;
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

        // constructors
        /// <summary>
        /// Creates a new instance of MongoServerSettings. Usually you would use a connection string instead.
        /// </summary>
        public MongoServerSettings()
        {
            connectionMode = ConnectionMode.Direct;
            connectTimeout = MongoDefaults.ConnectTimeout;
            defaultCredentials = null;
            guidRepresentation = MongoDefaults.GuidRepresentation;
            ipv6 = false;
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

        /// <summary>
        /// Creates a new instance of MongoServerSettings. Usually you would use a connection string instead.
        /// </summary>
        /// <param name="connectionMode">The connection mode (Direct or ReplicaSet).</param>
        /// <param name="connectTimeout">The connect timeout.</param>
        /// <param name="defaultCredentials">The default credentials.</param>
        /// <param name="guidRepresentation">The representation for Guids.</param>
        /// <param name="ipv6">Whether to use IPv6.</param>
        /// <param name="maxConnectionIdleTime">The max connection idle time.</param>
        /// <param name="maxConnectionLifeTime">The max connection life time.</param>
        /// <param name="maxConnectionPoolSize">The max connection pool size.</param>
        /// <param name="minConnectionPoolSize">The min connection pool size.</param>
        /// <param name="replicaSetName">The name of the replica set.</param>
        /// <param name="safeMode">The safe mode.</param>
        /// <param name="servers">The server addresses (normally one unless it is the seed list for connecting to a replica set).</param>
        /// <param name="slaveOk">Whether queries should be sent to secondary servers.</param>
        /// <param name="socketTimeout">The socket timeout.</param>
        /// <param name="waitQueueSize">The wait queue size.</param>
        /// <param name="waitQueueTimeout">The wait queue timeout.</param>
        public MongoServerSettings(ConnectionMode connectionMode, TimeSpan connectTimeout, MongoCredentials defaultCredentials, GuidRepresentation guidRepresentation, bool ipv6, TimeSpan maxConnectionIdleTime, TimeSpan maxConnectionLifeTime, int maxConnectionPoolSize, int minConnectionPoolSize, string replicaSetName, SafeMode safeMode, IEnumerable<MongoServerAddress> servers, bool slaveOk, TimeSpan socketTimeout, int waitQueueSize, TimeSpan waitQueueTimeout)
        {
            this.connectionMode = connectionMode;
            this.connectTimeout = connectTimeout;
            this.defaultCredentials = defaultCredentials;
            this.guidRepresentation = guidRepresentation;
            this.ipv6 = ipv6;
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

        // public properties
        /// <summary>
        /// Gets the AddressFamily for the IPEndPoint (derived from the IPv6 setting).
        /// </summary>
        public AddressFamily AddressFamily
        {
            get { return ipv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork; }
        }

        /// <summary>
        /// Gets or sets the connection mode.
        /// </summary>
        public ConnectionMode ConnectionMode
        {
            get { return connectionMode; }
            set
            {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                connectionMode = value;
            }
        }

        /// <summary>
        /// Gets or sets the connect timeout.
        /// </summary>
        public TimeSpan ConnectTimeout
        {
            get { return connectTimeout; }
            set
            {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                connectTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the default credentials.
        /// </summary>
        public MongoCredentials DefaultCredentials
        {
            get { return defaultCredentials; }
            set
            {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                defaultCredentials = value;
            }
        }

        /// <summary>
        /// Gets or sets the representation to use for Guids.
        /// </summary>
        public GuidRepresentation GuidRepresentation
        {
            get { return guidRepresentation; }
            set
            {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                guidRepresentation = value;
            }
        }

        /// <summary>
        /// Gets whether the settings have been frozen to prevent further changes.
        /// </summary>
        public bool IsFrozen
        {
            get { return isFrozen; }
        }

        /// <summary>
        /// Gets or sets whether to use IPv6.
        /// </summary>
        public bool IPv6
        {
            get { return ipv6; }
            set
            {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                ipv6 = value;
            }
        }

        /// <summary>
        /// Gets or sets the max connection idle time.
        /// </summary>
        public TimeSpan MaxConnectionIdleTime
        {
            get { return maxConnectionIdleTime; }
            set
            {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                maxConnectionIdleTime = value;
            }
        }

        /// <summary>
        /// Gets or sets the max connection life time.
        /// </summary>
        public TimeSpan MaxConnectionLifeTime
        {
            get { return maxConnectionLifeTime; }
            set
            {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                maxConnectionLifeTime = value;
            }
        }

        /// <summary>
        /// Gets or sets the max connection pool size.
        /// </summary>
        public int MaxConnectionPoolSize
        {
            get { return maxConnectionPoolSize; }
            set
            {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                maxConnectionPoolSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the min connection pool size.
        /// </summary>
        public int MinConnectionPoolSize
        {
            get { return minConnectionPoolSize; }
            set
            {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                minConnectionPoolSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the replica set.
        /// </summary>
        public string ReplicaSetName
        {
            get { return replicaSetName; }
            set
            {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                replicaSetName = value;
            }
        }

        /// <summary>
        /// Gets or sets the SafeMode to use.
        /// </summary>
        public SafeMode SafeMode
        {
            get { return safeMode; }
            set
            {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                safeMode = value;
            }
        }

        /// <summary>
        /// Gets or sets the address of the server (see also Servers if using more than one address).
        /// </summary>
        public MongoServerAddress Server
        {
            get { return (servers == null) ? null : servers.Single(); }
            set
            {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                servers = new MongoServerAddress[] { value };
            }
        }

        /// <summary>
        /// Gets or sets the list of server addresses (see also Server if using only one address).
        /// </summary>
        public IEnumerable<MongoServerAddress> Servers
        {
            get { return servers; }
            set
            {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                servers = value;
            }
        }

        /// <summary>
        /// Gets or sets whether queries should be sent to secondary servers.
        /// </summary>
        public bool SlaveOk
        {
            get { return slaveOk; }
            set
            {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                slaveOk = value;
            }
        }

        /// <summary>
        /// Gets or sets the socket timeout.
        /// </summary>
        public TimeSpan SocketTimeout
        {
            get { return socketTimeout; }
            set
            {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                socketTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the wait queue size.
        /// </summary>
        public int WaitQueueSize
        {
            get { return waitQueueSize; }
            set
            {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                waitQueueSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the wait queue timeout.
        /// </summary>
        public TimeSpan WaitQueueTimeout
        {
            get { return waitQueueTimeout; }
            set
            {
                if (isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                waitQueueTimeout = value;
            }
        }

        // public methods
        /// <summary>
        /// Creates a clone of the settings.
        /// </summary>
        /// <returns>A clone of the settings.</returns>
        public MongoServerSettings Clone()
        {
            return new MongoServerSettings(connectionMode, connectTimeout, defaultCredentials, guidRepresentation, ipv6, maxConnectionIdleTime, maxConnectionLifeTime, maxConnectionPoolSize, minConnectionPoolSize, replicaSetName, safeMode, servers, slaveOk, socketTimeout, waitQueueSize, waitQueueTimeout);
        }

        /// <summary>
        /// Compares two MongoServerSettings instances.
        /// </summary>
        /// <param name="obj">The other instance.</param>
        /// <returns>True if the two instances are equal.</returns>
        public override bool Equals(object obj)
        {
            var rhs = obj as MongoServerSettings;
            if (rhs == null)
            {
                return false;
            }
            else
            {
                if (this.isFrozen && rhs.isFrozen)
                {
                    return this.frozenStringRepresentation == rhs.frozenStringRepresentation;
                }
                else
                {
                    return
                        this.connectionMode == rhs.connectionMode &&
                        this.connectTimeout == rhs.connectTimeout &&
                        this.defaultCredentials == rhs.defaultCredentials &&
                        this.guidRepresentation == rhs.guidRepresentation &&
                        this.ipv6 == rhs.ipv6 &&
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

        /// <summary>
        /// Freezes the settings.
        /// </summary>
        /// <returns>The frozen settings.</returns>
        public MongoServerSettings Freeze()
        {
            if (!isFrozen)
            {
                safeMode = safeMode.FrozenCopy();
                frozenHashCode = GetHashCodeHelper();
                frozenStringRepresentation = ToStringHelper();
                isFrozen = true;
            }
            return this;
        }

        /// <summary>
        /// Returns a frozen copy of the settings.
        /// </summary>
        /// <returns>A frozen copy of the settings.</returns>
        public MongoServerSettings FrozenCopy()
        {
            if (isFrozen)
            {
                return this;
            }
            else
            {
                return Clone().Freeze();
            }
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            if (isFrozen)
            {
                return frozenHashCode;
            }
            else
            {
                return GetHashCodeHelper();
            }
        }

        /// <summary>
        /// Returns a string representation of the settings.
        /// </summary>
        /// <returns>A string representation of the settings.</returns>
        public override string ToString()
        {
            if (isFrozen)
            {
                return frozenStringRepresentation;
            }
            else
            {
                return ToStringHelper();
            }
        }

        // private methods
        private int GetHashCodeHelper()
        {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + connectionMode.GetHashCode();
            hash = 37 * hash + connectTimeout.GetHashCode();
            hash = 37 * hash + (defaultCredentials == null ? 0 : defaultCredentials.GetHashCode());
            hash = 37 * hash + guidRepresentation.GetHashCode();
            hash = 37 * hash + ipv6.GetHashCode();
            hash = 37 * hash + maxConnectionIdleTime.GetHashCode();
            hash = 37 * hash + maxConnectionLifeTime.GetHashCode();
            hash = 37 * hash + maxConnectionPoolSize.GetHashCode();
            hash = 37 * hash + minConnectionPoolSize.GetHashCode();
            hash = 37 * hash + (replicaSetName == null ? 0 : replicaSetName.GetHashCode());
            hash = 37 * hash + (safeMode == null ? 0 : safeMode.GetHashCode());
            if (servers != null)
            {
                foreach (var server in servers)
                {
                    hash = 37 * hash + server.GetHashCode();
                }
            }
            hash = 37 * hash + slaveOk.GetHashCode();
            hash = 37 * hash + socketTimeout.GetHashCode();
            hash = 37 * hash + waitQueueSize.GetHashCode();
            hash = 37 * hash + waitQueueTimeout.GetHashCode();
            return hash;
        }

        private string ToStringHelper()
        {
            var sb = new StringBuilder();
            string serversString = null;
            if (servers != null)
            {
                serversString = string.Join(",", servers.Select(s => s.ToString()).ToArray());
            }
            sb.AppendFormat("ConnectionMode={0};", connectionMode);
            sb.AppendFormat("ConnectTimeout={0};", connectTimeout);
            sb.AppendFormat("DefaultCredentials={0};", defaultCredentials);
            sb.AppendFormat("GuidRepresentation={0};", guidRepresentation);
            sb.AppendFormat("IPv6={0};", ipv6);
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
    }
}
