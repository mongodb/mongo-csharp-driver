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
using FluentAssertions;
using MongoDB.Bson.IO;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Bson.Tests.IO
{
    [TestFixture]
    public class BsonStreamExtensionsTests
    {
        [Test]
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

        [Test]
        public void BackpatchSize_should_throw_when_size_is_larger_than_2GB()
        {
            using (var stream = Substitute.For<BsonStream>())
            {
                var position = (long)int.MaxValue + 1;
                stream.Position.Returns(position);
                stream.Length.Returns(position);

                Action action = () => stream.BackpatchSize(0);

                action.ShouldThrow<FormatException>();
            }
        }

        [Test]
        public void BackpatchSize_should_throw_when_startPosition_is_out_of_range(
            [Values(-1, 4)]
            int startPosition)
        {
            using (var stream = Substitute.For<BsonStream>())
            {
                stream.Length.Returns(3);

                Action action = () => stream.BackpatchSize(startPosition);

                action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("startPosition");
            }
        }

        [Test]
        public void BackpatchSize_should_throw_when_stream_is_null()
        {
            BsonStream stream = null;

            Action action = () => stream.BackpatchSize(0);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("stream");
        }

        [TestCase(0, BsonBinarySubType.Binary)]
        [TestCase(1, BsonBinarySubType.Function)]
#pragma warning disable 618
        [TestCase(2, BsonBinarySubType.OldBinary)]
#pragma warning restore
        [TestCase(3, BsonBinarySubType.UuidLegacy)]
        [TestCase(4, BsonBinarySubType.UuidStandard)]
        [TestCase(5, BsonBinarySubType.MD5)]
        [TestCase(0x80, BsonBinarySubType.UserDefined)]
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

        [Test]
        public void ReadBinarySubType_should_throw_when_at_end_of_stream()
        {
            using (var memoryStream = new MemoryStream())
            using (var stream = new BsonStreamAdapter(memoryStream))
            {
                Action action = () => stream.ReadBinarySubType();

                action.ShouldThrow<EndOfStreamException>();
            }
        }

        [Test]
        public void ReadBinarySubType_should_throw_when_stream_is_null()
        {
            BsonStream stream = null;

            Action action = () => stream.ReadBinarySubType();

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("stream");
        }

        [TestCase(0, false)]
        [TestCase(1, true)]
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

        [Test]
        public void ReadBoolean_should_throw_when_at_end_of_stream()
        {
            using (var memoryStream = new MemoryStream())
            using (var stream = new BsonStreamAdapter(memoryStream))
            {
                Action action = () => stream.ReadBoolean();

                action.ShouldThrow<EndOfStreamException>();
            }
        }

        [Test]
        public void ReadBoolean_should_throw_when_stream_is_null()
        {
            BsonStream stream = null;

            Action action = () => stream.ReadBoolean();

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("stream");
        }

        [TestCase(0x00, BsonType.EndOfDocument)]
        [TestCase(0x01, BsonType.Double)]
        [TestCase(0x02, BsonType.String)]
        [TestCase(0x03, BsonType.Document)]
        [TestCase(0x04, BsonType.Array)]
        [TestCase(0x05, BsonType.Binary)]
        [TestCase(0x06, BsonType.Undefined)]
        [TestCase(0x07, BsonType.ObjectId)]
        [TestCase(0x08, BsonType.Boolean)]
        [TestCase(0x09, BsonType.DateTime)]
        [TestCase(0x0a, BsonType.Null)]
        [TestCase(0x0b, BsonType.RegularExpression)]
        [TestCase(0x0d, BsonType.JavaScript)]
        [TestCase(0x0e, BsonType.Symbol)]
        [TestCase(0x0f, BsonType.JavaScriptWithScope)]
        [TestCase(0x10, BsonType.Int32)]
        [TestCase(0x11, BsonType.Timestamp)]
        [TestCase(0x12, BsonType.Int64)]
        [TestCase(0xff, BsonType.MinKey)]
        [TestCase(0x7f, BsonType.MaxKey)]
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

        [Test]
        public void ReadBsonType_should_throw_when_at_end_of_stream()
        {
            using (var memoryStream = new MemoryStream())
            using (var stream = new BsonStreamAdapter(memoryStream))
            {
                Action action = () => stream.ReadBsonType();

                action.ShouldThrow<EndOfStreamException>();
            }
        }

        [Test]
        public void ReadBsonType_should_throw_when_stream_is_null()
        {
            BsonStream stream = null;

            Action action = () => stream.ReadBsonType();

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("stream");
        }

        [TestCase(0x0c)]
        [TestCase(0x13)]
        [TestCase(0xfe)]
        public void ReadBsonType_should_throw_when_value_is_invalid(int n)
        {
            var bytes = new byte[] { (byte)n };

            using (var memoryStream = new MemoryStream(bytes))
            using (var stream = new BsonStreamAdapter(memoryStream))
            {
                Action action = () => stream.ReadBsonType();

                action.ShouldThrow<FormatException>();
            }
        }

        [Test]
        public void ReadBytes_with_buffer_should_handle_partial_reads()
        {
            using (var baseStream = Substitute.For<BsonStream>())
            using (var stream = new BsonStreamAdapter(baseStream))
            {
                var buffer = new byte[3];
                baseStream.Read(Arg.Any<byte[]>(), 0, 3).Returns(x => { var b = (byte[])x[0]; b[0] = 1; return 1; });
                baseStream.Read(Arg.Any<byte[]>(), 1, 2).Returns(x => { var b = (byte[])x[0]; b[1] = 2; b[2] = 3; return 2; });

                stream.ReadBytes(buffer, 0, 3);

                buffer.Should().Equal(new byte[] { 1, 2, 3 });
                baseStream.Received(1).Read(buffer, 0, 3);
                baseStream.Received(1).Read(buffer, 1, 2);
            }
        }

        [Test]
        public void ReadBytes_with_buffer_should_optimize_count_of_one()
        {
            using (var baseStream = Substitute.For<Stream>())
            using (var stream = new BsonStreamAdapter(baseStream))
            {
                baseStream.ReadByte().Returns(1);
                var buffer = new byte[1];

                stream.ReadBytes(buffer, 0, 1);

                buffer.Should().Equal(new byte[] { 1 });
                baseStream.Received(1).ReadByte();
            }
        }

        [Test]
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

        [Test]
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

        [Test]
        public void ReadBytes_with_buffer_should_throw_when_buffer_is_null()
        {
            var stream = Substitute.For<BsonStream>();

            Action action = () => stream.ReadBytes(null, 0, 0);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("buffer");
        }

        [TestCase(0, 0, 1)]
        [TestCase(1, 0, 2)]
        [TestCase(1, 1, 1)]
        [TestCase(2, 0, 3)]
        [TestCase(2, 1, 2)]
        [TestCase(2, 2, 1)]
        public void ReadBytes_with_buffer_should_throw_when_count_extends_beyond_end_of_buffer(
            int length,
            int offset,
            int count)
        {
            using (var stream = Substitute.For<BsonStream>())
            {
                var buffer = new byte[length];

                Action action = () => stream.ReadBytes(buffer, offset, count);

                action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("count");
            }
        }

        [TestCase(0, 0, 1)]
        [TestCase(1, 0, 2)]
        [TestCase(1, 1, 1)]
        [TestCase(2, 0, 3)]
        [TestCase(2, 1, 2)]
        [TestCase(2, 2, 1)]
        public void ReadBytes_with_buffer_should_throw_when_count_is_out_of_range(int length, int offset, int count)
        {
            using (var stream = Substitute.For<BsonStream>())
            {
                var buffer = new byte[length];

                Action action = () => stream.ReadBytes(buffer, offset, count);

                action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("count");
            }
        }

        [TestCase(0, -1)]
        [TestCase(0, 1)]
        [TestCase(1, -1)]
        [TestCase(1, 2)]
        [TestCase(2, -1)]
        [TestCase(2, 3)]
        public void ReadBytes_with_buffer_should_throw_when_offset_is_out_of_range(int length, int count)
        {
            using (var stream = Substitute.For<BsonStream>())
            {
                var buffer = new byte[length];

                Action action = () => stream.ReadBytes(buffer, count, 0);

                action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("offset");
            }
        }

        [Test]
        public void ReadBytes_with_buffer_should_throw_when_stream_is_null()
        {
            BsonStream stream = null;
            var buffer = new byte[0];

            Action action = () => stream.ReadBytes(buffer, 0, 0);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("stream");
        }

        [Test]
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

        [Test]
        public void ReadBytes_with_count_should_throw_when_at_end_of_stream()
        {
            using (var memoryStream = new MemoryStream())
            using (var stream = new BsonStreamAdapter(memoryStream))
            {
                Action action = () => stream.ReadBytes(1);

                action.ShouldThrow<EndOfStreamException>();
            }
        }

        [Test]
        public void ReadBytes_with_count_should_throw_when_count_is_negative()
        {
            using (var stream = Substitute.For<BsonStream>())
            {
                Action action = () => stream.ReadBytes(-1);

                action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("count");
            }
        }

        [Test]
        public void ReadBytes_with_count_should_throw_when_stream_is_null()
        {
            BsonStream stream = null;

            Action action = () => stream.ReadBytes(1);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("stream");
        }

        [Test]
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

        [TestCase(BsonBinarySubType.Binary, 0)]
        [TestCase(BsonBinarySubType.Function, 1)]
#pragma warning disable 618
        [TestCase(BsonBinarySubType.OldBinary, 2)]
#pragma warning restore
        [TestCase(BsonBinarySubType.UuidLegacy, 3)]
        [TestCase(BsonBinarySubType.UuidStandard, 4)]
        [TestCase(BsonBinarySubType.MD5, 5)]
        [TestCase(BsonBinarySubType.UserDefined, 0x80)]
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

         [Test]
        public void WriteBinarySubType_should_throw_when_stream_is_null()
        {
            BsonStream stream = null;

            Action action = () => stream.WriteBinarySubType(0);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("stream");
        }

        [TestCase(false, 0)]
        [TestCase(true, 1)]
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

        [Test]
        public void WriteBoolean_should_throw_when_stream_is_null()
        {
            BsonStream stream = null;

            Action action = () => stream.WriteBoolean(false);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("stream");
        }

        [TestCase(BsonType.EndOfDocument, 0x00)]
        [TestCase(BsonType.Double, 0x01)]
        [TestCase(BsonType.String, 0x02)]
        [TestCase(BsonType.Document, 0x03)]
        [TestCase(BsonType.Array, 0x04)]
        [TestCase(BsonType.Binary, 0x05)]
        [TestCase(BsonType.Undefined, 0x06)]
        [TestCase(BsonType.ObjectId, 0x07)]
        [TestCase(BsonType.Boolean, 0x08)]
        [TestCase(BsonType.DateTime, 0x09)]
        [TestCase(BsonType.Null, 0x0a)]
        [TestCase(BsonType.RegularExpression, 0x0b)]
        [TestCase(BsonType.JavaScript, 0x0d)]
        [TestCase(BsonType.Symbol, 0x0e)]
        [TestCase(BsonType.JavaScriptWithScope, 0x0f)]
        [TestCase(BsonType.Int32, 0x10)]
        [TestCase(BsonType.Timestamp, 0x11)]
        [TestCase(BsonType.Int64, 0x12)]
        [TestCase(BsonType.MinKey, 0xff)]
        [TestCase(BsonType.MaxKey, 0x7f)]
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

        [Test]
        public void WriteBsonType_should_throw_when_stream_is_null()
        {
            BsonStream stream = null;

            Action action = () => stream.WriteBsonType(0);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("stream");
        }

        [TestCase(0, 0, 0)]
        [TestCase(1, 0, 0)]
        [TestCase(1, 0, 1)]
        [TestCase(1, 1, 0)]
        [TestCase(2, 0, 0)]
        [TestCase(2, 0, 1)]
        [TestCase(2, 0, 2)]
        [TestCase(2, 1, 1)]
        [TestCase(2, 2, 0)]
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

        [Test]
        public void WriteBytes_should_optimize_count_of_one()
        {
            using (var baseStream = Substitute.For<Stream>())
            using (var stream = new BsonStreamAdapter(baseStream))
            {
                var buffer = new byte[] { 1 };

                stream.WriteBytes(buffer, 0, 1);

                baseStream.Received(1).WriteByte(1);
            }
        }

        [Test]
        public void WriteBytes_should_throw_when_buffer_is_null()
        {
            using (var stream = Substitute.For<BsonStream>())
            {
                byte[] buffer = null;
                var offset = 0;
                var count = 0;

                Action action = () => stream.WriteBytes(buffer, offset, count);

                action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("buffer");
            }
        }

        [TestCase(0, 0, 1)]
        [TestCase(1, 0, 2)]
        [TestCase(1, 1, 1)]
        public void WriteBytes_should_throw_when_count_is_out_of_range(
            int length,
            int offset,
            int count)
        {
            using (var stream = Substitute.For<BsonStream>())
            {
                var buffer = new byte[length];

                Action action = () => stream.WriteBytes(buffer, offset, count);

                action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("count");
            }
        }

        [TestCase(0, -1)]
        [TestCase(0, 1)]
        [TestCase(1, -1)]
        [TestCase(1, 2)]
        public void WriteBytes_should_throw_when_offset_is_out_of_range(
            int length,
            int offset)
        {
            using (var stream = Substitute.For<BsonStream>())
            {
                var buffer = new byte[length];
                var count = 0;

                Action action = () => stream.WriteBytes(buffer, offset, count);

                action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("offset");
            }
        }

        [Test]
        public void WriteBytes_should_throw_when_stream_is_null()
        {
            BsonStream stream = null;
            var buffer = new byte[0];
            var offset = 0;
            var count = 0;

            Action action = () => stream.WriteBytes(buffer, offset, count);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("stream");
        }

        [Test]
        public void WriteSlice_should_have_expected_effect(
            [Values(0, 1, 2, 16)]
            int length)
        {
            using (var memoryStream = new MemoryStream())
            using (var stream = new BsonStreamAdapter(memoryStream))
            {
                var bytes = Enumerable.Range(0, length).Select(n => (byte)n).ToArray();
                var slice = new ByteArrayBuffer(bytes, isReadOnly: true);

                stream.WriteSlice(slice);

                memoryStream.ToArray().Should().Equal(bytes);
            }
        }

        [Test]
        public void WriteSlice_should_throw_when_slice_is_null()
        {
            var stream = Substitute.For<BsonStream>();
            IByteBuffer slice = null;

            Action action = () => stream.WriteSlice(slice);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("slice");
        }

        [Test]
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
