/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Bson.IO;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Misc
{
    public class StreamExtensionMethodsTests
    {
        [Theory]
        [InlineData(true, 0, new byte[] { 0, 0 })]
        [InlineData(true, 1, new byte[] { 1, 0 })]
        [InlineData(true, 2, new byte[] { 1, 2 })]
        [InlineData(false, 0, new byte[] { 0, 0 })]
        [InlineData(false, 1, new byte[] { 1, 0 })]
        [InlineData(false, 2, new byte[] { 1, 2 })]
        public async Task ReadBytes_with_byte_array_should_have_expected_effect_for_count(bool async, int count, byte[] expectedBytes)
        {
            var bytes = new byte[] { 1, 2 };
            var stream = new MemoryStream(bytes);
            var destination = new byte[2];

            if (async)
            {
                await stream.ReadBytesAsync(OperationContext.NoTimeout, destination, 0, count, Timeout.InfiniteTimeSpan);
            }
            else
            {
                stream.ReadBytes(OperationContext.NoTimeout, destination, 0, count, Timeout.InfiniteTimeSpan);
            }

            destination.Should().Equal(expectedBytes);
        }

        [Theory]
        [InlineData(true, 1, new byte[] { 0, 1, 0 })]
        [InlineData(true, 2, new byte[] { 0, 0, 1 })]
        [InlineData(false, 1, new byte[] { 0, 1, 0 })]
        [InlineData(false, 2, new byte[] { 0, 0, 1 })]
        public async Task ReadBytes_with_byte_array_should_have_expected_effect_for_offset(bool async, int offset, byte[] expectedBytes)
        {
            var bytes = new byte[] { 1 };
            var stream = new MemoryStream(bytes);
            var destination = new byte[3];

            if (async)
            {
                await stream.ReadBytesAsync(OperationContext.NoTimeout, destination, offset, 1, Timeout.InfiniteTimeSpan);
            }
            else
            {
                stream.ReadBytes(OperationContext.NoTimeout, destination, offset, 1, Timeout.InfiniteTimeSpan);
            }

            destination.Should().Equal(expectedBytes);
        }

        [Theory]
        [InlineData(true, 1, new[] { 3 })]
        [InlineData(true, 2, new[] { 1, 2 })]
        [InlineData(true, 3, new[] { 2, 1 })]
        [InlineData(true, 4, new[] { 1, 1, 1 })]
        [InlineData(false, 1, new[] { 3 })]
        [InlineData(false, 2, new[] { 1, 2 })]
        [InlineData(false, 3, new[] { 2, 1 })]
        [InlineData(false, 4, new[] { 1, 1, 1 })]
        public async Task ReadBytes_with_byte_array_should_have_expected_effect_for_partial_reads(bool async, int testCase, int[] partition)
        {
            var mockStream = new Mock<Stream>();
            var bytes = new byte[] { 1, 2, 3 };
            var n = 0;
            var position = 0;
            int ReadPartial (byte[] buffer, int offset, int count)
            {
                var length = partition[n++];
                Buffer.BlockCopy(bytes, position, buffer, offset, length);
                position += length;
                return length;
            }

            mockStream.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns((byte[] buffer, int offset, int count, CancellationToken cancellationToken) => Task.FromResult(ReadPartial(buffer, offset, count)));
            mockStream.Setup(s => s.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns((byte[] buffer, int offset, int count) => ReadPartial(buffer, offset, count));
            var destination = new byte[3];

            if (async)
            {
                await mockStream.Object.ReadBytesAsync(OperationContext.NoTimeout, destination, 0, 3, Timeout.InfiniteTimeSpan);
            }
            else
            {
                mockStream.Object.ReadBytes(OperationContext.NoTimeout, destination, 0, 3, Timeout.InfiniteTimeSpan);
            }

            destination.Should().Equal(bytes);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task ReadBytes_with_byte_array_should_throw_when_end_of_stream_is_reached([Values(true, false)]bool async)
        {
            var mockStream = new Mock<Stream>();
            var destination = new byte[1];
            mockStream.Setup(s => s.ReadAsync(destination, 0, 1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);
            mockStream.Setup(s => s.BeginRead(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<AsyncCallback>(), It.IsAny<object>()))
                .Returns(Task.FromResult(0));

            var exception = async ?
                await Record.ExceptionAsync(() => mockStream.Object.ReadBytesAsync(OperationContext.NoTimeout, destination, 0, 1, Timeout.InfiniteTimeSpan)) :
                Record.Exception(() => mockStream.Object.ReadBytes(OperationContext.NoTimeout, destination, 0, 1, Timeout.InfiniteTimeSpan));

            exception.Should().BeOfType<EndOfStreamException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task ReadBytes_with_byte_array_should_throw_when_buffer_is_null([Values(true, false)]bool async)
        {
            var stream = new Mock<Stream>().Object;
            byte[] destination = null;

            var exception = async ?
                await Record.ExceptionAsync(() => stream.ReadBytesAsync(OperationContext.NoTimeout, destination, 0, 0, Timeout.InfiniteTimeSpan)) :
                Record.Exception(() => stream.ReadBytes(OperationContext.NoTimeout, destination, 0, 0, Timeout.InfiniteTimeSpan));

            exception.Should().BeOfType<ArgumentNullException>().Subject
                .ParamName.Should().Be("buffer");
        }

        [Theory]
        [InlineData(true, 0, -1)]
        [InlineData(true, 1, 2)]
        [InlineData(true, 2, 1)]
        [InlineData(false, 0, -1)]
        [InlineData(false, 1, 2)]
        [InlineData(false, 2, 1)]
        public async Task ReadBytes_with_byte_array_should_throw_when_count_is_invalid(bool async, int offset, int count)
        {
            var stream = new Mock<Stream>().Object;
            var destination = new byte[2];

            var exception = async ?
                await Record.ExceptionAsync(() => stream.ReadBytesAsync(OperationContext.NoTimeout, destination, offset, count, Timeout.InfiniteTimeSpan)) :
                Record.Exception(() => stream.ReadBytes(OperationContext.NoTimeout, destination, offset, count, Timeout.InfiniteTimeSpan));

            exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject
                .ParamName.Should().Be("count");
        }

        [Theory]
        [ParameterAttributeData]
        public async Task ReadBytes_with_byte_array_should_throw_when_offset_is_invalid(
            [Values(true, false)]bool async,
            [Values(-1, 3)]int offset)
        {
            var stream = new Mock<Stream>().Object;
            var destination = new byte[2];

            var exception = async ?
                await Record.ExceptionAsync(() => stream.ReadBytesAsync(OperationContext.NoTimeout, destination, offset, 0, Timeout.InfiniteTimeSpan)) :
                Record.Exception(() => stream.ReadBytes(OperationContext.NoTimeout, destination, offset, 0, Timeout.InfiniteTimeSpan));

            exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject
                .ParamName.Should().Be("offset");
        }

        [Theory]
        [ParameterAttributeData]
        public async Task ReadBytes_with_byte_array_should_throw_when_stream_is_null([Values(true, false)]bool async)
        {
            Stream stream = null;
            var destination = new byte[0];

            var exception = async ?
                await Record.ExceptionAsync(() => stream.ReadBytesAsync(OperationContext.NoTimeout, destination, 0, 0, Timeout.InfiniteTimeSpan)) :
                Record.Exception(() => stream.ReadBytes(OperationContext.NoTimeout, destination, 0, 0, Timeout.InfiniteTimeSpan));

            exception.Should().BeOfType<ArgumentNullException>().Subject
                .ParamName.Should().Be("stream");
        }

        [Theory]
        [ParameterAttributeData]
        public async Task ReadBytes_with_byte_array_throws_on_timeout([Values(true, false)]bool async)
        {
            var streamMock = new Mock<Stream>();
            SetupStreamRead(streamMock);
            var stream = streamMock.Object;

            var destination = new byte[2];
            var timeout = TimeSpan.FromMilliseconds(10);

            var exception = async ?
                await Record.ExceptionAsync(() => stream.ReadAsync(destination, 0, 2, timeout, CancellationToken.None)) :
                Record.Exception(() => stream.Read(destination, 0, 2, timeout, CancellationToken.None));

            exception.Should().BeOfType<TimeoutException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task ReadBytes_with_byte_array_throws_on_cancellation([Values(true, false)]bool async)
        {
            var streamMock = new Mock<Stream>();
            SetupStreamRead(streamMock);
            var stream = streamMock.Object;

            var destination = new byte[2];
            using var cancellationTokenSource = new CancellationTokenSource(10);

            var exception = async ?
                await Record.ExceptionAsync(() => stream.ReadAsync(destination, 0, 2, Timeout.InfiniteTimeSpan, cancellationTokenSource.Token)) :
                Record.Exception(() => stream.Read(destination, 0, 2, Timeout.InfiniteTimeSpan, cancellationTokenSource.Token));

            if (async)
            {
                exception.Should().BeOfType<TaskCanceledException>();
            }
            else
            {
                exception.Should().BeOfType<OperationCanceledException>();
            }
        }

        [Theory]
        [InlineData(true, 0, new byte[] { 0, 0 })]
        [InlineData(true, 1, new byte[] { 1, 0 })]
        [InlineData(true, 2, new byte[] { 1, 2 })]
        [InlineData(false, 0, new byte[] { 0, 0 })]
        [InlineData(false, 1, new byte[] { 1, 0 })]
        [InlineData(false, 2, new byte[] { 1, 2 })]
        public async Task ReadBytes_with_byte_buffer_should_have_expected_effect_for_count(bool async, int count, byte[] expectedBytes)
        {
            var bytes = new byte[] { 1, 2 };
            var stream = new MemoryStream(bytes);
            var destination = new ByteArrayBuffer(new byte[2]);

            if (async)
            {
                await stream.ReadBytesAsync(OperationContext.NoTimeout, destination, 0, count, Timeout.InfiniteTimeSpan);
            }
            else
            {
                stream.ReadBytes(OperationContext.NoTimeout, destination, 0, count, Timeout.InfiniteTimeSpan);
            }

            destination.AccessBackingBytes(0).Array.Should().Equal(expectedBytes);
        }

        [Theory]
        [InlineData(true, 1, new byte[] { 0, 1, 0 })]
        [InlineData(true, 2, new byte[] { 0, 0, 1 })]
        [InlineData(false, 1, new byte[] { 0, 1, 0 })]
        [InlineData(false, 2, new byte[] { 0, 0, 1 })]
        public async Task ReadBytes_with_byte_buffer_should_have_expected_effect_for_offset(bool async, int offset, byte[] expectedBytes)
        {
            var bytes = new byte[] { 1 };
            var stream = new MemoryStream(bytes);
            var destination = new ByteArrayBuffer(new byte[3]);

            if (async)
            {
                await stream.ReadBytesAsync(OperationContext.NoTimeout, destination, offset, 1, Timeout.InfiniteTimeSpan);
            }
            else
            {
                stream.ReadBytes(OperationContext.NoTimeout, destination, offset, 1, Timeout.InfiniteTimeSpan);
            }

            destination.AccessBackingBytes(0).Array.Should().Equal(expectedBytes);
        }

        [Theory]
        [InlineData(true, 1, new[] { 3 })]
        [InlineData(true, 2, new[] { 1, 2 })]
        [InlineData(true, 3, new[] { 2, 1 })]
        [InlineData(true, 4, new[] { 1, 1, 1 })]
        [InlineData(false, 1, new[] { 3 })]
        [InlineData(false, 2, new[] { 1, 2 })]
        [InlineData(false, 3, new[] { 2, 1 })]
        [InlineData(false, 4, new[] { 1, 1, 1 })]
        public async Task ReadBytes_with_byte_buffer_should_have_expected_effect_for_partial_reads(bool async, int testCase, int[] partition)
        {
            var bytes = new byte[] { 1, 2, 3 };
            var mockStream = new Mock<Stream>();
            var destination = new ByteArrayBuffer(new byte[3], 3);
            var n = 0;
            var position = 0;
            int ReadPartial (byte[] buffer, int offset, int count)
            {
                var length = partition[n++];
                Buffer.BlockCopy(bytes, position, buffer, offset, length);
                position += length;
                return length;
            }

            mockStream.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns((byte[] buffer, int offset, int count, CancellationToken cancellationToken) => Task.FromResult(ReadPartial(buffer, offset, count)));
            mockStream.Setup(s => s.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns((byte[] buffer, int offset, int count) => ReadPartial(buffer, offset, count));

            if (async)
            {
                await mockStream.Object.ReadBytesAsync(OperationContext.NoTimeout, destination, 0, 3, Timeout.InfiniteTimeSpan);
            }
            else
            {
                mockStream.Object.ReadBytes(OperationContext.NoTimeout, destination, 0, 3, Timeout.InfiniteTimeSpan);
            }

            destination.AccessBackingBytes(0).Array.Should().Equal(bytes);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task ReadBytes_with_byte_buffer_should_throw_when_end_of_stream_is_reached([Values(true, false)]bool async)
        {
            var mockStream = new Mock<Stream>();
            var destination = CreateMockByteBuffer(1).Object;
            mockStream.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), 0, 1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);
            mockStream.Setup(s => s.BeginRead(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<AsyncCallback>(), It.IsAny<object>()))
                .Returns(Task.FromResult(0));

            var exception = async ?
                await Record.ExceptionAsync(() => mockStream.Object.ReadBytesAsync(OperationContext.NoTimeout, destination, 0, 1, Timeout.InfiniteTimeSpan)) :
                Record.Exception(() => mockStream.Object.ReadBytes(OperationContext.NoTimeout, destination, 0, 1, Timeout.InfiniteTimeSpan));

            exception.Should().BeOfType<EndOfStreamException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task ReadBytes_with_byte_buffer_should_throw_when_buffer_is_null([Values(true, false)]bool async)
        {
            var stream = new Mock<Stream>().Object;
            IByteBuffer destination = null;

            var exception = async ?
                await Record.ExceptionAsync(() => stream.ReadBytesAsync(OperationContext.NoTimeout, destination, 0, 0, Timeout.InfiniteTimeSpan)) :
                Record.Exception(() => stream.ReadBytes(OperationContext.NoTimeout, destination, 0, 0, Timeout.InfiniteTimeSpan));

            exception.Should().BeOfType<ArgumentNullException>().Subject
                .ParamName.Should().Be("buffer");
        }

        [Theory]
        [InlineData(true, 0, -1)]
        [InlineData(true, 1, 2)]
        [InlineData(true, 2, 1)]
        [InlineData(false, 0, -1)]
        [InlineData(false, 1, 2)]
        [InlineData(false, 2, 1)]
        public async Task ReadBytes_with_byte_buffer_should_throw_when_count_is_invalid(bool async, int offset, int count)
        {
            var stream = new Mock<Stream>().Object;
            var destination = CreateMockByteBuffer(2).Object;

            var exception = async ?
                await Record.ExceptionAsync(() => stream.ReadBytesAsync(OperationContext.NoTimeout, destination, offset, count, Timeout.InfiniteTimeSpan)) :
                Record.Exception(() => stream.ReadBytes(OperationContext.NoTimeout, destination, offset, count, Timeout.InfiniteTimeSpan));

            exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject
                .ParamName.Should().Be("count");
        }

        [Theory]
        [ParameterAttributeData]
        public async Task ReadBytes_with_byte_buffer_should_throw_when_offset_is_invalid(
            [Values(true, false)] bool async,
            [Values(-1, 3)]int offset)
        {
            var stream = new Mock<Stream>().Object;
            var destination = CreateMockByteBuffer(2).Object;

            var exception = async ?
                await Record.ExceptionAsync(() => stream.ReadBytesAsync(OperationContext.NoTimeout, destination, offset, 0, Timeout.InfiniteTimeSpan)) :
                Record.Exception(() => stream.ReadBytes(OperationContext.NoTimeout, destination, offset, 0, Timeout.InfiniteTimeSpan));

            exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject
                .ParamName.Should().Be("offset");
        }

        [Theory]
        [ParameterAttributeData]
        public async Task ReadBytes_with_byte_buffer_should_throw_when_stream_is_null([Values(true, false)]bool async)
        {
            Stream stream = null;
            var destination = new Mock<IByteBuffer>().Object;

            var exception = async ?
                await Record.ExceptionAsync(() => stream.ReadBytesAsync(OperationContext.NoTimeout, destination, 0, 0, Timeout.InfiniteTimeSpan)) :
                Record.Exception(() => stream.ReadBytes(OperationContext.NoTimeout, destination, 0, 0, Timeout.InfiniteTimeSpan));

            exception.Should().BeOfType<ArgumentNullException>().Subject
                .ParamName.Should().Be("stream");
        }

        [Theory]
        [InlineData(true, 0, new byte[] { })]
        [InlineData(true, 1, new byte[] { 1 })]
        [InlineData(true, 2, new byte[] { 1, 2 })]
        [InlineData(false, 0, new byte[] { })]
        [InlineData(false, 1, new byte[] { 1 })]
        [InlineData(false, 2, new byte[] { 1, 2 })]
        public async Task WriteBytes_should_have_expected_effect_for_count(bool async, int count, byte[] expectedBytes)
        {
            var stream = new MemoryStream();
            var source = new ByteArrayBuffer(new byte[] { 1, 2 });

            if (async)
            {
                await stream.WriteBytesAsync(OperationContext.NoTimeout, source, 0, count, Timeout.InfiniteTimeSpan);
            }
            else
            {
                stream.WriteBytes(OperationContext.NoTimeout, source, 0, count, Timeout.InfiniteTimeSpan);
            }

            stream.ToArray().Should().Equal(expectedBytes);
        }

        [Theory]
        [InlineData(true, 1, new byte[] { 2 })]
        [InlineData(true, 2, new byte[] { 3 })]
        [InlineData(false, 1, new byte[] { 2 })]
        [InlineData(false, 2, new byte[] { 3 })]
        public async Task WriteBytes_should_have_expected_effect_for_offset(bool async, int offset, byte[] expectedBytes)
        {
            var stream = new MemoryStream();
            var source = new ByteArrayBuffer(new byte[] { 1, 2, 3 });

            if (async)
            {
                await stream.WriteBytesAsync(OperationContext.NoTimeout, source, offset, 1, Timeout.InfiniteTimeSpan);
            }
            else
            {
                stream.WriteBytes(OperationContext.NoTimeout, source, offset, 1, Timeout.InfiniteTimeSpan);
            }

            stream.ToArray().Should().Equal(expectedBytes);
        }

        [Theory]
        [InlineData(true, 1, new[] { 3 })]
        [InlineData(true, 2, new[] { 1, 2 })]
        [InlineData(true, 3, new[] { 2, 1 })]
        [InlineData(true, 4, new[] { 1, 1, 1 })]
        [InlineData(false, 1, new[] { 3 })]
        [InlineData(false, 2, new[] { 1, 2 })]
        [InlineData(false, 3, new[] { 2, 1 })]
        [InlineData(false, 4, new[] { 1, 1, 1 })]
        public async Task WriteBytes_should_have_expected_effect_for_partial_writes(bool async, int testCase, int[] partition)
        {
            var stream = new MemoryStream();
            var mockSource = new Mock<IByteBuffer>();
            mockSource.SetupGet(s => s.Length).Returns(3);
            var bytes = new byte[] { 1, 2, 3 };
            var n = 0;
            mockSource.Setup(s => s.AccessBackingBytes(It.IsAny<int>()))
                .Returns((int position) =>
                {
                    var length = partition[n++];
                    return new ArraySegment<byte>(bytes, position, length);
                });

            if (async)
            {
                await stream.WriteBytesAsync(OperationContext.NoTimeout, mockSource.Object, 0, 3, Timeout.InfiniteTimeSpan);
            }
            else
            {
                stream.WriteBytes(OperationContext.NoTimeout, mockSource.Object, 0, 3, Timeout.InfiniteTimeSpan);
            }

            stream.ToArray().Should().Equal(bytes);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task WriteBytes_should_throw_when_buffer_is_null([Values(true, false)]bool async)
        {
            var stream = new Mock<Stream>().Object;

            var exception = async ?
                await Record.ExceptionAsync(() => stream.WriteBytesAsync(OperationContext.NoTimeout, null, 0, 0, Timeout.InfiniteTimeSpan)) :
                Record.Exception(() => stream.WriteBytes(OperationContext.NoTimeout, null, 0, 0, Timeout.InfiniteTimeSpan));

            exception.Should().BeOfType<ArgumentNullException>().Subject
                .ParamName.Should().Be("buffer");
        }

        [Theory]
        [InlineData(true, 0, -1)]
        [InlineData(true, 1, 2)]
        [InlineData(true, 2, 1)]
        [InlineData(false, 0, -1)]
        [InlineData(false, 1, 2)]
        [InlineData(false, 2, 1)]
        public async Task WriteBytes_should_throw_when_count_is_invalid(bool async, int offset, int count)
        {
            var stream = new Mock<Stream>().Object;
            var source = CreateMockByteBuffer(2).Object;

            var exception = async ?
                await Record.ExceptionAsync(() => stream.WriteBytesAsync(OperationContext.NoTimeout, source, offset, count, Timeout.InfiniteTimeSpan)) :
                Record.Exception(() => stream.WriteBytes(OperationContext.NoTimeout, source, offset, count, Timeout.InfiniteTimeSpan));

            exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject
                .ParamName.Should().Be("count");
        }

        [Theory]
        [ParameterAttributeData]
        public async Task WriteBytes_should_throw_when_offset_is_invalid(
            [Values(true, false)]bool async,
            [Values(-1, 3)]int offset)
        {
            var stream = new Mock<Stream>().Object;
            var source = CreateMockByteBuffer(2).Object;

            var exception = async ?
                await Record.ExceptionAsync(() => stream.WriteBytesAsync(OperationContext.NoTimeout, source, offset, 0, Timeout.InfiniteTimeSpan)) :
                Record.Exception(() => stream.WriteBytes(OperationContext.NoTimeout, source, offset, 0, Timeout.InfiniteTimeSpan));

            exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject
                .ParamName.Should().Be("offset");
        }

        [Theory]
        [ParameterAttributeData]
        public async Task WriteBytes_should_throw_when_stream_is_null([Values(true, false)]bool async)
        {
            Stream stream = null;
            var source = new Mock<IByteBuffer>().Object;

            var exception = async ?
                await Record.ExceptionAsync(() => stream.WriteBytesAsync(OperationContext.NoTimeout, source, 0, 0, Timeout.InfiniteTimeSpan)) :
                Record.Exception(() => stream.WriteBytes(OperationContext.NoTimeout, source, 0, 0, Timeout.InfiniteTimeSpan));

            exception.Should().BeOfType<ArgumentNullException>().Subject
                .ParamName.Should().Be("stream");
        }

        // helper methods
        private Mock<IByteBuffer> CreateMockByteBuffer(int length)
        {
            var mockBuffer = new Mock<IByteBuffer>();
            mockBuffer.SetupGet(b => b.Length).Returns(length);
            return mockBuffer;
        }

        private void SetupStreamRead(Mock<Stream> streamMock, TaskCompletionSource<int> readTaskCompletionSource = null)
        {
            readTaskCompletionSource ??= new TaskCompletionSource<int>();
            streamMock.Setup(s => s.Close()).Callback(() =>
            {
                readTaskCompletionSource.SetException(new IOException());
            });
            streamMock.Setup(s => s.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Returns(() =>
                readTaskCompletionSource.Task.GetAwaiter().GetResult());
            streamMock.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(() =>
                readTaskCompletionSource.Task);
        }
    }
}
