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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal abstract class BulkUnmixedWriteOperationBase : IWriteOperation<BulkWriteResult>
    {
        // fields
        private CollectionNamespace _collectionNamespace;
        private bool _isOrdered = true;
        private int _maxBatchCount = 0;
        private int _maxBatchLength = int.MaxValue;
        private MessageEncoderSettings _messageEncoderSettings;
        private IEnumerable<WriteRequest> _requests;
        private WriteConcern _writeConcern = WriteConcern.Acknowledged;

        // constructors
        protected BulkUnmixedWriteOperationBase(
            CollectionNamespace collectionNamespace,
            IEnumerable<WriteRequest> requests,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _requests = Ensure.IsNotNull(requests, "requests");
            _messageEncoderSettings = messageEncoderSettings;
        }

        // properties
        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
            set { _collectionNamespace = Ensure.IsNotNull(value, "value"); }
        }

        protected abstract string CommandName { get; }

        public int MaxBatchCount
        {
            get { return _maxBatchCount; }
            set { _maxBatchCount = Ensure.IsGreaterThanOrEqualToZero(value, "value"); }
        }

        public int MaxBatchLength
        {
            get { return _maxBatchLength; }
            set { _maxBatchLength = Ensure.IsGreaterThanOrEqualToZero(value, "value"); }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
            set { _messageEncoderSettings = value; }
        }

        public bool IsOrdered
        {
            get { return _isOrdered; }
            set { _isOrdered = value; }
        }

        public IEnumerable<WriteRequest> Requests
        {
            get { return _requests; }
            set { _requests = Ensure.IsNotNull(value, "value"); }
        }

        protected abstract string RequestsElementName { get; }

        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = Ensure.IsNotNull(value, "value"); }
        }

        // methods
        protected abstract BatchSerializer CreateBatchSerializer(int maxBatchCount, int maxBatchLength, int maxDocumentSize, int maxWireDocumentSize);

        protected abstract BulkUnmixedWriteOperationEmulatorBase CreateEmulator();

        private BsonDocument CreateWriteCommand(BatchSerializer batchSerializer, BatchableSource<WriteRequest> requestSource)
        {
            var batchWrapper = new BsonDocumentWrapper(requestSource, batchSerializer);

            var writeConcern = _writeConcern.ToBsonDocument();
            if (writeConcern.ElementCount == 0)
            {
                writeConcern = null; // omit field if writeConcern is { }
            }

            return new BsonDocument
            {
                { CommandName, _collectionNamespace.CollectionName },   
                { "writeConcern", writeConcern, writeConcern != null },
                { "ordered", _isOrdered },
                { RequestsElementName, new BsonArray { batchWrapper } }
            };
        }

        private CommandWireProtocol CreateWriteCommandProtocol(BsonDocument command)
        {
            return new CommandWireProtocol(_collectionNamespace.DatabaseNamespace, command, false, _messageEncoderSettings);
        }

        protected virtual IEnumerable<WriteRequest> DecorateRequests(IEnumerable<WriteRequest> requests)
        {
            return requests;
        }

        public async Task<BulkWriteResult> ExecuteAsync(IConnectionHandle connection, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (connection.Description.ServerVersion >= new SemanticVersion(2, 6, 0))
            {
                return await ExecuteBatchesAsync(connection, timeout, cancellationToken);
            }
            else
            {
                var emulator = CreateEmulator();
                return await emulator.ExecuteAsync(connection, timeout, cancellationToken);
            }
        }

        public async Task<BulkWriteResult> ExecuteAsync(IWriteBinding binding, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var slidingTimeout = new SlidingTimeout(timeout);
            using (var connectionSource = await binding.GetWriteConnectionSourceAsync(slidingTimeout, cancellationToken))
            using (var connection = await connectionSource.GetConnectionAsync(slidingTimeout, cancellationToken))
            {
                return await ExecuteAsync(connection, slidingTimeout, cancellationToken);
            }
        }

        private async Task<BulkWriteBatchResult> ExecuteBatchAsync(IConnectionHandle connection, BatchableSource<WriteRequest> requestSource, int originalIndex, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var maxBatchCount = Math.Min(_maxBatchCount, connection.Description.MaxBatchCount);
            var maxBatchLength = Math.Min(_maxBatchLength, connection.Description.MaxDocumentSize);
            var maxDocumentSize = connection.Description.MaxDocumentSize;
            var maxWireDocumentSize = connection.Description.MaxWireDocumentSize;

            var batchSerializer = CreateBatchSerializer(maxBatchCount, maxBatchLength, maxDocumentSize, maxWireDocumentSize);
            var writeCommand = CreateWriteCommand(batchSerializer, requestSource);
            var protocol = CreateWriteCommandProtocol(writeCommand);
            var writeCommandResult = await protocol.ExecuteAsync(connection, timeout, cancellationToken);

            var indexMap = new IndexMap.RangeBased(0, originalIndex, requestSource.Batch.Count);
            return BulkWriteBatchResult.Create(
                _isOrdered,
                requestSource.Batch,
                writeCommandResult,
                indexMap);
        }

        private async Task<BulkWriteResult> ExecuteBatchesAsync(IConnectionHandle connection, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var slidingTimeout = new SlidingTimeout(timeout);
            var batchResults = new List<BulkWriteBatchResult>();
            var remainingRequests = Enumerable.Empty<WriteRequest>();
            var hasWriteErrors = false;

            var decoratedRequests = DecorateRequests(_requests);
            using (var enumerator = decoratedRequests.GetEnumerator())
            {
                var originalIndex = 0;

                var requestSource = new BatchableSource<WriteRequest>(enumerator);
                while (requestSource.HasMore)
                {
                    if (hasWriteErrors && _isOrdered)
                    {
                        remainingRequests = remainingRequests.Concat(requestSource.GetRemainingItems());
                        break;
                    }

                    var batchResult = await ExecuteBatchAsync(connection, requestSource, originalIndex, slidingTimeout, cancellationToken);
                    batchResults.Add(batchResult);
                    hasWriteErrors |= batchResult.HasWriteErrors;
                    originalIndex += batchResult.BatchCount;

                    requestSource.ClearBatch();
                }
            }

            var combiner = new BulkWriteBatchResultCombiner(batchResults, _writeConcern.IsAcknowledged);
            return combiner.CreateResultOrThrowIfHasErrors(remainingRequests.ToList());
        }

        // nested types
        protected abstract class BatchSerializer : SerializerBase<BatchableSource<WriteRequest>>
        {
            // fields
            private int _batchCount;
            private int _batchLength;
            private int _batchStartPosition;
            private int _lastRequestPosition;
            private readonly int _maxBatchCount;
            private readonly int _maxBatchLength;
            private readonly int _maxDocumentSize;
            private readonly int _maxWireDocumentSize;

            // constructors
            public BatchSerializer(int maxBatchCount, int maxBatchLength, int maxDocumentSize, int maxWireDocumentSize)
            {
                _maxBatchCount = maxBatchCount;
                _maxBatchLength = maxBatchLength;
                _maxDocumentSize = maxDocumentSize;
                _maxWireDocumentSize = maxWireDocumentSize;
            }

            // properties
            protected int MaxBatchCount
            {
                get { return _maxBatchCount; }
            }

            protected int MaxBatchLength
            {
                get { return _maxBatchLength; }
            }

            protected int MaxDocumentSize
            {
                get { return _maxDocumentSize; }
            }

            protected int MaxWireDocumentSize
            {
                get { return _maxWireDocumentSize; }
            }

            // methods
            private void AddRequest(BsonSerializationContext context, IByteBuffer overflow)
            {
                var bsonBinaryWriter = (BsonBinaryWriter)context.Writer;
                var stream = bsonBinaryWriter.Stream;
                _lastRequestPosition = (int)stream.Position;
                bsonBinaryWriter.WriteRawBsonDocument(overflow);
                _batchCount++;
                _batchLength = (int)stream.Position - _batchStartPosition;
            }

            private void AddRequest(BsonSerializationContext context, WriteRequest request)
            {
                var bsonBinaryWriter = (BsonBinaryWriter)context.Writer;
                var stream = bsonBinaryWriter.Stream;
                _lastRequestPosition = (int)stream.Position;
                SerializeRequest(context, request);
                _batchCount++;
                _batchLength = (int)stream.Position - _batchStartPosition;
            }

            private IByteBuffer RemoveLastRequest(BsonSerializationContext context)
            {
                var bsonBinaryWriter = (BsonBinaryWriter)context.Writer;
                var stream = bsonBinaryWriter.Stream;
                var lastRequestLength = (int)stream.Position - _lastRequestPosition;
                stream.Position = _lastRequestPosition;
                var lastRequest = new byte[lastRequestLength];
                stream.FillBuffer(lastRequest, 0, lastRequestLength);
                stream.Position = _lastRequestPosition;
                stream.SetLength(_lastRequestPosition);
                _batchCount--;
                _batchLength = (int)stream.Position - _batchStartPosition;

                if ((BsonType)lastRequest[0] != BsonType.Document)
                {
                    throw new MongoInternalException("Expected overflow item to be a BsonDocument.");
                }
                var sliceOffset = Array.IndexOf<byte>(lastRequest, 0) + 1; // skip over type and array index
                return new ByteArrayBuffer(lastRequest, sliceOffset, lastRequest.Length - sliceOffset, true);
            }

            public override void Serialize(BsonSerializationContext context, BatchableSource<WriteRequest> requestSource)
            {
                if (requestSource.IsBatchable)
                {
                    SerializeNextBatch(context, requestSource);
                }
                else
                {
                    SerializeSingleBatch(context, requestSource);
                }
            }

            private void SerializeNextBatch(BsonSerializationContext context, BatchableSource<WriteRequest> requestSource)
            {
                var batch = new List<WriteRequest>();

                var bsonBinaryWriter = (BsonBinaryWriter)context.Writer;
                _batchStartPosition = (int)bsonBinaryWriter.Stream.Position;

                var overflow = requestSource.StartBatch();
                if (overflow != null)
                {
                    AddRequest(context, (IByteBuffer)overflow.State);
                    batch.Add(overflow.Item);
                }

                // always go one document too far so that we can set IsDone as early as possible
                while (requestSource.MoveNext())
                {
                    var request = requestSource.Current;
                    AddRequest(context, request);

                    if ((_batchCount > _maxBatchCount || _batchLength > _maxBatchLength) && _batchCount > 1)
                    {
                        var serializedRequest = RemoveLastRequest(context);
                        overflow = new BatchableSource<WriteRequest>.Overflow { Item = request, State = serializedRequest };
                        requestSource.EndBatch(batch, overflow);
                        return;
                    }

                    batch.Add(request);
                }

                requestSource.EndBatch(batch);
            }

            private void SerializeSingleBatch(BsonSerializationContext context, BatchableSource<WriteRequest> requestSource)
            {
                var bsonBinaryWriter = (BsonBinaryWriter)context.Writer;
                _batchStartPosition = (int)bsonBinaryWriter.Stream.Position;

                // always go one document too far so that we can set IsDone as early as possible
                foreach (var request in requestSource.Batch)
                {
                    AddRequest(context, request);

                    if ((_batchCount > _maxBatchCount || _batchLength > _maxBatchLength) && _batchCount > 1)
                    {
                        throw new ArgumentException("The non-batchable requests do not fit in a single write command.");
                    }
                }
            }

            protected abstract void SerializeRequest(BsonSerializationContext context, WriteRequest request);
        }
    }
}
