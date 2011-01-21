/* Copyright 2010-2011 10gen Inc.
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
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.DefaultSerializer;
using MongoDB.Bson.Serialization;

namespace MongoDB.BsonUnitTests.DefaultSerializer {
    [TestFixture]
    public class RepresentationSerializationOptionsTests {
        [Test]
        public void TestDefaults() {
            var options = new RepresentationSerializationOptions(BsonType.Int32);
            Assert.AreEqual(BsonType.Int32, options.Representation);
            Assert.AreEqual(false, options.AllowOverflow);
            Assert.AreEqual(false, options.AllowTruncation);
        }

        [Test]
        public void TestFalseFalse() {
            var options = new RepresentationSerializationOptions(BsonType.Int32, false, false);
            Assert.AreEqual(BsonType.Int32, options.Representation);
            Assert.AreEqual(false, options.AllowOverflow);
            Assert.AreEqual(false, options.AllowTruncation);
        }

        [Test]
        public void TestFalseTrue() {
            var options = new RepresentationSerializationOptions(BsonType.Int32, false, true);
            Assert.AreEqual(BsonType.Int32, options.Representation);
            Assert.AreEqual(false, options.AllowOverflow);
            Assert.AreEqual(true, options.AllowTruncation);
        }

        [Test]
        public void TestTrueFalse() {
            var options = new RepresentationSerializationOptions(BsonType.Int32, true, false);
            Assert.AreEqual(BsonType.Int32, options.Representation);
            Assert.AreEqual(true, options.AllowOverflow);
            Assert.AreEqual(false, options.AllowTruncation);
        }

        [Test]
        public void TestTrueTrue() {
            var options = new RepresentationSerializationOptions(BsonType.Int32, true, true);
            Assert.AreEqual(BsonType.Int32, options.Representation);
            Assert.AreEqual(true, options.AllowOverflow);
            Assert.AreEqual(true, options.AllowTruncation);
        }

        [Test]
        public void TestConversions() {
            var options = new RepresentationSerializationOptions(BsonType.Int32, false, false);
            Assert.AreEqual(1.5, options.ToDouble((double) 1.5));
            Assert.AreEqual(1.5, options.ToDouble((float) 1.5F));
            Assert.AreEqual(double.MinValue, options.ToDouble(float.MinValue));
            Assert.AreEqual(double.MaxValue, options.ToDouble(float.MaxValue));
            Assert.AreEqual(double.NegativeInfinity, options.ToDouble(float.NegativeInfinity));
            Assert.AreEqual(double.PositiveInfinity, options.ToDouble(float.PositiveInfinity));
            Assert.AreEqual(double.NaN, options.ToDouble(float.NaN));
            Assert.AreEqual(1, options.ToDouble((int) 1));
            Assert.AreEqual(1, options.ToDouble((long) 1));
            Assert.AreEqual(1, options.ToDouble((short) 1));
            Assert.AreEqual(1, options.ToDouble((uint) 1));
            Assert.AreEqual(1, options.ToDouble((ulong) 1));
            Assert.AreEqual(1, options.ToDouble((ushort) 1));
            Assert.AreEqual((short) 1, options.ToInt16((double) 1.0));
            Assert.AreEqual((short) 1, options.ToInt16((int) 1));
            Assert.AreEqual((short) 1, options.ToInt16((long) 1));
            Assert.AreEqual((int) 1, options.ToInt32((double) 1.0));
            Assert.AreEqual((int) 1, options.ToInt32((float) 1.0F));
            Assert.AreEqual((int) 1, options.ToInt32((int) 1));
            Assert.AreEqual((int) 1, options.ToInt32((long) 1));
            Assert.AreEqual((int) 1, options.ToInt32((short) 1));
            Assert.AreEqual((int) 1, options.ToInt32((uint) 1));
            Assert.AreEqual((int) 1, options.ToInt32((ulong) 1));
            Assert.AreEqual((int) 1, options.ToInt32((ushort) 1));
            Assert.AreEqual((long) 1, options.ToInt64((double) 1.0));
            Assert.AreEqual((long) 1, options.ToInt64((float) 1.0F));
            Assert.AreEqual((long) 1, options.ToInt64((int) 1));
            Assert.AreEqual((long) 1, options.ToInt64((long) 1));
            Assert.AreEqual((long) 1, options.ToInt64((short) 1));
            Assert.AreEqual((long) 1, options.ToInt64((uint) 1));
            Assert.AreEqual((long) 1, options.ToInt64((ulong) 1));
            Assert.AreEqual((long) 1, options.ToInt64((ushort) 1));
            Assert.AreEqual((float) 1.0F, options.ToSingle((double) 1.0));
            Assert.AreEqual((float) 1.0F, options.ToSingle((int) 1));
            Assert.AreEqual((float) 1.0F, options.ToSingle((long) 1));
            Assert.AreEqual((ushort) 1, options.ToUInt16((double) 1.0));
            Assert.AreEqual((ushort) 1, options.ToUInt16((int) 1));
            Assert.AreEqual((ushort) 1, options.ToUInt16((long) 1));
            Assert.AreEqual((uint) 1, options.ToUInt32((double) 1.0));
            Assert.AreEqual((uint) 1, options.ToUInt32((int) 1));
            Assert.AreEqual((uint) 1, options.ToUInt32((long) 1));
            Assert.AreEqual((ulong) 1, options.ToUInt64((double) 1.0));
            Assert.AreEqual((ulong) 1, options.ToUInt64((int) 1));
            Assert.AreEqual((ulong) 1, options.ToUInt64((long) 1));
        }

        [Test]
        public void TestAllowOverflowFalse() {
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
        public void TestAllowOverflowTrue() {
            var options = new RepresentationSerializationOptions(BsonType.Int32, true, false);
            Assert.AreEqual(unchecked((short) double.MaxValue), options.ToInt16(double.MaxValue));
            Assert.AreEqual(unchecked((short) double.MinValue), options.ToInt16(double.MinValue));
            Assert.AreEqual(unchecked((short) int.MaxValue), options.ToInt16(int.MaxValue));
            Assert.AreEqual(unchecked((short) int.MinValue), options.ToInt16(int.MinValue));
            Assert.AreEqual(unchecked((short) long.MaxValue), options.ToInt16(long.MaxValue));
            Assert.AreEqual(unchecked((short) long.MinValue), options.ToInt16(long.MinValue));
            // Assert.AreEqual(unchecked((int) double.MaxValue), options.ToInt32(double.MaxValue)); // TODO: why is this failing?
            // Assert.AreEqual(unchecked((int) double.MinValue), options.ToInt32(double.MinValue)); // TODO: why is this failing?
            // Assert.AreEqual(unchecked((int) float.MaxValue), options.ToInt32(float.MaxValue)); // TODO: why is this failing?
            // Assert.AreEqual(unchecked((int) float.MinValue), options.ToInt32(float.MinValue)); // TODO: why is this failing?
            Assert.AreEqual(unchecked((int) long.MaxValue), options.ToInt32(long.MaxValue));
            Assert.AreEqual(unchecked((int) long.MinValue), options.ToInt32(long.MinValue));
            Assert.AreEqual(unchecked((int) uint.MaxValue), options.ToInt32(uint.MaxValue));
            Assert.AreEqual(unchecked((int) ulong.MaxValue), options.ToInt32(ulong.MaxValue));
            Assert.AreEqual(unchecked((long) double.MaxValue), options.ToInt64(double.MaxValue));
            Assert.AreEqual(unchecked((long) double.MinValue), options.ToInt64(double.MinValue));
            Assert.AreEqual(unchecked((long) float.MaxValue), options.ToInt64(float.MaxValue));
            Assert.AreEqual(unchecked((long) float.MinValue), options.ToInt64(float.MinValue));
            Assert.AreEqual(unchecked((long) ulong.MaxValue), options.ToInt64(ulong.MaxValue));
            Assert.AreEqual(unchecked((float) (double.MaxValue / 10.0)), options.ToSingle(double.MaxValue / 10.0));
            Assert.AreEqual(unchecked((float) (double.MinValue / 10.0)), options.ToSingle(double.MinValue / 10.0));
            Assert.AreEqual(unchecked((ushort) double.MaxValue), options.ToUInt16(double.MaxValue));
            Assert.AreEqual(unchecked((ushort) double.MinValue), options.ToUInt16(double.MinValue));
            Assert.AreEqual(unchecked((ushort) int.MaxValue), options.ToUInt16(int.MaxValue));
            Assert.AreEqual(unchecked((ushort) int.MinValue), options.ToUInt16(int.MinValue));
            Assert.AreEqual(unchecked((ushort) long.MaxValue), options.ToUInt16(long.MaxValue));
            Assert.AreEqual(unchecked((ushort) long.MinValue), options.ToUInt16(long.MinValue));
            Assert.AreEqual(unchecked((uint) double.MaxValue), options.ToUInt32(double.MaxValue));
            Assert.AreEqual(unchecked((uint) double.MinValue), options.ToUInt32(double.MinValue));
            Assert.AreEqual(unchecked((uint) int.MinValue), options.ToUInt32(int.MinValue));
            Assert.AreEqual(unchecked((uint) long.MaxValue), options.ToUInt32(long.MaxValue));
            Assert.AreEqual(unchecked((uint) long.MinValue), options.ToUInt32(long.MinValue));
            Assert.AreEqual(unchecked((ulong) double.MaxValue), options.ToUInt64(double.MaxValue));
            Assert.AreEqual(unchecked((ulong) double.MinValue), options.ToUInt64(double.MinValue));
            Assert.AreEqual(unchecked((ulong) int.MinValue), options.ToUInt64(int.MinValue));
            Assert.AreEqual(unchecked((ulong) long.MinValue), options.ToUInt64(long.MinValue));
        }

        [Test]
        public void TestAllowTruncationFalse() {
            var options = new RepresentationSerializationOptions(BsonType.Int32, false, false);
            Assert.Throws<TruncationException>(() => options.ToDouble(long.MaxValue));
            Assert.Throws<TruncationException>(() => options.ToDouble(ulong.MaxValue));
            Assert.Throws<TruncationException>(() => options.ToInt16((double) 1.5));
            Assert.Throws<TruncationException>(() => options.ToInt32((double) 1.5));
            Assert.Throws<TruncationException>(() => options.ToInt32((float) 1.5F));
            Assert.Throws<TruncationException>(() => options.ToInt64((double) 1.5));
            Assert.Throws<TruncationException>(() => options.ToInt64((float) 1.5F));
            Assert.Throws<TruncationException>(() => options.ToSingle(double.Epsilon));
            Assert.Throws<TruncationException>(() => options.ToUInt16((double) 1.5));
            Assert.Throws<TruncationException>(() => options.ToUInt32((double) 1.5));
            Assert.Throws<TruncationException>(() => options.ToUInt64((double) 1.5));
        }

        [Test]
        public void TestAllowTruncationTrue() {
            var options = new RepresentationSerializationOptions(BsonType.Int32, false, true);
            Assert.AreEqual((double) long.MaxValue, options.ToDouble(long.MaxValue));
            Assert.AreEqual((double) ulong.MaxValue, options.ToDouble(ulong.MaxValue));
            Assert.AreEqual((short) 1, options.ToInt16((double) 1.5));
            Assert.AreEqual((int) 1, options.ToInt32((double) 1.5));
            Assert.AreEqual((int) 1, options.ToInt32((float) 1.5F));
            Assert.AreEqual((long) 1, options.ToInt64((double) 1.5));
            Assert.AreEqual((long) 1, options.ToInt64((float) 1.5F));
            Assert.AreEqual((float) 0.0F, options.ToSingle(double.Epsilon));
            Assert.AreEqual((ushort) 1, options.ToUInt16((double) 1.5));
            Assert.AreEqual((uint) 1, options.ToUInt32((double) 1.5));
            Assert.AreEqual((ulong) 1, options.ToUInt64((double) 1.5));
        }
    }
}
