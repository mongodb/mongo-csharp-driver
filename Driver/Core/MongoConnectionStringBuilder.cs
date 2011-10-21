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
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using MongoDB.Bson;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver {
    /// <summary>
    /// Represents .NET style connection strings. We recommend you use URL style connection strings
    /// (see MongoUrl and MongoUrlBuilder).
    /// </summary>
    public class MongoConnectionStringBuilder : DbConnectionStringBuilder {
        #region private static fields
        private static Dictionary<string, string> canonicalKeywords = new Dictionary<string, string> {
            { "connect", "connect" },
            { "connecttimeout", "connectTimeout" },
            { "connecttimeoutms", "connectTimeoutMS" },
            { "database", "database" },
            { "fsync", "fsync" },
            { "guids", "guids" },
            { "j", "j" },
            { "maxidletime", "maxIdleTime" },
            { "maxlifetime", "maxLifeTime" },
            { "maxpoolsize", "maxPoolSize" },
            { "minpoolsize", "minPoolSize" },
            { "password", "password" },
            { "replicaset", "replicaSet" },
            { "safe", "safe" },
            { "server", "server" },
            { "servers", "server" },
            { "slaveok", "slaveOk" },
            { "sockettimeout", "socketTimeout" },
            { "sockettimeoutms", "socketTimeoutMS" },
            { "username", "username" },
            { "w", "w" },
            { "waitqueuemultiple", "waitQueueMultiple" },
            { "waitqueuesize", "waitQueueSize" },
            { "waitqueuetimeout", "waitQueueTimeout" },
            { "waitqueuetimeoutms", "waitQueueTimeoutMS" },
            { "wtimeout", "wtimeout" },
            { "wtimeoutms", "wtimeout" }
        };
        #endregion

        #region private fields
        // default values are set in ResetValues
        private ConnectionMode connectionMode;
        private TimeSpan connectTimeout;
        private string databaseName;
        private GuidRepresentation guidRepresentation;
        private bool ipv6;
        private TimeSpan maxConnectionIdleTime;
        private TimeSpan maxConnectionLifeTime;
        private int maxConnectionPoolSize;
        private int minConnectionPoolSize;
        private string password;
        private string replicaSetName;
        private SafeMode safeMode;
        private IEnumerable<MongoServerAddress> servers;
        private bool slaveOk;
        private TimeSpan socketTimeout;
        private string username;
        private double waitQueueMultiple;
        private int waitQueueSize;
        private TimeSpan waitQueueTimeout;
        #endregion

        #region constructors
        /// <summary>
        /// Creates a new instance of MongoConnectionStringBuilder.
        /// </summary>
        public MongoConnectionStringBuilder()
            : base() {
            ResetValues();
        }

        /// <summary>
        /// Creates a new instance of MongoConnectionStringBuilder.
        /// </summary>
        /// <param name="connectionString">The initial settings.</param>
        public MongoConnectionStringBuilder(
            string connectionString
        ) {
            ConnectionString = connectionString; // base class calls Clear which calls ResetValues
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the actual wait queue size (either WaitQueueSize or WaitQueueMultiple x MaxConnectionPoolSize).
        /// </summary>
        public int ComputedWaitQueueSize {
            get {
                if (waitQueueMultiple == 0.0) {
                    return waitQueueSize;
                } else {
                    return (int) (waitQueueMultiple * maxConnectionPoolSize);
                }
            }
        }

        /// <summary>
        /// Gets or sets the connection mode.
        /// </summary>
        public ConnectionMode ConnectionMode {
            get { return connectionMode; }
            set {
                connectionMode = value;
                base["connect"] = MongoUtils.ToCamelCase(value.ToString());
            }
        }

        /// <summary>
        /// Gets or sets the connect timeout.
        /// </summary>
        public TimeSpan ConnectTimeout {
            get { return connectTimeout; }
            set {
                connectTimeout = value;
                base["connectTimeout"] = MongoUrlBuilder.FormatTimeSpan(value);
            }
        }

        /// <summary>
        /// Gets or sets the optional database name.
        /// </summary>
        public string DatabaseName {
            get { return databaseName; }
            set {
                base["database"] = databaseName = value;
            }
        }

        /// <summary>
        /// Gets or sets the representation for Guids.
        /// </summary>
        public GuidRepresentation GuidRepresentation {
            get { return guidRepresentation; }
            set {
                base["guids"] = guidRepresentation = value;
            }
        }

        /// <summary>
        /// Gets or sets whether to use IPv6.
        /// </summary>
        public bool IPv6 {
            get { return ipv6; }
            set {
                ipv6 = value;
                base["ipv6"] = XmlConvert.ToString(value);
            }
        }

        /// <summary>
        /// Gets or sets the max connection idle time.
        /// </summary>
        public TimeSpan MaxConnectionIdleTime {
            get { return maxConnectionIdleTime; }
            set {
                maxConnectionIdleTime = value;
                base["maxIdleTime"] = MongoUrlBuilder.FormatTimeSpan(value);
            }
        }

        /// <summary>
        /// Gets or sets the max connection life time.
        /// </summary>
        public TimeSpan MaxConnectionLifeTime {
            get { return maxConnectionLifeTime; }
            set {
                maxConnectionLifeTime = value;
                base["maxLifeTime"] = MongoUrlBuilder.FormatTimeSpan(value);
            }
        }

        /// <summary>
        /// Gets or sets the max connection pool size.
        /// </summary>
        public int MaxConnectionPoolSize {
            get { return maxConnectionPoolSize; }
            set {
                maxConnectionPoolSize = value;
                base["maxPoolSize"] = XmlConvert.ToString(value);
            }
        }

        /// <summary>
        /// Gets or sets the min connection pool size.
        /// </summary>
        public int MinConnectionPoolSize {
            get { return minConnectionPoolSize; }
            set {
                minConnectionPoolSize = value;
                base["minPoolSize"] = XmlConvert.ToString(value);
            }
        }

        /// <summary>
        /// Gets or sets the default password.
        /// </summary>
        public string Password {
            get { return password; }
            set {
                base["password"] = password = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the replica set.
        /// </summary>
        public string ReplicaSetName {
            get { return replicaSetName; }
            set {
                ConnectionMode = ConnectionMode.ReplicaSet;
                base["replicaSet"] = replicaSetName = value;
            }
        }

        /// <summary>
        /// Gets or sets the SafeMode to use.
        /// </summary>
        public SafeMode SafeMode {
            get { return safeMode; }
            set {
                safeMode = value;
                if (value == null) {
                    base["safe"] = null;
                    base["w"] = null;
                    base["wtimeout"] = null;
                    base["fsync"] = null;
                    base["j"] = null;
                } else {
                    if (value.Enabled) {
                        base["safe"] = "true";
                        base["w"] = (value.W != 0) ? value.W.ToString() : (value.WMode != null) ? value.WMode : null;
                        base["wtimeout"] = (value.W != 0 && value.WTimeout != TimeSpan.Zero) ? MongoUrlBuilder.FormatTimeSpan(value.WTimeout) : null;
                        base["fsync"] = (value.FSync) ? "true" : null;
                        base["j"] = (value.J) ? "true" : null;
                    } else {
                        base["safe"] = "false";
                        base["w"] = null;
                        base["wtimeout"] = null;
                        base["fsync"] = null;
                        base["j"] = null;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the address of the server (see also Servers if using more than one address).
        /// </summary>
        public MongoServerAddress Server {
            get { return (servers == null) ? null : servers.Single(); }
            set {
                Servers = new[] { value };
            }
        }

        /// <summary>
        /// Gets or sets the list of server addresses (see also Server if using only one address).
        /// </summary>
        public IEnumerable<MongoServerAddress> Servers {
            get { return servers; }
            set {
                servers = value;
                base["server"] = GetServersString();
                connectionMode = (value.Count() == 1) ? ConnectionMode.Direct : ConnectionMode.ReplicaSet; // assign to field not to property
            }
        }

        /// <summary>
        /// Gets or sets whether queries should be sent to secondary servers.
        /// </summary>
        public bool SlaveOk {
            get { return slaveOk; }
            set {
                slaveOk = value;
                base["slaveOk"] = XmlConvert.ToString(value);
            }
        }

        /// <summary>
        /// Gets or sets the socket timeout.
        /// </summary>
        public TimeSpan SocketTimeout {
            get { return socketTimeout; }
            set {
                socketTimeout = value;
                base["socketTimeout"] = MongoUrlBuilder.FormatTimeSpan(value);
            }
        }

        /// <summary>
        /// Gets or sets the default username.
        /// </summary>
        public string Username {
            get { return username; }
            set {
                base["username"] = username = value;
            }
        }

        /// <summary>
        /// Gets or sets the wait queue multiple (the actual wait queue size will be WaitQueueMultiple x MaxConnectionPoolSize).
        /// </summary>
        public double WaitQueueMultiple {
            get { return waitQueueMultiple; }
            set {
                waitQueueMultiple = value;
                base["waitQueueMultiple"] = (value != 0) ? XmlConvert.ToString(value) : null;
            }
        }

        /// <summary>
        /// Gets or sets the wait queue size.
        /// </summary>
        public int WaitQueueSize {
            get { return waitQueueSize; }
            set {
                waitQueueSize = value;
                base["waitQueueSize"] = (value != 0) ? XmlConvert.ToString(value) : null;
            }
        }

        /// <summary>
        /// Gets or sets the wait queue timeout.
        /// </summary>
        public TimeSpan WaitQueueTimeout {
            get { return waitQueueTimeout; }
            set {
                waitQueueTimeout = value;
                base["waitQueueTimeout"] = MongoUrlBuilder.FormatTimeSpan(value);
            }
        }
        #endregion

        #region public indexers
        /// <summary>
        /// Gets or sets individual settings by keyword.
        /// </summary>
        /// <param name="keyword">The keyword.</param>
        /// <returns>The value of the setting.</returns>
        public override object this[
            string keyword
        ] {
            get {
                if (keyword == null) { throw new ArgumentNullException("keyword"); }
                return base[canonicalKeywords[keyword.ToLower()]];
            }
            set {
                if (keyword == null) { throw new ArgumentNullException("keyword"); }
                switch (keyword.ToLower()) {
                    case "connect":
                        if (value is string) {
                            ConnectionMode = (ConnectionMode) Enum.Parse(typeof(ConnectionMode), (string) value, true);
                        } else {
                            ConnectionMode = (ConnectionMode) value;
                        }
                        break;
                    case "connecttimeout":
                    case "connecttimeoutms":
                        ConnectTimeout = ToTimeSpan(keyword, value);
                        break;
                    case "database":
                        DatabaseName = (string) value;
                        break;
                    case "fsync":
                        if (safeMode == null) { safeMode = new SafeMode(false); }
                        safeMode.FSync = Convert.ToBoolean(value);
                        SafeMode = safeMode;
                        break;
                    case "guids":
                        GuidRepresentation = (GuidRepresentation) Enum.Parse(typeof(GuidRepresentation), (string) value, true); // ignoreCase
                        break;
                    case "ipv6":
                        IPv6 = Convert.ToBoolean(value);
                        break;
                    case "j":
                        if (safeMode == null) { safeMode = new SafeMode(false); }
                        safeMode.J = Convert.ToBoolean(value);
                        SafeMode = safeMode;
                        break;
                    case "maxidletime":
                    case "maxidletimems":
                        MaxConnectionIdleTime = ToTimeSpan(keyword, value);
                        break;
                    case "maxlifetime":
                    case "maxlifetimems":
                        MaxConnectionLifeTime = ToTimeSpan(keyword, value);
                        break;
                    case "maxpoolsize":
                        MaxConnectionPoolSize = Convert.ToInt32(value);
                        break;
                    case "minpoolsize":
                        MinConnectionPoolSize = Convert.ToInt32(value);
                        break;
                    case "password":
                        Password = (string) value;
                        break;
                    case "replicaset":
                        ReplicaSetName = (string) value;
                        ConnectionMode = ConnectionMode.ReplicaSet;
                        break;
                    case "safe":
                        if (safeMode == null) { safeMode = new SafeMode(false); }
                        safeMode.Enabled = Convert.ToBoolean(value);
                        SafeMode = safeMode;
                        break;
                    case "server":
                    case "servers":
                        Servers = ParseServersString((string) value);
                        break;
                    case "slaveok":
                        SlaveOk = Convert.ToBoolean(value);
                        break;
                    case "sockettimeout":
                    case "sockettimeoutms":
                        SocketTimeout = ToTimeSpan(keyword, value);
                        break;
                    case "username":
                        Username = (string) value;
                        break;
                    case "w":
                        if (safeMode == null) { safeMode = new SafeMode(false); }
                        try {
                            safeMode.W = Convert.ToInt32(value);
                        } catch (FormatException) {
                            safeMode.WMode = (string) value;
                        }
                        SafeMode = safeMode;
                        break;
                    case "waitqueuemultiple":
                        WaitQueueMultiple = Convert.ToDouble(value);
                        WaitQueueSize = 0;
                        break;
                    case "waitqueuesize":
                        WaitQueueSize = Convert.ToInt32(value);
                        WaitQueueMultiple = 0;
                        break;
                    case "waitqueuetimeout":
                    case "waitqueuetimeoutms":
                        WaitQueueTimeout = ToTimeSpan(keyword, value);
                        break;
                    case "wtimeout":
                        if (safeMode == null) { safeMode = new SafeMode(false); }
                        safeMode.WTimeout = ToTimeSpan(keyword, value);
                        SafeMode = safeMode;
                        break;
                    default:
                        var message = string.Format("Invalid keyword '{0}'.", keyword);
                        throw new ArgumentException(message);
                }
            }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Clears all settings to their default values.
        /// </summary>
        public override void Clear() {
            base.Clear();
            ResetValues();
        }

        /// <summary>
        /// Tests whether a keyword is valid.
        /// </summary>
        /// <param name="keyword">The keyword.</param>
        /// <returns>True if the keyword is valid.</returns>
        public override bool ContainsKey(
            string keyword
        ) {
            return canonicalKeywords.ContainsKey(keyword.ToLower());
        }

        /// <summary>
        /// Creates a new instance of MongoServerSettings based on the settings in this MongoConnectionStringBuilder.
        /// </summary>
        /// <returns>A new instance of MongoServerSettings.</returns>
        public MongoServerSettings ToServerSettings() {
            return new MongoServerSettings(
                connectionMode,
                connectTimeout,
                MongoCredentials.Create(username, password), // defaultCredentials
                guidRepresentation,
                ipv6,
                maxConnectionIdleTime,
                maxConnectionLifeTime,
                maxConnectionPoolSize,
                minConnectionPoolSize,
                replicaSetName,
                safeMode ?? MongoDefaults.SafeMode,
                servers,
                slaveOk,
                socketTimeout,
                ComputedWaitQueueSize, // waitQueueSize
                waitQueueTimeout
            );
        }
        #endregion

        #region private methods
        private string GetServersString() {
            var sb = new StringBuilder();
            foreach (var server in servers) {
                if (sb.Length > 0) { sb.Append(","); }
                if (server.Port == 27017) {
                    sb.Append(server.Host);
                } else {
                    sb.AppendFormat("{0}:{1}", server.Host, server.Port);
                }
            }
            return sb.ToString();
        }

        private IEnumerable<MongoServerAddress> ParseServersString(
            string value
        ) {
            var servers = new List<MongoServerAddress>();
            foreach (var server in value.Split(',')) {
                servers.Add(MongoServerAddress.Parse(server));
            }
            return servers;
        }

        private void ResetValues() {
            // set fields and not properties so base class items aren't set
        	connectionMode = ConnectionMode.Direct;
        	connectTimeout = MongoDefaults.ConnectTimeout;
        	databaseName = null;
            guidRepresentation = MongoDefaults.GuidRepresentation;
            ipv6 = false;
        	maxConnectionIdleTime = MongoDefaults.MaxConnectionIdleTime;
        	maxConnectionLifeTime = MongoDefaults.MaxConnectionLifeTime;
        	maxConnectionPoolSize = MongoDefaults.MaxConnectionPoolSize;
        	minConnectionPoolSize = MongoDefaults.MinConnectionPoolSize;
        	password = null;
        	replicaSetName = null;
        	safeMode = null;
            servers = null;
        	slaveOk = false;
        	socketTimeout = MongoDefaults.SocketTimeout;
        	username = null;
        	waitQueueMultiple = MongoDefaults.WaitQueueMultiple;
        	waitQueueSize = MongoDefaults.WaitQueueSize;
        	waitQueueTimeout = MongoDefaults.WaitQueueTimeout;
        }

        private TimeSpan ToTimeSpan(
            string keyword,
            object value
        ) {
            if (value is TimeSpan) {
                return (TimeSpan) value;
            } else if (value is string) {
                return MongoUrlBuilder.ParseTimeSpan(keyword, (string) value);
            } else {
                return TimeSpan.FromSeconds(Convert.ToDouble(value));
            }
        }
        #endregion
    }
}
