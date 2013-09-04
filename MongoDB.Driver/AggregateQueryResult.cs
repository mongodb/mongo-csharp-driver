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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Operations;

namespace MongoDB.Driver
{
    internal class AggregateQueryResult : IEnumerable<BsonDocument>
    {
        private MongoCollection _collection;
        private IEnumerable<BsonDocument> _operations;
        private MongoAggregateOptions _options;

        public AggregateQueryResult(
            MongoCollection collection,
            IEnumerable<BsonDocument> operations,
            MongoAggregateOptions options)
        {
            _collection = collection;
            _operations = operations; // TODO: make a defensive copy?
            _options = options; // TODO: make a defensive copy?
        }

        public IEnumerator<BsonDocument> GetEnumerator()
        {
            var result = _collection.Aggregate(_operations, _options);
            if (result.CursorId != 0)
            {
                var connectionProvider = new ServerInstanceConnectionProvider(result.ServerInstance);
                var readerSettings = new BsonBinaryReaderSettings
                {
                    Encoding = _collection.Settings.ReadEncoding ?? MongoDefaults.ReadEncoding,
                    GuidRepresentation = _collection.Settings.GuidRepresentation
                };
                return new CursorEnumerator<BsonDocument>(
                    connectionProvider,
                    _collection.FullName,
                    result.FirstBatch,
                    result.CursorId,
                    _options.BatchSize,
                    0,
                    readerSettings,
                    BsonDocumentSerializer.Instance,
                    null);
            }
            else if (result.OutputNamespace != null)
            {
                var ns = result.OutputNamespace;
                var firstDot = ns.IndexOf('.');
                var databaseName = ns.Substring(0, firstDot);
                var collectionName = ns.Substring(firstDot + 1);
                var database = _collection.Database.Server.GetDatabase(databaseName);
                var collectionSettings = new MongoCollectionSettings { ReadPreference = ReadPreference.Primary };
                var collection = database.GetCollection<BsonDocument>(collectionName, collectionSettings);
                return collection.FindAll().GetEnumerator();
            }
            else if (result.FirstBatch != null)
            {
                return result.FirstBatch.GetEnumerator();
            }
            else if (result.ResultDocuments != null)
            {
                return result.ResultDocuments.GetEnumerator();
            }
            else
            {
                throw new NotSupportedException("Unexpected response to aggregate command.");
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
