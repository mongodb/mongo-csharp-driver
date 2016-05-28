/* Copyright 2013-2016 MongoDB Inc.
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Misc
{
    public class StreamExtensionMethodsTests
    {
        [Theory]
        [InlineData(0, new byte[] { 0, 0 })]
        [InlineData(1, new byte[] { 1, 0 })]
        [InlineData(2, new byte[] { 1, 2 })]
        public async Task ReadBytesAsync_with_byte_array_should_have_expected_effect_for_count(int count, byte[] expectedBytes)
        {
            var bytes = new byte[] { 1, 2 };
            var stream = new MemoryStream(bytes);
            var destination = new byte[2];

            await stream.ReadBytesAsync(destination, 0, count, CancellationToken.None);

            destination.Should().Equal(expectedBytes);
        }

        [Theory]
        [InlineData(1, new byte[] { 0, 1, 0 })]
        [InlineData(2, new byte[] { 0, 0, 1 })]
        public async Task ReadBytesAsync_with_byte_array_should_have_expected_effect_for_offset(int offset, byte[] expectedBytes)
        {
            var bytes = new byte[] { 1 };
            var stream = new MemoryStream(bytes);
            var destination = new byte[3];

            await stream.ReadBytesAsync(destination, offset, 1, CancellationToken.None);

            destination.Should().Equal(expectedBytes);
        }

        [Theory]
        [InlineData(1, new[] { 3 })]
        [InlineData(2, new[] { 1, 2 })]
        [InlineData(3, new[] { 2, 1 })]
        [InlineData(4, new[] { 1, 1, 1 })]
        public async Task ReadBytesAsync_with_byte_array_should_have_expected_effect_for_partial_reads(int testCase, int[] partition)
        {
            var mockStream = new Mock<Stream>();
            var bytes = new byte[] { 1, 2, 3 };
            var n = 0;
            var position = 0;
            mockStream.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns((byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
                {
                    var length = partition[n++];
                    Buffer.BlockCopy(bytes, position, buffer, offset, length);
                    position += length;
                    return Task.FromResult(length);
                });
            var destination = new byte[3];

            await mockStream.Object.ReadBytesAsync(destination, 0, 3, CancellationToken.None);

            destination.Should().Equal(bytes);
        }

        [Fact]
        public void ReadBytesAsync_with_byte_array_should_throw_when_end_of_stream_is_reached()
        {
            var mockStream = new Mock<Stream>();
            var destination = new byte[1];
            mockStream.Setup(s => s.ReadAsync(destination, 0, 1, It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));

            Func<Task> action = () => mockStream.Object.ReadBytesAsync(destination, 0, 1, CancellationToken.None);

            action.ShouldThrow<EndOfStreamException>();
        }

        [Fact]
        public void ReadBytesAsync_with_byte_array_should_throw_when_buffer_is_null()
        {
            var stream = new Mock<Stream>().Object;
            byte[] destination = null;

            Func<Task> action = () => stream.ReadBytesAsync(destination, 0, 0, CancellationToken.None);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("buffer");
        }

        [Theory]
        [InlineData(0, -1)]
        [InlineData(1, 2)]
        [InlineData(2, 1)]
        public void ReadBytesAsync_with_byte_array_should_throw_when_count_is_invalid(int offset, int count)
        {
            var stream = new Mock<Stream>().Object;
            var destination = new byte[2];

            Func<Task> action = () => stream.ReadBytesAsync(destination, offset, count, CancellationToken.None);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("count");
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadBytesAsync_with_byte_array_should_throw_when_offset_is_invalid(
            [Values(-1, 3)]
            int offset)
        {
            var stream = new Mock<Stream>().Object;
            var destination = new byte[2];

            Func<Task> action = () => stream.ReadBytesAsync(destination, offset, 0, CancellationToken.None);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("offset");
        }

        [Fact]
        public void ReadBytesAsync_with_byte_array_should_throw_when_stream_is_null()
        {
            Stream stream = null;
            var destination = new byte[0];

            Func<Task> action = () => stream.ReadBytesAsync(destination, 0, 0, CancellationToken.None);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("stream");
        }

        [Theory]
        [InlineData(0, new byte[] { 0, 0 })]
        [InlineData(1, new byte[] { 1, 0 })]
        [InlineData(2, new byte[] { 1, 2 })]
        public async Task ReadBytesAsync_with_byte_buffer_should_have_expected_effect_for_count(int count, byte[] expectedBytes)
        {
            var bytes = new byte[] { 1, 2 };
            var stream = new MemoryStream(bytes);
            var destination = new ByteArrayBuffer(new byte[2]);

            await stream.ReadBytesAsync(destination, 0, count, CancellationToken.None);

            destination.AccessBackingBytes(0).Array.Should().Equal(expectedBytes);
        }

        [Theory]
        [InlineData(1, new byte[] { 0, 1, 0 })]
        [InlineData(2, new byte[] { 0, 0, 1 })]
        public async Task ReadBytesAsync_with_byte_buffer_should_have_expected_effect_for_offset(int offset, byte[] expectedBytes)
        {
            var bytes = new byte[] { 1 };
            var stream = new MemoryStream(bytes);
            var destination = new ByteArrayBuffer(new byte[3]);

            await stream.ReadBytesAsync(destination, offset, 1, CancellationToken.None);

            destination.AccessBackingBytes(0).Array.Should().Equal(expectedBytes);
        }

        [Theory]
        [InlineData(1, new[] { 3 })]
        [InlineData(2, new[] { 1, 2 })]
        [InlineData(3, new[] { 2, 1 })]
        [InlineData(4, new[] { 1, 1, 1 })]
        public async Task ReadBytesAsync_with_byte_buffer_should_have_expected_effect_for_partial_reads(int testCase, int[] partition)
        {
            var bytes = new byte[] { 1, 2, 3 };
            var mockStream = new Mock<Stream>();
            var destination = new ByteArrayBuffer(new byte[3], 3);
            var n = 0;
            var position = 0;
            mockStream.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns((byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
                {
                    var length = partition[n++];
                    Buffer.BlockCopy(bytes, position, buffer, offset, length);
                    position += length;
                    return Task.FromResult(length);
                });

            await mockStream.Object.ReadBytesAsync(destination, 0, 3, CancellationToken.None);

            destination.AccessBackingBytes(0).Array.Should().Equal(bytes);
        }

        [Fact]
        public void ReadBytesAsync_with_byte_buffer_should_throw_when_end_of_stream_is_reached()
        {
            var mockStream = new Mock<Stream>();
            var destination = CreateMockByteBuffer(1).Object;
            mockStream.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), 0, 1, It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));

            Func<Task> action = () => mockStream.Object.ReadBytesAsync(destination, 0, 1, CancellationToken.None);

            action.ShouldThrow<EndOfStreamException>();
        }

        [Fact]
        public void ReadBytesAsync_with_byte_buffer_should_throw_when_buffer_is_null()
        {
            var stream = new Mock<Stream>().Object;
            IByteBuffer destination = null;

            Func<Task> action = () => stream.ReadBytesAsync(destination, 0, 0, CancellationToken.None);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("buffer");
        }

        [Theory]
        [InlineData(0, -1)]
        [InlineData(1, 2)]
        [InlineData(2, 1)]
        public void ReadBytesAsync_with_byte_buffer_should_throw_when_count_is_invalid(int offset, int count)
        {
            var stream = new Mock<Stream>().Object;
            var destination = CreateMockByteBuffer(2).Object;

            Func<Task> action = () => stream.ReadBytesAsync(destination, offset, count, CancellationToken.None);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("count");
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadBytesAsync_with_byte_buffer_should_throw_when_offset_is_invalid(
            [Values(-1, 3)]
            int offset)
        {
            var stream = new Mock<Stream>().Object;
            var destination = CreateMockByteBuffer(2).Object;

            Func<Task> action = () => stream.ReadBytesAsync(destination, offset, 0, CancellationToken.None);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("offset");
        }

        [Fact]
        public void ReadBytesAsync_with_byte_buffer_should_throw_when_stream_is_null()
        {
            Stream stream = null;
            var destination = new Mock<IByteBuffer>().Object;

            Func<Task> action = () => stream.ReadBytesAsync(destination, 0, 0, CancellationToken.None);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("stream");
        }

        [Theory]
        [InlineData(0, new byte[] { })]
        [InlineData(1, new byte[] { 1 })]
        [InlineData(2, new byte[] { 1, 2 })]
        public async Task WriteBytesAsync_should_have_expected_effect_for_count(int count, byte[] expectedBytes)
        {
            var stream = new MemoryStream();
            var source = new ByteArrayBuffer(new byte[] { 1, 2 });

            await stream.WriteBytesAsync(source, 0, count, CancellationToken.None);

            stream.ToArray().Should().Equal(expectedBytes);
        }

        [Theory]
        [InlineData(1, new byte[] { 2 })]
        [InlineData(2, new byte[] { 3 })]
        public async Task WriteBytesAsync_should_have_expected_effect_for_offset(int offset, byte[] expectedBytes)
        {
            var stream = new MemoryStream();
            var source = new ByteArrayBuffer(new byte[] { 1, 2, 3 });

            await stream.WriteBytesAsync(source, offset, 1, CancellationToken.None);

            stream.ToArray().Should().Equal(expectedBytes);
        }

        [Theory]
        [InlineData(1, new[] { 3 })]
        [InlineData(2, new[] { 1, 2 })]
        [InlineData(3, new[] { 2, 1 })]
        [InlineData(4, new[] { 1, 1, 1 })]
        public async Task WriteBytesAsync_should_have_expected_effect_for_partial_writes(int testCase, int[] partition)
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

            await stream.WriteBytesAsync(mockSource.Object, 0, 3, CancellationToken.None);

            stream.ToArray().Should().Equal(bytes);
        }

        [Fact]
        public void WriteBytesAsync_should_throw_when_buffer_is_null()
        {
            var stream = new Mock<Stream>().Object;

            Func<Task> action = () => stream.WriteBytesAsync(null, 0, 0, CancellationToken.None);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("buffer");
        }

        [Theory]
        [InlineData(0, -1)]
        [InlineData(1, 2)]
        [InlineData(2, 1)]
        public void WriteBytesAsync_should_throw_when_count_is_invalid(int offset, int count)
        {
            var stream = new Mock<Stream>().Object;
            var source = CreateMockByteBuffer(2).Object;

            Func<Task> action = () => stream.WriteBytesAsync(source, offset, count, CancellationToken.None);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("count");
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteBytesAsync_should_throw_when_offset_is_invalid(
            [Values(-1, 3)]
            int offset)
        {
            var stream = new Mock<Stream>().Object;
            var destination = CreateMockByteBuffer(2).Object;

            Func<Task> action = () => stream.WriteBytesAsync(destination, offset, 0, CancellationToken.None);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("offset");
        }

        [Fact]
        public void WriteBytesAsync_should_throw_when_stream_is_null()
        {
            Stream stream = null;
            var source = new Mock<IByteBuffer>().Object;

            Func<Task> action = () => stream.WriteBytesAsync(source, 0, 0, CancellationToken.None);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("stream");
        }

        // helper methods
        private Mock<IByteBuffer> CreateMockByteBuffer(int length)
        {
            var mockBuffer = new Mock<IByteBuffer>();
            mockBuffer.SetupGet(b => b.Length).Returns(length);
            return mockBuffer;
        }
    }
}
