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
    public class DeleteMessageBinaryEncoderTests
    {
        #region static
        // static fields
        private static DeleteMessage __testMessage;
        private static readonly byte[] __testMessageBytes;

        // static constructor
        static DeleteMessageBinaryEncoderTests()
        {
            var query = new BsonDocument("x", 1);
            __testMessage = new DeleteMessage(1, "d", "c", query, false);

            __testMessageBytes = new byte[]
            {
                40, 0, 0, 0, // messageLength
                1, 0, 0, 0, // requestId
                0, 0, 0, 0, // responseTo
                214, 7, 0, 0, // opcode = 2006
                0, 0, 0, 0, // reserved
                (byte)'d', (byte)'.', (byte)'c', 0, // fullCollectionName
                1, 0, 0, 0, // flags
                12, 0, 0, 0, 0x10, (byte)'x', 0, 1, 0, 0, 0, 0 // query
            };
        }
        #endregion

        [Test]
        public void Constructor_should_not_throw_if_both_arguments_are_provided()
        { 
            using (var stream = new MemoryStream())
            using (var binaryReader = new BsonBinaryReader(stream))
            using (var binaryWriter = new BsonBinaryWriter(stream))
            {
                Action action = () => new DeleteMessageBinaryEncoder(binaryReader, binaryWriter);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_should_not_throw_if_only_binaryReader_is_provided()
        {
            using (var stream = new MemoryStream())
            using (var binaryReader = new BsonBinaryReader(stream))
            {
                Action action = () => new DeleteMessageBinaryEncoder(binaryReader, null);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_should_not_throw_if_only_binaryWriter_is_provided()
        {
            using (var stream = new MemoryStream())
            using (var binaryWriter = new BsonBinaryWriter(stream))
            {
                Action action = () => new DeleteMessageBinaryEncoder(null, binaryWriter);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_should_throw_if_both_arguments_are_null()
        {
            Action action = () => new DeleteMessageBinaryEncoder(null, null);
            action.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void ReadMessage_should_read_a_properly_formatted_binary_message()
        {
            using (var stream = new MemoryStream(__testMessageBytes))
            using (var binaryReader = new BsonBinaryReader(stream))
            {
                var encoder = new DeleteMessageBinaryEncoder(binaryReader, null);
                var message = encoder.ReadMessage();
                message.RequestId.Should().Be(__testMessage.RequestId);
                message.DatabaseName.Should().Be(__testMessage.DatabaseName);
                message.CollectionName.Should().Be(__testMessage.CollectionName);
                message.IsMulti.Should().Be(__testMessage.IsMulti);
                message.Query.Should().Be(__testMessage.Query);
            }
        }

        [Test]
        public void ReadMessage_should_throw_if_binaryReader_was_not_provided()
        {
            using (var stream = new MemoryStream())
            using (var binaryWriter = new BsonBinaryWriter(stream))
            {
                var encoder = new DeleteMessageBinaryEncoder(null, binaryWriter);
                Action action = () => encoder.ReadMessage();
                action.ShouldThrow<InvalidOperationException>();
            }
        }

        [Test]
        public void WriteMessage_should_throw_if_binaryWriter_was_not_provided()
        {
            using (var stream = new MemoryStream())
            using (var binaryReader = new BsonBinaryReader(stream))
            {
                var encoder = new DeleteMessageBinaryEncoder(binaryReader, null);
                var message = new DeleteMessage(1, "database", "collection", new BsonDocument(), false);
                Action action = () => encoder.WriteMessage(message);
                action.ShouldThrow<InvalidOperationException>();
            }
        }

        [Test]
        public void WriteMessage_should_write_a_properly_formatted_binary_message()
        {
            byte[] bytes;
            using (var stream = new MemoryStream())
            using (var binaryWriter = new BsonBinaryWriter(stream))
            {
                var encoder = new DeleteMessageBinaryEncoder(null, binaryWriter);
                encoder.WriteMessage(__testMessage);
                bytes = stream.ToArray();
            }

            bytes.Should().Equal(__testMessageBytes);
        }

        [Test]
        public void WriteMessage_should_throw_if_message_is_null()
        {
            using (var stream = new MemoryStream())
            using (var binaryReader = new BsonBinaryReader(stream))
            {
                var encoder = new DeleteMessageBinaryEncoder(binaryReader, null);
                Action action = () => encoder.WriteMessage(null);
                action.ShouldThrow<ArgumentNullException>();
            }
        }
    }
}
