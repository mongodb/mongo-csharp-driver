using System;
using Xunit;
using MongoDB.Bson.IO;

namespace MongoDB.Bson.Tests.IO
{
    public class BinaryPrimitivesCompatTests
    {
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

        [Fact]
        public void ReadSingleLittleEndian_should_throw_on_insufficient_length()
        {
            var shortBuffer = new byte[3];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                BinaryPrimitivesCompat.ReadSingleLittleEndian(shortBuffer));
        }

        [Fact]
        public void WriteSingleLittleEndian_should_throw_on_insufficient_length()
        {
            var shortBuffer = new byte[3];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                BinaryPrimitivesCompat.WriteSingleLittleEndian(shortBuffer, 1.23f));
        }
    }
}

