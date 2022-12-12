/* Copyright 2021-present MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Driver.GridFS.Tests
{
    public class DelegatingStreamTests
    {
        [Fact]
        public void BeginRead_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new DelegatingStream(mockStream.Object);
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

        [Fact]
        public void BeginWrite_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new DelegatingStream(mockStream.Object);
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

        [Theory]
        [ParameterAttributeData]
        public void CanRead_should_call_wrapped_stream(
            [Values(false, true)]
            bool canRead)
        {
            var mockStream = new Mock<Stream>();
            var subject = new DelegatingStream(mockStream.Object);
            mockStream.SetupGet(s => s.CanRead).Returns(canRead);

            var result = subject.CanRead;

            result.Should().Be(canRead);
            mockStream.VerifyGet(s => s.CanRead, Times.Once);
        }

        [Theory]
        [ParameterAttributeData]
        public void CanSeek_should_call_wrapped_stream(
            [Values(false, true)]
            bool canSeek)
        {
            var mockStream = new Mock<Stream>();
            var subject = new DelegatingStream(mockStream.Object);
            mockStream.SetupGet(s => s.CanSeek).Returns(canSeek);

            var result = subject.CanSeek;

            result.Should().Be(canSeek);
            mockStream.VerifyGet(s => s.CanSeek, Times.Once);
        }

        [Theory]
        [ParameterAttributeData]
        public void CanTimeout_should_call_wrapped_stream(
            [Values(false, true)]
            bool canTimeout)
        {
            var mockStream = new Mock<Stream>();
            var subject = new DelegatingStream(mockStream.Object);
            mockStream.SetupGet(s => s.CanTimeout).Returns(canTimeout);

            var result = subject.CanTimeout;

            result.Should().Be(canTimeout);
            mockStream.VerifyGet(s => s.CanTimeout, Times.Once);
        }

        [Theory]
        [ParameterAttributeData]
        public void CanWrite_should_call_wrapped_stream(
            [Values(false, true)]
            bool canWrite)
        {
            var mockStream = new Mock<Stream>();
            var subject = new DelegatingStream(mockStream.Object);
            mockStream.SetupGet(s => s.CanWrite).Returns(canWrite);

            var result = subject.CanWrite;

            result.Should().Be(canWrite);
            mockStream.VerifyGet(s => s.CanWrite, Times.Once);
        }

        [Fact]
        public void Close_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new DelegatingStream(mockStream.Object);

            subject.Close();

            mockStream.Verify(s => s.Close(), Times.Once);
        }

        [Fact]
        public void CopyToAsync_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new DelegatingStream(mockStream.Object);
            var mockDestination = new Mock<Stream>();
            var bufferSize = 1;
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            var task1 = Task.FromResult<object>(null);
            mockStream.Setup(s => s.CopyToAsync(mockDestination.Object, bufferSize, cancellationToken)).Returns(task1);

            var result = subject.CopyToAsync(mockDestination.Object, bufferSize, cancellationToken);

            result.Should().Be(task1);
            mockStream.Verify(s => s.CopyToAsync(mockDestination.Object, bufferSize, cancellationToken), Times.Once);
        }

        [Fact]
        public void EndRead_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new DelegatingStream(mockStream.Object);
            var mockAsyncResult = new Mock<IAsyncResult>();
            var numberOfBytesRead = 1;
            mockStream.Setup(s => s.EndRead(mockAsyncResult.Object)).Returns(numberOfBytesRead);

            var result = subject.EndRead(mockAsyncResult.Object);

            result.Should().Be(numberOfBytesRead);
            mockStream.Verify(s => s.EndRead(mockAsyncResult.Object), Times.Once);
        }

        [Fact]
        public void EndWrite_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new DelegatingStream(mockStream.Object);
            var mockAsyncResult = new Mock<IAsyncResult>();

            subject.EndWrite(mockAsyncResult.Object);

            mockStream.Verify(s => s.EndWrite(mockAsyncResult.Object), Times.Once);
        }

        [Fact]
        public void Equals_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new DelegatingStream(mockStream.Object);
            mockStream.Setup(s => s.Equals(subject)).Returns(true);

            var result = subject.Equals(subject);

            result.Should().BeTrue();
            mockStream.Verify(s => s.Equals(subject), Times.Once);
        }

        [Fact]
        public void Flush_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new DelegatingStream(mockStream.Object);

            subject.Flush();

            mockStream.Verify(s => s.Flush(), Times.Once);
        }

        [Fact]
        public void FlushAsync_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new DelegatingStream(mockStream.Object);
            var task = Task.FromResult<object>(null);
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            mockStream.Setup(s => s.FlushAsync(cancellationToken)).Returns(task);

            var result = subject.FlushAsync(cancellationToken);

            result.Should().Be(task);
            mockStream.Verify(s => s.FlushAsync(cancellationToken), Times.Once);
        }

        [Fact]
        public void GetHashCode_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new DelegatingStream(mockStream.Object);
            var hashCode = 123;
            mockStream.Setup(s => s.GetHashCode()).Returns(hashCode);

            var result = subject.GetHashCode();

            result.Should().Be(hashCode);
            mockStream.Verify(s => s.GetHashCode(), Times.Once);
        }

        [Fact]
        public void Read_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new DelegatingStream(mockStream.Object);
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
        public void ReadAsync_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new DelegatingStream(mockStream.Object);
            var buffer = new byte[3];
            var offset = 1;
            var count = 2;
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            var task = Task.FromResult(1);
            mockStream.Setup(s => s.ReadAsync(buffer, offset, count, cancellationToken)).Returns(task);

            var result = subject.ReadAsync(buffer, offset, count, cancellationToken);

            result.Should().Be(task);
            mockStream.Verify(s => s.ReadAsync(buffer, offset, count, cancellationToken), Times.Once);
        }

        [Fact]
        public void ReadByte_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new DelegatingStream(mockStream.Object);
            var b = 1;
            mockStream.Setup(s => s.ReadByte()).Returns(b);

            var result = subject.ReadByte();

            result.Should().Be(b);
            mockStream.Verify(s => s.ReadByte(), Times.Once);
        }

        [Fact]
        public void Seek_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new DelegatingStream(mockStream.Object);
            var offset = 1L;
            var origin = SeekOrigin.Current;
            var newPosition = 2L;
            mockStream.Setup(s => s.Seek(offset, origin)).Returns(newPosition);

            var result = subject.Seek(offset, origin);

            result.Should().Be(newPosition);
            mockStream.Verify(s => s.Seek(offset, origin), Times.Once);
        }

        [Fact]
        public void SetLength_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new DelegatingStream(mockStream.Object);
            var length = 1L;

            subject.SetLength(length);

            mockStream.Verify(s => s.SetLength(length), Times.Once);
        }

        [Fact]
        public void ToString_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new DelegatingStream(mockStream.Object);
            var toString = "123";
            mockStream.Setup(s => s.ToString()).Returns(toString);

            var result = subject.ToString();

            result.Should().Be(toString);
            mockStream.Verify(s => s.ToString(), Times.Once);
        }

        [Fact]
        public void Write_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new DelegatingStream(mockStream.Object);
            var buffer = new byte[3];
            var offset = 1;
            var count = 2;

            subject.Write(buffer, offset, count);

            mockStream.Verify(s => s.Write(buffer, offset, count), Times.Once);
        }

        [Fact]
        public void WriteAsync_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new DelegatingStream(mockStream.Object);
            var buffer = new byte[3];
            var offset = 1;
            var count = 2;
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            var task = Task.FromResult<object>(null);
            mockStream.Setup(s => s.WriteAsync(buffer, offset, count, cancellationToken)).Returns(task);

            var result = subject.WriteAsync(buffer, offset, count, cancellationToken);

            result.Should().Be(task);
            mockStream.Verify(s => s.WriteAsync(buffer, offset, count, cancellationToken), Times.Once);
        }

        [Fact]
        public void WriteByte_should_call_wrapped_stream()
        {
            var mockStream = new Mock<Stream>();
            var subject = new DelegatingStream(mockStream.Object);
            byte b = 1;

            subject.WriteByte(b);

            mockStream.Verify(s => s.WriteByte(b), Times.Once);
        }
    }
}
