﻿/* Copyright 2010-2011 10gen Inc.
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

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Builders;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver {
    /// <summary>
    /// Represents a MongoDB database and the settings used to access it. This class is thread-safe.
    /// </summary>
    public class MongoDatabase {
        #region private fields
        private object databaseLock = new object();
        private MongoServer server;
        private MongoDatabaseSettings settings;
        private string name;
        private Dictionary<MongoCollectionSettings, MongoCollection> collections = new Dictionary<MongoCollectionSettings, MongoCollection>();
        private MongoCollection<BsonDocument> commandCollection;
        private MongoGridFS gridFS;
        #endregion

        #region constructors
        /// <summary>
        /// Creates a new instance of MongoDatabase. Normally you would call one of the indexers or GetDatabase methods
        /// of MongoServer instead.
        /// </summary>
        /// <param name="server">The server that contains this database.</param>
        /// <param name="settings">The settings to use to access this database.</param>
        public MongoDatabase(
            MongoServer server,
            MongoDatabaseSettings settings
        ) {
            ValidateDatabaseName(settings.DatabaseName);
            this.server = server;
            this.settings = settings.Freeze();
            this.name = settings.DatabaseName;

            // make sure commands get routed to the primary server by using slaveOk false
            var commandCollectionSettings = CreateCollectionSettings<BsonDocument>("$cmd");
            commandCollectionSettings.AssignIdOnInsert = false;
            commandCollectionSettings.SlaveOk = false;
            commandCollection = GetCollection(commandCollectionSettings);
        }
        #endregion

        #region factory methods
        /// <summary>
        /// Creates a new instance or returns an existing instance of MongoDatabase. Only one instance
        /// is created for each combination of database settings. Automatically creates an instance
        /// of MongoServer if needed.
        /// </summary>
        /// <param name="builder">Server and database settings in the form of a MongoConnectionStringBuilder.</param>
        /// <returns>
        /// A new or existing instance of MongoDatabase.
        /// </returns>
        public static MongoDatabase Create(
            MongoConnectionStringBuilder builder
        ) {
            var serverSettings = builder.ToServerSettings();
            var databaseName = builder.DatabaseName;
            return Create(serverSettings, databaseName);
        }

        /// <summary>
        /// Creates a new instance or returns an existing instance of MongoDatabase. Only one instance
        /// is created for each combination of database settings. Automatically creates an instance
        /// of MongoServer if needed.
        /// </summary>
        /// <param name="serverSettings">The server settings for the server that contains this database.</param>
        /// <param name="databaseName">The name of this database (will be accessed using default settings).</param>
        /// <returns>
        /// A new or existing instance of MongoDatabase.
        /// </returns>
        public static MongoDatabase Create(
            MongoServerSettings serverSettings,
            string databaseName
        ) {
            if (databaseName == null) {
                throw new ArgumentException("Database name is missing");
            }
            var server = MongoServer.Create(serverSettings);
            return server.GetDatabase(databaseName);
        }

        /// <summary>
        /// Creates a new instance or returns an existing instance of MongoDatabase. Only one instance
        /// is created for each combination of database settings. Automatically creates an instance
        /// of MongoServer if needed.
        /// </summary>
        /// <param name="url">Server and database settings in the form of a MongoUrl.</param>
        /// <returns>
        /// A new or existing instance of MongoDatabase.
        /// </returns>
        public static MongoDatabase Create(
            MongoUrl url
        ) {
            var serverSettings = url.ToServerSettings();
            var databaseName = url.DatabaseName;
            return Create(serverSettings, databaseName);
        }

        /// <summary>
        /// Creates a new instance or returns an existing instance of MongoDatabase. Only one instance
        /// is created for each combination of database settings. Automatically creates an instance
        /// of MongoServer if needed.
        /// </summary>
        /// <param name="connectionString">Server and database settings in the form of a connection string.</param>
        /// <returns>
        /// A new or existing instance of MongoDatabase.
        /// </returns>
        public static MongoDatabase Create(
            string connectionString
        ) {
            if (connectionString.StartsWith("mongodb://")) {
                MongoUrl url = MongoUrl.Create(connectionString);
                return Create(url);
            } else {
                MongoConnectionStringBuilder builder = new MongoConnectionStringBuilder(connectionString);
                return Create(builder);
            }
        }

        /// <summary>
        /// Creates a new instance or returns an existing instance of MongoDatabase. Only one instance
        /// is created for each combination of database settings. Automatically creates an instance
        /// of MongoServer if needed.
        /// </summary>
        /// <param name="uri">Server and database settings in the form of a Uri.</param>
        /// <returns>
        /// A new or existing instance of MongoDatabase.
        /// </returns>
        public static MongoDatabase Create(
            Uri uri
        ) {
            return Create(MongoUrl.Create(uri.ToString()));
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the command collection for this database.
        /// </summary>
        public virtual MongoCollection<BsonDocument> CommandCollection {
            get { return commandCollection; }
        }

        /// <summary>
        /// Gets the credentials being used to access this database.
        /// </summary>
        public virtual MongoCredentials Credentials {
            get { return settings.Credentials; }
        }

        /// <summary>
        /// Gets the default GridFS instance for this database. The default GridFS instance uses default GridFS
        /// settings. See also GetGridFS if you need to use GridFS with custom settings.
        /// </summary>
        public virtual MongoGridFS GridFS {
            get {
                lock (databaseLock) {
                    if (gridFS == null) {
                        gridFS = new MongoGridFS(this);
                    }
                    return gridFS;
                }
            }
        }

        /// <summary>
        /// Gets the name of this database.
        /// </summary>
        public virtual string Name {
            get { return name; }
        }

        /// <summary>
        /// Gets the server that contains this database.
        /// </summary>
        public virtual MongoServer Server {
            get { return server; }
        }

        /// <summary>
        /// Gets the settings being used to access this database.
        /// </summary>
        public virtual MongoDatabaseSettings Settings {
            get { return settings; }
        }
        #endregion

        #region public indexers
        /// <summary>
        /// Gets a MongoCollection instance representing a collection on this database
        /// with a default document type of BsonDocument.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <returns>An instance of MongoCollection.</returns>
        public virtual MongoCollection<BsonDocument> this[
            string collectionName
        ] {
            get { return GetCollection(collectionName); }
        }

        /// <summary>
        /// Gets a MongoCollection instance representing a collection on this database
        /// with a default document type of BsonDocument.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="safeMode">The safe mode to use when accessing this collection.</param>
        /// <returns>An instance of MongoCollection.</returns>
        public virtual MongoCollection<BsonDocument> this[
            string collectionName,
            SafeMode safeMode
        ] {
            get { return GetCollection(collectionName, safeMode); }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Adds a user to this database.
        /// </summary>
        /// <param name="credentials">The user's credentials.</param>
        public virtual void AddUser(
            MongoCredentials credentials
        ) {
            AddUser(credentials, false);
        }

        /// <summary>
        /// Adds a user to this database.
        /// </summary>
        /// <param name="credentials">The user's credentials.</param>
        /// <param name="readOnly">True if the user is a read-only user.</param>
        public virtual void AddUser(
            MongoCredentials credentials,
            bool readOnly
        ) {
            var users = GetCollection("system.users");
            var user = users.FindOne(Query.EQ("user", credentials.Username));
            if (user == null) {
                user = new BsonDocument("user", credentials.Username);
            }
            user["readOnly"] = readOnly;
            user["pwd"] = MongoUtils.Hash(credentials.Username + ":mongo:" + credentials.Password);
            users.Save(user);
        }

        /// <summary>
        /// Tests whether a collection exists on this database.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <returns>True if the collection exists.</returns>
        public virtual bool CollectionExists(
            string collectionName
        ) {
            return GetCollectionNames().Contains(collectionName);
        }

        /// <summary>
        /// Creates a collection. MongoDB creates collections automatically when they are first used, so
        /// you only need to call this method if you want to provide non-default options.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="options">Options for creating this collection (usually a CollectionOptionsDocument or constructed using the CollectionOptions builder).</param>
        /// <returns>A CommandResult.</returns>
        public virtual CommandResult CreateCollection(
            string collectionName,
            IMongoCollectionOptions options
        ) {
            var command = new CommandDocument("create", collectionName);
            if (options != null) {
                command.Merge(options.ToBsonDocument());
            }
            return RunCommand(command);
        }

        /// <summary>
        /// Creates an instance of MongoCollectionSettings for the named collection with the rest of the settings inherited.
        /// You can override some of these settings before calling GetCollection.
        /// </summary>
        /// <typeparam name="TDefaultDocument">The default document type for this collection.</typeparam>
        /// <param name="collectionName">The name of this collection.</param>
        /// <returns>A MongoCollectionSettings.</returns>
        public virtual MongoCollectionSettings<TDefaultDocument> CreateCollectionSettings<TDefaultDocument>(
            string collectionName
        ) {
            return new MongoCollectionSettings<TDefaultDocument>(
                collectionName,
                MongoDefaults.AssignIdOnInsert,
                settings.SafeMode,
                settings.SlaveOk
            );
        }

        /// <summary>
        /// Drops a database.
        /// </summary>
        public virtual void Drop() {
            server.DropDatabase(name);
        }

        /// <summary>
        /// Drops a collection.
        /// </summary>
        /// <param name="collectionName">The name of the collection to drop.</param>
        /// <returns>A CommandResult.</returns>
        public virtual CommandResult DropCollection(
            string collectionName
        ) {
            var command = new CommandDocument("drop", collectionName);
            return RunCommand(command);
        }

        /// <summary>
        /// Evaluates JavaScript code at the server.
        /// </summary>
        /// <param name="code">The code to evaluate.</param>
        /// <param name="args">Optional arguments (only used when the code is a function with parameters).</param>
        /// <returns>The result of evaluating the code.</returns>
        public virtual BsonValue Eval(
            string code,
            params object[] args
        ) {
            var command = new CommandDocument {
                { "$eval", code },
                { "args", new BsonArray(args) }
            };
            var result = RunCommand(command);
            return result.Response["retval"];
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
            if (dbRef.DatabaseName != null && dbRef.DatabaseName != name) {
                return server.FetchDBRefAs<TDocument>(dbRef);
            }

            var collection = GetCollection(dbRef.CollectionName);
            var query = Query.EQ("_id", BsonValue.Create(dbRef.Id));
            return collection.FindOneAs<TDocument>(query);
        }

        /// <summary>
        /// Gets a MongoCollection instance representing a collection on this database
        /// with a default document type of TDefaultDocument.
        /// </summary>
        /// <typeparam name="TDefaultDocument">The default document type for this collection.</typeparam>
        /// <param name="collectionSettings">The settings to use when accessing this collection.</param>
        /// <returns>An instance of MongoCollection.</returns>
        public virtual MongoCollection<TDefaultDocument> GetCollection<TDefaultDocument>(
            MongoCollectionSettings<TDefaultDocument> collectionSettings
        ) {
            lock (databaseLock) {
                MongoCollection collection;
                if (!collections.TryGetValue(collectionSettings, out collection)) {
                    collection = new MongoCollection<TDefaultDocument>(this, collectionSettings);
                    collections.Add(collectionSettings, collection);
                }
                return (MongoCollection<TDefaultDocument>) collection;
            }
        }

        /// <summary>
        /// Gets a MongoCollection instance representing a collection on this database
        /// with a default document type of TDefaultDocument.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <returns>An instance of MongoCollection.</returns>
        public virtual MongoCollection<TDefaultDocument> GetCollection<TDefaultDocument>(
            string collectionName
        ) {
            var collectionSettings = CreateCollectionSettings<TDefaultDocument>(collectionName);
            return GetCollection(collectionSettings);
        }

        /// <summary>
        /// Gets a MongoCollection instance representing a collection on this database
        /// with a default document type of TDefaultDocument.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="safeMode">The safe mode to use when accessing this collection.</param>
        /// <returns>An instance of MongoCollection.</returns>
        public virtual MongoCollection<TDefaultDocument> GetCollection<TDefaultDocument>(
            string collectionName,
            SafeMode safeMode
        ) {
            var collectionSettings = CreateCollectionSettings<TDefaultDocument>(collectionName);
            collectionSettings.SafeMode = safeMode;
            return GetCollection(collectionSettings);
        }

        /// <summary>
        /// Gets a MongoCollection instance representing a collection on this database
        /// with a default document type of BsonDocument.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <returns>An instance of MongoCollection.</returns>
        public virtual MongoCollection<BsonDocument> GetCollection(
            string collectionName
        ) {
            var collectionSettings = CreateCollectionSettings<BsonDocument>(collectionName);
            return GetCollection(collectionSettings);
        }

        /// <summary>
        /// Gets a MongoCollection instance representing a collection on this database
        /// with a default document type of BsonDocument.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="safeMode">The safe mode to use when accessing this collection.</param>
        /// <returns>An instance of MongoCollection.</returns>
        public virtual MongoCollection<BsonDocument> GetCollection(
            string collectionName,
            SafeMode safeMode
        ) {
            var collectionSettings = CreateCollectionSettings<BsonDocument>(collectionName);
            collectionSettings.SafeMode = safeMode;
            return GetCollection(collectionSettings);
        }

        /// <summary>
        /// Gets a list of the names of all the collections in this database.
        /// </summary>
        /// <returns>A list of collection names.</returns>
        public virtual IEnumerable<string> GetCollectionNames() {
            List<string> collectionNames = new List<string>();
            var namespaces = GetCollection("system.namespaces");
            var prefix = name + ".";
            foreach (var @namespace in namespaces.FindAll()) {
                string collectionName = @namespace["name"].AsString;
                if (!collectionName.StartsWith(prefix)) { continue; }
                if (collectionName.Contains('$')) { continue; }
                collectionNames.Add(collectionName.Substring(prefix.Length));
            }
            collectionNames.Sort();
            return collectionNames;
        }

        /// <summary>
        /// Gets the current operation.
        /// </summary>
        /// <returns>The current operation.</returns>
        public virtual BsonDocument GetCurrentOp() {
            var collection = GetCollection("$cmd.sys.inprog");
            return collection.FindOne();
        }

        /// <summary>
        /// Gets an instance of MongoGridFS for this database using custom GridFS settings.
        /// </summary>
        /// <param name="gridFSSettings">The GridFS settings to use.</param>
        /// <returns>An instance of MongoGridFS.</returns>
        public virtual MongoGridFS GetGridFS(
            MongoGridFSSettings gridFSSettings
        ) {
            return new MongoGridFS(this, gridFSSettings);
        }

        // TODO: mongo shell has GetPrevError at the database level?
        // TODO: mongo shell has GetProfilingLevel at the database level?
        // TODO: mongo shell has GetReplicationInfo at the database level?

        /// <summary>
        /// Gets a sister database on the same server.
        /// </summary>
        /// <param name="databaseName">The name of the sister database.</param>
        /// <returns>An instance of MongoDatabase.</returns>
        public virtual MongoDatabase GetSisterDatabase(
            string databaseName
        ) {
            return server.GetDatabase(databaseName);
        }

        /// <summary>
        /// Gets the current database stats.
        /// </summary>
        /// <returns>An instance of DatabaseStatsResult.</returns>
        public virtual DatabaseStatsResult GetStats() {
            return RunCommandAs<DatabaseStatsResult>("dbstats");
        }

        // TODO: mongo shell has IsMaster at database level?

        /// <summary>
        /// Removes a user from this database.
        /// </summary>
        /// <param name="username">The username to remove.</param>
        public virtual void RemoveUser(
            string username
        ) {
            var users = GetCollection("system.users");
            users.Remove(Query.EQ("user", username));
        }

        /// <summary>
        /// Renames a collection on this database.
        /// </summary>
        /// <param name="oldCollectionName">The old name for the collection.</param>
        /// <param name="newCollectionName">The new name for the collection.</param>
        /// <returns>A CommandResult.</returns>
        public virtual CommandResult RenameCollection(
            string oldCollectionName,
            string newCollectionName
        ) {
            var command = new CommandDocument {
                { "renameCollection", string.Format("{0}.{1}", name, oldCollectionName) },
                { "to", string.Format("{0}.{1}", name, newCollectionName) }
            };
            return server.RunAdminCommand(command);
        }

        /// <summary>
        /// Lets the server know that this thread is done with a series of related operations. Instead of calling this method it is better
        /// to put the return value of RequestStart in a using statement.
        /// </summary>
        public virtual void RequestDone() {
            server.RequestDone();
        }

        /// <summary>
        /// Lets the server know that this thread is about to begin a series of related operations that must all occur
        /// on the same connection. The return value of this method implements IDisposable and can be placed in a
        /// using statement (in which case RequestDone will be called automatically when leaving the using statement).
        /// </summary>
        /// <returns>A helper object that implements IDisposable and calls <see cref="RequestDone"/> from the Dispose method.</returns>
        public virtual IDisposable RequestStart() {
            return server.RequestStart(this);
        }

        // TODO: mongo shell has ResetError at the database level

        /// <summary>
        /// Removes all entries for this database in the index cache used by EnsureIndex. Call this method
        /// when you know (or suspect) that a process other than this one may have dropped one or
        /// more indexes.
        /// </summary>
        public virtual void ResetIndexCache() {
            server.IndexCache.Reset(this);
        }

        /// <summary>
        /// Runs a command on this database.
        /// </summary>
        /// <param name="command">The command object.</param>
        /// <returns>A CommandResult</returns>
        public virtual CommandResult RunCommand(
            IMongoCommand command
        ) {
            return RunCommandAs<CommandResult>(command);
        }

        /// <summary>
        /// Runs a command on this database.
        /// </summary>
        /// <param name="commandName">The name of the command.</param>
        /// <returns>A CommandResult</returns>
        public virtual CommandResult RunCommand(
            string commandName
        ) {
            return RunCommandAs<CommandResult>(commandName);
        }

        /// <summary>
        /// Runs a command on this database and returns the result as a TCommandResult.
        /// </summary>
        /// <param name="command">The command object.</param>
        /// <returns>A TCommandResult</returns>
        public virtual TCommandResult RunCommandAs<TCommandResult>(
            IMongoCommand command
        ) where TCommandResult : CommandResult, new() {
            var response = CommandCollection.FindOne(command);
            if (response == null) {
                var commandName = command.ToBsonDocument().GetElement(0).Name;
                var message = string.Format("Command '{0}' failed: no response returned", commandName);
                throw new MongoCommandException(message);
            }
            var commandResult = new TCommandResult(); // generic type constructor can't have arguments
            commandResult.Initialize(command, response); // so two phase construction required
            if (!commandResult.Ok) {
                if (commandResult.ErrorMessage == "not master") {
                    server.Disconnect();
                }
                throw new MongoCommandException(commandResult);
            }
            return commandResult;
        }

        /// <summary>
        /// Runs a command on this database and returns the result as a TCommandResult.
        /// </summary>
        /// <param name="commandName">The name of the command.</param>
        /// <returns>A TCommandResult</returns>
        public virtual TCommandResult RunCommandAs<TCommandResult>(
            string commandName
        ) where TCommandResult : CommandResult, new() {
            var command = new CommandDocument(commandName, true);
            return RunCommandAs<TCommandResult>(command);
        }

        /// <summary>
        /// Gets a canonical string representation for this database.
        /// </summary>
        /// <returns>A canonical string representation for this database.</returns>
        public override string ToString() {
            return name;
        }
        #endregion

        #region private methods
        private void ValidateDatabaseName(
            string name
        ) {
            if (name == null) {
                throw new ArgumentNullException("name");
            }
            if (name == "") {
                throw new ArgumentException("Database name is empty");
            }
            if (name.IndexOfAny(new char[] { '\0', ' ', '.', '$', '/', '\\' }) != -1) {
                throw new ArgumentException("Database name cannot contain the following special characters: null, space, period, $, / or \\");
            }
            if (Encoding.UTF8.GetBytes(name).Length > 64) {
                throw new ArgumentException("Database name cannot exceed 64 bytes (after encoding to UTF8)");
            }
        }
        #endregion
    }
}
