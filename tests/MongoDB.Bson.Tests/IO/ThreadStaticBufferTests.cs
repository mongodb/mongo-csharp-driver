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
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests.IO
{
    public class ThreadStaticBufferTests
    {
        public static readonly IEnumerable<object[]> IncrementalSizeTestData = new List<object[]>
        {
            new object[]
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
        public void RentBuffer_should_throw_when_size_is_invalid(int size)
        {
            var exception = Record.Exception(() => ThreadStaticBuffer.RentBuffer(size));
            var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;

            e.ParamName.Should().Be("size");
        }

        [Fact]
        public void RentBuffer_should_allow_checkout_after_checkin()
        {
            using (var rentableBufferFirst = ThreadStaticBuffer.RentBuffer(2))
            {
                rentableBufferFirst.Bytes.Should().NotBeNull();
            }

            using var rentableBufferSecond = ThreadStaticBuffer.RentBuffer(2);
            rentableBufferSecond.Bytes.Should().NotBeNull();
        }

        [Fact]
        public void RentBuffer_should_not_allow_checkout_without_checkin()
        {
            using (var rentedBuffer = ThreadStaticBuffer.RentBuffer(2))
            {
                var exception = Record.Exception(() => ThreadStaticBuffer.RentBuffer(2));
                exception.Should().BeOfType<InvalidOperationException>();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void RentBuffer_should_not_allow_checkin_buffer_from_other_thread(
            [Values(true, false)] bool rentBufferInOwnThread,
            [Values(128, 16385)] int size)
        {
            ThreadStaticBuffer.RentedBuffer rentedBufferInOwnThread = default;
            ThreadStaticBuffer.RentedBuffer rentedBufferInOtherThread = default;

            if (rentBufferInOwnThread)
            {
                rentedBufferInOwnThread = ThreadStaticBuffer.RentBuffer(size);
            }

            ThreadingUtilities.ExecuteOnNewThreads(1, i =>
            {
                rentedBufferInOtherThread = ThreadStaticBuffer.RentBuffer(size);
            });

            var exception = Record.Exception(() => rentedBufferInOtherThread.Dispose());
            exception.Should().BeOfType<InvalidOperationException>();

            if (rentBufferInOwnThread)
            {
                rentedBufferInOwnThread.Dispose();
            }
        }

        [Theory]
        [MemberData(nameof(IncrementalSizeTestData))]
        public void RentBuffer_incrementally_growing_buffer_size_expected_powerof2(params (int requestedSize, int expectedSize)[] sizes)
        {
            ThreadingUtilities.ExecuteOnNewThreads(2, _ =>
            {
                foreach (var (requestedSize, expectedSize) in sizes)
                {
                    using var rentedBuffer = ThreadStaticBuffer.RentBuffer(requestedSize);

                    rentedBuffer.Bytes.Length.Should().Be(expectedSize);
                }
            });
        }

        [Theory]
        [InlineData(16384)]
        [InlineData(16385)]
        [InlineData(16386)]
        [InlineData(32767)]
        [InlineData(32769)]
        public void RentBuffer_should_return_exact_size_when_requested_greater_than_maxsize(int requestedSize)
        {
            using var rentedBuffer = ThreadStaticBuffer.RentBuffer(requestedSize);
            rentedBuffer.Bytes.Length.Should().Be(requestedSize);
        }

        [Fact]
        public void RentBuffer_should_return_different_instance_in_different_threads()
        {
            const int threadsCount = 2;
            const int size = 256;

            var allBuffers = new ConcurrentBag<byte[]>();

            ThreadingUtilities.ExecuteOnNewThreads(threadsCount, i =>
            {
                byte[] bufferInstance;
                using (var rentedBuffer = ThreadStaticBuffer.RentBuffer(size))
                {
                    rentedBuffer.Bytes.Length.Should().Be(size);
                    bufferInstance = rentedBuffer.Bytes;
                }

                var newSize = size;
                while (newSize > 4)
                {
                    newSize = (newSize >> 1) + 1;

                    using var bufferCurrent = ThreadStaticBuffer.RentBuffer(newSize);
                    bufferCurrent.Bytes.Should().BeSameAs(bufferInstance);
                }

                allBuffers.Add(bufferInstance);
            });

            allBuffers.Distinct().Should().HaveCount(threadsCount);
        }
    }
}
