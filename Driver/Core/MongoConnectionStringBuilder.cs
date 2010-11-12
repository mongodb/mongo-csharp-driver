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
        private static string[] validKeywords = {
            "connection",
            "database",
            "fsync",
            "password",
            "replicaset",
            "safe",
            "server",
            "slaveok",
            "username",
            "w",
            "wtimeout"
        };
        #endregion

        #region private fields
        private ConnectionMode connectionMode = ConnectionMode.Direct;
        private string databaseName;
        private string password;
        private string replicaSetName;
        private SafeMode safeMode;
        private IEnumerable<MongoServerAddress> servers;
        private bool slaveOk;
        private string username;
        #endregion

        #region constructors
        public MongoConnectionStringBuilder()
            : base() {
        }

        public MongoConnectionStringBuilder(
            string connectionString
        )
            : this() {
            ConnectionString = connectionString;
        }
        #endregion

        #region public properties
        public ConnectionMode ConnectionMode {
            get {
                if (ContainsKey("connect")) {
                    return connectionMode;
                } else {
                    return (servers.Count() == 1) ? ConnectionMode.Direct : ConnectionMode.ReplicaSet;
                }
            }
            set {
                connectionMode = value;
                base["connect"] = value.ToString().ToLower();
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
                base["replicaset"] = replicaSetName = value;
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
                        base["wtimeout"] = (value.W != 0 && value.WTimeout != TimeSpan.Zero) ? ((int) value.WTimeout.TotalMilliseconds).ToString() : null;
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
            }
        }

        public bool SlaveOk {
            get {
                return slaveOk;
            }
            set {
                slaveOk = value;
                base["slaveok"] = XmlConvert.ToString(value);
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
        #endregion

        #region public indexers
        public override object this[
            string keyword
        ] {
            get {
                return base[keyword.ToLower()];
            }
            set {
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
                    case "password":
                        Password = (string) value;
                        break;
                    case "replicaset":
                        ConnectionMode = ConnectionMode.ReplicaSet;
                        ReplicaSetName = (string) value;
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
                        Servers = ParseServersString((string) value);
                        break;
                    case "slaveok":
                        if (value is string) {
                            SlaveOk = XmlConvert.ToBoolean((string) value);
                        } else {
                            SlaveOk = (bool) value;
                        }
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
                    case "wtimeout":
                        if (value is string) {
                            wtimeout = TimeSpan.FromMilliseconds(int.Parse((string) value));
                        } else {
                            wtimeout = TimeSpan.FromMilliseconds((int) value);
                        }
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
        public new void Add(
            string keyword,
            object value
        ) {
            this[keyword] = value;
        }

        public MongoUrl ToMongoUrl() {
            var builder = new MongoUrlBuilder {
                Servers = Servers,
                DatabaseName = DatabaseName,
                Credentials = MongoCredentials.Create(Username, Password),
                ConnectionMode = ConnectionMode,
                ReplicaSetName = ReplicaSetName,
                SafeMode = SafeMode,
                SlaveOk = SlaveOk
            };
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
        #endregion
    }
}
