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
        private readonly int? _batchSize;
        private readonly string _collectionName;
        private readonly string _databaseName;
        private readonly int _numberOfCursors = 4;
        private readonly IBsonSerializer<TDocument> _serializer;

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

        private ParallelScanOperation(
            int? batchSize,
            string collectionName,
            string databaseName,
            int numberOfCursors,
            IBsonSerializer<TDocument> serializer)
        {
            _batchSize = batchSize;
            _collectionName = collectionName;
            _databaseName = databaseName;
            _numberOfCursors = numberOfCursors;
            _serializer = serializer;
        }

        // properties
        public int? BatchSize
        {
            get { return _batchSize; }
        }

        public string CollectionName
        {
            get { return _collectionName; }
        }

        public string DatabaseName
        {
            get { return _databaseName; }
        }

        public int NumberOfCursors
        {
            get { return _numberOfCursors; }
        }

        public IBsonSerializer<TDocument> Serializer
        {
            get { return _serializer; }
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
                    var firstBatch = cursorDocument["firstBatch"].AsBsonArray.Cast<TDocument>().ToList(); // TODO: deserialize TDocuments

                    var cursor = new Cursor<TDocument>(
                        connectionSource.Fork(),
                        _databaseName,
                        _collectionName,
                        command,
                        firstBatch,
                        cursorId,
                        _batchSize ?? 0,
                        _serializer,
                        timeout,
                        cancellationToken);
                    cursors.Add(cursor);
                }

                return cursors;
            }
        }

        public ParallelScanOperation<TDocument> WithBatchSize(int? value)
        {
            Ensure.IsNullOrGreaterThanZero(value, "value");
            return _batchSize == value ? this : new Builder(this) { _batchSize = value }.Build();
        }

        public ParallelScanOperation<TDocument> WithCollectionName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return _collectionName == value ? this : new Builder(this) { _collectionName = value }.Build();
        }

        public ParallelScanOperation<TDocument> WithDatabaseName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return _databaseName == value ? this : new Builder(this) { _databaseName = value }.Build();
        }

        public ParallelScanOperation<TDocument> WithNumberOfCursors(int value)
        {
            Ensure.IsBetween(value, 1, 1000, "value");
            return _numberOfCursors == value ? this : new Builder(this) { _numberOfCursors = value }.Build();
        }

        public ParallelScanOperation<TDocument> WithSerializer(IBsonSerializer<TDocument> value)
        {
            Ensure.IsNotNull(value, "value");
            return object.ReferenceEquals(_serializer, value) ? this : new Builder(this) { _serializer = value }.Build();
        }

        public ParallelScanOperation<TOther> WithSerializer<TOther>(IBsonSerializer<TOther> value)
        {
            Ensure.IsNotNull(value, "value");
            return new ParallelScanOperation<TOther>(
                _databaseName,
                _collectionName,
                _numberOfCursors,
                value);
        }

        // nested types
        private struct Builder
        {
            // fields
            public int? _batchSize;
            public string _collectionName;
            public string _databaseName;
            public int _numberOfCursors;
            public IBsonSerializer<TDocument> _serializer;

            // constructors
            public Builder(ParallelScanOperation<TDocument> other)
            {
                _batchSize = other._batchSize;
                _collectionName = other._collectionName;
                _databaseName = other._databaseName;
                _numberOfCursors = other._numberOfCursors;
                _serializer = other._serializer;
            }

            // methods
            public ParallelScanOperation<TDocument> Build()
            {
                return new ParallelScanOperation<TDocument>(
                    _batchSize,
                    _collectionName,
                    _databaseName,
                    _numberOfCursors,
                    _serializer);
            }
        }
    }
}
