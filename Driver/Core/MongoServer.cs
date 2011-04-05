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
    /// <summary>
    /// Represents a MongoDB server (either a single instance or a replica set) and the settings used to access it. This class is thread-safe.
    /// </summary>
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
        private IndexCache indexCache = new IndexCache();
        #endregion

        #region constructors
        /// <summary>
        /// Creates a new instance of MongoServer. Normally you will use one of the Create methods instead
        /// of the constructor to create instances of this class.
        /// </summary>
        /// <param name="settings">The settings for this instance of MongoServer.</param>
        public MongoServer(
            MongoServerSettings settings
        ) {
            this.settings = settings.Freeze();

            foreach (var address in settings.Servers) {
                endPoints.Add(address.ToIPEndPoint(settings.AddressFamily));
            }
        }
        #endregion

        #region factory methods
        /// <summary>
        /// Creates a new instance or returns an existing instance of MongoServer. Only one instance
        /// is created for each combination of server settings.
        /// </summary>
        /// <returns>
        /// A new or existing instance of MongoServer.
        /// </returns>
        public static MongoServer Create() {
            return Create("mongodb://localhost");
        }

        /// <summary>
        /// Creates a new instance or returns an existing instance of MongoServer. Only one instance
        /// is created for each combination of server settings.
        /// </summary>
        /// <param name="builder">Server settings in the form of a MongoConnectionStringBuilder.</param>
        /// <returns>
        /// A new or existing instance of MongoServer.
        /// </returns>
        public static MongoServer Create(
            MongoConnectionStringBuilder builder
        ) {
            return Create(builder.ToServerSettings());
        }

        /// <summary>
        /// Creates a new instance or returns an existing instance of MongoServer. Only one instance
        /// is created for each combination of server settings.
        /// </summary>
        /// <param name="settings">Server settings.</param>
        /// <returns>
        /// A new or existing instance of MongoServer.
        /// </returns>
        public static MongoServer Create(
            MongoServerSettings settings
        ) {
            lock (staticLock) {
                MongoServer server;
                if (!servers.TryGetValue(settings, out server)) {
                    server = new MongoServer(settings);
                    servers.Add(settings, server);
                }
                return server;
            }
        }

        /// <summary>
        /// Creates a new instance or returns an existing instance of MongoServer. Only one instance
        /// is created for each combination of server settings.
        /// </summary>
        /// <param name="url">Server settings in the form of a MongoUrl.</param>
        /// <returns>
        /// A new or existing instance of MongoServer.
        /// </returns>
        public static MongoServer Create(
            MongoUrl url
        ) {
            return Create(url.ToServerSettings());
        }

        /// <summary>
        /// Creates a new instance or returns an existing instance of MongoServer. Only one instance
        /// is created for each combination of server settings.
        /// </summary>
        /// <param name="connectionString">Server settings in the form of a connection string.</param>
        /// <returns>
        /// A new or existing instance of MongoServer.
        /// </returns>
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

        /// <summary>
        /// Creates a new instance or returns an existing instance of MongoServer. Only one instance
        /// is created for each combination of server settings.
        /// </summary>
        /// <param name="uri">Server settings in the form of a Uri.</param>
        /// <returns>
        /// A new or existing instance of MongoServer.
        /// </returns>
        public static MongoServer Create(
            Uri uri
        ) {
            var url = MongoUrl.Create(uri.ToString());
            return Create(url);
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the admin database for this server.
        /// </summary>
        public virtual MongoDatabase AdminDatabase {
            get { return GetDatabase("admin"); }
        }

        /// <summary>
        /// Gets the connection pool (if connected to a replica set this is the connection pool to the primary).
        /// </summary>
        public virtual MongoConnectionPool ConnectionPool {
            get { return primaryConnectionPool; }
        }

        /// <summary>
        /// Gets the IP end points for this server.
        /// </summary>
        public virtual IEnumerable<IPEndPoint> EndPoints {
            get { return endPoints; }
        }

        /// <summary>
        /// Gets the index cache (used by EnsureIndex) for this server.
        /// </summary>
        public virtual IndexCache IndexCache {
            get { return indexCache; }
        }

        /// <summary>
        /// Gets the max document size for this server (not valid until connected).
        /// </summary>
        public virtual int MaxDocumentSize {
            get { return maxDocumentSize; }
        }

        /// <summary>
        /// Gets the max message length for this server (not valid until connected).
        /// </summary>
        public virtual int MaxMessageLength {
            get { return maxMessageLength; }
        }

        /// <summary>
        /// Gets a list of the members of the replica set (not valid until connected).
        /// </summary>
        public virtual IEnumerable<MongoServerAddress> ReplicaSet {
            get { return replicaSet; }
        }

        /// <summary>
        /// Gets the RequestStart nesting level for the current thread.
        /// </summary>
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

        /// <summary>
        /// Gets a read only list of the connection pools to the secondary servers (when connected to a replica set).
        /// </summary>
        public IList<MongoConnectionPool> SecondaryConnectionPools {
            get { return secondaryConnectionPools.AsReadOnly(); }
        }

        /// <summary>
        /// Gets the settings for this server.
        /// </summary>
        public virtual MongoServerSettings Settings {
            get { return settings; }
        }

        /// <summary>
        /// Gets the current state of this server (as of the last operation, not updated until another operation is performed).
        /// </summary>
        public virtual MongoServerState State {
            get { return state; }
        }
        #endregion

        #region public indexers
        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        public virtual MongoDatabase this[
            string databaseName
        ] {
            get { return GetDatabase(databaseName); }
        }

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="credentials">The credentials to use with this database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        public virtual MongoDatabase this[
            string databaseName,
            MongoCredentials credentials
        ] {
            get { return GetDatabase(databaseName, credentials); }
        }

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="databaseSettings">The settings to use with this database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        public virtual MongoDatabase this[
            MongoDatabaseSettings databaseSettings
        ] {
            get { return GetDatabase(databaseSettings); }
        }

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="credentials">The credentials to use with this database.</param>
        /// <param name="safeMode">The safe mode to use with this database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        public virtual MongoDatabase this[
            string databaseName,
            MongoCredentials credentials,
            SafeMode safeMode
        ] {
            get { return GetDatabase(databaseName, credentials, safeMode); }
        }

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="safeMode">The safe mode to use with this database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        public virtual MongoDatabase this[
            string databaseName,
            SafeMode safeMode
        ] {
            get { return GetDatabase(databaseName, safeMode); }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Connects to the server. Normally there is no need to call this method as
        /// the driver will connect to the server automatically when needed.
        /// </summary>
        public virtual void Connect() {
            Connect(settings.ConnectTimeout);
        }

        /// <summary>
        /// Connects to the server. Normally there is no need to call this method as
        /// the driver will connect to the server automatically when needed.
        /// </summary>
        /// <param name="timeout">How long to wait before timing out.</param>
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
        /// <summary>
        /// Copies a database.
        /// </summary>
        /// <param name="from">The name of an existing database.</param>
        /// <param name="to">The name of the new database.</param>
        public virtual void CopyDatabase(
            string from,
            string to
        ) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates an instance of MongoDatabaseSettings for the named database with the rest of the settings inherited.
        /// You can override some of these settings before calling GetDatabase.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <returns>An instance of MongoDatabase for <paramref name="databaseName"/>.</returns>
        public virtual MongoDatabaseSettings CreateDatabaseSettings(
            string databaseName
        ) {
            return new MongoDatabaseSettings(
                databaseName,
                settings.DefaultCredentials,
                settings.SafeMode,
                settings.SlaveOk
            );
        }

        /// <summary>
        /// Tests whether a database exists.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <returns>True if the database exists.</returns>
        public virtual bool DatabaseExists(
            string databaseName
        ) {
            return GetDatabaseNames().Contains(databaseName);
        }

        /// <summary>
        /// Disconnects from the server. Normally there is no need to call this method so
        /// you should be sure to have a good reason to call it.
        /// </summary>
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

        /// <summary>
        /// Drops a database.
        /// </summary>
        /// <param name="databaseName">The name of the database to be dropped.</param>
        /// <returns>A <see cref="CommandResult"/>.</returns>
        public virtual CommandResult DropDatabase(
            string databaseName
        ) {
            MongoDatabase database = GetDatabase(databaseName);
            var command = new CommandDocument("dropDatabase", 1);
            return database.RunCommand(command);
        }

        /// <summary>
        /// Fetches the document referred to by the DBRef.
        /// </summary>
        /// <param name="dbRef">The <see cref="MongoDBRef"/> to fetch.</param>
        /// <returns>A BsonDocument (or null if the document was not found).</returns>
        public virtual BsonDocument FetchDBRef(
            MongoDBRef dbRef
        ) {
            return FetchDBRefAs<BsonDocument>(dbRef);
        }

        /// <summary>
        /// Fetches the document referred to by the DBRef, deserialized as a <typeparamref name="TDocument"/>.
        /// </summary>
        /// <param name="dbRef">The <see cref="MongoDBRef"/> to fetch.</param>
        /// <returns>A <typeparamref name="TDocument"/> (or null if the document was not found).</returns>
        public virtual TDocument FetchDBRefAs<TDocument>(
            MongoDBRef dbRef
        ) {
            if (dbRef.DatabaseName == null) {
                throw new ArgumentException("MongoDBRef DatabaseName missing");
            }

            var database = GetDatabase(dbRef.DatabaseName);
            return database.FetchDBRefAs<TDocument>(dbRef);
        }

        /// <summary>
        /// Gets a MongoDatabase instance representing the admin database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="credentials">The credentials to use with the admin database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        public virtual MongoDatabase GetAdminDatabase(
            MongoCredentials credentials
        ) {
            return GetDatabase("admin", credentials);
        }

        /// <summary>
        /// Gets a MongoDatabase instance representing the admin database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="credentials">The credentials to use with the admin database.</param>
        /// <param name="safeMode">The safe mode to use with the admin database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        public virtual MongoDatabase GetAdminDatabase(
            MongoCredentials credentials,
            SafeMode safeMode
        ) {
            return GetDatabase("admin", credentials, safeMode);
        }

        /// <summary>
        /// Gets a MongoDatabase instance representing the admin database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="safeMode">The safe mode to use with the admin database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        public virtual MongoDatabase GetAdminDatabase(
            SafeMode safeMode
        ) {
            return GetDatabase("admin", safeMode);
        }

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="databaseSettings">The settings to use with this database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        public virtual MongoDatabase GetDatabase(
            MongoDatabaseSettings databaseSettings
        ) {
            lock (serverLock) {
                MongoDatabase database;
                if (!databases.TryGetValue(databaseSettings, out database)) {
                    database = new MongoDatabase(this, databaseSettings);
                    databases.Add(databaseSettings, database);
                }
                return database;
            }
        }

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        public virtual MongoDatabase GetDatabase(
            string databaseName
        ) {
            var databaseSettings = CreateDatabaseSettings(databaseName);
            return GetDatabase(databaseSettings);
        }

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="credentials">The credentials to use with this database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        public virtual MongoDatabase GetDatabase(
            string databaseName,
            MongoCredentials credentials
        ) {
            var databaseSettings = CreateDatabaseSettings(databaseName);
            databaseSettings.Credentials = credentials;
            return GetDatabase(databaseSettings);
        }

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="credentials">The credentials to use with this database.</param>
        /// <param name="safeMode">The safe mode to use with this database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        public virtual MongoDatabase GetDatabase(
            string databaseName,
            MongoCredentials credentials,
            SafeMode safeMode
        ) {
            var databaseSettings = CreateDatabaseSettings(databaseName);
            databaseSettings.Credentials = credentials;
            databaseSettings.SafeMode = safeMode;
            return GetDatabase(databaseSettings);
        }

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="safeMode">The safe mode to use with this database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        public virtual MongoDatabase GetDatabase(
            string databaseName,
            SafeMode safeMode
        ) {
            var databaseSettings = CreateDatabaseSettings(databaseName);
            databaseSettings.SafeMode = safeMode;
            return GetDatabase(databaseSettings);
        }

        /// <summary>
        /// Gets the names of the databases on this server.
        /// </summary>
        /// <returns>A list of database names.</returns>
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

        /// <summary>
        /// Gets the last error (if any) that occurred on this connection. You MUST be within a RequestStart to call this method.
        /// </summary>
        /// <returns>The last error (<see cref=" GetLastErrorResult"/>)</returns>
        public virtual GetLastErrorResult GetLastError() {
            if (RequestNestingLevel == 0) {
                throw new InvalidOperationException("GetLastError can only be called if RequestStart has been called first");
            }
            var adminDatabase = GetAdminDatabase((MongoCredentials) null); // no credentials needed for getlasterror
            return adminDatabase.RunCommandAs<GetLastErrorResult>("getlasterror"); // use all lowercase for backward compatibility
        }

        /// <summary>
        /// Checks whether the server is alive (throws an exception if not).
        /// </summary>
        public virtual void Ping() {
            var command = new CommandDocument("ping", 1);
            RunAdminCommand(command);
        }

        /// <summary>
        /// Reconnects to the server. Normally there is no need to call this method. All connections
        /// are closed and new connections will be opened as needed. Calling
        /// this method frequently will result in connection thrashing.
        /// </summary>
        public virtual void Reconnect() {
            lock (serverLock) {
                Disconnect();
                Connect();
            }
        }

        /// <summary>
        /// Lets the server know that this thread is done with a series of related operations. Instead of calling this method it is better
        /// to put the return value of RequestStart in a using statement.
        /// </summary>
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

        /// <summary>
        /// Lets the server know that this thread is about to begin a series of related operations that must all occur
        /// on the same connection. The return value of this method implements IDisposable and can be placed in a
        /// using statement (in which case RequestDone will be called automatically when leaving the using statement).
        /// </summary>
        /// <param name="initialDatabase">One of the databases involved in the related operations.</param>
        /// <returns>A helper object that implements IDisposable and calls <see cref="RequestDone"/> from the Dispose method.</returns>
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

        /// <summary>
        /// Removes all entries in the index cache used by EnsureIndex. Call this method
        /// when you know (or suspect) that a process other than this one may have dropped one or
        /// more indexes.
        /// </summary>
        public virtual void ResetIndexCache() {
            indexCache.Reset();
        }

        /// <summary>
        /// Runs a command on the admin database.
        /// </summary>
        /// <param name="command">The command to run.</param>
        /// <returns>The result of the command (see <see cref="CommandResult"/>).</returns>
        public virtual CommandResult RunAdminCommand(
            IMongoCommand command
        ) {
            return RunAdminCommandAs<CommandResult>(command);
        }

        /// <summary>
        /// Runs a command on the admin database.
        /// </summary>
        /// <param name="commandName">The name of the command to run.</param>
        /// <returns>The result of the command (as a <see cref="CommandResult"/>).</returns>
        public virtual CommandResult RunAdminCommand(
            string commandName
        ) {
            return RunAdminCommandAs<CommandResult>(commandName);
        }

        /// <summary>
        /// Runs a command on the admin database.
        /// </summary>
        /// <typeparam name="TCommandResult">The type to use for the command result.</typeparam>
        /// <param name="command">The command to run.</param>
        /// <returns>The result of the command (as a <typeparamref name="TCommandResult"/>).</returns>
        public virtual TCommandResult RunAdminCommandAs<TCommandResult>(
            IMongoCommand command
        ) where TCommandResult : CommandResult, new() {
            return AdminDatabase.RunCommandAs<TCommandResult>(command);
        }

        /// <summary>
        /// Runs a command on the admin database.
        /// </summary>
        /// <typeparam name="TCommandResult">The type to use for the command result.</typeparam>
        /// <param name="commandName">The name of the command to run.</param>
        /// <returns>The result of the command (as a <typeparamref name="TCommandResult"/>).</returns>
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
