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
using System.Text;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver.Support;

namespace MongoDB.Driver.Internal
{
    internal class MongoInsertMessage : MongoRequestMessage
    {
        // private fields
        private readonly Batch<InsertRequest> _batch;
        private int _batchCount;
        private int _batchLength;
        private BatchProgress<InsertRequest> _batchProgress;
        private int _batchStartPosition;
        private IBsonSerializer _cachedSerializer;
        private Type _cachedSerializerType;
        private readonly string _collectionFullName;
        private readonly bool _checkElementNames;
        private readonly InsertFlags _flags;
        private readonly int _maxBatchCount;
        private readonly int _maxBatchLength;
        private readonly int _maxDocumentSize;
        private int _lastDocumentStartPosition;

        // constructors
        internal MongoInsertMessage(
            BsonBinaryWriterSettings writerSettings,
            string collectionFullName,
            bool checkElementNames,
            InsertFlags flags,
            int maxBatchCount,
            int maxBatchLength,
            int maxDocumentSize,
            Batch<InsertRequest> batch)
            : base(MessageOpcode.Insert, writerSettings)
        {
            _collectionFullName = collectionFullName;
            _checkElementNames = checkElementNames;
            _flags = flags;
            _maxBatchCount = maxBatchCount;
            _maxBatchLength = maxBatchLength;
            _maxDocumentSize = maxDocumentSize;
            _batch = batch;
        }

        // public properties
        public BatchProgress<InsertRequest> BatchProgress
        {
            get { return _batchProgress; }
        }

        // internal methods
        internal override void WriteBodyTo(BsonBuffer buffer)
        {
            var processedRequests = new List<InsertRequest>();

            var continuationBatch = _batch as ContinuationBatch<InsertRequest, byte[]>;
            if (continuationBatch != null)
            {
                AddOverflow(buffer, continuationBatch.PendingState);
                processedRequests.Add(continuationBatch.PendingItem);
                continuationBatch.ClearPending(); // so pending objects can be garbage collected sooner
            }

            // always go one document too far so that we can set IsDone as early as possible
            var enumerator = _batch.Enumerator;
            while (enumerator.MoveNext())
            {
                var request = enumerator.Current;
                AddRequest(buffer, request);

                if ((_batchCount > _maxBatchCount || _batchLength > _maxBatchLength) && _batchCount > 1)
                {
                    var serializedDocument = RemoveLastDocument(buffer);
                    var nextBatch = new ContinuationBatch<InsertRequest, byte[]>(enumerator, request, serializedDocument);
                    _batchProgress = new BatchProgress<InsertRequest>(_batchCount, _batchLength, processedRequests, nextBatch);
                    return;
                }

                processedRequests.Add(request);
            }

            _batchProgress = new BatchProgress<InsertRequest>(_batchCount, _batchLength, processedRequests, null);
        }

        internal override void WriteHeaderTo(BsonBuffer buffer)
        {
            _batchStartPosition = buffer.Position;
            base.WriteHeaderTo(buffer);
            buffer.WriteInt32((int)_flags);
            buffer.WriteCString(new UTF8Encoding(false, true), _collectionFullName);
        }

        // private methods
        private void AddOverflow(BsonBuffer buffer, byte[] serializedDocument)
        {
            buffer.WriteBytes(serializedDocument);

            _batchCount++;
            _batchLength = buffer.Position - _batchStartPosition;
        }

        private void AddRequest(BsonBuffer buffer, InsertRequest request)
        {
            var document = request.Document;
            if (document == null)
            {
                throw new ArgumentException("Batch contains one or more null documents.");
            }

            var serializer = request.Serializer;
            if (serializer == null)
            {
                var actualType = document.GetType();
                if (_cachedSerializerType != actualType)
                {
                    _cachedSerializer = BsonSerializer.LookupSerializer(actualType);
                    _cachedSerializerType = actualType;
                }
                serializer = _cachedSerializer;
            }
            var serializationOptions = request.SerializationOptions ?? DocumentSerializationOptions.SerializeIdFirstInstance;

            _lastDocumentStartPosition = buffer.Position;
            using (var bsonWriter = new BsonBinaryWriter(buffer, false, WriterSettings))
            {
                bsonWriter.PushMaxDocumentSize(_maxDocumentSize);
                bsonWriter.CheckElementNames = _checkElementNames;
                serializer.Serialize(bsonWriter, request.NominalType, document, serializationOptions);
                bsonWriter.PopMaxDocumentSize();
            }

            _batchCount++;
            _batchLength = buffer.Position - _batchStartPosition;
        }

        private byte[] RemoveLastDocument(BsonBuffer buffer)
        {
            var lastDocumentLength = buffer.Position - _lastDocumentStartPosition;
            buffer.Position = _lastDocumentStartPosition;
            var lastDocument = buffer.ReadBytes(lastDocumentLength);
            buffer.Position = _lastDocumentStartPosition;
            buffer.Length = _lastDocumentStartPosition;

            _batchCount -= 1;
            _batchLength = buffer.Position - _batchStartPosition;

            return lastDocument;
        }
    }
}
