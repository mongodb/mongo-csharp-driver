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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Sync;
using MongoDB.Driver.Core.SyncExtensionMethods;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver
{
    internal class AggregateEnumerableResult : IEnumerable<BsonDocument>
    {
        // private fields
        private readonly MongoCollection _collection;
        private readonly AggregateArgs _args;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly string _outputCollectionName;

        // constructors
        public AggregateEnumerableResult(
            MongoCollection collection,
            AggregateArgs args,
            string outputCollectionName,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collection = collection;
            _args = args; // TODO: make a defensive copy?
            _outputCollectionName = outputCollectionName;
            _messageEncoderSettings = messageEncoderSettings;
        }

        // public methods
        public IEnumerator<BsonDocument> GetEnumerator()
        {
            if (_outputCollectionName != null)
            {
                var database = _collection.Database;
                var collectionSettings = new MongoCollectionSettings { ReadPreference = ReadPreference.Primary };
                var collection = database.GetCollection<BsonDocument>(_outputCollectionName, collectionSettings);
                return collection.FindAll().GetEnumerator();
            }

            var result = _collection.RunAggregateCommand(_args);
            if (result.CursorId != 0)
            {
                var batchSize = _args.BatchSize ?? 0;
                var cursorId = result.CursorId;
                var firstBatch = result.ResultDocuments.ToList();
                var query = new BsonDocument(); // TODO: what should the query be?
                var readPreference = _collection.Settings.ReadPreference ?? ReadPreference.Primary;
                var serializer = BsonDocumentSerializer.Instance;

                using (var binding = _collection.Database.Server.GetReadBinding(readPreference))
                using (var connectionSource = binding.GetReadConnectionSource(Timeout.InfiniteTimeSpan, CancellationToken.None))
                {
                    var cursor = new BatchCursor<BsonDocument>(
                        connectionSource.Fork(),
                        new CollectionNamespace(_collection.Database.Name, _collection.Name),
                        query,
                        firstBatch,
                        cursorId,
                        batchSize,
                        0, // limit
                        serializer,
                        _messageEncoderSettings,
                        Timeout.InfiniteTimeSpan,
                        CancellationToken.None);

                    return new AsyncCursorEnumeratorAdapter<BsonDocument>(cursor).GetEnumerator();
                }
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
