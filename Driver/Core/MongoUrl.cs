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
        private static Dictionary<string, MongoUrl> cache = new Dictionary<string, MongoUrl>();
        #endregion

        #region private fields
        private MongoServerSettings serverSettings;
        private double waitQueueMultiple;
        private int waitQueueSize;
        private string databaseName;
        private string url;
        #endregion

        #region constructors
        public MongoUrl(
            string url
        ) {
            var builder = new MongoUrlBuilder(url); // parses url
            serverSettings = builder.ToServerSettings();
            serverSettings.Freeze();
            this.waitQueueMultiple = builder.WaitQueueMultiple;
            this.waitQueueSize = builder.WaitQueueSize;
            this.databaseName = builder.DatabaseName;
            this.url = builder.ToString(); // keep canonical form
        }
        #endregion

        #region public properties
        public int ComputedWaitQueueSize {
            get {
                if (waitQueueMultiple == 0.0) {
                    return waitQueueSize;
                } else {
                    return (int) (waitQueueMultiple * serverSettings.MaxConnectionPoolSize);
                }
            }
        }

        public ConnectionMode ConnectionMode {
            get { return serverSettings.ConnectionMode; }
        }

        public TimeSpan ConnectTimeout {
            get { return serverSettings.ConnectTimeout; }
        }

        public string DatabaseName {
            get { return databaseName; }
        }

        public MongoCredentials DefaultCredentials {
            get { return serverSettings.DefaultCredentials; }
        }

        public TimeSpan MaxConnectionIdleTime {
            get { return serverSettings.MaxConnectionIdleTime; }
        }

        public TimeSpan MaxConnectionLifeTime {
            get { return serverSettings.MaxConnectionLifeTime; }
        }

        public int MaxConnectionPoolSize {
            get { return serverSettings.MaxConnectionPoolSize; }
        }

        public int MinConnectionPoolSize {
            get { return serverSettings.MinConnectionPoolSize; }
        }

        public string ReplicaSetName {
            get { return serverSettings.ReplicaSetName; }
        }

        public SafeMode SafeMode {
            get { return serverSettings.SafeMode; }
        }

        public MongoServerAddress Server {
            get { return serverSettings.Server; }
        }

        public IEnumerable<MongoServerAddress> Servers {
            get { return serverSettings.Servers; }
        }

        public bool SlaveOk {
            get { return serverSettings.SlaveOk; }
        }

        public TimeSpan SocketTimeout {
            get { return serverSettings.SocketTimeout; }
        }

        public string Url {
            get { return url; }
        }

        public double WaitQueueMultiple {
            get { return waitQueueMultiple; }
        }

        public int WaitQueueSize {
            get { return waitQueueSize; }
        }

        public TimeSpan WaitQueueTimeout {
            get { return serverSettings.WaitQueueTimeout; }
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
        public static void ClearCache() {
            cache.Clear();
        }

        public static MongoUrl Create(
            string url
        ) {
            // cache previously seen urls to avoid repeated parsing
            lock (staticLock) {
                MongoUrl mongoUrl;
                if (!cache.TryGetValue(url, out mongoUrl)) {
                    mongoUrl = new MongoUrl(url);
                    var canonicalUrl = mongoUrl.ToString();
                    if (canonicalUrl != url) {
                        if (cache.ContainsKey(canonicalUrl)) {
                            mongoUrl = cache[canonicalUrl]; // use existing MongoUrl
                        } else {
                            cache[canonicalUrl] = mongoUrl; // cache under canonicalUrl also
                        }
                    }
                    cache[url] = mongoUrl;
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

        public MongoServerSettings ToServerSettings() {
            return serverSettings;
        }

        public override string ToString() {
            return url;
        }
        #endregion
    }
}
