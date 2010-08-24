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
        private BsonDocument fields;
        private BsonDocument orderBy;
        private bool snapshot;
        private QueryFlags flags;
        private int skip;
        private int limit; // number of documents to return (enforced by cursor)
        private int batchSize; // number of documents to return in each reply
        private bool explain;
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
        public MongoCursor<T> Batch(
            int batchSize
        ) {
            if (disposed) { throw new ObjectDisposedException("MongoCursor"); }
            this.batchSize = batchSize;
            return this;
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

        // TODO: verbose argument?
        public BsonDocument Explain() {
            explain = true;
            throw new NotImplementedException();
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
            var server = collection.Database.Server;
            var connection = MongoConnectionPool.AcquireConnection(server.Host, server.Port);

            MongoReplyMessage<T> reply = null;
            int count = 0;
            int limit = (this.limit > 0) ? this.limit : -this.limit;
            do {
                try {
                    if (reply == null) {
                        reply = ExecuteQuery(connection);
                    } else {
                        reply = GetMore(connection, reply.CursorID);
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
            } while ((count != limit) && reply.CursorID > 0);

            MongoConnectionPool.ReleaseConnection(connection);
        }

        public MongoCursor<T> Hint(
            BsonDocument hint
        ) {
            throw new NotImplementedException();
        }

        public MongoCursor<T> Limit(
            int limit
        ) {
            if (disposed) { throw new ObjectDisposedException("MongoCursor"); }
            this.limit = limit;
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
            this.snapshot = true;
            return this;
        }

        public MongoCursor<T> Sort(
            BsonDocument orderBy
        ) {
            if (disposed) { throw new ObjectDisposedException("MongoCursor"); }
            this.orderBy = orderBy;
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
            orderBy = new BsonDocument {
                { key, direction }
            };
            return this;
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

            var message = new MongoQueryMessage(collection, skip, numberToReturn, query, fields);
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
            long cursorID
        ) {
            var message = new MongoGetMoreMessage(collection, batchSize, cursorID);
            connection.SendMessage(message);
            var reply = connection.ReceiveMessage<T>();
            if ((reply.ResponseFlags & ResponseFlags.QueryFailure) != 0) {
                throw new MongoException("Query failure");
            }
            return reply;
        }
        #endregion
    }
}
