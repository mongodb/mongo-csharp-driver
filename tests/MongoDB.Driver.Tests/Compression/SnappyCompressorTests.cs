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
using System.Text;
using FluentAssertions;
using MongoDB.Driver.Compression;
using Xunit;

namespace MongoDB.Driver.Tests.Compression
{
    public class SnappyCompressorTests
    {
        [Fact]
        public void Type_should_return_snappy_compressor_type()
        {
            var compressor = new SnappyCompressor();
            compressor.Type.Should().Be(CompressorType.Snappy);
        }

        [Fact]
        public void Compress_should_throw_when_input_is_null()
        {
            var compressor = new SnappyCompressor();
            using var output = new MemoryStream();

            var exception = Record.Exception(() => compressor.Compress(null, output));

            exception.Should().BeOfType<ArgumentNullException>().Subject
                .ParamName.Should().Be("input");
        }

        [Fact]
        public void Compress_should_throw_when_output_is_null()
        {
            var compressor = new SnappyCompressor();
            using var input = new MemoryStream();

            var exception = Record.Exception(() => compressor.Compress(input, null));

            exception.Should().BeOfType<ArgumentNullException>().Subject
                .ParamName.Should().Be("output");
        }

        [Fact]
        public void Decompress_should_throw_when_input_is_null()
        {
            var compressor = new SnappyCompressor();
            using var output = new MemoryStream();

            var exception = Record.Exception(() => compressor.Decompress(null, output));

            exception.Should().BeOfType<ArgumentNullException>().Subject
                .ParamName.Should().Be("input");
        }

        [Fact]
        public void Decompress_should_throw_when_output_is_null()
        {
            var compressor = new SnappyCompressor();
            using var input = new MemoryStream();

            var exception = Record.Exception(() => compressor.Decompress(input, null));

            exception.Should().BeOfType<ArgumentNullException>().Subject
                .ParamName.Should().Be("output");
        }

        [Theory]
        [MemberData(nameof(RoundTripData))]
        public void Compress_and_Decompress_should_round_trip_data_shape(string description, byte[] bytes)
        {
            _ = description;
            var compressor = new SnappyCompressor();

            using var input = new MemoryStream(bytes);
            using var compressed = new MemoryStream();
            using var decompressed = new MemoryStream();

            compressor.Compress(input, compressed);
            compressed.Position = 0;
            compressor.Decompress(compressed, decompressed);

            decompressed.ToArray().Should().Equal(bytes);
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
        [InlineData(100_000, 0)]
        [InlineData(200_000, 1)]
        [InlineData(1024 * 1024, 1)]
        public void Compress_should_shrink_highly_compressible_data(int size, int pattern)
        {
            var bytes = pattern == 0 ? new byte[size] : CreateRepeatingPattern(size);
            var compressor = new SnappyCompressor();

            using var input = new MemoryStream(bytes);
            using var compressed = new MemoryStream();

            compressor.Compress(input, compressed);

            compressed.Length.Should().BeLessThan(size / 4);
        }

        [Fact]
        public void Compress_should_produce_non_empty_output_for_empty_input()
        {
            var compressor = new SnappyCompressor();

            using var input = new MemoryStream();
            using var compressed = new MemoryStream();

            compressor.Compress(input, compressed);

            // Snappy always writes at least the varint-encoded uncompressed length preamble
            compressed.Length.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Compress_should_not_fail_on_incompressible_data_that_expands()
        {
            var bytes = CreateRandomBytes(size: 4096, seed: 12345);
            var compressor = new SnappyCompressor();

            using var input = new MemoryStream(bytes);
            using var compressed = new MemoryStream();

            compressor.Compress(input, compressed);

            compressed.Length.Should().BeLessOrEqualTo(Snappier.Snappy.GetMaxCompressedLength(bytes.Length));
        }

        [Fact]
        public void Compress_should_only_process_remainder_of_input_when_position_is_non_zero()
        {
            var header = Encoding.ASCII.GetBytes("HEADER-NOT-COMPRESSED");
            var payload = CreateRepeatingPattern(size: 10_000);
            var compressor = new SnappyCompressor();

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
            var compressor = new SnappyCompressor();

            using var compressedPayload = new MemoryStream();
            compressor.Compress(new MemoryStream(payload), compressedPayload);

            using var input = new MemoryStream();
            input.Write(header, 0, header.Length);
            input.Write(compressedPayload.ToArray(), 0, (int)compressedPayload.Length);
            input.Position = header.Length;
            using var decompressed = new MemoryStream();

            compressor.Decompress(input, decompressed);

            input.Position.Should().Be(input.Length);
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
            var pattern = "The quick brown fox jumps over the lazy dog."u8.ToArray();
            var bytes = new byte[size];
            for (var i = 0; i < size; i++)
            {
                bytes[i] = pattern[i % pattern.Length];
            }
            return bytes;
        }
    }
}
