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

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Compression
{
    public class CompressedMessageBinaryEncoderTests
    {
        private readonly MessageEncoderSettings _messageEncoderSettings = new MessageEncoderSettings();

        [Fact]
        public void Constructor_should_throw_the_exception_if_the_compressors_are_null()
        {
            var exception = Record.Exception(() => CreateSubject(Mock.Of<Stream>(), compressors: null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("compressorSource");
        }

        [Fact]
        public void Constructor_should_throw_the_exception_if_the_stream_is_null()
        {
            var exception = Record.Exception(() => CreateSubject(null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("stream");
        }

        [Theory]
        [InlineData(CompressorType.Noop, null)]
        [InlineData(CompressorType.Zlib, -1)]
        [InlineData(CompressorType.Zlib, 0)]
        [InlineData(CompressorType.Zlib, 6)]
        [InlineData(CompressorType.Snappy, null)]
        [InlineData(CompressorType.ZStandard, null)]
        public void Encoder_should_read_the_previously_written_message(
            CompressorType compressorType,
            object compressionOption)
        {
            var message = CreateMessage(1, 2);

            var compressedMessage = GetCompressedMessage(message, compressorType);
            using (compressedMessage.OriginalMessageStream)
            {
                CompressorConfiguration compressionProperty = new CompressorConfiguration(compressorType);
                if (compressionOption != null)
                {
                    compressionProperty.Properties.Add("Level", compressionOption);
                }

                using (var memoryStream = new MemoryStream())
                {
                    var subject = CreateSubject(memoryStream, compressionProperty);

                    subject.WriteMessage(compressedMessage);
                    memoryStream.Position = 0;
                    var result = subject.ReadMessage();

                    result.ShouldBeEquivalentTo(
                        compressedMessage,
                        options =>
                            options
                                .Excluding(p => p.OriginalMessageStream));
                }
            }
        }

        // private methods
        private CommandResponseMessage CreateMessage(
            int requestId = 0,
            int responseTo = 0,
            IEnumerable<CommandMessageSection> sections = null,
            bool moreToCome = false)
        {
            sections = sections ?? new[] { CreateType0Section() };
            return new CommandResponseMessage(new CommandMessage(requestId, responseTo, sections, moreToCome));
        }

        private CompressedMessageBinaryEncoder CreateSubject(Stream stream, params CompressorConfiguration[] compressors)
        {
            var compressorSource = GetCompressorSource(compressors);
            var encoderSelector = new CommandResponseMessageEncoderSelector();
            var subject = new CompressedMessageBinaryEncoder(stream, encoderSelector, compressorSource, _messageEncoderSettings);
            return subject;
        }

        private Type0CommandMessageSection<RawBsonDocument> CreateType0Section(BsonDocument document = null)
        {
            document = document ?? new BsonDocument("t", 0);
            var rawBson = new RawBsonDocument(document.ToBson());
            return new Type0CommandMessageSection<RawBsonDocument>(rawBson, RawBsonDocumentSerializer.Instance);
        }

        private CompressedMessage GetCompressedMessage(MongoDBMessage message, CompressorType? compressorType = null)
        {
            if (!compressorType.HasValue)
            {
                compressorType = CompressorType.Zlib;
            }

            using (var memoryStream = new MemoryStream())
            {
                var encoderFactory = new BinaryMessageEncoderFactory(memoryStream, _messageEncoderSettings);
                message.GetEncoder(encoderFactory).WriteMessage(message);
                var byteBuffer = new ByteArrayBuffer(memoryStream.ToArray());
                var stream = new ByteBufferStream(byteBuffer);
                return new CompressedMessage(
                    message,
                    stream,
                    compressorType.Value);
            }
        }

        private ICompressorSource GetCompressorSource(params CompressorConfiguration[] compressors)
        {
            if (compressors == null)
            {
                return null;
            }

            if (compressors.Length == 0)
            {
                compressors = new[] { new CompressorConfiguration(CompressorType.Zlib), new CompressorConfiguration(CompressorType.Snappy) };
            }
            return new CompressorSource(compressors);
        }
    }
}