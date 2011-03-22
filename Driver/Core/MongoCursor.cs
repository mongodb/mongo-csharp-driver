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
using System.Linq;
using System.Text;

using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver {
    /// <summary>
    /// An object that can be enumerated to fetch the results of a query. The query is not sent
    /// to the server until you begin enumerating the results.
    /// </summary>
    /// <typeparam name="TDocument">The type of the documents returned.</typeparam>
    public class MongoCursor<TDocument> : IEnumerable<TDocument> {
        #region private fields
        private MongoServer server;
        private MongoDatabase database;
        private MongoCollection collection;
        private IMongoQuery query;
        private IMongoFields fields;
        private BsonDocument options;
        private QueryFlags flags;
        private bool slaveOk;
        private int skip;
        private int limit; // number of documents to return (enforced by cursor)
        private int batchSize; // number of documents to return in each reply
        private bool isFrozen; // prevent any further modifications once enumeration has begun
        #endregion

        #region constructors
        /// <summary>
        /// Creates a new MongoCursor. It is very unlikely that you will call this constructor. Instead, see all the Find methods in MongoCollection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="query">The query.</param>
        public MongoCursor(
            MongoCollection collection,
            IMongoQuery query
        ) {
            this.server = collection.Database.Server;
            this.database = collection.Database;
            this.collection = collection;
            this.query = query;
            this.slaveOk = collection.Settings.SlaveOk;
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the server that the query will be sent to.
        /// </summary>
        public virtual MongoServer Server {
            get { return server; }
        }

        /// <summary>
        /// Gets the database that constains the collection that is being queried.
        /// </summary>
        public virtual MongoDatabase Database {
            get { return database; }
        }

        /// <summary>
        /// Gets the collection that is being queried.
        /// </summary>
        public virtual MongoCollection Collection {
            get { return collection; }
        }

        /// <summary>
        /// Gets the query that will be sent to the server.
        /// </summary>
        public virtual IMongoQuery Query {
            get { return query; }
        }

        /// <summary>
        /// Gets or sets the fields that will be returned from the server.
        /// </summary>
        public virtual IMongoFields Fields {
            get { return fields; }
            set { fields = value; }
        }

        /// <summary>
        /// Gets or sets the cursor options. See also the individual Set{Option} methods, which are easier to use.
        /// </summary>
        public virtual BsonDocument Options {
            get { return options; }
            set { options = value; }
        }

        /// <summary>
        /// Gets or sets the query flags.
        /// </summary>
        public virtual QueryFlags Flags {
            get { return flags | (slaveOk ? QueryFlags.SlaveOk : 0); }
            set { flags = value; }
        }

        /// <summary>
        /// Gets or sets whether the query should be sent to a secondary server.
        /// </summary>
        public virtual bool SlaveOk {
            get { return slaveOk || ((flags & QueryFlags.SlaveOk) != 0); }
            set { slaveOk = value; }
        }

        /// <summary>
        /// Gets or sets the number of documents the server should skip before returning the rest of the documents.
        /// </summary>
        public virtual int Skip {
            get { return skip; }
            set { skip = value; }
        }

        /// <summary>
        /// Gets or sets the limit on the number of documents to be returned.
        /// </summary>
        public virtual int Limit {
            get { return limit; }
            set { limit = value; }
        }

        /// <summary>
        /// Gets or sets the batch size (the number of documents returned per batch).
        /// </summary>
        public virtual int BatchSize {
            get { return batchSize; }
            set { batchSize = value; }
        }

        /// <summary>
        /// Gets whether the cursor has been frozen to prevent further changes.
        /// </summary>
        public virtual bool IsFrozen {
            get { return isFrozen; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Creates a clone of the cursor.
        /// </summary>
        /// <returns>A clone of the cursor.</returns>
        public virtual MongoCursor<TNewDocument> Clone<TNewDocument>() {
            var clone = new MongoCursor<TNewDocument>(collection, query);
            clone.options = options == null ? null : (BsonDocument) options.Clone();
            clone.flags = flags;
            clone.slaveOk = slaveOk;
            clone.skip = skip;
            clone.limit = limit;
            clone.batchSize = batchSize;
            return clone;
        }

        /// <summary>
        /// Returns the number of documents that match the query (ignores Skip and Limit, unlike Size which honors them).
        /// </summary>
        /// <returns>The number of documents that match the query.</returns>
        public virtual int Count() {
            isFrozen = true;
            var command = new CommandDocument {
                { "count", collection.Name },
                { "query", BsonDocumentWrapper.Create(query) } // query is optional
            };
            var result = database.RunCommand(command);
            return result.Response["n"].ToInt32();
        }

        /// <summary>
        /// Returns an explanation of how the query was executed (instead of the results).
        /// </summary>
        /// <returns>An explanation of thow the query was executed.</returns>
        public virtual BsonDocument Explain() {
            return Explain(false);
        }

        /// <summary>
        /// Returns an explanation of how the query was executed (instead of the results).
        /// </summary>
        /// <param name="verbose">Whether the explanation should contain more details.</param>
        /// <returns>An explanation of thow the query was executed.</returns>
        public virtual BsonDocument Explain(
            bool verbose
        ) {
            isFrozen = true;
            var clone = this.Clone<BsonDocument>();
            clone.SetOption("$explain", true);
            clone.limit = -clone.limit; // TODO: should this be -1?
            var explanation = clone.FirstOrDefault();
            if (!verbose) {
                explanation.Remove("allPlans");
                explanation.Remove("oldPlan");
                if (explanation.Contains("shards")) {
                    var shards = explanation["shards"];
                    if (shards.BsonType == BsonType.Array) {
                        foreach (BsonDocument shard in shards.AsBsonArray) {
                            shard.Remove("allPlans");
                            shard.Remove("oldPlan");
                        }
                    } else {
                        var shard = shards.AsBsonDocument;
                        shard.Remove("allPlans");
                        shard.Remove("oldPlan");
                    }
                }
            }
            return explanation;
        }

        /// <summary>
        /// Returns an enumerator that can be used to enumerate the cursor. Normally you will use the foreach statement
        /// to enumerate the cursor (foreach will call GetEnumerator for you).
        /// </summary>
        /// <returns>An enumerator that can be used to iterate over the cursor.</returns>
        public virtual IEnumerator<TDocument> GetEnumerator() {
            isFrozen = true;
            return new MongoCursorEnumerator<TDocument>(this);
        }

        /// <summary>
        /// Sets the batch size (the number of documents returned per batch).
        /// </summary>
        /// <param name="batchSize">The number of documents in each batch.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor<TDocument> SetBatchSize(
            int batchSize
        ) {
            if (isFrozen) { ThrowFrozen(); }
            if (batchSize < 0) { throw new ArgumentException("BatchSize cannot be negative"); }
            this.batchSize = batchSize;
            return this;
        }

        /// <summary>
        /// Sets the fields that will be returned from the server.
        /// </summary>
        /// <param name="fields">The fields that will be returned from the server.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor<TDocument> SetFields(
            IMongoFields fields
        ) {
            if (isFrozen) { ThrowFrozen(); }
            this.fields = fields;
            return this;
        }

        /// <summary>
        /// Sets the fields that will be returned from the server.
        /// </summary>
        /// <param name="fields">The fields that will be returned from the server.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor<TDocument> SetFields(
            params string[] fields
        ) {
            if (isFrozen) { ThrowFrozen(); }
            this.fields = Builders.Fields.Include(fields);
            return this;
        }

        /// <summary>
        /// Sets the query flags.
        /// </summary>
        /// <param name="flags">The query flags.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor<TDocument> SetFlags(
            QueryFlags flags
        ) {
            if (isFrozen) { ThrowFrozen(); }
            this.flags = flags;
            return this;
        }

        /// <summary>
        /// Sets the index hint for the query.
        /// </summary>
        /// <param name="hint">The index hint.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor<TDocument> SetHint(
            BsonDocument hint
        ) {
            if (isFrozen) { ThrowFrozen(); }
            SetOption("$hint", hint);
            return this;
        }

        /// <summary>
        /// Sets the index hint for the query.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor<TDocument> SetHint(
            string indexName
        ) {
            if (isFrozen) { ThrowFrozen(); }
            SetOption("$hint", indexName);
            return this;
        }

        /// <summary>
        /// Sets the limit on the number of documents to be returned.
        /// </summary>
        /// <param name="limit">The limit on the number of documents to be returned.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor<TDocument> SetLimit(
            int limit
        ) {
            if (isFrozen) { ThrowFrozen(); }
            this.limit = limit;
            return this;
        }

        /// <summary>
        /// Sets the max value for the index key range of documents to return (note: the max value itself is excluded from the range).
        /// Often combined with SetHint (if SetHint is not used the server will attempt to determine the matching index automatically).
        /// </summary>
        /// <param name="max">The max value.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor<TDocument> SetMax(
           BsonDocument max
       ) {
            if (isFrozen) { ThrowFrozen(); }
            SetOption("$max", max);
            return this;
        }

        /// <summary>
        /// Sets the maximum number of documents to scan.
        /// </summary>
        /// <param name="maxScan">The maximum number of documents to scan.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor<TDocument> SetMaxScan(
            int maxScan
        ) {
            if (isFrozen) { ThrowFrozen(); }
            SetOption("$maxscan", maxScan);
            return this;
        }

        /// <summary>
        /// Sets the min value for the index key range of documents to return (note: the min value itself is included in the range).
        /// Often combined with SetHint (if SetHint is not used the server will attempt to determine the matching index automatically).
        /// </summary>
        /// <param name="min">The min value.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor<TDocument> SetMin(
           BsonDocument min
       ) {
            if (isFrozen) { ThrowFrozen(); }
            SetOption("$min", min);
            return this;
        }

        /// <summary>
        /// Sets a cursor option.
        /// </summary>
        /// <param name="name">The name of the option.</param>
        /// <param name="value">The value of the option.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor<TDocument> SetOption(
            string name,
            BsonValue value
        ) {
            if (isFrozen) { ThrowFrozen(); }
            if (options == null) { options = new BsonDocument(); }
            options[name] = value;
            return this;
        }

        /// <summary>
        /// Sets multiple cursor options. See also the individual Set{Option} methods, which are easier to use.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor<TDocument> SetOptions(
            BsonDocument options
        ) {
            if (isFrozen) { ThrowFrozen(); }
            if (options != null) {
                if (this.options == null) { this.options = new BsonDocument(); }
                this.options.Merge(options, true); // overwriteExistingElements
            }
            return this;
        }

        /// <summary>
        /// Sets the $showDiskLoc option.
        /// </summary>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor<TDocument> SetShowDiskLoc() {
            if (isFrozen) { ThrowFrozen(); }
            SetOption("$showDiskLoc", true);
            return this;
        }

        /// <summary>
        /// Sets the number of documents the server should skip before returning the rest of the documents.
        /// </summary>
        /// <param name="skip">The number of documents to skip.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor<TDocument> SetSkip(
            int skip
        ) {
            if (isFrozen) { ThrowFrozen(); }
            if (skip < 0) { throw new ArgumentException("Skip cannot be negative"); }
            this.skip = skip;
            return this;
        }

        /// <summary>
        /// Sets whether the query should be sent to a secondary server.
        /// </summary>
        /// <param name="slaveOk">Whether the query should be sent to a secondary server.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor<TDocument> SetSlaveOk(
            bool slaveOk
        ) {
            if (isFrozen) { ThrowFrozen(); }
            this.slaveOk = slaveOk;
            return this;
        }

        /// <summary>
        /// Sets the $snapshot option.
        /// </summary>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor<TDocument> SetSnapshot() {
            if (isFrozen) { ThrowFrozen(); }
            SetOption("$snapshot", true);
            return this;
        }

        /// <summary>
        /// Sets the sort order for the server to sort the documents by before returning them.
        /// </summary>
        /// <param name="sortBy">The sort order.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor<TDocument> SetSortOrder(
            IMongoSortBy sortBy
        ) {
            if (isFrozen) { ThrowFrozen(); }
            SetOption("$orderby", BsonDocumentWrapper.Create(sortBy));
            return this;
        }

        /// <summary>
        /// Sets the sort order for the server to sort the documents by before returning them.
        /// </summary>
        /// <param name="keys">The names of the fields to sort by.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor<TDocument> SetSortOrder(
            params string[] keys
        ) {
            if (isFrozen) { ThrowFrozen(); }
            return SetSortOrder(SortBy.Ascending(keys));
        }

        /// <summary>
        /// Returns the size of the result set (honors Skip and Limit, unlike Count which does not).
        /// </summary>
        /// <returns>The size of the result set.</returns>
        public virtual int Size() {
            isFrozen = true;
            var command = new CommandDocument {
                { "count", collection.Name },
                { "query", BsonDocumentWrapper.Create(query) }, // query is optional
                { "limit", limit, limit != 0 },
                { "skip", skip, skip != 0 }
            };
            var result = database.RunCommand(command);
            return result.Response["n"].ToInt32();
        }
        #endregion

        #region explicit interface implementations
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
        #endregion

        #region private methods
        // funnel exceptions through this method so we can have a single error message
        private void ThrowFrozen() {
            throw new InvalidOperationException("A MongoCursor object cannot be modified once it has been frozen");
        }
        #endregion
    }
}
