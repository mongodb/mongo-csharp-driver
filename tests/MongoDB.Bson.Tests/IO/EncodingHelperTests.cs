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
using System.Linq;
using System.Text;
using Shouldly;
using MongoDB.Bson.IO;
using MongoDB.Bson.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests.IO
{
    public class EncodingHelperTests
    {
        [Fact]
        public void GetBytesUsingThreadStaticBuffer_should_throw_when_encoding_is_null()
        {
            var exception = Record.Exception(() => EncodingHelper.GetBytesUsingThreadStaticBuffer(null, "asd"));

            var e = exception.ShouldBeOfType<ArgumentNullException>();
            e.ParamName.ShouldBe("encoding");

            AssertGetBytesUsingThreadStaticBufferWorks();
        }

        [Fact]
        public void GetBytesUsingThreadStaticBuffer_should_throw_when_string_is_invalid()
        {
            const string invalidUtf8String = "\uD801";

            var exception = Record.Exception(() => EncodingHelper.GetBytesUsingThreadStaticBuffer(Utf8Encodings.Strict, invalidUtf8String));
            exception.ShouldBeOfType<EncoderFallbackException>();

            AssertGetBytesUsingThreadStaticBufferWorks();
        }

        [Fact]
        public void GetBytesUsingThreadStaticBuffer_should_throw_when_value_is_null()
        {
            var exception = Record.Exception(() => EncodingHelper.GetBytesUsingThreadStaticBuffer(Encoding.ASCII, null));

            var e = exception.ShouldBeOfType<ArgumentNullException>();
            e.ParamName.ShouldBe("value");

            AssertGetBytesUsingThreadStaticBufferWorks();
        }

        [Fact]
        public void GetBytesUsingThreadStaticBuffer_should_return_cached_empty_segment_when_value_is_empty_string()
        {
            using var segmentA = EncodingHelper.GetBytesUsingThreadStaticBuffer(Encoding.ASCII, "");
            using var segmentB = EncodingHelper.GetBytesUsingThreadStaticBuffer(Encoding.ASCII, "");

            segmentA.Segment.Array.Length.ShouldBe(0);
            segmentA.Segment.Array.ShouldBeSameAs(segmentB.Segment.Array);
        }

        [Theory]
        [InlineData(60)]
        [InlineData(127)]
        [InlineData(511)]
        public void GetBytesUsingThreadStaticBuffer_should_return_same_instance_when_possible(int maxStringSize)
        {
            var encoding = Utf8Encodings.Strict;

            byte[] previousInstance = null;

            while (maxStringSize > 4)
            {
                maxStringSize = (maxStringSize >> 1) + 1;

                var str = GetString(maxStringSize);

                using var rentedSegment = encoding.GetBytesUsingThreadStaticBuffer(str);
                var segment = rentedSegment.Segment;
                var encodedExpected = encoding.GetBytes(str);
                var encodedActual = segment.ToArray();

                previousInstance = previousInstance ?? segment.Array;

                encodedActual.ShouldBe(encodedExpected);
                segment.Array.ShouldBeSameAs(previousInstance);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void GetBytesUsingThreadStaticBuffer_should_return_expected_result_when_multiple_threads_are_used([RandomSeed] int seed)
        {
            const int threadsCount = 10;
            const int iterationsCount = 10;
            const int maxSize = 1024;

            var random = new Random(seed);
            var encoding = Utf8Encodings.Strict;

            ThreadingUtilities.ExecuteOnNewThreads(threadsCount, _ =>
                {
                    for (int j = 0; j < iterationsCount; j++)
                    {
                        var sizeCurrent = random.Next(8, maxSize);
                        var str = GetString(sizeCurrent);

                        using var rentedSegment = encoding.GetBytesUsingThreadStaticBuffer(str);
                        var segment = rentedSegment.Segment;
                        var encodedExpected = encoding.GetBytes(str);
                        var encodedActual = segment.ToArray();

                        encodedActual.ShouldBe(encodedExpected);
                    }
                });
        }

        // private methods
        private void AssertGetBytesUsingThreadStaticBufferWorks()
        {
            using var segment = EncodingHelper.GetBytesUsingThreadStaticBuffer(Utf8Encodings.Strict, "Apples");
            segment.Segment.Array.Length.ShouldBeGreaterThan(0);
        }

        private string GetString(int length) =>
            new string(Enumerable.Range(0, length).Select(i => (char)('a' + i % 26)).ToArray());
    }
}
