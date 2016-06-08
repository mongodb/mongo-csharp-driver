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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders
{
    public class ReplyMessageBinaryEncoderTests
    {
        #region static
        // static fields
        private static readonly bool __awaitCapable = true;
        private static readonly long __cursorId = 3;
        private static readonly bool __cursorNotFound = true;
        private static readonly List<BsonDocument> __documents = new List<BsonDocument>(new[] { new BsonDocument("_id", 1), new BsonDocument("_id", 2) });
        private static readonly int __flagsOffset;
        private static readonly MessageEncoderSettings __messageEncoderSettings = new MessageEncoderSettings();
        private static readonly int __numberReturned = 2;
        private static readonly BsonDocument __queryFailureDocument = new BsonDocument("ok", 0);
        private static readonly ReplyMessage<BsonDocument> __queryFailureMessage;
        private static readonly byte[] __queryFailureMessageBytes;
        private static readonly int __requestId = 1;
        private static readonly int __responseTo = 2;
        private static readonly IBsonSerializer<BsonDocument> __serializer = BsonDocumentSerializer.Instance;
        private static readonly int __startingFrom = 4;
        private static readonly ReplyMessage<BsonDocument> __testMessage;
        private static readonly byte[] __testMessageBytes;

        // static constructor
        static ReplyMessageBinaryEncoderTests()
        {
            __testMessage = new ReplyMessage<BsonDocument>(__awaitCapable, __cursorId, __cursorNotFound, __documents, __numberReturned, false, null, __requestId, __responseTo, __serializer, __startingFrom);

            __testMessageBytes = new byte[]
            {
                0, 0, 0, 0, // messageLength
                1, 0, 0, 0, // requestId
                2, 0, 0, 0, // responseTo
                1, 0, 0, 0, // opcode = 1
                9, 0, 0, 0, // responseFlags
                3, 0, 0, 0, 0, 0, 0, 0, // cursorId
                4, 0, 0, 0, // startingFrom
                2, 0, 0, 0, // numberReturned
                14, 0, 0, 0, 0x10, (byte)'_', (byte)'i', (byte)'d', 0, 1, 0, 0, 0, 0, // documents[0]
                14, 0, 0, 0, 0x10, (byte)'_', (byte)'i', (byte)'d', 0, 2, 0, 0, 0, 0, // documents[1]
            };
            __testMessageBytes[0] = (byte)__testMessageBytes.Length;
            __flagsOffset = 16;

            __queryFailureMessage = new ReplyMessage<BsonDocument>(false, __cursorId, false, null, 0, true, __queryFailureDocument, __requestId, __responseTo, __serializer, 0);

            __queryFailureMessageBytes = new byte[]
            {
                0, 0, 0, 0, // messageLength
                1, 0, 0, 0, // requestId
                2, 0, 0, 0, // responseTo
                1, 0, 0, 0, // opcode = 1
                2, 0, 0, 0, // responseFlags
                3, 0, 0, 0, 0, 0, 0, 0, // cursorId
                0, 0, 0, 0, // startingFrom
                0, 0, 0, 0, // numberReturned
                13, 0, 0, 0, 0x10, (byte)'o', (byte)'k', 0, 0, 0, 0, 0, 0, // queryFailureDocument
            };
            __queryFailureMessageBytes[0] = (byte)__queryFailureMessageBytes.Length;
        }
        #endregion

        [Fact]
        public void Constructor_should_not_throw_if_stream_provided()
        {
            using (var stream = new MemoryStream())
            {
                Action action = () => new ReplyMessageBinaryEncoder<BsonDocument>(stream, __messageEncoderSettings, __serializer);
                action.ShouldNotThrow();
            }
        }

        [Fact]
        public void Constructor_should_throw_if_stream_is_null()
        {
            Action action = () => new ReplyMessageBinaryEncoder<BsonDocument>(null, __messageEncoderSettings, __serializer);
            action.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void Constructor_should_throw_if_serializer_is_null()
        {
            using (var stream = new MemoryStream())
            {
                Action action = () => new ReplyMessageBinaryEncoder<BsonDocument>(stream, __messageEncoderSettings, null);
                action.ShouldThrow<ArgumentNullException>();
            }
        }

        [Theory]
        [InlineData(0, false, false, false)]
        [InlineData(1, true, false, false)]
        [InlineData(2, false, true, false)]
        [InlineData(8, false, false, true)]
        public void ReadMessage_should_decode_flags_correctly(int flags, bool cursorNotFound, bool queryFailure, bool awaitCapable)
        {
            var bytes = (byte[])__testMessageBytes.Clone();
            bytes[__flagsOffset] = (byte)flags;

            using (var stream = new MemoryStream(bytes))
            {
                var subject = new ReplyMessageBinaryEncoder<BsonDocument>(stream, __messageEncoderSettings, __serializer);
                var message = subject.ReadMessage();
                message.CursorNotFound.Should().Be(cursorNotFound);
                message.QueryFailure.Should().Be(queryFailure);
                message.AwaitCapable.Should().Be(awaitCapable);
            }
        }

        [Fact]
        public void ReadMessage_should_read_a_message()
        {
            using (var stream = new MemoryStream(__testMessageBytes))
            {
                var subject = new ReplyMessageBinaryEncoder<BsonDocument>(stream, __messageEncoderSettings, __serializer);
                var message = subject.ReadMessage();
                message.AwaitCapable.Should().Be(__awaitCapable);
                message.CursorId.Should().Be(__cursorId);
                message.CursorNotFound.Should().Be(__cursorNotFound);
                message.Documents.Should().Equal(__documents);
                message.NumberReturned.Should().Be(__numberReturned);
                message.QueryFailure.Should().Be(false);
                message.QueryFailureDocument.Should().BeNull();
                message.Serializer.Should().BeSameAs(__serializer);
                message.StartingFrom.Should().Be(__startingFrom);
                message.RequestId.Should().Be(__requestId);
                message.ResponseTo.Should().Be(__responseTo);
            }
        }

        [Fact]
        public void ReadMessage_should_read_a_query_failure_message()
        {
            using (var stream = new MemoryStream(__queryFailureMessageBytes))
            {
                var subject = new ReplyMessageBinaryEncoder<BsonDocument>(stream, __messageEncoderSettings, __serializer);
                var message = subject.ReadMessage();
                message.AwaitCapable.Should().Be(false);
                message.CursorId.Should().Be(__cursorId);
                message.CursorNotFound.Should().Be(false);
                message.Documents.Should().BeNull();
                message.NumberReturned.Should().Be(0);
                message.QueryFailure.Should().Be(true);
                message.QueryFailureDocument.Should().Be(__queryFailureDocument);
                message.Serializer.Should().BeSameAs(__serializer);
                message.StartingFrom.Should().Be(0);
                message.RequestId.Should().Be(__requestId);
                message.ResponseTo.Should().Be(__responseTo);
            }
        }

        [Theory]
        [InlineData(0, false, false, false)]
        [InlineData(1, true, false, false)]
        [InlineData(2, false, true, false)]
        [InlineData(8, false, false, true)]
        public void WriteMessage_should_encode_flags_correctly(int flags, bool cursorNotFound, bool queryFailure, bool awaitCapable)
        {
            var documents = queryFailure ? null : __documents;
            var queryFailureDocument = queryFailure ? new BsonDocument("ok", 0) : null;
            var message = new ReplyMessage<BsonDocument>(awaitCapable, __cursorId, cursorNotFound, documents, __numberReturned, queryFailure, queryFailureDocument, __requestId, __responseTo, __serializer, __startingFrom);

            using (var stream = new MemoryStream())
            {
                var subject = new ReplyMessageBinaryEncoder<BsonDocument>(stream, __messageEncoderSettings, __serializer);
                subject.WriteMessage(message);
                var bytes = stream.ToArray();
                bytes[__flagsOffset].Should().Be((byte)flags);
            }
        }

        [Fact]
        public void WriteMessage_should_throw_if_message_is_null()
        {
            using (var stream = new MemoryStream())
            {
                var subject = new ReplyMessageBinaryEncoder<BsonDocument>(stream, __messageEncoderSettings, __serializer);
                Action action = () => subject.WriteMessage(null);
                action.ShouldThrow<ArgumentNullException>();
            }
        }

        [Fact]
        public void WriteMessage_should_write_a_message()
        {
            using (var stream = new MemoryStream())
            {
                var subject = new ReplyMessageBinaryEncoder<BsonDocument>(stream, __messageEncoderSettings, __serializer);
                subject.WriteMessage(__testMessage);
                var bytes = stream.ToArray();
                bytes.Should().Equal(__testMessageBytes);
            }
        }

        [Fact]
        public void WriteMessage_should_write_a_query_failure_message()
        {
            using (var stream = new MemoryStream())
            {
                var subject = new ReplyMessageBinaryEncoder<BsonDocument>(stream, __messageEncoderSettings, __serializer);
                subject.WriteMessage(__queryFailureMessage);
                var bytes = stream.ToArray();
                bytes.Should().Equal(__queryFailureMessageBytes);
            }
        }
    }
}
