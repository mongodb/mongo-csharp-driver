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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Tests.WireProtocol.Messages.Encoders.BinaryEncoders
{
    [TestFixture]
    public class BinaryMessageEncoderFactoryTests
    {
        [Test]
        public void Constructor_with_binaryReader_parameter_should_not_throw_if_binaryReader_is_not_null()
        {
            using (var stream = new MemoryStream())
            using (var binaryReader = new BsonBinaryReader(stream))
            {
                Action action = () => new BinaryMessageEncoderFactory(binaryReader);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_with_binaryReader_parameter_should_throw_if_binaryReader_is_null()
        {
            BsonBinaryReader binaryReader = null;
            Action action = () => new BinaryMessageEncoderFactory(binaryReader);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_with_binaryWriter_parameter_should_not_throw_if_binaryWriter_is_not_null()
        {
            using (var stream = new MemoryStream())
            using (var binaryWriter = new BsonBinaryWriter(stream))
            {
                Action action = () => new BinaryMessageEncoderFactory(binaryWriter);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_with_binaryWriter_parameter_should_throw_if_binaryWriter_is_null()
        {
            BsonBinaryWriter binaryWriter = null;
            Action action = () => new BinaryMessageEncoderFactory(binaryWriter);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_with_two_parameters_should_not_throw_if_only_binaryReader_is_provided()
        {
            using (var stream = new MemoryStream())
            using (var binaryReader = new BsonBinaryReader(stream))
            {
                Action action = () => new BinaryMessageEncoderFactory(binaryReader, null);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_with_two_parameters_should_not_throw_if_only_binaryWriter_is_provided()
        {
            using (var stream = new MemoryStream())
            using (var binaryWriter = new BsonBinaryWriter(stream))
            {
                Action action = () => new BinaryMessageEncoderFactory(null, binaryWriter);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_with_two_parameters_should_throw_if_both_values_are_null()
        {
            Action action = () => new BinaryMessageEncoderFactory(null, null);
            action.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void GetDeleteMessageEncoder_should_return_a_DeleteMessageBinaryEncoder()
        {
            using (var stream = new MemoryStream())
            using (var binaryWriter = new BsonBinaryWriter(stream))
            {
                var encoderFactory = new BinaryMessageEncoderFactory(null, binaryWriter);
                var encoder = encoderFactory.GetDeleteMessageEncoder();
                encoder.Should().BeOfType<DeleteMessageBinaryEncoder>();
            }
        }

        [Test]
        public void GetGetMoreMessageEncoder_should_return_a_GetMoreMessageBinaryEncoder()
        {
            using (var stream = new MemoryStream())
            using (var binaryWriter = new BsonBinaryWriter(stream))
            {
                var encoderFactory = new BinaryMessageEncoderFactory(null, binaryWriter);
                var encoder = encoderFactory.GetGetMoreMessageEncoder();
                encoder.Should().BeOfType<GetMoreMessageBinaryEncoder>();
            }
        }

        [Test]
        public void GetInsertMessageEncoder_should_return_a_InsertMessageBinaryEncoder()
        {
            using (var stream = new MemoryStream())
            using (var binaryWriter = new BsonBinaryWriter(stream))
            {
                var encoderFactory = new BinaryMessageEncoderFactory(null, binaryWriter);
                var encoder = encoderFactory.GetInsertMessageEncoder<BsonDocument>(BsonDocumentSerializer.Instance);
                encoder.Should().BeOfType<InsertMessageBinaryEncoder<BsonDocument>>();
            }
        }

        [Test]
        public void GetKillCursorsMessageEncoder_should_return_a_KillCursorsMessageBinaryEncoder()
        {
            using (var stream = new MemoryStream())
            using (var binaryWriter = new BsonBinaryWriter(stream))
            {
                var encoderFactory = new BinaryMessageEncoderFactory(null, binaryWriter);
                var encoder = encoderFactory.GetKillCursorsMessageEncoder();
                encoder.Should().BeOfType<KillCursorsMessageBinaryEncoder>();
            }
        }

        [Test]
        public void GetQueryMessageEncoder_should_return_a_QueryMessageBinaryEncoder()
        {
            using (var stream = new MemoryStream())
            using (var binaryWriter = new BsonBinaryWriter(stream))
            {
                var encoderFactory = new BinaryMessageEncoderFactory(null, binaryWriter);
                var encoder = encoderFactory.GetQueryMessageEncoder();
                encoder.Should().BeOfType<QueryMessageBinaryEncoder>();
            }
        }

        [Test]
        public void GetReplyMessageEncoder_should_return_a_ReplyMessageBinaryEncoder()
        {
            using (var stream = new MemoryStream())
            using (var binaryWriter = new BsonBinaryWriter(stream))
            {
                var encoderFactory = new BinaryMessageEncoderFactory(null, binaryWriter);
                var encoder = encoderFactory.GetReplyMessageEncoder<BsonDocument>(BsonDocumentSerializer.Instance);
                encoder.Should().BeOfType<ReplyMessageBinaryEncoder<BsonDocument>>();
            }
        }

        [Test]
        public void GetUpdateMessageEncoder_should_return_a_UpdateMessageBinaryEncoder()
        {
            using (var stream = new MemoryStream())
            using (var binaryWriter = new BsonBinaryWriter(stream))
            {
                var encoderFactory = new BinaryMessageEncoderFactory(null, binaryWriter);
                var encoder = encoderFactory.GetUpdateMessageEncoder();
                encoder.Should().BeOfType<UpdateMessageBinaryEncoder>();
            }
        }
    }
}
