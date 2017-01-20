/* Copyright 2010-2016 MongoDB Inc.
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
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Bson.Tests.IO
{
    public class SingleChunkBufferTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        public void AccessBackingBytes_should_return_expected_result_for_length(int length, int expectedCount)
        {
            var bytes = new byte[2];
            var subject = CreateSubject(bytes, length);

            var result = subject.AccessBackingBytes(0);

            result.Array.Should().BeSameAs(bytes);
            result.Offset.Should().Be(0);
            result.Count.Should().Be(expectedCount);
        }

        [Theory]
        [InlineData(0, 0, 2)]
        [InlineData(1, 1, 1)]
        [InlineData(2, 2, 0)]
        public void AccessBackingBytes_should_return_expected_result_for_position(int position, int expectedOffset, int expectedCount)
        {
            var bytes = new byte[2];
            var subject = CreateSubject(bytes);

            var result = subject.AccessBackingBytes(position);

            result.Array.Should().BeSameAs(bytes);
            result.Offset.Should().Be(expectedOffset);
            result.Count.Should().Be(expectedCount);
        }

        [Theory]
        [ParameterAttributeData]
        public void AccessBackingBytes_should_throw_when_position_is_invalid(
            [Values(-1, 3)]
            int position)
        {
            var subject = CreateSubject(2);

            Action action = () => subject.AccessBackingBytes(position);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("position");
        }

        [Fact]
        public void AccessBackingBytes_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.AccessBackingBytes(0);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("SingleChunkBuffer");
        }

        [Theory]
        [InlineData(false, 2)]
        [InlineData(true, 1)]
        public void Capacity_get_should_return_expected_result(bool isReadOnly, int expectedResult)
        {
            var subject = CreateSubject(2, 1, isReadOnly);

            var result = subject.Capacity;

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void Capacity_get_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => { var _ = subject.Capacity; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("SingleChunkBuffer");
        }

        [Theory]
        [InlineData(0, new byte[] { 1, 2 })]
        [InlineData(1, new byte[] { 0, 2 })]
        [InlineData(2, new byte[] { 0, 0 })]
        public void Clear_should_have_expected_effect_for_count(int count, byte[] expectedBytes)
        {
            var bytes = new byte[] { 1, 2 };
            var subject = CreateSubject(bytes);

            subject.Clear(0, count);

            bytes.Should().Equal(expectedBytes);
        }

        [Theory]
        [InlineData(1, new byte[] { 1, 0, 3 })]
        [InlineData(2, new byte[] { 1, 2, 0 })]
        public void Clear_should_have_expected_effect_for_position(int position, byte[] expectedBytes)
        {
            var bytes = new byte[] { 1, 2, 3 };
            var subject = CreateSubject(bytes);

            subject.Clear(position, 1);

            bytes.Should().Equal(expectedBytes);
        }

        [Theory]
        [InlineData(0, -1)]
        [InlineData(0, 3)]
        [InlineData(1, 2)]
        [InlineData(2, 1)]
        public void Clear_should_throw_when_count_is_invalid(int position, int count)
        {
            var subject = CreateSubject(2);

            Action action = () => subject.Clear(position, count);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("count");
        }

        [Theory]
        [ParameterAttributeData]
        public void Clear_should_throw_when_position_is_invalid(
            [Values(-1, 3)]
            int position)
        {
            var subject = CreateSubject(2);

            Action action = () => subject.Clear(position, 0);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("position");
        }

        [Fact]
        public void Clear_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.Clear(0, 0);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("SingleChunkBuffer");
        }

        [Fact]
        public void Clear_should_throw_when_subject_is_read_only()
        {
            var subject = CreateSubject(isReadOnly: true);

            Action action = () => subject.Clear(0, 0);

            action.ShouldThrow<InvalidOperationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_should_initialize_subject(
            [Values(1, 2)]
            int length,
            [Values(false, true)]
            bool isReadOnly)
        {
            var chunk = CreateFakeChunk(length);

            var subject = new SingleChunkBuffer(chunk, length, isReadOnly);

            var reflector = new Reflector(subject);
            subject.IsReadOnly.Should().Be(isReadOnly);
            subject.Length.Should().Be(length);
            reflector._chunk.Should().BeSameAs(chunk);
            reflector._disposed.Should().BeFalse();
        }

        [Fact]
        public void constructor_should_throw_when_chunk_is_null()
        {
            Action action = () => new SingleChunkBuffer(null, 0);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("chunk");
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_should_throw_when_length_is_invalid(
            [Values(-1, 3)]
            int length)
        {
            var chunk = CreateFakeChunk(2);

            Action action = () => new SingleChunkBuffer(chunk, length);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("length");
        }

        [Fact]
        public void Dispose_can_be_called_multiple_times()
        {
            var subject = CreateSubject();

            subject.Dispose();
            subject.Dispose();
        }

        [Fact]
        public void Dispose_should_dispose_subject()
        {
            var subject = CreateSubject();

            subject.Dispose();

            var reflector = new Reflector(subject);
            reflector._disposed.Should().BeTrue();
        }

        [Theory]
        [ParameterAttributeData]
        public void EnsureCapacity_should_do_nothing_when_minimumCapacity_is_less_than_or_equal_to_capacity(
            [Values(0, 1, 2)]
            int minimumCapacity)
        {
            var subject = CreateSubject(2);

            subject.EnsureCapacity(minimumCapacity);

            subject.Capacity.Should().Be(2);
        }

        [Fact]
        public void EnsureCapacity_should_throw_when_minimumCapacity_is_greater_than_capacity()
        {
            var subject = CreateSubject(2);

            Action action = () => subject.EnsureCapacity(3);

            action.ShouldThrow<NotSupportedException>();
        }

        [Fact]
        public void EnsureCapacity_should_throw_when_minimumCapacity_is_invalid()
        {
            var subject = CreateSubject(2);

            Action action = () => subject.EnsureCapacity(-1);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("minimumCapacity");
        }

        [Fact]
        public void EnsureCapacity_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.EnsureCapacity(1);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("SingleChunkBuffer");
        }

        [Fact]
        public void EnsureCapacity_should_throw_when_subject_is_read_only()
        {
            var subject = CreateSubject(isReadOnly: true);

            Action action = () => subject.EnsureCapacity(0);

            action.ShouldThrow<InvalidOperationException>();
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(2, 3)]
        public void GetByte_should_return_expected_result(int position, byte expectedResult)
        {
            var bytes = new byte[] { 1, 2, 3 };
            var subject = CreateSubject(bytes);

            var result = subject.GetByte(position);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void GetByte_should_throw_when_position_is_invalid(
            [Values(-1, 3)]
            int position)
        {
            var subject = CreateSubject(2);

            Action action = () => subject.GetByte(position);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("position");
        }

        [Fact]
        public void GetByte_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.GetByte(0);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("SingleChunkBuffer");
        }

        [Theory]
        [InlineData(0, new byte[] { 0, 0 })]
        [InlineData(1, new byte[] { 1, 0 })]
        [InlineData(2, new byte[] { 1, 2 })]
        public void GetBytes_should_have_expected_effect_for_count(int count, byte[] expectedBytes)
        {
            var bytes = new byte[] { 1, 2 };
            var subject = CreateSubject(bytes);
            var destination = new byte[2];

            subject.GetBytes(0, destination, 0, count);

            destination.Should().Equal(expectedBytes);
        }

        [Theory]
        [InlineData(1, new byte[] { 0, 1, 2, 0 })]
        [InlineData(2, new byte[] { 0, 0, 1, 2 })]
        public void GetBytes_should_have_expected_effect_for_offset(int offset, byte[] expectedBytes)
        {
            var bytes = new byte[] { 1, 2 };
            var subject = CreateSubject(bytes);
            var destination = new byte[4];

            subject.GetBytes(0, destination, offset, 2);

            destination.Should().Equal(expectedBytes);
        }

        [Theory]
        [InlineData(1, new byte[] { 2, 3 })]
        [InlineData(2, new byte[] { 3, 4 })]
        public void GetBytes_should_have_expected_effect_for_position(int position, byte[] expectedBytes)
        {
            var bytes = new byte[] { 1, 2, 3, 4 };
            var subject = CreateSubject(bytes);
            var destination = new byte[2];

            subject.GetBytes(position, destination, 0, 2);

            destination.Should().Equal(expectedBytes);
        }

        [Theory]
        [InlineData(0, -1)]
        [InlineData(0, 3)]
        [InlineData(1, 2)]
        [InlineData(2, 1)]
        public void GetBytes_should_throw_when_count_is_invalid_for_buffer(int position, int count)
        {
            var subject = CreateSubject(2);
            var destination = new byte[3];

            Action action = () => subject.GetBytes(position, destination, 0, count);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("count");
        }

        [Theory]
        [InlineData(0, -1)]
        [InlineData(0, 3)]
        [InlineData(1, 2)]
        [InlineData(2, 1)]
        public void GetBytes_should_throw_when_count_is_invalid_for_destination(int offset, int count)
        {
            var subject = CreateSubject(3);
            var destination = new byte[2];

            Action action = () => subject.GetBytes(0, destination, offset, count);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("count");
        }

        [Fact]
        public void GetBytes_should_throw_when_destination_is_null()
        {
            var subject = CreateSubject();

            Action action = () => subject.GetBytes(0, null, 0, 0);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("destination");
        }

        [Theory]
        [ParameterAttributeData]
        public void GetBytes_should_throw_when_offset_is_invalid(
            [Values(-1, 3)]
            int offset)
        {
            var subject = CreateSubject(4);
            var destination = new byte[2];

            Action action = () => subject.GetBytes(0, destination, offset, 0);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("offset");
        }

        [Theory]
        [ParameterAttributeData]
        public void GetBytes_should_throw_when_position_is_invalid(
            [Values(-1, 3)]
            int position)
        {
            var subject = CreateSubject(2);
            var destination = new byte[0];

            Action action = () => subject.GetBytes(position, destination, 0, 0);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("position");
        }

        [Fact]
        public void GetBytes_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();
            var destination = new byte[0];

            Action action = () => subject.GetBytes(0, destination, 0, 0);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("SingleChunkBuffer");
        }

        [Theory]
        [InlineData(0, 2)]
        [InlineData(1, 1)]
        [InlineData(2, 0)]
        public void GetSlice_should_return_expected_result(int position, int length)
        {
            var bytes = new byte[2];
            var subject = CreateSubject(bytes, isReadOnly: true);

            var result = subject.GetSlice(position, length);

            result.AccessBackingBytes(0).Array.Should().BeSameAs(bytes);
            result.AccessBackingBytes(0).Offset.Should().Be(position);
            result.AccessBackingBytes(0).Count.Should().Be(length);
        }

        [Fact]
        public void GetSlice_should_return_slice_that_does_not_dispose_subject_when_slice_is_disposed()
        {
            var bytes = new byte[] { 1, 2, 3 };
            var subject = CreateSubject(bytes, isReadOnly: true);
            var slice = subject.GetSlice(1, 1);

            slice.Dispose();

            subject.GetByte(1).Should().Be(2);
        }

        [Fact]
        public void GetSlice_should_return_slice_that_is_not_disposed_when_subject_is_disposed()
        {
            var bytes = new byte[] { 1, 2, 3 };
            var subject = CreateSubject(bytes, isReadOnly: true);
            var slice = subject.GetSlice(1, 1);

            subject.Dispose();

            slice.GetByte(0).Should().Be(2);
        }

        [Theory]
        [InlineData(0, -1)]
        [InlineData(0, 3)]
        [InlineData(1, 2)]
        [InlineData(2, 1)]
        public void GetSlice_should_throw_when_length_is_invalid(int position, int length)
        {
            var subject = CreateSubject(2, isReadOnly: true);

            Action action = () => subject.GetSlice(position, length);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("length");
        }

        [Theory]
        [ParameterAttributeData]
        public void GetSlice_should_throw_when_position_is_invalid(
            [Values(-1, 3)]
            int position)
        {
            var subject = CreateSubject(2, isReadOnly: true);

            Action action = () => subject.GetSlice(position, 0);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("position");
        }

        [Fact]
        public void GetSlice_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.GetSlice(0, 0);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("SingleChunkBuffer");
        }

        [Fact]
        public void GetSlice_should_throw_when_subject_is_not_read_only()
        {
            var subject = CreateSubject(isReadOnly: false);

            Action action = () => subject.GetSlice(0, 0);

            action.ShouldThrow<InvalidOperationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void IsReadOnly_get_should_return_expected_result(
            [Values(false, true)]
            bool isReadOnly)
        {
            var subject = CreateSubject(0, isReadOnly: isReadOnly);

            var result = subject.IsReadOnly;

            result.Should().Be(isReadOnly);
        }

        [Fact]
        public void IsReadOnly_get_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => { var _ = subject.IsReadOnly; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("SingleChunkBuffer");
        }

        [Theory]
        [ParameterAttributeData]
        public void Length_get_should_return_expected_result(
            [Values(1, 2)]
            int length)
        {
            var subject = CreateSubject(2, length);

            var result = subject.Length;

            result.Should().Be(length);
        }

        [Fact]
        public void Length_get_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => { var _ = subject.Length; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("SingleChunkBuffer");
        }

        [Theory]
        [ParameterAttributeData]
        public void Length_set_should_have_expected_effect(
            [Values(1, 2)]
            int length)
        {
            var subject = CreateSubject(2, 0);

            subject.Length = length;

            subject.Length.Should().Be(length);
        }

        [Fact]
        public void Length_set_should_throw_when_subject_is_read_only()
        {
            var subject = CreateSubject(0, 0, isReadOnly: true);

            Action action = () => subject.Length = 0;

            action.ShouldThrow<InvalidOperationException>();
        }

        [Theory]
        [InlineData(0, -1)]
        [InlineData(0, 1)]
        [InlineData(1, 2)]
        [InlineData(2, 3)]
        public void Length_set_should_throw_when_value_is_invalid(int size, int value)
        {
            var subject = CreateSubject(size, 0);

            Action action = () => subject.Length = value;

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("value");
        }

        [Fact]
        public void Length_set_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.Length = 0;

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("SingleChunkBuffer");
        }

        [Theory]
        [ParameterAttributeData]
        public void MakeReadOnly_should_have_expected_effect(
            [Values(false, true)]
            bool isReadOnly)
        {
            var subject = CreateSubject(isReadOnly: isReadOnly);

            subject.MakeReadOnly();

            subject.IsReadOnly.Should().BeTrue();
        }

        [Fact]
        public void MakeReadOnly_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.MakeReadOnly();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("SingleChunkBuffer");
        }

        [Theory]
        [InlineData(1, new byte[] { 0, 1, 0 })]
        [InlineData(2, new byte[] { 0, 0, 1 })]
        public void SetByte_should_have_expected_effect(int position, byte[] expectedBytes)
        {
            var bytes = new byte[3];
            var subject = CreateSubject(bytes);

            subject.SetByte(position, 1);

            bytes.Should().Equal(expectedBytes);
        }

        [Theory]
        [ParameterAttributeData]
        public void SetByte_should_throw_when_position_is_invalid(
            [Values(-1, 3)]
            int position)
        {
            var subject = CreateSubject(2);

            Action action = () => subject.SetByte(position, 0);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("position");
        }

        [Fact]
        public void SetByte_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.SetByte(0, 0);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("SingleChunkBuffer");
        }

        [Fact]
        public void SetByte_should_throw_when_subject_is_read_only()
        {
            var subject = CreateSubject(isReadOnly: true);

            Action action = () => subject.SetByte(0, 0);

            action.ShouldThrow<InvalidOperationException>();
        }

        [Theory]
        [InlineData(1, new byte[] { 1, 0 })]
        [InlineData(2, new byte[] { 1, 2 })]
        public void SetBytes_should_have_expected_effect_for_count(int count, byte[] expectedBytes)
        {
            var bytes = new byte[2];
            var subject = CreateSubject(bytes);
            var source = new byte[] { 1, 2 };

            subject.SetBytes(0, source, 0, count);

            bytes.Should().Equal(expectedBytes);
        }

        [Theory]
        [InlineData(1, new byte[] { 2 })]
        [InlineData(2, new byte[] { 3 })]
        public void SetBytes_should_have_expected_effect_for_offset(int offset, byte[] expectedBytes)
        {
            var bytes = new byte[1];
            var subject = CreateSubject(bytes);
            var source = new byte[] { 1, 2, 3 };

            subject.SetBytes(0, source, offset, 1);

            bytes.Should().Equal(expectedBytes);
        }

        [Theory]
        [InlineData(1, new byte[] { 0, 1, 0 })]
        [InlineData(2, new byte[] { 0, 0, 1 })]
        public void SetBytes_should_have_expected_effect_for_position(int position, byte[] expectedBytes)
        {
            var bytes = new byte[3];
            var subject = CreateSubject(bytes);
            var source = new byte[] { 1 };

            subject.SetBytes(position, source, 0, 1);

            bytes.Should().Equal(expectedBytes);
        }

        [Theory]
        [InlineData(0, -1)]
        [InlineData(1, 2)]
        [InlineData(2, 1)]
        public void SetBytes_should_throw_when_count_is_invalid_for_buffer(int position, int count)
        {
            var subject = CreateSubject(2);
            var source = new byte[4];

            Action action = () => subject.SetBytes(position, source, 0, count);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("count");
        }

        [Theory]
        [InlineData(0, -1)]
        [InlineData(1, 2)]
        [InlineData(2, 1)]
        public void SetBytes_should_throw_when_count_is_invalid_for_source(int offset, int count)
        {
            var subject = CreateSubject(2);
            var source = new byte[2];

            Action action = () => subject.SetBytes(0, source, offset, count);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("count");
        }

        [Theory]
        [ParameterAttributeData]
        public void SetBytes_should_throw_when_offset_is_invalid(
            [Values(-1, 3)]
            int offset)
        {
            var subject = CreateSubject(0);
            var source = new byte[2];

            Action action = () => subject.SetBytes(0, source, offset, 0);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("offset");
        }

        [Theory]
        [ParameterAttributeData]
        public void SetBytes_should_throw_when_position_is_invalid(
            [Values(-1, 3)]
            int position)
        {
            var subject = CreateSubject(2);
            var source = new byte[0];

            Action action = () => subject.SetBytes(position, source, 0, 0);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("position");
        }

        [Fact]
        public void SetBytes_should_throw_when_source_is_null()
        {
            var subject = CreateSubject();

            Action action = () => subject.SetBytes(0, null, 0, 0);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("source");
        }

        [Fact]
        public void SetBytes_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();
            var source = new byte[0];

            Action action = () => subject.SetBytes(0, source, 0, 0);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("SingleChunkBuffer");
        }

        [Fact]
        public void SetBytes_should_throw_when_subject_is_read_only()
        {
            var subject = CreateSubject(isReadOnly: true);
            var source = new byte[0];

            Action action = () => subject.SetBytes(0, source, 0, 0);

            action.ShouldThrow<InvalidOperationException>();
        }

        // helper methods
        private SingleChunkBuffer CreateDisposedSubject()
        {
            var subject = CreateSubject();
            subject.Dispose();
            return subject;
        }

        private IBsonChunk CreateFakeChunk(int size, int? length = null)
        {
            var bytes = new byte[size];
            var mockChunk = new Mock<IBsonChunk>();
            mockChunk.SetupGet(s => s.Bytes).Returns(new ArraySegment<byte>(bytes, 0, length ?? size));
            return mockChunk.Object;
        }

        private SingleChunkBuffer CreateSubject(byte[] bytes, int? length = null, bool isReadOnly = false)
        {
            var chunk = new ByteArrayChunk(bytes);
            return new SingleChunkBuffer(chunk, length ?? bytes.Length, isReadOnly);
        }

        private SingleChunkBuffer CreateSubject(int size = 0, int? length = null, bool isReadOnly = false)
        {
            var chunk = new ByteArrayChunk(size);
            return new SingleChunkBuffer(chunk, length ?? size, isReadOnly);
        }

        // nested types
        private class Reflector
        {
            private readonly SingleChunkBuffer _instance;

            public Reflector(SingleChunkBuffer instance)
            {
                _instance = instance;
            }

            public IBsonChunk _chunk
            {
                get
                {
                    var field = typeof(SingleChunkBuffer).GetField("_chunk", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (IBsonChunk)field.GetValue(_instance);
                }
            }

            public bool _disposed
            {
                get
                {
                    var field = typeof(SingleChunkBuffer).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (bool)field.GetValue(_instance);
                }
            }
        }
    }
}
