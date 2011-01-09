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
using System.Net;
using System.Text;
using System.Threading;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver {
    public class MongoServer {
        #region private static fields
        private static object staticLock = new object();
        private static Dictionary<string, MongoServer> servers = new Dictionary<string, MongoServer>();
        #endregion

        #region private fields
        private object serverLock = new object();
        private object requestsLock = new object();
        private MongoUrl url;
        private List<MongoServerAddress> addresses = new List<MongoServerAddress>();
        private List<IPEndPoint> endPoints = new List<IPEndPoint>();
        private MongoServerState state = MongoServerState.Disconnected;
        private IEnumerable<MongoServerAddress> replicaSet;
        private Dictionary<string, MongoDatabase> databases = new Dictionary<string, MongoDatabase>();
        private MongoConnectionPoolSettings connectionPoolSettings;
        private MongoConnectionPool primaryConnectionPool;
        private List<MongoConnectionPool> secondaryConnectionPools;
        private int secondaryConnectionPoolIndex; // used to distribute reads across secondaries in round robin fashion
        private int maxDocumentSize = BsonDefaults.MaxDocumentSize; // will get overridden if server advertises different maxDocumentSize
        private int maxMessageLength = MongoDefaults.MaxMessageLength; // will get overridden if server advertises different maxMessageLength
        private MongoCredentials defaultCredentials;
        private Dictionary<int, Request> requests = new Dictionary<int, Request>(); // tracks threads that have called RequestStart
        #endregion

        #region constructors
        public MongoServer(
            string connectionString
        ) {
            if (connectionString.StartsWith("mongodb://")) {
                this.url = MongoUrl.Create(connectionString);
            } else {
                var builder = new MongoConnectionStringBuilder(connectionString);
                this.url = builder.ToMongoUrl();
            }
            this.defaultCredentials = url.Credentials;
            this.connectionPoolSettings = url.ConnectionPoolSettings;

            foreach (var address in url.Servers) {
                addresses.Add(address);
                endPoints.Add(address.ToIPEndPoint());
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
            return Create(builder.ToString());
        }

        public static MongoServer Create(
            MongoUrl url
        ) {
            return Create(url.ToString());
        }

        public static MongoServer Create(
            string connectionString
        ) {
            lock (staticLock) {
                MongoServer server;
                if (!servers.TryGetValue(connectionString, out server)) {
                    server = new MongoServer(connectionString);
                    servers.Add(connectionString, server);
                }
                return server;
            }
        }

        public static MongoServer Create(
            Uri uri
        ) {
            return Create(uri.ToString());
        }
        #endregion

        #region public properties
        public MongoDatabase AdminDatabase {
            get { return GetDatabase("admin", defaultCredentials); }
        }

        public IEnumerable<MongoServerAddress> Addresses {
            get { return addresses; }
        }

        public MongoConnectionPoolSettings ConnectionPoolSettings {
            get { return connectionPoolSettings; }
        }

        public MongoCredentials DefaultCredentials {
            get { return defaultCredentials; }
        }

        public IEnumerable<IPEndPoint> EndPoints {
            get { return endPoints; }
        }

        public int MaxDocumentSize {
            get { return maxDocumentSize; }
        }

        public int MaxMessageLength {
            get { return maxMessageLength; }
        }

        public IEnumerable<MongoServerAddress> ReplicaSet {
            get { return replicaSet; }
        }

        public int RequestNestingLevel {
            get {
                int threadId = Thread.CurrentThread.ManagedThreadId;
                lock (requestsLock) {
                    Request request;
                    if (requests.TryGetValue(threadId, out request)) {
                        return request.NestingLevel;
                    } else {
                        return 0;
                    }
                }
            }
        }

        public SafeMode SafeMode {
            get { return url.SafeMode; }
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

        public MongoDatabase this[
            string databaseName,
            SafeMode safeMode
        ] {
            get { return GetDatabase(databaseName, safeMode); }
        }
        #endregion

        #region public methods
        public void CloneDatabase(
            string fromHost
        ) {
            throw new NotImplementedException();
        }

        public void Connect() {
            Connect(connectionPoolSettings.ConnectTimeout);
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
                                var directConnector = new DirectConnector(this);
                                directConnector.Connect(timeout);
                                primaryConnectionPool = new MongoConnectionPool(this, directConnector.Connection);
                                secondaryConnectionPools = null;
                                replicaSet = null;
                                maxDocumentSize = directConnector.MaxDocumentSize;
                                maxMessageLength = directConnector.MaxMessageLength;
                                break;
                            case ConnectionMode.ReplicaSet:
                                var replicaSetConnector = new ReplicaSetConnector(this);
                                replicaSetConnector.Connect(timeout);
                                primaryConnectionPool = new MongoConnectionPool(this, replicaSetConnector.PrimaryConnection);
                                if (url.SlaveOk && replicaSetConnector.SecondaryConnections.Count > 0) {
                                    secondaryConnectionPools = new List<MongoConnectionPool>();
                                    foreach (var connection in replicaSetConnector.SecondaryConnections) {
                                        var secondaryConnectionPool = new MongoConnectionPool(this, connection);
                                        secondaryConnectionPools.Add(secondaryConnectionPool);
                                    }
                                } else {
                                    secondaryConnectionPools = null;
                                }
                                replicaSet = replicaSetConnector.ReplicaSet;
                                maxDocumentSize = replicaSetConnector.MaxDocumentSize;
                                maxMessageLength = replicaSetConnector.MaxMessageLength;
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
                    primaryConnectionPool.Close();
                    primaryConnectionPool = null;
                    if (secondaryConnectionPools != null) {
                        foreach (var secondaryConnectionPool in secondaryConnectionPools) {
                            secondaryConnectionPool.Close();
                        }
                        secondaryConnectionPools = null;
                    }
                    state = MongoServerState.Disconnected;
                }
            }
        }

        public CommandResult DropDatabase(
            string databaseName
        ) {
            MongoDatabase database = GetDatabase(databaseName);
            var command = new CommandDocument("dropDatabase", 1);
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

        public MongoDatabase GetDatabase(
            string databaseName,
            SafeMode safeMode
        ) {
            return GetDatabase(databaseName, defaultCredentials, safeMode);
        }

        public IEnumerable<string> GetDatabaseNames() {
            var result = AdminDatabase.RunCommand("listDatabases");
            var databaseNames = new List<string>();
            foreach (BsonDocument database in result.Response["databases"].AsBsonArray.Values) {
                string databaseName = database["name"].AsString;
                databaseNames.Add(databaseName);
            }
            databaseNames.Sort();
            return databaseNames;
        }

        public GetLastErrorResult GetLastError() {
            if (RequestNestingLevel == 0) {
                throw new InvalidOperationException("GetLastError can only be called if RequestStart has been called first");
            }
            var adminDatabase = GetDatabase("admin", (MongoCredentials) null); // no credentials needed for getlasterror
            return adminDatabase.RunCommandAs<GetLastErrorResult>("getlasterror"); // use all lowercase for backward compatibility
        }

        public void Reconnect() {
            lock (serverLock) {
                Disconnect();
                Connect();
            }
        }

        public void RequestDone() {
            int threadId = Thread.CurrentThread.ManagedThreadId;
            MongoConnection connection = null;
            lock (requestsLock) {
                Request request;
                if (requests.TryGetValue(threadId, out request)) {
                    if (--request.NestingLevel == 0) {
                        connection = request.Connection;
                        requests.Remove(threadId);
                    }
                } else {
                    throw new InvalidOperationException("Thread is not in a request (did you call RequestStart?)");
                }
            }

            // release the connection outside of the lock
            if (connection != null) {
                ReleaseConnection(connection);
            }
        }

        // the result of RequestStart is IDisposable so you can use RequestStart in a using statment
        // and then RequestDone will be called automatically when leaving the using statement
        public IDisposable RequestStart(
            MongoDatabase initialDatabase
        ) {
            int threadId = Thread.CurrentThread.ManagedThreadId;
            lock (requestsLock) {
                Request request;
                if (requests.TryGetValue(threadId, out request)) {
                    request.NestingLevel++;
                    return new RequestStartResult(this);
                }
            }

            // get the connection outside of the lock
            var connection = AcquireConnection(initialDatabase, false); // not slaveOk

            lock (requestsLock) {
                var request = new Request(connection);
                requests.Add(threadId, request);
                return new RequestStartResult(this);
            }
        }

        public CommandResult RunAdminCommand(
            IMongoCommand command
        ) {
            return RunAdminCommandAs<CommandResult>(command);
        }

        public CommandResult RunAdminCommand(
            string commandName
        ) {
            return RunAdminCommandAs<CommandResult>(commandName);
        }

        public TCommandResult RunAdminCommandAs<TCommandResult>(
            IMongoCommand command
        ) where TCommandResult : CommandResult, new() {
            return AdminDatabase.RunCommandAs<TCommandResult>(command);
        }

        public TCommandResult RunAdminCommandAs<TCommandResult>(
            string commandName
        ) where TCommandResult : CommandResult, new() {
            return AdminDatabase.RunCommandAs<TCommandResult>(commandName);
        }
        #endregion

        #region internal methods
        internal MongoConnection AcquireConnection(
            MongoDatabase database,
            bool slaveOk
        ) {
            // if a thread has called RequestStart it wants all operations to take place on the same connection
            int threadId = Thread.CurrentThread.ManagedThreadId;
            lock (requestsLock) {
                Request request;
                if (requests.TryGetValue(threadId, out request)) {
                    request.Connection.CheckAuthentication(this, database); // will throw exception if authentication fails
                    return request.Connection;
                }
            }

            var connectionPool = GetConnectionPool(slaveOk);
            var connection = connectionPool.AcquireConnection(database);

            try {
                connection.CheckAuthentication(this, database); // will authenticate if necessary
            } catch (MongoAuthenticationException) {
                // don't let the connection go to waste just because authentication failed
                connectionPool.ReleaseConnection(connection);
                throw;
            }

            return connection;
        }

        internal MongoConnectionPool GetConnectionPool(
            bool slaveOk
        ) {
            lock (serverLock) {
                if (primaryConnectionPool == null) {
                    Connect();
                }
                if (slaveOk && secondaryConnectionPools != null) {
                    secondaryConnectionPoolIndex = (secondaryConnectionPoolIndex + 1) % secondaryConnectionPools.Count; // round robin
                    return secondaryConnectionPools[secondaryConnectionPoolIndex];
                }
                return primaryConnectionPool;
            }
        }

        internal void ReleaseConnection(
            MongoConnection connection
        ) {
            // if the thread has called RequestStart just verify that the connection it is releasing is the right one
            int threadId = Thread.CurrentThread.ManagedThreadId;
            lock (requestsLock) {
                Request request;
                if (requests.TryGetValue(threadId, out request)) {
                    if (connection != request.Connection) {
                        throw new ArgumentException("Connection being released is not the one assigned to the thread by RequestStart", "connection");
                    }
                    return; // hold on to the connection until RequestDone is called
                }
            }

            // the connection might belong to a connection pool that has already been discarded
            // so always release it to the connection pool it came from and not the current pool
            connection.ConnectionPool.ReleaseConnection(connection);
        }
        #endregion

        #region private nested classes
        private class Request {
            #region private fields
            private MongoConnection connection;
            private int nestingLevel;
            #endregion

            #region constructors
            public Request(
                MongoConnection connection
            ) {
                this.connection = connection;
                this.nestingLevel = 1;
            }
            #endregion

            #region public properties
            public MongoConnection Connection {
                get { return connection; }
                set { connection = value; }
            }

            public int NestingLevel {
                get { return nestingLevel; }
                set { nestingLevel = value; }
            }
            #endregion
        }

        private class RequestStartResult : IDisposable {
            #region private fields
            private MongoServer server;
            #endregion

            #region constructors
            public RequestStartResult(
                MongoServer server
            ) {
                this.server = server;
            }
            #endregion

            #region public methods
            public void Dispose() {
                server.RequestDone();
            }
            #endregion
        }
        #endregion
    }
}
