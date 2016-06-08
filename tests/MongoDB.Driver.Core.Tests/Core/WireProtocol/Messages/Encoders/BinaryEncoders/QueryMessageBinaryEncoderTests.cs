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
    public class QueryMessageBinaryEncoderTests
    {
        #region static
        // static fields
        private static readonly bool __awaitData = true;
        private static readonly int __batchSize = 3;
        private static readonly CollectionNamespace __collectionNamespace = new CollectionNamespace("d", "c");
        private static readonly BsonDocument __fields = new BsonDocument("f", 1);
        private static readonly int __flagsOffset;
        private static MessageEncoderSettings __messageEncoderSettings = new MessageEncoderSettings();
        private static readonly bool __noCursorTimeout = true;
        private static readonly bool __oplogReplay = true;
        private static readonly bool __partialOk = true;
        private static readonly BsonDocument __query = new BsonDocument("x", 1);
        private static readonly IElementNameValidator __queryValidator = NoOpElementNameValidator.Instance;
        private static readonly int __requestId = 1;
        private static readonly int __skip = 2;
        private static readonly bool __slaveOk = true;
        private static readonly bool __tailableCursor = true;
        private static readonly QueryMessage __testMessage;
        private static readonly byte[] __testMessageBytes;

        // static constructor
        static QueryMessageBinaryEncoderTests()
        {
            __testMessage = new QueryMessage(__requestId, __collectionNamespace, __query, __fields, __queryValidator, __skip, __batchSize, __slaveOk, __partialOk, __noCursorTimeout, __oplogReplay, __tailableCursor, __awaitData);

            __testMessageBytes = new byte[]
            {
                0, 0, 0, 0, // messageLength
                1, 0, 0, 0, // requestId
                0, 0, 0, 0, // responseTo
                212, 7, 0, 0, // opcode = 2004
                190, 0, 0, 0, // flags
                (byte)'d', (byte)'.', (byte)'c', 0, // fullCollectionName
                2, 0, 0, 0, // numberToSkip
                3, 0, 0, 0, // numberToReturn
                12, 0, 0, 0, 0x10, (byte)'x', 0, 1, 0, 0, 0, 0, // query
                12, 0, 0, 0, 0x10, (byte)'f', 0, 1, 0, 0, 0, 0 // fields
            };
            __testMessageBytes[0] = (byte)__testMessageBytes.Length;
            __flagsOffset = 16;
        }
        #endregion

        [Fact]
        public void Constructor_should_not_throw_if_stream_is_provided()
        {
            using (var stream = new MemoryStream())
            {
                Action action = () => new QueryMessageBinaryEncoder(stream, __messageEncoderSettings);
                action.ShouldNotThrow();
            }
        }

        [Fact]
        public void Constructor_should_throw_if_stream_is_null()
        {
            Action action = () => new QueryMessageBinaryEncoder(null, __messageEncoderSettings);
            action.ShouldThrow<ArgumentException>();
        }

        [Theory]
        [InlineData(0, false, false, false, false, false, false)]
        [InlineData(2, true, false, false, false, false, false)]
        [InlineData(4, false, true, false, false, false, false)]
        [InlineData(16, false, false, true, false, false, false)]
        [InlineData(8, false, false, false, true, false, false)]
        [InlineData(32, false, false, false, false, true, false)]
        [InlineData(128, false, false, false, false, false, true)]
        public void ReadMessage_should_decode_flags_correctly(int flags, bool tailableCursor, bool slaveOk, bool noCursorTimeout, bool oplogReplay, bool awaitData, bool partialOk)
        {
            var bytes = (byte[])__testMessageBytes.Clone();
            bytes[__flagsOffset] = (byte)flags;

            using (var stream = new MemoryStream(bytes))
            {
                var subject = new QueryMessageBinaryEncoder(stream, __messageEncoderSettings);
                var message = subject.ReadMessage();
                message.TailableCursor.Should().Be(tailableCursor);
                message.SlaveOk.Should().Be(slaveOk);
                message.NoCursorTimeout.Should().Be(noCursorTimeout);
                message.OplogReplay.Should().Be(oplogReplay);
                message.AwaitData.Should().Be(awaitData);
                message.PartialOk.Should().Be(partialOk);
            }
        }

        [Fact]
        public void ReadMessage_should_read_a_message()
        {
            using (var stream = new MemoryStream(__testMessageBytes))
            {
                var subject = new QueryMessageBinaryEncoder(stream, __messageEncoderSettings);
                var message = subject.ReadMessage();
                message.CollectionNamespace.Should().Be(__collectionNamespace);
                message.AwaitData.Should().Be(__awaitData);
                message.BatchSize.Should().Be(__batchSize);
                message.Fields.Should().Be(__fields);
                message.NoCursorTimeout.Should().Be(__noCursorTimeout);
                message.OplogReplay.Should().Be(__oplogReplay);
                message.PartialOk.Should().Be(__partialOk);
                message.Query.Should().Be(__query);
                message.RequestId.Should().Be(__requestId);
                message.Skip.Should().Be(__skip);
                message.SlaveOk.Should().Be(__slaveOk);
                message.TailableCursor.Should().Be(__tailableCursor);
            }
        }

        [Theory]
        [InlineData(0, false, false, false, false, false, false)]
        [InlineData(2, true, false, false, false, false, false)]
        [InlineData(4, false, true, false, false, false, false)]
        [InlineData(16, false, false, true, false, false, false)]
        [InlineData(8, false, false, false, true, false, false)]
        [InlineData(32, false, false, false, false, true, false)]
        [InlineData(128, false, false, false, false, false, true)]
        public void WriteMessage_should_encode_flags_correctly(int flags, bool tailableCursor, bool slaveOk, bool noCursorTimeout, bool oplogReplay, bool awaitData, bool partialOk)
        {
            var message = new QueryMessage(__requestId, __collectionNamespace, __query, __fields, __queryValidator, __skip, __batchSize, slaveOk, partialOk, noCursorTimeout, oplogReplay, tailableCursor, awaitData);

            using (var stream = new MemoryStream())
            {
                var subject = new QueryMessageBinaryEncoder(stream, __messageEncoderSettings);
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
                var subject = new QueryMessageBinaryEncoder(stream, __messageEncoderSettings);
                Action action = () => subject.WriteMessage(null);
                action.ShouldThrow<ArgumentNullException>();
            }
        }

        [Fact]
        public void WriteMessage_should_write_a_message()
        {
            using (var stream = new MemoryStream())
            {
                var subject = new QueryMessageBinaryEncoder(stream, __messageEncoderSettings);
                subject.WriteMessage(__testMessage);
                var bytes = stream.ToArray();
                bytes.Should().Equal(__testMessageBytes);
            }
        }
    }
}
