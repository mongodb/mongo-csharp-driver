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
        private MongoCredentials defaultCredentials;
        private Dictionary<string, MongoCollection> collections = new Dictionary<string, MongoCollection>();
        #endregion

        #region constructors
        internal MongoDatabase(
            MongoServer server,
            string name
        ) {
            this.server = server;
            this.name = name;
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

        public MongoCredentials DefaultCredentials {
            get { return defaultCredentials; }
            set { defaultCredentials = value; }
        }
        #endregion

        #region public indexers
        public MongoCollection this[
            string name
        ] {
            get { return GetCollection(name); }
        }
        #endregion

        #region public properties
        public string Name {
            get { return name; }
        }
        #endregion

        #region public methods
        public void AddUser(
            string username,
            string password
        ) {
            throw new NotImplementedException();
        }

        public bool CollectionExists(
            string name
        ) {
            return GetCollectionNames().Contains(name);
        }

        public void CreateCollection(
            string name,
            BsonDocument options
        ) {
            throw new NotImplementedException();
        }
           
        public void DropCollection(
            string name
        ) {
            BsonDocument command = new BsonDocument {
                { "drop", name }
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
           string name
        ) {
            MongoCollection collection;
            if (!collections.TryGetValue(name, out collection)) {
                collection = new MongoCollection(this, name);
                collections[name] = collection;
            }
            return collection;
        }

        public MongoCollection<T> GetCollection<T>(
            string name
        ) where T : new() {
            MongoCollection collection;
            string key = string.Format("{0}<{1}>", name, typeof(T).FullName);
            if (!collections.TryGetValue(key, out collection)) {
                collection = new MongoCollection<T>(this, name);
                collections[name] = collection;
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

        public BsonDocument GetStats() {
            return RunCommand("dbstats");
        }

        public void RequestDone() {
            throw new NotImplementedException();
        }

        public void RequestStart() {
            throw new NotImplementedException();
        }

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

        public override string ToString() {
            return name;
        }
        #endregion
    }
}
