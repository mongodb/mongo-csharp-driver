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
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    public class ParallelScanOperation : ParallelScanOperation<BsonDocument>
    {
        // constructors
        public ParallelScanOperation(
            CollectionNamespace collectionNamespace,
            int numberOfCursors,
            MessageEncoderSettings messageEncoderSettings)
            : base(collectionNamespace, numberOfCursors, BsonDocumentSerializer.Instance, messageEncoderSettings)
        {
        }
    }

    public class ParallelScanOperation<TDocument> : IReadOperation<IReadOnlyList<Cursor<TDocument>>>
    {
        // fields
        private int? _batchSize;
        private CollectionNamespace _collectionNamespace;
        private MessageEncoderSettings _messageEncoderSettings;
        private int _numberOfCursors = 4;
        private IBsonSerializer<TDocument> _serializer;

        // constructors
        public ParallelScanOperation(
            CollectionNamespace collectionNamespace,
            int numberOfCursors,
            IBsonSerializer<TDocument> serializer,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _numberOfCursors = Ensure.IsBetween(numberOfCursors, 0, 10000, "numberOfCursors");
            _serializer = Ensure.IsNotNull(serializer, "serializer");
            _messageEncoderSettings = messageEncoderSettings;
        }

        // properties
        public int? BatchSize
        {
            get { return _batchSize; }
            set { _batchSize = Ensure.IsNullOrGreaterThanZero(value, "value"); }
        }

        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
            set { _collectionNamespace = Ensure.IsNotNull(value, "value"); }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
            set { _messageEncoderSettings = value; }
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
                { "parallelCollectionScan", _collectionNamespace.CollectionName },
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
                var operation = new ReadCommandOperation(_collectionNamespace.DatabaseNamespace, command, _messageEncoderSettings);
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
                        _collectionNamespace,
                        command,
                        firstBatch,
                        cursorId,
                        _batchSize ?? 0,
                        0, // limit
                        _serializer,
                        _messageEncoderSettings,
                        timeout,
                        cancellationToken);
                    cursors.Add(cursor);
                }

                return cursors;
            }
        }
    }
}
