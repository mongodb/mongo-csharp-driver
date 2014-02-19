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
using MongoDB.Bson.Serialization.Attributes;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Serialization
{
    [TestFixture]
    public class BsonExtraElementsTestsUsingBsonDocument
    {
#pragma warning disable 649 // never assigned to
        private class C
        {
            public int Id;
            public int A;
            public int B;
            [BsonExtraElements]
            public BsonDocument X;
        }
#pragma warning restore

        [Test]
        public void TestNoExtraElements()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3 }".Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.AreEqual(json, c.ToJson());
        }

        [Test]
        public void TestOneExtraInt32Element()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3, 'C' : 4 }".Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.AreEqual(json, c.ToJson());
        }

        [Test]
        public void TestOneExtraInt32ElementNamedX()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3, 'X' : 4 }".Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.AreEqual(json, c.ToJson());
        }

        [Test]
        public void TestOneExtraStringElement()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3, 'C' : 'xyz' }".Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.AreEqual(json, c.ToJson());
        }

        [Test]
        public void TestOneExtraDocumentElement()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3, 'C' : { 'D' : 4, 'E' : 'xyz' } }".Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.AreEqual(json, c.ToJson());
        }

        [Test]
        public void TestTwoExtraElements()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3, 'C' : 4, 'D' : 'xyz' }".Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.AreEqual(json, c.ToJson());
        }
    }

    [TestFixture]
    public class BsonExtraElementsTestsUsingDictionary
    {
#pragma warning disable 649 // never assigned to
        private class C
        {
            public int Id;
            public int A;
            public int B;
            [BsonExtraElements]
            public IDictionary<string, object> X;
        }
#pragma warning restore

        [Test]
        public void TestNoExtraElements()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3 }".Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.AreEqual(json, c.ToJson());
        }

        [Test]
        public void TestOneExtraInt32Element()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3, 'C' : 4 }".Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.AreEqual(json, c.ToJson());
        }

        [Test]
        public void TestOneExtraInt32ElementNamedX()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3, 'X' : 4 }".Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.AreEqual(json, c.ToJson());
        }

        [Test]
        public void TestOneExtraStringElement()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3, 'C' : 'xyz' }".Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.AreEqual(json, c.ToJson());
        }

        [Test]
        public void TestOneExtraDocumentElement()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3, 'C' : { 'D' : 4, 'E' : 'xyz' } }".Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.AreEqual(json, c.ToJson());
        }

        [Test]
        public void TestTwoExtraElements()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3, 'C' : 4, 'D' : 'xyz' }".Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.AreEqual(json, c.ToJson());
        }

        [Test]
        public void TestExtraElementsOfAllTypes()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3, #X }";
            var extraElements = new string[][]
            {
                new string[] { "XArray", "[1, 2.0]" },
                new string[] { "XBinary", "HexData(2, '1234')" },
                new string[] { "XBoolean", "true" },
                new string[] { "XByteArray", "HexData(0, '1234')" },
                new string[] { "XDateTime", "ISODate('2012-03-16T11:19:00Z')" },
                new string[] { "XDocument", "{ 'a' : 1 }" },
                new string[] { "XDouble", "1.0" },
                new string[] { "XGuidLegacy", "HexData(3, '33221100554477668899aabbccddeeff')" },
                new string[] { "XGuidStandard", "HexData(4, '00112233445566778899aabbccddeeff')" },
                new string[] { "XInt32", "1" },
                new string[] { "XInt64", "NumberLong(1)" },
                new string[] { "XJavaScript", "{ '$code' : 'abc' }" },
                new string[] { "XJavaScriptWithScope", "{ '$code' : 'abc', '$scope' : { 'x' : 1 } }" },
                new string[] { "XMaxKey", "{ '$maxkey' : 1 }" },
                new string[] { "XMinKey", "{ '$minkey' : 1 }" },
                new string[] { "XNull", "null" },
                new string[] { "XObjectId", "ObjectId('00112233445566778899aabb')" },
                new string[] { "XRegularExpression", "/abc/" },
                new string[] { "XString", "'abc'" },
                new string[] { "XSymbol", "{ '$symbol' : 'abc' }" },
                new string[] { "XTimestamp", "{ '$timestamp' : NumberLong(1234) }" },
                new string[] { "XUndefined", "undefined" },
            };
            var extraElementsRepresentation = string.Join(", ", extraElements.Select(e => string.Format("'{0}' : {1}", e[0], e[1])).ToArray());
            json = json.Replace("#X", extraElementsRepresentation).Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            
            // round trip it both ways before checking individual values
            json = c.ToJson();
            c = BsonSerializer.Deserialize<C>(json);

            Assert.IsInstanceOf<List<object>>(c.X["XArray"]);
            Assert.IsInstanceOf<BsonBinaryData>(c.X["XBinary"]);
            Assert.IsInstanceOf<bool>(c.X["XBoolean"]);
            Assert.IsInstanceOf<byte[]>(c.X["XByteArray"]);
            Assert.IsInstanceOf<DateTime>(c.X["XDateTime"]);
            Assert.IsInstanceOf<Dictionary<string, object>>(c.X["XDocument"]);
            Assert.IsInstanceOf<double>(c.X["XDouble"]);
            Assert.IsInstanceOf<Guid>(c.X["XGuidLegacy"]);
            Assert.IsInstanceOf<Guid>(c.X["XGuidStandard"]);
            Assert.IsInstanceOf<int>(c.X["XInt32"]);
            Assert.IsInstanceOf<long>(c.X["XInt64"]);
            Assert.IsInstanceOf<BsonJavaScript>(c.X["XJavaScript"]);
            Assert.IsInstanceOf<BsonJavaScriptWithScope>(c.X["XJavaScriptWithScope"]);
            Assert.IsInstanceOf<BsonMaxKey>(c.X["XMaxKey"]);
            Assert.IsInstanceOf<BsonMinKey>(c.X["XMinKey"]);
            Assert.IsNull(c.X["XNull"]);
            Assert.IsInstanceOf<ObjectId>(c.X["XObjectId"]);
            Assert.IsInstanceOf<BsonRegularExpression>(c.X["XRegularExpression"]);
            Assert.IsInstanceOf<string>(c.X["XString"]);
            Assert.IsInstanceOf<BsonSymbol>(c.X["XSymbol"]);
            Assert.IsInstanceOf<BsonTimestamp>(c.X["XTimestamp"]);
            Assert.IsInstanceOf<BsonUndefined>(c.X["XUndefined"]);

            Assert.AreEqual(22, c.X.Count);
            Assert.IsTrue(new object[] { 1, 2.0 }.SequenceEqual((List<object>)c.X["XArray"]));
#pragma warning disable 618 // OldBinary is obsolete
            Assert.AreEqual(BsonBinarySubType.OldBinary, ((BsonBinaryData)c.X["XBinary"]).SubType);
#pragma warning restore 618
            Assert.IsTrue(new byte[] { 0x12, 0x34 }.SequenceEqual(((BsonBinaryData)c.X["XBinary"]).Bytes));
            Assert.AreEqual(true, c.X["XBoolean"]);
            Assert.IsTrue(new byte[] { 0x12, 0x34 }.SequenceEqual((byte[])c.X["XByteArray"]));
            Assert.AreEqual(new DateTime(2012, 3, 16, 11, 19, 0, DateTimeKind.Utc), c.X["XDateTime"]);
            Assert.AreEqual(1, ((IDictionary<string, object>)c.X["XDocument"]).Count);
            Assert.AreEqual(1, ((IDictionary<string, object>)c.X["XDocument"])["a"]);
            Assert.AreEqual(1.0, c.X["XDouble"]);
            Assert.AreEqual(new Guid("00112233-4455-6677-8899-aabbccddeeff"), c.X["XGuidLegacy"]);
            Assert.AreEqual(new Guid("00112233-4455-6677-8899-aabbccddeeff"), c.X["XGuidStandard"]);
            Assert.AreEqual(1, c.X["XInt32"]);
            Assert.AreEqual(1L, c.X["XInt64"]);
            Assert.AreEqual("abc", ((BsonJavaScript)c.X["XJavaScript"]).Code);
            Assert.AreEqual("abc", ((BsonJavaScriptWithScope)c.X["XJavaScriptWithScope"]).Code);
            Assert.AreEqual(1, ((BsonJavaScriptWithScope)c.X["XJavaScriptWithScope"]).Scope.ElementCount);
            Assert.AreEqual(new BsonInt32(1), ((BsonJavaScriptWithScope)c.X["XJavaScriptWithScope"]).Scope["x"]);
            Assert.AreSame(BsonMaxKey.Value, c.X["XMaxKey"]);
            Assert.AreSame(BsonMinKey.Value, c.X["XMinKey"]);
            Assert.AreEqual(null, c.X["XNull"]);
            Assert.AreEqual(ObjectId.Parse("00112233445566778899aabb"), c.X["XObjectId"]);
            Assert.AreEqual(new BsonRegularExpression("abc"), c.X["XRegularExpression"]);
            Assert.AreEqual("abc", c.X["XString"]);
            Assert.AreSame(BsonSymbolTable.Lookup("abc"), c.X["XSymbol"]);
            Assert.AreEqual(new BsonTimestamp(1234), c.X["XTimestamp"]);
            Assert.AreSame(BsonUndefined.Value, c.X["XUndefined"]);
        }
    }
}
