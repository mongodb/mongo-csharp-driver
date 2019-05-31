/* Copyright 2019-present MongoDB Inc.
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

using System.IO;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.WireProtocol.Messages
{
    public class CompressedMessageTests
    {
        [Fact]
        public void Constructor_should_initialize_instance()
        {
            var compressorType = CompressorType.Zlib;
            var commandMessage = GetCommandMessage();

            var result = new CompressedMessage(commandMessage, Mock.Of<BsonStream>(), compressorType);

            result.CompressorType.Should().Be(compressorType);
            result.OriginalMessage.ShouldBeEquivalentTo(commandMessage);
            result.MessageType.Should().Be(MongoDBMessageType.Compressed);
        }

        [Fact]
        public void GetEncoder_should_return_a_CompressedMessageEncoder()
        {
            var subject = CreateSubject();

            var stream = new MemoryStream();
            var encoderSettings = new MessageEncoderSettings();
            var encoderFactory = new BinaryMessageEncoderFactory(stream, encoderSettings, Mock.Of<ICompressorSource>());

            var result = subject.GetEncoder(encoderFactory);

            result.Should().BeOfType<CompressedMessageBinaryEncoder>();
        }

        private CompressedMessage CreateSubject()
        {
            var message = GetCommandMessage();
            var compressorType = CompressorType.Zlib;

            return new CompressedMessage(message, Mock.Of<BsonStream>(), compressorType);
        }

        private CommandMessage GetCommandMessage()
        {
            return new CommandMessage(
                1,
                1,
                new CommandMessageSection[1]
                {
                    new Type0CommandMessageSection<BsonDocument>(new BsonDocument(), BsonDocumentSerializer.Instance )
                },
                false);
        }
    }
}
