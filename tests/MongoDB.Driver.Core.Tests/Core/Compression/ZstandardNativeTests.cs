/* Copyright 2020-present MongoDB Inc.
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
using System.IO.Compression;
using System.Text;
using FluentAssertions;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Compression.Zstandard;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.Compression
{
    public class ZstandardNativeTests
    {
        #region static
        // private constants
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
        private static readonly byte[] __bigMessage = GenerateBigMessage(135000); // bigger than recommended size for one native operation

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

        [Theory]
        [ParameterAttributeData]
        public void Compressor_should_decompress_the_previously_compressed_message([Range(1, 22)] int compressionLevel)
        {
            var messageBytes = __bigMessage;

            var compressedBytes = Compress(messageBytes, compressionLevel);
            compressedBytes.Length.Should().BeLessThan(messageBytes.Length / 2);

            var decompressedBytes = Decompress(compressedBytes);
            decompressedBytes.Should().Equal(messageBytes);
        }

        [Theory]
        [InlineData(1, "40,181,47,253,0,72,108,1,0,84,2,97,98,99,100,101,102,103,104,105,106,107,108,109,110,111,112,113,114,115,116,117,118,119,120,121,122,48,49,50,51,52,53,54,55,56,57,32,1,0,53,132,170,39,1,0,0")]
        [InlineData(4, "40,181,47,253,0,88,108,1,0,84,2,97,98,99,100,101,102,103,104,105,106,107,108,109,110,111,112,113,114,115,116,117,118,119,120,121,122,48,49,50,51,52,53,54,55,56,57,32,1,0,53,132,170,39,1,0,0")]
        [InlineData(15, "40,181,47,253,0,96,108,1,0,84,2,97,98,99,100,101,102,103,104,105,106,107,108,109,110,111,112,113,114,115,116,117,118,119,120,121,122,48,49,50,51,52,53,54,55,56,57,32,1,0,53,132,170,39,1,0,0")]
        [InlineData(21, "40,181,47,253,0,128,108,1,0,84,2,97,98,99,100,101,102,103,104,105,106,107,108,109,110,111,112,113,114,115,116,117,118,119,120,121,122,48,49,50,51,52,53,54,55,56,57,32,1,0,53,132,170,39,1,0,0")]
        public void Compress_should_generate_expected_bytes_for_different_compression_levels(int compressionLevel, string expectedBytes)
        {
            var testMessage = "abcdefghijklmnopqrstuvwxyz0123456789 abcdefghijklmnopqrstuvwxyz0123456789 abcdefghijklmnopqrstuvwxyz0123456789";
            var data = Encoding.ASCII.GetBytes(testMessage);

            var resultBytes = Compress(data, compressionLevel);
            string.Join(",", resultBytes).Should().Be(expectedBytes);
        }

        [Fact]
        public void Compressed_size_with_low_compression_level_should_be_bigger_than_with_high()
        {
            var lengths = new List<int>();
            // note: some close compression levels can give the same results for not huge text sizes
            foreach (var compressionLevel in new [] { 1, 5, 10, 15, 22 })
            {
                var compressedBytes = Compress(__bigMessage, compressionLevel);
                lengths.Add(compressedBytes.Length);
            }
            lengths.Should().BeInDescendingOrder();
        }

        [Fact]
        public void Constructor_should_throw_when_compressionMode_is_incorrect()
        {
            using (var memoryStream = new MemoryStream())
            {
                var exception = Record.Exception(() => new ZstandardStream(memoryStream, (CompressionMode)2));
                var e = exception.Should().BeOfType<ArgumentException>().Subject;
                e.ParamName.Should().Be("compressionMode");
            }
        }

        [Fact]
        public void Constructor_should_throw_when_compressedStream_is_null()
        {
            var exception = Record.Exception(() => new ZstandardStream(null, CompressionMode.Compress));
            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("compressedStream");
        }

        [Fact]
        public void Constructor_should_throw_when_compressionLevel_has_been_set_for_Decompress_mode()
        {
            using (var memoryStream = new MemoryStream())
            {
                var exception = Record.Exception(() => new ZstandardStream(memoryStream, CompressionMode.Decompress, 1));
                var e = exception.Should().BeOfType<ArgumentException>().Subject;
                e.ParamName.Should().Be("compressionLevel");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Constructor_should_throw_when_compressionLevel_is_out_of_range([Values(0, 23)] int compressionLevel)
        {
            using (var memoryStream = new MemoryStream())
            {
                var exception = Record.Exception(() => new ZstandardStream(memoryStream, CompressionMode.Compress, compressionLevel));
                var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
                e.ParamName.Should().Be(nameof(compressionLevel));
            }
        }

        // private methods
        private byte[] Compress(byte[] data, int compressionLevel)
        {
            using (var inputStream = new MemoryStream(data))
            using (var outputStream = new MemoryStream())
            {
                using (var zstandardStream = new ZstandardStream(outputStream, CompressionMode.Compress, compressionLevel))
                {
                    inputStream.EfficientCopyTo(zstandardStream);
                    zstandardStream.Flush();
                }
                return outputStream.ToArray();
            }
        }

        private byte[] Decompress(byte[] compressed)
        {
            using (var inputStream = new MemoryStream(compressed))
            using (var zstandardStream = new ZstandardStream(inputStream, CompressionMode.Decompress))
            using (var outputStream = new MemoryStream())
            {
                zstandardStream.CopyTo(outputStream);
                return outputStream.ToArray();
            }
        }
    }
}
