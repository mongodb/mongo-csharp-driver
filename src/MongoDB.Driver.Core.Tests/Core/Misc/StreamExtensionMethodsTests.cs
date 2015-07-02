/* Copyright 2013-2015 MongoDB Inc.
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
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Misc
{
    [TestFixture]
    public class StreamExtensionMethodsTests
    {
        [TestCase(0, new byte[] { 0, 0 })]
        [TestCase(1, new byte[] { 1, 0 })]
        [TestCase(2, new byte[] { 1, 2 })]
        public async Task ReadBytesAsync_with_byte_array_should_have_expected_effect_for_count(int count, byte[] expectedBytes)
        {
            var bytes = new byte[] { 1, 2 };
            var stream = new MemoryStream(bytes);
            var destination = new byte[2];

            await stream.ReadBytesAsync(destination, 0, count, CancellationToken.None);

            destination.Should().Equal(expectedBytes);
        }

        [TestCase(1, new byte[] { 0, 1, 0 })]
        [TestCase(2, new byte[] { 0, 0, 1 })]
        public async Task ReadBytesAsync_with_byte_array_should_have_expected_effect_for_offset(int offset, byte[] expectedBytes)
        {
            var bytes = new byte[] { 1 };
            var stream = new MemoryStream(bytes);
            var destination = new byte[3];

            await stream.ReadBytesAsync(destination, offset, 1, CancellationToken.None);

            destination.Should().Equal(expectedBytes);
        }

        [TestCase(1, new[] { 3 })]
        [TestCase(2, new[] { 1, 2 })]
        [TestCase(3, new[] { 2, 1 })]
        [TestCase(4, new[] { 1, 1, 1 })]
        public async Task ReadBytesAsync_with_byte_array_should_have_expected_effect_for_partial_reads(int testCase, int[] partition)
        {
            var stream = Substitute.For<Stream>();
            var bytes = new byte[] { 1, 2, 3 };
            var n = 0;
            var p = 0;
            stream.ReadAsync(Arg.Any<byte[]>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(x =>
            {
                var l = partition[n++];
                var b = (byte[])x[0];
                var o = (int)x[1];
                Buffer.BlockCopy(bytes, p, b, o, l);
                p += l;
                return Task.FromResult(l);
            });
            var destination = new byte[3];

            await stream.ReadBytesAsync(destination, 0, 3, CancellationToken.None);

            destination.Should().Equal(bytes);
        }

        [Test]
        public void ReadBytesAsync_with_byte_array_should_throw_when_end_of_stream_is_reached()
        {
            var stream = Substitute.For<Stream>();
            var destination = new byte[1];
            stream.ReadAsync(destination, 0, 1).Returns(Task.FromResult(0));

            Func<Task> action = () => stream.ReadBytesAsync(destination, 0, 1, CancellationToken.None);

            action.ShouldThrow<EndOfStreamException>();
        }

        [Test]
        public void ReadBytesAsync_with_byte_array_should_throw_when_buffer_is_null()
        {
            var stream = Substitute.For<Stream>();
            byte[] destination = null;

            Func<Task> action = () => stream.ReadBytesAsync(destination, 0, 0, CancellationToken.None);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("buffer");
        }

        [TestCase(0, -1)]
        [TestCase(1, 2)]
        [TestCase(2, 1)]
        public void ReadBytesAsync_with_byte_array_should_throw_when_count_is_invalid(int offset, int count)
        {
            var stream = Substitute.For<Stream>();
            var destination = new byte[2];

            Func<Task> action = () => stream.ReadBytesAsync(destination, offset, count, CancellationToken.None);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("count");
        }

        [Test]
        public void ReadBytesAsync_with_byte_array_should_throw_when_offset_is_invalid(
            [Values(-1, 3)]
            int offset)
        {
            var stream = Substitute.For<Stream>();
            var destination = new byte[2];

            Func<Task> action = () => stream.ReadBytesAsync(destination, offset, 0, CancellationToken.None);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("offset");
        }

        [Test]
        public void ReadBytesAsync_with_byte_array_should_throw_when_stream_is_null()
        {
            Stream stream = null;
            var destination = new byte[0];

            Func<Task> action = () => stream.ReadBytesAsync(destination, 0, 0, CancellationToken.None);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("stream");
        }

        [TestCase(0, new byte[] { 0, 0 })]
        [TestCase(1, new byte[] { 1, 0 })]
        [TestCase(2, new byte[] { 1, 2 })]
        public async Task ReadBytesAsync_with_byte_buffer_should_have_expected_effect_for_count(int count, byte[] expectedBytes)
        {
            var bytes = new byte[] { 1, 2 };
            var stream = new MemoryStream(bytes);
            var destination = new ByteArrayBuffer(new byte[2]);

            await stream.ReadBytesAsync(destination, 0, count, CancellationToken.None);

            destination.AccessBackingBytes(0).Array.Should().Equal(expectedBytes);
        }

        [TestCase(1, new byte[] { 0, 1, 0 })]
        [TestCase(2, new byte[] { 0, 0, 1 })]
        public async Task ReadBytesAsync_with_byte_buffer_should_have_expected_effect_for_offset(int offset, byte[] expectedBytes)
        {
            var bytes = new byte[] { 1 };
            var stream = new MemoryStream(bytes);
            var destination = new ByteArrayBuffer(new byte[3]);

            await stream.ReadBytesAsync(destination, offset, 1, CancellationToken.None);

            destination.AccessBackingBytes(0).Array.Should().Equal(expectedBytes);
        }

        [TestCase(1, new[] { 3 })]
        [TestCase(2, new[] { 1, 2 })]
        [TestCase(3, new[] { 2, 1 })]
        [TestCase(4, new[] { 1, 1, 1 })]
        public async Task ReadBytesAsync_with_byte_buffer_should_have_expected_effect_for_partial_reads(int testCase, int[] partition)
        {
            var bytes = new byte[] { 1, 2, 3 };
            var stream = Substitute.For<Stream>();
            var destination = new ByteArrayBuffer(new byte[3], 3);
            var n = 0;
            var p = 0;
            stream.ReadAsync(Arg.Any<byte[]>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(x =>
            {
                var l = partition[n++];
                var b = (byte[])x[0];
                var o = (int)x[1];
                Buffer.BlockCopy(bytes, p, b, o, l);
                p += l;
                return Task.FromResult(l);
            });

            await stream.ReadBytesAsync(destination, 0, 3, CancellationToken.None);

            destination.AccessBackingBytes(0).Array.Should().Equal(bytes);
        }

        [Test]
        public void ReadBytesAsync_with_byte_buffer_should_throw_when_end_of_stream_is_reached()
        {
            var stream = Substitute.For<Stream>();
            var destination = CreateFakeByteBuffer(1);
            stream.ReadAsync(Arg.Any<byte[]>(), 0, 1).Returns(Task.FromResult(0));

            Func<Task> action = () => stream.ReadBytesAsync(destination, 0, 1, CancellationToken.None);

            action.ShouldThrow<EndOfStreamException>();
        }

        [Test]
        public void ReadBytesAsync_with_byte_buffer_should_throw_when_buffer_is_null()
        {
            var stream = Substitute.For<Stream>();
            IByteBuffer destination = null;

            Func<Task> action = () => stream.ReadBytesAsync(destination, 0, 0, CancellationToken.None);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("buffer");
        }

        [TestCase(0, -1)]
        [TestCase(1, 2)]
        [TestCase(2, 1)]
        public void ReadBytesAsync_with_byte_buffer_should_throw_when_count_is_invalid(int offset, int count)
        {
            var stream = Substitute.For<Stream>();
            var destination = CreateFakeByteBuffer(2);

            Func<Task> action = () => stream.ReadBytesAsync(destination, offset, count, CancellationToken.None);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("count");
        }

        [Test]
        public void ReadBytesAsync_with_byte_buffer_should_throw_when_offset_is_invalid(
            [Values(-1, 3)]
            int offset)
        {
            var stream = Substitute.For<Stream>();
            var destination = CreateFakeByteBuffer(2);

            Func<Task> action = () => stream.ReadBytesAsync(destination, offset, 0, CancellationToken.None);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("offset");
        }

        [Test]
        public void ReadBytesAsync_with_byte_buffer_should_throw_when_stream_is_null()
        {
            Stream stream = null;
            var destination = Substitute.For<IByteBuffer>();

            Func<Task> action = () => stream.ReadBytesAsync(destination, 0, 0, CancellationToken.None);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("stream");
        }

        [TestCase(0, new byte[] { })]
        [TestCase(1, new byte[] { 1 })]
        [TestCase(2, new byte[] { 1, 2 })]
        public async Task WriteBytesAsync_should_have_expected_effect_for_count(int count, byte[] expectedBytes)
        {
            var stream = new MemoryStream();
            var source = new ByteArrayBuffer(new byte[] { 1, 2 });

            await stream.WriteBytesAsync(source, 0, count, CancellationToken.None);

            stream.ToArray().Should().Equal(expectedBytes);
        }

        [TestCase(1, new byte[] { 2 })]
        [TestCase(2, new byte[] { 3 })]
        public async Task WriteBytesAsync_should_have_expected_effect_for_offset(int offset, byte[] expectedBytes)
        {
            var stream = new MemoryStream();
            var source = new ByteArrayBuffer(new byte[] { 1, 2, 3 });

            await stream.WriteBytesAsync(source, offset, 1, CancellationToken.None);

            stream.ToArray().Should().Equal(expectedBytes);
        }

        [TestCase(1, new[] { 3 })]
        [TestCase(2, new[] { 1, 2 })]
        [TestCase(3, new[] { 2, 1 })]
        [TestCase(4, new[] { 1, 1, 1 })]
        public async Task WriteBytesAsync_should_have_expected_effect_for_partial_writes(int testCase, int[] partition)
        {
            var stream = new MemoryStream();
            var source = Substitute.For<IByteBuffer>();
            source.Length = 3;
            var bytes = new byte[] { 1, 2, 3 };
            var n = 0;
            source.AccessBackingBytes(Arg.Any<int>()).Returns(x =>
            {
                var l = partition[n++];
                var o = (int)x[0];
                return new ArraySegment<byte>(bytes, o, l);
            });

            await stream.WriteBytesAsync(source, 0, 3, CancellationToken.None);

            stream.ToArray().Should().Equal(bytes);
        }

        [Test]
        public void WriteBytesAsync_should_throw_when_buffer_is_null()
        {
            var stream = Substitute.For<Stream>();

            Func<Task> action = () => stream.WriteBytesAsync(null, 0, 0, CancellationToken.None);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("buffer");
        }

        [TestCase(0, -1)]
        [TestCase(1, 2)]
        [TestCase(2, 1)]
        public void WriteBytesAsync_should_throw_when_count_is_invalid(int offset, int count)
        {
            var stream = Substitute.For<Stream>();
            var source = CreateFakeByteBuffer(2);

            Func<Task> action = () => stream.WriteBytesAsync(source, offset, count, CancellationToken.None);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("count");
        }

        [Test]
        public void WriteBytesAsync_should_throw_when_offset_is_invalid(
            [Values(-1, 3)]
            int offset)
        {
            var stream = Substitute.For<Stream>();
            var destination = CreateFakeByteBuffer(2);

            Func<Task> action = () => stream.WriteBytesAsync(destination, offset, 0, CancellationToken.None);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("offset");
        }

        [Test]
        public void WriteBytesAsync_should_throw_when_stream_is_null()
        {
            Stream stream = null;
            var source = Substitute.For<IByteBuffer>();

            Func<Task> action = () => stream.WriteBytesAsync(source, 0, 0, CancellationToken.None);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("stream");
        }

        // helper methods
        private IByteBuffer CreateFakeByteBuffer(int length)
        {
            var buffer = Substitute.For<IByteBuffer>();
            buffer.Length = length;
            return buffer;
        }
    }
}
