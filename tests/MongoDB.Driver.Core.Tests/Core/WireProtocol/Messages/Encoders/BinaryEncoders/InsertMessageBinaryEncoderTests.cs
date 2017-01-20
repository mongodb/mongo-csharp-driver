/* Copyright 2013-2016 MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;
using Xunit;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders
{
    public class InsertMessageBinaryEncoderTests
    {
        #region static
        // static fields
        private static readonly CollectionNamespace __collectionNamespace = new CollectionNamespace("d", "c");
        private static readonly bool __continueOnError = true;
        private static readonly BsonDocument[] __documents = new[] { new BsonDocument("_id", 1), new BsonDocument("_id", 2) };
        private static readonly BatchableSource<BsonDocument> __documentSource = new BatchableSource<BsonDocument>(__documents);
        private static readonly int __flagsOffset;
        private static readonly int __maxBatchCount = 1000;
        private static readonly int __maxMessageSize = 40000000;
        private static readonly MessageEncoderSettings __messageEncoderSettings = new MessageEncoderSettings();
        private static readonly int __requestId = 1;
        private static readonly IBsonSerializer<BsonDocument> __serializer = BsonDocumentSerializer.Instance;
        private static readonly InsertMessage<BsonDocument> __testMessage;
        private static readonly byte[] __testMessageBytes;

        // static constructor
        static InsertMessageBinaryEncoderTests()
        {
            __testMessage = new InsertMessage<BsonDocument>(__requestId, __collectionNamespace, __serializer, __documentSource, __maxBatchCount, __maxMessageSize, __continueOnError);

            __testMessageBytes = new byte[]
            {
                0, 0, 0, 0, // messageLength
                1, 0, 0, 0, // requestId
                0, 0, 0, 0, // responseTo
                210, 7, 0, 0, // opcode = 2002
                1, 0, 0, 0, // flags
                (byte)'d', (byte)'.', (byte)'c', 0, // fullCollectionName
                14, 0, 0, 0, 0x10, (byte)'_', (byte)'i', (byte)'d', 0, 1, 0, 0, 0, 0, // documents[0]
                14, 0, 0, 0, 0x10, (byte)'_', (byte)'i', (byte)'d', 0, 2, 0, 0, 0, 0 // documents[1]
            };
            __testMessageBytes[0] = (byte)__testMessageBytes.Length;
            __flagsOffset = 16;
        }
        #endregion

        [Fact]
        public void Constructor_should_not_throw_if_stream_is_not_null()
        {
            using (var stream = new MemoryStream())
            {
                Action action = () => new InsertMessageBinaryEncoder<BsonDocument>(stream, __messageEncoderSettings, __serializer);
                action.ShouldNotThrow();
            }
        }

        [Fact]
        public void Constructor_should_throw_if_stream_is_null()
        {
            Action action = () => new InsertMessageBinaryEncoder<BsonDocument>(null, __messageEncoderSettings, __serializer);
            action.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void Constructor_should_throw_if_serializer_is_null()
        {
            using (var stream = new MemoryStream())
            {
                Action action = () => new InsertMessageBinaryEncoder<BsonDocument>(stream, __messageEncoderSettings, null);
                action.ShouldThrow<ArgumentNullException>();
            }
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, true)]
        public void ReadMessage_should_decode_flags_correctly(int flags, bool continueOnError)
        {
            var bytes = (byte[])__testMessageBytes.Clone();
            bytes[__flagsOffset] = (byte)flags;

            using (var stream = new MemoryStream(bytes))
            {
                var subject = new InsertMessageBinaryEncoder<BsonDocument>(stream, __messageEncoderSettings, __serializer);
                var message = subject.ReadMessage();
                message.ContinueOnError.Should().Be(continueOnError);
            }
        }

        [Fact]
        public void ReadMessage_should_read_a_message()
        {
            using (var stream = new MemoryStream(__testMessageBytes))
            {
                var subject = new InsertMessageBinaryEncoder<BsonDocument>(stream, __messageEncoderSettings, __serializer);
                var message = subject.ReadMessage();
                message.CollectionNamespace.Should().Be(__collectionNamespace);
                message.ContinueOnError.Should().Be(__continueOnError);
                message.DocumentSource.Batch.Should().Equal(__documentSource.Batch);
                message.MaxBatchCount.Should().Be(int.MaxValue);
                message.MaxMessageSize.Should().Be(int.MaxValue);
                message.RequestId.Should().Be(__requestId);
                message.Serializer.Should().BeSameAs(__serializer);
            }
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, true)]
        public void WriteMessage_should_encode_flags_correctly(int flags, bool continueOnError)
        {
            var message = new InsertMessage<BsonDocument>(__requestId, __collectionNamespace, __serializer, __documentSource, __maxBatchCount, __maxMessageSize, continueOnError);

            using (var stream = new MemoryStream())
            {
                var subject = new InsertMessageBinaryEncoder<BsonDocument>(stream, __messageEncoderSettings, __serializer);
                subject.WriteMessage(message);
                var bytes = stream.ToArray();
                bytes[__flagsOffset].Should().Be((byte)flags);
            }
        }

        [Theory]
        [InlineData(1, 1, 1)]
        [InlineData(1, 2, 1)]
        [InlineData(2, 1, 2)]
        [InlineData(2, 2, 1)]
        [InlineData(2, 3, 1)]
        [InlineData(3, 1, 3)]
        [InlineData(3, 2, 2)]
        [InlineData(3, 3, 1)]
        [InlineData(3, 4, 1)]
        public void WriteMessage_should_split_batches_when_maxBatchCount_is_reached(int numberOfDocuments, int maxBatchCount, int expectedNumberOfBatches)
        {
            var documents = new List<BsonDocument>(numberOfDocuments);
            for (var i = 0; i < numberOfDocuments; i++)
            {
                documents.Add(new BsonDocument("_id", i));
            }

            using (var enumerator = documents.GetEnumerator())
            {
                var documentSource = new BatchableSource<BsonDocument>(enumerator);
                var message = new InsertMessage<BsonDocument>(__requestId, __collectionNamespace, __serializer, documentSource, maxBatchCount, __maxMessageSize, __continueOnError);

                var numberOfBatches = 0;
                var batchedDocuments = new List<BsonDocument>();

                while (documentSource.HasMore)
                {
                    using (var stream = new MemoryStream())
                    {
                        var subject = new InsertMessageBinaryEncoder<BsonDocument>(stream, __messageEncoderSettings, __serializer);
                        subject.WriteMessage(message);
                    }

                    numberOfBatches++;
                    batchedDocuments.AddRange(documentSource.Batch);

                    documentSource.ClearBatch();
                }

                numberOfBatches.Should().Be(expectedNumberOfBatches);
                batchedDocuments.Should().Equal(documents);
            }
        }

        [Theory]
        [InlineData(1, 1, 0, 1)]
        [InlineData(1, 1, -1, 1)]
        [InlineData(1, 1, 1, 1)]
        [InlineData(2, 1, 0, 2)]
        [InlineData(2, 2, 0, 1)]
        [InlineData(2, 2, -1, 2)]
        [InlineData(2, 2, 1, 1)]
        public void WriteMessage_should_split_batches_when_maxMessageSize_is_reached(int numberOfDocuments, int maxMessageSizeMultiple, int maxMessageSizeDelta, int expectedNumberOfBatches)
        {
            var documents = new List<BsonDocument>(numberOfDocuments);
            for (var i = 0; i < numberOfDocuments; i++)
            {
                documents.Add(new BsonDocument("_id", i));
            }

            var documentSize = documents[0].ToBson().Length;
            var messageSizeWithZeroDocuments = __testMessageBytes.Length - 2 * documentSize;
            var maxMessageSize = messageSizeWithZeroDocuments + (maxMessageSizeMultiple * documentSize) + maxMessageSizeDelta;

            using (var enumerator = documents.GetEnumerator())
            {
                var documentSource = new BatchableSource<BsonDocument>(enumerator);
                var message = new InsertMessage<BsonDocument>(__requestId, __collectionNamespace, __serializer, documentSource, __maxBatchCount, maxMessageSize, __continueOnError);

                var numberOfBatches = 0;
                var batchedDocuments = new List<BsonDocument>();

                while (documentSource.HasMore)
                {
                    using (var stream = new MemoryStream())
                    {
                        var subject = new InsertMessageBinaryEncoder<BsonDocument>(stream, __messageEncoderSettings, __serializer);
                        subject.WriteMessage(message);
                    }

                    numberOfBatches++;
                    batchedDocuments.AddRange(documentSource.Batch);

                    documentSource.ClearBatch();
                }

                numberOfBatches.Should().Be(expectedNumberOfBatches);
                batchedDocuments.Should().Equal(documents);
            }
        }

        [Fact]
        public void WriteMessage_should_throw_if_message_is_null()
        {
            using (var stream = new MemoryStream())
            {
                var subject = new InsertMessageBinaryEncoder<BsonDocument>(stream, __messageEncoderSettings, __serializer);
                Action action = () => subject.WriteMessage(null);
                action.ShouldThrow<ArgumentNullException>();
            }
        }

        [Fact]
        public void WriteMessage_should_write_a_message()
        {
            using (var stream = new MemoryStream())
            {
                var subject = new InsertMessageBinaryEncoder<BsonDocument>(stream, __messageEncoderSettings, __serializer);
                subject.WriteMessage(__testMessage);
                var bytes = stream.ToArray();
                bytes.Should().Equal(__testMessageBytes);
            }
        }
    }
}