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
    public class MongoServer : IDisposable {
        #region private fields
        private string host;
        private int port;
        private Dictionary<string, MongoDatabase> databases = new Dictionary<string, MongoDatabase>();
        #endregion

        #region constructors
        public MongoServer() :
            this("localhost", 27017) {
        }

        public MongoServer(
            string host,
            int port
        ) {
            this.host = host;
            this.port = port;
        }
        #endregion

        #region public properties
        public string Host {
            get { return host; }
            set { host = value; }
        }

        public int Port {
            get { return port; }
            set { port = value; }
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
        public void Dispose() {
        }

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
