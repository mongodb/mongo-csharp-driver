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
    public class MongoServer {
        #region private fields
        private List<MongoServerAddress> addresses = new List<MongoServerAddress>();
        private Dictionary<string, MongoDatabase> databases = new Dictionary<string, MongoDatabase>();
        #endregion

        #region constructors
        public MongoServer(
            string connectionString
        )
            : this(new MongoConnectionStringBuilder(connectionString)) {
        }

        public MongoServer(
            MongoConnectionStringBuilder csb
        ) {
            addresses = csb.Servers;
            if (csb.Database != null) {
                MongoDatabase database = new MongoDatabase(this, csb.Database);
                if (csb.Username != null && csb.Password != null) {
                    database.DefaultCredentials = new MongoCredentials(csb.Username, csb.Password);
                }
                databases[database.Name] = database;
            }
        }
        #endregion

        #region public properties
        public string Host {
            get { return addresses[0].Host; }
        }

        public int Port {
            get { return addresses[0].Port; }
        }
        #endregion

        #region public indexers
        public MongoDatabase this[
            string name
        ] {
            get { return GetDatabase(name); }
        }
        #endregion

        #region public methods
        public MongoDatabase GetDatabase(
            string name
        ) {
            MongoDatabase database;
            if (!databases.TryGetValue(name, out database)) {
                database = new MongoDatabase(this, name);
                databases[name] = database;
            }
            return database;
        }
        #endregion
    }
}
