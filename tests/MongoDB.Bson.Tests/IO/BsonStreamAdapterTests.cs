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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Bson.Tests
{
    public class BsonStreamAdapterTests
    {
        // test methods
        [Fact]
        public void BaseStream_should_return_expected_resut()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);

            var result = subject.BaseStream;

            result.Should().Be(mockStream.Object);
        }

        [Fact]
        public void BaseStream_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            subject.Dispose();

            Action action = () => { var _ = subject.BaseStream; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

#if NET45
        [Fact]
        public void BeginRead_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            var buffer = new byte[0];
            var offset = 1;
            var count = 2;
            var mockCallback = new Mock<AsyncCallback>();
            var state = new object();
            var mockAsyncResult = new Mock<IAsyncResult>();
            mockStream.Setup(s => s.BeginRead(buffer, offset, count, mockCallback.Object, state)).Returns(mockAsyncResult.Object);

            var result = subject.BeginRead(buffer, offset, count, mockCallback.Object, state);

            result.Should().BeSameAs(mockAsyncResult.Object);
            mockStream.Verify(s => s.BeginRead(buffer, offset, count, mockCallback.Object, state), Times.Once);
        }
#endif

#if NET45
        [Fact]
        public void BeginRead_should_throw_when_subject_is_diposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            var buffer = new byte[0];
            var offset = 1;
            var count = 2;
            var mockCallback = new Mock<AsyncCallback>();
            var state = new object();
            subject.Dispose();

            Action action = () => subject.BeginRead(buffer, offset, count, mockCallback.Object, state);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }
#endif

#if NET45
        [Fact]
        public void BeginWrite_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            var buffer = new byte[0];
            var offset = 1;
            var count = 2;
            var mockCallback = new Mock<AsyncCallback>();
            var state = new object();
            var mockAsyncResult = new Mock<IAsyncResult>();
            mockStream.Setup(s => s.BeginWrite(buffer, offset, count, mockCallback.Object, state)).Returns(mockAsyncResult.Object);

            var result = subject.BeginWrite(buffer, offset, count, mockCallback.Object, state);

            result.Should().BeSameAs(mockAsyncResult.Object);
            mockStream.Verify(s => s.BeginWrite(buffer, offset, count, mockCallback.Object, state), Times.Once);
        }
#endif

#if NET45
        [Fact]
        public void BeginWrite_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            var buffer = new byte[0];
            var offset = 1;
            var count = 2;
            var mockCallback = new Mock<AsyncCallback>();
            var state = new object();
            subject.Dispose();

            Action action = () => subject.BeginWrite(buffer, offset, count, mockCallback.Object, state);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }
#endif

        [Theory]
        [ParameterAttributeData]
        public void CanRead_should_call_wrapped_stream(
            [Values(false, true)]
            bool canRead)
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            mockStream.SetupGet(s => s.CanRead).Returns(canRead);

            var result = subject.CanRead;

            result.Should().Be(canRead);
            mockStream.VerifyGet(s => s.CanRead, Times.Once);
        }

        [Fact]
        public void CanRead_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            subject.Dispose();

            Action action = () => { var _ = subject.CanRead; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Theory]
        [ParameterAttributeData]
        public void CanSeek_should_call_wrapped_stream(
            [Values(false, true)]
            bool canSeek)
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            mockStream.SetupGet(s => s.CanSeek).Returns(canSeek);

            var result = subject.CanSeek;

            result.Should().Be(canSeek);
            mockStream.VerifyGet(s => s.CanSeek, Times.Once);
        }

        [Fact]
        public void CanSeek_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            subject.Dispose();

            Action action = () => { var _ = subject.CanSeek; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Theory]
        [ParameterAttributeData]
        public void CanTimeout_should_call_wrapped_stream(
            [Values(false, true)]
            bool canTimeout)
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            mockStream.SetupGet(s => s.CanTimeout).Returns(canTimeout);

            var result = subject.CanTimeout;

            result.Should().Be(canTimeout);
            mockStream.VerifyGet(s => s.CanTimeout, Times.Once);
        }

        [Fact]
        public void CanTimeout_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            subject.Dispose();

            Action action = () => { var _ = subject.CanTimeout; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Theory]
        [ParameterAttributeData]
        public void CanWrite_should_call_wrapped_stream(
            [Values(false, true)]
            bool canWrite)
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            mockStream.SetupGet(s => s.CanWrite).Returns(canWrite);

            var result = subject.CanWrite;

            result.Should().Be(canWrite);
            mockStream.VerifyGet(s => s.CanWrite, Times.Once);
        }

        [Fact]
        public void CanWrite_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            subject.Dispose();

            Action action = () => { var _ = subject.CanWrite; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

#if NET45
        [Fact]
        public void Close_can_be_called_multiple_times()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);

            subject.Close();
            subject.Close();

            var subjectReflector = new Reflector(subject);
            subjectReflector._disposed.Should().BeTrue();
        }
#endif

#if NET45
        [Fact]
        public void Close_should_dispose_subject()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);

            subject.Close();

            var subjectReflector = new Reflector(subject);
            subjectReflector._disposed.Should().BeTrue();
        }
#endif

        [Fact]
        public void constructor_should_use_false_as_the_default_value_for_ownsStream()
        {
            var mockStream = new Mock<Stream>();

            var subject = new BsonStreamAdapter(mockStream.Object);

            var subjectReflector = new Reflector(subject);
            subjectReflector._ownsStream.Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_should_initialize_instance(
            [Values(false, true)]
            bool ownsStream)
        {
            var mockStream = new Mock<Stream>();

            var subject = new BsonStreamAdapter(mockStream.Object, ownsStream: ownsStream);

            var subjectReflector = new Reflector(subject);
            subjectReflector._disposed.Should().BeFalse();
            subjectReflector._ownsStream.Should().Be(ownsStream);
            subjectReflector._stream.Should().Be(mockStream.Object);
            subjectReflector._temp.Should().NotBeNull();
            subjectReflector._tempUtf8.Should().NotBeNull();
        }

        [Fact]
        public void constructor_should_throw_when_stream_is_null()
        {
            Action action = () => new BsonStreamAdapter(null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("stream");
        }

        [Fact]
        public void CopyToAsync_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            var mockDestination = new Mock<Stream>();
            var bufferSize = 1;
            var cancellationToken = new CancellationTokenSource().Token;
            var task = new TaskCompletionSource<object>().Task;
            mockStream.Setup(s => s.CopyToAsync(mockDestination.Object, bufferSize, cancellationToken)).Returns(task);

            var result = subject.CopyToAsync(mockDestination.Object, bufferSize, cancellationToken);

            result.Should().Be(task);
            mockStream.Verify(s => s.CopyToAsync(mockDestination.Object, bufferSize, cancellationToken), Times.Once);
        }

        [Fact]
        public void CopyToAsync_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            var mockDestination = new Mock<Stream>();
            var bufferSize = 1;
            var cancellationToken = new CancellationTokenSource().Token;
            subject.Dispose();

            Action action = () => subject.CopyToAsync(mockDestination.Object, bufferSize, cancellationToken);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Fact]
        public void Dispose_can_be_called_multiple_times()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);

            subject.Dispose();
            subject.Dispose();

            var subjectReflector = new Reflector(subject);
            subjectReflector._disposed.Should().BeTrue();
        }

#if NET45
        [Fact]
        public void Dispose_should_dispose_stream_once_when_Disposed_is_called_more_than_once()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object, ownsStream: true);

            subject.Dispose();
            subject.Dispose();

            mockStream.Verify(s => s.Close(), Times.Once); // Dispose is not virtual but calls virtual Close
        }
#endif

#if NET45
       [Theory]
        [ParameterAttributeData]
        public void Dispose_should_dispose_stream_only_when_it_owns_it(
            [Values(false, true)]
            bool ownsStream)
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object, ownsStream: ownsStream);

            subject.Dispose();

            mockStream.Verify(s => s.Close(), Times.Exactly(ownsStream ? 1 : 0)); // Dispose is not virtual but calls virtual Close
        }
#endif

        [Fact]
        public void Dispose_should_dispose_subject()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);

            subject.Dispose();

            var subjectReflector = new Reflector(subject);
            subjectReflector._disposed.Should().BeTrue();
        }

#if NET45
        [Fact]
        public void EndRead_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            var mockAsyncResult = new Mock<IAsyncResult>();
            var numberOfBytesRead = 1;
            mockStream.Setup(s => s.EndRead(mockAsyncResult.Object)).Returns(numberOfBytesRead);

            var result = subject.EndRead(mockAsyncResult.Object);

            result.Should().Be(numberOfBytesRead);
            mockStream.Verify(s => s.EndRead(mockAsyncResult.Object), Times.Once);
        }
#endif

#if NET45
        [Fact]
        public void EndRead_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            var mockAsyncResult = new Mock<IAsyncResult>();
            subject.Dispose();

            Action action = () => subject.EndRead(mockAsyncResult.Object);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }
#endif

#if NET45
        [Fact]
        public void EndWrite_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            var mockAsyncResult = new Mock<IAsyncResult>();

            subject.EndWrite(mockAsyncResult.Object);

            mockStream.Verify(s => s.EndWrite(mockAsyncResult.Object), Times.Once);
        }
#endif

#if NET45
        [Fact]
        public void EndWrite_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            var mockAsyncResult = new Mock<IAsyncResult>();
            subject.Dispose();

            Action action = () => subject.EndWrite(mockAsyncResult.Object);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }
#endif

        [Fact]
        public void Flush_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);

            subject.Flush();

            mockStream.Verify(s => s.Flush(), Times.Once);
        }

        [Fact]
        public void Flush_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            subject.Dispose();

            Action action = () => subject.Flush();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Fact]
        public void FlushAsync_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            var task = new TaskCompletionSource<object>().Task;
            var cancellationToken = new CancellationTokenSource().Token;
            mockStream.Setup(s => s.FlushAsync(cancellationToken)).Returns(task);

            var result = subject.FlushAsync(cancellationToken);

            result.Should().Be(task);
            mockStream.Verify(s => s.FlushAsync(cancellationToken), Times.Once);
        }

        [Fact]
        public void FlushAsync_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            var cancellationToken = new CancellationTokenSource().Token;
            subject.Dispose();

            Action action = () => subject.FlushAsync(cancellationToken);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Theory]
        [ParameterAttributeData]
        public void Length_get_should_call_wrapped_stream(
            [Values(0L, 1L, 2L)]
            long length)
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            mockStream.SetupGet(s => s.Length).Returns(length);

            var result = subject.Length;

            result.Should().Be(length);
            mockStream.VerifyGet(s => s.Length, Times.Once);
        }

        [Fact]
        public void Length_get_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            subject.Dispose();

            Action action = () => { var _ = subject.Length; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Theory]
        [ParameterAttributeData]
        public void Position_get_should_call_wrapped_stream(
            [Values(0, 1, 2)]
            long position)
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            mockStream.SetupGet(s => s.Position).Returns(position);

            var result = subject.Position;

            result.Should().Be(position);
            mockStream.VerifyGet(s => s.Position, Times.Once);
        }

        [Fact]
        public void Position_get_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            subject.Dispose();

            Action action = () => { var _ = subject.Position; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Theory]
        [ParameterAttributeData]
        public void Position_set_should_call_wrapped_stream(
            [Values(0, 1, 2)]
            long position)
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);

            subject.Position = position;

            mockStream.VerifySet(s => s.Position = position, Times.Once);
        }

        [Fact]
        public void Position_set_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            subject.Dispose();

            Action action = () => { subject.Position = 0; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Fact]
        public void Read_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            var buffer = new byte[3];
            var offset = 1;
            var count = 2;
            var numberOfBytesRead = 1;
            mockStream.Setup(s => s.Read(buffer, offset, count)).Returns(numberOfBytesRead);

            var result = subject.Read(buffer, offset, count);

            result.Should().Be(numberOfBytesRead);
            mockStream.Verify(s => s.Read(buffer, offset, count), Times.Once);
        }

        [Fact]
        public void Read_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            var buffer = new byte[3];
            var offset = 1;
            var count = 2;
            subject.Dispose();

            Action action = () => subject.Read(buffer, offset, count);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Fact]
        public void ReadAsync_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            var task = new TaskCompletionSource<int>().Task;
            var buffer = new byte[3];
            var offset = 1;
            var count = 2;
            var cancellationToken = new CancellationTokenSource().Token;
            mockStream.Setup(s => s.ReadAsync(buffer, offset, count, cancellationToken)).Returns(task);

            var result = subject.ReadAsync(buffer, offset, count, cancellationToken);

            result.Should().Be(task);
            mockStream.Verify(s => s.ReadAsync(buffer, offset, count, cancellationToken), Times.Once);
        }

        [Fact]
        public void ReadAsync_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            var buffer = new byte[3];
            var offset = 1;
            var count = 2;
            var cancellationToken = new CancellationTokenSource().Token;
            subject.Dispose();

            Action action = () => subject.ReadAsync(buffer, offset, count, cancellationToken);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Fact]
        public void ReadByte_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            mockStream.Setup(s => s.ReadByte()).Returns(1);

            var result = subject.ReadByte();

            result.Should().Be(1);
            mockStream.Verify(s => s.ReadByte(), Times.Once);
        }

        [Fact]
        public void ReadByte_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            subject.Dispose();

            Action action = () => subject.ReadByte();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Theory]
        [InlineData(new byte[] { 0 }, "")]
        [InlineData(new byte[] { 97, 0 }, "a")]
        [InlineData(new byte[] { 97, 98, 0 }, "ab")]
        [InlineData(new byte[] { 97, 98, 99, 0 }, "abc")]
        public void ReadCString_should_return_expected_result(byte[] bytes, string expectedResult)
        {
            var stream = new MemoryStream(bytes);
            var subject = new BsonStreamAdapter(stream);

            var result = subject.ReadCString(Utf8Encodings.Strict);

            result.Should().Be(expectedResult);
            subject.Position.Should().Be(bytes.Length);
        }

        [Fact]
        public void ReadCString_should_throw_when_encoding_is_null()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);

            Action action = () => subject.ReadCString(null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("encoding");
        }

        [Fact]
        public void ReadCString_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            subject.Dispose();

            Action action = () => subject.ReadCString(Utf8Encodings.Strict);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Theory]
        [InlineData(new byte[] { 0 }, "")]
        [InlineData(new byte[] { 97, 0 }, "a")]
        [InlineData(new byte[] { 97, 98, 0 }, "ab")]
        [InlineData(new byte[] { 97, 98, 99, 0 }, "abc")]
        public void ReadCStringBytes_should_return_expected_result(byte[] bytes, string expectedResult)
        {
            var stream = new MemoryStream(bytes);
            var subject = new BsonStreamAdapter(stream);

            var result = subject.ReadCStringBytes();

            result.Array.Skip(result.Offset).Take(result.Count).Should().Equal(bytes.Take(bytes.Length - 1));
            subject.Position.Should().Be(bytes.Length);
        }

        [Fact]
        public void ReadCStringBytes_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            subject.Dispose();

            Action action = () => subject.ReadCStringBytes();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Fact]
        public void ReadCStringBytes_should_throw_when_terminating_null_byte_is_missing()
        {
            var bytes = new byte[] { 0, 97, 98 };
            var stream = new MemoryStream(bytes);
            var subject = new BsonStreamAdapter(stream);
            subject.SetLength(3);
            subject.Position = 1;

            Action action = () => subject.ReadCStringBytes();

            action.ShouldThrow<EndOfStreamException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadDecimal128_should_return_expected_result(
            [Values("-1.0", "0.0", "1.0", "NaN", "-Infinity", "Infinity")]
            string valueString
            )
        {
            var value = Decimal128.Parse(valueString);
            var bytes = new byte[16];
            Buffer.BlockCopy(BitConverter.GetBytes(value.GetIEEELowBits()), 0, bytes, 0, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(value.GetIEEEHighBits()), 0, bytes, 8, 8);
            var stream = new MemoryStream(bytes);
            var subject = new BsonStreamAdapter(stream);

            var result = subject.ReadDecimal128();

            result.Should().Be(value);
            subject.Position.Should().Be(16);
        }

        [Fact]
        public void ReadDecimal128_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            subject.Dispose();

            Action action = () => subject.ReadDecimal128();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadDouble_should_return_expected_result(
            [Values(-1.0, 0.0, 1.0, double.Epsilon, double.MaxValue, double.MinValue, double.NaN, double.NegativeInfinity, double.PositiveInfinity)]
            double value
            )
        {
            var bytes = BitConverter.GetBytes(value);
            var stream = new MemoryStream(bytes);
            var subject = new BsonStreamAdapter(stream);

            var result = subject.ReadDouble();

            result.Should().Be(value);
            subject.Position.Should().Be(8);
        }

        [Fact]
        public void ReadDouble_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            subject.Dispose();

            Action action = () => subject.ReadDouble();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Fact]
        public void ReadInt32_should_be_little_endian()
        {
            var bytes = new byte[] { 1, 2, 3, 4 };
            var stream = new MemoryStream(bytes);
            var subject = new BsonStreamAdapter(stream);

            var result = subject.ReadInt32();

            result.Should().Be(0x04030201);
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadInt32_should_return_expected_result(
            [Values(-1, 0, 1, int.MaxValue, int.MinValue)]
            int value)
        {
            var bytes = BitConverter.GetBytes(value);
            var stream = new MemoryStream(bytes);
            var subject = new BsonStreamAdapter(stream);

            var result = subject.ReadInt32();

            result.Should().Be(value);
            subject.Position.Should().Be(4);
        }

        [Fact]
        public void ReadInt32_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            subject.Dispose();

            Action action = () => subject.ReadInt32();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Fact]
        public void ReadInt64_should_be_little_endian()
        {
            var bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            var stream = new MemoryStream(bytes);
            var subject = new BsonStreamAdapter(stream);

            var result = subject.ReadInt64();

            result.Should().Be(0x0807060504030201);
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadInt64_should_return_expected_result(
            [Values(-1, 0, 1, long.MaxValue, long.MinValue)]
            long value)
        {
            var bytes = BitConverter.GetBytes(value);
            var stream = new MemoryStream(bytes);
            var subject = new BsonStreamAdapter(stream);

            var result = subject.ReadInt64();

            result.Should().Be(value);
            subject.Position.Should().Be(8);
        }

        [Fact]
        public void ReadInt64_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            subject.Dispose();

            Action action = () => subject.ReadInt64();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Fact]
        public void ReadObjectId_should_be_big_endian()
        {
            var bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var stream = new MemoryStream(bytes);
            var subject = new BsonStreamAdapter(stream);
            var expectedResult = new ObjectId(0x01020304, 0x050607, 0x0809, 0x0a0b0c);

            var result = subject.ReadObjectId();

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void ReadObjectId_should_return_expected_result()
        {
            var objectId = ObjectId.GenerateNewId();
            var bytes = objectId.ToByteArray();
            var stream = new MemoryStream(bytes);
            var subject = new BsonStreamAdapter(stream);

            var result = subject.ReadObjectId();

            result.Should().Be(objectId);
            subject.Position.Should().Be(12);
        }

        [Fact]
        public void ReadObjectId_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            subject.Dispose();

            Action action = () => subject.ReadObjectId();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Fact]
        public void ReadSlice_should_return_expected_result()
        {
            var bytes = new byte[] { 7, 0, 0, 0, 1, 2, 3 };
            var stream = new MemoryStream(bytes);
            var subject = new BsonStreamAdapter(stream);

            var result = subject.ReadSlice();

            result.IsReadOnly.Should().BeTrue();
            var segment = result.AccessBackingBytes(0);
            segment.Array.Skip(segment.Offset).Take(segment.Count).Should().Equal(bytes);
            subject.Position.Should().Be(7);
        }

        [Fact]
        public void ReadSlice_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            subject.Dispose();

            Action action = () => subject.ReadSlice();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }


        [Theory]
        [InlineData(new byte[] { 1, 0, 0, 0, 0 }, "")]
        [InlineData(new byte[] { 2, 0, 0, 0, 97, 0 }, "a")]
        [InlineData(new byte[] { 3, 0, 0, 0, 97, 98, 0 }, "ab")]
        [InlineData(new byte[] { 4, 0, 0, 0, 97, 98, 99, 0 }, "abc")]
        public void ReadString_should_return_expected_result(byte[] bytes, string expectedResult)
        {
            var stream = new MemoryStream(bytes);
            var subject = new BsonStreamAdapter(stream);

            var result = subject.ReadString(Utf8Encodings.Strict);

            result.Should().Be(expectedResult);
            subject.Position.Should().Be(bytes.Length);
        }

        [Fact]
        public void ReadString_should_throw_when_encoding_is_null()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);

            Action action = () => subject.ReadString(null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("encoding");
        }

        [Fact]
        public void ReadString_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            subject.Dispose();

            Action action = () => subject.ReadString(Utf8Encodings.Strict);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Fact]
        public void ReadString_should_throw_when_terminating_null_byte_is_missing()
        {
            var bytes = new byte[] { 2, 0, 0, 0, 97, 1 };
            var stream = new MemoryStream(bytes);
            var subject = new BsonStreamAdapter(stream);

            Action action = () => subject.ReadString(Utf8Encodings.Strict);

            action.ShouldThrow<FormatException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadTimeout_get_should_call_wrapped_stream(
            [Values(0, 1, 2)]
            int readTimeout)
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            mockStream.SetupGet(s => s.ReadTimeout).Returns(readTimeout);

            var result = subject.ReadTimeout;

            result.Should().Be(readTimeout);
            mockStream.VerifyGet(s => s.ReadTimeout, Times.Once);
        }

        [Fact]
        public void ReadTimeout_get_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            subject.Dispose();

            Action action = () => { var _ = subject.ReadTimeout; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadTimeout_set_should_call_wrapped_stream(
            [Values(0, 1, 2)]
            int readTimeout)
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);

            subject.ReadTimeout = readTimeout;

            mockStream.VerifySet(s => s.ReadTimeout = readTimeout);
        }

        [Fact]
        public void ReadTimeout_set_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            subject.Dispose();

            Action action = () => { subject.ReadTimeout = 0; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Fact]
        public void Seek_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            var offset = 1L;
            var origin = SeekOrigin.Current;
            var newPosition = 2L;
            mockStream.Setup(s => s.Seek(offset, origin)).Returns(newPosition);

            var result = subject.Seek(offset, origin);

            result.Should().Be(newPosition);
            mockStream.Verify(s => s.Seek(offset, origin), Times.Once);
        }

        [Fact]
        public void Seek_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            var offset = 1L;
            var origin = SeekOrigin.Current;
            subject.Dispose();

            Action action = () => subject.Seek(offset, origin);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Fact]
        public void SetLength_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            var length = 1L;

            subject.SetLength(length);

            mockStream.Verify(s => s.SetLength(length), Times.Once);
        }

        [Fact]
        public void SetLength_should_throw_when_subject_is_diposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            var length = 1L;
            subject.Dispose();

            Action action = () => subject.SetLength(length);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Theory]
        [InlineData(new byte[] { 0 })]
        [InlineData(new byte[] { 97, 0 })]
        [InlineData(new byte[] { 97, 98, 0 })]
        [InlineData(new byte[] { 97, 98, 99, 0 })]
        public void SkipCString_should_skip_cstring(byte[] bytes)
        {
            var stream = new MemoryStream(bytes);
            var subject = new BsonStreamAdapter(stream);

            subject.SkipCString();

            subject.Position.Should().Be(bytes.Length);
        }

        [Fact]
        public void SkipCString_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            subject.Dispose();

            Action action = () => subject.SkipCString();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Fact]
        public void SkipCString_should_throw_when_terminating_null_byte_is_missing()
        {
            var bytes = new byte[] { 97 };
            var stream = new MemoryStream(bytes);
            var subject = new BsonStreamAdapter(stream);

            Action action = () => subject.SkipCString();

            action.ShouldThrow<EndOfStreamException>();
        }

        [Fact]
        public void Write_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            var buffer = new byte[0];
            var offset = 1;
            var count = 2;

            subject.Write(buffer, offset, count);

            mockStream.Verify(s => s.Write(buffer, offset, count), Times.Once);
        }

        [Fact]
        public void Write_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            var buffer = new byte[0];
            var offset = 1;
            var count = 2;
            subject.Dispose();

            Action action = () => subject.Write(buffer, offset, count);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Fact]
        public void WriteAsync_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            var task = new TaskCompletionSource<object>().Task;
            var buffer = new byte[0];
            var offset = 1;
            var count = 2;
            var cancellationToken = new CancellationTokenSource().Token;
            mockStream.Setup(s => s.WriteAsync(buffer, offset, count, cancellationToken)).Returns(task);

            var result = subject.WriteAsync(buffer, offset, count, cancellationToken);

            result.Should().Be(task);
            mockStream.Verify(s => s.WriteAsync(buffer, offset, count, cancellationToken), Times.Once);
        }

        [Fact]
        public void WriteAsync_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            var task = new TaskCompletionSource<object>().Task;
            var buffer = new byte[0];
            var offset = 1;
            var count = 2;
            var cancellationToken = new CancellationTokenSource().Token;
            subject.Dispose();

            Action action = () => subject.WriteAsync(buffer, offset, count, cancellationToken);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Fact]
        public void WriteByte_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            var value = (byte)97;

            subject.WriteByte(value);

            mockStream.Verify(s => s.WriteByte(value), Times.Once);
        }

        [Fact]
        public void WriteByte_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            var value = (byte)97;
            subject.Dispose();

            Action action = () => subject.WriteByte(value);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Fact]
        public void WriteCString_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            var value = "abc";
            subject.Dispose();

            Action action = () => subject.WriteCString(value);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Fact]
        public void WriteCString_should_throw_when_value_contains_nulls()
        {
            var stream = new MemoryStream();
            var subject = new BsonStreamAdapter(stream);
            var value = "a\0b";

            Action action = () => subject.WriteCString(value);

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("value");
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteCString_should_throw_when_value_with_maxByteCount_near_tempUtf8_contains_nulls(
            [Values(-1, 0, 1)] int delta)
        {
            var stream = new MemoryStream();
            var subject = new BsonStreamAdapter(stream);
            var subjectReflector = new Reflector(subject);
            var valueLength = (subjectReflector._tempUtf8.Length / 3) + delta;
            var length1 = valueLength / 2;
            var length2 = valueLength - length1 - 1;
            var value = new string('x', length1) + '\0' + new string('x', length2);

            Action action = () => subject.WriteCString(value);

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("value");
        }

        [Fact]
        public void WriteCString_should_throw_when_value_is_null()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);

            Action action = () => subject.WriteCString(null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("value");
        }

        [Theory]
        [InlineData("", new byte[] { 0 })]
        [InlineData("a", new byte[] { 97, 0 })]
        [InlineData("ab", new byte[] { 97, 98, 0 })]
        [InlineData("abc", new byte[] { 97, 98, 99, 0 })]
        public void WriteCString_should_write_expected_bytes(string value, byte[] expectedBytes)
        {
            var stream = new MemoryStream();
            var subject = new BsonStreamAdapter(stream);

            subject.WriteCString(value);

            stream.ToArray().Should().Equal(expectedBytes);
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteCString_should_write_expected_bytes_when_maxByteCount_is_near_tempUtf8_length(
            [Values(-1, 0, 1)]
            int delta)
        {
            var stream = new MemoryStream();
            var subject = new BsonStreamAdapter(stream);
            var subjectReflector = new Reflector(subject);
            var valueLength = (subjectReflector._tempUtf8.Length / 3) + delta;
            var value = new string('a', valueLength);
            var expectedBytes = Enumerable.Repeat<byte>(97, valueLength).Concat(new byte[] { 0 }).ToArray();

            subject.WriteCString(value);

            stream.ToArray().Should().Equal(expectedBytes);
        }

        [Fact]
        public void WriteCStringBytes_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            var value = new byte[0];
            subject.Dispose();

            Action action = () => subject.WriteCStringBytes(value);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Fact]
        public void WriteCStringBytes_should_throw_when_value_is_null()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);

            Action action = () => subject.WriteCStringBytes(null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("value");
        }

        [Theory]
        [InlineData(new byte[] { })]
        [InlineData(new byte[] { 97 })]
        [InlineData(new byte[] { 97, 98 })]
        [InlineData(new byte[] { 97, 98, 99 })]
        public void WriteCStringBytes_should_write_expected_bytes(byte[] value)
        {
            var stream = new MemoryStream();
            var subject = new BsonStreamAdapter(stream);

            subject.WriteCStringBytes(value);

            stream.ToArray().Should().Equal(value.Concat(new byte[] { 0 }));
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteDecimal128_should_write_expected_bytes(
            [Values("-1.0", "0.0", "1.0", "NaN", "-Infinity", "Infinity")]
            string valueString)
        {
            var value = Decimal128.Parse(valueString);
            var stream = new MemoryStream();
            var subject = new BsonStreamAdapter(stream);
            var expectedBytes = new byte[16];
            Buffer.BlockCopy(BitConverter.GetBytes(value.GetIEEELowBits()), 0, expectedBytes, 0, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(value.GetIEEEHighBits()), 0, expectedBytes, 8, 8);

            subject.WriteDecimal128(value);

            stream.ToArray().Should().Equal(expectedBytes);
        }

        [Fact]
        public void WriteDecimal128_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            subject.Dispose();

            Action action = () => subject.WriteDecimal128(Decimal128.Zero);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteDouble_should_write_expected_bytes(
           [Values(-1.0, 0.0, 1.0, double.Epsilon, double.MaxValue, double.MinValue, double.NaN, double.NegativeInfinity, double.PositiveInfinity)]
            double value)
        {
            var stream = new MemoryStream();
            var subject = new BsonStreamAdapter(stream);
            var expectedBytes = BitConverter.GetBytes(value);

            subject.WriteDouble(value);

            stream.ToArray().Should().Equal(expectedBytes);
        }

        [Fact]
        public void WriteDouble_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            subject.Dispose();

            Action action = () => subject.WriteDouble(1.0);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Fact]
        public void WriteInt32_should_be_little_endian()
        {
            var stream = new MemoryStream();
            var subject = new BsonStreamAdapter(stream);
            var value = 0x01020304;
            var expectedBytes = new byte[] { 4, 3, 2, 1 };

            subject.WriteInt32(value);

            stream.ToArray().Should().Equal(expectedBytes);
        }

        [Fact]
        public void WriteInt32_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            subject.Dispose();

            Action action = () => subject.WriteInt32(1);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteInt32_should_write_expected_bytes(
            [Values(-1, 0, 1, int.MaxValue, int.MinValue)]
            int value)
        {
            var stream = new MemoryStream();
            var subject = new BsonStreamAdapter(stream);
            var expectedBytes = BitConverter.GetBytes(value);

            subject.WriteInt32(value);

            stream.ToArray().Should().Equal(expectedBytes);
        }

        [Fact]
        public void WriteInt64_should_be_little_endian()
        {
            var stream = new MemoryStream();
            var subject = new BsonStreamAdapter(stream);
            var value = 0x0102030405060708;
            var expectedBytes = new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 };

            subject.WriteInt64(value);

            stream.ToArray().Should().Equal(expectedBytes);
        }

        [Fact]
        public void WriteInt64_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            subject.Dispose();

            Action action = () => subject.WriteInt64(1);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteInt64_should_write_expected_bytes(
            [Values(-1L, 0L, 1L, long.MaxValue, long.MinValue)]
            long value)
        {
            var stream = new MemoryStream();
            var subject = new BsonStreamAdapter(stream);
            var expectedBytes = BitConverter.GetBytes(value);

            subject.WriteInt64(value);

            stream.ToArray().Should().Equal(expectedBytes);
        }

        [Fact]
        public void WriteObjectId_should_be_big_endian()
        {
            var stream = new MemoryStream();
            var subject = new BsonStreamAdapter(stream);
            var value = new ObjectId(0x01020304, 0x050607, 0x0809, 0x0a0b0c);
            var expectedBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

            subject.WriteObjectId(value);

            stream.ToArray().Should().Equal(expectedBytes);
        }

        [Fact]
        public void WriteObjectId_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            subject.Dispose();

            Action action = () => subject.WriteObjectId(ObjectId.Empty);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Fact]
        public void WriteObjectId_should_write_expected_bytes()
        {
            var stream = new MemoryStream();
            var subject = new BsonStreamAdapter(stream);
            var value = ObjectId.GenerateNewId();
            var expectedBytes = value.ToByteArray();

            subject.WriteObjectId(value);

            stream.ToArray().Should().Equal(expectedBytes);
        }

        [Fact]
        public void WriteString_should_throw_when_encoding_is_null()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            var value = "abc";

            Action action = () => subject.WriteString(value, null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("encoding");
        }

        [Fact]
        public void WriteString_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            var value = "abc";
            subject.Dispose();

            Action action = () => subject.WriteString(value, Utf8Encodings.Strict);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Fact]
        public void WriteString_should_throw_when_value_is_null()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);

            Action action = () => subject.WriteString(null, Utf8Encodings.Strict);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("value");
        }

        [Theory]
        [InlineData("", new byte[] { 1, 0, 0, 0, 0 })]
        [InlineData("a", new byte[] { 2, 0, 0, 0, 97, 0 })]
        [InlineData("ab", new byte[] { 3, 0, 0, 0, 97, 98, 0 })]
        [InlineData("abc", new byte[] { 4, 0, 0, 0, 97, 98, 99, 0 })]
        [InlineData("a\0c", new byte[] { 4, 0, 0, 0, 97, 0, 99, 0 })]
        public void WriteString_should_write_expected_bytes(string value, byte[] expectedBytes)
        {
            var stream = new MemoryStream();
            var subject = new BsonStreamAdapter(stream);

            subject.WriteString(value, Utf8Encodings.Strict);

            stream.ToArray().Should().Equal(expectedBytes);
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteString_should_write_expected_bytes_when_size_is_near_tempUtf8_length(
            [Values(-1, 0, 1)]
            int delta)
        {
            var stream = new MemoryStream();
            var subject = new BsonStreamAdapter(stream);
            var subjectReflector = new Reflector(subject);
            var valueLength = ((subjectReflector._tempUtf8.Length - 5) / 3) + delta;
            var value = new string('a', valueLength);
            var expectedBytes = BitConverter.GetBytes(valueLength + 1)
                .Concat(Enumerable.Repeat<byte>(97, valueLength))
                .Concat(new byte[] { 0 })
                .ToArray();

            subject.WriteString(value, Utf8Encodings.Strict);

            stream.ToArray().Should().Equal(expectedBytes);
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteTimeout_get_should_call_wrapped_stream(
            [Values(0, 1, 2)]
            int writeTimeout)
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            mockStream.SetupGet(s => s.WriteTimeout).Returns(writeTimeout);

            var result = subject.WriteTimeout;

            result.Should().Be(writeTimeout);
            mockStream.VerifyGet(s => s.WriteTimeout, Times.Once);
        }

        [Fact]
        public void WriteTimeout_get_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            subject.Dispose();

            Action action = () => { var _ = subject.WriteTimeout; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteTimeout_set_should_call_wrapped_stream(
            [Values(0, 1, 2)]
            int writeTimeout)
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);

            subject.WriteTimeout = writeTimeout;

            mockStream.VerifySet(s => s.WriteTimeout = writeTimeout);
        }

        [Fact]
        public void WriteTimeout_set_should_throw_when_subject_is_disposed()
        {
            var mockStream = new Mock<Stream>();
            var subject = new BsonStreamAdapter(mockStream.Object);
            subject.Dispose();

            Action action = () => { subject.WriteTimeout = 0; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        // nested types
        private class Reflector
        {
            // fields
            private readonly BsonStreamAdapter _instance;

            // constructors
            public Reflector(BsonStreamAdapter instance)
            {
                _instance = instance;
            }

            // properties
            public bool _disposed
            {
                get
                {
                    var field = typeof(BsonStreamAdapter).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (bool)field.GetValue(_instance);
                }
            }

            public bool _ownsStream
            {
                get
                {
                    var field = typeof(BsonStreamAdapter).GetField("_ownsStream", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (bool)field.GetValue(_instance);
                }
            }

            public Stream _stream
            {
                get
                {
                    var field = typeof(BsonStreamAdapter).GetField("_stream", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (Stream)field.GetValue(_instance);
                }
            }

            public byte[] _temp
            {
                get
                {
                    var field = typeof(BsonStreamAdapter).GetField("_temp", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (byte[])field.GetValue(_instance);
                }
            }

            public byte[] _tempUtf8
            {
                get
                {
                    var field = typeof(BsonStreamAdapter).GetField("_tempUtf8", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (byte[])field.GetValue(_instance);
                }
            }
        }
    }
}
