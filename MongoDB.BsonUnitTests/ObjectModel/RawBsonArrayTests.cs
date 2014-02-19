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
    public class RawBsonArrayTests
    {
        [Test]
        public void TestClone()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var rawBsonArray = rawBsonDocument["a"].AsBsonArray;
                var clone = rawBsonArray.Clone();
                Assert.AreEqual(rawBsonArray, clone);
            }
        }

        [Test]
        public void TestContains()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var rawBsonArray = rawBsonDocument["a"].AsBsonArray;
                Assert.AreEqual(true, rawBsonArray.Contains(1));
                Assert.AreEqual(true, rawBsonArray.Contains(2));
                Assert.AreEqual(false, rawBsonArray.Contains(3));
            }
        }

        [Test]
        public void TestCopyToBsonValueArray()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var rawBsonArray = rawBsonDocument["a"].AsBsonArray;
                var array = new BsonValue[2];
                rawBsonArray.CopyTo(array, 0);
                Assert.AreEqual(1, array[0].AsInt32);
                Assert.AreEqual(2, array[1].AsInt32);
            }
        }

        [Test]
        public void TestCopyToObjectArray()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var rawBsonArray = rawBsonDocument["a"].AsBsonArray;
                var array = new object[2];
#pragma warning disable 618
                rawBsonArray.CopyTo(array, 0);
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
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var count = rawBsonDocument["a"].AsBsonArray.Count;
                Assert.AreEqual(2, count);
            }
        }

        [Test]
        public void TestDeepClone()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var rawBsonArray = rawBsonDocument["a"].AsBsonArray;
                var clone = rawBsonArray.DeepClone();
                Assert.AreEqual(rawBsonArray, clone);
            }
        }

        [Test]
        public void TestIndexer()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var rawBsonArray = rawBsonDocument["a"].AsBsonArray;
                Assert.AreEqual(1, rawBsonArray[0].AsInt32);
                Assert.AreEqual(2, rawBsonArray[1].AsInt32);
                Assert.Throws<ArgumentOutOfRangeException>(() => { var x = rawBsonArray[2]; });
            }
        }

        [Test]
        public void TestIndexOf()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var rawBsonArray = rawBsonDocument["a"].AsBsonArray;
                Assert.AreEqual(0, rawBsonArray.IndexOf(1));
                Assert.AreEqual(1, rawBsonArray.IndexOf(2));
                Assert.AreEqual(-1, rawBsonArray.IndexOf(3));
            }
        }

        [Test]
        public void TestIndexOfWithRange()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2, 3 });
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var rawBsonArray = rawBsonDocument["a"].AsBsonArray;

                Assert.AreEqual(-1, rawBsonArray.IndexOf(1, 0, 0));
                Assert.AreEqual(0, rawBsonArray.IndexOf(1, 0, 1));
                Assert.AreEqual(0, rawBsonArray.IndexOf(1, 0, 2));
                Assert.AreEqual(0, rawBsonArray.IndexOf(1, 0, 3));
                Assert.AreEqual(-1, rawBsonArray.IndexOf(1, 1, 0));
                Assert.AreEqual(-1, rawBsonArray.IndexOf(1, 1, 1));
                Assert.AreEqual(-1, rawBsonArray.IndexOf(1, 1, 2));
                Assert.AreEqual(-1, rawBsonArray.IndexOf(1, 2, 0));
                Assert.AreEqual(-1, rawBsonArray.IndexOf(1, 2, 1));

                Assert.AreEqual(-1, rawBsonArray.IndexOf(2, 0, 0));
                Assert.AreEqual(-1, rawBsonArray.IndexOf(2, 0, 1));
                Assert.AreEqual(1, rawBsonArray.IndexOf(2, 0, 2));
                Assert.AreEqual(1, rawBsonArray.IndexOf(2, 0, 3));
                Assert.AreEqual(-1, rawBsonArray.IndexOf(2, 1, 0));
                Assert.AreEqual(1, rawBsonArray.IndexOf(2, 1, 1));
                Assert.AreEqual(1, rawBsonArray.IndexOf(2, 1, 2));
                Assert.AreEqual(-1, rawBsonArray.IndexOf(2, 2, 0));
                Assert.AreEqual(-1, rawBsonArray.IndexOf(2, 2, 1));

                Assert.AreEqual(-1, rawBsonArray.IndexOf(3, 0, 0));
                Assert.AreEqual(-1, rawBsonArray.IndexOf(3, 0, 1));
                Assert.AreEqual(-1, rawBsonArray.IndexOf(3, 0, 2));
                Assert.AreEqual(2, rawBsonArray.IndexOf(3, 0, 3));
                Assert.AreEqual(-1, rawBsonArray.IndexOf(3, 1, 0));
                Assert.AreEqual(-1, rawBsonArray.IndexOf(3, 1, 1));
                Assert.AreEqual(2, rawBsonArray.IndexOf(3, 1, 2));
                Assert.AreEqual(-1, rawBsonArray.IndexOf(3, 2, 0));
                Assert.AreEqual(2, rawBsonArray.IndexOf(3, 2, 1));
            }
        }

        [Test]
        public void TestIsReadOnly()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2, 3 });
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var rawBsonArray = rawBsonDocument["a"].AsBsonArray;
                Assert.IsTrue(rawBsonArray.IsReadOnly);
            }
        }

        [Test]
        public void TestNestedRawBsonArray()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { new BsonArray { 1, 2 } });
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var rawBsonArray = rawBsonDocument["a"].AsBsonArray;
                var nestedRawBsonArray = rawBsonArray[0].AsBsonArray;
                var values = nestedRawBsonArray.Values.ToArray();
                Assert.AreEqual(2, values.Length);
                Assert.AreEqual(1, values[0].AsInt32);
                Assert.AreEqual(2, values[1].AsInt32);
            }
        }

        [Test]
        public void TestNestedRawBsonDocument()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { new BsonDocument { { "x", 1 }, { "y", 2 } } });
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var rawBsonArray = rawBsonDocument["a"].AsBsonArray;
                var nestedRawBsonDocument = rawBsonArray[0].AsBsonDocument;
                var elements = nestedRawBsonDocument.Elements.ToArray();
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
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
#pragma warning disable 618
                var rawValues = rawBsonDocument["a"].AsBsonArray.RawValues.ToArray();
#pragma warning restore
                Assert.AreEqual(2, rawValues.Length);
                Assert.AreEqual(1, rawValues[0]);
                Assert.AreEqual(2, rawValues[1]);
            }
        }

        [Test]
        public void TestToArray()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var rawBsonArray = rawBsonDocument["a"].AsBsonArray;
                var array = rawBsonArray.ToArray();
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
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var rawBsonArray = rawBsonDocument["a"].AsBsonArray;
                var list = rawBsonArray.ToList();
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
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var rawBsonArray = rawBsonDocument["a"].AsBsonArray;
                Assert.AreEqual("[1, 2]", rawBsonArray.ToString());
            }
        }

        [Test]
        public void TestValues()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var values = rawBsonDocument["a"].AsBsonArray.Values.ToArray();
                Assert.AreEqual(2, values.Length);
                Assert.AreEqual(1, values[0].AsInt32);
                Assert.AreEqual(2, values[1].AsInt32);
            }
        }
    }
}
