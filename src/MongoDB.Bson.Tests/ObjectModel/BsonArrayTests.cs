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
using Xunit;

namespace MongoDB.Bson.Tests
{
    public class BsonArrayTests
    {
        [Fact]
        public void TestAdd()
        {
            var array = new BsonArray();
            var value = BsonValue.Create(1);
            array.Add(value);
            Assert.Equal(1, array.Count);
            Assert.Equal(value, array[0]);
        }

        [Fact]
        public void TestAddNull()
        {
            var array = new BsonArray();
            var value = (BsonValue)null;
            Assert.Throws<ArgumentNullException>(() => { array.Add(value); });
        }

        [Fact]
        public void TestAddRangeBooleanNull()
        {
            var array = new BsonArray();
            var values = (bool[])null;
            Assert.Throws<ArgumentNullException>(() => { array.AddRange(values); });
        }

        [Fact]
        public void TestAddRangeBsonValueNull()
        {
            var array = new BsonArray();
            var values = (BsonValue[])null;
            Assert.Throws<ArgumentNullException>(() => { array.AddRange(values); });
        }

        [Fact]
        public void TestAddRangeDateTimeNull()
        {
            var array = new BsonArray();
            var values = (DateTime[])null;
            Assert.Throws<ArgumentNullException>(() => { array.AddRange(values); });
        }

        [Fact]
        public void TestAddRangeDoubleNull()
        {
            var array = new BsonArray();
            var values = (double[])null;
            Assert.Throws<ArgumentNullException>(() => { array.AddRange(values); });
        }

        [Fact]
        public void TestAddRangeInt32Null()
        {
            var array = new BsonArray();
            var values = (int[])null;
            Assert.Throws<ArgumentNullException>(() => { array.AddRange(values); });
        }

        [Fact]
        public void TestAddRangeInt64Null()
        {
            var array = new BsonArray();
            var values = (long[])null;
            Assert.Throws<ArgumentNullException>(() => { array.AddRange(values); });
        }

        [Fact]
        public void TestAddRangeObjectIdNull()
        {
            var array = new BsonArray();
            var values = (ObjectId[])null;
            Assert.Throws<ArgumentNullException>(() => { array.AddRange(values); });
        }

        [Fact]
        public void TestAddRangeStringNull()
        {
            var array = new BsonArray();
            var values = (string[])null;
            Assert.Throws<ArgumentNullException>(() => { array.AddRange(values); });
        }

        [Fact]
        public void TestAddRangeIEnumerableNull()
        {
            var array = new BsonArray();
            var values = (object[])null;
            Assert.Throws<ArgumentNullException>(() => { array.AddRange(values); });
        }

        [Fact]
        public void TestCapacity()
        {
            var array = new BsonArray(0);
            Assert.Equal(0, array.Capacity);
            array.Capacity = 8;
            Assert.Equal(8, array.Capacity);
        }

        [Fact]
        public void TestClone()
        {
            var array = new BsonArray(4) { 1, 2, new BsonArray(3) { 3, 4 } };
            var clone = (BsonArray)array.Clone();
            Assert.Equal(4, clone.Capacity);
            Assert.Equal(3, clone.Count);
            Assert.Equal(1, clone[0].AsInt32);
            Assert.Equal(2, clone[1].AsInt32);
            Assert.Same(array[2], clone[2]); // not deep cloned
        }

        [Fact]
        public void TestClear()
        {
            var array = new BsonArray { 1, 2 };
            Assert.Equal(2, array.Count);
            array.Clear();
            Assert.Equal(0, array.Count);
        }

        [Fact]
        public void TestContains()
        {
            var array = new BsonArray { 1, 2 };
            Assert.True(array.Contains(1));
            Assert.True(array.Contains(2));
            Assert.False(array.Contains(3));
        }

        [Fact]
        public void TestContainsNull()
        {
            var array = new BsonArray { 1, 2 };
            Assert.False(array.Contains(null));
        }

        [Fact]
        public void TestCompareTo()
        {
            var a = (BsonValue)new BsonArray { 1, 2 };
            var b = (BsonValue)new BsonArray { 1, 2, 3 };
            var c = (BsonValue)new BsonArray { 4 };
            Assert.Equal(1, a.CompareTo(null));
            Assert.Equal(0, a.CompareTo(a));
            Assert.Equal(-1, a.CompareTo(b));
            Assert.Equal(1, b.CompareTo(a));
            Assert.Equal(-1, a.CompareTo(c));
            Assert.Equal(1, c.CompareTo(a));
            Assert.Equal(1, a.CompareTo(1)); // Array > Int32
            Assert.Equal(-1, a.CompareTo(true)); // Array < Boolean
        }

        [Fact]
        public void TestConstructorWithNoArguments()
        {
            var array = new BsonArray();
            Assert.Equal(BsonType.Array, array.BsonType);
            Assert.Equal(0, array.Capacity);
            Assert.Equal(0, array.Count);
            Assert.True(array.IsBsonArray);
            Assert.Equal(false, array.IsReadOnly);
        }

        [Fact]
        public void TestConstructorWithCapacity()
        {
            var array = new BsonArray(4);
            Assert.Equal(BsonType.Array, array.BsonType);
            Assert.Equal(4, array.Capacity);
            Assert.Equal(0, array.Count);
            Assert.True(array.IsBsonArray);
            Assert.Equal(false, array.IsReadOnly);
        }

        [Fact]
        public void TestCopyToBsonValueArray()
        {
            var bsonArray = new BsonArray { 1, 2 };
            var bsonValueArray = new BsonValue[2];
            bsonArray.CopyTo(bsonValueArray, 0);
            Assert.Same(bsonArray[0], bsonValueArray[0]);
            Assert.Same(bsonArray[1], bsonValueArray[1]);
        }

        [Fact]
        public void TestCopyToOjbectArray()
        {
            var bsonArray = new BsonArray { 1, 2 };
            var bsonValueArray = new object[2];
#pragma warning disable 618
            bsonArray.CopyTo(bsonValueArray, 0);
#pragma warning restore
            Assert.Equal(1, bsonValueArray[0]);
            Assert.Equal(2, bsonValueArray[1]);
        }

        [Fact]
        public void TestCreateBooleanArray()
        {
            var values = new bool[] { true, false };
            var array = new BsonArray(values);
            Assert.Equal(2, array.Count);
            Assert.IsType<BsonBoolean>(array[0]);
            Assert.IsType<BsonBoolean>(array[1]);
            Assert.Equal(true, array[0].AsBoolean);
            Assert.Equal(false, array[1].AsBoolean);
        }

        [Fact]
        public void TestCreateBsonValueArray()
        {
            var values = new BsonValue[] { true, 1, 1.5 };
            var array = new BsonArray(values);
            Assert.Equal(3, array.Count);
            Assert.IsType<BsonBoolean>(array[0]);
            Assert.IsType<BsonInt32>(array[1]);
            Assert.IsType<BsonDouble>(array[2]);
            Assert.Equal(true, array[0].AsBoolean);
            Assert.Equal(1, array[1].AsInt32);
            Assert.Equal(1.5, array[2].AsDouble);
        }

        [Fact]
        public void TestCreateDateTimeArray()
        {
            var value1 = DateTime.SpecifyKind(new DateTime(2011, 1, 18), DateTimeKind.Utc);
            var value2 = DateTime.SpecifyKind(new DateTime(2011, 1, 19), DateTimeKind.Utc);
            var values = new DateTime[] { value1, value2 };
            var array = new BsonArray(values);
            Assert.Equal(2, array.Count);
            Assert.IsType<BsonDateTime>(array[0]);
            Assert.IsType<BsonDateTime>(array[1]);
            Assert.Equal(value1, array[0].ToUniversalTime());
            Assert.Equal(value2, array[1].ToUniversalTime());
        }

        [Fact]
        public void TestCreateDoubleArray()
        {
            var values = new double[] { 1.5, 2.5 };
            var array = new BsonArray(values);
            Assert.Equal(2, array.Count);
            Assert.IsType<BsonDouble>(array[0]);
            Assert.IsType<BsonDouble>(array[1]);
            Assert.Equal(1.5, array[0].AsDouble);
            Assert.Equal(2.5, array[1].AsDouble);
        }

        [Fact]
        public void TestCreateInt32Array()
        {
            var values = new int[] { 1, 2 };
            var array = new BsonArray(values);
            Assert.Equal(2, array.Count);
            Assert.IsType<BsonInt32>(array[0]);
            Assert.IsType<BsonInt32>(array[1]);
            Assert.Equal(1, array[0].AsInt32);
            Assert.Equal(2, array[1].AsInt32);
        }

        [Fact]
        public void TestCreateInt64Array()
        {
            var values = new long[] { 1, 2 };
            var array = new BsonArray(values);
            Assert.Equal(2, array.Count);
            Assert.IsType<BsonInt64>(array[0]);
            Assert.IsType<BsonInt64>(array[1]);
            Assert.Equal(1, array[0].AsInt64);
            Assert.Equal(2, array[1].AsInt64);
        }

        [Fact]
        public void TestCreateNull()
        {
            object obj = null;
            Assert.Throws<ArgumentNullException>(() => { BsonArray.Create(obj); });
        }

        [Fact]
        public void TestCreateObjectArray()
        {
            var values = new object[] { true, 1 , 1.5, null }; // null will be mapped to BsonNull.Value
            var array = new BsonArray(values);
            Assert.Equal(4, array.Count);
            Assert.IsType<BsonBoolean>(array[0]);
            Assert.IsType<BsonInt32>(array[1]);
            Assert.IsType<BsonDouble>(array[2]);
            Assert.IsType<BsonNull>(array[3]);
            Assert.Equal(true, array[0].AsBoolean);
            Assert.Equal(1, array[1].AsInt32);
            Assert.Equal(1.5, array[2].AsDouble);
            Assert.Same(BsonNull.Value, array[3]);
        }

        [Fact]
        public void TestCreateObjectIdArray()
        {
            var value1 = ObjectId.GenerateNewId();
            var value2 = ObjectId.GenerateNewId();
            var values = new ObjectId[] { value1, value2 };
            var array = new BsonArray(values);
            Assert.Equal(2, array.Count);
            Assert.IsType<BsonObjectId>(array[0]);
            Assert.IsType<BsonObjectId>(array[1]);
            Assert.Equal(value1, array[0].AsObjectId);
            Assert.Equal(value2, array[1].AsObjectId);
        }

        [Fact]
        public void TestCreateStringArray()
        {
            var values = new string[] { "a", "b", null }; // null will be mapped to BsonNull.Value
            var array = new BsonArray(values);
            Assert.Equal(3, array.Count);
            Assert.IsType<BsonString>(array[0]);
            Assert.IsType<BsonString>(array[1]);
            Assert.IsType<BsonNull>(array[2]);
            Assert.Equal("a", array[0].AsString);
            Assert.Equal("b", array[1].AsString);
            Assert.Same(BsonNull.Value, array[2]);
        }

        [Fact]
        public void TestCreateFromObject()
        {
            var value = (object)new object[] { 1, 1.5, null }; // null will be mapped to BsonNull.Value
            var array = BsonArray.Create(value);
            Assert.Equal(3, array.Count);
            Assert.IsType<BsonInt32>(array[0]);
            Assert.IsType<BsonDouble>(array[1]);
            Assert.IsType<BsonNull>(array[2]);
            Assert.Equal(1, array[0].AsInt32);
            Assert.Equal(1.5, array[1].AsDouble);
            Assert.Same(BsonNull.Value, array[2]);
        }

        [Fact]
        public void TestDeepClone()
        {
            var array = new BsonArray(4) { 1, 2, new BsonArray(3) { 3, 4 } };
            var clone = (BsonArray)array.DeepClone();
            Assert.Equal(4, clone.Capacity);
            Assert.Equal(3, clone.Count);
            Assert.Equal(1, clone[0].AsInt32);
            Assert.Equal(2, clone[1].AsInt32);
            Assert.NotSame(array[2], clone[2]); // deep cloned
            Assert.Equal(array[2], clone[2]);
        }

        [Fact]
        public void TestEquals()
        {
            var a = new BsonArray { 1, 2 };
            var b = new BsonArray { 1, 2 };
            var c = new BsonArray { 3, 4 };
            Assert.True(a.Equals((object)a));
            Assert.True(a.Equals((object)b));
            Assert.False(a.Equals((object)c));
            Assert.False(a.Equals((object)null));
            Assert.False(a.Equals((object)1)); // types don't match
        }

        [Fact]
        public void TestGetHashCode()
        {
            var a = new BsonArray { 1, 2 };
            var hashCode = a.GetHashCode();
            Assert.Equal(hashCode, a.GetHashCode());
        }

        [Fact]
        public void TestIndexer()
        {
            var array = new BsonArray { 1 };
            Assert.Equal(1, array[0].AsInt32);
            array[0] = 2;
            Assert.Equal(2, array[0].AsInt32);
        }

        [Fact]
        public void TestIndexerSetNull()
        {
            var array = new BsonArray { 1 };
            Assert.Throws<ArgumentNullException>(() => { array[0] = null; });
        }

        [Fact]
        public void TestIndexOf()
        {
            var array = new BsonArray { 1, 2, 3 };
            Assert.Equal(0, array.IndexOf(1));
            Assert.Equal(1, array.IndexOf(2));
            Assert.Equal(2, array.IndexOf(3));
            Assert.Equal(-1, array.IndexOf(4));
            Assert.Throws<ArgumentNullException>(() => { array.IndexOf(null); });
        }

        [Fact]
        public void TestIndexOfWithStartingPosition()
        {
            var array = new BsonArray { 1, 2, 3 };
            Assert.Equal(-1, array.IndexOf(1, 1));
            Assert.Equal(1, array.IndexOf(2, 1));
            Assert.Equal(2, array.IndexOf(3, 1));
            Assert.Equal(-1, array.IndexOf(4, 1));
            Assert.Throws<ArgumentNullException>(() => { array.IndexOf(null, 1); });
        }

        [Fact]
        public void TestIndexOfWithStartingPositionAndCount()
        {
            var array = new BsonArray { 1, 2, 3 };
            Assert.Equal(-1, array.IndexOf(1, 1, 1));
            Assert.Equal(1, array.IndexOf(2, 1, 1));
            Assert.Equal(-1, array.IndexOf(3, 1, 1));
            Assert.Equal(-1, array.IndexOf(4, 1, 1));
            Assert.Throws<ArgumentNullException>(() => { array.IndexOf(null, 1, 1); });
        }

        [Fact]
        public void TestInsert()
        {
            var array = new BsonArray() { 1, 3 };
            array.Insert(1, 2);
            Assert.Equal(3, array.Count);
            Assert.Equal(1, array[0].AsInt32);
            Assert.Equal(2, array[1].AsInt32);
            Assert.Equal(3, array[2].AsInt32);
            Assert.Throws<ArgumentNullException>(() => { array.Insert(1, null); });
        }

        [Fact]
        public void TestOperatorEquals()
        {
            var lhs = new BsonArray { 1, 2 };
            var rhs = new BsonArray { 1, 2 };
            Assert.NotSame(lhs, rhs);
            Assert.True(lhs == rhs);
        }

        [Fact]
        public void TestOperatorEqualsBothNull()
        {
            var lhs = (BsonArray)null;
            var rhs = (BsonArray)null;
            Assert.True(lhs == rhs);
        }

        [Fact]
        public void TestOperatorEqualsLhsNull()
        {
            var lhs = (BsonArray)null;
            var rhs = new BsonArray { 1, 2 };
            Assert.False(lhs == rhs);
        }

        [Fact]
        public void TestOperatorEqualsRhsNull()
        {
            var lhs = new BsonArray { 1, 2 };
            var rhs = (BsonArray)null;
            Assert.False(lhs == rhs);
        }

        [Fact]
        public void TestOperatorNotEquals()
        {
            var lhs = new BsonArray { 1, 2 };
            var rhs = new BsonArray { 1, 2 };
            Assert.NotSame(lhs, rhs);
            Assert.False(lhs != rhs);
        }

        [Fact]
        public void TestOperatorNotEqualsBothNull()
        {
            var lhs = (BsonArray)null;
            var rhs = (BsonArray)null;
            Assert.False(lhs != rhs);
        }

        [Fact]
        public void TestOperatorNotEqualsLhsNull()
        {
            var lhs = (BsonArray)null;
            var rhs = new BsonArray { 1, 2 };
            Assert.True(lhs != rhs);
        }

        [Fact]
        public void TestOperatorNotEqualsRhsNull()
        {
            var lhs = new BsonArray { 1, 2 };
            var rhs = (BsonArray)null;
            Assert.True(lhs != rhs);
        }

        [Fact]
        public void TestRawValues()
        {
            var array = new BsonArray { 1, "abc", new BsonDocument("x", 1) };
            var expectedRawValues = new object[] { 1, "abc", null };
#pragma warning disable 618
            Assert.True(expectedRawValues.SequenceEqual(array.RawValues));
#pragma warning restore
        }

        [Fact]
        public void TestRemove()
        {
            var array = new BsonArray { 1, 2, 3 };
            Assert.True(array.Remove(2));
            Assert.False(array.Remove(2));
            Assert.Equal(2, array.Count);
            Assert.Equal(1, array[0].AsInt32);
            Assert.Equal(3, array[1].AsInt32);
            Assert.Throws<ArgumentNullException>(() => { array.Remove(null); });
        }

        [Fact]
        public void TestRemoveAt()
        {
            var array = new BsonArray { 1, 2, 3 };
            array.RemoveAt(1);
            Assert.Equal(2, array.Count);
            Assert.Equal(1, array[0].AsInt32);
            Assert.Equal(3, array[1].AsInt32);
        }

        [Fact]
        public void TestToArray()
        {
            var bsonArray = new BsonArray { 1, 2 };
            var array = bsonArray.ToArray();
            Assert.Equal(2, array.Length);
            Assert.Equal(1, array[0].AsInt32);
            Assert.Equal(2, array[1].AsInt32);
        }

        [Fact]
        public void TestToList()
        {
            var bsonArray = new BsonArray { 1, 2 };
            var list = bsonArray.ToList();
            Assert.Equal(2, list.Count);
            Assert.Equal(1, list[0].AsInt32);
            Assert.Equal(2, list[1].AsInt32);
        }

        [Fact]
        public void TestValues()
        {
            var array = new BsonArray { 1, "abc", new BsonDocument("x", 1) };
            var expectedValues = new BsonValue[] { 1, "abc", new BsonDocument("x", 1) };
            Assert.True(expectedValues.SequenceEqual(array.Values));
        }
    }
}
