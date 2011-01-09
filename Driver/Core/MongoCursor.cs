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
        private int skip;
        private int limit; // number of documents to return (enforced by cursor)
        private int batchSize; // number of documents to return in each reply
        private bool isFrozen; // prevent any further modifications once enumeration has begun
        #endregion

        #region constructors
        internal MongoCursor(
            MongoCollection collection,
            IMongoQuery query
        ) {
            this.server = collection.Database.Server;
            this.database = collection.Database;
            this.collection = collection;
            this.query = query;

            if (server.SlaveOk) {
                this.flags |= QueryFlags.SlaveOk;
            }
        }
        #endregion

        #region public properties
        public MongoCollection Collection {
            get { return collection; }
        }

        public IMongoQuery Query {
            get { return query; }
        }

        public IMongoFields Fields {
            get { return fields; }
            set { fields = value; }
        }

        public BsonDocument Options {
            get { return options; }
            set { options = value; }
        }

        public QueryFlags Flags {
            get { return flags; }
            set { flags = value; }
        }

        public int Skip {
            get { return skip; }
            set { skip = value; }
        }

        public int Limit {
            get { return limit; }
            set { limit = value; }
        }

        public int BatchSize {
            get { return batchSize; }
            set { batchSize = value; }
        }

        public bool IsFrozen {
            get { return isFrozen; }
        }
        #endregion

        #region public methods
        public MongoCursor<TNewDocument> Clone<TNewDocument>() {
            var clone = new MongoCursor<TNewDocument>(collection, query);
            clone.options = options == null ? null : (BsonDocument) options.Clone();
            clone.flags = flags;
            clone.skip = skip;
            clone.limit = limit;
            clone.batchSize = batchSize;
            return clone;
        }

        public int Count() {
            isFrozen = true;
            var command = new CommandDocument {
                { "count", collection.Name },
                { "query", BsonDocumentWrapper.Create(query) } // query is optional
            };
            var result = database.RunCommand(command);
            return result.Response["n"].ToInt32();
        }

        public BsonDocument Explain() {
            return Explain(false);
        }

        public BsonDocument Explain(
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

        public IEnumerator<TDocument> GetEnumerator() {
            isFrozen = true;
            return new MongoCursorEnumerator(this);
        }

        public MongoCursor<TDocument> SetBatchSize(
            int batchSize
        ) {
            if (isFrozen) { ThrowFrozen(); }
            if (batchSize < 0) { throw new ArgumentException("BatchSize cannot be negative"); }
            this.batchSize = batchSize;
            return this;
        }

        public MongoCursor<TDocument> SetFields(
            IMongoFields fields
        ) {
            if (isFrozen) { ThrowFrozen(); }
            this.fields = fields;
            return this;
        }

        public MongoCursor<TDocument> SetFields(
            params string[] fields
        ) {
            if (isFrozen) { ThrowFrozen(); }
            this.fields = Builders.Fields.Include(fields);
            return this;
        }

        public MongoCursor<TDocument> SetFlags(
            QueryFlags flags
        ) {
            if (isFrozen) { ThrowFrozen(); }
            this.flags = flags;
            return this;
        }

        public MongoCursor<TDocument> SetHint(
            BsonDocument hint
        ) {
            if (isFrozen) { ThrowFrozen(); }
            SetOption("$hint", hint);
            return this;
        }

        public MongoCursor<TDocument> SetLimit(
            int limit
        ) {
            if (isFrozen) { ThrowFrozen(); }
            this.limit = limit;
            return this;
        }

        public MongoCursor<TDocument> SetMax(
           BsonDocument max
       ) {
            if (isFrozen) { ThrowFrozen(); }
            SetOption("$max", max);
            return this;
        }

        public MongoCursor<TDocument> SetMaxScan(
            int maxScan
        ) {
            if (isFrozen) { ThrowFrozen(); }
            SetOption("$maxscan", maxScan);
            return this;
        }

        public MongoCursor<TDocument> SetMin(
           BsonDocument min
       ) {
            if (isFrozen) { ThrowFrozen(); }
            SetOption("$min", min);
            return this;
        }

        public MongoCursor<TDocument> SetOption(
            string name,
            BsonValue value
        ) {
            if (isFrozen) { ThrowFrozen(); }
            if (options == null) { options = new BsonDocument(); }
            options[name] = value;
            return this;
        }

        public MongoCursor<TDocument> SetOptions(
            BsonDocument options
        ) {
            if (isFrozen) { ThrowFrozen(); }
            this.options = options;
            foreach (var option in options) {
                this.options[option.Name] = option.Value;
            }
            return this;
        }

        public MongoCursor<TDocument> SetShowDiskLoc() {
            if (isFrozen) { ThrowFrozen(); }
            SetOption("$showDiskLoc", true);
            return this;
        }

        public MongoCursor<TDocument> SetSkip(
            int skip
        ) {
            if (isFrozen) { ThrowFrozen(); }
            if (skip < 0) { throw new ArgumentException("Skip cannot be negative"); }
            this.skip = skip;
            return this;
        }

        public MongoCursor<TDocument> SetSnapshot() {
            if (isFrozen) { ThrowFrozen(); }
            SetOption("$snapshot", true);
            return this;
        }

        public MongoCursor<TDocument> SetSortOrder(
            IMongoSortBy sortBy
        ) {
            if (isFrozen) { ThrowFrozen(); }
            SetOption("$orderby", BsonDocumentWrapper.Create(sortBy));
            return this;
        }

        public MongoCursor<TDocument> SetSortOrder(
            params string[] keys
        ) {
            if (isFrozen) { ThrowFrozen(); }
            return SetSortOrder(SortBy.Ascending(keys));
        }

        public int Size() {
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

        #region nested classes
        private class MongoCursorEnumerator : IEnumerator<TDocument> {
            #region private fields
            private bool disposed = false;
            private bool started = false;
            private bool done = false;
            private MongoCursor<TDocument> cursor;
            private MongoConnection connection;
            private int count;
            private int positiveLimit;
            private MongoReplyMessage<TDocument> reply;
            private int replyIndex;
            private long openCursorId;
            #endregion

            #region constructors
            public MongoCursorEnumerator(
                MongoCursor<TDocument> cursor
            ) {
                this.cursor = cursor;
                this.positiveLimit = cursor.limit >= 0 ? cursor.limit : -cursor.limit;
            }
            #endregion

            #region public properties
            public TDocument Current {
                get {
                    if (disposed) { throw new ObjectDisposedException("MongoCursorEnumerator"); }
                    if (!started) {
                        throw new InvalidOperationException("Current is not valid until MoveNext has been called");
                    }
                    if (done) {
                        throw new InvalidOperationException("Current is not valid after MoveNext has returned false");
                    }
                    return reply.Documents[replyIndex];
                }
            }
            #endregion

            #region public methods
            public void Dispose() {
                if (!disposed) {
                    try {
                        ReleaseConnection();
                    } finally {
                        disposed = true;
                    }
                }
            }

            public bool MoveNext() {
                if (disposed) { throw new ObjectDisposedException("MongoCursorEnumerator"); }
                if (done) {
                    return false;
                }

                if (!started) {
                    reply = GetFirst(); // sets connection if successfull
                    if (reply.Documents.Count == 0) {
                        reply = null;
                        done = true;
                        return false;
                    }
                    replyIndex = -1;
                    started = true;
                }

                if (positiveLimit != 0 && count == positiveLimit) {
                    ReleaseConnection(); // early exit
                    reply = null;
                    done = true;
                    return false;
                }

                if (replyIndex < reply.Documents.Count - 1) {
                    replyIndex++; // move to next document in the current reply
                } else {
                    if (openCursorId != 0) {
                        reply = GetMore(); // uses connection set by GetFirst
                        if (reply.Documents.Count == 0) {
                            reply = null;
                            done = true;
                            return false;
                        }
                        replyIndex = 0;
                    } else {
                        reply = null;
                        done = true;
                        return false;
                    }
                }

                count++;
                return true;
            }

            public void Reset() {
                throw new NotImplementedException();
            }
            #endregion

            #region explicit interface implementations
            object IEnumerator.Current {
                get { return Current; }
            }
            #endregion

            #region private methods
            private MongoReplyMessage<TDocument> GetFirst() {
                bool slaveOk = (cursor.flags & QueryFlags.SlaveOk) != 0;
                connection = cursor.server.AcquireConnection(cursor.database, slaveOk);
                try {
                    // some of these weird conditions are necessary to get commands to run correctly
                    // specifically numberToReturn has to be 1 or -1 for commands
                    int numberToReturn;
                    if (cursor.limit < 0) {
                        numberToReturn = cursor.limit;
                    } else if (cursor.limit == 0) {
                        numberToReturn = cursor.batchSize;
                    } else if (cursor.batchSize == 0) {
                        numberToReturn = cursor.limit;
                    } else if (cursor.limit < cursor.batchSize) {
                        numberToReturn = cursor.limit;
                    } else {
                        numberToReturn = cursor.batchSize;
                    }

                    using (
                        var message = new MongoQueryMessage(
                            cursor.server,
                            cursor.Collection.FullName,
                            cursor.flags,
                            cursor.skip,
                            numberToReturn,
                            WrapQuery(),
                            cursor.fields
                        )
                    ) {
                        return GetReply(message);
                    }
                } catch {
                    try { ReleaseConnection(); } catch { } // ignore exceptions
                    throw;
                }
            }

            private MongoReplyMessage<TDocument> GetMore() {
                try {
                    int numberToReturn;
                    if (positiveLimit != 0) {
                        numberToReturn = positiveLimit - count;
                        if (cursor.batchSize != 0 && numberToReturn > cursor.batchSize) {
                            numberToReturn = cursor.batchSize;
                        }
                    } else {
                        numberToReturn = cursor.batchSize;
                    }

                    using (
                        var message = new MongoGetMoreMessage(
                            cursor.server,
                            cursor.Collection.FullName,
                            numberToReturn,
                            openCursorId
                        )
                    ) {
                        return GetReply(message);
                    }
                } catch {
                    try { ReleaseConnection(); } catch { } // ignore exceptions
                    throw;
                }
            }

            private MongoReplyMessage<TDocument> GetReply(
                MongoRequestMessage message
            ) {
                connection.SendMessage(message, SafeMode.False); // safemode doesn't apply to queries
                var reply = connection.ReceiveMessage<TDocument>(cursor.server);
                openCursorId = reply.CursorId;
                if (openCursorId == 0) {
                    ReleaseConnection();
                }
                return reply;
            }

            private void ReleaseConnection() {
                if (connection != null) {
                    try {
                        if (openCursorId != 0) {
                            using (var message = new MongoKillCursorsMessage(cursor.server, openCursorId)) {
                                connection.SendMessage(message, SafeMode.False); // no need to use SafeMode for KillCursors
                            }
                        }
                        cursor.server.ReleaseConnection(connection);
                    } finally {
                        connection = null;
                        openCursorId = 0;
                    }
                }
            }

            private IMongoQuery WrapQuery() {
                if (cursor.options == null) {
                    return cursor.query;
                } else {
                    var query = (cursor.query == null) ? (BsonValue) new BsonDocument() : BsonDocumentWrapper.Create(cursor.query);
                    var wrappedQuery = new QueryDocument("$query", query);
                    wrappedQuery.Merge(cursor.options);
                    return wrappedQuery;
                }
            }
            #endregion
        }
        #endregion
    }
}
