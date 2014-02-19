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
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.IO
{
    [TestFixture]
    public class BsonDocumentWriterTests
    {
        // Empty Array tests
        [Test]
        public void TestOneEmptyArray()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartArray("a");
                writer.WriteEndArray();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : [] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedEmptyArray()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
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
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoEmptyArrays()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
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
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedEmptyArrays()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
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
            Assert.AreEqual(expected, json);
        }

        // Empty Document tests
        [Test]
        public void TestEmptyDocument()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ }";
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneEmptyDocument()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("a");
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : { } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedEmptyDocument()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
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
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoEmptyDocuments()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
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
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedEmptyDocuments()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
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
            Assert.AreEqual(expected, json);
        }

        // Array tests
        [Test]
        public void TestArrayWithOneElement()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartArray("a");
                writer.WriteInt32(1);
                writer.WriteEndArray();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : [1] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestArrayWithTwoElements()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
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
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestArrayWithNestedEmptyArray()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
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
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestArrayWithNestedArrayWithOneElement()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
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
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestArrayWithNestedArrayWithTwoElements()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
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
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestArrayWithTwoNestedArrays()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
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
            Assert.AreEqual(expected, json);
        }

        // Binary tests
        [Test]
        public void TestOneBinary()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteBytes("a", new byte[] { 1 });
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : new BinData(0, 'AQ==') }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedBinary()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteBytes("a", new byte[] { 1, 2 });
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : new BinData(0, 'AQI=') } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoBinaries()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteBytes("a", new byte[] { 1 });
                writer.WriteBytes("b", new byte[] { 2 });
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : new BinData(0, 'AQ=='), 'b' : new BinData(0, 'Ag==') }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedBinaries()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
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
            Assert.AreEqual(expected, json);
        }

        // Boolean tests
        [Test]
        public void TestOneBoolean()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteBoolean("a", true);
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : true }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedBoolean()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteBoolean("a", true);
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : true } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoBooleans()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteBoolean("a", true);
                writer.WriteBoolean("b", false);
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : true, 'b' : false }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedBooleans()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
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
            Assert.AreEqual(expected, json);
        }

        // DateTime tests
        [Test]
        public void TestOneDateTime()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteDateTime("a", 0);
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : ISODate('1970-01-01T00:00:00Z') }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedDateTime()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteDateTime("a", 0);
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : ISODate('1970-01-01T00:00:00Z') } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoDateTimes()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteDateTime("a", 0);
                writer.WriteDateTime("b", 0);
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : ISODate('1970-01-01T00:00:00Z'), 'b' : ISODate('1970-01-01T00:00:00Z') }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedDateTimes()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
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
            Assert.AreEqual(expected, json);
        }

        // Double tests
        [Test]
        public void TestOneDouble()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteDouble("a", 1.5);
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : 1.5 }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedDouble()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteDouble("a", 1.5);
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : 1.5 } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoDoubles()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteDouble("a", 1.5);
                writer.WriteDouble("b", 2.5);
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : 1.5, 'b' : 2.5 }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedDoubles()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
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
            Assert.AreEqual(expected, json);
        }

        // Int32 tests
        [Test]
        public void TestOneInt32()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteInt32("a", 1);
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : 1 }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedInt32()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteInt32("a", 1);
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : 1 } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoInt32s()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteInt32("a", 1);
                writer.WriteInt32("b", 2);
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : 1, 'b' : 2 }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedInt32s()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
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
            Assert.AreEqual(expected, json);
        }

        // Int64 tests
        [Test]
        public void TestOneInt64()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteInt64("a", 1);
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : NumberLong(1) }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedInt64()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteInt64("a", 1);
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : NumberLong(1) } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoInt64s()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteInt64("a", 1);
                writer.WriteInt64("b", 2);
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : NumberLong(1), 'b' : NumberLong(2) }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedInt64s()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
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
            Assert.AreEqual(expected, json);
        }

        // JavaScript tests
        [Test]
        public void TestOneJavaScript()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteJavaScript("a", "x");
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : { '$code' : 'x' } }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedJavaScript()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteJavaScript("a", "x");
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : { '$code' : 'x' } } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoJavaScripts()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteJavaScript("a", "x");
                writer.WriteJavaScript("b", "y");
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : { '$code' : 'x' }, 'b' : { '$code' : 'y' } }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedJavaScripts()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
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
            Assert.AreEqual(expected, json);
        }

        // JavaScriptWithScope tests
        [Test]
        public void TestOneJavaScriptWithScope()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
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
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedJavaScriptWithScope()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
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
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoJavaScriptWithScopes()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
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
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedJavaScriptWithScopes()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
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
            Assert.AreEqual(expected, json);
        }

        // MaxKey tests
        [Test]
        public void TestOneMaxKey()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteMaxKey("a");
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : { '$maxkey' : 1 } }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedMaxKey()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteMaxKey("a");
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : { '$maxkey' : 1 } } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoMaxKeys()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteMaxKey("a");
                writer.WriteMaxKey("b");
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : { '$maxkey' : 1 }, 'b' : { '$maxkey' : 1 } }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedMaxKeys()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteMaxKey("a");
                writer.WriteMaxKey("b");
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : { '$maxkey' : 1 }, 'b' : { '$maxkey' : 1 } } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        // MinKey tests
        [Test]
        public void TestOneMinKey()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteMinKey("a");
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : { '$minkey' : 1 } }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedMinKey()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteMinKey("a");
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : { '$minkey' : 1 } } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoMinKeys()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteMinKey("a");
                writer.WriteMinKey("b");
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : { '$minkey' : 1 }, 'b' : { '$minkey' : 1 } }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedMinKeys()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteMinKey("a");
                writer.WriteMinKey("b");
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : { '$minkey' : 1 }, 'b' : { '$minkey' : 1 } } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        // Null tests
        [Test]
        public void TestOneNull()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteNull("a");
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : null }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedNull()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteNull("a");
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : null } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNulls()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteNull("a");
                writer.WriteNull("b");
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : null, 'b' : null }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedNulls()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
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
            Assert.AreEqual(expected, json);
        }

        // ObjectId tests
        [Test]
        public void TestOneObjectId()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteObjectId("a", ObjectId.Empty);
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : ObjectId('000000000000000000000000') }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedObjectId()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteObjectId("a", ObjectId.Empty);
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : ObjectId('000000000000000000000000') } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoObjectIds()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteObjectId("a", ObjectId.Empty);
                writer.WriteObjectId("b", ObjectId.Empty);
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : ObjectId('000000000000000000000000'), 'b' : ObjectId('000000000000000000000000') }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedObjectIds()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
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
            Assert.AreEqual(expected, json);
        }

        // RegularExpression tests
        [Test]
        public void TestOneRegularExpression()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteRegularExpression("a", new BsonRegularExpression("p", "i"));
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : /p/i }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedRegularExpression()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteRegularExpression("a", new BsonRegularExpression("p", "i"));
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : /p/i } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoRegularExpressions()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteRegularExpression("a", new BsonRegularExpression("p", "i"));
                writer.WriteRegularExpression("b", new BsonRegularExpression("q", "m"));
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : /p/i, 'b' : /q/m }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedRegularExpressions()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
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
            Assert.AreEqual(expected, json);
        }

        // String tests
        [Test]
        public void TestOneString()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteString("a", "x");
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : 'x' }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedString()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteString("a", "x");
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : 'x' } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoStrings()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteString("a", "x");
                writer.WriteString("b", "y");
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : 'x', 'b' : 'y' }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedStrings()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
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
            Assert.AreEqual(expected, json);
        }

        // Symbol tests
        [Test]
        public void TestOneSymbol()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteSymbol("a", "x");
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : { '$symbol' : 'x' } }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedSymbol()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteSymbol("a", "x");
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : { '$symbol' : 'x' } } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoSymbols()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteSymbol("a", "x");
                writer.WriteSymbol("b", "y");
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : { '$symbol' : 'x' }, 'b' : { '$symbol' : 'y' } }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedSymbols()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
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
            Assert.AreEqual(expected, json);
        }

        // Timestamp tests
        [Test]
        public void TestOneTimestamp()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteTimestamp("a", 1);
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : { '$timestamp' : NumberLong(1) } }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedTimestamp()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteTimestamp("a", 1);
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : { '$timestamp' : NumberLong(1) } } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoTimestamps()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteTimestamp("a", 1);
                writer.WriteTimestamp("b", 2);
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : { '$timestamp' : NumberLong(1) }, 'b' : { '$timestamp' : NumberLong(2) } }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedTimestamps()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteTimestamp("a", 1);
                writer.WriteTimestamp("b", 2);
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : { '$timestamp' : NumberLong(1) }, 'b' : { '$timestamp' : NumberLong(2) } } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        // Undefined tests
        [Test]
        public void TestOneUndefined()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteUndefined("a");
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : undefined }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedUndefined()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteStartDocument("nested");
                writer.WriteUndefined("a");
                writer.WriteEndDocument();
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : undefined } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoUndefineds()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
            {
                writer.WriteStartDocument();
                writer.WriteUndefined("a");
                writer.WriteUndefined("b");
                writer.WriteEndDocument();
            }
            var json = document.ToJson();
            var expected = "{ 'a' : undefined, 'b' : undefined }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedUndefineds()
        {
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document))
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
            Assert.AreEqual(expected, json);
        }
    }
}
