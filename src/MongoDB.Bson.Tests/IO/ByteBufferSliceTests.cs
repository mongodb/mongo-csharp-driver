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
    public class ByteBufferSliceTests
    {
        [Test]
        public void AccessBackingBytes_should_adjust_count()
        {
            var bytes = new byte[4];
            var buffer = new ByteArrayBuffer(bytes, isReadOnly: true);
            var subject = new ByteBufferSlice(buffer, 1, 2);

            var result = subject.AccessBackingBytes(0);

            result.Count.Should().Be(2); // not 3
        }

        [Test]
        public void AccessBackingBytes_should_adjust_count_when_multiple_chunks_are_present()
        {
            var arrays = new[] { new byte[] { 1, 2 }, new byte[] { 3, 4 } };
            var chunks = arrays.Select(a => new ByteArrayChunk(a));
            var buffer = new MultiChunkBuffer(chunks, isReadOnly: true);
            var subject = new ByteBufferSlice(buffer, 1, 2);

            var result = subject.AccessBackingBytes(0);

            result.Array.Should().BeSameAs(arrays[0]);
            result.Offset.Should().Be(1);
            result.Count.Should().Be(1); // not 2 or 3
        }

        [Test]
        public void AccessBackingBytes_should_adjust_position()
        {
            var subject = CreateSubjectWithFakeBuffer();
            subject.Buffer.AccessBackingBytes(1).Returns(new ArraySegment<byte>(new byte[3], 1, 2));

            subject.AccessBackingBytes(0);

            subject.Buffer.Received(1).AccessBackingBytes(1);
        }

        [Test]
        public void AccessBackingBytes_should_throw_when_position_is_out_of_range(
            [Values(-1, 3)]
            int position)
        {
            var subject = CreateSubject();

            Action action = () => subject.AccessBackingBytes(position);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("position");
        }

        [Test]
        public void AccessBackingBytes_should_throw_when_subject_is_disposed()
        {
            var subject = CreateSubject();
            subject.Dispose();

            Action action = () => subject.AccessBackingBytes(0);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferSlice");
        }

        [Test]
        public void Capacity_get_should_return_expected_result()
        {
            var subject = CreateSubject();

            var result = subject.Capacity;

            result.Should().Be(2);
        }

        [Test]
        public void Capacity_get_should_throw_when_subject_is_disposed()
        {
            var subject = CreateSubject();
            subject.Dispose();

            Action action = () => { var _ = subject.Capacity; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferSlice");
        }

        [Test]
        public void Clear_should_adjust_position()
        {
            var subject = CreateSubjectWithFakeBuffer();
            var buffer = subject.Buffer;

            subject.Clear(0, 2);

            buffer.Received(1).Clear(1, 2);
        }

        [TestCase(0, 3)]
        [TestCase(1, 2)]
        [TestCase(2, 1)]
        public void Clear_should_throw_when_count_is_out_of_range(int position, int count)
        {
            var subject = CreateSubject();

            Action action = () => subject.Clear(position, count);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("count");
        }

        [Test]
        public void Clear_should_throw_when_position_is_out_of_range(
            [Values(-1, 3)]
            int position)
        {
            var subject = CreateSubject();

            Action action = () => subject.Clear(position, 2);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("position");
        }

        [Test]
        public void Clear_should_throw_when_subject_is_disposed()
        {
            var subject = CreateSubject();
            subject.Dispose();

            Action action = () => subject.Clear(0, 0);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferSlice");
        }

        [Test]
        public void constructor_should_initialize_subject()
        {
            var buffer = new ByteArrayBuffer(new byte[3], isReadOnly: true);

            var subject = new ByteBufferSlice(buffer, 1, 2);

            var reflector = new Reflector(subject);
            subject.Buffer.Should().BeSameAs(buffer);
            reflector._disposed.Should().BeFalse();
            reflector._offset.Should().Be(1);
            reflector._length.Should().Be(2);
        }

        [Test]
        public void constructor_should_throw_when_buffer_is_null()
        {
            Action action = () => new ByteBufferSlice(null, 0, 0);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("buffer");
        }

        [Test]
        public void constructor_should_throw_when_buffer_is_not_readonly()
        {
            var buffer = new ByteArrayBuffer(new byte[1], isReadOnly: false);

            Action action = () => new ByteBufferSlice(buffer, 0, 0);

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("buffer");
        }

        [TestCase(0, 0, 1)]
        [TestCase(1, 0, 2)]
        [TestCase(1, 1, 1)]
        [TestCase(2, 0, 3)]
        [TestCase(2, 1, 2)]
        [TestCase(2, 2, 1)]
        public void constructor_should_throw_when_length_is_out_of_range(int size, int offset, int length)
        {
            var buffer = new ByteArrayBuffer(new byte[size], isReadOnly: true);

            Action action = () => new ByteBufferSlice(buffer, offset, length);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("length");
        }

        [Test]
        public void constructor_should_throw_when_offset_is_out_of_range(
            [Values(-1, 2)]
            int offset)
        {
            var buffer = new ByteArrayBuffer(new byte[1], isReadOnly: true);

            Action action = () => new ByteBufferSlice(buffer, offset, 0);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("offset");
        }

        [Test]
        public void Dispose_can_be_called_more_than_once()
        {
            var subject = CreateSubject();

            subject.Dispose();
            subject.Dispose();
        }

        [Test]
        public void Dispose_should_dispose_buffer()
        {
            var subject = CreateSubjectWithFakeBuffer();
            var buffer = subject.Buffer;

            subject.Dispose();

            buffer.Received(1).Dispose();
        }

        [Test]
        public void Dispose_should_dispose_subject()
        {
            var subject = CreateSubject();

            subject.Dispose();

            var reflector = new Reflector(subject);
            reflector._disposed.Should().BeTrue();
        }

        [Test]
        public void EnsureCapacity_should_throw()
        {
            var subject = CreateSubject();

            Action action = () => subject.EnsureCapacity(0);

            action.ShouldThrow<NotSupportedException>();
        }

        [Test]
        public void GetByte_should_adjust_position()
        {
            var subject = CreateSubjectWithFakeBuffer();
            var buffer = subject.Buffer;

            subject.GetByte(0);

            buffer.Received(1).GetByte(1);
        }

        [Test]
        public void GetByte_should_throw_when_position_is_out_of_range(
            [Values(-1, 3)]
            int position)
        {
            var subject = CreateSubject();

            Action action = () => subject.GetByte(position);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("position");
        }

        [Test]
        public void GetByte_should_throw_when_subject_is_disposed()
        {
            var subject = CreateSubject();
            subject.Dispose();

            Action action = () => subject.GetByte(0);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferSlice");
        }

        [Test]
        public void GetBytes_should_adjust_position()
        {
            var subject = CreateSubjectWithFakeBuffer();
            var buffer = subject.Buffer;
            var destination = new byte[1];

            subject.GetBytes(1, destination, 0, 1);

            buffer.Received(1).GetBytes(2, destination, 0, 1);
        }

        [TestCase(0, 3)]
        [TestCase(1, 2)]
        [TestCase(2, 1)]
        public void GetBytes_should_throw_when_count_is_out_of_range(int position, int count)
        {
            var subject = CreateSubject();
            var destination = new byte[1];

            Action action = () => subject.GetBytes(position, destination, 0, count);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("count");
        }

        [Test]
        public void GetBytes_should_throw_when_position_is_out_of_range(
            [Values(-1, 3)]
            int position)
        {
            var subject = CreateSubject();
            var destination = new byte[1];

            Action action = () => subject.GetBytes(position, destination, 0, 0);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("position");
        }

        [Test]
        public void GetBytes_should_throw_when_subject_is_disposed()
        {
            var subject = CreateSubject();
            var destination = new byte[1];
            subject.Dispose();

            Action action = () => subject.GetBytes(0, destination, 0, 0);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferSlice");
        }

        [Test]
        public void GetSlice_should_adjust_position()
        {
            var subject = CreateSubjectWithFakeBuffer();
            var buffer = subject.Buffer;

            subject.GetSlice(1, 1);

            buffer.Received(1).GetSlice(2, 1);
        }

        [TestCase(0, 3)]
        [TestCase(1, 2)]
        [TestCase(2, 1)]
        public void GetSlice_should_throw_when_length_is_out_of_range(int position, int length)
        {
            var subject = CreateSubject();

            Action action = () => subject.GetSlice(position, length);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("length");
        }

        [Test]
        public void GetSlice_should_throw_when_position_is_out_of_range(
            [Values(-1, 3)]
            int position)
        {
            var subject = CreateSubject();

            Action action = () => subject.GetSlice(position, 2);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("position");
        }

        [Test]
        public void GetSlice_should_throw_when_subject_is_disposed()
        {
            var subject = CreateSubject();
            subject.Dispose();

            Action action = () => subject.GetSlice(0, 0);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferSlice");
        }

        [Test]
        public void IsReadOnly_get_should_return_expected_result()
        {
            var subject = CreateSubject();

            var result = subject.IsReadOnly;

            result.Should().BeTrue();
        }

        [Test]
        public void IsReadOnly_get_should_throw_when_subject_is_disposed()
        {
            var subject = CreateSubject();
            subject.Dispose();

            Action action = () => { var _ = subject.IsReadOnly; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferSlice");
        }

        [Test]
        public void Length_get_should_return_expected_result()
        {
            var subject = CreateSubject();

            var result = subject.Length;

            result.Should().Be(2);
        }

        [Test]
        public void Length_get_should_throw_when_subject_is_disposed()
        {
            var subject = CreateSubject();
            subject.Dispose();

            Action action = () => { var _ = subject.Length; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferSlice");
        }

        [Test]
        public void MakeReadOnly_should_throw_when_subject_is_disposed()
        {
            var subject = CreateSubject();
            subject.Dispose();

            Action action = () => subject.MakeReadOnly();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferSlice");
        }

        [Test]
        public void SetByte_should_throw()
        {
            var subject = CreateSubject();

            Action action = () => subject.SetByte(0, 0);

            action.ShouldThrow<NotSupportedException>();
        }

        [Test]
        public void SetBytes_should_throw()
        {
            var subject = CreateSubject();
            var source = new byte[1];
            subject.Dispose();

            Action action = () => subject.SetBytes(0, source, 0, 0);

            action.ShouldThrow<NotSupportedException>();
        }

        // helper methods
        public ByteBufferSlice CreateSubject(int size = 3, int offset = 1, int length = 2)
        {
            var buffer = new ByteArrayBuffer(new byte[size], isReadOnly: true);
            return new ByteBufferSlice(buffer, offset, length);
        }

        public ByteBufferSlice CreateSubjectWithFakeBuffer(int size = 3, int offset = 1, int length = 2)
        {
            var buffer = Substitute.For<IByteBuffer>();
            buffer.Length.Returns(size);
            buffer.IsReadOnly.Returns(true);
            return new ByteBufferSlice(buffer, offset, length);
        }

        // nested types
        private class Reflector
        {
            private readonly ByteBufferSlice _instance;

            public Reflector(ByteBufferSlice instance)
            {
                _instance = instance;
            }

            public bool _disposed
            {
                get
                {
                    var field = typeof(ByteBufferSlice).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (bool)field.GetValue(_instance);
                }
            }

            public int _length
            {
                get
                {
                    var field = typeof(ByteBufferSlice).GetField("_length", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (int)field.GetValue(_instance);
                }
            }

            public int _offset
            {
                get
                {
                    var field = typeof(ByteBufferSlice).GetField("_offset", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (int)field.GetValue(_instance);
                }
            }
        }
    }
}
