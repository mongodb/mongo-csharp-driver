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
using System.Linq;
using System.Reflection;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson.IO;
using Xunit;
using Xunit.Sdk;

namespace MongoDB.Bson.Tests.IO
{
    public class BufferCacheTests
    {
        // static fields
        private static readonly int[] __sizes = { 1, 2, 3, 4, 7, 8, 9, 15, 16, 17, 127, 128, 129, 8191, 8192 };

        [Fact]
        public void TestBufferSizeIncreasing_expected_powerof2()
        {
            ExecuteOnNewThread(1, _ =>
            {
                foreach (var requestedSize in __sizes)
                {
                    var expectedSize = Math.Max(16, 1 << (int)Math.Ceiling(Math.Log(requestedSize, 2)));

                    var buffer = BufferCache.GetBuffer(requestedSize);
                    buffer.Length.Should().Be(expectedSize);
                }
            });
        }

        [Theory]
        [InlineData(8193)]
        [InlineData(10000)]
        [InlineData(16384)]
        [InlineData(32767)]
        [InlineData(32769)]
        public void TestBufferSizeGreaterThanMaxSize_expected_exact_size(int requestedSize)
        {
            var buffer = BufferCache.GetBuffer(requestedSize);
            buffer.Length.Should().Be(requestedSize);
        }

        [Fact]
        public void TestBufferMultiThreaded_expected_unique_instance_per_thread()
        {
            const int threadsCount = 10;
            const int size = 256;

            var allBuffers = new ConcurrentBag<byte[]>();

            ExecuteOnNewThread(threadsCount, i =>
            {
                var buffer = BufferCache.GetBuffer(size);
                buffer.Length.Should().Be(size);

                var newSize = size;
                while (newSize > 4)
                {
                    newSize = (newSize >> 1) + 1;

                    var bufferCurrent = BufferCache.GetBuffer(newSize);
                    bufferCurrent.Should().BeSameAs(buffer);
                }

                buffer[0] = (byte)i;
                allBuffers.Add(buffer);
            });

            var buffersOrdered = allBuffers.OrderBy(b => b[0]).ToArray();
            var buffersDistinct = buffersOrdered.Distinct().ToArray();

            buffersOrdered.Length.Should().Be(threadsCount);
            buffersOrdered.ShouldAllBeEquivalentTo(buffersDistinct);

            GetBuffersCount().Should().BeGreaterOrEqualTo(threadsCount);
        }

        [Fact]
        public void TestBufferMultiThreaded_should_not_cache_buffers()
        {
            var currCount = GetBuffersCount();

            const int newBufferCount = 1024;
            const int threadsCount = 4;
            const int size = 256;

            SetBuffersCount(newBufferCount);

            ExecuteOnNewThread(threadsCount, _ =>
            {
                var buffer = BufferCache.GetBuffer(size);
                buffer.Length.Should().Be(size);

                var bufferNew = BufferCache.GetBuffer(size);
                bufferNew.Length.Should().Be(size);
                bufferNew.Should().NotBeSameAs(buffer);
            });

            var newCount = GetBuffersCount();
            newCount.Should().Be(newBufferCount + threadsCount * 2);

            SetBuffersCount(currCount);
        }

        // private methods
        private void ExecuteOnNewThread(int threadsCount, Action<int> action)
        {
            var threads = Enumerable.Range(0, threadsCount).Select(i =>
            {
                var thread = new Thread(_ => action(i));
                thread.Start();

                return thread;
            }).ToArray();


            foreach (var thread in threads)
            {
                if (!thread.Join(10000))
                    throw new TimeoutException();
            }
        }

        private FieldInfo BuffersCountField() =>
            typeof(BufferCache).GetField("__buffersCount", BindingFlags.NonPublic | BindingFlags.Static);

        private int GetBuffersCount() =>
            (int)BuffersCountField().GetValue(null);

        private void SetBuffersCount(int count) =>
            BuffersCountField().SetValue(null, count);
    }
}
