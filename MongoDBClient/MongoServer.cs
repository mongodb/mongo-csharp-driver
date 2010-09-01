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

using MongoDB.BsonLibrary;
using MongoDB.MongoDBClient.Internal;

namespace MongoDB.MongoDBClient {
    public class MongoServer {
        #region private static fields
        private static List<MongoServer> servers = new List<MongoServer>();
        #endregion

        #region private fields
        private List<MongoServerAddress> addresses = new List<MongoServerAddress>();
        private SafeMode safeMode = SafeMode.False;
        private bool slaveOK;
        private Dictionary<string, MongoDatabase> databases = new Dictionary<string, MongoDatabase>();
        private MongoConnectionPool connectionPool;
        private MongoCredentials adminCredentials;
        #endregion

        #region constructors
        public MongoServer(
            List<MongoServerAddress> addresses
        ) {
            this.addresses = addresses;
            this.connectionPool = new MongoConnectionPool(this);
        }
        #endregion

        #region factory methods
        internal static MongoServer Create(
           IMongoConnectionSettings settings
       ) {
            MongoServer server = Create(settings.Addresses);
            return server;
        }

        public static MongoServer Create(
            List<MongoServerAddress> addresses
        ) {
            foreach (MongoServer server in servers) {
                if (server.Addresses.SequenceEqual(addresses)) {
                    return server;
                }
            }

            MongoServer newServer = new MongoServer(addresses);
            servers.Add(newServer);
            return newServer;
        }

        public static MongoServer Create(
            MongoConnectionStringBuilder builder
        ) {
            return Create(builder);
        }

        public static MongoServer Create(
            MongoUrl url
        ) {
            return Create(url);
        }

        public static MongoServer Create(
            string connectionString
        ) {
            if (connectionString.StartsWith("mongodb://")) {
                var url = new MongoUrl(connectionString);
                return Create(url);
            } else {
                MongoConnectionStringBuilder builder = new MongoConnectionStringBuilder(connectionString);
                return Create(builder);
            }
        }

        public static MongoServer Create(
            Uri uri
        ) {
            return Create(new MongoUrl(uri.ToString()));
        }
        #endregion

        #region public properties
        public IEnumerable<MongoServerAddress> Addresses {
            get { return addresses; }
        }

        public MongoCredentials AdminCredentials {
            get { return adminCredentials; }
            set { adminCredentials = value; }
        }

        public MongoDatabase AdminDatabase {
            get { return GetDatabase("admin", adminCredentials); }
        }

        public string Host {
            get { return addresses[0].Host; }
        }

        public int Port {
            get { return addresses[0].Port; }
        }

        public SafeMode SafeMode {
            get { return safeMode; }
            set { safeMode = value; }
        }

        public bool SlaveOK {
            get { return slaveOK; }
            set { slaveOK = value; }
        }
        #endregion

        #region internal properties
        internal MongoConnectionPool ConnectionPool {
            get { return connectionPool; }
        }
        #endregion

        #region public indexers
        public MongoDatabase this[
            string databaseName
        ] {
            get { return GetDatabase(databaseName); }
        }
        #endregion

        #region public methods
        public void CloneDatabase(
            string fromHost
        ) {
            throw new NotImplementedException();
        }

        // TODO: fromHost parameter?
        public void CopyDatabase(
            string from,
            string to
        ) {
            throw new NotImplementedException();
        }

        public void DropDatabase(
            string databaseName
        ) {
            MongoDatabase database = GetDatabase(databaseName);
            var command = new BsonDocument("dropDatabase", 1);
            database.RunCommand(command);
        }

        public MongoDatabase GetDatabase(
            string databaseName
        ) {
            return GetDatabase(databaseName, null);
        }

        public MongoDatabase GetDatabase(
            string databaseName,
            MongoCredentials credentials
        ) {
            string key;
            if (credentials == null) {
                key = databaseName;
            } else {
                key = string.Format("{0}[{1}]", databaseName, credentials);
            }

            MongoDatabase database;
            if (!databases.TryGetValue(key, out database)) {
                if (credentials == null) {
                    database = new MongoDatabase(this, databaseName);
                } else {
                    database = new MongoDatabase(this, databaseName, credentials);
                }
                databases[databaseName] = database;
            }
            return database;
        }

        public List<string> GetDatabaseNames() {
            var databaseNames = new List<string>();
            var result = AdminDatabase.RunCommand("listDatabases");
            var databases = (BsonDocument) result["databases"];
            foreach (BsonElement database in databases) {
                string databaseName = (string) ((BsonDocument) database.Value)["name"];
                databaseNames.Add(databaseName);
            }
            databaseNames.Sort();
            return databaseNames;
        }

        public BsonDocument RenameCollection(
            string oldCollectionName,
            string newCollectionName
        ) {
            var command = new BsonDocument {
                { "renameCollection", oldCollectionName },
                { "to", newCollectionName }
            };
            return AdminDatabase.RunCommand(command);
        }
        #endregion
    }
}
