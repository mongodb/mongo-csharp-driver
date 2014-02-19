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
using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests
{
    [TestFixture]
    public class LazyBsonArrayTests
    {
        [Test]
        public void TestAdd()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                lazyBsonArray.Add(3);
                Assert.AreEqual(3, lazyBsonArray.Count);
                Assert.AreEqual(3, lazyBsonArray[2].AsInt32);
            }
        }

        [Test]
        public void TestAddRangeOfBooleans()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                lazyBsonArray.AddRange(new[] { false, true });
                Assert.AreEqual(4, lazyBsonArray.Count);
                Assert.AreEqual(false, lazyBsonArray[2].AsBoolean);
                Assert.AreEqual(true, lazyBsonArray[3].AsBoolean);
            }
        }

        [Test]
        public void TestAddRangeOfBsonValues()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                lazyBsonArray.AddRange(new BsonValue[] { 3, "abc" });
                Assert.AreEqual(4, lazyBsonArray.Count);
                Assert.AreEqual(3, lazyBsonArray[2].AsInt32);
                Assert.AreEqual("abc", lazyBsonArray[3].AsString);
            }
        }

        [Test]
        public void TestAddRangeOfDateTimes()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                lazyBsonArray.AddRange(new[] { new DateTime(2013, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2013, 1, 2, 0, 0, 0, DateTimeKind.Utc) });
                Assert.AreEqual(4, lazyBsonArray.Count);
                Assert.AreEqual(new DateTime(2013, 1, 1, 0, 0, 0, DateTimeKind.Utc), lazyBsonArray[2].ToUniversalTime());
                Assert.AreEqual(new DateTime(2013, 1, 2, 0, 0, 0, DateTimeKind.Utc), lazyBsonArray[3].ToUniversalTime());
            }
        }

        [Test]
        public void TestAddRangeOfDoubles()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                lazyBsonArray.AddRange(new[] { 3.0, 4.0 });
                Assert.AreEqual(4, lazyBsonArray.Count);
                Assert.AreEqual(3.0, lazyBsonArray[2].AsDouble);
                Assert.AreEqual(4.0, lazyBsonArray[3].AsDouble);
            }
        }

        [Test]
        public void TestAddRangeOfInt32s()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                lazyBsonArray.AddRange(new[] { 3, 4 });
                Assert.AreEqual(4, lazyBsonArray.Count);
                Assert.AreEqual(3, lazyBsonArray[2].AsInt32);
                Assert.AreEqual(4, lazyBsonArray[3].AsInt32);
            }
        }

        [Test]
        public void TestAddRangeOfInt64s()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                lazyBsonArray.AddRange(new[] { 3L, 4L });
                Assert.AreEqual(4, lazyBsonArray.Count);
                Assert.AreEqual(3L, lazyBsonArray[2].AsInt64);
                Assert.AreEqual(4L, lazyBsonArray[3].AsInt64);
            }
        }

        [Test]
        public void TestAddRangeOfObjects()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                lazyBsonArray.AddRange(new object[] { 1, "abc" });
                Assert.AreEqual(4, lazyBsonArray.Count);
                Assert.AreEqual(1, lazyBsonArray[2].AsInt32);
                Assert.AreEqual("abc", lazyBsonArray[3].AsString);
            }
        }

        [Test]
        public void TestAddRangeOfObjectIds()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                var objectIds = new[] { ObjectId.GenerateNewId(), ObjectId.GenerateNewId() };
                lazyBsonArray.AddRange(objectIds);
                Assert.AreEqual(4, lazyBsonArray.Count);
                Assert.AreEqual(objectIds[0], lazyBsonArray[2].AsObjectId);
                Assert.AreEqual(objectIds[1], lazyBsonArray[3].AsObjectId);
            }
        }

        [Test]
        public void TestAddRangeOfStrings()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                lazyBsonArray.AddRange(new[] { "abc", "def" });
                Assert.AreEqual(4, lazyBsonArray.Count);
                Assert.AreEqual("abc", lazyBsonArray[2].AsString);
                Assert.AreEqual("def", lazyBsonArray[3].AsString);
            }
        }

        [Test]
        public void TestCapacity()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                lazyBsonArray.Capacity = 3;
                Assert.AreEqual(3, lazyBsonArray.Capacity);
            }
        }

        [Test]
        public void TestClear()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                lazyBsonArray.Clear();
                Assert.AreEqual(0, lazyBsonArray.Count);
            }
        }

        [Test]
        public void TestClone()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;

                var clone1 = lazyBsonArray.Clone();
                Assert.IsInstanceOf<LazyBsonArray>(clone1);
                Assert.AreEqual(lazyBsonArray, clone1);

                var clone2 = lazyBsonArray.Clone();
                Assert.IsInstanceOf<BsonArray>(clone2);
                Assert.AreEqual(lazyBsonArray, clone2);
            }
        }

        [Test]
        public void TestCompareToBsonArray()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                Assert.AreEqual(0, lazyBsonArray.CompareTo(bsonDocument["a"].AsBsonArray));
            }

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                bsonDocument["a"][0] = 0;
                Assert.AreEqual(1, lazyBsonArray.CompareTo(bsonDocument["a"].AsBsonArray));
            }

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                bsonDocument["a"][0] = 2;
                Assert.AreEqual(-1, lazyBsonArray.CompareTo(bsonDocument["a"].AsBsonArray));
            }
        }

        [Test]
        public void TestCompareToBsonValue()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                Assert.AreEqual(0, lazyBsonArray.CompareTo(bsonDocument["a"]));
            }

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                bsonDocument["a"][0] = 0;
                Assert.AreEqual(1, lazyBsonArray.CompareTo(bsonDocument["a"]));
            }

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                bsonDocument["a"][0] = 2;
                Assert.AreEqual(-1, lazyBsonArray.CompareTo(bsonDocument["a"]));
            }
        }

        [Test]
        public void TestContains()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                Assert.AreEqual(true, lazyBsonArray.Contains(1));
                Assert.AreEqual(true, lazyBsonArray.Contains(2));
                Assert.AreEqual(false, lazyBsonArray.Contains(3));
            }
        }

        [Test]
        public void TestCopyToBsonValueArray()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                var array = new BsonValue[2];
                lazyBsonArray.CopyTo(array, 0);
                Assert.AreEqual(1, array[0].AsInt32);
                Assert.AreEqual(2, array[1].AsInt32);
            }
        }

        [Test]
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
                Assert.AreEqual(1, array[0]);
                Assert.AreEqual(2, array[1]);
            }
        }

        [Test]
        public void TestCount()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var count = lazyBsonDocument["a"].AsBsonArray.Count;
                Assert.AreEqual(2, count);
            }
        }

        [Test]
        public void TestDeepClone()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;

                var clone1 = lazyBsonArray.DeepClone();
                Assert.IsInstanceOf<LazyBsonArray>(clone1);
                Assert.AreEqual(lazyBsonArray, clone1);

                var clone2 = lazyBsonArray.DeepClone();
                Assert.IsInstanceOf<BsonArray>(clone2);
                Assert.AreEqual(lazyBsonArray, clone2);
            }
        }

        [Test]
        public void TestGetHashcode()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                Assert.AreEqual(bsonDocument["a"].GetHashCode(), lazyBsonArray.GetHashCode());
            }
        }

        [Test]
        public void TestIndexer()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                Assert.AreEqual(1, lazyBsonArray[0].AsInt32);
                Assert.AreEqual(2, lazyBsonArray[1].AsInt32);
                Assert.Throws<ArgumentOutOfRangeException>(() => { var x = lazyBsonArray[2]; });
            }

            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                lazyBsonArray[0] = 3;
                Assert.AreEqual(3, lazyBsonArray[0].AsInt32);
                Assert.AreEqual(2, lazyBsonArray[1].AsInt32);
                Assert.Throws<ArgumentOutOfRangeException>(() => { lazyBsonArray[2] = 3; });
            }
        }

        [Test]
        public void TestIndexOf()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                Assert.AreEqual(0, lazyBsonArray.IndexOf(1));
                Assert.AreEqual(1, lazyBsonArray.IndexOf(2));
                Assert.AreEqual(-1, lazyBsonArray.IndexOf(3));
            }
        }

        [Test]
        public void TestIndexOfWithIndex()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2, 3 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;

                Assert.AreEqual(0, lazyBsonArray.IndexOf(1, 0));
                Assert.AreEqual(-1, lazyBsonArray.IndexOf(1, 1));
                Assert.AreEqual(-1, lazyBsonArray.IndexOf(1, 2));

                Assert.AreEqual(1, lazyBsonArray.IndexOf(2, 0));
                Assert.AreEqual(1, lazyBsonArray.IndexOf(2, 1));
                Assert.AreEqual(-1, lazyBsonArray.IndexOf(2, 2));

                Assert.AreEqual(2, lazyBsonArray.IndexOf(3, 0));
                Assert.AreEqual(2, lazyBsonArray.IndexOf(3, 1));
                Assert.AreEqual(2, lazyBsonArray.IndexOf(3, 2));
            }
        }

        [Test]
        public void TestIndexOfWithIndexAndCount()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2, 3 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;

                Assert.AreEqual(-1, lazyBsonArray.IndexOf(1, 0, 0));
                Assert.AreEqual(0, lazyBsonArray.IndexOf(1, 0, 1));
                Assert.AreEqual(0, lazyBsonArray.IndexOf(1, 0, 2));
                Assert.AreEqual(0, lazyBsonArray.IndexOf(1, 0, 3));
                Assert.AreEqual(-1, lazyBsonArray.IndexOf(1, 1, 0));
                Assert.AreEqual(-1, lazyBsonArray.IndexOf(1, 1, 1));
                Assert.AreEqual(-1, lazyBsonArray.IndexOf(1, 1, 2));
                Assert.AreEqual(-1, lazyBsonArray.IndexOf(1, 2, 0));
                Assert.AreEqual(-1, lazyBsonArray.IndexOf(1, 2, 1));

                Assert.AreEqual(-1, lazyBsonArray.IndexOf(2, 0, 0));
                Assert.AreEqual(-1, lazyBsonArray.IndexOf(2, 0, 1));
                Assert.AreEqual(1, lazyBsonArray.IndexOf(2, 0, 2));
                Assert.AreEqual(1, lazyBsonArray.IndexOf(2, 0, 3));
                Assert.AreEqual(-1, lazyBsonArray.IndexOf(2, 1, 0));
                Assert.AreEqual(1, lazyBsonArray.IndexOf(2, 1, 1));
                Assert.AreEqual(1, lazyBsonArray.IndexOf(2, 1, 2));
                Assert.AreEqual(-1, lazyBsonArray.IndexOf(2, 2, 0));
                Assert.AreEqual(-1, lazyBsonArray.IndexOf(2, 2, 1));

                Assert.AreEqual(-1, lazyBsonArray.IndexOf(3, 0, 0));
                Assert.AreEqual(-1, lazyBsonArray.IndexOf(3, 0, 1));
                Assert.AreEqual(-1, lazyBsonArray.IndexOf(3, 0, 2));
                Assert.AreEqual(2, lazyBsonArray.IndexOf(3, 0, 3));
                Assert.AreEqual(-1, lazyBsonArray.IndexOf(3, 1, 0));
                Assert.AreEqual(-1, lazyBsonArray.IndexOf(3, 1, 1));
                Assert.AreEqual(2, lazyBsonArray.IndexOf(3, 1, 2));
                Assert.AreEqual(-1, lazyBsonArray.IndexOf(3, 2, 0));
                Assert.AreEqual(2, lazyBsonArray.IndexOf(3, 2, 1));
            }
        }

        [Test]
        public void TestInsert()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                lazyBsonArray.Insert(0, 3);
                Assert.AreEqual(3, lazyBsonArray[0].AsInt32);
                Assert.AreEqual(1, lazyBsonArray[1].AsInt32);
                Assert.AreEqual(2, lazyBsonArray[2].AsInt32);
            }
        }

        [Test]
        public void TestNestedLazyBsonArray()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { new BsonArray { 1, 2 } });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                var nestedLazyBsonArray = lazyBsonArray[0].AsBsonArray;
                var values = nestedLazyBsonArray.Values.ToArray();
                Assert.AreEqual(2, values.Length);
                Assert.AreEqual(1, values[0].AsInt32);
                Assert.AreEqual(2, values[1].AsInt32);
            }
        }

        [Test]
        public void TestNestedLazyBsonDocument()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { new BsonDocument { { "x", 1 }, { "y", 2 } } });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                var nestedLazyBsonDocument = lazyBsonArray[0].AsBsonDocument;
                var elements = nestedLazyBsonDocument.Elements.ToArray();
                Assert.AreEqual(2, elements.Length);
                Assert.AreEqual("x", elements[0].Name);
                Assert.AreEqual(1, elements[0].Value.AsInt32);
                Assert.AreEqual("y", elements[1].Name);
                Assert.AreEqual(2, elements[1].Value.AsInt32);
            }
        }

        [Test]
        public void TestRawValues()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
#pragma warning disable 618
                var lazyValues = lazyBsonDocument["a"].AsBsonArray.RawValues.ToArray();
#pragma warning restore
                Assert.AreEqual(2, lazyValues.Length);
                Assert.AreEqual(1, lazyValues[0]);
                Assert.AreEqual(2, lazyValues[1]);
            }
        }

        [Test]
        public void TestRemove()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2, 3 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                Assert.IsTrue(lazyBsonArray.Remove(2));
                Assert.IsFalse(lazyBsonArray.Remove(4));
                Assert.AreEqual(2, lazyBsonArray.Count);
                Assert.AreEqual(1, lazyBsonArray[0].AsInt32);
                Assert.AreEqual(3, lazyBsonArray[1].AsInt32);
            }
        }

        [Test]
        public void TestRemoveAt()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2, 3 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                lazyBsonArray.RemoveAt(1);
                Assert.AreEqual(2, lazyBsonArray.Count);
                Assert.AreEqual(1, lazyBsonArray[0].AsInt32);
                Assert.AreEqual(3, lazyBsonArray[1].AsInt32);
            }
        }

        [Test]
        public void TestToArray()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                var array = lazyBsonArray.ToArray();
                Assert.AreEqual(2, array.Length);
                Assert.AreEqual(1, array[0].AsInt32);
                Assert.AreEqual(2, array[1].AsInt32);
            }
        }

        [Test]
        public void TestToList()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                var list = lazyBsonArray.ToList();
                Assert.AreEqual(2, list.Count);
                Assert.AreEqual(1, list[0].AsInt32);
                Assert.AreEqual(2, list[1].AsInt32);
            }
        }

        [Test]
        public void TestToString()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var lazyBsonArray = lazyBsonDocument["a"].AsBsonArray;
                Assert.AreEqual("[1, 2]", lazyBsonArray.ToString());
            }
        }

        [Test]
        public void TestValues()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                var values = lazyBsonDocument["a"].AsBsonArray.Values.ToArray();
                Assert.AreEqual(2, values.Length);
                Assert.AreEqual(1, values[0].AsInt32);
                Assert.AreEqual(2, values[1].AsInt32);
            }
        }

        [Test]
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
            BsonDefaults.MaxDocumentSize = 4 * 1024 * 1024;
            using (var lazyBsonDocument = BsonSerializer.Deserialize<LazyBsonDocument>(bson))
            {
                Assert.AreEqual(2, lazyBsonDocument.ElementCount);
                Assert.AreEqual(noOfArrayFields, lazyBsonDocument["arrayfield"].AsBsonArray.Count);
            }
        }
    }
}
