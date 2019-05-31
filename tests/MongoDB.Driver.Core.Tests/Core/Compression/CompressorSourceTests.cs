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
using System.Linq;
using FluentAssertions;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Configuration;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.Compression
{
    public class CompressorSourceTests
    {
        [Fact]
        public void Constructor_should_throw_if_the_allowed_compressors_collection_is_null()
        {
            var exception = Record.Exception(() => CreateSubject(null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("allowedCompressors");
        }

        [Fact]
        public void Get_should_add_a_new_compressor_to_the_cache_and_return_the_cached_compressors_if_available()
        {
            var subject = CreateSubject();

            var compressor = subject.Get(CompressorType.Zlib);
            var cachedCompressor = subject.Get(CompressorType.Zlib);

            compressor.Should().BeSameAs(cachedCompressor);
        }

        [Theory]
        [InlineData(CompressorType.Zlib)]
        [InlineData(CompressorType.Noop)]
        public void Get_should_return_expected_result(CompressorType compressorType)
        {
            var subject = CreateSubject(compressorType);

            var compressor = subject.Get(compressorType);

            compressor.Type.Should().Be(compressor.Type);
        }

        [Fact]
        public void Get_should_throw_the_exception_for_an_supported_compressor_type_that_was_not_be_specified_during_class_creation()
        {
            var subject = CreateSubject();

            var exception =  Record.Exception(() => subject.Get(CompressorType.Snappy));

            exception.Should().BeOfType<NotSupportedException>();
        }

        private ICompressorSource CreateSubject(params CompressorType[] requestedCompressors)
        {
            if (requestedCompressors != null && requestedCompressors.Length == 0)
            {
                requestedCompressors = new[] { CompressorType.Zlib };
            }

            CompressorConfiguration[] allowedCompressors = null;
            if (requestedCompressors != null)
            {
                allowedCompressors = 
                    requestedCompressors
                        .Select(c => new CompressorConfiguration(c))
                        .ToArray();
            }

            return new CompressorSource(allowedCompressors);
        }
    }
}
