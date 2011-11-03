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
using System.Text.RegularExpressions;
using System.Xml;

using MongoDB.Bson;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver {
    /// <summary>
    /// Represents URL style connection strings. This is the recommended connection string style, but see also
    /// MongoConnectionStringBuilder if you wish to use .NET style connection strings.
    /// </summary>
    [Serializable]
    public class MongoUrlBuilder {
        #region private fields
        // default values are set in ResetValues
        private ConnectionMode connectionMode;
        private TimeSpan connectTimeout;
        private string databaseName;
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
        private double waitQueueMultiple;
        private int waitQueueSize;
        private TimeSpan waitQueueTimeout;
        #endregion

        #region constructors
        /// <summary>
        /// Creates a new instance of MongoUrlBuilder.
        /// </summary>
        public MongoUrlBuilder() {
            ResetValues();
        }

        /// <summary>
        /// Creates a new instance of MongoUrlBuilder.
        /// </summary>
        /// <param name="url">The initial settings.</param>
        public MongoUrlBuilder(
            string url
        ) {
            Parse(url); // Parse calls ResetValues
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
            set { connectionMode = value; }
        }

        /// <summary>
        /// Gets or sets the connect timeout.
        /// </summary>
        public TimeSpan ConnectTimeout {
            get { return connectTimeout; }
            set { connectTimeout = value; }
        }

        /// <summary>
        /// Gets or sets the optional database name.
        /// </summary>
        public string DatabaseName {
            get { return databaseName; }
            set { databaseName = value; }
        }

        /// <summary>
        /// Gets or sets the default credentials.
        /// </summary>
        public MongoCredentials DefaultCredentials {
            get { return defaultCredentials; }
            set { defaultCredentials = value; }
        }

        /// <summary>
        /// Gets or sets the representation to use for Guids.
        /// </summary>
        public GuidRepresentation GuidRepresentation {
            get { return guidRepresentation; }
            set { guidRepresentation = value; }
        }

        /// <summary>
        /// Gets or sets whether to use IPv6.
        /// </summary>
        public bool IPv6 {
            get { return ipv6; }
            set { ipv6 = value; }
        }

        /// <summary>
        /// Gets or sets the max connection idle time.
        /// </summary>
        public TimeSpan MaxConnectionIdleTime {
            get { return maxConnectionIdleTime; }
            set { maxConnectionIdleTime = value; }
        }

        /// <summary>
        /// Gets or sets the max connection life time.
        /// </summary>
        public TimeSpan MaxConnectionLifeTime {
            get { return maxConnectionLifeTime; }
            set { maxConnectionLifeTime = value; }
        }

        /// <summary>
        /// Gets or sets the max connection pool size.
        /// </summary>
        public int MaxConnectionPoolSize {
            get { return maxConnectionPoolSize; }
            set { maxConnectionPoolSize = value; }
        }

        /// <summary>
        /// Gets or sets the min connection pool size.
        /// </summary>
        public int MinConnectionPoolSize {
            get { return minConnectionPoolSize; }
            set { minConnectionPoolSize = value; }
        }

        /// <summary>
        /// Gets or sets the name of the replica set.
        /// </summary>
        public string ReplicaSetName {
            get { return replicaSetName; }
            set {
                replicaSetName = value;
                connectionMode = ConnectionMode.ReplicaSet;
            }
        }

        /// <summary>
        /// Gets or sets the SafeMode to use.
        /// </summary>
        public SafeMode SafeMode {
            get { return safeMode; }
            set { safeMode = value; }
        }

        /// <summary>
        /// Gets or sets the address of the server (see also Servers if using more than one address).
        /// </summary>
        public MongoServerAddress Server {
            get { return (servers == null) ? null : servers.Single(); }
            set { servers = new MongoServerAddress[] { value }; }
        }

        /// <summary>
        /// Gets or sets the list of server addresses (see also Server if using only one address).
        /// </summary>
        public IEnumerable<MongoServerAddress> Servers {
            get { return servers; }
            set {
                servers = value;
                connectionMode = (servers.Count() <= 1) ? ConnectionMode.Direct : ConnectionMode.ReplicaSet;
            }
        }

        /// <summary>
        /// Gets or sets whether queries should be sent to secondary servers.
        /// </summary>
        public bool SlaveOk {
            get { return slaveOk; }
            set { slaveOk = value; }
        }

        /// <summary>
        /// Gets or sets the socket timeout.
        /// </summary>
        public TimeSpan SocketTimeout {
            get { return socketTimeout; }
            set { socketTimeout = value; }
        }

        /// <summary>
        /// Gets or sets the wait queue multiple (the actual wait queue size will be WaitQueueMultiple x MaxConnectionPoolSize).
        /// </summary>
        public double WaitQueueMultiple {
            get { return waitQueueMultiple; }
            set {
                waitQueueMultiple = value;
                waitQueueSize = 0;
            }
        }

        /// <summary>
        /// Gets or sets the wait queue size.
        /// </summary>
        public int WaitQueueSize {
            get { return waitQueueSize; }
            set {
                waitQueueMultiple = 0;
                waitQueueSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the wait queue timeout.
        /// </summary>
        public TimeSpan WaitQueueTimeout {
            get { return waitQueueTimeout; }
            set { waitQueueTimeout = value; }
        }
        #endregion

        #region internal static methods
        // these helper methods are shared with MongoConnectionStringBuilder
        internal static string FormatTimeSpan(
            TimeSpan value
        ) {
            const int oneSecond = 1000; // milliseconds
            const int oneMinute = 60 * oneSecond;
            const int oneHour = 60 * oneMinute;

            var ms = (int) value.TotalMilliseconds;
            if ((ms % oneHour) == 0) {
                return string.Format("{0}h", ms / oneHour);
            } else if ((ms % oneMinute) == 0) {
                return string.Format("{0}m", ms / oneMinute);
            } else if ((ms % oneSecond) == 0) {
                return string.Format("{0}s", ms / oneSecond);
            } else {
                return string.Format("{0}ms", ms);
            }
        }

        internal static ConnectionMode ParseConnectionMode(
            string name,
            string s
        ) {
            try {
                return (ConnectionMode) Enum.Parse(typeof(ConnectionMode), s, true); // ignoreCase
            } catch (ArgumentException) {
                throw new FormatException(FormatMessage(name, s));
            }
        }

        internal static bool ParseBoolean(
            string name,
            string s
        ) {
            try {
                return XmlConvert.ToBoolean(s.ToLower());
            } catch (FormatException) {
                throw new FormatException(FormatMessage(name, s));
            }
        }

        internal static double ParseDouble(
            string name,
            string s
        ) {
            try {
                return XmlConvert.ToDouble(s);
            } catch (FormatException) {
                throw new FormatException(FormatMessage(name, s));
            }
        }

        internal static int ParseInt32(
            string name,
            string s
        ) {
            try {
                return XmlConvert.ToInt32(s);
            } catch (FormatException) {
                throw new FormatException(FormatMessage(name, s));
            }
        }

        internal static TimeSpan ParseTimeSpan(
            string name,
            string s
        ) {
            TimeSpan result;
            if (TryParseTimeSpan(name, s, out result)) {
                return result;
            } else {
                throw new FormatException(FormatMessage(name, s));
            }
        }

        internal static bool TryParseTimeSpan(
            string name,
            string s,
            out TimeSpan result
        ) {
            if (name != null && s != null) {
                name = name.ToLower();
                s = s.ToLower();

                var multiplier = 1000;
                if (name.EndsWith("ms")) {
                    multiplier = 1;
                } else if (s.EndsWith("ms")) {
                    s = s.Substring(0, s.Length - 2);
                    multiplier = 1;
                } else if (s.EndsWith("s")) {
                    s = s.Substring(0, s.Length - 1);
                    multiplier = 1000;
                } else if (s.EndsWith("m")) {
                    s = s.Substring(0, s.Length - 1);
                    multiplier = 60 * 1000;
                } else if (s.EndsWith("h")) {
                    s = s.Substring(0, s.Length - 1);
                    multiplier = 60 * 60 * 1000;
                } else if (s.Contains(":")) {
                    return TimeSpan.TryParse(s, out result);
                }

                try {
                    result = TimeSpan.FromMilliseconds(multiplier * XmlConvert.ToDouble(s));
                    return true;
                } catch (FormatException) {
                    result = default(TimeSpan);
                    return false;
                }
            }

            result = default(TimeSpan);
            return false;
        }
        #endregion

        #region private static methods
        private static string FormatMessage(
            string name,
            string value
        ) {
            return string.Format("Invalid key value pair in connection string. {0}='{1}'.", name, value);
        }
        #endregion

        #region public methods
        /// <summary>
        /// Parses a URL and sets all settings to match the URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        public void Parse(
            string url
        ) {
            ResetValues();
            const string serverPattern = @"((\[[^]]+?\]|[^:,/]+)(:\d+)?)";
            const string pattern =
                @"^mongodb://" +
                @"((?<username>[^:]+):(?<password>[^@]+)@)?" +
                @"(?<servers>" + serverPattern + "(," + serverPattern + ")*)" +
                @"(/(?<database>[^?]+)?(\?(?<query>.*))?)?$";
            Match match = Regex.Match(url, pattern);
            if (match.Success) {
                string username = Uri.UnescapeDataString(match.Groups["username"].Value);
                string password = Uri.UnescapeDataString(match.Groups["password"].Value);
                string servers = match.Groups["servers"].Value;
                string databaseName = match.Groups["database"].Value;
                string query = match.Groups["query"].Value;

                if (username != "" && password != "") {
                    this.defaultCredentials = new MongoCredentials(username, password);
                } else {
                    this.defaultCredentials = null;
                }

                if (servers != "") {
                    List<MongoServerAddress> addresses = new List<MongoServerAddress>();
                    foreach (string server in servers.Split(',')) {
                        var address = MongoServerAddress.Parse(server);
                        addresses.Add(address);
                    }
                    if (addresses.Count == 1) {
                        this.connectionMode = ConnectionMode.Direct;
                    } else if (addresses.Count > 1) {
                        this.connectionMode = ConnectionMode.ReplicaSet;
                    }
                    this.servers = addresses;
                } else {
                    throw new FormatException("Invalid connection string. Server missing.");
                }

                this.databaseName = (databaseName != "") ? databaseName : null;

                if (!string.IsNullOrEmpty(query)) {
                    foreach (var pair in query.Split('&', ';')) {
                        var parts = pair.Split('=');
                        if (parts.Length != 2) {
                            throw new FormatException(string.Format("Invalid connection string '{0}'.", parts));
                        }
                        var name = parts[0];
                        var value = parts[1];

                        switch (name.ToLower()) {
                            case "connect":
                                connectionMode = ParseConnectionMode(name, value);
                                break;
                            case "connecttimeout":
                            case "connecttimeoutms":
                                connectTimeout = ParseTimeSpan(name, value);
                                break;
                            case "fsync":
                                if (safeMode == null) { safeMode = new SafeMode(false); }
                                safeMode.FSync = ParseBoolean(name, value);
                                break;
                            case "guids":
                                guidRepresentation = (GuidRepresentation) Enum.Parse(typeof(GuidRepresentation), value, true); // ignoreCase
                                break;
                            case "ipv6":
                                ipv6 = ParseBoolean(name, value);
                                break;
                            case "j":
                                if (safeMode == null) { safeMode = new SafeMode(false); }
                                SafeMode.J = ParseBoolean(name, value);
                                break;
                            case "maxidletime":
                            case "maxidletimems":
                                maxConnectionIdleTime = ParseTimeSpan(name, value);
                                break;
                            case "maxlifetime":
                            case "maxlifetimems":
                                maxConnectionLifeTime = ParseTimeSpan(name, value);
                                break;
                            case "maxpoolsize":
                                maxConnectionPoolSize = ParseInt32(name, value);
                                break;
                            case "minpoolsize":
                                minConnectionPoolSize = ParseInt32(name, value);
                                break;
                            case "replicaset":
                                this.replicaSetName = value;
                                this.connectionMode = ConnectionMode.ReplicaSet;
                                break;
                            case "safe":
                                if (safeMode == null) { safeMode = new SafeMode(false); }
                                SafeMode.Enabled = ParseBoolean(name, value);
                                break;
                            case "slaveok":
                                slaveOk = ParseBoolean(name, value);
                                break;
                            case "sockettimeout":
                            case "sockettimeoutms":
                                socketTimeout = ParseTimeSpan(name, value);
                                break;
                            case "w":
                                if (safeMode == null) { safeMode = new SafeMode(false); }
                                try {
                                    SafeMode.W = ParseInt32(name, value);
                                } catch (FormatException) {
                                    SafeMode.WMode = value;
                                }
                                break;
                            case "waitqueuemultiple":
                                waitQueueMultiple = ParseDouble(name, value);
                                waitQueueSize = 0;
                                break;
                            case "waitqueuesize":
                                waitQueueMultiple = 0;
                                waitQueueSize = ParseInt32(name, value);
                                break;
                            case "waitqueuetimeout":
                            case "waitqueuetimeoutms":
                                waitQueueTimeout = ParseTimeSpan(name, value);
                                break;
                            case "wtimeout":
                            case "wtimeoutms":
                                if (safeMode == null) { safeMode = new SafeMode(false); }
                                SafeMode.WTimeout = ParseTimeSpan(name, value);
                                break;
                        }
                    }
                }
            } else {
                throw new FormatException(string.Format("Invalid connection string '{0}'.", url));
            }
        }

        /// <summary>
        /// Creates a new instance of MongoUrl based on the settings in this MongoUrlBuilder.
        /// </summary>
        /// <returns>A new instance of MongoUrl.</returns>
        public MongoUrl ToMongoUrl() {
            return MongoUrl.Create(ToString());
        }

        /// <summary>
        /// Creates a new instance of MongoServerSettings based on the settings in this MongoUrlBuilder.
        /// </summary>
        /// <returns>A new instance of MongoServerSettings.</returns>
        public MongoServerSettings ToServerSettings() {
            return new MongoServerSettings(
                connectionMode,
                connectTimeout,
                defaultCredentials,
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

        /// <summary>
        /// Returns the canonical URL based on the settings in this MongoUrlBuilder.
        /// </summary>
        /// <returns>The canonical URL.</returns>
        public override string ToString() {
            StringBuilder url = new StringBuilder();
            url.Append("mongodb://");
            if (defaultCredentials != null) {
                url.AppendFormat("{0}:{1}@", Uri.EscapeDataString(defaultCredentials.Username), Uri.EscapeDataString(defaultCredentials.Password));
            }
            if (servers != null) {
                bool firstServer = true;
                foreach (MongoServerAddress server in servers) {
                    if (!firstServer) { url.Append(","); }
                    if (server.Port == 27017) {
                        url.Append(server.Host);
                    } else {
                        url.AppendFormat("{0}:{1}", server.Host, server.Port);
                    }
                    firstServer = false;
                }
            }
            if (databaseName != null) {
                url.Append("/");
                url.Append(databaseName);
            }
            var query = new StringBuilder();
            if (ipv6) {
                query.AppendFormat("ipv6=true;");
            }
            if (
                connectionMode == ConnectionMode.Direct && servers != null && servers.Count() != 1 ||
                connectionMode == ConnectionMode.ReplicaSet && (servers == null || servers.Count() == 1)
            ) {
                query.AppendFormat("connect={0};", MongoUtils.ToCamelCase(connectionMode.ToString()));
            }
            if (!string.IsNullOrEmpty(replicaSetName)) {
                query.AppendFormat("replicaSet={0};", replicaSetName);
            }
            if (slaveOk) {
                query.AppendFormat("slaveOk=true;");
            }
            if (safeMode != null && safeMode.Enabled) {
                query.AppendFormat("safe=true;");
                if (safeMode.FSync) {
                    query.Append("fsync=true;");
                }
                if (safeMode.J) {
                    query.Append("j=true;");
                }
                if (safeMode.W != 0 || safeMode.WMode != null) {
                    if (safeMode.W != 0) {
                        query.AppendFormat("w={0};", safeMode.W);
                    } else {
                        query.AppendFormat("w={0};", safeMode.WMode);
                    }
                    if (safeMode.WTimeout != TimeSpan.Zero) {
                        query.AppendFormat("wtimeout={0};", FormatTimeSpan(safeMode.WTimeout));
                    }
                }
            }
            if (connectTimeout != MongoDefaults.ConnectTimeout) {
                query.AppendFormat("connectTimeout={0};", FormatTimeSpan(connectTimeout));
            }
            if (maxConnectionIdleTime != MongoDefaults.MaxConnectionIdleTime) {
                query.AppendFormat("maxIdleTime={0};", FormatTimeSpan(maxConnectionIdleTime));
            }
            if (maxConnectionLifeTime != MongoDefaults.MaxConnectionLifeTime) {
                query.AppendFormat("maxLifeTime={0};", FormatTimeSpan(maxConnectionLifeTime));
            }
            if (maxConnectionPoolSize != MongoDefaults.MaxConnectionPoolSize) {
                query.AppendFormat("maxPoolSize={0};", maxConnectionPoolSize);
            }
            if (minConnectionPoolSize != MongoDefaults.MinConnectionPoolSize) {
                query.AppendFormat("minPoolSize={0};", minConnectionPoolSize);
            }
            if (socketTimeout != MongoDefaults.SocketTimeout) {
                query.AppendFormat("socketTimeout={0};", FormatTimeSpan(socketTimeout));
            }
            if (waitQueueMultiple != 00 && waitQueueMultiple != MongoDefaults.WaitQueueMultiple) {
                query.AppendFormat("waitQueueMultiple={0};", waitQueueMultiple);
            }
            if (waitQueueSize != 0 && waitQueueSize != MongoDefaults.WaitQueueSize) {
                query.AppendFormat("waitQueueSize={0};", waitQueueSize);
            }
            if (waitQueueTimeout != MongoDefaults.WaitQueueTimeout) {
                query.AppendFormat("waitQueueTimeout={0};", FormatTimeSpan(WaitQueueTimeout));
            }
            if (guidRepresentation != MongoDefaults.GuidRepresentation) {
                query.AppendFormat("guids={0};", guidRepresentation);
            }
            if (query.Length != 0) {
                query.Length = query.Length - 1; // remove trailing ";"
                if (databaseName == null) {
                    url.Append("/");
                }
                url.Append("?");
                url.Append(query.ToString());
            }
            return url.ToString();
        }
        #endregion

        #region private methods
        private void ResetValues() {
            connectionMode = ConnectionMode.Direct;
            connectTimeout = MongoDefaults.ConnectTimeout;
            databaseName = null;
            defaultCredentials = null;
            guidRepresentation = MongoDefaults.GuidRepresentation;
            ipv6 = false;
            maxConnectionIdleTime = MongoDefaults.MaxConnectionIdleTime;
            maxConnectionLifeTime = MongoDefaults.MaxConnectionLifeTime;
            maxConnectionPoolSize = MongoDefaults.MaxConnectionPoolSize;
            minConnectionPoolSize = MongoDefaults.MinConnectionPoolSize;
            replicaSetName = null;
            safeMode = null;
            servers = null;
            slaveOk = false;
            socketTimeout = MongoDefaults.SocketTimeout;
            waitQueueMultiple = MongoDefaults.WaitQueueMultiple;
            waitQueueSize = MongoDefaults.WaitQueueSize;
            waitQueueTimeout = MongoDefaults.WaitQueueTimeout;
        }
        #endregion
    }
}
