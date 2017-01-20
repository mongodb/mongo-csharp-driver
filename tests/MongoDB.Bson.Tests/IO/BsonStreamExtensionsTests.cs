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
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Bson.Tests.IO
{
    public class BsonStreamExtensionsTests
    {
        [Theory]
        [ParameterAttributeData]
        public void BackpatchSize_should_backpatch_the_size(
            [Values(0, 1, 5)]
            int startPosition)
        {
            var bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var length = bytes.Length - startPosition;
            var expectedBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            Array.Copy(BitConverter.GetBytes(length), 0, expectedBytes, startPosition, 4);

            using (var memoryStream = new MemoryStream())
            using (var stream = new BsonStreamAdapter(memoryStream))
            {
                stream.WriteBytes(bytes, 0, bytes.Length);
                var position = stream.Position;

                stream.BackpatchSize(startPosition);

                memoryStream.ToArray().Should().Equal(expectedBytes);
                stream.Position.Should().Be(position);
            }
        }

        [Fact]
        public void BackpatchSize_should_throw_when_size_is_larger_than_2GB()
        {
            var mockStream = new Mock<BsonStream>();
            var position = (long)int.MaxValue + 1;
            mockStream.SetupGet(s => s.Position).Returns(position);
            mockStream.SetupGet(s => s.Length).Returns(position);

            using (var stream = mockStream.Object)
            {
                Action action = () => stream.BackpatchSize(0);

                action.ShouldThrow<FormatException>();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void BackpatchSize_should_throw_when_startPosition_is_out_of_range(
            [Values(-1, 4)]
            int startPosition)
        {
            var mockStream = new Mock<BsonStream>();
            mockStream.SetupGet(s => s.Length).Returns(3);

            using (var stream = mockStream.Object)
            {
                Action action = () => stream.BackpatchSize(startPosition);

                action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("startPosition");
            }
        }

        [Fact]
        public void BackpatchSize_should_throw_when_stream_is_null()
        {
            BsonStream stream = null;

            Action action = () => stream.BackpatchSize(0);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("stream");
        }

        [Theory]
        [InlineData(0, BsonBinarySubType.Binary)]
        [InlineData(1, BsonBinarySubType.Function)]
#pragma warning disable 618
        [InlineData(2, BsonBinarySubType.OldBinary)]
#pragma warning restore
        [InlineData(3, BsonBinarySubType.UuidLegacy)]
        [InlineData(4, BsonBinarySubType.UuidStandard)]
        [InlineData(5, BsonBinarySubType.MD5)]
        [InlineData(0x80, BsonBinarySubType.UserDefined)]
        public void ReadBinarySubType_should_return_expected_result(int n, BsonBinarySubType expectedResult)
        {
            var bytes = new byte[] { (byte)n };

            using (var memoryStream = new MemoryStream(bytes))
            using (var stream = new BsonStreamAdapter(memoryStream))
            {
                var result = stream.ReadBinarySubType();

                result.Should().Be(expectedResult);
            }
        }

        [Fact]
        public void ReadBinarySubType_should_throw_when_at_end_of_stream()
        {
            using (var memoryStream = new MemoryStream())
            using (var stream = new BsonStreamAdapter(memoryStream))
            {
                Action action = () => stream.ReadBinarySubType();

                action.ShouldThrow<EndOfStreamException>();
            }
        }

        [Fact]
        public void ReadBinarySubType_should_throw_when_stream_is_null()
        {
            BsonStream stream = null;

            Action action = () => stream.ReadBinarySubType();

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("stream");
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, true)]
        public void ReadBoolean_should_return_expected_result(int n, bool expectedResult)
        {
            var bytes = new byte[] { (byte)n };

            using (var memoryStream = new MemoryStream(bytes))
            using (var stream = new BsonStreamAdapter(memoryStream))
            {
                var result = stream.ReadBoolean();

                result.Should().Be(expectedResult);
            }
        }

        [Fact]
        public void ReadBoolean_should_throw_when_at_end_of_stream()
        {
            using (var memoryStream = new MemoryStream())
            using (var stream = new BsonStreamAdapter(memoryStream))
            {
                Action action = () => stream.ReadBoolean();

                action.ShouldThrow<EndOfStreamException>();
            }
        }

        [Fact]
        public void ReadBoolean_should_throw_when_stream_is_null()
        {
            BsonStream stream = null;

            Action action = () => stream.ReadBoolean();

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("stream");
        }

        [Theory]
        [InlineData(0x00, BsonType.EndOfDocument)]
        [InlineData(0x01, BsonType.Double)]
        [InlineData(0x02, BsonType.String)]
        [InlineData(0x03, BsonType.Document)]
        [InlineData(0x04, BsonType.Array)]
        [InlineData(0x05, BsonType.Binary)]
        [InlineData(0x06, BsonType.Undefined)]
        [InlineData(0x07, BsonType.ObjectId)]
        [InlineData(0x08, BsonType.Boolean)]
        [InlineData(0x09, BsonType.DateTime)]
        [InlineData(0x0a, BsonType.Null)]
        [InlineData(0x0b, BsonType.RegularExpression)]
        [InlineData(0x0d, BsonType.JavaScript)]
        [InlineData(0x0e, BsonType.Symbol)]
        [InlineData(0x0f, BsonType.JavaScriptWithScope)]
        [InlineData(0x10, BsonType.Int32)]
        [InlineData(0x11, BsonType.Timestamp)]
        [InlineData(0x12, BsonType.Int64)]
        [InlineData(0xff, BsonType.MinKey)]
        [InlineData(0x7f, BsonType.MaxKey)]
        public void ReadBsonType_should_return_expected_result(int n, BsonType expectedResult)
        {
            var bytes = new byte[] { (byte)n };

            using (var memoryStream = new MemoryStream(bytes))
            using (var stream = new BsonStreamAdapter(memoryStream))
            {
                var result = stream.ReadBsonType();

                result.Should().Be(expectedResult);
            }
        }

        [Fact]
        public void ReadBsonType_should_throw_when_at_end_of_stream()
        {
            using (var memoryStream = new MemoryStream())
            using (var stream = new BsonStreamAdapter(memoryStream))
            {
                Action action = () => stream.ReadBsonType();

                action.ShouldThrow<EndOfStreamException>();
            }
        }

        [Fact]
        public void ReadBsonType_should_throw_when_stream_is_null()
        {
            BsonStream stream = null;

            Action action = () => stream.ReadBsonType();

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("stream");
        }

        [Theory]
        [InlineData(0x0c)]
        [InlineData(0x14)]
        [InlineData(0xfe)]
        public void ReadBsonType_should_throw_when_value_is_invalid(int n)
        {
            var bytes = new byte[] { (byte)n };

            using (var memoryStream = new MemoryStream(bytes))
            using (var stream = new BsonStreamAdapter(memoryStream))
            {
                Action action = () => stream.ReadBsonType();

                var hexBsonType = string.Format("{0:x2}", n);
                var expectedMessage = $"Detected unknown BSON type \"\\x{hexBsonType}\". Are you using the latest driver version?";
                action.ShouldThrow<FormatException>().WithMessage(expectedMessage);
            }
        }

        [Fact]
        public void ReadBytes_with_buffer_should_handle_partial_reads()
        {
            var mockBaseStream = new Mock<BsonStream>();
            var buffer = new byte[3];
            mockBaseStream.Setup(s => s.Read(It.IsAny<byte[]>(), 0, 3)).Returns((byte[] b, int o, int c) => { b[0] = 1; return 1; });
            mockBaseStream.Setup(s => s.Read(It.IsAny<byte[]>(), 1, 2)).Returns((byte[] b, int o, int c) => { b[1] = 2; b[2] = 3; return 2; });

            using (var baseStream = mockBaseStream.Object)
            using (var stream = new BsonStreamAdapter(baseStream))
            {
                stream.ReadBytes(buffer, 0, 3);

                buffer.Should().Equal(new byte[] { 1, 2, 3 });
                mockBaseStream.Verify(s => s.Read(buffer, 0, 3), Times.Once);
                mockBaseStream.Verify(s => s.Read(buffer, 1, 2), Times.Once);
            }
        }

        [Fact]
        public void ReadBytes_with_buffer_should_optimize_count_of_one()
        {
            var mockBaseStream = new Mock<BsonStream>();
            mockBaseStream.Setup(s => s.ReadByte()).Returns(1);

            using (var baseStream = mockBaseStream.Object)
            using (var stream = new BsonStreamAdapter(baseStream))
            {
                var buffer = new byte[1];

                stream.ReadBytes(buffer, 0, 1);

                buffer.Should().Equal(new byte[] { 1 });
                mockBaseStream.Verify(s => s.ReadByte(), Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadBytes_with_buffer_should_return_expected_result(
            [Values(0, 1, 2, 16)]
            int length)
        {
            var bytes = Enumerable.Range(0, length).Select(n => (byte)n).ToArray();

            using (var memoryStream = new MemoryStream(bytes))
            using (var stream = new BsonStreamAdapter(memoryStream))
            {
                var buffer = new byte[length];

                stream.ReadBytes(buffer, 0, length);

                buffer.Should().Equal(bytes);
                stream.Position.Should().Be(length);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadBytes_with_buffer_should_throw_when_at_end_of_stream(
            [Values(0, 1, 2, 16)]
            int length)
        {
            var bytes = Enumerable.Range(0, length).Select(n => (byte)n).ToArray();

            using (var memoryStream = new MemoryStream(bytes))
            using (var stream = new BsonStreamAdapter(memoryStream))
            {
                var buffer = new byte[length + 1];

                Action action = () => stream.ReadBytes(buffer, 0, length + 1);

                action.ShouldThrow<EndOfStreamException>();
            }
        }

        [Fact]
        public void ReadBytes_with_buffer_should_throw_when_buffer_is_null()
        {
            var mockStream = new Mock<BsonStream>();

            Action action = () => mockStream.Object.ReadBytes(null, 0, 0);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("buffer");
        }

        [Theory]
        [InlineData(0, 0, 1)]
        [InlineData(1, 0, 2)]
        [InlineData(1, 1, 1)]
        [InlineData(2, 0, 3)]
        [InlineData(2, 1, 2)]
        [InlineData(2, 2, 1)]
        public void ReadBytes_with_buffer_should_throw_when_count_extends_beyond_end_of_buffer(
            int length,
            int offset,
            int count)
        {
            var mockStream = new Mock<BsonStream>();

            using (var stream = mockStream.Object)
            {
                var buffer = new byte[length];

                Action action = () => stream.ReadBytes(buffer, offset, count);

                action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("count");
            }
        }

        [Theory]
        [InlineData(0, 0, 1)]
        [InlineData(1, 0, 2)]
        [InlineData(1, 1, 1)]
        [InlineData(2, 0, 3)]
        [InlineData(2, 1, 2)]
        [InlineData(2, 2, 1)]
        public void ReadBytes_with_buffer_should_throw_when_count_is_out_of_range(int length, int offset, int count)
        {
            var mockStream = new Mock<BsonStream>();

            using (var stream = mockStream.Object)
            {
                var buffer = new byte[length];

                Action action = () => stream.ReadBytes(buffer, offset, count);

                action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("count");
            }
        }

        [Theory]
        [InlineData(0, -1)]
        [InlineData(0, 1)]
        [InlineData(1, -1)]
        [InlineData(1, 2)]
        [InlineData(2, -1)]
        [InlineData(2, 3)]
        public void ReadBytes_with_buffer_should_throw_when_offset_is_out_of_range(int length, int count)
        {
            var mockStream = new Mock<BsonStream>();

            using (var stream = mockStream.Object)
            {
                var buffer = new byte[length];

                Action action = () => stream.ReadBytes(buffer, count, 0);

                action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("offset");
            }
        }

        [Fact]
        public void ReadBytes_with_buffer_should_throw_when_stream_is_null()
        {
            BsonStream stream = null;
            var buffer = new byte[0];

            Action action = () => stream.ReadBytes(buffer, 0, 0);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("stream");
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadBytes_with_count_should_return_expected_result(
            [Values(0, 1, 2, 16)]
            int length)
        {
            var bytes = Enumerable.Range(0, length).Select(n => (byte)n).ToArray();

            using (var memoryStream = new MemoryStream(bytes))
            using (var stream = new BsonStreamAdapter(memoryStream))
            {
                var result = stream.ReadBytes(length);

                result.Should().Equal(bytes);
                stream.Position.Should().Be(length);
            }
        }

        [Fact]
        public void ReadBytes_with_count_should_throw_when_at_end_of_stream()
        {
            using (var memoryStream = new MemoryStream())
            using (var stream = new BsonStreamAdapter(memoryStream))
            {
                Action action = () => stream.ReadBytes(1);

                action.ShouldThrow<EndOfStreamException>();
            }
        }

        [Fact]
        public void ReadBytes_with_count_should_throw_when_count_is_negative()
        {
            var mockStream = new Mock<BsonStream>();

            using (var stream = mockStream.Object)
            {
                Action action = () => stream.ReadBytes(-1);

                action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("count");
            }
        }

        [Fact]
        public void ReadBytes_with_count_should_throw_when_stream_is_null()
        {
            BsonStream stream = null;

            Action action = () => stream.ReadBytes(1);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("stream");
        }

        [Fact]
        public void static_constructor_should_initialize_validBsonTypes()
        {
            var validBsonTypes = Reflector.__validBsonTypes;

            validBsonTypes.Should().HaveCount(256);
            for (var n = 0; n < 256; n++)
            {
                var expectedValue = Enum.IsDefined(typeof(BsonType), n);
                validBsonTypes[n].Should().Be(expectedValue);
            }
        }

        [Theory]
        [InlineData(BsonBinarySubType.Binary, 0)]
        [InlineData(BsonBinarySubType.Function, 1)]
#pragma warning disable 618
        [InlineData(BsonBinarySubType.OldBinary, 2)]
#pragma warning restore
        [InlineData(BsonBinarySubType.UuidLegacy, 3)]
        [InlineData(BsonBinarySubType.UuidStandard, 4)]
        [InlineData(BsonBinarySubType.MD5, 5)]
        [InlineData(BsonBinarySubType.UserDefined, 0x80)]
        public void WriteBinarySubType_should_have_expected_effect(
            BsonBinarySubType value,
            byte expectedByte)
        {
            using (var memoryStream = new MemoryStream())
            using (var stream = new BsonStreamAdapter(memoryStream))
            {
                var expectedBytes = new byte[] { expectedByte };

                stream.WriteBinarySubType(value);

                memoryStream.ToArray().Should().Equal(expectedBytes);
            }
        }

         [Fact]
        public void WriteBinarySubType_should_throw_when_stream_is_null()
        {
            BsonStream stream = null;

            Action action = () => stream.WriteBinarySubType(0);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("stream");
        }

        [Theory]
        [InlineData(false, 0)]
        [InlineData(true, 1)]
        public void WriteBoolean_should_have_expected_effect(
            bool value,
            byte expectedByte)
        {
            using (var memoryStream = new MemoryStream())
            using (var stream = new BsonStreamAdapter(memoryStream))
            {
                var expectedBytes = new byte[] { expectedByte };

                stream.WriteBoolean(value);

                memoryStream.ToArray().Should().Equal(expectedBytes);
            }
        }

        [Fact]
        public void WriteBoolean_should_throw_when_stream_is_null()
        {
            BsonStream stream = null;

            Action action = () => stream.WriteBoolean(false);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("stream");
        }

        [Theory]
        [InlineData(BsonType.EndOfDocument, 0x00)]
        [InlineData(BsonType.Double, 0x01)]
        [InlineData(BsonType.String, 0x02)]
        [InlineData(BsonType.Document, 0x03)]
        [InlineData(BsonType.Array, 0x04)]
        [InlineData(BsonType.Binary, 0x05)]
        [InlineData(BsonType.Undefined, 0x06)]
        [InlineData(BsonType.ObjectId, 0x07)]
        [InlineData(BsonType.Boolean, 0x08)]
        [InlineData(BsonType.DateTime, 0x09)]
        [InlineData(BsonType.Null, 0x0a)]
        [InlineData(BsonType.RegularExpression, 0x0b)]
        [InlineData(BsonType.JavaScript, 0x0d)]
        [InlineData(BsonType.Symbol, 0x0e)]
        [InlineData(BsonType.JavaScriptWithScope, 0x0f)]
        [InlineData(BsonType.Int32, 0x10)]
        [InlineData(BsonType.Timestamp, 0x11)]
        [InlineData(BsonType.Int64, 0x12)]
        [InlineData(BsonType.MinKey, 0xff)]
        [InlineData(BsonType.MaxKey, 0x7f)]
        public void WriteBsonType_should_have_expected_effect(
            BsonType value,
            byte expectedByte)
        {
            using (var memoryStream = new MemoryStream())
            using (var stream = new BsonStreamAdapter(memoryStream))
            {
                var expectedBytes = new byte[] { expectedByte };

                stream.WriteBsonType(value);

                memoryStream.ToArray().Should().Equal(expectedBytes);
            }
        }

        [Fact]
        public void WriteBsonType_should_throw_when_stream_is_null()
        {
            BsonStream stream = null;

            Action action = () => stream.WriteBsonType(0);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("stream");
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(1, 0, 0)]
        [InlineData(1, 0, 1)]
        [InlineData(1, 1, 0)]
        [InlineData(2, 0, 0)]
        [InlineData(2, 0, 1)]
        [InlineData(2, 0, 2)]
        [InlineData(2, 1, 1)]
        [InlineData(2, 2, 0)]
        public void WriteBytes_should_have_expected_effect(
            int length,
            int offset,
            int count)
        {
            using (var memoryStream = new MemoryStream())
            using (var stream = new BsonStreamAdapter(memoryStream))
            {
                var buffer = Enumerable.Range(0, length).Select(n => (byte)n).ToArray();

                stream.WriteBytes(buffer, offset, count);

                memoryStream.ToArray().Should().Equal(buffer.Skip(offset).Take(count));
            }
        }

        [Fact]
        public void WriteBytes_should_optimize_count_of_one()
        {
            var mockBaseStream = new Mock<Stream>();

            using (var baseStream = mockBaseStream.Object)
            using (var stream = new BsonStreamAdapter(baseStream))
            {
                var buffer = new byte[] { 1 };

                stream.WriteBytes(buffer, 0, 1);

                mockBaseStream.Verify(s => s.WriteByte(1), Times.Once);
            }
        }

        [Fact]
        public void WriteBytes_should_throw_when_buffer_is_null()
        {
            var mockStream = new Mock<BsonStream>();
            using (var stream = mockStream.Object)
            {
                byte[] buffer = null;
                var offset = 0;
                var count = 0;

                Action action = () => stream.WriteBytes(buffer, offset, count);

                action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("buffer");
            }
        }

        [Theory]
        [InlineData(0, 0, 1)]
        [InlineData(1, 0, 2)]
        [InlineData(1, 1, 1)]
        public void WriteBytes_should_throw_when_count_is_out_of_range(
            int length,
            int offset,
            int count)
        {
            var mockStream = new Mock<BsonStream>();
            using (var stream = mockStream.Object)
            {
                var buffer = new byte[length];

                Action action = () => stream.WriteBytes(buffer, offset, count);

                action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("count");
            }
        }

        [Theory]
        [InlineData(0, -1)]
        [InlineData(0, 1)]
        [InlineData(1, -1)]
        [InlineData(1, 2)]
        public void WriteBytes_should_throw_when_offset_is_out_of_range(
            int length,
            int offset)
        {
            var mockStream = new Mock<BsonStream>();
            using (var stream = mockStream.Object)
            {
                var buffer = new byte[length];
                var count = 0;

                Action action = () => stream.WriteBytes(buffer, offset, count);

                action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("offset");
            }
        }

        [Fact]
        public void WriteBytes_should_throw_when_stream_is_null()
        {
            BsonStream stream = null;
            var buffer = new byte[0];
            var offset = 0;
            var count = 0;

            Action action = () => stream.WriteBytes(buffer, offset, count);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("stream");
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteSlice_should_have_expected_effect(
            [Values(0, 1, 2, 16)]
            int length,
            [Values(1, 2, 3)]
            int numberOfChunks)
        {
            numberOfChunks = length == 0 ? 1 : length < numberOfChunks ? length : numberOfChunks;

            using (var memoryStream = new MemoryStream())
            using (var stream = new BsonStreamAdapter(memoryStream))
            {
                IByteBuffer slice;
                var bytes = Enumerable.Range(0, length).Select(n => (byte)n).ToArray();
                if (numberOfChunks == 1)
                {
                    slice = new ByteArrayBuffer(bytes, isReadOnly: true);
                }
                else
                {
                    var chunkSize = length / numberOfChunks;
                    var chunks = Enumerable.Range(0, numberOfChunks)
                        .Select(i => bytes.Skip(i * chunkSize).Take(i < numberOfChunks - 1 ? chunkSize : int.MaxValue).ToArray())
                        .Select(b => new ByteArrayChunk(b));
                    slice = new MultiChunkBuffer(chunks);
                }

                stream.WriteSlice(slice);

                memoryStream.ToArray().Should().Equal(bytes);
            }
        }

        [Fact]
        public void WriteSlice_should_throw_when_slice_is_null()
        {
            var mockStream = new Mock<BsonStream>();
            IByteBuffer slice = null;

            Action action = () => mockStream.Object.WriteSlice(slice);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("slice");
        }

        [Fact]
        public void WriteSlice_should_throw_when_stream_is_null()
        {
            BsonStream stream = null;
            IByteBuffer slice = null;

            Action action = () => stream.WriteSlice(slice);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("stream");
        }

        // nested types
        private class Reflector
        {
            public static bool[] __validBsonTypes
            {
                get
                {
                    var field = typeof(BsonStreamExtensions).GetField("__validBsonTypes", BindingFlags.Static | BindingFlags.NonPublic);
                    return (bool[])field.GetValue(null);
                }
            }
        }
    }
}
