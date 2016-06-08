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
using Xunit;

namespace MongoDB.Bson.Tests
{
    public class RawBsonArrayTests
    {
        [Fact]
        public void TestClone()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var rawBsonArray = rawBsonDocument["a"].AsBsonArray;
                var clone = rawBsonArray.Clone();
                Assert.Equal(rawBsonArray, clone);
            }
        }

        [Fact]
        public void TestContains()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var rawBsonArray = rawBsonDocument["a"].AsBsonArray;
                Assert.Equal(true, rawBsonArray.Contains(1));
                Assert.Equal(true, rawBsonArray.Contains(2));
                Assert.Equal(false, rawBsonArray.Contains(3));
            }
        }

        [Fact]
        public void TestCopyToBsonValueArray()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var rawBsonArray = rawBsonDocument["a"].AsBsonArray;
                var array = new BsonValue[2];
                rawBsonArray.CopyTo(array, 0);
                Assert.Equal(1, array[0].AsInt32);
                Assert.Equal(2, array[1].AsInt32);
            }
        }

        [Fact]
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
                Assert.Equal(1, array[0]);
                Assert.Equal(2, array[1]);
            }
        }

        [Fact]
        public void TestCount()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var count = rawBsonDocument["a"].AsBsonArray.Count;
                Assert.Equal(2, count);
            }
        }

        [Fact]
        public void TestDeepClone()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var rawBsonArray = rawBsonDocument["a"].AsBsonArray;
                var clone = rawBsonArray.DeepClone();
                Assert.Equal(rawBsonArray, clone);
            }
        }

        [Fact]
        public void TestIndexer()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var rawBsonArray = rawBsonDocument["a"].AsBsonArray;
                Assert.Equal(1, rawBsonArray[0].AsInt32);
                Assert.Equal(2, rawBsonArray[1].AsInt32);
                Assert.Throws<ArgumentOutOfRangeException>(() => { var x = rawBsonArray[2]; });
            }
        }

        [Fact]
        public void TestIndexOf()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var rawBsonArray = rawBsonDocument["a"].AsBsonArray;
                Assert.Equal(0, rawBsonArray.IndexOf(1));
                Assert.Equal(1, rawBsonArray.IndexOf(2));
                Assert.Equal(-1, rawBsonArray.IndexOf(3));
            }
        }

        [Fact]
        public void TestIndexOfWithRange()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2, 3 });
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var rawBsonArray = rawBsonDocument["a"].AsBsonArray;

                Assert.Equal(-1, rawBsonArray.IndexOf(1, 0, 0));
                Assert.Equal(0, rawBsonArray.IndexOf(1, 0, 1));
                Assert.Equal(0, rawBsonArray.IndexOf(1, 0, 2));
                Assert.Equal(0, rawBsonArray.IndexOf(1, 0, 3));
                Assert.Equal(-1, rawBsonArray.IndexOf(1, 1, 0));
                Assert.Equal(-1, rawBsonArray.IndexOf(1, 1, 1));
                Assert.Equal(-1, rawBsonArray.IndexOf(1, 1, 2));
                Assert.Equal(-1, rawBsonArray.IndexOf(1, 2, 0));
                Assert.Equal(-1, rawBsonArray.IndexOf(1, 2, 1));

                Assert.Equal(-1, rawBsonArray.IndexOf(2, 0, 0));
                Assert.Equal(-1, rawBsonArray.IndexOf(2, 0, 1));
                Assert.Equal(1, rawBsonArray.IndexOf(2, 0, 2));
                Assert.Equal(1, rawBsonArray.IndexOf(2, 0, 3));
                Assert.Equal(-1, rawBsonArray.IndexOf(2, 1, 0));
                Assert.Equal(1, rawBsonArray.IndexOf(2, 1, 1));
                Assert.Equal(1, rawBsonArray.IndexOf(2, 1, 2));
                Assert.Equal(-1, rawBsonArray.IndexOf(2, 2, 0));
                Assert.Equal(-1, rawBsonArray.IndexOf(2, 2, 1));

                Assert.Equal(-1, rawBsonArray.IndexOf(3, 0, 0));
                Assert.Equal(-1, rawBsonArray.IndexOf(3, 0, 1));
                Assert.Equal(-1, rawBsonArray.IndexOf(3, 0, 2));
                Assert.Equal(2, rawBsonArray.IndexOf(3, 0, 3));
                Assert.Equal(-1, rawBsonArray.IndexOf(3, 1, 0));
                Assert.Equal(-1, rawBsonArray.IndexOf(3, 1, 1));
                Assert.Equal(2, rawBsonArray.IndexOf(3, 1, 2));
                Assert.Equal(-1, rawBsonArray.IndexOf(3, 2, 0));
                Assert.Equal(2, rawBsonArray.IndexOf(3, 2, 1));
            }
        }

        [Fact]
        public void TestIsReadOnly()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2, 3 });
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var rawBsonArray = rawBsonDocument["a"].AsBsonArray;
                Assert.True(rawBsonArray.IsReadOnly);
            }
        }

        [Fact]
        public void TestNestedRawBsonArray()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { new BsonArray { 1, 2 } });
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var rawBsonArray = rawBsonDocument["a"].AsBsonArray;
                var nestedRawBsonArray = rawBsonArray[0].AsBsonArray;
                var values = nestedRawBsonArray.Values.ToArray();
                Assert.Equal(2, values.Length);
                Assert.Equal(1, values[0].AsInt32);
                Assert.Equal(2, values[1].AsInt32);
            }
        }

        [Fact]
        public void TestNestedRawBsonDocument()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { new BsonDocument { { "x", 1 }, { "y", 2 } } });
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var rawBsonArray = rawBsonDocument["a"].AsBsonArray;
                var nestedRawBsonDocument = rawBsonArray[0].AsBsonDocument;
                var elements = nestedRawBsonDocument.Elements.ToArray();
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
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
#pragma warning disable 618
                var rawValues = rawBsonDocument["a"].AsBsonArray.RawValues.ToArray();
#pragma warning restore
                Assert.Equal(2, rawValues.Length);
                Assert.Equal(1, rawValues[0]);
                Assert.Equal(2, rawValues[1]);
            }
        }

        [Fact]
        public void TestToArray()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var rawBsonArray = rawBsonDocument["a"].AsBsonArray;
                var array = rawBsonArray.ToArray();
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
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var rawBsonArray = rawBsonDocument["a"].AsBsonArray;
                var list = rawBsonArray.ToList();
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
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var rawBsonArray = rawBsonDocument["a"].AsBsonArray;
                Assert.Equal("[1, 2]", rawBsonArray.ToString());
            }
        }

        [Fact]
        public void TestValues()
        {
            var bsonDocument = new BsonDocument("a", new BsonArray { 1, 2 });
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var values = rawBsonDocument["a"].AsBsonArray.Values.ToArray();
                Assert.Equal(2, values.Length);
                Assert.Equal(1, values[0].AsInt32);
                Assert.Equal(2, values[1].AsInt32);
            }
        }
    }
}
