/* Copyright 2010-2013 10gen Inc.
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
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Serialization.DictionarySerializers
{
    [BsonDiscriminator("DictionarySerializers.C")] // "C" is an ambiguous discriminator when nominalType is System.Object
    public class C
    {
        public string P { get; set; }
    }

    [TestFixture]
    public class DictionarySerializerTests
    {
        public class T
        {
            public Hashtable HT { get; set; }
            public IDictionary ID { get; set; }
            public ListDictionary LD { get; set; }
            public OrderedDictionary OD { get; set; }
            public SortedList SL { get; set; }
        }

        [Test]
        public void TestNull()
        {
            var obj = new T { HT = null, ID = null, LD = null, OD = null, SL = null };
            var json = obj.ToJson();
            var rep = "null";
            var expected = "{ 'HT' : #R, 'ID' : #R, 'LD' : #R, 'OD' : #R, 'SL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsNull(rehydrated.HT);
            Assert.IsNull(rehydrated.ID);
            Assert.IsNull(rehydrated.LD);
            Assert.IsNull(rehydrated.OD);
            Assert.IsNull(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestEmpty()
        {
            var ht = new Hashtable();
            var ld = CreateListDictionary(ht);
            var od = CreateOrderedDictionary(ht);
            var sl = CreateSortedList(ht);
            var obj = new T { HT = ht, ID = ht, LD = ld, OD = od, SL = sl };
            var json = obj.ToJson();
            var rep = "{ }";
            var expected = "{ 'HT' : #R, 'ID' : #R, 'LD' : #R, 'OD' : #R, 'SL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<Hashtable>(rehydrated.HT);
            Assert.IsInstanceOf<Hashtable>(rehydrated.ID);
            Assert.IsInstanceOf<ListDictionary>(rehydrated.LD);
            Assert.IsInstanceOf<OrderedDictionary>(rehydrated.OD);
            Assert.IsInstanceOf<SortedList>(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOneC()
        {
            var ht = new Hashtable { { "A", new C { P = "x" } } };
            var ld = CreateListDictionary(ht);
            var od = CreateOrderedDictionary(ht);
            var sl = CreateSortedList(ht);
            var obj = new T { HT = ht, ID = ht, LD = ld, OD = od, SL = sl };
            var json = obj.ToJson();
            var rep = "{ 'A' : { '_t' : 'DictionarySerializers.C', 'P' : 'x' } }";
            var expected = "{ 'HT' : #R, 'ID' : #R, 'LD' : #R, 'OD' : #R, 'SL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<Hashtable>(rehydrated.HT);
            Assert.IsInstanceOf<Hashtable>(rehydrated.ID);
            Assert.IsInstanceOf<ListDictionary>(rehydrated.LD);
            Assert.IsInstanceOf<OrderedDictionary>(rehydrated.OD);
            Assert.IsInstanceOf<SortedList>(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOneInt()
        {
            var ht = new Hashtable { { "A", 1 } };
            var ld = CreateListDictionary(ht);
            var od = CreateOrderedDictionary(ht);
            var sl = CreateSortedList(ht);
            var obj = new T { HT = ht, ID = ht, LD = ld, OD = od, SL = sl };
            var json = obj.ToJson();
            var rep = "{ 'A' : 1 }";
            var expected = "{ 'HT' : #R, 'ID' : #R, 'LD' : #R, 'OD' : #R, 'SL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<Hashtable>(rehydrated.HT);
            Assert.IsInstanceOf<Hashtable>(rehydrated.ID);
            Assert.IsInstanceOf<ListDictionary>(rehydrated.LD);
            Assert.IsInstanceOf<OrderedDictionary>(rehydrated.OD);
            Assert.IsInstanceOf<SortedList>(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOneIntWithIntKey()
        {
            var ht = new Hashtable { { 1, 2 } };
            var ld = CreateListDictionary(ht);
            var od = CreateOrderedDictionary(ht);
            var sl = CreateSortedList(ht);
            var obj = new T { HT = ht, ID = ht, LD = ld, OD = od, SL = sl };
            var json = obj.ToJson();
            var rep = "[[1, 2]]";
            var expected = "{ 'HT' : #R, 'ID' : #R, 'LD' : #R, 'OD' : #R, 'SL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<Hashtable>(rehydrated.HT);
            Assert.IsInstanceOf<Hashtable>(rehydrated.ID);
            Assert.IsInstanceOf<ListDictionary>(rehydrated.LD);
            Assert.IsInstanceOf<OrderedDictionary>(rehydrated.OD);
            Assert.IsInstanceOf<SortedList>(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOneString()
        {
            var ht = new Hashtable { { "A", "x" } };
            var ld = CreateListDictionary(ht);
            var od = CreateOrderedDictionary(ht);
            var sl = CreateSortedList(ht);
            var obj = new T { HT = ht, ID = ht, LD = ld, OD = od, SL = sl };
            var json = obj.ToJson();
            var rep = "{ 'A' : 'x' }";
            var expected = "{ 'HT' : #R, 'ID' : #R, 'LD' : #R, 'OD' : #R, 'SL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<Hashtable>(rehydrated.HT);
            Assert.IsInstanceOf<Hashtable>(rehydrated.ID);
            Assert.IsInstanceOf<ListDictionary>(rehydrated.LD);
            Assert.IsInstanceOf<OrderedDictionary>(rehydrated.OD);
            Assert.IsInstanceOf<SortedList>(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOneStringWithIntKey()
        {
            var ht = new Hashtable { { 1, "x" } };
            var ld = CreateListDictionary(ht);
            var od = CreateOrderedDictionary(ht);
            var sl = CreateSortedList(ht);
            var obj = new T { HT = ht, ID = ht, LD = ld, OD = od, SL = sl };
            var json = obj.ToJson();
            var rep = "[[1, 'x']]";
            var expected = "{ 'HT' : #R, 'ID' : #R, 'LD' : #R, 'OD' : #R, 'SL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<Hashtable>(rehydrated.HT);
            Assert.IsInstanceOf<Hashtable>(rehydrated.ID);
            Assert.IsInstanceOf<ListDictionary>(rehydrated.LD);
            Assert.IsInstanceOf<OrderedDictionary>(rehydrated.OD);
            Assert.IsInstanceOf<SortedList>(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestTwoCs()
        {
            var ht = new Hashtable { { "A", new C { P = "x" } }, { "B", new C { P = "y" } } };
            var ld = CreateListDictionary(ht);
            var od = CreateOrderedDictionary(ht);
            var sl = CreateSortedList(ht);
            var obj = new T { HT = ht, ID = ht, LD = ld, OD = od, SL = sl };
            var json = obj.ToJson();
            var reps = new Hashtable
            {
                { "A", "{ '_t' : 'DictionarySerializers.C', 'P' : 'x' }"},
                { "B", "{ '_t' : 'DictionarySerializers.C', 'P' : 'y' }"}
            };
            var htRep = GetDocumentRepresentationInKeyOrder(ht, reps);
            var ldRep = GetDocumentRepresentationInKeyOrder(ld, reps);
            var odRep = GetDocumentRepresentationInKeyOrder(od, reps);
            var slRep = GetDocumentRepresentationInKeyOrder(sl, reps);
            var expected = "{ 'HT' : #HT, 'ID' : #HT, 'LD' : #LD, 'OD' : #OD, 'SL' : #SL }";
            expected = expected.Replace("#HT", htRep);
            expected = expected.Replace("#LD", ldRep);
            expected = expected.Replace("#OD", odRep);
            expected = expected.Replace("#SL", slRep);
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<Hashtable>(rehydrated.HT);
            Assert.IsInstanceOf<Hashtable>(rehydrated.ID);
            Assert.IsInstanceOf<ListDictionary>(rehydrated.LD);
            Assert.IsInstanceOf<OrderedDictionary>(rehydrated.OD);
            Assert.IsInstanceOf<SortedList>(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestTwoCsWithIntKeys()
        {
            var ht = new Hashtable { { 1, new C { P = "x" } }, { 2, new C { P = "y" } } };
            var ld = CreateListDictionary(ht);
            var od = CreateOrderedDictionary(ht);
            var sl = CreateSortedList(ht);
            var obj = new T { HT = ht, ID = ht, LD = ld, OD = od, SL = sl };
            var json = obj.ToJson();
            var reps = new Hashtable
            {
                { 1, "[1, { '_t' : 'DictionarySerializers.C', 'P' : 'x' }]"},
                { 2, "[2, { '_t' : 'DictionarySerializers.C', 'P' : 'y' }]"}
            };
            var htRep = GetArrayRepresentationInKeyOrder(ht, reps);
            var ldRep = GetArrayRepresentationInKeyOrder(ld, reps);
            var odRep = GetArrayRepresentationInKeyOrder(od, reps);
            var slRep = GetArrayRepresentationInKeyOrder(sl, reps);
            var expected = "{ 'HT' : #HT, 'ID' : #HT, 'LD' : #LD, 'OD' : #OD, 'SL' : #SL }";
            expected = expected.Replace("#HT", htRep);
            expected = expected.Replace("#LD", ldRep);
            expected = expected.Replace("#OD", odRep);
            expected = expected.Replace("#SL", slRep);
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<Hashtable>(rehydrated.HT);
            Assert.IsInstanceOf<Hashtable>(rehydrated.ID);
            Assert.IsInstanceOf<ListDictionary>(rehydrated.LD);
            Assert.IsInstanceOf<OrderedDictionary>(rehydrated.OD);
            Assert.IsInstanceOf<SortedList>(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestTwoInts()
        {
            var ht = new Hashtable { { "A", 1 }, { "B", 2 } };
            var ld = CreateListDictionary(ht);
            var od = CreateOrderedDictionary(ht);
            var sl = CreateSortedList(ht);
            var obj = new T { HT = ht, ID = ht, LD = ld, OD = od, SL = sl };
            var json = obj.ToJson();
            var reps = new Hashtable
            {
                { "A", "1"},
                { "B", "2"}
            };
            var htRep = GetDocumentRepresentationInKeyOrder(ht, reps);
            var ldRep = GetDocumentRepresentationInKeyOrder(ld, reps);
            var odRep = GetDocumentRepresentationInKeyOrder(od, reps);
            var slRep = GetDocumentRepresentationInKeyOrder(sl, reps);
            var expected = "{ 'HT' : #HT, 'ID' : #HT, 'LD' : #LD, 'OD' : #OD, 'SL' : #SL }";
            expected = expected.Replace("#HT", htRep);
            expected = expected.Replace("#LD", ldRep);
            expected = expected.Replace("#OD", odRep);
            expected = expected.Replace("#SL", slRep);
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<Hashtable>(rehydrated.HT);
            Assert.IsInstanceOf<Hashtable>(rehydrated.ID);
            Assert.IsInstanceOf<ListDictionary>(rehydrated.LD);
            Assert.IsInstanceOf<OrderedDictionary>(rehydrated.OD);
            Assert.IsInstanceOf<SortedList>(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestTwoIntsWithIntKeys()
        {
            var ht = new Hashtable { { 1, 2 }, { 3, 4 } };
            var ld = CreateListDictionary(ht);
            var od = CreateOrderedDictionary(ht);
            var sl = CreateSortedList(ht);
            var obj = new T { HT = ht, ID = ht, LD = ld, OD = od, SL = sl };
            var json = obj.ToJson();
            var reps = new Hashtable
            {
                { 1, "[1, 2]"},
                { 3, "[3, 4]"}
            };
            var htRep = GetArrayRepresentationInKeyOrder(ht, reps);
            var ldRep = GetArrayRepresentationInKeyOrder(ld, reps);
            var odRep = GetArrayRepresentationInKeyOrder(od, reps);
            var slRep = GetArrayRepresentationInKeyOrder(sl, reps);
            var expected = "{ 'HT' : #HT, 'ID' : #HT, 'LD' : #LD, 'OD' : #OD, 'SL' : #SL }";
            expected = expected.Replace("#HT", htRep);
            expected = expected.Replace("#LD", ldRep);
            expected = expected.Replace("#OD", odRep);
            expected = expected.Replace("#SL", slRep);
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<Hashtable>(rehydrated.HT);
            Assert.IsInstanceOf<Hashtable>(rehydrated.ID);
            Assert.IsInstanceOf<ListDictionary>(rehydrated.LD);
            Assert.IsInstanceOf<OrderedDictionary>(rehydrated.OD);
            Assert.IsInstanceOf<SortedList>(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestTwoStrings()
        {
            var ht = new Hashtable { { "A", "x" }, { "B", "y" } };
            var ld = CreateListDictionary(ht);
            var od = CreateOrderedDictionary(ht);
            var sl = CreateSortedList(ht);
            var obj = new T { HT = ht, ID = ht, LD = ld, OD = od, SL = sl };
            var json = obj.ToJson();
            var reps = new Hashtable
            {
                { "A", "'x'"},
                { "B", "'y'"}
            };
            var htRep = GetDocumentRepresentationInKeyOrder(ht, reps);
            var ldRep = GetDocumentRepresentationInKeyOrder(ld, reps);
            var odRep = GetDocumentRepresentationInKeyOrder(od, reps);
            var slRep = GetDocumentRepresentationInKeyOrder(sl, reps);
            var expected = "{ 'HT' : #HT, 'ID' : #HT, 'LD' : #LD, 'OD' : #OD, 'SL' : #SL }";
            expected = expected.Replace("#HT", htRep);
            expected = expected.Replace("#LD", ldRep);
            expected = expected.Replace("#OD", odRep);
            expected = expected.Replace("#SL", slRep);
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<Hashtable>(rehydrated.HT);
            Assert.IsInstanceOf<Hashtable>(rehydrated.ID);
            Assert.IsInstanceOf<ListDictionary>(rehydrated.LD);
            Assert.IsInstanceOf<OrderedDictionary>(rehydrated.OD);
            Assert.IsInstanceOf<SortedList>(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestTwoStringsWithIntKeys()
        {
            var ht = new Hashtable { { 1, "x" }, { 2, "y" } };
            var ld = CreateListDictionary(ht);
            var od = CreateOrderedDictionary(ht);
            var sl = CreateSortedList(ht);
            var obj = new T { HT = ht, ID = ht, LD = ld, OD = od, SL = sl };
            var json = obj.ToJson();
            var reps = new Hashtable
            {
                { 1, "[1, 'x']"},
                { 2, "[2, 'y']"}
            };
            var htRep = GetArrayRepresentationInKeyOrder(ht, reps);
            var ldRep = GetArrayRepresentationInKeyOrder(ld, reps);
            var odRep = GetArrayRepresentationInKeyOrder(od, reps);
            var slRep = GetArrayRepresentationInKeyOrder(sl, reps);
            var expected = "{ 'HT' : #HT, 'ID' : #HT, 'LD' : #LD, 'OD' : #OD, 'SL' : #SL }";
            expected = expected.Replace("#HT", htRep);
            expected = expected.Replace("#LD", ldRep);
            expected = expected.Replace("#OD", odRep);
            expected = expected.Replace("#SL", slRep);
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<Hashtable>(rehydrated.HT);
            Assert.IsInstanceOf<Hashtable>(rehydrated.ID);
            Assert.IsInstanceOf<ListDictionary>(rehydrated.LD);
            Assert.IsInstanceOf<OrderedDictionary>(rehydrated.OD);
            Assert.IsInstanceOf<SortedList>(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMixedPrimitiveTypes()
        {
            var dateTime = DateTime.SpecifyKind(new DateTime(2010, 1, 1, 11, 22, 33), DateTimeKind.Utc);
            var isoDate = dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFZ");
            var guid = Guid.Empty;
            var objectId = ObjectId.Empty;
            var ht = new Hashtable
            {
                { "A", true },
                { "B", dateTime },
                { "C", 1.5 },
                { "D", 1 },
                { "E", 2L },
                { "F", guid },
                { "G", objectId },
                { "H", "x" }
            };
            var ld = CreateListDictionary(ht);
            var od = CreateOrderedDictionary(ht);
            var sl = CreateSortedList(ht);
            var obj = new T { HT = ht, ID = ht, LD = ld, OD = od, SL = sl };
            var json = obj.ToJson();
            var reps = new Hashtable
            {
                { "A", "true" },
                { "B", "ISODate('#')".Replace("#", isoDate) },
                { "C", "1.5" },
                { "D", "1" },
                { "E", "NumberLong(2)" },
                { "F", "CSUUID('00000000-0000-0000-0000-000000000000')" },
                { "G", "ObjectId('000000000000000000000000')" },
                { "H", "'x'" }
            };
            var htRep = GetDocumentRepresentationInKeyOrder(ht, reps);
            var ldRep = GetDocumentRepresentationInKeyOrder(ld, reps);
            var odRep = GetDocumentRepresentationInKeyOrder(od, reps);
            var slRep = GetDocumentRepresentationInKeyOrder(sl, reps);
            var expected = "{ 'HT' : #HT, 'ID' : #HT, 'LD' : #LD, 'OD' : #OD, 'SL' : #SL }";
            expected = expected.Replace("#HT", htRep);
            expected = expected.Replace("#LD", ldRep);
            expected = expected.Replace("#OD", odRep);
            expected = expected.Replace("#SL", slRep);
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<Hashtable>(rehydrated.HT);
            Assert.IsInstanceOf<Hashtable>(rehydrated.ID);
            Assert.IsInstanceOf<ListDictionary>(rehydrated.LD);
            Assert.IsInstanceOf<OrderedDictionary>(rehydrated.OD);
            Assert.IsInstanceOf<SortedList>(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMixedPrimitiveTypesWithIntKeys()
        {
            var dateTime = DateTime.SpecifyKind(new DateTime(2010, 1, 1, 11, 22, 33), DateTimeKind.Utc);
            var isoDate = dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFZ");
            var guid = Guid.Empty;
            var objectId = ObjectId.Empty;
            var ht = new Hashtable
            {
                { 1, true },
                { 2, dateTime },
                { 3, 1.5 },
                { 4, 1 },
                { 5, 2L },
                { 6, guid },
                { 7, objectId },
                { 8, "x" }
            };
            var ld = CreateListDictionary(ht);
            var od = CreateOrderedDictionary(ht);
            var sl = CreateSortedList(ht);
            var obj = new T { HT = ht, ID = ht, LD = ld, OD = od, SL = sl };
            var json = obj.ToJson();
            var reps = new Hashtable
            {
                { 1, "[1, true]" },
                { 2, "[2, ISODate('#')]".Replace("#", isoDate) },
                { 3, "[3, 1.5]" },
                { 4, "[4, 1]" },
                { 5, "[5, NumberLong(2)]" },
                { 6, "[6, CSUUID('00000000-0000-0000-0000-000000000000')]" },
                { 7, "[7, ObjectId('000000000000000000000000')]" },
                { 8, "[8, 'x']" }
            };
            var htRep = GetArrayRepresentationInKeyOrder(ht, reps);
            var ldRep = GetArrayRepresentationInKeyOrder(ld, reps);
            var odRep = GetArrayRepresentationInKeyOrder(od, reps);
            var slRep = GetArrayRepresentationInKeyOrder(sl, reps);
            var expected = "{ 'HT' : #HT, 'ID' : #HT, 'LD' : #LD, 'OD' : #OD, 'SL' : #SL }";
            expected = expected.Replace("#HT", htRep);
            expected = expected.Replace("#LD", ldRep);
            expected = expected.Replace("#OD", odRep);
            expected = expected.Replace("#SL", slRep);
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<Hashtable>(rehydrated.HT);
            Assert.IsInstanceOf<Hashtable>(rehydrated.ID);
            Assert.IsInstanceOf<ListDictionary>(rehydrated.LD);
            Assert.IsInstanceOf<OrderedDictionary>(rehydrated.OD);
            Assert.IsInstanceOf<SortedList>(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMixedPrimitiveTypesWithMixedKeys()
        {
            // note: no SortedList in this test because you can't sort a set of keys that have mixed types
            var dateTime = DateTime.SpecifyKind(new DateTime(2010, 1, 1, 11, 22, 33), DateTimeKind.Utc);
            var isoDate = dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFZ");
            var guid = Guid.Empty;
            var objectId = ObjectId.Empty;
            var ht = new Hashtable
            {
                { "A", true },
                { "B", dateTime },
                { "C", 1.5 },
                { "D", 1 },
                { 4, 2L },
                { 5.0, guid },
                { true, objectId },
                { false, "x" }
            };
            var ld = CreateListDictionary(ht);
            var od = CreateOrderedDictionary(ht);
            var obj = new T { HT = ht, ID = ht, LD = ld, OD = od };
            var json = obj.ToJson();
            var reps = new Hashtable
            {
                { "A", "['A', true]" },
                { "B", "['B', ISODate('#')]".Replace("#", isoDate) },
                { "C", "['C', 1.5]" },
                { "D", "['D', 1]" },
                { 4, "[4, NumberLong(2)]" },
                { 5.0, "[5.0, CSUUID('00000000-0000-0000-0000-000000000000')]" },
                { true, "[true, ObjectId('000000000000000000000000')]" },
                { false, "[false, 'x']" }
            };
            var htRep = GetArrayRepresentationInKeyOrder(ht, reps);
            var ldRep = GetArrayRepresentationInKeyOrder(ld, reps);
            var odRep = GetArrayRepresentationInKeyOrder(od, reps);
            var expected = "{ 'HT' : #HT, 'ID' : #HT, 'LD' : #LD, 'OD' : #OD, 'SL' : null }";
            expected = expected.Replace("#HT", htRep);
            expected = expected.Replace("#LD", ldRep);
            expected = expected.Replace("#OD", odRep);
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<Hashtable>(rehydrated.HT);
            Assert.IsInstanceOf<Hashtable>(rehydrated.ID);
            Assert.IsInstanceOf<ListDictionary>(rehydrated.LD);
            Assert.IsInstanceOf<OrderedDictionary>(rehydrated.OD);
            Assert.IsNull(rehydrated.SL);
            Assert.IsTrue(CompareDictionaries(obj.HT, rehydrated.HT));
            Assert.IsTrue(CompareDictionaries(obj.ID, rehydrated.ID));
            Assert.IsTrue(CompareDictionaries(obj.LD, rehydrated.LD));
            Assert.IsTrue(CompareOrderedDictionaries(obj.OD, rehydrated.OD));
            // can't do usual BSON byte by byte comparison because order of Dictionary entries is not guaranteed to stay the same
        }

        public bool CompareDictionaries(IDictionary dictionary1, IDictionary dictionary2)
        {
            if (object.ReferenceEquals(dictionary1, dictionary2)) { return true; }
            if (dictionary1 == null) { return false; }
            if (dictionary2 == null) { return false; }
            if (dictionary1.Count != dictionary2.Count) { return false; }
            foreach (var key in dictionary1.Keys)
            {
                var item1 = dictionary1[key];
                var item2 = dictionary2[key];
                if (object.ReferenceEquals(item1, item2)) { continue; }
                if (item1 == null) { return false; }
                if (item2 == null) { return false; }
                if (!item1.Equals(item2)) { return false; }
            }
            return true;
        }

        public bool CompareOrderedDictionaries(IOrderedDictionary dictionary1, IOrderedDictionary dictionary2)
        {
            if (object.ReferenceEquals(dictionary1, dictionary2)) { return true; }
            if (dictionary1 == null) { return false; }
            if (dictionary2 == null) { return false; }
            if (dictionary1.Count != dictionary2.Count) { return false; }
            if (!dictionary1.Keys.Cast<object>().SequenceEqual(dictionary2.Keys.Cast<Object>())) { return false; }
            for (int i = 0; i < dictionary1.Count; i++)
            {
                var item1 = dictionary1[i];
                var item2 = dictionary2[i];
                if (object.ReferenceEquals(item1, item2)) { continue; }
                if (item1 == null) { return false; }
                if (item2 == null) { return false; }
                if (!item1.Equals(item2)) { return false; }
            }
            return true;
        }

        private ListDictionary CreateListDictionary(Hashtable ht)
        {
            var ld = new ListDictionary();
            foreach (DictionaryEntry entry in ht)
            {
                ld.Add(entry.Key, entry.Value);
            }
            return ld;
        }

        private OrderedDictionary CreateOrderedDictionary(Hashtable ht)
        {
            var od = new OrderedDictionary();
            foreach (DictionaryEntry entry in ht)
            {
                od.Add(entry.Key, entry.Value);
            }
            return od;
        }

        private SortedList CreateSortedList(Hashtable ht)
        {
            var sl = new SortedList();
            foreach (DictionaryEntry entry in ht)
            {
                sl.Add(entry.Key, entry.Value);
            }
            return sl;
        }

        private string GetArrayRepresentationInKeyOrder(IDictionary dictionary, IDictionary representations)
        {
            var sb = new StringBuilder();
            foreach (var key in dictionary.Keys)
            {
                sb.Append((sb.Length == 0) ? "[" : ", ");
                sb.Append(representations[key]);
            }
            sb.Append("]");
            return sb.ToString();
        }

        private string GetDocumentRepresentationInKeyOrder(IDictionary dictionary, IDictionary representations)
        {
            var sb = new StringBuilder();
            foreach (var key in dictionary.Keys)
            {
                sb.Append((sb.Length == 0) ? "{ " : ", ");
                sb.AppendFormat("'{0}' : {1}", key, representations[key]);
            }
            sb.Append(" }");
            return sb.ToString();
        }
    }

    [TestFixture]
    public class EnumDictionaryTests
    {
        private enum E
        {
            None,
            A,
            B
        }

        private class C
        {
            [BsonRepresentation(BsonType.String)]
            public Hashtable Hashtable;
        }

        // required for deterministic tests
        private class D
        {
            [BsonRepresentation(BsonType.String)]
            public SortedList Hashtable;
        }

        [Test]
        public void TestSerializeNull()
        {
            C c = new C { Hashtable = null };
            var json = c.ToJson();
            var expected = ("{ 'Hashtable' : null }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeEmpty()
        {
            C c = new C { Hashtable = new Hashtable() };
            var json = c.ToJson();
            var expected = ("{ 'Hashtable' : { } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerialize1()
        {
            C c = new C { Hashtable = new Hashtable { { "a", E.A } } };
            var json = c.ToJson();
            var expected = ("{ 'Hashtable' : { \"a\" : \"A\" } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerialize2()
        {
            D d = new D { Hashtable = new SortedList { { "a", E.A }, { "b", E.B } } };
            var json = d.ToJson();
            var expected = ("{ 'Hashtable' : { \"a\" : \"A\", \"b\" : \"B\" } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = d.ToBson();
            var rehydrated = BsonSerializer.Deserialize<D>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
