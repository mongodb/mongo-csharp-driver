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
        private Dictionary<string, MongoDatabase> databases = new Dictionary<string, MongoDatabase>();
        #endregion

        #region constructors
        private MongoServer(
            List<MongoServerAddress> addresses
        ) {
            this.addresses = addresses;
        }
        #endregion

        #region factory methods
        public static MongoServer FromAddresses(
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

        public static MongoServer FromConnectionString(
            string connectionString
        ) {
            if (connectionString.StartsWith("mongodb://")) {
                var url = new MongoUrl(connectionString);
                return FromMongoUrl(url);
            } else {
                MongoConnectionStringBuilder builder = new MongoConnectionStringBuilder(connectionString);
                return FromMongoConnectionStringBuilder(builder);
            }
        }

        internal static MongoServer FromMongoConnectionSettings(
           IMongoConnectionSettings settings
       ) {
            MongoServer server = FromAddresses(settings.Addresses);
            if (settings.Username != null && settings.Password != null) {
                MongoDatabase database = server.GetDatabase(settings.Database);
                database.DefaultCredentials = new MongoCredentials(settings.Username, settings.Password);
            }
            return server;
        }

        public static MongoServer FromMongoConnectionStringBuilder(
            MongoConnectionStringBuilder builder
        ) {
            return FromMongoConnectionSettings(builder);
        }

        public static MongoServer FromMongoUrl(
            MongoUrl url
        ) {
            return FromMongoConnectionSettings(url);
        }

        public static MongoServer FromUri(
            Uri uri
        ) {
            return FromMongoUrl(new MongoUrl(uri.ToString()));
        }
        #endregion

        #region public properties
        public IEnumerable<MongoServerAddress> Addresses {
            get { return addresses; }
        }

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
        public void DropDatabase(
            string name
        ) {
            MongoDatabase database = GetDatabase(name);
            var command = new BsonDocument {
                { "dropDatabase", 1 }
            };
            database.RunCommand(command);
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

        public List<string> GetDatabaseNames() {
            var databaseNames = new List<string>();
            var adminDatabase = GetDatabase("admin");
            var result = adminDatabase.RunCommand("listDatabases");
            var databases = (BsonDocument) result["databases"];
            foreach (BsonElement database in databases) {
                string databaseName = (string) ((BsonDocument) database.Value)["name"];
                databaseNames.Add(databaseName);
            }
            databaseNames.Sort();
            return databaseNames;
        }
        #endregion
    }
}
