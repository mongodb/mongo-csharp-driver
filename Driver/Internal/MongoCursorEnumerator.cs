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

namespace MongoDB.Driver.Internal {
    internal class MongoCursorEnumerator<TDocument> : IEnumerator<TDocument> {
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
            this.positiveLimit = cursor.Limit >= 0 ? cursor.Limit : -cursor.Limit;
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
            connection = cursor.Server.AcquireConnection(cursor.Database, cursor.SlaveOk);
            try {
                // some of these weird conditions are necessary to get commands to run correctly
                // specifically numberToReturn has to be 1 or -1 for commands
                int numberToReturn;
                if (cursor.Limit < 0) {
                    numberToReturn = cursor.Limit;
                } else if (cursor.Limit == 0) {
                    numberToReturn = cursor.BatchSize;
                } else if (cursor.BatchSize == 0) {
                    numberToReturn = cursor.Limit;
                } else if (cursor.Limit < cursor.BatchSize) {
                    numberToReturn = cursor.Limit;
                } else {
                    numberToReturn = cursor.BatchSize;
                }

                using (
                    var message = new MongoQueryMessage(
                        cursor.Server,
                        cursor.Collection.FullName,
                        cursor.Flags,
                        cursor.Skip,
                        numberToReturn,
                        WrapQuery(),
                        cursor.Fields
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
                    if (cursor.BatchSize != 0 && numberToReturn > cursor.BatchSize) {
                        numberToReturn = cursor.BatchSize;
                    }
                } else {
                    numberToReturn = cursor.BatchSize;
                }

                using (
                    var message = new MongoGetMoreMessage(
                        cursor.Server,
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
            var reply = connection.ReceiveMessage<TDocument>(cursor.Server);
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
                        using (var message = new MongoKillCursorsMessage(cursor.Server, openCursorId)) {
                            connection.SendMessage(message, SafeMode.False); // no need to use SafeMode for KillCursors
                        }
                    }
                    cursor.Server.ReleaseConnection(connection);
                } finally {
                    connection = null;
                    openCursorId = 0;
                }
            }
        }

        private IMongoQuery WrapQuery() {
            if (cursor.Options == null) {
                return cursor.Query;
            } else {
                var query = (cursor.Query == null) ? (BsonValue) new BsonDocument() : BsonDocumentWrapper.Create(cursor.Query);
                var wrappedQuery = new QueryDocument("$query", query);
                wrappedQuery.Merge(cursor.Options);
                return wrappedQuery;
            }
        }
        #endregion
    }
}
