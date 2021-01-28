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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.TestHelpers;
using Xunit;

namespace MongoDB.Bson.Tests.IO
{
    public class ThreadStaticBufferTests
    {
        public static readonly IEnumerable<object[]> IncrementalSizeTestData = new List<object[]>
        {   new object[]
            {
                (1, 256),
                (2, 256),
                (128, 256),
                (255, 256),
                (256, 256),
                (257, 512),
                (512, 512),
                (8191, 8192),
                (8192, 8192)
            }
        };

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        [InlineData(1024 * 1024 * 1024 + 1)]
        public void TestBuffer_invalid_buffer_size_should_throw(int size)
        {
            var exception = Record.Exception(() => ThreadStaticBuffer.GetBuffer(size));
            var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;

            e.ParamName.Should().Be("size");
        }

        [Theory]
        [MemberData(nameof(IncrementalSizeTestData))]
        public void GetBuffer_incrementally_growing_buffer_size_expected_powerof2(params (int requestedSize, int expectedSize)[] sizes)
        {
            ThreadingUtilities.ExecuteOnNewThread(2, _ =>
            {
                foreach (var (requestedSize, expectedSize) in sizes)
                {
                    var buffer = ThreadStaticBuffer.GetBuffer(requestedSize);

                    buffer.Length.Should().Be(expectedSize);
                }
            });
        }

        [Theory]
        [InlineData(16384)]
        [InlineData(16385)]
        [InlineData(16386)]
        [InlineData(32767)]
        [InlineData(32769)]
        public void GetBuffer_should_return_exact_size_if_requested_greater_than_maxsize(int requestedSize)
        {
            var buffer = ThreadStaticBuffer.GetBuffer(requestedSize);
            buffer.Length.Should().Be(requestedSize);
        }

        [Fact]
        public void GetBuffer_should_return_different_instance_in_different_threads()
        {
            const int threadsCount = 2;
            const int size = 256;

            var allBuffers = new ConcurrentBag<byte[]>();

            ThreadingUtilities.ExecuteOnNewThread(threadsCount, i =>
            {
                var buffer = ThreadStaticBuffer.GetBuffer(size);
                buffer.Length.Should().Be(size);

                var newSize = size;
                while (newSize > 4)
                {
                    newSize = (newSize >> 1) + 1;

                    var bufferCurrent = ThreadStaticBuffer.GetBuffer(newSize);
                    bufferCurrent.Should().BeSameAs(buffer);
                }

                allBuffers.Add(buffer);
            });

            var buffersDistinct = allBuffers.Distinct().ToArray();

            allBuffers.Count.Should().Be(threadsCount);
            buffersDistinct.Length.Should().Be(threadsCount);
        }
    }
}
