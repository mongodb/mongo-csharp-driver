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
using NUnit.Framework;

namespace MongoDB.Bson.Tests.Serialization
{
    [TestFixture]
    public class RepresentationConverterTests
    {
        [Test]
        public void TestConversions()
        {
            var converter = new RepresentationConverter(false, false);

            Assert.AreEqual((double)1.5, converter.ToDouble((double)1.5));
            Assert.AreEqual((double)double.MinValue, converter.ToDouble(double.MinValue));
            Assert.AreEqual((double)double.MaxValue, converter.ToDouble(double.MaxValue));
            Assert.AreEqual((double)double.NegativeInfinity, converter.ToDouble(double.NegativeInfinity));
            Assert.AreEqual((double)double.PositiveInfinity, converter.ToDouble(double.PositiveInfinity));
            Assert.AreEqual((double)double.NaN, converter.ToDouble(double.NaN));
            Assert.AreEqual((double)1.5, converter.ToDouble((float)1.5F));
            Assert.AreEqual((double)double.MinValue, converter.ToDouble(float.MinValue));
            Assert.AreEqual((double)double.MaxValue, converter.ToDouble(float.MaxValue));
            Assert.AreEqual((double)double.NegativeInfinity, converter.ToDouble(float.NegativeInfinity));
            Assert.AreEqual((double)double.PositiveInfinity, converter.ToDouble(float.PositiveInfinity));
            Assert.AreEqual((double)double.NaN, converter.ToDouble(float.NaN));
            Assert.AreEqual((double)1.0, converter.ToDouble((int)1));
            Assert.AreEqual((double)int.MaxValue, converter.ToDouble(int.MaxValue));
            Assert.AreEqual((double)int.MinValue, converter.ToDouble(int.MinValue));
            Assert.AreEqual((double)1.0, converter.ToDouble((long)1));
            Assert.AreEqual((double)1.0, converter.ToDouble((short)1));
            Assert.AreEqual((double)short.MaxValue, converter.ToDouble(short.MaxValue));
            Assert.AreEqual((double)short.MinValue, converter.ToDouble(short.MinValue));
            Assert.AreEqual((double)1.0, converter.ToDouble((uint)1));
            Assert.AreEqual((double)uint.MaxValue, converter.ToDouble(uint.MaxValue));
            Assert.AreEqual((double)uint.MinValue, converter.ToDouble(uint.MinValue));
            Assert.AreEqual((double)1.0, converter.ToDouble((ulong)1));
            Assert.AreEqual((double)ulong.MinValue, converter.ToDouble(ulong.MinValue));
            Assert.AreEqual((double)1.0, converter.ToDouble((ushort)1));
            Assert.AreEqual((double)ushort.MaxValue, converter.ToDouble(ushort.MaxValue));
            Assert.AreEqual((double)ushort.MinValue, converter.ToDouble(ushort.MinValue));

            Assert.AreEqual((short)1, converter.ToInt16((double)1.0));
            Assert.AreEqual((short)1, converter.ToInt16((int)1));
            Assert.AreEqual((short)1, converter.ToInt16((long)1));

            Assert.AreEqual((int)1, converter.ToInt32((double)1.0));
            Assert.AreEqual((int)1, converter.ToInt32((float)1.0F));
            Assert.AreEqual((int)1, converter.ToInt32((int)1));
            Assert.AreEqual((int)int.MaxValue, converter.ToInt32(int.MaxValue));
            Assert.AreEqual((int)int.MinValue, converter.ToInt32(int.MinValue));
            Assert.AreEqual((int)1, converter.ToInt32((long)1));
            Assert.AreEqual((int)1, converter.ToInt32((short)1));
            Assert.AreEqual((int)short.MaxValue, converter.ToInt32(short.MaxValue));
            Assert.AreEqual((int)short.MinValue, converter.ToInt32(short.MinValue));
            Assert.AreEqual((int)1, converter.ToInt32((uint)1));
            Assert.AreEqual((int)uint.MinValue, converter.ToInt32(uint.MinValue));
            Assert.AreEqual((int)1, converter.ToInt32((ulong)1));
            Assert.AreEqual((int)ulong.MinValue, converter.ToInt32(ulong.MinValue));
            Assert.AreEqual((int)1, converter.ToInt32((ushort)1));
            Assert.AreEqual((int)ushort.MaxValue, converter.ToInt32(ushort.MaxValue));
            Assert.AreEqual((int)ushort.MinValue, converter.ToInt32(ushort.MinValue));

            Assert.AreEqual((long)1, converter.ToInt64((double)1.0));
            Assert.AreEqual((long)1, converter.ToInt64((float)1.0F));
            Assert.AreEqual((long)1, converter.ToInt64((int)1));
            Assert.AreEqual((long)int.MaxValue, converter.ToInt64(int.MaxValue));
            Assert.AreEqual((long)int.MinValue, converter.ToInt64(int.MinValue));
            Assert.AreEqual((long)1, converter.ToInt64((long)1));
            Assert.AreEqual((long)long.MaxValue, converter.ToInt64(long.MaxValue));
            Assert.AreEqual((long)long.MinValue, converter.ToInt64(long.MinValue));
            Assert.AreEqual((long)1, converter.ToInt64((short)1));
            Assert.AreEqual((long)short.MaxValue, converter.ToInt64(short.MaxValue));
            Assert.AreEqual((long)short.MinValue, converter.ToInt64(short.MinValue));
            Assert.AreEqual((long)1, converter.ToInt64((uint)1));
            Assert.AreEqual((long)uint.MaxValue, converter.ToInt64(uint.MaxValue));
            Assert.AreEqual((long)uint.MinValue, converter.ToInt64(uint.MinValue));
            Assert.AreEqual((long)1, converter.ToInt64((ulong)1));
            Assert.AreEqual((long)ulong.MinValue, converter.ToInt64(ulong.MinValue));
            Assert.AreEqual((long)1, converter.ToInt64((ushort)1));
            Assert.AreEqual((long)ushort.MaxValue, converter.ToInt64(ushort.MaxValue));
            Assert.AreEqual((long)ushort.MinValue, converter.ToInt64(ushort.MinValue));

            Assert.AreEqual((float)1.0F, converter.ToSingle((double)1.0));
            Assert.AreEqual((float)float.MinValue, converter.ToSingle(double.MinValue));
            Assert.AreEqual((float)float.MaxValue, converter.ToSingle(double.MaxValue));
            Assert.AreEqual((float)float.NegativeInfinity, converter.ToSingle(double.NegativeInfinity));
            Assert.AreEqual((float)float.PositiveInfinity, converter.ToSingle(double.PositiveInfinity));
            Assert.AreEqual((float)float.NaN, converter.ToSingle(double.NaN));
            Assert.AreEqual((float)1.0F, converter.ToSingle((int)1));
            Assert.AreEqual((float)1.0F, converter.ToSingle((long)1));

            Assert.AreEqual((ushort)1, converter.ToUInt16((double)1.0));
            Assert.AreEqual((ushort)1, converter.ToUInt16((int)1));
            Assert.AreEqual((ushort)1, converter.ToUInt16((long)1));

            Assert.AreEqual((uint)1, converter.ToUInt32((double)1.0));
            Assert.AreEqual((uint)1, converter.ToUInt32((int)1));
            Assert.AreEqual((uint)1, converter.ToUInt32((long)1));

            Assert.AreEqual((ulong)1, converter.ToUInt64((double)1.0));
            Assert.AreEqual((ulong)1, converter.ToUInt64((int)1));
            Assert.AreEqual((ulong)int.MaxValue, converter.ToInt64(int.MaxValue));
            Assert.AreEqual((ulong)1, converter.ToUInt64((long)1));
            Assert.AreEqual((ulong)long.MaxValue, converter.ToInt64(long.MaxValue));
        }

        [Test]
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

        [Test]
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

            Assert.AreEqual(unchecked((short)doubleMaxValue), converter.ToInt16(double.MaxValue));
            Assert.AreEqual(unchecked((short)doubleMinValue), converter.ToInt16(double.MinValue));
            Assert.AreEqual(unchecked((short)intMaxValue), converter.ToInt16(int.MaxValue));
            Assert.AreEqual(unchecked((short)intMinValue), converter.ToInt16(int.MinValue));
            Assert.AreEqual(unchecked((short)longMaxValue), converter.ToInt16(long.MaxValue));
            Assert.AreEqual(unchecked((short)longMinValue), converter.ToInt16(long.MinValue));

            Assert.AreEqual(unchecked((int)doubleMaxValue), converter.ToInt32(double.MaxValue));
            Assert.AreEqual(unchecked((int)doubleMinValue), converter.ToInt32(double.MinValue));
            Assert.AreEqual(unchecked((int)floatMaxValue), converter.ToInt32(float.MaxValue));
            Assert.AreEqual(unchecked((int)floatMinValue), converter.ToInt32(float.MinValue));
            Assert.AreEqual(unchecked((int)longMaxValue), converter.ToInt32(long.MaxValue));
            Assert.AreEqual(unchecked((int)longMinValue), converter.ToInt32(long.MinValue));
            Assert.AreEqual(unchecked((int)uintMaxValue), converter.ToInt32(uint.MaxValue));
            Assert.AreEqual(unchecked((int)ulongMaxValue), converter.ToInt32(ulong.MaxValue));

            Assert.AreEqual(unchecked((long)doubleMaxValue), converter.ToInt64(double.MaxValue));
            Assert.AreEqual(unchecked((long)doubleMinValue), converter.ToInt64(double.MinValue));
            Assert.AreEqual(unchecked((long)floatMaxValue), converter.ToInt64(float.MaxValue));
            Assert.AreEqual(unchecked((long)floatMinValue), converter.ToInt64(float.MinValue));
            Assert.AreEqual(unchecked((long)ulongMaxValue), converter.ToInt64(ulong.MaxValue));

            Assert.AreEqual(unchecked((float)(doubleMaxValue / 10.0)), converter.ToSingle(double.MaxValue / 10.0));
            Assert.AreEqual(unchecked((float)(doubleMinValue / 10.0)), converter.ToSingle(double.MinValue / 10.0));

            Assert.AreEqual(unchecked((ushort)doubleMaxValue), converter.ToUInt16(double.MaxValue));
            Assert.AreEqual(unchecked((ushort)doubleMinValue), converter.ToUInt16(double.MinValue));
            Assert.AreEqual(unchecked((ushort)intMaxValue), converter.ToUInt16(int.MaxValue));
            Assert.AreEqual(unchecked((ushort)intMinValue), converter.ToUInt16(int.MinValue));
            Assert.AreEqual(unchecked((ushort)longMaxValue), converter.ToUInt16(long.MaxValue));
            Assert.AreEqual(unchecked((ushort)longMinValue), converter.ToUInt16(long.MinValue));

            Assert.AreEqual(unchecked((uint)doubleMaxValue), converter.ToUInt32(double.MaxValue));
            Assert.AreEqual(unchecked((uint)doubleMinValue), converter.ToUInt32(double.MinValue));
            Assert.AreEqual(unchecked((uint)intMinValue), converter.ToUInt32(int.MinValue));
            Assert.AreEqual(unchecked((uint)longMaxValue), converter.ToUInt32(long.MaxValue));
            Assert.AreEqual(unchecked((uint)longMinValue), converter.ToUInt32(long.MinValue));

            Assert.AreEqual(unchecked((ulong)doubleMaxValue), converter.ToUInt64(double.MaxValue));
            Assert.AreEqual(unchecked((ulong)doubleMinValue), converter.ToUInt64(double.MinValue));
            Assert.AreEqual(unchecked((ulong)intMinValue), converter.ToUInt64(int.MinValue));
            Assert.AreEqual(unchecked((ulong)longMinValue), converter.ToUInt64(long.MinValue));
        }

        [Test]
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

        [Test]
        public void TestAllowTruncationTrue()
        {
            var converter = new RepresentationConverter(false, true);

            Assert.AreEqual((double)long.MaxValue, converter.ToDouble(long.MaxValue));
            Assert.AreEqual((double)ulong.MaxValue, converter.ToDouble(ulong.MaxValue));
            Assert.AreEqual((short)1, converter.ToInt16((double)1.5));
            Assert.AreEqual((int)1, converter.ToInt32((double)1.5));
            Assert.AreEqual((int)1, converter.ToInt32((float)1.5F));
            Assert.AreEqual((long)1, converter.ToInt64((double)1.5));
            Assert.AreEqual((long)1, converter.ToInt64((float)1.5F));
            Assert.AreEqual((float)0.0F, converter.ToSingle(double.Epsilon));
            Assert.AreEqual((ushort)1, converter.ToUInt16((double)1.5));
            Assert.AreEqual((uint)1, converter.ToUInt32((double)1.5));
            Assert.AreEqual((ulong)1, converter.ToUInt64((double)1.5));
        }
    }
}
