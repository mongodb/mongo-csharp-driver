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
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Bson.Tests.IO
{
    public class ByteBufferStreamTests
    {
        [Fact]
        public void Buffer_get_should_return_expected_result()
        {
            var mockBuffer = new Mock<IByteBuffer>();
            var subject = new ByteBufferStream(mockBuffer.Object);

            var result = subject.Buffer;

            result.Should().BeSameAs(mockBuffer.Object);
        }

        [Fact]
        public void Buffer_get_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => { var _ = subject.Buffer; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferStream");
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public void CanRead_get_should_return_expected_result(bool disposed, bool expectedResult)
        {
            var subject = CreateSubject();
            if (disposed)
            {
                subject.Dispose();
            }

            var result = subject.CanRead;

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public void CanSeek_get_should_return_expected_result(bool disposed, bool expectedResult)
        {
            var subject = CreateSubject();
            if (disposed)
            {
                subject.Dispose();
            }

            var result = subject.CanSeek;

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CanTimeout_get_should_return_expected_result(
            [Values(false, true)]
            bool disposed)
        {
            var subject = CreateSubject();
            if (disposed)
            {
                subject.Dispose();
            }

            var result = subject.CanTimeout;

            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(false, false, true)]
        [InlineData(false, true, false)]
        [InlineData(true, false, false)]
        [InlineData(true, true, false)]
        public void CanWrite_get_should_return_expected_result(bool bufferIsReadOnly, bool disposed, bool expectedResult)
        {
            var subject = CreateSubject();
            var mockBuffer = Mock.Get(subject.Buffer);
            mockBuffer.SetupGet(b => b.IsReadOnly).Returns(bufferIsReadOnly);
            if (disposed)
            {
                subject.Dispose();
            }

            var result = subject.CanWrite;

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_should_initialize_subject(
            [Values(false, true)]
            bool ownsBuffer)
        {
            var length = 123;
            var mockBuffer = new Mock<IByteBuffer>();
            mockBuffer.SetupGet(s => s.Length).Returns(length);

            var subject = new ByteBufferStream(mockBuffer.Object, ownsBuffer);

            var reflector = new Reflector(subject);
            subject.Buffer.Should().BeSameAs(mockBuffer.Object);
            subject.Length.Should().Be(length);
            subject.Position.Should().Be(0);
            reflector._disposed.Should().BeFalse();
            reflector._ownsBuffer.Should().Be(ownsBuffer);
        }

        [Fact]
        public void constructor_should_throw_when_buffer_is_null()
        {
            Action action = () => new ByteBufferStream(null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("buffer");
        }

        [Fact]
        public void Dispose_can_be_called_more_than_once()
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
        public void Dispose_should_dispose_buffer_if_it_owns_it(
            [Values(false, true)]
            bool ownsBuffer)
        {
            var mockBuffer = new Mock<IByteBuffer>();
            var subject = new ByteBufferStream(mockBuffer.Object, ownsBuffer: ownsBuffer);

            subject.Dispose();

            mockBuffer.Verify(s => s.Dispose(), Times.Exactly(ownsBuffer ? 1 : 0));
        }

        [Fact]
        public void Flush_should_do_nothing()
        {
            var subject = CreateSubject();

            subject.Flush();
        }

        [Fact]
        public void Flush_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.Flush();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferStream");
        }

        [Fact]
        public void Length_get_should_return_expected_result()
        {
            var subject = CreateSubject(new byte[1]);

            var result = subject.Length;

            result.Should().Be(1);
        }

        [Fact]
        public void Length_get_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => { var _ = subject.Length; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferStream");
        }

        [Fact]
        public void Position_get_should_return_expected_result()
        {
            var subject = CreateSubject();
            subject.Position = 1;

            var result = subject.Position;

            result.Should().Be(1);
        }

        [Fact]
        public void Position_get_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => { var _ = subject.Position; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferStream");
        }

        [Fact]
        public void Position_set_should_have_expected_effect()
        {
            var subject = CreateSubject();

            subject.Position = 1;

            subject.Position.Should().Be(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Position_set_should_throw_when_value_is_invalid(
            [Values(-1L, (long)int.MaxValue + 1)]
            long value)
        {
            var subject = CreateSubject();

            Action action = () => subject.Position = value;

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("value");
        }

        [Fact]
        public void Position_set_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.Position = 1;

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferStream");
        }

        [SkippableFact]
        public void PrepareToWrite_should_throw_when_stream_would_exceed_2GB()
        {
            RequireProcess.Check().Bits(64);

            using (var buffer = new MultiChunkBuffer(BsonChunkPool.Default))
            using (var subject = new ByteBufferStream(buffer))
            {
                var bytes = new byte[int.MaxValue / 2 + 1024];
                subject.Write(bytes, 0, bytes.Length);

                Action action = () => subject.Write(bytes, 0, bytes.Length); // indirectly calls private PrepareToWrite method

                action.ShouldThrow<IOException>();
            }
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(2, 0)]
        [InlineData(2, 1)]
        [InlineData(3, 0)]
        [InlineData(3, 1)]
        [InlineData(3, 2)]
        public void Read_should_return_available_bytes_when_available_bytes_is_less_than_count(long length, long position)
        {
            var subject = CreateSubject();
            var mockBuffer = Mock.Get(subject.Buffer);
            mockBuffer.SetupGet(b => b.Capacity).Returns((int)length);
            subject.SetLength(length);
            subject.Position = position;
            var available = (int)(length - position);
            var destination = new byte[available + 1];

            var result = subject.Read(destination, 0, available + 1);

            result.Should().Be(available);
            subject.Position.Should().Be(position + available);
            mockBuffer.Verify(b => b.GetBytes((int)position, destination, 0, available), Times.Once);
        }

        [Theory]
        [InlineData(1, 0, 1)]
        [InlineData(2, 0, 2)]
        [InlineData(2, 1, 1)]
        [InlineData(3, 0, 3)]
        [InlineData(3, 1, 2)]
        [InlineData(3, 2, 1)]
        public void Read_should_return_count_bytes_when_count_is_less_than_or_equal_to_available_bytes(long length, long position, int count)
        {
            var subject = CreateSubject();
            var mockBuffer = Mock.Get(subject.Buffer);
            mockBuffer.SetupGet(b => b.Capacity).Returns((int)length);
            subject.SetLength(length);
            subject.Position = position;
            var destination = new byte[count];

            var result = subject.Read(destination, 0, count);

            result.Should().Be(count);
            subject.Position.Should().Be(position + count);
            mockBuffer.Verify(b => b.GetBytes((int)position, destination, 0, count), Times.Once);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 1)]
        [InlineData(1, 2)]
        [InlineData(2, 2)]
        public void Read_should_return_zero_when_position_is_greater_than_or_equal_to_length(long length, long position)
        {
            var subject = CreateSubject();
            var mockBuffer = Mock.Get(subject.Buffer);
            mockBuffer.SetupGet(b => b.Capacity).Returns((int)length);
            subject.SetLength(length);
            subject.Position = position;
            var destination = new byte[1];

            var result = subject.Read(destination, 0, 1);

            result.Should().Be(0);
            subject.Position.Should().Be(position);
        }

        [Fact]
        public void Read_should_throw_when_buffer_is_null()
        {
            var subject = CreateSubject();

            Action action = () => subject.Read(null, 0, 0);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("buffer");
        }

        [Theory]
        [InlineData(1, 0, -1)]
        [InlineData(1, 0, 2)]
        [InlineData(2, 0, -1)]
        [InlineData(2, 0, 3)]
        [InlineData(2, 1, 2)]
        [InlineData(2, 2, 1)]
        public void Read_should_throw_when_count_is_out_of_range(int destinationSize, int offset, int count)
        {
            var subject = CreateSubject();
            var destination = new byte[destinationSize];

            Action action = () => subject.Read(destination, offset, count);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("count");
        }

        [Theory]
        [InlineData(1, -1)]
        [InlineData(1, 2)]
        [InlineData(2, -1)]
        [InlineData(2, 3)]
        public void Read_should_throw_when_offset_is_out_of_range(int destinationSize, int offset)
        {
            var subject = CreateSubject();
            var destination = new byte[destinationSize];

            Action action = () => subject.Read(destination, offset, 0);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("offset");
        }

        [Fact]
        public void Read_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();
            var destination = new byte[1];

            Action action = () => subject.Read(destination, 0, 1);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferStream");
        }

        [Theory]
        [InlineData(1, 0, 1)]
        [InlineData(2, 0, 1)]
        [InlineData(2, 1, 2)]
        [InlineData(3, 0, 1)]
        [InlineData(3, 1, 2)]
        [InlineData(3, 2, 3)]
        public void ReadByte_should_return_expected_result(long length, long position, byte expectedResult)
        {
            var subject = CreateSubject();
            var mockBuffer = Mock.Get(subject.Buffer);
            mockBuffer.SetupGet(b => b.Capacity).Returns((int)length);
            mockBuffer.Setup(b => b.GetByte((int)position)).Returns(expectedResult);
            subject.SetLength(length);
            subject.Position = position;

            var result = subject.ReadByte();

            result.Should().Be(expectedResult);
            subject.Position.Should().Be(position + 1);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 1)]
        [InlineData(1, 1)]
        [InlineData(1, 2)]
        [InlineData(2, 2)]
        [InlineData(2, 3)]
        public void ReadByte_should_return_minus_one_when_position_is_greater_than_or_equal_to_length(long length, long position)
        {
            var subject = CreateSubject();
            var mockBuffer = Mock.Get(subject.Buffer);
            mockBuffer.SetupGet(b => b.Capacity).Returns((int)length);
            subject.SetLength(length);
            subject.Position = position;

            var result = subject.ReadByte();

            result.Should().Be(-1);
        }

        [Fact]
        public void ReadByte_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.ReadByte();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferStream");
        }

        [Theory]
        [InlineData(1, 1, new byte[] { 0 }, "")]
        [InlineData(1, 2, new byte[] { 0 }, "")]
        [InlineData(2, 1, new byte[] { 97, 0 }, "a")]
        [InlineData(2, 2, new byte[] { 97, 0 }, "a")]
        [InlineData(3, 1, new byte[] { 97, 98, 0 }, "ab")]
        [InlineData(3, 2, new byte[] { 97, 98, 0 }, "ab")]
        [InlineData(4, 1, new byte[] { 97, 98, 99, 0 }, "abc")]
        [InlineData(4, 2, new byte[] { 97, 98, 99, 0 }, "abc")]
        public void ReadCString_should_return_expected_result(int length, int numberOfChunks, byte[] bytes, string expectedResult)
        {
            var subject = CreateSubject(bytes, numberOfChunks);
            var expectedPosition = length;

            var result = subject.ReadCString(Utf8Encodings.Strict);

            result.Should().Be(expectedResult);
            subject.Position.Should().Be(expectedPosition);
        }

        [Fact]
        public void ReadCString_should_throw_when_encoding_is_null()
        {
            var subject = CreateSubject();

            Action action = () => subject.ReadCString(null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("encoding");
        }

        [Fact]
        public void ReadCString_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.ReadCString(Utf8Encodings.Strict);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferStream");
        }

        [Theory]
        [InlineData(1, 1, new byte[] { 0 }, new byte[] { })]
        [InlineData(2, 1, new byte[] { 97, 0 }, new byte[] { 97 })]
        [InlineData(2, 2, new byte[] { 97, 0 }, new byte[] { 97 })]
        [InlineData(3, 1, new byte[] { 97, 98, 0 }, new byte[] { 97, 98 })]
        [InlineData(3, 2, new byte[] { 97, 98, 0 }, new byte[] { 97, 98 })]
        [InlineData(4, 1, new byte[] { 97, 98, 99, 0 }, new byte[] { 97, 98, 99 })]
        [InlineData(4, 2, new byte[] { 97, 98, 99, 0 }, new byte[] { 97, 98, 99 })]
        public void ReadCStringBytes_should_return_expected_result(int length, int numberOfChunks, byte[] bytes, byte[] expectedResult)
        {
            var subject = CreateSubject(bytes, numberOfChunks);
            var expectedPosition = length;

            var result = subject.ReadCStringBytes();

            result.Should().Equal(expectedResult);
            subject.Position.Should().Be(expectedPosition);
        }

        [Theory]
        [InlineData(0, 1, new byte[] { })]
        [InlineData(1, 1, new byte[] { 97 })]
        [InlineData(2, 1, new byte[] { 97, 98 })]
        [InlineData(2, 2, new byte[] { 97, 98 })]
        [InlineData(3, 1, new byte[] { 97, 98, 99 })]
        [InlineData(3, 2, new byte[] { 97, 98, 99 })]
        public void ReadCStringBytes_should_throw_when_end_of_stream_is_reached(int length, int numberOfChunks, byte[] bytes)
        {
            var subject = CreateSubject(bytes, numberOfChunks);

            Action action = () => subject.ReadCStringBytes();

            action.ShouldThrow<EndOfStreamException>();
        }

        [Fact]
        public void ReadCStringBytes_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.ReadCStringBytes();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferStream");
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadDecimal18_should_return_expected_result(
            [Values(1, 2)]
            int numberOfChunks,
            [Values("-1.0", "0.0", "1.0", "NaN", "-Infinity", "Infinity")]
            string valueString)
        {
            var value = Decimal128.Parse(valueString);
            var bytes = new byte[16];
            Buffer.BlockCopy(BitConverter.GetBytes(value.GetIEEELowBits()), 0, bytes, 0, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(value.GetIEEEHighBits()), 0, bytes, 8, 8);
            var subject = CreateSubject(bytes, numberOfChunks);

            var result = subject.ReadDecimal128();

            result.Should().Be(value);
            subject.Position.Should().Be(16);
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadDecimal128_should_throw_when_at_end_of_stream(
            [Values(1, 2)]
            int numberOfChunks,
            [Values(0, 1, 7)]
            int length)
        {
            var subject = CreateSubject(length, numberOfChunks);

            Action action = () => subject.ReadDecimal128();

            action.ShouldThrow<EndOfStreamException>();
        }

        [Fact]
        public void ReadDecimal128_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.ReadDecimal128();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferStream");
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadDouble_should_return_expected_result(
            [Values(1, 2)]
            int numberOfChunks,
            [Values(-1.0, 0.0, 1.0, double.Epsilon, double.MaxValue, double.MinValue, double.NaN, double.NegativeInfinity, double.PositiveInfinity)]
            double value)
        {
            var bytes = BitConverter.GetBytes(value);
            var subject = CreateSubject(bytes, numberOfChunks);

            var result = subject.ReadDouble();

            result.Should().Be(value);
            subject.Position.Should().Be(8);
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadDouble_should_throw_when_at_end_of_stream(
            [Values(1, 2)]
            int numberOfChunks,
            [Values(0, 1, 7)]
            int length)
        {
            var subject = CreateSubject(length, numberOfChunks);

            Action action = () => subject.ReadDouble();

            action.ShouldThrow<EndOfStreamException>();
        }

        [Fact]
        public void ReadDouble_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.ReadDouble();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferStream");
        }

        [Fact]
        public void ReadInt32_should_be_little_endian()
        {
            var bytes = new byte[] { 4, 3, 2, 1 };
            var subject = CreateSubject(bytes);

            var result = subject.ReadInt32();

            result.Should().Be(0x01020304);
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadInt32_should_return_expected_result(
            [Values(1, 2)]
            int numberOfChunks,
            [Values(-1, 0, 1, int.MaxValue, int.MinValue)]
            int value)
        {
            var bytes = BitConverter.GetBytes(value);
            var subject = CreateSubject(bytes, numberOfChunks);

            var result = subject.ReadInt32();

            result.Should().Be(value);
            subject.Position.Should().Be(4);
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadInt32_should_throw_when_at_end_of_stream(
            [Values(1, 2)]
            int numberOfChunks,
            [Values(0, 1, 3)]
            int length)
        {
            var subject = CreateSubject(length, numberOfChunks);

            Action action = () => subject.ReadInt32();

            action.ShouldThrow<EndOfStreamException>();
        }

        [Fact]
        public void ReadInt32_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.ReadInt32();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferStream");
        }

        [Fact]
        public void ReadInt64_should_be_little_endian()
        {
            var bytes = new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 };
            var subject = CreateSubject(bytes);

            var result = subject.ReadInt64();

            result.Should().Be(0x0102030405060708);
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadInt64_should_return_expected_result(
            [Values(1, 2)]
            int numberOfChunks,
            [Values(-1, 0, 1, long.MaxValue, long.MinValue)]
            long value)
        {
            var bytes = BitConverter.GetBytes(value);
            var subject = CreateSubject(bytes, numberOfChunks);

            var result = subject.ReadInt64();

            result.Should().Be(value);
            subject.Position.Should().Be(8);
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadInt64_should_throw_when_at_end_of_stream(
            [Values(1, 2)]
            int numberOfChunks,
            [Values(0, 1, 7)]
            int length)
        {
            var subject = CreateSubject(length, numberOfChunks);

            Action action = () => subject.ReadInt64();

            action.ShouldThrow<EndOfStreamException>();
        }

        [Fact]
        public void ReadInt64_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.ReadInt64();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferStream");
        }

        [Fact]
        public void ReadObjectId_should_be_big_endian()
        {
            var bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var subject = CreateSubject(bytes);

            var result = subject.ReadObjectId();

            result.Timestamp.Should().Be(0x01020304);
            result.Machine.Should().Be(0x050607);
            result.Pid.Should().Be(0x0809);
            result.Increment.Should().Be(0x0a0b0c);
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadObjectId_should_return_expected_result(
            [Values(1, 2)]
            int numberOfChunks)
        {
            var bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var subject = CreateSubject(bytes, numberOfChunks);
            var expectedResult = new ObjectId(0x01020304, 0x050607, 0x0809, 0x0a0b0c);

            var result = subject.ReadObjectId();

            result.Should().Be(expectedResult);
            subject.Position.Should().Be(12);
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadObjectId_should_throw_when_at_end_of_stream(
            [Values(1, 2)]
            int numberOfChunks,
            [Values(0, 1, 11)]
            int length)
        {
            var subject = CreateSubject(length, numberOfChunks);

            Action action = () => subject.ReadObjectId();

            action.ShouldThrow<EndOfStreamException>();
        }

        [Fact]
        public void ReadObjectId_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.ReadObjectId();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferStream");
        }

        [Theory]
        [InlineData(4, new byte[] { 4, 0, 0, 0 })]
        [InlineData(5, new byte[] { 5, 0, 0, 0, 1 })]
        [InlineData(6, new byte[] { 6, 0, 0, 0, 1, 2 })]
        [InlineData(7, new byte[] { 7, 0, 0, 0, 1, 2, 3 })]
        public void ReadSlice_should_return_expected_result(int length, byte[] bytes)
        {
            var mockBuffer = new Mock<IByteBuffer>();
            mockBuffer.Setup(s => s.AccessBackingBytes(It.IsAny<int>())).Returns((int p) => { return new ArraySegment<byte>(bytes, p, bytes.Length - p); });
            mockBuffer.SetupGet(s => s.IsReadOnly).Returns(true);
            mockBuffer.SetupGet(s => s.Length).Returns(bytes.Length);
            var subject = new ByteBufferStream(mockBuffer.Object);
            var expectedPosition = length;

            subject.ReadSlice();

            subject.Position.Should().Be(expectedPosition);
            mockBuffer.Verify(b => b.GetSlice(0, bytes.Length), Times.Once);
        }

        [Theory]
        [InlineData(1, new byte[] { })]
        [InlineData(2, new byte[] { 6 })]
        [InlineData(3, new byte[] { 6, 0 })]
        [InlineData(4, new byte[] { 6, 0, 0 })]
        [InlineData(5, new byte[] { 6, 0, 0, 0 })]
        [InlineData(6, new byte[] { 6, 0, 0, 0, 1 })]
        public void ReadSlice_should_throw_when_at_end_of_stream(int length, byte[] bytes)
        {
            var subject = CreateSubject(bytes);

            Action action = () => subject.ReadSlice();

            action.ShouldThrow<EndOfStreamException>();
        }

        [Fact]
        public void ReadSlice_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.ReadSlice();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferStream");
        }

        [Theory]
        [InlineData(5, 1, new byte[] { 1, 0, 0, 0, 0 }, "")]
        [InlineData(6, 1, new byte[] { 2, 0, 0, 0, 97, 0 }, "a")]
        [InlineData(6, 2, new byte[] { 2, 0, 0, 0, 97, 0 }, "a")]
        [InlineData(7, 1, new byte[] { 3, 0, 0, 0, 97, 98, 0 }, "ab")]
        [InlineData(7, 2, new byte[] { 3, 0, 0, 0, 97, 98, 0 }, "ab")]
        [InlineData(8, 1, new byte[] { 4, 0, 0, 0, 97, 98, 99, 0 }, "abc")]
        [InlineData(8, 2, new byte[] { 4, 0, 0, 0, 97, 98, 99, 0 }, "abc")]
        public void ReadString_should_return_expected_result(int length, int numberOfAdditionalChunks, byte[] bytes, string expectedResult)
        {
            var subject = CreateSubject(bytes, 4, CalculateChunkSizes(length - 4, numberOfAdditionalChunks));

            var result = subject.ReadString(Utf8Encodings.Strict);

            result.Should().Be(expectedResult);
            subject.Position.Should().Be(length);
        }

        [Fact]
        public void ReadString_should_throw_when_encoding_is_null()
        {
            var subject = CreateSubject();

            Action action = () => subject.ReadString(null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("encoding");
        }

        [Theory]
        [InlineData(4, 1, new byte[] { 1, 0, 0, 0 })]
        [InlineData(5, 1, new byte[] { 2, 0, 0, 0, 97 })]
        [InlineData(6, 1, new byte[] { 3, 0, 0, 0, 97, 98 })]
        [InlineData(6, 2, new byte[] { 3, 0, 0, 0, 97, 98 })]
        [InlineData(7, 1, new byte[] { 4, 0, 0, 0, 97, 98, 99 })]
        [InlineData(7, 2, new byte[] { 4, 0, 0, 0, 97, 98, 99 })]
        public void ReadString_should_throw_when_at_end_of_stream(int length, int numberOfAdditionalChunks, byte[] bytes)
        {
            var subject = CreateSubject(bytes, 4, CalculateChunkSizes(length - 4, numberOfAdditionalChunks));

            Action action = () => subject.ReadString(Utf8Encodings.Strict);

            action.ShouldThrow<EndOfStreamException>();
        }

        [Fact]
        public void ReadString_should_throw_when_length_is_less_than_zero()
        {
            var bytes = BitConverter.GetBytes(-1);
            var subject = CreateSubject(bytes);

            Action action = () => subject.ReadString(Utf8Encodings.Strict);

            action.ShouldThrow<FormatException>();
        }

        [Fact]
        public void ReadString_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.ReadString(Utf8Encodings.Strict);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferStream");
        }

        [Theory]
        [InlineData(5, 1, new byte[] { 1, 0, 0, 0, 1 })]
        [InlineData(6, 1, new byte[] { 2, 0, 0, 0, 97, 1 })]
        [InlineData(6, 2, new byte[] { 2, 0, 0, 0, 97, 1 })]
        [InlineData(7, 1, new byte[] { 3, 0, 0, 0, 97, 98, 1 })]
        [InlineData(7, 2, new byte[] { 3, 0, 0, 0, 97, 98, 1 })]
        [InlineData(8, 1, new byte[] { 4, 0, 0, 0, 97, 98, 99, 1 })]
        [InlineData(8, 2, new byte[] { 4, 0, 0, 0, 97, 98, 99, 1 })]
        public void ReadString_should_throw_when_terminating_null_byte_is_missing(int length, int numberOfAdditionalChunks, byte[] bytes)
        {
            var subject = CreateSubject(bytes, 4, CalculateChunkSizes(length - 4, numberOfAdditionalChunks));

            Action action = () => subject.ReadString(Utf8Encodings.Strict);

            action.ShouldThrow<FormatException>();
        }

        [Theory]
        [InlineData(SeekOrigin.Begin, 1, 0, 0)]
        [InlineData(SeekOrigin.Begin, 1, 1, 1)]
        [InlineData(SeekOrigin.Current, 1, -1, 0)]
        [InlineData(SeekOrigin.Current, 1, 0, 1)]
        [InlineData(SeekOrigin.Current, 1, 1, 2)]
        [InlineData(SeekOrigin.End, 1, -1, 2)]
        [InlineData(SeekOrigin.End, 1, 0, 3)]
        public void Seek_should_return_expected_result(SeekOrigin origin, long position, long offset, int expectedPosition)
        {
            var subject = CreateSubject();
            var mockBuffer = Mock.Get(subject.Buffer);
            mockBuffer.SetupGet(b => b.Capacity).Returns(3);
            subject.SetLength(3);
            subject.Position = position;

            var result = subject.Seek(offset, origin);

            result.Should().Be(expectedPosition);
            subject.Position.Should().Be(expectedPosition);
        }

        [Fact]
        public void Seek_should_throw_when_origin_is_invalid()
        {
            var subject = CreateSubject();

            Action action = () => subject.Seek(0, (SeekOrigin)(-1));

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("origin");
        }

        [Theory]
        [InlineData(SeekOrigin.Begin, -1)]
        [InlineData(SeekOrigin.Begin, (long)int.MaxValue + 1)]
        [InlineData(SeekOrigin.Current, -2)]
        [InlineData(SeekOrigin.Current, int.MaxValue)]
        [InlineData(SeekOrigin.End, -3)]
        [InlineData(SeekOrigin.End, int.MaxValue - 1)]
        public void Seek_should_throw_when_new_position_is_invalid(SeekOrigin origin, long offset)
        {
            var subject = CreateSubject();
            var mockBuffer = Mock.Get(subject.Buffer);
            mockBuffer.SetupGet(b => b.Capacity).Returns(2);
            subject.SetLength(2);
            subject.Position = 1;

            Action action = () => subject.Seek(offset, origin);

            action.ShouldThrow<IOException>();
        }

        [Fact]
        public void Seek_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.Seek(0, SeekOrigin.Begin);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferStream");
        }

        [Fact]
        public void SetLength_should_throw_when_buffer_is_not_writable()
        {
            var subject = CreateSubject();
            var mockBuffer = Mock.Get(subject.Buffer);
            mockBuffer.SetupGet(b => b.IsReadOnly).Returns(true);

            Action action = () => subject.SetLength(0);

            action.ShouldThrow<NotSupportedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void SetLength_should_set_length(
            [Values(0, 1, 2, 3)]
            long length)
        {
            var subject = CreateSubject();
            var mockBuffer = Mock.Get(subject.Buffer);

            subject.SetLength(length);

            subject.Length.Should().Be(length);
            mockBuffer.Verify(b => b.EnsureCapacity((int)length), Times.Once);
        }

        [Theory]
        [ParameterAttributeData]
        public void SetLength_should_set_position_when_position_is_greater_than_new_length(
            [Values(0, 1, 2, 3)]
            long length)
        {
            var subject = CreateSubject();
            subject.Position = length + 1;

            subject.SetLength(length);

            subject.Position.Should().Be(length);
        }

        [Theory]
        [ParameterAttributeData]
        public void SetLength_should_throw_when_length_is_out_of_range(
            [Values(-1, (long)int.MaxValue + 1)]
            long length)
        {
            var subject = CreateSubject();

            Action action = () => subject.SetLength(length);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("value");
        }

        [Fact]
        public void SetLength_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.SetLength(0);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferStream");
        }

        [Theory]
        [InlineData(1, 1, new byte[] { 0 }, 1)]
        [InlineData(1, 2, new byte[] { 0 }, 1)]
        [InlineData(2, 1, new byte[] { 97, 0 }, 2)]
        [InlineData(2, 2, new byte[] { 97, 0 }, 2)]
        [InlineData(3, 2, new byte[] { 97, 98, 0 }, 3)]
        [InlineData(3, 1, new byte[] { 97, 98, 0 }, 3)]
        [InlineData(4, 1, new byte[] { 97, 98, 99, 0 }, 4)]
        [InlineData(4, 2, new byte[] { 97, 98, 99, 0 }, 4)]
        public void SkiCString_should_have_expected_effect(int length, int numberOfChunks, byte[] bytes, long expectedPosition)
        {
            var subject = CreateSubject(bytes, numberOfChunks);

            subject.SkipCString();

            subject.Position.Should().Be(expectedPosition);
        }

        [Theory]
        [InlineData(0, 1, new byte[] { })]
        [InlineData(1, 1, new byte[] { 97 })]
        [InlineData(2, 1, new byte[] { 97, 98 })]
        [InlineData(2, 2, new byte[] { 97, 98 })]
        [InlineData(3, 1, new byte[] { 97, 98, 99 })]
        [InlineData(3, 2, new byte[] { 97, 98, 99 })]
        public void SkipCString_should_throw_when_end_of_stream_is_reached(int length, int numberOfChunks, byte[] bytes)
        {
            var subject = CreateSubject(bytes);

            Action action = () => subject.SkipCString();

            action.ShouldThrow<EndOfStreamException>();
        }

        [Fact]
        public void SkipCString_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.SkipCString();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferStream");
        }

        [Fact]
        public void ThrowIfEndOfStream_should_throw_when_position_plus_length_exceeds_2GB()
        {
            using (var buffer = new ByteArrayBuffer(new byte[1024]))
            using (var subject = new ByteBufferStream(buffer))
            {
                subject.Position = 1024;
                subject.WriteInt32(int.MaxValue - 128);
                subject.Position = 1024;

                Action action = () => subject.ReadSlice(); // indirectly calls private ThrowIfEndOfStream method

                action.ShouldThrow<EndOfStreamException>();
            }
        }

        [Fact]
        public void Write_should_clear_bytes_between_length_and_position()
        {
            var subject = CreateSubject();
            var mockBuffer = Mock.Get(subject.Buffer);
            subject.Position = 1;

            subject.Write(new byte[3], 1, 2);

            mockBuffer.Verify(b => b.Clear(0, 1), Times.Once);
        }

        [Fact]
        public void Write_should_ensure_capacity()
        {
            var subject = CreateSubject();
            var mockBuffer = Mock.Get(subject.Buffer);
            var capacity = 0;
            mockBuffer.SetupGet(b => b.Capacity).Returns(() => capacity);
            mockBuffer.Setup(b => b.EnsureCapacity(It.IsAny<int>())).Callback((int minimumCapacity) => capacity = minimumCapacity);
            mockBuffer.SetupProperty(b => b.Length);
            subject.Position = 1;

            subject.Write(new byte[3], 1, 2);

            mockBuffer.Verify(b => b.EnsureCapacity(3), Times.Once);
            subject.Buffer.Length.Should().Be(3);
        }

        [Theory]
        [InlineData(1, 0, 1, 0, 1)]
        [InlineData(4, 1, 2, 1, 3)]
        public void Write_should_have_expected_effect(int sourceSize, int offset, int count, long initialPosition, long expectedPosition)
        {
            var subject = CreateSubject();
            var mockBuffer = Mock.Get(subject.Buffer);
            subject.Position = initialPosition;
            var source = new byte[sourceSize];

            subject.Write(source, offset, count);

            subject.Position.Should().Be(expectedPosition);
            subject.Length.Should().Be(expectedPosition);
            mockBuffer.Verify(b => b.SetBytes((int)initialPosition, source, offset, count), Times.Once);
        }

        [Fact]
        public void Write_should_throw_when_buffer_is_null()
        {
            var subject = CreateSubject();

            Action action = () => subject.Write(null, 0, 0);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("buffer");
        }

        [Theory]
        [InlineData(0, 0, -1)]
        [InlineData(0, 0, 1)]
        [InlineData(1, 0, -1)]
        [InlineData(1, 0, 2)]
        [InlineData(1, 1, -2)]
        [InlineData(1, 1, 1)]
        [InlineData(2, 0, -1)]
        [InlineData(2, 0, 3)]
        [InlineData(2, 1, -2)]
        [InlineData(2, 1, 2)]
        [InlineData(2, 2, -3)]
        [InlineData(2, 2, 1)]
        public void Write_should_throw_when_count_is_out_of_range(int sourceSize, int offset, int count)
        {
            var subject = CreateSubject();
            var source = new byte[sourceSize];

            Action action = () => subject.Write(source, offset, count);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("count");
        }

        [Fact]
        public void Write_should_throw_when_not_writable()
        {
            var subject = CreateSubject();
            var mockBuffer = Mock.Get(subject.Buffer);
            mockBuffer.SetupGet(b => b.IsReadOnly).Returns(true);
            var source = new byte[1];

            Action action = () => subject.Write(source, 0, 0);

            action.ShouldThrow<NotSupportedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Write_should_throw_when_offset_is_out_of_range(
            [Values(-1, 2)]
            int offset)
        {
            var subject = CreateSubject();
            var source = new byte[1];

            Action action = () => subject.Write(source, offset, 0);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("offset");
        }

        [Fact]
        public void Write_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();
            var source = new byte[1];

            Action action = () => subject.Write(source, 0, 0);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferStream");
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 2)]
        public void WriteByte_should_have_expected_effect(long initialPosition, long expectedPosition)
        {
            var subject = CreateSubject();
            var mockBuffer = Mock.Get(subject.Buffer);
            subject.Position = initialPosition;

            subject.WriteByte(4);

            subject.Position.Should().Be(expectedPosition);
            subject.Length.Should().Be(expectedPosition);
            mockBuffer.Verify(b => b.SetByte((int)initialPosition, 4), Times.Once);
        }

        [Fact]
        public void WriteByte_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.WriteByte(0);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferStream");
        }

        [Theory]
        [InlineData(1, 1, "", new byte[] { 0 })]
        [InlineData(2, 1, "a", new byte[] { 97, 0 })]
        [InlineData(2, 2, "a", new byte[] { 97, 0 })]
        [InlineData(3, 1, "ab", new byte[] { 97, 98, 0 })]
        [InlineData(3, 2, "ab", new byte[] { 97, 98, 0 })]
        [InlineData(4, 1, "abc", new byte[] { 97, 98, 99, 0 })]
        [InlineData(4, 2, "abc", new byte[] { 97, 98, 99, 0 })]
        public void WriteCString_should_have_expected_effect(int length, int numberOfChunks, string value, byte[] expectedBytes)
        {
            var maxLength = Utf8Encodings.Strict.GetMaxByteCount(value.Length) + 1;
            var subject = CreateSubject(0, CalculateChunkSizes(maxLength, numberOfChunks));

            subject.WriteCString(value);

            subject.Position = 0;
            subject.ReadBytes((int)subject.Length).Should().Equal(expectedBytes);
        }

        [Fact]
        public void WriteCString_should_have_expected_effect_when_tempUtf8_is_not_used()
        {
            var value = new string('a', 1024);
            var maxLength = Utf8Encodings.Strict.GetMaxByteCount(value.Length) + 1;
            var subject = CreateSubject(0, CalculateChunkSizes(maxLength, 2));
            var expectedBytes = Utf8Encodings.Strict.GetBytes(value).Concat(new byte[] { 0 }).ToArray();

            subject.WriteCString(value);

            subject.Position = 0;
            subject.ReadBytes((int)subject.Length).Should().Equal(expectedBytes);
        }

        [Fact]
        public void WriteCString_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.WriteCString("");

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferStream");
        }

        [Fact]
        public void WriteCString_should_throw_when_value_is_null()
        {
            var subject = CreateSubject();

            Action action = () => subject.WriteCString(null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("value");
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteCString_should_throw_when_value_contains_nulls(
            [Values(1, 2)]
            int numberOfChunks,
            [Values("\0", "a\0", "a\0b")]
            string value)
        {
            var maxLength = Utf8Encodings.Strict.GetMaxByteCount(value.Length) + 1;
            var subject = CreateSubject(0, CalculateChunkSizes(maxLength, numberOfChunks));

            Action action = () => subject.WriteCString(value);

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("value");
        }

        [Fact]
        public void WriteCString_should_throw_when_value_contains_nulls_and_tempUtf8_is_not_used()
        {
            var value = new string('a', 1024) + '\0';
            var maxLength = Utf8Encodings.Strict.GetMaxByteCount(value.Length) + 1;
            var subject = CreateSubject(0, CalculateChunkSizes(maxLength, 2));
            var expectedBytes = Utf8Encodings.Strict.GetBytes(value).Concat(new byte[] { 0 }).ToArray();

            Action action = () => subject.WriteCString(value);

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("value");
        }

        [Fact]
        public void WriteCStringBytes_should_have_expected_effect()
        {
            var subject = CreateSubject();
            var mockBuffer = Mock.Get(subject.Buffer);
            var value = new byte[] { 97, 98, 99 };

            subject.WriteCStringBytes(value);

            subject.Position.Should().Be(4);
            subject.Length.Should().Be(4);
            mockBuffer.Verify(b => b.SetBytes(0, value, 0, 3), Times.Once);
            mockBuffer.Verify(b => b.SetByte(3, 0), Times.Once);
        }

        [Fact]
        public void WriteCStringBytes_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.WriteCStringBytes(new byte[0]);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferStream");
        }

        [Fact]
        public void WriteCStringBytes_should_throw_when_value_is_null()
        {
            var subject = CreateSubject();

            Action action = () => subject.WriteCStringBytes(null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("value");
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteDecimal128_should_have_expected_effect(
            [Values("-1.0", "0.0", "1.0", "NaN", "-Infinity", "Infinity")]
            string valueString)
        {
            var value = Decimal128.Parse(valueString);
            var subject = CreateSubject();
            var mockBuffer = Mock.Get(subject.Buffer);
            var expectedBytes = new byte[16];
            Buffer.BlockCopy(BitConverter.GetBytes(value.GetIEEELowBits()), 0, expectedBytes, 0, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(value.GetIEEEHighBits()), 0, expectedBytes, 8, 8);

            subject.WriteDecimal128(value);

            subject.Position.Should().Be(16);
            subject.Length.Should().Be(16);
            mockBuffer.Verify(b => b.SetBytes(0, It.Is<byte[]>(x => x.SequenceEqual(expectedBytes.Take(8))), 0, 8), Times.Once);
            mockBuffer.Verify(b => b.SetBytes(8, It.Is<byte[]>(x => x.SequenceEqual(expectedBytes.Skip(8))), 0, 8), Times.Once);
        }

        [Fact]
        public void WriteDecimal128_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.WriteDecimal128(Decimal128.Zero);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferStream");
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteDouble_should_have_expected_effect(
            [Values(-1.0, 0.0, 1.0, double.Epsilon, double.MaxValue, double.MinValue, double.NaN, double.NegativeInfinity, double.PositiveInfinity)]
            double value)
        {
            var subject = CreateSubject();
            var mockBuffer = Mock.Get(subject.Buffer);
            var bytes = BitConverter.GetBytes(value);

            subject.WriteDouble(value);

            subject.Position.Should().Be(8);
            subject.Length.Should().Be(8);
            mockBuffer.Verify(b => b.SetBytes(0, It.Is<byte[]>(x => x.SequenceEqual(bytes)), 0, 8), Times.Once);
        }

        [Fact]
        public void WriteDouble_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.WriteDouble(0.0);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferStream");
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteInt32_should_have_expected_effect(
            [Values(1, 2)]
            int numberOfChunks,
            [Values(-1, 0, 1, int.MaxValue, int.MinValue)]
            int value)
        {
            var subject = CreateSubject(0, CalculateChunkSizes(4, numberOfChunks));
            var expectedBytes = BitConverter.GetBytes(value);

            subject.WriteInt32(value);

            subject.Position.Should().Be(4);
            subject.Length.Should().Be(4);
            subject.Position = 0;
            subject.ReadBytes(4).Should().Equal(expectedBytes);
        }

        [Fact]
        public void WriteInt32_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.WriteInt32(0);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferStream");
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteInt64_should_have_expected_effect(
            [Values(-1, 0, 1, long.MaxValue, long.MinValue)]
            long value)
        {
            var subject = CreateSubject();
            var mockBuffer = Mock.Get(subject.Buffer);
            var expectedBytes = BitConverter.GetBytes(value);

            subject.WriteInt64(value);

            subject.Position.Should().Be(8);
            subject.Length.Should().Be(8);
            mockBuffer.Verify(b => b.SetBytes(0, It.Is<byte[]>(x => x.SequenceEqual(expectedBytes)), 0, 8), Times.Once);
        }

        [Fact]
        public void WriteInt64_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.WriteInt64(0);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferStream");
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteWriteObjectIdshould_have_expected_effect(
            [Values(1, 2)]
            int numberOfChunks)
        {
            var subject = CreateSubject(0, CalculateChunkSizes(12, numberOfChunks));
            var bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var value = new ObjectId(bytes);

            subject.WriteObjectId(value);

            subject.Position.Should().Be(12);
            subject.Length.Should().Be(12);
            subject.Position = 0;
            subject.ReadBytes(12).Should().Equal(bytes);
        }

        [Fact]
        public void WriteObjectId_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.WriteObjectId(ObjectId.Empty);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferStream");
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteString_should_have_expected_effect(
            [Values(0, 1, 16, 1024)]
            int length,
            [Values(1, 2)]
            int numberOfChunks)
        {
            var value = new string('a', length);
            var encoding = Utf8Encodings.Strict;
            var maxLength = encoding.GetMaxByteCount(value.Length) + 5;
            var subject = CreateSubject(0, CalculateChunkSizes(maxLength, numberOfChunks));
            var expectedBytes = BitConverter.GetBytes(value.Length + 1).Concat(encoding.GetBytes(value)).Concat(new byte[] { 0 }).ToArray();
            var expectedLength = expectedBytes.Length;
            var expectedPosition = expectedLength;

            subject.WriteString(value, encoding);

            subject.Position.Should().Be(expectedPosition);
            subject.Length.Should().Be(expectedLength);
            subject.Position = 0;
            subject.ReadBytes(expectedLength).Should().Equal(expectedBytes);
        }

        [Fact]
        public void WriteString_should_throw_when_subject_is_disposed()
        {
            var subject = CreateDisposedSubject();

            Action action = () => subject.WriteString("", Utf8Encodings.Strict);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("ByteBufferStream");
        }

        // helper methods
        private IEnumerable<int> CalculateChunkSizes(int length, int numberOfChunks)
        {
            var chunkSizes = new List<int>();

            var bytesPerChunk = length / numberOfChunks;
            if (bytesPerChunk == 0)
            {
                bytesPerChunk = 1;
                numberOfChunks = length;
            }

            for (var n = 1; n <= numberOfChunks; n++)
            {
                var chunkSize = n < numberOfChunks ? bytesPerChunk : length - bytesPerChunk * (numberOfChunks - 1);
                chunkSizes.Add(chunkSize);
            }

            return chunkSizes;
        }

        private IEnumerable<IBsonChunk> CreateChunks(byte[] bytes, IEnumerable<int> chunkSizes)
        {
            var offset = 0;
            foreach (var size in chunkSizes)
            {
                if (size > 0)
                {
                    var chunk = new ByteArrayChunk(size);
                    Buffer.BlockCopy(bytes, offset, chunk.Bytes.Array, chunk.Bytes.Offset, size);
                    yield return chunk;
                    offset += size;
                }
            }
        }

        private IEnumerable<IBsonChunk> CreateChunks(IEnumerable<int> chunkSizes)
        {
            return chunkSizes.Select(size => new ByteArrayChunk(size));
        }

        private ByteBufferStream CreateDisposedSubject()
        {
            var subject = CreateSubject();
            subject.Dispose();
            return subject;
        }

        private ByteBufferStream CreateSubject()
        {
            var mockBuffer = new Mock<IByteBuffer>();
            return new ByteBufferStream(mockBuffer.Object);
        }

        private ByteBufferStream CreateSubject(byte[] bytes)
        {
            return CreateSubject(bytes, new[] { bytes.Length });
        }

        private ByteBufferStream CreateSubject(byte[] bytes, IEnumerable<int> chunkSizes)
        {
            return CreateSubject(bytes.Length, CreateChunks(bytes, chunkSizes));
        }

        private ByteBufferStream CreateSubject(byte[] bytes, int initialChunkSize, IEnumerable<int> additionalChunkSizes)
        {
            return CreateSubject(bytes, new[] { initialChunkSize }.Concat(additionalChunkSizes));
        }

        private ByteBufferStream CreateSubject(byte[] bytes, int numberOfChunks)
        {
            return CreateSubject(bytes, CalculateChunkSizes(bytes.Length, numberOfChunks));
        }

        private ByteBufferStream CreateSubject(int length, IEnumerable<IBsonChunk> chunks)
        {
            IByteBuffer buffer;
            if (chunks.Count() == 1)
            {
                var chunk = chunks.First();
                buffer = new SingleChunkBuffer(chunk, length);
            }
            else
            {
                buffer = new MultiChunkBuffer(chunks, length);
            }

            return new ByteBufferStream(buffer, ownsBuffer: true);
        }

        private ByteBufferStream CreateSubject(int length, IEnumerable<int> chunkSizes)
        {
            return CreateSubject(length, CreateChunks(chunkSizes));
        }

        private ByteBufferStream CreateSubject(int length, int numberOfChunks)
        {
            return CreateSubject(length, CalculateChunkSizes(length, numberOfChunks));
        }

        // nested types
        private class Reflector
        {
            private readonly ByteBufferStream _instance;

            public Reflector(ByteBufferStream instance)
            {
                _instance = instance;
            }

            public bool _disposed
            {
                get
                {
                    var field = typeof(ByteBufferStream).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (bool)field.GetValue(_instance);
                }
            }

            public bool _ownsBuffer
            {
                get
                {
                    var field = typeof(ByteBufferStream).GetField("_ownsBuffer", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (bool)field.GetValue(_instance);
                }
            }
        }
    }
}
