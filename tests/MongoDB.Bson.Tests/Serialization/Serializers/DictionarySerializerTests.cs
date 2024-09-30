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
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.DictionarySerializers
{
    [BsonDiscriminator("DictionarySerializers.C")] // "C" is an ambiguous discriminator when nominalType is System.Object
    public class C
    {
        public string P { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(C))
            {
                return false;
            }

            var other = (C)obj;
            return P.Equals(other.P);
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }

    [Collection(RegisterObjectSerializerFixture.CollectionName)]
    public class DictionarySerializerTests
    {
        static DictionarySerializerTests()
        {
            _ = new RegisterObjectSerializerFixture(); // ensure correct ObjectSerializer is registered

            var objectSerializer = BsonSerializer.LookupSerializer<object>();
            var dictionaryRepresentation = DictionaryRepresentation.Document;
            var keySerializer = objectSerializer;
            var valueSerializer = objectSerializer;
            var hashTableSerializer = new DictionaryInterfaceImplementerSerializer<Hashtable>(dictionaryRepresentation, keySerializer, valueSerializer);
            var iDictionarySerializer = new ImpliedImplementationInterfaceSerializer<IDictionary, Hashtable>(hashTableSerializer);
            var listDictionarySerializer = new DictionaryInterfaceImplementerSerializer<ListDictionary>(dictionaryRepresentation, keySerializer, valueSerializer);
            var orderedDictionarySerializer = new DictionaryInterfaceImplementerSerializer<OrderedDictionary>(dictionaryRepresentation, keySerializer, valueSerializer);
            var sortedListDictionarySerializer = new DictionaryInterfaceImplementerSerializer<SortedList>(dictionaryRepresentation, keySerializer, valueSerializer);

            BsonClassMap.RegisterClassMap<T>(cm =>
            {
                cm.MapProperty(t => t.HT).SetSerializer(hashTableSerializer);
                cm.MapProperty(t => t.ID).SetSerializer(iDictionarySerializer);
                cm.MapProperty(t => t.LD).SetSerializer(listDictionarySerializer);
                cm.MapProperty(t => t.OD).SetSerializer(orderedDictionarySerializer);
                cm.MapProperty(t => t.SL).SetSerializer(sortedListDictionarySerializer);
            });
        }

        public class T
        {
            public Hashtable HT { get; set; }
            public IDictionary ID { get; set; }
            public ListDictionary LD { get; set; }
            public OrderedDictionary OD { get; set; }
            public SortedList SL { get; set; }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new T { HT = null, ID = null, LD = null, OD = null, SL = null };
            var json = obj.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var rep = "null";
            var expected = "{ 'HT' : #R, 'ID' : #R, 'LD' : #R, 'OD' : #R, 'SL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.Null(rehydrated.HT);
            Assert.Null(rehydrated.ID);
            Assert.Null(rehydrated.LD);
            Assert.Null(rehydrated.OD);
            Assert.Null(rehydrated.SL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestEmpty()
        {
            var ht = new Hashtable();
            var ld = CreateListDictionary(ht);
            var od = CreateOrderedDictionary(ht);
            var sl = CreateSortedList(ht);
            var obj = new T { HT = ht, ID = ht, LD = ld, OD = od, SL = sl };
            var json = obj.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var rep = "{ }";
            var expected = "{ 'HT' : #R, 'ID' : #R, 'LD' : #R, 'OD' : #R, 'SL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);

            rehydrated.HT.Should().BeEquivalentTo(obj.HT);
            rehydrated.ID.Should().BeEquivalentTo(obj.ID);
            rehydrated.LD.Should().Equal(obj.LD);
            rehydrated.OD.Should().Equal(obj.OD);
            rehydrated.LD.Should().Equal(obj.LD);
        }

        [Fact]
        public void TestOneC()
        {
            var ht = new Hashtable { { "A", new C { P = "x" } } };
            var ld = CreateListDictionary(ht);
            var od = CreateOrderedDictionary(ht);
            var sl = CreateSortedList(ht);
            var obj = new T { HT = ht, ID = ht, LD = ld, OD = od, SL = sl };
            var json = obj.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var rep = "{ 'A' : { '_t' : 'DictionarySerializers.C', 'P' : 'x' } }";
            var expected = "{ 'HT' : #R, 'ID' : #R, 'LD' : #R, 'OD' : #R, 'SL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);

            rehydrated.HT.Should().BeEquivalentTo(obj.HT);
            rehydrated.ID.Should().BeEquivalentTo(obj.ID);
            rehydrated.LD.Should().Equal(obj.LD);
            rehydrated.OD.Should().Equal(obj.OD);
            rehydrated.LD.Should().Equal(obj.LD);
        }

        [Fact]
        public void TestOneInt()
        {
            var ht = new Hashtable { { "A", 1 } };
            var ld = CreateListDictionary(ht);
            var od = CreateOrderedDictionary(ht);
            var sl = CreateSortedList(ht);
            var obj = new T { HT = ht, ID = ht, LD = ld, OD = od, SL = sl };
            var json = obj.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var rep = "{ 'A' : 1 }";
            var expected = "{ 'HT' : #R, 'ID' : #R, 'LD' : #R, 'OD' : #R, 'SL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);

            rehydrated.HT.Should().BeEquivalentTo(obj.HT);
            rehydrated.ID.Should().BeEquivalentTo(obj.ID);
            rehydrated.LD.Should().Equal(obj.LD);
            rehydrated.OD.Should().Equal(obj.OD);
            rehydrated.LD.Should().Equal(obj.LD);
        }

        [Fact]
        public void TestOneIntWithIntKey()
        {
            var ht = new Hashtable { { 1, 2 } };
            var ld = CreateListDictionary(ht);
            var od = CreateOrderedDictionary(ht);
            var sl = CreateSortedList(ht);
            var obj = new T { HT = ht, ID = ht, LD = ld, OD = od, SL = sl };
            Assert.Throws<BsonSerializationException>(() => obj.ToBson());
        }

        [Fact]
        public void TestOneString()
        {
            var ht = new Hashtable { { "A", "x" } };
            var ld = CreateListDictionary(ht);
            var od = CreateOrderedDictionary(ht);
            var sl = CreateSortedList(ht);
            var obj = new T { HT = ht, ID = ht, LD = ld, OD = od, SL = sl };
            var json = obj.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var rep = "{ 'A' : 'x' }";
            var expected = "{ 'HT' : #R, 'ID' : #R, 'LD' : #R, 'OD' : #R, 'SL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);

            rehydrated.HT.Should().BeEquivalentTo(obj.HT);
            rehydrated.ID.Should().BeEquivalentTo(obj.ID);
            rehydrated.LD.Should().Equal(obj.LD);
            rehydrated.OD.Should().Equal(obj.OD);
            rehydrated.LD.Should().Equal(obj.LD);
        }

        [Fact]
        public void TestOneStringWithIntKey()
        {
            var ht = new Hashtable { { 1, "x" } };
            var ld = CreateListDictionary(ht);
            var od = CreateOrderedDictionary(ht);
            var sl = CreateSortedList(ht);
            var obj = new T { HT = ht, ID = ht, LD = ld, OD = od, SL = sl };
            Assert.Throws<BsonSerializationException>(() => obj.ToBson());
        }

        [Fact]
        public void TestTwoCs()
        {
            var ht = new Hashtable { { "A", new C { P = "x" } }, { "B", new C { P = "y" } } };
            var ld = CreateListDictionary(ht);
            var od = CreateOrderedDictionary(ht);
            var sl = CreateSortedList(ht);
            var obj = new T { HT = ht, ID = ht, LD = ld, OD = od, SL = sl };
            var json = obj.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
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
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);

            rehydrated.HT.Should().BeEquivalentTo(obj.HT);
            rehydrated.ID.Should().BeEquivalentTo(obj.ID);
            rehydrated.LD.Should().Equal(obj.LD);
            rehydrated.OD.Should().Equal(obj.OD);
            rehydrated.LD.Should().Equal(obj.LD);
        }

        [Fact]
        public void TestTwoCsWithIntKeys()
        {
            var ht = new Hashtable { { 1, new C { P = "x" } }, { 2, new C { P = "y" } } };
            var ld = CreateListDictionary(ht);
            var od = CreateOrderedDictionary(ht);
            var sl = CreateSortedList(ht);
            var obj = new T { HT = ht, ID = ht, LD = ld, OD = od, SL = sl };
            Assert.Throws<BsonSerializationException>(() => obj.ToBson());
        }

        [Fact]
        public void TestTwoInts()
        {
            var ht = new Hashtable { { "A", 1 }, { "B", 2 } };
            var ld = CreateListDictionary(ht);
            var od = CreateOrderedDictionary(ht);
            var sl = CreateSortedList(ht);
            var obj = new T { HT = ht, ID = ht, LD = ld, OD = od, SL = sl };
            var json = obj.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
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
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);

            rehydrated.HT.Should().BeEquivalentTo(obj.HT);
            rehydrated.ID.Should().BeEquivalentTo(obj.ID);
            rehydrated.LD.Should().Equal(obj.LD);
            rehydrated.OD.Should().Equal(obj.OD);
            rehydrated.LD.Should().Equal(obj.LD);
        }

        [Fact]
        public void TestTwoIntsWithIntKeys()
        {
            var ht = new Hashtable { { 1, 2 }, { 3, 4 } };
            var ld = CreateListDictionary(ht);
            var od = CreateOrderedDictionary(ht);
            var sl = CreateSortedList(ht);
            var obj = new T { HT = ht, ID = ht, LD = ld, OD = od, SL = sl };
            Assert.Throws<BsonSerializationException>(() => obj.ToBson());
        }

        [Fact]
        public void TestTwoStrings()
        {
            var ht = new Hashtable { { "A", "x" }, { "B", "y" } };
            var ld = CreateListDictionary(ht);
            var od = CreateOrderedDictionary(ht);
            var sl = CreateSortedList(ht);
            var obj = new T { HT = ht, ID = ht, LD = ld, OD = od, SL = sl };
            var json = obj.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
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
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);

            rehydrated.HT.Should().BeEquivalentTo(obj.HT);
            rehydrated.ID.Should().BeEquivalentTo(obj.ID);
            rehydrated.LD.Should().Equal(obj.LD);
            rehydrated.OD.Should().Equal(obj.OD);
            rehydrated.LD.Should().Equal(obj.LD);
        }

        [Fact]
        public void TestTwoStringsWithIntKeys()
        {
            var ht = new Hashtable { { 1, "x" }, { 2, "y" } };
            var ld = CreateListDictionary(ht);
            var od = CreateOrderedDictionary(ht);
            var sl = CreateSortedList(ht);
            var obj = new T { HT = ht, ID = ht, LD = ld, OD = od, SL = sl };
            Assert.Throws<BsonSerializationException>(() => obj.ToBson());
        }

        [Fact]
        public void TestMixedPrimitiveTypes()
        {
            var dateTime = DateTime.SpecifyKind(new DateTime(2010, 1, 1, 11, 22, 33), DateTimeKind.Utc);
            var isoDate = dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFZ", CultureInfo.InvariantCulture);
            var guid = Guid.Empty;
            string expectedGuidJson = null;
            var objectId = ObjectId.Empty;
            var ht = new Hashtable
            {
                { "A", true },
                { "B", dateTime },
                { "C", 1.5 },
                { "D", 1 },
                { "E", 2L },
                { "G", objectId },
                { "H", "x" }
            };
            if (expectedGuidJson != null)
            {
                ht.Add("F", guid);
            }
            var ld = CreateListDictionary(ht);
            var od = CreateOrderedDictionary(ht);
            var sl = CreateSortedList(ht);
            var obj = new T { HT = ht, ID = ht, LD = ld, OD = od, SL = sl };
            var json = obj.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var reps = new Hashtable
            {
                { "A", "true" },
                { "B", "ISODate('#')".Replace("#", isoDate) },
                { "C", "1.5" },
                { "D", "1" },
                { "E", "NumberLong(2)" },
                { "G", "ObjectId('000000000000000000000000')" },
                { "H", "'x'" }
            };
            if (expectedGuidJson != null)
            {
                reps.Add("F", expectedGuidJson);
            }
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
            Assert.Equal(expected, json);

            var bson = obj.ToBson(writerSettings: new BsonBinaryWriterSettings());
            var rehydrated = BsonSerializer.Deserialize<T>(new BsonBinaryReader(new MemoryStream(bson), new BsonBinaryReaderSettings()));

            rehydrated.HT.Should().BeEquivalentTo(obj.HT);
            rehydrated.ID.Should().BeEquivalentTo(obj.ID);
            rehydrated.LD.Should().Equal(obj.LD);
            rehydrated.OD.Should().Equal(obj.OD);
            rehydrated.LD.Should().Equal(obj.LD);
        }

        [Fact]
        public void TestMixedPrimitiveTypesWithIntKeys()
        {
            var dateTime = DateTime.SpecifyKind(new DateTime(2010, 1, 1, 11, 22, 33), DateTimeKind.Utc);
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
            Assert.Throws<BsonSerializationException>(() => obj.ToBson());
        }

        [Fact]
        public void TestMixedPrimitiveTypesWithMixedKeys()
        {
            // note: no SortedList in this test because you can't sort a set of keys that have mixed types
            var dateTime = DateTime.SpecifyKind(new DateTime(2010, 1, 1, 11, 22, 33), DateTimeKind.Utc);
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
            Assert.Throws<BsonSerializationException>(() => obj.ToBson());
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
            public Hashtable Hashtable;
        }

        // required for deterministic tests
        private class D
        {
            public SortedList Hashtable;
        }

        [Fact]
        public void TestSerializeNull()
        {
            C c = new C { Hashtable = null };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'Hashtable' : null }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeEmpty()
        {
            C c = new C { Hashtable = new Hashtable() };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'Hashtable' : { } }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerialize1()
        {
            C c = new C { Hashtable = new Hashtable { { "a", E.A } } };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'Hashtable' : { \"a\" : 1 } }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerialize2()
        {
            D d = new D { Hashtable = new SortedList { { "a", E.A }, { "b", E.B } } };
            var json = d.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'Hashtable' : { \"a\" : 1, \"b\" : 2 } }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = d.ToBson();
            var rehydrated = BsonSerializer.Deserialize<D>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
