﻿/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
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

        [Fact]
        public void TestNoExtraElements()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3 }".Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.Equal(json, c.ToJson());
        }

        [Fact]
        public void TestOneExtraInt32Element()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3, 'C' : 4 }".Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.Equal(json, c.ToJson());
        }

        [Fact]
        public void TestOneExtraInt32ElementNamedX()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3, 'X' : 4 }".Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.Equal(json, c.ToJson());
        }

        [Fact]
        public void TestOneExtraStringElement()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3, 'C' : 'xyz' }".Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.Equal(json, c.ToJson());
        }

        [Fact]
        public void TestOneExtraDocumentElement()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3, 'C' : { 'D' : 4, 'E' : 'xyz' } }".Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.Equal(json, c.ToJson());
        }

        [Fact]
        public void TestTwoExtraElements()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3, 'C' : 4, 'D' : 'xyz' }".Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.Equal(json, c.ToJson());
        }
    }

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

        [Fact]
        public void TestNoExtraElements()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3 }".Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.Equal(json, c.ToJson());
        }

        [Fact]
        public void TestOneExtraInt32Element()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3, 'C' : 4 }".Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.Equal(json, c.ToJson());
        }

        [Fact]
        public void TestOneExtraInt32ElementNamedX()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3, 'X' : 4 }".Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.Equal(json, c.ToJson());
        }

        [Fact]
        public void TestOneExtraStringElement()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3, 'C' : 'xyz' }".Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.Equal(json, c.ToJson());
        }

        [Fact]
        public void TestOneExtraDocumentElement()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3, 'C' : { 'D' : 4, 'E' : 'xyz' } }".Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.Equal(json, c.ToJson());
        }

        [Fact]
        public void TestTwoExtraElements()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3, 'C' : 4, 'D' : 'xyz' }".Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.Equal(json, c.ToJson());
        }

        [Theory]
        [ParameterAttributeData]
        [ResetGuidModeAfterTest]
        public void TestExtraElementsOfAllTypes(
            [ClassValues(typeof(GuidModeValues))] GuidMode mode)
        {
            mode.Set();

#pragma warning disable 618
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3, #X }";
            var extraElements = new List<string[]>
            {
                new string[] { "XArray", "[1, 2.0]" },
                new string[] { "XBinary", "HexData(2, '1234')" },
                new string[] { "XBoolean", "true" },
                new string[] { "XByteArray", "HexData(0, '1234')" },
                new string[] { "XDateTime", "ISODate('2012-03-16T11:19:00Z')" },
                new string[] { "XDocument", "{ 'a' : 1 }" },
                new string[] { "XDouble", "1.0" },
                new string[] { "XInt32", "1" },
                new string[] { "XInt64", "NumberLong(1)" },
                new string[] { "XJavaScript", "{ '$code' : 'abc' }" },
                new string[] { "XJavaScriptWithScope", "{ '$code' : 'abc', '$scope' : { 'x' : 1 } }" },
                new string[] { "XMaxKey", "MaxKey" },
                new string[] { "XMinKey", "MinKey" },
                new string[] { "XNull", "null" },
                new string[] { "XObjectId", "ObjectId('00112233445566778899aabb')" },
                new string[] { "XRegularExpression", "/abc/" },
                new string[] { "XString", "'abc'" },
                new string[] { "XSymbol", "{ '$symbol' : 'abc' }" },
                new string[] { "XTimestamp", "{ '$timestamp' : NumberLong(1234) }" },
                new string[] { "XUndefined", "undefined" },
            };
            var hasXGuidLegacy = false;
            var hasXGuidStandard = false;
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                switch (BsonDefaults.GuidRepresentation)
                {
                    case GuidRepresentation.CSharpLegacy:
                    case GuidRepresentation.JavaLegacy:
                    case GuidRepresentation.PythonLegacy:
                        extraElements.Add(new string[] { "XGuidLegacy", "HexData(3, '00112233445566778899aabbccddeeff')" });
                        hasXGuidLegacy = true;
                        break;

                    case GuidRepresentation.Standard:
                        extraElements.Add(new string[] { "XGuidStandard", "HexData(4, '00112233445566778899aabbccddeeff')" });
                        hasXGuidStandard = true;
                        break;
                }
            }
            var extraElementsRepresentation = string.Join(", ", extraElements.Select(e => string.Format("'{0}' : {1}", e[0], e[1])).ToArray());
            json = json.Replace("#X", extraElementsRepresentation).Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(new JsonReader(json, new JsonReaderSettings()));

            // round trip it both ways before checking individual values
            json = c.ToJson(new JsonWriterSettings());
            c = BsonSerializer.Deserialize<C>(new JsonReader(json, new JsonReaderSettings()));

            Assert.IsType<List<object>>(c.X["XArray"]);
            Assert.IsType<BsonBinaryData>(c.X["XBinary"]);
            Assert.IsType<bool>(c.X["XBoolean"]);
            Assert.IsType<byte[]>(c.X["XByteArray"]);
            Assert.IsType<DateTime>(c.X["XDateTime"]);
            Assert.IsType<Dictionary<string, object>>(c.X["XDocument"]);
            Assert.IsType<double>(c.X["XDouble"]);
            if (hasXGuidLegacy)
            {
                Assert.IsType<Guid>(c.X["XGuidLegacy"]);
            }
            if (hasXGuidStandard)
            {
                Assert.IsType<Guid>(c.X["XGuidStandard"]);
            }
            Assert.IsType<int>(c.X["XInt32"]);
            Assert.IsType<long>(c.X["XInt64"]);
            Assert.IsType<BsonJavaScript>(c.X["XJavaScript"]);
            Assert.IsType<BsonJavaScriptWithScope>(c.X["XJavaScriptWithScope"]);
            Assert.IsType<BsonMaxKey>(c.X["XMaxKey"]);
            Assert.IsType<BsonMinKey>(c.X["XMinKey"]);
            Assert.Null(c.X["XNull"]);
            Assert.IsType<ObjectId>(c.X["XObjectId"]);
            Assert.IsType<BsonRegularExpression>(c.X["XRegularExpression"]);
            Assert.IsType<string>(c.X["XString"]);
            Assert.IsType<BsonSymbol>(c.X["XSymbol"]);
            Assert.IsType<BsonTimestamp>(c.X["XTimestamp"]);
            Assert.IsType<BsonUndefined>(c.X["XUndefined"]);

            Assert.Equal(extraElements.Count, c.X.Count);
            Assert.True(new object[] { 1, 2.0 }.SequenceEqual((List<object>)c.X["XArray"]));
            Assert.Equal(BsonBinarySubType.OldBinary, ((BsonBinaryData)c.X["XBinary"]).SubType);
            Assert.True(new byte[] { 0x12, 0x34 }.SequenceEqual(((BsonBinaryData)c.X["XBinary"]).Bytes));
            Assert.Equal(true, c.X["XBoolean"]);
            Assert.True(new byte[] { 0x12, 0x34 }.SequenceEqual((byte[])c.X["XByteArray"]));
            Assert.Equal(new DateTime(2012, 3, 16, 11, 19, 0, DateTimeKind.Utc), c.X["XDateTime"]);
            Assert.Equal(1, ((IDictionary<string, object>)c.X["XDocument"]).Count);
            Assert.Equal(1, ((IDictionary<string, object>)c.X["XDocument"])["a"]);
            Assert.Equal(1.0, c.X["XDouble"]);
            if (hasXGuidLegacy)
            {
                var expectedLegacyGuid = Guid.Empty;
                switch (BsonDefaults.GuidRepresentation)
                {
                    case GuidRepresentation.CSharpLegacy: expectedLegacyGuid = new Guid("33221100554477668899aabbccddeeff"); break;
                    case GuidRepresentation.JavaLegacy: expectedLegacyGuid = new Guid("7766554433221100ffeeddccbbaa9988"); break;
                    case GuidRepresentation.PythonLegacy: expectedLegacyGuid = new Guid("00112233445566778899aabbccddeeff"); break;
                }
                Assert.Equal(expectedLegacyGuid, c.X["XGuidLegacy"]);
            }
            if (hasXGuidStandard)
            {
                Assert.Equal(new Guid("00112233-4455-6677-8899-aabbccddeeff"), c.X["XGuidStandard"]);
            }
            Assert.Equal(1, c.X["XInt32"]);
            Assert.Equal(1L, c.X["XInt64"]);
            Assert.Equal("abc", ((BsonJavaScript)c.X["XJavaScript"]).Code);
            Assert.Equal("abc", ((BsonJavaScriptWithScope)c.X["XJavaScriptWithScope"]).Code);
            Assert.Equal(1, ((BsonJavaScriptWithScope)c.X["XJavaScriptWithScope"]).Scope.ElementCount);
            Assert.Equal(new BsonInt32(1), ((BsonJavaScriptWithScope)c.X["XJavaScriptWithScope"]).Scope["x"]);
            Assert.Same(BsonMaxKey.Value, c.X["XMaxKey"]);
            Assert.Same(BsonMinKey.Value, c.X["XMinKey"]);
            Assert.Equal(null, c.X["XNull"]);
            Assert.Equal(ObjectId.Parse("00112233445566778899aabb"), c.X["XObjectId"]);
            Assert.Equal(new BsonRegularExpression("abc"), c.X["XRegularExpression"]);
            Assert.Equal("abc", c.X["XString"]);
            Assert.Same(BsonSymbolTable.Lookup("abc"), c.X["XSymbol"]);
            Assert.Equal(new BsonTimestamp(1234), c.X["XTimestamp"]);
            Assert.Same(BsonUndefined.Value, c.X["XUndefined"]);
#pragma warning restore 618
        }
    }
}
