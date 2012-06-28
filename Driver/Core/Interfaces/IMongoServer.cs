using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver
{
    public interface IMongoServer
    {
        /// <summary>
        /// Gets the arbiter instances.
        /// </summary>
        MongoServerInstance[] Arbiters { get; }

        /// <summary>
        /// Gets the build info of the server.
        /// </summary>
        MongoServerBuildInfo BuildInfo { get; }

        /// <summary>
        /// Gets the most recent connection attempt number.
        /// </summary>
        int ConnectionAttempt { get; }

        /// <summary>
        /// Gets the index cache (used by EnsureIndex) for this server.
        /// </summary>
        IndexCache IndexCache { get; }

        /// <summary>
        /// Gets the one and only instance for this server.
        /// </summary>
        MongoServerInstance Instance { get; }

        /// <summary>
        /// Gets the instances for this server.
        /// </summary>
        MongoServerInstance[] Instances { get; }

        /// <summary>
        /// Gets the passive instances.
        /// </summary>
        MongoServerInstance[] Passives { get; }

        /// <summary>
        /// Gets the primary instance (null if there is no primary).
        /// </summary>
        MongoServerInstance Primary { get; }

        /// <summary>
        /// Gets the name of the replica set (null if not connected to a replica set).
        /// </summary>
        string ReplicaSetName { get; }

        /// <summary>
        /// Gets the connection reserved by the current RequestStart scope (null if not in the scope of a RequestStart).
        /// </summary>
        MongoConnection RequestConnection { get; }

        /// <summary>
        /// Gets the RequestStart nesting level for the current thread.
        /// </summary>
        int RequestNestingLevel { get; }

        /// <summary>
        /// Gets the secondary instances.
        /// </summary>
        MongoServerInstance[] Secondaries { get; }

        /// <summary>
        /// Gets the unique sequential Id for this server.
        /// </summary>
        int SequentialId { get; }

        /// <summary>
        /// Gets the settings for this server.
        /// </summary>
        MongoServerSettings Settings { get; }

        /// <summary>
        /// Gets the current state of this server (as of the last operation, not updated until another operation is performed).
        /// </summary>
        MongoServerState State { get; }

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        MongoDatabase this[string databaseName] { get; }

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="credentials">The credentials to use with this database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        MongoDatabase this[string databaseName, MongoCredentials credentials] { get; }

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="databaseSettings">The settings to use with this database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        MongoDatabase this[MongoDatabaseSettings databaseSettings] { get; }

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="credentials">The credentials to use with this database.</param>
        /// <param name="safeMode">The safe mode to use with this database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        MongoDatabase this[string databaseName, MongoCredentials credentials, SafeMode safeMode] { get; }

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="safeMode">The safe mode to use with this database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        MongoDatabase this[string databaseName, SafeMode safeMode] { get; }

        /// <summary>
        /// Connects to the server. Normally there is no need to call this method as
        /// the driver will connect to the server automatically when needed.
        /// </summary>
        void Connect();

        /// <summary>
        /// Connects to the server. Normally there is no need to call this method as
        /// the driver will connect to the server automatically when needed.
        /// </summary>
        /// <param name="waitFor">What to wait for before returning (when connecting to a replica set).</param>
        void Connect(ConnectWaitFor waitFor);

        /// <summary>
        /// Connects to the server. Normally there is no need to call this method as
        /// the driver will connect to the server automatically when needed.
        /// </summary>
        /// <param name="timeout">How long to wait before timing out.</param>
        void Connect(TimeSpan timeout);

        /// <summary>
        /// Connects to the server. Normally there is no need to call this method as
        /// the driver will connect to the server automatically when needed.
        /// </summary>
        /// <param name="timeout">How long to wait before timing out.</param>
        /// <param name="waitFor">What to wait for before returning (when connecting to a replica set).</param>
        void Connect(TimeSpan timeout, ConnectWaitFor waitFor);

        /// <summary>
        /// Copies a database.
        /// </summary>
        /// <param name="from">The name of an existing database.</param>
        /// <param name="to">The name of the new database.</param>
        void CopyDatabase(string from, string to);

        /// <summary>
        /// Creates an instance of MongoDatabaseSettings for the named database with the rest of the settings inherited.
        /// You can override some of these settings before calling GetDatabase.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <returns>An instance of MongoDatabase for <paramref name="databaseName"/>.</returns>
        MongoDatabaseSettings CreateDatabaseSettings(string databaseName);

        /// <summary>
        /// Tests whether a database exists.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <returns>True if the database exists.</returns>
        bool DatabaseExists(string databaseName);

        /// <summary>
        /// Tests whether a database exists.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="adminCredentials">Credentials for the admin database.</param>
        /// <returns>True if the database exists.</returns>
        bool DatabaseExists(string databaseName, MongoCredentials adminCredentials);

        /// <summary>
        /// Disconnects from the server. Normally there is no need to call this method so
        /// you should be sure to have a good reason to call it.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Drops a database.
        /// </summary>
        /// <param name="databaseName">The name of the database to be dropped.</param>
        /// <returns>A <see cref="CommandResult"/>.</returns>
        CommandResult DropDatabase(string databaseName);

        /// <summary>
        /// Drops a database.
        /// </summary>
        /// <param name="databaseName">The name of the database to be dropped.</param>
        /// <param name="credentials">Credentials for the database to be dropped (or admin credentials).</param>
        /// <returns>A <see cref="CommandResult"/>.</returns>
        CommandResult DropDatabase(string databaseName, MongoCredentials credentials);

        /// <summary>
        /// Fetches the document referred to by the DBRef.
        /// </summary>
        /// <param name="dbRef">The <see cref="MongoDBRef"/> to fetch.</param>
        /// <returns>A BsonDocument (or null if the document was not found).</returns>
        BsonDocument FetchDBRef(MongoDBRef dbRef);

        /// <summary>
        /// Fetches the document referred to by the DBRef, deserialized as a <typeparamref name="TDocument"/>.
        /// </summary>
        /// <typeparam name="TDocument">The nominal type of the document to fetch.</typeparam>
        /// <param name="dbRef">The <see cref="MongoDBRef"/> to fetch.</param>
        /// <returns>A <typeparamref name="TDocument"/> (or null if the document was not found).</returns>
        TDocument FetchDBRefAs<TDocument>(MongoDBRef dbRef);

        /// <summary>
        /// Fetches the document referred to by the DBRef.
        /// </summary>
        /// <param name="documentType">The nominal type of the document to fetch.</param>
        /// <param name="dbRef">The <see cref="MongoDBRef"/> to fetch.</param>
        /// <returns>The document (or null if the document was not found).</returns>
        object FetchDBRefAs(Type documentType, MongoDBRef dbRef);

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="databaseSettings">The settings to use with this database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        MongoDatabase GetDatabase(MongoDatabaseSettings databaseSettings);

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        MongoDatabase GetDatabase(string databaseName);

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="credentials">The credentials to use with this database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        MongoDatabase GetDatabase(string databaseName, MongoCredentials credentials);

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="credentials">The credentials to use with this database.</param>
        /// <param name="safeMode">The safe mode to use with this database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        MongoDatabase GetDatabase(
            string databaseName,
            MongoCredentials credentials,
            SafeMode safeMode);

        /// <summary>
        /// Gets a MongoDatabase instance representing a database on this server. Only one instance
        /// is created for each combination of database settings.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="safeMode">The safe mode to use with this database.</param>
        /// <returns>A new or existing instance of MongoDatabase.</returns>
        MongoDatabase GetDatabase(string databaseName, SafeMode safeMode);

        /// <summary>
        /// Gets the names of the databases on this server.
        /// </summary>
        /// <returns>A list of database names.</returns>
        IEnumerable<string> GetDatabaseNames();

        /// <summary>
        /// Gets the names of the databases on this server.
        /// </summary>
        /// <param name="adminCredentials">Credentials for the admin database.</param>
        /// <returns>A list of database names.</returns>
        IEnumerable<string> GetDatabaseNames(MongoCredentials adminCredentials);

        /// <summary>
        /// Gets the last error (if any) that occurred on this connection. You MUST be within a RequestStart to call this method.
        /// </summary>
        /// <returns>The last error (<see cref=" GetLastErrorResult"/>)</returns>
        GetLastErrorResult GetLastError();

        /// <summary>
        /// Gets the last error (if any) that occurred on this connection. You MUST be within a RequestStart to call this method.
        /// </summary>
        /// <param name="adminCredentials">Credentials for the admin database.</param>
        /// <returns>The last error (<see cref=" GetLastErrorResult"/>)</returns>
        GetLastErrorResult GetLastError(MongoCredentials adminCredentials);

        /// <summary>
        /// Checks whether a given database name is valid on this server.
        /// </summary>
        /// <param name="databaseName">The database name.</param>
        /// <param name="message">An error message if the database name is not valid.</param>
        /// <returns>True if the database name is valid; otherwise, false.</returns>
        bool IsDatabaseNameValid(string databaseName, out string message);

        /// <summary>
        /// Checks whether the server is alive (throws an exception if not). If server is a replica set, pings all members one at a time.
        /// </summary>
        void Ping();

        /// <summary>
        /// Reconnects to the server. Normally there is no need to call this method. All connections
        /// are closed and new connections will be opened as needed. Calling
        /// this method frequently will result in connection thrashing.
        /// </summary>
        void Reconnect();

        /// <summary>
        /// Lets the server know that this thread is done with a series of related operations. Instead of calling this method it is better
        /// to put the return value of RequestStart in a using statement.
        /// </summary>
        void RequestDone();

        /// <summary>
        /// Lets the server know that this thread is about to begin a series of related operations that must all occur
        /// on the same connection. The return value of this method implements IDisposable and can be placed in a
        /// using statement (in which case RequestDone will be called automatically when leaving the using statement).
        /// </summary>
        /// <param name="initialDatabase">One of the databases involved in the related operations.</param>
        /// <returns>A helper object that implements IDisposable and calls <see cref="MongoServer.RequestDone"/> from the Dispose method.</returns>
        IDisposable RequestStart(MongoDatabase initialDatabase);

        /// <summary>
        /// Lets the server know that this thread is about to begin a series of related operations that must all occur
        /// on the same connection. The return value of this method implements IDisposable and can be placed in a
        /// using statement (in which case RequestDone will be called automatically when leaving the using statement).
        /// </summary>
        /// <param name="initialDatabase">One of the databases involved in the related operations.</param>
        /// <param name="slaveOk">Whether queries should be sent to secondary servers.</param>
        /// <returns>A helper object that implements IDisposable and calls <see cref="MongoServer.RequestDone"/> from the Dispose method.</returns>
        IDisposable RequestStart(MongoDatabase initialDatabase, bool slaveOk);

        /// <summary>
        /// Lets the server know that this thread is about to begin a series of related operations that must all occur
        /// on the same connection. The return value of this method implements IDisposable and can be placed in a
        /// using statement (in which case RequestDone will be called automatically when leaving the using statement).
        /// </summary>
        /// <param name="initialDatabase">One of the databases involved in the related operations.</param>
        /// <param name="serverInstance">The server instance this request should be tied to.</param>
        /// <returns>A helper object that implements IDisposable and calls <see cref="MongoServer.RequestDone"/> from the Dispose method.</returns>
        IDisposable RequestStart(MongoDatabase initialDatabase, MongoServerInstance serverInstance);

        /// <summary>
        /// Removes all entries in the index cache used by EnsureIndex. Call this method
        /// when you know (or suspect) that a process other than this one may have dropped one or
        /// more indexes.
        /// </summary>
        void ResetIndexCache();

        /// <summary>
        /// Shuts down the server.
        /// </summary>
        void Shutdown();

        /// <summary>
        /// Shuts down the server.
        /// </summary>
        /// <param name="adminCredentials">Credentials for the admin database.</param>
        void Shutdown(MongoCredentials adminCredentials);

        /// <summary>
        /// Verifies the state of the server (in the case of a replica set all members are contacted one at a time).
        /// </summary>
        void VerifyState();
    }
}
