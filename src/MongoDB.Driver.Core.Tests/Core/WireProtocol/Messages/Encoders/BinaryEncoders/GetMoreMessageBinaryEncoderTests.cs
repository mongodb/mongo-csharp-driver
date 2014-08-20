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
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Tests.WireProtocol.Messages.Encoders.BinaryEncoders
{
    [TestFixture]
    public class GetMoreMessageBinaryEncoderTests
    {
        #region static
        // static fields
        private static readonly int __batchSize = 3;
        private static readonly string __collectionName = "c";
        private static readonly long __cursorId = 2;
        private static readonly string __databaseName = "d";
        private static readonly int __requestId = 1;
        private static readonly GetMoreMessage __testMessage;
        private static readonly byte[] __testMessageBytes;

        // static constructor
        static GetMoreMessageBinaryEncoderTests()
        {
            __testMessage = new GetMoreMessage(__requestId, __databaseName, __collectionName, __cursorId, __batchSize);

            __testMessageBytes = new byte[]
            {
                0, 0, 0, 0, // messageLength
                1, 0, 0, 0, // requestId
                0, 0, 0, 0, // responseTo
                213, 7, 0, 0, // opcode = 2004
                0, 0, 0, 0, // reserved
                (byte)'d', (byte)'.', (byte)'c', 0, // fullCollectionName
                3, 0, 0, 0, // batchSize
                2, 0, 0, 0, 0, 0, 0, 0 // cursorId
            };
            __testMessageBytes[0] = (byte)__testMessageBytes.Length;
        }
        #endregion

        [Test]
        public void Constructor_should_not_throw_if_binaryReader_and_binaryWriter_are_both_provided()
        {
            using (var stream = new MemoryStream())
            using (var binaryReader = new BsonBinaryReader(stream))
            using (var binaryWriter = new BsonBinaryWriter(stream))
            {
                Action action = () => new GetMoreMessageBinaryEncoder(binaryReader, binaryWriter);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_should_not_throw_if_only_binaryReader_is_provided()
        {
            using (var stream = new MemoryStream())
            using (var binaryReader = new BsonBinaryReader(stream))
            {
                Action action = () => new GetMoreMessageBinaryEncoder(binaryReader, null);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_should_not_throw_if_only_binaryWriter_is_provided()
        {
            using (var stream = new MemoryStream())
            using (var binaryWriter = new BsonBinaryWriter(stream))
            {
                Action action = () => new GetMoreMessageBinaryEncoder(null, binaryWriter);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_should_throw_if_binaryReader_and_binaryWriter_are_both_null()
        {
            Action action = () => new GetMoreMessageBinaryEncoder(null, null);
            action.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void ReadMessage_should_read_a_message()
        {
            using (var stream = new MemoryStream(__testMessageBytes))
            using (var binaryReader = new BsonBinaryReader(stream))
            {
                var subject = new GetMoreMessageBinaryEncoder(binaryReader, null);
                var message = subject.ReadMessage();
                message.BatchSize.Should().Be(__batchSize);
                message.CollectionName.Should().Be(__collectionName);
                message.CursorId.Should().Be(__cursorId);
                message.DatabaseName.Should().Be(__databaseName);
                message.RequestId.Should().Be(__requestId);
            }
        }

        [Test]
        public void ReadMessage_should_throw_if_binaryReader_was_not_provided()
        {
            using (var stream = new MemoryStream())
            using (var binaryWriter = new BsonBinaryWriter(stream))
            {
                var subject = new GetMoreMessageBinaryEncoder(null, binaryWriter);
                Action action = () => subject.ReadMessage();
                action.ShouldThrow<InvalidOperationException>();
            }
        }

        [Test]
        public void WriteMessage_should_throw_if_binaryWriter_was_not_provided()
        {
            using (var stream = new MemoryStream())
            using (var binaryReader = new BsonBinaryReader(stream))
            {
                var subject = new GetMoreMessageBinaryEncoder(binaryReader, null);
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
                var subject = new GetMoreMessageBinaryEncoder(null, binaryWriter);
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
                var subject = new GetMoreMessageBinaryEncoder(null, binaryWriter);
                subject.WriteMessage(__testMessage);
                var bytes = stream.ToArray();
                bytes.Should().Equal(__testMessageBytes);
            }
        }
    }
}
