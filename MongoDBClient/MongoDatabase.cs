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

using MongoDB.BsonLibrary;
using MongoDB.MongoDBClient.Internal;

namespace MongoDB.MongoDBClient {
    public class MongoDatabase {
        #region private fields
        private MongoServer server;
        private string name;
        private MongoCredentials credentials;
        private bool safeMode;
        private Dictionary<string, MongoCollection> collections = new Dictionary<string, MongoCollection>();
        #endregion

        #region constructors
        public MongoDatabase(
            MongoServer server,
            string name
        ) {
            ValidateName(name);
            this.server = server;
            this.name = name;
            this.safeMode = server.SafeMode;
        }

        public MongoDatabase(
            MongoServer server,
            string name,
            MongoCredentials credentials
        ) {
            ValidateName(name);
            this.server = server;
            this.name = name;
            this.credentials = credentials;
            this.safeMode = server.SafeMode;
        }
        #endregion

        #region factory methods
        public static MongoDatabase FromConnectionString(
            string connectionString
        ) {
            if (connectionString.StartsWith("mongodb://")) {
                MongoUrl url = new MongoUrl(connectionString);
                return FromMongoUrl(url);
            } else {
                MongoConnectionStringBuilder builder = new MongoConnectionStringBuilder(connectionString);
                return FromMongoConnectionStringBuilder(builder);
            }
        }

        internal static MongoDatabase FromMongoConnectionSettings(
            IMongoConnectionSettings settings
        ) {
            if (settings.Database == null) {
                throw new ArgumentException("Connection string must have database name");
            }
            MongoServer server = MongoServer.FromMongoConnectionSettings(settings);
            return server.GetDatabase(settings.Database);
        }

        public static MongoDatabase FromMongoConnectionStringBuilder(
            MongoConnectionStringBuilder builder
        ) {
            return FromMongoConnectionSettings(builder);
        }

        public static MongoDatabase FromMongoUrl(
            MongoUrl url
        ) {
            return FromMongoConnectionSettings(url);
        }

        public static MongoDatabase FromUri(
            Uri uri
        ) {
            return FromMongoUrl(new MongoUrl(uri.ToString()));
        }
        #endregion

        #region public properties
        public MongoServer Server {
            get { return server; }
        }

        public string Name {
            get { return name; }
        }

        public MongoCredentials Credentials {
            get { return credentials; }
        }

        public bool SafeMode {
            get { return safeMode; }
            set { safeMode = value; }
        }
        #endregion

        #region public indexers
        public MongoCollection this[
            string collectionName
        ] {
            get { return GetCollection(collectionName); }
        }
        #endregion

        #region public methods
        public void AddUser(
            MongoCredentials credentials
        ) {
            throw new NotImplementedException();
        }

        public bool CollectionExists(
            string collectionName
        ) {
            return GetCollectionNames().Contains(collectionName);
        }

        public void CreateCollection(
            string collectionName,
            BsonDocument options
        ) {
            throw new NotImplementedException();
        }

        public BsonDocument CurrentOp() {
            throw new NotImplementedException();
        }
           
        public void DropCollection(
            string collectionName
        ) {
            BsonDocument command = new BsonDocument {
                { "drop", collectionName }
            };
            RunCommand(command);
        }

        public object Eval(
            string code,
            params object[] args
        ) {
            BsonDocument command = new BsonDocument {
                { "$eval", code },
                { "args", args }
            };
            var result = RunCommand(command);
            return result["retval"];
        }

        public MongoCollection GetCollection(
           string collectionName
        ) {
            MongoCollection collection;
            if (!collections.TryGetValue(collectionName, out collection)) {
                collection = new MongoCollection(this, collectionName);
                collections[collectionName] = collection;
            }
            return collection;
        }

        public MongoCollection<T> GetCollection<T>(
            string collectionName
        ) where T : new() {
            MongoCollection collection;
            string key = string.Format("{0}<{1}>", collectionName, typeof(T).FullName);
            if (!collections.TryGetValue(key, out collection)) {
                collection = new MongoCollection<T>(this, collectionName);
                collections[collectionName] = collection;
            }
            return (MongoCollection<T>) collection;
        }

        public List<string> GetCollectionNames() {
            List<string> collectionNames = new List<string>();
            MongoCollection namespaces = GetCollection("system.namespaces");
            var prefix = name + ".";
            foreach (BsonDocument ns in namespaces.FindAll<BsonDocument>()) {
                string collectionName = (string) ns["name"];
                if (!collectionName.StartsWith(prefix)) { continue; }
                if (collectionName.Contains('$')) { continue; }
                collectionNames.Add(collectionName);
            }
            collectionNames.Sort();
            return collectionNames;
        }

        // TODO: mongo shell has GetLastError at the database level?
        // TODO: mongo shell has GetPrevError at the database level?
        // TODO: mongo shell has GetProfilingLevel at the database level?
        // TODO: mongo shell has GetReplicationInfo at the database level?

        public MongoDatabase GetSisterDatabase(
            string databaseName
        ) {
            return server.GetDatabase(databaseName);
        }

        public BsonDocument GetStats() {
            return RunCommand("dbstats");
        }

        // TODO: mongo shell has IsMaster at database level?

        public void RemoveUser(
            string username
        ) {
            throw new NotImplementedException();
        }

        public void RequestDone() {
            throw new NotImplementedException();
        }

        public void RequestStart() {
            throw new NotImplementedException();
        }

        // TODO: mongo shell has ResetError at the database level

        public BsonDocument RunCommand(
            BsonDocument command
        ) {
            MongoCollection commandCollection = GetCollection("$cmd");
            BsonDocument result = commandCollection.FindOne<BsonDocument>(command);

            object ok = result["ok"];
            if (ok == null) {
                throw new MongoException("ok element is missing");
            }
            if (
                ok is bool && !((bool) ok) ||
                ok is int && (int) ok != 1 ||
                ok is double && (double) ok != 1.0
            ) {
                string commandName = (string) command.GetElement(0).Name;
                string errorMessage = (string) result["errmsg"] ?? "Unknown error";
                string message = string.Format("Command {0} failed ({1})", commandName, errorMessage);
                throw new MongoException(message);
            }

            return result;
        }

        public BsonDocument RunCommand(
            string commandName
        ) {
            BsonDocument command = new BsonDocument {
                { commandName, true }
            };
            return RunCommand(command);
        }

        // TODO: 

        public override string ToString() {
            return name;
        }
        #endregion

        #region private methods
        private void ValidateName(
            string name
        ) {
            if (name == null) {
                throw new NotImplementedException();
            }
            if (
                name == "" ||
                name.IndexOfAny(new char[] { '\0', ' ', '.', '$', '/', '\\' }) != -1 ||
                name != name.ToLower() ||
                Encoding.UTF8.GetBytes(name).Length > 64
            ) {
                throw new MongoException("Invalid database name");
            }
        }
        #endregion
    }
}
