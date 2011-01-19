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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver {
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
        public virtual MongoServer Server {
            get { return server; }
        }

        public virtual MongoDatabase Database {
            get { return database; }
        }

        public virtual MongoCollection Collection {
            get { return collection; }
        }

        public virtual IMongoQuery Query {
            get { return query; }
        }

        public virtual IMongoFields Fields {
            get { return fields; }
            set { fields = value; }
        }

        public virtual BsonDocument Options {
            get { return options; }
            set { options = value; }
        }

        public virtual QueryFlags Flags {
            get { return flags | (slaveOk ? QueryFlags.SlaveOk : 0); }
            set { flags = value; }
        }

        public virtual bool SlaveOk {
            get { return slaveOk || ((flags & QueryFlags.SlaveOk) != 0); }
            set { slaveOk = value; }
        }

        public virtual int Skip {
            get { return skip; }
            set { skip = value; }
        }

        public virtual int Limit {
            get { return limit; }
            set { limit = value; }
        }

        public virtual int BatchSize {
            get { return batchSize; }
            set { batchSize = value; }
        }

        public virtual bool IsFrozen {
            get { return isFrozen; }
        }
        #endregion

        #region public methods
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

        public virtual int Count() {
            isFrozen = true;
            var command = new CommandDocument {
                { "count", collection.Name },
                { "query", BsonDocument.Wrap(query) } // query is optional
            };
            var result = database.RunCommand(command);
            return result.Response["n"].ToInt32();
        }

        public virtual BsonDocument Explain() {
            return Explain(false);
        }

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

        public virtual IEnumerator<TDocument> GetEnumerator() {
            isFrozen = true;
            return new MongoCursorEnumerator<TDocument>(this);
        }

        public virtual MongoCursor<TDocument> SetBatchSize(
            int batchSize
        ) {
            if (isFrozen) { ThrowFrozen(); }
            if (batchSize < 0) { throw new ArgumentException("BatchSize cannot be negative"); }
            this.batchSize = batchSize;
            return this;
        }

        public virtual MongoCursor<TDocument> SetFields(
            IMongoFields fields
        ) {
            if (isFrozen) { ThrowFrozen(); }
            this.fields = fields;
            return this;
        }

        public virtual MongoCursor<TDocument> SetFields(
            params string[] fields
        ) {
            if (isFrozen) { ThrowFrozen(); }
            this.fields = Builders.Fields.Include(fields);
            return this;
        }

        public virtual MongoCursor<TDocument> SetFlags(
            QueryFlags flags
        ) {
            if (isFrozen) { ThrowFrozen(); }
            this.flags = flags;
            return this;
        }

        public virtual MongoCursor<TDocument> SetHint(
            BsonDocument hint
        ) {
            if (isFrozen) { ThrowFrozen(); }
            SetOption("$hint", hint);
            return this;
        }

        public virtual MongoCursor<TDocument> SetLimit(
            int limit
        ) {
            if (isFrozen) { ThrowFrozen(); }
            this.limit = limit;
            return this;
        }

        public virtual MongoCursor<TDocument> SetMax(
           BsonDocument max
       ) {
            if (isFrozen) { ThrowFrozen(); }
            SetOption("$max", max);
            return this;
        }

        public virtual MongoCursor<TDocument> SetMaxScan(
            int maxScan
        ) {
            if (isFrozen) { ThrowFrozen(); }
            SetOption("$maxscan", maxScan);
            return this;
        }

        public virtual MongoCursor<TDocument> SetMin(
           BsonDocument min
       ) {
            if (isFrozen) { ThrowFrozen(); }
            SetOption("$min", min);
            return this;
        }

        public virtual MongoCursor<TDocument> SetOption(
            string name,
            BsonValue value
        ) {
            if (isFrozen) { ThrowFrozen(); }
            if (options == null) { options = new BsonDocument(); }
            options[name] = value;
            return this;
        }

        public virtual MongoCursor<TDocument> SetOptions(
            BsonDocument options
        ) {
            if (isFrozen) { ThrowFrozen(); }
            this.options = options;
            foreach (var option in options) {
                this.options[option.Name] = option.Value;
            }
            return this;
        }

        public virtual MongoCursor<TDocument> SetShowDiskLoc() {
            if (isFrozen) { ThrowFrozen(); }
            SetOption("$showDiskLoc", true);
            return this;
        }

        public virtual MongoCursor<TDocument> SetSkip(
            int skip
        ) {
            if (isFrozen) { ThrowFrozen(); }
            if (skip < 0) { throw new ArgumentException("Skip cannot be negative"); }
            this.skip = skip;
            return this;
        }

        public virtual MongoCursor<TDocument> SetSlaveOk(
            bool slaveOk
        ) {
            if (isFrozen) { ThrowFrozen(); }
            this.slaveOk = slaveOk;
            return this;
        }

        public virtual MongoCursor<TDocument> SetSnapshot() {
            if (isFrozen) { ThrowFrozen(); }
            SetOption("$snapshot", true);
            return this;
        }

        public virtual MongoCursor<TDocument> SetSortOrder(
            IMongoSortBy sortBy
        ) {
            if (isFrozen) { ThrowFrozen(); }
            SetOption("$orderby", BsonDocument.Wrap(sortBy));
            return this;
        }

        public virtual MongoCursor<TDocument> SetSortOrder(
            params string[] keys
        ) {
            if (isFrozen) { ThrowFrozen(); }
            return SetSortOrder(SortBy.Ascending(keys));
        }

        public virtual int Size() {
            isFrozen = true;
            var command = new CommandDocument {
                { "count", collection.Name },
                { "query", BsonDocument.Wrap(query) }, // query is optional
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
