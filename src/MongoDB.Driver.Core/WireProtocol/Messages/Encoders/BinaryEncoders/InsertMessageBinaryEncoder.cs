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
        private void AddDocument(State state, byte[] serializedDocument)
        {
            var streamWriter = _binaryWriter.StreamWriter;
            streamWriter.WriteBytes(serializedDocument);
            state.BatchCount++;
            state.MessageSize = (int)streamWriter.Position - state.MessageStartPosition;
        }

        private void AddDocument(State state, TDocument document)
        {
            var streamWriter = _binaryWriter.StreamWriter;
            var context = BsonSerializationContext.CreateRoot<TDocument>(_binaryWriter);
            _serializer.Serialize(context, document);
            state.BatchCount++;
            state.MessageSize = (int)streamWriter.Position - state.MessageStartPosition;
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

            var documentSource = new BatchableSource<TDocument>(documents);
            var maxBatchCount = 0;
            var maxMessageSize = 0;
            var continueOnError = false;

            return new InsertMessage<TDocument>(
                requestId,
                databaseName,
                collectionName,
                _serializer,
                documentSource,
                maxBatchCount,
                maxMessageSize,
                continueOnError);
        }

        private byte[] RemoveLastDocument(State state, int documentStartPosition)
        {
            var stream = _binaryWriter.Stream;

            var documentSize = (int)stream.Position - documentStartPosition;
            stream.Position = documentStartPosition;
            var serializedDocument = new byte[documentSize];
            stream.FillBuffer(serializedDocument, 0, documentSize);
            stream.Position = documentStartPosition;
            stream.SetLength(documentStartPosition);
            state.BatchCount--;
            state.MessageSize = (int)stream.Position - state.MessageStartPosition;

            return serializedDocument;
        }

        private void WriteDocuments(State state)
        {
            if (state.Message.DocumentSource.Batch != null)
            {
                WriteExistingBatch(state);
            }
            else
            {
                WriteNewBatch(state);
            }
        }

        private void WriteExistingBatch(State state)
        {
            var streamReader = _binaryReader.StreamReader;
            var message = state.Message;

            foreach (var document in message.DocumentSource.Batch)
            {
                AddDocument(state, document);

                if ((state.BatchCount > message.MaxBatchCount || state.MessageSize > message.MaxMessageSize) && state.BatchCount > 1)
                {
                    throw new ArgumentException("The existing batch does not fit in an Insert message.");
                }
            }
        }

        public void WriteMessage(InsertMessage<TDocument> message)
        {
            var streamWriter = _binaryWriter.StreamWriter;
            var messageStartPosition = (int)streamWriter.Position;
            var state = new State { Message = message, MessageStartPosition = messageStartPosition };

            streamWriter.WriteInt32(0); // messageSize
            streamWriter.WriteInt32(message.RequestId);
            streamWriter.WriteInt32(0); // responseTo
            streamWriter.WriteInt32((int)Opcode.Insert);
            streamWriter.WriteInt32((int)BuildInsertFlags(message));
            streamWriter.WriteCString(message.DatabaseName + "." + message.CollectionName);
            WriteDocuments(state);
            streamWriter.BackpatchSize(messageStartPosition);
        }

        private void WriteNewBatch(State state)
        {
            var batch = new List<TDocument>();

            var message = state.Message;
            var documentSource = message.DocumentSource;

            var overflow = (Overflow)documentSource.StartBatch();
            if (overflow != null)
            {
                batch.Add(overflow.Document);
                AddDocument(state, overflow.SerializedDocument);
            }

            // always go one document too far so that we can detect when the docuemntSource runs out of documents
            while (documentSource.MoveNext())
            {
                var document = documentSource.Current;

                var streamReader = _binaryReader.StreamReader;
                var documentStartPosition = (int)streamReader.Position;
                AddDocument(state, document);

                if ((state.BatchCount > message.MaxBatchCount || state.MessageSize > message.MaxMessageSize) && state.BatchCount > 1)
                {
                    var serializedDocument = RemoveLastDocument(state, documentStartPosition);
                    overflow = new Overflow { Document = document, SerializedDocument = serializedDocument };
                    documentSource.EndBatch(batch, overflow);
                    return;
                }

                batch.Add(document);
            }

            documentSource.EndBatch(batch);
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

        // nested types
        private class Overflow
        {
            public TDocument Document;
            public byte[] SerializedDocument;
        }

        [Flags]
        private enum InsertFlags
        {
            None = 0,
            ContinueOnError = 1
        }

        private class State
        {
            public InsertMessage<TDocument> Message;
            public int MessageStartPosition;
            public int BatchCount;
            public int MessageSize;
        }
    }
}
