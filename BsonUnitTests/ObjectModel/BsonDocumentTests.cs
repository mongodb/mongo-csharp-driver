/* Copyright 2010-2012 10gen Inc.
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace MongoDB.BsonUnitTests
{
    [TestFixture]
    public class BsonDocumentTests
    {
        [Test]
        public void TestBenchmarks()
        {
            int iterations;
            DateTime start;
            DateTime end;
            TimeSpan duration;

            iterations = 1;
            start = DateTime.UtcNow;
            for (int i = 0; i < iterations; i++)
            {
                // about 2.06 on my machine
                //var doc = new BsonDocument {
                //    { "a", 1 },
                //    { "b", 2.0 },
                //    { "c", "hello" },
                //    { "d", DateTime.UtcNow },
                //    { "e", true }
                // };
                byte[] value = { 1, 2, 3, 4 };
                MemoryStream stream = new MemoryStream();
                for (int n = 0; n < 100000; n++)
                {
                    stream.Write(value, 0, 4);
                }
            }
            end = DateTime.UtcNow;
            duration = end - start;
            System.Diagnostics.Debug.WriteLine(duration);

            start = DateTime.UtcNow;
            for (int i = 0; i < iterations; i++)
            {
                // about 2.22 on my machine
                //var doc = new BsonDocument {
                //    { "a", BsonValue.Create((object) 1) },
                //    { "b", BsonValue.Create((object) 2.0) },
                //    { "c", BsonValue.Create((object) "hello") },
                //    { "d", BsonValue.Create((object) DateTime.UtcNow) },
                //    { "e", BsonValue.Create((object) true) }
                //};
                byte[] value = { 1, 2, 3, 4 };
                using (var buffer = new BsonBuffer())
                {
                    for (int n = 0; n < 100000; n++)
                    {
                        buffer.WriteBytes(value);
                    }
                }
            }
            end = DateTime.UtcNow;
            duration = end - start;
            System.Diagnostics.Debug.WriteLine(duration);
        }

        [Test]
        public void TestHelloWorldWithBsonWriter()
        {
            MemoryStream stream = new MemoryStream();
            using (BsonWriter bsonWriter = BsonWriter.Create(stream))
            {
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteString("hello", "world");
                bsonWriter.WriteEndDocument();
            }
            byte[] bytes = stream.ToArray();
            AssertAreEqual(@"\x16\x00\x00\x00\x02hello\x00\x06\x00\x00\x00world\x00\x00", bytes);
        }

        [Test]
        public void TestElementAccess()
        {
            var book = new BsonDocument
            {
                { "author", "Ernest Hemingway" },
                { "title", "For Whom the Bell Tolls" },
                { "pages", 123 },
                { "price", 9.95 },
                { "ok", BsonNull.Value }
            };
            Assert.AreEqual("Ernest Hemingway", book["author"].AsString);
            Assert.AreEqual(123, book["pages"].AsInt32);
            Assert.AreEqual(9.95, book["price"].AsDouble, 0.0);
            Assert.AreEqual(false, book["ok"].ToBoolean());

            book["err"] = "";
            Assert.AreEqual(false, book["err"].ToBoolean());
            book["err"] = "Error message.";
            Assert.AreEqual(true, book["err"].ToBoolean());

            book["price"] = (double)book["price"] * 1.1;
            double price = book["price"].AsDouble;
        }

        [Test]
        public void TestHelloWorldWithBsonDocument()
        {
            // this test is from http://bsonspec.org/#/specification
            BsonDocument document = new BsonDocument("hello", "world");
            byte[] bson = document.ToBson();
            AssertAreEqual(@"\x16\x00\x00\x00\x02hello\x00\x06\x00\x00\x00world\x00\x00", bson);
        }

        [Test]
        // this test is from http://bsonspec.org/#/specification
        public void TestBsonAwesomeWithBsonWriter()
        {
            MemoryStream stream = new MemoryStream();
            using (BsonWriter bsonWriter = BsonWriter.Create(stream))
            {
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteStartArray("BSON");
                bsonWriter.WriteString("awesome");
                bsonWriter.WriteDouble(5.05);
                bsonWriter.WriteInt32(1986);
                bsonWriter.WriteEndArray();
                bsonWriter.WriteEndDocument();
            }
            byte[] bytes = stream.ToArray();
            AssertAreEqual(@"1\x00\x00\x00\x04BSON\x00&\x00\x00\x00\x020\x00\x08\x00\x00\x00awesome\x00\x011\x00333333\x14@\x102\x00\xc2\x07\x00\x00\x00\x00", bytes);
        }

        [Test]
        // this test is from http://bsonspec.org/#/specification
        public void TestBsonAwesomeWithBsonDocument()
        {
            BsonDocument document = new BsonDocument("BSON", new BsonArray { "awesome", 5.05, 1986 });
            byte[] bson = document.ToBson();
            AssertAreEqual(@"1\x00\x00\x00\x04BSON\x00&\x00\x00\x00\x020\x00\x08\x00\x00\x00awesome\x00\x011\x00333333\x14@\x102\x00\xc2\x07\x00\x00\x00\x00", bson);
        }

        [Test]
        public void TestMerge()
        {
            var document = new BsonDocument();
            document.Merge(new BsonDocument("x", 1));
            Assert.AreEqual(1, document["x"].AsInt32);
            document.Merge(new BsonDocument("x", 2)); // don't overwriteExistingElements
            Assert.AreEqual(1, document["x"].AsInt32); // has old value
            document.Merge(new BsonDocument("x", 2), true); // overwriteExistingElements
            Assert.AreEqual(2, document["x"].AsInt32); // has new value
        }

        [Test]
        public void TestNullableBoolean()
        {
            var document = new BsonDocument { { "v", true }, { "n", BsonNull.Value }, { "s", "" } };
            Assert.AreEqual(true, (bool?)document["v"]);
            Assert.AreEqual(null, (bool?)document["n"]);
            Assert.AreEqual(null, (bool?)document["x", null]);
            Assert.AreEqual(null, (bool?)document["x", (bool?)null]);
            Assert.AreEqual(null, (bool?)document["x", BsonNull.Value]);
            Assert.AreEqual(true, document["v"].AsNullableBoolean);
            Assert.AreEqual(null, document["n"].AsNullableBoolean);
            Assert.AreEqual(null, document["x", (bool?)null].AsNullableBoolean);
            Assert.AreEqual(null, document["x", BsonNull.Value].AsNullableBoolean);
            Assert.Throws<InvalidCastException>(() => { var v = (bool?)document["s"]; });
            Assert.Throws<InvalidCastException>(() => { var v = document["s"].AsNullableBoolean; });
        }

        [Test]
        public void TestNullableDateTime()
        {
            var utcNow = DateTime.UtcNow;
            var utcNowTruncated = utcNow.AddTicks(-(utcNow.Ticks % 10000));
            var document = new BsonDocument { { "v", utcNow }, { "n", BsonNull.Value }, { "s", "" } };
            Assert.AreEqual(utcNowTruncated, (DateTime?)document["v"]);
            Assert.AreEqual(null, (DateTime?)document["n"]);
            Assert.AreEqual(null, (DateTime?)document["x", null]);
            Assert.AreEqual(null, (DateTime?)document["x", (DateTime?)null]);
            Assert.AreEqual(null, (DateTime?)document["x", BsonNull.Value]);
            Assert.AreEqual(utcNowTruncated, document["v"].AsNullableDateTime);
            Assert.AreEqual(null, document["n"].AsNullableDateTime);
            Assert.AreEqual(null, document["x", (DateTime?)null].AsNullableDateTime);
            Assert.AreEqual(null, document["x", BsonNull.Value].AsNullableDateTime);
            Assert.Throws<InvalidCastException>(() => { var v = (DateTime?)document["s"]; });
            Assert.Throws<InvalidCastException>(() => { var v = document["s"].AsNullableDateTime; });
        }

        [Test]
        public void TestNullableDouble()
        {
            var document = new BsonDocument { { "v", 1.5 }, { "n", BsonNull.Value }, { "s", "" } };
            Assert.AreEqual(1.5, (double?)document["v"]);
            Assert.AreEqual(null, (double?)document["n"]);
            Assert.AreEqual(null, (double?)document["x", null]);
            Assert.AreEqual(null, (double?)document["x", (double?)null]);
            Assert.AreEqual(null, (double?)document["x", BsonNull.Value]);
            Assert.AreEqual(1.5, document["v"].AsNullableDouble);
            Assert.AreEqual(null, document["n"].AsNullableDouble);
            Assert.AreEqual(null, document["x", (double?)null].AsNullableDouble);
            Assert.AreEqual(null, document["x", BsonNull.Value].AsNullableDouble);
            Assert.Throws<InvalidCastException>(() => { var v = (double?)document["s"]; });
            Assert.Throws<InvalidCastException>(() => { var v = document["s"].AsNullableDouble; });
        }

        [Test]
        public void TestNullableGuid()
        {
            var guid = Guid.NewGuid();
            var document = new BsonDocument { { "v", guid }, { "n", BsonNull.Value }, { "s", "" } };
            Assert.AreEqual(guid, (Guid?)document["v"]);
            Assert.AreEqual(null, (Guid?)document["n"]);
            Assert.AreEqual(null, (Guid?)document["x", null]);
            Assert.AreEqual(null, (Guid?)document["x", (Guid?)null]);
            Assert.AreEqual(null, (Guid?)document["x", BsonNull.Value]);
            Assert.AreEqual(guid, document["v"].AsNullableGuid);
            Assert.AreEqual(null, document["n"].AsNullableGuid);
            Assert.AreEqual(null, document["x", (Guid?)null].AsNullableGuid);
            Assert.AreEqual(null, document["x", BsonNull.Value].AsNullableGuid);
            Assert.Throws<InvalidCastException>(() => { var v = (Guid?)document["s"]; });
            Assert.Throws<InvalidCastException>(() => { var v = document["s"].AsNullableGuid; });
        }

        [Test]
        public void TestNullableInt32()
        {
            var document = new BsonDocument { { "v", 1 }, { "n", BsonNull.Value }, { "s", "" } };
            Assert.AreEqual(1, (int?)document["v"]);
            Assert.AreEqual(null, (int?)document["n"]);
            Assert.AreEqual(null, (int?)document["x", null]);
            Assert.AreEqual(null, (int?)document["x", (int?)null]);
            Assert.AreEqual(null, (int?)document["x", BsonNull.Value]);
            Assert.AreEqual(1, document["v"].AsNullableInt32);
            Assert.AreEqual(null, document["n"].AsNullableInt32);
            Assert.AreEqual(null, document["x", (int?)null].AsNullableInt32);
            Assert.AreEqual(null, document["x", BsonNull.Value].AsNullableInt32);
            Assert.Throws<InvalidCastException>(() => { var v = (int?)document["s"]; });
            Assert.Throws<InvalidCastException>(() => { var v = document["s"].AsNullableInt32; });
        }

        [Test]
        public void TestNullableInt64()
        {
            var document = new BsonDocument { { "v", 1L }, { "n", BsonNull.Value }, { "s", "" } };
            Assert.AreEqual(1L, (long?)document["v"]);
            Assert.AreEqual(null, (long?)document["n"]);
            Assert.AreEqual(null, (long?)document["x", null]);
            Assert.AreEqual(null, (long?)document["x", (long?)null]);
            Assert.AreEqual(null, (long?)document["x", BsonNull.Value]);
            Assert.AreEqual(1L, document["v"].AsNullableInt64);
            Assert.AreEqual(null, document["n"].AsNullableInt64);
            Assert.AreEqual(null, document["x", (long?)null].AsNullableInt64);
            Assert.AreEqual(null, document["x", BsonNull.Value].AsNullableInt64);
            Assert.Throws<InvalidCastException>(() => { var v = (long?)document["s"]; });
            Assert.Throws<InvalidCastException>(() => { var v = document["s"].AsNullableInt64; });
        }

        [Test]
        public void TestNullableObjectId()
        {
            var objectId = ObjectId.GenerateNewId();
            var document = new BsonDocument { { "v", objectId }, { "n", BsonNull.Value }, { "s", "" } };
            Assert.AreEqual(objectId, (ObjectId?)document["v"]);
            Assert.AreEqual(null, (ObjectId?)document["n"]);
            Assert.AreEqual(null, (ObjectId?)document["x", null]);
            Assert.AreEqual(null, (ObjectId?)document["x", (ObjectId?)null]);
            Assert.AreEqual(null, (ObjectId?)document["x", BsonNull.Value]);
            Assert.AreEqual(objectId, document["v"].AsNullableObjectId);
            Assert.AreEqual(null, document["n"].AsNullableObjectId);
            Assert.AreEqual(null, document["x", (ObjectId?)null].AsNullableObjectId);
            Assert.AreEqual(null, document["x", BsonNull.Value].AsNullableObjectId);
            Assert.Throws<InvalidCastException>(() => { var v = (ObjectId?)document["s"]; });
            Assert.Throws<InvalidCastException>(() => { var v = document["s"].AsNullableObjectId; });
        }

        [Test]
        public void TestZeroLengthElementName()
        {
            var document = new BsonDocument("", "zero length");
            Assert.AreEqual(0, document.GetElement(0).Name.Length);
        }

        [Test]
        public void TestAddArrayListWithOneEntry()
        {
            var arrayList = new ArrayList { 1 };
            var array = new BsonArray(arrayList);
            var json = array.ToJson();
            var expected = "[1]".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestAddArrayListWithTwoEntries()
        {
            var arrayList = new ArrayList { 1, 2 };
            var array = new BsonArray(arrayList);
            var json = array.ToJson();
            var expected = "[1, 2]".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestAddHashtableWithOneEntry()
        {
            var hashtable = new Hashtable { { "A", 1 } };
            var document = new BsonDocument(hashtable);
            var json = document.ToJson();
            var expected = "{ 'A' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestAddHashtableWithTwoEntries()
        {
            var hashtable = new Hashtable { { "A", 1 }, { "B", 2 } };
            var document = new BsonDocument(hashtable);
            // note: can't test json against expected because the order of the keys in the hash table is not defined
            Assert.AreEqual(2, document.ElementCount);
            Assert.AreEqual(1, document["A"].AsInt32);
            Assert.AreEqual(2, document["B"].AsInt32);
        }

        [Test]
        public void TestAddHashtableWithNestedHashtable()
        {
            var hashtable = new Hashtable
            {
                { "A", 1 },
                { "B", new Hashtable { { "C", 2 }, { "D", 3 } } }
            };
            var document = new BsonDocument(hashtable);
            // note: can't test json against expected because the order of the keys in the hash table is not defined
            Assert.AreEqual(2, document.ElementCount);
            Assert.AreEqual(1, document["A"].AsInt32);
            Assert.AreEqual(2, document["B"].AsBsonDocument["C"].AsInt32);
            Assert.AreEqual(3, document["B"].AsBsonDocument["D"].AsInt32);
        }

        [Test]
        public void TestParse()
        {
            var json = "{ a : 1, b : 'abc' }";
            var document = BsonDocument.Parse(json);
        }

        [Test]
        public void TestToDictionaryEmpty()
        {
            var document = new BsonDocument();
            var dictionary = document.ToDictionary();
            Assert.AreEqual(0, dictionary.Count);
        }

        [Test]
        public void TestToDictionaryNestedArray()
        {
            var document = new BsonDocument
            {
                { "x", 1 },
                { "array", new BsonArray { 1, "abc" } }
            };
            var dictionary = document.ToDictionary();
            Assert.AreEqual(2, dictionary.Count);
            Assert.IsInstanceOf<int>(dictionary["x"]);
            Assert.AreEqual(1, dictionary["x"]);
            Assert.IsInstanceOf<object[]>(dictionary["array"]);
            var nested = (object[])dictionary["array"];
            Assert.IsInstanceOf<int>(nested[0]);
            Assert.IsInstanceOf<string>(nested[1]);
            Assert.AreEqual(1, nested[0]);
            Assert.AreEqual("abc", nested[1]);
        }

        [Test]
        public void TestToDictionaryNestedDocument()
        {
            var document = new BsonDocument
            {
                { "x", 1 },
                { "nested", new BsonDocument { { "a", 1 }, { "b", 2 } } }
            };
            var dictionary = document.ToDictionary();
            Assert.AreEqual(2, dictionary.Count);
            Assert.IsInstanceOf<int>(dictionary["x"]);
            Assert.AreEqual(1, dictionary["x"]);
            Assert.IsInstanceOf<Dictionary<string, object>>(dictionary["nested"]);
            var nested = (Dictionary<string, object>)dictionary["nested"];
            Assert.IsInstanceOf<int>(nested["a"]);
            Assert.IsInstanceOf<int>(nested["b"]);
            Assert.AreEqual(1, nested["a"]);
            Assert.AreEqual(2, nested["b"]);
        }

        [Test]
        public void TestToDictionaryOneBinary()
        {
            var document = new BsonDocument("x", new BsonBinaryData(new byte[] { 1, 2, 3 }, BsonBinarySubType.Binary));
            var dictionary = document.ToDictionary();
            Assert.AreEqual(1, dictionary.Count);
            Assert.IsInstanceOf<byte[]>(dictionary["x"]);
            Assert.IsTrue(new byte[] { 1, 2, 3 }.SequenceEqual((byte[])dictionary["x"]));
        }

        [Test]
        public void TestToDictionaryOneBoolean()
        {
            var document = new BsonDocument("x", true);
            var dictionary = document.ToDictionary();
            Assert.AreEqual(1, dictionary.Count);
            Assert.IsInstanceOf<bool>(dictionary["x"]);
            Assert.AreEqual(true, dictionary["x"]);
        }

        [Test]
        public void TestToDictionaryOneDateTime()
        {
            var utcNow = DateTime.UtcNow;
            var utcNowTruncated = utcNow.AddTicks(-(utcNow.Ticks % 10000));
            var document = new BsonDocument("x", utcNow);
            var dictionary = document.ToDictionary();
            Assert.AreEqual(1, dictionary.Count);
            Assert.IsInstanceOf<DateTime>(dictionary["x"]);
            Assert.AreEqual(DateTimeKind.Utc, ((DateTime)dictionary["x"]).Kind);
            Assert.AreEqual(utcNowTruncated, dictionary["x"]);
        }

        [Test]
        public void TestToDictionaryOneDouble()
        {
            var document = new BsonDocument("x", 1.0);
            var dictionary = document.ToDictionary();
            Assert.AreEqual(1, dictionary.Count);
            Assert.IsInstanceOf<double>(dictionary["x"]);
            Assert.AreEqual(1.0, dictionary["x"]);
        }

        [Test]
        public void TestToDictionaryOneGuidLegacy()
        {
            var guid = Guid.NewGuid();
            var document = new BsonDocument("x", new BsonBinaryData(guid, GuidRepresentation.CSharpLegacy));
            var dictionary = document.ToDictionary();
            Assert.AreEqual(1, dictionary.Count);
            Assert.IsInstanceOf<Guid>(dictionary["x"]);
            Assert.AreEqual(guid, dictionary["x"]);
        }

        [Test]
        public void TestToDictionaryOneGuidStandard()
        {
            var guid = Guid.NewGuid();
            var document = new BsonDocument("x", new BsonBinaryData(guid, GuidRepresentation.Standard));
            var dictionary = document.ToDictionary();
            Assert.AreEqual(1, dictionary.Count);
            Assert.IsInstanceOf<Guid>(dictionary["x"]);
            Assert.AreEqual(guid, dictionary["x"]);
        }

        [Test]
        public void TestToDictionaryOneInt32()
        {
            var document = new BsonDocument("x", 1);
            var dictionary = document.ToDictionary();
            Assert.AreEqual(1, dictionary.Count);
            Assert.IsInstanceOf<int>(dictionary["x"]);
            Assert.AreEqual(1, dictionary["x"]);
        }

        [Test]
        public void TestToDictionaryOneInt64()
        {
            var document = new BsonDocument("x", 1L);
            var dictionary = document.ToDictionary();
            Assert.AreEqual(1, dictionary.Count);
            Assert.IsInstanceOf<long>(dictionary["x"]);
            Assert.AreEqual(1L, dictionary["x"]);
        }

        [Test]
        public void TestToDictionaryOneObjectId()
        {
            var objectId = ObjectId.GenerateNewId();
            var hashtable = new BsonDocument("x", objectId);
            var dictionary = hashtable.ToDictionary();
            Assert.AreEqual(1, dictionary.Count);
            Assert.IsInstanceOf<ObjectId>(dictionary["x"]);
            Assert.AreEqual(objectId, dictionary["x"]);
        }

        [Test]
        public void TestToDictionaryOneString()
        {
            var document = new BsonDocument("x", "abc");
            var dictionary = document.ToDictionary();
            Assert.AreEqual(1, dictionary.Count);
            Assert.IsInstanceOf<string>(dictionary["x"]);
            Assert.AreEqual("abc", dictionary["x"]);
        }

        [Test]
        public void TestToDictionaryUnsupportedTypes()
        {
            var document = new BsonDocument
            {
                { "JavaScript", new BsonJavaScript("x = 1") },
                { "JavaScriptWithScope", new BsonJavaScriptWithScope("x = y", new BsonDocument("y", 1)) },
                { "MaxKey", BsonMaxKey.Value },
                { "MinKey", BsonMinKey.Value },
                { "Null", BsonNull.Value },
                { "RegularExpression", new BsonRegularExpression("abc") },
                { "Symbol", BsonSymbol.Create("name") },
                { "Timestamp", new BsonTimestamp(123L) },
                { "Undefined", BsonUndefined.Value },
            };
            var dictionary = document.ToDictionary();
            Assert.AreEqual(9, dictionary.Count);
            Assert.IsNull(dictionary["JavaScript"]);
            Assert.IsNull(dictionary["JavaScriptWithScope"]);
            Assert.IsNull(dictionary["MaxKey"]);
            Assert.IsNull(dictionary["MinKey"]);
            Assert.IsNull(dictionary["Null"]);
            Assert.IsNull(dictionary["RegularExpression"]);
            Assert.IsNull(dictionary["Symbol"]);
            Assert.IsNull(dictionary["Timestamp"]);
            Assert.IsNull(dictionary["Undefined"]);
        }

        [Test]
        public void TestToHashtableEmpty()
        {
            var document = new BsonDocument();
            var hashtable = document.ToHashtable();
            Assert.AreEqual(0, hashtable.Count);
        }

        [Test]
        public void TestToHashtableNestedArray()
        {
            var document = new BsonDocument
            {
                { "x", 1 },
                { "array", new BsonArray { 1, "abc" } }
            };
            var hashtable = document.ToHashtable();
            Assert.AreEqual(2, hashtable.Count);
            Assert.IsInstanceOf<int>(hashtable["x"]);
            Assert.AreEqual(1, hashtable["x"]);
            Assert.IsInstanceOf<object[]>(hashtable["array"]);
            var nested = (object[])hashtable["array"];
            Assert.IsInstanceOf<int>(nested[0]);
            Assert.IsInstanceOf<string>(nested[1]);
            Assert.AreEqual(1, nested[0]);
            Assert.AreEqual("abc", nested[1]);
        }

        [Test]
        public void TestToHashtableNestedDocument()
        {
            var document = new BsonDocument
            {
                { "x", 1 },
                { "nested", new BsonDocument { { "a", 1 }, { "b", 2 } } }
            };
            var hashtable = document.ToHashtable();
            Assert.AreEqual(2, hashtable.Count);
            Assert.IsInstanceOf<int>(hashtable["x"]);
            Assert.AreEqual(1, hashtable["x"]);
            Assert.IsInstanceOf<Hashtable>(hashtable["nested"]);
            var nested = (Hashtable)hashtable["nested"];
            Assert.IsInstanceOf<int>(nested["a"]);
            Assert.IsInstanceOf<int>(nested["b"]);
            Assert.AreEqual(1, nested["a"]);
            Assert.AreEqual(2, nested["b"]);
        }

        [Test]
        public void TestToHashtableOneBinary()
        {
            var document = new BsonDocument("x", new BsonBinaryData(new byte[] { 1, 2, 3 }, BsonBinarySubType.Binary));
            var hashtable = document.ToHashtable();
            Assert.AreEqual(1, hashtable.Count);
            Assert.IsInstanceOf<byte[]>(hashtable["x"]);
            Assert.IsTrue(new byte[] { 1, 2, 3 }.SequenceEqual((byte[])hashtable["x"]));
        }

        [Test]
        public void TestToHashtableOneBoolean()
        {
            var document = new BsonDocument("x", true);
            var dictionary = document.ToHashtable();
            Assert.AreEqual(1, dictionary.Count);
            Assert.IsInstanceOf<bool>(dictionary["x"]);
            Assert.AreEqual(true, dictionary["x"]);
        }

        [Test]
        public void TestToHashtableOneDateTime()
        {
            var utcNow = DateTime.UtcNow;
            var utcNowTruncated = utcNow.AddTicks(-(utcNow.Ticks % 10000));
            var hashtable = new BsonDocument("x", utcNow);
            var dictionary = hashtable.ToHashtable();
            Assert.AreEqual(1, dictionary.Count);
            Assert.IsInstanceOf<DateTime>(dictionary["x"]);
            Assert.AreEqual(DateTimeKind.Utc, ((DateTime)dictionary["x"]).Kind);
            Assert.AreEqual(utcNowTruncated, dictionary["x"]);
        }

        [Test]
        public void TestToHashtableOneDouble()
        {
            var document = new BsonDocument("x", 1.0);
            var hashtable = document.ToHashtable();
            Assert.AreEqual(1, hashtable.Count);
            Assert.IsInstanceOf<double>(hashtable["x"]);
            Assert.AreEqual(1.0, hashtable["x"]);
        }

        [Test]
        public void TestToHashtableOneGuidLegacy()
        {
            var guid = Guid.NewGuid();
            var hashtable = new BsonDocument("x", new BsonBinaryData(guid, GuidRepresentation.CSharpLegacy));
            var dictionary = hashtable.ToHashtable();
            Assert.AreEqual(1, dictionary.Count);
            Assert.IsInstanceOf<Guid>(dictionary["x"]);
            Assert.AreEqual(guid, dictionary["x"]);
        }

        [Test]
        public void TestToHashtableOneGuidStandard()
        {
            var guid = Guid.NewGuid();
            var hashtable = new BsonDocument("x", new BsonBinaryData(guid, GuidRepresentation.Standard));
            var dictionary = hashtable.ToHashtable();
            Assert.AreEqual(1, dictionary.Count);
            Assert.IsInstanceOf<Guid>(dictionary["x"]);
            Assert.AreEqual(guid, dictionary["x"]);
        }

        [Test]
        public void TestToHashtableOneInt32()
        {
            var document = new BsonDocument("x", 1);
            var hashtable = document.ToHashtable();
            Assert.AreEqual(1, hashtable.Count);
            Assert.IsInstanceOf<int>(hashtable["x"]);
            Assert.AreEqual(1, hashtable["x"]);
        }

        [Test]
        public void TestToHashtableOneInt64()
        {
            var document = new BsonDocument("x", 1L);
            var hashtable = document.ToHashtable();
            Assert.AreEqual(1, hashtable.Count);
            Assert.IsInstanceOf<long>(hashtable["x"]);
            Assert.AreEqual(1L, hashtable["x"]);
        }

        [Test]
        public void TestToHashtableOneObjectId()
        {
            var objectId = ObjectId.GenerateNewId();
            var hashtable = new BsonDocument("x", objectId);
            var dictionary = hashtable.ToHashtable();
            Assert.AreEqual(1, dictionary.Count);
            Assert.IsInstanceOf<ObjectId>(dictionary["x"]);
            Assert.AreEqual(objectId, dictionary["x"]);
        }

        [Test]
        public void TestToHashtableOneString()
        {
            var document = new BsonDocument("x", "abc");
            var hashtable = document.ToHashtable();
            Assert.AreEqual(1, hashtable.Count);
            Assert.IsInstanceOf<string>(hashtable["x"]);
            Assert.AreEqual("abc", hashtable["x"]);
        }

        [Test]
        public void TestToHashtableUnsupportedTypes()
        {
            var document = new BsonDocument
            {
                { "JavaScript", new BsonJavaScript("x = 1") },
                { "JavaScriptWithScope", new BsonJavaScriptWithScope("x = y", new BsonDocument("y", 1)) },
                { "MaxKey", BsonMaxKey.Value },
                { "MinKey", BsonMinKey.Value },
                { "Null", BsonNull.Value },
                { "RegularExpression", new BsonRegularExpression("abc") },
                { "Symbol", BsonSymbol.Create("name") },
                { "Timestamp", new BsonTimestamp(123L) },
                { "Undefined", BsonUndefined.Value },
            };
            var hashtable = document.ToHashtable();
            Assert.AreEqual(9, hashtable.Count);
            Assert.IsNull(hashtable["JavaScript"]);
            Assert.IsNull(hashtable["JavaScriptWithScope"]);
            Assert.IsNull(hashtable["MaxKey"]);
            Assert.IsNull(hashtable["MinKey"]);
            Assert.IsNull(hashtable["Null"]);
            Assert.IsNull(hashtable["RegularExpression"]);
            Assert.IsNull(hashtable["Symbol"]);
            Assert.IsNull(hashtable["Timestamp"]);
            Assert.IsNull(hashtable["Undefined"]);
        }

        private void AssertAreEqual(string expected, byte[] actual)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in actual)
            {
                if (b >= 0x20 && b <= 0x7e)
                {
                    sb.Append((char)b);
                }
                else
                {
                    string hex = "0123456789abcdef";
                    int x = b >> 4;
                    int y = b & 0x0f;
                    sb.AppendFormat(@"\x{0}{1}", hex[x], hex[y]);
                }
            }
            Assert.AreEqual(expected, sb.ToString());
        }
    }
}
