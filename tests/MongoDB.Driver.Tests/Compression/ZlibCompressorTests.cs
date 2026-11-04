/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Driver.Compression;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Compression
{
    public class ZlibCompressorTests
    {
        [Fact]
        public void Type_should_return_Zlib_compressor_type()
        {
            var compressor = new ZlibCompressor();
            compressor.Type.Should().Be(CompressorType.Zlib);
        }

        [Theory]
        [ParameterAttributeData]
        public void Constructor_should_throw_if_the_level_is_out_of_range([Values(-2, 10)] int compressionLevel)
        {
            var exception = Record.Exception(() => new ZlibCompressor(compressionLevel));

            var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            e.ParamName.Should().Be("compressionLevel");
        }

        [Fact]
        public void Compress_should_throw_when_input_is_null()
        {
            var compressor = new ZlibCompressor();
            using var output = new MemoryStream();

            var exception = Record.Exception(() => compressor.Compress(null, output));

            exception.Should().BeOfType<ArgumentNullException>().Subject
                .ParamName.Should().Be("input");
        }

        [Fact]
        public void Compress_should_throw_when_output_is_null()
        {
            var compressor = new ZlibCompressor();
            using var input = new MemoryStream();

            var exception = Record.Exception(() => compressor.Compress(input, null));

            exception.Should().BeOfType<ArgumentNullException>().Subject
                .ParamName.Should().Be("output");
        }

        [Fact]
        public void Decompress_should_throw_when_input_is_null()
        {
            var compressor = new ZlibCompressor();
            using var output = new MemoryStream();

            var exception = Record.Exception(() => compressor.Decompress(null, output));

            exception.Should().BeOfType<ArgumentNullException>().Subject
                .ParamName.Should().Be("input");
        }

        [Fact]
        public void Decompress_should_throw_when_output_is_null()
        {
            var compressor = new ZlibCompressor();
            using var input = new MemoryStream();

            var exception = Record.Exception(() => compressor.Decompress(input, null));

            exception.Should().BeOfType<ArgumentNullException>().Subject
                .ParamName.Should().Be("output");
        }

        public static TheoryData<string, byte[]> RoundTripData => new()
        {
            { "empty", new byte[0] },
            { "single byte", new byte[] { 0x42 } },
            { "all byte values", CreateAllByteValues() },
            { "random incompressible", CreateRandomBytes(size: 4096, seed: 12345) },
            { "zeros", new byte[100_000] },
            { "repeating pattern above 64KB", CreateRepeatingPattern(size: 200_000) },
            { "large repeating pattern 1MB", CreateRepeatingPattern(size: 1024 * 1024) },
            { "large random 1MB", CreateRandomBytes(size: 1024 * 1024, seed: 67890) }
        };

        [Theory]
        [MemberData(nameof(RoundTripData))]
        public void Compress_and_Decompress_should_round_trip_data_shape(string description, byte[] bytes)
        {
            _ = description;

            foreach (var compressionLevel in new[] { -1, 0, 1, 6, 9 })
            {
                var compressor = new ZlibCompressor(compressionLevel);

                using var input = new MemoryStream(bytes);
                using var compressed = new MemoryStream();
                using var decompressed = new MemoryStream();

                compressor.Compress(input, compressed);
                compressed.Position = 0;
                compressor.Decompress(compressed, decompressed);

                decompressed.ToArray().Should().Equal(bytes, $"compression level {compressionLevel}");
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(6)]
        [InlineData(9)]
        public void Compress_should_shrink_highly_compressible_data(int compressionLevel)
        {
            var compressor = new ZlibCompressor(compressionLevel);

            foreach (var bytes in new[] { new byte[100_000], CreateRepeatingPattern(size: 200_000), CreateRepeatingPattern(size: 1024 * 1024) })
            {
                using var input = new MemoryStream(bytes);
                using var compressed = new MemoryStream();

                compressor.Compress(input, compressed);

                compressed.Length.Should().BeLessThan(bytes.Length / 4);
            }
        }

        [Fact]
        public void Compress_should_only_process_remainder_of_input_when_position_is_non_zero()
        {
            var header = "HEADER-NOT-COMPRESSED"u8.ToArray();
            var payload = CreateRepeatingPattern(size: 10_000);
            var compressor = new ZlibCompressor();

            using var input = new MemoryStream();
            input.Write(header, 0, header.Length);
            input.Write(payload, 0, payload.Length);
            input.Position = header.Length;
            using var compressed = new MemoryStream();
            using var decompressed = new MemoryStream();

            compressor.Compress(input, compressed);

            input.Position.Should().Be(input.Length);
            compressed.Position = 0;
            compressor.Decompress(compressed, decompressed);
            decompressed.ToArray().Should().Equal(payload);
        }

        [Fact]
        public void Decompress_should_only_process_remainder_of_input_when_position_is_non_zero()
        {
            var header = "HEADER-NOT-COMPRESSED"u8.ToArray();
            var payload = CreateRepeatingPattern(size: 10_000);
            var compressor = new ZlibCompressor();

            using var compressedPayload = new MemoryStream();
            compressor.Compress(new MemoryStream(payload), compressedPayload);

            using var input = new MemoryStream();
            input.Write(header, 0, header.Length);
            input.Write(compressedPayload.ToArray(), 0, (int)compressedPayload.Length);
            input.Position = header.Length;
            using var decompressed = new MemoryStream();

            compressor.Decompress(input, decompressed);

            decompressed.ToArray().Should().Equal(payload);
        }

        private static byte[] CreateAllByteValues()
        {
            var bytes = new byte[256 * 4];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(i % 256);
            }
            return bytes;
        }

        private static byte[] CreateRandomBytes(int size, int seed)
        {
            var bytes = new byte[size];
            new Random(seed).NextBytes(bytes);
            return bytes;
        }

        private static byte[] CreateRepeatingPattern(int size)
        {
            var pattern = "The quick brown fox jumps over the lazy dog. "u8.ToArray();
            var bytes = new byte[size];
            for (var i = 0; i < size; i++)
            {
                bytes[i] = pattern[i % pattern.Length];
            }
            return bytes;
        }
    }
}
