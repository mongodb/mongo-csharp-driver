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
using System.IO.Compression;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Compression;
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
                    // Expected bytes are what System.IO.Compression.DeflateStream produces at level 6.
                    // These differ from the SharpCompress-produced bytes used before CSHARP-6037 because
                    // SharpCompress emitted an intermediate Z_SYNC_FLUSH (00 00 FF FF) followed by a final
                    // block (FlushType.Sync), while DeflateStream emits a single final block. Both decode
                    // to the same plaintext — Zlib_decompress_should_handle_sync_flush_markers pins the
                    // inbound-compatibility guarantee for peers that still emit sync-flush framing.
                    result
                        .Should()
                        .Be("120,156,75,76,74,78,73,77,75,207,200,204,202,206,201,205,203,47,40,44,42,46,41,45,43,175,168,172,50,48,52,50,54,49,53,51,183,176,84,72,164,150,34,0,228,159,39,197");
                });
        }

        [Fact]
        public void Zlib_compressed_bytes_should_have_valid_header_and_roundtrip()
        {
            var bytes = Encoding.ASCII.GetBytes(__testMessage);
            Assert(
                bytes,
                (input, output) =>
                {
                    var compressor = GetCompressor(CompressorType.Zlib, 6);
                    compressor.Compress(input, output);
                    output.Length.Should().BeLessThan(input.Length);
                },
                (input, output) =>
                {
                    var resultBytes = output.ToArray();
                    resultBytes[0].Should().Be(0x78); // CMF byte: deflate with 32K window (RFC 1950)
                    ((resultBytes[0] * 256 + resultBytes[1]) % 31).Should().Be(0); // valid header checksum

                    output.Position = 0;
                    input.Position = 0;
                    input.SetLength(0);
                    var compressor = GetCompressor(CompressorType.Zlib, 6);
                    compressor.Decompress(output, input);
                    input.Position = 0;
                    Encoding.ASCII.GetString(input.ReadBytes((int)input.Length)).Should().Be(__testMessage);
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
                        // Level 0 (no compression) always adds framing overhead, so output > input
                        output.Length.Should().BeGreaterThan(input.Length);
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
        public void Zlib_decompress_should_handle_sync_flush_markers()
        {
            // These are the exact bytes SharpCompress emitted for __testMessage at level 6 with FlushType.Sync.
            // The 00 00 FF FF sequence is a Z_SYNC_FLUSH empty stored block — valid RFC 1951 deflate.
            // Verifies that incoming data from implementations that emit sync flushes decompresses correctly.
            var bytesWithSyncFlush = new byte[]
            {
                120, 156, 74, 76, 74, 78, 73, 77, 75, 207, 200, 204, 202, 206, 201, 205, 203, 47, 40, 44,
                42, 46, 41, 45, 43, 175, 168, 172, 50, 48, 52, 50, 54, 49, 53, 51, 183, 176, 84, 72, 164,
                150, 34, 0, 0, 0, 0, 255, 255, 3, 0, 228, 159, 39, 197
            };
            var compressor = GetCompressor(CompressorType.Zlib, 6);
            using (var output = new MemoryStream())
            {
                compressor.Decompress(new MemoryStream(bytesWithSyncFlush), output);
                Encoding.ASCII.GetString(output.ToArray()).Should().Be(__testMessage);
            }
        }

        [Fact]
        public void Zlib_decompress_should_throw_on_invalid_header()
        {
            var compressor = GetCompressor(CompressorType.Zlib, 6);
            var badData = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }; // invalid zlib header
            var exception = Record.Exception(() => compressor.Decompress(new MemoryStream(badData), new MemoryStream()));
            exception.Should().BeOfType<FormatException>();
        }

        [Fact]
        public void Zlib_decompress_should_throw_on_preset_dictionary()
        {
            // CMF=0x78, FLG=0xBB: low nibble CM=8, FDICT bit set, (0x78*256+0xBB) % 31 == 0.
            var compressor = GetCompressor(CompressorType.Zlib, 6);
            var fdictData = new byte[] { 0x78, 0xBB, 0, 0, 0, 0, 0x03, 0x00, 0, 0, 0, 1 };
            var exception = Record.Exception(() => compressor.Decompress(new MemoryStream(fdictData), new MemoryStream()));
            exception.Should().BeOfType<NotSupportedException>();
        }

        [Fact]
        public void Zlib_decompress_should_throw_on_checksum_mismatch()
        {
            var compressor = GetCompressor(CompressorType.Zlib, 6);
            using (var compressed = new MemoryStream())
            {
                compressor.Compress(new MemoryStream(Encoding.ASCII.GetBytes(__testMessage)), compressed);
                var bytes = compressed.ToArray();
                bytes[bytes.Length - 1] ^= 0xFF; // corrupt the Adler-32
                var exception = Record.Exception(() => compressor.Decompress(new MemoryStream(bytes), new MemoryStream()));
                exception.Should().BeOfType<InvalidDataException>();
            }
        }

        [Fact]
        public void Zlib_roundtrip_should_handle_empty_input()
        {
            // Compressing zero bytes must still produce a valid zlib stream: header + empty deflate
            // block (03 00) + Adler-32 of empty input (0x00000001). Exercises the Dispose-without-Write
            // path in ZlibStream that emits the empty stream when no Write call ever happened.
            var compressor = GetCompressor(CompressorType.Zlib, 6);
            using (var compressed = new MemoryStream())
            using (var roundtripped = new MemoryStream())
            {
                compressor.Compress(new MemoryStream(Array.Empty<byte>()), compressed);
                var bytes = compressed.ToArray();
                bytes.Length.Should().Be(8); // 2 header + 2 deflate + 4 adler — the minimum valid zlib stream
                bytes[bytes.Length - 4].Should().Be(0x00);
                bytes[bytes.Length - 3].Should().Be(0x00);
                bytes[bytes.Length - 2].Should().Be(0x00);
                bytes[bytes.Length - 1].Should().Be(0x01); // Adler-32 of empty == 1

                compressed.Position = 0;
                compressor.Decompress(compressed, roundtripped);
                roundtripped.Length.Should().Be(0);
            }
        }

        [Fact]
        public void Zlib_decompress_read_should_handle_nonzero_offset_and_partial_reads()
        {
            // Drives ZlibStream.Read directly with non-zero offsets and small counts to verify the
            // offset is propagated correctly into UpdateAdler32 (not aliased to 0) and that calling
            // Read after EOF returns 0 without re-validating the trailer (_trailerValidated short-circuit).
            // The driver's Stream.CopyTo path only ever uses offset=0 with a large buffer, so this is
            // the only place those branches get exercised.
            var payload = Encoding.ASCII.GetBytes(__testMessage);
            var compressor = GetCompressor(CompressorType.Zlib, 6);
            byte[] compressedBytes;
            using (var compressed = new MemoryStream())
            {
                compressor.Compress(new MemoryStream(payload), compressed);
                compressedBytes = compressed.ToArray();
            }

            var assembled = new List<byte>();
            using (var zlib = new ZlibStream(new MemoryStream(compressedBytes), CompressionMode.Decompress, leaveOpen: true))
            {
                // 32-byte working buffer with a 5-byte prefix offset, reading 13 bytes at a time.
                var buffer = new byte[32];
                int n;
                while ((n = zlib.Read(buffer, 5, 13)) > 0)
                {
                    for (var i = 0; i < n; i++) assembled.Add(buffer[5 + i]);
                }

                // Read again past EOF — should return 0 and not throw (trailer already validated).
                zlib.Read(buffer, 5, 13).Should().Be(0);
            }

            assembled.ToArray().Should().Equal(payload);
        }

        [Fact]
        public void Zlib_compress_level_zero_should_emit_no_compression_header()
        {
            // Level 0 must emit the FLEVEL=0 header (0x78, 0x01), not the default FLEVEL=2 (0x78, 0x9C).
            // Both decompress identically (FLEVEL is informational per RFC 1950 §2.2), so a swapped
            // header constant would round-trip fine — pin the actual byte.
            var compressor = GetCompressor(CompressorType.Zlib, 0);
            using (var compressed = new MemoryStream())
            {
                compressor.Compress(new MemoryStream(Encoding.ASCII.GetBytes(__testMessage)), compressed);
                var bytes = compressed.ToArray();
                bytes[0].Should().Be(0x78);
                bytes[1].Should().Be(0x01);
            }
        }

        [Fact]
        public void Zlib_roundtrip_should_validate_adler32_across_nmax_chunk_boundary()
        {
            // UpdateAdler32 batches its deferred modulo every NMAX (5552) bytes. Exercise that
            // path with an input that spans multiple chunks and uses varied byte values to stress
            // both the s1 (byte sum) and s2 (running-sum-of-sums) accumulators near the boundary.
            // An off-by-one in chunk advancement (offset/count) or a missed modulo would corrupt
            // the trailing Adler-32 and the decompress-side validation would throw.
            const int size = 5552 * 3 + 137; // straddles three NMAX windows with a tail
            var payload = new byte[size];
            for (var i = 0; i < size; i++)
            {
                payload[i] = (byte)((i * 31 + 7) & 0xFF); // covers full 0-255 range, non-trivial pattern
            }

            var compressor = GetCompressor(CompressorType.Zlib, 6);
            using (var compressed = new MemoryStream())
            using (var roundtripped = new MemoryStream())
            {
                compressor.Compress(new MemoryStream(payload), compressed);
                compressed.Position = 0;
                compressor.Decompress(compressed, roundtripped);
                roundtripped.ToArray().Should().Equal(payload);
            }
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
                test(byteBufferStream, memoryStream);
                assertResult?.Invoke(byteBufferStream, memoryStream);
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
