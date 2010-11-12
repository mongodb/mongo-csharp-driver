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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using MongoDB.Driver.Internal;

namespace MongoDB.Driver {
    [Serializable]
    public enum ConnectionMode {
        Direct,
        ReplicaSet
    }

    [Serializable]
    public class MongoUrl {
        #region private static fields
        private static object staticLock = new object();
        private static Dictionary<string, MongoUrl> mongoUrls = new Dictionary<string, MongoUrl>();
        #endregion

        #region private fields
        private ConnectionMode connectionMode;
        private MongoCredentials credentials;
        private string databaseName;
        private string replicaSetName;
        private SafeMode safeMode;
        private IEnumerable<MongoServerAddress> servers;
        private bool slaveOk;
        private string url;
        #endregion

        #region constructors
        public MongoUrl(
            string url
        ) {
            var builder = new MongoUrlBuilder(url);
            this.url = builder.ToString(); // keep canonical form
            this.credentials = builder.Credentials;
            this.servers = builder.Servers;
            this.databaseName = builder.DatabaseName;
            this.connectionMode = builder.ConnectionMode;
            this.replicaSetName = builder.ReplicaSetName;
            this.safeMode = builder.SafeMode ?? SafeMode.False; // never null
            this.slaveOk = builder.SlaveOk;
        }
        #endregion

        #region public properties
        public ConnectionMode ConnectionMode {
            get { return connectionMode; }
        }

        public MongoCredentials Credentials {
            get { return credentials; }
        }

        public string DatabaseName {
            get { return databaseName; }
        }

        public string ReplicaSetName {
            get { return replicaSetName; }
        }

        public SafeMode SafeMode {
            get { return safeMode; }
        }

        public MongoServerAddress Server {
            get { return (servers == null) ? null : servers.Single(); }
        }

        public IEnumerable<MongoServerAddress> Servers {
            get { return servers; }
        }

        public bool SlaveOk {
            get { return slaveOk; }
        }

        public string Url {
            get { return url; }
        }
        #endregion

        #region public operators
        public static bool operator ==(
            MongoUrl lhs,
            MongoUrl rhs
        ) {
            return object.Equals(lhs, rhs);
        }

        public static bool operator !=(
            MongoUrl lhs,
            MongoUrl rhs
        ) {
            return !(lhs == rhs);
        }
        #endregion

        #region public static methods
        public static MongoUrl Create(
            string url
        ) {
            lock (staticLock) {
                MongoUrl mongoUrl;
                if (!mongoUrls.TryGetValue(url, out mongoUrl)) {
                    mongoUrl = new MongoUrl(url);
                    var canonicalUrl = mongoUrl.ToString();
                    if (canonicalUrl != url) {
                        if (mongoUrls.ContainsKey(canonicalUrl)) {
                            mongoUrl = mongoUrls[canonicalUrl]; // use existing MongoUrl
                        } else {
                            mongoUrls[canonicalUrl] = mongoUrl; // cache under canonicalUrl also
                        }
                    }
                    mongoUrls[url] = mongoUrl;
                }
                return mongoUrl;
            }
        }
        #endregion

        #region public methods
        public bool Equals(
            MongoUrl rhs
        ) {
            // this works because URL is in canonical form
            return this.url == rhs.url;
        }

        public override bool Equals(
            object obj
        ) {
            return Equals(obj as MongoUrl);
        }

        public override int GetHashCode() {
            // this works because URL is in canonical form
            return url.GetHashCode();
        }

        public override string ToString() {
            return url;
        }
        #endregion
    }
}
