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

using MongoDB.Bson;
using MongoDB.Bson.IO;
using Xunit;

namespace MongoDB.Bson.Tests.IO
{
    public class BsonDocumentWriterTests
    {
        // Empty Array tests
        [Fact]
        public void TestOneEmptyArray()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartArray("a");
                writer.WriteEndArray();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : [] }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestOneNestedEmptyArray()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteStartArray("a");
                writer.WriteEndArray();
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : [] } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoEmptyArrays()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartArray("a");
                writer.WriteEndArray();
                writer.WriteStartArray("b");
                writer.WriteEndArray();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : [], 'b' : [] }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoNestedEmptyArrays()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteStartArray("a");
                writer.WriteEndArray();
                writer.WriteStartArray("b");
                writer.WriteEndArray();
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : [], 'b' : [] } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        // Empty Document tests
        [Fact]
        public void TestEmptyDocument()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ }";
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestOneEmptyDocument()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("a");
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : { } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestOneNestedEmptyDocument()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteStartDocument("a");
                writer.WriteEndDocument();
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : { } } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoEmptyDocuments()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("a");
                writer.WriteEndDocument();
                writer.WriteStartDocument("b");
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : { }, 'b' : { } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoNestedEmptyDocuments()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteStartDocument("a");
                writer.WriteEndDocument();
                writer.WriteStartDocument("b");
                writer.WriteEndDocument();
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : { }, 'b' : { } } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        // Array tests
        [Fact]
        public void TestArrayWithOneElement()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartArray("a");
                writer.WriteInt32(1);
                writer.WriteEndArray();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : [1] }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestArrayWithTwoElements()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartArray("a");
                writer.WriteInt32(1);
                writer.WriteInt32(2);
                writer.WriteEndArray();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : [1, 2] }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestArrayWithNestedEmptyArray()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartArray("a");
                writer.WriteStartArray();
                writer.WriteEndArray();
                writer.WriteEndArray();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : [[]] }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestArrayWithNestedArrayWithOneElement()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartArray("a");
                writer.WriteStartArray();
                writer.WriteString("a");
                writer.WriteEndArray();
                writer.WriteEndArray();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : [['a']] }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestArrayWithNestedArrayWithTwoElements()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartArray("a");
                writer.WriteStartArray();
                writer.WriteString("a");
                writer.WriteString("b");
                writer.WriteEndArray();
                writer.WriteEndArray();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : [['a', 'b']] }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestArrayWithTwoNestedArrays()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartArray("a");
                writer.WriteStartArray();
                writer.WriteString("a");
                writer.WriteString("b");
                writer.WriteEndArray();
                writer.WriteStartArray();
                writer.WriteString("c");
                writer.WriteStartDocument();
                writer.WriteInt32("d", 9);
                writer.WriteEndDocument();
                writer.WriteEndArray();
                writer.WriteEndArray();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : [['a', 'b'], ['c', { 'd' : 9 }]] }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        // Binary tests
        [Fact]
        public void TestOneBinary()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteBytes("a", new byte[] { 1 });
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : new BinData(0, 'AQ==') }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestOneNestedBinary()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteBytes("a", new byte[] { 1, 2 });
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : new BinData(0, 'AQI=') } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoBinaries()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteBytes("a", new byte[] { 1 });
                writer.WriteBytes("b", new byte[] { 2 });
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : new BinData(0, 'AQ=='), 'b' : new BinData(0, 'Ag==') }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoNestedBinaries()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteBytes("a", new byte[] { 1 });
                writer.WriteBytes("b", new byte[] { 2 });
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : new BinData(0, 'AQ=='), 'b' : new BinData(0, 'Ag==') } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        // Boolean tests
        [Fact]
        public void TestOneBoolean()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteBoolean("a", true);
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : true }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestOneNestedBoolean()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteBoolean("a", true);
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : true } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoBooleans()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteBoolean("a", true);
                writer.WriteBoolean("b", false);
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : true, 'b' : false }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoNestedBooleans()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteBoolean("a", true);
                writer.WriteBoolean("b", false);
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : true, 'b' : false } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        // DateTime tests
        [Fact]
        public void TestOneDateTime()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteDateTime("a", 0);
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : ISODate('1970-01-01T00:00:00Z') }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestOneNestedDateTime()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteDateTime("a", 0);
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : ISODate('1970-01-01T00:00:00Z') } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoDateTimes()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteDateTime("a", 0);
                writer.WriteDateTime("b", 0);
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : ISODate('1970-01-01T00:00:00Z'), 'b' : ISODate('1970-01-01T00:00:00Z') }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoNestedDateTimes()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteDateTime("a", 0);
                writer.WriteDateTime("b", 0);
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : ISODate('1970-01-01T00:00:00Z'), 'b' : ISODate('1970-01-01T00:00:00Z') } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        // Decimal128 tests
        [Fact]
        public void TestOneDecimal128()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteDecimal128("a", (Decimal128)1.5M);
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : NumberDecimal('1.5') }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestOneNestedDecimal128()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteDecimal128("a", (Decimal128)1.5M);
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : NumberDecimal('1.5') } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoDecimal128s()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteDecimal128("a", (Decimal128)1.5M);
                writer.WriteDecimal128("b", (Decimal128)2.5M);
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : NumberDecimal('1.5'), 'b' : NumberDecimal('2.5') }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoNestedDecimal128s()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteDecimal128("a", (Decimal128)1.5M);
                writer.WriteDecimal128("b", (Decimal128)2.5M);
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : NumberDecimal('1.5'), 'b' : NumberDecimal('2.5') } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        // Double tests
        [Fact]
        public void TestOneDouble()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteDouble("a", 1.5);
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : 1.5 }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestOneNestedDouble()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteDouble("a", 1.5);
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : 1.5 } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoDoubles()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteDouble("a", 1.5);
                writer.WriteDouble("b", 2.5);
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : 1.5, 'b' : 2.5 }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoNestedDoubles()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteDouble("a", 1.5);
                writer.WriteDouble("b", 2.5);
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : 1.5, 'b' : 2.5 } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        // Int32 tests
        [Fact]
        public void TestOneInt32()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteInt32("a", 1);
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : 1 }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestOneNestedInt32()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteInt32("a", 1);
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : 1 } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoInt32s()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteInt32("a", 1);
                writer.WriteInt32("b", 2);
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : 1, 'b' : 2 }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoNestedInt32s()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteInt32("a", 1);
                writer.WriteInt32("b", 2);
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : 1, 'b' : 2 } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        // Int64 tests
        [Fact]
        public void TestOneInt64()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteInt64("a", 1);
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : NumberLong(1) }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestOneNestedInt64()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteInt64("a", 1);
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : NumberLong(1) } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoInt64s()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteInt64("a", 1);
                writer.WriteInt64("b", 2);
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : NumberLong(1), 'b' : NumberLong(2) }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoNestedInt64s()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteInt64("a", 1);
                writer.WriteInt64("b", 2);
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : NumberLong(1), 'b' : NumberLong(2) } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        // JavaScript tests
        [Fact]
        public void TestOneJavaScript()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteJavaScript("a", "x");
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : { '$code' : 'x' } }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestOneNestedJavaScript()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteJavaScript("a", "x");
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : { '$code' : 'x' } } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoJavaScripts()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteJavaScript("a", "x");
                writer.WriteJavaScript("b", "y");
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : { '$code' : 'x' }, 'b' : { '$code' : 'y' } }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoNestedJavaScripts()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteJavaScript("a", "x");
                writer.WriteJavaScript("b", "y");
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : { '$code' : 'x' }, 'b' : { '$code' : 'y' } } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        // JavaScriptWithScope tests
        [Fact]
        public void TestOneJavaScriptWithScope()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteJavaScriptWithScope("a", "x");
                writer.WriteStartDocument();
                writer.WriteInt32("x", 1);
                writer.WriteInt32("y", 2);
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : { '$code' : 'x', '$scope' : { 'x' : 1, 'y' : 2 } } }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestOneNestedJavaScriptWithScope()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteJavaScriptWithScope("a", "x");
                writer.WriteStartDocument();
                writer.WriteInt32("x", 1);
                writer.WriteInt32("y", 2);
                writer.WriteEndDocument();
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : { '$code' : 'x', '$scope' : { 'x' : 1, 'y' : 2 } } } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoJavaScriptWithScopes()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteJavaScriptWithScope("a", "x");
                writer.WriteStartDocument();
                writer.WriteInt32("x", 1);
                writer.WriteEndDocument();
                writer.WriteJavaScriptWithScope("b", "y");
                writer.WriteStartDocument();
                writer.WriteInt32("y", 2);
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : { '$code' : 'x', '$scope' : { 'x' : 1 } }, 'b' : { '$code' : 'y', '$scope' : { 'y' : 2 } } }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoNestedJavaScriptWithScopes()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteJavaScriptWithScope("a", "x");
                writer.WriteStartDocument();
                writer.WriteInt32("x", 1);
                writer.WriteEndDocument();
                writer.WriteJavaScriptWithScope("b", "y");
                writer.WriteStartDocument();
                writer.WriteInt32("y", 2);
                writer.WriteEndDocument();
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : { '$code' : 'x', '$scope' : { 'x' : 1 } }, 'b' : { '$code' : 'y', '$scope' : { 'y' : 2 } } } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        // MaxKey tests
        [Fact]
        public void TestOneMaxKey()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteMaxKey("a");
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : MaxKey }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestOneNestedMaxKey()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteMaxKey("a");
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : MaxKey } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoMaxKeys()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteMaxKey("a");
                writer.WriteMaxKey("b");
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : MaxKey, 'b' : MaxKey }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoNestedMaxKeys()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteMaxKey("a");
                writer.WriteMaxKey("b");
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : MaxKey, 'b' : MaxKey } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        // MinKey tests
        [Fact]
        public void TestOneMinKey()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteMinKey("a");
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : MinKey }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestOneNestedMinKey()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteMinKey("a");
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : MinKey } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoMinKeys()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteMinKey("a");
                writer.WriteMinKey("b");
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : MinKey, 'b' : MinKey }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoNestedMinKeys()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteMinKey("a");
                writer.WriteMinKey("b");
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : MinKey, 'b' : MinKey } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        // Null tests
        [Fact]
        public void TestOneNull()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteNull("a");
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : null }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestOneNestedNull()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteNull("a");
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : null } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoNulls()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteNull("a");
                writer.WriteNull("b");
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : null, 'b' : null }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoNestedNulls()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteNull("a");
                writer.WriteNull("b");
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : null, 'b' : null } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        // ObjectId tests
        [Fact]
        public void TestOneObjectId()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteObjectId("a", ObjectId.Empty);
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : ObjectId('000000000000000000000000') }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestOneNestedObjectId()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteObjectId("a", ObjectId.Empty);
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : ObjectId('000000000000000000000000') } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoObjectIds()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteObjectId("a", ObjectId.Empty);
                writer.WriteObjectId("b", ObjectId.Empty);
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : ObjectId('000000000000000000000000'), 'b' : ObjectId('000000000000000000000000') }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoNestedObjectIds()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteObjectId("a", ObjectId.Empty);
                writer.WriteObjectId("b", ObjectId.Empty);
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : ObjectId('000000000000000000000000'), 'b' : ObjectId('000000000000000000000000') } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        // RegularExpression tests
        [Fact]
        public void TestOneRegularExpression()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteRegularExpression("a", new BsonRegularExpression("p", "i"));
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : /p/i }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestOneNestedRegularExpression()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteRegularExpression("a", new BsonRegularExpression("p", "i"));
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : /p/i } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoRegularExpressions()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteRegularExpression("a", new BsonRegularExpression("p", "i"));
                writer.WriteRegularExpression("b", new BsonRegularExpression("q", "m"));
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : /p/i, 'b' : /q/m }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoNestedRegularExpressions()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteRegularExpression("a", new BsonRegularExpression("p", "i"));
                writer.WriteRegularExpression("b", new BsonRegularExpression("q", "m"));
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : /p/i, 'b' : /q/m } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        // String tests
        [Fact]
        public void TestOneString()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteString("a", "x");
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : 'x' }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestOneNestedString()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteString("a", "x");
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : 'x' } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoStrings()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteString("a", "x");
                writer.WriteString("b", "y");
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : 'x', 'b' : 'y' }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoNestedStrings()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteString("a", "x");
                writer.WriteString("b", "y");
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : 'x', 'b' : 'y' } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        // Symbol tests
        [Fact]
        public void TestOneSymbol()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteSymbol("a", "x");
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : { '$symbol' : 'x' } }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestOneNestedSymbol()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteSymbol("a", "x");
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : { '$symbol' : 'x' } } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoSymbols()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteSymbol("a", "x");
                writer.WriteSymbol("b", "y");
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : { '$symbol' : 'x' }, 'b' : { '$symbol' : 'y' } }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoNestedSymbols()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteSymbol("a", "x");
                writer.WriteSymbol("b", "y");
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : { '$symbol' : 'x' }, 'b' : { '$symbol' : 'y' } } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        // Timestamp tests
        [Fact]
        public void TestOneTimestamp()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteTimestamp("a", 1);
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : Timestamp(0, 1) }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestOneNestedTimestamp()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteTimestamp("a", 1);
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : Timestamp(0, 1) } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoTimestamps()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteTimestamp("a", 1);
                writer.WriteTimestamp("b", 2);
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : Timestamp(0, 1), 'b' : Timestamp(0, 2) }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoNestedTimestamps()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteTimestamp("a", 1);
                writer.WriteTimestamp("b", 2);
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : Timestamp(0, 1), 'b' : Timestamp(0, 2) } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        // Undefined tests
        [Fact]
        public void TestOneUndefined()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteUndefined("a");
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : undefined }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestOneNestedUndefined()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteUndefined("a");
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : undefined } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoUndefineds()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteUndefined("a");
                writer.WriteUndefined("b");
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : undefined, 'b' : undefined }".Replace("'", "\""); ;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestTwoNestedUndefineds()
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteUndefined("a");
                writer.WriteUndefined("b");
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : undefined, 'b' : undefined } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }
    }
}
