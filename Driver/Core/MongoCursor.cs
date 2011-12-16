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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver
{
    /// <summary>
    /// An object that can be enumerated to fetch the results of a query. The query is not sent
    /// to the server until you begin enumerating the results.
    /// </summary>
    public abstract class MongoCursor : IEnumerable
    {
        // private fields
        private MongoServer server;
        private MongoDatabase database;
        private MongoCollection collection;
        private IMongoQuery query;
        private IMongoFields fields;
        private BsonDocument options;
        private QueryFlags flags;
        private bool slaveOk;
        private int skip;
        private int limit; // number of documents to return (enforced by cursor)
        private int batchSize; // number of documents to return in each reply
        private IBsonSerializationOptions serializationOptions;
        private bool isFrozen; // prevent any further modifications once enumeration has begun

        // constructors
        /// <summary>
        /// Creates a new MongoCursor. It is very unlikely that you will call this constructor. Instead, see all the Find methods in MongoCollection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="query">The query.</param>
        protected MongoCursor(MongoCollection collection, IMongoQuery query)
        {
            this.server = collection.Database.Server;
            this.database = collection.Database;
            this.collection = collection;
            this.query = query;
            this.slaveOk = collection.Settings.SlaveOk;
        }

        // public properties
        /// <summary>
        /// Gets the server that the query will be sent to.
        /// </summary>
        public virtual MongoServer Server
        {
            get { return server; }
        }

        /// <summary>
        /// Gets the database that constains the collection that is being queried.
        /// </summary>
        public virtual MongoDatabase Database
        {
            get { return database; }
        }

        /// <summary>
        /// Gets the collection that is being queried.
        /// </summary>
        public virtual MongoCollection Collection
        {
            get { return collection; }
        }

        /// <summary>
        /// Gets the query that will be sent to the server.
        /// </summary>
        public virtual IMongoQuery Query
        {
            get { return query; }
        }

        /// <summary>
        /// Gets or sets the fields that will be returned from the server.
        /// </summary>
        public virtual IMongoFields Fields
        {
            get { return fields; }
            set
            {
                if (isFrozen) { ThrowFrozen(); }
                fields = value;
            }
        }

        /// <summary>
        /// Gets or sets the cursor options. See also the individual Set{Option} methods, which are easier to use.
        /// </summary>
        public virtual BsonDocument Options
        {
            get { return options; }
            set
            {
                if (isFrozen) { ThrowFrozen(); }
                options = value;
            }
        }

        /// <summary>
        /// Gets or sets the query flags.
        /// </summary>
        public virtual QueryFlags Flags
        {
            get { return flags | (slaveOk ? QueryFlags.SlaveOk : 0); }
            set
            {
                if (isFrozen) { ThrowFrozen(); }
                flags = value;
            }
        }

        /// <summary>
        /// Gets or sets whether the query should be sent to a secondary server.
        /// </summary>
        public virtual bool SlaveOk
        {
            get { return slaveOk || ((flags & QueryFlags.SlaveOk) != 0); }
            set
            {
                if (isFrozen) { ThrowFrozen(); }
                slaveOk = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of documents the server should skip before returning the rest of the documents.
        /// </summary>
        public virtual int Skip
        {
            get { return skip; }
            set
            {
                if (isFrozen) { ThrowFrozen(); }
                skip = value;
            }
        }

        /// <summary>
        /// Gets or sets the limit on the number of documents to be returned.
        /// </summary>
        public virtual int Limit
        {
            get { return limit; }
            set
            {
                if (isFrozen) { ThrowFrozen(); }
                limit = value;
            }
        }

        /// <summary>
        /// Gets or sets the batch size (the number of documents returned per batch).
        /// </summary>
        public virtual int BatchSize
        {
            get { return batchSize; }
            set
            {
                if (isFrozen) { ThrowFrozen(); }
                batchSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the serialization options (only needed in rare cases).
        /// </summary>
        public virtual IBsonSerializationOptions SerializationOptions
        {
            get { return serializationOptions; }
            set
            {
                if (isFrozen) { ThrowFrozen(); }
                serializationOptions = value;
            }
        }

        /// <summary>
        /// Gets whether the cursor has been frozen to prevent further changes.
        /// </summary>
        public virtual bool IsFrozen
        {
            get { return isFrozen; }
            protected set { isFrozen = value; }
        }

        // public static methods
        /// <summary>
        /// Creates a cursor.
        /// </summary>
        /// <param name="documentType">The type of the returned documents.</param>
        /// <param name="collection">The collection to query.</param>
        /// <param name="query">A query.</param>
        /// <returns>A cursor.</returns>
        public static MongoCursor Create(Type documentType, MongoCollection collection, IMongoQuery query)
        {
            var cursorDefinition = typeof(MongoCursor<>);
            var cursorType = cursorDefinition.MakeGenericType(documentType);
            var constructorInfo = cursorType.GetConstructor(new Type[] { typeof(MongoCollection), typeof(IMongoQuery) });
            return (MongoCursor)constructorInfo.Invoke(new object[] { collection, query });
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
        /// <param name="documentType">The type of the documents returned.</param>
        /// <returns>A clone of the cursor.</returns>
        public virtual MongoCursor Clone(Type documentType)
        {
            var clone = Create(documentType, collection, query);
            clone.options = options == null ? null : (BsonDocument)options.Clone();
            clone.flags = flags;
            clone.slaveOk = slaveOk;
            clone.skip = skip;
            clone.limit = limit;
            clone.batchSize = batchSize;
            clone.fields = fields;
            clone.serializationOptions = serializationOptions;
            return clone;
        }

        /// <summary>
        /// Returns the number of documents that match the query (ignores Skip and Limit, unlike Size which honors them).
        /// </summary>
        /// <returns>The number of documents that match the query.</returns>
        public virtual long Count()
        {
            isFrozen = true;
            var command = new CommandDocument
            {
                { "count", collection.Name },
                { "query", BsonDocumentWrapper.Create(query) } // query is optional
            };
            var result = database.RunCommand(command);
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
            isFrozen = true;
            var clone = Clone<BsonDocument>();
            clone.SetOption("$explain", true);
            clone.limit = -clone.limit; // TODO: should this be -1?
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
            if (isFrozen) { ThrowFrozen(); }
            if (batchSize < 0) { throw new ArgumentException("BatchSize cannot be negative."); }
            this.batchSize = batchSize;
            return this;
        }

        /// <summary>
        /// Sets the fields that will be returned from the server.
        /// </summary>
        /// <param name="fields">The fields that will be returned from the server.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor SetFields(IMongoFields fields)
        {
            if (isFrozen) { ThrowFrozen(); }
            this.fields = fields;
            return this;
        }

        /// <summary>
        /// Sets the fields that will be returned from the server.
        /// </summary>
        /// <param name="fields">The fields that will be returned from the server.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor SetFields(params string[] fields)
        {
            if (isFrozen) { ThrowFrozen(); }
            this.fields = Builders.Fields.Include(fields);
            return this;
        }

        /// <summary>
        /// Sets the query flags.
        /// </summary>
        /// <param name="flags">The query flags.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor SetFlags(QueryFlags flags)
        {
            if (isFrozen) { ThrowFrozen(); }
            this.flags = flags;
            return this;
        }

        /// <summary>
        /// Sets the index hint for the query.
        /// </summary>
        /// <param name="hint">The index hint.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor SetHint(BsonDocument hint)
        {
            if (isFrozen) { ThrowFrozen(); }
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
            if (isFrozen) { ThrowFrozen(); }
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
            if (isFrozen) { ThrowFrozen(); }
            this.limit = limit;
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
            if (isFrozen) { ThrowFrozen(); }
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
            if (isFrozen) { ThrowFrozen(); }
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
            if (isFrozen) { ThrowFrozen(); }
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
            if (isFrozen) { ThrowFrozen(); }
            if (options == null) { options = new BsonDocument(); }
            options[name] = value;
            return this;
        }

        /// <summary>
        /// Sets multiple cursor options. See also the individual Set{Option} methods, which are easier to use.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor SetOptions(BsonDocument options)
        {
            if (isFrozen) { ThrowFrozen(); }
            if (options != null)
            {
                if (this.options == null) { this.options = new BsonDocument(); }
                this.options.Merge(options, true); // overwriteExistingElements
            }
            return this;
        }

        /// <summary>
        /// Sets the serialization options (only needed in rare cases).
        /// </summary>
        /// <param name="serializationOptions">The serialization options.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor SetSerializationOptions(IBsonSerializationOptions serializationOptions)
        {
            if (isFrozen) { ThrowFrozen(); }
            this.serializationOptions = serializationOptions;
            return this;
        }

        /// <summary>
        /// Sets the $showDiskLoc option.
        /// </summary>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor SetShowDiskLoc()
        {
            if (isFrozen) { ThrowFrozen(); }
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
            if (isFrozen) { ThrowFrozen(); }
            if (skip < 0) { throw new ArgumentException("Skip cannot be negative."); }
            this.skip = skip;
            return this;
        }

        /// <summary>
        /// Sets whether the query should be sent to a secondary server.
        /// </summary>
        /// <param name="slaveOk">Whether the query should be sent to a secondary server.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor SetSlaveOk(bool slaveOk)
        {
            if (isFrozen) { ThrowFrozen(); }
            this.slaveOk = slaveOk;
            return this;
        }

        /// <summary>
        /// Sets the $snapshot option.
        /// </summary>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public virtual MongoCursor SetSnapshot()
        {
            if (isFrozen) { ThrowFrozen(); }
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
            if (isFrozen) { ThrowFrozen(); }
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
            if (isFrozen) { ThrowFrozen(); }
            return SetSortOrder(SortBy.Ascending(keys));
        }

        /// <summary>
        /// Returns the size of the result set (honors Skip and Limit, unlike Count which does not).
        /// </summary>
        /// <returns>The size of the result set.</returns>
        public virtual long Size()
        {
            isFrozen = true;
            var command = new CommandDocument
            {
                { "count", collection.Name },
                { "query", BsonDocumentWrapper.Create(query) }, // query is optional
                { "limit", limit, limit != 0 },
                { "skip", skip, skip != 0 }
            };
            var result = database.RunCommand(command);
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
        public MongoCursor(MongoCollection collection, IMongoQuery query)
            : base(collection, query)
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
            return new MongoCursorEnumerator<TDocument>(this);
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
        /// Sets the serialization options (only needed in rare cases).
        /// </summary>
        /// <param name="serializationOptions">The serialization options.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        public new virtual MongoCursor<TDocument> SetSerializationOptions(IBsonSerializationOptions serializationOptions)
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
        public new virtual MongoCursor<TDocument> SetSlaveOk(bool slaveOk)
        {
            return (MongoCursor<TDocument>)base.SetSlaveOk(slaveOk);
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
    }
}
