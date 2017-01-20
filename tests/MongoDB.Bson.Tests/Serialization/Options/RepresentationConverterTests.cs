/* Copyright 2010-2014 MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Options;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class RepresentationConverterTests
    {
        [Fact]
        public void TestConversions()
        {
            var converter = new RepresentationConverter(false, false);

            Assert.Equal((double)1.5, converter.ToDouble((double)1.5));
            Assert.Equal((double)double.MinValue, converter.ToDouble(double.MinValue));
            Assert.Equal((double)double.MaxValue, converter.ToDouble(double.MaxValue));
            Assert.Equal((double)double.NegativeInfinity, converter.ToDouble(double.NegativeInfinity));
            Assert.Equal((double)double.PositiveInfinity, converter.ToDouble(double.PositiveInfinity));
            Assert.Equal((double)double.NaN, converter.ToDouble(double.NaN));
            Assert.Equal((double)1.5, converter.ToDouble((float)1.5F));
            Assert.Equal((double)double.MinValue, converter.ToDouble(float.MinValue));
            Assert.Equal((double)double.MaxValue, converter.ToDouble(float.MaxValue));
            Assert.Equal((double)double.NegativeInfinity, converter.ToDouble(float.NegativeInfinity));
            Assert.Equal((double)double.PositiveInfinity, converter.ToDouble(float.PositiveInfinity));
            Assert.Equal((double)double.NaN, converter.ToDouble(float.NaN));
            Assert.Equal((double)1.0, converter.ToDouble((int)1));
            Assert.Equal((double)int.MaxValue, converter.ToDouble(int.MaxValue));
            Assert.Equal((double)int.MinValue, converter.ToDouble(int.MinValue));
            Assert.Equal((double)1.0, converter.ToDouble((long)1));
            Assert.Equal((double)1.0, converter.ToDouble((short)1));
            Assert.Equal((double)short.MaxValue, converter.ToDouble(short.MaxValue));
            Assert.Equal((double)short.MinValue, converter.ToDouble(short.MinValue));
            Assert.Equal((double)1.0, converter.ToDouble((uint)1));
            Assert.Equal((double)uint.MaxValue, converter.ToDouble(uint.MaxValue));
            Assert.Equal((double)uint.MinValue, converter.ToDouble(uint.MinValue));
            Assert.Equal((double)1.0, converter.ToDouble((ulong)1));
            Assert.Equal((double)ulong.MinValue, converter.ToDouble(ulong.MinValue));
            Assert.Equal((double)1.0, converter.ToDouble((ushort)1));
            Assert.Equal((double)ushort.MaxValue, converter.ToDouble(ushort.MaxValue));
            Assert.Equal((double)ushort.MinValue, converter.ToDouble(ushort.MinValue));

            Assert.Equal((short)1, converter.ToInt16((double)1.0));
            Assert.Equal((short)1, converter.ToInt16((int)1));
            Assert.Equal((short)1, converter.ToInt16((long)1));

            Assert.Equal((int)1, converter.ToInt32((double)1.0));
            Assert.Equal((int)1, converter.ToInt32((float)1.0F));
            Assert.Equal((int)1, converter.ToInt32((int)1));
            Assert.Equal((int)int.MaxValue, converter.ToInt32(int.MaxValue));
            Assert.Equal((int)int.MinValue, converter.ToInt32(int.MinValue));
            Assert.Equal((int)1, converter.ToInt32((long)1));
            Assert.Equal((int)1, converter.ToInt32((short)1));
            Assert.Equal((int)short.MaxValue, converter.ToInt32(short.MaxValue));
            Assert.Equal((int)short.MinValue, converter.ToInt32(short.MinValue));
            Assert.Equal((int)1, converter.ToInt32((uint)1));
            Assert.Equal((int)uint.MinValue, converter.ToInt32(uint.MinValue));
            Assert.Equal((int)1, converter.ToInt32((ulong)1));
            Assert.Equal((int)ulong.MinValue, converter.ToInt32(ulong.MinValue));
            Assert.Equal((int)1, converter.ToInt32((ushort)1));
            Assert.Equal((int)ushort.MaxValue, converter.ToInt32(ushort.MaxValue));
            Assert.Equal((int)ushort.MinValue, converter.ToInt32(ushort.MinValue));

            Assert.Equal((long)1, converter.ToInt64((double)1.0));
            Assert.Equal((long)1, converter.ToInt64((float)1.0F));
            Assert.Equal((long)1, converter.ToInt64((int)1));
            Assert.Equal((long)int.MaxValue, converter.ToInt64(int.MaxValue));
            Assert.Equal((long)int.MinValue, converter.ToInt64(int.MinValue));
            Assert.Equal((long)1, converter.ToInt64((long)1));
            Assert.Equal((long)long.MaxValue, converter.ToInt64(long.MaxValue));
            Assert.Equal((long)long.MinValue, converter.ToInt64(long.MinValue));
            Assert.Equal((long)1, converter.ToInt64((short)1));
            Assert.Equal((long)short.MaxValue, converter.ToInt64(short.MaxValue));
            Assert.Equal((long)short.MinValue, converter.ToInt64(short.MinValue));
            Assert.Equal((long)1, converter.ToInt64((uint)1));
            Assert.Equal((long)uint.MaxValue, converter.ToInt64(uint.MaxValue));
            Assert.Equal((long)uint.MinValue, converter.ToInt64(uint.MinValue));
            Assert.Equal((long)1, converter.ToInt64((ulong)1));
            Assert.Equal((long)ulong.MinValue, converter.ToInt64(ulong.MinValue));
            Assert.Equal((long)1, converter.ToInt64((ushort)1));
            Assert.Equal((long)ushort.MaxValue, converter.ToInt64(ushort.MaxValue));
            Assert.Equal((long)ushort.MinValue, converter.ToInt64(ushort.MinValue));

            Assert.Equal((float)1.0F, converter.ToSingle((double)1.0));
            Assert.Equal((float)float.MinValue, converter.ToSingle(double.MinValue));
            Assert.Equal((float)float.MaxValue, converter.ToSingle(double.MaxValue));
            Assert.Equal((float)float.NegativeInfinity, converter.ToSingle(double.NegativeInfinity));
            Assert.Equal((float)float.PositiveInfinity, converter.ToSingle(double.PositiveInfinity));
            Assert.Equal((float)float.NaN, converter.ToSingle(double.NaN));
            Assert.Equal((float)1.0F, converter.ToSingle((int)1));
            Assert.Equal((float)1.0F, converter.ToSingle((long)1));

            Assert.Equal((ushort)1, converter.ToUInt16((double)1.0));
            Assert.Equal((ushort)1, converter.ToUInt16((int)1));
            Assert.Equal((ushort)1, converter.ToUInt16((long)1));

            Assert.Equal((uint)1, converter.ToUInt32((double)1.0));
            Assert.Equal((uint)1, converter.ToUInt32((int)1));
            Assert.Equal((uint)1, converter.ToUInt32((long)1));

            Assert.Equal((ulong)1, converter.ToUInt64((double)1.0));
            Assert.Equal((ulong)1, converter.ToUInt64((int)1));
            Assert.Equal((long)(ulong)int.MaxValue, converter.ToInt64(int.MaxValue));
            Assert.Equal(1UL, converter.ToUInt64((long)1));
            Assert.Equal((long)(ulong)long.MaxValue, converter.ToInt64(long.MaxValue));
        }

        [Fact]
        public void TestAllowOverflowFalse()
        {
            var converter = new RepresentationConverter(false, false);

            Assert.Throws<OverflowException>(() => converter.ToInt16(double.MaxValue));
            Assert.Throws<OverflowException>(() => converter.ToInt16(double.MinValue));
            Assert.Throws<OverflowException>(() => converter.ToInt16(int.MaxValue));
            Assert.Throws<OverflowException>(() => converter.ToInt16(int.MinValue));
            Assert.Throws<OverflowException>(() => converter.ToInt16(long.MaxValue));
            Assert.Throws<OverflowException>(() => converter.ToInt16(long.MinValue));

            Assert.Throws<OverflowException>(() => converter.ToInt32(double.MaxValue));
            Assert.Throws<OverflowException>(() => converter.ToInt32(double.MinValue));
            Assert.Throws<OverflowException>(() => converter.ToInt32(float.MaxValue));
            Assert.Throws<OverflowException>(() => converter.ToInt32(float.MinValue));
            Assert.Throws<OverflowException>(() => converter.ToInt32(long.MaxValue));
            Assert.Throws<OverflowException>(() => converter.ToInt32(long.MinValue));
            Assert.Throws<OverflowException>(() => converter.ToInt32(uint.MaxValue));
            Assert.Throws<OverflowException>(() => converter.ToInt32(ulong.MaxValue));

            Assert.Throws<OverflowException>(() => converter.ToInt64(double.MaxValue));
            Assert.Throws<OverflowException>(() => converter.ToInt64(double.MinValue));
            Assert.Throws<OverflowException>(() => converter.ToInt64(float.MaxValue));
            Assert.Throws<OverflowException>(() => converter.ToInt64(float.MinValue));
            Assert.Throws<OverflowException>(() => converter.ToInt64(ulong.MaxValue));

            Assert.Throws<OverflowException>(() => converter.ToSingle(double.MaxValue / 10.0));
            Assert.Throws<OverflowException>(() => converter.ToSingle(double.MinValue / 10.0));

            Assert.Throws<OverflowException>(() => converter.ToUInt16(double.MaxValue));
            Assert.Throws<OverflowException>(() => converter.ToUInt16(double.MinValue));
            Assert.Throws<OverflowException>(() => converter.ToUInt16(int.MaxValue));
            Assert.Throws<OverflowException>(() => converter.ToUInt16(int.MinValue));
            Assert.Throws<OverflowException>(() => converter.ToUInt16(long.MaxValue));
            Assert.Throws<OverflowException>(() => converter.ToUInt16(long.MinValue));

            Assert.Throws<OverflowException>(() => converter.ToUInt32(double.MaxValue));
            Assert.Throws<OverflowException>(() => converter.ToUInt32(double.MinValue));
            Assert.Throws<OverflowException>(() => converter.ToUInt32(int.MinValue));
            Assert.Throws<OverflowException>(() => converter.ToUInt32(long.MaxValue));
            Assert.Throws<OverflowException>(() => converter.ToUInt32(long.MinValue));

            Assert.Throws<OverflowException>(() => converter.ToUInt64(double.MaxValue));
            Assert.Throws<OverflowException>(() => converter.ToUInt64(double.MinValue));
            Assert.Throws<OverflowException>(() => converter.ToUInt64(int.MinValue));
            Assert.Throws<OverflowException>(() => converter.ToUInt64(long.MinValue));
        }

        [Fact]
        public void TestAllowOverflowTrue()
        {
            var converter = new RepresentationConverter(true, false);

            // need variables to get some conversions to happen at runtime instead of compile time
            var doubleMaxValue = double.MaxValue;
            var doubleMinValue = double.MinValue;
            var floatMaxValue = float.MaxValue;
            var floatMinValue = float.MinValue;
            var intMaxValue = int.MaxValue;
            var intMinValue = int.MinValue;
            var longMaxValue = long.MaxValue;
            var longMinValue = long.MinValue;
            var uintMaxValue = uint.MaxValue;
            var ulongMaxValue = ulong.MaxValue;

            Assert.Equal(unchecked((short)doubleMaxValue), converter.ToInt16(double.MaxValue));
            Assert.Equal(unchecked((short)doubleMinValue), converter.ToInt16(double.MinValue));
            Assert.Equal(unchecked((short)intMaxValue), converter.ToInt16(int.MaxValue));
            Assert.Equal(unchecked((short)intMinValue), converter.ToInt16(int.MinValue));
            Assert.Equal(unchecked((short)longMaxValue), converter.ToInt16(long.MaxValue));
            Assert.Equal(unchecked((short)longMinValue), converter.ToInt16(long.MinValue));

            Assert.Equal(unchecked((int)doubleMaxValue), converter.ToInt32(double.MaxValue));
            Assert.Equal(unchecked((int)doubleMinValue), converter.ToInt32(double.MinValue));
            Assert.Equal(unchecked((int)floatMaxValue), converter.ToInt32(float.MaxValue));
            Assert.Equal(unchecked((int)floatMinValue), converter.ToInt32(float.MinValue));
            Assert.Equal(unchecked((int)longMaxValue), converter.ToInt32(long.MaxValue));
            Assert.Equal(unchecked((int)longMinValue), converter.ToInt32(long.MinValue));
            Assert.Equal(unchecked((int)uintMaxValue), converter.ToInt32(uint.MaxValue));
            Assert.Equal(unchecked((int)ulongMaxValue), converter.ToInt32(ulong.MaxValue));

            Assert.Equal(unchecked((long)doubleMaxValue), converter.ToInt64(double.MaxValue));
            Assert.Equal(unchecked((long)doubleMinValue), converter.ToInt64(double.MinValue));
            Assert.Equal(unchecked((long)floatMaxValue), converter.ToInt64(float.MaxValue));
            Assert.Equal(unchecked((long)floatMinValue), converter.ToInt64(float.MinValue));
            Assert.Equal(unchecked((long)ulongMaxValue), converter.ToInt64(ulong.MaxValue));

            Assert.Equal(unchecked((float)(doubleMaxValue / 10.0)), converter.ToSingle(double.MaxValue / 10.0));
            Assert.Equal(unchecked((float)(doubleMinValue / 10.0)), converter.ToSingle(double.MinValue / 10.0));

            Assert.Equal(unchecked((ushort)doubleMaxValue), converter.ToUInt16(double.MaxValue));
            Assert.Equal(unchecked((ushort)doubleMinValue), converter.ToUInt16(double.MinValue));
            Assert.Equal(unchecked((ushort)intMaxValue), converter.ToUInt16(int.MaxValue));
            Assert.Equal(unchecked((ushort)intMinValue), converter.ToUInt16(int.MinValue));
            Assert.Equal(unchecked((ushort)longMaxValue), converter.ToUInt16(long.MaxValue));
            Assert.Equal(unchecked((ushort)longMinValue), converter.ToUInt16(long.MinValue));

            Assert.Equal(unchecked((uint)doubleMaxValue), converter.ToUInt32(double.MaxValue));
            Assert.Equal(unchecked((uint)doubleMinValue), converter.ToUInt32(double.MinValue));
            Assert.Equal(unchecked((uint)intMinValue), converter.ToUInt32(int.MinValue));
            Assert.Equal(unchecked((uint)longMaxValue), converter.ToUInt32(long.MaxValue));
            Assert.Equal(unchecked((uint)longMinValue), converter.ToUInt32(long.MinValue));

            Assert.Equal(unchecked((ulong)doubleMaxValue), converter.ToUInt64(double.MaxValue));
            Assert.Equal(unchecked((ulong)doubleMinValue), converter.ToUInt64(double.MinValue));
            Assert.Equal(unchecked((ulong)intMinValue), converter.ToUInt64(int.MinValue));
            Assert.Equal(unchecked((ulong)longMinValue), converter.ToUInt64(long.MinValue));
        }

        [Fact]
        public void TestAllowTruncationFalse()
        {
            var converter = new RepresentationConverter(false, false);

            Assert.Throws<TruncationException>(() => converter.ToDouble(long.MaxValue));
            Assert.Throws<TruncationException>(() => converter.ToDouble(ulong.MaxValue));
            Assert.Throws<TruncationException>(() => converter.ToInt16((double)1.5));
            Assert.Throws<TruncationException>(() => converter.ToInt32((double)1.5));
            Assert.Throws<TruncationException>(() => converter.ToInt32((float)1.5F));
            Assert.Throws<TruncationException>(() => converter.ToInt64((double)1.5));
            Assert.Throws<TruncationException>(() => converter.ToInt64((float)1.5F));
            Assert.Throws<TruncationException>(() => converter.ToSingle(double.Epsilon));
            Assert.Throws<TruncationException>(() => converter.ToUInt16((double)1.5));
            Assert.Throws<TruncationException>(() => converter.ToUInt32((double)1.5));
            Assert.Throws<TruncationException>(() => converter.ToUInt64((double)1.5));
        }

        [Fact]
        public void TestAllowTruncationTrue()
        {
            var converter = new RepresentationConverter(false, true);

            Assert.Equal((double)long.MaxValue, converter.ToDouble(long.MaxValue));
            Assert.Equal((double)ulong.MaxValue, converter.ToDouble(ulong.MaxValue));
            Assert.Equal((short)1, converter.ToInt16((double)1.5));
            Assert.Equal((int)1, converter.ToInt32((double)1.5));
            Assert.Equal((int)1, converter.ToInt32((float)1.5F));
            Assert.Equal((long)1, converter.ToInt64((double)1.5));
            Assert.Equal((long)1, converter.ToInt64((float)1.5F));
            Assert.Equal((float)0.0F, converter.ToSingle(double.Epsilon));
            Assert.Equal((ushort)1, converter.ToUInt16((double)1.5));
            Assert.Equal((uint)1, converter.ToUInt32((double)1.5));
            Assert.Equal((ulong)1, converter.ToUInt64((double)1.5));
        }
    }
}
