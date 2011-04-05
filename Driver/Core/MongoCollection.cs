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
using MongoDB.Driver.Wrappers;

namespace MongoDB.Driver {
    /// <summary>
    /// Represents a MongoDB collection and the settings used to access it. This class is thread-safe.
    /// </summary>
    public abstract class MongoCollection {
        #region private fields
        private MongoServer server;
        private MongoDatabase database;
        private MongoCollectionSettings settings;
        private string name;
        #endregion

        #region constructors
        /// <summary>
        /// Protected constructor for abstract base class.
        /// </summary>
        /// <param name="database">The database that contains this collection.</param>
        /// <param name="settings">The settings to use to access this collection.</param>
        protected MongoCollection(
            MongoDatabase database,
            MongoCollectionSettings settings
        ) {
            ValidateCollectionName(settings.CollectionName);
            this.server = database.Server;
            this.database = database;
            this.settings = settings.Freeze();
            this.name = settings.CollectionName;
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the database that contains this collection.
        /// </summary>
        public virtual MongoDatabase Database {
            get { return database; }
        }

        /// <summary>
        /// Gets the fully qualified name of this collection.
        /// </summary>
        public virtual string FullName {
            get { return database.Name + "." + name; }
        }

        /// <summary>
        /// Gets the name of this collection.
        /// </summary>
        public virtual string Name {
            get { return name; }
        }

        /// <summary>
        /// Gets the settings being used to access this collection.
        /// </summary>
        public virtual MongoCollectionSettings Settings {
            get { return settings; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Counts the number of documents in this collection.
        /// </summary>
        /// <returns>The number of documents in this collection.</returns>
        public virtual int Count() {
            return Count(Query.Null);
        }

        /// <summary>
        /// Counts the number of documents in this collection that match a query.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>The number of documents in this collection that match the query.</returns>
        public virtual int Count(
            IMongoQuery query
        ) {
            var command = new CommandDocument {
                { "count", name },
                { "query", BsonDocumentWrapper.Create(query) } // query is optional
            };
            var result = database.RunCommand(command);
            return result.Response["n"].ToInt32();
        }

        /// <summary>
        /// Creates an index for this collection.
        /// </summary>
        /// <param name="keys">The indexed fields (usually an IndexKeysDocument or constructed using the IndexKeys builder).</param>
        /// <param name="options">The index options(usually an IndexOptionsDocument or created using the IndexOption builder).</param>
        /// <returns>A SafeModeResult.</returns>
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

        /// <summary>
        /// Creates an index for this collection.
        /// </summary>
        /// <param name="keys">The indexed fields (usually an IndexKeysDocument or constructed using the IndexKeys builder).</param>
        /// <returns>A SafeModeResult.</returns>
        public virtual SafeModeResult CreateIndex(
            IMongoIndexKeys keys
        ) {
            return CreateIndex(keys, IndexOptions.Null);
        }

        /// <summary>
        /// Creates an index for this collection.
        /// </summary>
        /// <param name="keyNames">The names of the indexed fields.</param>
        /// <returns>A SafeModeResult.</returns>
        public virtual SafeModeResult CreateIndex(
            params string[] keyNames
        ) {
            return CreateIndex(IndexKeys.Ascending(keyNames));
        }

        /// <summary>
        /// Returns the distinct values for a given field.
        /// </summary>
        /// <param name="key">The key of the field.</param>
        /// <returns>The distint values of the field.</returns>
        public virtual IEnumerable<BsonValue> Distinct(
            string key
        ) {
            return Distinct(key, Query.Null);
        }

        /// <summary>
        /// Returns the distinct values for a given field for documents that match a query.
        /// </summary>
        /// <param name="key">The key of the field.</param>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>The distint values of the field.</returns>
        public virtual IEnumerable<BsonValue> Distinct(
            string key,
            IMongoQuery query
        ) {
            var command = new CommandDocument {
                { "distinct", name },
                { "key", key },
                { "query", BsonDocumentWrapper.Create(query) } // query is optional
            };
            var result = database.RunCommand(command);
            return result.Response["values"].AsBsonArray;
        }

        /// <summary>
        /// Drops this collection.
        /// </summary>
        public virtual void Drop() {
            database.DropCollection(name);
        }

        /// <summary>
        /// Drops all indexes on this collection.
        /// </summary>
        /// <returns>A <see cref="CommandResult"/>.</returns>
        public virtual CommandResult DropAllIndexes() {
            return DropIndexByName("*");
        }

        /// <summary>
        /// Drops an index on this collection.
        /// </summary>
        /// <param name="keys">The indexed fields (usually an IndexKeysDocument or constructed using the IndexKeys builder).</param>
        /// <returns>A <see cref="CommandResult"/>.</returns>
        public virtual CommandResult DropIndex(
            IMongoIndexKeys keys
        ) {
            string indexName = GetIndexName(keys.ToBsonDocument(), null);
            return DropIndexByName(indexName);
        }

        /// <summary>
        /// Drops an index on this collection.
        /// </summary>
        /// <param name="keyNames">The names of the indexed fields.</param>
        /// <returns>A <see cref="CommandResult"/>.</returns>
        public virtual CommandResult DropIndex(
            params string[] keyNames
        ) {
            string indexName = GetIndexName(keyNames);
            return DropIndexByName(indexName);
        }

        /// <summary>
        /// Drops an index on this collection.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <returns>A <see cref="CommandResult"/>.</returns>
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

        /// <summary>
        /// Ensures that the desired index exists and creates it if it does not.
        /// </summary>
        /// <param name="keys">The indexed fields (usually an IndexKeysDocument or constructed using the IndexKeys builder).</param>
        /// <param name="options">The index options(usually an IndexOptionsDocument or created using the IndexOption builder).</param>
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

        /// <summary>
        /// Ensures that the desired index exists and creates it if it does not.
        /// </summary>
        /// <param name="keys">The indexed fields (usually an IndexKeysDocument or constructed using the IndexKeys builder).</param>
        public virtual void EnsureIndex(
            IMongoIndexKeys keys
        ) {
            EnsureIndex(keys, IndexOptions.Null);
        }

        /// <summary>
        /// Ensures that the desired index exists and creates it if it does not.
        /// </summary>
        /// <param name="keyNames">The names of the indexed fields.</param>
        public virtual void EnsureIndex(
            params string[] keyNames
        ) {
            string indexName = GetIndexName(keyNames);
            if (!server.IndexCache.Contains(this, indexName)) {
                CreateIndex(IndexKeys.Ascending(keyNames), IndexOptions.SetName(indexName));
                server.IndexCache.Add(this, indexName);
            }
        }

        /// <summary>
        /// Tests whether this collection exists.
        /// </summary>
        /// <returns>True if this collection exists.</returns>
        public virtual bool Exists() {
            return database.CollectionExists(name);
        }

        /// <summary>
        /// Returns a cursor that can be used to find all documents in this collection as TDocuments.
        /// </summary>
        /// <typeparam name="TDocument">The type to deserialize the documents as.</typeparam>
        /// <returns>A <see cref="MongoCursor{TDocument}"/>.</returns>
        public virtual MongoCursor<TDocument> FindAllAs<TDocument>() {
            return FindAs<TDocument>(Query.Null);
        }

        /// <summary>
        /// Finds one matching document using the query and sortBy parameters and applies the specified update to it.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="sortBy">The sort order to select one of the matching documents.</param>
        /// <param name="update">The update to apply to the matching document.</param>
        /// <returns>A <see cref="FindAndModifyResult"/>.</returns>
        public virtual FindAndModifyResult FindAndModify(
            IMongoQuery query,
            IMongoSortBy sortBy,
            IMongoUpdate update
        ) {
            return FindAndModify(query, sortBy, update, false);
        }

        /// <summary>
        /// Finds one matching document using the query and sortBy parameters and applies the specified update to it.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="sortBy">The sort order to select one of the matching documents.</param>
        /// <param name="update">The update to apply to the matching document.</param>
        /// <param name="returnNew">Whether to return the new or old version of the modified document in the <see cref="FindAndModifyResult"/>.</param>
        /// <returns>A <see cref="FindAndModifyResult"/>.</returns>
        public virtual FindAndModifyResult FindAndModify(
            IMongoQuery query,
            IMongoSortBy sortBy,
            IMongoUpdate update,
            bool returnNew
        ) {
            return FindAndModify(query, sortBy, update, returnNew, false);
        }

        /// <summary>
        /// Finds one matching document using the query and sortBy parameters and applies the specified update to it.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="sortBy">The sort order to select one of the matching documents.</param>
        /// <param name="update">The update to apply to the matching document.</param>
        /// <param name="returnNew">Whether to return the new or old version of the modified document in the <see cref="FindAndModifyResult"/>.</param>
        /// <param name="upsert">Whether to do an upsert if no matching document is found.</param>
        /// <returns>A <see cref="FindAndModifyResult"/>.</returns>
        public virtual FindAndModifyResult FindAndModify(
            IMongoQuery query,
            IMongoSortBy sortBy,
            IMongoUpdate update,
            bool returnNew,
            bool upsert
        ) {
            return FindAndModify(query, sortBy, update, Fields.Null, returnNew, upsert);
        }

        /// <summary>
        /// Finds one matching document using the query and sortBy parameters and applies the specified update to it.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="sortBy">The sort order to select one of the matching documents.</param>
        /// <param name="update">The update to apply to the matching document.</param>
        /// <param name="fields">Which fields of the modified document to return in the <see cref="FindAndModifyResult"/>.</param>
        /// <param name="returnNew">Whether to return the new or old version of the modified document in the <see cref="FindAndModifyResult"/>.</param>
        /// <param name="upsert">Whether to do an upsert if no matching document is found.</param>
        /// <returns>A <see cref="FindAndModifyResult"/>.</returns>
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
                { "query", BsonDocumentWrapper.Create(query) },
                { "sort", BsonDocumentWrapper.Create(sortBy) },
                { "update", BsonDocumentWrapper.Create(update) },
                { "fields", BsonDocumentWrapper.Create(fields) },
                { "new", true, returnNew },
                { "upsert", true, upsert}
            };
            return database.RunCommandAs<FindAndModifyResult>(command);
        }

        /// <summary>
        /// Finds one matching document using the query and sortBy parameters and removes it from this collection.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="sortBy">The sort order to select one of the matching documents.</param>
        /// <returns>A <see cref="FindAndModifyResult"/>.</returns>
        public virtual FindAndModifyResult FindAndRemove(
            IMongoQuery query,
            IMongoSortBy sortBy
        ) {
            var command = new CommandDocument {
                { "findAndModify", name },
                { "query", BsonDocumentWrapper.Create(query) },
                { "sort", BsonDocumentWrapper.Create(sortBy) },
                { "remove", true }
            };
            return database.RunCommandAs<FindAndModifyResult>(command);
        }

        /// <summary>
        /// Returns a cursor that can be used to find all documents in this collection that match the query as TDocuments.
        /// </summary>
        /// <typeparam name="TDocument">The type to deserialize the documents as.</typeparam>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>A <see cref="MongoCursor{TDocument}"/>.</returns>
        public virtual MongoCursor<TDocument> FindAs<TDocument>(
            IMongoQuery query
        ) {
            return new MongoCursor<TDocument>(this, query);
        }

        /// <summary>
        /// Returns a cursor that can be used to find one document in this collection as a TDocument.
        /// </summary>
        /// <typeparam name="TDocument">The type to deserialize the documents as.</typeparam>
        /// <returns>A TDocument (or null if not found).</returns>
        public virtual TDocument FindOneAs<TDocument>() {
            return FindAllAs<TDocument>().SetLimit(1).FirstOrDefault();
        }

        /// <summary>
        /// Returns a cursor that can be used to find one document in this collection that matches a query as a TDocument.
        /// </summary>
        /// <typeparam name="TDocument">The type to deserialize the documents as.</typeparam>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>A TDocument (or null if not found).</returns>
        public virtual TDocument FindOneAs<TDocument>(
            IMongoQuery query
        ) {
            return FindAs<TDocument>(query).SetLimit(1).FirstOrDefault();
        }

        /// <summary>
        /// Returns a cursor that can be used to find one document in this collection by its _id value as a TDocument.
        /// </summary>
        /// <typeparam name="TDocument">The type to deserialize the documents as.</typeparam>
        /// <param name="id">The id of the document.</param>
        /// <returns>A TDocument (or null if not found).</returns>
        public virtual TDocument FindOneByIdAs<TDocument>(
            BsonValue id
        ) {
            return FindOneAs<TDocument>(Query.EQ("_id", id));
        }

        /// <summary>
        /// Runs a GeoNear command on this collection.
        /// </summary>
        /// <typeparam name="TDocument">The type to deserialize the documents as.</typeparam>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="x">The x coordinate of the starting location.</param>
        /// <param name="y">The y coordinate of the starting location.</param>
        /// <param name="limit">The maximum number of results returned.</param>
        /// <returns>A <see cref="GeoNearResult{TDocument}"/>.</returns>
        public virtual GeoNearResult<TDocument> GeoNearAs<TDocument>(
            IMongoQuery query,
            double x,
            double y,
            int limit
        ) {
            return GeoNearAs<TDocument>(query, x, y, limit, GeoNearOptions.Null);
        }

        /// <summary>
        /// Runs a GeoNear command on this collection.
        /// </summary>
        /// <typeparam name="TDocument">The type to deserialize the documents as.</typeparam>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="x">The x coordinate of the starting location.</param>
        /// <param name="y">The y coordinate of the starting location.</param>
        /// <param name="limit">The maximum number of results returned.</param>
        /// <param name="options">The GeoNear command options (usually a GeoNearOptionsDocument or constructed using the GeoNearOptions builder).</param>
        /// <returns>A <see cref="GeoNearResult{TDocument}"/>.</returns>
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
                { "query", BsonDocumentWrapper.Create(query) } // query is optional
            };
            command.Merge(options.ToBsonDocument());
            return database.RunCommandAs<GeoNearResult<TDocument>>(command);
        }

        /// <summary>
        /// Gets the indexes for this collection.
        /// </summary>
        /// <returns>A list of BsonDocuments that describe the indexes.</returns>
        public virtual IEnumerable<BsonDocument> GetIndexes() {
            var indexes = database.GetCollection("system.indexes");
            var query = Query.EQ("ns", FullName);
            return indexes.Find(query).ToList(); // force query to execute before returning
        }

        /// <summary>
        /// Gets the stats for this collection.
        /// </summary>
        /// <returns>The stats for this collection as a <see cref="CollectionStatsResult"/>.</returns>
        public virtual CollectionStatsResult GetStats() {
            var command = new CommandDocument("collstats", name);
            return database.RunCommandAs<CollectionStatsResult>(command);
        }

        /// <summary>
        /// Gets the total data size for this collection (data + indexes).
        /// </summary>
        /// <returns>The total data size.</returns>
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

        /// <summary>
        /// Gets the total storage size for this collection (data + indexes + overhead).
        /// </summary>
        /// <returns>The total storage size.</returns>
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

        /// <summary>
        /// Runs the group command on this collection.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="keyFunction">A JavaScript function that returns the key value to group on.</param>
        /// <param name="initial">Initial value passed to the reduce function for each group.</param>
        /// <param name="reduce">A JavaScript function that is called for each matching document in a group.</param>
        /// <param name="finalize">A JavaScript function that is called at the end of the group command.</param>
        /// <returns>A list of results as BsonDocuments.</returns>
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
                    { "condition", BsonDocumentWrapper.Create(query) }, // condition is optional
                    { "$keyf", keyFunction },
                    { "initial", initial },
                    { "$reduce", reduce },
                    { "finalize", finalize }
                } }
            };
            var result = database.RunCommand(command);
            return result.Response["retval"].AsBsonArray.Values.Cast<BsonDocument>();
        }

        /// <summary>
        /// Runs the group command on this collection.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="keys">The names of the fields to group on.</param>
        /// <param name="initial">Initial value passed to the reduce function for each group.</param>
        /// <param name="reduce">A JavaScript function that is called for each matching document in a group.</param>
        /// <param name="finalize">A JavaScript function that is called at the end of the group command.</param>
        /// <returns>A list of results as BsonDocuments.</returns>
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
                    { "condition", BsonDocumentWrapper.Create(query) }, // condition is optional
                    { "key", BsonDocumentWrapper.Create(keys) },
                    { "initial", initial },
                    { "$reduce", reduce },
                    { "finalize", finalize }
                } }
            };
            var result = database.RunCommand(command);
            return result.Response["retval"].AsBsonArray.Values.Cast<BsonDocument>();
        }

        /// <summary>
        /// Runs the group command on this collection.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="key">The name of the field to group on.</param>
        /// <param name="initial">Initial value passed to the reduce function for each group.</param>
        /// <param name="reduce">A JavaScript function that is called for each matching document in a group.</param>
        /// <param name="finalize">A JavaScript function that is called at the end of the group command.</param>
        /// <returns>A list of results as BsonDocuments.</returns>
        public virtual IEnumerable<BsonDocument> Group(
            IMongoQuery query,
            string key,
            BsonDocument initial,
            BsonJavaScript reduce,
            BsonJavaScript finalize
        ) {
            return Group(query, GroupBy.Keys(key), initial, reduce, finalize);
        }

        /// <summary>
        /// Tests whether an index exists.
        /// </summary>
        /// <param name="keys">The indexed fields (usually an IndexKeysDocument or constructed using the IndexKeys builder).</param>
        /// <returns>True if the index exists.</returns>
        public virtual bool IndexExists(
            IMongoIndexKeys keys
        ) {
            string indexName = GetIndexName(keys.ToBsonDocument(), null);
            return IndexExistsByName(indexName);
        }

        /// <summary>
        /// Tests whether an index exists.
        /// </summary>
        /// <param name="keyNames">The names of the fields in the index.</param>
        /// <returns>True if the index exists.</returns>
        public virtual bool IndexExists(
            params string[] keyNames
        ) {
            string indexName = GetIndexName(keyNames);
            return IndexExistsByName(indexName);
        }

        /// <summary>
        /// Tests whether an index exists.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <returns>True if the index exists.</returns>
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

        /// <summary>
        /// Inserts a document into this collection (see also InsertBatch to insert multiple documents at once).
        /// </summary>
        /// <typeparam name="TDocument">The type of the document to insert.</typeparam>
        /// <param name="document">The document to insert.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        public virtual SafeModeResult Insert<TDocument>(
            TDocument document
        ) {
            return Insert(document, settings.SafeMode);
        }

        /// <summary>
        /// Inserts a document into this collection (see also InsertBatch to insert multiple documents at once).
        /// </summary>
        /// <typeparam name="TDocument">The type of the document to insert.</typeparam>
        /// <param name="document">The document to insert.</param>
        /// <param name="safeMode">The SafeMode to use for this Insert.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        public virtual SafeModeResult Insert<TDocument>(
            TDocument document,
            SafeMode safeMode
        ) {
            var results = InsertBatch<TDocument>(new TDocument[] { document }, safeMode);
            return (results == null) ? null : results.Single();
        }

        /// <summary>
        /// Inserts multiple documents at once into this collection (see also Insert to insert a single document).
        /// </summary>
        /// <typeparam name="TDocument">The type of the documents to insert.</typeparam>
        /// <param name="documents">The documents to insert.</param>
        /// <returns>A list of SafeModeResults (or null if SafeMode is not being used).</returns>
        public virtual IEnumerable<SafeModeResult> InsertBatch<TDocument>(
            IEnumerable<TDocument> documents
        ) {
            return InsertBatch<TDocument>(documents, settings.SafeMode);
        }

        /// <summary>
        /// Inserts multiple documents at once into this collection (see also Insert to insert a single document).
        /// </summary>
        /// <typeparam name="TDocument">The type of the documents to insert.</typeparam>
        /// <param name="documents">The documents to insert.</param>
        /// <param name="safeMode">The SafeMode to use for this Insert.</param>
        /// <returns>A list of SafeModeResults (or null if SafeMode is not being used).</returns>
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

        /// <summary>
        /// Tests whether this collection is capped.
        /// </summary>
        /// <returns>True if this collection is capped.</returns>
        public virtual bool IsCapped() {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Runs a Map/Reduce command on this collection.
        /// </summary>
        /// <param name="map">A JavaScript function called for each document.</param>
        /// <param name="reduce">A JavaScript function called on the values emitted by the map function.</param>
        /// <param name="options">Options for this map/reduce command (see <see cref="MapReduceOptionsDocument"/>, <see cref="MapReduceOptionsWrapper"/> and the <see cref="MapReduceOptions"/> builder).</param>
        /// <returns>A <see cref="MapReduceResult"/>.</returns>
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
            command.Add(options.ToBsonDocument());
            var result = database.RunCommandAs<MapReduceResult>(command);
            result.SetInputDatabase(database);
            return result;
        }

        /// <summary>
        /// Runs a Map/Reduce command on document in this collection that match a query.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="map">A JavaScript function called for each document.</param>
        /// <param name="reduce">A JavaScript function called on the values emitted by the map function.</param>
        /// <param name="options">Options for this map/reduce command (see <see cref="MapReduceOptionsDocument"/>, <see cref="MapReduceOptionsWrapper"/> and the <see cref="MapReduceOptions"/> builder).</param>
        /// <returns>A <see cref="MapReduceResult"/>.</returns>
        public virtual MapReduceResult MapReduce(
            IMongoQuery query,
            BsonJavaScript map,
            BsonJavaScript reduce,
            IMongoMapReduceOptions options
        ) {
            // create a new set of options because we don't want to modify caller's data
            options = MapReduceOptions.SetQuery(query).AddOptions(options.ToBsonDocument());
            return MapReduce(map, reduce, options);
        }

        /// <summary>
        /// Runs a Map/Reduce command on document in this collection that match a query.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="map">A JavaScript function called for each document.</param>
        /// <param name="reduce">A JavaScript function called on the values emitted by the map function.</param>
        /// <returns>A <see cref="MapReduceResult"/>.</returns>
        public virtual MapReduceResult MapReduce(
            IMongoQuery query,
            BsonJavaScript map,
            BsonJavaScript reduce
        ) {
            var options = MapReduceOptions.SetQuery(query).SetOutput(MapReduceOutput.Inline);
            return MapReduce(map, reduce, options);
        }

        /// <summary>
        /// Runs a Map/Reduce command on this collection.
        /// </summary>
        /// <param name="map">A JavaScript function called for each document.</param>
        /// <param name="reduce">A JavaScript function called on the values emitted by the map function.</param>
        /// <returns>A <see cref="MapReduceResult"/>.</returns>
        public virtual MapReduceResult MapReduce(
            BsonJavaScript map,
            BsonJavaScript reduce
        ) {
            var options = MapReduceOptions.SetOutput(MapReduceOutput.Inline);
            return MapReduce(map, reduce, options);
        }

        /// <summary>
        /// Runs the ReIndex command on this collection.
        /// </summary>
        public virtual CommandResult ReIndex() {
            var command = new CommandDocument("reIndex", name);
            return database.RunCommand(command);
        }

        /// <summary>
        /// Removes documents from this collection that match a query.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        public virtual SafeModeResult Remove(
            IMongoQuery query
        ) {
            return Remove(query, RemoveFlags.None, settings.SafeMode);
        }

        /// <summary>
        /// Removes documents from this collection that match a query.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="safeMode">The SafeMode to use for this operation.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        public virtual SafeModeResult Remove(
            IMongoQuery query,
            SafeMode safeMode
        ) {
            return Remove(query, RemoveFlags.None, safeMode);
        }

        /// <summary>
        /// Removes documents from this collection that match a query.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="flags">The flags for this Remove (see <see cref="RemoveFlags"/>).</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        public virtual SafeModeResult Remove(
            IMongoQuery query,
            RemoveFlags flags
        ) {
            return Remove(query, flags, settings.SafeMode);
        }

        /// <summary>
        /// Removes documents from this collection that match a query.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="flags">The flags for this Remove (see <see cref="RemoveFlags"/>).</param>
        /// <param name="safeMode">The SafeMode to use for this operation.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        public virtual SafeModeResult Remove(
           IMongoQuery query,
           RemoveFlags flags,
           SafeMode safeMode
        ) {
            using (var message = new MongoDeleteMessage(server, FullName, flags, query)) {
                var connection = server.AcquireConnection(database, false); // not slaveOk
                try {
                    return connection.SendMessage(message, safeMode);
                } finally {
                    server.ReleaseConnection(connection);
                }
            }
        }

        /// <summary>
        /// Removes all documents from this collection (see also <see cref="Drop"/>).
        /// </summary>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        public virtual SafeModeResult RemoveAll() {
            return Remove(Query.Null, RemoveFlags.None, settings.SafeMode);
        }

        /// <summary>
        /// Removes all documents from this collection (see also <see cref="Drop"/>).
        /// </summary>
        /// <param name="safeMode">The SafeMode to use for this operation.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        public virtual SafeModeResult RemoveAll(
           SafeMode safeMode
        ) {
            return Remove(Query.Null, RemoveFlags.None, safeMode);
        }

        /// <summary>
        /// Removes all entries for this collection in the index cache used by EnsureIndex. Call this method
        /// when you know (or suspect) that a process other than this one may have dropped one or
        /// more indexes.
        /// </summary>
        public virtual void ResetIndexCache() {
            server.IndexCache.Reset(this);
        }

        /// <summary>
        /// Saves a document to this collection. The document must have an identifiable Id field. Based on the value
        /// of the Id field Save will perform either an Insert or an Update.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document to save.</typeparam>
        /// <param name="document">The document to save.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        public virtual SafeModeResult Save<TDocument>(
            TDocument document
        ) {
            return Save(document, settings.SafeMode);
        }

        /// <summary>
        /// Saves a document to this collection. The document must have an identifiable Id field. Based on the value
        /// of the Id field Save will perform either an Insert or an Update.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document to save.</typeparam>
        /// <param name="document">The document to save.</param>
        /// <param name="safeMode">The SafeMode to use for this operation.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        public virtual SafeModeResult Save<TDocument>(
            TDocument document,
            SafeMode safeMode
        ) {
            var serializer = BsonSerializer.LookupSerializer(document.GetType());
            object id;
            IIdGenerator idGenerator;
            if (serializer.GetDocumentId(document, out id, out idGenerator)) {
                if (id == null && idGenerator == null) {
                    throw new InvalidOperationException("No IdGenerator found");
                }

                if (idGenerator != null && idGenerator.IsEmpty(id)) {
                    id = idGenerator.GenerateId();
                    serializer.SetDocumentId(document, id);
                    return Insert(document, safeMode);
                } else {
                    var query = Query.EQ("_id", BsonValue.Create(id));
                    var update = Builders.Update.Replace(document);
                    return Update(query, update, UpdateFlags.Upsert, safeMode);
                }
            } else {
                throw new InvalidOperationException("Save can only be used with documents that have an Id");
            }
        }

        /// <summary>
        /// Gets a canonical string representation for this database.
        /// </summary>
        /// <returns>A canonical string representation for this database.</returns>
        public override string ToString() {
 	        return FullName;
        }

        /// <summary>
        /// Updates one matching document in this collection.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="update">The update to perform on the matching document.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        public virtual SafeModeResult Update(
            IMongoQuery query,
            IMongoUpdate update
        ) {
            return Update(query, update, UpdateFlags.None, settings.SafeMode);
        }

        /// <summary>
        /// Updates one matching document in this collection.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="update">The update to perform on the matching document.</param>
        /// <param name="safeMode">The SafeMode to use for this operation.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        public virtual SafeModeResult Update(
            IMongoQuery query,
            IMongoUpdate update,
            SafeMode safeMode
        ) {
            return Update(query, update, UpdateFlags.None, safeMode);
        }

        /// <summary>
        /// Updates one or more matching documents in this collection (for multiple updates use UpdateFlags.Multi).
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="update">The update to perform on the matching document.</param>
        /// <param name="flags">The flags for this Update.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        public virtual SafeModeResult Update(
            IMongoQuery query,
            IMongoUpdate update,
            UpdateFlags flags
        ) {
            return Update(query, update, flags, settings.SafeMode);
        }

        /// <summary>
        /// Updates one or more matching documents in this collection (for multiple updates use UpdateFlags.Multi).
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="update">The update to perform on the matching document.</param>
        /// <param name="flags">The flags for this Update.</param>
        /// <param name="safeMode">The SafeMode to use for this operation.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        public virtual SafeModeResult Update(
            IMongoQuery query,
            IMongoUpdate update,
            UpdateFlags flags,
            SafeMode safeMode
        ) {
            using (var message = new MongoUpdateMessage(server, FullName, flags, query, update)) {
                var connection = server.AcquireConnection(database, false); // not slaveOk
                try {
                    return connection.SendMessage(message, safeMode);
                } finally {
                    server.ReleaseConnection(connection);
                }
            }
        }

        /// <summary>
        /// Validates the integrity of this collection.
        /// </summary>
        /// <returns>A <see cref="ValidateCollectionResult"/>.</returns>
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

    /// <summary>
    /// Represents a MongoDB collection and the settings used to access it as well as a default document type. This class is thread-safe.
    /// </summary>
    public class MongoCollection<TDefaultDocument> : MongoCollection {
        #region constructors
        /// <summary>
        /// Creates a new instance of MongoCollection. Normally you would call one of the indexers or GetCollection methods
        /// of MongoDatabase instead.
        /// </summary>
        /// <param name="database">The database that contains this collection.</param>
        /// <param name="settings">The settings to use to access this collection.</param>
        public MongoCollection(
            MongoDatabase database,
            MongoCollectionSettings<TDefaultDocument> settings
        )
            : base(database, settings) {
        }
        #endregion

        #region public methods
        /// <summary>
        /// Returns a cursor that can be used to find all documents in this collection that match the query as TDefaultDocuments.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>A <see cref="MongoCursor{TDocument}"/>.</returns>
        public virtual MongoCursor<TDefaultDocument> Find(
            IMongoQuery query
        ) {
            return FindAs<TDefaultDocument>(query);
        }

        /// <summary>
        /// Returns a cursor that can be used to find all documents in this collection as TDefaultDocuments.
        /// </summary>
        /// <returns>A <see cref="MongoCursor{TDocument}"/>.</returns>
        public virtual MongoCursor<TDefaultDocument> FindAll() {
            return FindAllAs<TDefaultDocument>();
        }

        /// <summary>
        /// Returns a cursor that can be used to find one document in this collection as a TDefaultDocument.
        /// </summary>
        /// <returns>A TDefaultDocument (or null if not found).</returns>
        public virtual TDefaultDocument FindOne() {
            return FindOneAs<TDefaultDocument>();
        }

        /// <summary>
        /// Returns a cursor that can be used to find one document in this collection that matches a query as a TDefaultDocument.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>A TDefaultDocument (or null if not found).</returns>
        public virtual TDefaultDocument FindOne(
            IMongoQuery query
        ) {
            return FindOneAs<TDefaultDocument>(query);
        }

        /// <summary>
        /// Returns a cursor that can be used to find one document in this collection by its _id value as a TDefaultDocument.
        /// </summary>
        /// <param name="id">The id of the document.</param>
        /// <returns>A TDefaultDocument (or null if not found).</returns>
        public virtual TDefaultDocument FindOneById(
            BsonValue id
        ) {
            return FindOneByIdAs<TDefaultDocument>(id);
        }

        /// <summary>
        /// Runs a GeoNear command on this collection.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="x">The x coordinate of the starting location.</param>
        /// <param name="y">The y coordinate of the starting location.</param>
        /// <param name="limit">The maximum number of results returned.</param>
        /// <returns>A <see cref="GeoNearResult{TDefaultDocument}"/>.</returns>
        public virtual GeoNearResult<TDefaultDocument> GeoNear(
            IMongoQuery query,
            double x,
            double y,
            int limit
        ) {
            return GeoNearAs<TDefaultDocument>(query, x, y, limit);
        }

        /// <summary>
        /// Runs a GeoNear command on this collection.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="x">The x coordinate of the starting location.</param>
        /// <param name="y">The y coordinate of the starting location.</param>
        /// <param name="limit">The maximum number of results returned.</param>
        /// <param name="options">Options for the GeoNear command (see <see cref="GeoNearOptionsDocument"/>, <see cref="GeoNearOptionsWrapper"/>, and the <see cref="GeoNearOptions"/> builder).</param>
        /// <returns>A <see cref="GeoNearResult{TDefaultDocument}"/>.</returns>
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
