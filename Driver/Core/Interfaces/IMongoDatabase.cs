using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver.GridFS;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a MongoDB database and the settings used to access it. This class is thread-safe.
    /// </summary>
    public interface IMongoDatabase
    {
        /// <summary>
        /// Gets the command collection for this database.
        /// </summary>
        MongoCollection<BsonDocument> CommandCollection { get; }

        /// <summary>
        /// Gets the credentials being used to access this database.
        /// </summary>
        MongoCredentials Credentials { get; }

        /// <summary>
        /// Gets the default GridFS instance for this database. The default GridFS instance uses default GridFS
        /// settings. See also GetGridFS if you need to use GridFS with custom settings.
        /// </summary>
        MongoGridFS GridFS { get; }

        /// <summary>
        /// Gets the name of this database.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the server that contains this database.
        /// </summary>
        IMongoServer Server { get; }

        /// <summary>
        /// Gets the settings being used to access this database.
        /// </summary>
        MongoDatabaseSettings Settings { get; }

        /// <summary>
        /// Gets a MongoCollection instance representing a collection on this database
        /// with a default document type of BsonDocument.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <returns>An instance of MongoCollection.</returns>
        MongoCollection<BsonDocument> this[string collectionName] { get; }

        /// <summary>
        /// Gets a MongoCollection instance representing a collection on this database
        /// with a default document type of BsonDocument.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="safeMode">The safe mode to use when accessing this collection.</param>
        /// <returns>An instance of MongoCollection.</returns>
        MongoCollection<BsonDocument> this[string collectionName, SafeMode safeMode] { get; }

        /// <summary>
        /// Adds a user to this database.
        /// </summary>
        /// <param name="credentials">The user's credentials.</param>
        void AddUser(MongoCredentials credentials);

        /// <summary>
        /// Adds a user to this database.
        /// </summary>
        /// <param name="credentials">The user's credentials.</param>
        /// <param name="readOnly">True if the user is a read-only user.</param>
        void AddUser(MongoCredentials credentials, bool readOnly);

        /// <summary>
        /// Adds a user to this database.
        /// </summary>
        /// <param name="user">The user.</param>
        void AddUser(MongoUser user);

        /// <summary>
        /// Tests whether a collection exists on this database.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <returns>True if the collection exists.</returns>
        bool CollectionExists(string collectionName);

        /// <summary>
        /// Creates a collection. MongoDB creates collections automatically when they are first used, so
        /// this command is mainly here for frameworks.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <returns>A CommandResult.</returns>
        CommandResult CreateCollection(string collectionName);

        /// <summary>
        /// Creates a collection. MongoDB creates collections automatically when they are first used, so
        /// you only need to call this method if you want to provide non-default options.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="options">Options for creating this collection (usually a CollectionOptionsDocument or constructed using the CollectionOptions builder).</param>
        /// <returns>A CommandResult.</returns>
        CommandResult CreateCollection(string collectionName, IMongoCollectionOptions options);

        /// <summary>
        /// Creates an instance of MongoCollectionSettings for the named collection with the rest of the settings inherited.
        /// You can override some of these settings before calling GetCollection.
        /// </summary>
        /// <typeparam name="TDefaultDocument">The default document type for this collection.</typeparam>
        /// <param name="collectionName">The name of this collection.</param>
        /// <returns>A MongoCollectionSettings.</returns>
        MongoCollectionSettings<TDefaultDocument> CreateCollectionSettings<TDefaultDocument>(
            string collectionName);

        /// <summary>
        /// Creates an instance of MongoCollectionSettings for the named collection with the rest of the settings inherited.
        /// You can override some of these settings before calling GetCollection.
        /// </summary>
        /// <param name="defaultDocumentType">The default document type for this collection.</param>
        /// <param name="collectionName">The name of this collection.</param>
        /// <returns>A MongoCollectionSettings.</returns>
        MongoCollectionSettings CreateCollectionSettings(
            Type defaultDocumentType,
            string collectionName);

        /// <summary>
        /// Drops a database.
        /// </summary>
        void Drop();

        /// <summary>
        /// Drops a collection.
        /// </summary>
        /// <param name="collectionName">The name of the collection to drop.</param>
        /// <returns>A CommandResult.</returns>
        CommandResult DropCollection(string collectionName);

        /// <summary>
        /// Evaluates JavaScript code at the server.
        /// </summary>
        /// <param name="flags">Flags that control Eval options.</param>
        /// <param name="code">The code to evaluate.</param>
        /// <param name="args">Optional arguments (only used when the code is a function with parameters).</param>
        /// <returns>The result of evaluating the code.</returns>
        BsonValue Eval(EvalFlags flags, BsonJavaScript code, params object[] args);

        /// <summary>
        /// Evaluates JavaScript code at the server.
        /// </summary>
        /// <param name="code">The code to evaluate.</param>
        /// <param name="args">Optional arguments (only used when the code is a function with parameters).</param>
        /// <returns>The result of evaluating the code.</returns>
        BsonValue Eval(BsonJavaScript code, params object[] args);

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
        /// <returns>An instance of nominalType (or null if the document was not found).</returns>
        object FetchDBRefAs(Type documentType, MongoDBRef dbRef);

        /// <summary>
        /// Finds all users of this database.
        /// </summary>
        /// <returns>An array of users.</returns>
        MongoUser[] FindAllUsers();

        /// <summary>
        /// Finds a user of this database.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns>The user.</returns>
        MongoUser FindUser(string username);

        /// <summary>
        /// Gets a MongoCollection instance representing a collection on this database
        /// with a default document type of TDefaultDocument.
        /// </summary>
        /// <typeparam name="TDefaultDocument">The default document type for this collection.</typeparam>
        /// <param name="collectionSettings">The settings to use when accessing this collection.</param>
        /// <returns>An instance of MongoCollection.</returns>
        MongoCollection<TDefaultDocument> GetCollection<TDefaultDocument>(
            MongoCollectionSettings<TDefaultDocument> collectionSettings);

        /// <summary>
        /// Gets a MongoCollection instance representing a collection on this database
        /// with a default document type of TDefaultDocument.
        /// </summary>
        /// <typeparam name="TDefaultDocument">The default document type for this collection.</typeparam>
        /// <param name="collectionName">The name of the collection.</param>
        /// <returns>An instance of MongoCollection.</returns>
        MongoCollection<TDefaultDocument> GetCollection<TDefaultDocument>(string collectionName);

        /// <summary>
        /// Gets a MongoCollection instance representing a collection on this database
        /// with a default document type of TDefaultDocument.
        /// </summary>
        /// <typeparam name="TDefaultDocument">The default document type for this collection.</typeparam>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="safeMode">The safe mode to use when accessing this collection.</param>
        /// <returns>An instance of MongoCollection.</returns>
        MongoCollection<TDefaultDocument> GetCollection<TDefaultDocument>(
            string collectionName,
            SafeMode safeMode);

        /// <summary>
        /// Gets a MongoCollection instance representing a collection on this database
        /// with a default document type of TDefaultDocument.
        /// </summary>
        /// <param name="collectionSettings">The settings to use when accessing this collection.</param>
        /// <returns>An instance of MongoCollection.</returns>
        MongoCollection GetCollection(MongoCollectionSettings collectionSettings);

        /// <summary>
        /// Gets a MongoCollection instance representing a collection on this database
        /// with a default document type of BsonDocument.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <returns>An instance of MongoCollection.</returns>
        MongoCollection<BsonDocument> GetCollection(string collectionName);

        /// <summary>
        /// Gets a MongoCollection instance representing a collection on this database
        /// with a default document type of BsonDocument.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="safeMode">The safe mode to use when accessing this collection.</param>
        /// <returns>An instance of MongoCollection.</returns>
        MongoCollection<BsonDocument> GetCollection(string collectionName, SafeMode safeMode);

        /// <summary>
        /// Gets a MongoCollection instance representing a collection on this database
        /// with a default document type of BsonDocument.
        /// </summary>
        /// <param name="defaultDocumentType">The default document type.</param>
        /// <param name="collectionName">The name of the collection.</param>
        /// <returns>An instance of MongoCollection.</returns>
        MongoCollection GetCollection(Type defaultDocumentType, string collectionName);

        /// <summary>
        /// Gets a MongoCollection instance representing a collection on this database
        /// with a default document type of BsonDocument.
        /// </summary>
        /// <param name="defaultDocumentType">The default document type.</param>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="safeMode">The safe mode to use when accessing this collection.</param>
        /// <returns>An instance of MongoCollection.</returns>
        MongoCollection GetCollection(
            Type defaultDocumentType,
            string collectionName,
            SafeMode safeMode);

        /// <summary>
        /// Gets a list of the names of all the collections in this database.
        /// </summary>
        /// <returns>A list of collection names.</returns>
        IEnumerable<string> GetCollectionNames();

        /// <summary>
        /// Gets the current operation.
        /// </summary>
        /// <returns>The current operation.</returns>
        BsonDocument GetCurrentOp();

        /// <summary>
        /// Gets an instance of MongoGridFS for this database using custom GridFS settings.
        /// </summary>
        /// <param name="gridFSSettings">The GridFS settings to use.</param>
        /// <returns>An instance of MongoGridFS.</returns>
        MongoGridFS GetGridFS(MongoGridFSSettings gridFSSettings);

        /// <summary>
        /// Gets the last error (if any) that occurred on this connection. You MUST be within a RequestStart to call this method.
        /// </summary>
        /// <returns>The last error (<see cref=" GetLastErrorResult"/>)</returns>
        GetLastErrorResult GetLastError();

        /// <summary>
        /// Gets one or more documents from the system.profile collection.
        /// </summary>
        /// <param name="query">A query to select which documents to return.</param>
        /// <returns>A cursor.</returns>
        IMongoCursor<SystemProfileInfo> GetProfilingInfo(IMongoQuery query);

        /// <summary>
        /// Gets the current profiling level.
        /// </summary>
        /// <returns>The profiling level.</returns>
        GetProfilingLevelResult GetProfilingLevel();

        /// <summary>
        /// Gets a sister database on the same server.
        /// </summary>
        /// <param name="databaseName">The name of the sister database.</param>
        /// <returns>An instance of MongoDatabase.</returns>
        IMongoDatabase GetSisterDatabase(string databaseName);

        /// <summary>
        /// Gets the current database stats.
        /// </summary>
        /// <returns>An instance of DatabaseStatsResult.</returns>
        DatabaseStatsResult GetStats();

        /// <summary>
        /// Checks whether a given collection name is valid in this database.
        /// </summary>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="message">An error message if the collection name is not valid.</param>
        /// <returns>True if the collection name is valid; otherwise, false.</returns>
        bool IsCollectionNameValid(string collectionName, out string message);

        /// <summary>
        /// Removes a user from this database.
        /// </summary>
        /// <param name="user">The user to remove.</param>
        void RemoveUser(MongoUser user);

        /// <summary>
        /// Removes a user from this database.
        /// </summary>
        /// <param name="username">The username to remove.</param>
        void RemoveUser(string username);

        /// <summary>
        /// Renames a collection on this database.
        /// </summary>
        /// <param name="oldCollectionName">The old name for the collection.</param>
        /// <param name="newCollectionName">The new name for the collection.</param>
        /// <returns>A CommandResult.</returns>
        CommandResult RenameCollection(string oldCollectionName, string newCollectionName);

        /// <summary>
        /// Renames a collection on this database.
        /// </summary>
        /// <param name="oldCollectionName">The old name for the collection.</param>
        /// <param name="newCollectionName">The new name for the collection.</param>
        /// <param name="dropTarget">Whether to drop the target collection first if it already exists.</param>
        /// <returns>A CommandResult.</returns>
        CommandResult RenameCollection(string oldCollectionName, string newCollectionName, bool dropTarget);

        /// <summary>
        /// Renames a collection on this database.
        /// </summary>
        /// <param name="oldCollectionName">The old name for the collection.</param>
        /// <param name="newCollectionName">The new name for the collection.</param>
        /// <param name="dropTarget">Whether to drop the target collection first if it already exists.</param>
        /// <param name="adminCredentials">Credentials for the admin database.</param>
        /// <returns>A CommandResult.</returns>
        CommandResult RenameCollection(
            string oldCollectionName,
            string newCollectionName,
            bool dropTarget,
            MongoCredentials adminCredentials);

        /// <summary>
        /// Renames a collection on this database.
        /// </summary>
        /// <param name="oldCollectionName">The old name for the collection.</param>
        /// <param name="newCollectionName">The new name for the collection.</param>
        /// <param name="adminCredentials">Credentials for the admin database.</param>
        /// <returns>A CommandResult.</returns>
        CommandResult RenameCollection(string oldCollectionName, string newCollectionName, MongoCredentials adminCredentials);

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
        /// <returns>A helper object that implements IDisposable and calls <see cref="MongoDatabase.RequestDone"/> from the Dispose method.</returns>
        IDisposable RequestStart();

        /// <summary>
        /// Lets the server know that this thread is about to begin a series of related operations that must all occur
        /// on the same connection. The return value of this method implements IDisposable and can be placed in a
        /// using statement (in which case RequestDone will be called automatically when leaving the using statement).
        /// </summary>
        /// <param name="slaveOk">Whether queries should be sent to secondary servers.</param>
        /// <returns>A helper object that implements IDisposable and calls <see cref="MongoDatabase.RequestDone"/> from the Dispose method.</returns>
        IDisposable RequestStart(bool slaveOk);

        /// <summary>
        /// Removes all entries for this database in the index cache used by EnsureIndex. Call this method
        /// when you know (or suspect) that a process other than this one may have dropped one or
        /// more indexes.
        /// </summary>
        void ResetIndexCache();

        /// <summary>
        /// Runs a command on this database.
        /// </summary>
        /// <param name="command">The command object.</param>
        /// <returns>A CommandResult</returns>
        CommandResult RunCommand(IMongoCommand command);

        /// <summary>
        /// Runs a command on this database.
        /// </summary>
        /// <param name="commandName">The name of the command.</param>
        /// <returns>A CommandResult</returns>
        CommandResult RunCommand(string commandName);

        /// <summary>
        /// Runs a command on this database and returns the result as a TCommandResult.
        /// </summary>
        /// <typeparam name="TCommandResult">The type of the returned command result.</typeparam>
        /// <param name="command">The command object.</param>
        /// <returns>A TCommandResult</returns>
        TCommandResult RunCommandAs<TCommandResult>(IMongoCommand command)
            where TCommandResult : CommandResult, new();

        /// <summary>
        /// Runs a command on this database and returns the result as a TCommandResult.
        /// </summary>
        /// <typeparam name="TCommandResult">The type of the returned command result.</typeparam>
        /// <param name="commandName">The name of the command.</param>
        /// <returns>A TCommandResult</returns>
        TCommandResult RunCommandAs<TCommandResult>(string commandName)
            where TCommandResult : CommandResult, new();

        /// <summary>
        /// Runs a command on this database and returns the result as a TCommandResult.
        /// </summary>
        /// <param name="commandResultType">The command result type.</param>
        /// <param name="command">The command object.</param>
        /// <returns>A TCommandResult</returns>
        CommandResult RunCommandAs(Type commandResultType, IMongoCommand command);

        /// <summary>
        /// Runs a command on this database and returns the result as a TCommandResult.
        /// </summary>
        /// <param name="commandResultType">The command result type.</param>
        /// <param name="commandName">The name of the command.</param>
        /// <returns>A TCommandResult</returns>
        CommandResult RunCommandAs(Type commandResultType, string commandName);

        /// <summary>
        /// Sets the level of profile information to write.
        /// </summary>
        /// <param name="level">The profiling level.</param>
        /// <returns>A CommandResult.</returns>
        CommandResult SetProfilingLevel(ProfilingLevel level);

        /// <summary>
        /// Sets the level of profile information to write.
        /// </summary>
        /// <param name="level">The profiling level.</param>
        /// <param name="slow">The threshold that defines a slow query.</param>
        /// <returns>A CommandResult.</returns>
        CommandResult SetProfilingLevel(ProfilingLevel level, TimeSpan slow);

        /// <summary>
        /// Gets a canonical string representation for this database.
        /// </summary>
        /// <returns>A canonical string representation for this database.</returns>
        string ToString();
    }
}