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
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests
{
    public class RawBsonDocumentTests
    {
        [Fact]
        public void TestClone()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            using (var clone = (IDisposable)rawBsonDocument.Clone())
            {
                Assert.Equal(rawBsonDocument, clone);
            }
        }

        [Fact]
        public void TestContains()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                Assert.Equal(true, rawBsonDocument.Contains("x"));
                Assert.Equal(false, rawBsonDocument.Contains("z"));
            }
        }

        [Fact]
        public void TestContainsValue()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                Assert.Equal(true, rawBsonDocument.ContainsValue(1));
                Assert.Equal(false, rawBsonDocument.ContainsValue(3));
            }
        }

        [Fact]
        public void TestDeepClone()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            using (var clone = (IDisposable)rawBsonDocument.DeepClone())
            {
                Assert.Equal(rawBsonDocument, clone);
            }
        }

        [Fact]
        public void TestElementCount()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var count = rawBsonDocument.ElementCount;
                Assert.Equal(2, count);
            }
        }

        [Fact]
        public void TestElements()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var elements = rawBsonDocument.Elements.ToArray();
                Assert.Equal(2, elements.Length);
                Assert.Equal("x", elements[0].Name);
                Assert.Equal(1, elements[0].Value.AsInt32);
                Assert.Equal("y", elements[1].Name);
                Assert.Equal(2, elements[1].Value.AsInt32);
            }
        }

        [Fact]
        public void TestGetElementByIndex()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var elements = new[] { rawBsonDocument.GetElement(0), rawBsonDocument.GetElement(1) };
                Assert.Equal("x", elements[0].Name);
                Assert.Equal(1, elements[0].Value.AsInt32);
                Assert.Equal("y", elements[1].Name);
                Assert.Equal(2, elements[1].Value.AsInt32);

                Assert.Throws<ArgumentOutOfRangeException>(() => { rawBsonDocument.GetElement(2); });
            }
        }

        [Fact]
        public void TestGetElementByName()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var elements = new[] { rawBsonDocument.GetElement("x"), rawBsonDocument.GetElement("y") };
                Assert.Equal("x", elements[0].Name);
                Assert.Equal(1, elements[0].Value.AsInt32);
                Assert.Equal("y", elements[1].Name);
                Assert.Equal(2, elements[1].Value.AsInt32);

                Assert.Throws<KeyNotFoundException>(() => { rawBsonDocument.GetElement("z"); });
            }
        }

        [Fact]
        public void TestGetHashcode()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                Assert.Equal(bsonDocument.GetHashCode(), rawBsonDocument.GetHashCode());
            }
        }

        [Fact]
        public void TestGetValueByIndex()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var values = new[] { rawBsonDocument.GetValue(0), rawBsonDocument.GetValue(1) };
                Assert.Equal(1, values[0].AsInt32);
                Assert.Equal(2, values[1].AsInt32);

                Assert.Throws<ArgumentOutOfRangeException>(() => { rawBsonDocument.GetValue(2); });
            }
        }

        [Fact]
        public void TestGetValueByName()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var values = new[] { rawBsonDocument.GetValue("x"), rawBsonDocument.GetValue("y") };
                Assert.Equal(1, values[0].AsInt32);
                Assert.Equal(2, values[1].AsInt32);

                Assert.Throws<KeyNotFoundException>(() => { rawBsonDocument.GetValue("z"); });
            }
        }

        [Fact]
        public void TestGetValueByNameWithDefaultValue()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                Assert.Equal(1, rawBsonDocument.GetValue("x", 3).AsInt32);
                Assert.Equal(3, rawBsonDocument.GetValue("z", 3).AsInt32);
            }
        }

        [Fact]
        public void TestNames()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var names = rawBsonDocument.Names.ToArray();
                Assert.Equal(2, names.Length);
                Assert.Equal("x", names[0]);
                Assert.Equal("y", names[1]);
            }
        }

        [Fact]
        public void TestNestedRawBsonArray()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "a", new BsonArray { 1, 2 } } };
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var nestedRawBsonArray = rawBsonDocument["a"].AsBsonArray;
                var nestedValues = nestedRawBsonArray.Values.ToArray();
                Assert.Equal(1, rawBsonDocument["x"].AsInt32);
                Assert.Equal(2, nestedValues.Length);
                Assert.Equal(1, nestedValues[0].AsInt32);
                Assert.Equal(2, nestedValues[1].AsInt32);
            }
        }

        [Fact]
        public void TestNestedRawBsonDocument()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "d", new BsonDocument { { "x", 1 }, { "y", 2 } } } };
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var nestedRawBsonDocument = rawBsonDocument["d"].AsBsonDocument;
                var nestedElements = nestedRawBsonDocument.Elements.ToArray();
                Assert.Equal(1, rawBsonDocument["x"].AsInt32);
                Assert.Equal(2, nestedElements.Length);
                Assert.Equal("x", nestedElements[0].Name);
                Assert.Equal(1, nestedElements[0].Value.AsInt32);
                Assert.Equal("y", nestedElements[1].Name);
                Assert.Equal(2, nestedElements[1].Value.AsInt32);
            }
        }

        [Fact]
        public void TestRawValues()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
#pragma warning disable 618
                var rawValues = rawBsonDocument.RawValues.ToArray();
#pragma warning restore
                Assert.Equal(2, rawValues.Length);
                Assert.Equal(1, rawValues[0]);
                Assert.Equal(2, rawValues[1]);
            }
        }

        [Fact]
        public void TestValues()
        {
            var bsonDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = bsonDocument.ToBson();
            using (var rawBsonDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                var rawValues = rawBsonDocument.Values.ToArray();
                Assert.Equal(2, rawValues.Length);
                Assert.Equal(1, rawValues[0].AsInt32);
                Assert.Equal(2, rawValues[1].AsInt32);
            }
        }
    }
}
