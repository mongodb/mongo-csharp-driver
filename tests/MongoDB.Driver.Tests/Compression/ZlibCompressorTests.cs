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
using System.Linq;
using FluentAssertions;
using MongoDB.Driver.Compression;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Compression
{
    public class ZlibCompressorTests : CompressorTestsBase
    {
        private protected override CompressorType ExpectedCompressorType => CompressorType.Zlib;

        private protected override ICompressor CreateCompressor() => new ZlibCompressor();

        private protected override IEnumerable<ICompressor> CreateRoundTripCompressors() =>
            new[] { -1, 0, 1, 6, 9 }.Select(level => new ZlibCompressor(level));

        // level 0 is excluded because it does not compress
        private protected override IEnumerable<ICompressor> CreateShrinkingCompressors() =>
            new[] { 1, 6, 9 }.Select(level => new ZlibCompressor(level));

        [Theory]
        [ParameterAttributeData]
        public void Constructor_should_throw_if_the_level_is_out_of_range([Values(-2, 10)] int compressionLevel)
        {
            var exception = Record.Exception(() => new ZlibCompressor(compressionLevel));

            var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            e.ParamName.Should().Be("compressionLevel");
        }

        [Fact]
        public void Compress_with_higher_compression_should_produce_smaller_output()
        {
            AssertHigherCompressionLevelsProduceSmallerOutput(level => new ZlibCompressor(level), 0, 1, 6, 9);
        }
    }
}
