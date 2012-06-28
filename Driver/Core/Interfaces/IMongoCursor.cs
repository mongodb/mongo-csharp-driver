using System;
using System.Collections;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver
{
    /// <summary>
    /// An object that can be enumerated to fetch the results of a query. The query is not sent
    /// to the server until you begin enumerating the results.
    /// </summary>
    public interface IMongoCursor : IEnumerable
    {
        /// <summary>
        /// Gets the server that the query will be sent to.
        /// </summary>
        IMongoServer Server { get; }

        /// <summary>
        /// Gets the database that constains the collection that is being queried.
        /// </summary>
        IMongoDatabase Database { get; }

        /// <summary>
        /// Gets the collection that is being queried.
        /// </summary>
        IMongoCollection Collection { get; }

        /// <summary>
        /// Gets the query that will be sent to the server.
        /// </summary>
        IMongoQuery Query { get; }

        /// <summary>
        /// Gets or sets the fields that will be returned from the server.
        /// </summary>
        IMongoFields Fields { get; set; }

        /// <summary>
        /// Gets or sets the cursor options. See also the individual Set{Option} methods, which are easier to use.
        /// </summary>
        BsonDocument Options { get; set; }

        /// <summary>
        /// Gets or sets the query flags.
        /// </summary>
        QueryFlags Flags { get; set; }

        /// <summary>
        /// Gets or sets whether the query should be sent to a secondary server.
        /// </summary>
        bool SlaveOk { get; set; }

        /// <summary>
        /// Gets or sets the number of documents the server should skip before returning the rest of the documents.
        /// </summary>
        int Skip { get; set; }

        /// <summary>
        /// Gets or sets the limit on the number of documents to be returned.
        /// </summary>
        int Limit { get; set; }

        /// <summary>
        /// Gets or sets the batch size (the number of documents returned per batch).
        /// </summary>
        int BatchSize { get; set; }

        /// <summary>
        /// Gets or sets the serialization options (only needed in rare cases).
        /// </summary>
        IBsonSerializationOptions SerializationOptions { get; set; }

        /// <summary>
        /// Gets whether the cursor has been frozen to prevent further changes.
        /// </summary>
        bool IsFrozen { get; }

        /// <summary>
        /// Creates a clone of the cursor.
        /// </summary>
        /// <typeparam name="TDocument">The type of the documents returned.</typeparam>
        /// <returns>A clone of the cursor.</returns>
        IMongoCursor<TDocument> Clone<TDocument>();

        /// <summary>
        /// Creates a clone of the cursor.
        /// </summary>
        /// <param name="documentType">The type of the documents returned.</param>
        /// <returns>A clone of the cursor.</returns>
        IMongoCursor Clone(Type documentType);

        /// <summary>
        /// Returns the number of documents that match the query (ignores Skip and Limit, unlike Size which honors them).
        /// </summary>
        /// <returns>The number of documents that match the query.</returns>
        long Count();

        /// <summary>
        /// Returns an explanation of how the query was executed (instead of the results).
        /// </summary>
        /// <returns>An explanation of thow the query was executed.</returns>
        BsonDocument Explain();

        /// <summary>
        /// Returns an explanation of how the query was executed (instead of the results).
        /// </summary>
        /// <param name="verbose">Whether the explanation should contain more details.</param>
        /// <returns>An explanation of thow the query was executed.</returns>
        BsonDocument Explain(bool verbose);

        /// <summary>
        /// Sets the batch size (the number of documents returned per batch).
        /// </summary>
        /// <param name="batchSize">The number of documents in each batch.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        IMongoCursor SetBatchSize(int batchSize);

        /// <summary>
        /// Sets the fields that will be returned from the server.
        /// </summary>
        /// <param name="fields">The fields that will be returned from the server.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        IMongoCursor SetFields(IMongoFields fields);

        /// <summary>
        /// Sets the fields that will be returned from the server.
        /// </summary>
        /// <param name="fields">The fields that will be returned from the server.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        IMongoCursor SetFields(params string[] fields);

        /// <summary>
        /// Sets the query flags.
        /// </summary>
        /// <param name="flags">The query flags.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        IMongoCursor SetFlags(QueryFlags flags);

        /// <summary>
        /// Sets the index hint for the query.
        /// </summary>
        /// <param name="hint">The index hint.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        IMongoCursor SetHint(BsonDocument hint);

        /// <summary>
        /// Sets the index hint for the query.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        IMongoCursor SetHint(string indexName);

        /// <summary>
        /// Sets the limit on the number of documents to be returned.
        /// </summary>
        /// <param name="limit">The limit on the number of documents to be returned.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        IMongoCursor SetLimit(int limit);

        /// <summary>
        /// Sets the max value for the index key range of documents to return (note: the max value itself is excluded from the range).
        /// Often combined with SetHint (if SetHint is not used the server will attempt to determine the matching index automatically).
        /// </summary>
        /// <param name="max">The max value.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        IMongoCursor SetMax(BsonDocument max);

        /// <summary>
        /// Sets the maximum number of documents to scan.
        /// </summary>
        /// <param name="maxScan">The maximum number of documents to scan.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        IMongoCursor SetMaxScan(int maxScan);

        /// <summary>
        /// Sets the min value for the index key range of documents to return (note: the min value itself is included in the range).
        /// Often combined with SetHint (if SetHint is not used the server will attempt to determine the matching index automatically).
        /// </summary>
        /// <param name="min">The min value.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        IMongoCursor SetMin(BsonDocument min);

        /// <summary>
        /// Sets a cursor option.
        /// </summary>
        /// <param name="name">The name of the option.</param>
        /// <param name="value">The value of the option.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        IMongoCursor SetOption(string name, BsonValue value);

        /// <summary>
        /// Sets multiple cursor options. See also the individual Set{Option} methods, which are easier to use.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        IMongoCursor SetOptions(BsonDocument options);

        /// <summary>
        /// Sets the serialization options (only needed in rare cases).
        /// </summary>
        /// <param name="serializationOptions">The serialization options.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        IMongoCursor SetSerializationOptions(IBsonSerializationOptions serializationOptions);

        /// <summary>
        /// Sets the $showDiskLoc option.
        /// </summary>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        IMongoCursor SetShowDiskLoc();

        /// <summary>
        /// Sets the number of documents the server should skip before returning the rest of the documents.
        /// </summary>
        /// <param name="skip">The number of documents to skip.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        IMongoCursor SetSkip(int skip);

        /// <summary>
        /// Sets whether the query should be sent to a secondary server.
        /// </summary>
        /// <param name="slaveOk">Whether the query should be sent to a secondary server.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        IMongoCursor SetSlaveOk(bool slaveOk);

        /// <summary>
        /// Sets the $snapshot option.
        /// </summary>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        IMongoCursor SetSnapshot();

        /// <summary>
        /// Sets the sort order for the server to sort the documents by before returning them.
        /// </summary>
        /// <param name="sortBy">The sort order.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        IMongoCursor SetSortOrder(IMongoSortBy sortBy);

        /// <summary>
        /// Sets the sort order for the server to sort the documents by before returning them.
        /// </summary>
        /// <param name="keys">The names of the fields to sort by.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        IMongoCursor SetSortOrder(params string[] keys);

        /// <summary>
        /// Returns the size of the result set (honors Skip and Limit, unlike Count which does not).
        /// </summary>
        /// <returns>The size of the result set.</returns>
        long Size();
    }

    /// <summary>
    /// An object that can be enumerated to fetch the results of a query. The query is not sent
    /// to the server until you begin enumerating the results.
    /// </summary>
    /// <typeparam name="TDocument">The type of the documents returned.</typeparam>
    public interface IMongoCursor<TDocument> : IMongoCursor, IEnumerable<TDocument>
    {
        ///// <summary>
        ///// Returns an enumerator that can be used to enumerate the cursor. Normally you will use the foreach statement
        ///// to enumerate the cursor (foreach will call GetEnumerator for you).
        ///// </summary>
        ///// <returns>An enumerator that can be used to iterate over the cursor.</returns>
        //new IEnumerator<TDocument> GetEnumerator();

        /// <summary>
        /// Sets the batch size (the number of documents returned per batch).
        /// </summary>
        /// <param name="batchSize">The number of documents in each batch.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        new IMongoCursor<TDocument> SetBatchSize(int batchSize);

        /// <summary>
        /// Sets the fields that will be returned from the server.
        /// </summary>
        /// <param name="fields">The fields that will be returned from the server.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        new IMongoCursor<TDocument> SetFields(IMongoFields fields);

        /// <summary>
        /// Sets the fields that will be returned from the server.
        /// </summary>
        /// <param name="fields">The fields that will be returned from the server.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        new IMongoCursor<TDocument> SetFields(params string[] fields);

        /// <summary>
        /// Sets the query flags.
        /// </summary>
        /// <param name="flags">The query flags.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        new IMongoCursor<TDocument> SetFlags(QueryFlags flags);

        /// <summary>
        /// Sets the index hint for the query.
        /// </summary>
        /// <param name="hint">The index hint.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        new IMongoCursor<TDocument> SetHint(BsonDocument hint);

        /// <summary>
        /// Sets the index hint for the query.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        new IMongoCursor<TDocument> SetHint(string indexName);

        /// <summary>
        /// Sets the limit on the number of documents to be returned.
        /// </summary>
        /// <param name="limit">The limit on the number of documents to be returned.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        new IMongoCursor<TDocument> SetLimit(int limit);

        /// <summary>
        /// Sets the max value for the index key range of documents to return (note: the max value itself is excluded from the range).
        /// Often combined with SetHint (if SetHint is not used the server will attempt to determine the matching index automatically).
        /// </summary>
        /// <param name="max">The max value.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        new IMongoCursor<TDocument> SetMax(BsonDocument max);

        /// <summary>
        /// Sets the maximum number of documents to scan.
        /// </summary>
        /// <param name="maxScan">The maximum number of documents to scan.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        new IMongoCursor<TDocument> SetMaxScan(int maxScan);

        /// <summary>
        /// Sets the min value for the index key range of documents to return (note: the min value itself is included in the range).
        /// Often combined with SetHint (if SetHint is not used the server will attempt to determine the matching index automatically).
        /// </summary>
        /// <param name="min">The min value.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        new IMongoCursor<TDocument> SetMin(BsonDocument min);

        /// <summary>
        /// Sets a cursor option.
        /// </summary>
        /// <param name="name">The name of the option.</param>
        /// <param name="value">The value of the option.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        new IMongoCursor<TDocument> SetOption(string name, BsonValue value);

        /// <summary>
        /// Sets multiple cursor options. See also the individual Set{Option} methods, which are easier to use.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        new IMongoCursor<TDocument> SetOptions(BsonDocument options);

        /// <summary>
        /// Sets the serialization options (only needed in rare cases).
        /// </summary>
        /// <param name="serializationOptions">The serialization options.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        new IMongoCursor<TDocument> SetSerializationOptions(
            IBsonSerializationOptions serializationOptions);

        /// <summary>
        /// Sets the $showDiskLoc option.
        /// </summary>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        new IMongoCursor<TDocument> SetShowDiskLoc();

        /// <summary>
        /// Sets the number of documents the server should skip before returning the rest of the documents.
        /// </summary>
        /// <param name="skip">The number of documents to skip.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        new IMongoCursor<TDocument> SetSkip(int skip);

        /// <summary>
        /// Sets whether the query should be sent to a secondary server.
        /// </summary>
        /// <param name="slaveOk">Whether the query should be sent to a secondary server.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        new IMongoCursor<TDocument> SetSlaveOk(bool slaveOk);

        /// <summary>
        /// Sets the $snapshot option.
        /// </summary>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        new IMongoCursor<TDocument> SetSnapshot();

        /// <summary>
        /// Sets the sort order for the server to sort the documents by before returning them.
        /// </summary>
        /// <param name="sortBy">The sort order.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        new IMongoCursor<TDocument> SetSortOrder(IMongoSortBy sortBy);

        /// <summary>
        /// Sets the sort order for the server to sort the documents by before returning them.
        /// </summary>
        /// <param name="keys">The names of the fields to sort by.</param>
        /// <returns>The cursor (so you can chain method calls to it).</returns>
        new IMongoCursor<TDocument> SetSortOrder(params string[] keys);
    }
}