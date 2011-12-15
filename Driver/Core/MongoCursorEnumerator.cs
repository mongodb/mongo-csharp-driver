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
using System.Net;
using System.Text;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver
{
    /// <summary>
    /// Reprsents an enumerator that fetches the results of a query sent to the server.
    /// </summary>
    /// <typeparam name="TDocument">The type of the documents returned.</typeparam>
    public class MongoCursorEnumerator<TDocument> : IEnumerator<TDocument>
    {
        // private fields
        private bool disposed = false;
        private bool started = false;
        private bool done = false;
        private MongoCursor<TDocument> cursor;
        private MongoServerInstance serverInstance; // set when first request is sent to server instance
        private int count;
        private int positiveLimit;
        private MongoReplyMessage<TDocument> reply;
        private int replyIndex;
        private ResponseFlags responseFlags;
        private long openCursorId;

        // constructors
        /// <summary>
        /// Initializes a new instance of the MongoCursorEnumerator class.
        /// </summary>
        /// <param name="cursor">The cursor to be enumerated.</param>
        public MongoCursorEnumerator(MongoCursor<TDocument> cursor)
        {
            this.cursor = cursor;
            this.positiveLimit = cursor.Limit >= 0 ? cursor.Limit : -cursor.Limit;
        }

        // public properties
        /// <summary>
        /// Gets the current document.
        /// </summary>
        public TDocument Current
        {
            get
            {
                if (disposed) { throw new ObjectDisposedException("MongoCursorEnumerator"); }
                if (!started)
                {
                    throw new InvalidOperationException("Current is not valid until MoveNext has been called.");
                }
                if (done)
                {
                    throw new InvalidOperationException("Current is not valid after MoveNext has returned false.");
                }
                return reply.Documents[replyIndex];
            }
        }

        /// <summary>
        /// Gets whether the cursor is dead (used with tailable cursors).
        /// </summary>
        public bool IsDead
        {
            get { return openCursorId == 0; }
        }

        /// <summary>
        /// Gets whether the server is await capable (used with tailable cursors).
        /// </summary>
        public bool IsServerAwaitCapable
        {
            get { return (responseFlags & ResponseFlags.AwaitCapable) != 0; }
        }

        // public methods
        /// <summary>
        /// Disposes of any resources held by this enumerator.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                try
                {
                    KillCursor();
                }
                finally
                {
                    disposed = true;
                }
            }
        }

        /// <summary>
        /// Moves to the next result and returns true if another result is available.
        /// </summary>
        /// <returns>True if another result is available.</returns>
        public bool MoveNext()
        {
            if (disposed) { throw new ObjectDisposedException("MongoCursorEnumerator"); }
            if (done)
            {
                // normally once MoveNext returns false the enumerator is done and MoveNext will return false forever after that
                // but for a tailable cursor MoveNext can return false for awhile and eventually return true again once new data arrives
                // so a tailable cursor is never really done (at least while there is still an open cursor)
                if ((cursor.Flags & QueryFlags.TailableCursor) != 0 && openCursorId != 0)
                {
                    done = false;
                }
                else
                {
                    return false;
                }
            }

            if (!started)
            {
                reply = GetFirst();
                if (reply.Documents.Count == 0)
                {
                    reply = null;
                    done = true;
                    return false;
                }
                replyIndex = -1;
                started = true;
            }

            if (positiveLimit != 0 && count == positiveLimit)
            {
                KillCursor(); // early exit
                reply = null;
                done = true;
                return false;
            }

            // reply would only be null if the cursor is tailable and temporarily ran out of documents
            if (reply != null && replyIndex < reply.Documents.Count - 1)
            {
                replyIndex++; // move to next document in the current reply
            }
            else
            {
                if (openCursorId != 0)
                {
                    reply = GetMore();
                    if (reply.Documents.Count == 0)
                    {
                        reply = null;
                        done = true;
                        return false;
                    }
                    replyIndex = 0;
                }
                else
                {
                    reply = null;
                    done = true;
                    return false;
                }
            }

            count++;
            return true;
        }

        /// <summary>
        /// Resets the enumerator (not supported by MongoCursorEnumerator).
        /// </summary>
        public void Reset()
        {
            throw new NotSupportedException();
        }

        // explicit interface implementations
        object IEnumerator.Current
        {
            get { return Current; }
        }

        // private methods
        private MongoConnection AcquireConnection()
        {
            if (serverInstance == null)
            {
                // first time we need a connection let Server.AcquireConnection pick the server instance
                var connection = cursor.Server.AcquireConnection(cursor.Database, cursor.SlaveOk);
                serverInstance = connection.ServerInstance;
                return connection;
            }
            else
            {
                // all subsequent requests for the same cursor must go to the same server instance
                return cursor.Server.AcquireConnection(cursor.Database, serverInstance);
            }
        }

        private MongoReplyMessage<TDocument> GetFirst()
        {
            var connection = AcquireConnection();
            try
            {
                // some of these weird conditions are necessary to get commands to run correctly
                // specifically numberToReturn has to be 1 or -1 for commands
                int numberToReturn;
                if (cursor.Limit < 0)
                {
                    numberToReturn = cursor.Limit;
                }
                else if (cursor.Limit == 0)
                {
                    numberToReturn = cursor.BatchSize;
                }
                else if (cursor.BatchSize == 0)
                {
                    numberToReturn = cursor.Limit;
                }
                else if (cursor.Limit < cursor.BatchSize)
                {
                    numberToReturn = cursor.Limit;
                }
                else
                {
                    numberToReturn = cursor.BatchSize;
                }

                var writerSettings = cursor.Collection.GetWriterSettings(connection);
                using (var message = new MongoQueryMessage(writerSettings, cursor.Collection.FullName, cursor.Flags, cursor.Skip, numberToReturn, WrapQuery(), cursor.Fields))
                {
                    return GetReply(connection, message);
                }
            }
            finally
            {
                cursor.Server.ReleaseConnection(connection);
            }
        }

        private MongoReplyMessage<TDocument> GetMore()
        {
            var connection = AcquireConnection();
            try
            {
                int numberToReturn;
                if (positiveLimit != 0)
                {
                    numberToReturn = positiveLimit - count;
                    if (cursor.BatchSize != 0 && numberToReturn > cursor.BatchSize)
                    {
                        numberToReturn = cursor.BatchSize;
                    }
                }
                else
                {
                    numberToReturn = cursor.BatchSize;
                }

                using (var message = new MongoGetMoreMessage(cursor.Collection.FullName, numberToReturn, openCursorId))
                {
                    return GetReply(connection, message);
                }
            }
            finally
            {
                cursor.Server.ReleaseConnection(connection);
            }
        }

        private MongoReplyMessage<TDocument> GetReply(MongoConnection connection, MongoRequestMessage message)
        {
            var readerSettings = cursor.Collection.GetReaderSettings(connection);
            connection.SendMessage(message, SafeMode.False); // safemode doesn't apply to queries
            var reply = connection.ReceiveMessage<TDocument>(readerSettings, cursor.SerializationOptions);
            responseFlags = reply.ResponseFlags;
            openCursorId = reply.CursorId;
            return reply;
        }

        private void KillCursor()
        {
            if (openCursorId != 0)
            {
                try
                {
                    if (serverInstance != null && serverInstance.State == MongoServerState.Connected)
                    {
                        var connection = cursor.Server.AcquireConnection(cursor.Database, serverInstance);
                        try
                        {
                            using (var message = new MongoKillCursorsMessage(openCursorId))
                            {
                                connection.SendMessage(message, SafeMode.False); // no need to use SafeMode for KillCursors
                            }
                        }
                        finally
                        {
                            cursor.Server.ReleaseConnection(connection);
                        }
                    }
                }
                finally
                {
                    openCursorId = 0;
                }
            }
        }

        private IMongoQuery WrapQuery()
        {
            if (cursor.Options == null)
            {
                return cursor.Query;
            }
            else
            {
                var query = (cursor.Query == null) ? (BsonValue)new BsonDocument() : BsonDocumentWrapper.Create(cursor.Query);
                var wrappedQuery = new QueryDocument("$query", query);
                wrappedQuery.Merge(cursor.Options);
                return wrappedQuery;
            }
        }
    }
}
