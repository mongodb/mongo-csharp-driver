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
using System.IO;
using System.Text;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Compression;
using Moq;
using SharpCompress.IO;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.Compression
{
    public class CompressorsTests
    {
        private static string __testMessage = "abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz";

        [Theory]
        [InlineData(CompressorType.Snappy)]
        public void Compressor_should_read_the_previously_written_message_or_throw_the_exception_if_the_current_platform_is_not_supported(CompressorType compressorType)
        {
            var bytes = Encoding.ASCII.GetBytes(__testMessage);
            var compressor = GetCompressor(compressorType);
#if NET452 || NETSTANDARD2_0
            Assert(
                bytes,
                (input, output) =>
                {
                    compressor.Compress(input, output);
                    input.Length.Should().BeGreaterThan(output.Length);
                    input.Position = 0;
                    input.SetLength(0);
                    output.Position = 0;
                    compressor.Decompress(output, input);
                },
                (input, output) =>
                {
                    input.Position = 0;
                    var result = Encoding.ASCII.GetString(input.ReadBytes((int)input.Length));
                    result.Should().Be(__testMessage);
                });
#else
            var exception = Record.Exception(() => { compressor.Compress(Mock.Of<Stream>(), Mock.Of<Stream>()); });

            exception.Should().BeOfType<NotSupportedException>();

            exception = Record.Exception(() => { compressor.Decompress(Mock.Of<Stream>(), Mock.Of<Stream>()); });

            exception.Should().BeOfType<NotSupportedException>();
#endif
        }

        [Fact]
        public void Zlib_should_generate_expected_compressed_bytes()
        {
            var bytes = Encoding.ASCII.GetBytes(__testMessage);
            Assert(
                bytes,
                (input, output) =>
                {
                    var compressor = GetCompressor(CompressorType.Zlib, 6);
                    compressor.Compress(input, output);
                },
                (input, output) =>
                {
                    var resultBytes = output.ToArray();
                    var result = string.Join(",", resultBytes);
                    result
                        .Should()
                        .Be("120,156,74,76,74,78,73,77,75,207,200,204,202,206,201,205,203,47,40,44,42,46,41,45,43,175,168,172,74,36,67,6,0,0,0,255,255,3,0,21,79,33,94");
                });
        }

        [Theory]
        [InlineData(CompressorType.Zlib, -1)]
        [InlineData(CompressorType.Zlib, 0)]
        [InlineData(CompressorType.Zlib, 1)]
        [InlineData(CompressorType.Zlib, 2)]
        [InlineData(CompressorType.Zlib, 3)]
        [InlineData(CompressorType.Zlib, 4)]
        [InlineData(CompressorType.Zlib, 5)]
        [InlineData(CompressorType.Zlib, 6)]
        [InlineData(CompressorType.Zlib, 7)]
        [InlineData(CompressorType.Zlib, 8)]
        [InlineData(CompressorType.Zlib, 9)]
        public void Zlib_should_read_the_previously_written_message(CompressorType compressorType, int compressionOption)
        {
            var bytes = Encoding.ASCII.GetBytes(__testMessage);
            int zlibHeaderSize = 21;

            Assert(
                bytes,
                (input, output) =>
                {
                    var compressor = GetCompressor(compressorType, compressionOption);
                    compressor.Compress(input, output);
                    if (compressionOption != 0)
                    {
                        input.Length.Should().BeGreaterThan(output.Length);
                    }
                    else
                    {
                        output.Length.Should().Be(input.Length + zlibHeaderSize);
                    }
                    input.Position = 0;
                    input.SetLength(0);
                    output.Position = 0;
                    compressor.Decompress(output, input);
                },
                (input, output) =>
                {
                    input.Position = 0;
                    var result = Encoding.ASCII.GetString(input.ReadBytes((int)input.Length));
                    result.Should().Be(__testMessage);
                });
        }

        [Theory]
        [ParameterAttributeData]
        public void Zlib_should_throw_exception_if_the_level_is_out_of_range([Values(-2, 10)] int compressionOption)
        {
            var exception = Record.Exception(() => GetCompressor(CompressorType.Zlib, compressionOption));

            var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            e.ParamName.Should().Be("compressionLevel");
        }

        private void Assert(byte[] bytes, Action<ByteBufferStream, MemoryStream> test, Action<ByteBufferStream, MemoryStream> assertResult = null)
        {
            using (var buffer = new ByteArrayBuffer(bytes))
            {
                var memoryStream = new MemoryStream();
                var byteBufferStream = new ByteBufferStream(buffer);
                using (new NonDisposingStream(memoryStream))
                using (new NonDisposingStream(byteBufferStream))
                {
                    test(byteBufferStream, memoryStream);
                    assertResult?.Invoke(byteBufferStream, memoryStream);
                }
            }
        }

        private ICompressor GetCompressor(CompressorType compressorType, object option = null)
        {
            switch (compressorType)
            {
                case CompressorType.Snappy:
                    return new SnappyCompressor();
                case CompressorType.Zlib:
                    return new ZlibCompressor((int)option);
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
