/* Copyright 2010-2016 MongoDB Inc.
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
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests
{
    public class LazyBsonArrayTests
    {
        [Fact]
        public void TestAdd()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                lazyBsonArray.Add(3);
                Assert.Equal(3, lazyBsonArray.Count);
                Assert.Equal(3, lazyBsonArray[2].AsInt32);
            }
        }

        [Fact]
        public void TestAddRangeOfBooleans()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                lazyBsonArray.AddRange(new[] { false, true });
                Assert.Equal(4, lazyBsonArray.Count);
                Assert.Equal(false, lazyBsonArray[2].AsBoolean);
                Assert.Equal(true, lazyBsonArray[3].AsBoolean);
            }
        }

        [Fact]
        public void TestAddRangeOfBsonValues()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                lazyBsonArray.AddRange(new BsonValue[] { 3, "abc" });
                Assert.Equal(4, lazyBsonArray.Count);
                Assert.Equal(3, lazyBsonArray[2].AsInt32);
                Assert.Equal("abc", lazyBsonArray[3].AsString);
            }
        }

        [Fact]
        public void TestAddRangeOfDateTimes()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                lazyBsonArray.AddRange(new[] { new DateTime(2013, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2013, 1, 2, 0, 0, 0, DateTimeKind.Utc) });
                Assert.Equal(4, lazyBsonArray.Count);
                Assert.Equal(new DateTime(2013, 1, 1, 0, 0, 0, DateTimeKind.Utc), lazyBsonArray[2].ToUniversalTime());
                Assert.Equal(new DateTime(2013, 1, 2, 0, 0, 0, DateTimeKind.Utc), lazyBsonArray[3].ToUniversalTime());
            }
        }

        [Fact]
        public void TestAddRangeOfDoubles()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                lazyBsonArray.AddRange(new[] { 3.0, 4.0 });
                Assert.Equal(4, lazyBsonArray.Count);
                Assert.Equal(3.0, lazyBsonArray[2].AsDouble);
                Assert.Equal(4.0, lazyBsonArray[3].AsDouble);
            }
        }

        [Fact]
        public void TestAddRangeOfInt32s()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                lazyBsonArray.AddRange(new[] { 3, 4 });
                Assert.Equal(4, lazyBsonArray.Count);
                Assert.Equal(3, lazyBsonArray[2].AsInt32);
                Assert.Equal(4, lazyBsonArray[3].AsInt32);
            }
        }

        [Fact]
        public void TestAddRangeOfInt64s()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                lazyBsonArray.AddRange(new[] { 3L, 4L });
                Assert.Equal(4, lazyBsonArray.Count);
                Assert.Equal(3L, lazyBsonArray[2].AsInt64);
                Assert.Equal(4L, lazyBsonArray[3].AsInt64);
            }
        }

        [Fact]
        public void TestAddRangeOfObjects()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                lazyBsonArray.AddRange(new object[] { 1, "abc" });
                Assert.Equal(4, lazyBsonArray.Count);
                Assert.Equal(1, lazyBsonArray[2].AsInt32);
                Assert.Equal("abc", lazyBsonArray[3].AsString);
            }
        }

        [Fact]
        public void TestAddRangeOfObjectIds()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                var objectIds = new[] { ObjectId.GenerateNewId(), ObjectId.GenerateNewId() };
                lazyBsonArray.AddRange(objectIds);
                Assert.Equal(4, lazyBsonArray.Count);
                Assert.Equal(objectIds[0], lazyBsonArray[2].AsObjectId);
                Assert.Equal(objectIds[1], lazyBsonArray[3].AsObjectId);
            }
        }

        [Fact]
        public void TestAddRangeOfStrings()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                lazyBsonArray.AddRange(new[] { "abc", "def" });
                Assert.Equal(4, lazyBsonArray.Count);
                Assert.Equal("abc", lazyBsonArray[2].AsString);
                Assert.Equal("def", lazyBsonArray[3].AsString);
            }
        }

        [Fact]
        public void TestCapacity()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                lazyBsonArray.Capacity = 3;
                Assert.Equal(3, lazyBsonArray.Capacity);
            }
        }

        [Fact]
        public void TestClear()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                lazyBsonArray.Clear();
                Assert.Equal(0, lazyBsonArray.Count);
            }
        }

        [Fact]
        public void TestClone()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;

                var clone1 = lazyBsonArray.Clone();
                Assert.IsType<LazyBsonArray>(clone1);
                Assert.Equal(lazyBsonArray, clone1);

                var clone2 = lazyBsonArray.Clone();
                Assert.IsType<BsonArray>(clone2);
                Assert.StrictEqual(lazyBsonArray, clone2);
            }
        }

        [Fact]
        public void TestCompareToBsonArray()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                Assert.Equal(0, lazyBsonArray.CompareTo(bsonDocument["a"].AsBsonArray));
            }

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                bsonDocument["a"][0] = 0;
                Assert.Equal(1, lazyBsonArray.CompareTo(bsonDocument["a"].AsBsonArray));
            }

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                bsonDocument["a"][0] = 2;
                Assert.Equal(-1, lazyBsonArray.CompareTo(bsonDocument["a"].AsBsonArray));
            }
        }

        [Fact]
        public void TestCompareToBsonValue()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                Assert.Equal(0, lazyBsonArray.CompareTo(bsonDocument["a"]));
            }

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                bsonDocument["a"][0] = 0;
                Assert.Equal(1, lazyBsonArray.CompareTo(bsonDocument["a"]));
            }

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                bsonDocument["a"][0] = 2;
                Assert.Equal(-1, lazyBsonArray.CompareTo(bsonDocument["a"]));
            }
        }

        [Fact]
        public void TestContains()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                Assert.Equal(true, lazyBsonArray.Contains(1));
                Assert.Equal(true, lazyBsonArray.Contains(2));
                Assert.Equal(false, lazyBsonArray.Contains(3));
            }
        }

        [Fact]
        public void TestCopyToBsonValueArray()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                var array = new BsonValue[2];
                lazyBsonArray.CopyTo(array, 0);
                Assert.Equal(1, array[0].AsInt32);
                Assert.Equal(2, array[1].AsInt32);
            }
        }

        [Fact]
        public void TestCopyToObjectArray()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                var array = new object[2];
#pragma warning disable 618
                lazyBsonArray.CopyTo(array, 0);
#pragma warning restore
                Assert.Equal(1, array[0]);
                Assert.Equal(2, array[1]);
            }
        }

        [Fact]
        public void TestCount()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var count = lazyBsonDocument["a"].AsBsonArray.Count;
                Assert.Equal(2, count);
            }
        }

        [Fact]
        public void TestDeepClone()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;

                var clone1 = lazyBsonArray.DeepClone();
                Assert.IsType<LazyBsonArray>(clone1);
                Assert.Equal(lazyBsonArray, clone1);

                var clone2 = lazyBsonArray.DeepClone();
                Assert.IsType<BsonArray>(clone2);
                Assert.StrictEqual(lazyBsonArray, clone2);
            }
        }

        [Fact]
        public void TestGetHashcode()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                Assert.Equal(bsonDocument["a"].GetHashCode(), lazyBsonArray.GetHashCode());
            }
        }

        [Fact]
        public void TestIndexer()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                Assert.Equal(1, lazyBsonArray[0].AsInt32);
                Assert.Equal(2, lazyBsonArray[1].AsInt32);
                Assert.Throws<ArgumentOutOfRangeException>(() => { var x = lazyBsonArray[2]; });
            }

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                lazyBsonArray[0] = 3;
                Assert.Equal(3, lazyBsonArray[0].AsInt32);
                Assert.Equal(2, lazyBsonArray[1].AsInt32);
                Assert.Throws<ArgumentOutOfRangeException>(() => { lazyBsonArray[2] = 3; });
            }
        }

        [Fact]
        public void TestIndexOf()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                Assert.Equal(0, lazyBsonArray.IndexOf(1));
                Assert.Equal(1, lazyBsonArray.IndexOf(2));
                Assert.Equal(-1, lazyBsonArray.IndexOf(3));
            }
        }

        [Fact]
        public void TestIndexOfWithIndex()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2, 3 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;

                Assert.Equal(0, lazyBsonArray.IndexOf(1, 0));
                Assert.Equal(-1, lazyBsonArray.IndexOf(1, 1));
                Assert.Equal(-1, lazyBsonArray.IndexOf(1, 2));

                Assert.Equal(1, lazyBsonArray.IndexOf(2, 0));
                Assert.Equal(1, lazyBsonArray.IndexOf(2, 1));
                Assert.Equal(-1, lazyBsonArray.IndexOf(2, 2));

                Assert.Equal(2, lazyBsonArray.IndexOf(3, 0));
                Assert.Equal(2, lazyBsonArray.IndexOf(3, 1));
                Assert.Equal(2, lazyBsonArray.IndexOf(3, 2));
            }
        }

        [Fact]
        public void TestIndexOfWithIndexAndCount()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2, 3 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;

                Assert.Equal(-1, lazyBsonArray.IndexOf(1, 0, 0));
                Assert.Equal(0, lazyBsonArray.IndexOf(1, 0, 1));
                Assert.Equal(0, lazyBsonArray.IndexOf(1, 0, 2));
                Assert.Equal(0, lazyBsonArray.IndexOf(1, 0, 3));
                Assert.Equal(-1, lazyBsonArray.IndexOf(1, 1, 0));
                Assert.Equal(-1, lazyBsonArray.IndexOf(1, 1, 1));
                Assert.Equal(-1, lazyBsonArray.IndexOf(1, 1, 2));
                Assert.Equal(-1, lazyBsonArray.IndexOf(1, 2, 0));
                Assert.Equal(-1, lazyBsonArray.IndexOf(1, 2, 1));

                Assert.Equal(-1, lazyBsonArray.IndexOf(2, 0, 0));
                Assert.Equal(-1, lazyBsonArray.IndexOf(2, 0, 1));
                Assert.Equal(1, lazyBsonArray.IndexOf(2, 0, 2));
                Assert.Equal(1, lazyBsonArray.IndexOf(2, 0, 3));
                Assert.Equal(-1, lazyBsonArray.IndexOf(2, 1, 0));
                Assert.Equal(1, lazyBsonArray.IndexOf(2, 1, 1));
                Assert.Equal(1, lazyBsonArray.IndexOf(2, 1, 2));
                Assert.Equal(-1, lazyBsonArray.IndexOf(2, 2, 0));
                Assert.Equal(-1, lazyBsonArray.IndexOf(2, 2, 1));

                Assert.Equal(-1, lazyBsonArray.IndexOf(3, 0, 0));
                Assert.Equal(-1, lazyBsonArray.IndexOf(3, 0, 1));
                Assert.Equal(-1, lazyBsonArray.IndexOf(3, 0, 2));
                Assert.Equal(2, lazyBsonArray.IndexOf(3, 0, 3));
                Assert.Equal(-1, lazyBsonArray.IndexOf(3, 1, 0));
                Assert.Equal(-1, lazyBsonArray.IndexOf(3, 1, 1));
                Assert.Equal(2, lazyBsonArray.IndexOf(3, 1, 2));
                Assert.Equal(-1, lazyBsonArray.IndexOf(3, 2, 0));
                Assert.Equal(2, lazyBsonArray.IndexOf(3, 2, 1));
            }
        }

        [Fact]
        public void TestInsert()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                lazyBsonArray.Insert(0, 3);
                Assert.Equal(3, lazyBsonArray[0].AsInt32);
                Assert.Equal(1, lazyBsonArray[1].AsInt32);
                Assert.Equal(2, lazyBsonArray[2].AsInt32);
            }
        }

        [Fact]
        public void TestNestedLazyBsonArray()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { new BsonArray { 1, 2 } });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                var nestedLazyBsonArray = lazyBsonArray[0].AsBsonArray;
                var values = nestedLazyBsonArray.Values.ToArray();
                Assert.Equal(2, values.Length);
                Assert.Equal(1, values[0].AsInt32);
                Assert.Equal(2, values[1].AsInt32);
            }
        }

        [Fact]
        public void TestNestedLazyBsonDocument()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { new BsonDocument { { "x", 1 }, { "y", 2 } } });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                var nestedLazyBsonDocument = lazyBsonArray[0].AsBsonDocument;
                var elements = nestedLazyBsonDocument.Elements.ToArray();
                Assert.Equal(2, elements.Length);
                Assert.Equal("x", elements[0].Name);
                Assert.Equal(1, elements[0].Value.AsInt32);
                Assert.Equal("y", elements[1].Name);
                Assert.Equal(2, elements[1].Value.AsInt32);
            }
        }

        [Fact]
        public void TestRawValues()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
#pragma warning disable 618
                var lazyValues = lazyBsonDocument["a"].AsBsonArray.RawValues.ToArray();
#pragma warning restore
                Assert.Equal(2, lazyValues.Length);
                Assert.Equal(1, lazyValues[0]);
                Assert.Equal(2, lazyValues[1]);
            }
        }

        [Fact]
        public void TestRemove()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2, 3 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                Assert.True(lazyBsonArray.Remove(2));
                Assert.False(lazyBsonArray.Remove(4));
                Assert.Equal(2, lazyBsonArray.Count);
                Assert.Equal(1, lazyBsonArray[0].AsInt32);
                Assert.Equal(3, lazyBsonArray[1].AsInt32);
            }
        }

        [Fact]
        public void TestRemoveAt()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2, 3 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                lazyBsonArray.RemoveAt(1);
                Assert.Equal(2, lazyBsonArray.Count);
                Assert.Equal(1, lazyBsonArray[0].AsInt32);
                Assert.Equal(3, lazyBsonArray[1].AsInt32);
            }
        }

        [Fact]
        public void TestToArray()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                var array = lazyBsonArray.ToArray();
                Assert.Equal(2, array.Length);
                Assert.Equal(1, array[0].AsInt32);
                Assert.Equal(2, array[1].AsInt32);
            }
        }

        [Fact]
        public void TestToList()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                var list = lazyBsonArray.ToList();
                Assert.Equal(2, list.Count);
                Assert.Equal(1, list[0].AsInt32);
                Assert.Equal(2, list[1].AsInt32);
            }
        }

        [Fact]
        public void TestToString()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                Assert.Equal("[1, 2]", lazyBsonArray.ToString());
            }
        }

        [Fact]
        public void TestValues()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var values = lazyBsonDocument["a"].AsBsonArray.Values.ToArray();
                Assert.Equal(2, values.Length);
                Assert.Equal(1, values[0].AsInt32);
                Assert.Equal(2, values[1].AsInt32);
            }
        }

        [Fact]
        public void TestLargeArrayDeserialization()
        {
            var bsonDocument = new BsonDocument { { "stringfield", "A" } };
            var noOfArrayFields = 400000;
            var bsonArray = new BsonArray(noOfArrayFields);
            for (var i = 0; i < noOfArrayFields; i++)
            {
                bsonArray.Add(i * 1.0);
            }
            bsonDocument.Add("arrayfield", bsonArray);
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                Assert.Equal(2, lazyBsonDocument.ElementCount);
                Assert.Equal(noOfArrayFields, lazyBsonDocument["arrayfield"].AsBsonArray.Count);
            }
        }
    }
}
