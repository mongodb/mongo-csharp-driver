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
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Compression;
using SharpCompress.IO;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.Compression
{
    public class CompressorsTests
    {
        #region static
        // private constants
        private const string __testMessage = "abcdefghijklmnopqrstuvwxyz0123456789 abcdefghijklmnopqrstuvwxyz0123456789 abcdefghijklmnopqrstuvwxyz0123456789";
        private const string __testMessagePortion = @"Two households, both alike in dignity,
        In fair Verona, where we lay our scene,
        From ancient grudge break to new mutiny,
        Where civil blood makes civil hands unclean.
            From forth the fatal loins of these two foes
            A pair of star-cross'd lovers take their life;
        Whose misadventured piteous overthrows
        Do with their death bury their parents' strife.
            The fearful passage of their death-mark'd love,
        And the continuance of their parents' rage,
        Which, but their children's end, nought could remove,
        Is now the two hours' traffic of our stage;
        The which if you with patient ears attend,
        What here shall miss, our toil shall strive to mend.";

        // private static fields
        private static readonly byte[] __bigMessage = GenerateBigMessage(135000);

        // private static methods
        private static byte[] GenerateBigMessage(int size)
        {
            var resultBytes = new List<byte>();
            var messagePortionBytes = Encoding.ASCII.GetBytes(__testMessagePortion);
            while (resultBytes.Count < size)
            {
                resultBytes.AddRange(messagePortionBytes);
            }
            return resultBytes.ToArray();
        }
        #endregion

        [Fact]
        public void Snappy_compressor_should_read_the_previously_written_message()
        {
            var bytes = Encoding.ASCII.GetBytes(__testMessage);
            var compressor = GetCompressor(CompressorType.Snappy);
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
                        .Be("120,156,74,76,74,78,73,77,75,207,200,204,202,206,201,205,203,47,40,44,42,46,41,45,43,175,168,172,50,48,52,50,54,49,53,51,183,176,84,72,164,150,34,0,0,0,0,255,255,3,0,228,159,39,197");
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

        [Fact]
        public void Zstandard_compress_should_throw_when_output_stream_is_null()
        {
            using (var input = new MemoryStream())
            {
                var compressor = GetCompressor(CompressorType.ZStandard, 6);
                var exception = Record.Exception(() => compressor.Compress(input, null));
                var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
                e.ParamName.Should().Be("stream");
            }
        }

        [Fact]
        public void Zstandard_compressed_size_with_low_compression_level_should_be_bigger_than_with_high()
        {
            var lengths = new List<int>();
            // note: some close compression levels can give the same results for not huge text sizes
            foreach (var compressionLevel in new[] { 1, 5, 10, 15, 22 })
            {
                using (var input = new MemoryStream(__bigMessage))
                using (var output = new MemoryStream())
                {
                    var compressor = GetCompressor(CompressorType.ZStandard, compressionLevel);
                    compressor.Compress(input, output);
                    lengths.Add((int)output.Length);
                }
            }
            lengths.Should().BeInDescendingOrder();
        }

        [Theory]
        [ParameterAttributeData]
        public void Zstandard_compressor_should_decompress_the_previously_compressed_message([Range(1, 22)] int compressionLevel)
        {
            var messageBytes = __bigMessage;
            var compressor = GetCompressor(CompressorType.ZStandard, compressionLevel);
            Assert(
                messageBytes,
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
                    var resultBytes = input.ReadBytes((int)input.Length);
                    resultBytes.Should().Equal(messageBytes);
                });
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
                case CompressorType.ZStandard:
                    return new ZstandardCompressor((int)option);
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
