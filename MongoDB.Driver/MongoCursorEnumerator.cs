/* Copyright 2010-2013 10gen Inc.
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
using MongoDB.Bson;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a container for the CanCommandBeSentToSecondary delegate.
    /// </summary>
    public static class CanCommandBeSentToSecondary
    {
        // private static fields
        private static Func<BsonDocument, bool> __delegate = DefaultImplementation;
        private static HashSet<string> __secondaryOkCommands = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
        {
            "group",
            "aggregate",
            "collStats",
            "dbStats",
            "count",
            "distinct",
            "geoNear",
            "geoSearch",
            "geoWalk"
        };

        // public static properties
        /// <summary>
        /// Gets or sets the CanCommandBeSentToSecondary delegate.
        /// </summary>
        public static Func<BsonDocument, bool> Delegate
        {
            get { return __delegate; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                __delegate = value;
            }
        }

        // public static methods
        /// <summary>
        /// Default implementation of the CanCommandBeSentToSecondary delegate.
        /// </summary>
        /// <param name="document">The command.</param>
        /// <returns>True if the command can be sent to a secondary member of a replica set.</returns>
        public static bool DefaultImplementation(BsonDocument document)
        {
            if (document.ElementCount == 0)
            {
                return false;
            }

            var commandName = document.GetElement(0).Name;

            if (__secondaryOkCommands.Contains(commandName))
            {
                return true;
            }

            if (commandName.Equals("mapreduce", StringComparison.InvariantCultureIgnoreCase))
            {
                BsonValue outValue;
                if (document.TryGetValue("out", out outValue) && outValue.IsBsonDocument)
                {
                    return outValue.AsBsonDocument.Contains("inline");
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Reprsents an enumerator that fetches the results of a query sent to the server.
    /// </summary>
    /// <typeparam name="TDocument">The type of the documents returned.</typeparam>
    public class MongoCursorEnumerator<TDocument> : IEnumerator<TDocument>
    {
        // private fields
        private readonly MongoCursor<TDocument> _cursor;
        private readonly QueryFlags _queryFlags;
        private readonly ReadPreference _readPreference;

        private bool _disposed = false;
        private bool _started = false;
        private bool _done = false;
        private MongoServerInstance _serverInstance; // set when first request is sent to server instance
        private int _count;
        private int _positiveLimit;
        private MongoReplyMessage<TDocument> _reply;
        private int _replyIndex;
        private ResponseFlags _responseFlags;
        private long _openCursorId;
        
        // constructors
        /// <summary>
        /// Initializes a new instance of the MongoCursorEnumerator class.
        /// </summary>
        /// <param name="cursor">The cursor to be enumerated.</param>
        public MongoCursorEnumerator(MongoCursor<TDocument> cursor)
        {
            _cursor = cursor;
            _positiveLimit = cursor.Limit >= 0 ? cursor.Limit : -cursor.Limit;
            _readPreference = _cursor.ReadPreference;
            _queryFlags = _cursor.Flags;

            if (_readPreference.ReadPreferenceMode != ReadPreferenceMode.Primary && _cursor.Collection.Name == "$cmd")
            {
                var queryDocument = _cursor.Query.ToBsonDocument();
                var isSecondaryOk = CanCommandBeSentToSecondary.Delegate(queryDocument);
                if (!isSecondaryOk)
                {
                    // if the command can't be sent to a secondary, then we use primary here
                    // regardless of the user's choice.
                    _readPreference = ReadPreference.Primary;
                    // remove the slaveOk bit from the flags
                    _queryFlags &= ~QueryFlags.SlaveOk;
                }
            }
        }

        // public properties
        /// <summary>
        /// Gets the current document.
        /// </summary>
        public TDocument Current
        {
            get
            {
                if (_disposed) { throw new ObjectDisposedException("MongoCursorEnumerator"); }
                if (!_started)
                {
                    throw new InvalidOperationException("Current is not valid until MoveNext has been called.");
                }
                if (_done)
                {
                    throw new InvalidOperationException("Current is not valid after MoveNext has returned false.");
                }
                return _reply.Documents[_replyIndex];
            }
        }

        /// <summary>
        /// Gets whether the cursor is dead (used with tailable cursors).
        /// </summary>
        public bool IsDead
        {
            get { return _openCursorId == 0; }
        }

        /// <summary>
        /// Gets whether the server is await capable (used with tailable cursors).
        /// </summary>
        public bool IsServerAwaitCapable
        {
            get { return (_responseFlags & ResponseFlags.AwaitCapable) != 0; }
        }

        // public methods
        /// <summary>
        /// Disposes of any resources held by this enumerator.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    KillCursor();
                }
                finally
                {
                    _disposed = true;
                }
            }
        }

        /// <summary>
        /// Moves to the next result and returns true if another result is available.
        /// </summary>
        /// <returns>True if another result is available.</returns>
        public bool MoveNext()
        {
            if (_disposed) { throw new ObjectDisposedException("MongoCursorEnumerator"); }
            if (_done)
            {
                // normally once MoveNext returns false the enumerator is done and MoveNext will return false forever after that
                // but for a tailable cursor MoveNext can return false for awhile and eventually return true again once new data arrives
                // so a tailable cursor is never really done (at least while there is still an open cursor)
                if ((_cursor.Flags & QueryFlags.TailableCursor) != 0 && _openCursorId != 0)
                {
                    _done = false;
                }
                else
                {
                    return false;
                }
            }

            if (!_started)
            {
                _reply = GetFirst();
                if (_reply.Documents.Count == 0)
                {
                    _reply = null;
                    _done = true;
                    return false;
                }
                _replyIndex = -1;
                _started = true;
            }

            if (_positiveLimit != 0 && _count == _positiveLimit)
            {
                KillCursor(); // early exit
                _reply = null;
                _done = true;
                return false;
            }

            // reply would only be null if the cursor is tailable and temporarily ran out of documents
            if (_reply != null && _replyIndex < _reply.Documents.Count - 1)
            {
                _replyIndex++; // move to next document in the current reply
            }
            else
            {
                if (_openCursorId != 0)
                {
                    _reply = GetMore();
                    if (_reply.Documents.Count == 0)
                    {
                        _reply = null;
                        _done = true;
                        return false;
                    }
                    _replyIndex = 0;
                }
                else
                {
                    _reply = null;
                    _done = true;
                    return false;
                }
            }

            _count++;
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
            if (_serverInstance == null)
            {
                // first time we need a connection let Server.AcquireConnection pick the server instance
                var connection = _cursor.Server.AcquireConnection(_readPreference);
                _serverInstance = connection.ServerInstance;
                return connection;
            }
            else
            {
                // all subsequent requests for the same cursor must go to the same server instance
                return _cursor.Server.AcquireConnection(_serverInstance);
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
                if (_cursor.Limit < 0)
                {
                    numberToReturn = _cursor.Limit;
                }
                else if (_cursor.Limit == 0)
                {
                    numberToReturn = _cursor.BatchSize;
                }
                else if (_cursor.BatchSize == 0)
                {
                    numberToReturn = _cursor.Limit;
                }
                else if (_cursor.Limit < _cursor.BatchSize)
                {
                    numberToReturn = _cursor.Limit;
                }
                else
                {
                    numberToReturn = _cursor.BatchSize;
                }

                var writerSettings = _cursor.Collection.GetWriterSettings(connection);
                using (var message = new MongoQueryMessage(writerSettings, _cursor.Collection.FullName, _queryFlags, _cursor.Skip, numberToReturn, WrapQuery(), _cursor.Fields))
                {
                    return GetReply(connection, message);
                }
            }
            finally
            {
                _cursor.Server.ReleaseConnection(connection);
            }
        }

        private MongoReplyMessage<TDocument> GetMore()
        {
            var connection = AcquireConnection();
            try
            {
                int numberToReturn;
                if (_positiveLimit != 0)
                {
                    numberToReturn = _positiveLimit - _count;
                    if (_cursor.BatchSize != 0 && numberToReturn > _cursor.BatchSize)
                    {
                        numberToReturn = _cursor.BatchSize;
                    }
                }
                else
                {
                    numberToReturn = _cursor.BatchSize;
                }

                using (var message = new MongoGetMoreMessage(_cursor.Collection.FullName, numberToReturn, _openCursorId))
                {
                    return GetReply(connection, message);
                }
            }
            finally
            {
                _cursor.Server.ReleaseConnection(connection);
            }
        }

        private MongoReplyMessage<TDocument> GetReply(MongoConnection connection, MongoRequestMessage message)
        {
            var readerSettings = _cursor.Collection.GetReaderSettings(connection);
            connection.SendMessage(message, null, _cursor.Database.Name); // write concern doesn't apply to queries
            var reply = connection.ReceiveMessage<TDocument>(readerSettings, _cursor.SerializationOptions);
            _responseFlags = reply.ResponseFlags;
            _openCursorId = reply.CursorId;
            return reply;
        }

        private void KillCursor()
        {
            if (_openCursorId != 0)
            {
                try
                {
                    if (_serverInstance != null && _serverInstance.State == MongoServerState.Connected)
                    {
                        var connection = _cursor.Server.AcquireConnection(_serverInstance);
                        try
                        {
                            using (var message = new MongoKillCursorsMessage(_openCursorId))
                            {
                                connection.SendMessage(message, WriteConcern.Unacknowledged, _cursor.Database.Name);
                            }
                        }
                        finally
                        {
                            _cursor.Server.ReleaseConnection(connection);
                        }
                    }
                }
                finally
                {
                    _openCursorId = 0;
                }
            }
        }

        private IMongoQuery WrapQuery()
        {
            BsonDocument formattedReadPreference = null;
            if (_serverInstance.InstanceType == MongoServerInstanceType.ShardRouter &&
                _readPreference.ReadPreferenceMode != ReadPreferenceMode.Primary)
            {
                BsonArray tagSetsArray = null;
                if (_readPreference.TagSets != null)
                {
                    tagSetsArray = new BsonArray();
                    foreach (var tagSet in _readPreference.TagSets)
                    {
                        var tagSetDocument = new BsonDocument();
                        foreach (var tag in tagSet)
                        {
                            tagSetDocument.Add(tag.Name, tag.Value);
                        }
                        tagSetsArray.Add(tagSetDocument);
                    }
                }

                if (tagSetsArray != null || _readPreference.ReadPreferenceMode != ReadPreferenceMode.SecondaryPreferred)
                {
                    formattedReadPreference = new BsonDocument
                    {
                        { "mode", MongoUtils.ToCamelCase(_readPreference.ReadPreferenceMode.ToString()) },
                        { "tags", tagSetsArray, tagSetsArray != null } // optional
                    };
                }
            }

            if (_cursor.Options == null && formattedReadPreference == null)
            {
                return _cursor.Query;
            }
            else
            {
                var query = (_cursor.Query == null) ? (BsonValue)new BsonDocument() : BsonDocumentWrapper.Create(_cursor.Query);
                var wrappedQuery = new QueryDocument
                {
                    { "$query", query },
                    { "$readPreference", formattedReadPreference, formattedReadPreference != null }, // only if sending query to a mongos
                };
                wrappedQuery.Merge(_cursor.Options);
                return wrappedQuery;
            }
        }
    }
}
