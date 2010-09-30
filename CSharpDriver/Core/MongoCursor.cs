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

using MongoDB.BsonLibrary;
using MongoDB.CSharpDriver.Builders;
using MongoDB.CSharpDriver.Internal;

namespace MongoDB.CSharpDriver {
    public class MongoCursor<TQuery, TResult> : IEnumerable<TResult> {
        #region private fields
        private MongoCollection collection;
        private TQuery query;
        private BsonDocumentWrapper fields;
        private BsonDocument options;
        private QueryFlags flags;
        private int skip;
        private int limit; // number of documents to return (enforced by cursor)
        private int batchSize; // number of documents to return in each reply
        private bool frozen; // prevent any further modifications once enumeration has begun
        #endregion

        #region constructors
        internal MongoCursor(
            MongoCollection collection,
            TQuery query
        ) {
            this.collection = collection;
            this.query = query;

            if (collection.Database.Server.SlaveOk) {
                this.flags |= QueryFlags.SlaveOk;
            }
        }
        #endregion

        #region public properties
        public MongoCollection Collection {
            get { return collection; }
        }
        #endregion

        #region public methods
        public MongoCursor<TQuery, TResult> AddOption(
            string name,
            BsonValue value
        ) {
            if (frozen) { ThrowFrozen(); }
            if (options == null) { options = new BsonDocument(); }
            options[name] = value;
            return this;
        }

        public MongoCursor<TQuery, TResult> BatchSize(
            int batchSize
        ) {
            if (frozen) { ThrowFrozen(); }
            if (batchSize < 0) { throw new ArgumentException("BatchSize cannot be negative"); }
            this.batchSize = batchSize;
            return this;
        }

        public MongoCursor<TQuery, TResultNew> Clone<TResultNew>() {
            var clone = new MongoCursor<TQuery, TResultNew>(collection, query);
            clone.options = options == null ? null : (BsonDocument) options.Clone();
            clone.flags = flags;
            clone.skip = skip;
            clone.limit = limit;
            clone.batchSize = batchSize;
            return clone;
        }

        public int Count() {
            frozen = true;
            var command = new BsonDocument {
                { "count", collection.Name },
                { "query", BsonDocumentWrapper.Create(query) } // query is optional
            };
            var result = collection.Database.RunCommand(command);
            return result["n"].ToInt32();
        }

        public BsonDocument Explain() {
            return Explain(false);
        }

        public BsonDocument Explain(
            bool verbose
        ) {
            frozen = true;
            var clone = this.Clone<BsonDocument>();
            clone.AddOption("$explain", true);
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

        public MongoCursor<TQuery, TResult> Fields<TFields>(
            TFields fields
        ) {
            if (frozen) { ThrowFrozen(); }
            this.fields = BsonDocumentWrapper.Create(fields);
            return this;
        }

        public MongoCursor<TQuery, TResult> Fields<TFields>(
            params string[] fields
        ) {
            if (frozen) { ThrowFrozen(); }
            this.fields = BsonDocumentWrapper.Create(Builders.Fields.Include(fields));
            return this;
        }

        public MongoCursor<TQuery, TResult> Flags(
            QueryFlags flags
        ) {
            if (frozen) { ThrowFrozen(); }
            this.flags = flags;
            return this;
        }

        public IEnumerator<TResult> GetEnumerator() {
            frozen = true;
            return new MongoCursorEnumerator(this);
        }

        public MongoCursor<TQuery, TResult> Hint(
            BsonDocument hint
        ) {
            if (frozen) { ThrowFrozen(); }
            AddOption("$hint", hint);
            return this;
        }

        public MongoCursor<TQuery, TResult> Limit(
            int limit
        ) {
            if (frozen) { ThrowFrozen(); }
            this.limit = limit;
            return this;
        }

        public MongoCursor<TQuery, TResult> Max(
           BsonDocument max
       ) {
            if (frozen) { ThrowFrozen(); }
            AddOption("$max", max);
            return this;
        }

        public MongoCursor<TQuery, TResult> MaxScan(
            int maxScan
        ) {
            if (frozen) { ThrowFrozen(); }
            AddOption("$maxscan", maxScan);
            return this;
        }

        public MongoCursor<TQuery, TResult> Min(
           BsonDocument min
       ) {
            if (frozen) { ThrowFrozen(); }
            AddOption("$min", min);
            return this;
        }

        public MongoCursor<TQuery, TResult> Options(
            BsonDocument options
        ) {
            if (frozen) { ThrowFrozen(); }
            this.options = options;
            return this;
        }

        public int Size() {
            frozen = true;
            var command = new BsonDocument {
                { "count", collection.Name },
                { "query", BsonDocumentWrapper.Create(query) }, // query is optional
                { limit != 0, "limit", limit },
                { skip != 0, "skip", skip }
            };
            var result = collection.Database.RunCommand(command);
            return result["n"].ToInt32();
        }

        public MongoCursor<TQuery, TResult> ShowDiskLoc() {
            if (frozen) { ThrowFrozen(); }
            AddOption("$showDiskLoc", true);
            return this;
        }

        public MongoCursor<TQuery, TResult> Skip(
            int skip
        ) {
            if (frozen) { ThrowFrozen(); }
            if (skip < 0) { throw new ArgumentException("Skip cannot be negative"); }
            this.skip = skip;
            return this;
        }

        public MongoCursor<TQuery, TResult> Snapshot() {
            if (frozen) { ThrowFrozen(); }
            AddOption("$snapshot", true);
            return this;
        }

        public MongoCursor<TQuery, TResult> Sort<TSortBy>(
            TSortBy sortBy
        ) {
            if (frozen) { ThrowFrozen(); }
            AddOption("$orderby", BsonDocumentWrapper.Create(sortBy));
            return this;
        }

        public MongoCursor<TQuery, TResult> Sort(
            params string[] keys
        ) {
            if (frozen) { ThrowFrozen(); }
            return Sort(SortBy.Ascending(keys));
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
            throw new InvalidOperationException("A cursor cannot be modified after enumeration has begun");
        }
        #endregion

        #region nested classes
        private class MongoCursorEnumerator : IEnumerator<TResult> {
            #region private fields
            private bool disposed = false;
            private bool started = false;
            private bool done = false;
            private MongoCursor<TQuery, TResult> cursor;
            private MongoConnection connection;
            private int count;
            private int positiveLimit;
            private MongoReplyMessage<TResult> reply;
            private int replyIndex;
            private long openCursorId;
            #endregion

            #region constructors
            public MongoCursorEnumerator(
                MongoCursor<TQuery, TResult> cursor
            ) {
                this.cursor = cursor;
                this.positiveLimit = cursor.limit >= 0 ? cursor.limit : -cursor.limit;
            }
            #endregion

            #region public properties
            public TResult Current {
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
            private MongoReplyMessage<TResult> GetFirst() {
                connection = cursor.Collection.Database.GetConnection();
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
                        var message = new MongoQueryMessage<BsonDocumentWrapper>(
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

            private MongoReplyMessage<TResult> GetMore() {
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

            private MongoReplyMessage<TResult> GetReply(
                MongoRequestMessage message
            ) {
                connection.SendMessage(message, SafeMode.False); // safemode doesn't apply to queries
                var reply = connection.ReceiveMessage<TResult>();
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
                            using (var message = new MongoKillCursorsMessage(openCursorId)) {
                                connection.SendMessage(message, SafeMode.False); // no need to use SafeMode for KillCursors
                            }
                        }
                        cursor.Collection.Database.ReleaseConnection(connection);
                    } finally {
                        connection = null;
                        openCursorId = 0;
                    }
                }
            }

            private BsonDocumentWrapper WrapQuery() {
                if (cursor.options == null) {
                    return BsonDocumentWrapper.Create(cursor.query);
                } else {
                    var query = (cursor.query == null) ? (BsonValue) new BsonDocument() : BsonDocumentWrapper.Create(cursor.query);
                    var wrappedQuery = new BsonDocument("$query", query);
                    wrappedQuery.Merge(cursor.options);
                    return BsonDocumentWrapper.Create(wrappedQuery);
                }
            }
            #endregion
        }
        #endregion
    }
}
