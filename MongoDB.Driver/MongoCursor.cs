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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Internal;
using MongoDB.Driver.Operations;

namespace MongoDB.Driver
{
    /// <summary>
    /// An object that can be enumerated to fetch the results of a query. The query is not sent
    /// to the server until you begin enumerating the results.
    /// </summary>
    public abstract class MongoCursor : IEnumerable
    {
        // private fields
        private readonly MongoCollection _collection;
        private readonly MongoDatabase _database;
        private readonly IMongoQuery _query;
        private readonly IBsonSerializer _serializer;
        private readonly MongoServer _server;
        private IMongoFields _fields;
        private BsonDocument _options;
        private QueryFlags _flags;
        private ReadPreference _readPreference;
        private int _skip;
        private int _limit; // number of documents to return (enforced by cursor)
        private int _batchSize; // number of documents to return in each reply
        private IBsonSerializationOptions _serializationOptions;
        private bool _isFrozen; // prevent any further modifications once enumeration has begun

        // constructors
        /// <summary>
        /// Creates a new MongoCursor. It is very unlikely that you will call this constructor. Instead, see all the Find methods in MongoCollection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="query">The query.</param>
        /// <param name="readPreference">The read preference.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="serializationOptions">The serialization options.</param>
        protected MongoCursor(MongoCollection collection, IMongoQuery query, ReadPreference readPreference, IBsonSerializer serializer, IBsonSerializationOptions serializationOptions)
        {
            _collection = collection;
            _database = collection.Database;
            _server = collection.Database.Server;
            _query = query;
            _serializer = serializer;
            _serializationOptions = serializationOptions;
            _readPreference = readPreference;
        }

        // public properties
        /// <summary>
        /// Gets the server that the query will be sent to.
        /// </summary>
        public virtual MongoServer Server
        {
            get { return _server; }
        }

        /// <summary>
        /// Gets the database that constains the collection that is being queried.
        /// </summary>
        public virtual MongoDatabase Database
        {
            get { return _database; }
        }

        /// <summary>
        /// Gets the collection that is being queried.
        /// </summary>
        public virtual MongoCollection Collection
        {
            get { return _collection; }
        }

        /// <summary>
        /// Gets the query that will be sent to the server.
        /// </summary>
        public virtual IMongoQuery Query
        {
            get { return _query; }
        }

        /// <summary>
        /// Gets or sets the fields that will be returned from the server.
        /// </summary>
        public virtual IMongoFields Fields
        {
            get { return _fields; }
            set
            {
                if (_isFrozen) { ThrowFrozen(); }
                _fields = value;
            }
        }

        /// <summary>
        /// Gets or sets the cursor options. See also the individual Set{Option} methods, which are easier to use.
        /// </summary>
        public virtual BsonDocument Options
        {
            get { return _options; }
            set
            {
                if (_isFrozen) { ThrowFrozen(); }
                _options = value;
            }
        }

        /// <summary>
        /// Gets or sets the query flags.
        /// </summary>
        public virtual QueryFlags Flags
        {
            get
            {
                if (_readPreference.ReadPreferenceMode == ReadPreferenceMode.Primary)
                {
                    return _flags;
                }
                else
                {
                    return _flags | QueryFlags.SlaveOk;
                }
            }
            set
            {
                if (_isFrozen) { ThrowFrozen(); }
                _flags = value;
            }
        }

        /// <summary>
        /// Gets or sets the read preference.
        /// </summary>
        public virtual ReadPreference ReadPreference
        {
            get { return _readPreference; }
            set
            {
                if (_isFrozen) { ThrowFrozen(); }
                _readPreference = value;
            }
        }

        /// <summary>
        /// Gets or sets whether the query can be sent to a secondary server.
        /// </summary>
        [Obsolete("Use ReadPreference instead.")]
        public virtual bool SlaveOk
        {
            get
            {
                return _readPreference.ToSlaveOk();
            }
            set
            {
                if (_isFrozen) { ThrowFrozen(); }
                _readPreference = ReadPreference.FromSlaveOk(value);
            }
        }

        /// <summary>
        /// Gets or sets the number of documents the server should skip before returning the rest of the documents.
        /// </summary>
        public virtual int Skip
        {
            get { return _skip; }
            set
            {
                if (_isFrozen) { ThrowFrozen(); }
                _skip = value;
            }
        }

        /// <summary>
        /// Gets or sets the limit on the number of documents to be returned.
        /// </summary>
        public virtual int Limit
        {
            get { return _limit; }
            set
            {
                if (_isFrozen) { ThrowFrozen(); }
                _limit = value;
            }
        }

        /// <summary>
        /// Gets or sets the batch size (the number of documents returned per batch).
        /// </summary>
        public virtual int BatchSize
        {
            get { return _batchSize; }
            set
            {
                if (_isFrozen) { ThrowFrozen(); }
                _batchSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the serialization options (only needed in rare cases).
        /// </summary>
        public virtual IBsonSerializationOptions SerializationOptions
        {
            get { return _serializationOptions; }
            set
            {
                if (_isFrozen) { ThrowFrozen(); }
                _serializationOptions = value;
            }
        }

        /// <summary>
        /// Gets the serializer.
        /// </summary>
        public virtual IBsonSerializer Serializer
        {
            get { return _serializer; }
        }

        /// <summary>
        /// Gets whether the cursor has been frozen to prevent further changes.
        /// </summary>
        public virtual bool IsFrozen
        {
            get { return _isFrozen; }
            protected set { _isFrozen = value; }
        }

        // public static methods
        /// <summary>
        /// Creates a cursor.
        /// </summary>
        /// <param name="documentType">The type of the returned documents.</param>
        /// <param name="collection">The collection to query.</param>
        /// <param name="query">A query.</param>
        /// <param name="readPreference">The read preference.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="serializationOptions">The serialization options.</param>
        /// <returns>
        /// A cursor.
        /// </returns>
        public static MongoCursor Create(Type documentType, MongoCollection collection, IMongoQuery query, ReadPreference readPreference, IBsonSerializer serializer, IBsonSerializationOptions serializationOptions)
        {
            var cursorDefinition = typeof(MongoCursor<>);
            var cursorType = cursorDefinition.MakeGenericType(documentType);
            var constructorInfo = cursorType.GetConstructor(new Type[] { typeof(MongoCollection), typeof(IMongoQuery), typeof(ReadPreference), typeof(IBsonSerializer), typeof(IBsonSerializationOptions)});
            return (MongoCursor)constructorInfo.Invoke(new object[] { collection, query, readPreference, serializer, serializationOptions });
        }

        // public methods
        /// <summary>
        /// Creates a clone of the cursor.
        /// </summary>
        /// <typeparam name="TDocument">The type of the documents returned.</typeparam>
        /// <returns>A clone of the cursor.</returns>
        public virtual MongoCursor<TDocument> Clone<TDocument>()
        {
            return (MongoCursor<TDocument>)Clone(typeof(TDocument));
        }

        /// <summary>
        /// Creates a clone of the cursor.
        /// </summary>
        /// <typeparam name="TDocument">The type of the documents returned.</typeparam>
        /// <param name="serializer">The serializer to use.</param>
        /// <param name="serializationOptions">The serialization options.</param>
        /// <returns>
        /// A clone of the cursor.
        /// </returns>
        public virtual MongoCursor<TDocument> Clone<TDocument>(IBsonSerializer serializer, IBsonSerializationOptions serializationOptions)
        {
            return (MongoCursor<TDocument>)Clone(typeof(TDocument), serializer, serializationOptions);
        }

        /// <summary>
        /// Creates a clone of the cursor.
        /// </summary>
        /// <param name="documentType">The type of the documents returned.</param>
        /// <returns>A clone of the cursor.</returns>
        public virtual MongoCursor Clone(Type documentType)
        {
            var serializer = BsonSerializer.LookupSerializer(documentType);
            return Clone(documentType, serializer, null);
        }

        /// <summary>
        /// Creates a clone of the cursor.
        /// </summary>
        /// <param name="documentType">The type of the documents returned.</param>
        /// <param name="serializer">The serializer to use.</param>
        /// <param name="serializationOptions">The serialization options.</param>
        /// <returns>
        /// A clone of the cursor.
        /// </returns>
        public virtual MongoCursor Clone(Type documentType, IBsonSerializer serializer, IBsonSerializationOptions serializationOptions)
        {
            var clone = Create(documentType, _collection, _query, _readPreference, serializer, serializationOptions);
            clone._options = _options == null ? null : (BsonDocument)_options.Clone();
            clone._flags = _flags;
            clone._skip = _skip;
            clone._limit = _limit;
            clone._batchSize = _batchSize;
            clone._fields = _fields;
            clone._serializationOptions = _serializationOptions;
            return clone;
        }

        /// <summary>
        /// Returns the number of documents that match the query (ignores Skip and Limit, unlike Size which honors them).
        /// </summary>
        /// <returns>The number of documents that match the query.</returns>
        public virtual long Count()
        {
            _isFrozen = true;
            var command = new CommandDocument
            {
                { "count", _collection.Name },
                { "query", BsonDocumentWrapper.Create(_query), _query != null } // query is optional
            };
            var result = _database.RunCommand(command);
            return result.Response["n"].ToInt64();
        }

        /// <summary>
        /// Returns an explanation of how the query was executed (instead of the results).
        /// </summary>
        /// <returns>An explanation of thow the query was executed.</returns>
        public virtual BsonDocument Explain()
        {
            return Explain(false);
        }

        /// <summary>
        /// Returns an explanation of how the query was executed (instead of the results).
        /// </summary>
        /// <param name="verbose">Whether the explanation should contain more details.</param>
        /// <returns>An explanation of thow the query was executed.</returns>
        public virtual BsonDocument Explain(bool verbose)
        {
            _isFrozen = true;
            var clone = Clone<BsonDocument>(BsonDocumentSerializer.Instance, null);
            clone.SetOption("$explain", true);
            clone._limit = -clone._limit; // TODO: should this be -1?
            var explanation = clone.FirstOrDefault();
            if (!verbose)
            {
                explanation.Remove("allPlans");
                explanation.Remove("oldPlan");
                if (explanation.Contains("shards"))
                {
                    var shards = explanation["shards"];
                    if (shards.BsonType == BsonType.Array)
                    {
                        foreach (BsonDocument shard in shards.AsBsonArray)
                        {
                            shard.Remove("allPlans");
                            shard.Remove("oldPlan");
                        }
                    }
                    else
                    {
                        var shard = shards.AsBsonDocument;
                        shard.Remove("allPlans");
                        shard.Remove("oldPlan");
                    }
                }
            }
            return explanation;
        }

        /// <summary>
        /// Sets the batch size (the number of documents returned per batch).
        /// </summary>
        /// <param name="batchSize">The number of documents in each batch.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor SetBatchSize(int batchSize)
        {
            if (_isFrozen) { ThrowFrozen(); }
            if (batchSize < 0) { throw new ArgumentException("BatchSize cannot be negative."); }
            _batchSize = batchSize;
            return this;
        }

        /// <summary>
        /// Sets the fields that will be returned from the server.
        /// </summary>
        /// <param name="fields">The fields that will be returned from the server.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor SetFields(IMongoFields fields)
        {
            if (_isFrozen) { ThrowFrozen(); }
            _fields = fields;
            return this;
        }

        /// <summary>
        /// Sets the fields that will be returned from the server.
        /// </summary>
        /// <param name="fields">The fields that will be returned from the server.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor SetFields(params string[] fields)
        {
            if (_isFrozen) { ThrowFrozen(); }
            _fields = Builders.Fields.Include(fields);
            return this;
        }

        /// <summary>
        /// Sets the query flags.
        /// </summary>
        /// <param name="flags">The query flags.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor SetFlags(QueryFlags flags)
        {
            if (_isFrozen) { ThrowFrozen(); }
            _flags = flags;
            return this;
        }

        /// <summary>
        /// Sets the index hint for the query.
        /// </summary>
        /// <param name="hint">The index hint.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor SetHint(BsonDocument hint)
        {
            if (_isFrozen) { ThrowFrozen(); }
            SetOption("$hint", hint);
            return this;
        }

        /// <summary>
        /// Sets the index hint for the query.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor SetHint(string indexName)
        {
            if (_isFrozen) { ThrowFrozen(); }
            SetOption("$hint", indexName);
            return this;
        }

        /// <summary>
        /// Sets the limit on the number of documents to be returned.
        /// </summary>
        /// <param name="limit">The limit on the number of documents to be returned.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor SetLimit(int limit)
        {
            if (_isFrozen) { ThrowFrozen(); }
            _limit = limit;
            return this;
        }

        /// <summary>
        /// Sets the max value for the index key range of documents to return (note: the max value itself is excluded from the range).
        /// Often combined with SetHint (if SetHint is not used the server will attempt to determine the matching index automatically).
        /// </summary>
        /// <param name="max">The max value.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor SetMax(BsonDocument max)
        {
            if (_isFrozen) { ThrowFrozen(); }
            SetOption("$max", max);
            return this;
        }

        /// <summary>
        /// Sets the maximum number of documents to scan.
        /// </summary>
        /// <param name="maxScan">The maximum number of documents to scan.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor SetMaxScan(int maxScan)
        {
            if (_isFrozen) { ThrowFrozen(); }
            SetOption("$maxscan", maxScan);
            return this;
        }

        /// <summary>
        /// Sets the min value for the index key range of documents to return (note: the min value itself is included in the range).
        /// Often combined with SetHint (if SetHint is not used the server will attempt to determine the matching index automatically).
        /// </summary>
        /// <param name="min">The min value.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor SetMin(BsonDocument min)
        {
            if (_isFrozen) { ThrowFrozen(); }
            SetOption("$min", min);
            return this;
        }

        /// <summary>
        /// Sets a cursor option.
        /// </summary>
        /// <param name="name">The name of the option.</param>
        /// <param name="value">The value of the option.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor SetOption(string name, BsonValue value)
        {
            if (_isFrozen) { ThrowFrozen(); }
            if (_options == null) { _options = new BsonDocument(); }
            _options[name] = value;
            return this;
        }

        /// <summary>
        /// Sets multiple cursor options. See also the individual Set{Option} methods, which are easier to use.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor SetOptions(BsonDocument options)
        {
            if (_isFrozen) { ThrowFrozen(); }
            if (options != null)
            {
                if (_options == null) { _options = new BsonDocument(); }
                _options.Merge(options, true); // overwriteExistingElements
            }
            return this;
        }

        /// <summary>
        /// Sets the read preference.
        /// </summary>
        /// <param name="readPreference">The read preference.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor SetReadPreference(ReadPreference readPreference)
        {
            if (_isFrozen) { ThrowFrozen(); }
            _readPreference = readPreference;
            return this;
        }

        /// <summary>
        /// Sets the serialization options (only needed in rare cases).
        /// </summary>
        /// <param name="serializationOptions">The serialization options.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor SetSerializationOptions(IBsonSerializationOptions serializationOptions)
        {
            if (_isFrozen) { ThrowFrozen(); }
            _serializationOptions = serializationOptions;
            return this;
        }

        /// <summary>
        /// Sets the $showDiskLoc option.
        /// </summary>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor SetShowDiskLoc()
        {
            if (_isFrozen) { ThrowFrozen(); }
            SetOption("$showDiskLoc", true);
            return this;
        }

        /// <summary>
        /// Sets the number of documents the server should skip before returning the rest of the documents.
        /// </summary>
        /// <param name="skip">The number of documents to skip.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor SetSkip(int skip)
        {
            if (_isFrozen) { ThrowFrozen(); }
            if (skip < 0) { throw new ArgumentException("Skip cannot be negative."); }
            _skip = skip;
            return this;
        }

        /// <summary>
        /// Sets whether the query should be sent to a secondary server.
        /// </summary>
        /// <param name="slaveOk">Whether the query can be sent to a secondary server.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        [Obsolete("Use SetReadPreference instead.")]
        public virtual MongoCursor SetSlaveOk(bool slaveOk)
        {
            if (_isFrozen) { ThrowFrozen(); }
            _readPreference = ReadPreference.FromSlaveOk(slaveOk);
            return this;
        }

        /// <summary>
        /// Sets the $snapshot option.
        /// </summary>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor SetSnapshot()
        {
            if (_isFrozen) { ThrowFrozen(); }
            SetOption("$snapshot", true);
            return this;
        }

        /// <summary>
        /// Sets the sort order for the server to sort the documents by before returning them.
        /// </summary>
        /// <param name="sortBy">The sort order.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor SetSortOrder(IMongoSortBy sortBy)
        {
            if (sortBy == null)
            {
                throw new ArgumentNullException("sortBy");
            }
            if (_isFrozen) { ThrowFrozen(); }
            SetOption("$orderby", BsonDocumentWrapper.Create(sortBy));
            return this;
        }

        /// <summary>
        /// Sets the sort order for the server to sort the documents by before returning them.
        /// </summary>
        /// <param name="keys">The names of the fields to sort by.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor SetSortOrder(params string[] keys)
        {
            if (_isFrozen) { ThrowFrozen(); }
            return SetSortOrder(SortBy.Ascending(keys));
        }

        /// <summary>
        /// Returns the size of the result set (honors Skip and Limit, unlike Count which does not).
        /// </summary>
        /// <returns>The size of the result set.</returns>
        public virtual long Size()
        {
            _isFrozen = true;
            var command = new CommandDocument
            {
                { "count", _collection.Name },
                { "query", BsonDocumentWrapper.Create(_query), _query != null }, // query is optional
                { "limit", _limit, _limit != 0 },
                { "skip", _skip, _skip != 0 }
            };
            var result = _database.RunCommand(command);
            return result.Response["n"].ToInt64();
        }

        // protected methods
        /// <summary>
        /// Gets the non-generic enumerator.
        /// </summary>
        /// <returns>The enumerator.</returns>
        protected abstract IEnumerator IEnumerableGetEnumerator();

        // private methods
        // funnel exceptions through this method so we can have a single error message
        private void ThrowFrozen()
        {
            throw new InvalidOperationException("A MongoCursor object cannot be modified once it has been frozen.");
        }

        // explicit interface implementations
        IEnumerator IEnumerable.GetEnumerator()
        {
            return IEnumerableGetEnumerator();
        }
    }

    /// <summary>
    /// An object that can be enumerated to fetch the results of a query. The query is not sent
    /// to the server until you begin enumerating the results.
    /// </summary>
    /// <typeparam name="TDocument">The type of the documents returned.</typeparam>
    public class MongoCursor<TDocument> : MongoCursor, IEnumerable<TDocument>
    {
        // constructors
        /// <summary>
        /// Creates a new MongoCursor. It is very unlikely that you will call this constructor. Instead, see all the Find methods in MongoCollection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="query">The query.</param>
        /// <param name="readPreference">The read preference.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="serializationOptions">The serialization options.</param>
        public MongoCursor(MongoCollection collection, IMongoQuery query, ReadPreference readPreference, IBsonSerializer serializer, IBsonSerializationOptions serializationOptions)
            : base(collection, query, readPreference, serializer, serializationOptions)
        {
        }

        // public methods
        /// <summary>
        /// Returns an enumerator that can be used to enumerate the cursor. Normally you will use the foreach statement
        /// to enumerate the cursor (foreach will call GetEnumerator for you).
        /// </summary>
        /// <returns>An enumerator that can be used to iterate over the cursor.</returns>
        public virtual IEnumerator<TDocument> GetEnumerator()
        {
            IsFrozen = true;

            var readerSettings = new BsonBinaryReaderSettings
            {
                Encoding = Collection.Settings.ReadEncoding ?? MongoDefaults.ReadEncoding,
                GuidRepresentation = Collection.Settings.GuidRepresentation
            };

            var writerSettings = new BsonBinaryWriterSettings
            {
                Encoding = Collection.Settings.WriteEncoding ?? MongoDefaults.WriteEncoding,
                GuidRepresentation = Collection.Settings.GuidRepresentation
            };

            // the following values are overridden for commands that can't be sent to a secondary
            var flags = Flags;
            var readPreference = ReadPreference;

            if (readPreference.ReadPreferenceMode != ReadPreferenceMode.Primary && Collection.Name == "$cmd")
            {
                var queryDocument = Query.ToBsonDocument();
                var isSecondaryOk = CanCommandBeSentToSecondary.Delegate(queryDocument);
                if (!isSecondaryOk)
                {
                    // if the command can't be sent to a secondary, then we use primary here
                    // regardless of the user's choice.
                    readPreference = ReadPreference.Primary;
                    // remove the slaveOk bit from the flags
                    flags &= ~QueryFlags.SlaveOk;
                }
            }

            var readOperation = new QueryOperation<TDocument>(
                Database.Name,
                Collection.Name,
                readerSettings,
                writerSettings,
                BatchSize,
                Fields,
                flags,
                Limit,
                Options,
                Query,
                readPreference,
                SerializationOptions,
                Serializer,
                Skip);

            return readOperation.Execute(new MongoCursorConnectionProvider(Server, readPreference));
        }

        /// <summary>
        /// Sets the batch size (the number of documents returned per batch).
        /// </summary>
        /// <param name="batchSize">The number of documents in each batch.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public new virtual MongoCursor<TDocument> SetBatchSize(int batchSize)
        {
            return (MongoCursor<TDocument>)base.SetBatchSize(batchSize);
        }

        /// <summary>
        /// Sets the fields that will be returned from the server.
        /// </summary>
        /// <param name="fields">The fields that will be returned from the server.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public new virtual MongoCursor<TDocument> SetFields(IMongoFields fields)
        {
            return (MongoCursor<TDocument>)base.SetFields(fields);
        }

        /// <summary>
        /// Sets the fields that will be returned from the server.
        /// </summary>
        /// <param name="fields">The fields that will be returned from the server.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public new virtual MongoCursor<TDocument> SetFields(params string[] fields)
        {
            return (MongoCursor<TDocument>)base.SetFields(fields);
        }

        /// <summary>
        /// Sets the query flags.
        /// </summary>
        /// <param name="flags">The query flags.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public new virtual MongoCursor<TDocument> SetFlags(QueryFlags flags)
        {
            return (MongoCursor<TDocument>)base.SetFlags(flags);
        }

        /// <summary>
        /// Sets the index hint for the query.
        /// </summary>
        /// <param name="hint">The index hint.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public new virtual MongoCursor<TDocument> SetHint(BsonDocument hint)
        {
            return (MongoCursor<TDocument>)base.SetHint(hint);
        }

        /// <summary>
        /// Sets the index hint for the query.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public new virtual MongoCursor<TDocument> SetHint(string indexName)
        {
            return (MongoCursor<TDocument>)base.SetHint(indexName);
        }

        /// <summary>
        /// Sets the limit on the number of documents to be returned.
        /// </summary>
        /// <param name="limit">The limit on the number of documents to be returned.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public new virtual MongoCursor<TDocument> SetLimit(int limit)
        {
            return (MongoCursor<TDocument>)base.SetLimit(limit);
        }

        /// <summary>
        /// Sets the max value for the index key range of documents to return (note: the max value itself is excluded from the range).
        /// Often combined with SetHint (if SetHint is not used the server will attempt to determine the matching index automatically).
        /// </summary>
        /// <param name="max">The max value.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public new virtual MongoCursor<TDocument> SetMax(BsonDocument max)
        {
            return (MongoCursor<TDocument>)base.SetMax(max);
        }

        /// <summary>
        /// Sets the maximum number of documents to scan.
        /// </summary>
        /// <param name="maxScan">The maximum number of documents to scan.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public new virtual MongoCursor<TDocument> SetMaxScan(int maxScan)
        {
            return (MongoCursor<TDocument>)base.SetMaxScan(maxScan);
        }

        /// <summary>
        /// Sets the min value for the index key range of documents to return (note: the min value itself is included in the range).
        /// Often combined with SetHint (if SetHint is not used the server will attempt to determine the matching index automatically).
        /// </summary>
        /// <param name="min">The min value.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public new virtual MongoCursor<TDocument> SetMin(BsonDocument min)
        {
            return (MongoCursor<TDocument>)base.SetMin(min);
        }

        /// <summary>
        /// Sets a cursor option.
        /// </summary>
        /// <param name="name">The name of the option.</param>
        /// <param name="value">The value of the option.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public new virtual MongoCursor<TDocument> SetOption(string name, BsonValue value)
        {
            return (MongoCursor<TDocument>)base.SetOption(name, value);
        }

        /// <summary>
        /// Sets multiple cursor options. See also the individual Set{Option} methods, which are easier to use.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public new virtual MongoCursor<TDocument> SetOptions(BsonDocument options)
        {
            return (MongoCursor<TDocument>)base.SetOptions(options);
        }

        /// <summary>
        /// Sets the read preference.
        /// </summary>
        /// <param name="readPreference">The read preference.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public new virtual MongoCursor<TDocument> SetReadPreference(ReadPreference readPreference)
        {
            return (MongoCursor<TDocument>)base.SetReadPreference(readPreference);
        }

        /// <summary>
        /// Sets the serialization options (only needed in rare cases).
        /// </summary>
        /// <param name="serializationOptions">The serialization options.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public new virtual MongoCursor<TDocument> SetSerializationOptions(
            IBsonSerializationOptions serializationOptions)
        {
            return (MongoCursor<TDocument>)base.SetSerializationOptions(serializationOptions);
        }

        /// <summary>
        /// Sets the $showDiskLoc option.
        /// </summary>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public new virtual MongoCursor<TDocument> SetShowDiskLoc()
        {
            return (MongoCursor<TDocument>)base.SetShowDiskLoc();
        }

        /// <summary>
        /// Sets the number of documents the server should skip before returning the rest of the documents.
        /// </summary>
        /// <param name="skip">The number of documents to skip.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public new virtual MongoCursor<TDocument> SetSkip(int skip)
        {
            return (MongoCursor<TDocument>)base.SetSkip(skip);
        }

        /// <summary>
        /// Sets whether the query should be sent to a secondary server.
        /// </summary>
        /// <param name="slaveOk">Whether the query should be sent to a secondary server.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        [Obsolete("Use SetReadPreference instead.")]
        public new virtual MongoCursor<TDocument> SetSlaveOk(bool slaveOk)
        {
#pragma warning disable 618
            return (MongoCursor<TDocument>)base.SetSlaveOk(slaveOk);
#pragma warning restore 618
        }

        /// <summary>
        /// Sets the $snapshot option.
        /// </summary>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public new virtual MongoCursor<TDocument> SetSnapshot()
        {
            return (MongoCursor<TDocument>)base.SetSnapshot();
        }

        /// <summary>
        /// Sets the sort order for the server to sort the documents by before returning them.
        /// </summary>
        /// <param name="sortBy">The sort order.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public new virtual MongoCursor<TDocument> SetSortOrder(IMongoSortBy sortBy)
        {
            return (MongoCursor<TDocument>)base.SetSortOrder(sortBy);
        }

        /// <summary>
        /// Sets the sort order for the server to sort the documents by before returning them.
        /// </summary>
        /// <param name="keys">The names of the fields to sort by.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public new virtual MongoCursor<TDocument> SetSortOrder(params string[] keys)
        {
            return (MongoCursor<TDocument>)base.SetSortOrder(keys);
        }

        // protected methods
        /// <summary>
        /// Gets the non-generic enumerator.
        /// </summary>
        /// <returns>The enumerator.</returns>
        protected override IEnumerator IEnumerableGetEnumerator()
        {
            return GetEnumerator();
        }

        // nested classes
        private class MongoCursorConnectionProvider : IConnectionProvider
        {
            private readonly MongoServer _server;
            private readonly ReadPreference _readPreference;
            private MongoServerInstance _serverInstance;

            public MongoCursorConnectionProvider(MongoServer server, ReadPreference readPreference)
            {
                _server = server;
                _readPreference = readPreference;
            }

            public MongoConnection AcquireConnection()
            {
                if (_serverInstance == null)
                {
                    // first time we need a connection let Server.AcquireConnection pick the server instance
                    var connection = _server.AcquireConnection(_readPreference);
                    _serverInstance = connection.ServerInstance;
                    return connection;
                }
                else
                {
                    // all subsequent requests for the same cursor must go to the same server instance
                    return _server.AcquireConnection(_serverInstance);
                }
            }

            public void ReleaseConnection(MongoConnection connection)
            {
                _server.ReleaseConnection(connection);
            }
        }
    }
}
