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
using System.Threading;

using MongoDB.Bson;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver {
    public class MongoServer {
        #region private static fields
        private static object staticLock = new object();
        private static Dictionary<MongoUrl, MongoServer> servers = new Dictionary<MongoUrl, MongoServer>();
        #endregion

        #region private fields
        private object serverLock = new object();
        private MongoServerState state = MongoServerState.Disconnected;
        private IEnumerable<MongoServerAddress> replicaSet;
        private Dictionary<string, MongoDatabase> databases = new Dictionary<string, MongoDatabase>();
        private MongoConnectionPool connectionPool;
        private MongoCredentials adminCredentials;
        private MongoCredentials defaultCredentials;
        private MongoUrl url;
        #endregion

        #region constructors
        public MongoServer(
            MongoUrl url
        ) {
            this.url = url;

            // credentials (if any) are for server only if no DatabaseName was provided
            if (url.Credentials != null && url.DatabaseName == null) {
                if (url.Credentials.Admin) {
                    this.adminCredentials = url.Credentials;
                } else {
                    this.defaultCredentials = url.Credentials;
                }
            }
        }
        #endregion

        #region factory methods
        public static MongoServer Create() {
            return Create("mongodb://localhost");
        }

        public static MongoServer Create(
            MongoConnectionStringBuilder builder
        ) {
            return Create(builder.ToMongoUrl());
        }

        public static MongoServer Create(
            MongoUrl url
        ) {
            lock (staticLock) {
                MongoServer server;
                if (!servers.TryGetValue(url, out server)) {
                    server = new MongoServer(url);
                    servers.Add(url, server);
                }
                return server;
            }
        }

        public static MongoServer Create(
            string connectionString
        ) {
            if (connectionString.StartsWith("mongodb://")) {
                var url = MongoUrl.Create(connectionString);
                return Create(url);
            } else {
                MongoConnectionStringBuilder builder = new MongoConnectionStringBuilder(connectionString);
                return Create(builder.ToMongoUrl());
            }
        }

        public static MongoServer Create(
            Uri uri
        ) {
            return Create(MongoUrl.Create(uri.ToString()));
        }
        #endregion

        #region public properties
        public MongoCredentials AdminCredentials {
            get { return adminCredentials; }
        }

        public MongoDatabase AdminDatabase {
            get { return GetDatabase("admin", adminCredentials); }
        }

        public MongoCredentials DefaultCredentials {
            get { return defaultCredentials; }
        }

        public IEnumerable<MongoServerAddress> ReplicaSet {
            get { return replicaSet; }
        }

        public SafeMode SafeMode {
            get { return url.SafeMode; }
        }

        public IEnumerable<MongoServerAddress> SeedList {
            get { return url.Servers; }
        }

        public bool SlaveOk {
            get { return url.SlaveOk; }
        }

        public MongoServerState State {
            get { return state; }
        }

        public MongoUrl Url {
            get { return url; }
        }
        #endregion

        #region public indexers
        public MongoDatabase this[
            string databaseName
        ] {
            get { return GetDatabase(databaseName); }
        }

        public MongoDatabase this[
            string databaseName,
            MongoCredentials credentials
        ] {
            get { return GetDatabase(databaseName, credentials); }
        }

        public MongoDatabase this[
            string databaseName,
            MongoCredentials credentials,
            SafeMode safeMode
        ] {
            get { return GetDatabase(databaseName, credentials, safeMode); }
        }
        #endregion

        #region public methods
        public void CloneDatabase(
            string fromHost
        ) {
            throw new NotImplementedException();
        }

        public void Connect() {
            Connect(MongoDefaults.ConnectTimeout);
        }

        public void Connect(
            TimeSpan timeout
        ) {
            lock (serverLock) {
                if (state != MongoServerState.Connected) {
                    state = MongoServerState.Connecting;
                    try {
                        switch (url.ConnectionMode) {
                            case ConnectionMode.Direct:
                                var directConnector = new DirectConnector(url);
                                directConnector.Connect(timeout);
                                connectionPool = new MongoConnectionPool(this, directConnector.Address, directConnector.Connection);
                                replicaSet = null;
                                break;
                            case ConnectionMode.ReplicaSet:
                                var replicaSetConnector = new ReplicaSetConnector(url);
                                replicaSetConnector.Connect(timeout);
                                connectionPool = new MongoConnectionPool(this, replicaSetConnector.Primary, replicaSetConnector.PrimaryConnection);
                                replicaSet = replicaSetConnector.ReplicaSet;
                                break;
                            default:
                                throw new MongoInternalException("Invalid ConnectionMode");
                        }
                        state = MongoServerState.Connected;
                    } catch {
                        state = MongoServerState.Disconnected;
                        throw;
                    }
                }
            }
        }

        // TODO: fromHost parameter?
        public void CopyDatabase(
            string from,
            string to
        ) {
            throw new NotImplementedException();
        }

        public void Disconnect() {
            // normally called from a connection when there is a SocketException
            // but anyone can call it if they want to close all sockets to the server
            lock (serverLock) {
                if (state == MongoServerState.Connected) {
                    connectionPool.Close();
                    connectionPool = null;
                    state = MongoServerState.Disconnected;
                }
            }
        }

        public BsonDocument DropDatabase(
            string databaseName
        ) {
            MongoDatabase database = GetDatabase(databaseName);
            var command = new BsonDocument("dropDatabase", 1);
            return database.RunCommand(command);
        }

        public BsonDocument FetchDBRef(
            MongoDBRef dbRef
        ) {
            return FetchDBRefAs<BsonDocument>(dbRef);
        }

        public TDocument FetchDBRefAs<TDocument>(
            MongoDBRef dbRef
        ) {
            if (dbRef.DatabaseName == null) {
                throw new ArgumentException("MongoDBRef DatabaseName missing");
            }

            var database = GetDatabase(dbRef.DatabaseName);
            return database.FetchDBRefAs<TDocument>(dbRef);
        }

        public MongoDatabase GetDatabase(
            string databaseName
        ) {
            return GetDatabase(databaseName, defaultCredentials);
        }

        public MongoDatabase GetDatabase(
            string databaseName,
            MongoCredentials credentials
        ) {
            return GetDatabase(databaseName, credentials, url.SafeMode);
        }

        public MongoDatabase GetDatabase(
            string databaseName,
            MongoCredentials credentials,
            SafeMode safeMode
        ) {
            lock (serverLock) {
                var key = string.Format("{0}[{1},{2}]", databaseName, (credentials == null) ? "anon" : credentials.ToString(), safeMode);
                MongoDatabase database;
                if (!databases.TryGetValue(key, out database)) {
                    database = new MongoDatabase(this, databaseName, credentials, safeMode);
                    databases.Add(key, database);
                }
                return database;
            }
        }

        public IEnumerable<string> GetDatabaseNames() {
            return GetDatabaseNames(adminCredentials);
        }

        public IEnumerable<string> GetDatabaseNames(
            MongoCredentials adminCredentials
        ) {
            var adminDatabase = GetDatabase("admin", adminCredentials);
            var result = adminDatabase.RunCommand("listDatabases");
            var databaseNames = new List<string>();
            foreach (BsonDocument database in result["databases"].AsBsonArray.Values) {
                string databaseName = database["name"].AsString;
                databaseNames.Add(databaseName);
            }
            databaseNames.Sort();
            return databaseNames;
        }

        public void Reconnect() {
            lock (serverLock) {
                Disconnect();
                Connect();
            }
        }

        public BsonDocument RunAdminCommand<TCommand>(
            MongoCredentials adminCredentials,
            TCommand command
        ) {
            var adminDatabase = GetDatabase("admin", adminCredentials);
            return adminDatabase.RunCommand(command);
        }

        public BsonDocument RunAdminCommand<TCommand>(
            TCommand command
        ) {
            return RunAdminCommand(adminCredentials , command);
        }

        public BsonDocument RunAdminCommand(
            MongoCredentials adminCredentials,
            string commandName
        ) {
            var adminDatabase = GetDatabase("admin", adminCredentials);
            var command = new BsonDocument(commandName, true);
            return adminDatabase.RunCommand(command);
        }

        public BsonDocument RunAdminCommand(
            string commandName
        ) {
            return RunAdminCommand(adminCredentials, commandName);
        }
        #endregion

        #region internal methods
        internal MongoConnectionPool GetConnectionPool() {
            lock (serverLock) {
                if (connectionPool == null) {
                    Connect();
                }
                return connectionPool;
            }
        }
        #endregion
    }
}
