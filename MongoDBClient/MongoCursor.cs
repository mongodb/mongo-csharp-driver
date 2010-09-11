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
using MongoDB.MongoDBClient.Internal;

namespace MongoDB.MongoDBClient {
    public class MongoCursor<T> : IEnumerable<T> where T : new() {
        #region private fields
        private MongoCollection collection;
        private BsonDocument query;
        private BsonDocument fields;
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
            BsonDocument query,
            BsonDocument fields
        ) {
            this.collection = collection;
            this.query = query;
            this.fields = fields;

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
        public MongoCursor<T> AddOption(
            string name,
            BsonValue value
        ) {
            if (frozen) { ThrowFrozen(); }
            if (options == null) { options = new BsonDocument(); }
            options[name] = value;
            return this;
        }

        public MongoCursor<T> BatchSize(
            int batchSize
        ) {
            if (frozen) { ThrowFrozen(); }
            if (batchSize < 0) { throw new ArgumentException("BatchSize cannot be negative"); }
            this.batchSize = batchSize;
            return this;
        }

        public MongoCursor<TNew> Clone<TNew>() where TNew : new() {
            var clone = new MongoCursor<TNew>(collection, query, fields);
            clone.options = options == null ? null : (BsonDocument) options.Clone();
            clone.flags = flags;
            clone.skip = skip;
            clone.limit = limit;
            clone.batchSize = batchSize;
            return clone;
        }

        public int Count() {
            var command = new BsonDocument {
                { "count", collection.Name },
                { "query", query ?? new BsonDocument() },
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
            var clone = this.Clone<BsonDocument>();
            clone.AddOption("$explain", true);
            clone.limit = -clone.limit; // TODO: should this be -1?
            var explanation = clone.FirstOrDefault();
            if (!verbose) {
                explanation.RemoveElement("allPlans");
                explanation.RemoveElement("oldPlan");
                if (explanation.ContainsElement("shards")) {
                    var shards = explanation["shards"];
                    if (shards.BsonType == BsonType.Array) {
                        foreach (BsonDocument shard in shards.AsBsonArray) {
                            shard.RemoveElement("allPlans");
                            shard.RemoveElement("oldPlan");
                        }
                    } else {
                        var shard = shards.AsBsonDocument;
                        shard.RemoveElement("allPlans");
                        shard.RemoveElement("oldPlan");
                    }
                }
            }
            return explanation;
        }

        public MongoCursor<T> Fields(
            BsonDocument fields
        ) {
            if (frozen) { ThrowFrozen(); }
            this.fields = fields;
            return this;
        }

        public MongoCursor<T> Flags(
            QueryFlags flags
        ) {
            if (frozen) { ThrowFrozen(); }
            this.flags = flags;
            return this;
        }

        public IEnumerator<T> GetEnumerator() {
            frozen = true;
            return new MongoCursorEnumerator(this);
        }

        public MongoCursor<T> Hint(
            BsonDocument hint
        ) {
            if (frozen) { ThrowFrozen(); }
            AddOption("$hint", hint);
            return this;
        }

        public MongoCursor<T> Limit(
            int limit
        ) {
            if (frozen) { ThrowFrozen(); }
            this.limit = limit;
            return this;
        }

        public MongoCursor<T> Max(
           BsonDocument max
       ) {
            if (frozen) { ThrowFrozen(); }
            AddOption("$max", max);
            return this;
        }

        public MongoCursor<T> MaxScan(
            int maxScan
        ) {
            if (frozen) { ThrowFrozen(); }
            AddOption("$maxscan", maxScan);
            return this;
        }

        public MongoCursor<T> Min(
           BsonDocument min
       ) {
            if (frozen) { ThrowFrozen(); }
            AddOption("$min", min);
            return this;
        }

        public MongoCursor<T> Options(
            BsonDocument options
        ) {
            if (frozen) { ThrowFrozen(); }
            this.options = options;
            return this;
        }

        public int Size() {
            var command = new BsonDocument {
                { "count", collection.Name },
                { "query", query ?? new BsonDocument() },
                { limit != 0, "limit", limit },
                { skip != 0, "skip", skip }
            };
            var result = collection.Database.RunCommand(command);
            return result["n"].ToInt32();
        }

        public MongoCursor<T> ShowDiskLoc() {
            if (frozen) { ThrowFrozen(); }
            AddOption("$showDiskLoc", true);
            return this;
        }

        public MongoCursor<T> Skip(
            int skip
        ) {
            if (frozen) { ThrowFrozen(); }
            if (skip < 0) { throw new ArgumentException("Skip cannot be negative"); }
            this.skip = skip;
            return this;
        }

        public MongoCursor<T> Snapshot() {
            if (frozen) { ThrowFrozen(); }
            AddOption("$snapshot", true);
            return this;
        }

        public MongoCursor<T> Sort(
            BsonDocument orderBy
        ) {
            if (frozen) { ThrowFrozen(); }
            AddOption("$orderby", orderBy);
            return this;
        }

        public MongoCursor<T> Sort(
            params string[] keys
        ) {
            if (frozen) { ThrowFrozen(); }
            var orderBy = new BsonDocument(keys.Select(k => new BsonElement(k, 1)));
            return Sort(orderBy);
        }

        public MongoCursor<T> Sort(
            string key,
            int direction
        ) {
            if (frozen) { ThrowFrozen(); }
            var orderBy = new BsonDocument(key, direction);
            return Sort(orderBy);
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
            throw new MongoException("A cursor cannot be modified after enumeration has begun");
        }
        #endregion

        #region nested classes
        private class MongoCursorEnumerator : IEnumerator<T> {
            #region private fields
            private bool disposed = false;
            private bool started = false;
            private bool done = false;
            private MongoCursor<T> cursor;
            private MongoConnection connection;
            private int count;
            private int positiveLimit;
            private MongoReplyMessage<T> reply;
            private int replyIndex;
            private long openCursorId;
            #endregion

            #region constructors
            public MongoCursorEnumerator(
                MongoCursor<T> cursor
            ) {
                this.cursor = cursor;
                this.positiveLimit = cursor.limit >= 0 ? cursor.limit : -cursor.limit;
            }
            #endregion

            #region public properties
            public T Current {
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
            private MongoReplyMessage<T> GetFirst() {
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

                    var message = new MongoQueryMessage(cursor.Collection.FullName, cursor.flags, cursor.skip, numberToReturn, WrapQuery(), cursor.fields);
                    return SendMessage(message);
                } catch {
                    try { ReleaseConnection(); } catch { } // ignore exceptions
                    throw;
                }
            }

            private MongoReplyMessage<T> GetMore() {
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

                    var message = new MongoGetMoreMessage(cursor.Collection.FullName, numberToReturn, openCursorId);
                    return SendMessage(message);
                } catch {
                    try { ReleaseConnection(); } catch { } // ignore exceptions
                    throw;
                }
            }

            private MongoReplyMessage<T> SendMessage(
                MongoRequestMessage message
            ) {
                connection.SendMessage(message, SafeMode.False); // safemode doesn't apply to queries
                var reply = connection.ReceiveMessage<T>();
                openCursorId = reply.CursorId;
                if (openCursorId == 0) {
                    ReleaseConnection();
                }
                if ((reply.ResponseFlags & ResponseFlags.QueryFailure) != 0) {
                    throw new MongoException("Query failure");
                }

                return reply;
            }

            private void ReleaseConnection() {
                if (connection != null) {
                    try {
                        if (openCursorId != 0) {
                            var message = new MongoKillCursorsMessage(openCursorId);
                            connection.SendMessage(message, SafeMode.False); // no need to use SafeMode for KillCursors
                        }
                        cursor.Collection.Database.ReleaseConnection(connection);
                    } finally {
                        connection = null;
                        openCursorId = 0;
                    }
                }
            }

            private BsonDocument WrapQuery() {
                if (cursor.options == null) {
                    return cursor.query;
                } else {
                    var wrappedQuery = new BsonDocument("$query", cursor.query ?? new BsonDocument());
                    wrappedQuery.Merge(cursor.options);
                    return wrappedQuery;
                }
            }
            #endregion
        }
        #endregion
    }
}
