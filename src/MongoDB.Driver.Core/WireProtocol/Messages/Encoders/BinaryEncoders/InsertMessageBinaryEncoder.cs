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
using System.IO;
using System.Text;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders
{
    public class InsertMessageBinaryEncoder<TDocument> : IMessageEncoder<InsertMessage<TDocument>>
    {
        // fields
        private readonly BsonBinaryReader _binaryReader;
        private readonly BsonBinaryWriter _binaryWriter;
        private readonly IBsonSerializer<TDocument> _serializer;

        // constructors
        public InsertMessageBinaryEncoder(BsonBinaryReader binaryReader, BsonBinaryWriter binaryWriter, IBsonSerializer<TDocument> serializer)
        {
            _binaryReader = binaryReader;
            _binaryWriter = binaryWriter;
            _serializer = Ensure.IsNotNull(serializer, "serializer");
        }

        // methods
        private void AddDocument(TDocument document, byte[] serializedDocument, ProgressTracker tracker)
        {
            var streamWriter = _binaryWriter.StreamWriter;
            streamWriter.WriteBytes(serializedDocument);
            tracker.BatchCount++;
            tracker.MessageSize = (int)streamWriter.Position - tracker.MessageStartPosition;
            tracker.Documents.Add(document);
        }

        private void AddDocument(TDocument document, ProgressTracker tracker)
        {
            var streamWriter = _binaryWriter.StreamWriter;
            var context = BsonSerializationContext.CreateRoot<TDocument>(_binaryWriter);
            _serializer.Serialize(context, document);
            tracker.BatchCount++;
            tracker.MessageSize = (int)streamWriter.Position - tracker.MessageStartPosition;
            tracker.Documents.Add(document);
        }

        private InsertFlags BuildInsertFlags(InsertMessage<TDocument> message)
        {
            var flags = InsertFlags.None;
            if (message.ContinueOnError)
            {
                flags |= InsertFlags.ContinueOnError;
            }
            return flags;
        }

        public InsertMessage<TDocument> ReadMessage()
        {
            var streamReader = _binaryReader.StreamReader;
            var startPosition = streamReader.Position;

            var messageSize = streamReader.ReadInt32();
            var requestId = streamReader.ReadInt32();
            var responseTo = streamReader.ReadInt32();
            var opcode = (Opcode)streamReader.ReadInt32();
            var flags = (InsertFlags)streamReader.ReadInt32();
            var fullCollectionName = streamReader.ReadCString();
            var documents = new List<TDocument>();
            while (streamReader.Position < startPosition + messageSize)
            {
                var context = BsonDeserializationContext.CreateRoot<TDocument>(_binaryReader);
                var document = _serializer.Deserialize(context);
                documents.Add(document);
            }

            var firstDot = fullCollectionName.IndexOf('.');
            var databaseName = fullCollectionName.Substring(0, firstDot);
            var collectionName = fullCollectionName.Substring(firstDot + 1);

            var batch = new FirstBatch<TDocument>(documents.GetEnumerator(), canBeSplit: false);
            var maxBatchCount = 0;
            var maxMessageSize = 0;
            var continueOnError = false;

            return new InsertMessage<TDocument>(
                requestId,
                databaseName,
                collectionName,
                _serializer,
                batch,
                maxBatchCount,
                maxMessageSize,
                continueOnError);
        }

        private byte[] RemoveLastDocument(int documentStartPosition, ProgressTracker tracker)
        {
            var streamReader = _binaryReader.StreamReader;
            var stream = streamReader.BaseStream;

            var documentSize = (int)streamReader.Position - documentStartPosition;
            streamReader.Position = documentStartPosition;
            var serializedDocument = new byte[documentSize];
            stream.FillBuffer(serializedDocument, 0, documentSize);
            streamReader.Position = documentStartPosition;
            stream.SetLength(documentStartPosition);
            tracker.BatchCount--;
            tracker.MessageSize = (int)streamReader.Position - tracker.MessageStartPosition;
            tracker.Documents.RemoveAt(tracker.Documents.Count - 1);
            return serializedDocument;
        }

        private void WriteDocuments(int messageStartPosition, InsertMessage<TDocument> message)
        {
            var batch = message.Documents;

            var tracker = new ProgressTracker { MessageStartPosition = messageStartPosition, Documents = new List<TDocument>() };

            var continuationBatch = batch as ContinuationBatch<TDocument, byte[]>;
            if (continuationBatch != null)
            {
                var document = continuationBatch.PendingItem;
                var serializedDocument = continuationBatch.PendingState;
                AddDocument(document, serializedDocument, tracker);
                continuationBatch.ClearPending(); // so it can get garbage collected sooner
            }

            var enumerator = batch.Enumerator;
            while (enumerator.MoveNext())
            {
                var streamReader = _binaryReader.StreamReader;
                var document = enumerator.Current;
                var documentStartPosition = (int)streamReader.Position;
                AddDocument(document, tracker);

                if ((tracker.BatchCount > message.MaxBatchCount || tracker.MessageSize > message.MaxMessageSize) && tracker.BatchCount > 1)
                {
                    var firstBatch = batch as FirstBatch<TDocument>;
                    if (firstBatch != null && !firstBatch.CanBeSplit)
                    {
                        throw new ArgumentException("The documents did not fit in a single batch.");
                    }

                    var serializedDocument = RemoveLastDocument(documentStartPosition, tracker);
                    var nextBatch = new ContinuationBatch<TDocument, byte[]>(enumerator, document, serializedDocument);
                    var intermediateBatchResult = new BatchResult<TDocument>(tracker.BatchCount, tracker.MessageSize, tracker.Documents, nextBatch);
                    batch.SetResult(intermediateBatchResult);
                    return;
                }

            }

            var lastBatchResult = new BatchResult<TDocument>(tracker.BatchCount, tracker.MessageSize, tracker.Documents, null);
            batch.SetResult(lastBatchResult);
        }

        public void WriteMessage(InsertMessage<TDocument> message)
        {
            var streamWriter = _binaryWriter.StreamWriter;
            var messageStartPosition = (int)streamWriter.Position;

            streamWriter.WriteInt32(0); // messageSize
            streamWriter.WriteInt32(message.RequestId);
            streamWriter.WriteInt32(0); // responseTo
            streamWriter.WriteInt32((int)Opcode.Insert);
            streamWriter.WriteInt32((int)BuildInsertFlags(message));
            streamWriter.WriteCString(message.DatabaseName + "." + message.CollectionName);
            WriteDocuments(messageStartPosition, message);
            streamWriter.BackpatchSize(messageStartPosition);
        }

        // explicit interface implementations
        MongoDBMessage IMessageEncoder.ReadMessage()
        {
            return ReadMessage();
        }

        void IMessageEncoder.WriteMessage(MongoDBMessage message)
        {
            WriteMessage((InsertMessage<TDocument>)message);
        }

        [Flags]
        private enum InsertFlags
        {
            None = 0,
            ContinueOnError = 1
        }

        private class ProgressTracker
        {
            public int MessageStartPosition;
            public int BatchCount;
            public int MessageSize;
            public List<TDocument> Documents;
        }
    }
}
