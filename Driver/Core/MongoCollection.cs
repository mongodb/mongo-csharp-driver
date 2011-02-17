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
        private MongoServer server;
        private MongoDatabase database;
        private MongoCollectionSettings settings;
        private string name;
        #endregion

        #region constructors
        protected MongoCollection(
            MongoDatabase database,
            MongoCollectionSettings settings
        ) {
            ValidateCollectionName(settings.CollectionName);
            this.server = database.Server;
            this.database = database;
            this.settings = settings;
            this.name = settings.CollectionName;
        }
        #endregion

        #region public properties
        public virtual MongoDatabase Database {
            get { return database; }
        }

        public virtual string FullName {
            get { return database.Name + "." + name; }
        }

        public virtual string Name {
            get { return name; }
        }

        public virtual MongoCollectionSettings Settings {
            get { return settings; }
        }
        #endregion

        #region public methods
        public virtual int Count() {
            return Count(Query.Null);
        }

        public virtual int Count(
            IMongoQuery query
        ) {
            var command = new CommandDocument {
                { "count", name },
                { "query", BsonDocument.Wrap(query) } // query is optional
            };
            var result = database.RunCommand(command);
            return result.Response["n"].ToInt32();
        }

        public virtual SafeModeResult CreateIndex(
            IMongoIndexKeys keys,
            IMongoIndexOptions options
        ) {
            var keysDocument = keys.ToBsonDocument();
            var optionsDocument = options.ToBsonDocument();
            var indexes = database.GetCollection("system.indexes");
            var indexName = GetIndexName(keysDocument, optionsDocument);
            var index = new BsonDocument {
                { "name", indexName },
                { "ns", FullName },
                { "key", keysDocument }
            };
            index.Merge(optionsDocument);
            var result = indexes.Insert(index, SafeMode.True);
            return result;
        }

        public virtual SafeModeResult CreateIndex(
            IMongoIndexKeys keys
        ) {
            return CreateIndex(keys, IndexOptions.Null);
        }

        public virtual SafeModeResult CreateIndex(
            params string[] keyNames
        ) {
            return CreateIndex(IndexKeys.Ascending(keyNames));
        }

        public virtual IEnumerable<BsonValue> Distinct(
            string key
        ) {
            return Distinct(key, Query.Null);
        }

        public virtual IEnumerable<BsonValue> Distinct(
            string key,
            IMongoQuery query
        ) {
            var command = new CommandDocument {
                { "distinct", name },
                { "key", key },
                { "query", BsonDocument.Wrap(query) } // query is optional
            };
            var result = database.RunCommand(command);
            return result.Response["values"].AsBsonArray;
        }

        public virtual void Drop() {
            database.DropCollection(name);
        }

        public virtual CommandResult DropAllIndexes() {
            return DropIndexByName("*");
        }

        public virtual CommandResult DropIndex(
            IMongoIndexKeys keys
        ) {
            string indexName = GetIndexName(keys.ToBsonDocument(), null);
            return DropIndexByName(indexName);
        }

        public virtual CommandResult DropIndex(
            params string[] keyNames
        ) {
            string indexName = GetIndexName(keyNames);
            return DropIndexByName(indexName);
        }

        public virtual CommandResult DropIndexByName(
            string indexName
        ) {
            // remove from cache first (even if command ends up failing)
            if (indexName == "*") {
                server.IndexCache.Reset(this);
            } else {
                server.IndexCache.Remove(this, indexName);
            }
            var command = new CommandDocument {
                    { "deleteIndexes", name }, // not FullName
                    { "index", indexName }
                };
            return database.RunCommand(command);
        }

        public virtual void EnsureIndex(
           IMongoIndexKeys keys,
           IMongoIndexOptions options
        ) {
            var keysDocument = keys.ToBsonDocument();
            var optionsDocument = options.ToBsonDocument();
            var indexName = GetIndexName(keysDocument, optionsDocument);
            if (!server.IndexCache.Contains(this, indexName)) {
                CreateIndex(keys, options);
                server.IndexCache.Add(this, indexName);
            }
        }

        public virtual void EnsureIndex(
            IMongoIndexKeys keys
        ) {
            EnsureIndex(keys, IndexOptions.Null);
        }

        public virtual void EnsureIndex(
            params string[] keyNames
        ) {
            string indexName = GetIndexName(keyNames);
            if (!server.IndexCache.Contains(this, indexName)) {
                CreateIndex(IndexKeys.Ascending(keyNames), IndexOptions.SetName(indexName));
                server.IndexCache.Add(this, indexName);
            }
        }

        public virtual bool Exists() {
            return database.CollectionExists(name);
        }

        public virtual MongoCursor<TDocument> FindAllAs<TDocument>() {
            return FindAs<TDocument>(Query.Null);
        }

        public virtual FindAndModifyResult FindAndModify(
            IMongoQuery query,
            IMongoSortBy sortBy,
            IMongoUpdate update
        ) {
            return FindAndModify(query, sortBy, update, false);
        }

        public virtual FindAndModifyResult FindAndModify(
            IMongoQuery query,
            IMongoSortBy sortBy,
            IMongoUpdate update,
            bool returnNew
        ) {
            return FindAndModify(query, sortBy, update, returnNew, false);
        }

        public virtual FindAndModifyResult FindAndModify(
            IMongoQuery query,
            IMongoSortBy sortBy,
            IMongoUpdate update,
            bool returnNew,
            bool upsert
        ) {
            return FindAndModify(query, sortBy, update, Fields.Null, returnNew, upsert);
        }

        public virtual FindAndModifyResult FindAndModify(
            IMongoQuery query,
            IMongoSortBy sortBy,
            IMongoUpdate update,
            IMongoFields fields,
            bool returnNew,
            bool upsert
        ) {
            var command = new CommandDocument {
                { "findAndModify", name },
                { "query", BsonDocument.Wrap(query) },
                { "sort", BsonDocument.Wrap(sortBy) },
                { "update", BsonDocument.Wrap(update) },
                { "fields", BsonDocument.Wrap(fields) },
                { "new", true, returnNew },
                { "upsert", true, upsert}
            };
            return database.RunCommandAs<FindAndModifyResult>(command);
        }

        public virtual FindAndModifyResult FindAndRemove(
            IMongoQuery query,
            IMongoSortBy sortBy
        ) {
            var command = new CommandDocument {
                { "findAndModify", name },
                { "query", BsonDocument.Wrap(query) },
                { "sort", BsonDocument.Wrap(sortBy) },
                { "remove", true }
            };
            return database.RunCommandAs<FindAndModifyResult>(command);
        }

        public virtual MongoCursor<TDocument> FindAs<TDocument>(
            IMongoQuery query
        ) {
            return new MongoCursor<TDocument>(this, query);
        }

        public virtual TDocument FindOneAs<TDocument>() {
            return FindAllAs<TDocument>().SetLimit(1).FirstOrDefault();
        }

        public virtual TDocument FindOneAs<TDocument>(
            IMongoQuery query
        ) {
            return FindAs<TDocument>(query).SetLimit(1).FirstOrDefault();
        }

        public virtual TDocument FindOneByIdAs<TDocument>(
            BsonValue id
        ) {
            return FindOneAs<TDocument>(Query.EQ("_id", id));
        }

        public virtual GeoNearResult<TDocument> GeoNearAs<TDocument>(
            IMongoQuery query,
            double x,
            double y,
            int limit
        ) {
            return GeoNearAs<TDocument>(query, x, y, limit, GeoNearOptions.Null);
        }

        public virtual GeoNearResult<TDocument> GeoNearAs<TDocument>(
            IMongoQuery query,
            double x,
            double y,
            int limit,
            IMongoGeoNearOptions options
        ) {
            var command = new CommandDocument {
                { "geoNear", name },
                { "near", new BsonArray { x, y } },
                { "num", limit },
                { "query", BsonDocument.Wrap(query) } // query is optional
            };
            command.Merge(options.ToBsonDocument());
            return database.RunCommandAs<GeoNearResult<TDocument>>(command);
        }

        public virtual IEnumerable<BsonDocument> GetIndexes() {
            var indexes = database.GetCollection("system.indexes");
            var query = Query.EQ("ns", FullName);
            return indexes.Find(query).ToList(); // force query to execute before returning
        }

        public virtual CollectionStatsResult GetStats() {
            var command = new CommandDocument("collstats", name);
            return database.RunCommandAs<CollectionStatsResult>(command);
        }

        public virtual long GetTotalDataSize() {
            var totalSize = GetStats().DataSize;
            var indexes = GetIndexes();
            foreach (var index in indexes) {
                var indexName = index["name"].AsString;
                var indexCollectionName = string.Format("{0}.${1}", name, indexName);
                var indexCollection = database.GetCollection(indexCollectionName);
                totalSize += indexCollection.GetStats().DataSize;
            }
            return totalSize;
        }

        public virtual long GetTotalStorageSize() {
            var totalSize = GetStats().StorageSize;
            var indexes = GetIndexes();
            foreach (var index in indexes) {
                var indexName = index["name"].AsString;
                var indexCollectionName = string.Format("{0}.${1}", name, indexName);
                var indexCollection = database.GetCollection(indexCollectionName);
                totalSize += indexCollection.GetStats().StorageSize;
            }
            return totalSize;
        }

        public virtual IEnumerable<BsonDocument> Group(
            IMongoQuery query,
            BsonJavaScript keyFunction,
            BsonDocument initial,
            BsonJavaScript reduce,
            BsonJavaScript finalize
        ) {
            var command = new CommandDocument {
                { "group", new BsonDocument {
                    { "ns", name },
                    { "condition", BsonDocument.Wrap(query) }, // condition is optional
                    { "$keyf", keyFunction },
                    { "initial", initial },
                    { "$reduce", reduce },
                    { "finalize", finalize }
                } }
            };
            var result = database.RunCommand(command);
            return result.Response["retval"].AsBsonArray.Values.Cast<BsonDocument>();
        }

        public virtual IEnumerable<BsonDocument> Group(
            IMongoQuery query,
            IMongoGroupBy keys,
            BsonDocument initial,
            BsonJavaScript reduce,
            BsonJavaScript finalize
        ) {
            var command = new CommandDocument {
                { "group", new BsonDocument {
                    { "ns", name },
                    { "condition", BsonDocument.Wrap(query) }, // condition is optional
                    { "key", BsonDocument.Wrap(keys) },
                    { "initial", initial },
                    { "$reduce", reduce },
                    { "finalize", finalize }
                } }
            };
            var result = database.RunCommand(command);
            return result.Response["retval"].AsBsonArray.Values.Cast<BsonDocument>();
        }

        public virtual IEnumerable<BsonDocument> Group(
            IMongoQuery query,
            string key,
            BsonDocument initial,
            BsonJavaScript reduce,
            BsonJavaScript finalize
        ) {
            return Group(query, GroupBy.Keys(key), initial, reduce, finalize);
        }

        public virtual bool IndexExists(
            IMongoIndexKeys keys
        ) {
            string indexName = GetIndexName(keys.ToBsonDocument(), null);
            return IndexExistsByName(indexName);
        }

        public virtual bool IndexExists(
            params string[] keyNames
        ) {
            string indexName = GetIndexName(keyNames);
            return IndexExistsByName(indexName);
        }

        public virtual bool IndexExistsByName(
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

        public virtual SafeModeResult Insert<TDocument>(
            TDocument document
        ) {
            return Insert(document, settings.SafeMode);
        }

        public virtual SafeModeResult Insert<TDocument>(
            TDocument document,
            SafeMode safeMode
        ) {
            var results = InsertBatch<TDocument>(new TDocument[] { document }, safeMode);
            return (results == null) ? null : results.Single();
        }

        public virtual IEnumerable<SafeModeResult> InsertBatch<TDocument>(
            IEnumerable<TDocument> documents
        ) {
            return InsertBatch<TDocument>(documents, settings.SafeMode);
        }

        public virtual IEnumerable<SafeModeResult> InsertBatch<TDocument>(
            IEnumerable<TDocument> documents,
            SafeMode safeMode
        ) {
            var connection = server.AcquireConnection(database, false); // not slaveOk
            try {
                List<SafeModeResult> results = (safeMode.Enabled) ? new List<SafeModeResult>() : null;

                using (var message = new MongoInsertMessage(server, FullName)) {
                    message.WriteToBuffer(); // must be called before AddDocument

                    foreach (var document in documents) {
                        if (settings.AssignIdOnInsert) {
                            var serializer = BsonSerializer.LookupSerializer(document.GetType());
                            object id;
                            IIdGenerator idGenerator;
                            if (serializer.GetDocumentId(document, out id, out idGenerator)) {
                                if (idGenerator != null && idGenerator.IsEmpty(id)) {
                                    id = idGenerator.GenerateId();
                                    serializer.SetDocumentId(document, id);
                                }
                            }
                        }
                        message.AddDocument(document);

                        if (message.MessageLength > server.MaxMessageLength) {
                            byte[] lastDocument = message.RemoveLastDocument();
                            var intermediateResult = connection.SendMessage(message, safeMode);
                            if (safeMode.Enabled) { results.Add(intermediateResult); }
                            message.ResetBatch(lastDocument);
                        }
                    }

                    var finalResult = connection.SendMessage(message, safeMode);
                    if (safeMode.Enabled) { results.Add(finalResult); }

                    return results;
                }
            } finally {
                server.ReleaseConnection(connection);
            }
        }

        public virtual bool IsCapped() {
            throw new NotImplementedException();
        }

        public virtual MapReduceResult MapReduce(
            BsonJavaScript map,
            BsonJavaScript reduce,
            IMongoMapReduceOptions options
        ) {
            var command = new CommandDocument {
                { "mapreduce", name },
                { "map", map },
                { "reduce", reduce }
            };
            command.Merge(options.ToBsonDocument());
            var result = database.RunCommandAs<MapReduceResult>(command);
            result.SetDatabase(database);
            return result;
        }

        public virtual MapReduceResult MapReduce(
            IMongoQuery query,
            BsonJavaScript map,
            BsonJavaScript reduce,
            IMongoMapReduceOptions options
        ) {
            // create a new set of options because we don't want to modify caller's data
            return MapReduce(map, reduce, MapReduceOptions.SetQuery(query).AddOptions(options.ToBsonDocument()));
        }

        public virtual MapReduceResult MapReduce(
            IMongoQuery query,
            BsonJavaScript map,
            BsonJavaScript reduce
        ) {
            return MapReduce(map, reduce, MapReduceOptions.SetQuery(query));
        }

        public virtual MapReduceResult MapReduce(
            BsonJavaScript map,
            BsonJavaScript reduce
        ) {
            return MapReduce(map, reduce, MapReduceOptions.Null);
        }

        public virtual void ReIndex() {
            throw new NotImplementedException();
        }

        public virtual SafeModeResult Remove(
            IMongoQuery query
        ) {
            return Remove(query, RemoveFlags.None, settings.SafeMode);
        }

        public virtual SafeModeResult Remove(
            IMongoQuery query,
            SafeMode safeMode
        ) {
            return Remove(query, RemoveFlags.None, safeMode);
        }

        public virtual SafeModeResult Remove(
            IMongoQuery query,
            RemoveFlags flags
        ) {
            return Remove(query, flags, settings.SafeMode);
        }

        public virtual SafeModeResult Remove(
           IMongoQuery query,
           RemoveFlags flags,
           SafeMode safeMode
        ) {
            // special case for query on _id
            // TODO: find _id even when type is not BsonDocument
            if (query != null) {
                BsonDocument queryBsonDocument = query as BsonDocument;
                if (queryBsonDocument != null) {
                    if (
                        queryBsonDocument.ElementCount == 1 &&
                        queryBsonDocument.GetElement(0).Name == "_id" &&
                        queryBsonDocument[0].BsonType == BsonType.ObjectId
                    ) {
                        flags |= RemoveFlags.Single;
                    }
                }
            }

            using (var message = new MongoDeleteMessage(server, FullName, flags, query)) {
                var connection = server.AcquireConnection(database, false); // not slaveOk
                try {
                    return connection.SendMessage(message, safeMode);
                } finally {
                    server.ReleaseConnection(connection);
                }
            }
        }

        public virtual SafeModeResult RemoveAll() {
            return Remove(Query.Null, RemoveFlags.None, settings.SafeMode);
        }

        public virtual SafeModeResult RemoveAll(
           SafeMode safeMode
        ) {
            return Remove(Query.Null, RemoveFlags.None, safeMode);
        }

        public virtual void ResetIndexCache() {
            server.IndexCache.Reset(this);
        }

        public virtual SafeModeResult Save<TDocument>(
            TDocument document
        ) {
            return Save(document, settings.SafeMode);
        }

        public virtual SafeModeResult Save<TDocument>(
            TDocument document,
            SafeMode safeMode
        ) {
            var serializer = BsonSerializer.LookupSerializer(document.GetType());
            object id;
            IIdGenerator idGenerator;
            if (serializer.GetDocumentId(document, out id, out idGenerator)) {
                if (idGenerator != null && idGenerator.IsEmpty(id)) {
                    id = idGenerator.GenerateId();
                    serializer.SetDocumentId(document, id);
                } else if (id != null) {
                    var query = Query.EQ("_id", BsonValue.Create(id));
                    return Update(query, Builders.Update.Replace(document), UpdateFlags.Upsert, safeMode);
                }
            }
            return Insert(document, safeMode);
        }

        public override string ToString() {
 	        return FullName;
        }

        public virtual SafeModeResult Update(
            IMongoQuery query,
            IMongoUpdate update
        ) {
            return Update(query, update, UpdateFlags.None, settings.SafeMode);
        }

        public virtual SafeModeResult Update(
            IMongoQuery query,
            IMongoUpdate update,
            SafeMode safeMode
        ) {
            return Update(query, update, UpdateFlags.None, safeMode);
        }

        public virtual SafeModeResult Update(
            IMongoQuery query,
            IMongoUpdate update,
            UpdateFlags flags
        ) {
            return Update(query, update, flags, settings.SafeMode);
        }

        public virtual SafeModeResult Update(
            IMongoQuery query,
            IMongoUpdate update,
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

            using (var message = new MongoUpdateMessage(server, FullName, flags, query, update)) {
                var connection = server.AcquireConnection(database, false); // not slaveOk
                try {
                    return connection.SendMessage(message, safeMode);
                } finally {
                    server.ReleaseConnection(connection);
                }
            }
        }

        public virtual ValidateCollectionResult Validate() {
            var command = new CommandDocument("validate", name);
            return database.RunCommandAs<ValidateCollectionResult>(command);
        }
        #endregion

        #region private methods
        private string GetIndexName(
            BsonDocument keys,
            BsonDocument options
        ) {
            if (options != null) {
                if (options.Contains("name")) {
                    return options["name"].AsString;
                }
            }

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
    // you can still Find any other document type by using the FindAs<TDocument> methods

    public class MongoCollection<TDefaultDocument> : MongoCollection {
        #region constructors
        public MongoCollection(
            MongoDatabase database,
            MongoCollectionSettings<TDefaultDocument> settings
        )
            : base(database, settings) {
        }
        #endregion

        #region public methods
        public virtual MongoCursor<TDefaultDocument> Find(
            IMongoQuery query
        ) {
            return FindAs<TDefaultDocument>(query);
        }

        public virtual MongoCursor<TDefaultDocument> FindAll() {
            return FindAllAs<TDefaultDocument>();
        }

        public virtual TDefaultDocument FindOne() {
            return FindOneAs<TDefaultDocument>();
        }

        public virtual TDefaultDocument FindOne(
            IMongoQuery query
        ) {
            return FindOneAs<TDefaultDocument>(query);
        }

        public virtual TDefaultDocument FindOneById(
            BsonValue id
        ) {
            return FindOneByIdAs<TDefaultDocument>(id);
        }

        public virtual GeoNearResult<TDefaultDocument> GeoNear(
            IMongoQuery query,
            double x,
            double y,
            int limit
        ) {
            return GeoNearAs<TDefaultDocument>(query, x, y, limit);
        }

        public virtual GeoNearResult<TDefaultDocument> GeoNear(
            IMongoQuery query,
            double x,
            double y,
            int limit,
            IMongoGeoNearOptions options
        ) {
            return GeoNearAs<TDefaultDocument>(query, x, y, limit, options);
        }
        #endregion
    }
}
