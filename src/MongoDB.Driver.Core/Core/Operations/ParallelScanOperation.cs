/* Copyright 2013-2014 MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Operations
{
    public class ParallelScanOperation : ParallelScanOperation<BsonDocument>
    {
        // constructors
        public ParallelScanOperation(
            string databaseName,
            string collectionName,
            int numberOfCursors)
            : base(databaseName, collectionName, numberOfCursors, BsonDocumentSerializer.Instance)
        {
        }
    }

    public class ParallelScanOperation<TDocument> : IReadOperation<IReadOnlyList<Cursor<TDocument>>>
    {
        // fields
        private int? _batchSize;
        private string _collectionName;
        private string _databaseName;
        private int _numberOfCursors = 4;
        private IBsonSerializer<TDocument> _serializer;

        // constructors
        public ParallelScanOperation(
            string databaseName,
            string collectionName,
            int numberOfCursors,
            IBsonSerializer<TDocument> serializer)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _numberOfCursors = Ensure.IsBetween(numberOfCursors, 0, 10000, "numberOfCursors");
            _serializer = Ensure.IsNotNull(serializer, "serializer");
        }

        // properties
        public int? BatchSize
        {
            get { return _batchSize; }
            set { _batchSize = Ensure.IsNullOrGreaterThanZero(value, "value"); }
        }

        public string CollectionName
        {
            get { return _collectionName; }
            set { _collectionName = Ensure.IsNotNullOrEmpty(value, "value"); }
        }

        public string DatabaseName
        {
            get { return _databaseName; }
            set { _databaseName = Ensure.IsNotNullOrEmpty(value, "value"); }
        }

        public int NumberOfCursors
        {
            get { return _numberOfCursors; }
            set { _numberOfCursors = Ensure.IsBetween(value, 1, 1000, "value"); }
        }

        public IBsonSerializer<TDocument> Serializer
        {
            get { return _serializer; }
            set { _serializer = Ensure.IsNotNull(value, "value"); }
        }

        // methods
        public BsonDocument CreateCommand()
        {
            return new BsonDocument
            {
                { "parallelCollectionScan", _collectionName },
                { "numCursors", _numberOfCursors }
            };
        }

        public async Task<IReadOnlyList<Cursor<TDocument>>> ExecuteAsync(IReadBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var slidingTimeout = new SlidingTimeout(timeout);

            using (var connectionSource = await binding.GetReadConnectionSourceAsync(slidingTimeout, cancellationToken))
            {
                var command = CreateCommand();
                var operation = new ReadCommandOperation(_databaseName, command);
                var result = await operation.ExecuteAsync(connectionSource, binding.ReadPreference, slidingTimeout, cancellationToken);

                var cursors = new List<Cursor<TDocument>>();

                foreach (var cursorDocument in result["cursors"].AsBsonArray.Select(v => v["cursor"].AsBsonDocument))
                {
                    var cursorId = cursorDocument["id"].ToInt64();
                    var firstBatch = cursorDocument["firstBatch"].AsBsonArray.Select(v =>
                        {
                            var bsonDocument = (BsonDocument)v;
                            using (var reader = new BsonDocumentReader(bsonDocument))
                            {
                                var context = BsonDeserializationContext.CreateRoot<TDocument>(reader);
                                var document = _serializer.Deserialize(context);
                                return document;
                            }
                        })
                        .ToList();

                    var cursor = new Cursor<TDocument>(
                        connectionSource.Fork(),
                        _databaseName,
                        _collectionName,
                        command,
                        firstBatch,
                        cursorId,
                        _batchSize ?? 0,
                        0, // limit
                        _serializer,
                        timeout,
                        cancellationToken);
                    cursors.Add(cursor);
                }

                return cursors;
            }
        }
    }
}
