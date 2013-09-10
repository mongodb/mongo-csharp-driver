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
    internal class AggregateEnumerableResult : IEnumerable<BsonDocument>
    {
        // private fields
        private readonly MongoCollection _collection;
        private readonly AggregateArgs _args;
        private AggregateResult _immediateExecutionResult;

        // constructors
        public AggregateEnumerableResult(
            MongoCollection collection,
            AggregateArgs args,
            AggregateResult immediateExecutionResult)
        {
            _collection = collection;
            _args = args; // TODO: make a defensive copy?
            _immediateExecutionResult = immediateExecutionResult;
        }

        // public methods
        public IEnumerator<BsonDocument> GetEnumerator()
        {
            AggregateResult result;
            if (_immediateExecutionResult != null)
            {
                result = _immediateExecutionResult;
                _immediateExecutionResult = null;
            }
            else
            {
                result = _collection.RunAggregateCommand(_args);
            }

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
                    result.ResultDocuments,
                    result.CursorId,
                    _args.BatchSize ?? 0,
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
            else if (result.ResultDocuments != null)
            {
                return result.ResultDocuments.GetEnumerator();
            }
            else
            {
                throw new NotSupportedException("Unexpected response to aggregate command.");
            }
        }

        // explicit interface implementations
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
