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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson.IO;
using Xunit;

namespace MongoDB.Bson.Tests.IO
{
    public class EncodingHelperTests
    {
        [Fact]
        public void TestEncodingHelper_invalid_encoding_shouldthrow()
        {
            var exception = Record.Exception(() => EncodingHelper.GetBytesCachedBuffer(null, "asd"));
            exception.Should().BeOfType<ArgumentNullException>();
            exception.Message.Should().Contain("encoding");
        }

        [Fact]
        public void TestEncodingHelper_invalid_string_shouldthrow()
        {
            var exception = Record.Exception(() => EncodingHelper.GetBytesCachedBuffer(Encoding.ASCII, null));
            exception.Should().BeOfType<ArgumentNullException>();
            exception.Message.Should().Contain("value");
        }

        [Fact]
        public void TestEncodingHelper_empty_string_shouldbe_default_buffer()
        {
            var segmentA = EncodingHelper.GetBytesCachedBuffer(Encoding.ASCII, "");
            var segmentB = EncodingHelper.GetBytesCachedBuffer(Encoding.ASCII, "");

            segmentA.Array.Length.Should().Be(0);
            segmentA.Array.Should().BeSameAs(segmentB.Array);
        }

        [Theory]
        [InlineData(60)]
        [InlineData(127)]
        [InlineData(511)]
        public void TestEncodingHelper_should_reuse_instance(int maxStringSize)
        {
            var encoding = Utf8Encodings.Strict;

            byte[] previousInstance = null;

            while (maxStringSize > 4)
            {
                maxStringSize = (maxStringSize >> 1) + 1;

                var str = GetString(maxStringSize);

                var segment = encoding.GetBytesCachedBuffer(str);
                var encodedExpected = encoding.GetBytes(str);
                var encodedActual = segment.ToArray();

                previousInstance = previousInstance ?? segment.Array;

                encodedActual.ShouldAllBeEquivalentTo(encodedExpected);
                segment.Array.Should().BeSameAs(previousInstance);
            }
        }

        [Fact]
        public async Task TestEncodingHelperMultithreaded_should_encode_correctly()
        {
            const int threadsCount = 10;
            const int maxIterations = 10;
            const int maxSize = 1024;

            var random = new Random();
            var encoding = Utf8Encodings.Strict;

            var tasks = Enumerable.Range(0, threadsCount).Select(i => Task.Run(() =>
                {
                    for (int j = 0; j < maxIterations; j++)
                    {
                        var sizeCurrent = random.Next(8, maxSize);
                        var str = GetString(sizeCurrent);

                        var segment = encoding.GetBytesCachedBuffer(str);
                        var encodedExpected = encoding.GetBytes(str);
                        var encodedActual = segment.ToArray();

                        encodedActual.ShouldAllBeEquivalentTo(encodedExpected);
                    }
                })).ToArray();

            await Task.WhenAll(tasks);
        }

        // private methods
        private string GetString(int length) =>
            new string(Enumerable.Range(0, length).Select(i => (char)('a' + i % 26)).ToArray());
    }
}
