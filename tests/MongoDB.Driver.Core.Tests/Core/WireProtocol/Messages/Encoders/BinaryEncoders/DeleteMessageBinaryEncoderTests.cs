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
using System.IO;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;
using Xunit;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders
{
    public class DeleteMessageBinaryEncoderTests
    {
        #region static
        // static fields
        private static readonly CollectionNamespace __collectionNamespace = new CollectionNamespace("d", "c");
        private static readonly int __flagsOffset;
        private static readonly bool __isMulti = false;
        private static readonly MessageEncoderSettings __messageEncoderSettings = new MessageEncoderSettings();
        private static readonly BsonDocument __query = new BsonDocument("x", 1);
        private static readonly int __requestId = 1;
        private static readonly DeleteMessage __testMessage;
        private static readonly byte[] __testMessageBytes;

        // static constructor
        static DeleteMessageBinaryEncoderTests()
        {
            __testMessage = new DeleteMessage(__requestId, __collectionNamespace, __query, __isMulti);

            __testMessageBytes = new byte[]
            {
                0, 0, 0, 0, // messageLength
                1, 0, 0, 0, // requestId
                0, 0, 0, 0, // responseTo
                214, 7, 0, 0, // opcode = 2006
                0, 0, 0, 0, // reserved
                (byte)'d', (byte)'.', (byte)'c', 0, // fullCollectionName
                1, 0, 0, 0, // flags
                12, 0, 0, 0, 0x10, (byte)'x', 0, 1, 0, 0, 0, 0 // query
            };
            __testMessageBytes[0] = (byte)__testMessageBytes.Length;
            __flagsOffset = 20 + __collectionNamespace.FullName.Length + 1;
        }
        #endregion

        [Fact]
        public void Constructor_should_not_throw_if_stream_is_not_null()
        {
            using (var stream = new MemoryStream())
            {
                Action action = () => new DeleteMessageBinaryEncoder(stream, __messageEncoderSettings);
                action.ShouldNotThrow();
            }
        }

        [Fact]
        public void Constructor_should_throw_if_stream_is_null()
        {
            Action action = () => new DeleteMessageBinaryEncoder(null, __messageEncoderSettings);
            action.ShouldThrow<ArgumentException>();
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(1, false)]
        public void ReadMessage_should_decode_flags_correctly(int flags, bool isMulti)
        {
            var bytes = (byte[])__testMessageBytes.Clone();
            bytes[__flagsOffset] = (byte)flags;

            using (var stream = new MemoryStream(bytes))
            {
                var subject = new DeleteMessageBinaryEncoder(stream, __messageEncoderSettings);
                var message = subject.ReadMessage();
                message.IsMulti.Should().Be(isMulti);
            }
        }

        [Fact]
        public void ReadMessage_should_read_a_message()
        {
            using (var stream = new MemoryStream(__testMessageBytes))
            {
                var subject = new DeleteMessageBinaryEncoder(stream, __messageEncoderSettings);
                var message = subject.ReadMessage();
                message.CollectionNamespace.Should().Be(__collectionNamespace);
                message.IsMulti.Should().Be(__isMulti);
                message.Query.Should().Be(__query);
                message.RequestId.Should().Be(__requestId);
            }
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(1, false)]
        public void WriteMessage_should_encode_flags_correctly(int flags, bool isMulti)
        {
            var message = new DeleteMessage(__requestId, __collectionNamespace, __query, isMulti);

            using (var stream = new MemoryStream())
            {
                var subject = new DeleteMessageBinaryEncoder(stream, null);
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
                var subject = new DeleteMessageBinaryEncoder(stream, null);
                Action action = () => subject.WriteMessage(null);
                action.ShouldThrow<ArgumentNullException>();
            }
        }

        [Fact]
        public void WriteMessage_should_write_a_message()
        {
            using (var stream = new MemoryStream())
            {
                var subject = new DeleteMessageBinaryEncoder(stream, null);
                subject.WriteMessage(__testMessage);
                var bytes = stream.ToArray();
                bytes.Should().Equal(__testMessageBytes);
            }
        }
    }
}
