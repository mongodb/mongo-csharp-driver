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
        private int numberToSkip;
        private int numberToReturn;
        private BsonDocument query;
        private BsonDocument fieldSelector;
        #endregion

        #region constructors
        public MongoCursor(
            MongoCollection collection,
            BsonDocument query
        ) {
            this.collection = collection;
            this.query = query;
        }
        #endregion

        #region public properties
        //public IEnumerable<T> Documents {
        //    get { return GetEnumerator(); }
        //}
        #endregion

        #region public methods
        public IEnumerator<T> GetEnumerator() {
            // hold connection until all documents have been enumerated
            // TODO: what if enumeration is abandoned before reaching the end?
            var server = collection.Database.Server;
            var connection = MongoConnectionPool.AcquireConnection(server.Host, server.Port);

            MongoReplyMessage<T> reply = null;
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
                }
            } while (reply.CursorID > 0);

            MongoConnectionPool.ReleaseConnection(connection);
        }

        public MongoCursor<T> Limit(
            int numberToReturn
        ) {
            this.numberToReturn = numberToReturn;
            return this;
        }

        public MongoCursor<T> Skip(
            int numberToSkip
        ) {
            this.numberToSkip = numberToSkip;
            return this;
        }
        #endregion

        #region private methods
        private MongoReplyMessage<T> ExecuteQuery(
            MongoConnection connection
        ) {
            var message = new MongoQueryMessage(collection, numberToSkip, numberToReturn, query, fieldSelector);
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
            var message = new MongoGetMoreMessage(collection, numberToReturn, cursorID);
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
