/* Copyright 2010-2013 10gen Inc.
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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Builders;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.Operations;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a MongoDB database and the settings used to access it. This class is thread-safe.
    /// </summary>
    public class MongoDatabase
    {
        // private fields
        private MongoServer _server;
        private MongoDatabaseSettings _settings;
        private string _name;

        // constructors
        /// <summary>
        /// Creates a new instance of MongoDatabase. Normally you would call one of the indexers or GetDatabase methods
        /// of MongoServer instead.
        /// </summary>
        /// <param name="server">The server that contains this database.</param>
        /// <param name="settings">The settings to use to access this database.</param>
        [Obsolete("Use MongoDatabase(MongoServer server, string name, MongoDatabaseSettings settings) instead.")]
        public MongoDatabase(MongoServer server, MongoDatabaseSettings settings)
            : this(server, settings.DatabaseName, settings)
        {
        }

        /// <summary>
        /// Creates a new instance of MongoDatabase. Normally you would call one of the indexers or GetDatabase methods
        /// of MongoServer instead.
        /// </summary>
        /// <param name="server">The server that contains this database.</param>
        /// <param name="name">The name of the database.</param>
        /// <param name="settings">The settings to use to access this database.</param>
        public MongoDatabase(MongoServer server, string name, MongoDatabaseSettings settings)
        {
            if (server == null)
            {
                throw new ArgumentNullException("server");
            }
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }
            string message;
            if (!server.IsDatabaseNameValid(name, out message))
            {
                throw new ArgumentOutOfRangeException("name", message);
            }

            settings = settings.Clone();
            settings.ApplyDefaultValues(server.Settings);
            settings.Freeze();

            _server = server;
            _settings = settings;
            _name = name;
        }

        // factory methods
        /// <summary>
        /// Creates a new instance or returns an existing instance of MongoDatabase. Only one instance
        /// is created for each combination of database settings. Automatically creates an instance
        /// of MongoServer if needed.
        /// </summary>
        /// <param name="builder">Server and database settings in the form of a MongoConnectionStringBuilder.</param>
        /// <returns>
        /// A new or existing instance of MongoDatabase.
        /// </returns>
        [Obsolete("Use MongoClient, GetServer and GetDatabase instead.")]
        public static MongoDatabase Create(MongoConnectionStringBuilder builder)
        {
            var serverSettings = MongoServerSettings.FromConnectionStringBuilder(builder);
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
        [Obsolete("Use MongoClient, GetServer and GetDatabase instead.")]
        public static MongoDatabase Create(MongoServerSettings serverSettings, string databaseName)
        {
            if (databaseName == null)
            {
                throw new ArgumentException("Database name is missing.");
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
        [Obsolete("Use MongoClient, GetServer and GetDatabase instead.")]
        public static MongoDatabase Create(MongoUrl url)
        {
            var serverSettings = MongoServerSettings.FromUrl(url);
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
        [Obsolete("Use MongoClient, GetServer and GetDatabase instead.")]
        public static MongoDatabase Create(string connectionString)
        {
            if (connectionString.StartsWith("mongodb://", StringComparison.Ordinal))
            {
                MongoUrl url = MongoUrl.Create(connectionString);
                return Create(url);
            }
            else
            {
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
        [Obsolete("Use MongoClient, GetServer and GetDatabase instead.")]
        public static MongoDatabase Create(Uri uri)
        {
            return Create(MongoUrl.Create(uri.ToString()));
        }

        // public properties
        /// <summary>
        /// Gets the command collection for this database.
        /// </summary>
        [Obsolete("CommandCollection will be removed and there will be no replacement.")]
        public virtual MongoCollection<BsonDocument> CommandCollection
        {
            get
            {
                var commandCollectionSettings = new MongoCollectionSettings { AssignIdOnInsert = false };
                return GetCollection("$cmd", commandCollectionSettings);
            }
        }

        /// <summary>
        /// Gets the default GridFS instance for this database. The default GridFS instance uses default GridFS
        /// settings. See also GetGridFS if you need to use GridFS with custom settings.
        /// </summary>
        public virtual MongoGridFS GridFS
        {
            get { return new MongoGridFS(this); }
        }

        /// <summary>
        /// Gets the name of this database.
        /// </summary>
        public virtual string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Gets the server that contains this database.
        /// </summary>
        public virtual MongoServer Server
        {
            get { return _server; }
        }

        /// <summary>
        /// Gets the settings being used to access this database.
        /// </summary>
        public virtual MongoDatabaseSettings Settings
        {
            get { return _settings; }
        }

        // public indexers
        /// <summary>
        /// Gets a MongoCollection instance representing a collection on this database
        /// with a default document type of BsonDocument.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <returns>An instance of MongoCollection.</returns>
        [Obsolete("Use GetCollection instead.")]
        public virtual MongoCollection<BsonDocument> this[string collectionName]
        {
            get { return GetCollection(collectionName); }
        }

        /// <summary>
        /// Gets a MongoCollection instance representing a collection on this database
        /// with a default document type of BsonDocument.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="writeConcern">The write concern to use when accessing this collection.</param>
        /// <returns>An instance of MongoCollection.</returns>
        [Obsolete("Use GetCollection instead.")]
        public virtual MongoCollection<BsonDocument> this[string collectionName, WriteConcern writeConcern]
        {
            get { return GetCollection(collectionName, writeConcern); }
        }

        // public methods
        /// <summary>
        /// Adds a user to this database.
        /// </summary>
        /// <param name="user">The user.</param>
        public virtual void AddUser(MongoUser user)
        {
            var users = GetCollection("system.users");
            var document = users.FindOne(Query.EQ("user", user.Username));
            if (document == null)
            {
                document = new BsonDocument("user", user.Username);
            }
            document["readOnly"] = user.IsReadOnly;
            document["pwd"] = user.PasswordHash;
            users.Save(document);
        }

        /// <summary>
        /// Tests whether a collection exists on this database.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <returns>True if the collection exists.</returns>
        public virtual bool CollectionExists(string collectionName)
        {
            return GetCollectionNames().Contains(collectionName);
        }

        /// <summary>
        /// Creates a collection. MongoDB creates collections automatically when they are first used, so
        /// this command is mainly here for frameworks.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <returns>A CommandResult.</returns>
        public virtual CommandResult CreateCollection(string collectionName)
        {
            return CreateCollection(collectionName, null);
        }

        /// <summary>
        /// Creates a collection. MongoDB creates collections automatically when they are first used, so
        /// you only need to call this method if you want to provide non-default options.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="options">Options for creating this collection (usually a CollectionOptionsDocument or constructed using the CollectionOptions builder).</param>
        /// <returns>A CommandResult.</returns>
        public virtual CommandResult CreateCollection(string collectionName, IMongoCollectionOptions options)
        {
            var command = new CommandDocument("create", collectionName);
            if (options != null)
            {
                command.Merge(options.ToBsonDocument());
            }
            return RunCommandAs<CommandResult>(command);
        }

        /// <summary>
        /// Creates an instance of MongoCollectionSettings for the named collection with the rest of the settings inherited.
        /// You can override some of these settings before calling GetCollection.
        /// </summary>
        /// <typeparam name="TDefaultDocument">The default document type for this collection.</typeparam>
        /// <param name="collectionName">The name of this collection.</param>
        /// <returns>A MongoCollectionSettings.</returns>
        [Obsolete("Use new MongoCollectionSettings() instead.")]
        public virtual MongoCollectionSettings<TDefaultDocument> CreateCollectionSettings<TDefaultDocument>(
            string collectionName)
        {
            return new MongoCollectionSettings<TDefaultDocument>(this, collectionName);
        }

        /// <summary>
        /// Creates an instance of MongoCollectionSettings for the named collection with the rest of the settings inherited.
        /// You can override some of these settings before calling GetCollection.
        /// </summary>
        /// <param name="defaultDocumentType">The default document type for this collection.</param>
        /// <param name="collectionName">The name of this collection.</param>
        /// <returns>A MongoCollectionSettings.</returns>
        [Obsolete("Use new MongoCollectionSettings() instead.")]
        public virtual MongoCollectionSettings CreateCollectionSettings(
            Type defaultDocumentType,
            string collectionName)
        {
            var settingsDefinition = typeof(MongoCollectionSettings<>);
            var settingsType = settingsDefinition.MakeGenericType(defaultDocumentType);
            var constructorInfo = settingsType.GetConstructor(new Type[] { typeof(MongoDatabase), typeof(string) });
            return (MongoCollectionSettings)constructorInfo.Invoke(new object[] { this, collectionName });
        }

        /// <summary>
        /// Drops a database.
        /// </summary>
        public virtual void Drop()
        {
            _server.DropDatabase(_name);
        }

        /// <summary>
        /// Drops a collection.
        /// </summary>
        /// <param name="collectionName">The name of the collection to drop.</param>
        /// <returns>A CommandResult.</returns>
        public virtual CommandResult DropCollection(string collectionName)
        {
            try
            {
                var command = new CommandDocument("drop", collectionName);
                var result = RunCommandAs<CommandResult>(command);
                _server.IndexCache.Reset(_name, collectionName);
                return result;
            }
            catch (MongoCommandException ex)
            {
                if (ex.CommandResult.ErrorMessage == "ns not found")
                {
                    return ex.CommandResult;
                }
                throw;
            }
        }

        /// <summary>
        /// Evaluates JavaScript code at the server.
        /// </summary>
        /// <param name="flags">Flags that control Eval options.</param>
        /// <param name="code">The code to evaluate.</param>
        /// <param name="args">Optional arguments (only used when the code is a function with parameters).</param>
        /// <returns>The result of evaluating the code.</returns>
        public virtual BsonValue Eval(EvalFlags flags, BsonJavaScript code, params object[] args)
        {
            var argsArray = (args != null && args.Length > 0) ? new BsonArray(args) : null;
            var command = new CommandDocument
            {
                { "$eval", code },
                { "args", argsArray, argsArray != null },
                { "nolock", true, (flags & EvalFlags.NoLock) != 0 }
            };
            var result = RunCommandAs<CommandResult>(command);
            return result.Response["retval"];
        }

        /// <summary>
        /// Evaluates JavaScript code at the server.
        /// </summary>
        /// <param name="code">The code to evaluate.</param>
        /// <param name="args">Optional arguments (only used when the code is a function with parameters).</param>
        /// <returns>The result of evaluating the code.</returns>
        public virtual BsonValue Eval(BsonJavaScript code, params object[] args)
        {
            return Eval(EvalFlags.None, code, args);
        }

        /// <summary>
        /// Fetches the document referred to by the DBRef.
        /// </summary>
        /// <param name="dbRef">The <see cref="MongoDBRef"/> to fetch.</param>
        /// <returns>A BsonDocument (or null if the document was not found).</returns>
        public virtual BsonDocument FetchDBRef(MongoDBRef dbRef)
        {
            return FetchDBRefAs<BsonDocument>(dbRef);
        }

        /// <summary>
        /// Fetches the document referred to by the DBRef, deserialized as a <typeparamref name="TDocument"/>.
        /// </summary>
        /// <typeparam name="TDocument">The nominal type of the document to fetch.</typeparam>
        /// <param name="dbRef">The <see cref="MongoDBRef"/> to fetch.</param>
        /// <returns>A <typeparamref name="TDocument"/> (or null if the document was not found).</returns>
        public virtual TDocument FetchDBRefAs<TDocument>(MongoDBRef dbRef)
        {
            return (TDocument)FetchDBRefAs(typeof(TDocument), dbRef);
        }

        /// <summary>
        /// Fetches the document referred to by the DBRef.
        /// </summary>
        /// <param name="documentType">The nominal type of the document to fetch.</param>
        /// <param name="dbRef">The <see cref="MongoDBRef"/> to fetch.</param>
        /// <returns>An instance of nominalType (or null if the document was not found).</returns>
        public virtual object FetchDBRefAs(Type documentType, MongoDBRef dbRef)
        {
            if (dbRef.DatabaseName != null && dbRef.DatabaseName != _name)
            {
                return _server.FetchDBRefAs(documentType, dbRef);
            }

            var collection = GetCollection(dbRef.CollectionName);
            var query = Query.EQ("_id", dbRef.Id);
            return collection.FindOneAs(documentType, query);
        }

        /// <summary>
        /// Finds all users of this database.
        /// </summary>
        /// <returns>An array of users.</returns>
        public virtual MongoUser[] FindAllUsers()
        {
            var result = new List<MongoUser>();
            var users = GetCollection("system.users");
            foreach (var document in users.FindAll())
            {
                var username = document["user"].AsString;
                var passwordHash = document["pwd"].AsString;
                var readOnly = document["readOnly"].ToBoolean();
                var user = new MongoUser(username, passwordHash, readOnly);
                result.Add(user);
            };
            return result.ToArray();
        }

        /// <summary>
        /// Finds a user of this database.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns>The user.</returns>
        public virtual MongoUser FindUser(string username)
        {
            var users = GetCollection("system.users");
            var query = Query.EQ("user", username);
            var document = users.FindOne(query);
            if (document != null)
            {
                var passwordHash = document["pwd"].AsString;
                var readOnly = document["readOnly"].ToBoolean();
                return new MongoUser(username, passwordHash, readOnly);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets a MongoCollection instance representing a collection on this database
        /// with a default document type of TDefaultDocument.
        /// </summary>
        /// <typeparam name="TDefaultDocument">The default document type for this collection.</typeparam>
        /// <param name="collectionSettings">The settings to use when accessing this collection.</param>
        /// <returns>An instance of MongoCollection.</returns>
        [Obsolete("Use GetCollection<TDefaultDocument>(string collectionName, MongoCollectionSettings collectionSettings) instead.")]
        public virtual MongoCollection<TDefaultDocument> GetCollection<TDefaultDocument>(
            MongoCollectionSettings<TDefaultDocument> collectionSettings)
        {
            return GetCollection<TDefaultDocument>(collectionSettings.CollectionName, collectionSettings);
        }

        /// <summary>
        /// Gets a MongoCollection instance representing a collection on this database
        /// with a default document type of TDefaultDocument.
        /// </summary>
        /// <typeparam name="TDefaultDocument">The default document type for this collection.</typeparam>
        /// <param name="collectionName">The name of the collection.</param>
        /// <returns>An instance of MongoCollection.</returns>
        public virtual MongoCollection<TDefaultDocument> GetCollection<TDefaultDocument>(string collectionName)
        {
            var collectionSettings = new MongoCollectionSettings();
            return GetCollection<TDefaultDocument>(collectionName, collectionSettings);
        }

        /// <summary>
        /// Gets a MongoCollection instance representing a collection on this database
        /// with a default document type of TDefaultDocument.
        /// </summary>
        /// <typeparam name="TDefaultDocument">The default document type for this collection.</typeparam>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="collectionSettings">The settings to use when accessing this collection.</param>
        /// <returns>An instance of MongoCollection.</returns>
        public virtual MongoCollection<TDefaultDocument> GetCollection<TDefaultDocument>(
            string collectionName, MongoCollectionSettings collectionSettings)
        {
            return new MongoCollection<TDefaultDocument>(this, collectionName, collectionSettings);
        }

        /// <summary>
        /// Gets a MongoCollection instance representing a collection on this database
        /// with a default document type of TDefaultDocument.
        /// </summary>
        /// <typeparam name="TDefaultDocument">The default document type for this collection.</typeparam>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="writeConcern">The write concern to use when accessing this collection.</param>
        /// <returns>An instance of MongoCollection.</returns>
        public virtual MongoCollection<TDefaultDocument> GetCollection<TDefaultDocument>(
            string collectionName,
            WriteConcern writeConcern)
        {
            var collectionSettings = new MongoCollectionSettings { WriteConcern = writeConcern };
            return GetCollection<TDefaultDocument>(collectionName, collectionSettings);
        }

        /// <summary>
        /// Gets a MongoCollection instance representing a collection on this database
        /// with a default document type of TDefaultDocument.
        /// </summary>
        /// <param name="collectionSettings">The settings to use when accessing this collection.</param>
        /// <returns>An instance of MongoCollection.</returns>
        [Obsolete("Use GetCollection(Type defaultDocumentType, string collectionName, MongoCollectionSettings settings) instead.")]
        public virtual MongoCollection GetCollection(MongoCollectionSettings collectionSettings)
        {
            return GetCollection(collectionSettings.DefaultDocumentType, collectionSettings.CollectionName, collectionSettings);
        }

        /// <summary>
        /// Gets a MongoCollection instance representing a collection on this database
        /// with a default document type of BsonDocument.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <returns>An instance of MongoCollection.</returns>
        public virtual MongoCollection<BsonDocument> GetCollection(string collectionName)
        {
            return GetCollection<BsonDocument>(collectionName);
        }

        /// <summary>
        /// Gets a MongoCollection instance representing a collection on this database
        /// with a default document type of TDefaultDocument.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="collectionSettings">The settings to use when accessing this collection.</param>
        /// <returns>An instance of MongoCollection.</returns>
        public virtual MongoCollection<BsonDocument> GetCollection(string collectionName, MongoCollectionSettings collectionSettings)
        {
            return GetCollection<BsonDocument>(collectionName, collectionSettings);
        }

        /// <summary>
        /// Gets a MongoCollection instance representing a collection on this database
        /// with a default document type of BsonDocument.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="writeConcern">The write concern to use when accessing this collection.</param>
        /// <returns>An instance of MongoCollection.</returns>
        public virtual MongoCollection<BsonDocument> GetCollection(string collectionName, WriteConcern writeConcern)
        {
            return GetCollection<BsonDocument>(collectionName, writeConcern);
        }

        /// <summary>
        /// Gets a MongoCollection instance representing a collection on this database
        /// with a default document type of BsonDocument.
        /// </summary>
        /// <param name="defaultDocumentType">The default document type.</param>
        /// <param name="collectionName">The name of the collection.</param>
        /// <returns>An instance of MongoCollection.</returns>
        public virtual MongoCollection GetCollection(Type defaultDocumentType, string collectionName)
        {
            var collectionSettings = new MongoCollectionSettings();
            return GetCollection(defaultDocumentType, collectionName, collectionSettings);
        }

        /// <summary>
        /// Gets a MongoCollection instance representing a collection on this database
        /// with a default document type of BsonDocument.
        /// </summary>
        /// <param name="defaultDocumentType">The default document type.</param>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="collectionSettings">The settings to use when accessing this collection.</param>
        /// <returns>An instance of MongoCollection.</returns>
        public virtual MongoCollection GetCollection(Type defaultDocumentType, string collectionName, MongoCollectionSettings collectionSettings)
        {
            var collectionDefinition = typeof(MongoCollection<>);
            var collectionType = collectionDefinition.MakeGenericType(defaultDocumentType);
            var constructorInfo = collectionType.GetConstructor(new Type[] { typeof(MongoDatabase), typeof(string), typeof(MongoCollectionSettings) });
            return (MongoCollection)constructorInfo.Invoke(new object[] { this, collectionName, collectionSettings });
        }

        /// <summary>
        /// Gets a MongoCollection instance representing a collection on this database
        /// with a default document type of BsonDocument.
        /// </summary>
        /// <param name="defaultDocumentType">The default document type.</param>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="writeConcern">The write concern to use when accessing this collection.</param>
        /// <returns>An instance of MongoCollection.</returns>
        public virtual MongoCollection GetCollection(
            Type defaultDocumentType,
            string collectionName,
            WriteConcern writeConcern)
        {
            var collectionSettings = new MongoCollectionSettings { WriteConcern = writeConcern };
            return GetCollection(defaultDocumentType, collectionName, collectionSettings);
        }

        /// <summary>
        /// Gets a list of the names of all the collections in this database.
        /// </summary>
        /// <returns>A list of collection names.</returns>
        public virtual IEnumerable<string> GetCollectionNames()
        {
            List<string> collectionNames = new List<string>();
            var namespaces = GetCollection("system.namespaces");
            var prefix = _name + ".";
            foreach (var @namespace in namespaces.FindAll())
            {
                string collectionName = @namespace["name"].AsString;
                if (!collectionName.StartsWith(prefix, StringComparison.Ordinal)) { continue; }
                if (collectionName.IndexOf('$') != -1) { continue; }
                collectionNames.Add(collectionName.Substring(prefix.Length));
            }
            collectionNames.Sort();
            return collectionNames;
        }

        /// <summary>
        /// Gets the current operation.
        /// </summary>
        /// <returns>The current operation.</returns>
        public virtual BsonDocument GetCurrentOp()
        {
            var collection = GetCollection("$cmd.sys.inprog");
            return collection.FindOne();
        }

        /// <summary>
        /// Gets an instance of MongoGridFS for this database using custom GridFS settings.
        /// </summary>
        /// <param name="gridFSSettings">The GridFS settings to use.</param>
        /// <returns>An instance of MongoGridFS.</returns>
        public virtual MongoGridFS GetGridFS(MongoGridFSSettings gridFSSettings)
        {
            return new MongoGridFS(this, gridFSSettings);
        }

        /// <summary>
        /// Gets the last error (if any) that occurred on this connection. You MUST be within a RequestStart to call this method.
        /// </summary>
        /// <returns>The last error (<see cref=" GetLastErrorResult"/>)</returns>
        public virtual GetLastErrorResult GetLastError()
        {
            if (Server.RequestNestingLevel == 0)
            {
                throw new InvalidOperationException("GetLastError can only be called if RequestStart has been called first.");
            }
            return RunCommandAs<GetLastErrorResult>("getlasterror"); // use all lowercase for backward compatibility
        }

        // TODO: mongo shell has GetPrevError at the database level?
        // TODO: mongo shell has GetProfilingLevel at the database level?
        // TODO: mongo shell has GetReplicationInfo at the database level?

        /// <summary>
        /// Gets one or more documents from the system.profile collection.
        /// </summary>
        /// <param name="query">A query to select which documents to return.</param>
        /// <returns>A cursor.</returns>
        public MongoCursor<SystemProfileInfo> GetProfilingInfo(IMongoQuery query)
        {
            var collectionSettings = new MongoCollectionSettings { ReadPreference = ReadPreference.Primary };
            var collection = GetCollection<SystemProfileInfo>("system.profile", collectionSettings);
            return collection.Find(query);
        }

        /// <summary>
        /// Gets the current profiling level.
        /// </summary>
        /// <returns>The profiling level.</returns>
        public GetProfilingLevelResult GetProfilingLevel()
        {
            var command = new CommandDocument("profile", -1);
            return RunCommandAs<GetProfilingLevelResult>(command);
        }

        /// <summary>
        /// Gets a sister database on the same server.
        /// </summary>
        /// <param name="databaseName">The name of the sister database.</param>
        /// <returns>An instance of MongoDatabase.</returns>
        public virtual MongoDatabase GetSisterDatabase(string databaseName)
        {
            return _server.GetDatabase(databaseName);
        }

        /// <summary>
        /// Gets the current database stats.
        /// </summary>
        /// <returns>An instance of DatabaseStatsResult.</returns>
        public virtual DatabaseStatsResult GetStats()
        {
            return RunCommandAs<DatabaseStatsResult>("dbstats");
        }

        /// <summary>
        /// Checks whether a given collection name is valid in this database.
        /// </summary>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="message">An error message if the collection name is not valid.</param>
        /// <returns>True if the collection name is valid; otherwise, false.</returns>
        public virtual bool IsCollectionNameValid(string collectionName, out string message)
        {
            if (collectionName == null)
            {
                throw new ArgumentNullException("collectionName");
            }

            if (collectionName == "")
            {
                message = "Collection name cannot be empty.";
                return false;
            }

            if (collectionName.IndexOf('\0') != -1)
            {
                message = "Collection name cannot contain null characters.";
                return false;
            }

            if (Encoding.UTF8.GetBytes(collectionName).Length > 121)
            {
                message = "Collection name cannot exceed 121 bytes (after encoding to UTF-8).";
                return false;
            }

            message = null;
            return true;
        }

        // TODO: mongo shell has IsMaster at database level?

        /// <summary>
        /// Removes a user from this database.
        /// </summary>
        /// <param name="user">The user to remove.</param>
        public virtual void RemoveUser(MongoUser user)
        {
            RemoveUser(user.Username);
        }

        /// <summary>
        /// Removes a user from this database.
        /// </summary>
        /// <param name="username">The username to remove.</param>
        public virtual void RemoveUser(string username)
        {
            var users = GetCollection("system.users");
            users.Remove(Query.EQ("user", username));
        }

        /// <summary>
        /// Renames a collection on this database.
        /// </summary>
        /// <param name="oldCollectionName">The old name for the collection.</param>
        /// <param name="newCollectionName">The new name for the collection.</param>
        /// <returns>A CommandResult.</returns>
        public virtual CommandResult RenameCollection(string oldCollectionName, string newCollectionName)
        {
            return RenameCollection(oldCollectionName, newCollectionName, false); // dropTarget = false
        }

        /// <summary>
        /// Renames a collection on this database.
        /// </summary>
        /// <param name="oldCollectionName">The old name for the collection.</param>
        /// <param name="newCollectionName">The new name for the collection.</param>
        /// <param name="dropTarget">Whether to drop the target collection first if it already exists.</param>
        /// <returns>A CommandResult.</returns>
        public virtual CommandResult RenameCollection(string oldCollectionName, string newCollectionName, bool dropTarget)
        {
            if (oldCollectionName == null)
            {
                throw new ArgumentNullException("oldCollectionName");
            }
            if (newCollectionName == null)
            {
                throw new ArgumentNullException("newCollectionName");
            }
            string message;
            if (!IsCollectionNameValid(newCollectionName, out message))
            {
                throw new ArgumentOutOfRangeException("newCollectionName", message);
            }

            var command = new CommandDocument
            {
                { "renameCollection", string.Format("{0}.{1}", _name, oldCollectionName) },
                { "to", string.Format("{0}.{1}", _name, newCollectionName) },
                { "dropTarget", dropTarget, dropTarget } // only added if dropTarget is true
            };
            var adminDatabase = _server.GetDatabase("admin");
            return adminDatabase.RunCommandAs<CommandResult>(command);
        }

        /// <summary>
        /// Lets the server know that this thread is done with a series of related operations. Instead of calling this method it is better
        /// to put the return value of RequestStart in a using statement.
        /// </summary>
        public virtual void RequestDone()
        {
            _server.RequestDone();
        }

        /// <summary>
        /// Lets the server know that this thread is about to begin a series of related operations that must all occur
        /// on the same connection. The return value of this method implements IDisposable and can be placed in a
        /// using statement (in which case RequestDone will be called automatically when leaving the using statement).
        /// </summary>
        /// <returns>A helper object that implements IDisposable and calls <see cref="RequestDone"/> from the Dispose method.</returns>
        public virtual IDisposable RequestStart()
        {
            return RequestStart(ReadPreference.Primary);
        }

        /// <summary>
        /// Lets the server know that this thread is about to begin a series of related operations that must all occur
        /// on the same connection. The return value of this method implements IDisposable and can be placed in a
        /// using statement (in which case RequestDone will be called automatically when leaving the using statement).
        /// </summary>
        /// <param name="slaveOk">Whether queries should be sent to secondary servers.</param>
        /// <returns>A helper object that implements IDisposable and calls <see cref="RequestDone"/> from the Dispose method.</returns>
        [Obsolete("Use the overload of RequestStart that has a ReadPreference parameter instead.")]
        public virtual IDisposable RequestStart(bool slaveOk)
        {
            return _server.RequestStart(this, ReadPreference.FromSlaveOk(slaveOk));
        }

        /// <summary>
        /// Lets the server know that this thread is about to begin a series of related operations that must all occur
        /// on the same connection. The return value of this method implements IDisposable and can be placed in a
        /// using statement (in which case RequestDone will be called automatically when leaving the using statement).
        /// </summary>
        /// <param name="readPreference">The read preference.</param>
        /// <returns>A helper object that implements IDisposable and calls <see cref="RequestDone"/> from the Dispose method.</returns>
        public virtual IDisposable RequestStart(ReadPreference readPreference)
        {
            return _server.RequestStart(this, readPreference);
        }

        // TODO: mongo shell has ResetError at the database level

        /// <summary>
        /// Removes all entries for this database in the index cache used by EnsureIndex. Call this method
        /// when you know (or suspect) that a process other than this one may have dropped one or
        /// more indexes.
        /// </summary>
        public virtual void ResetIndexCache()
        {
            _server.IndexCache.Reset(this);
        }

        /// <summary>
        /// Runs a command on this database.
        /// </summary>
        /// <param name="command">The command object.</param>
        /// <returns>A CommandResult</returns>
        public virtual CommandResult RunCommand(IMongoCommand command)
        {
            return RunCommandAs<CommandResult>(command);
        }

        /// <summary>
        /// Runs a command on this database.
        /// </summary>
        /// <param name="commandName">The name of the command.</param>
        /// <returns>A CommandResult</returns>
        public virtual CommandResult RunCommand(string commandName)
        {
            return RunCommandAs<CommandResult>(commandName);
        }

        /// <summary>
        /// Runs a command on this database and returns the result as a TCommandResult.
        /// </summary>
        /// <typeparam name="TCommandResult">The type of the returned command result.</typeparam>
        /// <param name="command">The command object.</param>
        /// <returns>A TCommandResult</returns>
        public virtual TCommandResult RunCommandAs<TCommandResult>(IMongoCommand command)
            where TCommandResult : CommandResult
        {
            var resultSerializer = BsonSerializer.LookupSerializer(typeof(TCommandResult));
            return RunCommandAs<TCommandResult>(command, resultSerializer, null);
        }

        /// <summary>
        /// Runs a command on this database and returns the result as a TCommandResult.
        /// </summary>
        /// <typeparam name="TCommandResult">The type of the returned command result.</typeparam>
        /// <param name="commandName">The name of the command.</param>
        /// <returns>A TCommandResult</returns>
        public virtual TCommandResult RunCommandAs<TCommandResult>(string commandName)
            where TCommandResult : CommandResult
        {
            var command = new CommandDocument(commandName, 1);
            return RunCommandAs<TCommandResult>(command);
        }

        /// <summary>
        /// Runs a command on this database and returns the result as a TCommandResult.
        /// </summary>
        /// <param name="commandResultType">The command result type.</param>
        /// <param name="command">The command object.</param>
        /// <returns>A TCommandResult</returns>
        public virtual CommandResult RunCommandAs(Type commandResultType, IMongoCommand command)
        {
            var methodDefinition = GetType().GetMethod("RunCommandAs", new Type[] { typeof(IMongoCommand) });
            var methodInfo = methodDefinition.MakeGenericMethod(commandResultType);
            return (CommandResult)methodInfo.Invoke(this, new object[] { command });
        }

        /// <summary>
        /// Runs a command on this database and returns the result as a TCommandResult.
        /// </summary>
        /// <param name="commandResultType">The command result type.</param>
        /// <param name="commandName">The name of the command.</param>
        /// <returns>A TCommandResult</returns>
        public virtual CommandResult RunCommandAs(Type commandResultType, string commandName)
        {
            var command = new CommandDocument(commandName, 1);
            return RunCommandAs(commandResultType, command);
        }

        /// <summary>
        /// Sets the level of profile information to write.
        /// </summary>
        /// <param name="level">The profiling level.</param>
        /// <returns>A CommandResult.</returns>
        public virtual CommandResult SetProfilingLevel(ProfilingLevel level)
        {
            return SetProfilingLevel(level, TimeSpan.Zero);
        }

        /// <summary>
        /// Sets the level of profile information to write.
        /// </summary>
        /// <param name="level">The profiling level.</param>
        /// <param name="slow">The threshold that defines a slow query.</param>
        /// <returns>A CommandResult.</returns>
        public virtual CommandResult SetProfilingLevel(ProfilingLevel level, TimeSpan slow)
        {
            var command = new CommandDocument
            {
                { "profile", (int) level },
                { "slowms", slow.TotalMilliseconds, slow != TimeSpan.Zero } // optional
            };
            return RunCommandAs<CommandResult>(command);
        }

        /// <summary>
        /// Gets a canonical string representation for this database.
        /// </summary>
        /// <returns>A canonical string representation for this database.</returns>
        public override string ToString()
        {
            return _name;
        }

        // private methods
        private TCommandResult RunCommandAs<TCommandResult>(
            IMongoCommand command,
            IBsonSerializer resultSerializer,
            IBsonSerializationOptions resultSerializationOptions) where TCommandResult : CommandResult
        {
            var readerSettings = new BsonBinaryReaderSettings
            {
                Encoding = _settings.ReadEncoding ?? MongoDefaults.ReadEncoding,
                GuidRepresentation = _settings.GuidRepresentation
            };
            var writerSettings = new BsonBinaryWriterSettings
            {
                Encoding = _settings.WriteEncoding ?? MongoDefaults.WriteEncoding,
                GuidRepresentation = _settings.GuidRepresentation
            };
            var readPreference = _settings.ReadPreference;
            if (readPreference != ReadPreference.Primary && !CanCommandBeSentToSecondary.Delegate(command.ToBsonDocument()))
            {
                readPreference = ReadPreference.Primary;
            }
            var flags = (readPreference == ReadPreference.Primary) ? QueryFlags.None : QueryFlags.SlaveOk;

            var commandOperation = new CommandOperation<TCommandResult>(
                _name,
                readerSettings,
                writerSettings,
                command,
                flags,
                null, // options
                readPreference,
                resultSerializationOptions,
                resultSerializer);

            var connection = _server.AcquireConnection(readPreference);
            try
            {
                return commandOperation.Execute(connection);
            }
            finally
            {
                _server.ReleaseConnection(connection);
            }
        }
    }
}
