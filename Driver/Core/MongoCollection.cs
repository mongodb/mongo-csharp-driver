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

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver {
    public abstract class MongoCollection {
        #region private fields
        private MongoDatabase database;
        private string name;
        private SafeMode safeMode;
        private bool assignIdOnInsert = true;
        private HashSet<string> indexCache = new HashSet<string>(); // serves as its own lock object also
        #endregion

        #region constructors
        protected MongoCollection(
            MongoDatabase database,
            string name,
            SafeMode safeMode
        ) {
            ValidateCollectionName(name);
            this.database = database;
            this.name = name;
            this.safeMode = safeMode;
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

        public SafeMode SafeMode {
            get { return safeMode; }
        }

        public bool AssignIdOnInsert {
            get { return assignIdOnInsert; }
            set { assignIdOnInsert = value; }
        }
        #endregion

        #region public methods
        public int Count() {
            return Count<BsonDocument>(null);
        }

        public int Count<TQuery>(
            TQuery query
        ) {
            var command = new BsonDocument {
                { "count", name },
                { "query", BsonDocumentWrapper.Create(query) } // query is optional
            };
            var result = database.RunCommand(command);
            return result["n"].ToInt32();
        }

        public BsonDocument CreateIndex<TIndexKeys, TIndexOptions>(
            TIndexKeys keys,
            TIndexOptions options
        ) {
            var keysDocument = keys.ToBsonDocument();
            var optionsDocument = options.ToBsonDocument();
            var indexes = database.GetCollection("system.indexes");
            var indexName = (optionsDocument != null && optionsDocument.Contains("name")) ? optionsDocument["name"].AsString : GetIndexName(keysDocument);
            var index = new BsonDocument {
                { "name", indexName },
                { "ns", FullName },
                { "key", keysDocument }
            };
            index.Merge(optionsDocument);
            var result = indexes.Insert(index, SafeMode.True);
            return result;
        }

        public BsonDocument CreateIndex<TIndexKeys>(
            TIndexKeys keys
        ) {
            return CreateIndex(keys, IndexOptions.None);
        }

        public BsonDocument CreateIndex(
            params string[] keyNames
        ) {
            return CreateIndex(IndexKeys.Ascending(keyNames));
        }

        public int DataSize() {
            return Stats()["size"].ToInt32();
        }

        public IEnumerable<BsonValue> Distinct(
            string key
        ) {
            BsonDocument query = null;
            return Distinct(key, query);
        }

        public IEnumerable<BsonValue> Distinct<TQuery>(
            string key,
            TQuery query
        ) {
            var command = new BsonDocument {
                { "distinct", name },
                { "key", key },
                { "query", BsonDocumentWrapper.Create(query) } // query is optional
            };
            var result = database.RunCommand(command);
            return result["values"].AsBsonArray;
        }

        public BsonDocument DropAllIndexes() {
            return DropIndexByName("*");
        }

        public BsonDocument DropIndex<TIndexKeys>(
            TIndexKeys keys
        ) {
            var keysDocument = keys.ToBsonDocument();
            string indexName = GetIndexName(keysDocument);
            return DropIndexByName(indexName);
        }

        public BsonDocument DropIndex(
            params string[] keyNames
        ) {
            string indexName = GetIndexName(keyNames);
            return DropIndexByName(indexName);
        }

        public BsonDocument DropIndexByName(
            string indexName
        ) {
            lock (indexCache) {
                var command = new BsonDocument {
                    { "deleteIndexes", name }, // not FullName
                    { "index", indexName }
                };
                var result = database.RunCommand(command);
                ResetIndexCache(); // TODO: what if RunCommand throws an exception
                return result;
            }
        }

        public void EnsureIndex<TIndexKeys, TIndexOptions>(
           TIndexKeys keys,
           TIndexOptions options
        ) {
            lock (indexCache) {
                var keysDocument = keys.ToBsonDocument();
                var optionsDocument = options.ToBsonDocument();
                var indexName = (optionsDocument != null && optionsDocument.Contains("name")) ? optionsDocument["name"].AsString : GetIndexName(keysDocument);
                if (!indexCache.Contains(indexName)) {
                    CreateIndex(keysDocument, optionsDocument);
                    indexCache.Add(indexName);
                }
            }
        }

        public void EnsureIndex<TIndexKeys>(
            TIndexKeys keys
        ) {
            EnsureIndex(keys, IndexOptions.None);
        }

        public void EnsureIndex(
            params string[] keyNames
        ) {
            lock (indexCache) {
                string indexName = GetIndexName(keyNames);
                if (!indexCache.Contains(indexName)) {
                    CreateIndex(IndexKeys.Ascending(keyNames), IndexOptions.SetName(indexName));
                    indexCache.Add(indexName);
                }
            }
        }

        public MongoCursor<BsonDocument, TDocument> FindAllAs<TDocument>() {
            BsonDocument query = null;
            return FindAs<BsonDocument, TDocument>(query);
        }

        public BsonDocument FindAndModify<TQuery, TSortBy, TUpdate>(
            TQuery query,
            TSortBy sortBy,
            TUpdate update
        ) {
            BsonDocument fields = null;
            return FindAndModify(query, sortBy, update, fields, false);
        }

        public BsonDocument FindAndModify<TQuery, TSortBy, TUpdate>(
            TQuery query,
            TSortBy sortBy,
            TUpdate update,
            bool returnNew
        ) {
            BsonDocument fields = null;
            return FindAndModify(query, sortBy, update, fields, returnNew);
        }

        public BsonDocument FindAndModify<TQuery, TSortBy, TUpdate, TFields>(
            TQuery query,
            TSortBy sortBy,
            TUpdate update,
            TFields fields,
            bool returnNew
        ) {
            var command = new BsonDocument {
                { "findAndModify", name },
                { "query", BsonDocumentWrapper.Create(query) },
                { "sort", BsonDocumentWrapper.Create(sortBy) },
                { "update", BsonDocumentWrapper.Create(update) },
                { "fields", BsonDocumentWrapper.Create(fields) },
                { "new", true, returnNew }
            };
            var result = database.RunCommand(command);
            return result["value"].AsBsonDocument;
        }

        public BsonDocument FindAndRemove<TQuery, TSortBy>(
            TQuery query,
            TSortBy sortBy
        ) {
            var command = new BsonDocument {
                { "findAndModify", name },
                { "query", BsonDocumentWrapper.Create(query) },
                { "sort", BsonDocumentWrapper.Create(sortBy) },
                { "remove", true }
            };
            var result = database.RunCommand(command);
            return result["value"].AsBsonDocument;
        }

        public MongoCursor<IBsonSerializable, TDocument> FindAs<TDocument>(
           IBsonSerializable query
       ) {
            return FindAs<IBsonSerializable, TDocument>(query);
        }

        public MongoCursor<TQuery, TDocument> FindAs<TQuery, TDocument>(
            TQuery query
        ) {
            return new MongoCursor<TQuery, TDocument>(this, query);
        }

        public TDocument FindOneAs<TDocument>() {
            return FindAllAs<TDocument>().SetLimit(1).FirstOrDefault();
        }

        public TDocument FindOneAs<TDocument>(
            IBsonSerializable query
        ) {
            return FindOneAs<IBsonSerializable, TDocument>(query);
        }

        public TDocument FindOneAs<TQuery, TDocument>(
            TQuery query
        ) {
            return FindAs<TQuery, TDocument>(query).SetLimit(1).FirstOrDefault();
        }

        public BsonDocument GeoNear<TQuery>(
            TQuery query,
            double x,
            double y,
            int limit
        ) {
            var command = new BsonDocument {
                { "geoNear", name },
                { "near", new BsonArray { x, y } },
                { "num", limit },
                { "query", BsonDocumentWrapper.Create(query) } // query is optional
            };
            return database.RunCommand(command);
        }

        public IEnumerable<BsonDocument> GetIndexes() {
            var indexes = database.GetCollection("system.indexes");
            var query = new BsonDocument("ns", FullName);
            return indexes.Find(query).ToList(); // force query to execute before returning
        }

        public IEnumerable<BsonDocument> Group<TGroupBy, TQuery>(
            TGroupBy groupBy,
            TQuery query,
            BsonDocument initial,
            BsonJavaScript reduce,
            BsonJavaScript finalize
        ) {
            BsonElement keyElement;
            var keyFunction = groupBy as BsonJavaScript;
            if (keyFunction == null) {
                keyElement = new BsonElement("key", BsonDocumentWrapper.Create(groupBy));
            } else {
                keyElement = new BsonElement("$keyf", keyFunction);
            }

            var command = new BsonDocument {
                { "group", new BsonDocument {
                    { "ns", name },
                    { "condition", BsonDocumentWrapper.Create(query) }, // condition is optional
                    keyElement, // name is either "key" or "$keyf"
                    { "initial", initial },
                    { "$reduce", reduce },
                    { "finalize", finalize }
                } }
            };
            var result = database.RunCommand(command);
            return result["retval"].AsBsonArray.Values.Cast<BsonDocument>();
        }

        public IEnumerable<BsonDocument> Group<TQuery>(
            string key,
            TQuery query,
            BsonDocument initial,
            BsonJavaScript reduce,
            BsonJavaScript finalize
        ) {
            return Group(GroupBy.Keys(key), query, initial, reduce, finalize);
        }

        public bool IndexExists<TIndexKeys>(
            TIndexKeys keys
        ) {
            var keysDocument = keys.ToBsonDocument();
            string indexName = GetIndexName(keysDocument);
            return IndexExistsByName(indexName);
        }

        public bool IndexExists(
            params string[] keyNames
        ) {
            string indexName = GetIndexName(keyNames);
            return IndexExistsByName(indexName);
        }

        public bool IndexExistsByName(
            string indexName
        ) {
            var indexes = database.GetCollection("system.indexes");
            var query = Query.And(
                Query.EQ("name", indexName),
                Query.EQ("ns", FullName)
            );
            return indexes.Count(query) != 0;
        }

        // WARNING: be VERY careful about adding any new overloads of Insert or InsertBatch (just don't do it!)
        // it's very easy for the compiler to end up inferring the wrong type for TDocument!
        // that's also why Insert and InsertBatch have to have different names

        public BsonDocument Insert<TDocument>(
            TDocument document
        ) {
            return Insert(document, safeMode);
        }

        public BsonDocument Insert<TDocument>(
            TDocument document,
            SafeMode safeMode
        ) {
            return InsertBatch<TDocument>(new TDocument[] { document }, safeMode);
        }

        public BsonDocument InsertBatch<TDocument>(
            IEnumerable<TDocument> documents
        ) {
            return InsertBatch<TDocument>(documents, safeMode);
        }

        public BsonDocument InsertBatch<TDocument>(
            IEnumerable<TDocument> documents,
            SafeMode safeMode
        ) {
            BsonArray batches = null;
            if (safeMode.Enabled) {
                batches = new BsonArray();
            }

            MongoConnection connection = database.GetConnection(false); // not slaveOk

            using (var message = new MongoInsertMessage(FullName)) {
                message.WriteToBuffer(); // must be called before AddDocument

                foreach (var document in documents) {
                    if (assignIdOnInsert) {
                        var serializer = BsonSerializer.LookupSerializer(document.GetType());
                        object id;
                        IBsonIdGenerator idGenerator;
                        if (serializer.GetDocumentId(document, out id, out idGenerator)) {
                            if (idGenerator != null && idGenerator.IsEmpty(id)) {
                                id = idGenerator.GenerateId();
                                serializer.SetDocumentId(document, id);
                            }
                        }
                    }
                    message.AddDocument(document);

                    if (message.MessageLength > MongoDefaults.MaxMessageLength) {
                        byte[] lastDocument = message.RemoveLastDocument();
                        var intermediateError = connection.SendMessage(message, safeMode);
                        if (safeMode.Enabled) { batches.Add(intermediateError); }
                        message.ResetBatch(lastDocument);
                    }
                }

                var lastError = connection.SendMessage(message, safeMode);
                if (safeMode.Enabled) { batches.Add(lastError); }
            }

            database.ReleaseConnection(connection);

            if (safeMode.Enabled) {
                if (batches.Count() == 1) {
                    return batches[0].AsBsonDocument;
                } else {
                    return new BsonDocument("batches", batches);
                }
            } else {
                return null;
            }
        }

        public bool IsCapped() {
            throw new NotImplementedException();
        }

        public MongoMapReduceResult MapReduce<TMapReduceOptions>(
            BsonJavaScript map,
            BsonJavaScript reduce,
            TMapReduceOptions options
        ) {
            var command = new BsonDocument {
                { "mapreduce", name },
                { "map", map },
                { "reduce", reduce }
            };
            command.Merge(options.ToBsonDocument());
            var result = database.RunCommand(command);
            return new MongoMapReduceResult(database, result);
        }

        public MongoMapReduceResult MapReduce<TQuery, TMapReduceOptions>(
            TQuery query,
            BsonJavaScript map,
            BsonJavaScript reduce,
            TMapReduceOptions options
        ) {
            // create a new set of options because we don't want to modify caller's data
            return MapReduce(map, reduce, MapReduceOptions.SetQuery(query).AddOptions(options.ToBsonDocument()));
        }

        public MongoMapReduceResult MapReduce<TQuery>(
            TQuery query,
            BsonJavaScript map,
            BsonJavaScript reduce
        ) {
            return MapReduce(map, reduce, MapReduceOptions.SetQuery(query));
        }

        public MongoMapReduceResult MapReduce(
            BsonJavaScript map,
            BsonJavaScript reduce
        ) {
            return MapReduce(map, reduce, MapReduceOptions.None);
        }

        public void ReIndex() {
            throw new NotImplementedException();
        }

        public BsonDocument Remove<TQuery>(
            TQuery query
        ) {
            return Remove(query, RemoveFlags.None, safeMode);
        }

        public BsonDocument Remove<TQuery>(
            TQuery query,
            SafeMode safeMode
        ) {
            return Remove(query, RemoveFlags.None, safeMode);
        }

        public BsonDocument Remove<TQuery>(
            TQuery query,
            RemoveFlags flags
        ) {
            return Remove(query, flags, safeMode);
        }

        public BsonDocument Remove<TQuery>(
           TQuery query,
           RemoveFlags flags,
           SafeMode safeMode
        ) {
            // special case for query on _id
            // TODO: find _id even when type is not BsonDocument
            if (query != null) {
                BsonDocument queryBsonDocument = query as BsonDocument;
                if (queryBsonDocument != null) {
                    if (
                        queryBsonDocument.Count == 1 &&
                        queryBsonDocument.GetElement(0).Name == "_id" &&
                        queryBsonDocument[0].BsonType == BsonType.ObjectId
                    ) {
                        flags |= RemoveFlags.Single;
                    }
                }
            }

            using (var message = new MongoDeleteMessage<TQuery>(FullName, flags, query)) {
                var connection = database.GetConnection(false); // not slaveOk
                var lastError = connection.SendMessage(message, safeMode);
                database.ReleaseConnection(connection);
                return lastError;
            }
        }

        public BsonDocument RemoveAll() {
            BsonDocument query = null;
            return Remove(query, RemoveFlags.None, safeMode);
        }

        public BsonDocument RemoveAll(
           SafeMode safeMode
        ) {
            BsonDocument query = null;
            return Remove(query, RemoveFlags.None, safeMode);
        }

        public void ResetIndexCache() {
            lock (indexCache) {
                indexCache.Clear();
            }
        }

        public BsonDocument Save<TDocument>(
            TDocument document
        ) {
            return Save(document, safeMode);
        }

        public BsonDocument Save<TDocument>(
            TDocument document,
            SafeMode safeMode
        ) {
            var serializer = BsonSerializer.LookupSerializer(document.GetType());
            object id;
            IBsonIdGenerator idGenerator;
            if (serializer.GetDocumentId(document, out id, out idGenerator)) {
                if (idGenerator != null && idGenerator.IsEmpty(id)) {
                    id = idGenerator.GenerateId();
                    serializer.SetDocumentId(document, id);
                } else if (id != null) {
                    var query = new BsonDocument("_id", BsonValue.Create(id));
                    return Update(query, document, UpdateFlags.Upsert, safeMode);
                }
            }
            return Insert(document, safeMode);
        }

        public BsonDocument Stats() {
            var command = new BsonDocument("collstats", name);
            return database.RunCommand(command);
        }

        public int StorageSize() {
            return Stats()["storageSize"].ToInt32();
        }

        public int TotalIndexSize() {
            return Stats()["totalIndexSize"].ToInt32();
        }

        public int TotalSize() {
            var totalSize = StorageSize();
            var indexes = GetIndexes();
            foreach (var index in indexes) {
                var indexName = index["name"].AsString;
                var indexCollectionName = string.Format("{0}.${1}", name, indexName);
                var indexCollection = database.GetCollection(indexCollectionName);
                totalSize += indexCollection.DataSize();
            }
            return totalSize;
        }

        public override string ToString() {
 	    return FullName;
        }

        public BsonDocument Update<TQuery, TUpdate>(
            TQuery query,
            TUpdate update
        ) {
            return Update(query, update, UpdateFlags.None, safeMode);
        }

        public BsonDocument Update<TQuery, TUpdate>(
            TQuery query,
            TUpdate update,
            SafeMode safeMode
        ) {
            return Update(query, update, UpdateFlags.None, safeMode);
        }

        public BsonDocument Update<TQuery, TUpdate>(
            TQuery query,
            TUpdate update,
            UpdateFlags flags
        ) {
            return Update(query, update, flags, safeMode);
        }

        public BsonDocument Update<TQuery, TUpdate>(
            TQuery query,
            TUpdate update,
            UpdateFlags flags,
            SafeMode safeMode
        ) {
            // TODO: remove this sanity check or make it configurable?
            var queryBsonDocument = query as BsonDocument;
            if (queryBsonDocument != null) {
                if (queryBsonDocument.Any(e => e.Name.StartsWith("$"))) {
                    throw new ArgumentException("Found atomic modifiers in query (are your arguments to Update in the wrong order?)");
                }
            }

            using (var message = new MongoUpdateMessage<TQuery, TUpdate>(FullName, flags, query, update)) {
                var connection = database.GetConnection(false); // not slaveOk
                var lastError = connection.SendMessage(message, safeMode);
                database.ReleaseConnection(connection);
                return lastError;
            }
        }

        public BsonDocument Validate() {
            var command = new BsonDocument("validate", name);
            return database.RunCommand(command);
        }
        #endregion

        #region private methods
        private string GetIndexName(
            BsonDocument keys
        ) {
            StringBuilder sb = new StringBuilder();
            foreach (var element in keys) {
                if (sb.Length > 0) {
                    sb.Append("_");
                }
                sb.Append(element.Name);
                sb.Append("_");
                var value = element.Value;
                if (
                    value.BsonType == BsonType.Int32 ||
                    value.BsonType == BsonType.Int64 ||
                    value.BsonType == BsonType.Double ||
                    value.BsonType == BsonType.String
                ) {
                    sb.Append(value.RawValue.ToString().Replace(' ', '_'));
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
                throw new ArgumentException("Invalid collection name", "name");
            }
        }
        #endregion
    }

    // this subclass provides a default document type for Find methods
    // you can still Find any other document type by using the Find<TQuery, TDocument> methods

    public class MongoCollection<TDefaultDocument> : MongoCollection {
        #region constructors
        public MongoCollection(
            MongoDatabase database,
            string name,
            SafeMode safeMode
        )
            : base(database, name, safeMode) {
        }
        #endregion

        #region public methods
        public MongoCursor<TQuery, TDefaultDocument> Find<TQuery>(
            TQuery query
        ) {
            return FindAs<TQuery, TDefaultDocument>(query);
        }

        public MongoCursor<BsonDocument, TDefaultDocument> FindAll() {
            return FindAllAs<TDefaultDocument>();
        }

        public TDefaultDocument FindOne() {
            return FindOneAs<TDefaultDocument>();
        }

        public TDefaultDocument FindOne<TQuery>(
            TQuery query
        ) {
            return FindOneAs<TQuery, TDefaultDocument>(query);
        }
        #endregion
    }
}
