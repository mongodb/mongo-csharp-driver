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
    internal class ListIndexesOperation
    {
        #region static
        // static fields
        private static readonly Version __serverVersionSupportingListIndexesCommand = new Version(2, 7, 6);
        #endregion

        // fields
        private readonly string _collectionName;
        private readonly string _databaseName;
        private readonly BsonBinaryReaderSettings _readerSettings;
        private readonly ReadPreference _readPreference;
        private readonly BsonBinaryWriterSettings _writerSettings;

        // constructors
        public ListIndexesOperation(
            string databaseName,
            string collectionName,
            ReadPreference readPreference,
            BsonBinaryReaderSettings readerSettings,
            BsonBinaryWriterSettings writerSettings)
       {
            if (string.IsNullOrEmpty(databaseName))
            {
                throw new ArgumentException("Database name cannot be null or empty.", "databaseName");
            }
            if (string.IsNullOrEmpty(collectionName))
            {
                throw new ArgumentException("Collection name cannot be null or empty.", "collectionName");
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
            _collectionName = collectionName;
            _readPreference = readPreference;
            _readerSettings = readerSettings;
            _writerSettings = writerSettings;
        }

        // methods
        public IEnumerable<BsonDocument> Execute(MongoConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (connection.ServerInstance.BuildInfo.Version >= __serverVersionSupportingListIndexesCommand)
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
            var command = new CommandDocument("listIndexes", _collectionName);
            var flags = QueryFlags.None;
            var options = new BsonDocument();
            IBsonSerializationOptions serializationOptions = null;
            var serializer = BsonSerializer.LookupSerializer(typeof(CommandResult));
            var operation = new CommandOperation<CommandResult>(_databaseName, _readerSettings, _writerSettings, command, flags, options, _readPreference, serializationOptions, serializer);

            CommandResult result;
            try
            {
                result = operation.Execute(connection);
            }
            catch (MongoCommandException ex)
            {
                var response = ex.Result;
                BsonValue code;
                if (response.TryGetValue("code", out code) && code.IsNumeric && code.ToInt32() == 26)
                {
                    return Enumerable.Empty<BsonDocument>();
                }
                throw;
            }

            return result.Response["indexes"].AsBsonArray.Cast<BsonDocument>();
        }

        private IEnumerable<BsonDocument> ExecuteUsingQuery(MongoConnection connection)
        {
            var indexes = new List<BsonDocument>();

            var batchSize = 0;
            IMongoFields fields = null;
            var flags = QueryFlags.None;
            var limit = 0;
            var options = new BsonDocument();
            var query = Query.EQ("ns", _databaseName + "." + _collectionName);
            IBsonSerializationOptions serializationOptions = null;
            var serializer = BsonDocumentSerializer.Instance;
            var skip = 0;
            var operation = new QueryOperation<BsonDocument>(_databaseName, "system.indexes", _readerSettings, _writerSettings, batchSize, fields, flags, limit, options, query, _readPreference, serializationOptions, serializer, skip);

            var connectionProvider = new ServerInstanceConnectionProvider(connection.ServerInstance);
            using (var enumerator = operation.Execute(connectionProvider))
            {
                while (enumerator.MoveNext())
                {
                    indexes.Add(enumerator.Current);
                }
            }

            return indexes;
        }
    }
}
