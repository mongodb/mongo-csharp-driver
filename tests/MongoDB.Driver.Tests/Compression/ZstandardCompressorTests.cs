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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using MongoDB.Driver.Compression;
using Xunit;

namespace MongoDB.Driver.Tests.Compression
{
    public class ZstandardCompressorTests : CompressorTestsBase
    {
        private static readonly int[] __compressionLevels = { 1, 6, 22 };

        private protected override CompressorType ExpectedCompressorType => CompressorType.ZStandard;

        private protected override ICompressor CreateCompressor() => new ZstandardCompressor();

        private protected override IEnumerable<ICompressor> CreateRoundTripCompressors() =>
            __compressionLevels.Select(level => new ZstandardCompressor(level));

        private protected override IEnumerable<ICompressor> CreateShrinkingCompressors() =>
            __compressionLevels.Select(level => new ZstandardCompressor(level));

        [Fact]
        public void Compress_should_produce_non_empty_output_for_empty_input()
        {
            var compressor = new ZstandardCompressor();

            using var input = new MemoryStream();
            using var compressed = new MemoryStream();

            compressor.Compress(input, compressed);

            // zstd always writes a frame header
            compressed.Length.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Compress_with_higher_compression_should_produce_smaller_output()
        {
            AssertHigherCompressionLevelsProduceSmallerOutput(level => new ZstandardCompressor(level), __compressionLevels);
        }
    }
}
