/* Copyright 2010-2014 MongoDB Inc.
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
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver.Operations
{
    internal class ListCollectionsOperation
    {
        #region static
        // static fields
        private static readonly Version __versionSupportingListCollectionsCommand = new Version(2, 7, 6);
        #endregion

        // fields
        private IMongoQuery _query;
        private readonly string _databaseName;
        private readonly BsonBinaryReaderSettings _readerSettings;
        private readonly ReadPreference _readPreference;
        private readonly BsonBinaryWriterSettings _writerSettings;

        // constructors
        public ListCollectionsOperation(
            string databaseName,
            ReadPreference readPreference,
            BsonBinaryReaderSettings readerSettings,
            BsonBinaryWriterSettings writerSettings)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                throw new ArgumentException("Database name cannot be null or empty.", "databaseName");
            }
            if (readPreference == null)
            {
                throw new ArgumentNullException("readPreference");
            }
            if (readerSettings == null)
            {
                throw new ArgumentNullException("readerSettings");
            }
            if (writerSettings == null)
            {
                throw new ArgumentNullException("writerSettings");
            }

            _databaseName = databaseName;
            _readPreference = readPreference;
            _readerSettings = readerSettings;
            _writerSettings = writerSettings;
        }

        // properties
        public IMongoQuery Query
        {
            get { return _query; }
            set { _query = value; }
        }

        // methods
        public IEnumerable<BsonDocument> Execute(MongoConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (connection.ServerInstance.BuildInfo.Version >= __versionSupportingListCollectionsCommand)
            {
                return ExecuteUsingCommand(connection);
            }
            else
            {
                return ExecuteUsingQuery(connection);
            }
        }

        private IEnumerable<BsonDocument> ExecuteUsingCommand(MongoConnection connection)
        {
            var command = new CommandDocument
            {
                { "listCollections", 1 },
                { "filter", () => _query.ToBsonDocument(), _query != null }
            };
            var flags = QueryFlags.None;
            var options = new BsonDocument();
            IBsonSerializationOptions serializationOptions = null;
            var serializer = BsonSerializer.LookupSerializer(typeof(CommandResult));
            var operation = new CommandOperation<CommandResult>(_databaseName, _readerSettings, _writerSettings, command, flags, options, _readPreference, serializationOptions, serializer);
            var result = operation.Execute(connection);
            var response = result.Response;
            return response["collections"].AsBsonArray.Select(value => (BsonDocument)value).ToList();
        }

        private IEnumerable<BsonDocument> ExecuteUsingQuery(MongoConnection connection)
        {
            // if the criteria includes a comparison to the "name" we must convert the value to a full namespace
            var query = _query;
            var queryDocument = query == null ? null : query.ToBsonDocument();
            if (queryDocument != null && queryDocument.Contains("name"))
            {
                var value = queryDocument["name"];
                if (!value.IsString)
                {
                    throw new NotSupportedException("Name criteria must be a plain string when connected to a server version less than 2.8.");
                }
                var clonedQueryDocument = new QueryDocument(queryDocument);
                clonedQueryDocument["name"] = _databaseName + "." + value;
                query = clonedQueryDocument;
            }

            var batchSize = 0;
            var fields = Fields.Null;
            var flags = QueryFlags.NoCursorTimeout;
            var limit = 0;
            BsonDocument options = null;
            IBsonSerializationOptions serializationOptions = null;
            var serializer = BsonDocumentSerializer.Instance;
            var skip = 0;
            var operation = new QueryOperation<BsonDocument>(_databaseName, "system.namespaces", _readerSettings, _writerSettings, batchSize, fields, flags, limit, options, query, _readPreference, serializationOptions, serializer, skip);

            var connectionProvider = new ServerInstanceConnectionProvider(connection.ServerInstance);
            using (var enumerator = operation.Execute(connectionProvider))
            {
                var collections = new List<BsonDocument>();
                var prefix = _databaseName + ".";

                while (enumerator.MoveNext())
                {
                    var collection = enumerator.Current;
                    var name = (string)collection["name"];
                    if (name.StartsWith(prefix))
                    {
                        var collectionName = name.Substring(prefix.Length);
                        if (!collectionName.Contains('$'))
                        {
                            collection["name"] = collectionName; // replace the full namespace with just the collection name
                            collections.Add(collection);
                        }
                    }
                }

                return collections;
            }
        }
    }
}
