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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using MongoDB.BsonLibrary;
using MongoDB.MongoDBClient.Internal;

namespace MongoDB.MongoDBClient {
    public class MongoCollection {
        #region private fields
        private MongoDatabase database;
        private string name;
        private bool safeMode;
        private bool assignObjectIdsOnInsert = true;
        private HashSet<string> indexCache = new HashSet<string>();
        #endregion

        #region constructors
        public MongoCollection(
            MongoDatabase database,
            string name
        ) {
            ValidateCollectionName(name);
            this.database = database;
            this.name = name;
            this.safeMode = database.SafeMode;
        }
        #endregion

        #region public properties
        public MongoDatabase Database {
            get { return database; }
        }

        public string FullName {
            get { return database.Name + "." + name; }
        }

        public string Name {
            get { return name; }
        }

        public bool SafeMode {
            get { return safeMode; }
            set { safeMode = value; }
        }

        public bool AssignObjectIdsOnInsert {
            get { return assignObjectIdsOnInsert; }
            set { assignObjectIdsOnInsert = value; }
        }
        #endregion

        #region public methods
        public int Count() {
            return Count(null);
        }

        public int Count(
            BsonDocument query
        ) {
            BsonDocument command = new BsonDocument {
                { "count", name },
                { "query", query ?? new BsonDocument() }
            };
            var result = database.RunCommand(command);
            return (int) result.GetDouble("n");
        }

        public void CreateIndex(
            BsonDocument keys
        ) {
            BsonDocument options = null;
            CreateIndex(keys, options);
        }

        public void CreateIndex(
            BsonDocument keys,
            BsonDocument options
        ) {
            var indexes = database.GetCollection("system.indexes");
            var indexName = ((options != null) ? options.GetString("name") : null) ?? GetIndexName(keys);
            var index = new BsonDocument {
                { "name", indexName },
                { "ns", FullName },
                { "key", keys }
            };
            if (options != null) {
                foreach (var element in options) {
                    index[element.Name] = element.Value;
                }
            }
            indexes.Insert(index, true);
            indexCache.Add(indexName);
        }

        public void CreateIndex(
            BsonDocument keys,
            string indexName
        ) {
            CreateIndex(keys, indexName, false);
        }

        public void CreateIndex(
            BsonDocument keys,
            string indexName,
            bool unique
        ) {
            BsonDocument options = new BsonDocument {
                { "name", indexName },
                { unique, "unique", true }
            };
            CreateIndex(keys, options);
        }

        public void CreateIndex(
            params string[] keyNames
        ) {
            BsonDocument keys = new BsonDocument();
            foreach (string keyName in keyNames) {
                keys.Add(keyName, 1);
            }
            CreateIndex(keys);
        }

        // TODO: any arguments?
        public long DataSize() {
            throw new NotImplementedException();
        }

        public List<BsonDocument> Distinct(
            BsonDocument keys
        ) {
            return Distinct(keys, null);
        }

        public List<BsonDocument> Distinct(
            BsonDocument keys,
            BsonDocument query
        ) {
            throw new NotImplementedException();
        }

        public void DropAllIndexes() {
            DropIndex("*");
        }

        public void DropIndex(
            BsonDocument keys
        ) {
            string indexName = GetIndexName(keys);
            DropIndex(indexName);
        }

        public void DropIndex(
            params string[] keyNames
        ) {
            string indexName = GetIndexName(keyNames);
            DropIndex(indexName);
        }

        public void DropIndex(
            string indexName
        ) {
            var command = new BsonDocument {
                { "deleteIndexes", FullName },
                { "index", indexName }
            };
            database.RunCommand(command);
            ResetIndexCache(); // TODO: what if RunCommand throws an exception
        }

        public void EnsureIndex(
            BsonDocument keys
        ) {
            string indexName = GetIndexName(keys);
            if (!indexCache.Contains(indexName)) {
                CreateIndex(keys, indexName);
            }
        }

        public void EnsureIndex(
           BsonDocument keys,
           BsonDocument options
        ) {
            string indexName = GetIndexName(keys);
            if (!indexCache.Contains(indexName)) {
                CreateIndex(keys, options);
            }
        }

        public void EnsureIndex(
            BsonDocument keys,
            string indexName
        ) {
            if (!indexCache.Contains(indexName)) {
                CreateIndex(keys, indexName);
            }
        }

        public void EnsureIndex(
           BsonDocument keys,
           string indexName,
           bool unique
        ) {
            if (!indexCache.Contains(indexName)) {
                CreateIndex(keys, indexName, unique);
            }
        }

        public void EnsureIndex(
            params string[] keyNames
        ) {
            string indexName = GetIndexName(keyNames);
            if (!indexCache.Contains(indexName)) {
                CreateIndex(keyNames);
            }
        }

        public MongoCursor<T> Find<T>(
            BsonDocument query
        ) where T : new() {
            return new MongoCursor<T>(this, query);
        }

        public MongoCursor<T> Find<T>(
            BsonDocument query,
            BsonDocument fields
        ) where T : new() {
            return new MongoCursor<T>(this, query, fields);
        }

        public MongoCursor<T> Find<T>(
            string where
        ) where T : new() {
            BsonDocument query = new BsonDocument("$where", new BsonJavaScriptCode(where));
            return new MongoCursor<T>(this, query);
        }

        public MongoCursor<T> Find<T>(
            string where,
            BsonDocument fields
        ) where T : new() {
            BsonDocument query = new BsonDocument("$where", new BsonJavaScriptCode(where));
            return new MongoCursor<T>(this, query, fields);
        }

        public MongoCursor<T> FindAll<T>() where T : new() {
            return new MongoCursor<T>(this, null);
        }

        public MongoCursor<T> FindAll<T>(
            BsonDocument fields
        ) where T : new() {
            return new MongoCursor<T>(this, null, fields);
        }

        public void FindAndModify() {
            throw new NotImplementedException();
        }

        public T FindOne<T>() where T : new() {
            using (var cursor = new MongoCursor<T>(this, null).Limit(1)) {
                return cursor.FirstOrDefault();
            }
        }

        public T FindOne<T>(
            BsonDocument query
        ) where T : new() {
            using (var cursor = new MongoCursor<T>(this, query).Limit(1)) {
                return cursor.FirstOrDefault();
            }
        }

        public T FindOne<T>(
            BsonDocument query,
            BsonDocument fields
        ) where T : new() {
            using (var cursor = new MongoCursor<T>(this, query, fields).Limit(1)) {
                return cursor.FirstOrDefault();
            }
        }

        public T FindOne<T>(
            string where
        ) where T : new() {
            BsonDocument query = new BsonDocument("$where", new BsonJavaScriptCode(where));
            using (var cursor = new MongoCursor<T>(this, query).Limit(1)) {
                return cursor.FirstOrDefault();
            }
        }

        public T FindOne<T>(
            string where,
            BsonDocument fields
        ) where T : new() {
            BsonDocument query = new BsonDocument("$where", new BsonJavaScriptCode(where));
            using (var cursor = new MongoCursor<T>(this, query, fields).Limit(1)) {
                return cursor.FirstOrDefault();
            }
        }

        public List<BsonDocument> GetIndexes() {
            var indexes = database.GetCollection("system.indexes");
            var query = new BsonDocument("ns", FullName);
            var info = new List<BsonDocument>(indexes.Find<BsonDocument>(query));
            return info;
        }

        public BsonDocument GetStats() {
            throw new NotImplementedException();
        }

        // TODO: order of arguments is different in mongo shell!
        public T Group<T>(
            BsonDocument keys,
            BsonDocument condition,
            BsonDocument initial,
            string reduce
        ) {
            throw new NotImplementedException();
        }

        public MongoWriteResult Insert<T>(
            IEnumerable<T> documents
        ) {
            return Insert(documents, safeMode);
        }

        public MongoWriteResult Insert<T>(
            IEnumerable<T> documents,
            bool safeMode
        ) {
            if (assignObjectIdsOnInsert) {
                if (typeof(T) == typeof(BsonDocument)) {
                    AssignObjectIds((IEnumerable<BsonDocument>) documents);
                }
            }

            var message = new MongoInsertMessage(this);
            foreach (var document in documents) {
                message.AddDocument(document);
                if (message.MessageLength > Mongo.MaxMessageLength) {
                    byte[] bsonDocument = message.RemoveLastDocument();
                    SendInsertMessage(message, safeMode);
                    message.Reset(bsonDocument);
                }
            }

            SendInsertMessage(message, safeMode);
            return null; // TODO: return what?
        }

        public MongoWriteResult Insert<T>(
            params T[] documents
        ) {
            return Insert((IEnumerable<T>) documents, safeMode);
        }

        public MongoWriteResult Insert<T>(
            T document,
            bool safeMode
        ) {
            return Insert((IEnumerable<T>) new T[] { document }, safeMode);
        }

        public MongoWriteResult Insert<T>(
            T[] documents,
            bool safeMode
        ) {
            return Insert((IEnumerable<T>) documents, safeMode);
        }

        public bool IsCapped() {
            throw new NotImplementedException();
        }

        // TODO: order of arguments is different in mongo shell
        public MongoMapReduceResult MapReduce(
            BsonDocument query,
            string map,
            string reduce,
            string outputCollection
        ) {
            throw new NotImplementedException();
        }

        public void ReIndex() {
            throw new NotImplementedException();
        }

        public MongoWriteResult Remove(
            BsonDocument query
        ) {
            return Remove(query, safeMode);
        }

        public MongoWriteResult Remove(
           BsonDocument query,
           bool safeMode
        ) {
            throw new NotImplementedException();
        }

        // TODO: what is dropTarget parameter in mongo shell?
        public void Rename(
            string newCollectionName
        ) {
            throw new NotImplementedException();
        }

        public void ResetIndexCache() {
            indexCache.Clear();
        }

        public MongoWriteResult Save<T>(
            T document
        ) {
            return Save<T>(document, safeMode);
        }

        public MongoWriteResult Save<T>(
            T document,
            bool safeMode
        ) {
            throw new NotImplementedException();
        }

        public long StorageSize() {
            throw new NotImplementedException();
        }

        public long TotalIndexSize() {
            throw new NotImplementedException();
        }

        public long TotalSize() {
            throw new NotImplementedException();
        }

        public override string ToString() {
 	         return FullName;
        }

        public MongoWriteResult Update<T>(
            BsonDocument query,
            T update
        ) {
            return Update<T>(query, update, false, false, safeMode);
        }

        public MongoWriteResult Update<T>(
            BsonDocument query,
            T update,
            bool safeMode
        ) {
            return Update<T>(query, update, false, false, safeMode);
        }

        public MongoWriteResult Update<T>(
            BsonDocument query,
            T update,
            bool upsert,
            bool multi
        ) {
            return Update<T>(query, update, upsert, multi, safeMode);
        }

        public MongoWriteResult Update<T>(
            BsonDocument query,
            T update,
            bool upsert,
            bool multi,
            bool safeMode
        ) {
            throw new NotImplementedException();
        }

        public MongoWriteResult UpdateMulti<T>(
            BsonDocument query,
            T update
        ) {
            return Update<T>(query, update, false, true, safeMode);
        }

        public MongoWriteResult UpdateMulti<T>(
            BsonDocument query,
            T update,
            bool safeMode
        ) {
            return Update<T>(query, update, false, true, safeMode);
        }

        public void Validate() {
            throw new NotImplementedException();
        }
        #endregion

        #region private methods
        private void AssignObjectIds(
            IEnumerable<BsonDocument> documents
        ) {
            foreach (var document in documents) {
                if (!document.ContainsElement("_id")) {
                    // TODO: assign ObjectId
                }
            }
        }

        private string GetIndexName(
            BsonDocument keys
        ) {
            StringBuilder sb = new StringBuilder();
            foreach (var element in keys) {
                string name = element.Name;
                object value = element.Value;
                if (sb.Length > 0) {
                    sb.Append("_");
                }
                sb.Append(name);
                if (
                    value.GetType() == typeof(int) ||
                    value.GetType() == typeof(long) ||
                    value.GetType() == typeof(double) ||
                    value.GetType() == typeof(string)
                ) {
                    sb.Append(value.ToString().Replace(' ', '_'));
                }
            }
            return sb.ToString();
        }

        private string GetIndexName(
            string[] keyNames
        ) {
            StringBuilder sb = new StringBuilder();
            foreach (string name in keyNames) {
                if (sb.Length > 0) {
                    sb.Append("_");
                }
                sb.Append(name);
                sb.Append("_1");
            }
            return sb.ToString();
        }

        private void SendInsertMessage(
            MongoInsertMessage insertMessage,
            bool safeMode
        ) {
            if (safeMode) {
                insertMessage.AddGetLastError();
            }

            MongoConnection connection = database.GetConnection();
            connection.SendMessage(insertMessage);
            if (safeMode) {
                connection.CheckLastError();
            }
        }

        private void ValidateCollectionName(
            string name
        ) {
            if (name == null) {
                throw new ArgumentNullException("name");
            }
            if (
                name == "" ||
                name.Contains('\0') ||
                Encoding.UTF8.GetBytes(name).Length > 121
            ) {
                throw new MongoException("Invalid collection name");
            }
        }
        #endregion
    }

    public class MongoCollection<T> : MongoCollection where T : new() {
        #region constructors
        public MongoCollection(
            MongoDatabase database,
            string name
        )
            : base(database, name) {
        }
        #endregion

        #region public methods
        public MongoCursor<T> Find(
            BsonDocument query
        ) {
            return Find<T>(query);
        }

        public MongoCursor<T> Find(
            BsonDocument query,
            BsonDocument fields
        ) {
            return Find<T>(query, fields);
        }

        public MongoCursor<T> Find(
            string where
        ) {
            return Find<T>(where);
        }

        public MongoCursor<T> Find(
            string where,
            BsonDocument fields
        ) {
            return Find<T>(where, fields);
        }

        public MongoCursor<T> FindAll() {
            return FindAll<T>();
        }

        public MongoCursor<T> FindAll(
            BsonDocument fields
        ) {
            return FindAll<T>(fields);
        }

        public T FindOne() {
            return FindOne<T>();
        }

        public T FindOne(
            BsonDocument query
        ) {
            return FindOne<T>(query);
        }

        public T FindOne(
            BsonDocument query,
            BsonDocument fields
        ) {
            return FindOne<T>(query, fields);
        }

        public T FindOne(
            string where
        ) {
            return FindOne<T>(where);
        }

        public T FindOne(
            string where,
            BsonDocument fields
        ) {
            return FindOne<T>(where, fields);
        }
        #endregion
    }
}
