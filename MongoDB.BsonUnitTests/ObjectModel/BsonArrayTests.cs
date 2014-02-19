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
using System.Linq;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests
{
    [TestFixture]
    public class BsonArrayTests
    {
        [Test]
        public void TestAdd()
        {
            var array = new BsonArray();
            var value = BsonValue.Create(1);
            array.Add(value);
            Assert.AreEqual(1, array.Count);
            Assert.AreEqual(value, array[0]);
        }

        [Test]
        public void TestAddNull()
        {
            var array = new BsonArray();
            var value = (BsonValue)null;
            array.Add(value);
            Assert.AreEqual(0, array.Count);
        }

        [Test]
        public void TestAddRangeBooleanNull()
        {
            var array = new BsonArray();
            var values = (bool[])null;
            array.AddRange(values);
            Assert.AreEqual(0, array.Count);
        }

        [Test]
        public void TestAddRangeBsonValueNull()
        {
            var array = new BsonArray();
            var values = (BsonValue[])null;
            array.AddRange(values);
            Assert.AreEqual(0, array.Count);
        }

        [Test]
        public void TestAddRangeDateTimeNull()
        {
            var array = new BsonArray();
            var values = (DateTime[])null;
            array.AddRange(values);
            Assert.AreEqual(0, array.Count);
        }

        [Test]
        public void TestAddRangeDoubleNull()
        {
            var array = new BsonArray();
            var values = (double[])null;
            array.AddRange(values);
            Assert.AreEqual(0, array.Count);
        }

        [Test]
        public void TestAddRangeInt32Null()
        {
            var array = new BsonArray();
            var values = (int[])null;
            array.AddRange(values);
            Assert.AreEqual(0, array.Count);
        }

        [Test]
        public void TestAddRangeInt64Null()
        {
            var array = new BsonArray();
            var values = (long[])null;
            array.AddRange(values);
            Assert.AreEqual(0, array.Count);
        }

        [Test]
        public void TestAddRangeObjectIdNull()
        {
            var array = new BsonArray();
            var values = (ObjectId[])null;
            array.AddRange(values);
            Assert.AreEqual(0, array.Count);
        }

        [Test]
        public void TestAddRangeStringNull()
        {
            var array = new BsonArray();
            var values = (string[])null;
            array.AddRange(values);
            Assert.AreEqual(0, array.Count);
        }

        [Test]
        public void TestAddRangeIEnumerableNull()
        {
            var array = new BsonArray();
            var values = (object[])null;
            array.AddRange(values);
            Assert.AreEqual(0, array.Count);
        }

        [Test]
        public void TestCapacity()
        {
            var array = new BsonArray(0);
            Assert.AreEqual(0, array.Capacity);
            array.Capacity = 8;
            Assert.AreEqual(8, array.Capacity);
        }

        [Test]
        public void TestClone()
        {
            var array = new BsonArray(4) { 1, 2, new BsonArray(3) { 3, 4 } };
            var clone = (BsonArray)array.Clone();
            Assert.AreEqual(4, clone.Capacity);
            Assert.AreEqual(3, clone.Count);
            Assert.AreEqual(1, clone[0].AsInt32);
            Assert.AreEqual(2, clone[1].AsInt32);
            Assert.AreSame(array[2], clone[2]); // not deep cloned
        }

        [Test]
        public void TestClear()
        {
            var array = new BsonArray { 1, 2 };
            Assert.AreEqual(2, array.Count);
            array.Clear();
            Assert.AreEqual(0, array.Count);
        }

        [Test]
        public void TestContains()
        {
            var array = new BsonArray { 1, 2 };
            Assert.IsTrue(array.Contains(1));
            Assert.IsTrue(array.Contains(2));
            Assert.IsFalse(array.Contains(3));
        }

        [Test]
        public void TestContainsNull()
        {
            var array = new BsonArray { 1, 2 };
            Assert.IsFalse(array.Contains(null));
        }

        [Test]
        public void TestCompareTo()
        {
            var a = (BsonValue)new BsonArray { 1, 2 };
            var b = (BsonValue)new BsonArray { 1, 2, 3 };
            var c = (BsonValue)new BsonArray { 4 };
            Assert.AreEqual(1, a.CompareTo(null));
            Assert.AreEqual(0, a.CompareTo(a));
            Assert.AreEqual(-1, a.CompareTo(b));
            Assert.AreEqual(1, b.CompareTo(a));
            Assert.AreEqual(-1, a.CompareTo(c));
            Assert.AreEqual(1, c.CompareTo(a));
            Assert.AreEqual(1, a.CompareTo(1)); // Array > Int32
            Assert.AreEqual(-1, a.CompareTo(true)); // Array < Boolean
        }

        [Test]
        public void TestConstructorWithNoArguments()
        {
            var array = new BsonArray();
            Assert.AreEqual(BsonType.Array, array.BsonType);
            Assert.AreEqual(0, array.Capacity);
            Assert.AreEqual(0, array.Count);
            Assert.IsTrue(array.IsBsonArray);
            Assert.AreEqual(false, array.IsReadOnly);
        }

        [Test]
        public void TestConstructorWithCapacity()
        {
            var array = new BsonArray(4);
            Assert.AreEqual(BsonType.Array, array.BsonType);
            Assert.AreEqual(4, array.Capacity);
            Assert.AreEqual(0, array.Count);
            Assert.IsTrue(array.IsBsonArray);
            Assert.AreEqual(false, array.IsReadOnly);
        }

        [Test]
        public void TestCopyToBsonValueArray()
        {
            var bsonArray = new BsonArray { 1, 2 };
            var bsonValueArray = new BsonValue[2];
            bsonArray.CopyTo(bsonValueArray, 0);
            Assert.AreSame(bsonArray[0], bsonValueArray[0]);
            Assert.AreSame(bsonArray[1], bsonValueArray[1]);
        }

        [Test]
        public void TestCopyToOjbectArray()
        {
            var bsonArray = new BsonArray { 1, 2 };
            var bsonValueArray = new object[2];
#pragma warning disable 618
            bsonArray.CopyTo(bsonValueArray, 0);
#pragma warning restore
            Assert.AreEqual(1, bsonValueArray[0]);
            Assert.AreEqual(2, bsonValueArray[1]);
        }

        [Test]
        public void TestCreateBooleanArray()
        {
            var values = new bool[] { true, false };
            var array = new BsonArray(values);
            Assert.AreEqual(2, array.Count);
            Assert.IsInstanceOf<BsonBoolean>(array[0]);
            Assert.IsInstanceOf<BsonBoolean>(array[1]);
            Assert.AreEqual(true, array[0].AsBoolean);
            Assert.AreEqual(false, array[1].AsBoolean);
        }

        [Test]
        public void TestCreateBooleanArrayNull()
        {
            bool[] values = null;
#pragma warning disable 618
            var array = BsonArray.Create(values);
#pragma warning restore
            Assert.IsNull(array);
        }

        [Test]
        public void TestCreateBsonValueArray()
        {
            var values = new BsonValue[] { true, 1, null, 1.5 }; // embedded null is skipped by functional construction
            var array = new BsonArray(values);
            Assert.AreEqual(3, array.Count);
            Assert.IsInstanceOf<BsonBoolean>(array[0]);
            Assert.IsInstanceOf<BsonInt32>(array[1]);
            Assert.IsInstanceOf<BsonDouble>(array[2]);
            Assert.AreEqual(true, array[0].AsBoolean);
            Assert.AreEqual(1, array[1].AsInt32);
            Assert.AreEqual(1.5, array[2].AsDouble);
        }

        [Test]
        public void TestCreateBsonValueArrayNull()
        {
            BsonValue[] values = null;
#pragma warning disable 618
            var array = BsonArray.Create(values);
#pragma warning restore
            Assert.IsNull(array);
        }

        [Test]
        public void TestCreateDateTimeArray()
        {
            var value1 = DateTime.SpecifyKind(new DateTime(2011, 1, 18), DateTimeKind.Utc);
            var value2 = DateTime.SpecifyKind(new DateTime(2011, 1, 19), DateTimeKind.Utc);
            var values = new DateTime[] { value1, value2 };
            var array = new BsonArray(values);
            Assert.AreEqual(2, array.Count);
            Assert.IsInstanceOf<BsonDateTime>(array[0]);
            Assert.IsInstanceOf<BsonDateTime>(array[1]);
            Assert.AreEqual(value1, array[0].ToUniversalTime());
            Assert.AreEqual(value2, array[1].ToUniversalTime());
        }

        [Test]
        public void TestCreateDateTimeArrayNull()
        {
            DateTime[] values = null;
#pragma warning disable 618
            var array = BsonArray.Create(values);
#pragma warning restore
            Assert.IsNull(array);
        }

        [Test]
        public void TestCreateDoubleArray()
        {
            var values = new double[] { 1.5, 2.5 };
            var array = new BsonArray(values);
            Assert.AreEqual(2, array.Count);
            Assert.IsInstanceOf<BsonDouble>(array[0]);
            Assert.IsInstanceOf<BsonDouble>(array[1]);
            Assert.AreEqual(1.5, array[0].AsDouble);
            Assert.AreEqual(2.5, array[1].AsDouble);
        }

        [Test]
        public void TestCreateDoubleArrayNull()
        {
            double[] values = null;
#pragma warning disable 618
            var array = BsonArray.Create(values);
#pragma warning restore
            Assert.IsNull(array);
        }

        [Test]
        public void TestCreateInt32Array()
        {
            var values = new int[] { 1, 2 };
            var array = new BsonArray(values);
            Assert.AreEqual(2, array.Count);
            Assert.IsInstanceOf<BsonInt32>(array[0]);
            Assert.IsInstanceOf<BsonInt32>(array[1]);
            Assert.AreEqual(1, array[0].AsInt32);
            Assert.AreEqual(2, array[1].AsInt32);
        }

        [Test]
        public void TestCreateInt32ArrayNull()
        {
            int[] values = null;
#pragma warning disable 618
            var array = BsonArray.Create(values);
#pragma warning restore
            Assert.IsNull(array);
        }

        [Test]
        public void TestCreateInt64Array()
        {
            var values = new long[] { 1, 2 };
            var array = new BsonArray(values);
            Assert.AreEqual(2, array.Count);
            Assert.IsInstanceOf<BsonInt64>(array[0]);
            Assert.IsInstanceOf<BsonInt64>(array[1]);
            Assert.AreEqual(1, array[0].AsInt64);
            Assert.AreEqual(2, array[1].AsInt64);
        }

        [Test]
        public void TestCreateInt64ArrayNull()
        {
            long[] values = null;
#pragma warning disable 618
            var array = BsonArray.Create(values);
#pragma warning restore
            Assert.IsNull(array);
        }

        [Test]
        public void TestCreateObjectArray()
        {
            var values = new object[] { true, 1 , 1.5, null }; // null will be mapped to BsonNull.Value
            var array = new BsonArray(values);
            Assert.AreEqual(4, array.Count);
            Assert.IsInstanceOf<BsonBoolean>(array[0]);
            Assert.IsInstanceOf<BsonInt32>(array[1]);
            Assert.IsInstanceOf<BsonDouble>(array[2]);
            Assert.IsInstanceOf<BsonNull>(array[3]);
            Assert.AreEqual(true, array[0].AsBoolean);
            Assert.AreEqual(1, array[1].AsInt32);
            Assert.AreEqual(1.5, array[2].AsDouble);
            Assert.AreSame(BsonNull.Value, array[3]);
        }

        [Test]
        public void TestCreateObjectArrayNull()
        {
            object[] values = null;
#pragma warning disable 618
            var array = BsonArray.Create(values);
#pragma warning restore
            Assert.IsNull(array);
        }

        [Test]
        public void TestCreateObjectIdArray()
        {
            var value1 = ObjectId.GenerateNewId();
            var value2 = ObjectId.GenerateNewId();
            var values = new ObjectId[] { value1, value2 };
            var array = new BsonArray(values);
            Assert.AreEqual(2, array.Count);
            Assert.IsInstanceOf<BsonObjectId>(array[0]);
            Assert.IsInstanceOf<BsonObjectId>(array[1]);
            Assert.AreEqual(value1, array[0].AsObjectId);
            Assert.AreEqual(value2, array[1].AsObjectId);
        }

        [Test]
        public void TestCreateObjectIdArrayNull()
        {
            ObjectId[] values = null;
#pragma warning disable 618
            var array = BsonArray.Create(values);
#pragma warning restore
            Assert.IsNull(array);
        }

        [Test]
        public void TestCreateStringArray()
        {
            var values = new string[] { "a", "b", null }; // null will be mapped to BsonNull.Value
            var array = new BsonArray(values);
            Assert.AreEqual(3, array.Count);
            Assert.IsInstanceOf<BsonString>(array[0]);
            Assert.IsInstanceOf<BsonString>(array[1]);
            Assert.IsInstanceOf<BsonNull>(array[2]);
            Assert.AreEqual("a", array[0].AsString);
            Assert.AreEqual("b", array[1].AsString);
            Assert.AreSame(BsonNull.Value, array[2]);
        }

        [Test]
        public void TestCreateStringArrayNull()
        {
            string[] values = null;
#pragma warning disable 618
            var array = BsonArray.Create(values);
#pragma warning restore
            Assert.IsNull(array);
        }

        [Test]
        public void TestCreateFromObject()
        {
            var value = (object)new object[] { 1, 1.5, null }; // null will be mapped to BsonNull.Value
            var array = BsonArray.Create(value);
            Assert.AreEqual(3, array.Count);
            Assert.IsInstanceOf<BsonInt32>(array[0]);
            Assert.IsInstanceOf<BsonDouble>(array[1]);
            Assert.IsInstanceOf<BsonNull>(array[2]);
            Assert.AreEqual(1, array[0].AsInt32);
            Assert.AreEqual(1.5, array[1].AsDouble);
            Assert.AreSame(BsonNull.Value, array[2]);
        }

        [Test]
        public void TestCreateFromObjectNull()
        {
            object value = null;
            var array = BsonArray.Create(value);
            Assert.IsNull(array);
        }

        [Test]
        public void TestDeepClone()
        {
            var array = new BsonArray(4) { 1, 2, new BsonArray(3) { 3, 4 } };
            var clone = (BsonArray)array.DeepClone();
            Assert.AreEqual(4, clone.Capacity);
            Assert.AreEqual(3, clone.Count);
            Assert.AreEqual(1, clone[0].AsInt32);
            Assert.AreEqual(2, clone[1].AsInt32);
            Assert.AreNotSame(array[2], clone[2]); // deep cloned
            Assert.AreEqual(array[2], clone[2]);
        }

        [Test]
        public void TestEquals()
        {
            var a = new BsonArray { 1, 2 };
            var b = new BsonArray { 1, 2 };
            var c = new BsonArray { 3, 4 };
            Assert.IsTrue(a.Equals((object)a));
            Assert.IsTrue(a.Equals((object)b));
            Assert.IsFalse(a.Equals((object)c));
            Assert.IsFalse(a.Equals((object)null));
            Assert.IsFalse(a.Equals((object)1)); // types don't match
        }

        [Test]
        public void TestGetHashCode()
        {
            var a = new BsonArray { 1, 2 };
            var hashCode = a.GetHashCode();
            Assert.AreEqual(hashCode, a.GetHashCode());
        }

        [Test]
        public void TestIndexer()
        {
            var array = new BsonArray { 1 };
            Assert.AreEqual(1, array[0].AsInt32);
            array[0] = 2;
            Assert.AreEqual(2, array[0].AsInt32);
        }

        [Test]
        public void TestIndexerSetNull()
        {
            var array = new BsonArray { 1 };
            Assert.Throws<ArgumentNullException>(() => { array[0] = null; });
        }

        [Test]
        public void TestIndexOf()
        {
            var array = new BsonArray { 1, 2, 3 };
            Assert.AreEqual(0, array.IndexOf(1));
            Assert.AreEqual(1, array.IndexOf(2));
            Assert.AreEqual(2, array.IndexOf(3));
            Assert.AreEqual(-1, array.IndexOf(4));
            Assert.Throws<ArgumentNullException>(() => { array.IndexOf(null); });
        }

        [Test]
        public void TestIndexOfWithStartingPosition()
        {
            var array = new BsonArray { 1, 2, 3 };
            Assert.AreEqual(-1, array.IndexOf(1, 1));
            Assert.AreEqual(1, array.IndexOf(2, 1));
            Assert.AreEqual(2, array.IndexOf(3, 1));
            Assert.AreEqual(-1, array.IndexOf(4, 1));
            Assert.Throws<ArgumentNullException>(() => { array.IndexOf(null, 1); });
        }

        [Test]
        public void TestIndexOfWithStartingPositionAndCount()
        {
            var array = new BsonArray { 1, 2, 3 };
            Assert.AreEqual(-1, array.IndexOf(1, 1, 1));
            Assert.AreEqual(1, array.IndexOf(2, 1, 1));
            Assert.AreEqual(-1, array.IndexOf(3, 1, 1));
            Assert.AreEqual(-1, array.IndexOf(4, 1, 1));
            Assert.Throws<ArgumentNullException>(() => { array.IndexOf(null, 1, 1); });
        }

        [Test]
        public void TestInsert()
        {
            var array = new BsonArray() { 1, 3 };
            array.Insert(1, 2);
            Assert.AreEqual(3, array.Count);
            Assert.AreEqual(1, array[0].AsInt32);
            Assert.AreEqual(2, array[1].AsInt32);
            Assert.AreEqual(3, array[2].AsInt32);
            Assert.Throws<ArgumentNullException>(() => { array.Insert(1, null); });
        }

        [Test]
        public void TestOperatorEquals()
        {
            var lhs = new BsonArray { 1, 2 };
            var rhs = new BsonArray { 1, 2 };
            Assert.AreNotSame(lhs, rhs);
            Assert.IsTrue(lhs == rhs);
        }

        [Test]
        public void TestOperatorEqualsBothNull()
        {
            var lhs = (BsonArray)null;
            var rhs = (BsonArray)null;
            Assert.IsTrue(lhs == rhs);
        }

        [Test]
        public void TestOperatorEqualsLhsNull()
        {
            var lhs = (BsonArray)null;
            var rhs = new BsonArray { 1, 2 };
            Assert.IsFalse(lhs == rhs);
        }

        [Test]
        public void TestOperatorEqualsRhsNull()
        {
            var lhs = new BsonArray { 1, 2 };
            var rhs = (BsonArray)null;
            Assert.IsFalse(lhs == rhs);
        }

        [Test]
        public void TestOperatorNotEquals()
        {
            var lhs = new BsonArray { 1, 2 };
            var rhs = new BsonArray { 1, 2 };
            Assert.AreNotSame(lhs, rhs);
            Assert.IsFalse(lhs != rhs);
        }

        [Test]
        public void TestOperatorNotEqualsBothNull()
        {
            var lhs = (BsonArray)null;
            var rhs = (BsonArray)null;
            Assert.IsFalse(lhs != rhs);
        }

        [Test]
        public void TestOperatorNotEqualsLhsNull()
        {
            var lhs = (BsonArray)null;
            var rhs = new BsonArray { 1, 2 };
            Assert.IsTrue(lhs != rhs);
        }

        [Test]
        public void TestOperatorNotEqualsRhsNull()
        {
            var lhs = new BsonArray { 1, 2 };
            var rhs = (BsonArray)null;
            Assert.IsTrue(lhs != rhs);
        }

        [Test]
        public void TestRawValues()
        {
            var array = new BsonArray { 1, "abc", new BsonDocument("x", 1) };
            var expectedRawValues = new object[] { 1, "abc", null };
#pragma warning disable 618
            Assert.IsTrue(expectedRawValues.SequenceEqual(array.RawValues));
#pragma warning restore
        }

        [Test]
        public void TestRemove()
        {
            var array = new BsonArray { 1, 2, 3 };
            Assert.IsTrue(array.Remove(2));
            Assert.IsFalse(array.Remove(2));
            Assert.AreEqual(2, array.Count);
            Assert.AreEqual(1, array[0].AsInt32);
            Assert.AreEqual(3, array[1].AsInt32);
            Assert.Throws<ArgumentNullException>(() => { array.Remove(null); });
        }

        [Test]
        public void TestRemoveAt()
        {
            var array = new BsonArray { 1, 2, 3 };
            array.RemoveAt(1);
            Assert.AreEqual(2, array.Count);
            Assert.AreEqual(1, array[0].AsInt32);
            Assert.AreEqual(3, array[1].AsInt32);
        }

        [Test]
        public void TestToArray()
        {
            var bsonArray = new BsonArray { 1, 2 };
            var array = bsonArray.ToArray();
            Assert.AreEqual(2, array.Length);
            Assert.AreEqual(1, array[0].AsInt32);
            Assert.AreEqual(2, array[1].AsInt32);
        }

        [Test]
        public void TestToList()
        {
            var bsonArray = new BsonArray { 1, 2 };
            var list = bsonArray.ToList();
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(1, list[0].AsInt32);
            Assert.AreEqual(2, list[1].AsInt32);
        }

        [Test]
        public void TestValues()
        {
            var array = new BsonArray { 1, "abc", new BsonDocument("x", 1) };
            var expectedValues = new BsonValue[] { 1, "abc", new BsonDocument("x", 1) };
            Assert.IsTrue(expectedValues.SequenceEqual(array.Values));
        }
    }
}
