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
        private static Dictionary<MongoServerSettings, MongoServer> servers = new Dictionary<MongoServerSettings, MongoServer>();
        #endregion

        #region private fields
        private object serverLock = new object();
        private object requestsLock = new object();
        private MongoServerSettings settings;
        private List<IPEndPoint> endPoints = new List<IPEndPoint>();
        private MongoServerState state = MongoServerState.Disconnected;
        private IEnumerable<MongoServerAddress> replicaSet;
        private Dictionary<MongoDatabaseSettings, MongoDatabase> databases = new Dictionary<MongoDatabaseSettings, MongoDatabase>();
        private MongoConnectionPool primaryConnectionPool;
        private List<MongoConnectionPool> secondaryConnectionPools;
        private int secondaryConnectionPoolIndex; // used to distribute reads across secondaries in round robin fashion
        private int maxDocumentSize = BsonDefaults.MaxDocumentSize; // will get overridden if server advertises different maxDocumentSize
        private int maxMessageLength = MongoDefaults.MaxMessageLength; // will get overridden if server advertises different maxMessageLength
        private Dictionary<int, Request> requests = new Dictionary<int, Request>(); // tracks threads that have called RequestStart
        #endregion

        #region constructors
        public MongoServer(
            MongoServerSettings settings
        ) {
            this.settings = settings;

            foreach (var address in settings.Servers) {
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
            return Create(builder.ToServerSettings());
        }

        public static MongoServer Create(
            MongoServerSettings settings
        ) {
            lock (staticLock) {
                MongoServer server;
                settings.Freeze();
                if (!servers.TryGetValue(settings, out server)) {
                    server = new MongoServer(settings);
                    servers.Add(settings, server);
                }
                return server;
            }
        }

        public static MongoServer Create(
            MongoUrl url
        ) {
            return Create(url.ToServerSettings());
        }

        public static MongoServer Create(
            string connectionString
        ) {
            if (connectionString.StartsWith("mongodb://")) {
                var url = MongoUrl.Create(connectionString);
                return Create(url);
            } else {
                var builder = new MongoConnectionStringBuilder(connectionString);
                return Create(builder);
            }
        }

        public static MongoServer Create(
            Uri uri
        ) {
            var url = MongoUrl.Create(uri.ToString());
            return Create(url);
        }
        #endregion

        #region public properties
        public virtual MongoDatabase AdminDatabase {
            get { return GetDatabase("admin", settings.DefaultCredentials); }
        }

        public virtual IEnumerable<IPEndPoint> EndPoints {
            get { return endPoints; }
        }

        public virtual int MaxDocumentSize {
            get { return maxDocumentSize; }
        }

        public virtual int MaxMessageLength {
            get { return maxMessageLength; }
        }

        public virtual IEnumerable<MongoServerAddress> ReplicaSet {
            get { return replicaSet; }
        }

        public virtual int RequestNestingLevel {
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

        public virtual MongoServerSettings Settings {
            get { return settings; }
        }

        public virtual MongoServerState State {
            get { return state; }
        }
        #endregion

        #region public indexers
        public virtual MongoDatabase this[
            string databaseName
        ] {
            get { return GetDatabase(databaseName); }
        }

        public virtual MongoDatabase this[
            string databaseName,
            MongoCredentials credentials
        ] {
            get { return GetDatabase(databaseName, credentials); }
        }

        public virtual MongoDatabase this[
            MongoDatabaseSettings databaseSettings
        ] {
            get { return GetDatabase(databaseSettings); }
        }

        public virtual MongoDatabase this[
            string databaseName,
            MongoCredentials credentials,
            SafeMode safeMode
        ] {
            get { return GetDatabase(databaseName, credentials, safeMode); }
        }

        public virtual MongoDatabase this[
            string databaseName,
            SafeMode safeMode
        ] {
            get { return GetDatabase(databaseName, safeMode); }
        }
        #endregion

        #region public methods
        public virtual void CloneDatabase(
            string fromHost
        ) {
            throw new NotImplementedException();
        }

        public virtual void Connect() {
            Connect(settings.ConnectTimeout);
        }

        public virtual void Connect(
            TimeSpan timeout
        ) {
            lock (serverLock) {
                if (state != MongoServerState.Connected) {
                    state = MongoServerState.Connecting;
                    try {
                        switch (settings.ConnectionMode) {
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
                                if (settings.SlaveOk && replicaSetConnector.SecondaryConnections.Count > 0) {
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
        public virtual void CopyDatabase(
            string from,
            string to
        ) {
            throw new NotImplementedException();
        }

        public virtual void Disconnect() {
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

        public virtual CommandResult DropDatabase(
            string databaseName
        ) {
            MongoDatabase database = GetDatabase(databaseName);
            var command = new CommandDocument("dropDatabase", 1);
            return database.RunCommand(command);
        }

        public virtual BsonDocument FetchDBRef(
            MongoDBRef dbRef
        ) {
            return FetchDBRefAs<BsonDocument>(dbRef);
        }

        public virtual TDocument FetchDBRefAs<TDocument>(
            MongoDBRef dbRef
        ) {
            if (dbRef.DatabaseName == null) {
                throw new ArgumentException("MongoDBRef DatabaseName missing");
            }

            var database = GetDatabase(dbRef.DatabaseName);
            return database.FetchDBRefAs<TDocument>(dbRef);
        }

        public virtual MongoDatabase GetDatabase(
            MongoDatabaseSettings databaseSettings
        ) {
            lock (serverLock) {
                MongoDatabase database;
                databaseSettings.Freeze();
                if (!databases.TryGetValue(databaseSettings, out database)) {
                    database = new MongoDatabase(this, databaseSettings);
                    databases.Add(databaseSettings, database);
                }
                return database;
            }
        }

        public virtual MongoDatabase GetDatabase(
            string databaseName
        ) {
            var databaseSettings = GetDatabaseSettings(databaseName);
            return GetDatabase(databaseSettings);
        }

        public virtual MongoDatabase GetDatabase(
            string databaseName,
            MongoCredentials credentials
        ) {
            var databaseSettings = GetDatabaseSettings(databaseName);
            databaseSettings.Credentials = credentials;
            return GetDatabase(databaseSettings);
        }

        public virtual MongoDatabase GetDatabase(
            string databaseName,
            MongoCredentials credentials,
            SafeMode safeMode
        ) {
            var databaseSettings = GetDatabaseSettings(databaseName);
            databaseSettings.Credentials = credentials;
            databaseSettings.SafeMode = safeMode;
            return GetDatabase(databaseSettings);
        }

        public virtual MongoDatabase GetDatabase(
            string databaseName,
            SafeMode safeMode
        ) {
            var databaseSettings = GetDatabaseSettings(databaseName);
            databaseSettings.SafeMode = safeMode;
            return GetDatabase(databaseSettings);
        }

        public virtual IEnumerable<string> GetDatabaseNames() {
            var result = AdminDatabase.RunCommand("listDatabases");
            var databaseNames = new List<string>();
            foreach (BsonDocument database in result.Response["databases"].AsBsonArray.Values) {
                string databaseName = database["name"].AsString;
                databaseNames.Add(databaseName);
            }
            databaseNames.Sort();
            return databaseNames;
        }

        public virtual MongoDatabaseSettings GetDatabaseSettings(
            string databaseName
        ) {
            return new MongoDatabaseSettings(
                databaseName,
                settings.DefaultCredentials,
                settings.SafeMode,
                settings.SlaveOk
            );
        }

        public virtual GetLastErrorResult GetLastError() {
            if (RequestNestingLevel == 0) {
                throw new InvalidOperationException("GetLastError can only be called if RequestStart has been called first");
            }
            var adminDatabase = GetDatabase("admin", (MongoCredentials) null); // no credentials needed for getlasterror
            return adminDatabase.RunCommandAs<GetLastErrorResult>("getlasterror"); // use all lowercase for backward compatibility
        }

        public virtual void Reconnect() {
            lock (serverLock) {
                Disconnect();
                Connect();
            }
        }

        public virtual void RequestDone() {
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
        public virtual IDisposable RequestStart(
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

        public virtual CommandResult RunAdminCommand(
            IMongoCommand command
        ) {
            return RunAdminCommandAs<CommandResult>(command);
        }

        public virtual CommandResult RunAdminCommand(
            string commandName
        ) {
            return RunAdminCommandAs<CommandResult>(commandName);
        }

        public virtual TCommandResult RunAdminCommandAs<TCommandResult>(
            IMongoCommand command
        ) where TCommandResult : CommandResult, new() {
            return AdminDatabase.RunCommandAs<TCommandResult>(command);
        }

        public virtual TCommandResult RunAdminCommandAs<TCommandResult>(
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
