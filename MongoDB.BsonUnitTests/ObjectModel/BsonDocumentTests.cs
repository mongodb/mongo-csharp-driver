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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests
{
    [TestFixture]
    public class BsonDocumentTests
    {
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
            Assert.AreEqual(2, document["B"]["C"].AsInt32);
            Assert.AreEqual(3, document["B"]["D"].AsInt32);
        }

        [Test]
        public void TestAddNameValueWithCondition()
        {
            var document = new BsonDocument();
            document.Add("x", 1, false);
            Assert.AreEqual(0, document.ElementCount);
            document.Add("x", 1, true);
            Assert.AreEqual(1, document.ElementCount);
            Assert.AreEqual(1, document["x"].AsInt32);
        }

        [Test]
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
            Assert.AreSame(streetAddress, document["Addresses"][0][0]);

            Assert.Throws<NotSupportedException>(() => { var x = document["Addresses"][0][0][0]; });
            Assert.Throws<NotSupportedException>(() => { var x = document["Addresses"][0][0]["x"]; });
            Assert.Throws<NotSupportedException>(() => { document["Addresses"][0][0][0] = 1; });
            Assert.Throws<NotSupportedException>(() => { document["Addresses"][0][0]["x"] = 1; });
        }

        [Test]
        public void TestClear()
        {
            var document = new BsonDocument("x", 1);
            Assert.AreEqual(1, document.ElementCount);
            document.Clear();
            Assert.AreEqual(0, document.ElementCount);
        }

        [Test]
        public void TestClone()
        {
            var document = new BsonDocument("d", new BsonDocument("x", 1));
            var clone = (BsonDocument)document.Clone();
            Assert.AreEqual(clone, document);
            Assert.AreSame(clone["d"], document["d"]);
        }

        [Test]
        public void TestConstructorAllowDuplicateNames()
        {
            var document = new BsonDocument(true);
            Assert.AreEqual(true, document.AllowDuplicateNames);
            document.AllowDuplicateNames = false;
            Assert.AreEqual(false, document.AllowDuplicateNames);
        }

        [Test]
        public void TestConstructorElement()
        {
            var element = new BsonElement("x", 1);
            var document = new BsonDocument(element);
            Assert.AreEqual(1, document.ElementCount);
            Assert.AreEqual(1, document["x"].AsInt32);
            Assert.AreEqual(true, document.Contains("x"));
            Assert.AreEqual(true, document.ContainsValue(1));
            Assert.AreSame(element, document.GetElement("x"));
        }

        [Test]
        public void TestConstructorElements()
        {
            var elements = new BsonElement[] {
                new BsonElement("x", 1),
                new BsonElement("y", 2)
            };
            var document = new BsonDocument((IEnumerable<BsonElement>)elements);
            Assert.AreEqual(2, document.ElementCount);
            Assert.AreEqual(1, document["x"].AsInt32);
            Assert.AreEqual(2, document["y"].AsInt32);
            Assert.AreEqual(true, document.Contains("x"));
            Assert.AreEqual(true, document.Contains("y"));
            Assert.AreEqual(true, document.ContainsValue(1));
            Assert.AreEqual(true, document.ContainsValue(2));
            Assert.AreSame(elements[0], document.GetElement("x"));
            Assert.AreSame(elements[1], document.GetElement("y"));
        }

        [Test]
        public void TestConstructorElementsDocument()
        {
            var originalDocument = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var document = new BsonDocument(originalDocument);
            Assert.AreEqual(2, document.ElementCount);
            Assert.AreEqual(1, document["x"].AsInt32);
            Assert.AreEqual(2, document["y"].AsInt32);
            Assert.AreEqual(true, document.Contains("x"));
            Assert.AreEqual(true, document.Contains("y"));
            Assert.AreEqual(true, document.ContainsValue(1));
            Assert.AreEqual(true, document.ContainsValue(2));
            Assert.AreSame(originalDocument.GetElement("x"), document.GetElement("x"));
            Assert.AreSame(originalDocument.GetElement("y"), document.GetElement("y"));
        }

        [Test]
        public void TestConstructorElementsParams()
        {
            var element1 = new BsonElement("x", 1);
            var element2 = new BsonElement("y", 2);
#pragma warning disable 618
            var document = new BsonDocument(element1, element2);
#pragma warning restore
            Assert.AreEqual(2, document.ElementCount);
            Assert.AreEqual(1, document["x"].AsInt32);
            Assert.AreEqual(2, document["y"].AsInt32);
            Assert.AreEqual(true, document.Contains("x"));
            Assert.AreEqual(true, document.Contains("y"));
            Assert.AreEqual(true, document.ContainsValue(1));
            Assert.AreEqual(true, document.ContainsValue(2));
            Assert.AreSame(element1, document.GetElement("x"));
            Assert.AreSame(element2, document.GetElement("y"));
        }

        [Test]
        public void TestConstructorDictionaryGeneric()
        {
            var dictionary = new Dictionary<string, object> { { "x", 1 } };
            var document = new BsonDocument(dictionary);
            Assert.AreEqual(1, document.ElementCount);
            Assert.AreEqual(1, document["x"].AsInt32);
            Assert.AreEqual(true, document.Contains("x"));
            Assert.AreEqual(true, document.ContainsValue(1));
        }

        [Test]
        public void TestConstructorDictionaryGenericWithKeys()
        {
            var dictionary = new Dictionary<string, object> { { "x", 1 }, { "y", 2 } };
            var keys = new string[] { "x" };
#pragma warning disable 618
            var document = new BsonDocument(dictionary, keys);
#pragma warning restore
            Assert.AreEqual(1, document.ElementCount);
            Assert.AreEqual(1, document["x"].AsInt32);
            Assert.AreEqual(true, document.Contains("x"));
            Assert.AreEqual(true, document.ContainsValue(1));
        }

        [Test]
        public void TestConstructorIDictionary()
        {
            var hashtable = (IDictionary)new Hashtable { { "x", 1 } };
            var document = new BsonDocument(hashtable);
            Assert.AreEqual(1, document.ElementCount);
            Assert.AreEqual(1, document["x"].AsInt32);
            Assert.AreEqual(true, document.Contains("x"));
            Assert.AreEqual(true, document.ContainsValue(1));
        }

        [Test]
        public void TestConstructorIDictionaryGeneric()
        {
            var dictionary = (IDictionary<string, object>)new Dictionary<string, object> { { "x", 1 } };
            var document = new BsonDocument(dictionary);
            Assert.AreEqual(1, document.ElementCount);
            Assert.AreEqual(1, document["x"].AsInt32);
            Assert.AreEqual(true, document.Contains("x"));
            Assert.AreEqual(true, document.ContainsValue(1));
        }

        [Test]
        public void TestConstructorIDictionaryGenericWithKeys()
        {
            var dictionary = (IDictionary<string, object>)new Dictionary<string, object> { { "x", 1 }, { "y", 2 } };
            var keys = new string[] { "x" };
#pragma warning disable 618
            var document = new BsonDocument(dictionary, keys);
#pragma warning restore
            Assert.AreEqual(1, document.ElementCount);
            Assert.AreEqual(1, document["x"].AsInt32);
            Assert.AreEqual(true, document.Contains("x"));
            Assert.AreEqual(true, document.ContainsValue(1));
        }

        [Test]
        public void TestConstructorIDictionaryWithKeys()
        {
            var hashtable = (IDictionary)new Hashtable { { "x", 1 }, { "y", 2 } };
            var keys = new string[] { "x" };
#pragma warning disable 618
            var document = new BsonDocument(hashtable, keys);
#pragma warning restore
            Assert.AreEqual(1, document.ElementCount);
            Assert.AreEqual(1, document["x"].AsInt32);
            Assert.AreEqual(true, document.Contains("x"));
            Assert.AreEqual(true, document.ContainsValue(1));
        }

        [Test]
        public void TestConstructorNameValue()
        {
            var document = new BsonDocument("x", 1);
            Assert.AreEqual(1, document.ElementCount);
            Assert.AreEqual(1, document["x"].AsInt32);
            Assert.AreEqual(true, document.Contains("x"));
            Assert.AreEqual(true, document.ContainsValue(1));
        }

        [Test]
        public void TestConstructorNoArgs()
        {
            var document = new BsonDocument();
            Assert.AreEqual(false, document.AllowDuplicateNames);
            Assert.IsInstanceOf<BsonDocument>(document.AsBsonDocument);
            Assert.AreEqual(BsonType.Document, document.BsonType);
            Assert.AreEqual(false, document.Contains("name"));
            Assert.AreEqual(false, document.ContainsValue(0));
            Assert.AreEqual(0, document.ElementCount);
            Assert.AreEqual(0, document.Elements.Count());
            Assert.AreEqual(false, document.IsBsonArray);
            Assert.AreEqual(true, document.IsBsonDocument);
            Assert.AreEqual(0, document.Names.Count());
#pragma warning disable 618
            Assert.AreEqual(null, document.RawValue);
            Assert.AreEqual(0, document.RawValues.Count());
#pragma warning restore
            Assert.AreEqual(true, document.ToBoolean());
            Assert.AreSame(document, document.ToBsonDocument());
            Assert.AreEqual("{ }", document.ToJson());
            Assert.AreEqual(0, document.Values.Count());
        }

        [Test]
        public void TestContains()
        {
            var document = new BsonDocument();
            Assert.IsFalse(document.Contains("x"));
            document["x"] = 1;
            Assert.IsTrue(document.Contains("x"));
        }

        [Test]
        public void TestContainsValue()
        {
            var document = new BsonDocument();
            Assert.IsFalse(document.ContainsValue(1));
            document["x"] = 1;
            Assert.IsTrue(document.ContainsValue(1));
        }

        [Test]
        public void TestCreateFromDictionary()
        {
            var dictionary = new Dictionary<string, object> { { "x", 1 }, { "n", null }, { "a", new object[] { 1, null } } }; // null will be mapped to BsonNull.Value
            var document = new BsonDocument(dictionary);
            Assert.AreEqual(3, document.ElementCount);
            Assert.AreEqual(1, document["x"].AsInt32);
            Assert.AreSame(BsonNull.Value, document["n"]);
            Assert.IsTrue(document["a"].IsBsonArray);
            Assert.AreEqual(2, document["a"].AsBsonArray.Count);
            Assert.AreEqual(1, document["a"][0].AsInt32);
            Assert.AreSame(BsonNull.Value, document["a"][1]);
            Assert.AreEqual(true, document.Contains("x"));
            Assert.AreEqual(true, document.Contains("n"));
            Assert.AreEqual(true, document.Contains("a"));
            Assert.AreEqual(true, document.ContainsValue(1));
            Assert.AreEqual(true, document.ContainsValue(BsonNull.Value));
        }

        [Test]
        public void TestCreateNull()
        {
            var document = BsonDocument.Create(null);
            Assert.IsNull(document);
        }

        [Test]
        public void TestDeepClone()
        {
            var document = new BsonDocument("d", new BsonDocument("x", 1));
            var clone = (BsonDocument)document.DeepClone();
            Assert.AreEqual(clone, document);
            Assert.AreNotSame(clone["d"], document["d"]);
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
        public void TestElementNameZeroLength()
        {
            var document = new BsonDocument("", "zero length");
            Assert.AreEqual(0, document.GetElement(0).Name.Length);
        }

        [Test]
        public void TestGetDocumentId()
        {
            var document = new BsonDocument("_id", 1);
            object id;
            Type nominalType;
            IIdGenerator idGenerator;
            Assert.IsTrue(((IBsonIdProvider)BsonDocumentSerializer.Instance).GetDocumentId(document, out id, out nominalType, out idGenerator));
            Assert.IsInstanceOf<BsonInt32>(id);
            Assert.AreEqual(new BsonInt32(1), id);
            Assert.AreEqual(typeof(BsonValue), nominalType);
            Assert.IsNull(idGenerator);
        }

        [Test]
        public void TestGetDocumentIdWhenIdIsGuid()
        {
            var document = new BsonDocument("_id", Guid.Empty);
            object id;
            Type nominalType;
            IIdGenerator idGenerator;
            Assert.IsTrue(((IBsonIdProvider)BsonDocumentSerializer.Instance).GetDocumentId(document, out id, out nominalType, out idGenerator));
            Assert.IsInstanceOf<BsonBinaryData>(id);
            Assert.AreEqual(new BsonBinaryData(Guid.Empty), id);
            Assert.AreEqual(typeof(BsonValue), nominalType);
            Assert.IsInstanceOf<BsonBinaryDataGuidGenerator>(idGenerator);
        }

        [Test]
        public void TestGetDocumentIdWhenIdIsMissing()
        {
            var document = new BsonDocument();
            object id;
            Type nominalType;
            IIdGenerator idGenerator;
            Assert.IsTrue(((IBsonIdProvider)BsonDocumentSerializer.Instance).GetDocumentId(document, out id, out nominalType, out idGenerator));
            Assert.IsNull(id);
            Assert.AreEqual(typeof(BsonValue), nominalType);
            Assert.IsInstanceOf<BsonObjectIdGenerator>(idGenerator);
        }

        [Test]
        public void TestGetHashCode()
        {
            var document = new BsonDocument("x", 1);
            var hashCode = document.GetHashCode();
            Assert.AreEqual(hashCode, document.GetHashCode()); // returns same value when called again
        }

        [Test]
        public void TestGetValueByIndex()
        {
            var document = new BsonDocument("x", 1);
            Assert.AreEqual(1, document.GetValue(0).AsInt32);
        }

        [Test]
        public void TestGetValueByName()
        {
            var document = new BsonDocument("x", 1);
            Assert.AreEqual(1, document.GetValue("x").AsInt32);
        }

        [Test]
        public void TestGetValueByNameWithDefaultValue()
        {
            var document = new BsonDocument("x", 1);
            Assert.AreEqual(1, document.GetValue("x", 2).AsInt32);
            Assert.AreEqual(2, document.GetValue("y", 2).AsInt32);
        }

        [Test]
        public void TestIndexer()
        {
            var document = new BsonDocument();
            Assert.AreEqual(0, document.ElementCount);
            document["x"] = 1;
            Assert.AreEqual(1, document.ElementCount);
            Assert.AreEqual(1, document["x"].AsInt32);
            Assert.AreEqual(1, document[0].AsInt32);
            document["y"] = 2;
            Assert.AreEqual(2, document.ElementCount);
            Assert.AreEqual(2, document["y"].AsInt32);
            Assert.AreEqual(2, document[1].AsInt32);
            document["y"] = 3;
            Assert.AreEqual(2, document.ElementCount);
            Assert.AreEqual(3, document["y"].AsInt32);
            Assert.AreEqual(3, document[1].AsInt32);
            document[1] = 4;
            Assert.AreEqual(2, document.ElementCount);
            Assert.AreEqual(4, document["y"].AsInt32);
            Assert.AreEqual(4, document[1].AsInt32);
        }

        [Test]
        public void TestIndexerWithDefaultValue()
        {
#pragma warning disable 618
            var document = new BsonDocument();
            Assert.AreEqual(0, document["x", 0].AsInt32);
            document["x"] = 1;
            Assert.AreEqual(1, document["x", 1].AsInt32);
#pragma warning restore
        }

        [Test]
        public void TestInsertAt()
        {
            var document = new BsonDocument();
            document.InsertAt(0, new BsonElement("x", 1));
            Assert.AreEqual("x", document.GetElement(0).Name);
            Assert.AreEqual(1, document[0].AsInt32);
            document.InsertAt(0, new BsonElement("y", 2));
            Assert.AreEqual("y", document.GetElement(0).Name);
            Assert.AreEqual(2, document[0].AsInt32);
            Assert.AreEqual("x", document.GetElement(1).Name);
            Assert.AreEqual(1, document[1].AsInt32);
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
#pragma warning disable 618
            Assert.AreEqual(null, (bool?)document["x", null]);
            Assert.AreEqual(null, (bool?)document["x", (bool?)null]);
            Assert.AreEqual(null, (bool?)document["x", BsonNull.Value]);
#pragma warning restore
            Assert.AreEqual(true, document["v"].AsNullableBoolean);
            Assert.AreEqual(null, document["n"].AsNullableBoolean);
#pragma warning disable 618
            Assert.AreEqual(null, document["x", (bool?)null].AsNullableBoolean);
            Assert.AreEqual(null, document["x", BsonNull.Value].AsNullableBoolean);
#pragma warning restore
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
#pragma warning disable 618
            Assert.AreEqual(null, (DateTime?)document["x", null]);
            Assert.AreEqual(null, (DateTime?)document["x", (DateTime?)null]);
            Assert.AreEqual(null, (DateTime?)document["x", BsonNull.Value]);
#pragma warning restore
            Assert.AreEqual(utcNowTruncated, document["v"].ToNullableUniversalTime());
            Assert.AreEqual(null, document["n"].ToNullableUniversalTime());
#pragma warning disable 618
            Assert.AreEqual(null, document["x", (DateTime?)null].ToNullableUniversalTime());
            Assert.AreEqual(null, document["x", BsonNull.Value].ToNullableUniversalTime());
#pragma warning restore
            Assert.Throws<InvalidCastException>(() => { var v = (DateTime?)document["s"]; });
#pragma warning disable 618
            Assert.Throws<InvalidCastException>(() => { var v = document["s"].AsNullableDateTime; });
#pragma warning restore
            Assert.Throws<NotSupportedException>(() => { var v = document["s"].ToNullableUniversalTime(); });
        }

        [Test]
        public void TestNullableDouble()
        {
            var document = new BsonDocument { { "v", 1.5 }, { "n", BsonNull.Value }, { "s", "" } };
            Assert.AreEqual(1.5, (double?)document["v"]);
            Assert.AreEqual(null, (double?)document["n"]);
#pragma warning disable 618
            Assert.AreEqual(null, (double?)document["x", null]);
            Assert.AreEqual(null, (double?)document["x", (double?)null]);
            Assert.AreEqual(null, (double?)document["x", BsonNull.Value]);
#pragma warning restore
            Assert.AreEqual(1.5, document["v"].AsNullableDouble);
            Assert.AreEqual(null, document["n"].AsNullableDouble);
#pragma warning disable 618
            Assert.AreEqual(null, document["x", (double?)null].AsNullableDouble);
            Assert.AreEqual(null, document["x", BsonNull.Value].AsNullableDouble);
#pragma warning restore
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
#pragma warning disable 618
            Assert.AreEqual(null, (Guid?)document["x", null]);
            Assert.AreEqual(null, (Guid?)document["x", (Guid?)null]);
            Assert.AreEqual(null, (Guid?)document["x", BsonNull.Value]);
#pragma warning restore
            Assert.AreEqual(guid, document["v"].AsNullableGuid);
            Assert.AreEqual(null, document["n"].AsNullableGuid);
#pragma warning disable 618
            Assert.AreEqual(null, document["x", (Guid?)null].AsNullableGuid);
            Assert.AreEqual(null, document["x", BsonNull.Value].AsNullableGuid);
#pragma warning restore
            Assert.Throws<InvalidCastException>(() => { var v = (Guid?)document["s"]; });
            Assert.Throws<InvalidCastException>(() => { var v = document["s"].AsNullableGuid; });
        }

        [Test]
        public void TestNullableInt32()
        {
            var document = new BsonDocument { { "v", 1 }, { "n", BsonNull.Value }, { "s", "" } };
            Assert.AreEqual(1, (int?)document["v"]);
            Assert.AreEqual(null, (int?)document["n"]);
#pragma warning disable 618
            Assert.AreEqual(null, (int?)document["x", null]);
            Assert.AreEqual(null, (int?)document["x", (int?)null]);
            Assert.AreEqual(null, (int?)document["x", BsonNull.Value]);
#pragma warning restore
            Assert.AreEqual(1, document["v"].AsNullableInt32);
            Assert.AreEqual(null, document["n"].AsNullableInt32);
#pragma warning disable 618
            Assert.AreEqual(null, document["x", (int?)null].AsNullableInt32);
            Assert.AreEqual(null, document["x", BsonNull.Value].AsNullableInt32);
#pragma warning restore
            Assert.Throws<InvalidCastException>(() => { var v = (int?)document["s"]; });
            Assert.Throws<InvalidCastException>(() => { var v = document["s"].AsNullableInt32; });
        }

        [Test]
        public void TestNullableInt64()
        {
            var document = new BsonDocument { { "v", 1L }, { "n", BsonNull.Value }, { "s", "" } };
            Assert.AreEqual(1L, (long?)document["v"]);
            Assert.AreEqual(null, (long?)document["n"]);
#pragma warning disable 618
            Assert.AreEqual(null, (long?)document["x", null]);
            Assert.AreEqual(null, (long?)document["x", (long?)null]);
            Assert.AreEqual(null, (long?)document["x", BsonNull.Value]);
#pragma warning restore
            Assert.AreEqual(1L, document["v"].AsNullableInt64);
            Assert.AreEqual(null, document["n"].AsNullableInt64);
#pragma warning disable 618
            Assert.AreEqual(null, document["x", (long?)null].AsNullableInt64);
            Assert.AreEqual(null, document["x", BsonNull.Value].AsNullableInt64);
#pragma warning restore
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
#pragma warning disable 618
            Assert.AreEqual(null, (ObjectId?)document["x", null]);
            Assert.AreEqual(null, (ObjectId?)document["x", (ObjectId?)null]);
            Assert.AreEqual(null, (ObjectId?)document["x", BsonNull.Value]);
#pragma warning restore
            Assert.AreEqual(objectId, document["v"].AsNullableObjectId);
            Assert.AreEqual(null, document["n"].AsNullableObjectId);
#pragma warning disable 618
            Assert.AreEqual(null, document["x", (ObjectId?)null].AsNullableObjectId);
            Assert.AreEqual(null, document["x", BsonNull.Value].AsNullableObjectId);
#pragma warning restore
            Assert.Throws<InvalidCastException>(() => { var v = (ObjectId?)document["s"]; });
            Assert.Throws<InvalidCastException>(() => { var v = document["s"].AsNullableObjectId; });
        }

        [Test]
        public void TestOperatorEqual()
        {
            var document1 = new BsonDocument("x", 1);
            var document2 = new BsonDocument("x", 1);
            Assert.AreNotSame(document1, document2);
            Assert.IsTrue(document1 == document2);
        }

        [Test]
        public void TestOperatorEqualBothNull()
        {
            BsonDocument document1 = null;
            BsonDocument document2 = null;
            Assert.IsTrue(document1 == document2);
        }

        [Test]
        public void TestOperatorEqualLhsNull()
        {
            var document = new BsonDocument();
            Assert.IsFalse(null == document);
        }

        [Test]
        public void TestOperatorEqualRhsNull()
        {
            var document = new BsonDocument();
            Assert.IsFalse(document == null);
        }

        [Test]
        public void TestOperatorNotEqual()
        {
            var document1 = new BsonDocument("x", 1);
            var document2 = new BsonDocument("x", 1);
            Assert.AreNotSame(document1, document2);
            Assert.IsFalse(document1 != document2);
        }

        [Test]
        public void TestOperatorNotEqualBothNull()
        {
            BsonDocument document1 = null;
            BsonDocument document2 = null;
            Assert.IsFalse(document1 != document2);
        }

        [Test]
        public void TestOperatorNotEqualLhsNull()
        {
            var document = new BsonDocument();
            Assert.IsTrue(null != document);
        }

        [Test]
        public void TestOperatorNotEqualRhsNull()
        {
            var document = new BsonDocument();
            Assert.IsTrue(document != null);
        }

        [Test]
        public void TestParse()
        {
            var json = "{ a : 1, b : 'abc' }";
            var document = BsonDocument.Parse(json);
        }

        [Test]
        public void TestRemove()
        {
            var document = new BsonDocument("x", 1);
            Assert.AreEqual(1, document.ElementCount);
            document.Remove("x");
            Assert.AreEqual(0, document.ElementCount);
        }

        [Test]
        public void TestRemoveAt()
        {
            var document = new BsonDocument { { "x", 1 }, { "y", 2 } };
            Assert.AreEqual(2, document.ElementCount);
            document.RemoveAt(0);
            Assert.AreEqual(1, document.ElementCount);
            Assert.AreEqual(2, document["y"].AsInt32);
        }

        [Test]
        public void TestRemoveElement()
        {
            var document = new BsonDocument { { "x", 1 }, { "y", 2 } };
            Assert.AreEqual(2, document.ElementCount);
            document.RemoveElement(document.GetElement(0));
            Assert.AreEqual(1, document.ElementCount);
            Assert.AreEqual(2, document["y"].AsInt32);
        }

        [Test]
        public void TestSetByIndex()
        {
            var document = new BsonDocument("x", 1);
            Assert.AreEqual(1, document.ElementCount);
            Assert.AreEqual(1, document["x"].AsInt32);
            document.Set(0, 2);
            Assert.AreEqual(1, document.ElementCount);
            Assert.AreEqual(2, document["x"].AsInt32);
        }

        [Test]
        public void TestSetByName()
        {
            var document = new BsonDocument();
            Assert.AreEqual(0, document.ElementCount);
            document.Set("x", 1);
            Assert.AreEqual(1, document.ElementCount);
            Assert.AreEqual(1, document["x"].AsInt32);
            document.Set("x", 2);
            Assert.AreEqual(1, document.ElementCount);
            Assert.AreEqual(2, document["x"].AsInt32);
        }

        [Test]
        public void TestSetDocumentId()
        {
            var document = new BsonDocument("_id", 1);
            ((IBsonIdProvider)BsonDocumentSerializer.Instance).SetDocumentId(document, BsonValue.Create(2));
            Assert.AreEqual(2, document["_id"].AsInt32);
        }

        [Test]
        public void TestSetDocumentIdNewElement()
        {
            var document = new BsonDocument("x", 1);
            ((IBsonIdProvider)BsonDocumentSerializer.Instance).SetDocumentId(document, BsonValue.Create(2));
            Assert.AreEqual(2, document.ElementCount);
            Assert.AreEqual("_id", document.GetElement(0).Name);
            Assert.AreEqual(2, document["_id"].AsInt32);
        }

        [Test]
        public void TestSetElementByIndex()
        {
            var document = new BsonDocument("x", 1);
            var element = new BsonElement("y", 2);
            document.SetElement(0, element);
            Assert.AreEqual(1, document.ElementCount);
            Assert.AreEqual("y", document.GetElement(0).Name);
            Assert.AreEqual(2, document["y"].AsInt32);
        }

        [Test]
        public void TestSetElementNewElement()
        {
            var document = new BsonDocument("x", 1);
            var element = new BsonElement("y", 2);
            document.SetElement(element);
            Assert.AreEqual(2, document.ElementCount);
            Assert.AreEqual("x", document.GetElement(0).Name);
            Assert.AreEqual(1, document["x"].AsInt32);
            Assert.AreEqual("y", document.GetElement(1).Name);
            Assert.AreEqual(2, document["y"].AsInt32);
        }

        [Test]
        public void TestSetElementReplaceElement()
        {
            var document = new BsonDocument("x", 1);
            var element = new BsonElement("x", 2);
            document.SetElement(element);
            Assert.AreEqual(1, document.ElementCount);
            Assert.AreEqual("x", document.GetElement(0).Name);
            Assert.AreEqual(2, document["x"].AsInt32);
        }

        [Test]
        public void TestSpecBsonAwesomeWithBsonDocument()
        {
            // this test is from http://bsonspec.org/#/specification
            BsonDocument document = new BsonDocument("BSON", new BsonArray { "awesome", 5.05, 1986 });
            byte[] bson = document.ToBson();
            AssertAreEqual(@"1\x00\x00\x00\x04BSON\x00&\x00\x00\x00\x020\x00\x08\x00\x00\x00awesome\x00\x011\x00333333\x14@\x102\x00\xc2\x07\x00\x00\x00\x00", bson);
        }

        [Test]
        public void TestSpecBsonAwesomeWithBsonWriter()
        {
            // this test is from http://bsonspec.org/#/specification
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
        public void TestSpecHelloWorldWithBsonDocument()
        {
            // this test is from http://bsonspec.org/#/specification
            BsonDocument document = new BsonDocument("hello", "world");
            byte[] bson = document.ToBson();
            AssertAreEqual(@"\x16\x00\x00\x00\x02hello\x00\x06\x00\x00\x00world\x00\x00", bson);
        }

        [Test]
        public void TestSpecHelloWorldWithBsonWriter()
        {
            // this test is from http://bsonspec.org/#/specification
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
        public void TestToBsonDocument()
        {
            var document = new BsonDocument();
            Assert.AreSame(document, document.ToBsonDocument());
            Assert.AreSame(document, ((IConvertibleToBsonDocument)document).ToBsonDocument());
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
            Assert.AreEqual(9, dictionary.Count);
            Assert.IsInstanceOf<BsonJavaScript>(dictionary["JavaScript"]);
            Assert.IsInstanceOf<BsonJavaScriptWithScope>(dictionary["JavaScriptWithScope"]);
            Assert.AreSame(BsonMaxKey.Value, dictionary["MaxKey"]);
            Assert.AreSame(BsonMinKey.Value, dictionary["MinKey"]);
            Assert.IsNull(dictionary["Null"]);
            Assert.IsInstanceOf<BsonRegularExpression>(dictionary["RegularExpression"]);
            Assert.IsInstanceOf<BsonSymbol>(dictionary["Symbol"]);
            Assert.IsInstanceOf<BsonTimestamp>(dictionary["Timestamp"]);
            Assert.AreSame(BsonUndefined.Value, dictionary["Undefined"]);
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
            Assert.AreEqual(9, hashtable.Count);
            Assert.IsInstanceOf<BsonJavaScript>(hashtable["JavaScript"]);
            Assert.IsInstanceOf<BsonJavaScriptWithScope>(hashtable["JavaScriptWithScope"]);
            Assert.AreSame(BsonMaxKey.Value, hashtable["MaxKey"]);
            Assert.AreSame(BsonMinKey.Value, hashtable["MinKey"]);
            Assert.IsNull(hashtable["Null"]);
            Assert.IsInstanceOf<BsonRegularExpression>(hashtable["RegularExpression"]);
            Assert.IsInstanceOf<BsonSymbol>(hashtable["Symbol"]);
            Assert.IsInstanceOf<BsonTimestamp>(hashtable["Timestamp"]);
            Assert.AreSame(BsonUndefined.Value, hashtable["Undefined"]);
        }

        [Test]
        public void TestTryGetElement()
        {
            var document = new BsonDocument();
            BsonElement element;
            Assert.IsFalse(document.TryGetElement("x", out element));
            document["x"] = 1;
            Assert.IsTrue(document.TryGetElement("x", out element));
            Assert.AreEqual("x", element.Name);
            Assert.AreEqual(1, element.Value.AsInt32);
        }

        [Test]
        public void TestTryGetValue()
        {
            var document = new BsonDocument();
            BsonValue value;
            Assert.IsFalse(document.TryGetValue("x", out value));
            document["x"] = 1;
            Assert.IsTrue(document.TryGetValue("x", out value));
            Assert.AreEqual(1, value.AsInt32);
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
