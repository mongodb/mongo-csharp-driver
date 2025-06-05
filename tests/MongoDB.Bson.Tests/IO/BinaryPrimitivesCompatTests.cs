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
using Xunit;
using FluentAssertions;
using MongoDB.Bson.IO;

namespace MongoDB.Bson.Tests.IO
{
    public class BinaryPrimitivesCompatTests
    {
        [Fact]
        public void ReadSingleLittleEndian_should_read_correctly()
        {
            var bytes = new byte[] { 0x00, 0x00, 0x80, 0x3F }; // 1.0f in little endian
            var result = BinaryPrimitivesCompat.ReadSingleLittleEndian(bytes);
            result.Should().Be(1.0f);
        }

        [Fact]
        public void ReadSingleLittleEndian_should_throw_on_insufficient_length()
        {
            var shortBuffer = new byte[3];
            var exception = Record.Exception(() =>
                BinaryPrimitivesCompat.ReadSingleLittleEndian(shortBuffer));

            var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            e.ParamName.Should().Be("Length");
        }

        [Fact]
        public void WriteSingleLittleEndian_should_throw_on_insufficient_length()
        {
            var shortBuffer = new byte[3];
            var exception = Record.Exception(() =>
                BinaryPrimitivesCompat.WriteSingleLittleEndian(shortBuffer, 1.23f));

            var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            e.ParamName.Should().Be("Length");
        }

        [Fact]
        public void WriteSingleLittleEndian_should_write_correctly()
        {
            Span<byte> buffer = new byte[4];
            BinaryPrimitivesCompat.WriteSingleLittleEndian(buffer, 1.0f);
            buffer.ToArray().Should().Equal(0x00, 0x00, 0x80, 0x3F); // 1.0f little-endian
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(1.0f)]
        [InlineData(-1.5f)]
        [InlineData(float.MaxValue)]
        [InlineData(float.MinValue)]
        [InlineData(float.NaN)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity)]
        public void WriteAndReadSingleLittleEndian_should_roundtrip_correctly(float value)
        {
            Span<byte> buffer = new byte[4];

            BinaryPrimitivesCompat.WriteSingleLittleEndian(buffer, value);
            float result = BinaryPrimitivesCompat.ReadSingleLittleEndian(buffer);

            if (float.IsNaN(value))
            {
                Assert.True(float.IsNaN(result));
            }
            else
            {
                Assert.Equal(value, result);
            }
        }
    }
}
