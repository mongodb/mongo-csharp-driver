/* Copyright 2010-2015 MongoDB Inc.
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
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson.IO;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Bson.Tests.IO
{
    [TestFixture]
    public class BsonChunkPoolTests
    {
        [Test]
        public void ChunkSize_get_should_return_expected_result()
        {
            var subject = new BsonChunkPool(1, 16);

            var result = subject.ChunkSize;

            result.Should().Be(16);
        }

        [Test]
        public void constructor_should_initialize_subject()
        {
            var maxChunkCount = 1;
            var chunkSize = 16;

            var subject = new BsonChunkPool(maxChunkCount, chunkSize);

            var reflector = new Reflector(subject);
            subject.MaxChunkCount.Should().Be(maxChunkCount);
            subject.ChunkSize.Should().Be(chunkSize);
            reflector._disposed.Should().BeFalse();
        }

        [Test]
        public void constructor_should_throw_chunkSize_is_less_than_zero(
            [Values(-1, 0)]
            int chunkSize)
        {
            Action action = () => new BsonChunkPool(1, chunkSize);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("chunkSize");
        }

        [Test]
        public void constructor_should_throw_when_MaxChunkCount_is_less_than_zero()
        {
            Action action = () => new BsonChunkPool(-1, 16);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("maxChunkCount");
        }

        [Test]
        public void Default_get_should_return_expected_result()
        {
            var result = BsonChunkPool.Default;

            result.ChunkSize.Should().Be(64 * 1024);
            result.MaxChunkCount.Should().Be(8192);
        }

        [Test]
        public void Default_set_should_have_expected_effect()
        {
            var originalDefaultPool = BsonChunkPool.Default;
            try
            {
                var newDefaultPool = new BsonChunkPool(1, 16);

                BsonChunkPool.Default = newDefaultPool;

                BsonChunkPool.Default.Should().BeSameAs(newDefaultPool);
            }
            finally
            {
                BsonChunkPool.Default = originalDefaultPool;
            }
        }

        [Test]
        public void Default_set_should_throw_when_value_is_null()
        {
            Action action = () => BsonChunkPool.Default = null;

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("value");
        }

        [Test]
        public void Dispose_can_be_called_more_than_once()
        {
            var subject = new BsonChunkPool(1, 16);

            subject.Dispose();
            subject.Dispose();
        }

        [Test]
        public void Dispose_should_dispose_subject()
        {
            var subject = new BsonChunkPool(1, 16);

            subject.Dispose();

            var reflector = new Reflector(subject);
            reflector._disposed.Should().BeTrue();
        }

        [Test]
        public void GetChunk_should_return_expected_result(
            [Values(1, 15, 16, 17, 32)]
            int requestedSize)
        {
            var subject = new BsonChunkPool(1, 16);

            var result = subject.GetChunk(requestedSize);

            result.Bytes.Count.Should().Be(16);
        }

        [Test]
        public void GetChunk_should_return_pooled_chunk_when_one_is_availabe()
        {
            var subject = new BsonChunkPool(1, 16);
            var pooledChunk = subject.GetChunk(1);
            var expectedArray = pooledChunk.Bytes.Array;
            var expectedOffset = pooledChunk.Bytes.Offset;
            pooledChunk.Dispose();

            var result = subject.GetChunk(1);

            result.Bytes.Array.Should().BeSameAs(expectedArray);
            result.Bytes.Offset.Should().Be(expectedOffset);
            result.Bytes.Count.Should().Be(16);
        }

        [Test]
        public void GetChunk_should_throw_when_subject_is_disposed()
        {
            var subject = new BsonChunkPool(1, 16);
            subject.Dispose();

            Action action = () => subject.GetChunk(1);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonChunkPool");
        }

        [Test]
        public void MaxChunkCount_get_should_return_expected_result()
        {
            var subject = new BsonChunkPool(1, 16);

            var result = subject.MaxChunkCount;

            result.Should().Be(1);
        }

        // nested types
        private class Reflector
        {
            private readonly BsonChunkPool _instance;

            public Reflector(BsonChunkPool instance)
            {
                _instance = instance;
            }

            public bool _disposed
            {
                get
                {
                    var field = typeof(BsonChunkPool).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (bool)field.GetValue(_instance);
                }
            }
        }
    }

    [TestFixture]
    public class BsonChunkPool_DisposableChunkTests
    {
        [Test]
        public void Bytes_get_should_return_expected_result()
        {
            var pool = new BsonChunkPool(1, 16);
            var subject = pool.GetChunk(1);

            var result = subject.Bytes;

            result.Array.Length.Should().Be(16);
            result.Offset.Should().Be(0);
            result.Count.Should().Be(16);
        }

        [Test]
        public void Bytes_get_should_throw_when_subject_is_disposed()
        {
            var pool = new BsonChunkPool(1, 16);
            var subject = pool.GetChunk(1);
            subject.Dispose();

            Action action = () => { var _ = subject.Bytes; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("DisposableChunk");
        }

        [Test]
        public void Dispose_can_be_called_more_than_once()
        {
            var pool = new BsonChunkPool(1, 16);
            var subject = pool.GetChunk(1);

            subject.Dispose();
            subject.Dispose();
        }

        [Test]
        public void Dispose_should_dispose_subject()
        {
            var pool = new BsonChunkPool(1, 16);
            var subject = pool.GetChunk(1);

            subject.Dispose();

            var reflector = new Reflector(subject);
            reflector._disposed.Should().BeTrue();
        }

        [Test]
        public void Dispose_should_return_chunk_to_the_pool()
        {
            var pool = new BsonChunkPool(1, 16);
            var subject = pool.GetChunk(1);

            subject.Dispose();

            pool.ChunkCount.Should().Be(1);
        }

        [Test]
        public void Dispose_should_return_chunk_to_the_pool_after_all_handles_have_been_disposed(
            [Values(0, 1, 2, 3, 4, 16)]
            int numberOfHandles,
            [Values(false, true)]
            bool disposeSubjectLast)
        {
            var pool = new BsonChunkPool(1, 16);
            var subject = pool.GetChunk(1);

            var handles = new List<IBsonChunk>();
            for (var n = 0; n < numberOfHandles; n++)
            {
                handles.Add(subject.Fork());
            }

            if (disposeSubjectLast)
            {
                handles.Add(subject);
            }
            else
            {
                handles.Insert(0, subject);
            }

            foreach (var handle in handles)
            {
                pool.ChunkCount.Should().Be(0);
                handle.Dispose();
            }

            pool.ChunkCount.Should().Be(1);
        }

        [Test]
        public void Fork_should_return_expected_result()
        {
            var pool = new BsonChunkPool(1, 16);
            var subject = pool.GetChunk(1);

            var handle = subject.Fork();

            handle.Bytes.Array.Should().BeSameAs(subject.Bytes.Array);
            handle.Bytes.Offset.Should().Be(subject.Bytes.Offset);
            handle.Bytes.Count.Should().Be(subject.Bytes.Count);
        }

        [Test]
        public void Fork_should_throw_when_subject_is_disposed()
        {
            var pool = new BsonChunkPool(1, 16);
            var subject = pool.GetChunk(1);
            subject.Dispose();

            Action action = () => subject.Fork();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("DisposableChunk");
        }

        // nested types
        private class Reflector
        {
            private readonly IBsonChunk _instance;

            public Reflector(IBsonChunk instance)
            {
                _instance = instance;
            }

            public bool _disposed
            {
                get
                {
                    var field = _instance.GetType().GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (bool)field.GetValue(_instance);
                }
            }
        }
    }
}
