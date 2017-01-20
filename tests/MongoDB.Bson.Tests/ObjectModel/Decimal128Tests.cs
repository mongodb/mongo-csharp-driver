/* Copyright 2016 MongoDB Inc.
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using FluentAssertions;
using System.Globalization;
using Xunit;

namespace MongoDB.Bson.Tests
{
    public class Decimal128Tests
    {
        [Fact]
        public void Default_value()
        {
            var subject = default(Decimal128);

            subject.ToString().Should().Be("0");
            AssertSpecialProperties(subject);
        }

        [Theory]
        [InlineData("-1.01", "-1.01")]
        [InlineData("-1", "-1")]
        [InlineData("0", "0")]
        [InlineData("1", "1")]
        [InlineData("1.01", "1.01")]
        [InlineData("79228162514264337593543950335", "79228162514264337593543950335")]
        [InlineData("-79228162514264337593543950335", "-79228162514264337593543950335")]
        public void Decimal(string valueString, string s)
        {
            var value = decimal.Parse(valueString);
            var subject = new Decimal128(value);

            subject.ToString().Should().Be(s);
            AssertSpecialProperties(subject);

            var result = Decimal128.ToDecimal(subject);
            result.Should().Be(value);

            result = (decimal)subject;
            result.Should().Be(value);
        }

        [Theory]
        [InlineData((byte)0, "0")]
        [InlineData((byte)1, "1")]
        [InlineData(byte.MaxValue, "255")]
        public void Byte(byte value, string s)
        {
            var subject = new Decimal128(value);

            subject.ToString().Should().Be(s);
            AssertSpecialProperties(subject);

            var result = Decimal128.ToByte(subject);
            result.Should().Be(value);

            result = (byte)subject;
            result.Should().Be(value);
        }

        [Theory]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        [InlineData(byte.MaxValue + 1)]
        [InlineData(byte.MinValue - 1)]
        public void Byte_overflow(int value)
        {
            var subject = new Decimal128(value);

            Action act = () => Decimal128.ToByte(subject);
            act.ShouldThrow<OverflowException>();
        }

        [Theory]
        [InlineData((short)-1, "-1")]
        [InlineData((short)0, "0")]
        [InlineData((short)1, "1")]
        [InlineData(short.MaxValue, "32767")]
        [InlineData(short.MinValue, "-32768")]
        public void Int16(short value, string s)
        {
            var subject = new Decimal128(value);

            subject.ToString().Should().Be(s);
            AssertSpecialProperties(subject);

            var result = Decimal128.ToInt16(subject);
            result.Should().Be(value);

            result = (short)subject;
            result.Should().Be(value);
        }

        [Theory]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        [InlineData(short.MaxValue + 1)]
        [InlineData(short.MinValue - 1)]
        public void Int16_overflow(int value)
        {
            var subject = new Decimal128(value);

            Action act = () => Decimal128.ToInt16(subject);
            act.ShouldThrow<OverflowException>();
        }

        [Theory]
        [InlineData(-1, "-1")]
        [InlineData(0, "0")]
        [InlineData(1, "1")]
        [InlineData(int.MaxValue, "2147483647")]
        [InlineData(int.MinValue, "-2147483648")]
        public void Int32(int value, string s)
        {
            var subject = new Decimal128(value);

            subject.ToString().Should().Be(s);
            AssertSpecialProperties(subject);

            var result = Decimal128.ToInt32(subject);
            result.Should().Be(value);

            result = (int)subject;
            result.Should().Be(value);
        }

        [Theory]
        [InlineData(long.MaxValue)]
        [InlineData(long.MinValue)]
        [InlineData((long)int.MaxValue + 1)]
        [InlineData((long)int.MinValue - 1)]
        public void Int32_overflow(long value)
        {
            var subject = new Decimal128(value);

            Action act = () => Decimal128.ToInt32(subject);
            act.ShouldThrow<OverflowException>();
        }

        [Theory]
        [InlineData(-1, "-1")]
        [InlineData(0, "0")]
        [InlineData(1, "1")]
        [InlineData(long.MaxValue, "9223372036854775807")]
        [InlineData(long.MinValue, "-9223372036854775808")]
        public void Int64(long value, string s)
        {
            var subject = new Decimal128(value);

            subject.ToString().Should().Be(s);
            AssertSpecialProperties(subject);

            var result = Decimal128.ToInt64(subject);
            result.Should().Be(value);

            result = (long)subject;
            result.Should().Be(value);
        }

        [Theory]
        [InlineData(long.MaxValue + 1ul)]
        [InlineData(ulong.MaxValue)]
        public void Int64_overflow(ulong value)
        {
            var subject = new Decimal128(value);

            Action act = () => Decimal128.ToInt64(subject);
            act.ShouldThrow<OverflowException>();
        }

        [Theory]
        [InlineData((sbyte)0, "0")]
        [InlineData((sbyte)1, "1")]
        [InlineData(sbyte.MaxValue, "127")]
        public void SByte(sbyte value, string s)
        {
            var subject = new Decimal128(value);

            subject.ToString().Should().Be(s);
            AssertSpecialProperties(subject);

            var result = Decimal128.ToSByte(subject);
            result.Should().Be(value);

            result = (sbyte)subject;
            result.Should().Be(value);
        }

        [Theory]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        [InlineData(sbyte.MaxValue + 1)]
        [InlineData(sbyte.MinValue - 1)]
        public void SByte_overflow(int value)
        {
            var subject = new Decimal128(value);

            Action act = () => Decimal128.ToSByte(subject);
            act.ShouldThrow<OverflowException>();
        }

        [Theory]
        [InlineData((ushort)0, "0")]
        [InlineData((ushort)1, "1")]
        [InlineData(ushort.MaxValue, "65535")]
        public void UInt16(ushort value, string s)
        {
            var subject = new Decimal128(value);

            subject.ToString().Should().Be(s);
            AssertSpecialProperties(subject);

            var result = Decimal128.ToUInt16(subject);
            result.Should().Be(value);

            result = (ushort)subject;
            result.Should().Be(value);
        }

        [Theory]
        [InlineData(ushort.MaxValue + 1L)]
        [InlineData(-1L)]
        public void UInt16_overflow(long value)
        {
            var subject = new Decimal128(value);

            Action act = () => Decimal128.ToUInt16(subject);
            act.ShouldThrow<OverflowException>();
        }

        [Theory]
        [InlineData(0u, "0")]
        [InlineData(1u, "1")]
        [InlineData(uint.MaxValue, "4294967295")]
        public void UInt32(uint value, string s)
        {
            var subject = new Decimal128(value);

            subject.ToString().Should().Be(s);
            AssertSpecialProperties(subject);

            var result = Decimal128.ToUInt32(subject);
            result.Should().Be(value);

            result = (uint)subject;
            result.Should().Be(value);
        }

        [Theory]
        [InlineData(-1L)]
        public void UInt32_overflow(long value)
        {
            var subject = new Decimal128(value);

            Action act = () => Decimal128.ToUInt32(subject);
            act.ShouldThrow<OverflowException>();
        }

        [Theory]
        [InlineData(0ul, "0")]
        [InlineData(1ul, "1")]
        [InlineData(ulong.MaxValue, "18446744073709551615")]
        public void UInt64(ulong value, string s)
        {
            var subject = new Decimal128(value);

            subject.ToString().Should().Be(s);
            AssertSpecialProperties(subject);

            var result = Decimal128.ToUInt64(subject);
            result.Should().Be(value);

            result = (ulong)subject;
            result.Should().Be(value);
        }

        [Theory]
        [InlineData(-1L)]
        public void UInt64_overflow(long value)
        {
            var subject = new Decimal128(value);

            Action act = () => Decimal128.ToUInt64(subject);
            act.ShouldThrow<OverflowException>();
        }

        [Fact]
        public void NegativeInfinity()
        {
            var subject = Decimal128.NegativeInfinity;

            subject.ToString().Should().Be("-Infinity");
            AssertSpecialProperties(subject, negInfinity: true);
        }

        [Fact]
        public void PositiveInfinity()
        {
            var subject = Decimal128.PositiveInfinity;

            subject.ToString().Should().Be("Infinity");
            AssertSpecialProperties(subject, posInfinity: true);
        }

        [Fact]
        public void QNaN()
        {
            var subject = Decimal128.QNaN;

            subject.ToString().Should().Be("NaN");
            AssertSpecialProperties(subject, qNaN: true);
        }

        [Fact]
        public void SNaN()
        {
            var subject = Decimal128.SNaN;

            subject.ToString().Should().Be("NaN");
            AssertSpecialProperties(subject, sNaN: true);
        }

        private void AssertSpecialProperties(Decimal128 subject, bool qNaN = false, bool sNaN = false, bool posInfinity = false, bool negInfinity = false)
        {
            Decimal128.IsNaN(subject).Should().Be(qNaN || sNaN);
            Decimal128.IsQNaN(subject).Should().Be(qNaN);
            Decimal128.IsSNaN(subject).Should().Be(sNaN);
            Decimal128.IsInfinity(subject).Should().Be(posInfinity || negInfinity);
            Decimal128.IsNegativeInfinity(subject).Should().Be(negInfinity);
            Decimal128.IsPositiveInfinity(subject).Should().Be(posInfinity);
        }
    }
}