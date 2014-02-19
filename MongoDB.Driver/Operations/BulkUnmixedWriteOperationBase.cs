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
        protected abstract BatchSerializer CreateBatchSerializer();

        protected virtual IEnumerable<WriteRequest> DecorateRequests(IEnumerable<WriteRequest> requests)
        {
            return requests;
        }

        // private methods
        private CommandDocument CreateWriteCommand(BatchSerializer batchSerializer, Batch<WriteRequest> batch)
        {
            var wrappedActualType = batch.GetType();
            var batchWrapper = new BsonDocumentWrapper(wrappedActualType, batch, batchSerializer, null, false);

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
                null, // serializationOptions
                BsonSerializer.LookupSerializer(typeof(CommandResult))); // resultSerializer
        }

        private BulkWriteBatchResult ExecuteBatch(MongoConnection connection, Batch<WriteRequest> batch, int originalIndex)
        {
            var batchSerializer = CreateBatchSerializer();
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
        protected abstract class BatchSerializer : BsonBaseSerializer
        {
            // private fields
            private readonly BulkWriteOperationArgs _args;
            private int _batchCount;
            private int _batchLength;
            private BatchProgress<WriteRequest> _batchProgress;
            private int _batchStartPosition;
            private int _lastRequestPosition;

            // constructors
            public BatchSerializer(BulkWriteOperationArgs args)
            {
                _args = args;
            }

            // public properties
            public BatchProgress<WriteRequest> BatchProgress
            {
                get { return _batchProgress; }
            }

            // public methods
            public override void Serialize(BsonWriter bsonWriter, Type nominalType, object value, IBsonSerializationOptions options)
            {
                var batch = (Batch<WriteRequest>)value;
                var bsonBinaryWriter = (BsonBinaryWriter)bsonWriter;
                _batchStartPosition = bsonBinaryWriter.Buffer.Position;
                var processedRequests = new List<WriteRequest>();

                var continuationBatch = batch as ContinuationBatch<WriteRequest, IByteBuffer>;
                if (continuationBatch != null)
                {
                    AddOverfow(bsonBinaryWriter, continuationBatch.PendingState);
                    processedRequests.Add(continuationBatch.PendingItem);
                    continuationBatch.ClearPending(); // so pending objects can be garbage collected sooner
                }

                var maxBatchLength = _args.MaxBatchLength;
                if (maxBatchLength > _args.MaxDocumentSize)
                {
                    maxBatchLength = _args.MaxDocumentSize; // not MaxWireDocumentSize! leave room for overhead
                }

                // always go one document too far so that we can set IsDone as early as possible
                var enumerator = batch.Enumerator;
                while (enumerator.MoveNext())
                {
                    var request = enumerator.Current;
                    AddRequest(bsonBinaryWriter, request);

                    if ((_batchCount > _args.MaxBatchCount || _batchLength > maxBatchLength) && _batchCount > 1)
                    {
                        var serializedRequest = RemoveOverflow(bsonBinaryWriter.Buffer);
                        var nextBatch = new ContinuationBatch<WriteRequest, IByteBuffer>(enumerator, request, serializedRequest);
                        _batchProgress = new BatchProgress<WriteRequest>(_batchCount, _batchLength, processedRequests, nextBatch);
                        return;
                    }

                    processedRequests.Add(request);
                }

                _batchProgress = new BatchProgress<WriteRequest>(_batchCount, _batchLength, processedRequests, null);
            }

            // protected methods
            protected abstract void SerializeRequest(BsonBinaryWriter bsonWriter, WriteRequest request);

            // private methods
            private void AddOverfow(BsonBinaryWriter bsonBinaryWriter, IByteBuffer overflow)
            {
                _lastRequestPosition = bsonBinaryWriter.Buffer.Position;
                bsonBinaryWriter.WriteRawBsonDocument(overflow);
                _batchCount++;
                _batchLength = bsonBinaryWriter.Buffer.Position - _batchStartPosition;
            }

            private void AddRequest(BsonBinaryWriter bsonBinaryWriter, WriteRequest request)
            {
                _lastRequestPosition = bsonBinaryWriter.Buffer.Position;
                SerializeRequest(bsonBinaryWriter, request);
                _batchCount++;
                _batchLength = bsonBinaryWriter.Buffer.Position - _batchStartPosition;
            }

            private IByteBuffer RemoveOverflow(BsonBuffer buffer)
            {
                var lastRequestLength = buffer.Position - _lastRequestPosition;
                buffer.Position = _lastRequestPosition;
                var lastArrayItem = buffer.ReadBytes(lastRequestLength);
                if ((BsonType)lastArrayItem[0] != BsonType.Document)
                {
                    throw new MongoInternalException("Expected overflow item to be a BsonDocument.");
                }
                var sliceOffset = Array.IndexOf<byte>(lastArrayItem, 0) + 1; // skip over type and array index
                var overflow = new ByteArrayBuffer(lastArrayItem, sliceOffset, lastArrayItem.Length - sliceOffset, true);
                buffer.Position = _lastRequestPosition;
                buffer.Length = _lastRequestPosition;

                _batchCount--;
                _batchLength = buffer.Position - _batchStartPosition;

                return overflow;
            }
        }
    }
}
