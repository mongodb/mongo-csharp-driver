/* Copyright 2010-2016 MongoDB Inc.
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
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.Driver.Operations;
using MongoDB.Driver.Wrappers;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a MongoDB collection and the settings used to access it. This class is thread-safe.
    /// </summary>
    public abstract class MongoCollection
    {
        // private fields
        private readonly MongoServer _server;
        private readonly MongoDatabase _database;
        private readonly MongoCollectionSettings _settings;
        private readonly CollectionNamespace _collectionNamespace;

        // constructors
        /// <summary>
        /// Protected constructor for abstract base class.
        /// </summary>
        /// <param name="database">The database that contains this collection.</param>
        /// <param name="name">The name of the collection.</param>
        /// <param name="settings">The settings to use to access this collection.</param>
        protected MongoCollection(MongoDatabase database, string name, MongoCollectionSettings settings)
        {
            if (database == null)
            {
                throw new ArgumentNullException("database");
            }
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }
            string message;
            if (!database.IsCollectionNameValid(name, out message))
            {
                throw new ArgumentOutOfRangeException("name", message);
            }

            settings = settings.Clone();
            settings.ApplyDefaultValues(database.Settings);
            settings.Freeze();

            _server = database.Server;
            _database = database;
            _collectionNamespace = new CollectionNamespace(_database.Name, name);
            _settings = settings;
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
            get { return _collectionNamespace.FullName; }
        }

        /// <summary>
        /// Gets the name of this collection.
        /// </summary>
        public virtual string Name
        {
            get { return _collectionNamespace.CollectionName; }
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
        /// Represents an aggregate framework query. The command is not sent to the server until the result is enumerated.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>A sequence of documents.</returns>
        public virtual IEnumerable<BsonDocument> Aggregate(AggregateArgs args)
        {
            if (args == null) { throw new ArgumentNullException("args"); }
            if (args.Pipeline == null) { throw new ArgumentException("Pipeline is null.", "args"); }

            var messageEncoderSettings = GetMessageEncoderSettings();

            var last = args.Pipeline.LastOrDefault();
            if (last != null && last.GetElement(0).Name == "$out")
            {
                var aggregateOperation = new AggregateToCollectionOperation(_collectionNamespace, args.Pipeline, messageEncoderSettings)
                {
                    AllowDiskUse = args.AllowDiskUse,
                    BypassDocumentValidation = args.BypassDocumentValidation,
                    Collation = args.Collation,
                    MaxTime = args.MaxTime,
                    WriteConcern = _settings.WriteConcern
                };
                ExecuteWriteOperation(aggregateOperation);

                var outputCollectionName = last[0].AsString;
                var outputCollectionNamespace = new CollectionNamespace(_collectionNamespace.DatabaseNamespace, outputCollectionName);
                var resultSerializer = BsonDocumentSerializer.Instance;
                var findOperation = new FindOperation<BsonDocument>(outputCollectionNamespace, resultSerializer, messageEncoderSettings)
                {
                    BatchSize = args.BatchSize,
                    Collation = args.Collation,
                    MaxTime = args.MaxTime
                };

                return new AggregateEnumerable(this, findOperation, ReadPreference.Primary);
            }
            else
            {
                var resultSerializer = BsonDocumentSerializer.Instance;
                var operation = new AggregateOperation<BsonDocument>(_collectionNamespace, args.Pipeline, resultSerializer, messageEncoderSettings)
                {
                    AllowDiskUse = args.AllowDiskUse,
                    BatchSize = args.BatchSize,
                    Collation = args.Collation,
                    MaxTime = args.MaxTime,
                    ReadConcern = _settings.ReadConcern,
                    UseCursor = args.OutputMode == AggregateOutputMode.Cursor
                };
                return new AggregateEnumerable(this, operation, _settings.ReadPreference);
            }
        }

        /// <summary>
        /// Runs an aggregate command with explain set and returns the explain result.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>The explain result.</returns>
        public virtual CommandResult AggregateExplain(AggregateArgs args)
        {
            var messageEncoderSettings = GetMessageEncoderSettings();
            var operation = new AggregateExplainOperation(_collectionNamespace, args.Pipeline, messageEncoderSettings)
            {
                AllowDiskUse = args.AllowDiskUse,
                Collation = args.Collation,
                MaxTime = args.MaxTime
            };
            var response = ExecuteReadOperation(operation);
            return new CommandResult(response);
        }

        /// <summary>
        /// Counts the number of documents in this collection.
        /// </summary>
        /// <returns>The number of documents in this collection.</returns>
        public virtual long Count()
        {
            var args = new CountArgs { Query = null };
            return Count(args);
        }

        /// <summary>
        /// Counts the number of documents in this collection that match a query.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>
        /// The number of documents in this collection that match the query.
        /// </returns>
        public virtual long Count(CountArgs args)
        {
            if (args == null) { throw new ArgumentNullException("args"); }

            var filter = args.Query == null ? null : new BsonDocumentWrapper(args.Query);
            var operation = new CountOperation(_collectionNamespace, GetMessageEncoderSettings())
            {
                Collation = args.Collation,
                Filter = filter,
                Hint = args.Hint,
                Limit = args.Limit,
                MaxTime = args.MaxTime,
                ReadConcern = _settings.ReadConcern,
                Skip = args.Skip
            };

            return ExecuteReadOperation(operation, args.ReadPreference);
        }

        /// <summary>
        /// Counts the number of documents in this collection that match a query.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>The number of documents in this collection that match the query.</returns>
        public virtual long Count(IMongoQuery query)
        {
            var args = new CountArgs { Query = query };
            return Count(args);
        }

        /// <summary>
        /// Creates an index for this collection.
        /// </summary>
        /// <param name="keys">The indexed fields (usually an IndexKeysDocument or constructed using the IndexKeys builder).</param>
        /// <param name="options">The index options(usually an IndexOptionsDocument or created using the IndexOption builder).</param>
        /// <returns>A WriteConcernResult.</returns>
        public virtual WriteConcernResult CreateIndex(IMongoIndexKeys keys, IMongoIndexOptions options)
        {
            try
            {
                var keysDocument = keys.ToBsonDocument();
                var optionsDocument = options.ToBsonDocument();
                var requests = new[] { new CreateIndexRequest(keysDocument) { AdditionalOptions = optionsDocument } };
                var operation = new CreateIndexesOperation(_collectionNamespace, requests, GetMessageEncoderSettings())
                {
                    WriteConcern = _settings.WriteConcern
                };
                ExecuteWriteOperation(operation);
                return new WriteConcernResult(new BsonDocument("ok", 1));
            }
            catch (MongoCommandException ex)
            {
                var writeConcernResult = new WriteConcernResult(ex.Result);
                throw new MongoWriteConcernException(ex.ConnectionId, ex.Message, writeConcernResult);
            }
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
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="args">The args.</param>
        /// <returns>The distint values of the field.</returns>
        public IEnumerable<TValue> Distinct<TValue>(DistinctArgs args)
        {
            if (args == null) { throw new ArgumentNullException("args"); }
            if (args.Key == null) { throw new ArgumentException("Key is null.", "args"); }

            var valueSerializer = (IBsonSerializer<TValue>)args.ValueSerializer ?? BsonSerializer.LookupSerializer<TValue>();
            var operation = new DistinctOperation<TValue>(_collectionNamespace, valueSerializer, args.Key, GetMessageEncoderSettings())
            {
                Collation = args.Collation,
                Filter = args.Query == null ? null : new BsonDocumentWrapper(args.Query),
                MaxTime = args.MaxTime,
                ReadConcern = _settings.ReadConcern
            };

            return ExecuteReadOperation(operation).ToList();
        }

        /// <summary>
        /// Returns the distinct values for a given field.
        /// </summary>
        /// <param name="key">The key of the field.</param>
        /// <returns>The distint values of the field.</returns>
        public virtual IEnumerable<BsonValue> Distinct(string key)
        {
            return Distinct<BsonValue>(new DistinctArgs
            {
                Key = key,
                ValueSerializer = BsonValueSerializer.Instance
            });
        }

        /// <summary>
        /// Returns the distinct values for a given field for documents that match a query.
        /// </summary>
        /// <param name="key">The key of the field.</param>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>The distint values of the field.</returns>
        public virtual IEnumerable<BsonValue> Distinct(string key, IMongoQuery query)
        {
            return Distinct<BsonValue>(new DistinctArgs
            {
                Key = key,
                Query = query,
                ValueSerializer = BsonValueSerializer.Instance
            });
        }

        /// <summary>
        /// Returns the distinct values for a given field.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="key">The key of the field.</param>
        /// <returns>The distint values of the field.</returns>
        public virtual IEnumerable<TValue> Distinct<TValue>(string key)
        {
            return Distinct<TValue>(new DistinctArgs
            {
                Key = key
            });
        }

        /// <summary>
        /// Returns the distinct values for a given field for documents that match a query.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="key">The key of the field.</param>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>The distint values of the field.</returns>
        public virtual IEnumerable<TValue> Distinct<TValue>(string key, IMongoQuery query)
        {
            return Distinct<TValue>(new DistinctArgs
            {
                Key = key,
                Query = query
            });
        }

        /// <summary>
        /// Drops this collection.
        /// </summary>
        /// <returns>A CommandResult.</returns>
        public virtual CommandResult Drop()
        {
            return _database.DropCollection(_collectionNamespace.CollectionName);
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
            string indexName = IndexNameHelper.GetIndexName(keys.ToBsonDocument());
            return DropIndexByName(indexName);
        }

        /// <summary>
        /// Drops an index on this collection.
        /// </summary>
        /// <param name="keyNames">The names of the indexed fields.</param>
        /// <returns>A <see cref="CommandResult"/>.</returns>
        public virtual CommandResult DropIndex(params string[] keyNames)
        {
            string indexName = IndexNameHelper.GetIndexName(keyNames);
            return DropIndexByName(indexName);
        }

        /// <summary>
        /// Drops an index on this collection.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <returns>A <see cref="CommandResult"/>.</returns>
        public virtual CommandResult DropIndexByName(string indexName)
        {
            var operation = new DropIndexOperation(_collectionNamespace, indexName, GetMessageEncoderSettings())
            {
                WriteConcern = _settings.WriteConcern
            };
            var response = ExecuteWriteOperation(operation);
            return new CommandResult(response);
        }

        /// <summary>
        /// Ensures that the desired index exists and creates it if it does not.
        /// </summary>
        /// <param name="keys">The indexed fields (usually an IndexKeysDocument or constructed using the IndexKeys builder).</param>
        /// <param name="options">The index options(usually an IndexOptionsDocument or created using the IndexOption builder).</param>
        [Obsolete("Use CreateIndex instead.")]
        public virtual void EnsureIndex(IMongoIndexKeys keys, IMongoIndexOptions options)
        {
            CreateIndex(keys, options);
        }

        /// <summary>
        /// Ensures that the desired index exists and creates it if it does not.
        /// </summary>
        /// <param name="keys">The indexed fields (usually an IndexKeysDocument or constructed using the IndexKeys builder).</param>
        [Obsolete("Use CreateIndex instead.")]
        public virtual void EnsureIndex(IMongoIndexKeys keys)
        {
            CreateIndex(keys);
        }

        /// <summary>
        /// Ensures that the desired index exists and creates it if it does not.
        /// </summary>
        /// <param name="keyNames">The names of the indexed fields.</param>
        [Obsolete("Use CreateIndex instead.")]
        public virtual void EnsureIndex(params string[] keyNames)
        {
            CreateIndex(keyNames);
        }

        /// <summary>
        /// Tests whether this collection exists.
        /// </summary>
        /// <returns>True if this collection exists.</returns>
        public virtual bool Exists()
        {
            return _database.CollectionExists(_collectionNamespace.CollectionName);
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
        [Obsolete("Use the overload of FindAndModify that has a FindAndModifyArgs parameter instead.")]
        public virtual FindAndModifyResult FindAndModify(IMongoQuery query, IMongoSortBy sortBy, IMongoUpdate update)
        {
            var args = new FindAndModifyArgs { Query = query, SortBy = sortBy, Update = update };
            return FindAndModify(args);
        }

        /// <summary>
        /// Finds one matching document using the query and sortBy parameters and applies the specified update to it.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="sortBy">The sort order to select one of the matching documents.</param>
        /// <param name="update">The update to apply to the matching document.</param>
        /// <param name="returnNew">Whether to return the new or old version of the modified document in the <see cref="FindAndModifyResult"/>.</param>
        /// <returns>A <see cref="FindAndModifyResult"/>.</returns>
        [Obsolete("Use the overload of FindAndModify that has a FindAndModifyArgs parameter instead.")]
        public virtual FindAndModifyResult FindAndModify(
            IMongoQuery query,
            IMongoSortBy sortBy,
            IMongoUpdate update,
            bool returnNew)
        {
            var versionReturned = returnNew ? FindAndModifyDocumentVersion.Modified : FindAndModifyDocumentVersion.Original;
            var args = new FindAndModifyArgs { Query = query, SortBy = sortBy, Update = update, VersionReturned = versionReturned };
            return FindAndModify(args);
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
        [Obsolete("Use the overload of FindAndModify that has a FindAndModifyArgs parameter instead.")]
        public virtual FindAndModifyResult FindAndModify(
            IMongoQuery query,
            IMongoSortBy sortBy,
            IMongoUpdate update,
            bool returnNew,
            bool upsert)
        {
            var versionReturned = returnNew ? FindAndModifyDocumentVersion.Modified : FindAndModifyDocumentVersion.Original;
            var args = new FindAndModifyArgs { Query = query, SortBy = sortBy, Update = update, VersionReturned = versionReturned, Upsert = upsert };
            return FindAndModify(args);
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
        [Obsolete("Use the overload of FindAndModify that has a FindAndModifyArgs parameter instead.")]
        public virtual FindAndModifyResult FindAndModify(
            IMongoQuery query,
            IMongoSortBy sortBy,
            IMongoUpdate update,
            IMongoFields fields,
            bool returnNew,
            bool upsert)
        {
            var versionReturned = returnNew ? FindAndModifyDocumentVersion.Modified : FindAndModifyDocumentVersion.Original;
            var args = new FindAndModifyArgs { Query = query, SortBy = sortBy, Update = update, Fields = fields, VersionReturned = versionReturned, Upsert = upsert };
            return FindAndModify(args);
        }

        /// <summary>
        /// Finds one matching document using the supplied arguments and applies the specified update to it.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>A <see cref="FindAndModifyResult"/>.</returns>
        public virtual FindAndModifyResult FindAndModify(FindAndModifyArgs args)
        {
            if (args == null) { throw new ArgumentNullException("args"); }
            if (args.Update == null) { throw new ArgumentException("Update is null.", "args"); }

            var filter = args.Query == null ? new BsonDocument() : new BsonDocumentWrapper(args.Query);
            var updateDocument = args.Update.ToBsonDocument();
            var resultSerializer = BsonDocumentSerializer.Instance;
            var messageEncoderSettings = GetMessageEncoderSettings();
            var projection = args.Fields == null ? null : new BsonDocumentWrapper(args.Fields);
            var returnDocument = !args.VersionReturned.HasValue || args.VersionReturned.Value == FindAndModifyDocumentVersion.Original
                ? Core.Operations.ReturnDocument.Before
                : Core.Operations.ReturnDocument.After;
            var sort = args.SortBy == null ? null : new BsonDocumentWrapper(args.SortBy);

            FindAndModifyOperationBase<BsonDocument> operation;
            if (updateDocument.ElementCount > 0 && updateDocument.GetElement(0).Name.StartsWith("$"))
            {
                operation = new FindOneAndUpdateOperation<BsonDocument>(_collectionNamespace, filter, updateDocument, resultSerializer, messageEncoderSettings)
                {
                    BypassDocumentValidation = args.BypassDocumentValidation,
                    Collation = args.Collation,
                    IsUpsert = args.Upsert,
                    MaxTime = args.MaxTime,
                    Projection = projection,
                    ReturnDocument = returnDocument,
                    Sort = sort,
                    WriteConcern = _settings.WriteConcern
                };
            }
            else
            {
                var replacement = updateDocument;
                operation = new FindOneAndReplaceOperation<BsonDocument>(_collectionNamespace, filter, replacement, resultSerializer, messageEncoderSettings)
                {
                    BypassDocumentValidation = args.BypassDocumentValidation,
                    Collation = args.Collation,
                    IsUpsert = args.Upsert,
                    MaxTime = args.MaxTime,
                    Projection = projection,
                    ReturnDocument = returnDocument,
                    Sort = sort,
                    WriteConcern = _settings.WriteConcern
                };
            }

            try
            {
                var response = ExecuteWriteOperation(operation);
                return new FindAndModifyResult(response);
            }
            catch (MongoCommandException ex)
            {
                if (ex.ErrorMessage == "No matching object found")
                {
                    // create a new command result with what the server should have responded
                    var response = new BsonDocument
                    {
                        { "value", BsonNull.Value },
                        { "ok", true }
                    };
                    return new FindAndModifyResult(response);
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
        [Obsolete("Use the overload of FindAndRemove that has a FindAndRemoveArgs parameter instead.")]
        public virtual FindAndModifyResult FindAndRemove(IMongoQuery query, IMongoSortBy sortBy)
        {
            var args = new FindAndRemoveArgs { Query = query, SortBy = sortBy };
            return FindAndRemove(args);
        }

        /// <summary>
        /// Finds one matching document using the supplied args and removes it from this collection.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>A <see cref="FindAndModifyResult"/>.</returns>
        public virtual FindAndModifyResult FindAndRemove(FindAndRemoveArgs args)
        {
            if (args == null) { throw new ArgumentNullException("args"); }

            var filter = args.Query == null ? new BsonDocument() : new BsonDocumentWrapper(args.Query);
            var resultSerializer = BsonDocumentSerializer.Instance;
            var messageEncoderSettings = GetMessageEncoderSettings();
            var projection = args.Fields == null ? null : new BsonDocumentWrapper(args.Fields);
            var sort = args.SortBy == null ? null : new BsonDocumentWrapper(args.SortBy);

            var operation = new FindOneAndDeleteOperation<BsonDocument>(_collectionNamespace, filter, resultSerializer, messageEncoderSettings)
            {
                Collation = args.Collation,
                MaxTime = args.MaxTime,
                Projection = projection,
                Sort = sort,
                WriteConcern = _settings.WriteConcern
            };

            try
            {
                var response = ExecuteWriteOperation(operation);
                return new FindAndModifyResult(response);
            }
            catch (MongoCommandException ex)
            {
                if (ex.ErrorMessage == "No matching object found")
                {
                    // create a new command result with what the server should have responded
                    var response = new BsonDocument
                    {
                        { "value", BsonNull.Value },
                        { "ok", true }
                    };
                    return new FindAndModifyResult(response);
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
            var serializer = BsonSerializer.LookupSerializer(typeof(TDocument));
            return FindAs<TDocument>(query, serializer);
        }

        /// <summary>
        /// Returns a cursor that can be used to find all documents in this collection that match the query as TDocuments.
        /// </summary>
        /// <param name="documentType">The nominal type of the documents.</param>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>A <see cref="MongoCursor{TDocument}"/>.</returns>
        public virtual MongoCursor FindAs(Type documentType, IMongoQuery query)
        {
            var serializer = BsonSerializer.LookupSerializer(documentType);
            return FindAs(documentType, query, serializer);
        }

        /// <summary>
        /// Returns one document in this collection as a TDocument.
        /// </summary>
        /// <typeparam name="TDocument">The type to deserialize the documents as.</typeparam>
        /// <returns>A TDocument (or null if not found).</returns>
        public virtual TDocument FindOneAs<TDocument>()
        {
            var args = new FindOneArgs { Query = null };
            return FindOneAs<TDocument>(args);
        }

        /// <summary>
        /// Returns one document in this collection as a TDocument.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="args">The args.</param>
        /// <returns>A TDocument (or null if not found).</returns>
        public virtual TDocument FindOneAs<TDocument>(FindOneArgs args)
        {
            var modifiers = new BsonDocument();
            var queryDocument = args.Query == null ? new BsonDocument() : args.Query.ToBsonDocument();
            var serializer = BsonSerializer.LookupSerializer<TDocument>();
            var messageEncoderSettings = GetMessageEncoderSettings();
            var fields = args.Fields == null ? null : args.Fields.ToBsonDocument();
            if (args.Hint != null)
            {
                modifiers["$hint"] = IndexNameHelper.GetIndexName(args.Hint);
            }

            var operation = new FindOperation<TDocument>(_collectionNamespace, serializer, messageEncoderSettings)
            {
                Collation = args.Collation,
                Filter = queryDocument,
                Limit = -1,
                MaxTime = args.MaxTime,
                Projection = fields,
                Skip = args.Skip,
                Sort = args.SortBy.ToBsonDocument()
            };

            using (var cursor = ExecuteReadOperation(operation, args.ReadPreference))
            {
                if (cursor.MoveNext(CancellationToken.None))
                {
                    return cursor.Current.SingleOrDefault();
                }

                return default(TDocument);
            }
        }

        /// <summary>
        /// Returns one document in this collection that matches a query as a TDocument.
        /// </summary>
        /// <typeparam name="TDocument">The type to deserialize the documents as.</typeparam>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>A TDocument (or null if not found).</returns>
        public virtual TDocument FindOneAs<TDocument>(IMongoQuery query)
        {
            var args = new FindOneArgs { Query = query };
            return FindOneAs<TDocument>(args);
        }

        /// <summary>
        /// Returns one document in this collection as a TDocument.
        /// </summary>
        /// <param name="documentType">The nominal type of the documents.</param>
        /// <returns>A document (or null if not found).</returns>
        public virtual object FindOneAs(Type documentType)
        {
            var args = new FindOneArgs { Query = null };
            return FindOneAs(documentType, args);
        }

        /// <summary>
        /// Returns one document in this collection as a TDocument.
        /// </summary>
        /// <param name="documentType">The nominal type of the documents.</param>
        /// <param name="args">The args.</param>
        /// <returns>A document (or null if not found).</returns>
        public virtual object FindOneAs(Type documentType, FindOneArgs args)
        {
            var methodDefinition = GetType().GetTypeInfo().GetMethod("FindOneAs", new Type[] { typeof(FindOneArgs) });
            var methodInfo = methodDefinition.MakeGenericMethod(documentType);
            try
            {
                return methodInfo.Invoke(this, new object[] { args });
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        /// <summary>
        /// Returns one document in this collection that matches a query as a TDocument.
        /// </summary>
        /// <param name="documentType">The type to deserialize the documents as.</param>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>A TDocument (or null if not found).</returns>
        public virtual object FindOneAs(Type documentType, IMongoQuery query)
        {
            var args = new FindOneArgs { Query = query };
            return FindOneAs(documentType, args);
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
        [Obsolete("Use the overload of GeoHaystackSearchAs that has a GeoHaystackSearchArgs parameter instead.")]
        public virtual GeoHaystackSearchResult<TDocument> GeoHaystackSearchAs<TDocument>(
            double x,
            double y,
            IMongoGeoHaystackSearchOptions options)
        {
            var optionsDocument = options.ToBsonDocument();
            var args = new GeoHaystackSearchArgs
            {
                Near = new XYPoint(x, y),
                MaxDistance = optionsDocument.Contains("maxDistance") ? (int?)optionsDocument["maxDistance"].ToInt32() : null,
                Limit = optionsDocument.Contains("limit") ? (int?)optionsDocument["limit"].ToInt32() : null
            };
            if (optionsDocument.Contains("search"))
            {
                var searchElement = optionsDocument["search"].AsBsonDocument.GetElement(0);
                args.AdditionalFieldName = searchElement.Name;
                args.AdditionalFieldValue = searchElement.Value;
            }
            return GeoHaystackSearchAs<TDocument>(args);
        }

        /// <summary>
        /// Runs a geoHaystack search command on this collection.
        /// </summary>
        /// <typeparam name="TDocument">The type of the found documents.</typeparam>
        /// <param name="args">The args.</param>
        /// <returns>A <see cref="GeoNearResult{TDocument}"/>.</returns>
        public virtual GeoHaystackSearchResult<TDocument> GeoHaystackSearchAs<TDocument>(GeoHaystackSearchArgs args)
        {
            if (args == null) { throw new ArgumentNullException("args"); }
            if (args.Near == null) { throw new ArgumentException("Near is null.", "args"); }

            BsonDocument search = null;
            if (args.AdditionalFieldName != null && args.AdditionalFieldValue != null)
            {
                search = new BsonDocument(args.AdditionalFieldName, args.AdditionalFieldValue);
            }

            var operation = new GeoSearchOperation<GeoHaystackSearchResult<TDocument>>(
                _collectionNamespace,
                new BsonArray { args.Near.X, args.Near.Y },
                _settings.SerializerRegistry.GetSerializer<GeoHaystackSearchResult<TDocument>>(),
                GetMessageEncoderSettings())
            {
                Limit = args.Limit,
                MaxDistance = args.MaxDistance,
                MaxTime = args.MaxTime,
                ReadConcern = _settings.ReadConcern,
                Search = search
            };

            return ExecuteReadOperation(operation);
        }

        /// <summary>
        /// Runs a geoHaystack search command on this collection.
        /// </summary>
        /// <param name="documentType">The type to deserialize the documents as.</param>
        /// <param name="x">The x coordinate of the starting location.</param>
        /// <param name="y">The y coordinate of the starting location.</param>
        /// <param name="options">The options for the geoHaystack search (null if none).</param>
        /// <returns>A <see cref="GeoNearResult{TDocument}"/>.</returns>
        [Obsolete("Use the overload of GeoHaystackSearchAs that has a GeoHaystackSearchArgs parameter instead.")]
        public virtual GeoHaystackSearchResult GeoHaystackSearchAs(
            Type documentType,
            double x,
            double y,
            IMongoGeoHaystackSearchOptions options)
        {
            var methodDefinition = GetType().GetTypeInfo().GetMethod("GeoHaystackSearchAs", new Type[] { typeof(double), typeof(double), typeof(IMongoGeoHaystackSearchOptions) });
            var methodInfo = methodDefinition.MakeGenericMethod(documentType);
            return (GeoHaystackSearchResult)methodInfo.Invoke(this, new object[] { x, y, options });
        }

        /// <summary>
        /// Runs a geoHaystack search command on this collection.
        /// </summary>
        /// <param name="documentType">The type to deserialize the documents as.</param>
        /// <param name="args">The args.</param>
        /// <returns>A <see cref="GeoNearResult{TDocument}"/>.</returns>
        public virtual GeoHaystackSearchResult GeoHaystackSearchAs(Type documentType, GeoHaystackSearchArgs args)
        {
            var methodDefinition = GetType().GetTypeInfo().GetMethod("GeoHaystackSearchAs", new Type[] { typeof(GeoHaystackSearchArgs) });
            var methodInfo = methodDefinition.MakeGenericMethod(documentType);
            return (GeoHaystackSearchResult)methodInfo.Invoke(this, new object[] { args });
        }

        /// <summary>
        /// Runs a GeoNear command on this collection.
        /// </summary>
        /// <typeparam name="TDocument">The type to deserialize the documents as.</typeparam>
        /// <param name="args">The args.</param>
        /// <returns>A <see cref="GeoNearResult{TDocument}"/>.</returns>
        public virtual GeoNearResult<TDocument> GeoNearAs<TDocument>(GeoNearArgs args)
        {
            if (args == null) { throw new ArgumentNullException("args"); }
            if (args.Near == null) { throw new ArgumentException("Near is null.", "args"); }

            var operation = new GeoNearOperation<GeoNearResult<TDocument>>(
                _collectionNamespace,
                args.Near.ToGeoNearCommandValue(),
                _settings.SerializerRegistry.GetSerializer<GeoNearResult<TDocument>>(),
                GetMessageEncoderSettings())
            {
                Collation = args.Collation,
                DistanceMultiplier = args.DistanceMultiplier,
                Filter = BsonDocumentWrapper.Create(args.Query),
                IncludeLocs = args.IncludeLocs,
                Limit = args.Limit,
                MaxDistance = args.MaxDistance,
                MaxTime = args.MaxTime,
                ReadConcern = _settings.ReadConcern,
                Spherical = args.Spherical,
                UniqueDocs = args.UniqueDocs
            };

            var result = ExecuteReadOperation(operation, _settings.ReadPreference);
            result.Response["ns"] = FullName;
            return result;
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
        [Obsolete("Use the overload of GeoNearAs that has a GeoNearArgs parameter instead.")]
        public virtual GeoNearResult<TDocument> GeoNearAs<TDocument>(
            IMongoQuery query,
            double x,
            double y,
            int limit)
        {
#pragma warning disable 618
            return GeoNearAs<TDocument>(query, x, y, limit, GeoNearOptions.Null);
#pragma warning restore
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
        [Obsolete("Use the overload of GeoNearAs that has a GeoNearArgs parameter instead.")]
        public virtual GeoNearResult<TDocument> GeoNearAs<TDocument>(
            IMongoQuery query,
            double x,
            double y,
            int limit,
            IMongoGeoNearOptions options)
        {
            var optionsDocument = options.ToBsonDocument();
            var args = new GeoNearArgs
            {
                Near = new XYPoint(x, y),
                Limit = limit,
                Query = query,
                DistanceMultiplier = optionsDocument.Contains("distanceMultiplier") ? (double?)optionsDocument["distanceMultiplier"].ToDouble() : null,
                MaxDistance = optionsDocument.Contains("maxDistance") ? (double?)optionsDocument["maxDistance"].ToDouble() : null,
                Spherical = optionsDocument.Contains("spherical") ? (bool?)optionsDocument["spherical"].ToBoolean() : null
            };
            return GeoNearAs<TDocument>(args);
        }

        /// <summary>
        /// Runs a GeoNear command on this collection.
        /// </summary>
        /// <param name="documentType">The type to deserialize the documents as.</param>
        /// <param name="args">The args.</param>
        /// <returns>A <see cref="GeoNearResult{TDocument}"/>.</returns>
        public virtual GeoNearResult GeoNearAs(Type documentType, GeoNearArgs args)
        {
            var methodDefinition = GetType().GetTypeInfo().GetMethod("GeoNearAs", new Type[] { typeof(GeoNearArgs) });
            var methodInfo = methodDefinition.MakeGenericMethod(documentType);
            return (GeoNearResult)methodInfo.Invoke(this, new object[] { args });
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
        [Obsolete("Use the overload of GeoNearAs that has a GeoNearArgs parameter instead.")]
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
        [Obsolete("Use the overload of GeoNearAs that has a GeoNearArgs parameter instead.")]
        public virtual GeoNearResult GeoNearAs(
            Type documentType,
            IMongoQuery query,
            double x,
            double y,
            int limit,
            IMongoGeoNearOptions options)
        {
            var methodDefinition = GetType().GetTypeInfo().GetMethod("GeoNearAs", new Type[] { typeof(IMongoQuery), typeof(double), typeof(double), typeof(int), typeof(IMongoGeoNearOptions) });
            var methodInfo = methodDefinition.MakeGenericMethod(documentType);
            return (GeoNearResult)methodInfo.Invoke(this, new object[] { query, x, y, limit, options });
        }

        /// <summary>
        /// Gets the indexes for this collection.
        /// </summary>
        /// <returns>A list of BsonDocuments that describe the indexes.</returns>
        public virtual GetIndexesResult GetIndexes()
        {
            var operation = new ListIndexesOperation(_collectionNamespace, GetMessageEncoderSettings());
            var cursor = ExecuteReadOperation(operation, ReadPreference.Primary);
            var list = cursor.ToList();
            return new GetIndexesResult(list.ToArray());
        }

        /// <summary>
        /// Gets the stats for this collection.
        /// </summary>
        /// <returns>The stats for this collection as a <see cref="CollectionStatsResult"/>.</returns>
        public virtual CollectionStatsResult GetStats()
        {
            return GetStats(new GetStatsArgs());
        }

        /// <summary>
        /// Gets the stats for this collection.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>The stats for this collection as a <see cref="CollectionStatsResult"/>.</returns>
        public virtual CollectionStatsResult GetStats(GetStatsArgs args)
        {
            if (args == null) { throw new ArgumentNullException("args"); }

            var command = new CommandDocument
            {
                { "collstats", _collectionNamespace.CollectionName },
                { "scale", () => args.Scale.Value, args.Scale.HasValue }, // optional
                { "maxTimeMS", () => args.MaxTime.Value.TotalMilliseconds, args.MaxTime.HasValue } // optional
            };
            return _database.RunCommandAs<CollectionStatsResult>(command, ReadPreference.Primary);
        }

        /// <summary>
        /// Runs the group command on this collection.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>A list of results as BsonDocuments.</returns>
        public virtual IEnumerable<BsonDocument> Group(GroupArgs args)
        {
            if (args == null) { throw new ArgumentNullException("args"); }
            if (args.KeyFields == null && args.KeyFunction == null)
            {
                throw new ArgumentException("KeyFields and KeyFunction are both null.", "args");
            }
            if (args.KeyFields != null && args.KeyFunction != null)
            {
                throw new ArgumentException("KeyFields and KeyFunction are mutually exclusive.", "args");
            }
            if (args.Initial == null)
            {
                throw new ArgumentException("Initial is null.", "args");
            }
            if (args.ReduceFunction == null)
            {
                throw new ArgumentException("ReduceFunction is null.", "args");
            }

            var filter = args.Query == null ? null : BsonDocumentWrapper.Create(args.Query);
            var messageEncoderSettings = GetMessageEncoderSettings();

            GroupOperation<BsonDocument> operation;
            if (args.KeyFields != null)
            {
                var key = new BsonDocumentWrapper(args.KeyFields);
                operation = new GroupOperation<BsonDocument>(_collectionNamespace, key, args.Initial, args.ReduceFunction, filter, messageEncoderSettings);
            }
            else
            {
                operation = new GroupOperation<BsonDocument>(_collectionNamespace, args.KeyFunction, args.Initial, args.ReduceFunction, filter, messageEncoderSettings);
            }
            operation.Collation = args.Collation;
            operation.FinalizeFunction = args.FinalizeFunction;
            operation.MaxTime = args.MaxTime;

            var readPreference = _settings.ReadPreference ?? ReadPreference.Primary;
            using (var binding = _server.GetReadBinding(readPreference))
            {
                return operation.Execute(binding, CancellationToken.None);
            }
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
            return Group(new GroupArgs
            {
                Query = query,
                KeyFunction = keyFunction,
                Initial = initial,
                ReduceFunction = reduce,
                FinalizeFunction = finalize
            });
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
            return Group(new GroupArgs
            {
                Query = query,
                KeyFields = keys,
                Initial = initial,
                ReduceFunction = reduce,
                FinalizeFunction = finalize
            });
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
            return Group(new GroupArgs
            {
                Query = query,
                KeyFields = GroupBy.Keys(key),
                Initial = initial,
                ReduceFunction = reduce,
                FinalizeFunction = finalize
            });
        }

        /// <summary>
        /// Tests whether an index exists.
        /// </summary>
        /// <param name="keys">The indexed fields (usually an IndexKeysDocument or constructed using the IndexKeys builder).</param>
        /// <returns>True if the index exists.</returns>
        public virtual bool IndexExists(IMongoIndexKeys keys)
        {
            string indexName = IndexNameHelper.GetIndexName(keys.ToBsonDocument());
            return IndexExistsByName(indexName);
        }

        /// <summary>
        /// Tests whether an index exists.
        /// </summary>
        /// <param name="keyNames">The names of the fields in the index.</param>
        /// <returns>True if the index exists.</returns>
        public virtual bool IndexExists(params string[] keyNames)
        {
            string indexName = IndexNameHelper.GetIndexName(keyNames);
            return IndexExistsByName(indexName);
        }

        /// <summary>
        /// Tests whether an index exists.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <returns>True if the index exists.</returns>
        public virtual bool IndexExistsByName(string indexName)
        {
            var operation = new ListIndexesOperation(_collectionNamespace, GetMessageEncoderSettings());
            var indexes = ExecuteReadOperation(operation, ReadPreference.Primary).ToList();
            return indexes.Any(index => index["name"].AsString == indexName);
        }

        /// <summary>
        /// Creates a fluent builder for an ordered bulk operation.
        /// </summary>
        /// <typeparam name="TDocument">The type of the documents.</typeparam>
        /// <returns>A fluent bulk operation builder.</returns>
        public virtual BulkWriteOperation<TDocument> InitializeOrderedBulkOperationAs<TDocument>()
        {
            return new BulkWriteOperation<TDocument>(this, true);
        }

        /// <summary>
        /// Creates a fluent builder for an unordered bulk operation.
        /// </summary>
        /// <typeparam name="TDocument">The type of the documents.</typeparam>
        /// <returns>A fluent bulk operation builder.</returns>
        public virtual BulkWriteOperation<TDocument> InitializeUnorderedBulkOperationAs<TDocument>()
        {
            return new BulkWriteOperation<TDocument>(this, false);
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
            var options = new MongoInsertOptions();
            return Insert(document, options);
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
            if (document == null)
            {
                throw new ArgumentNullException("document");
            }
            var results = InsertBatch(new[] { document }, options);
            return (results == null) ? null : results.Single();
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
            var options = new MongoInsertOptions { WriteConcern = writeConcern };
            return Insert(document, options);
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
            var options = new MongoInsertOptions { WriteConcern = writeConcern };
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
            var options = new MongoInsertOptions();
            return InsertBatch(documents, options);
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
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            if (_settings.AssignIdOnInsert)
            {
                documents = documents.Select(d => { AssignId(d, null); return d; });
            }

            using (var enumerator = documents.GetEnumerator())
            {
                var documentSource = new BatchableSource<TNominalType>(enumerator);
                var serializer = BsonSerializer.LookupSerializer<TNominalType>();
                var messageEncoderSettings = GetMessageEncoderSettings();
                var continueOnError = (options.Flags & InsertFlags.ContinueOnError) == InsertFlags.ContinueOnError;
                var writeConcern = options.WriteConcern ?? _settings.WriteConcern;

                var operation = new InsertOpcodeOperation<TNominalType>(_collectionNamespace, documentSource, serializer, messageEncoderSettings)
                {
                    BypassDocumentValidation = options.BypassDocumentValidation,
                    ContinueOnError = continueOnError,
                    WriteConcern = writeConcern
                };

                return ExecuteWriteOperation(operation);
            }
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
            var options = new MongoInsertOptions { WriteConcern = writeConcern };
            return InsertBatch(typeof(TNominalType), documents, options);
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
            var options = new MongoInsertOptions { WriteConcern = writeConcern };
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
            if (nominalType == null)
            {
                throw new ArgumentNullException("nominalType");
            }

            var methodDefinition = typeof(MongoCollection).GetTypeInfo().GetMethod("InsertBatchInvoker", BindingFlags.NonPublic | BindingFlags.Instance);
            var methodInfo = methodDefinition.MakeGenericMethod(nominalType);
            return (IEnumerable<WriteConcernResult>)methodInfo.Invoke(this, new object[] { documents, options });
        }

        private IEnumerable<WriteConcernResult> InsertBatchInvoker<TDocument>(IEnumerable documents, MongoInsertOptions options)
        {
            return InsertBatch<TDocument>(documents.Cast<TDocument>(), options);
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
        /// Runs a map-reduce command on this collection.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>A <see cref="MapReduceResult"/>.</returns>
        public virtual MapReduceResult MapReduce(MapReduceArgs args)
        {
            if (args == null) { throw new ArgumentNullException("args"); }
            if (args.MapFunction == null) { throw new ArgumentException("MapFunction is null.", "args"); }
            if (args.ReduceFunction == null) { throw new ArgumentException("ReduceFunction is null.", "args"); }

            var query = args.Query == null ? null : BsonDocumentWrapper.Create(args.Query);
            var messageEncoderSettings = GetMessageEncoderSettings();
            var scope = args.Scope == null ? null : BsonDocumentWrapper.Create(args.Scope);
            var sort = args.SortBy == null ? null : BsonDocumentWrapper.Create(args.SortBy);

            BsonDocument response;
            if (args.OutputMode == MapReduceOutputMode.Inline)
            {
                var operation = new MapReduceLegacyOperation(
                    _collectionNamespace,
                    args.MapFunction,
                    args.ReduceFunction,
                    messageEncoderSettings)
                {
                    Collation = args.Collation,
                    Filter = query,
                    FinalizeFunction = args.FinalizeFunction,
                    JavaScriptMode = args.JsMode,
                    Limit = args.Limit,
                    MaxTime = args.MaxTime,
                    ReadConcern = _settings.ReadConcern,
                    Scope = scope,
                    Sort = sort,
                    Verbose = args.Verbose
                };

                response = ExecuteReadOperation(operation);
            }
            else
            {
                var outputDatabaseNamespace = args.OutputDatabaseName == null ? _collectionNamespace.DatabaseNamespace : new DatabaseNamespace(args.OutputDatabaseName);
                var outputCollectionNamespace = new CollectionNamespace(outputDatabaseNamespace, args.OutputCollectionName);
                var outputMode = args.OutputMode.ToCore();

                var operation = new MapReduceOutputToCollectionOperation(
                    _collectionNamespace,
                    outputCollectionNamespace,
                    args.MapFunction,
                    args.ReduceFunction,
                    messageEncoderSettings)
                {
                    BypassDocumentValidation = args.BypassDocumentValidation,
                    Collation = args.Collation,
                    Filter = query,
                    FinalizeFunction = args.FinalizeFunction,
                    JavaScriptMode = args.JsMode,
                    Limit = args.Limit,
                    MaxTime = args.MaxTime,
                    NonAtomicOutput = args.OutputIsNonAtomic,
                    OutputMode = outputMode,
                    Scope = scope,
                    ShardedOutput = args.OutputIsSharded,
                    Sort = sort,
                    Verbose = args.Verbose,
                    WriteConcern = _settings.WriteConcern
                };

                response = ExecuteWriteOperation(operation);
            }

            var result = new MapReduceResult(response);
            result.SetInputDatabase(_database);

            return result;
        }

        /// <summary>
        /// Scans an entire collection in parallel using multiple cursors.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="args">The args.</param>
        /// <returns>Multiple enumerators, one for each cursor.</returns>
        public ReadOnlyCollection<IEnumerator<TDocument>> ParallelScanAs<TDocument>(ParallelScanArgs<TDocument> args)
        {
            var batchSize = args.BatchSize;
            var serializer = args.Serializer ?? BsonSerializer.LookupSerializer<TDocument>();
            var messageEncoderSettings = GetMessageEncoderSettings();

            var operation = new ParallelScanOperation<TDocument>(_collectionNamespace, args.NumberOfCursors, serializer, messageEncoderSettings)
            {
                BatchSize = batchSize,
                ReadConcern = _settings.ReadConcern
            };

            var cursors = ExecuteReadOperation(operation, args.ReadPreference);
            var documentEnumerators = cursors.Select(c => c.ToEnumerable().GetEnumerator()).ToList();
            return new ReadOnlyCollection<IEnumerator<TDocument>>(documentEnumerators);
        }

        /// <summary>
        /// Scans an entire collection in parallel using multiple cursors.
        /// </summary>
        /// <param name="documentType">Type of the document.</param>
        /// <param name="args">The args.</param>
        /// <returns>Multiple enumerators, one for each cursor.</returns>
        public ReadOnlyCollection<IEnumerator> ParallelScanAs(Type documentType, ParallelScanArgs args)
        {
            var parallelScanArgsDefinition = typeof(ParallelScanArgs<>);
            var parallelScanArgsType = parallelScanArgsDefinition.MakeGenericType(documentType);
            if (args.GetType() == typeof(ParallelScanArgs))
            {
                var genericArgs = (ParallelScanArgs)Activator.CreateInstance(parallelScanArgsType);
                genericArgs.BatchSize = args.BatchSize;
                genericArgs.NumberOfCursors = args.NumberOfCursors;
                genericArgs.ReadPreference = args.ReadPreference;
                genericArgs.Serializer = args.Serializer;
                args = genericArgs;
            }
            else if (args.GetType() != parallelScanArgsType)
            {
                var message = string.Format("Invalid args type. Expected '{0}', was '{1}'.",
                    BsonUtils.GetFriendlyTypeName(parallelScanArgsType),
                    BsonUtils.GetFriendlyTypeName(args.GetType()));
                throw new ArgumentException(message, "args");
            }

            var methodDefinition = GetType().GetTypeInfo().GetMethods().Where(m => m.Name == "ParallelScanAs" && m.IsGenericMethodDefinition).Single();
            var methodInfo = methodDefinition.MakeGenericMethod(documentType);
            try
            {
                return ((IEnumerable)methodInfo.Invoke(this, new object[] { args })).Cast<IEnumerator>().ToList().AsReadOnly();
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        /// <summary>
        /// Runs the ReIndex command on this collection.
        /// </summary>
        /// <returns>A CommandResult.</returns>
        public virtual CommandResult ReIndex()
        {
            var operation = new ReIndexOperation(_collectionNamespace, GetMessageEncoderSettings())
            {
                WriteConcern = _settings.WriteConcern
            };
            var result = ExecuteWriteOperation(operation);
            return new CommandResult(result);
        }

        /// <summary>
        /// Removes documents from this collection that match a query.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Remove(IMongoQuery query)
        {
            return Remove(new RemoveArgs { Query = query });
        }

        /// <summary>
        /// Removes documents from this collection that match a query.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="writeConcern">The write concern to use for this Insert.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Remove(IMongoQuery query, WriteConcern writeConcern)
        {
            return Remove(new RemoveArgs { Query = query, WriteConcern = writeConcern });
        }

        /// <summary>
        /// Removes documents from this collection that match a query.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="flags">The flags for this Remove (see <see cref="RemoveFlags"/>).</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Remove(IMongoQuery query, RemoveFlags flags)
        {
            return Remove(new RemoveArgs { Query = query, Flags = flags });
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
            return Remove(new RemoveArgs { Query = query, Flags = flags, WriteConcern = writeConcern });
        }

        /// <summary>
        /// Removes documents from this collection that match a query.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult Remove(RemoveArgs args)
        {
            var queryDocument = args.Query == null ? new BsonDocument() : args.Query.ToBsonDocument();
            var messageEncoderSettings = GetMessageEncoderSettings();
            var isMulti = (args.Flags & RemoveFlags.Single) != RemoveFlags.Single;
            var writeConcern = args.WriteConcern ?? _settings.WriteConcern ?? WriteConcern.Acknowledged;

            var request = new DeleteRequest(queryDocument)
            {
                Collation = args.Collation,
                Limit = isMulti ? 0 : 1
            };
            var operation = new DeleteOpcodeOperation(_collectionNamespace, request, messageEncoderSettings)
            {
                WriteConcern = writeConcern
            };

            return ExecuteWriteOperation(operation);
        }

        /// <summary>
        /// Removes all documents from this collection (see also <see cref="Drop"/>).
        /// </summary>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult RemoveAll()
        {
            return Remove(new RemoveArgs { Query = new QueryDocument() });
        }

        /// <summary>
        /// Removes all documents from this collection (see also <see cref="Drop"/>).
        /// </summary>
        /// <param name="writeConcern">The write concern to use for this Insert.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        public virtual WriteConcernResult RemoveAll(WriteConcern writeConcern)
        {
            return Remove(new RemoveArgs { Query = new QueryDocument(), WriteConcern = writeConcern });
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
            if (nominalType == null)
            {
                throw new ArgumentNullException("nominalType");
            }
            if (document == null)
            {
                throw new ArgumentNullException("document");
            }

            var serializer = BsonSerializer.LookupSerializer(document.GetType());

            // if we can determine for sure that it is a new document and we can generate an Id for it then insert it
            var idProvider = serializer as IBsonIdProvider;
            if (idProvider != null)
            {
                object id;
                Type idNominalType;
                IIdGenerator idGenerator;
                var hasId = idProvider.GetDocumentId(document, out id, out idNominalType, out idGenerator);

                if (idGenerator == null && (!hasId || id == null))
                {
                    throw new InvalidOperationException("No IdGenerator found.");
                }

                if (idGenerator != null && (!hasId || idGenerator.IsEmpty(id)))
                {
                    id = idGenerator.GenerateId(this, document);
                    idProvider.SetDocumentId(document, id);
                    return Insert(nominalType, document, options);
                }
            }

            // since we can't determine for sure whether it's a new document or not upsert it
            // the only safe way to get the serialized _id value needed for the query is to serialize the entire document
            var bsonDocument = new BsonDocument();
            var writerSettings = new BsonDocumentWriterSettings { GuidRepresentation = _settings.GuidRepresentation };
            using (var bsonWriter = new BsonDocumentWriter(bsonDocument, writerSettings))
            {
                var context = BsonSerializationContext.CreateRoot(bsonWriter);
                var args = new BsonSerializationArgs { NominalType = nominalType };
                serializer.Serialize(context, args, document);
            }

            BsonValue idBsonValue;
            if (!bsonDocument.TryGetValue("_id", out idBsonValue))
            {
                throw new InvalidOperationException("Save can only be used with documents that have an Id.");
            }

            var query = Query.EQ("_id", idBsonValue);
            var update = Builders.Update.Replace(bsonDocument);
            var updateOptions = new MongoUpdateOptions
            {
                Flags = UpdateFlags.Upsert,
                WriteConcern = options.WriteConcern
            };

            return Update(query, update, updateOptions);
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
            var options = new MongoInsertOptions { WriteConcern = writeConcern };
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

            var queryDocument = query == null ? new BsonDocument() : query.ToBsonDocument();
            var updateDocument = update.ToBsonDocument();
            var messageEncoderSettings = GetMessageEncoderSettings();
            var isMulti = (options.Flags & UpdateFlags.Multi) == UpdateFlags.Multi;
            var isUpsert = (options.Flags & UpdateFlags.Upsert) == UpdateFlags.Upsert;
            var writeConcern = options.WriteConcern ?? _settings.WriteConcern ?? WriteConcern.Acknowledged;

            var request = new UpdateRequest(UpdateType.Unknown, queryDocument, updateDocument)
            {
                Collation = options.Collation,
                IsMulti = isMulti,
                IsUpsert = isUpsert
            };
            var operation = new UpdateOpcodeOperation(_collectionNamespace, request, messageEncoderSettings)
            {
                BypassDocumentValidation = options.BypassDocumentValidation,
                WriteConcern = writeConcern
            };

            return ExecuteWriteOperation(operation);
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
            var options = new MongoUpdateOptions { WriteConcern = writeConcern };
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
            return Validate(new ValidateCollectionArgs());
        }

        /// <summary>
        /// Validates the integrity of this collection.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>A <see cref="ValidateCollectionResult"/>.</returns>
        public virtual ValidateCollectionResult Validate(ValidateCollectionArgs args)
        {
            if (args == null) { throw new ArgumentNullException("args"); }

            var command = new CommandDocument
            {
                { "validate", _collectionNamespace.CollectionName },
                { "full", () => args.Full.Value, args.Full.HasValue }, // optional
                { "scandata", () => args.ScanData.Value, args.ScanData.HasValue }, // optional
                { "maxTimeMS", () => args.MaxTime.Value.TotalMilliseconds, args.MaxTime.HasValue } // optional
            };
            return _database.RunCommandAs<ValidateCollectionResult>(command, ReadPreference.Primary);
        }

        // internal methods
        internal MessageEncoderSettings GetMessageEncoderSettings()
        {
            return new MessageEncoderSettings
            {
                { MessageEncoderSettingsName.GuidRepresentation, _settings.GuidRepresentation },
                { MessageEncoderSettingsName.ReadEncoding, _settings.ReadEncoding ?? Utf8Encodings.Strict },
                { MessageEncoderSettingsName.WriteEncoding, _settings.WriteEncoding ?? Utf8Encodings.Strict }
            };
        }

        // private methods
        internal void AssignId(object document, IBsonSerializer serializer)
        {
            if (document != null)
            {
                var actualType = document.GetType();
                if (serializer == null)
                {
                    serializer = BsonSerializer.LookupSerializer(actualType);
                }

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
                            id = idGenerator.GenerateId((MongoCollection)this, document);
                            idProvider.SetDocumentId(document, id);
                        }
                    }
                }
            }
        }

        internal TResult ExecuteReadOperation<TResult>(IReadOperation<TResult> operation, ReadPreference readPreference = null)
        {
            readPreference = readPreference ?? _settings.ReadPreference ?? ReadPreference.Primary;
            using (var binding = _server.GetReadBinding(readPreference))
            {
                return operation.Execute(binding, CancellationToken.None);
            }
        }

        internal TResult ExecuteWriteOperation<TResult>(IWriteOperation<TResult> operation)
        {
            using (var binding = _server.GetWriteBinding())
            {
                return operation.Execute(binding, CancellationToken.None);
            }
        }

        private MongoCursor FindAs(Type documentType, IMongoQuery query, IBsonSerializer serializer)
        {
            return MongoCursor.Create(documentType, this, query, _settings.ReadConcern, _settings.ReadPreference, serializer);
        }

        private MongoCursor<TDocument> FindAs<TDocument>(IMongoQuery query, IBsonSerializer serializer)
        {
            return new MongoCursor<TDocument>(this, query, _settings.ReadConcern, _settings.ReadPreference, serializer);
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
        /// <param name="name">The name of the collection.</param>
        /// <param name="settings">The settings to use to access this collection.</param>
        public MongoCollection(MongoDatabase database, string name, MongoCollectionSettings settings)
            : base(database, name, settings)
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
        /// Returns one document in this collection as a TDefaultDocument.
        /// </summary>
        /// <returns>A TDefaultDocument (or null if not found).</returns>
        public virtual TDefaultDocument FindOne()
        {
            return FindOneAs<TDefaultDocument>();
        }

        /// <summary>
        /// Returns one document in this collection that matches a query as a TDefaultDocument.
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
        [Obsolete("Use the overload of GeoHaystackSearch that has a GeoHaystackSearchArgs parameter instead.")]
        public virtual GeoHaystackSearchResult<TDefaultDocument> GeoHaystackSearch(
            double x,
            double y,
            IMongoGeoHaystackSearchOptions options)
        {
            return GeoHaystackSearchAs<TDefaultDocument>(x, y, options);
        }

        /// <summary>
        /// Runs a geoHaystack search command on this collection.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>A <see cref="GeoHaystackSearchResult{TDocument}"/>.</returns>
        public virtual GeoHaystackSearchResult<TDefaultDocument> GeoHaystackSearch(GeoHaystackSearchArgs args)
        {
            return GeoHaystackSearchAs<TDefaultDocument>(args);
        }

        /// <summary>
        /// Runs a GeoNear command on this collection.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>A <see cref="GeoNearResult{TDefaultDocument}"/>.</returns>
        public virtual GeoNearResult<TDefaultDocument> GeoNear(GeoNearArgs args)
        {
            return GeoNearAs<TDefaultDocument>(args);
        }

        /// <summary>
        /// Runs a GeoNear command on this collection.
        /// </summary>
        /// <param name="query">The query (usually a QueryDocument or constructed using the Query builder).</param>
        /// <param name="x">The x coordinate of the starting location.</param>
        /// <param name="y">The y coordinate of the starting location.</param>
        /// <param name="limit">The maximum number of results returned.</param>
        /// <returns>A <see cref="GeoNearResult{TDefaultDocument}"/>.</returns>
        [Obsolete("Use the overload of GeoNear that takes a GeoNearArgs parameter instead.")]
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
        [Obsolete("Use the overload of GeoNear that takes a GeoNearArgs parameter instead.")]
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
        /// Creates a fluent builder for an ordered bulk operation.
        /// </summary>
        /// <returns>A fluent bulk operation builder.</returns>
        public virtual BulkWriteOperation<TDefaultDocument> InitializeOrderedBulkOperation()
        {
            return new BulkWriteOperation<TDefaultDocument>(this, true);
        }

        /// <summary>
        /// Creates a fluent builder for an unordered bulk operation.
        /// </summary>
        /// <returns>A fluent bulk operation builder.</returns>
        public virtual BulkWriteOperation<TDefaultDocument> InitializeUnorderedBulkOperation()
        {
            return new BulkWriteOperation<TDefaultDocument>(this, false);
        }

        /// <summary>
        /// Inserts a document into this collection (see also InsertBatch to insert multiple documents at once).
        /// </summary>
        /// <param name="document">The document to insert.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1061:DoNotHideBaseClassMethods")]
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1061:DoNotHideBaseClassMethods")]
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1061:DoNotHideBaseClassMethods")]
        public virtual WriteConcernResult Insert(TDefaultDocument document, WriteConcern writeConcern)
        {
            return Insert<TDefaultDocument>(document, writeConcern);
        }

        /// <summary>
        /// Inserts multiple documents at once into this collection (see also Insert to insert a single document).
        /// </summary>
        /// <param name="documents">The documents to insert.</param>
        /// <returns>A list of WriteConcernResults (or null if WriteConcern is disabled).</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1061:DoNotHideBaseClassMethods")]
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1061:DoNotHideBaseClassMethods")]
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1061:DoNotHideBaseClassMethods")]
        public virtual IEnumerable<WriteConcernResult> InsertBatch(
            IEnumerable<TDefaultDocument> documents,
            WriteConcern writeConcern)
        {
            return InsertBatch<TDefaultDocument>(documents, writeConcern);
        }

        /// <summary>
        /// Scans an entire collection in parallel using multiple cursors.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Multiple enumerators, one for each cursor.</returns>
        public virtual ReadOnlyCollection<IEnumerator<TDefaultDocument>> ParallelScan(ParallelScanArgs<TDefaultDocument> args)
        {
            return ParallelScanAs<TDefaultDocument>(args);
        }

        /// <summary>
        /// Saves a document to this collection. The document must have an identifiable Id field. Based on the value
        /// of the Id field Save will perform either an Insert or an Update.
        /// </summary>
        /// <param name="document">The document to save.</param>
        /// <returns>A WriteConcernResult (or null if WriteConcern is disabled).</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1061:DoNotHideBaseClassMethods")]
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1061:DoNotHideBaseClassMethods")]
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1061:DoNotHideBaseClassMethods")]
        public virtual WriteConcernResult Save(TDefaultDocument document, WriteConcern writeConcern)
        {
            return Save<TDefaultDocument>(document, writeConcern);
        }

        /// <summary>
        /// Returns a new MongoCollection instance with a different read concern setting.
        /// </summary>
        /// <param name="readConcern">The read concern.</param>
        /// <returns>A new MongoCollection instance with a different read concern setting.</returns>
        public virtual MongoCollection<TDefaultDocument> WithReadConcern(ReadConcern readConcern)
        {
            Ensure.IsNotNull(readConcern, nameof(readConcern));
            var newSettings = Settings.Clone();
            newSettings.ReadConcern = readConcern;
            return new MongoCollection<TDefaultDocument>(Database, Name, newSettings);
        }

        /// <summary>
        /// Returns a new MongoCollection instance with a different read preference setting.
        /// </summary>
        /// <param name="readPreference">The read preference.</param>
        /// <returns>A new MongoCollection instance with a different read preference setting.</returns>
        public virtual MongoCollection<TDefaultDocument> WithReadPreference(ReadPreference readPreference)
        {
            Ensure.IsNotNull(readPreference, nameof(readPreference));
            var newSettings = Settings.Clone();
            newSettings.ReadPreference = readPreference;
            return new MongoCollection<TDefaultDocument>(Database, Name, newSettings);
        }

        /// <summary>
        /// Returns a new MongoCollection instance with a different write concern setting.
        /// </summary>
        /// <param name="writeConcern">The write concern.</param>
        /// <returns>A new MongoCollection instance with a different write concern setting.</returns>
        public virtual MongoCollection<TDefaultDocument> WithWriteConcern(WriteConcern writeConcern)
        {
            Ensure.IsNotNull(writeConcern, nameof(writeConcern));
            var newSettings = Settings.Clone();
            newSettings.WriteConcern = writeConcern;
            return new MongoCollection<TDefaultDocument>(Database, Name, newSettings);
        }
    }
}
