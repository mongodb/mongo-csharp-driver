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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using MongoDB.Driver.Compression;
using Xunit;

namespace MongoDB.Driver.Tests.Compression
{
    // ICompressor is internal, so the members that expose it are private protected.
    public abstract class CompressorTestsBase
    {
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

        private protected abstract CompressorType ExpectedCompressorType { get; }

        // The compressor with default settings.
        private protected abstract ICompressor CreateCompressor();

        // The compressors (for example, one per compression level) that the round trip tests run against.
        private protected virtual IEnumerable<ICompressor> CreateRoundTripCompressors() => [CreateCompressor()];

        // The compressors that are expected to shrink highly compressible data.
        private protected virtual IEnumerable<ICompressor> CreateShrinkingCompressors() => [CreateCompressor()];

        [Fact]
        public void Type_should_return_expected_compressor_type()
        {
            var compressor = CreateCompressor();

            compressor.Type.Should().Be(ExpectedCompressorType);
        }

        [Fact]
        public void Compress_should_throw_when_input_is_null()
        {
            var compressor = CreateCompressor();
            using var output = new MemoryStream();

            var exception = Record.Exception(() => compressor.Compress(null, output));

            exception.Should().BeOfType<ArgumentNullException>().Subject
                .ParamName.Should().Be("input");
        }

        [Fact]
        public void Compress_should_throw_when_output_is_null()
        {
            var compressor = CreateCompressor();
            using var input = new MemoryStream();

            var exception = Record.Exception(() => compressor.Compress(input, null));

            exception.Should().BeOfType<ArgumentNullException>().Subject
                .ParamName.Should().Be("output");
        }

        [Fact]
        public void Decompress_should_throw_when_input_is_null()
        {
            var compressor = CreateCompressor();
            using var output = new MemoryStream();

            var exception = Record.Exception(() => compressor.Decompress(null, output));

            exception.Should().BeOfType<ArgumentNullException>().Subject
                .ParamName.Should().Be("input");
        }

        [Fact]
        public void Decompress_should_throw_when_output_is_null()
        {
            var compressor = CreateCompressor();
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

            foreach (var compressor in CreateRoundTripCompressors())
            {
                using var input = new MemoryStream(bytes);
                using var compressed = new MemoryStream();
                using var decompressed = new MemoryStream();

                compressor.Compress(input, compressed);
                compressed.Position = 0;
                compressor.Decompress(compressed, decompressed);

                decompressed.ToArray().Should().Equal(bytes, $"compressor {compressor}");
            }
        }

        [Theory]
        [InlineData(100_000, 0)]
        [InlineData(200_000, 1)]
        [InlineData(1024 * 1024, 1)]
        public void Compress_should_shrink_highly_compressible_data(int size, int pattern)
        {
            var bytes = pattern == 0 ? new byte[size] : CreateRepeatingPattern(size);

            foreach (var compressor in CreateShrinkingCompressors())
            {
                using var input = new MemoryStream(bytes);
                using var compressed = new MemoryStream();

                compressor.Compress(input, compressed);

                compressed.Length.Should().BeLessThan(size / 4, $"compressor {compressor}");
            }
        }

        [Fact]
        public void Compress_should_only_process_remainder_of_input_when_position_is_non_zero()
        {
            var header = "HEADER-NOT-COMPRESSED"u8.ToArray();
            var payload = CreateRepeatingPattern(size: 10_000);
            var compressor = CreateCompressor();

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
            var compressor = CreateCompressor();

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

        // Asserts that the output size strictly decreases as the compression level increases.
        private protected static void AssertHigherCompressionLevelsProduceSmallerOutput(
            Func<int, ICompressor> createCompressor,
            params int[] compressionLevels)
        {
            var bytes = CreateJsonLikeData(size: 512 * 1024, seed: 24680);

            var compressedSizes = compressionLevels.Select(compressionLevel =>
            {
                var compressor = createCompressor(compressionLevel);
                using var input = new MemoryStream(bytes);
                using var compressed = new MemoryStream();

                compressor.Compress(input, compressed);

                return compressed.Length;
            }).ToArray();

            for (var i = 1; i < compressionLevels.Length; i++)
            {
                compressedSizes[i].Should().BeLessThan(
                    compressedSizes[i - 1],
                    $"compression level {compressionLevels[i]} should produce smaller output than level {compressionLevels[i - 1]}");
            }
        }

        private protected static byte[] CreateAllByteValues()
        {
            var bytes = new byte[256 * 4];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(i % 256);
            }
            return bytes;
        }

        private protected static byte[] CreateJsonLikeData(int size, int seed)
        {
            // Semi-structured records with random values give data that is compressible but not trivially so,
            // so that different compression levels produce measurably different output sizes.
            var random = new Random(seed);
            var builder = new StringBuilder(size + 128);
            for (var i = 0; builder.Length < size; i++)
            {
                builder.Append($"{{\"id\":{i},\"name\":\"user{random.Next(500)}\",\"score\":{random.Next(1000)},\"tags\":[\"a{random.Next(20)}\",\"b{random.Next(20)}\"]}},");
            }

            return Encoding.ASCII.GetBytes(builder.ToString(0, size));
        }

        private protected static byte[] CreateRandomBytes(int size, int seed)
        {
            var bytes = new byte[size];
            new Random(seed).NextBytes(bytes);
            return bytes;
        }

        private protected static byte[] CreateRepeatingPattern(int size)
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
