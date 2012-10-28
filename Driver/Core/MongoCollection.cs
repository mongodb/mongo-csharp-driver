/* Copyright 2010-2012 10gen Inc.
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
using System.IO;
using System.Linq;
using System.Text;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Internal;
using MongoDB.Driver.Wrappers;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a MongoDB collection and the settings used to access it. This class is thread-safe.
    /// </summary>
    public abstract class MongoCollection
    {
        // private fields
        private MongoServer _server;
        private MongoDatabase _database;
        private MongoCollectionSettings _settings;
        private string _name;
        private MongoCollection<BsonDocument> _commandCollection; // used to run commands with this collection's settings

        // constructors
        /// <summary>
        /// Protected constructor for abstract base class.
        /// </summary>
        /// <param name="database">The database that contains this collection.</param>
        /// <param name="settings">The settings to use to access this collection.</param>
        protected MongoCollection(MongoDatabase database, MongoCollectionSettings settings)
        {
            if (database == null)
            {
                throw new ArgumentNullException("database");
            }
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }
            string message;
            if (!database.IsCollectionNameValid(settings.CollectionName, out message))
            {
                throw new ArgumentOutOfRangeException("settings", message);
            }

            _server = database.Server;
            _database = database;
            _settings = settings.FrozenCopy();
            _name = settings.CollectionName;

            if (_name != "$cmd")
            {
                var commandCollectionSettings = new MongoCollectionSettings<BsonDocument>(_database, "$cmd")
                {
                    AssignIdOnInsert = false,
                    GuidRepresentation = _settings.GuidRepresentation,
                    ReadPreference = _settings.ReadPreference,
                    WriteConcern = _settings.WriteConcern
                };
                _commandCollection = _database.GetCollection(commandCollectionSettings);
            }
        }

        // public properties
        /// <summary>
        /// Gets the database that contains this collection.
        /// </summary>
        public virtual MongoDatabase Database
        {
            get { return _database; }
        }

        /// <summary>
        /// Gets the fully qualified name of this collection.
        /// </summary>
        public virtual string FullName
        {
            get { return _database.Name + "." + _name; }
        }

        /// <summary>
        /// Gets the name of this collection.
        /// </summary>
        public virtual string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Gets the settings being used to access this collection.
        /// </summary>
        public virtual MongoCollectionSettings Settings
        {
            get { return _settings; }
        }

        // public methods
        /// <summary>
        /// Runs an aggregation framework command.
        /// </summary>
        /// <param name="operations">The pipeline operations.</param>
        /// <returns>An AggregateResult.</returns>
        public virtual AggregateResult Aggregate(IEnumerable<BsonDocument> operations)
        {
            var pipeline = new BsonArray();
            foreach (var operation in operations)
            {
                pipeline.Add(operation);
            }

            var aggregateCommand = new CommandDocument
            {
                { "aggregate", _name },
                { "pipeline", pipeline }
            };
            return RunCommandAs<AggregateResult>(aggregateCommand);
        }

        /// <summary>
        /// Runs an aggregation framework command.
        /// </summary>
        /// <param name="operations">The pipeline operations.</param>
        /// <returns>An AggregateResult.</returns>
        public virtual AggregateResult Aggregate(params BsonDocument[] operations)
        {
            return Aggregate((IEnumerable<BsonDocument>) operations);
        }

        /// <summary>
        /// Counts the number of documents in this collection.
        /// </summary>
        /// <returns>The number of documents in this collection.</returns>
        public virtual long Count()
        {
            return Count(Query.Null);
        }

        /// <summary>
        /// Counts the number of documents in this collection that match a query.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>The number of documents in this collection that match the query.</returns>
        public virtual long Count(IMongoQuery query)
        {
            var command = new CommandDocument
            {
                { "count", _name },
                { "query", BsonDocumentWrapper.Create(query), query != null } // query is optional
            };
            var result = RunCommand(command);
            return result.Response["n"].ToInt64();
        }

        /// <summary>
        /// Creates an index for this collection.
        /// </summary>
        /// <param name="keys">The indexed fields (usually an IndexKeysDocument or constructed using the IndexKeys builder).</param>
        /// <param name="options">The index options(usually an IndexOptionsDocument or created using the IndexOption builder).</param>
        /// <returns>A WriteConcernResult.</returns>
        public virtual WriteConcernResult CreateIndex(IMongoIndexKeys keys, IMongoIndexOptions options)
        {
            var keysDocument = keys.ToBsonDocument();
            var optionsDocument = options.ToBsonDocument();
            var indexes = _database.GetCollection("system.indexes");
            var indexName = GetIndexName(keysDocument, optionsDocument);
            var index = new BsonDocument
            {
                { "name", indexName },
                { "ns", FullName },
                { "key", keysDocument }
            };
            index.Merge(optionsDocument);
            var insertOptions = new MongoInsertOptions
            {
                CheckElementNames = false,
                WriteConcern = WriteConcern.Errors
            };
            var result = indexes.Insert(index, insertOptions);
            return result;
        }

        /// <summary>
        /// Creates an index for this collection.
        /// </summary>
        /// <param name="keys">The indexed fields (usually an IndexKeysDocument or constructed using the IndexKeys builder).</param>
        /// <returns>A WriteConcernResult.</returns>
        public virtual WriteConcernResult CreateIndex(IMongoIndexKeys keys)
        {
            return CreateIndex(keys, IndexOptions.Null);
        }

        /// <summary>
        /// Creates an index for this collection.
        /// </summary>
        /// <param name="keyNames">The names of the indexed fields.</param>
        /// <returns>A WriteConcernResult.</returns>
        public virtual WriteConcernResult CreateIndex(params string[] keyNames)
        {
            return CreateIndex(IndexKeys.Ascending(keyNames));
        }

        /// <summary>
        /// Returns the distinct values for a given field.
        /// </summary>
        /// <param name="key">The key of the field.</param>
        /// <returns>The distint values of the field.</returns>
        public virtual IEnumerable<BsonValue> Distinct(string key)
        {
            return Distinct(key, Query.Null);
        }

        /// <summary>
        /// Returns the distinct values for a given field for documents that match a query.
        /// </summary>
        /// <param name="key">The key of the field.</param>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>The distint values of the field.</returns>
        public virtual IEnumerable<BsonValue> Distinct(string key, IMongoQuery query)
        {
            var command = new CommandDocument
            {
                { "distinct", _name },
                { "key", key },
                { "query", BsonDocumentWrapper.Create(query), query != null } // query is optional
            };
            var result = RunCommand(command);
            return result.Response["values"].AsBsonArray;
        }

        /// <summary>
        /// Drops this collection.
        /// </summary>
        /// <returns>A CommandResult.</returns>
        public virtual CommandResult Drop()
        {
            return _database.DropCollection(_name);
        }

        /// <summary>
        /// Drops all indexes on this collection.
        /// </summary>
        /// <returns>A <see cref="CommandResult"/>.</returns>
        public virtual CommandResult DropAllIndexes()
        {
            return DropIndexByName("*");
        }

        /// <summary>
        /// Drops an index on this collection.
        /// </summary>
        /// <param name="keys">The indexed fields (usually an IndexKeysDocument or constructed using the IndexKeys builder).</param>
        /// <returns>A <see cref="CommandResult"/>.</returns>
        public virtual CommandResult DropIndex(IMongoIndexKeys keys)
        {
            string indexName = GetIndexName(keys.ToBsonDocument(), null);
            return DropIndexByName(indexName);
        }

        /// <summary>
        /// Drops an index on this collection.
        /// </summary>
        /// <param name="keyNames">The names of the indexed fields.</param>
        /// <returns>A <see cref="CommandResult"/>.</returns>
        public virtual CommandResult DropIndex(params string[] keyNames)
        {
            string indexName = GetIndexName(keyNames);
            return DropIndexByName(indexName);
        }

        /// <summary>
        /// Drops an index on this collection.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <returns>A <see cref="CommandResult"/>.</returns>
        public virtual CommandResult DropIndexByName(string indexName)
        {
            // remove from cache first (even if command ends up failing)
            if (indexName == "*")
            {
                _server.IndexCache.Reset(this);
            }
            else
            {
                _server.IndexCache.Remove(this, indexName);
            }
            var command = new CommandDocument
            {
                { "deleteIndexes", _name }, // not FullName
                { "index", indexName }
            };
            try
            {
                return RunCommand(command);
            }
            catch (MongoCommandException ex)
            {
                if (ex.CommandResult.ErrorMessage == "ns not found")
                {
                    return ex.CommandResult;
                }
                throw;
            }
        }

        /// <summary>
        /// Ensures that the desired index exists and creates it if it does not.
        /// </summary>
        /// <param name="keys">The indexed fields (usually an IndexKeysDocument or constructed using the IndexKeys builder).</param>
        /// <param name="options">The index options(usually an IndexOptionsDocument or created using the IndexOption builder).</param>
        public virtual void EnsureIndex(IMongoIndexKeys keys, IMongoIndexOptions options)
        {
            var keysDocument = keys.ToBsonDocument();
            var optionsDocument = options.ToBsonDocument();
            var indexName = GetIndexName(keysDocument, optionsDocument);
            if (!_server.IndexCache.Contains(this, indexName))
            {
                CreateIndex(keys, options);
                _server.IndexCache.Add(this, indexName);
            }
        }

        /// <summary>
        /// Ensures that the desired index exists and creates it if it does not.
        /// </summary>
        /// <param name="keys">The indexed fields (usually an IndexKeysDocument or constructed using the IndexKeys builder).</param>
        public virtual void EnsureIndex(IMongoIndexKeys keys)
        {
            EnsureIndex(keys, IndexOptions.Null);
        }

        /// <summary>
        /// Ensures that the desired index exists and creates it if it does not.
        /// </summary>
        /// <param name="keyNames">The names of the indexed fields.</param>
        public virtual void EnsureIndex(params string[] keyNames)
        {
            string indexName = GetIndexName(keyNames);
            if (!_server.IndexCache.Contains(this, indexName))
            {
                CreateIndex(IndexKeys.Ascending(keyNames), IndexOptions.SetName(indexName));
                _server.IndexCache.Add(this, indexName);
            }
        }

        /// <summary>
        /// Tests whether this collection exists.
        /// </summary>
        /// <returns>True if this collection exists.</returns>
        public virtual bool Exists()
        {
            return _database.CollectionExists(_name);
        }

        /// <summary>
        /// Returns a cursor that can be used to find all documents in this collection as TDocuments.
        /// </summary>
        /// <typeparam name="TDocument">The nominal type of the documents.</typeparam>
        /// <returns>A <see cref="MongoCursor{TDocument}"/>.</returns>
        public virtual MongoCursor<TDocument> FindAllAs<TDocument>()
        {
            return FindAs<TDocument>(Query.Null);
        }

        /// <summary>
        /// Returns a cursor that can be used to find all documents in this collection as TDocuments.
        /// </summary>
        /// <param name="documentType">The nominal type of the documents.</param>
        /// <returns>A <see cref="MongoCursor{TDocument}"/>.</returns>
        public virtual MongoCursor FindAllAs(Type documentType)
        {
            return FindAs(documentType, Query.Null);
        }

        /// <summary>
        /// Finds one matching document using the query and sortBy parameters and applies the specified update to it.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="sortBy">The sort order to select one of the matching documents.</param>
        /// <param name="update">The update to apply to the matching document.</param>
        /// <returns>A <see cref="FindAndModifyResult"/>.</returns>
        public virtual FindAndModifyResult FindAndModify(IMongoQuery query, IMongoSortBy sortBy, IMongoUpdate update)
        {
            return FindAndModify(query, sortBy, update, false);
        }

        /// <summary>
        /// Finds one matching document using the query and sortBy parameters and applies the specified update to it.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="sortBy">The sort order to select one of the matching documents.</param>
        /// <param name="update">The update to apply to the matching document.</param>
        /// <param name="returnNew">Whether to return the new or old version of the modified document in the <see cref="FindAndModifyResult"/>.</param>
        /// <returns>A <see cref="FindAndModifyResult"/>.</returns>
        public virtual FindAndModifyResult FindAndModify(
            IMongoQuery query,
            IMongoSortBy sortBy,
            IMongoUpdate update,
            bool returnNew)
        {
            return FindAndModify(query, sortBy, update, returnNew, false);
        }

        /// <summary>
        /// Finds one matching document using the query and sortBy parameters and applies the specified update to it.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="sortBy">The sort order to select one of the matching documents.</param>
        /// <param name="update">The update to apply to the matching document.</param>
        /// <param name="returnNew">Whether to return the new or old version of the modified document in the <see cref="FindAndModifyResult"/>.</param>
        /// <param name="upsert">Whether to do an upsert if no matching document is found.</param>
        /// <returns>A <see cref="FindAndModifyResult"/>.</returns>
        public virtual FindAndModifyResult FindAndModify(
            IMongoQuery query,
            IMongoSortBy sortBy,
            IMongoUpdate update,
            bool returnNew,
            bool upsert)
        {
            return FindAndModify(query, sortBy, update, Fields.Null, returnNew, upsert);
        }

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
        public virtual FindAndModifyResult FindAndModify(
            IMongoQuery query,
            IMongoSortBy sortBy,
            IMongoUpdate update,
            IMongoFields fields,
            bool returnNew,
            bool upsert)
        {
            var command = new CommandDocument
            {
                { "findAndModify", _name },
                { "query", BsonDocumentWrapper.Create(query), query != null }, // query is optional
                { "sort", BsonDocumentWrapper.Create(sortBy), sortBy != null }, // sortBy is optional
                { "update", BsonDocumentWrapper.Create(update, true) }, // isUpdateDocument = true
                { "fields", BsonDocumentWrapper.Create(fields), fields != null }, // fields is optional
                { "new", true, returnNew },
                { "upsert", true, upsert}
            };
            try
            {
                return RunCommandAs<FindAndModifyResult>(command);
            }
            catch (MongoCommandException ex)
            {
                if (ex.CommandResult.ErrorMessage == "No matching object found")
                {
                    // create a new command result with what the server should have responded
                    var response = new BsonDocument
                    {
                        { "value", BsonNull.Value },
                        { "ok", true }
                    };
                    var result = new FindAndModifyResult();
                    result.Initialize(command, response);
                    return result;
                }
                throw;
            }
        }

        /// <summary>
        /// Finds one matching document using the query and sortBy parameters and removes it from this collection.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="sortBy">The sort order to select one of the matching documents.</param>
        /// <returns>A <see cref="FindAndModifyResult"/>.</returns>
        public virtual FindAndModifyResult FindAndRemove(IMongoQuery query, IMongoSortBy sortBy)
        {
            var command = new CommandDocument
            {
                { "findAndModify", _name },
                { "query", BsonDocumentWrapper.Create(query), query != null }, // query is optional
                { "sort", BsonDocumentWrapper.Create(sortBy), sortBy != null }, // sort is optional
                { "remove", true }
            };
            try
            {
                return RunCommandAs<FindAndModifyResult>(command);
            }
            catch (MongoCommandException ex)
            {
                if (ex.CommandResult.ErrorMessage == "No matching object found")
                {
                    // create a new command result with what the server should have responded
                    var response = new BsonDocument
                    {
                        { "value", BsonNull.Value },
                        { "ok", true }
                    };
                    var result = new FindAndModifyResult();
                    result.Initialize(command, response);
                    return result;
                }
                throw;
            }
        }

        /// <summary>
        /// Returns a cursor that can be used to find all documents in this collection that match the query as TDocuments.
        /// </summary>
        /// <typeparam name="TDocument">The type to deserialize the documents as.</typeparam>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>A <see cref="MongoCursor{TDocument}"/>.</returns>
        public virtual MongoCursor<TDocument> FindAs<TDocument>(IMongoQuery query)
        {
            return new MongoCursor<TDocument>(this, query, _settings.ReadPreference);
        }

        /// <summary>
        /// Returns a cursor that can be used to find all documents in this collection that match the query as TDocuments.
        /// </summary>
        /// <param name="documentType">The nominal type of the documents.</param>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>A <see cref="MongoCursor{TDocument}"/>.</returns>
        public virtual MongoCursor FindAs(Type documentType, IMongoQuery query)
        {
            return MongoCursor.Create(documentType, this, query, _settings.ReadPreference);
        }

        /// <summary>
        /// Returns a cursor that can be used to find one document in this collection as a TDocument.
        /// </summary>
        /// <typeparam name="TDocument">The type to deserialize the documents as.</typeparam>
        /// <returns>A TDocument (or null if not found).</returns>
        public virtual TDocument FindOneAs<TDocument>()
        {
            return FindAllAs<TDocument>().SetLimit(1).FirstOrDefault();
        }

        /// <summary>
        /// Returns a cursor that can be used to find one document in this collection that matches a query as a TDocument.
        /// </summary>
        /// <typeparam name="TDocument">The type to deserialize the documents as.</typeparam>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>A TDocument (or null if not found).</returns>
        public virtual TDocument FindOneAs<TDocument>(IMongoQuery query)
        {
            return FindAs<TDocument>(query).SetLimit(1).FirstOrDefault();
        }

        /// <summary>
        /// Returns a cursor that can be used to find one document in this collection as a TDocument.
        /// </summary>
        /// <param name="documentType">The nominal type of the documents.</param>
        /// <returns>A document (or null if not found).</returns>
        public virtual object FindOneAs(Type documentType)
        {
            return FindAllAs(documentType).SetLimit(1).OfType<object>().FirstOrDefault();
        }

        /// <summary>
        /// Returns a cursor that can be used to find one document in this collection that matches a query as a TDocument.
        /// </summary>
        /// <param name="documentType">The type to deserialize the documents as.</param>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>A TDocument (or null if not found).</returns>
        public virtual object FindOneAs(Type documentType, IMongoQuery query)
        {
            return FindAs(documentType, query).SetLimit(1).OfType<object>().FirstOrDefault();
        }

        /// <summary>
        /// Returns a cursor that can be used to find one document in this collection by its _id value as a TDocument.
        /// </summary>
        /// <typeparam name="TDocument">The nominal type of the document.</typeparam>
        /// <param name="id">The id of the document.</param>
        /// <returns>A TDocument (or null if not found).</returns>
        public virtual TDocument FindOneByIdAs<TDocument>(BsonValue id)
        {
            return FindOneAs<TDocument>(Query.EQ("_id", id));
        }

        /// <summary>
        /// Returns a cursor that can be used to find one document in this collection by its _id value as a TDocument.
        /// </summary>
        /// <param name="documentType">The nominal type of the document.</param>
        /// <param name="id">The id of the document.</param>
        /// <returns>A TDocument (or null if not found).</returns>
        public virtual object FindOneByIdAs(Type documentType, BsonValue id)
        {
            return FindOneAs(documentType, Query.EQ("_id", id));
        }

        /// <summary>
        /// Runs a geoHaystack search command on this collection.
        /// </summary>
        /// <typeparam name="TDocument">The type of the found documents.</typeparam>
        /// <param name="x">The x coordinate of the starting location.</param>
        /// <param name="y">The y coordinate of the starting location.</param>
        /// <param name="options">The options for the geoHaystack search (null if none).</param>
        /// <returns>A <see cref="GeoNearResult{TDocument}"/>.</returns>
        public virtual GeoHaystackSearchResult<TDocument> GeoHaystackSearchAs<TDocument>(
            double x,
            double y,
            IMongoGeoHaystackSearchOptions options)
        {
            return (GeoHaystackSearchResult<TDocument>)GeoHaystackSearchAs(typeof(TDocument), x, y, options);
        }

        /// <summary>
        /// Runs a geoHaystack search command on this collection.
        /// </summary>
        /// <param name="documentType">The type to deserialize the documents as.</param>
        /// <param name="x">The x coordinate of the starting location.</param>
        /// <param name="y">The y coordinate of the starting location.</param>
        /// <param name="options">The options for the geoHaystack search (null if none).</param>
        /// <returns>A <see cref="GeoNearResult{TDocument}"/>.</returns>
        public virtual GeoHaystackSearchResult GeoHaystackSearchAs(
            Type documentType,
            double x,
            double y,
            IMongoGeoHaystackSearchOptions options)
        {
            var command = new CommandDocument
            {
                { "geoSearch", _name },
                { "near", new BsonArray { x, y } }
            };
            command.Merge(options.ToBsonDocument());
            var geoHaystackSearchResultDefinition = typeof(GeoHaystackSearchResult<>);
            var geoHaystackSearchResultType = geoHaystackSearchResultDefinition.MakeGenericType(documentType);
            return (GeoHaystackSearchResult)RunCommandAs(geoHaystackSearchResultType, command);
        }

        /// <summary>
        /// Runs a GeoNear command on this collection.
        /// </summary>
        /// <typeparam name="TDocument">The type to deserialize the documents as.</typeparam>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="x">The x coordinate of the starting location.</param>
        /// <param name="y">The y coordinate of the starting location.</param>
        /// <param name="limit">The maximum number of results returned.</param>
        /// <returns>A <see cref="GeoNearResult{TDocument}"/>.</returns>
        public virtual GeoNearResult<TDocument> GeoNearAs<TDocument>(
            IMongoQuery query,
            double x,
            double y,
            int limit)
        {
            return GeoNearAs<TDocument>(query, x, y, limit, GeoNearOptions.Null);
        }

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
        public virtual GeoNearResult<TDocument> GeoNearAs<TDocument>(
            IMongoQuery query,
            double x,
            double y,
            int limit,
            IMongoGeoNearOptions options)
        {
            var command = new CommandDocument
            {
                { "geoNear", _name },
                { "near", new BsonArray { x, y } },
                { "num", limit },
                { "query", BsonDocumentWrapper.Create(query), query != null } // query is optional
            };
            command.Merge(options.ToBsonDocument());
            return RunCommandAs<GeoNearResult<TDocument>>(command);
        }

        /// <summary>
        /// Runs a GeoNear command on this collection.
        /// </summary>
        /// <param name="documentType">The type to deserialize the documents as.</param>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="x">The x coordinate of the starting location.</param>
        /// <param name="y">The y coordinate of the starting location.</param>
        /// <param name="limit">The maximum number of results returned.</param>
        /// <returns>A <see cref="GeoNearResult{TDocument}"/>.</returns>
        public virtual GeoNearResult GeoNearAs(Type documentType, IMongoQuery query, double x, double y, int limit)
        {
            return GeoNearAs(documentType, query, x, y, limit, GeoNearOptions.Null);
        }

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
        public virtual GeoNearResult GeoNearAs(
            Type documentType,
            IMongoQuery query,
            double x,
            double y,
            int limit,
            IMongoGeoNearOptions options)
        {
            var command = new CommandDocument
            {
                { "geoNear", _name },
                { "near", new BsonArray { x, y } },
                { "num", limit },
                { "query", BsonDocumentWrapper.Create(query), query != null } // query is optional
            };
            command.Merge(options.ToBsonDocument());
            var geoNearResultDefinition = typeof(GeoNearResult<>);
            var geoNearResultType = geoNearResultDefinition.MakeGenericType(documentType);
            return (GeoNearResult)RunCommandAs(geoNearResultType, command);
        }

        /// <summary>
        /// Gets the indexes for this collection.
        /// </summary>
        /// <returns>A list of BsonDocuments that describe the indexes.</returns>
        public virtual GetIndexesResult GetIndexes()
        {
            var indexes = _database.GetCollection("system.indexes");
            var query = Query.EQ("ns", FullName);
            return new GetIndexesResult(indexes.Find(query).ToArray()); // ToArray forces execution of the query
        }

        /// <summary>
        /// Gets the stats for this collection.
        /// </summary>
        /// <returns>The stats for this collection as a <see cref="CollectionStatsResult"/>.</returns>
        public virtual CollectionStatsResult GetStats()
        {
            var command = new CommandDocument("collstats", _name);
            return RunCommandAs<CollectionStatsResult>(command);
        }

        /// <summary>
        /// Gets the total data size for this collection (data + indexes).
        /// </summary>
        /// <returns>The total data size.</returns>
        public virtual long GetTotalDataSize()
        {
            var totalSize = GetStats().DataSize;
            foreach (var index in GetIndexes())
            {
                var indexCollectionName = string.Format("{0}.${1}", _name, index.Name);
                var indexCollection = _database.GetCollection(indexCollectionName);
                totalSize += indexCollection.GetStats().DataSize;
            }
            return totalSize;
        }

        /// <summary>
        /// Gets the total storage size for this collection (data + indexes + overhead).
        /// </summary>
        /// <returns>The total storage size.</returns>
        public virtual long GetTotalStorageSize()
        {
            var totalSize = GetStats().StorageSize;
            foreach (var index in GetIndexes())
            {
                var indexCollectionName = string.Format("{0}.${1}", _name, index.Name);
                var indexCollection = _database.GetCollection(indexCollectionName);
                totalSize += indexCollection.GetStats().StorageSize;
            }
            return totalSize;
        }

        /// <summary>
        /// Runs the group command on this collection.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="keyFunction">A JavaScript function that returns the key value to group on.</param>
        /// <param name="initial">Initial value passed to the reduce function for each group.</param>
        /// <param name="reduce">A JavaScript function that is called for each matching document in a group.</param>
        /// <param name="finalize">A JavaScript function that is called at the end of the group command.</param>
        /// <returns>A list of results as BsonDocuments.</returns>
        public virtual IEnumerable<BsonDocument> Group(
            IMongoQuery query,
            BsonJavaScript keyFunction,
            BsonDocument initial,
            BsonJavaScript reduce,
            BsonJavaScript finalize)
        {
            if (keyFunction == null)
            {
                throw new ArgumentNullException("keyFunction");
            }
            if (initial == null)
            {
                throw new ArgumentNullException("initial");
            }
            if (reduce == null)
            {
                throw new ArgumentNullException("reduce");
            }

            var command = new CommandDocument
            {
                {
                    "group", new BsonDocument
                    {
                        { "ns", _name },
                        { "condition", BsonDocumentWrapper.Create(query), query != null }, // condition is optional
                        { "$keyf", keyFunction },
                        { "initial", initial },
                        { "$reduce", reduce },
                        { "finalize", finalize, finalize != null } // finalize is optional
                    }
                }
            };
            var result = RunCommand(command);
            return result.Response["retval"].AsBsonArray.Values.Cast<BsonDocument>();
        }

        /// <summary>
        /// Runs the group command on this collection.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="keys">The names of the fields to group on.</param>
        /// <param name="initial">Initial value passed to the reduce function for each group.</param>
        /// <param name="reduce">A JavaScript function that is called for each matching document in a group.</param>
        /// <param name="finalize">A JavaScript function that is called at the end of the group command.</param>
        /// <returns>A list of results as BsonDocuments.</returns>
        public virtual IEnumerable<BsonDocument> Group(
            IMongoQuery query,
            IMongoGroupBy keys,
            BsonDocument initial,
            BsonJavaScript reduce,
            BsonJavaScript finalize)
        {
            if (keys == null)
            {
                throw new ArgumentNullException("keys");
            }
            if (initial == null)
            {
                throw new ArgumentNullException("initial");
            }
            if (reduce == null)
            {
                throw new ArgumentNullException("reduce");
            }

            var command = new CommandDocument
            {
                {
                    "group", new BsonDocument
                    {
                        { "ns", _name },
                        { "condition", BsonDocumentWrapper.Create(query), query != null }, // condition is optional
                        { "key", BsonDocumentWrapper.Create(keys) },
                        { "initial", initial },
                        { "$reduce", reduce },
                        { "finalize", finalize, finalize != null } // finalize is optional
                    }
                }
            };
            var result = RunCommand(command);
            return result.Response["retval"].AsBsonArray.Values.Cast<BsonDocument>();
        }

        /// <summary>
        /// Runs the group command on this collection.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="key">The name of the field to group on.</param>
        /// <param name="initial">Initial value passed to the reduce function for each group.</param>
        /// <param name="reduce">A JavaScript function that is called for each matching document in a group.</param>
        /// <param name="finalize">A JavaScript function that is called at the end of the group command.</param>
        /// <returns>A list of results as BsonDocuments.</returns>
        public virtual IEnumerable<BsonDocument> Group(
            IMongoQuery query,
            string key,
            BsonDocument initial,
            BsonJavaScript reduce,
            BsonJavaScript finalize)
        {
            return Group(query, GroupBy.Keys(key), initial, reduce, finalize);
        }

        /// <summary>
        /// Tests whether an index exists.
        /// </summary>
        /// <param name="keys">The indexed fields (usually an IndexKeysDocument or constructed using the IndexKeys builder).</param>
        /// <returns>True if the index exists.</returns>
        public virtual bool IndexExists(IMongoIndexKeys keys)
        {
            string indexName = GetIndexName(keys.ToBsonDocument(), null);
            return IndexExistsByName(indexName);
        }

        /// <summary>
        /// Tests whether an index exists.
        /// </summary>
        /// <param name="keyNames">The names of the fields in the index.</param>
        /// <returns>True if the index exists.</returns>
        public virtual bool IndexExists(params string[] keyNames)
        {
            string indexName = GetIndexName(keyNames);
            return IndexExistsByName(indexName);
        }

        /// <summary>
        /// Tests whether an index exists.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <returns>True if the index exists.</returns>
        public virtual bool IndexExistsByName(string indexName)
        {
            var indexes = _database.GetCollection("system.indexes");
            var query = Query.And(Query.EQ("name", indexName), Query.EQ("ns", FullName));
            return indexes.Count(query) != 0;
        }

        // WARNING: be VERY careful about adding any new overloads of Insert or InsertBatch (just don't do it!)
        // it's very easy for the compiler to end up inferring the wrong type for TDocument!
        // that's also why Insert and InsertBatch have to have different names

        /// <summary>
        /// Inserts a document into this collection (see also InsertBatch to insert multiple documents at once).
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the document to insert.</typeparam>
        /// <param name="document">The document to insert.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Insert<TNominalType>(TNominalType document)
        {
            return Insert(typeof(TNominalType), document);
        }

        /// <summary>
        /// Inserts a document into this collection (see also InsertBatch to insert multiple documents at once).
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the document to insert.</typeparam>
        /// <param name="document">The document to insert.</param>
        /// <param name="options">The options to use for this Insert.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Insert<TNominalType>(TNominalType document, MongoInsertOptions options)
        {
            return Insert(typeof(TNominalType), document, options);
        }

        /// <summary>
        /// Inserts a document into this collection (see also InsertBatch to insert multiple documents at once).
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the document to insert.</typeparam>
        /// <param name="document">The document to insert.</param>
        /// <param name="writeConcern">The write concern to use for this Insert.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Insert<TNominalType>(TNominalType document, WriteConcern writeConcern)
        {
            return Insert(typeof(TNominalType), document, writeConcern);
        }

        /// <summary>
        /// Inserts a document into this collection (see also InsertBatch to insert multiple documents at once).
        /// </summary>
        /// <param name="nominalType">The nominal type of the document to insert.</param>
        /// <param name="document">The document to insert.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Insert(Type nominalType, object document)
        {
            var options = new MongoInsertOptions();
            return Insert(nominalType, document, options);
        }

        /// <summary>
        /// Inserts a document into this collection (see also InsertBatch to insert multiple documents at once).
        /// </summary>
        /// <param name="nominalType">The nominal type of the document to insert.</param>
        /// <param name="document">The document to insert.</param>
        /// <param name="options">The options to use for this Insert.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Insert(Type nominalType, object document, MongoInsertOptions options)
        {
            if (document == null)
            {
                throw new ArgumentNullException("document");
            }
            var results = InsertBatch(nominalType, new object[] { document }, options);
            return (results == null) ? null : results.Single();
        }

        /// <summary>
        /// Inserts a document into this collection (see also InsertBatch to insert multiple documents at once).
        /// </summary>
        /// <param name="nominalType">The nominal type of the document to insert.</param>
        /// <param name="document">The document to insert.</param>
        /// <param name="writeConcern">The write concern to use for this Insert.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Insert(Type nominalType, object document, WriteConcern writeConcern)
        {
            var options = new MongoInsertOptions { WriteConcern = writeConcern};
            return Insert(nominalType, document, options);
        }

        /// <summary>
        /// Inserts multiple documents at once into this collection (see also Insert to insert a single document).
        /// </summary>
        /// <typeparam name="TNominalType">The type of the documents to insert.</typeparam>
        /// <param name="documents">The documents to insert.</param>
        /// <returns>A list of WriteConcernResults (or null if WriteConcern is disabled).</returns>
        public virtual IEnumerable<WriteConcernResult> InsertBatch<TNominalType>(IEnumerable<TNominalType> documents)
        {
            if (documents == null)
            {
                throw new ArgumentNullException("documents");
            }
            return InsertBatch(typeof(TNominalType), documents.Cast<object>());
        }

        /// <summary>
        /// Inserts multiple documents at once into this collection (see also Insert to insert a single document).
        /// </summary>
        /// <typeparam name="TNominalType">The type of the documents to insert.</typeparam>
        /// <param name="documents">The documents to insert.</param>
        /// <param name="options">The options to use for this Insert.</param>
        /// <returns>A list of WriteConcernResults (or null if WriteConcern is disabled).</returns>
        public virtual IEnumerable<WriteConcernResult> InsertBatch<TNominalType>(
            IEnumerable<TNominalType> documents,
            MongoInsertOptions options)
        {
            if (documents == null)
            {
                throw new ArgumentNullException("documents");
            }
            return InsertBatch(typeof(TNominalType), documents.Cast<object>(), options);
        }

        /// <summary>
        /// Inserts multiple documents at once into this collection (see also Insert to insert a single document).
        /// </summary>
        /// <typeparam name="TNominalType">The type of the documents to insert.</typeparam>
        /// <param name="documents">The documents to insert.</param>
        /// <param name="writeConcern">The write concern to use for this Insert.</param>
        /// <returns>A list of WriteConcernResults (or null if WriteConcern is disabled).</returns>
        public virtual IEnumerable<WriteConcernResult> InsertBatch<TNominalType>(
            IEnumerable<TNominalType> documents,
            WriteConcern writeConcern)
        {
            if (documents == null)
            {
                throw new ArgumentNullException("documents");
            }
            return InsertBatch(typeof(TNominalType), documents.Cast<object>(), writeConcern);
        }

        /// <summary>
        /// Inserts multiple documents at once into this collection (see also Insert to insert a single document).
        /// </summary>
        /// <param name="nominalType">The nominal type of the documents to insert.</param>
        /// <param name="documents">The documents to insert.</param>
        /// <returns>A list of WriteConcernResults (or null if WriteConcern is disabled).</returns>
        public virtual IEnumerable<WriteConcernResult> InsertBatch(Type nominalType, IEnumerable documents)
        {
            var options = new MongoInsertOptions();
            return InsertBatch(nominalType, documents, options);
        }

        /// <summary>
        /// Inserts multiple documents at once into this collection (see also Insert to insert a single document).
        /// </summary>
        /// <param name="nominalType">The nominal type of the documents to insert.</param>
        /// <param name="documents">The documents to insert.</param>
        /// <param name="writeConcern">The write concern to use for this Insert.</param>
        /// <returns>A list of WriteConcernResults (or null if WriteConcern is disabled).</returns>
        public virtual IEnumerable<WriteConcernResult> InsertBatch(
            Type nominalType,
            IEnumerable documents,
            WriteConcern writeConcern)
        {
            var options = new MongoInsertOptions { WriteConcern = writeConcern};
            return InsertBatch(nominalType, documents, options);
        }

        /// <summary>
        /// Inserts multiple documents at once into this collection (see also Insert to insert a single document).
        /// </summary>
        /// <param name="nominalType">The nominal type of the documents to insert.</param>
        /// <param name="documents">The documents to insert.</param>
        /// <param name="options">The options to use for this Insert.</param>
        /// <returns>A list of WriteConcernResults (or null if WriteConcern is disabled).</returns>
        public virtual IEnumerable<WriteConcernResult> InsertBatch(
            Type nominalType,
            IEnumerable documents,
            MongoInsertOptions options)
        {
            if (documents == null)
            {
                throw new ArgumentNullException("documents");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            var connection = _server.AcquireConnection(_database, ReadPreference.Primary);
            try
            {
                var writeConcern = options.WriteConcern ?? _settings.WriteConcern;

                List<WriteConcernResult> results = (writeConcern.FireAndForget) ? null : new List<WriteConcernResult>();

                var writerSettings = GetWriterSettings(connection);
                using (var message = new MongoInsertMessage(writerSettings, FullName, options.CheckElementNames, options.Flags))
                {
                    message.WriteToBuffer(); // must be called before AddDocument

                    foreach (var document in documents)
                    {
                        if (document == null)
                        {
                            throw new ArgumentException("Batch contains one or more null documents.");
                        }

                        if (_settings.AssignIdOnInsert)
                        {
                            var serializer = BsonSerializer.LookupSerializer(document.GetType());
                            var idProvider = serializer as IBsonIdProvider;
                            if (idProvider != null)
                            {
                                object id;
                                Type idNominalType;
                                IIdGenerator idGenerator;
                                if (idProvider.GetDocumentId(document, out id, out idNominalType, out idGenerator))
                                {
                                    if (idGenerator != null && idGenerator.IsEmpty(id))
                                    {
                                        id = idGenerator.GenerateId(this, document);
                                        idProvider.SetDocumentId(document, id);
                                    }
                                }
                            }
                        }
                        message.AddDocument(nominalType, document);

                        if (message.MessageLength > connection.ServerInstance.MaxMessageLength)
                        {
                            byte[] lastDocument = message.RemoveLastDocument();
                            var intermediateResult = connection.SendMessage(message, writeConcern, _database.Name);
                            if (!writeConcern.FireAndForget) { results.Add(intermediateResult); }
                            message.ResetBatch(lastDocument);
                        }
                    }

                    var finalResult = connection.SendMessage(message, writeConcern, _database.Name);
                    if (!writeConcern.FireAndForget) { results.Add(finalResult); }

                    return results;
                }
            }
            finally
            {
                _server.ReleaseConnection(connection);
            }
        }

        /// <summary>
        /// Tests whether this collection is capped.
        /// </summary>
        /// <returns>True if this collection is capped.</returns>
        public virtual bool IsCapped()
        {
            return GetStats().IsCapped;
        }

        /// <summary>
        /// Runs a Map/Reduce command on this collection.
        /// </summary>
        /// <param name="map">A JavaScript function called for each document.</param>
        /// <param name="reduce">A JavaScript function called on the values emitted by the map function.</param>
        /// <param name="options">Options for this map/reduce command (see <see cref="MapReduceOptionsDocument"/>, <see cref="MapReduceOptionsWrapper"/> and the <see cref="MapReduceOptions"/> builder).</param>
        /// <returns>A <see cref="MapReduceResult"/>.</returns>
        public virtual MapReduceResult MapReduce(
            BsonJavaScript map,
            BsonJavaScript reduce,
            IMongoMapReduceOptions options)
        {
            var command = new CommandDocument
            {
                { "mapreduce", _name },
                { "map", map },
                { "reduce", reduce }
            };
            command.Add(options.ToBsonDocument());
            var result = RunCommandAs<MapReduceResult>(command);
            result.SetInputDatabase(_database);
            return result;
        }

        /// <summary>
        /// Runs a Map/Reduce command on document in this collection that match a query.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="map">A JavaScript function called for each document.</param>
        /// <param name="reduce">A JavaScript function called on the values emitted by the map function.</param>
        /// <param name="options">Options for this map/reduce command (see <see cref="MapReduceOptionsDocument"/>, <see cref="MapReduceOptionsWrapper"/> and the <see cref="MapReduceOptions"/> builder).</param>
        /// <returns>A <see cref="MapReduceResult"/>.</returns>
        public virtual MapReduceResult MapReduce(
            IMongoQuery query,
            BsonJavaScript map,
            BsonJavaScript reduce,
            IMongoMapReduceOptions options)
        {
            // create a new set of options because we don't want to modify caller's data
            options = MapReduceOptions.SetQuery(query).AddOptions(options.ToBsonDocument());
            return MapReduce(map, reduce, options);
        }

        /// <summary>
        /// Runs a Map/Reduce command on document in this collection that match a query.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="map">A JavaScript function called for each document.</param>
        /// <param name="reduce">A JavaScript function called on the values emitted by the map function.</param>
        /// <returns>A <see cref="MapReduceResult"/>.</returns>
        public virtual MapReduceResult MapReduce(IMongoQuery query, BsonJavaScript map, BsonJavaScript reduce)
        {
            var options = MapReduceOptions.SetQuery(query).SetOutput(MapReduceOutput.Inline);
            return MapReduce(map, reduce, options);
        }

        /// <summary>
        /// Runs a Map/Reduce command on this collection.
        /// </summary>
        /// <param name="map">A JavaScript function called for each document.</param>
        /// <param name="reduce">A JavaScript function called on the values emitted by the map function.</param>
        /// <returns>A <see cref="MapReduceResult"/>.</returns>
        public virtual MapReduceResult MapReduce(BsonJavaScript map, BsonJavaScript reduce)
        {
            var options = MapReduceOptions.SetOutput(MapReduceOutput.Inline);
            return MapReduce(map, reduce, options);
        }

        /// <summary>
        /// Runs the ReIndex command on this collection.
        /// </summary>
        /// <returns>A CommandResult.</returns>
        public virtual CommandResult ReIndex()
        {
            var command = new CommandDocument("reIndex", _name);
            return RunCommand(command);
        }

        /// <summary>
        /// Removes documents from this collection that match a query.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Remove(IMongoQuery query)
        {
            return Remove(query, RemoveFlags.None, null);
        }

        /// <summary>
        /// Removes documents from this collection that match a query.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="writeConcern">The write concern to use for this Insert.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Remove(IMongoQuery query, WriteConcern writeConcern)
        {
            return Remove(query, RemoveFlags.None, writeConcern);
        }

        /// <summary>
        /// Removes documents from this collection that match a query.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="flags">The flags for this Remove (see <see cref="RemoveFlags"/>).</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Remove(IMongoQuery query, RemoveFlags flags)
        {
            return Remove(query, flags, null);
        }

        /// <summary>
        /// Removes documents from this collection that match a query.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="flags">The flags for this Remove (see <see cref="RemoveFlags"/>).</param>
        /// <param name="writeConcern">The write concern to use for this Insert.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Remove(IMongoQuery query, RemoveFlags flags, WriteConcern writeConcern)
        {
            var connection = _server.AcquireConnection(_database, ReadPreference.Primary);
            try
            {
                var writerSettings = GetWriterSettings(connection);
                using (var message = new MongoDeleteMessage(writerSettings, FullName, flags, query))
                {
                    return connection.SendMessage(message, writeConcern ?? _settings.WriteConcern, _database.Name);
                }
            }
            finally
            {
                _server.ReleaseConnection(connection);
            }
        }

        /// <summary>
        /// Removes all documents from this collection (see also <see cref="Drop"/>).
        /// </summary>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult RemoveAll()
        {
            return Remove(Query.Null, RemoveFlags.None, null);
        }

        /// <summary>
        /// Removes all documents from this collection (see also <see cref="Drop"/>).
        /// </summary>
        /// <param name="writeConcern">The write concern to use for this Insert.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult RemoveAll(WriteConcern writeConcern)
        {
            return Remove(Query.Null, RemoveFlags.None, writeConcern);
        }

        /// <summary>
        /// Removes all entries for this collection in the index cache used by EnsureIndex. Call this method
        /// when you know (or suspect) that a process other than this one may have dropped one or
        /// more indexes.
        /// </summary>
        public virtual void ResetIndexCache()
        {
            _server.IndexCache.Reset(this);
        }

        /// <summary>
        /// Saves a document to this collection. The document must have an identifiable Id field. Based on the value
        /// of the Id field Save will perform either an Insert or an Update.
        /// </summary>
        /// <typeparam name="TNominalType">The type of the document to save.</typeparam>
        /// <param name="document">The document to save.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Save<TNominalType>(TNominalType document)
        {
            return Save(typeof(TNominalType), document);
        }

        /// <summary>
        /// Saves a document to this collection. The document must have an identifiable Id field. Based on the value
        /// of the Id field Save will perform either an Insert or an Update.
        /// </summary>
        /// <typeparam name="TNominalType">The type of the document to save.</typeparam>
        /// <param name="document">The document to save.</param>
        /// <param name="options">The options to use for this Save.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Save<TNominalType>(TNominalType document, MongoInsertOptions options)
        {
            return Save(typeof(TNominalType), document, options);
        }

        /// <summary>
        /// Saves a document to this collection. The document must have an identifiable Id field. Based on the value
        /// of the Id field Save will perform either an Insert or an Update.
        /// </summary>
        /// <typeparam name="TNominalType">The type of the document to save.</typeparam>
        /// <param name="document">The document to save.</param>
        /// <param name="writeConcern">The write concern to use for this Insert.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Save<TNominalType>(TNominalType document, WriteConcern writeConcern)
        {
            return Save(typeof(TNominalType), document, writeConcern);
        }

        /// <summary>
        /// Saves a document to this collection. The document must have an identifiable Id field. Based on the value
        /// of the Id field Save will perform either an Insert or an Update.
        /// </summary>
        /// <param name="nominalType">The type of the document to save.</param>
        /// <param name="document">The document to save.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Save(Type nominalType, object document)
        {
            var options = new MongoInsertOptions();
            return Save(nominalType, document, options);
        }

        /// <summary>
        /// Saves a document to this collection. The document must have an identifiable Id field. Based on the value
        /// of the Id field Save will perform either an Insert or an Update.
        /// </summary>
        /// <param name="nominalType">The type of the document to save.</param>
        /// <param name="document">The document to save.</param>
        /// <param name="options">The options to use for this Save.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Save(Type nominalType, object document, MongoInsertOptions options)
        {
            if (document == null)
            {
                throw new ArgumentNullException("document");
            }
            var serializer = BsonSerializer.LookupSerializer(document.GetType());
            var idProvider = serializer as IBsonIdProvider;
            object id;
            Type idNominalType;
            IIdGenerator idGenerator;
            if (idProvider != null && idProvider.GetDocumentId(document, out id, out idNominalType, out idGenerator))
            {
                if (id == null && idGenerator == null)
                {
                    throw new InvalidOperationException("No IdGenerator found.");
                }

                if (idGenerator != null && idGenerator.IsEmpty(id))
                {
                    id = idGenerator.GenerateId(this, document);
                    idProvider.SetDocumentId(document, id);
                    return Insert(nominalType, document, options);
                }
                else
                {
                    BsonValue idBsonValue;
                    var documentType = document.GetType();
                    if (BsonClassMap.IsClassMapRegistered(documentType))
                    {
                        var classMap = BsonClassMap.LookupClassMap(documentType);
                        var idMemberMap = classMap.IdMemberMap;
                        var idSerializer = idMemberMap.GetSerializer(id.GetType());
                        // we only care about the serialized _id value but we need a dummy document to serialize it into
                        var bsonDocument = new BsonDocument();
                        var bsonDocumentWriterSettings = new BsonDocumentWriterSettings
                        {
                            GuidRepresentation = _settings.GuidRepresentation
                        };
                        var bsonWriter = BsonWriter.Create(bsonDocument, bsonDocumentWriterSettings);
                        bsonWriter.WriteStartDocument();
                        bsonWriter.WriteName("_id");
                        idSerializer.Serialize(bsonWriter, id.GetType(), id, idMemberMap.SerializationOptions);
                        bsonWriter.WriteEndDocument();
                        idBsonValue = bsonDocument[0]; // extract the _id value from the dummy document
                    } else {
                        if (!BsonTypeMapper.TryMapToBsonValue(id, out idBsonValue))
                        {
                            idBsonValue = BsonDocumentWrapper.Create(idNominalType, id);
                        }
                    }

                    var query = Query.EQ("_id", idBsonValue);
                    var update = Builders.Update.Replace(nominalType, document);
                    var updateOptions = new MongoUpdateOptions
                    {
                        CheckElementNames = options.CheckElementNames,
                        Flags = UpdateFlags.Upsert,
                        WriteConcern = options.WriteConcern
                    };
                    return Update(query, update, updateOptions);
                }
            }
            else
            {
                throw new InvalidOperationException("Save can only be used with documents that have an Id.");
            }
        }

        /// <summary>
        /// Saves a document to this collection. The document must have an identifiable Id field. Based on the value
        /// of the Id field Save will perform either an Insert or an Update.
        /// </summary>
        /// <param name="nominalType">The type of the document to save.</param>
        /// <param name="document">The document to save.</param>
        /// <param name="writeConcern">The write concern to use for this Insert.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Save(Type nominalType, object document, WriteConcern writeConcern)
        {
            var options = new MongoInsertOptions { WriteConcern = writeConcern};
            return Save(nominalType, document, options);
        }

        /// <summary>
        /// Gets a canonical string representation for this database.
        /// </summary>
        /// <returns>A canonical string representation for this database.</returns>
        public override string ToString()
        {
            return FullName;
        }

        /// <summary>
        /// Updates one matching document in this collection.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="update">The update to perform on the matching document.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Update(IMongoQuery query, IMongoUpdate update)
        {
            var options = new MongoUpdateOptions();
            return Update(query, update, options);
        }

        /// <summary>
        /// Updates one or more matching documents in this collection (for multiple updates use UpdateFlags.Multi).
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="update">The update to perform on the matching document.</param>
        /// <param name="options">The update options.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Update(IMongoQuery query, IMongoUpdate update, MongoUpdateOptions options)
        {
            var updateBuilder = update as UpdateBuilder;
            if (updateBuilder != null)
            {
                if (updateBuilder.Document.ElementCount == 0)
                {
                    throw new ArgumentException("Update called with an empty UpdateBuilder that has no update operations.");
                }
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            var connection = _server.AcquireConnection(_database, ReadPreference.Primary);
            try
            {
                var writerSettings = GetWriterSettings(connection);
                using (var message = new MongoUpdateMessage(writerSettings, FullName, options.CheckElementNames, options.Flags, query, update))
                {
                    var writeConcern = options.WriteConcern ?? _settings.WriteConcern;
                    return connection.SendMessage(message, writeConcern, _database.Name);
                }
            }
            finally
            {
                _server.ReleaseConnection(connection);
            }
        }

        /// <summary>
        /// Updates one matching document in this collection.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="update">The update to perform on the matching document.</param>
        /// <param name="writeConcern">The write concern to use for this Insert.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Update(IMongoQuery query, IMongoUpdate update, WriteConcern writeConcern)
        {
            var options = new MongoUpdateOptions { WriteConcern = writeConcern};
            return Update(query, update, options);
        }

        /// <summary>
        /// Updates one or more matching documents in this collection (for multiple updates use UpdateFlags.Multi).
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="update">The update to perform on the matching document.</param>
        /// <param name="flags">The flags for this Update.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Update(IMongoQuery query, IMongoUpdate update, UpdateFlags flags)
        {
            var options = new MongoUpdateOptions { Flags = flags };
            return Update(query, update, options);
        }

        /// <summary>
        /// Updates one or more matching documents in this collection (for multiple updates use UpdateFlags.Multi).
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="update">The update to perform on the matching document.</param>
        /// <param name="flags">The flags for this Update.</param>
        /// <param name="writeConcern">The write concern to use for this Insert.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Update(
            IMongoQuery query,
            IMongoUpdate update,
            UpdateFlags flags,
            WriteConcern writeConcern)
        {
            var options = new MongoUpdateOptions
            {
                Flags = flags,
                WriteConcern = writeConcern
            };
            return Update(query, update, options);
        }

        /// <summary>
        /// Validates the integrity of this collection.
        /// </summary>
        /// <returns>A <see cref="ValidateCollectionResult"/>.</returns>
        public virtual ValidateCollectionResult Validate()
        {
            var command = new CommandDocument("validate", _name);
            return RunCommandAs<ValidateCollectionResult>(command);
        }

        // internal methods
        internal BsonBinaryReaderSettings GetReaderSettings(MongoConnection connection)
        {
            return new BsonBinaryReaderSettings
            {
                GuidRepresentation = _settings.GuidRepresentation,
                MaxDocumentSize = connection.ServerInstance.MaxDocumentSize
            };
        }

        internal BsonBinaryWriterSettings GetWriterSettings(MongoConnection connection)
        {
            return new BsonBinaryWriterSettings
            {
                GuidRepresentation = _settings.GuidRepresentation,
                MaxDocumentSize = connection.ServerInstance.MaxDocumentSize
            };
        }

        internal CommandResult RunCommand(IMongoCommand command)
        {
            return RunCommandAs<CommandResult>(command);
        }

        internal TCommandResult RunCommandAs<TCommandResult>(IMongoCommand command)
            where TCommandResult : CommandResult, new()
        {
            return (TCommandResult)RunCommandAs(typeof(TCommandResult), command);
        }

        internal CommandResult RunCommandAs(Type commandResultType, IMongoCommand command)
        {
            // if necessary delegate running the command to the _commandCollection
            if (_name == "$cmd")
            {
                var response = FindOneAs<BsonDocument>(command);
                if (response == null)
                {
                    var commandName = command.ToBsonDocument().GetElement(0).Name;
                    var message = string.Format("Command '{0}' failed. No response returned.", commandName);
                    throw new MongoCommandException(message);
                }
                var commandResult = (CommandResult)Activator.CreateInstance(commandResultType); // constructor can't have arguments
                commandResult.Initialize(command, response); // so two phase construction required
                if (!commandResult.Ok)
                {
                    if (commandResult.ErrorMessage == "not master")
                    {
                        // TODO: figure out which instance gave the error and set its state to Unknown
                        _server.Disconnect();
                    }
                    throw new MongoCommandException(commandResult);
                }
                return commandResult;
            }
            else
            {
                return _commandCollection.RunCommandAs(commandResultType, command);
            }
        }

        // private methods
        private string GetIndexName(BsonDocument keys, BsonDocument options)
        {
            if (options != null)
            {
                if (options.Contains("name"))
                {
                    return options["name"].AsString;
                }
            }

            StringBuilder sb = new StringBuilder();
            foreach (var element in keys)
            {
                if (sb.Length > 0)
                {
                    sb.Append("_");
                }
                sb.Append(element.Name);
                sb.Append("_");
                var value = element.Value;
                if (value.BsonType == BsonType.Int32 ||
                    value.BsonType == BsonType.Int64 ||
                    value.BsonType == BsonType.Double ||
                    value.BsonType == BsonType.String)
                {
                    sb.Append(value.RawValue.ToString().Replace(' ', '_'));
                }
            }
            return sb.ToString();
        }

        private string GetIndexName(string[] keyNames)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string name in keyNames)
            {
                if (sb.Length > 0)
                {
                    sb.Append("_");
                }
                sb.Append(name);
                sb.Append("_1");
            }
            return sb.ToString();
        }
    }

    // this subclass provides a default document type for Find methods
    // you can still Find any other document type by using the FindAs<TDocument> methods

    /// <summary>
    /// Represents a MongoDB collection and the settings used to access it as well as a default document type. This class is thread-safe.
    /// </summary>
    /// <typeparam name="TDefaultDocument">The default document type of the collection.</typeparam>
    public class MongoCollection<TDefaultDocument> : MongoCollection
    {
        // constructors
        /// <summary>
        /// Creates a new instance of MongoCollection. Normally you would call one of the indexers or GetCollection methods
        /// of MongoDatabase instead.
        /// </summary>
        /// <param name="database">The database that contains this collection.</param>
        /// <param name="settings">The settings to use to access this collection.</param>
        public MongoCollection(MongoDatabase database, MongoCollectionSettings<TDefaultDocument> settings)
            : base(database, settings)
        {
        }

        // public methods
        /// <summary>
        /// Returns a cursor that can be used to find all documents in this collection that match the query as TDefaultDocuments.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>A <see cref="MongoCursor{TDocument}"/>.</returns>
        public virtual MongoCursor<TDefaultDocument> Find(IMongoQuery query)
        {
            return FindAs<TDefaultDocument>(query);
        }

        /// <summary>
        /// Returns a cursor that can be used to find all documents in this collection as TDefaultDocuments.
        /// </summary>
        /// <returns>A <see cref="MongoCursor{TDocument}"/>.</returns>
        public virtual MongoCursor<TDefaultDocument> FindAll()
        {
            return FindAllAs<TDefaultDocument>();
        }

        /// <summary>
        /// Returns a cursor that can be used to find one document in this collection as a TDefaultDocument.
        /// </summary>
        /// <returns>A TDefaultDocument (or null if not found).</returns>
        public virtual TDefaultDocument FindOne()
        {
            return FindOneAs<TDefaultDocument>();
        }

        /// <summary>
        /// Returns a cursor that can be used to find one document in this collection that matches a query as a TDefaultDocument.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>A TDefaultDocument (or null if not found).</returns>
        public virtual TDefaultDocument FindOne(IMongoQuery query)
        {
            return FindOneAs<TDefaultDocument>(query);
        }

        /// <summary>
        /// Returns a cursor that can be used to find one document in this collection by its _id value as a TDefaultDocument.
        /// </summary>
        /// <param name="id">The id of the document.</param>
        /// <returns>A TDefaultDocument (or null if not found).</returns>
        public virtual TDefaultDocument FindOneById(BsonValue id)
        {
            return FindOneByIdAs<TDefaultDocument>(id);
        }

        /// <summary>
        /// Runs a geoHaystack search command on this collection.
        /// </summary>
        /// <param name="x">The x coordinate of the starting location.</param>
        /// <param name="y">The y coordinate of the starting location.</param>
        /// <param name="options">The options for the geoHaystack search (null if none).</param>
        /// <returns>A <see cref="GeoHaystackSearchResult{TDocument}"/>.</returns>
        public virtual GeoHaystackSearchResult<TDefaultDocument> GeoHaystackSearch(
            double x,
            double y,
            IMongoGeoHaystackSearchOptions options)
        {
            return GeoHaystackSearchAs<TDefaultDocument>(x, y, options);
        }

        /// <summary>
        /// Runs a GeoNear command on this collection.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="x">The x coordinate of the starting location.</param>
        /// <param name="y">The y coordinate of the starting location.</param>
        /// <param name="limit">The maximum number of results returned.</param>
        /// <returns>A <see cref="GeoNearResult{TDefaultDocument}"/>.</returns>
        public virtual GeoNearResult<TDefaultDocument> GeoNear(IMongoQuery query, double x, double y, int limit)
        {
            return GeoNearAs<TDefaultDocument>(query, x, y, limit);
        }

        /// <summary>
        /// Runs a GeoNear command on this collection.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="x">The x coordinate of the starting location.</param>
        /// <param name="y">The y coordinate of the starting location.</param>
        /// <param name="limit">The maximum number of results returned.</param>
        /// <param name="options">Options for the GeoNear command (see <see cref="GeoNearOptionsDocument"/>, <see cref="GeoNearOptionsWrapper"/>, and the <see cref="GeoNearOptions"/> builder).</param>
        /// <returns>A <see cref="GeoNearResult{TDefaultDocument}"/>.</returns>
        public virtual GeoNearResult<TDefaultDocument> GeoNear(
            IMongoQuery query,
            double x,
            double y,
            int limit,
            IMongoGeoNearOptions options)
        {
            return GeoNearAs<TDefaultDocument>(query, x, y, limit, options);
        }

        /// <summary>
        /// Inserts a document into this collection (see also InsertBatch to insert multiple documents at once).
        /// </summary>
        /// <param name="document">The document to insert.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Insert(TDefaultDocument document)
        {
            return Insert<TDefaultDocument>(document);
        }

        /// <summary>
        /// Inserts a document into this collection (see also InsertBatch to insert multiple documents at once).
        /// </summary>
        /// <param name="document">The document to insert.</param>
        /// <param name="options">The options to use for this Insert.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Insert(TDefaultDocument document, MongoInsertOptions options)
        {
            return Insert<TDefaultDocument>(document, options);
        }

        /// <summary>
        /// Inserts a document into this collection (see also InsertBatch to insert multiple documents at once).
        /// </summary>
        /// <param name="document">The document to insert.</param>
        /// <param name="writeConcern">The write concern to use for this Insert.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Insert(TDefaultDocument document, WriteConcern writeConcern)
        {
            return Insert<TDefaultDocument>(document, writeConcern);
        }

        /// <summary>
        /// Inserts multiple documents at once into this collection (see also Insert to insert a single document).
        /// </summary>
        /// <param name="documents">The documents to insert.</param>
        /// <returns>A list of WriteConcernResults (or null if WriteConcern is disabled).</returns>
        public virtual IEnumerable<WriteConcernResult> InsertBatch(IEnumerable<TDefaultDocument> documents)
        {
            return InsertBatch<TDefaultDocument>(documents);
        }

        /// <summary>
        /// Inserts multiple documents at once into this collection (see also Insert to insert a single document).
        /// </summary>
        /// <param name="documents">The documents to insert.</param>
        /// <param name="options">The options to use for this Insert.</param>
        /// <returns>A list of WriteConcernResults (or null if WriteConcern is disabled).</returns>
        public virtual IEnumerable<WriteConcernResult> InsertBatch(
            IEnumerable<TDefaultDocument> documents,
            MongoInsertOptions options)
        {
            return InsertBatch<TDefaultDocument>(documents, options);
        }

        /// <summary>
        /// Inserts multiple documents at once into this collection (see also Insert to insert a single document).
        /// </summary>
        /// <param name="documents">The documents to insert.</param>
        /// <param name="writeConcern">The write concern to use for this Insert.</param>
        /// <returns>A list of WriteConcernResults (or null if WriteConcern is disabled).</returns>
        public virtual IEnumerable<WriteConcernResult> InsertBatch(
            IEnumerable<TDefaultDocument> documents,
            WriteConcern writeConcern)
        {
            return InsertBatch<TDefaultDocument>(documents, writeConcern);
        }

        /// <summary>
        /// Saves a document to this collection. The document must have an identifiable Id field. Based on the value
        /// of the Id field Save will perform either an Insert or an Update.
        /// </summary>
        /// <param name="document">The document to save.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Save(TDefaultDocument document)
        {
            return Save<TDefaultDocument>(document);
        }

        /// <summary>
        /// Saves a document to this collection. The document must have an identifiable Id field. Based on the value
        /// of the Id field Save will perform either an Insert or an Update.
        /// </summary>
        /// <param name="document">The document to save.</param>
        /// <param name="options">The options to use for this Save.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Save(TDefaultDocument document, MongoInsertOptions options)
        {
            return Save<TDefaultDocument>(document, options);
        }

        /// <summary>
        /// Saves a document to this collection. The document must have an identifiable Id field. Based on the value
        /// of the Id field Save will perform either an Insert or an Update.
        /// </summary>
        /// <param name="document">The document to save.</param>
        /// <param name="writeConcern">The write concern to use for this Insert.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Save(TDefaultDocument document, WriteConcern writeConcern)
        {
            return Save<TDefaultDocument>(document, writeConcern);
        }
    }
}
