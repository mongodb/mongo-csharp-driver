/* Copyright 2010-2013 10gen Inc.
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

namespace MongoDB.BsonUnitTests.Serialization
{
    [TestFixture]
    public class RepresentationSerializationOptionsTests
    {
        [Test]
        public void TestDefaults()
        {
            var options = new RepresentationSerializationOptions(BsonType.Int32);
            Assert.AreEqual(BsonType.Int32, options.Representation);
            Assert.AreEqual(false, options.AllowOverflow);
            Assert.AreEqual(false, options.AllowTruncation);
        }

        [Test]
        public void TestFalseFalse()
        {
            var options = new RepresentationSerializationOptions(BsonType.Int32, false, false);
            Assert.AreEqual(BsonType.Int32, options.Representation);
            Assert.AreEqual(false, options.AllowOverflow);
            Assert.AreEqual(false, options.AllowTruncation);
        }

        [Test]
        public void TestFalseTrue()
        {
            var options = new RepresentationSerializationOptions(BsonType.Int32, false, true);
            Assert.AreEqual(BsonType.Int32, options.Representation);
            Assert.AreEqual(false, options.AllowOverflow);
            Assert.AreEqual(true, options.AllowTruncation);
        }

        [Test]
        public void TestTrueFalse()
        {
            var options = new RepresentationSerializationOptions(BsonType.Int32, true, false);
            Assert.AreEqual(BsonType.Int32, options.Representation);
            Assert.AreEqual(true, options.AllowOverflow);
            Assert.AreEqual(false, options.AllowTruncation);
        }

        [Test]
        public void TestTrueTrue()
        {
            var options = new RepresentationSerializationOptions(BsonType.Int32, true, true);
            Assert.AreEqual(BsonType.Int32, options.Representation);
            Assert.AreEqual(true, options.AllowOverflow);
            Assert.AreEqual(true, options.AllowTruncation);
        }

        [Test]
        public void TestConversions()
        {
            var options = new RepresentationSerializationOptions(BsonType.Int32, false, false);
            Assert.AreEqual((double)1.5, options.ToDouble((double)1.5));
            Assert.AreEqual((double)double.MinValue, options.ToDouble(double.MinValue));
            Assert.AreEqual((double)double.MaxValue, options.ToDouble(double.MaxValue));
            Assert.AreEqual((double)double.NegativeInfinity, options.ToDouble(double.NegativeInfinity));
            Assert.AreEqual((double)double.PositiveInfinity, options.ToDouble(double.PositiveInfinity));
            Assert.AreEqual((double)double.NaN, options.ToDouble(double.NaN));
            Assert.AreEqual((double)1.5, options.ToDouble((float)1.5F));
            Assert.AreEqual((double)double.MinValue, options.ToDouble(float.MinValue));
            Assert.AreEqual((double)double.MaxValue, options.ToDouble(float.MaxValue));
            Assert.AreEqual((double)double.NegativeInfinity, options.ToDouble(float.NegativeInfinity));
            Assert.AreEqual((double)double.PositiveInfinity, options.ToDouble(float.PositiveInfinity));
            Assert.AreEqual((double)double.NaN, options.ToDouble(float.NaN));
            Assert.AreEqual((double)1.0, options.ToDouble((int)1));
            Assert.AreEqual((double)int.MaxValue, options.ToDouble(int.MaxValue));
            Assert.AreEqual((double)int.MinValue, options.ToDouble(int.MinValue));
            Assert.AreEqual((double)1.0, options.ToDouble((long)1));
            Assert.AreEqual((double)1.0, options.ToDouble((short)1));
            Assert.AreEqual((double)short.MaxValue, options.ToDouble(short.MaxValue));
            Assert.AreEqual((double)short.MinValue, options.ToDouble(short.MinValue));
            Assert.AreEqual((double)1.0, options.ToDouble((uint)1));
            Assert.AreEqual((double)uint.MaxValue, options.ToDouble(uint.MaxValue));
            Assert.AreEqual((double)uint.MinValue, options.ToDouble(uint.MinValue));
            Assert.AreEqual((double)1.0, options.ToDouble((ulong)1));
            Assert.AreEqual((double)ulong.MinValue, options.ToDouble(ulong.MinValue));
            Assert.AreEqual((double)1.0, options.ToDouble((ushort)1));
            Assert.AreEqual((double)ushort.MaxValue, options.ToDouble(ushort.MaxValue));
            Assert.AreEqual((double)ushort.MinValue, options.ToDouble(ushort.MinValue));

            Assert.AreEqual((short)1, options.ToInt16((double)1.0));
            Assert.AreEqual((short)1, options.ToInt16((int)1));
            Assert.AreEqual((short)1, options.ToInt16((long)1));

            Assert.AreEqual((int)1, options.ToInt32((double)1.0));
            Assert.AreEqual((int)1, options.ToInt32((float)1.0F));
            Assert.AreEqual((int)1, options.ToInt32((int)1));
            Assert.AreEqual((int)int.MaxValue, options.ToInt32(int.MaxValue));
            Assert.AreEqual((int)int.MinValue, options.ToInt32(int.MinValue));
            Assert.AreEqual((int)1, options.ToInt32((long)1));
            Assert.AreEqual((int)1, options.ToInt32((short)1));
            Assert.AreEqual((int)short.MaxValue, options.ToInt32(short.MaxValue));
            Assert.AreEqual((int)short.MinValue, options.ToInt32(short.MinValue));
            Assert.AreEqual((int)1, options.ToInt32((uint)1));
            Assert.AreEqual((int)uint.MinValue, options.ToInt32(uint.MinValue));
            Assert.AreEqual((int)1, options.ToInt32((ulong)1));
            Assert.AreEqual((int)ulong.MinValue, options.ToInt32(ulong.MinValue));
            Assert.AreEqual((int)1, options.ToInt32((ushort)1));
            Assert.AreEqual((int)ushort.MaxValue, options.ToInt32(ushort.MaxValue));
            Assert.AreEqual((int)ushort.MinValue, options.ToInt32(ushort.MinValue));

            Assert.AreEqual((long)1, options.ToInt64((double)1.0));
            Assert.AreEqual((long)1, options.ToInt64((float)1.0F));
            Assert.AreEqual((long)1, options.ToInt64((int)1));
            Assert.AreEqual((long)int.MaxValue, options.ToInt64(int.MaxValue));
            Assert.AreEqual((long)int.MinValue, options.ToInt64(int.MinValue));
            Assert.AreEqual((long)1, options.ToInt64((long)1));
            Assert.AreEqual((long)long.MaxValue, options.ToInt64(long.MaxValue));
            Assert.AreEqual((long)long.MinValue, options.ToInt64(long.MinValue));
            Assert.AreEqual((long)1, options.ToInt64((short)1));
            Assert.AreEqual((long)short.MaxValue, options.ToInt64(short.MaxValue));
            Assert.AreEqual((long)short.MinValue, options.ToInt64(short.MinValue));
            Assert.AreEqual((long)1, options.ToInt64((uint)1));
            Assert.AreEqual((long)uint.MaxValue, options.ToInt64(uint.MaxValue));
            Assert.AreEqual((long)uint.MinValue, options.ToInt64(uint.MinValue));
            Assert.AreEqual((long)1, options.ToInt64((ulong)1));
            Assert.AreEqual((long)ulong.MinValue, options.ToInt64(ulong.MinValue));
            Assert.AreEqual((long)1, options.ToInt64((ushort)1));
            Assert.AreEqual((long)ushort.MaxValue, options.ToInt64(ushort.MaxValue));
            Assert.AreEqual((long)ushort.MinValue, options.ToInt64(ushort.MinValue));

            Assert.AreEqual((float)1.0F, options.ToSingle((double)1.0));
            Assert.AreEqual((float)float.MinValue, options.ToSingle(double.MinValue));
            Assert.AreEqual((float)float.MaxValue, options.ToSingle(double.MaxValue));
            Assert.AreEqual((float)float.NegativeInfinity, options.ToSingle(double.NegativeInfinity));
            Assert.AreEqual((float)float.PositiveInfinity, options.ToSingle(double.PositiveInfinity));
            Assert.AreEqual((float)float.NaN, options.ToSingle(double.NaN));
            Assert.AreEqual((float)1.0F, options.ToSingle((int)1));
            Assert.AreEqual((float)1.0F, options.ToSingle((long)1));

            Assert.AreEqual((ushort)1, options.ToUInt16((double)1.0));
            Assert.AreEqual((ushort)1, options.ToUInt16((int)1));
            Assert.AreEqual((ushort)1, options.ToUInt16((long)1));

            Assert.AreEqual((uint)1, options.ToUInt32((double)1.0));
            Assert.AreEqual((uint)1, options.ToUInt32((int)1));
            Assert.AreEqual((uint)1, options.ToUInt32((long)1));

            Assert.AreEqual((ulong)1, options.ToUInt64((double)1.0));
            Assert.AreEqual((ulong)1, options.ToUInt64((int)1));
            Assert.AreEqual((ulong)int.MaxValue, options.ToInt64(int.MaxValue));
            Assert.AreEqual((ulong)1, options.ToUInt64((long)1));
            Assert.AreEqual((ulong)long.MaxValue, options.ToInt64(long.MaxValue));
        }

        [Test]
        public void TestAllowOverflowFalse()
        {
            var options = new RepresentationSerializationOptions(BsonType.Int32, false, false);
            Assert.Throws<OverflowException>(() => options.ToInt16(double.MaxValue));
            Assert.Throws<OverflowException>(() => options.ToInt16(double.MinValue));
            Assert.Throws<OverflowException>(() => options.ToInt16(int.MaxValue));
            Assert.Throws<OverflowException>(() => options.ToInt16(int.MinValue));
            Assert.Throws<OverflowException>(() => options.ToInt16(long.MaxValue));
            Assert.Throws<OverflowException>(() => options.ToInt16(long.MinValue));

            Assert.Throws<OverflowException>(() => options.ToInt32(double.MaxValue));
            Assert.Throws<OverflowException>(() => options.ToInt32(double.MinValue));
            Assert.Throws<OverflowException>(() => options.ToInt32(float.MaxValue));
            Assert.Throws<OverflowException>(() => options.ToInt32(float.MinValue));
            Assert.Throws<OverflowException>(() => options.ToInt32(long.MaxValue));
            Assert.Throws<OverflowException>(() => options.ToInt32(long.MinValue));
            Assert.Throws<OverflowException>(() => options.ToInt32(uint.MaxValue));
            Assert.Throws<OverflowException>(() => options.ToInt32(ulong.MaxValue));

            Assert.Throws<OverflowException>(() => options.ToInt64(double.MaxValue));
            Assert.Throws<OverflowException>(() => options.ToInt64(double.MinValue));
            Assert.Throws<OverflowException>(() => options.ToInt64(float.MaxValue));
            Assert.Throws<OverflowException>(() => options.ToInt64(float.MinValue));
            Assert.Throws<OverflowException>(() => options.ToInt64(ulong.MaxValue));

            Assert.Throws<OverflowException>(() => options.ToSingle(double.MaxValue / 10.0));
            Assert.Throws<OverflowException>(() => options.ToSingle(double.MinValue / 10.0));

            Assert.Throws<OverflowException>(() => options.ToUInt16(double.MaxValue));
            Assert.Throws<OverflowException>(() => options.ToUInt16(double.MinValue));
            Assert.Throws<OverflowException>(() => options.ToUInt16(int.MaxValue));
            Assert.Throws<OverflowException>(() => options.ToUInt16(int.MinValue));
            Assert.Throws<OverflowException>(() => options.ToUInt16(long.MaxValue));
            Assert.Throws<OverflowException>(() => options.ToUInt16(long.MinValue));

            Assert.Throws<OverflowException>(() => options.ToUInt32(double.MaxValue));
            Assert.Throws<OverflowException>(() => options.ToUInt32(double.MinValue));
            Assert.Throws<OverflowException>(() => options.ToUInt32(int.MinValue));
            Assert.Throws<OverflowException>(() => options.ToUInt32(long.MaxValue));
            Assert.Throws<OverflowException>(() => options.ToUInt32(long.MinValue));

            Assert.Throws<OverflowException>(() => options.ToUInt64(double.MaxValue));
            Assert.Throws<OverflowException>(() => options.ToUInt64(double.MinValue));
            Assert.Throws<OverflowException>(() => options.ToUInt64(int.MinValue));
            Assert.Throws<OverflowException>(() => options.ToUInt64(long.MinValue));
        }

        [Test]
        public void TestAllowOverflowTrue()
        {
            var options = new RepresentationSerializationOptions(BsonType.Int32, true, false);

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

            Assert.AreEqual(unchecked((short)doubleMaxValue), options.ToInt16(double.MaxValue));
            Assert.AreEqual(unchecked((short)doubleMinValue), options.ToInt16(double.MinValue));
            Assert.AreEqual(unchecked((short)intMaxValue), options.ToInt16(int.MaxValue));
            Assert.AreEqual(unchecked((short)intMinValue), options.ToInt16(int.MinValue));
            Assert.AreEqual(unchecked((short)longMaxValue), options.ToInt16(long.MaxValue));
            Assert.AreEqual(unchecked((short)longMinValue), options.ToInt16(long.MinValue));

            Assert.AreEqual(unchecked((int)doubleMaxValue), options.ToInt32(double.MaxValue));
            Assert.AreEqual(unchecked((int)doubleMinValue), options.ToInt32(double.MinValue));
            Assert.AreEqual(unchecked((int)floatMaxValue), options.ToInt32(float.MaxValue));
            Assert.AreEqual(unchecked((int)floatMinValue), options.ToInt32(float.MinValue));
            Assert.AreEqual(unchecked((int)longMaxValue), options.ToInt32(long.MaxValue));
            Assert.AreEqual(unchecked((int)longMinValue), options.ToInt32(long.MinValue));
            Assert.AreEqual(unchecked((int)uintMaxValue), options.ToInt32(uint.MaxValue));
            Assert.AreEqual(unchecked((int)ulongMaxValue), options.ToInt32(ulong.MaxValue));

            Assert.AreEqual(unchecked((long)doubleMaxValue), options.ToInt64(double.MaxValue));
            Assert.AreEqual(unchecked((long)doubleMinValue), options.ToInt64(double.MinValue));
            Assert.AreEqual(unchecked((long)floatMaxValue), options.ToInt64(float.MaxValue));
            Assert.AreEqual(unchecked((long)floatMinValue), options.ToInt64(float.MinValue));
            Assert.AreEqual(unchecked((long)ulongMaxValue), options.ToInt64(ulong.MaxValue));

            Assert.AreEqual(unchecked((float)(doubleMaxValue / 10.0)), options.ToSingle(double.MaxValue / 10.0));
            Assert.AreEqual(unchecked((float)(doubleMinValue / 10.0)), options.ToSingle(double.MinValue / 10.0));

            Assert.AreEqual(unchecked((ushort)doubleMaxValue), options.ToUInt16(double.MaxValue));
            Assert.AreEqual(unchecked((ushort)doubleMinValue), options.ToUInt16(double.MinValue));
            Assert.AreEqual(unchecked((ushort)intMaxValue), options.ToUInt16(int.MaxValue));
            Assert.AreEqual(unchecked((ushort)intMinValue), options.ToUInt16(int.MinValue));
            Assert.AreEqual(unchecked((ushort)longMaxValue), options.ToUInt16(long.MaxValue));
            Assert.AreEqual(unchecked((ushort)longMinValue), options.ToUInt16(long.MinValue));

            Assert.AreEqual(unchecked((uint)doubleMaxValue), options.ToUInt32(double.MaxValue));
            Assert.AreEqual(unchecked((uint)doubleMinValue), options.ToUInt32(double.MinValue));
            Assert.AreEqual(unchecked((uint)intMinValue), options.ToUInt32(int.MinValue));
            Assert.AreEqual(unchecked((uint)longMaxValue), options.ToUInt32(long.MaxValue));
            Assert.AreEqual(unchecked((uint)longMinValue), options.ToUInt32(long.MinValue));

            Assert.AreEqual(unchecked((ulong)doubleMaxValue), options.ToUInt64(double.MaxValue));
            Assert.AreEqual(unchecked((ulong)doubleMinValue), options.ToUInt64(double.MinValue));
            Assert.AreEqual(unchecked((ulong)intMinValue), options.ToUInt64(int.MinValue));
            Assert.AreEqual(unchecked((ulong)longMinValue), options.ToUInt64(long.MinValue));
        }

        [Test]
        public void TestAllowTruncationFalse()
        {
            var options = new RepresentationSerializationOptions(BsonType.Int32, false, false);
            Assert.Throws<TruncationException>(() => options.ToDouble(long.MaxValue));
            Assert.Throws<TruncationException>(() => options.ToDouble(ulong.MaxValue));
            Assert.Throws<TruncationException>(() => options.ToInt16((double)1.5));
            Assert.Throws<TruncationException>(() => options.ToInt32((double)1.5));
            Assert.Throws<TruncationException>(() => options.ToInt32((float)1.5F));
            Assert.Throws<TruncationException>(() => options.ToInt64((double)1.5));
            Assert.Throws<TruncationException>(() => options.ToInt64((float)1.5F));
            Assert.Throws<TruncationException>(() => options.ToSingle(double.Epsilon));
            Assert.Throws<TruncationException>(() => options.ToUInt16((double)1.5));
            Assert.Throws<TruncationException>(() => options.ToUInt32((double)1.5));
            Assert.Throws<TruncationException>(() => options.ToUInt64((double)1.5));
        }

        [Test]
        public void TestAllowTruncationTrue()
        {
            var options = new RepresentationSerializationOptions(BsonType.Int32, false, true);
            Assert.AreEqual((double)long.MaxValue, options.ToDouble(long.MaxValue));
            Assert.AreEqual((double)ulong.MaxValue, options.ToDouble(ulong.MaxValue));
            Assert.AreEqual((short)1, options.ToInt16((double)1.5));
            Assert.AreEqual((int)1, options.ToInt32((double)1.5));
            Assert.AreEqual((int)1, options.ToInt32((float)1.5F));
            Assert.AreEqual((long)1, options.ToInt64((double)1.5));
            Assert.AreEqual((long)1, options.ToInt64((float)1.5F));
            Assert.AreEqual((float)0.0F, options.ToSingle(double.Epsilon));
            Assert.AreEqual((ushort)1, options.ToUInt16((double)1.5));
            Assert.AreEqual((uint)1, options.ToUInt32((double)1.5));
            Assert.AreEqual((ulong)1, options.ToUInt64((double)1.5));
        }
    }
}
