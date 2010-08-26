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
    public class MongoCursor<T> : IDisposable, IEnumerable<T> where T : new() {
        #region private fields
        private bool disposed = false;
        private MongoCollection collection;
        private BsonDocument query;
        private BsonDocument options;
        private BsonDocument fields;
        private QueryFlags flags;
        private int skip;
        private int limit; // number of documents to return (enforced by cursor)
        private int batchSize; // number of documents to return in each reply
        private bool frozen; // TODO: freeze cursor once execution begins
        #endregion

        #region constructors
        public MongoCursor(
            MongoCollection collection,
            BsonDocument query
        ) {
            this.collection = collection;
            this.query = query;
        }

        public MongoCursor(
            MongoCollection collection,
            BsonDocument query,
            BsonDocument fields
        ) {
            this.collection = collection;
            this.query = query;
            this.fields = fields;
        }
        #endregion

        #region public properties
        public MongoCollection Collection {
            get { return collection; }
        }

        //public IEnumerable<T> Documents {
        //    get { return GetEnumerator(); }
        //}
        #endregion

        #region public methods
        public MongoCursor<T> AddOption(
            string name,
            object value
        ) {
            if (disposed) { throw new ObjectDisposedException("MongoCursor"); }
            if (options == null) { options = new BsonDocument(); }
            options[name] = value;
            return this;
        }

        public MongoCursor<T> Batch(
            int batchSize
        ) {
            if (disposed) { throw new ObjectDisposedException("MongoCursor"); }
            this.batchSize = batchSize;
            return this;
        }

        public MongoCursor<TNew> Clone<TNew>() where TNew : new() {
            var clone = new MongoCursor<TNew>(collection, query, fields);
            if (options != null) {
                foreach (var option in options) {
                    clone.AddOption(option.Name, option.Value);
                }
            }
            clone.flags = flags;
            clone.skip = skip;
            clone.limit = limit;
            clone.batchSize = batchSize;
            return clone;
        }

        public int Count() {
            if (disposed) { throw new ObjectDisposedException("MongoCursor"); }
            var command = new BsonDocument {
                { "count", collection.Name },
                { "query", query ?? new BsonDocument() },
            };
            var result = collection.Database.RunCommand(command);
            return (int) result.GetDouble("n");
        }

        public void Dispose() {
            if (!disposed) {
                // TODO: implement Dispose
                disposed = true;
            }
        }

        // not a property because it requires at least one round trip to the server
        public IEnumerable<T> Documents() {
            throw new NotImplementedException();
        }

        public BsonDocument Explain() {
            return Explain(false);
        }

        public BsonDocument Explain(
            bool verbose
        ) {
            using (var clone = this.Clone<BsonDocument>()) {
                clone.AddOption("$explain", true);
                clone.limit = -clone.limit;
                var enumerator = clone.GetEnumerator();
                enumerator.MoveNext();
                var explanation = enumerator.Current;
                if (!verbose) {
                    explanation.RemoveElement("allPlans");
                    explanation.RemoveElement("oldPlan");
                    if (explanation.ContainsElement("shards")) {
                        var shards = explanation["shards"];
                        if (shards is BsonArray) {
                            foreach (BsonDocument shard in ((BsonArray) shards).Values) {
                                shard.RemoveElement("allPlans");
                                shard.RemoveElement("oldPlan");
                            }
                        } else {
                            BsonDocument shard = (BsonDocument) shards;
                            shard.RemoveElement("allPlans");
                            shard.RemoveElement("oldPlan");
                        }
                    }
                }
                return explanation;
            }
        }

        public MongoCursor<T> Fields(
            BsonDocument fields
        ) {
            if (disposed) { throw new ObjectDisposedException("MongoCursor"); }
            this.fields = fields;
            return this;
        }

        public MongoCursor<T> Flags(
            QueryFlags flags
        ) {
            if (disposed) { throw new ObjectDisposedException("MongoCursor"); }
            this.flags = flags;
            return this;
        }

        public IEnumerator<T> GetEnumerator() {
            if (disposed) { throw new ObjectDisposedException("MongoCursor"); }

            // hold connection until all documents have been enumerated
            // TODO: what if enumeration is abandoned before reaching the end?
            var connection = collection.Database.GetConnection();

            MongoReplyMessage<T> reply = null;
            int count = 0;
            int limit = (this.limit > 0) ? this.limit : -this.limit;
            do {
                try {
                    if (reply == null) {
                        reply = ExecuteQuery(connection);
                    } else {
                        reply = GetMore(connection, reply.CursorId);
                    }
                } catch {
                    try { connection.Dispose(); } catch { } // ignore exceptions
                    throw;
                }
                foreach (var document in reply.Documents) {
                    yield return document;
                    count++;
                    if (count == limit) {
                        break;
                    }
                }
            } while ((count != limit) && reply.CursorId > 0);

            collection.Database.ReleaseConnection(connection);
        }

        public MongoCursor<T> Hint(
            BsonDocument hint
        ) {
            AddOption("$hint", hint);
            return this;
        }

        public MongoCursor<T> Limit(
            int limit
        ) {
            if (disposed) { throw new ObjectDisposedException("MongoCursor"); }
            this.limit = limit;
            return this;
        }

        public MongoCursor<T> Max(
           BsonDocument max
       ) {
            AddOption("$max", max);
            return this;
        }

        public MongoCursor<T> MaxScan(
            int maxScan
        ) {
            AddOption("$maxscan", maxScan);
            return this;
        }

        public MongoCursor<T> Min(
           BsonDocument min
       ) {
            AddOption("$min", min);
            return this;
        }

        public int Size() {
            if (disposed) { throw new ObjectDisposedException("MongoCursor"); }
            var command = new BsonDocument {
                { "count", collection.Name },
                { "query", query ?? new BsonDocument() },
                { limit != 0, "limit", limit },
                { skip != 0, "skip", skip }
            };
            var result = collection.Database.RunCommand(command);
            return (int) result.GetDouble("n");
        }

        public MongoCursor<T> ShowDiskLoc() {
            throw new NotImplementedException();
        }

        public MongoCursor<T> Skip(
            int skip
        ) {
            if (disposed) { throw new ObjectDisposedException("MongoCursor"); }
            this.skip = skip;
            return this;
        }

        public MongoCursor<T> Snapshot() {
            if (disposed) { throw new ObjectDisposedException("MongoCursor"); }
            AddOption("$snapshot", true);
            return this;
        }

        public MongoCursor<T> Sort(
            BsonDocument orderBy
        ) {
            if (disposed) { throw new ObjectDisposedException("MongoCursor"); }
            AddOption("$orderby", orderBy);
            return this;
        }

        public MongoCursor<T> Sort(
            string key
        ) {
            if (disposed) { throw new ObjectDisposedException("MongoCursor"); }
            return Sort(key, 1);
        }

        public MongoCursor<T> Sort(
            string key,
            int direction
        ) {
            if (disposed) { throw new ObjectDisposedException("MongoCursor"); }
            var orderBy = new BsonDocument(key, direction);
            return Sort(orderBy);
        }
        #endregion

        #region private methods
        private MongoReplyMessage<T> ExecuteQuery(
            MongoConnection connection
        ) {
            int numberToReturn;
            if (batchSize == 0) {
                numberToReturn = limit;
            } else if (limit == 0) {
                numberToReturn = batchSize;
            } else if (batchSize < limit) {
                numberToReturn = batchSize;
            } else {
                numberToReturn = limit;
            }

            var message = new MongoQueryMessage(collection, flags, skip, numberToReturn, WrapQuery(), fields);
            connection.SendMessage(message);
            var reply = connection.ReceiveMessage<T>();
            if ((reply.ResponseFlags & ResponseFlags.QueryFailure) != 0) {
                throw new MongoException("Query failure");
            }
            return reply;
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        private MongoReplyMessage<T> GetMore(
            MongoConnection connection,
            long cursorId
        ) {
            var message = new MongoGetMoreMessage(collection, batchSize, cursorId);
            connection.SendMessage(message);
            var reply = connection.ReceiveMessage<T>();
            if ((reply.ResponseFlags & ResponseFlags.QueryFailure) != 0) {
                throw new MongoException("Query failure");
            }
            return reply;
        }

        private BsonDocument WrapQuery() {
            if (options == null) {
                return query;
            }

            var wrappedQuery = new BsonDocument {
                { "$query", query ?? new BsonDocument() },
                { options.Elements }
            };
            return wrappedQuery;
        }
        #endregion
    }
}
