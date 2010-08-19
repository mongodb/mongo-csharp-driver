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

namespace MongoDB.MongoDBClient {
    public class MongoDatabase {
        #region private fields
        private MongoServer server;
        private string name;
        private MongoCredentials defaultCredentials;
        private Dictionary<string, MongoCollection> collections = new Dictionary<string, MongoCollection>();
        #endregion

        #region constructors
        public MongoDatabase(
            string connectionString
        ) {
            MongoConnectionStringBuilder csb = new MongoConnectionStringBuilder(connectionString);
            MongoConnectionStringBuilder servercsb = new MongoConnectionStringBuilder {
                Servers = csb.Servers
            };
            this.server = new MongoServer(servercsb);
            this.name = csb.Database;
            if (csb.Username != null && csb.Password != null) {
                defaultCredentials = new MongoCredentials(csb.Username, csb.Password);
            }
        }

        internal MongoDatabase(
            MongoServer server,
            string name
        ) {
            this.server = server;
            this.name = name;
        }
        #endregion

        #region public properties
        public MongoServer Server {
            get { return server; }
        }

        public MongoCredentials DefaultCredentials {
            get { return defaultCredentials; }
            set { defaultCredentials = value; }
        }
        #endregion

        #region public indexers
        public MongoCollection this[
            string name
        ] {
            get { return GetCollection(name); }
        }
        #endregion

        #region public properties
        public string Name {
            get { return name; }
        }
        #endregion

        #region public methods
        public MongoCollection GetCollection(
           string name
       ) {
            MongoCollection collection;
            if (!collections.TryGetValue(name, out collection)) {
                collection = new MongoCollection(this, name);
                collections[name] = collection;
            }
            return collection;
        }

        public MongoCollection<T> GetCollection<T>(
            string name
        ) where T : new() {
            MongoCollection collection;
            string key = string.Format("{0}<{1}>", name, typeof(T).FullName);
            if (!collections.TryGetValue(key, out collection)) {
                collection = new MongoCollection<T>(this, name);
                collections[name] = collection;
            }
            return (MongoCollection<T>) collection;
        }
        #endregion
    }
}
