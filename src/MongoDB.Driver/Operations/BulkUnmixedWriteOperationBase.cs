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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Internal;
using MongoDB.Driver.Support;

namespace MongoDB.Driver.Operations
{
    internal abstract class BulkUnmixedWriteOperationBase
    {
        // private fields
        private readonly BulkWriteOperationArgs _args;

        // constructors
        protected BulkUnmixedWriteOperationBase(BulkWriteOperationArgs args)
        {
            _args = args;
        }

        // protected properties
        protected abstract string CommandName { get; }

        protected abstract string RequestsElementName { get; }

        // public methods
        public virtual BulkWriteResult Execute(MongoConnection connection)
        {
            var batchResults = new List<BulkWriteBatchResult>();
            var remainingRequests = Enumerable.Empty<WriteRequest>();
            var hasWriteErrors = false;

            var decoratedRequests = DecorateRequests(_args.Requests);
            using (var enumerator = decoratedRequests.GetEnumerator())
            {
                var originalIndex = 0;
                Batch<WriteRequest> batch = new FirstBatch<WriteRequest>(enumerator);
                while (batch != null)
                {
                    if (hasWriteErrors && _args.IsOrdered)
                    {
                        remainingRequests = remainingRequests.Concat(batch.RemainingItems);
                        break;
                    }

                    var batchResult = ExecuteBatch(connection, batch, originalIndex);
                    batchResults.Add(batchResult);

                    hasWriteErrors |= batchResult.HasWriteErrors;
                    originalIndex += batchResult.BatchCount;
                    batch = batchResult.NextBatch;
                }
            }

            var combiner = new BulkWriteBatchResultCombiner(batchResults, _args.WriteConcern.Enabled);
            return combiner.CreateResultOrThrowIfHasErrors(remainingRequests);
        }

        // protected methods
        protected abstract BatchSerializer CreateBatchSerializer(int maxBatchCount, int maxBatchLength, int maxDocumentSize, int maxWireDocumentSize);

        protected virtual IEnumerable<WriteRequest> DecorateRequests(IEnumerable<WriteRequest> requests)
        {
            return requests;
        }

        // private methods
        private CommandDocument CreateWriteCommand(BatchSerializer batchSerializer, Batch<WriteRequest> batch)
        {
            var batchWrapper = new BsonDocumentWrapper(batch, batchSerializer, false);

            var writeConcern = _args.WriteConcern.ToBsonDocument();
            if (writeConcern.ElementCount == 0)
            {
                writeConcern = null; // omit field if writeConcern is { }
            }

            return new CommandDocument
            {
                { CommandName, _args.CollectionName },
                { "writeConcern", writeConcern, writeConcern != null },
                { "ordered", _args.IsOrdered },
                { RequestsElementName, new BsonArray { batchWrapper } }
            };
        }

        private CommandOperation<CommandResult> CreateWriteCommandOperation(IMongoCommand command)
        {
            return new CommandOperation<CommandResult>(
                _args.DatabaseName,
                _args.ReaderSettings,
                _args.WriterSettings,
                command,
                QueryFlags.None,
                null, // options
                ReadPreference.Primary,
                BsonSerializer.LookupSerializer<CommandResult>()); // resultSerializer
        }

        private BulkWriteBatchResult ExecuteBatch(MongoConnection connection, Batch<WriteRequest> batch, int originalIndex)
        {
            var maxBatchCount = Math.Min(_args.MaxBatchCount, connection.ServerInstance.MaxBatchCount);
            var maxBatchLength = Math.Min(_args.MaxBatchLength, connection.ServerInstance.MaxDocumentSize);
            var maxDocumentSize = connection.ServerInstance.MaxDocumentSize;
            var maxWireDocumentSize = connection.ServerInstance.MaxWireDocumentSize;

            var batchSerializer = CreateBatchSerializer(maxBatchCount, maxBatchLength, maxDocumentSize, maxWireDocumentSize);
            var writeCommand = CreateWriteCommand(batchSerializer, batch);
            var writeCommandOperation = CreateWriteCommandOperation(writeCommand);
            var writeCommandResult = writeCommandOperation.Execute(connection);
            var batchProgress = batchSerializer.BatchProgress;

            var indexMap = new IndexMap.RangeBased(0, originalIndex, batchProgress.BatchCount);
            return BulkWriteBatchResult.Create(
                _args.IsOrdered,
                batchProgress.BatchItems,
                writeCommandResult.Response,
                indexMap,
                batchProgress.NextBatch);
        }

        // nested classes
        protected abstract class BatchSerializer : SerializerBase<Batch<WriteRequest>>
        {
            // private fields
            private int _batchCount;
            private int _batchLength;
            private BatchProgress<WriteRequest> _batchProgress;
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

            // public properties
            public BatchProgress<WriteRequest> BatchProgress
            {
                get { return _batchProgress; }
            }

            // protected properties
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

            // public methods
            public override void Serialize(BsonSerializationContext context, Batch<WriteRequest> batch)
            {
                var bsonWriter = (BsonBinaryWriter)context.Writer;

                _batchStartPosition = (int)bsonWriter.Stream.Position;
                var processedRequests = new List<WriteRequest>();

                var continuationBatch = batch as ContinuationBatch<WriteRequest, IByteBuffer>;
                if (continuationBatch != null)
                {
                    AddOverfow(bsonWriter, continuationBatch.PendingState);
                    processedRequests.Add(continuationBatch.PendingItem);
                    continuationBatch.ClearPending(); // so pending objects can be garbage collected sooner
                }

                // always go one document too far so that we can set IsDone as early as possible
                var enumerator = batch.Enumerator;
                while (enumerator.MoveNext())
                {
                    var request = enumerator.Current;
                    AddRequest(bsonWriter, request);

                    if ((_batchCount > _maxBatchCount || _batchLength > _maxBatchLength) && _batchCount > 1)
                    {
                        var serializedRequest = RemoveOverflow(bsonWriter.Stream);
                        var nextBatch = new ContinuationBatch<WriteRequest, IByteBuffer>(enumerator, request, serializedRequest);
                        _batchProgress = new BatchProgress<WriteRequest>(_batchCount, _batchLength, processedRequests, nextBatch);
                        return;
                    }

                    processedRequests.Add(request);
                }

                _batchProgress = new BatchProgress<WriteRequest>(_batchCount, _batchLength, processedRequests, null);
            }

            // protected methods
            protected abstract void SerializeRequest(BsonBinaryWriter bsonBinaryWriter, WriteRequest request);

            // private methods
            private void AddOverfow(BsonBinaryWriter bsonBinaryWriter, IByteBuffer overflow)
            {
                _lastRequestPosition = (int)bsonBinaryWriter.Stream.Position;
                bsonBinaryWriter.WriteRawBsonDocument(overflow);
                _batchCount++;
                _batchLength = (int)bsonBinaryWriter.Stream.Position - _batchStartPosition;
            }

            private void AddRequest(BsonBinaryWriter bsonBinaryWriter, WriteRequest request)
            {
                _lastRequestPosition = (int)bsonBinaryWriter.Stream.Position;
                SerializeRequest(bsonBinaryWriter, request);
                _batchCount++;
                _batchLength = (int)bsonBinaryWriter.Stream.Position - _batchStartPosition;
            }

            private IByteBuffer RemoveOverflow(Stream stream)
            {
                var streamReader = new BsonStreamReader(stream, Utf8Helper.StrictUtf8Encoding);
                var lastRequestLength = (int)stream.Position - _lastRequestPosition;
                stream.Position = _lastRequestPosition;
                var lastArrayItem = streamReader.ReadBytes(lastRequestLength);
                if ((BsonType)lastArrayItem[0] != BsonType.Document)
                {
                    throw new MongoInternalException("Expected overflow item to be a BsonDocument.");
                }
                var sliceOffset = Array.IndexOf<byte>(lastArrayItem, 0) + 1; // skip over type and array index
                var overflow = new ByteArrayBuffer(lastArrayItem, sliceOffset, lastArrayItem.Length - sliceOffset, true);
                stream.Position = _lastRequestPosition;
                stream.SetLength(_lastRequestPosition);

                _batchCount--;
                _batchLength = (int)stream.Position - _batchStartPosition;

                return overflow;
            }
        }
    }
}
