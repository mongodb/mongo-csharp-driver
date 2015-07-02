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
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.TestHelpers;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Bson.Tests.IO
{
    [TestFixture]
    public class MultiChunkBufferTests
    {
        [TestCase(0, 0, 0, 1)]
        [TestCase(1, 1, 0, 2)]
        [TestCase(2, 1, 1, 1)]
        [TestCase(3, 2, 0, 3)]
        [TestCase(4, 2, 1, 2)]
        [TestCase(5, 2, 2, 1)]
        [TestCase(6, 2, 3, 0)]
        public void AccessBackingBytes_should_return_expected_result(int position, int expectedChunkIndex, int expectedOffset, int expectedCount)
        {
            var chunks = new[] { new byte[] { 1 }, new byte[] { 2, 3 }, new byte[] { 4, 5, 6 } };
            var subject = CreateSubject(chunks);

            var result = subject.AccessBackingBytes(position);

            result.Array.Should().BeSameAs(chunks[expectedChunkIndex]);
            result.Offset.Should().Be(expectedOffset);
            result.Count.Should().Be(expectedCount);
        }

        [Test]
        public void AccessBackingBytes_should_return_expected_result_when_there_are_zero_chunks()
        {
            var chunkSource = Substitute.For<IBsonChunkSource>();
            var subject = new MultiChunkBuffer(chunkSource);

            var result = subject.AccessBackingBytes(0);

            result.Array.Should().HaveCount(0);
            result.Offset.Should().Be(0);
            result.Count.Should().Be(0);
        }

        [Test]
        public void AccessBackingBytes_should_throw_when_position_is_invalid(
            [Values(-1, 3)]
            int position)
        {
            var subject = CreateSubject(2);

            Action action = () => subject.AccessBackingBytes(position);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("position");
        }

        [Test]
        public void AccessBackingBytes_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.AccessBackingBytes(0);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("MultiChunkBuffer");
        }

        [TestCase(false, 2)]
        [TestCase(true, 1)]
        public void Capacity_get_should_return_expected_result(bool isReadOnly, int expectedResult)
        {
            var subject = CreateSubject(2, 1, isReadOnly);

            var result = subject.Capacity;

            result.Should().Be(expectedResult);
        }

        [Test]
        public void Capacity_get_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => { var _ = subject.Capacity; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("MultiChunkBuffer");
        }

        [Test]
        public void ChunkSource_get_should_return_expected_result(
            [Values(false, true)]
            bool disposed)
        {
            var chunkSource = Substitute.For<IBsonChunkSource>();
            var subject = new MultiChunkBuffer(chunkSource);
            if (disposed)
            {
                subject.Dispose();
            }

            var result = subject.ChunkSource;

            result.Should().BeSameAs(chunkSource);
        }

        [TestCase(0, 0, new byte[] { 1, 2, 3, 4, 5, 6 })]
        [TestCase(0, 1, new byte[] { 0, 2, 3, 4, 5, 6 })]
        [TestCase(0, 2, new byte[] { 0, 0, 3, 4, 5, 6 })]
        [TestCase(0, 3, new byte[] { 0, 0, 0, 4, 5, 6 })]
        [TestCase(0, 4, new byte[] { 0, 0, 0, 0, 5, 6 })]
        [TestCase(0, 5, new byte[] { 0, 0, 0, 0, 0, 6 })]
        [TestCase(0, 6, new byte[] { 0, 0, 0, 0, 0, 0 })]
        [TestCase(1, 0, new byte[] { 1, 2, 3, 4, 5, 6 })]
        [TestCase(1, 1, new byte[] { 1, 0, 3, 4, 5, 6 })]
        [TestCase(1, 2, new byte[] { 1, 0, 0, 4, 5, 6 })]
        [TestCase(1, 3, new byte[] { 1, 0, 0, 0, 5, 6 })]
        [TestCase(1, 4, new byte[] { 1, 0, 0, 0, 0, 6 })]
        [TestCase(1, 5, new byte[] { 1, 0, 0, 0, 0, 0 })]
        [TestCase(2, 0, new byte[] { 1, 2, 3, 4, 5, 6 })]
        [TestCase(2, 1, new byte[] { 1, 2, 0, 4, 5, 6 })]
        [TestCase(2, 2, new byte[] { 1, 2, 0, 0, 5, 6 })]
        [TestCase(2, 3, new byte[] { 1, 2, 0, 0, 0, 6 })]
        [TestCase(2, 4, new byte[] { 1, 2, 0, 0, 0, 0 })]
        [TestCase(3, 0, new byte[] { 1, 2, 3, 4, 5, 6 })]
        [TestCase(3, 1, new byte[] { 1, 2, 3, 0, 5, 6 })]
        [TestCase(3, 2, new byte[] { 1, 2, 3, 0, 0, 6 })]
        [TestCase(3, 3, new byte[] { 1, 2, 3, 0, 0, 0 })]
        [TestCase(4, 0, new byte[] { 1, 2, 3, 4, 5, 6 })]
        [TestCase(4, 1, new byte[] { 1, 2, 3, 4, 0, 6 })]
        [TestCase(4, 2, new byte[] { 1, 2, 3, 4, 0, 0 })]
        [TestCase(5, 0, new byte[] { 1, 2, 3, 4, 5, 6 })]
        [TestCase(5, 1, new byte[] { 1, 2, 3, 4, 5, 0 })]
        [TestCase(6, 0, new byte[] { 1, 2, 3, 4, 5, 6 })]
        public void Clear_should_have_expected_effect(int position, int count, byte[] expectedBytes)
        {
            var chunks = new[] { new byte[] { 1 }, new byte[] { 2, 3 }, new byte[] { 4, 5, 6 } };
            var subject = CreateSubject(chunks);

            subject.Clear(position, count);

            ToByteArray(subject).Should().Equal(expectedBytes);
        }

        [TestCase(0, -1)]
        [TestCase(0, 3)]
        [TestCase(1, 2)]
        [TestCase(2, 1)]
        public void Clear_should_throw_when_count_is_invalid(int position, int count)
        {
            var subject = CreateSubject(2);

            Action action = () => subject.Clear(position, count);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("count");
        }

        [Test]
        public void Clear_should_throw_when_position_is_invalid(
            [Values(-1, 3)]
            int position)
        {
            var subject = CreateSubject(2);

            Action action = () => subject.Clear(position, 0);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("position");
        }

        [Test]
        public void Clear_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.Clear(0, 0);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("MultiChunkBuffer");
        }

        [Test]
        public void Clear_should_throw_when_subject_is_read_only()
        {
            var subject = CreateSubject(isReadOnly: true);

            Action action = () => subject.Clear(0, 0);

            action.ShouldThrow<InvalidOperationException>();
        }

        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(2, 3)]
        public void constructor_with_chunks_should_compute_capacity(int numberOfChunks, int expectedCapacity)
        {
            var chunkSizes = Enumerable.Range(1, numberOfChunks);
            var chunks = CreateChunks(chunkSizes);

            var subject = new MultiChunkBuffer(chunks);

            subject.Capacity.Should().Be(expectedCapacity);
        }

        [TestCase(0, new int[] { 0 })]
        [TestCase(1, new int[] { 0, 1 })]
        [TestCase(2, new int[] { 0, 1, 3 })]
        public void constructor_with_chunks_should_compute_positions(int numberOfChunks, int[] expectedPositions)
        {
            var chunkSizes = Enumerable.Range(1, numberOfChunks);
            var chunks = CreateChunks(chunkSizes);

            var subject = new MultiChunkBuffer(chunks);

            var reflector = new Reflector(subject);
            reflector._positions.Should().Equal(expectedPositions);
        }

        [Test]
        public void constructor_with_chunks_should_default_isReadOnly_to_false()
        {
            var chunks = Enumerable.Empty<IBsonChunk>();

            var subject = new MultiChunkBuffer(chunks);

            subject.IsReadOnly.Should().BeFalse();
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void constructor_with_chunks_should_default_length_to_capacity(int numberOfChunks)
        {
            var chunkSizes = Enumerable.Range(1, numberOfChunks);
            var chunks = CreateChunks(chunkSizes);

            var subject = new MultiChunkBuffer(chunks);

            subject.Length.Should().Be(subject.Capacity);
        }

        [Test]
        public void constructor_with_chunks_should_initialize_subject()
        {
            var chunks = Enumerable.Empty<IBsonChunk>();

            var subject = new MultiChunkBuffer(chunks, 0, false);

            var reflector = new Reflector(subject);
            subject.Capacity.Should().Be(0);
            subject.ChunkSource.Should().BeNull();
            subject.IsReadOnly.Should().BeFalse();
            subject.Length.Should().Be(0);
            reflector._chunks.Should().HaveCount(0);
            reflector._disposed.Should().BeFalse();
            reflector._positions.Should().Equal(new[] { 0 });
        }

        [Test]
        public void constructor_with_chunks_should_throw_when_chunks_is_null()
        {
            IEnumerable<IBsonChunk> chunks = null;

            Action action = () => new MultiChunkBuffer(chunks);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("chunks");
        }

        [Test]
        public void constructor_with_chunks_should_throw_when_length_is_invalid(
            [Values(-1, 4)]
            int length)
        {
            var chunkSizes = new[] { 1, 2 };
            var chunks = CreateChunks(chunkSizes);

            Action action = () => new MultiChunkBuffer(chunks, length);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("length");
        }

        [Test]
        public void constructor_with_chunkSource_should_initialize_subject()
        {
            var chunkSource = Substitute.For<IBsonChunkSource>();

            var subject = new MultiChunkBuffer(chunkSource);

            var reflector = new Reflector(subject);
            subject.Capacity.Should().Be(0);
            subject.ChunkSource.Should().BeSameAs(chunkSource);
            subject.IsReadOnly.Should().BeFalse();
            subject.Length.Should().Be(0);
            reflector._chunks.Should().HaveCount(0);
            reflector._disposed.Should().BeFalse();
            reflector._positions.Should().Equal(new[] { 0 });
        }

        [Test]
        public void constructor_with_chunkSource_should_throw_when_chunkSource_is_null()
        {
            IBsonChunkSource chunkSource = null;

            Action action = () => new MultiChunkBuffer(chunkSource);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("chunkSource");
        }

        [Test]
        public void Dispose_can_be_called_multiple_times()
        {
            var subject = CreateSubject();

            subject.Dispose();
            subject.Dispose();
        }

        [Test]
        public void Dispose_should_dispose_chunks(
            [Values(0, 1, 2, 3)]
            int numberOfChunks)
        {
            var chunks = Enumerable.Range(1, numberOfChunks).Select(_ => Substitute.For<IBsonChunk>()).ToList();
            var subject = new MultiChunkBuffer(chunks);

            subject.Dispose();

            foreach (var chunk in chunks)
            {
                chunk.Received(1).Dispose();
            }
        }

        [Test]
        public void Dispose_should_dispose_subject()
        {
            var subject = CreateSubject();

            subject.Dispose();

            var reflector = new Reflector(subject);
            reflector._disposed.Should().BeTrue();
        }

        [TestCase(0, new int[] { })]
        [TestCase(1, new int[] { 1 })]
        [TestCase(2, new int[] { 1, 2 })]
        [TestCase(3, new int[] { 1, 2 })]
        [TestCase(4, new int[] { 1, 2, 3 })]
        [TestCase(5, new int[] { 1, 2, 3 })]
        [TestCase(6, new int[] { 1, 2, 3 })]
        [TestCase(7, new int[] { 1, 2, 3, 4 })]
        public void EnsureCapacity_should_have_expected_effect(int minimumCapacity, int[] expectedChunkSizes)
        {
            var chunkSource = Substitute.For<IBsonChunkSource>();
            var subject = new MultiChunkBuffer(chunkSource);
            var chunkSize = 1;
            chunkSource.GetChunk(Arg.Any<int>()).Returns(x => new ByteArrayChunk(chunkSize++));

            subject.EnsureCapacity(minimumCapacity);

            var reflector = new Reflector(subject);
            subject.Capacity.Should().BeGreaterOrEqualTo(minimumCapacity);
            reflector._chunks.Select(c => c.Bytes.Count).Should().Equal(expectedChunkSizes);
        }

        [Test]
        public void EnsureCapacity_should_throw_when_chunkSource_is_null()
        {
            var subject = CreateSubject(0);

            Action action = () => subject.EnsureCapacity(1);

            action.ShouldThrow<InvalidOperationException>();
        }

        [Test]
        public void EnsureCapacity_should_throw_when_minimumCapacity_is_invalid()
        {
            var subject = CreateSubject(2);

            Action action = () => subject.EnsureCapacity(-1);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("minimumCapacity");
        }

        [Test]
        public void EnsureCapacity_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.EnsureCapacity(1);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("MultiChunkBuffer");
        }

        [Test]
        public void EnsureCapacity_should_throw_when_subject_is_read_only()
        {
            var subject = CreateSubject(isReadOnly: true);

            Action action = () => subject.EnsureCapacity(1);

            action.ShouldThrow<InvalidOperationException>();
        }

        [Test]
        [Requires64BitProcess]
        public void ExpandCapacity_should_throw_when_expanded_capacity_exceeds_2GB()
        {
            using (var subject = new MultiChunkBuffer(BsonChunkPool.Default))
            {
                subject.EnsureCapacity(int.MaxValue - 128 * 1024 * 1024);

                Action action = () => subject.EnsureCapacity(int.MaxValue); // indirectly calls private ExpandCapacity method

                action.ShouldThrow<InvalidOperationException>();
            }
        }

        [TestCase(0, 1)]
        [TestCase(1, 2)]
        [TestCase(2, 3)]
        [TestCase(3, 4)]
        [TestCase(4, 5)]
        [TestCase(5, 6)]
        public void GetByte_should_return_expected_result(int position, byte expectedResult)
        {
            var chunks = new[] { new byte[] { 1 }, new byte[] { 2, 3 }, new byte[] { 4, 5, 6 } };
            var subject = CreateSubject(chunks);

            var result = subject.GetByte(position);

            result.Should().Be(expectedResult);
        }

        [Test]
        public void GetByte_should_throw_when_position_is_invalid(
            [Values(-1, 3)]
            int position)
        {
            var subject = CreateSubject(2);

            Action action = () => subject.GetByte(position);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("position");
        }

        [Test]
        public void GetByte_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.GetByte(0);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("MultiChunkBuffer");
        }

        [TestCase(1, new byte[] { 0, 1, 2, 0 })]
        [TestCase(2, new byte[] { 0, 0, 1, 2 })]
        public void GetBytes_should_have_expected_effect_for_offset(int offset, byte[] expectedBytes)
        {
            var chunks = new[] { new byte[] { 1 }, new byte[] { 2, 3 } };
            var subject = CreateSubject(chunks);
            var destination = new byte[4];

            subject.GetBytes(0, destination, offset, 2);

            destination.Should().Equal(expectedBytes);
        }

        [TestCase(0, 0, new byte[] { })]
        [TestCase(0, 1, new byte[] { 1 })]
        [TestCase(0, 2, new byte[] { 1, 2 })]
        [TestCase(0, 3, new byte[] { 1, 2, 3 })]
        [TestCase(0, 4, new byte[] { 1, 2, 3, 4 })]
        [TestCase(0, 5, new byte[] { 1, 2, 3, 4, 5 })]
        [TestCase(0, 6, new byte[] { 1, 2, 3, 4, 5, 6})]
        [TestCase(1, 0, new byte[] { })]
        [TestCase(1, 1, new byte[] { 2 })]
        [TestCase(1, 2, new byte[] { 2, 3 })]
        [TestCase(1, 3, new byte[] { 2, 3, 4  })]
        [TestCase(1, 4, new byte[] { 2, 3, 4, 5 })]
        [TestCase(1, 5, new byte[] { 2, 3, 4, 5, 6 })]
        [TestCase(2, 0, new byte[] { })]
        [TestCase(2, 1, new byte[] { 3 })]
        [TestCase(2, 2, new byte[] { 3, 4 })]
        [TestCase(2, 3, new byte[] { 3, 4, 5 })]
        [TestCase(2, 4, new byte[] { 3, 4, 5, 6 })]
        [TestCase(3, 0, new byte[] { })]
        [TestCase(3, 1, new byte[] { 4 })]
        [TestCase(3, 2, new byte[] { 4, 5 })]
        [TestCase(3, 3, new byte[] { 4, 5, 6 })]
        [TestCase(4, 0, new byte[] { })]
        [TestCase(4, 1, new byte[] { 5 })]
        [TestCase(4, 2, new byte[] { 5, 6 })]
        [TestCase(5, 0, new byte[] { })]
        [TestCase(5, 1, new byte[] { 6 })]
        [TestCase(6, 0, new byte[] { })]
        public void GetBytes_should_expected_effect_for_position_and_count(int position, int count, byte[] expectedBytes)
        {
            var chunks = new[] { new byte[] { 1 }, new byte[] { 2, 3 }, new byte[] { 4, 5, 6 } };
            var subject = CreateSubject(chunks);
            var destination = new byte[count];

            subject.GetBytes(position, destination, 0, count);

            destination.Should().Equal(expectedBytes);
        }

        [TestCase(0, -1)]
        [TestCase(0, 3)]
        [TestCase(1, 2)]
        [TestCase(2, 1)]
        public void GetBytes_should_throw_when_count_is_invalid_for_buffer(int position, int count)
        {
            var subject = CreateSubject(2);
            var destination = new byte[3];

            Action action = () => subject.GetBytes(position, destination, 0, count);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("count");
        }

        [TestCase(0, -1)]
        [TestCase(0, 3)]
        [TestCase(1, 2)]
        [TestCase(2, 1)]
        public void GetBytes_should_throw_when_count_is_invalid_for_destination(int offset, int count)
        {
            var subject = CreateSubject(3);
            var destination = new byte[2];

            Action action = () => subject.GetBytes(0, destination, offset, count);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("count");
        }

        [Test]
        public void GetBytes_should_throw_when_destination_is_null()
        {
            var subject = CreateSubject();

            Action action = () => subject.GetBytes(0, null, 0, 0);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("destination");
        }

        [Test]
        public void GetBytes_should_throw_when_offset_is_invalid(
            [Values(-1, 3)]
            int offset)
        {
            var subject = CreateSubject(4);
            var destination = new byte[2];

            Action action = () => subject.GetBytes(0, destination, offset, 0);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("offset");
        }

        [Test]
        public void GetBytes_should_throw_when_position_is_invalid(
             [Values(-1, 3)]
           int position)
        {
            var subject = CreateSubject(2);
            var destination = new byte[0];

            Action action = () => subject.GetBytes(position, destination, 0, 0);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("position");
        }

        [Test]
        public void GetBytes_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();
            var destination = new byte[0];

            Action action = () => subject.GetBytes(0, destination, 0, 0);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("MultiChunkBuffer");
        }

        [TestCase(0, 0, new byte[] { })]
        [TestCase(0, 1, new byte[] { 1 })]
        [TestCase(0, 2, new byte[] { 1, 2 })]
        [TestCase(0, 3, new byte[] { 1, 2, 3 })]
        [TestCase(0, 4, new byte[] { 1, 2, 3, 4 })]
        [TestCase(0, 5, new byte[] { 1, 2, 3, 4, 5 })]
        [TestCase(0, 6, new byte[] { 1, 2, 3, 4, 5, 6 })]
        [TestCase(1, 0, new byte[] { })]
        [TestCase(1, 1, new byte[] { 2 })]
        [TestCase(1, 2, new byte[] { 2, 3 })]
        [TestCase(1, 3, new byte[] { 2, 3, 4 })]
        [TestCase(1, 4, new byte[] { 2, 3, 4, 5 })]
        [TestCase(1, 5, new byte[] { 2, 3, 4, 5, 6 })]
        [TestCase(2, 0, new byte[] { })]
        [TestCase(2, 1, new byte[] { 3 })]
        [TestCase(2, 2, new byte[] { 3, 4 })]
        [TestCase(2, 3, new byte[] { 3, 4, 5 })]
        [TestCase(2, 4, new byte[] { 3, 4, 5, 6 })]
        [TestCase(3, 0, new byte[] { })]
        [TestCase(3, 1, new byte[] { 4 })]
        [TestCase(3, 2, new byte[] { 4, 5 })]
        [TestCase(3, 3, new byte[] { 4, 5, 6 })]
        [TestCase(4, 0, new byte[] { })]
        [TestCase(4, 1, new byte[] { 5 })]
        [TestCase(4, 2, new byte[] { 5, 6 })]
        [TestCase(5, 0, new byte[] { })]
        [TestCase(5, 1, new byte[] { 6 })]
        [TestCase(6, 0, new byte[] { })]
        public void GetSlice_should_return_expected_result(int position, int length, byte[] expectedBytes)
        {
            var chunks = new[] { new byte[] { 1 }, new byte[] { 2, 3 }, new byte[] { 4, 5, 6 } };
            var subject = CreateSubject(chunks, isReadOnly: true);

            var result = subject.GetSlice(position, length);

            ToByteArray(result).Should().Equal(expectedBytes);
        }

        [Test]
        public void GetSlice_should_return_slice_that_does_not_dispose_subject_when_slice_is_disposed()
        {
            var chunks = new[] { new byte[] { 1, 2, 3 } };
            var subject = CreateSubject(chunks, isReadOnly: true);
            var slice = subject.GetSlice(1, 1);

            slice.Dispose();

            subject.GetByte(1).Should().Be(2);
        }

        [Test]
        public void GetSlice_should_return_slice_that_is_not_disposed_when_subject_is_disposed()
        {
            var chunks = new[] { new byte[] { 1, 2, 3 } };
            var subject = CreateSubject(chunks, isReadOnly: true);
            var slice = subject.GetSlice(1, 1);

            subject.Dispose();

            slice.GetByte(0).Should().Be(2);
        }

        [TestCase(0, -1)]
        [TestCase(0, 3)]
        [TestCase(1, 2)]
        [TestCase(2, 1)]
        public void GetSlice_should_throw_when_length_is_invalid(int position, int length)
        {
            var subject = CreateSubject(2, isReadOnly: true);

            Action action = () => subject.GetSlice(position, length);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("length");
        }

        [Test]
        public void GetSlice_should_throw_when_position_is_invalid(
            [Values(-1, 3)]
            int position)
        {
            var subject = CreateSubject(2, isReadOnly: true);

            Action action = () => subject.GetSlice(position, 0);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("position");
        }

        [Test]
        public void GetSlice_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.GetSlice(0, 0);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("MultiChunkBuffer");
        }

        [Test]
        public void GetSlice_should_throw_when_subject_is_not_read_only()
        {
            var subject = CreateSubject(isReadOnly: false);

            Action action = () => subject.GetSlice(0, 0);

            action.ShouldThrow<InvalidOperationException>();
        }

        [Test]
        public void IsReadOnly_get_should_return_expected_result(
            [Values(false, true)]
            bool isReadOnly)
        {
            var subject = CreateSubject(0, isReadOnly: isReadOnly);

            var result = subject.IsReadOnly;

            result.Should().Be(isReadOnly);
        }

        [Test]
        public void IsReadOnly_get_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => { var _ = subject.IsReadOnly; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("MultiChunkBuffer");
        }

        [Test]
        public void Length_get_should_return_expected_result(
            [Values(1, 2)]
            int length)
        {
            var subject = CreateSubject(2, length);

            var result = subject.Length;

            result.Should().Be(length);
        }

        [Test]
        public void Length_get_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => { var _ = subject.Length; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("MultiChunkBuffer");
        }

        [Test]
        public void Length_set_should_have_expected_effect(
            [Values(1, 2)]
            int length)
        {
            var subject = CreateSubject(2, 0);

            subject.Length = length;

            subject.Length.Should().Be(length);
        }

        [Test]
        public void Length_set_should_throw_when_subject_is_read_only()
        {
            var subject = CreateSubject(0, 0, isReadOnly: true);

            Action action = () => subject.Length = 0;

            action.ShouldThrow<InvalidOperationException>();
        }

        [TestCase(0, -1)]
        [TestCase(0, 1)]
        [TestCase(1, 2)]
        [TestCase(2, 3)]
        public void Length_set_should_throw_when_value_is_invalid(int size, int value)
        {
            var subject = CreateSubject(size, 0);

            Action action = () => subject.Length = value;

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("value");
        }

        [Test]
        public void Length_set_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.Length = 0;

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("MultiChunkBuffer");
        }

        [Test]
        public void MakeReadOnly_should_have_expected_effect(
            [Values(false, true)]
            bool isReadOnly)
        {
            var subject = CreateSubject(isReadOnly: isReadOnly);

            subject.MakeReadOnly();

            subject.IsReadOnly.Should().BeTrue();
        }

        [Test]
        public void MakeReadOnly_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.MakeReadOnly();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("MultiChunkBuffer");
        }

        [TestCase(0, new byte[] { 0, 2, 3, 4, 5, 6 })]
        [TestCase(1, new byte[] { 1, 0, 3, 4, 5, 6 })]
        [TestCase(2, new byte[] { 1, 2, 0, 4, 5, 6 })]
        [TestCase(3, new byte[] { 1, 2, 3, 0, 5, 6 })]
        [TestCase(4, new byte[] { 1, 2, 3, 4, 0, 6 })]
        [TestCase(5, new byte[] { 1, 2, 3, 4, 5, 0 })]
        public void SetByte_should_have_expected_effect(int position, byte[] expectedBytes)
        {
            var chunks = new[] { new byte[] { 1 }, new byte[] { 2, 3 }, new byte[] { 4, 5, 6 } };
            var subject = CreateSubject(chunks);

            subject.SetByte(position, 0);

            ToByteArray(subject).Should().Equal(expectedBytes);
        }

        [Test]
        public void SetByte_should_throw_when_position_is_invalid(
            [Values(-1, 3)]
            int position)
        {
            var subject = CreateSubject(2);

            Action action = () => subject.SetByte(position, 0);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("position");
        }

        [Test]
        public void SetByte_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.SetByte(0, 0);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("MultiChunkBuffer");
        }

        [Test]
        public void SetByte_should_throw_when_subject_is_read_only()
        {
            var subject = CreateSubject(1, isReadOnly: true);

            Action action = () => subject.SetByte(0, 0);

            action.ShouldThrow<InvalidOperationException>();
        }

        [TestCase(0, 0, 0, new byte[] { 1, 2, 3, 4, 5, 6 })]
        [TestCase(0, 0, 1, new byte[] { 7, 2, 3, 4, 5, 6 })]
        [TestCase(0, 0, 2, new byte[] { 7, 8, 3, 4, 5, 6 })]
        [TestCase(0, 0, 3, new byte[] { 7, 8, 9, 4, 5, 6 })]
        [TestCase(0, 1, 0, new byte[] { 1, 2, 3, 4, 5, 6 })]
        [TestCase(0, 1, 1, new byte[] { 8, 2, 3, 4, 5, 6 })]
        [TestCase(0, 1, 2, new byte[] { 8, 9, 3, 4, 5, 6 })]
        [TestCase(1, 0, 0, new byte[] { 1, 2, 3, 4, 5, 6 })]
        [TestCase(1, 0, 1, new byte[] { 1, 7, 3, 4, 5, 6 })]
        [TestCase(1, 0, 2, new byte[] { 1, 7, 8, 4, 5, 6 })]
        [TestCase(1, 0, 3, new byte[] { 1, 7, 8, 9, 5, 6 })]
        [TestCase(1, 1, 0, new byte[] { 1, 2, 3, 4, 5, 6 })]
        [TestCase(1, 1, 1, new byte[] { 1, 8, 3, 4, 5, 6 })]
        [TestCase(1, 1, 2, new byte[] { 1, 8, 9, 4, 5, 6 })]
        [TestCase(2, 0, 0, new byte[] { 1, 2, 3, 4, 5, 6 })]
        [TestCase(2, 0, 1, new byte[] { 1, 2, 7, 4, 5, 6 })]
        [TestCase(2, 0, 2, new byte[] { 1, 2, 7, 8, 5, 6 })]
        [TestCase(2, 0, 3, new byte[] { 1, 2, 7, 8, 9, 6 })]
        [TestCase(2, 1, 0, new byte[] { 1, 2, 3, 4, 5, 6 })]
        [TestCase(2, 1, 1, new byte[] { 1, 2, 8, 4, 5, 6 })]
        [TestCase(2, 1, 2, new byte[] { 1, 2, 8, 9, 5, 6 })]
        public void SetBytes_should_have_expected_effect(int position, int offset, int count, byte[] expectedBytes)
        {
            var chunks = new[] { new byte[] { 1 }, new byte[] { 2, 3 }, new byte[] { 4, 5, 6 } };
            var subject = CreateSubject(chunks);
            var source = new byte[] { 7, 8, 9 };

            subject.SetBytes(position, source, offset, count);

            ToByteArray(subject).Should().Equal(expectedBytes);
        }

        [TestCase(0, -1)]
        [TestCase(1, 2)]
        [TestCase(2, 1)]
        public void SetBytes_should_throw_when_count_is_invalid_for_buffer(int position, int count)
        {
            var subject = CreateSubject(2);
            var source = new byte[4];

            Action action = () => subject.SetBytes(position, source, 0, count);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("count");
        }

        [TestCase(0, -1)]
        [TestCase(1, 2)]
        [TestCase(2, 1)]
        public void SetBytes_should_throw_when_count_is_invalid_for_source(int offset, int count)
        {
            var subject = CreateSubject(2);
            var source = new byte[2];

            Action action = () => subject.SetBytes(0, source, offset, count);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("count");
        }

        [Test]
        public void SetBytes_should_throw_when_offset_is_invalid(
            [Values(-1, 3)]
            int offset)
        {
            var subject = CreateSubject(0);
            var source = new byte[2];

            Action action = () => subject.SetBytes(0, source, offset, 0);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("offset");
        }

        [Test]
        public void SetBytes_should_throw_when_position_is_invalid(
            [Values(-1, 3)]
            int position)
        {
            var subject = CreateSubject(2);
            var source = new byte[0];

            Action action = () => subject.SetBytes(position, source, 0, 0);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("position");
        }

        [Test]
        public void SetBytes_should_throw_when_source_is_null()
        {
            var subject = CreateSubject();

            Action action = () => subject.SetBytes(0, null, 0, 0);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("source");
        }

        [Test]
        public void SetBytes_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();
            var source = new byte[0];

            Action action = () => subject.SetBytes(0, source, 0, 0);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("MultiChunkBuffer");
        }

        [Test]
        public void SetBytes_should_throw_when_subject_is_read_only()
        {
            var subject = CreateSubject(isReadOnly: true);
            var source = new byte[0];

            Action action = () => subject.SetBytes(0, source, 0, 0);

            action.ShouldThrow<InvalidOperationException>();
        }

        // helper methods
        private IEnumerable<IBsonChunk> CreateChunks(IEnumerable<int> chunkSizes)
        {
            return chunkSizes.Select(s => new ByteArrayChunk(s));
        }

        private MultiChunkBuffer CreateDisposedSubject()
        {
            var subject = CreateSubject();
            subject.Dispose();
            return subject;
        }

        private MultiChunkBuffer CreateSubject(byte[][] chunks, int? length = null, bool isReadOnly = false)
        {
            return new MultiChunkBuffer(chunks.Select(c => new ByteArrayChunk(c)), length, isReadOnly);
        }

        private MultiChunkBuffer CreateSubject(int size = 0, int? length = null, bool isReadOnly = false)
        {
            var chunk = new ByteArrayChunk(size);
            return new MultiChunkBuffer(new[] { chunk }, length ?? size, isReadOnly);
        }

        private byte[] ToByteArray(IByteBuffer buffer)
        {
            var bytes = new byte[buffer.Length];
            buffer.GetBytes(0, bytes, 0, buffer.Length);
            return bytes;
        }

        // nested types
        private class Reflector
        {
            private readonly MultiChunkBuffer _instance;

            public Reflector(MultiChunkBuffer instance)
            {
                _instance = instance;
            }

            public List<IBsonChunk> _chunks
            {
                get
                {
                    var field = typeof(MultiChunkBuffer).GetField("_chunks", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (List<IBsonChunk>)field.GetValue(_instance);
                }
            }

            public bool _disposed
            {
                get
                {
                    var field = typeof(MultiChunkBuffer).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (bool)field.GetValue(_instance);
                }
            }

            public List<int> _positions
            {
                get
                {
                    var field = typeof(MultiChunkBuffer).GetField("_positions", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (List<int>)field.GetValue(_instance);
                }
            }
        }
    }
}
