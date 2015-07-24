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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Mini.Bson.Tests
{
    [TestFixture]
    public class BsonStreamAdapterTests
    {
        // test methods
        [Test]
        public void BaseStream_should_return_expected_resut()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);

            var result = subject.BaseStream;

            result.Should().Be(stream);
        }

        [Test]
        public void BaseStream_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            subject.Dispose();

            Action action = () => { var _ = subject.BaseStream; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void BeginRead_should_call_wrapped_stream()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var buffer = new byte[0];
            var offset = 1;
            var count = 2;
            var callback = Substitute.For<AsyncCallback>();
            var state = new object();
            var asyncResult = Substitute.For<IAsyncResult>();
            stream.BeginRead(buffer, offset, count, callback, state).Returns(asyncResult);

            var result = subject.BeginRead(buffer, offset, count, callback, state);

            result.Should().Be(asyncResult);
            stream.Received(1).BeginRead(buffer, offset, count, callback, state);
        }

        [Test]
        public void BeginRead_should_throw_when_subject_is_diposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var buffer = new byte[0];
            var offset = 1;
            var count = 2;
            var callback = Substitute.For<AsyncCallback>();
            var state = new object();
            subject.Dispose();

            Action action = () => subject.BeginRead(buffer, offset, count, callback, state);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void BeginWrite_should_call_wrapped_stream()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var buffer = new byte[0];
            var offset = 1;
            var count = 2;
            var callback = Substitute.For<AsyncCallback>();
            var state = new object();
            var asyncResult = Substitute.For<IAsyncResult>();
            stream.BeginWrite(buffer, offset, count, callback, state).Returns(asyncResult);

            var result = subject.BeginWrite(buffer, offset, count, callback, state);

            result.Should().Be(asyncResult);
            stream.Received(1).BeginWrite(buffer, offset, count, callback, state);
        }

        [Test]
        public void BeginWrite_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var buffer = new byte[0];
            var offset = 1;
            var count = 2;
            var callback = Substitute.For<AsyncCallback>();
            var state = new object();
            subject.Dispose();

            Action action = () => subject.BeginWrite(buffer, offset, count, callback, state);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void CanRead_should_call_wrapped_stream(
            [Values(false, true)]
            bool canRead)
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            stream.CanRead.Returns(canRead);

            var result = subject.CanRead;

            result.Should().Be(canRead);
            var temp = stream.Received(1).CanRead;
        }

        [Test]
        public void CanRead_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            subject.Dispose();

            Action action = () => { var _ = subject.CanRead; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void CanSeek_should_call_wrapped_stream(
            [Values(false, true)]
            bool canSeek)
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            stream.CanSeek.Returns(canSeek);

            var result = subject.CanSeek;

            result.Should().Be(canSeek);
            var temp = stream.Received(1).CanSeek;
        }

        [Test]
        public void CanSeek_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            subject.Dispose();

            Action action = () => { var _ = subject.CanSeek; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void CanTimeout_should_call_wrapped_stream(
            [Values(false, true)]
            bool canTimeout)
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            stream.CanTimeout.Returns(canTimeout);

            var result = subject.CanTimeout;

            result.Should().Be(canTimeout);
            var temp = stream.Received(1).CanTimeout;
        }

        [Test]
        public void CanTimeout_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            subject.Dispose();

            Action action = () => { var _ = subject.CanTimeout; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void CanWrite_should_call_wrapped_stream(
            [Values(false, true)]
            bool canWrite)
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            stream.CanWrite.Returns(canWrite);

            var result = subject.CanWrite;

            result.Should().Be(canWrite);
            var temp = stream.Received(1).CanWrite;
        }

        [Test]
        public void CanWrite_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            subject.Dispose();

            Action action = () => { var _ = subject.CanWrite; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void Close_can_be_called_multiple_times()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);

            subject.Close();
            subject.Close();

            var subjectReflector = new Reflector(subject);
            subjectReflector._disposed.Should().BeTrue();
        }

        [Test]
        public void Close_should_dispose_subject()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);

            subject.Close();

            var subjectReflector = new Reflector(subject);
            subjectReflector._disposed.Should().BeTrue();
        }

        [Test]
        public void constructor_should_use_false_as_the_default_value_for_ownsStream()
        {
            var stream = Substitute.For<Stream>();

            var subject = new BsonStreamAdapter(stream);

            var subjectReflector = new Reflector(subject);
            subjectReflector._ownsStream.Should().BeFalse();
        }

        [Test]
        public void constructor_should_initialize_instance(
            [Values(false, true)]
            bool ownsStream)
        {
            var stream = Substitute.For<Stream>();

            var subject = new BsonStreamAdapter(stream, ownsStream: ownsStream);

            var subjectReflector = new Reflector(subject);
            subjectReflector._disposed.Should().BeFalse();
            subjectReflector._ownsStream.Should().Be(ownsStream);
            subjectReflector._stream.Should().Be(stream);
            subjectReflector._temp.Should().NotBeNull();
            subjectReflector._tempUtf8.Should().NotBeNull();
        }

        [Test]
        public void constructor_should_throw_when_stream_is_null()
        {
            Action action = () => new BsonStreamAdapter(null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("stream");
        }

        [Test]
        public void CopyToAsync_should_call_wrapped_stream()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var destination = Substitute.For<Stream>();
            var bufferSize = 1;
            var cancellationToken = new CancellationTokenSource().Token;
            var task = new TaskCompletionSource<object>().Task;
            stream.CopyToAsync(destination, bufferSize, cancellationToken).Returns(task);

            var result = subject.CopyToAsync(destination, bufferSize, cancellationToken);

            result.Should().Be(task);
            stream.Received(1).CopyToAsync(destination, bufferSize, cancellationToken);
        }

        [Test]
        public void CopyToAsync_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var destination = Substitute.For<Stream>();
            var bufferSize = 1;
            var cancellationToken = new CancellationTokenSource().Token;
            subject.Dispose();

            Action action = () => subject.CopyToAsync(destination, bufferSize, cancellationToken);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void Dispose_can_be_called_multiple_times()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);

            subject.Dispose();
            subject.Dispose();

            var subjectReflector = new Reflector(subject);
            subjectReflector._disposed.Should().BeTrue();
        }

        [Test]
        public void Dispose_should_dispose_stream_once_when_Disposed_is_called_more_than_once()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream, ownsStream: true);

            subject.Dispose();
            subject.Dispose();

            stream.Received(1).Dispose();
        }

        [Test]
        public void Dispose_should_dispose_stream_only_when_it_owns_it(
            [Values(false, true)]
            bool ownsStream)
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream, ownsStream: ownsStream);

            subject.Dispose();

            stream.Received(ownsStream ? 1 : 0).Dispose();
        }

        [Test]
        public void Dispose_should_dispose_subject()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);

            subject.Dispose();

            var subjectReflector = new Reflector(subject);
            subjectReflector._disposed.Should().BeTrue();
        }

        [Test]
        public void EndRead_should_call_wrapped_stream()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var asyncResult = Substitute.For<IAsyncResult>();
            var numberOfBytesRead = 1;
            stream.EndRead(asyncResult).Returns(numberOfBytesRead);

            var result = subject.EndRead(asyncResult);

            result.Should().Be(numberOfBytesRead);
            stream.Received(1).EndRead(asyncResult);
        }

        [Test]
        public void EndRead_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var asyncResult = Substitute.For<IAsyncResult>();
            subject.Dispose();

            Action action = () => subject.EndRead(asyncResult);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void EndWrite_should_call_wrapped_stream()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var asyncResult = Substitute.For<IAsyncResult>();

            subject.EndWrite(asyncResult);

            stream.Received(1).EndWrite(asyncResult);
        }

        [Test]
        public void EndWrite_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var asyncResult = Substitute.For<IAsyncResult>();
            subject.Dispose();

            Action action = () => subject.EndWrite(asyncResult);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void Flush_should_call_wrapped_stream()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);

            subject.Flush();

            stream.Received(1).Flush();
        }

        [Test]
        public void Flush_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            subject.Dispose();

            Action action = () => subject.Flush();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void FlushAsync_should_call_wrapped_stream()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var task = new TaskCompletionSource<object>().Task;
            var cancellationToken = new CancellationTokenSource().Token;
            stream.FlushAsync(cancellationToken).Returns(task);

            var result = subject.FlushAsync(cancellationToken);

            result.Should().Be(task);
            stream.Received(1).FlushAsync(cancellationToken);
        }

        [Test]
        public void FlushAsync_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var cancellationToken = new CancellationTokenSource().Token;
            subject.Dispose();

            Action action = () => subject.FlushAsync(cancellationToken);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void Length_get_should_call_wrapped_stream(
            [Values(0L, 1L, 2L)]
            long length)
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            stream.Length.Returns(length);

            var result = subject.Length;

            result.Should().Be(length);
            var temp = stream.Received(1).Length;
        }

        [Test]
        public void Length_get_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            subject.Dispose();

            Action action = () => { var _ = subject.Length; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void Position_get_should_call_wrapped_stream(
            [Values(0, 1, 2)]
            long position)
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            stream.Position.Returns(position);

            var result = subject.Position;

            result.Should().Be(position);
            var temp = stream.Received(1).Position;
        }

        [Test]
        public void Position_get_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            subject.Dispose();

            Action action = () => { var _ = subject.Position; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void Position_set_should_call_wrapped_stream(
            [Values(0, 1, 2)]
            long position)
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);

            subject.Position = position;

            stream.Received(1).Position = position;
        }

        [Test]
        public void Position_set_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            subject.Dispose();

            Action action = () => { subject.Position = 0; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void Read_should_call_wrapped_stream()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var buffer = new byte[3];
            var offset = 1;
            var count = 2;
            var numberOfBytesRead = 1;
            stream.Read(buffer, offset, count).Returns(numberOfBytesRead);

            var result = subject.Read(buffer, offset, count);

            result.Should().Be(numberOfBytesRead);
            stream.Received(1).Read(buffer, offset, count);
        }

        [Test]
        public void Read_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var buffer = new byte[3];
            var offset = 1;
            var count = 2;
            subject.Dispose();

            Action action = () => subject.Read(buffer, offset, count);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void ReadAsync_should_call_wrapped_stream()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var task = new TaskCompletionSource<int>().Task;
            var buffer = new byte[3];
            var offset = 1;
            var count = 2;
            var cancellationToken = new CancellationTokenSource().Token;
            stream.ReadAsync(buffer, offset, count, cancellationToken).Returns(task);

            var result = subject.ReadAsync(buffer, offset, count, cancellationToken);

            result.Should().Be(task);
            stream.Received(1).ReadAsync(buffer, offset, count, cancellationToken);
        }

        [Test]
        public void ReadAsync_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var buffer = new byte[3];
            var offset = 1;
            var count = 2;
            var cancellationToken = new CancellationTokenSource().Token;
            subject.Dispose();

            Action action = () => subject.ReadAsync(buffer, offset, count, cancellationToken);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void ReadByte_should_call_wrapped_stream()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            stream.ReadByte().Returns(1);

            var result = subject.ReadByte();

            result.Should().Be(1);
            stream.Received(1).ReadByte();
        }

        [Test]
        public void ReadByte_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            subject.Dispose();

            Action action = () => subject.ReadByte();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [TestCase(new byte[] { 0 }, "")]
        [TestCase(new byte[] { 97, 0 }, "a")]
        [TestCase(new byte[] { 97, 98, 0 }, "ab")]
        [TestCase(new byte[] { 97, 98, 99, 0 }, "abc")]
        public void ReadCString_should_return_expected_result(byte[] bytes, string expectedResult)
        {
            var stream = new MemoryStream(bytes);
            var subject = new BsonStreamAdapter(stream);

            var result = subject.ReadCString(Utf8Encodings.Strict);

            result.Should().Be(expectedResult);
            subject.Position.Should().Be(bytes.Length);
        }

        [Test]
        public void ReadCString_should_throw_when_encoding_is_null()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);

            Action action = () => subject.ReadCString(null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("encoding");
        }

        [Test]
        public void ReadCString_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            subject.Dispose();

            Action action = () => subject.ReadCString(Utf8Encodings.Strict);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [TestCase(new byte[] { 0 }, "")]
        [TestCase(new byte[] { 97, 0 }, "a")]
        [TestCase(new byte[] { 97, 98, 0 }, "ab")]
        [TestCase(new byte[] { 97, 98, 99, 0 }, "abc")]
        public void ReadCStringBytes_should_return_expected_result(byte[] bytes, string expectedResult)
        {
            var stream = new MemoryStream(bytes);
            var subject = new BsonStreamAdapter(stream);

            var result = subject.ReadCStringBytes();

            result.Array.Skip(result.Offset).Take(result.Count).Should().Equal(bytes.Take(bytes.Length - 1));
            subject.Position.Should().Be(bytes.Length);
        }

        [Test]
        public void ReadCStringBytes_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            subject.Dispose();

            Action action = () => subject.ReadCStringBytes();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
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

        [Test]
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

        [Test]
        public void ReadDouble_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            subject.Dispose();

            Action action = () => subject.ReadDouble();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void ReadInt32_should_be_little_endian()
        {
            var bytes = new byte[] { 1, 2, 3, 4 };
            var stream = new MemoryStream(bytes);
            var subject = new BsonStreamAdapter(stream);

            var result = subject.ReadInt32();

            result.Should().Be(0x04030201);
        }

        [Test]
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

        [Test]
        public void ReadInt32_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            subject.Dispose();

            Action action = () => subject.ReadInt32();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void ReadInt64_should_be_little_endian()
        {
            var bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            var stream = new MemoryStream(bytes);
            var subject = new BsonStreamAdapter(stream);

            var result = subject.ReadInt64();

            result.Should().Be(0x0807060504030201);
        }

        [Test]
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

        [Test]
        public void ReadInt64_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            subject.Dispose();

            Action action = () => subject.ReadInt64();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void ReadObjectId_should_be_big_endian()
        {
            var bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var stream = new MemoryStream(bytes);
            var subject = new BsonStreamAdapter(stream);
            var expectedResult = new ObjectId(0x01020304, 0x050607, 0x0809, 0x0a0b0c);

            var result = subject.ReadObjectId();

            result.Should().Be(expectedResult);
        }

        [Test]
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

        [Test]
        public void ReadObjectId_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            subject.Dispose();

            Action action = () => subject.ReadObjectId();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
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

        [Test]
        public void ReadSlice_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            subject.Dispose();

            Action action = () => subject.ReadSlice();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }


        [TestCase(new byte[] { 1, 0, 0, 0, 0 }, "")]
        [TestCase(new byte[] { 2, 0, 0, 0, 97, 0 }, "a")]
        [TestCase(new byte[] { 3, 0, 0, 0, 97, 98, 0 }, "ab")]
        [TestCase(new byte[] { 4, 0, 0, 0, 97, 98, 99, 0 }, "abc")]
        public void ReadString_should_return_expected_result(byte[] bytes, string expectedResult)
        {
            var stream = new MemoryStream(bytes);
            var subject = new BsonStreamAdapter(stream);

            var result = subject.ReadString(Utf8Encodings.Strict);

            result.Should().Be(expectedResult);
            subject.Position.Should().Be(bytes.Length);
        }

        [Test]
        public void ReadString_should_throw_when_encoding_is_null()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);

            Action action = () => subject.ReadString(null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("encoding");
        }

        [Test]
        public void ReadString_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            subject.Dispose();

            Action action = () => subject.ReadString(Utf8Encodings.Strict);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void ReadString_should_throw_when_terminating_null_byte_is_missing()
        {
            var bytes = new byte[] { 2, 0, 0, 0, 97, 1 };
            var stream = new MemoryStream(bytes);
            var subject = new BsonStreamAdapter(stream);

            Action action = () => subject.ReadString(Utf8Encodings.Strict);

            action.ShouldThrow<FormatException>();
        }

        [Test]
        public void ReadTimeout_get_should_call_wrapped_stream(
            [Values(0, 1, 2)]
            int readTimeout)
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            stream.ReadTimeout.Returns(readTimeout);

            var result = subject.ReadTimeout;

            result.Should().Be(readTimeout);
            var temp = stream.Received(1).ReadTimeout;
        }

        [Test]
        public void ReadTimeout_get_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            subject.Dispose();

            Action action = () => { var _ = subject.ReadTimeout; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void ReadTimeout_set_should_call_wrapped_stream(
            [Values(0, 1, 2)]
            int readTimeout)
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);

            subject.ReadTimeout = readTimeout;

            stream.Received(1).ReadTimeout = readTimeout;
        }

        [Test]
        public void ReadTimeout_set_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            subject.Dispose();

            Action action = () => { subject.ReadTimeout = 0; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void Seek_should_call_wrapped_stream()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var offset = 1L;
            var origin = SeekOrigin.Current;
            var newPosition = 2L;
            stream.Seek(offset, origin).Returns(newPosition);

            var result = subject.Seek(offset, origin);

            result.Should().Be(newPosition);
            stream.Received(1).Seek(offset, origin);
        }

        [Test]
        public void Seek_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var offset = 1L;
            var origin = SeekOrigin.Current;
            subject.Dispose();

            Action action = () => subject.Seek(offset, origin);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void SetLength_should_call_wrapped_stream()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var length = 1L;

            subject.SetLength(length);

            stream.Received(1).SetLength(length);
        }

        [Test]
        public void SetLength_should_throw_when_subject_is_diposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var length = 1L;
            subject.Dispose();

            Action action = () => subject.SetLength(length);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [TestCase(new byte[] { 0 })]
        [TestCase(new byte[] { 97, 0 })]
        [TestCase(new byte[] { 97, 98, 0 })]
        [TestCase(new byte[] { 97, 98, 99, 0 })]
        public void SkipCString_should_skip_cstring(byte[] bytes)
        {
            var stream = new MemoryStream(bytes);
            var subject = new BsonStreamAdapter(stream);

            subject.SkipCString();

            subject.Position.Should().Be(bytes.Length);
        }

        [Test]
        public void SkipCString_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            subject.Dispose();

            Action action = () => subject.SkipCString();

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void SkipCString_should_throw_when_terminating_null_byte_is_missing()
        {
            var bytes = new byte[] { 97 };
            var stream = new MemoryStream(bytes);
            var subject = new BsonStreamAdapter(stream);

            Action action = () => subject.SkipCString();

            action.ShouldThrow<EndOfStreamException>();
        }

        [Test]
        public void Write_should_call_wrapped_stream()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var buffer = new byte[0];
            var offset = 1;
            var count = 2;

            subject.Write(buffer, offset, count);

            stream.Received(1).Write(buffer, offset, count);
        }

        [Test]
        public void Write_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var buffer = new byte[0];
            var offset = 1;
            var count = 2;
            subject.Dispose();

            Action action = () => subject.Write(buffer, offset, count);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void WriteAsync_should_call_wrapped_stream()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var task = new TaskCompletionSource<object>().Task;
            var buffer = new byte[0];
            var offset = 1;
            var count = 2;
            var cancellationToken = new CancellationTokenSource().Token;
            stream.WriteAsync(buffer, offset, count, cancellationToken).Returns(task);

            var result = subject.WriteAsync(buffer, offset, count, cancellationToken);

            result.Should().Be(task);
            stream.Received(1).WriteAsync(buffer, offset, count, cancellationToken);
        }

        [Test]
        public void WriteAsync_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var task = new TaskCompletionSource<object>().Task;
            var buffer = new byte[0];
            var offset = 1;
            var count = 2;
            var cancellationToken = new CancellationTokenSource().Token;
            subject.Dispose();

            Action action = () => subject.WriteAsync(buffer, offset, count, cancellationToken);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void WriteByte_should_call_wrapped_stream()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var value = (byte)97;

            subject.WriteByte(value);

            stream.Received(1).WriteByte(value);
        }

        [Test]
        public void WriteByte_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var value = (byte)97;
            subject.Dispose();

            Action action = () => subject.WriteByte(value);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void WriteCString_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var value = "abc";
            subject.Dispose();

            Action action = () => subject.WriteCString(value);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void WriteCString_should_throw_when_value_contains_null_bytes()
        {
            var stream = new MemoryStream();
            var subject = new BsonStreamAdapter(stream);
            var value = "a\0b";

            Action action = () => subject.WriteCString(value);

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("value");
        }

        [Test]
        public void WriteCString_should_throw_when_value_is_null()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);

            Action action = () => subject.WriteCString(null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("value");
        }

        [TestCase("", new byte[] { 0 })]
        [TestCase("a", new byte[] { 97, 0 })]
        [TestCase("ab", new byte[] { 97, 98, 0 })]
        [TestCase("abc", new byte[] { 97, 98, 99, 0 })]
        public void WriteCString_should_write_expected_bytes(string value, byte[] expectedBytes)
        {
            var stream = new MemoryStream();
            var subject = new BsonStreamAdapter(stream);

            subject.WriteCString(value);

            stream.ToArray().Should().Equal(expectedBytes);
        }

        [Test]
        public void WriteCString_should_write_expected_bytes_when_size_is_near_tempUtf8_length(
            [Values(-1, 0, 1)]
            int delta)
        {
            var stream = new MemoryStream();
            var subject = new BsonStreamAdapter(stream);
            var subjectReflector = new Reflector(subject);
            var size = subjectReflector._tempUtf8.Length + delta;
            var length = size - 1;
            var value = new string('a', length);
            var expectedBytes = Enumerable.Repeat<byte>(97, length).Concat(new byte[] { 0 }).ToArray();

            subject.WriteCString(value);

            stream.ToArray().Should().Equal(expectedBytes);
        }

        [Test]
        public void WriteCStringBytes_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var value = new byte[0];
            subject.Dispose();

            Action action = () => subject.WriteCStringBytes(value);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void WriteCStringBytes_should_throw_when_value_is_null()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);

            Action action = () => subject.WriteCStringBytes(null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("value");
        }

        [Test]
        public void WriteCStringBytes_should_throw_when_value_contains_nulls()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var value = new byte[] { 1, 0, 3 };

            Action action = () => subject.WriteCStringBytes(value);

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("value");
        }

        [TestCase(new byte[] { })]
        [TestCase(new byte[] { 97 })]
        [TestCase(new byte[] { 97, 98 })]
        [TestCase(new byte[] { 97, 98, 99 })]
        public void WriteCStringBytes_should_write_expected_bytes(byte[] value)
        {
            var stream = new MemoryStream();
            var subject = new BsonStreamAdapter(stream);

            subject.WriteCStringBytes(value);

            stream.ToArray().Should().Equal(value.Concat(new byte[] { 0 }));
        }

        [Test]
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

        [Test]
        public void WriteDouble_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            subject.Dispose();

            Action action = () => subject.WriteDouble(1.0);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void WriteInt32_should_be_little_endian()
        {
            var stream = new MemoryStream();
            var subject = new BsonStreamAdapter(stream);
            var value = 0x01020304;
            var expectedBytes = new byte[] { 4, 3, 2, 1 };

            subject.WriteInt32(value);

            stream.ToArray().Should().Equal(expectedBytes);
        }

        [Test]
        public void WriteInt32_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            subject.Dispose();

            Action action = () => subject.WriteInt32(1);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
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

        [Test]
        public void WriteInt64_should_be_little_endian()
        {
            var stream = new MemoryStream();
            var subject = new BsonStreamAdapter(stream);
            var value = 0x0102030405060708;
            var expectedBytes = new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 };

            subject.WriteInt64(value);

            stream.ToArray().Should().Equal(expectedBytes);
        }

        [Test]
        public void WriteInt64_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            subject.Dispose();

            Action action = () => subject.WriteInt64(1);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
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

        [Test]
        public void WriteObjectId_should_be_big_endian()
        {
            var stream = new MemoryStream();
            var subject = new BsonStreamAdapter(stream);
            var value = new ObjectId(0x01020304, 0x050607, 0x0809, 0x0a0b0c);
            var expectedBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

            subject.WriteObjectId(value);

            stream.ToArray().Should().Equal(expectedBytes);
        }

        [Test]
        public void WriteObjectId_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            subject.Dispose();

            Action action = () => subject.WriteObjectId(ObjectId.Empty);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void WriteObjectId_should_write_expected_bytes()
        {
            var stream = new MemoryStream();
            var subject = new BsonStreamAdapter(stream);
            var value = ObjectId.GenerateNewId();
            var expectedBytes = value.ToByteArray();

            subject.WriteObjectId(value);

            stream.ToArray().Should().Equal(expectedBytes);
        }

        [Test]
        public void WriteString_should_throw_when_encoding_is_null()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var value = "abc";

            Action action = () => subject.WriteString(value, null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("encoding");
        }

        [Test]
        public void WriteString_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            var value = "abc";
            subject.Dispose();

            Action action = () => subject.WriteString(value, Utf8Encodings.Strict);

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void WriteString_should_throw_when_value_is_null()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);

            Action action = () => subject.WriteString(null, Utf8Encodings.Strict);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("value");
        }

        [TestCase("", new byte[] { 1, 0, 0, 0, 0 })]
        [TestCase("a", new byte[] { 2, 0, 0, 0, 97, 0 })]
        [TestCase("ab", new byte[] { 3, 0, 0, 0, 97, 98, 0 })]
        [TestCase("abc", new byte[] { 4, 0, 0, 0, 97, 98, 99, 0 })]
        [TestCase("a\0c", new byte[] { 4, 0, 0, 0, 97, 0, 99, 0 })]
        public void WriteString_should_write_expected_bytes(string value, byte[] expectedBytes)
        {
            var stream = new MemoryStream();
            var subject = new BsonStreamAdapter(stream);

            subject.WriteString(value, Utf8Encodings.Strict);

            stream.ToArray().Should().Equal(expectedBytes);
        }

        [Test]
        public void WriteString_should_write_expected_bytes_when_size_is_near_tempUtf8_length(
            [Values(-1, 0, 1)]
            int delta)
        {
            var stream = new MemoryStream();
            var subject = new BsonStreamAdapter(stream);
            var subjectReflector = new Reflector(subject);
            var size = subjectReflector._tempUtf8.Length + delta;
            var length = size - 1;
            var value = new string('a', length);
            var expectedBytes = BitConverter.GetBytes(length + 1).Concat(Enumerable.Repeat<byte>(97, length)).Concat(new byte[] { 0 }).ToArray();

            subject.WriteString(value, Utf8Encodings.Strict);

            stream.ToArray().Should().Equal(expectedBytes);
        }

        [Test]
        public void WriteTimeout_get_should_call_wrapped_stream(
            [Values(0, 1, 2)]
            int writeTimeout)
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            stream.WriteTimeout.Returns(writeTimeout);

            var result = subject.WriteTimeout;

            result.Should().Be(writeTimeout);
            var temp = stream.Received(1).WriteTimeout;
        }

        [Test]
        public void WriteTimeout_get_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
            subject.Dispose();

            Action action = () => { var _ = subject.WriteTimeout; };

            action.ShouldThrow<ObjectDisposedException>().And.ObjectName.Should().Be("BsonStreamAdapter");
        }

        [Test]
        public void WriteTimeout_set_should_call_wrapped_stream(
            [Values(0, 1, 2)]
            int writeTimeout)
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);

            subject.WriteTimeout = writeTimeout;

            stream.Received(1).WriteTimeout = writeTimeout;
        }

        [Test]
        public void WriteTimeout_set_should_throw_when_subject_is_disposed()
        {
            var stream = Substitute.For<Stream>();
            var subject = new BsonStreamAdapter(stream);
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
