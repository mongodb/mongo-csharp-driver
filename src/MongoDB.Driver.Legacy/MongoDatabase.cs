/* Copyright 2010-2016 MongoDB Inc.
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
using System.Reflection;
using System.Text;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
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
        private readonly MongoServer _server;
        private readonly MongoDatabaseSettings _settings;
        private readonly DatabaseNamespace _namespace;

        // constructors
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
            _namespace = new DatabaseNamespace(name);
            _settings = settings;
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
            get { return GetGridFS(new MongoGridFSSettings()); }
        }

        /// <summary>
        /// Gets the name of this database.
        /// </summary>
        public virtual string Name
        {
            get { return _namespace.DatabaseName; }
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
        [Obsolete("Use the new user management command 'createUser' or 'updateUser'.")]
        public virtual void AddUser(MongoUser user)
        {
            var operation = new AddUserOperation(
                _namespace,
                user.Username,
                user.PasswordHash,
                user.IsReadOnly,
                GetMessageEncoderSettings());
            ExecuteWriteOperation(operation);
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
            if (collectionName == null)
            {
                throw new ArgumentNullException("collectionName");
            }

            var collectionNamespace = new CollectionNamespace(_namespace, collectionName);
            var messageEncoderSettings = GetMessageEncoderSettings();
            bool? autoIndexId = null;
            bool? capped = null;
            Collation collation = null;
            BsonDocument indexOptionDefaults = null;
            int? maxDocuments = null;
            long? maxSize = null;
            bool? noPadding = null;
            BsonDocument storageEngine = null;
            bool? usePowerOf2Sizes = null;
            DocumentValidationAction? validationAction = null;
            DocumentValidationLevel? validationLevel = null;
            BsonDocument validator = null;

            if (options != null)
            {
                var optionsDocument = options.ToBsonDocument();

                BsonValue value;
                if (optionsDocument.TryGetValue("autoIndexId", out value))
                {
                    autoIndexId = value.ToBoolean();
                }
                if (optionsDocument.TryGetValue("capped", out value))
                {
                    capped = value.ToBoolean();
                }
                if (optionsDocument.TryGetValue("collation", out value))
                {
                    collation = Collation.FromBsonDocument(value.AsBsonDocument);
                }
                if (optionsDocument.TryGetValue("indexOptionDefaults", out value))
                {
                    indexOptionDefaults = value.AsBsonDocument;
                }
                if (optionsDocument.TryGetValue("max", out value))
                {
                    maxDocuments = value.ToInt32();
                }
                if (optionsDocument.TryGetValue("flags", out value))
                {
                    noPadding = ((CollectionUserFlags)value.ToInt32() & CollectionUserFlags.NoPadding) != 0;
                }
                if (optionsDocument.TryGetValue("size", out value))
                {
                    maxSize = value.ToInt64();
                }
                if (optionsDocument.TryGetValue("storageEngine", out value))
                {
                    storageEngine = value.AsBsonDocument;
                }
                if (optionsDocument.TryGetValue("flags", out value))
                {
                    usePowerOf2Sizes = ((CollectionUserFlags)value.ToInt32() & CollectionUserFlags.UsePowerOf2Sizes) != 0;
                }
                if (optionsDocument.TryGetValue("validationAction", out value))
                {
                    validationAction = (DocumentValidationAction)Enum.Parse(typeof(DocumentValidationAction), value.AsString, ignoreCase: true);
                }
                if (optionsDocument.TryGetValue("validationLevel", out value))
                {
                    validationLevel = (DocumentValidationLevel)Enum.Parse(typeof(DocumentValidationLevel), value.AsString, ignoreCase: true);
                }
                if (optionsDocument.TryGetValue("validator", out value))
                {
                    validator = value.AsBsonDocument;
                }
            }

            var operation = new CreateCollectionOperation(collectionNamespace, messageEncoderSettings)
            {
                AutoIndexId = autoIndexId,
                Capped = capped,
                Collation = collation,
                IndexOptionDefaults = indexOptionDefaults,
                MaxDocuments = maxDocuments,
                MaxSize = maxSize,
                NoPadding = noPadding,
                StorageEngine = storageEngine,
                UsePowerOf2Sizes = usePowerOf2Sizes,
                ValidationAction = validationAction,
                ValidationLevel = validationLevel,
                Validator = validator,
                WriteConcern = _settings.WriteConcern
            };

            var response = ExecuteWriteOperation(operation);
            return new CommandResult(response);
        }

        /// <summary>
        /// Creates a view.
        /// </summary>
        /// <param name="viewName">The name of the view.</param>
        /// <param name="viewOn">The name of the collection that the view is on.</param>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="options">The options.</param>
        /// <returns>A CommandResult.</returns>
        public virtual CommandResult CreateView(string viewName, string viewOn, IEnumerable<BsonDocument> pipeline, IMongoCreateViewOptions options)
        {
            if (viewName == null)
            {
                throw new ArgumentNullException(nameof(viewName));
            }
            if (viewOn == null)
            {
                throw new ArgumentNullException(nameof(viewOn));
            }
            if (pipeline == null)
            {
                throw new ArgumentNullException(nameof(pipeline));
            }

            Collation collation = null;

            if (options != null)
            {
                var optionsDocument = options.ToBsonDocument();

                BsonValue value;
                if (optionsDocument.TryGetValue("collation", out value))
                {
                    collation = Collation.FromBsonDocument(value.AsBsonDocument);
                }
            }

            var operation = new CreateViewOperation(_namespace, viewName, viewOn, pipeline, GetMessageEncoderSettings())
            {
                Collation = collation,
                WriteConcern = _settings.WriteConcern
            };

            var response = ExecuteWriteOperation(operation);
            return new CommandResult(response);
        }

        /// <summary>
        /// Drops a database.
        /// </summary>
        public virtual void Drop()
        {
            _server.DropDatabase(_namespace.DatabaseName);
        }

        /// <summary>
        /// Drops a collection.
        /// </summary>
        /// <param name="collectionName">The name of the collection to drop.</param>
        /// <returns>A CommandResult.</returns>
        public virtual CommandResult DropCollection(string collectionName)
        {
            var collectionNamespace = new CollectionNamespace(_namespace, collectionName);
            var messageEncoderSettings = GetMessageEncoderSettings();

            var operation = new DropCollectionOperation(collectionNamespace, messageEncoderSettings)
            {
                WriteConcern = _settings.WriteConcern
            };
            var response = ExecuteWriteOperation(operation);
            return new CommandResult(response);
        }

        /// <summary>
        /// Evaluates JavaScript code at the server.
        /// </summary>
        /// <param name="flags">Flags that control Eval options.</param>
        /// <param name="code">The code to evaluate.</param>
        /// <param name="args">Optional arguments (only used when the code is a function with parameters).</param>
        /// <returns>The result of evaluating the code.</returns>
        [Obsolete("Use the overload of Eval that has an EvalArgs parameter instead.")]
        public virtual BsonValue Eval(EvalFlags flags, BsonJavaScript code, params object[] args)
        {
            var mappedArgs = args.Select(a => BsonTypeMapper.MapToBsonValue(a));
            var @lock = ((flags & EvalFlags.NoLock) != 0) ? (bool?)false : null;
            var evalArgs = new EvalArgs { Code = code, Args = mappedArgs, Lock = @lock };
            return Eval(evalArgs);
        }

        /// <summary>
        /// Evaluates JavaScript code at the server.
        /// </summary>
        /// <param name="code">The code to evaluate.</param>
        /// <param name="args">Optional arguments (only used when the code is a function with parameters).</param>
        /// <returns>The result of evaluating the code.</returns>
        [Obsolete("Use the overload of Eval that has an EvalArgs parameter instead.")]
        public virtual BsonValue Eval(BsonJavaScript code, params object[] args)
        {
            var mappedArgs = args.Select(a => BsonTypeMapper.MapToBsonValue(a));
            var evalArgs = new EvalArgs { Code = code, Args = mappedArgs };
            return Eval(evalArgs);
        }

        /// <summary>
        /// Evaluates JavaScript code at the server.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>The result of evaluating the code.</returns>
        public virtual BsonValue Eval(EvalArgs args)
        {
            if (args == null) { throw new ArgumentNullException("args"); }
            if (args.Code == null) { throw new ArgumentException("Code is null.", "args"); }

            var operation = new EvalOperation(_namespace, args.Code, GetMessageEncoderSettings())
            {
                Args = args.Args,
                MaxTime = args.MaxTime,
                NoLock = args.Lock.HasValue ? !args.Lock : null
            };

            using (var binding = _server.GetWriteBinding())
            {
                return operation.Execute(binding, CancellationToken.None);
            }
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
            if (dbRef.DatabaseName != null && dbRef.DatabaseName != _namespace.DatabaseName)
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
        [Obsolete("Use the new user management command 'usersInfo'.")]
        public virtual MongoUser[] FindAllUsers()
        {
            var operation = new FindUsersOperation(_namespace, null, GetMessageEncoderSettings());
            var userDocuments = ExecuteReadOperation(operation, ReadPreference.Primary);
            return userDocuments.Select(u => ToMongoUser(u)).ToArray();
        }

        /// <summary>
        /// Finds a user of this database.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns>The user.</returns>
        [Obsolete("Use the new user management command 'usersInfo'.")]
        public virtual MongoUser FindUser(string username)
        {
            var operation = new FindUsersOperation(_namespace, username, GetMessageEncoderSettings());
            var userDocuments = ExecuteReadOperation(operation, ReadPreference.Primary);
            return userDocuments.Select(u => ToMongoUser(u)).FirstOrDefault();
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
            var constructorInfo = collectionType.GetTypeInfo().GetConstructor(new Type[] { typeof(MongoDatabase), typeof(string), typeof(MongoCollectionSettings) });
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
            var operation = new ListCollectionsOperation(_namespace, GetMessageEncoderSettings());
            var cursor = ExecuteReadOperation(operation, ReadPreference.Primary);
            var list = cursor.ToList();
            return list.Select(c => c["name"].AsString).OrderBy(n => n).ToList();
        }

        /// <summary>
        /// Gets the current operation.
        /// </summary>
        /// <returns>The current operation.</returns>
        public virtual BsonDocument GetCurrentOp()
        {
            var operation = new CurrentOpOperation(_namespace, GetMessageEncoderSettings());
            return ExecuteReadOperation(operation);
        }

        /// <summary>
        /// Gets an instance of MongoGridFS for this database using custom GridFS settings.
        /// </summary>
        /// <param name="gridFSSettings">The GridFS settings to use.</param>
        /// <returns>An instance of MongoGridFS.</returns>
        public virtual MongoGridFS GetGridFS(MongoGridFSSettings gridFSSettings)
        {
            var clonedSettings = gridFSSettings.Clone();
            clonedSettings.ApplyDefaultValues(_settings);
            clonedSettings.Freeze();
            return new MongoGridFS(_server, _namespace.DatabaseName, clonedSettings);
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
            return RunCommandAs<GetProfilingLevelResult>(command, ReadPreference.Primary);
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
            var command = new CommandDocument("dbstats", 1);
            return RunCommandAs<DatabaseStatsResult>(command, ReadPreference.Primary);
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
        [Obsolete("Use RunCommand with a { dropUser: <username> } document.")]
        public virtual void RemoveUser(MongoUser user)
        {
            RemoveUser(user.Username);
        }

        /// <summary>
        /// Removes a user from this database.
        /// </summary>
        /// <param name="username">The username to remove.</param>
        [Obsolete("Use RunCommand with a { dropUser: <username> } document.")]
        public virtual void RemoveUser(string username)
        {
            var operation = new DropUserOperation(_namespace, username, GetMessageEncoderSettings());
            ExecuteWriteOperation(operation);
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

            var oldCollectionNamespace = new CollectionNamespace(_namespace, oldCollectionName);
            var newCollectionNamespace = new CollectionNamespace(_namespace, newCollectionName);
            var messageEncoderSettings = GetMessageEncoderSettings();
            var operation = new RenameCollectionOperation(oldCollectionNamespace, newCollectionNamespace, messageEncoderSettings)
            {
                DropTarget = dropTarget,
                WriteConcern = _settings.WriteConcern
            };
            var response = ExecuteWriteOperation(operation);
            return new CommandResult(response);
        }

        // TODO: mongo shell has ResetError at the database level

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
            return RunCommandAs<TCommandResult>(command, ReadPreference.Primary);
        }

        /// <summary>
        /// Runs a command on this database and returns the result as a TCommandResult.
        /// </summary>
        /// <typeparam name="TCommandResult">The type of the returned command result.</typeparam>
        /// <param name="command">The command object.</param>
        /// <param name="readPreference">The read preference.</param>
        /// <returns>A TCommandResult</returns>
        public TCommandResult RunCommandAs<TCommandResult>(
            IMongoCommand command,
            ReadPreference readPreference)
            where TCommandResult : CommandResult
        {
            var resultSerializer = BsonSerializer.LookupSerializer<TCommandResult>();
            return RunCommandAs<TCommandResult>(command, resultSerializer, readPreference);
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
            var methodDefinition = GetType().GetTypeInfo().GetMethod("RunCommandAs", new Type[] { typeof(IMongoCommand) });
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
            return RunCommandAs<CommandResult>(command, ReadPreference.Primary);
        }

        /// <summary>
        /// Gets a canonical string representation for this database.
        /// </summary>
        /// <returns>A canonical string representation for this database.</returns>
        public override string ToString()
        {
            return _namespace.DatabaseName;
        }

        /// <summary>
        /// Returns a new MongoDatabase instance with a different read concern setting.
        /// </summary>
        /// <param name="readConcern">The read concern.</param>
        /// <returns>A new MongoDatabase instance with a different read concern setting.</returns>
        public virtual MongoDatabase WithReadConcern(ReadConcern readConcern)
        {
            Ensure.IsNotNull(readConcern, nameof(readConcern));
            var newSettings = Settings.Clone();
            newSettings.ReadConcern = readConcern;
            return new MongoDatabase(_server, _namespace.DatabaseName, newSettings);
        }

        /// <summary>
        /// Returns a new MongoDatabase instance with a different read preference setting.
        /// </summary>
        /// <param name="readPreference">The read preference.</param>
        /// <returns>A new MongoDatabase instance with a different read preference setting.</returns>
        public virtual MongoDatabase WithReadPreference(ReadPreference readPreference)
        {
            Ensure.IsNotNull(readPreference, nameof(readPreference));
            var newSettings = Settings.Clone();
            newSettings.ReadPreference = readPreference;
            return new MongoDatabase(_server, _namespace.DatabaseName, newSettings);
        }

        /// <summary>
        /// Returns a new MongoDatabase instance with a different write concern setting.
        /// </summary>
        /// <param name="writeConcern">The write concern.</param>
        /// <returns>A new MongoDatabase instance with a different write concern setting.</returns>
        public virtual MongoDatabase WithWriteConcern(WriteConcern writeConcern)
        {
            Ensure.IsNotNull(writeConcern, nameof(writeConcern));
            var newSettings = Settings.Clone();
            newSettings.WriteConcern = writeConcern;
            return new MongoDatabase(_server, _namespace.DatabaseName, newSettings);
        }

        // private methods
        private TResult ExecuteReadOperation<TResult>(IReadOperation<TResult> operation, ReadPreference readPreference = null)
        {
            readPreference = readPreference ?? _settings.ReadPreference ?? ReadPreference.Primary;
            using (var binding = _server.GetReadBinding(readPreference))
            {
                return operation.Execute(binding, CancellationToken.None);
            }
        }

        private TResult ExecuteWriteOperation<TResult>(IWriteOperation<TResult> operation)
        {
            using (var binding = _server.GetWriteBinding())
            {
                return operation.Execute(binding, CancellationToken.None);
            }
        }

        private MessageEncoderSettings GetMessageEncoderSettings()
        {
            return new MessageEncoderSettings
            {
                { MessageEncoderSettingsName.GuidRepresentation, _settings.GuidRepresentation },
                { MessageEncoderSettingsName.ReadEncoding, _settings.ReadEncoding ?? Utf8Encodings.Strict },
                { MessageEncoderSettingsName.WriteEncoding, _settings.WriteEncoding ?? Utf8Encodings.Strict }
            };
        }

        internal TCommandResult RunCommandAs<TCommandResult>(
            IMongoCommand command,
            IBsonSerializer<TCommandResult> resultSerializer,
            ReadPreference readPreference)
            where TCommandResult : CommandResult
        {
            var commandDocument = command.ToBsonDocument();
            var messageEncoderSettings = GetMessageEncoderSettings();

            if (readPreference == ReadPreference.Primary)
            {
                var operation = new WriteCommandOperation<TCommandResult>(_namespace, commandDocument, resultSerializer, messageEncoderSettings);
                return ExecuteWriteOperation(operation);
            }
            else
            {
                var operation = new ReadCommandOperation<TCommandResult>(_namespace, commandDocument, resultSerializer, messageEncoderSettings);
                return ExecuteReadOperation(operation, readPreference);
            }
        }

#pragma warning disable 618
        private MongoUser ToMongoUser(BsonDocument userDocument)
        {
            var username = userDocument["user"].AsString;
            var passwordHash = userDocument.GetValue("pwd", "").AsString;
            var readOnly = userDocument.GetValue("readOnly", false).ToBoolean();
            return new MongoUser(username, passwordHash, readOnly);
        }
#pragma warning restore
    }
}
