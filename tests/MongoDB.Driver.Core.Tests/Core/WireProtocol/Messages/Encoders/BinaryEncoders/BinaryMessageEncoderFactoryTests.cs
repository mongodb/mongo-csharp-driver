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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;
using Xunit;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders
{
    public class BinaryMessageEncoderFactoryTests
    {
        [Fact]
        public void Constructor_should_not_throw_if_stream_is_not_null()
        {
            using (var stream = new MemoryStream())
            {
                Action action = () => new BinaryMessageEncoderFactory(stream, null);
                action.ShouldNotThrow();
            }
        }

        [Fact]
        public void Constructor_should_throw_if_stream_is_null()
        {
            Action action = () => new BinaryMessageEncoderFactory(null, null);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void GetDeleteMessageEncoder_should_return_a_DeleteMessageBinaryEncoder()
        {
            using (var stream = new MemoryStream())
            {
                var encoderFactory = new BinaryMessageEncoderFactory(stream, null);
                var encoder = encoderFactory.GetDeleteMessageEncoder();
                encoder.Should().BeOfType<DeleteMessageBinaryEncoder>();
            }
        }

        [Fact]
        public void GetGetMoreMessageEncoder_should_return_a_GetMoreMessageBinaryEncoder()
        {
            using (var stream = new MemoryStream())
            {
                var encoderFactory = new BinaryMessageEncoderFactory(stream, null);
                var encoder = encoderFactory.GetGetMoreMessageEncoder();
                encoder.Should().BeOfType<GetMoreMessageBinaryEncoder>();
            }
        }

        [Fact]
        public void GetInsertMessageEncoder_should_return_a_InsertMessageBinaryEncoder()
        {
            using (var stream = new MemoryStream())
            {
                var encoderFactory = new BinaryMessageEncoderFactory(stream, null);
                var encoder = encoderFactory.GetInsertMessageEncoder<BsonDocument>(BsonDocumentSerializer.Instance);
                encoder.Should().BeOfType<InsertMessageBinaryEncoder<BsonDocument>>();
            }
        }

        [Fact]
        public void GetKillCursorsMessageEncoder_should_return_a_KillCursorsMessageBinaryEncoder()
        {
            using (var stream = new MemoryStream())
            {
                var encoderFactory = new BinaryMessageEncoderFactory(stream, null);
                var encoder = encoderFactory.GetKillCursorsMessageEncoder();
                encoder.Should().BeOfType<KillCursorsMessageBinaryEncoder>();
            }
        }

        [Fact]
        public void GetQueryMessageEncoder_should_return_a_QueryMessageBinaryEncoder()
        {
            using (var stream = new MemoryStream())
            {
                var encoderFactory = new BinaryMessageEncoderFactory(stream, null);
                var encoder = encoderFactory.GetQueryMessageEncoder();
                encoder.Should().BeOfType<QueryMessageBinaryEncoder>();
            }
        }

        [Fact]
        public void GetReplyMessageEncoder_should_return_a_ReplyMessageBinaryEncoder()
        {
            using (var stream = new MemoryStream())
            {
                var encoderFactory = new BinaryMessageEncoderFactory(stream, null);
                var encoder = encoderFactory.GetReplyMessageEncoder<BsonDocument>(BsonDocumentSerializer.Instance);
                encoder.Should().BeOfType<ReplyMessageBinaryEncoder<BsonDocument>>();
            }
        }

        [Fact]
        public void GetUpdateMessageEncoder_should_return_a_UpdateMessageBinaryEncoder()
        {
            using (var stream = new MemoryStream())
            {
                var encoderFactory = new BinaryMessageEncoderFactory(stream, null);
                var encoder = encoderFactory.GetUpdateMessageEncoder();
                encoder.Should().BeOfType<UpdateMessageBinaryEncoder>();
            }
        }
    }
}
