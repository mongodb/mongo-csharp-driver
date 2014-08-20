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
using System.IO;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;
using NUnit.Framework;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders
{
    [TestFixture]
    public class UpdateMessageBinaryEncoderTests
    {
        #region static
        // static fields
        private static readonly string __collectionName = "c";
        private static readonly string __databaseName = "d";
        private static readonly int __flagsOffset;
        private static readonly bool __isMulti = true;
        private static readonly bool __isUpsert = true;
        private static readonly BsonDocument __query = new BsonDocument("x", 1);
        private static readonly int __requestId = 1;
        private static readonly UpdateMessage __testMessage;
        private static readonly byte[] __testMessageBytes;
        private static readonly BsonDocument __update = new BsonDocument("y", 1);

        // static constructor
        static UpdateMessageBinaryEncoderTests()
        {
            __testMessage = new UpdateMessage(__requestId, __databaseName, __collectionName, __query, __update, __isMulti, __isUpsert);

            __testMessageBytes = new byte[]
            {
                0, 0, 0, 0, // messageLength
                1, 0, 0, 0, // requestId
                0, 0, 0, 0, // responseTo
                209, 7, 0, 0, // opcode = 2001
                0, 0, 0, 0, // reserved
                (byte)'d', (byte)'.', (byte)'c', 0, // fullCollectionName
                3, 0, 0, 0, // flags
                12, 0, 0, 0, 0x10, (byte)'x', 0, 1, 0, 0, 0, 0, // query
                12, 0, 0, 0, 0x10, (byte)'y', 0, 1, 0, 0, 0, 0 // fields
            };
            __testMessageBytes[0] = (byte)__testMessageBytes.Length;
            __flagsOffset = 20 + (__databaseName.Length + 1 + __collectionName.Length + 1);
        }
        #endregion

        [Test]
        public void Constructor_should_not_throw_if_binaryReader_and_binaryWriter_are_both_provided()
        {
            using (var stream = new MemoryStream())
            using (var binaryReader = new BsonBinaryReader(stream))
            using (var binaryWriter = new BsonBinaryWriter(stream))
            {
                Action action = () => new UpdateMessageBinaryEncoder(binaryReader, binaryWriter);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_should_not_throw_if_only_binaryReader_is_provided()
        {
            using (var stream = new MemoryStream())
            using (var binaryReader = new BsonBinaryReader(stream))
            {
                Action action = () => new UpdateMessageBinaryEncoder(binaryReader, null);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_should_not_throw_if_only_binaryWriter_is_provided()
        {
            using (var stream = new MemoryStream())
            using (var binaryWriter = new BsonBinaryWriter(stream))
            {
                Action action = () => new UpdateMessageBinaryEncoder(null, binaryWriter);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_should_throw_if_binaryReader_and_binaryWriter_are_both_null()
        {
            Action action = () => new UpdateMessageBinaryEncoder(null, null);
            action.ShouldThrow<ArgumentException>();
        }

        [TestCase(0, false, false)]
        [TestCase(1, true, false)]
        [TestCase(2, false, true)]
        public void ReadMessage_should_decode_flags_correctly(int flags, bool isUpsert, bool isMulti)
        {
            var bytes = (byte[])__testMessageBytes.Clone();
            bytes[__flagsOffset] = (byte)flags;

            using (var stream = new MemoryStream(bytes))
            using (var binaryReader = new BsonBinaryReader(stream))
            {
                var subject = new UpdateMessageBinaryEncoder(binaryReader, null);
                var message = subject.ReadMessage();
                message.IsMulti.Should().Be(isMulti);
                message.IsUpsert.Should().Be(isUpsert);
            }
        }

        [Test]
        public void ReadMessage_should_read_a_message()
        {
            using (var stream = new MemoryStream(__testMessageBytes))
            using (var binaryReader = new BsonBinaryReader(stream))
            {
                var subject = new UpdateMessageBinaryEncoder(binaryReader, null);
                var message = subject.ReadMessage();
                message.DatabaseName.Should().Be(__databaseName);
                message.CollectionName.Should().Be(__collectionName);
                message.IsMulti.Should().Be(__isMulti);
                message.IsUpsert.Should().Be(__isUpsert);
                message.Query.Should().Be(__query);
                message.RequestId.Should().Be(__requestId);
                message.Update.Should().Be(__update);
            }
        }

        [Test]
        public void ReadMessage_should_throw_if_binaryReader_was_not_provided()
        {
            using (var stream = new MemoryStream())
            using (var binaryWriter = new BsonBinaryWriter(stream))
            {
                var subject = new UpdateMessageBinaryEncoder(null, binaryWriter);
                Action action = () => subject.ReadMessage();
                action.ShouldThrow<InvalidOperationException>();
            }
        }

        [TestCase(0, false, false)]
        [TestCase(1, true, false)]
        [TestCase(2, false, true)]
        public void WriteMessage_should_encode_flags_correctly(int flags, bool isUpsert, bool isMulti)
        {
            var message = new UpdateMessage(__requestId, __databaseName, __collectionName, __query, __update, isMulti, isUpsert);

            using (var stream = new MemoryStream())
            using (var binaryWriter = new BsonBinaryWriter(stream))
            {
                var subject = new UpdateMessageBinaryEncoder(null, binaryWriter);
                subject.WriteMessage(message);
                var bytes = stream.ToArray();
                bytes[__flagsOffset].Should().Be((byte)flags);
            }
        }

        [Test]
        public void WriteMessage_should_throw_if_binaryWriter_was_not_provided()
        {
            using (var stream = new MemoryStream())
            using (var binaryReader = new BsonBinaryReader(stream))
            {
                var subject = new UpdateMessageBinaryEncoder(binaryReader, null);
                Action action = () => subject.WriteMessage(__testMessage);
                action.ShouldThrow<InvalidOperationException>();
            }
        }

        [Test]
        public void WriteMessage_should_throw_if_message_is_null()
        {
            using (var stream = new MemoryStream())
            using (var binaryWriter = new BsonBinaryWriter(stream))
            {
                var subject = new UpdateMessageBinaryEncoder(null, binaryWriter);
                Action action = () => subject.WriteMessage(null);
                action.ShouldThrow<ArgumentNullException>();
            }
        }

        [Test]
        public void WriteMessage_should_write_a_message()
        {
            using (var stream = new MemoryStream())
            using (var binaryWriter = new BsonBinaryWriter(stream))
            {
                var subject = new UpdateMessageBinaryEncoder(null, binaryWriter);
                subject.WriteMessage(__testMessage);
                var bytes = stream.ToArray();
                bytes.Should().Equal(__testMessageBytes);
            }
        }
    }
}
