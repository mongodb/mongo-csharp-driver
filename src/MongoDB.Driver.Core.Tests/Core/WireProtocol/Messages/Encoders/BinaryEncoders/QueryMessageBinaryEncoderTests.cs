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

namespace MongoDB.Driver.Core.Tests.WireProtocol.Messages.Encoders.BinaryEncoders
{
    [TestFixture]
    public class QueryMessageBinaryEncoderTests
    {
        #region static
        // static fields
        private static readonly bool __awaitData = true;
        private static readonly int __batchSize = 3;
        private static readonly string __collectionName = "c";
        private static readonly string __databaseName = "d";
        private static readonly BsonDocument __fields = new BsonDocument("f", 1);
        private static readonly int __flagsOffset;
        private static readonly bool __noCursorTimeout = true;
        private static readonly bool __partialOk = true;
        private static readonly BsonDocument __query = new BsonDocument("x", 1);
        private static readonly int __requestId = 1;
        private static readonly int __skip = 2;
        private static readonly bool __slaveOk = true;
        private static readonly bool __tailableCursor = true;
        private static readonly QueryMessage __testMessage;
        private static readonly byte[] __testMessageBytes;

        // static constructor
        static QueryMessageBinaryEncoderTests()
        {
            __testMessage = new QueryMessage(__requestId, __databaseName, __collectionName, __query, __fields, __skip, __batchSize, __slaveOk, __partialOk, __noCursorTimeout, __tailableCursor, __awaitData);

            __testMessageBytes = new byte[]
            {
                0, 0, 0, 0, // messageLength
                1, 0, 0, 0, // requestId
                0, 0, 0, 0, // responseTo
                212, 7, 0, 0, // opcode = 2004
                182, 0, 0, 0, // flags
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

        [Test]
        public void Constructor_should_not_throw_if_binaryReader_and_binaryWriter_are_both_provided()
        {
            using (var stream = new MemoryStream())
            using (var binaryReader = new BsonBinaryReader(stream))
            using (var binaryWriter = new BsonBinaryWriter(stream))
            {
                Action action = () => new QueryMessageBinaryEncoder(binaryReader, binaryWriter);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_should_not_throw_if_only_binaryReader_is_provided()
        {
            using (var stream = new MemoryStream())
            using (var binaryReader = new BsonBinaryReader(stream))
            {
                Action action = () => new QueryMessageBinaryEncoder(binaryReader, null);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_should_not_throw_if_only_binaryWriter_is_provided()
        {
            using (var stream = new MemoryStream())
            using (var binaryWriter = new BsonBinaryWriter(stream))
            {
                Action action = () => new QueryMessageBinaryEncoder(null, binaryWriter);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_should_throw_if_binaryReader_and_binaryWriter_are_both_null()
        {
            Action action = () => new QueryMessageBinaryEncoder(null, null);
            action.ShouldThrow<ArgumentException>();
        }

        [TestCase(0, false, false, false, false, false)]
        [TestCase(2, true, false, false, false, false)]
        [TestCase(4, false, true, false, false, false)]
        [TestCase(16, false, false, true, false, false)]
        [TestCase(32, false, false, false, true, false)]
        [TestCase(128, false, false, false, false, true)]
        public void ReadMessage_should_decode_flags_correctly(int flags, bool tailableCursor, bool slaveOk, bool noCursorTimeout, bool awaitData, bool partialOk)
        {
            var bytes = (byte[])__testMessageBytes.Clone();
            bytes[__flagsOffset] = (byte)flags;

            using (var stream = new MemoryStream(bytes))
            using (var binaryReader = new BsonBinaryReader(stream))
            {
                var subject = new QueryMessageBinaryEncoder(binaryReader, null);
                var message = subject.ReadMessage();
                message.TailableCursor.Should().Be(tailableCursor);
                message.SlaveOk.Should().Be(slaveOk);
                message.NoCursorTimeout.Should().Be(noCursorTimeout);
                message.AwaitData.Should().Be(awaitData);
                message.PartialOk.Should().Be(partialOk);
            }
        }

        [Test]
        public void ReadMessage_should_read_a_message()
        {
            using (var stream = new MemoryStream(__testMessageBytes))
            using (var binaryReader = new BsonBinaryReader(stream))
            {
                var subject = new QueryMessageBinaryEncoder(binaryReader, null);
                var message = subject.ReadMessage();
                message.DatabaseName.Should().Be(__databaseName);
                message.CollectionName.Should().Be(__collectionName);
                message.AwaitData.Should().Be(__awaitData);
                message.BatchSize.Should().Be(__batchSize);
                message.Fields.Should().Be(__fields);
                message.NoCursorTimeout.Should().Be(__noCursorTimeout);
                message.PartialOk.Should().Be(__partialOk);
                message.Query.Should().Be(__query);
                message.RequestId.Should().Be(__requestId);
                message.Skip.Should().Be(__skip);
                message.SlaveOk.Should().Be(__slaveOk);
                message.TailableCursor.Should().Be(__tailableCursor);
            }
        }

        [Test]
        public void ReadMessage_should_throw_if_binaryReader_was_not_provided()
        {
            using (var stream = new MemoryStream())
            using (var binaryWriter = new BsonBinaryWriter(stream))
            {
                var subject = new QueryMessageBinaryEncoder(null, binaryWriter);
                Action action = () => subject.ReadMessage();
                action.ShouldThrow<InvalidOperationException>();
            }
        }

        [TestCase(0, false, false, false, false, false)]
        [TestCase(2, true, false, false, false, false)]
        [TestCase(4, false, true, false, false, false)]
        [TestCase(16, false, false, true, false, false)]
        [TestCase(32, false, false, false, true, false)]
        [TestCase(128, false, false, false, false, true)]
        public void WriteMessage_should_encode_flags_correctly(int flags, bool tailableCursor, bool slaveOk, bool noCursorTimeout, bool awaitData, bool partialOk)
        {
            var message = new QueryMessage(__requestId, __databaseName, __collectionName, __query, __fields, __skip, __batchSize, slaveOk, partialOk, noCursorTimeout, tailableCursor, awaitData);

            using (var stream = new MemoryStream())
            using (var binaryWriter = new BsonBinaryWriter(stream))
            {
                var subject = new QueryMessageBinaryEncoder(null, binaryWriter);
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
                var subject = new QueryMessageBinaryEncoder(binaryReader, null);
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
                var subject = new QueryMessageBinaryEncoder(null, binaryWriter);
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
                var subject = new QueryMessageBinaryEncoder(null, binaryWriter);
                subject.WriteMessage(__testMessage);
                var bytes = stream.ToArray();
                bytes.Should().Equal(__testMessageBytes);
            }
        }
    }
}
