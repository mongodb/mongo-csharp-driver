/* Copyright 2010 10gen Inc.
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

using MongoDB.Driver.Internal;

namespace MongoDB.Driver {
    public class MongoConnectionStringBuilder : DbConnectionStringBuilder {
        #region private static fields
        private static Dictionary<string, string> canonicalKeywords = new Dictionary<string, string> {
            { "connect", "connect" },
            { "connecttimeout", "connectTimeout" },
            { "connecttimeoutms", "connectTimeoutMS" },
            { "database", "database" },
            { "fsync", "fsync" },
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
        };
        #endregion

        #region private fields
        private ConnectionMode connectionMode;
        private TimeSpan connectTimeout;
        private string databaseName;
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
        public MongoConnectionStringBuilder()
            : base() {
            ResetValues();
        }

        public MongoConnectionStringBuilder(
            string connectionString
        ) {
            Clear(); // not sure if base class calls Clear or not
            ConnectionString = connectionString;
        }
        #endregion

        #region public properties
        public ConnectionMode ConnectionMode {
            get {
                return connectionMode;
            }
            set {
                connectionMode = value;
                base["connect"] = MongoUtils.ToCamelCase(value.ToString());
            }
        }

        public TimeSpan ConnectTimeout {
            get {
                return connectTimeout;
            }
            set {
                connectTimeout = value;
                base["connect"] = MongoUrlBuilder.FormatTimeSpan(value);
            }
        }

        public string DatabaseName {
            get {
                return databaseName;
            }
            set {
                base["database"] = databaseName = value;
            }
        }

        public TimeSpan MaxConnectionIdleTime {
            get {
                return maxConnectionIdleTime;
            }
            set {
                maxConnectionIdleTime = value;
                base["maxIdleTime"] = MongoUrlBuilder.FormatTimeSpan(value);
            }
        }

        public TimeSpan MaxConnectionLifeTime {
            get {
                return maxConnectionLifeTime;
            }
            set {
                maxConnectionLifeTime = value;
                base["maxLifeTime"] = MongoUrlBuilder.FormatTimeSpan(value);
            }
        }

        public int MaxConnectionPoolSize {
            get {
                return maxConnectionPoolSize;
            }
            set {
                maxConnectionPoolSize = value;
                base["maxPoolSize"] = XmlConvert.ToString(value);
            }
        }

        public int MinConnectionPoolSize {
            get {
                return minConnectionPoolSize;
            }
            set {
                minConnectionPoolSize = value;
                base["minPoolSize"] = XmlConvert.ToString(value);
            }
        }

        public string Password {
            get {
                return password;
            }
            set {
                base["password"] = password = value;
            }
        }

        public string ReplicaSetName {
            get {
                return replicaSetName;
            }
            set {
                ConnectionMode = ConnectionMode.ReplicaSet;
                base["replicaSet"] = replicaSetName = value;
            }
        }

        public SafeMode SafeMode {
            get {
                return safeMode;
            }
            set {
                safeMode = value;
                if (value == null) {
                    base["safe"] = null;
                    base["w"] = null;
                    base["wtimeout"] = null;
                    base["fsync"] = null;
                } else {
                    if (value.Enabled) {
                        base["safe"] = "true";
                        base["w"] = (value.W != 0) ? value.W.ToString() : null;
                        base["wtimeout"] = (value.W != 0 && value.WTimeout != TimeSpan.Zero) ? MongoUrlBuilder.FormatTimeSpan(value.WTimeout) : null;
                        base["fsync"] = (value.FSync) ? "true" : null;
                    } else {
                        base["safe"] = "false";
                        base["w"] = null;
                        base["wtimeout"] = null;
                        base["fsync"] = null;
                    }
                }
            }
        }

        public MongoServerAddress Server {
            get {
                return (servers == null) ? null : servers.Single();
            }
            set {
                Servers = new[] { value };
            }
        }

        public IEnumerable<MongoServerAddress> Servers {
            get {
                return servers;
            }
            set {
                servers = value;
                base["server"] = GetServersString();
                connectionMode = (value.Count() == 1) ? ConnectionMode.Direct : ConnectionMode.ReplicaSet; // assign to field not to property
            }
        }

        public bool SlaveOk {
            get {
                return slaveOk;
            }
            set {
                slaveOk = value;
                base["slaveOk"] = XmlConvert.ToString(value);
            }
        }

        public TimeSpan SocketTimeout {
            get {
                return socketTimeout;
            }
            set {
                socketTimeout = value;
                base["socketTimeout"] = MongoUrlBuilder.FormatTimeSpan(value);
            }
        }

        public string Username {
            get {
                return username;
            }
            set {
                base["username"] = username = value;
            }
        }

        public double WaitQueueMultiple {
            get {
                return waitQueueMultiple;
            }
            set {
                waitQueueMultiple = value;
                base["waitQueueMultiple"] = XmlConvert.ToString(value);
            }
        }

        public int WaitQueueSize {
            get {
                return waitQueueSize;
            }
            set {
                waitQueueSize = value;
                base["waitQueueSize"] = XmlConvert.ToString(value);
            }
        }

        public TimeSpan WaitQueueTimeout {
            get {
                return waitQueueTimeout;
            }
            set {
                waitQueueTimeout = value;
                base["waitQueueTimeout"] = MongoUrlBuilder.FormatTimeSpan(value);
            }
        }
        #endregion

        #region public indexers
        public override object this[
            string keyword
        ] {
            get {
                if (keyword == null) { throw new ArgumentNullException("keyword"); }
                return base[canonicalKeywords[keyword.ToLower()]];
            }
            set {
                if (keyword == null) { throw new ArgumentNullException("keyword"); }
                bool fsync;
                int w;
                TimeSpan wtimeout;
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
                        if (value is string) {
                            fsync = XmlConvert.ToBoolean((string) value);
                        } else {
                            fsync = (bool) value;
                        }
                        w = (safeMode != null) ? safeMode.W : 0;
                        wtimeout = (safeMode != null) ? safeMode.WTimeout : TimeSpan.Zero;
                        SafeMode = SafeMode.Create(true, fsync, w, wtimeout);
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
                        if (value is string) {
                            MaxConnectionPoolSize = int.Parse((string) value);
                        } else {
                            MaxConnectionPoolSize = (int) value;
                        }
                        break;
                    case "minpoolsize":
                        if (value is string) {
                            MinConnectionPoolSize = int.Parse((string) value);
                        } else {
                            MinConnectionPoolSize = (int) value;
                        }
                        break;
                    case "password":
                        Password = (string) value;
                        break;
                    case "replicaset":
                        ReplicaSetName = (string) value;
                        ConnectionMode = ConnectionMode.ReplicaSet;
                        break;
                    case "safe":
                        bool enabled;
                        if (value is string) {
                            enabled = XmlConvert.ToBoolean((string) value);
                        } else {
                            enabled = (bool) value;
                        }
                        if (enabled) {
                            fsync = (safeMode == null) ? false : safeMode.FSync;
                            w = (safeMode == null) ? 0 : safeMode.W;
                            wtimeout = (w == 0 || safeMode == null) ? TimeSpan.Zero : safeMode.WTimeout;
                            SafeMode = SafeMode.Create(true, fsync, w, wtimeout);
                        } else {
                            SafeMode = SafeMode.False;
                        }
                        break;
                    case "server":
                    case "servers":
                        Servers = ParseServersString((string) value);
                        break;
                    case "slaveok":
                        if (value is string) {
                            SlaveOk = XmlConvert.ToBoolean((string) value);
                        } else {
                            SlaveOk = (bool) value;
                        }
                        break;
                    case "sockettimeout":
                    case "sockettimeoutms":
                        SocketTimeout = ToTimeSpan(keyword, value);
                        break;
                    case "username":
                        Username = (string) value;
                        break;
                    case "w":
                        if (value is string) {
                            w = int.Parse((string) value);
                        } else {
                            w = (int) value;
                        }
                        fsync = (safeMode == null) ? false : safeMode.FSync;
                        wtimeout = (w == 0 || safeMode == null) ? TimeSpan.Zero : safeMode.WTimeout;
                        SafeMode = SafeMode.Create(true, fsync, w, wtimeout);
                        break;
                    case "waitqueuemultiple":
                        if (value is string) {
                            WaitQueueMultiple = double.Parse((string) value);
                        } else if (value is int) {
                            WaitQueueMultiple = (int) value;
                        } else {
                            WaitQueueMultiple = (double) value;
                        }
                        break;
                    case "waitqueuesize":
                        if (value is string) {
                            WaitQueueSize = int.Parse((string) value);
                        } else {
                            WaitQueueSize = (int) value;
                        }
                        break;
                    case "waitqueuetimeout":
                    case "waitqueuetimeoutms":
                        WaitQueueTimeout = ToTimeSpan(keyword, value);
                        break;
                    case "wtimeout":
                        wtimeout = ToTimeSpan(keyword, value);
                        fsync = (safeMode == null) ? false : safeMode.FSync;
                        w = (safeMode == null) ? 0 : safeMode.W;
                        SafeMode = SafeMode.Create(true, fsync, w, wtimeout);
                        break;
                    default:
                        var message = string.Format("Invalid keyword: {0}", keyword);
                        throw new ArgumentException(message);
                }
            }
        }
        #endregion

        #region public methods
        public override void Clear() {
            base.Clear();
            ResetValues();
        }

        public override bool ContainsKey(
            string keyword
        ) {
            return canonicalKeywords.ContainsKey(keyword.ToLower());
        }

        public MongoUrl ToMongoUrl() {
            var builder = new MongoUrlBuilder {
                Credentials = MongoCredentials.Create(username, password),
                Servers = servers,
                DatabaseName = databaseName,
                ConnectionMode = connectionMode,
                ConnectTimeout = connectTimeout,
                MaxConnectionIdleTime = maxConnectionIdleTime,
                MaxConnectionLifeTime = maxConnectionLifeTime,
                MaxConnectionPoolSize = maxConnectionPoolSize,
                MinConnectionPoolSize = minConnectionPoolSize,
                ReplicaSetName = ReplicaSetName,
                SafeMode = SafeMode,
                SlaveOk = SlaveOk,
                SocketTimeout = socketTimeout,
                WaitQueueTimeout = waitQueueTimeout
            };
            if (waitQueueMultiple != 0) {
                builder.WaitQueueMultiple = waitQueueMultiple;
            } else {
                builder.WaitQueueSize = waitQueueSize;
            }
            return builder.ToMongoUrl();
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
        	connectionMode = ConnectionMode.Direct;
        	connectTimeout = MongoDefaults.ConnectTimeout;
        	databaseName = null;
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
            if (value is int) {
                return TimeSpan.FromSeconds((int) value);
            } else if (value is string) {
                return MongoUrlBuilder.ParseTimeSpan(keyword, (string) value);
            } else {
                return (TimeSpan) value;
            }
        }
        #endregion
    }
}
