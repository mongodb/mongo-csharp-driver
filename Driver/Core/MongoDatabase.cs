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
        private string name;
        private MongoCredentials credentials;
        private SafeMode safeMode;
        private Dictionary<string, MongoCollection> collections = new Dictionary<string, MongoCollection>();
        private MongoGridFS gridFS;
        #endregion

        #region constructors
        public MongoDatabase(
            MongoServer server,
            string name,
            MongoCredentials credentials,
            SafeMode safeMode
        ) {
            ValidateDatabaseName(name);
            this.server = server;
            this.name = name;
            this.credentials = credentials;
            this.safeMode = safeMode;
        }
        #endregion

        #region factory methods
        public static MongoDatabase Create(
            MongoConnectionStringBuilder builder
        ) {
            return Create(builder.ToMongoUrl());
        }

        public static MongoDatabase Create(
            MongoUrl url
        ) {
            if (url.DatabaseName == null) {
                throw new ArgumentException("Connection string must have database name");
            }
            MongoServer server = MongoServer.Create(url);
            return server.GetDatabase(url.DatabaseName, url.Credentials);
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
        public MongoCollection<BsonDocument> CommandCollection {
            get { return GetCollection<BsonDocument>("$cmd"); }
        }

        public MongoCredentials Credentials {
            get { return credentials; }
        }

        public MongoGridFS GridFS {
            get {
                lock (databaseLock) {
                    if (gridFS == null) {
                        gridFS = new MongoGridFS(this, MongoGridFSSettings.Defaults.Clone());
                    }
                    return gridFS;
                }
            }
        }

        public string Name {
            get { return name; }
        }

        public SafeMode SafeMode {
            get { return safeMode; }
        }

        public MongoServer Server {
            get { return server; }
        }
        #endregion

        #region public indexers
        public MongoCollection<BsonDocument> this[
            string collectionName
        ] {
            get { return GetCollection(collectionName); }
        }
        #endregion

        #region public methods
        public void AddUser(
            MongoCredentials credentials
        ) {
            AddUser(credentials, false);
        }

        public void AddUser(
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

        public bool CollectionExists(
            string collectionName
        ) {
            return GetCollectionNames().Contains(collectionName);
        }

        public CommandResult CreateCollection(
            string collectionName,
            BsonDocument options
        ) {
            BsonDocument command = new BsonDocument("create", collectionName);
            command.Merge(options);
            return RunCommand<CommandResult>(command);
        }

        public CommandResult DropCollection(
            string collectionName
        ) {
            BsonDocument command = new BsonDocument("drop", collectionName);
            return RunCommand<CommandResult>(command);
        }

        public BsonValue Eval(
            string code,
            params object[] args
        ) {
            BsonDocument command = new BsonDocument {
                { "$eval", code },
                { "args", new BsonArray(args) }
            };
            var result = RunCommand<CommandResult>(command);
            return result["retval"];
        }

        public BsonDocument FetchDBRef(
            MongoDBRef dbRef
        ) {
            return FetchDBRefAs<BsonDocument>(dbRef);
        }

        public TDocument FetchDBRefAs<TDocument>(
            MongoDBRef dbRef
        ) {
            if (dbRef.DatabaseName != null && dbRef.DatabaseName != name) {
                return server.FetchDBRefAs<TDocument>(dbRef);
            }

            var collection = GetCollection(dbRef.CollectionName);
            var query = Query.EQ("_id", BsonValue.Create(dbRef.Id));
            return collection.FindOneAs<TDocument>(query);
        }

        public MongoCollection<TDefaultDocument> GetCollection<TDefaultDocument>(
            string collectionName
        ) {
            return GetCollection<TDefaultDocument>(collectionName, safeMode);
        }

        public MongoCollection<TDefaultDocument> GetCollection<TDefaultDocument>(
            string collectionName,
            SafeMode safeMode
        ) {
            lock (databaseLock) {
                MongoCollection collection;
                string key = string.Format("{0}<{1}>[{2}]", collectionName, typeof(TDefaultDocument).FullName, safeMode);
                if (!collections.TryGetValue(key, out collection)) {
                    collection = new MongoCollection<TDefaultDocument>(this, collectionName, safeMode);
                    collections.Add(key, collection);
                }
                return (MongoCollection<TDefaultDocument>) collection;
            }
        }

        public MongoCollection<BsonDocument> GetCollection(
            string collectionName
        ) {
            return GetCollection<BsonDocument>(collectionName);
        }

        public IEnumerable<string> GetCollectionNames() {
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

        public BsonDocument GetCurrentOp() {
            var collection = GetCollection("$cmd.sys.inprog");
            return collection.FindOne();
        }

        // TODO: mongo shell has GetPrevError at the database level?
        // TODO: mongo shell has GetProfilingLevel at the database level?
        // TODO: mongo shell has GetReplicationInfo at the database level?

        public MongoDatabase GetSisterDatabase(
            string databaseName
        ) {
            return server.GetDatabase(databaseName);
        }

        public DatabaseStatsResult GetStats() {
            return RunCommand<DatabaseStatsResult>("dbstats");
        }

        // TODO: mongo shell has IsMaster at database level?

        public void RemoveUser(
            string username
        ) {
            var users = GetCollection("system.users");
            users.Remove(Query.EQ("user", username));
        }

        public CommandResult RenameCollection(
            MongoCredentials adminCredentials,
            string oldCollectionName,
            string newCollectionName
        ) {
            var command = new BsonDocument {
                { "renameCollection", string.Format("{0}.{1}", name, oldCollectionName) },
                { "to", string.Format("{0}.{1}", name, newCollectionName) }
            };
            return server.RunAdminCommand<CommandResult>(adminCredentials, command);
        }

        public CommandResult RenameCollection(
            string oldCollectionName,
            string newCollectionName
        ) {
            return RenameCollection(server.AdminCredentials, oldCollectionName, newCollectionName);
        }

        public void RequestDone() {
            server.RequestDone();
        }

        // the result of RequestStart is IDisposable so you can use RequestStart in a using statment
        // and then RequestDone will be called automatically when leaving the using statement
        public IDisposable RequestStart() {
            return server.RequestStart(this);
        }

        // TODO: mongo shell has ResetError at the database level

        public TCommandResult RunCommand<TCommand, TCommandResult>(
            TCommand command
        ) where TCommandResult : CommandResult {
            var result = CommandCollection.FindOneAs<TCommand, TCommandResult>(command);
            if (!result.Ok) {
                string errorMessage = string.Format("Command failed: {0}", result.ErrorMessage);
                throw new MongoCommandException(errorMessage);
            }
            return result;
        }

        public TCommandResult RunCommand<TCommandResult>(
            IBsonSerializable command
        ) where TCommandResult : CommandResult {
            return RunCommand<IBsonSerializable, TCommandResult>(command);
        }

        public TCommandResult RunCommand<TCommandResult>(
            string commandName
        ) where TCommandResult : CommandResult {
            BsonDocument command = new BsonDocument(commandName, true);
            return RunCommand<BsonDocument, TCommandResult>(command);
        }

        public override string ToString() {
            return name;
        }
        #endregion

        #region internal methods
        internal MongoConnection GetConnection(
            bool slaveOk
        ) {
            return server.GetConnection(this, slaveOk);
        }

        internal void ReleaseConnection(
            MongoConnection connection
        ) {
            server.ReleaseConnection(connection);
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
