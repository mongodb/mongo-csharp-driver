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

        public int DataSize() {
            return Stats().GetInt32("size");
        }

        public IEnumerable<object> Distinct(
            string key
        ) {
            return Distinct(key, null);
        }

        public IEnumerable<object> Distinct(
            string key,
            BsonDocument query
        ) {
            var command = new BsonDocument {
                { "distinct", name },
                { "key", key },
                { "query", query }
            };
            var result = database.RunCommand(command);

            var values = (BsonArray) result["values"];
            return values.Elements.Select(e => e.Value);
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
            BsonJavaScript where
        ) where T : new() {
            BsonDocument query = new BsonDocument("$where", where);
            return new MongoCursor<T>(this, query);
        }

        public MongoCursor<T> Find<T>(
            BsonJavaScript where,
            BsonDocument fields
        ) where T : new() {
            BsonDocument query = new BsonDocument("$where", where);
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

        public BsonDocument FindAndModify(
            BsonDocument query,
            BsonDocument sort,
            BsonDocument update
        ) {
            return FindAndModify(query, sort, update, null, false);
        }

        public BsonDocument FindAndModify(
            BsonDocument query,
            BsonDocument sort,
            BsonDocument update,
            bool returnNew
        ) {
            return FindAndModify(query, sort, update, null, returnNew);
        }

        public BsonDocument FindAndModify(
            BsonDocument query,
            BsonDocument sort,
            BsonDocument update,
            BsonDocument fields,
            bool returnNew
        ) {
            var command = new BsonDocument {
                { "findAndModify", name },
                { "query", query },
                { "sort", sort },
                { "update", update },
                { "fields", fields },
                { returnNew, "new", true }
            };
            var result = database.RunCommand(command);
            return result.GetEmbeddedDocument("value");
        }

        public BsonDocument FindAndRemove(
            BsonDocument query,
            BsonDocument sort
        ) {
            var command = new BsonDocument {
                { "findAndModify", name },
                { "query", query },
                { "sort", sort },
                { "remove", true }
            };
            var result = database.RunCommand(command);
            return result.GetEmbeddedDocument("value");
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
            BsonJavaScript where
        ) where T : new() {
            BsonDocument query = new BsonDocument("$where", where);
            using (var cursor = new MongoCursor<T>(this, query).Limit(1)) {
                return cursor.FirstOrDefault();
            }
        }

        public T FindOne<T>(
            BsonJavaScript where,
            BsonDocument fields
        ) where T : new() {
            BsonDocument query = new BsonDocument("$where", where);
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

        public IEnumerable<BsonDocument> Group(
            BsonDocument keys,
            BsonDocument initial,
            BsonJavaScript reduce
        ) {
            return Group(null, keys, initial, reduce, null);
        }

        public IEnumerable<BsonDocument> Group(
            BsonDocument query,
            BsonDocument keys,
            BsonDocument initial,
            BsonJavaScript reduce
        ) {
            return Group(query, keys, initial, reduce, null);
        }

        public IEnumerable<BsonDocument> Group(
            BsonDocument query,
            BsonDocument keys,
            BsonDocument initial,
            BsonJavaScript reduce,
            BsonJavaScript finalize
        ) {
            var command = new BsonDocument {
                { "group",
                    new BsonDocument {
                        { "ns", name },
                        { "cond", query },
                        { "key", keys },
                        { "initial", initial },
                        { "$reduce", reduce },
                        { "finalize", finalize }
                    }
                }
            };
            var result = database.RunCommand(command);
            return ((BsonArray) result["retval"]).Values.Cast<BsonDocument>();
        }

        public IEnumerable<BsonDocument> Group(
            BsonDocument query,
            BsonJavaScript keyf,
            BsonDocument initial,
            BsonJavaScript reduce,
            BsonJavaScript finalize
        ) {
            var command = new BsonDocument {
                { "group",
                    new BsonDocument {
                        { "ns", name },
                        { "cond", query },
                        { "$keyf", keyf },
                        { "initial", initial },
                        { "$reduce", reduce },
                        { "finalize", finalize }
                    }
                }
            };
            var result = database.RunCommand(command);
            return ((BsonArray) result["retval"]).Values.Cast<BsonDocument>();
        }

        public IEnumerable<BsonDocument> Group(
            string key,
            BsonDocument initial,
            BsonJavaScript reduce
        ) {
            var keys = new BsonDocument(key, 1);
            return Group(null, keys, initial, reduce, null);
        }

        public BsonDocument Insert<T>(
            IEnumerable<T> documents
        ) {
            return Insert(documents, safeMode);
        }

        public BsonDocument Insert<T>(
            IEnumerable<T> documents,
            bool safeMode
        ) {
            if (assignObjectIdsOnInsert) {
                if (typeof(T) == typeof(BsonDocument)) {
                    AssignObjectIds((IEnumerable<BsonDocument>) documents);
                }
            }

            BsonArray batches = null;
            if (safeMode) {
                batches = new BsonArray();
            }

            MongoConnection connection = database.AcquireConnection();
            var message = new MongoInsertMessage(this);
            foreach (var document in documents) {
                message.AddDocument(document);
                if (message.MessageLength > Mongo.MaxMessageLength) {
                    byte[] lastDocument = message.RemoveLastDocument();
                    var intermediateError = connection.SendMessage(message, safeMode);
                    if (safeMode) { batches.Add(intermediateError); }
                    message.Reset(lastDocument);
                }
            }

            var lastError = connection.SendMessage(message, safeMode);
            if (safeMode) { batches.Add(lastError); }

            database.ReleaseConnection(connection);

            if (safeMode) {
                if (batches.Count() == 1) {
                    return (BsonDocument) batches[0];
                } else {
                    return new BsonDocument("batches", batches);
                }
            } else {
                return null;
            }
        }

        public BsonDocument Insert<T>(
            params T[] documents
        ) {
            return Insert((IEnumerable<T>) documents, safeMode);
        }

        public BsonDocument Insert<T>(
            T document,
            bool safeMode
        ) {
            return Insert((IEnumerable<T>) new T[] { document }, safeMode);
        }

        public BsonDocument Insert<T>(
            T[] documents,
            bool safeMode
        ) {
            return Insert((IEnumerable<T>) documents, safeMode);
        }

        public bool IsCapped() {
            throw new NotImplementedException();
        }

        public MongoMapReduceResult MapReduce(
            BsonDocument query,
            BsonJavaScript map,
            BsonJavaScript reduce
        ) {
            return MapReduce(query, map, reduce, null, null);
        }

        public MongoMapReduceResult MapReduce(
            BsonDocument query,
            BsonJavaScript map,
            BsonJavaScript reduce,
            BsonJavaScript finalize
        ) {
            return MapReduce(query, map, reduce, finalize, null);
        }

        public MongoMapReduceResult MapReduce(
            BsonDocument query,
            BsonJavaScript map,
            BsonJavaScript reduce,
            BsonJavaScript finalize,
            BsonDocument options
        ) {
            var command = new BsonDocument {
                { "mapreduce", name },
                { "query", query },
                { "map", map },
                { "reduce", reduce },
                options
            };
            var commandResult = database.RunCommand(command);
            return new MongoMapReduceResult(database, commandResult);
        }

        public MongoMapReduceResult MapReduce(
            BsonJavaScript map,
            BsonJavaScript reduce
        ) {
            return MapReduce(null, map, reduce, null, null);
        }

        public MongoMapReduceResult MapReduce(
            BsonJavaScript map,
            BsonJavaScript reduce,
            BsonJavaScript finalize
        ) {
            return MapReduce(null, map, reduce, finalize, null);
        }

        public void ReIndex() {
            throw new NotImplementedException();
        }

        public BsonDocument Remove(
            BsonDocument query
        ) {
            return Remove(query, RemoveFlags.None, safeMode);
        }

        public BsonDocument Remove(
            BsonDocument query,
            bool safeMode
        ) {
            return Remove(query, RemoveFlags.None, safeMode);
        }

        public BsonDocument Remove(
            BsonDocument query,
            RemoveFlags flags
        ) {
            return Remove(query, flags, safeMode);
        }

        public BsonDocument Remove(
           BsonDocument query,
           RemoveFlags flags,
           bool safeMode
        ) {
            // special case for query on _id
            if (query != null && query.Count == 1 && query.GetElement(0).Name == "_id" && query[0] is BsonObjectId) {
                flags |= RemoveFlags.Single;
            }

            var message = new MongoDeleteMessage(this, flags, query);

            var connection = database.AcquireConnection();
            var lastError = connection.SendMessage(message, safeMode);
            database.ReleaseConnection(connection);

            return lastError;
        }

        public BsonDocument RemoveAll() {
            return Remove(null, RemoveFlags.None, safeMode);
        }

        public BsonDocument RemoveAll(
           bool safeMode
        ) {
            return Remove(null, RemoveFlags.None, safeMode);
        }

        public void ResetIndexCache() {
            indexCache.Clear();
        }

        public BsonDocument Save(
            BsonDocument document
        ) {
            return Save(document, safeMode);
        }

        // only works with BsonDocuments for now
        // reason: how do we find the _id value for an arbitrary class?
        public BsonDocument Save(
            BsonDocument document,
            bool safeMode
        ) {
            object id = document["_id"];
            if (id == null) {
                id = BsonObjectId.GenerateNewId();
                document["_id"] = id; // TODO: do we need to make sure it's the first element?
                return Insert(document, safeMode);
            } else {
                var query = new BsonDocument("_id", id);
                return Update(query, document, UpdateFlags.Upsert, safeMode);
            }
        }

        public BsonDocument Stats() {
            var command = new BsonDocument("collstats", name);
            return database.RunCommand(command);
        }

        public int StorageSize() {
            return Stats().GetInt32("storageSize");
        }

        public int TotalIndexSize() {
            return Stats().GetInt32("totalIndexSize");
        }

        public int TotalSize() {
            var totalSize = StorageSize();
            var indexes = GetIndexes();
            foreach (var index in indexes) {
                var indexName = index.GetString("name");
                var indexCollectionName = string.Format("{0}.${1}", name, indexName);
                var indexCollection = database.GetCollection(indexCollectionName);
                totalSize += indexCollection.DataSize();
            }
            return totalSize;
        }

        public override string ToString() {
 	         return FullName;
        }

        public BsonDocument Update<U>(
            BsonDocument query,
            U update
        ) where U : new() {
            return Update<U>(query, update, UpdateFlags.None, safeMode);
        }

        public BsonDocument Update<U>(
            BsonDocument query,
            U update,
            bool safeMode
        ) where U : new() {
            return Update<U>(query, update, UpdateFlags.None, safeMode);
        }

        public BsonDocument Update<U>(
            BsonDocument query,
            U update,
            UpdateFlags flags
        ) where U : new() {
            return Update<U>(query, update, flags, safeMode);
        }

        public BsonDocument Update<U>(
            BsonDocument query,
            U update,
            UpdateFlags flags,
            bool safeMode
        ) where U : new() {
            var message = new MongoUpdateMessage<U>(this, flags, query, update);

            var connection = database.AcquireConnection();
            var lastError = connection.SendMessage(message, safeMode);
            database.ReleaseConnection(connection);

            return lastError;
        }

        public BsonDocument Validate() {
            var command = new BsonDocument("validate", name);
            return database.RunCommand(command);
        }
        #endregion

        #region private methods
        private void AssignObjectIds(
            IEnumerable<BsonDocument> documents
        ) {
            foreach (var document in documents) {
                if (!document.ContainsElement("_id")) {
                    document["_id"] = BsonObjectId.GenerateNewId();
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
