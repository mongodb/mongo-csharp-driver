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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests
{
    public class BsonDocumentTests
    {
        [Fact]
        public void TestAddArrayListWithOneEntry()
        {
            var arrayList = new ArrayList { 1 };
            var array = new BsonArray(arrayList);
            var json = array.ToJson();
            var expected = "[1]".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestAddArrayListWithTwoEntries()
        {
            var arrayList = new ArrayList { 1, 2 };
            var array = new BsonArray(arrayList);
            var json = array.ToJson();
            var expected = "[1, 2]".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestAddHashtableWithOneEntry()
        {
            var hashtable = new Hashtable { { "A", 1 } };
            var document = new BsonDocument(hashtable);
            var json = document.ToJson();
            var expected = "{ 'A' : 1 }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestAddHashtableWithTwoEntries()
        {
            var hashtable = new Hashtable { { "A", 1 }, { "B", 2 } };
            var document = new BsonDocument(hashtable);
            // note: can't test json against expected because the order of the keys in the hash table is not defined
            Assert.Equal(2, document.ElementCount);
            Assert.Equal(1, document["A"].AsInt32);
            Assert.Equal(2, document["B"].AsInt32);
        }

        [Fact]
        public void TestAddHashtableWithNestedHashtable()
        {
            var hashtable = new Hashtable
            {
                { "A", 1 },
                { "B", new Hashtable { { "C", 2 }, { "D", 3 } } }
            };
            var document = new BsonDocument(hashtable);
            // note: can't test json against expected because the order of the keys in the hash table is not defined
            Assert.Equal(2, document.ElementCount);
            Assert.Equal(1, document["A"].AsInt32);
            Assert.Equal(2, document["B"]["C"].AsInt32);
            Assert.Equal(3, document["B"]["D"].AsInt32);
        }

        [Fact]
        public void TestAddNameValueWithCondition()
        {
            var document = new BsonDocument();
            document.Add("x", 1, false);
            Assert.Equal(0, document.ElementCount);
            document.Add("x", 1, true);
            Assert.Equal(1, document.ElementCount);
            Assert.Equal(1, document["x"].AsInt32);
        }

        [Fact]
        public void TestAutoIndexing()
        {
            var document = new BsonDocument
            {
                { "Addresses", new BsonArray
                    {
                        new BsonDocument { { "StreetAdress", "123 Main St" }, { "City", "Hope" } },
                        new BsonDocument { { "StreetAdress", "456 Main St" }, { "City", "Despair" } },
                    }
                }
            };
            var streetAddress = document["Addresses"][0][0];
            Assert.Same(streetAddress, document["Addresses"][0][0]);

            Assert.Throws<NotSupportedException>(() => { var x = document["Addresses"][0][0][0]; });
            Assert.Throws<NotSupportedException>(() => { var x = document["Addresses"][0][0]["x"]; });
            Assert.Throws<NotSupportedException>(() => { document["Addresses"][0][0][0] = 1; });
            Assert.Throws<NotSupportedException>(() => { document["Addresses"][0][0]["x"] = 1; });
        }

        [Fact]
        public void TestClear()
        {
            var document = new BsonDocument("x", 1);
            Assert.Equal(1, document.ElementCount);
            document.Clear();
            Assert.Equal(0, document.ElementCount);
        }

        [Fact]
        public void TestClone()
        {
            var document = new BsonDocument("d", new BsonDocument("x", 1));
            var clone = (BsonDocument)document.Clone();
            Assert.Equal(clone, document);
            Assert.Same(clone["d"], document["d"]);
        }

        [Fact]
        public void TestConstructorAllowDuplicateNames()
        {
            var document = new BsonDocument(true);
            Assert.Equal(true, document.AllowDuplicateNames);
            document.AllowDuplicateNames = false;
            Assert.Equal(false, document.AllowDuplicateNames);
        }

        [Fact]
        public void TestConstructorElement()
        {
            var element = new BsonElement("x", 1);
            var document = new BsonDocument(element);
            Assert.Equal(1, document.ElementCount);
            Assert.Equal(1, document["x"].AsInt32);
            Assert.Equal(true, document.Contains("x"));
            Assert.Equal(true, document.ContainsValue(1));
            Assert.Same(element.Value, document.GetElement("x").Value);
        }

        [Fact]
        public void TestConstructorElements()
        {
            var elements = new BsonElement[] {
                new BsonElement("x", 1),
                new BsonElement("y", 2)
            };
            var document = new BsonDocument((IEnumerable<BsonElement>)elements);
            Assert.Equal(2, document.ElementCount);
            Assert.Equal(1, document["x"].AsInt32);
            Assert.Equal(2, document["y"].AsInt32);
            Assert.Equal(true, document.Contains("x"));
            Assert.Equal(true, document.Contains("y"));
            Assert.Equal(true, document.ContainsValue(1));
            Assert.Equal(true, document.ContainsValue(2));
            Assert.Same(elements[0].Value, document.GetElement("x").Value);
            Assert.Same(elements[1].Value, document.GetElement("y").Value);
        }

        [Fact]
        public void TestConstructorElementsDocument()
        {
            var originalDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var document = new BsonDocument(originalDocument);
            Assert.Equal(2, document.ElementCount);
            Assert.Equal(1, document["x"].AsInt32);
            Assert.Equal(2, document["y"].AsInt32);
            Assert.Equal(true, document.Contains("x"));
            Assert.Equal(true, document.Contains("y"));
            Assert.Equal(true, document.ContainsValue(1));
            Assert.Equal(true, document.ContainsValue(2));
            Assert.Same(originalDocument.GetElement("x").Value, document.GetElement("x").Value);
            Assert.Same(originalDocument.GetElement("y").Value, document.GetElement("y").Value);
        }

        [Fact]
        public void TestConstructorElementsParams()
        {
            var element1 = new BsonElement("x", 1);
            var element2 = new BsonElement("y", 2);
#pragma warning disable 618
            var document = new BsonDocument(element1, element2);
#pragma warning restore
            Assert.Equal(2, document.ElementCount);
            Assert.Equal(1, document["x"].AsInt32);
            Assert.Equal(2, document["y"].AsInt32);
            Assert.Equal(true, document.Contains("x"));
            Assert.Equal(true, document.Contains("y"));
            Assert.Equal(true, document.ContainsValue(1));
            Assert.Equal(true, document.ContainsValue(2));
            Assert.Same(element1.Value, document.GetElement("x").Value);
            Assert.Same(element2.Value, document.GetElement("y").Value);
        }

        [Fact]
        public void TestConstructorDictionaryGeneric()
        {
            var dictionary = new Dictionary<string, object> { { "x", 1 } };
            var document = new BsonDocument(dictionary);
            Assert.Equal(1, document.ElementCount);
            Assert.Equal(1, document["x"].AsInt32);
            Assert.Equal(true, document.Contains("x"));
            Assert.Equal(true, document.ContainsValue(1));
        }

        [Fact]
        public void TestConstructorDictionaryGenericWithKeys()
        {
            var dictionary = new Dictionary<string, object> { { "x", 1 }, { "y", 2 } };
            var keys = new string[] { "x" };
#pragma warning disable 618
            var document = new BsonDocument(dictionary, keys);
#pragma warning restore
            Assert.Equal(1, document.ElementCount);
            Assert.Equal(1, document["x"].AsInt32);
            Assert.Equal(true, document.Contains("x"));
            Assert.Equal(true, document.ContainsValue(1));
        }

        [Fact]
        public void TestConstructorIDictionary()
        {
            var hashtable = (IDictionary)new Hashtable { { "x", 1 } };
            var document = new BsonDocument(hashtable);
            Assert.Equal(1, document.ElementCount);
            Assert.Equal(1, document["x"].AsInt32);
            Assert.Equal(true, document.Contains("x"));
            Assert.Equal(true, document.ContainsValue(1));
        }

        [Fact]
        public void TestConstructorIDictionaryGeneric()
        {
            var dictionary = (IDictionary<string, object>)new Dictionary<string, object> { { "x", 1 } };
            var document = new BsonDocument(dictionary);
            Assert.Equal(1, document.ElementCount);
            Assert.Equal(1, document["x"].AsInt32);
            Assert.Equal(true, document.Contains("x"));
            Assert.Equal(true, document.ContainsValue(1));
        }

        [Fact]
        public void TestConstructorIDictionaryGenericWithKeys()
        {
            var dictionary = (IDictionary<string, object>)new Dictionary<string, object> { { "x", 1 }, { "y", 2 } };
            var keys = new string[] { "x" };
#pragma warning disable 618
            var document = new BsonDocument(dictionary, keys);
#pragma warning restore
            Assert.Equal(1, document.ElementCount);
            Assert.Equal(1, document["x"].AsInt32);
            Assert.Equal(true, document.Contains("x"));
            Assert.Equal(true, document.ContainsValue(1));
        }

        [Fact]
        public void TestConstructorIDictionaryWithKeys()
        {
            var hashtable = (IDictionary)new Hashtable { { "x", 1 }, { "y", 2 } };
            var keys = new string[] { "x" };
#pragma warning disable 618
            var document = new BsonDocument(hashtable, keys);
#pragma warning restore
            Assert.Equal(1, document.ElementCount);
            Assert.Equal(1, document["x"].AsInt32);
            Assert.Equal(true, document.Contains("x"));
            Assert.Equal(true, document.ContainsValue(1));
        }

        [Fact]
        public void TestConstructorNameValue()
        {
            var document = new BsonDocument("x", 1);
            Assert.Equal(1, document.ElementCount);
            Assert.Equal(1, document["x"].AsInt32);
            Assert.Equal(true, document.Contains("x"));
            Assert.Equal(true, document.ContainsValue(1));
        }

        [Fact]
        public void TestConstructorNoArgs()
        {
            var document = new BsonDocument();
            Assert.Equal(false, document.AllowDuplicateNames);
            Assert.IsType<BsonDocument>(document.AsBsonDocument);
            Assert.Equal(BsonType.Document, document.BsonType);
            Assert.Equal(false, document.Contains("name"));
            Assert.Equal(false, document.ContainsValue(0));
            Assert.Equal(0, document.ElementCount);
            Assert.Equal(0, document.Elements.Count());
            Assert.Equal(false, document.IsBsonArray);
            Assert.Equal(true, document.IsBsonDocument);
            Assert.Equal(0, document.Names.Count());
#pragma warning disable 618
            Assert.Equal(null, document.RawValue);
            Assert.Equal(0, document.RawValues.Count());
#pragma warning restore
            Assert.Equal(true, document.ToBoolean());
            Assert.Same(document, document.ToBsonDocument());
            Assert.Equal("{ }", document.ToJson());
            Assert.Equal(0, document.Values.Count());
        }

        [Fact]
        public void TestContains()
        {
            var document = new BsonDocument();
            Assert.False(document.Contains("x"));
            document["x"] = 1;
            Assert.True(document.Contains("x"));
        }

        [Fact]
        public void TestContainsValue()
        {
            var document = new BsonDocument();
            Assert.False(document.ContainsValue(1));
            document["x"] = 1;
            Assert.True(document.ContainsValue(1));
        }

        [Fact]
        public void TestCreateFromDictionary()
        {
            var dictionary = new Dictionary<string, object> { { "x", 1 }, { "n", null }, { "a", new object[] { 1, null } } }; // null will be mapped to BsonNull.Value
            var document = new BsonDocument(dictionary);
            Assert.Equal(3, document.ElementCount);
            Assert.Equal(1, document["x"].AsInt32);
            Assert.Same(BsonNull.Value, document["n"]);
            Assert.True(document["a"].IsBsonArray);
            Assert.Equal(2, document["a"].AsBsonArray.Count);
            Assert.Equal(1, document["a"][0].AsInt32);
            Assert.Same(BsonNull.Value, document["a"][1]);
            Assert.Equal(true, document.Contains("x"));
            Assert.Equal(true, document.Contains("n"));
            Assert.Equal(true, document.Contains("a"));
            Assert.Equal(true, document.ContainsValue(1));
            Assert.Equal(true, document.ContainsValue(BsonNull.Value));
        }

        [Fact]
        public void TestCreateNull()
        {
            object obj = null;
            Assert.Throws<ArgumentNullException>(() => { BsonDocument.Create(obj); });
        }

        [Fact]
        public void TestDeepClone()
        {
            var document = new BsonDocument("d", new BsonDocument("x", 1));
            var clone = (BsonDocument)document.DeepClone();
            Assert.Equal(clone, document);
            Assert.NotSame(clone["d"], document["d"]);
        }

        [Fact]
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
            Assert.Equal("Ernest Hemingway", book["author"].AsString);
            Assert.Equal(123, book["pages"].AsInt32);
            Assert.Equal(0.0, 9.95 - book["price"].AsDouble);
            Assert.Equal(false, book["ok"].ToBoolean());

            book["err"] = "";
            Assert.Equal(false, book["err"].ToBoolean());
            book["err"] = "Error message.";
            Assert.Equal(true, book["err"].ToBoolean());

            book["price"] = (double)book["price"] * 1.1;
            double price = book["price"].AsDouble;
        }

        [Fact]
        public void TestElementNameZeroLength()
        {
            var document = new BsonDocument("", "zero length");
            Assert.Equal(0, document.GetElement(0).Name.Length);
        }

        [Fact]
        public void TestGetDocumentId()
        {
            var document = new BsonDocument("_id", 1);
            object id;
            Type nominalType;
            IIdGenerator idGenerator;
            Assert.True(((IBsonIdProvider)BsonDocumentSerializer.Instance).GetDocumentId(document, out id, out nominalType, out idGenerator));
            Assert.IsType<BsonInt32>(id);
            Assert.Equal(new BsonInt32(1), id);
            Assert.Equal(typeof(BsonValue), nominalType);
            Assert.Null(idGenerator);
        }

        [Fact]
        public void TestGetDocumentIdWhenIdIsGuid()
        {
            var document = new BsonDocument("_id", Guid.Empty);
            object id;
            Type nominalType;
            IIdGenerator idGenerator;
            Assert.True(((IBsonIdProvider)BsonDocumentSerializer.Instance).GetDocumentId(document, out id, out nominalType, out idGenerator));
            Assert.IsType<BsonBinaryData>(id);
            Assert.Equal(new BsonBinaryData(Guid.Empty), id);
            Assert.Equal(typeof(BsonValue), nominalType);
            Assert.IsType<BsonBinaryDataGuidGenerator>(idGenerator);
        }

        [Fact]
        public void TestGetDocumentIdWhenIdIsMissing()
        {
            var document = new BsonDocument();
            object id;
            Type nominalType;
            IIdGenerator idGenerator;
            Assert.True(((IBsonIdProvider)BsonDocumentSerializer.Instance).GetDocumentId(document, out id, out nominalType, out idGenerator));
            Assert.Null(id);
            Assert.Equal(typeof(BsonValue), nominalType);
            Assert.IsType<BsonObjectIdGenerator>(idGenerator);
        }

        [Fact]
        public void TestGetHashCode()
        {
            var document = new BsonDocument("x", 1);
            var hashCode = document.GetHashCode();
            Assert.Equal(hashCode, document.GetHashCode()); // returns same value when called again
        }

        [Fact]
        public void TestGetValueByIndex()
        {
            var document = new BsonDocument("x", 1);
            Assert.Equal(1, document.GetValue(0).AsInt32);
        }

        [Fact]
        public void TestGetValueByName()
        {
            var document = new BsonDocument("x", 1);
            Assert.Equal(1, document.GetValue("x").AsInt32);
        }

        [Fact]
        public void TestGetValueByNameWithDefaultValue()
        {
            var document = new BsonDocument("x", 1);
            Assert.Equal(1, document.GetValue("x", 2).AsInt32);
            Assert.Equal(2, document.GetValue("y", 2).AsInt32);
        }

        [Fact]
        public void TestIndexer()
        {
            var document = new BsonDocument();
            Assert.Equal(0, document.ElementCount);
            document["x"] = 1;
            Assert.Equal(1, document.ElementCount);
            Assert.Equal(1, document["x"].AsInt32);
            Assert.Equal(1, document[0].AsInt32);
            document["y"] = 2;
            Assert.Equal(2, document.ElementCount);
            Assert.Equal(2, document["y"].AsInt32);
            Assert.Equal(2, document[1].AsInt32);
            document["y"] = 3;
            Assert.Equal(2, document.ElementCount);
            Assert.Equal(3, document["y"].AsInt32);
            Assert.Equal(3, document[1].AsInt32);
            document[1] = 4;
            Assert.Equal(2, document.ElementCount);
            Assert.Equal(4, document["y"].AsInt32);
            Assert.Equal(4, document[1].AsInt32);
        }

        [Fact]
        public void TestIndexerWithDefaultValue()
        {
#pragma warning disable 618
            var document = new BsonDocument();
            Assert.Equal(0, document["x", 0].AsInt32);
            document["x"] = 1;
            Assert.Equal(1, document["x", 1].AsInt32);
#pragma warning restore
        }

        [Theory]
        [InlineData("_id", 0)]
        [InlineData("x", 1)]
        [InlineData("y", 2)]
        [InlineData("z", -1)]
        public void TestIndexOfName(string name, int expectedResult)
        {
            var subject = new BsonDocument
            {
                { "_id", 1 },
                { "x", 2 },
                { "y", 3 }
            };

            var result = subject.IndexOfName(name);

            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void TestInsertAt()
        {
            var document = new BsonDocument();
            document.InsertAt(0, new BsonElement("x", 1));
            Assert.Equal("x", document.GetElement(0).Name);
            Assert.Equal(1, document[0].AsInt32);
            document.InsertAt(0, new BsonElement("y", 2));
            Assert.Equal("y", document.GetElement(0).Name);
            Assert.Equal(2, document[0].AsInt32);
            Assert.Equal("x", document.GetElement(1).Name);
            Assert.Equal(1, document[1].AsInt32);
        }

        [Fact]
        public void TestMerge()
        {
            var document = new BsonDocument();
            document.Merge(new BsonDocument("x", 1));
            Assert.Equal(1, document["x"].AsInt32);
            document.Merge(new BsonDocument("x", 2)); // don't overwriteExistingElements
            Assert.Equal(1, document["x"].AsInt32); // has old value
            document.Merge(new BsonDocument("x", 2), true); // overwriteExistingElements
            Assert.Equal(2, document["x"].AsInt32); // has new value
        }

        [Fact]
        public void TestMergeNull()
        {
            var document = new BsonDocument();
            Assert.Throws<ArgumentNullException>(() => { document.Merge(null); });
        }

        [Fact]
        public void TestNullableBoolean()
        {
            var document = new BsonDocument { { "v", true }, { "n", BsonNull.Value }, { "s", "" } };
            Assert.Equal(true, (bool?)document["v"]);
            Assert.Equal(null, (bool?)document["n"]);
#pragma warning disable 618
            Assert.Equal(null, (bool?)document["x", null]);
            Assert.Equal(null, (bool?)document["x", (bool?)null]);
            Assert.Equal(null, (bool?)document["x", BsonNull.Value]);
#pragma warning restore
            Assert.Equal(true, document["v"].AsNullableBoolean);
            Assert.Equal(null, document["n"].AsNullableBoolean);
#pragma warning disable 618
            Assert.Equal(null, document["x", (bool?)null].AsNullableBoolean);
            Assert.Equal(null, document["x", BsonNull.Value].AsNullableBoolean);
#pragma warning restore
#pragma warning disable 219
            Assert.Throws<InvalidCastException>(() => { var v = (bool?)document["s"]; });
            Assert.Throws<InvalidCastException>(() => { var v = document["s"].AsNullableBoolean; });
#pragma warning restore
        }

        [Fact]
        public void TestNullableDateTime()
        {
            var utcNow = DateTime.UtcNow;
            var utcNowTruncated = utcNow.AddTicks(-(utcNow.Ticks % 10000));
            var document = new BsonDocument { { "v", utcNow }, { "n", BsonNull.Value }, { "s", "" } };
            Assert.Equal(utcNowTruncated, (DateTime?)document["v"]);
            Assert.Equal(null, (DateTime?)document["n"]);
#pragma warning disable 618
            Assert.Equal(null, (DateTime?)document["x", null]);
            Assert.Equal(null, (DateTime?)document["x", (DateTime?)null]);
            Assert.Equal(null, (DateTime?)document["x", BsonNull.Value]);
#pragma warning restore
            Assert.Equal(utcNowTruncated, document["v"].ToNullableUniversalTime());
            Assert.Equal(null, document["n"].ToNullableUniversalTime());
#pragma warning disable 618
            Assert.Equal(null, document["x", (DateTime?)null].ToNullableUniversalTime());
            Assert.Equal(null, document["x", BsonNull.Value].ToNullableUniversalTime());
#pragma warning restore
#pragma warning disable 618, 219
            Assert.Throws<InvalidCastException>(() => { var v = (DateTime?)document["s"]; });
            Assert.Throws<InvalidCastException>(() => { var v = document["s"].AsNullableDateTime; });
            Assert.Throws<NotSupportedException>(() => { var v = document["s"].ToNullableUniversalTime(); });
#pragma warning restore
        }

        [Fact]
        public void TestNullableDouble()
        {
            var document = new BsonDocument { { "v", 1.5 }, { "n", BsonNull.Value }, { "s", "" } };
            Assert.Equal(1.5, (double?)document["v"]);
            Assert.Equal(null, (double?)document["n"]);
#pragma warning disable 618
            Assert.Equal(null, (double?)document["x", null]);
            Assert.Equal(null, (double?)document["x", (double?)null]);
            Assert.Equal(null, (double?)document["x", BsonNull.Value]);
#pragma warning restore
            Assert.Equal(1.5, document["v"].AsNullableDouble);
            Assert.Equal(null, document["n"].AsNullableDouble);
#pragma warning disable 618
            Assert.Equal(null, document["x", (double?)null].AsNullableDouble);
            Assert.Equal(null, document["x", BsonNull.Value].AsNullableDouble);
#pragma warning restore
#pragma warning disable 219
            Assert.Throws<InvalidCastException>(() => { var v = (double?)document["s"]; });
            Assert.Throws<InvalidCastException>(() => { var v = document["s"].AsNullableDouble; });
#pragma warning restore
        }

        [Fact]
        public void TestNullableGuid()
        {
            var guid = Guid.NewGuid();
            var document = new BsonDocument { { "v", guid }, { "n", BsonNull.Value }, { "s", "" } };
            Assert.Equal(guid, (Guid?)document["v"]);
            Assert.Equal(null, (Guid?)document["n"]);
#pragma warning disable 618
            Assert.Equal(null, (Guid?)document["x", null]);
            Assert.Equal(null, (Guid?)document["x", (Guid?)null]);
            Assert.Equal(null, (Guid?)document["x", BsonNull.Value]);
#pragma warning restore
            Assert.Equal(guid, document["v"].AsNullableGuid);
            Assert.Equal(null, document["n"].AsNullableGuid);
#pragma warning disable 618
            Assert.Equal(null, document["x", (Guid?)null].AsNullableGuid);
            Assert.Equal(null, document["x", BsonNull.Value].AsNullableGuid);
#pragma warning restore
#pragma warning disable 219
            Assert.Throws<InvalidCastException>(() => { var v = (Guid?)document["s"]; });
            Assert.Throws<InvalidCastException>(() => { var v = document["s"].AsNullableGuid; });
#pragma warning restore
        }

        [Fact]
        public void TestNullableInt32()
        {
            var document = new BsonDocument { { "v", 1 }, { "n", BsonNull.Value }, { "s", "" } };
            Assert.Equal(1, (int?)document["v"]);
            Assert.Equal(null, (int?)document["n"]);
#pragma warning disable 618
            Assert.Equal(null, (int?)document["x", null]);
            Assert.Equal(null, (int?)document["x", (int?)null]);
            Assert.Equal(null, (int?)document["x", BsonNull.Value]);
#pragma warning restore
            Assert.Equal(1, document["v"].AsNullableInt32);
            Assert.Equal(null, document["n"].AsNullableInt32);
#pragma warning disable 618
            Assert.Equal(null, document["x", (int?)null].AsNullableInt32);
            Assert.Equal(null, document["x", BsonNull.Value].AsNullableInt32);
#pragma warning restore
#pragma warning disable 219
            Assert.Throws<InvalidCastException>(() => { var v = (int?)document["s"]; });
            Assert.Throws<InvalidCastException>(() => { var v = document["s"].AsNullableInt32; });
#pragma warning restore
        }

        [Fact]
        public void TestNullableInt64()
        {
            var document = new BsonDocument { { "v", 1L }, { "n", BsonNull.Value }, { "s", "" } };
            Assert.Equal(1L, (long?)document["v"]);
            Assert.Equal(null, (long?)document["n"]);
#pragma warning disable 618
            Assert.Equal(null, (long?)document["x", null]);
            Assert.Equal(null, (long?)document["x", (long?)null]);
            Assert.Equal(null, (long?)document["x", BsonNull.Value]);
#pragma warning restore
            Assert.Equal(1L, document["v"].AsNullableInt64);
            Assert.Equal(null, document["n"].AsNullableInt64);
#pragma warning disable 618
            Assert.Equal(null, document["x", (long?)null].AsNullableInt64);
            Assert.Equal(null, document["x", BsonNull.Value].AsNullableInt64);
#pragma warning restore
#pragma warning disable 219
            Assert.Throws<InvalidCastException>(() => { var v = (long?)document["s"]; });
            Assert.Throws<InvalidCastException>(() => { var v = document["s"].AsNullableInt64; });
#pragma warning restore
        }

        [Fact]
        public void TestNullableObjectId()
        {
            var objectId = ObjectId.GenerateNewId();
            var document = new BsonDocument { { "v", objectId }, { "n", BsonNull.Value }, { "s", "" } };
            Assert.Equal(objectId, (ObjectId?)document["v"]);
            Assert.Equal(null, (ObjectId?)document["n"]);
#pragma warning disable 618
            Assert.Equal(null, (ObjectId?)document["x", null]);
            Assert.Equal(null, (ObjectId?)document["x", (ObjectId?)null]);
            Assert.Equal(null, (ObjectId?)document["x", BsonNull.Value]);
#pragma warning restore
            Assert.Equal(objectId, document["v"].AsNullableObjectId);
            Assert.Equal(null, document["n"].AsNullableObjectId);
#pragma warning disable 618
            Assert.Equal(null, document["x", (ObjectId?)null].AsNullableObjectId);
            Assert.Equal(null, document["x", BsonNull.Value].AsNullableObjectId);
#pragma warning restore
#pragma warning disable 219
            Assert.Throws<InvalidCastException>(() => { var v = (ObjectId?)document["s"]; });
            Assert.Throws<InvalidCastException>(() => { var v = document["s"].AsNullableObjectId; });
#pragma warning restore
        }

        [Fact]
        public void TestOperatorEqual()
        {
            var document1 = new BsonDocument("x", 1);
            var document2 = new BsonDocument("x", 1);
            Assert.NotSame(document1, document2);
            Assert.True(document1 == document2);
        }

        [Fact]
        public void TestOperatorEqualBothNull()
        {
            BsonDocument document1 = null;
            BsonDocument document2 = null;
            Assert.True(document1 == document2);
        }

        [Fact]
        public void TestOperatorEqualLhsNull()
        {
            var document = new BsonDocument();
            Assert.False(null == document);
        }

        [Fact]
        public void TestOperatorEqualRhsNull()
        {
            var document = new BsonDocument();
            Assert.False(document == null);
        }

        [Fact]
        public void TestOperatorNotEqual()
        {
            var document1 = new BsonDocument("x", 1);
            var document2 = new BsonDocument("x", 1);
            Assert.NotSame(document1, document2);
            Assert.False(document1 != document2);
        }

        [Fact]
        public void TestOperatorNotEqualBothNull()
        {
            BsonDocument document1 = null;
            BsonDocument document2 = null;
            Assert.False(document1 != document2);
        }

        [Fact]
        public void TestOperatorNotEqualLhsNull()
        {
            var document = new BsonDocument();
            Assert.True(null != document);
        }

        [Fact]
        public void TestOperatorNotEqualRhsNull()
        {
            var document = new BsonDocument();
            Assert.True(document != null);
        }

        [Fact]
        public void TestParse()
        {
            var json = "{ a : 1, b : 'abc' }";
            var document = BsonDocument.Parse(json);
            Assert.Equal(2, document.ElementCount);
            Assert.Equal(1, document["a"].AsInt32);
            Assert.Equal("abc", document["b"].AsString);
        }

        [Fact]
        public void TestParseWithExtraCharacters()
        {
            var json = "{ a : 1, b : 'abc' } x";
            Action action = () => BsonDocument.Parse(json);
            action.ShouldThrow<FormatException>();
        }

        [Fact]
        public void TestParseWithExtraWhitespace()
        {
            var json = "{ a : 1, b : 'abc' }   ";
            var document = BsonDocument.Parse(json);
            Assert.Equal(2, document.ElementCount);
            Assert.Equal(1, document["a"].AsInt32);
            Assert.Equal("abc", document["b"].AsString);
        }

        [Fact]
        public void TestRemove()
        {
            var document = new BsonDocument("x", 1);
            Assert.Equal(1, document.ElementCount);
            document.Remove("x");
            Assert.Equal(0, document.ElementCount);
        }

        [Fact]
        public void TestRemoveAt()
        {
            var document = new BsonDocument { { "x", 1 }, { "y", 2 } };
            Assert.Equal(2, document.ElementCount);
            document.RemoveAt(0);
            Assert.Equal(1, document.ElementCount);
            Assert.Equal(2, document["y"].AsInt32);
        }

        [Fact]
        public void TestRemoveElement()
        {
            var document = new BsonDocument { { "x", 1 }, { "y", 2 } };
            Assert.Equal(2, document.ElementCount);
            document.RemoveElement(document.GetElement(0));
            Assert.Equal(1, document.ElementCount);
            Assert.Equal(2, document["y"].AsInt32);
        }

        [Fact]
        public void TestSetByIndex()
        {
            var document = new BsonDocument("x", 1);
            Assert.Equal(1, document.ElementCount);
            Assert.Equal(1, document["x"].AsInt32);
            document.Set(0, 2);
            Assert.Equal(1, document.ElementCount);
            Assert.Equal(2, document["x"].AsInt32);
        }

        [Fact]
        public void TestSetByName()
        {
            var document = new BsonDocument();
            Assert.Equal(0, document.ElementCount);
            document.Set("x", 1);
            Assert.Equal(1, document.ElementCount);
            Assert.Equal(1, document["x"].AsInt32);
            document.Set("x", 2);
            Assert.Equal(1, document.ElementCount);
            Assert.Equal(2, document["x"].AsInt32);
        }

        [Fact]
        public void TestSetDocumentId()
        {
            var document = new BsonDocument("_id", 1);
            ((IBsonIdProvider)BsonDocumentSerializer.Instance).SetDocumentId(document, BsonValue.Create(2));
            Assert.Equal(2, document["_id"].AsInt32);
        }

        [Fact]
        public void TestSetDocumentIdNewElement()
        {
            var document = new BsonDocument("x", 1);
            ((IBsonIdProvider)BsonDocumentSerializer.Instance).SetDocumentId(document, BsonValue.Create(2));
            Assert.Equal(2, document.ElementCount);
            Assert.Equal("_id", document.GetElement(0).Name);
            Assert.Equal(2, document["_id"].AsInt32);
        }

        [Fact]
        public void TestSetElementByIndex()
        {
            var document = new BsonDocument("x", 1);
            var element = new BsonElement("y", 2);
            document.SetElement(0, element);
            Assert.Equal(1, document.ElementCount);
            Assert.Equal("y", document.GetElement(0).Name);
            Assert.Equal(2, document["y"].AsInt32);
        }

        [Fact]
        public void TestSetElementNewElement()
        {
            var document = new BsonDocument("x", 1);
            var element = new BsonElement("y", 2);
            document.SetElement(element);
            Assert.Equal(2, document.ElementCount);
            Assert.Equal("x", document.GetElement(0).Name);
            Assert.Equal(1, document["x"].AsInt32);
            Assert.Equal("y", document.GetElement(1).Name);
            Assert.Equal(2, document["y"].AsInt32);
        }

        [Fact]
        public void TestSetElementReplaceElement()
        {
            var document = new BsonDocument("x", 1);
            var element = new BsonElement("x", 2);
            document.SetElement(element);
            Assert.Equal(1, document.ElementCount);
            Assert.Equal("x", document.GetElement(0).Name);
            Assert.Equal(2, document["x"].AsInt32);
        }

        [Fact]
        public void TestSpecBsonAwesomeWithBsonDocument()
        {
            // this test is from http://bsonspec.org/#/specification
            BsonDocument document = new BsonDocument("BSON", new BsonArray { "awesome", 5.05, 1986 });
            byte[] bson = document.ToBson();
            AssertAreEqual(@"1\x00\x00\x00\x04BSON\x00&\x00\x00\x00\x020\x00\x08\x00\x00\x00awesome\x00\x011\x00333333\x14@\x102\x00\xc2\x07\x00\x00\x00\x00", bson);
        }

        [Fact]
        public void TestSpecBsonAwesomeWithBsonWriter()
        {
            // this test is from http://bsonspec.org/#/specification
            var stream = new MemoryStream();
            using (var bsonWriter = new BsonBinaryWriter(stream))
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

        [Fact]
        public void TestSpecHelloWorldWithBsonDocument()
        {
            // this test is from http://bsonspec.org/#/specification
            BsonDocument document = new BsonDocument("hello", "world");
            byte[] bson = document.ToBson();
            AssertAreEqual(@"\x16\x00\x00\x00\x02hello\x00\x06\x00\x00\x00world\x00\x00", bson);
        }

        [Fact]
        public void TestSpecHelloWorldWithBsonWriter()
        {
            // this test is from http://bsonspec.org/#/specification
            var stream = new MemoryStream();
            using (var bsonWriter = new BsonBinaryWriter(stream))
            {
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteString("hello", "world");
                bsonWriter.WriteEndDocument();
            }
            byte[] bytes = stream.ToArray();
            AssertAreEqual(@"\x16\x00\x00\x00\x02hello\x00\x06\x00\x00\x00world\x00\x00", bytes);
        }

        [Fact]
        public void TestToBsonDocument()
        {
            var document = new BsonDocument();
            Assert.Same(document, document.ToBsonDocument());
            Assert.Same(document, ((IConvertibleToBsonDocument)document).ToBsonDocument());
        }

        [Fact]
        public void TestToDictionaryEmpty()
        {
            var document = new BsonDocument();
            var dictionary = document.ToDictionary();
            Assert.Equal(0, dictionary.Count);
        }

        [Fact]
        public void TestToDictionaryNestedArray()
        {
            var document = new BsonDocument
            {
                { "x", 1 },
                { "array", new BsonArray { 1, "abc" } }
            };
            var dictionary = document.ToDictionary();
            Assert.Equal(2, dictionary.Count);
            Assert.IsType<int>(dictionary["x"]);
            Assert.Equal(1, dictionary["x"]);
            Assert.IsType<object[]>(dictionary["array"]);
            var nested = (object[])dictionary["array"];
            Assert.IsType<int>(nested[0]);
            Assert.IsType<string>(nested[1]);
            Assert.Equal(1, nested[0]);
            Assert.Equal("abc", nested[1]);
        }

        [Fact]
        public void TestToDictionaryNestedDocument()
        {
            var document = new BsonDocument
            {
                { "x", 1 },
                { "nested", new BsonDocument { { "a", 1 }, { "b", 2 } } }
            };
            var dictionary = document.ToDictionary();
            Assert.Equal(2, dictionary.Count);
            Assert.IsType<int>(dictionary["x"]);
            Assert.Equal(1, dictionary["x"]);
            Assert.IsType<Dictionary<string, object>>(dictionary["nested"]);
            var nested = (Dictionary<string, object>)dictionary["nested"];
            Assert.IsType<int>(nested["a"]);
            Assert.IsType<int>(nested["b"]);
            Assert.Equal(1, nested["a"]);
            Assert.Equal(2, nested["b"]);
        }

        [Fact]
        public void TestToDictionaryOneBinary()
        {
            var document = new BsonDocument("x", new BsonBinaryData(new byte[] { 1, 2, 3 }, BsonBinarySubType.Binary));
            var dictionary = document.ToDictionary();
            Assert.Equal(1, dictionary.Count);
            Assert.IsType<byte[]>(dictionary["x"]);
            Assert.True(new byte[] { 1, 2, 3 }.SequenceEqual((byte[])dictionary["x"]));
        }

        [Fact]
        public void TestToDictionaryOneBoolean()
        {
            var document = new BsonDocument("x", true);
            var dictionary = document.ToDictionary();
            Assert.Equal(1, dictionary.Count);
            Assert.IsType<bool>(dictionary["x"]);
            Assert.Equal(true, dictionary["x"]);
        }

        [Fact]
        public void TestToDictionaryOneDateTime()
        {
            var utcNow = DateTime.UtcNow;
            var utcNowTruncated = utcNow.AddTicks(-(utcNow.Ticks % 10000));
            var document = new BsonDocument("x", utcNow);
            var dictionary = document.ToDictionary();
            Assert.Equal(1, dictionary.Count);
            Assert.IsType<DateTime>(dictionary["x"]);
            Assert.Equal(DateTimeKind.Utc, ((DateTime)dictionary["x"]).Kind);
            Assert.Equal(utcNowTruncated, dictionary["x"]);
        }

        [Fact]
        public void TestToDictionaryOneDouble()
        {
            var document = new BsonDocument("x", 1.0);
            var dictionary = document.ToDictionary();
            Assert.Equal(1, dictionary.Count);
            Assert.IsType<double>(dictionary["x"]);
            Assert.Equal(1.0, dictionary["x"]);
        }

        [Fact]
        public void TestToDictionaryOneGuidLegacy()
        {
            var guid = Guid.NewGuid();
            var document = new BsonDocument("x", new BsonBinaryData(guid, GuidRepresentation.CSharpLegacy));
            var dictionary = document.ToDictionary();
            Assert.Equal(1, dictionary.Count);
            Assert.IsType<Guid>(dictionary["x"]);
            Assert.Equal(guid, dictionary["x"]);
        }

        [Fact]
        public void TestToDictionaryOneGuidStandard()
        {
            var guid = Guid.NewGuid();
            var document = new BsonDocument("x", new BsonBinaryData(guid, GuidRepresentation.Standard));
            var dictionary = document.ToDictionary();
            Assert.Equal(1, dictionary.Count);
            Assert.IsType<Guid>(dictionary["x"]);
            Assert.Equal(guid, dictionary["x"]);
        }

        [Fact]
        public void TestToDictionaryOneInt32()
        {
            var document = new BsonDocument("x", 1);
            var dictionary = document.ToDictionary();
            Assert.Equal(1, dictionary.Count);
            Assert.IsType<int>(dictionary["x"]);
            Assert.Equal(1, dictionary["x"]);
        }

        [Fact]
        public void TestToDictionaryOneInt64()
        {
            var document = new BsonDocument("x", 1L);
            var dictionary = document.ToDictionary();
            Assert.Equal(1, dictionary.Count);
            Assert.IsType<long>(dictionary["x"]);
            Assert.Equal(1L, dictionary["x"]);
        }

        [Fact]
        public void TestToDictionaryOneObjectId()
        {
            var objectId = ObjectId.GenerateNewId();
            var hashtable = new BsonDocument("x", objectId);
            var dictionary = hashtable.ToDictionary();
            Assert.Equal(1, dictionary.Count);
            Assert.IsType<ObjectId>(dictionary["x"]);
            Assert.Equal(objectId, dictionary["x"]);
        }

        [Fact]
        public void TestToDictionaryOneString()
        {
            var document = new BsonDocument("x", "abc");
            var dictionary = document.ToDictionary();
            Assert.Equal(1, dictionary.Count);
            Assert.IsType<string>(dictionary["x"]);
            Assert.Equal("abc", dictionary["x"]);
        }

        [Fact]
        public void TestToDictionaryOtherTypes()
        {
            var document = new BsonDocument
            {
                { "JavaScript", new BsonJavaScript("x = 1") },
                { "JavaScriptWithScope", new BsonJavaScriptWithScope("x = y", new BsonDocument("y", 1)) },
                { "MaxKey", BsonMaxKey.Value },
                { "MinKey", BsonMinKey.Value },
                { "Null", BsonNull.Value },
                { "RegularExpression", new BsonRegularExpression("abc") },
                { "Symbol", BsonSymbolTable.Lookup("name") },
                { "Timestamp", new BsonTimestamp(123L) },
                { "Undefined", BsonUndefined.Value },
            };
            var dictionary = document.ToDictionary();
            Assert.Equal(9, dictionary.Count);
            Assert.IsType<BsonJavaScript>(dictionary["JavaScript"]);
            Assert.IsType<BsonJavaScriptWithScope>(dictionary["JavaScriptWithScope"]);
            Assert.Same(BsonMaxKey.Value, dictionary["MaxKey"]);
            Assert.Same(BsonMinKey.Value, dictionary["MinKey"]);
            Assert.Null(dictionary["Null"]);
            Assert.IsType<BsonRegularExpression>(dictionary["RegularExpression"]);
            Assert.IsType<BsonSymbol>(dictionary["Symbol"]);
            Assert.IsType<BsonTimestamp>(dictionary["Timestamp"]);
            Assert.Same(BsonUndefined.Value, dictionary["Undefined"]);
        }

        [Fact]
        public void TestToHashtableEmpty()
        {
            var document = new BsonDocument();
            var hashtable = document.ToHashtable();
            Assert.Equal(0, hashtable.Count);
        }

        [Fact]
        public void TestToHashtableNestedArray()
        {
            var document = new BsonDocument
            {
                { "x", 1 },
                { "array", new BsonArray { 1, "abc" } }
            };
            var hashtable = document.ToHashtable();
            Assert.Equal(2, hashtable.Count);
            Assert.IsType<int>(hashtable["x"]);
            Assert.Equal(1, hashtable["x"]);
            Assert.IsType<object[]>(hashtable["array"]);
            var nested = (object[])hashtable["array"];
            Assert.IsType<int>(nested[0]);
            Assert.IsType<string>(nested[1]);
            Assert.Equal(1, nested[0]);
            Assert.Equal("abc", nested[1]);
        }

        [Fact]
        public void TestToHashtableNestedDocument()
        {
            var document = new BsonDocument
            {
                { "x", 1 },
                { "nested", new BsonDocument { { "a", 1 }, { "b", 2 } } }
            };
            var hashtable = document.ToHashtable();
            Assert.Equal(2, hashtable.Count);
            Assert.IsType<int>(hashtable["x"]);
            Assert.Equal(1, hashtable["x"]);
            Assert.IsType<Hashtable>(hashtable["nested"]);
            var nested = (Hashtable)hashtable["nested"];
            Assert.IsType<int>(nested["a"]);
            Assert.IsType<int>(nested["b"]);
            Assert.Equal(1, nested["a"]);
            Assert.Equal(2, nested["b"]);
        }

        [Fact]
        public void TestToHashtableOneBinary()
        {
            var document = new BsonDocument("x", new BsonBinaryData(new byte[] { 1, 2, 3 }, BsonBinarySubType.Binary));
            var hashtable = document.ToHashtable();
            Assert.Equal(1, hashtable.Count);
            Assert.IsType<byte[]>(hashtable["x"]);
            Assert.True(new byte[] { 1, 2, 3 }.SequenceEqual((byte[])hashtable["x"]));
        }

        [Fact]
        public void TestToHashtableOneBoolean()
        {
            var document = new BsonDocument("x", true);
            var dictionary = document.ToHashtable();
            Assert.Equal(1, dictionary.Count);
            Assert.IsType<bool>(dictionary["x"]);
            Assert.Equal(true, dictionary["x"]);
        }

        [Fact]
        public void TestToHashtableOneDateTime()
        {
            var utcNow = DateTime.UtcNow;
            var utcNowTruncated = utcNow.AddTicks(-(utcNow.Ticks % 10000));
            var hashtable = new BsonDocument("x", utcNow);
            var dictionary = hashtable.ToHashtable();
            Assert.Equal(1, dictionary.Count);
            Assert.IsType<DateTime>(dictionary["x"]);
            Assert.Equal(DateTimeKind.Utc, ((DateTime)dictionary["x"]).Kind);
            Assert.Equal(utcNowTruncated, dictionary["x"]);
        }

        [Fact]
        public void TestToHashtableOneDouble()
        {
            var document = new BsonDocument("x", 1.0);
            var hashtable = document.ToHashtable();
            Assert.Equal(1, hashtable.Count);
            Assert.IsType<double>(hashtable["x"]);
            Assert.Equal(1.0, hashtable["x"]);
        }

        [Fact]
        public void TestToHashtableOneGuidLegacy()
        {
            var guid = Guid.NewGuid();
            var hashtable = new BsonDocument("x", new BsonBinaryData(guid, GuidRepresentation.CSharpLegacy));
            var dictionary = hashtable.ToHashtable();
            Assert.Equal(1, dictionary.Count);
            Assert.IsType<Guid>(dictionary["x"]);
            Assert.Equal(guid, dictionary["x"]);
        }

        [Fact]
        public void TestToHashtableOneGuidStandard()
        {
            var guid = Guid.NewGuid();
            var hashtable = new BsonDocument("x", new BsonBinaryData(guid, GuidRepresentation.Standard));
            var dictionary = hashtable.ToHashtable();
            Assert.Equal(1, dictionary.Count);
            Assert.IsType<Guid>(dictionary["x"]);
            Assert.Equal(guid, dictionary["x"]);
        }

        [Fact]
        public void TestToHashtableOneInt32()
        {
            var document = new BsonDocument("x", 1);
            var hashtable = document.ToHashtable();
            Assert.Equal(1, hashtable.Count);
            Assert.IsType<int>(hashtable["x"]);
            Assert.Equal(1, hashtable["x"]);
        }

        [Fact]
        public void TestToHashtableOneInt64()
        {
            var document = new BsonDocument("x", 1L);
            var hashtable = document.ToHashtable();
            Assert.Equal(1, hashtable.Count);
            Assert.IsType<long>(hashtable["x"]);
            Assert.Equal(1L, hashtable["x"]);
        }

        [Fact]
        public void TestToHashtableOneObjectId()
        {
            var objectId = ObjectId.GenerateNewId();
            var hashtable = new BsonDocument("x", objectId);
            var dictionary = hashtable.ToHashtable();
            Assert.Equal(1, dictionary.Count);
            Assert.IsType<ObjectId>(dictionary["x"]);
            Assert.Equal(objectId, dictionary["x"]);
        }

        [Fact]
        public void TestToHashtableOneString()
        {
            var document = new BsonDocument("x", "abc");
            var hashtable = document.ToHashtable();
            Assert.Equal(1, hashtable.Count);
            Assert.IsType<string>(hashtable["x"]);
            Assert.Equal("abc", hashtable["x"]);
        }

        [Fact]
        public void TestToHashtableOtherTypes()
        {
            var document = new BsonDocument
            {
                { "JavaScript", new BsonJavaScript("x = 1") },
                { "JavaScriptWithScope", new BsonJavaScriptWithScope("x = y", new BsonDocument("y", 1)) },
                { "MaxKey", BsonMaxKey.Value },
                { "MinKey", BsonMinKey.Value },
                { "Null", BsonNull.Value },
                { "RegularExpression", new BsonRegularExpression("abc") },
                { "Symbol", BsonSymbolTable.Lookup("name") },
                { "Timestamp", new BsonTimestamp(123L) },
                { "Undefined", BsonUndefined.Value },
            };
            var hashtable = document.ToHashtable();
            Assert.Equal(9, hashtable.Count);
            Assert.IsType<BsonJavaScript>(hashtable["JavaScript"]);
            Assert.IsType<BsonJavaScriptWithScope>(hashtable["JavaScriptWithScope"]);
            Assert.Same(BsonMaxKey.Value, hashtable["MaxKey"]);
            Assert.Same(BsonMinKey.Value, hashtable["MinKey"]);
            Assert.Null(hashtable["Null"]);
            Assert.IsType<BsonRegularExpression>(hashtable["RegularExpression"]);
            Assert.IsType<BsonSymbol>(hashtable["Symbol"]);
            Assert.IsType<BsonTimestamp>(hashtable["Timestamp"]);
            Assert.Same(BsonUndefined.Value, hashtable["Undefined"]);
        }

        [Fact]
        public void TestTryGetElement()
        {
            var document = new BsonDocument();
            BsonElement element;
            Assert.False(document.TryGetElement("x", out element));
            document["x"] = 1;
            Assert.True(document.TryGetElement("x", out element));
            Assert.Equal("x", element.Name);
            Assert.Equal(1, element.Value.AsInt32);
        }

        [Fact]
        public void TestTryGetValue()
        {
            var document = new BsonDocument();
            BsonValue value;
            Assert.False(document.TryGetValue("x", out value));
            document["x"] = 1;
            Assert.True(document.TryGetValue("x", out value));
            Assert.Equal(1, value.AsInt32);
        }

        [Fact]
        public void TestTryParse_success()
        {
            var s = "{ x : 1 }";
            BsonDocument result;

            var success = BsonDocument.TryParse(s, out result);

            success.Should().BeTrue();
            result.Should().Be(new BsonDocument("x", 1));
        }

        [Fact]
        public void TestTryParse_failure()
        {
            var s = "{ ...";
            BsonDocument result;

            var success = BsonDocument.TryParse(s, out result);

            success.Should().BeFalse();
            result.Should().BeNull();
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
            Assert.Equal(expected, sb.ToString());
        }
    }
}
