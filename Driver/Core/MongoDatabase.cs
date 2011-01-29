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
using System.Text;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.Internal;
using MongoDB.Driver.Builders;

namespace MongoDB.Driver {
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
        public MongoDatabase(
            MongoServer server,
            MongoDatabaseSettings settings
        ) {
            ValidateDatabaseName(settings.DatabaseName);
            this.server = server;
            this.settings = settings;
            this.name = settings.DatabaseName;

            // make sure commands get routed to the primary server by using slaveOk false
            var commandCollectionSettings = GetCollectionSettings<BsonDocument>("$cmd");
            commandCollectionSettings.AssignIdOnInsert = false;
            commandCollectionSettings.SlaveOk = false;
            commandCollection = GetCollection(commandCollectionSettings);
        }
        #endregion

        #region factory methods
        public static MongoDatabase Create(
            MongoConnectionStringBuilder builder
        ) {
            var serverSettings = builder.ToServerSettings();
            var databaseName = builder.DatabaseName;
            return Create(serverSettings, databaseName);
        }

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

        public static MongoDatabase Create(
            MongoUrl url
        ) {
            var serverSettings = url.ToServerSettings();
            var databaseName = url.DatabaseName;
            return Create(serverSettings, databaseName);
        }

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

        public static MongoDatabase Create(
            Uri uri
        ) {
            return Create(MongoUrl.Create(uri.ToString()));
        }
        #endregion

        #region public properties
        public virtual MongoCollection<BsonDocument> CommandCollection {
            get { return commandCollection; }
        }

        public virtual MongoCredentials Credentials {
            get { return settings.Credentials; }
        }

        public virtual MongoGridFS GridFS {
            get {
                lock (databaseLock) {
                    if (gridFS == null) {
                        gridFS = new MongoGridFS(this, MongoGridFS.DefaultSettings);
                    }
                    return gridFS;
                }
            }
        }

        public virtual string Name {
            get { return name; }
        }

        public virtual MongoServer Server {
            get { return server; }
        }

        public virtual MongoDatabaseSettings Settings {
            get { return settings; }
        }
        #endregion

        #region public indexers
        public virtual MongoCollection<BsonDocument> this[
            string collectionName
        ] {
            get { return GetCollection(collectionName); }
        }

        public virtual MongoCollection<BsonDocument> this[
            string collectionName,
            SafeMode safeMode
        ] {
            get { return GetCollection(collectionName, safeMode); }
        }
        #endregion

        #region public methods
        public virtual void AddUser(
            MongoCredentials credentials
        ) {
            AddUser(credentials, false);
        }

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

        public virtual bool CollectionExists(
            string collectionName
        ) {
            return GetCollectionNames().Contains(collectionName);
        }

        public virtual CommandResult CreateCollection(
            string collectionName,
            BsonDocument options
        ) {
            var command = new CommandDocument("create", collectionName);
            command.Merge(options);
            return RunCommand(command);
        }

        public virtual void Drop() {
            server.DropDatabase(name);
        }

        public virtual CommandResult DropCollection(
            string collectionName
        ) {
            var command = new CommandDocument("drop", collectionName);
            return RunCommand(command);
        }

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

        public virtual BsonDocument FetchDBRef(
            MongoDBRef dbRef
        ) {
            return FetchDBRefAs<BsonDocument>(dbRef);
        }

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

        public virtual MongoCollection<TDefaultDocument> GetCollection<TDefaultDocument>(
            MongoCollectionSettings<TDefaultDocument> collectionSettings
        ) {
            lock (databaseLock) {
                MongoCollection collection;
                collectionSettings.Freeze();
                if (!collections.TryGetValue(collectionSettings, out collection)) {
                    collection = new MongoCollection<TDefaultDocument>(this, collectionSettings);
                    collections.Add(collectionSettings, collection);
                }
                return (MongoCollection<TDefaultDocument>) collection;
            }
        }

        public virtual MongoCollection<TDefaultDocument> GetCollection<TDefaultDocument>(
            string collectionName
        ) {
            var collectionSettings = GetCollectionSettings<TDefaultDocument>(collectionName);
            return GetCollection(collectionSettings);
        }

        public virtual MongoCollection<TDefaultDocument> GetCollection<TDefaultDocument>(
            string collectionName,
            SafeMode safeMode
        ) {
            var collectionSettings = GetCollectionSettings<TDefaultDocument>(collectionName);
            collectionSettings.SafeMode = safeMode;
            return GetCollection(collectionSettings);
        }

        public virtual MongoCollection<BsonDocument> GetCollection(
            string collectionName
        ) {
            var collectionSettings = GetCollectionSettings<BsonDocument>(collectionName);
            return GetCollection(collectionSettings);
        }

        public virtual MongoCollection<BsonDocument> GetCollection(
            string collectionName,
            SafeMode safeMode
        ) {
            var collectionSettings = GetCollectionSettings<BsonDocument>(collectionName);
            collectionSettings.SafeMode = safeMode;
            return GetCollection(collectionSettings);
        }

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

        public virtual MongoCollectionSettings<TDefaultDocument> GetCollectionSettings<TDefaultDocument>(
            string collectionName
        ) {
            return new MongoCollectionSettings<TDefaultDocument>(
                collectionName,
                MongoDefaults.AssignIdOnInsert,
                settings.SafeMode,
                settings.SlaveOk
            );
        }

        public virtual BsonDocument GetCurrentOp() {
            var collection = GetCollection("$cmd.sys.inprog");
            return collection.FindOne();
        }

        public virtual MongoGridFS GetGridFS(
            MongoGridFSSettings gridFSSettings
        ) {
            return new MongoGridFS(this, gridFSSettings);
        }

        // TODO: mongo shell has GetPrevError at the database level?
        // TODO: mongo shell has GetProfilingLevel at the database level?
        // TODO: mongo shell has GetReplicationInfo at the database level?

        public virtual MongoDatabase GetSisterDatabase(
            string databaseName
        ) {
            return server.GetDatabase(databaseName);
        }

        public virtual DatabaseStatsResult GetStats() {
            return RunCommandAs<DatabaseStatsResult>("dbstats");
        }

        // TODO: mongo shell has IsMaster at database level?

        public virtual void RemoveUser(
            string username
        ) {
            var users = GetCollection("system.users");
            users.Remove(Query.EQ("user", username));
        }

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

        public virtual void RequestDone() {
            server.RequestDone();
        }

        // the result of RequestStart is IDisposable so you can use RequestStart in a using statment
        // and then RequestDone will be called automatically when leaving the using statement
        public virtual IDisposable RequestStart() {
            return server.RequestStart(this);
        }

        // TODO: mongo shell has ResetError at the database level

        public virtual CommandResult RunCommand(
            IMongoCommand command
        ) {
            return RunCommandAs<CommandResult>(command);
        }

        public virtual CommandResult RunCommand(
            string commandName
        ) {
            return RunCommandAs<CommandResult>(commandName);
        }

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

        public virtual TCommandResult RunCommandAs<TCommandResult>(
            string commandName
        ) where TCommandResult : CommandResult, new() {
            var command = new CommandDocument(commandName, true);
            return RunCommandAs<TCommandResult>(command);
        }

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
