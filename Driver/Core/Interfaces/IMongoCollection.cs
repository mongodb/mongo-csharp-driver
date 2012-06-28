using System;
using System.Collections;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Wrappers;

namespace MongoDB.Driver
{
    public interface IMongoCollection 
    {
        /// <summary>
        /// Gets the database that contains this collection.
        /// </summary>
        IMongoDatabase Database { get; }

        /// <summary>
        /// Gets the fully qualified name of this collection.
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// Gets the name of this collection.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the settings being used to access this collection.
        /// </summary>
        MongoCollectionSettings Settings { get; }

        /// <summary>
        /// Counts the number of documents in this collection.
        /// </summary>
        /// <returns>The number of documents in this collection.</returns>
        long Count();

        /// <summary>
        /// Counts the number of documents in this collection that match a query.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>The number of documents in this collection that match the query.</returns>
        long Count(IMongoQuery query);

        /// <summary>
        /// Creates an index for this collection.
        /// </summary>
        /// <param name="keys">The indexed fields (usually an IndexKeysDocument or constructed using the IndexKeys builder).</param>
        /// <param name="options">The index options(usually an IndexOptionsDocument or created using the IndexOption builder).</param>
        /// <returns>A SafeModeResult.</returns>
        SafeModeResult CreateIndex(IMongoIndexKeys keys, IMongoIndexOptions options);

        /// <summary>
        /// Creates an index for this collection.
        /// </summary>
        /// <param name="keys">The indexed fields (usually an IndexKeysDocument or constructed using the IndexKeys builder).</param>
        /// <returns>A SafeModeResult.</returns>
        SafeModeResult CreateIndex(IMongoIndexKeys keys);

        /// <summary>
        /// Creates an index for this collection.
        /// </summary>
        /// <param name="keyNames">The names of the indexed fields.</param>
        /// <returns>A SafeModeResult.</returns>
        SafeModeResult CreateIndex(params string[] keyNames);

        /// <summary>
        /// Returns the distinct values for a given field.
        /// </summary>
        /// <param name="key">The key of the field.</param>
        /// <returns>The distint values of the field.</returns>
        IEnumerable<BsonValue> Distinct(string key);

        /// <summary>
        /// Returns the distinct values for a given field for documents that match a query.
        /// </summary>
        /// <param name="key">The key of the field.</param>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>The distint values of the field.</returns>
        IEnumerable<BsonValue> Distinct(string key, IMongoQuery query);

        /// <summary>
        /// Drops this collection.
        /// </summary>
        /// <returns>A CommandResult.</returns>
        CommandResult Drop();

        /// <summary>
        /// Drops all indexes on this collection.
        /// </summary>
        /// <returns>A <see cref="CommandResult"/>.</returns>
        CommandResult DropAllIndexes();

        /// <summary>
        /// Drops an index on this collection.
        /// </summary>
        /// <param name="keys">The indexed fields (usually an IndexKeysDocument or constructed using the IndexKeys builder).</param>
        /// <returns>A <see cref="CommandResult"/>.</returns>
        CommandResult DropIndex(IMongoIndexKeys keys);

        /// <summary>
        /// Drops an index on this collection.
        /// </summary>
        /// <param name="keyNames">The names of the indexed fields.</param>
        /// <returns>A <see cref="CommandResult"/>.</returns>
        CommandResult DropIndex(params string[] keyNames);

        /// <summary>
        /// Drops an index on this collection.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <returns>A <see cref="CommandResult"/>.</returns>
        CommandResult DropIndexByName(string indexName);

        /// <summary>
        /// Ensures that the desired index exists and creates it if it does not.
        /// </summary>
        /// <param name="keys">The indexed fields (usually an IndexKeysDocument or constructed using the IndexKeys builder).</param>
        /// <param name="options">The index options(usually an IndexOptionsDocument or created using the IndexOption builder).</param>
        void EnsureIndex(IMongoIndexKeys keys, IMongoIndexOptions options);

        /// <summary>
        /// Ensures that the desired index exists and creates it if it does not.
        /// </summary>
        /// <param name="keys">The indexed fields (usually an IndexKeysDocument or constructed using the IndexKeys builder).</param>
        void EnsureIndex(IMongoIndexKeys keys);

        /// <summary>
        /// Ensures that the desired index exists and creates it if it does not.
        /// </summary>
        /// <param name="keyNames">The names of the indexed fields.</param>
        void EnsureIndex(params string[] keyNames);

        /// <summary>
        /// Tests whether this collection exists.
        /// </summary>
        /// <returns>True if this collection exists.</returns>
        bool Exists();

        /// <summary>
        /// Returns a cursor that can be used to find all documents in this collection as TDocuments.
        /// </summary>
        /// <typeparam name="TDocument">The nominal type of the documents.</typeparam>
        /// <returns>A <see cref="MongoCursor{TDocument}"/>.</returns>
        IMongoCursor<TDocument> FindAllAs<TDocument>();

        /// <summary>
        /// Returns a cursor that can be used to find all documents in this collection as TDocuments.
        /// </summary>
        /// <param name="documentType">The nominal type of the documents.</param>
        /// <returns>A <see cref="MongoCursor{TDocument}"/>.</returns>
        IMongoCursor FindAllAs(Type documentType);

        /// <summary>
        /// Finds one matching document using the query and sortBy parameters and applies the specified update to it.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="sortBy">The sort order to select one of the matching documents.</param>
        /// <param name="update">The update to apply to the matching document.</param>
        /// <returns>A <see cref="FindAndModifyResult"/>.</returns>
        FindAndModifyResult FindAndModify(IMongoQuery query, IMongoSortBy sortBy, IMongoUpdate update);

        /// <summary>
        /// Finds one matching document using the query and sortBy parameters and applies the specified update to it.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="sortBy">The sort order to select one of the matching documents.</param>
        /// <param name="update">The update to apply to the matching document.</param>
        /// <param name="returnNew">Whether to return the new or old version of the modified document in the <see cref="FindAndModifyResult"/>.</param>
        /// <returns>A <see cref="FindAndModifyResult"/>.</returns>
        FindAndModifyResult FindAndModify(
            IMongoQuery query,
            IMongoSortBy sortBy,
            IMongoUpdate update,
            bool returnNew);

        /// <summary>
        /// Finds one matching document using the query and sortBy parameters and applies the specified update to it.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="sortBy">The sort order to select one of the matching documents.</param>
        /// <param name="update">The update to apply to the matching document.</param>
        /// <param name="returnNew">Whether to return the new or old version of the modified document in the <see cref="FindAndModifyResult"/>.</param>
        /// <param name="upsert">Whether to do an upsert if no matching document is found.</param>
        /// <returns>A <see cref="FindAndModifyResult"/>.</returns>
        FindAndModifyResult FindAndModify(
            IMongoQuery query,
            IMongoSortBy sortBy,
            IMongoUpdate update,
            bool returnNew,
            bool upsert);

        /// <summary>
        /// Finds one matching document using the query and sortBy parameters and applies the specified update to it.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="sortBy">The sort order to select one of the matching documents.</param>
        /// <param name="update">The update to apply to the matching document.</param>
        /// <param name="fields">Which fields of the modified document to return in the <see cref="FindAndModifyResult"/>.</param>
        /// <param name="returnNew">Whether to return the new or old version of the modified document in the <see cref="FindAndModifyResult"/>.</param>
        /// <param name="upsert">Whether to do an upsert if no matching document is found.</param>
        /// <returns>A <see cref="FindAndModifyResult"/>.</returns>
        FindAndModifyResult FindAndModify(
            IMongoQuery query,
            IMongoSortBy sortBy,
            IMongoUpdate update,
            IMongoFields fields,
            bool returnNew,
            bool upsert);

        /// <summary>
        /// Finds one matching document using the query and sortBy parameters and removes it from this collection.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="sortBy">The sort order to select one of the matching documents.</param>
        /// <returns>A <see cref="FindAndModifyResult"/>.</returns>
        FindAndModifyResult FindAndRemove(IMongoQuery query, IMongoSortBy sortBy);

        /// <summary>
        /// Returns a cursor that can be used to find all documents in this collection that match the query as TDocuments.
        /// </summary>
        /// <typeparam name="TDocument">The type to deserialize the documents as.</typeparam>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>A <see cref="MongoCursor{TDocument}"/>.</returns>
        IMongoCursor<TDocument> FindAs<TDocument>(IMongoQuery query);

        /// <summary>
        /// Returns a cursor that can be used to find all documents in this collection that match the query as TDocuments.
        /// </summary>
        /// <param name="documentType">The nominal type of the documents.</param>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>A <see cref="MongoCursor{TDocument}"/>.</returns>
        IMongoCursor FindAs(Type documentType, IMongoQuery query);

        /// <summary>
        /// Returns a cursor that can be used to find one document in this collection as a TDocument.
        /// </summary>
        /// <typeparam name="TDocument">The type to deserialize the documents as.</typeparam>
        /// <returns>A TDocument (or null if not found).</returns>
        TDocument FindOneAs<TDocument>();

        /// <summary>
        /// Returns a cursor that can be used to find one document in this collection that matches a query as a TDocument.
        /// </summary>
        /// <typeparam name="TDocument">The type to deserialize the documents as.</typeparam>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>A TDocument (or null if not found).</returns>
        TDocument FindOneAs<TDocument>(IMongoQuery query);

        /// <summary>
        /// Returns a cursor that can be used to find one document in this collection as a TDocument.
        /// </summary>
        /// <param name="documentType">The nominal type of the documents.</param>
        /// <returns>A document (or null if not found).</returns>
        object FindOneAs(Type documentType);

        /// <summary>
        /// Returns a cursor that can be used to find one document in this collection that matches a query as a TDocument.
        /// </summary>
        /// <param name="documentType">The type to deserialize the documents as.</param>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>A TDocument (or null if not found).</returns>
        object FindOneAs(Type documentType, IMongoQuery query);

        /// <summary>
        /// Returns a cursor that can be used to find one document in this collection by its _id value as a TDocument.
        /// </summary>
        /// <typeparam name="TDocument">The nominal type of the document.</typeparam>
        /// <param name="id">The id of the document.</param>
        /// <returns>A TDocument (or null if not found).</returns>
        TDocument FindOneByIdAs<TDocument>(BsonValue id);

        /// <summary>
        /// Returns a cursor that can be used to find one document in this collection by its _id value as a TDocument.
        /// </summary>
        /// <param name="documentType">The nominal type of the document.</param>
        /// <param name="id">The id of the document.</param>
        /// <returns>A TDocument (or null if not found).</returns>
        object FindOneByIdAs(Type documentType, BsonValue id);

        /// <summary>
        /// Runs a geoHaystack search command on this collection.
        /// </summary>
        /// <typeparam name="TDocument">The type of the found documents.</typeparam>
        /// <param name="x">The x coordinate of the starting location.</param>
        /// <param name="y">The y coordinate of the starting location.</param>
        /// <param name="options">The options for the geoHaystack search (null if none).</param>
        /// <returns>A <see cref="GeoNearResult{TDocument}"/>.</returns>
        GeoHaystackSearchResult<TDocument> GeoHaystackSearchAs<TDocument>(
            double x,
            double y,
            IMongoGeoHaystackSearchOptions options);

        /// <summary>
        /// Runs a geoHaystack search command on this collection.
        /// </summary>
        /// <param name="documentType">The type to deserialize the documents as.</param>
        /// <param name="x">The x coordinate of the starting location.</param>
        /// <param name="y">The y coordinate of the starting location.</param>
        /// <param name="options">The options for the geoHaystack search (null if none).</param>
        /// <returns>A <see cref="GeoNearResult{TDocument}"/>.</returns>
        GeoHaystackSearchResult GeoHaystackSearchAs(
            Type documentType,
            double x,
            double y,
            IMongoGeoHaystackSearchOptions options);

        /// <summary>
        /// Runs a GeoNear command on this collection.
        /// </summary>
        /// <typeparam name="TDocument">The type to deserialize the documents as.</typeparam>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="x">The x coordinate of the starting location.</param>
        /// <param name="y">The y coordinate of the starting location.</param>
        /// <param name="limit">The maximum number of results returned.</param>
        /// <returns>A <see cref="GeoNearResult{TDocument}"/>.</returns>
        GeoNearResult<TDocument> GeoNearAs<TDocument>(
            IMongoQuery query,
            double x,
            double y,
            int limit);

        /// <summary>
        /// Runs a GeoNear command on this collection.
        /// </summary>
        /// <typeparam name="TDocument">The type to deserialize the documents as.</typeparam>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="x">The x coordinate of the starting location.</param>
        /// <param name="y">The y coordinate of the starting location.</param>
        /// <param name="limit">The maximum number of results returned.</param>
        /// <param name="options">The GeoNear command options (usually a GeoNearOptionsDocument or constructed using the GeoNearOptions builder).</param>
        /// <returns>A <see cref="GeoNearResult{TDocument}"/>.</returns>
        GeoNearResult<TDocument> GeoNearAs<TDocument>(
            IMongoQuery query,
            double x,
            double y,
            int limit,
            IMongoGeoNearOptions options);

        /// <summary>
        /// Runs a GeoNear command on this collection.
        /// </summary>
        /// <param name="documentType">The type to deserialize the documents as.</param>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="x">The x coordinate of the starting location.</param>
        /// <param name="y">The y coordinate of the starting location.</param>
        /// <param name="limit">The maximum number of results returned.</param>
        /// <returns>A <see cref="GeoNearResult{TDocument}"/>.</returns>
        GeoNearResult GeoNearAs(Type documentType, IMongoQuery query, double x, double y, int limit);

        /// <summary>
        /// Runs a GeoNear command on this collection.
        /// </summary>
        /// <param name="documentType">The type to deserialize the documents as.</param>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="x">The x coordinate of the starting location.</param>
        /// <param name="y">The y coordinate of the starting location.</param>
        /// <param name="limit">The maximum number of results returned.</param>
        /// <param name="options">The GeoNear command options (usually a GeoNearOptionsDocument or constructed using the GeoNearOptions builder).</param>
        /// <returns>A <see cref="GeoNearResult{TDocument}"/>.</returns>
        GeoNearResult GeoNearAs(
            Type documentType,
            IMongoQuery query,
            double x,
            double y,
            int limit,
            IMongoGeoNearOptions options);

        /// <summary>
        /// Gets the indexes for this collection.
        /// </summary>
        /// <returns>A list of BsonDocuments that describe the indexes.</returns>
        GetIndexesResult GetIndexes();

        /// <summary>
        /// Gets the stats for this collection.
        /// </summary>
        /// <returns>The stats for this collection as a <see cref="CollectionStatsResult"/>.</returns>
        CollectionStatsResult GetStats();

        /// <summary>
        /// Gets the total data size for this collection (data + indexes).
        /// </summary>
        /// <returns>The total data size.</returns>
        long GetTotalDataSize();

        /// <summary>
        /// Gets the total storage size for this collection (data + indexes + overhead).
        /// </summary>
        /// <returns>The total storage size.</returns>
        long GetTotalStorageSize();

        /// <summary>
        /// Runs the group command on this collection.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="keyFunction">A JavaScript function that returns the key value to group on.</param>
        /// <param name="initial">Initial value passed to the reduce function for each group.</param>
        /// <param name="reduce">A JavaScript function that is called for each matching document in a group.</param>
        /// <param name="finalize">A JavaScript function that is called at the end of the group command.</param>
        /// <returns>A list of results as BsonDocuments.</returns>
        IEnumerable<BsonDocument> Group(
            IMongoQuery query,
            BsonJavaScript keyFunction,
            BsonDocument initial,
            BsonJavaScript reduce,
            BsonJavaScript finalize);

        /// <summary>
        /// Runs the group command on this collection.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="keys">The names of the fields to group on.</param>
        /// <param name="initial">Initial value passed to the reduce function for each group.</param>
        /// <param name="reduce">A JavaScript function that is called for each matching document in a group.</param>
        /// <param name="finalize">A JavaScript function that is called at the end of the group command.</param>
        /// <returns>A list of results as BsonDocuments.</returns>
        IEnumerable<BsonDocument> Group(
            IMongoQuery query,
            IMongoGroupBy keys,
            BsonDocument initial,
            BsonJavaScript reduce,
            BsonJavaScript finalize);

        /// <summary>
        /// Runs the group command on this collection.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="key">The name of the field to group on.</param>
        /// <param name="initial">Initial value passed to the reduce function for each group.</param>
        /// <param name="reduce">A JavaScript function that is called for each matching document in a group.</param>
        /// <param name="finalize">A JavaScript function that is called at the end of the group command.</param>
        /// <returns>A list of results as BsonDocuments.</returns>
        IEnumerable<BsonDocument> Group(
            IMongoQuery query,
            string key,
            BsonDocument initial,
            BsonJavaScript reduce,
            BsonJavaScript finalize);

        /// <summary>
        /// Tests whether an index exists.
        /// </summary>
        /// <param name="keys">The indexed fields (usually an IndexKeysDocument or constructed using the IndexKeys builder).</param>
        /// <returns>True if the index exists.</returns>
        bool IndexExists(IMongoIndexKeys keys);

        /// <summary>
        /// Tests whether an index exists.
        /// </summary>
        /// <param name="keyNames">The names of the fields in the index.</param>
        /// <returns>True if the index exists.</returns>
        bool IndexExists(params string[] keyNames);

        /// <summary>
        /// Tests whether an index exists.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <returns>True if the index exists.</returns>
        bool IndexExistsByName(string indexName);

        /// <summary>
        /// Inserts a document into this collection (see also InsertBatch to insert multiple documents at once).
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the document to insert.</typeparam>
        /// <param name="document">The document to insert.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        SafeModeResult Insert<TNominalType>(TNominalType document);

        /// <summary>
        /// Inserts a document into this collection (see also InsertBatch to insert multiple documents at once).
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the document to insert.</typeparam>
        /// <param name="document">The document to insert.</param>
        /// <param name="options">The options to use for this Insert.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        SafeModeResult Insert<TNominalType>(TNominalType document, MongoInsertOptions options);

        /// <summary>
        /// Inserts a document into this collection (see also InsertBatch to insert multiple documents at once).
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the document to insert.</typeparam>
        /// <param name="document">The document to insert.</param>
        /// <param name="safeMode">The SafeMode to use for this Insert.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        SafeModeResult Insert<TNominalType>(TNominalType document, SafeMode safeMode);

        /// <summary>
        /// Inserts a document into this collection (see also InsertBatch to insert multiple documents at once).
        /// </summary>
        /// <param name="nominalType">The nominal type of the document to insert.</param>
        /// <param name="document">The document to insert.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        SafeModeResult Insert(Type nominalType, object document);

        /// <summary>
        /// Inserts a document into this collection (see also InsertBatch to insert multiple documents at once).
        /// </summary>
        /// <param name="nominalType">The nominal type of the document to insert.</param>
        /// <param name="document">The document to insert.</param>
        /// <param name="options">The options to use for this Insert.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        SafeModeResult Insert(Type nominalType, object document, MongoInsertOptions options);

        /// <summary>
        /// Inserts a document into this collection (see also InsertBatch to insert multiple documents at once).
        /// </summary>
        /// <param name="nominalType">The nominal type of the document to insert.</param>
        /// <param name="document">The document to insert.</param>
        /// <param name="safeMode">The SafeMode to use for this Insert.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        SafeModeResult Insert(Type nominalType, object document, SafeMode safeMode);

        /// <summary>
        /// Inserts multiple documents at once into this collection (see also Insert to insert a single document).
        /// </summary>
        /// <typeparam name="TNominalType">The type of the documents to insert.</typeparam>
        /// <param name="documents">The documents to insert.</param>
        /// <returns>A list of SafeModeResults (or null if SafeMode is not being used).</returns>
        IEnumerable<SafeModeResult> InsertBatch<TNominalType>(IEnumerable<TNominalType> documents);

        /// <summary>
        /// Inserts multiple documents at once into this collection (see also Insert to insert a single document).
        /// </summary>
        /// <typeparam name="TNominalType">The type of the documents to insert.</typeparam>
        /// <param name="documents">The documents to insert.</param>
        /// <param name="options">The options to use for this Insert.</param>
        /// <returns>A list of SafeModeResults (or null if SafeMode is not being used).</returns>
        IEnumerable<SafeModeResult> InsertBatch<TNominalType>(
            IEnumerable<TNominalType> documents,
            MongoInsertOptions options);

        /// <summary>
        /// Inserts multiple documents at once into this collection (see also Insert to insert a single document).
        /// </summary>
        /// <typeparam name="TNominalType">The type of the documents to insert.</typeparam>
        /// <param name="documents">The documents to insert.</param>
        /// <param name="safeMode">The SafeMode to use for this Insert.</param>
        /// <returns>A list of SafeModeResults (or null if SafeMode is not being used).</returns>
        IEnumerable<SafeModeResult> InsertBatch<TNominalType>(
            IEnumerable<TNominalType> documents,
            SafeMode safeMode);

        /// <summary>
        /// Inserts multiple documents at once into this collection (see also Insert to insert a single document).
        /// </summary>
        /// <param name="nominalType">The nominal type of the documents to insert.</param>
        /// <param name="documents">The documents to insert.</param>
        /// <returns>A list of SafeModeResults (or null if SafeMode is not being used).</returns>
        IEnumerable<SafeModeResult> InsertBatch(Type nominalType, IEnumerable documents);

        /// <summary>
        /// Inserts multiple documents at once into this collection (see also Insert to insert a single document).
        /// </summary>
        /// <param name="nominalType">The nominal type of the documents to insert.</param>
        /// <param name="documents">The documents to insert.</param>
        /// <param name="safeMode">The SafeMode to use for this Insert.</param>
        /// <returns>A list of SafeModeResults (or null if SafeMode is not being used).</returns>
        IEnumerable<SafeModeResult> InsertBatch(
            Type nominalType,
            IEnumerable documents,
            SafeMode safeMode);

        /// <summary>
        /// Inserts multiple documents at once into this collection (see also Insert to insert a single document).
        /// </summary>
        /// <param name="nominalType">The nominal type of the documents to insert.</param>
        /// <param name="documents">The documents to insert.</param>
        /// <param name="options">The options to use for this Insert.</param>
        /// <returns>A list of SafeModeResults (or null if SafeMode is not being used).</returns>
        IEnumerable<SafeModeResult> InsertBatch(
            Type nominalType,
            IEnumerable documents,
            MongoInsertOptions options);

        /// <summary>
        /// Tests whether this collection is capped.
        /// </summary>
        /// <returns>True if this collection is capped.</returns>
        bool IsCapped();

        /// <summary>
        /// Runs a Map/Reduce command on this collection.
        /// </summary>
        /// <param name="map">A JavaScript function called for each document.</param>
        /// <param name="reduce">A JavaScript function called on the values emitted by the map function.</param>
        /// <param name="options">Options for this map/reduce command (see <see cref="MapReduceOptionsDocument"/>, <see cref="MapReduceOptionsWrapper"/> and the <see cref="MapReduceOptions"/> builder).</param>
        /// <returns>A <see cref="MapReduceResult"/>.</returns>
        MapReduceResult MapReduce(
            BsonJavaScript map,
            BsonJavaScript reduce,
            IMongoMapReduceOptions options);

        /// <summary>
        /// Runs a Map/Reduce command on document in this collection that match a query.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="map">A JavaScript function called for each document.</param>
        /// <param name="reduce">A JavaScript function called on the values emitted by the map function.</param>
        /// <param name="options">Options for this map/reduce command (see <see cref="MapReduceOptionsDocument"/>, <see cref="MapReduceOptionsWrapper"/> and the <see cref="MapReduceOptions"/> builder).</param>
        /// <returns>A <see cref="MapReduceResult"/>.</returns>
        MapReduceResult MapReduce(
            IMongoQuery query,
            BsonJavaScript map,
            BsonJavaScript reduce,
            IMongoMapReduceOptions options);

        /// <summary>
        /// Runs a Map/Reduce command on document in this collection that match a query.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="map">A JavaScript function called for each document.</param>
        /// <param name="reduce">A JavaScript function called on the values emitted by the map function.</param>
        /// <returns>A <see cref="MapReduceResult"/>.</returns>
        MapReduceResult MapReduce(IMongoQuery query, BsonJavaScript map, BsonJavaScript reduce);

        /// <summary>
        /// Runs a Map/Reduce command on this collection.
        /// </summary>
        /// <param name="map">A JavaScript function called for each document.</param>
        /// <param name="reduce">A JavaScript function called on the values emitted by the map function.</param>
        /// <returns>A <see cref="MapReduceResult"/>.</returns>
        MapReduceResult MapReduce(BsonJavaScript map, BsonJavaScript reduce);

        /// <summary>
        /// Runs the ReIndex command on this collection.
        /// </summary>
        /// <returns>A CommandResult.</returns>
        CommandResult ReIndex();

        /// <summary>
        /// Removes documents from this collection that match a query.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        SafeModeResult Remove(IMongoQuery query);

        /// <summary>
        /// Removes documents from this collection that match a query.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="safeMode">The SafeMode to use for this operation.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        SafeModeResult Remove(IMongoQuery query, SafeMode safeMode);

        /// <summary>
        /// Removes documents from this collection that match a query.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="flags">The flags for this Remove (see <see cref="RemoveFlags"/>).</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        SafeModeResult Remove(IMongoQuery query, RemoveFlags flags);

        /// <summary>
        /// Removes documents from this collection that match a query.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="flags">The flags for this Remove (see <see cref="RemoveFlags"/>).</param>
        /// <param name="safeMode">The SafeMode to use for this operation.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        SafeModeResult Remove(IMongoQuery query, RemoveFlags flags, SafeMode safeMode);

        /// <summary>
        /// Removes all documents from this collection (see also <see cref="MongoCollection.Drop"/>).
        /// </summary>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        SafeModeResult RemoveAll();

        /// <summary>
        /// Removes all documents from this collection (see also <see cref="MongoCollection.Drop"/>).
        /// </summary>
        /// <param name="safeMode">The SafeMode to use for this operation.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        SafeModeResult RemoveAll(SafeMode safeMode);

        /// <summary>
        /// Removes all entries for this collection in the index cache used by EnsureIndex. Call this method
        /// when you know (or suspect) that a process other than this one may have dropped one or
        /// more indexes.
        /// </summary>
        void ResetIndexCache();

        /// <summary>
        /// Saves a document to this collection. The document must have an identifiable Id field. Based on the value
        /// of the Id field Save will perform either an Insert or an Update.
        /// </summary>
        /// <typeparam name="TNominalType">The type of the document to save.</typeparam>
        /// <param name="document">The document to save.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        SafeModeResult Save<TNominalType>(TNominalType document);

        /// <summary>
        /// Saves a document to this collection. The document must have an identifiable Id field. Based on the value
        /// of the Id field Save will perform either an Insert or an Update.
        /// </summary>
        /// <typeparam name="TNominalType">The type of the document to save.</typeparam>
        /// <param name="document">The document to save.</param>
        /// <param name="options">The options to use for this Save.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        SafeModeResult Save<TNominalType>(TNominalType document, MongoInsertOptions options);

        /// <summary>
        /// Saves a document to this collection. The document must have an identifiable Id field. Based on the value
        /// of the Id field Save will perform either an Insert or an Update.
        /// </summary>
        /// <typeparam name="TNominalType">The type of the document to save.</typeparam>
        /// <param name="document">The document to save.</param>
        /// <param name="safeMode">The SafeMode to use for this operation.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        SafeModeResult Save<TNominalType>(TNominalType document, SafeMode safeMode);

        /// <summary>
        /// Saves a document to this collection. The document must have an identifiable Id field. Based on the value
        /// of the Id field Save will perform either an Insert or an Update.
        /// </summary>
        /// <param name="nominalType">The type of the document to save.</param>
        /// <param name="document">The document to save.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        SafeModeResult Save(Type nominalType, object document);

        /// <summary>
        /// Saves a document to this collection. The document must have an identifiable Id field. Based on the value
        /// of the Id field Save will perform either an Insert or an Update.
        /// </summary>
        /// <param name="nominalType">The type of the document to save.</param>
        /// <param name="document">The document to save.</param>
        /// <param name="options">The options to use for this Save.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        SafeModeResult Save(Type nominalType, object document, MongoInsertOptions options);

        /// <summary>
        /// Saves a document to this collection. The document must have an identifiable Id field. Based on the value
        /// of the Id field Save will perform either an Insert or an Update.
        /// </summary>
        /// <param name="nominalType">The type of the document to save.</param>
        /// <param name="document">The document to save.</param>
        /// <param name="safeMode">The SafeMode to use for this operation.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        SafeModeResult Save(Type nominalType, object document, SafeMode safeMode);

        /// <summary>
        /// Gets a canonical string representation for this database.
        /// </summary>
        /// <returns>A canonical string representation for this database.</returns>
        string ToString();

        /// <summary>
        /// Updates one matching document in this collection.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="update">The update to perform on the matching document.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        SafeModeResult Update(IMongoQuery query, IMongoUpdate update);

        /// <summary>
        /// Updates one or more matching documents in this collection (for multiple updates use UpdateFlags.Multi).
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="update">The update to perform on the matching document.</param>
        /// <param name="options">The update options.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        SafeModeResult Update(IMongoQuery query, IMongoUpdate update, MongoUpdateOptions options);

        /// <summary>
        /// Updates one matching document in this collection.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="update">The update to perform on the matching document.</param>
        /// <param name="safeMode">The SafeMode to use for this operation.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        SafeModeResult Update(IMongoQuery query, IMongoUpdate update, SafeMode safeMode);

        /// <summary>
        /// Updates one or more matching documents in this collection (for multiple updates use UpdateFlags.Multi).
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="update">The update to perform on the matching document.</param>
        /// <param name="flags">The flags for this Update.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        SafeModeResult Update(IMongoQuery query, IMongoUpdate update, UpdateFlags flags);

        /// <summary>
        /// Updates one or more matching documents in this collection (for multiple updates use UpdateFlags.Multi).
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="update">The update to perform on the matching document.</param>
        /// <param name="flags">The flags for this Update.</param>
        /// <param name="safeMode">The SafeMode to use for this operation.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        SafeModeResult Update(
            IMongoQuery query,
            IMongoUpdate update,
            UpdateFlags flags,
            SafeMode safeMode);

        /// <summary>
        /// Validates the integrity of this collection.
        /// </summary>
        /// <returns>A <see cref="ValidateCollectionResult"/>.</returns>
        ValidateCollectionResult Validate();
    }

    public interface IMongoCollection<TDocument> : IMongoCollection
    {
        /// <summary>
        /// Returns a cursor that can be used to find all documents in this collection that match the query as TDefaultDocuments.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>A <see cref="MongoCursor{TDocument}"/>.</returns>
        IMongoCursor<TDocument> Find(IMongoQuery query);

        /// <summary>
        /// Returns a cursor that can be used to find all documents in this collection as TDefaultDocuments.
        /// </summary>
        /// <returns>A <see cref="MongoCursor{TDocument}"/>.</returns>
        IMongoCursor<TDocument> FindAll();

        /// <summary>
        /// Returns a cursor that can be used to find one document in this collection as a TDefaultDocument.
        /// </summary>
        /// <returns>A TDefaultDocument (or null if not found).</returns>
        TDocument FindOne();

        /// <summary>
        /// Returns a cursor that can be used to find one document in this collection that matches a query as a TDefaultDocument.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>A TDefaultDocument (or null if not found).</returns>
        TDocument FindOne(IMongoQuery query);

        /// <summary>
        /// Returns a cursor that can be used to find one document in this collection by its _id value as a TDefaultDocument.
        /// </summary>
        /// <param name="id">The id of the document.</param>
        /// <returns>A TDefaultDocument (or null if not found).</returns>
        TDocument FindOneById(BsonValue id);

        /// <summary>
        /// Runs a geoHaystack search command on this collection.
        /// </summary>
        /// <param name="x">The x coordinate of the starting location.</param>
        /// <param name="y">The y coordinate of the starting location.</param>
        /// <param name="options">The options for the geoHaystack search (null if none).</param>
        /// <returns>A <see cref="GeoHaystackSearchResult{TDocument}"/>.</returns>
        GeoHaystackSearchResult<TDocument> GeoHaystackSearch(
            double x,
            double y,
            IMongoGeoHaystackSearchOptions options);

        /// <summary>
        /// Runs a GeoNear command on this collection.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="x">The x coordinate of the starting location.</param>
        /// <param name="y">The y coordinate of the starting location.</param>
        /// <param name="limit">The maximum number of results returned.</param>
        /// <returns>A <see cref="GeoNearResult{TDefaultDocument}"/>.</returns>
        GeoNearResult<TDocument> GeoNear(IMongoQuery query, double x, double y, int limit);

        /// <summary>
        /// Runs a GeoNear command on this collection.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="x">The x coordinate of the starting location.</param>
        /// <param name="y">The y coordinate of the starting location.</param>
        /// <param name="limit">The maximum number of results returned.</param>
        /// <param name="options">Options for the GeoNear command (see <see cref="GeoNearOptionsDocument"/>, <see cref="GeoNearOptionsWrapper"/>, and the <see cref="GeoNearOptions"/> builder).</param>
        /// <returns>A <see cref="GeoNearResult{TDefaultDocument}"/>.</returns>
        GeoNearResult<TDocument> GeoNear(
            IMongoQuery query,
            double x,
            double y,
            int limit,
            IMongoGeoNearOptions options);

        /// <summary>
        /// Inserts a document into this collection (see also InsertBatch to insert multiple documents at once).
        /// </summary>
        /// <param name="document">The document to insert.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        SafeModeResult Insert(TDocument document);

        /// <summary>
        /// Inserts a document into this collection (see also InsertBatch to insert multiple documents at once).
        /// </summary>
        /// <param name="document">The document to insert.</param>
        /// <param name="options">The options to use for this Insert.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        SafeModeResult Insert(TDocument document, MongoInsertOptions options);

        /// <summary>
        /// Inserts a document into this collection (see also InsertBatch to insert multiple documents at once).
        /// </summary>
        /// <param name="document">The document to insert.</param>
        /// <param name="safeMode">The SafeMode to use for this Insert.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        SafeModeResult Insert(TDocument document, SafeMode safeMode);

        /// <summary>
        /// Inserts multiple documents at once into this collection (see also Insert to insert a single document).
        /// </summary>
        /// <param name="documents">The documents to insert.</param>
        /// <returns>A list of SafeModeResults (or null if SafeMode is not being used).</returns>
        IEnumerable<SafeModeResult> InsertBatch(IEnumerable<TDocument> documents);

        /// <summary>
        /// Inserts multiple documents at once into this collection (see also Insert to insert a single document).
        /// </summary>
        /// <param name="documents">The documents to insert.</param>
        /// <param name="options">The options to use for this Insert.</param>
        /// <returns>A list of SafeModeResults (or null if SafeMode is not being used).</returns>
        IEnumerable<SafeModeResult> InsertBatch(
            IEnumerable<TDocument> documents,
            MongoInsertOptions options);

        /// <summary>
        /// Inserts multiple documents at once into this collection (see also Insert to insert a single document).
        /// </summary>
        /// <param name="documents">The documents to insert.</param>
        /// <param name="safeMode">The SafeMode to use for this Insert.</param>
        /// <returns>A list of SafeModeResults (or null if SafeMode is not being used).</returns>
        IEnumerable<SafeModeResult> InsertBatch(
            IEnumerable<TDocument> documents,
            SafeMode safeMode);

        /// <summary>
        /// Saves a document to this collection. The document must have an identifiable Id field. Based on the value
        /// of the Id field Save will perform either an Insert or an Update.
        /// </summary>
        /// <param name="document">The document to save.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        SafeModeResult Save(TDocument document);

        /// <summary>
        /// Saves a document to this collection. The document must have an identifiable Id field. Based on the value
        /// of the Id field Save will perform either an Insert or an Update.
        /// </summary>
        /// <param name="document">The document to save.</param>
        /// <param name="options">The options to use for this Save.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        SafeModeResult Save(TDocument document, MongoInsertOptions options);

        /// <summary>
        /// Saves a document to this collection. The document must have an identifiable Id field. Based on the value
        /// of the Id field Save will perform either an Insert or an Update.
        /// </summary>
        /// <param name="document">The document to save.</param>
        /// <param name="safeMode">The SafeMode to use for this operation.</param>
        /// <returns>A SafeModeResult (or null if SafeMode is not being used).</returns>
        SafeModeResult Save(TDocument document, SafeMode safeMode);
    }
}