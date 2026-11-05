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

using System.IO;
using FluentAssertions;
using MongoDB.Driver.Compression;
using Xunit;

namespace MongoDB.Driver.Tests.Compression
{
    public class SnappyCompressorTests : CompressorTestsBase
    {
        private protected override CompressorType ExpectedCompressorType => CompressorType.Snappy;

        private protected override ICompressor CreateCompressor() => new SnappyCompressor();

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
    }
}
