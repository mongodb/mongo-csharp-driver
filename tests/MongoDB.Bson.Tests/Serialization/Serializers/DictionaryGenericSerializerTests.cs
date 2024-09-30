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
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.DictionaryGenericSerializers
{
    [BsonDiscriminator("DictionaryGenericSerializers.C")] // "C" is an ambiguous discriminator when nominalType is System.Object
    public class C
    {
        public string P { get; set; }
    }

    [Collection(RegisterObjectSerializerFixture.CollectionName)]
    public class DictionaryGenericSerializerTests
    {
        static DictionaryGenericSerializerTests()
        {
            _ = new RegisterObjectSerializerFixture(); // ensure correct ObjectSerializer is registered

            var objectSerializer = BsonSerializer.LookupSerializer<object>();
            var dictionaryRepresentation = DictionaryRepresentation.Document;
            var keySerializer = objectSerializer;
            var valueSerializer = objectSerializer;
            var dictionarySerializer = new DictionaryInterfaceImplementerSerializer<Dictionary<object, object>, object, object>(dictionaryRepresentation, keySerializer, valueSerializer);
            var idictionarySerializer = new ImpliedImplementationInterfaceSerializer<IDictionary<object, object>, Dictionary<object, object>>(dictionarySerializer);
            var readOnlyDictionarySerializer = new ReadOnlyDictionaryInterfaceImplementerSerializer<ReadOnlyDictionary<object, object>, object, object>(dictionaryRepresentation, keySerializer, valueSerializer);
            var ireadOnlyDictionarySerializer = new ImpliedImplementationInterfaceSerializer<IReadOnlyDictionary<object, object>, ReadOnlyDictionary<object, object>>(readOnlyDictionarySerializer);
            var sortedDictionarySerializer = new DictionaryInterfaceImplementerSerializer<SortedDictionary<object, object>, object, object>(dictionaryRepresentation, keySerializer, valueSerializer);
            var sortedListSerializer = new DictionaryInterfaceImplementerSerializer<SortedList<object, object>, object, object>(dictionaryRepresentation, keySerializer, valueSerializer);

            BsonClassMap.RegisterClassMap<T>(cm =>
            {
                cm.MapProperty(t => t.D).SetSerializer(dictionarySerializer);
                cm.MapProperty(t => t.ID).SetSerializer(idictionarySerializer);
                cm.MapProperty(t => t.IROD).SetSerializer(ireadOnlyDictionarySerializer);
                cm.MapProperty(t => t.ROD).SetSerializer(readOnlyDictionarySerializer);
                cm.MapProperty(t => t.SD).SetSerializer(sortedDictionarySerializer);
                cm.MapProperty(t => t.SL).SetSerializer(sortedListSerializer);
            });
        }

        public class T
        {
            public Dictionary<object, object> D { get; set; }
            public IDictionary<object, object> ID { get; set; }
            public IReadOnlyDictionary<object, object> IROD { get; set; }
            public ReadOnlyDictionary<object, object> ROD { get; set; }
            public SortedDictionary<object, object> SD { get; set; }
            public SortedList<object, object> SL { get; set; }
        }

        public class RO<TKey, TValue> : ReadOnlyDictionary<TKey, TValue>
        {
            private RO(IDictionary<TKey, TValue> dictionary) : base(dictionary)
            {
            }

            public static RO<TKey, TValue> ConstructorReplacement(IDictionary<TKey, TValue> dictionary)
            {
                return new RO<TKey, TValue>(dictionary);
            }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new T { D = null, ID = null, IROD = null, ROD = null, SD = null, SL = null };
            var json = obj.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var rep = "null";

            var expected = "{ 'D' : #R, 'ID' : #R, 'IROD' : #R, 'ROD' : #R, 'SD' : #R, 'SL' : #R }".Replace("#R", rep).Replace("'", "\"");
            // 'IROD' : #R, 'ROD' : #R,

            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.Null(rehydrated.D);
            Assert.Null(rehydrated.ID);
            Assert.Null(rehydrated.IROD);
            Assert.Null(rehydrated.ROD);
            Assert.Null(rehydrated.SD);
            Assert.Null(rehydrated.SL);

            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestEmpty()
        {
            var d = new Dictionary<object, object>();
            var rod = new ReadOnlyDictionary<object, object>(d);
            var sd = CreateSortedDictionary(d);
            var sl = CreateSortedList(d);
            var obj = new T { D = d, ID = d, IROD = rod, ROD = rod, SD = sd, SL = sl };
            var json = obj.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var rep = "{ }";
            var expected = "{ 'D' : #R, 'ID' : #R, 'IROD' : #R, 'ROD' : #R, 'SD' : #R, 'SL' : #R }".Replace("#R", rep).Replace("'", "\"");

            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<Dictionary<object, object>>(rehydrated.D);
            Assert.IsType<Dictionary<object, object>>(rehydrated.ID);
            Assert.IsType<ReadOnlyDictionary<object, object>>(rehydrated.IROD);
            Assert.IsType<ReadOnlyDictionary<object, object>>(rehydrated.ROD);
            Assert.IsType<SortedDictionary<object, object>>(rehydrated.SD);
            Assert.IsType<SortedList<object, object>>(rehydrated.SL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestOneC()
        {
            var d = new Dictionary<object, object> { { "A", new C { P = "x" } } };
            var rod = new ReadOnlyDictionary<object, object>(d);
            var sd = CreateSortedDictionary(d);
            var sl = CreateSortedList(d);
            var obj = new T { D = d, ID = d, IROD = rod, ROD = rod, SD = sd, SL = sl };
            var json = obj.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var rep = "{ 'A' : { '_t' : 'DictionaryGenericSerializers.C', 'P' : 'x' } }";
            var expected = "{ 'D' : #R, 'ID' : #R, 'IROD' : #R, 'ROD' : #R, 'SD' : #R, 'SL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<Dictionary<object, object>>(rehydrated.D);
            Assert.IsType<Dictionary<object, object>>(rehydrated.ID);
            Assert.IsType<ReadOnlyDictionary<object, object>>(rehydrated.IROD);
            Assert.IsType<ReadOnlyDictionary<object, object>>(rehydrated.ROD);
            Assert.IsType<SortedDictionary<object, object>>(rehydrated.SD);
            Assert.IsType<SortedList<object, object>>(rehydrated.SL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestOneInt()
        {
            var d = new Dictionary<object, object> { { "A", 1 } };
            var rod = new ReadOnlyDictionary<object, object>(d);
            var sd = CreateSortedDictionary(d);
            var sl = CreateSortedList(d);
            var obj = new T { D = d, ID = d, IROD = rod, ROD = rod, SD = sd, SL = sl };
            var json = obj.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var rep = "{ 'A' : 1 }";
            var expected = "{ 'D' : #R, 'ID' : #R, 'IROD' : #R, 'ROD' : #R, 'SD' : #R, 'SL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<Dictionary<object, object>>(rehydrated.D);
            Assert.IsType<Dictionary<object, object>>(rehydrated.ID);
            Assert.IsType<ReadOnlyDictionary<object, object>>(rehydrated.IROD);
            Assert.IsType<ReadOnlyDictionary<object, object>>(rehydrated.ROD);
            Assert.IsType<SortedDictionary<object, object>>(rehydrated.SD);
            Assert.IsType<SortedList<object, object>>(rehydrated.SL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestOneIntWithIntKey()
        {
            var d = new Dictionary<object, object> { { 1, 2 } };
            var rod = new ReadOnlyDictionary<object, object>(d);
            var sd = CreateSortedDictionary(d);
            var sl = CreateSortedList(d);
            var obj = new T { D = d, ID = d, IROD = rod, ROD = rod, SD = sd, SL = sl };
            Assert.Throws<BsonSerializationException>(() => obj.ToBson());
        }

        [Fact]
        public void TestOneString()
        {
            var d = new Dictionary<object, object> { { "A", "x" } };
            var rod = new ReadOnlyDictionary<object, object>(d);
            var sd = CreateSortedDictionary(d);
            var sl = CreateSortedList(d);
            var obj = new T { D = d, ID = d, IROD = rod, ROD = rod, SD = sd, SL = sl };
            var json = obj.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var rep = "{ 'A' : 'x' }";
            var expected = "{ 'D' : #R, 'ID' : #R, 'IROD' : #R, 'ROD' : #R, 'SD' : #R, 'SL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<Dictionary<object, object>>(rehydrated.D);
            Assert.IsType<Dictionary<object, object>>(rehydrated.ID);
            Assert.IsType<ReadOnlyDictionary<object, object>>(rehydrated.IROD);
            Assert.IsType<ReadOnlyDictionary<object, object>>(rehydrated.ROD);
            Assert.IsType<SortedDictionary<object, object>>(rehydrated.SD);
            Assert.IsType<SortedList<object, object>>(rehydrated.SL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestOneStringWithIntKey()
        {
            var d = new Dictionary<object, object> { { 1, "x" } };
            var rod = new ReadOnlyDictionary<object, object>(d);
            var sd = CreateSortedDictionary(d);
            var sl = CreateSortedList(d);
            var obj = new T { D = d, ID = d, IROD = rod, ROD = rod, SD = sd, SL = sl };
            Assert.Throws<BsonSerializationException>(() => obj.ToBson());
        }

        [Fact]
        public void TestTwoCs()
        {
            var d = new Dictionary<object, object> { { "A", new C { P = "x" } }, { "B", new C { P = "y" } } };
            var rod = new ReadOnlyDictionary<object, object>(d);
            var sd = CreateSortedDictionary(d);
            var sl = CreateSortedList(d);
            var obj = new T { D = d, ID = d, IROD = rod, ROD = rod, SD = sd, SL = sl };
            var json = obj.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var reps = new Dictionary<object, object>
            {
                { "A", "{ '_t' : 'DictionaryGenericSerializers.C', 'P' : 'x' }"},
                { "B", "{ '_t' : 'DictionaryGenericSerializers.C', 'P' : 'y' }"}
            };
            var htRep = GetDocumentRepresentationInKeyOrder(d, reps);
            var sdRep = GetDocumentRepresentationInKeyOrder(sd, reps);
            var slRep = GetDocumentRepresentationInKeyOrder(sl, reps);
            var expected = "{ 'D' : #D, 'ID' : #D, 'IROD' : #D, 'ROD' : #D, 'SD' : #SD, 'SL' : #SL }"
                .Replace("#D", htRep)
                .Replace("#SD", sdRep)
                .Replace("#SL", slRep)
                .Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<Dictionary<object, object>>(rehydrated.D);
            Assert.IsType<Dictionary<object, object>>(rehydrated.ID);
            Assert.IsType<ReadOnlyDictionary<object, object>>(rehydrated.IROD);
            Assert.IsType<ReadOnlyDictionary<object, object>>(rehydrated.ROD);
            Assert.IsType<SortedDictionary<object, object>>(rehydrated.SD);
            Assert.IsType<SortedList<object, object>>(rehydrated.SL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestTwoCsWithIntKeys()
        {
            var d = new Dictionary<object, object> { { 1, new C { P = "x" } }, { 2, new C { P = "y" } } };
            var rod = new ReadOnlyDictionary<object, object>(d);
            var sd = CreateSortedDictionary(d);
            var sl = CreateSortedList(d);
            var obj = new T { D = d, ID = d, IROD = rod, ROD = rod, SD = sd, SL = sl };
            Assert.Throws<BsonSerializationException>(() => obj.ToBson());
        }

        [Fact]
        public void TestTwoInts()
        {
            var d = new Dictionary<object, object> { { "A", 1 }, { "B", 2 } };
            var rod = new ReadOnlyDictionary<object, object>(d);
            var sd = CreateSortedDictionary(d);
            var sl = CreateSortedList(d);
            var obj = new T { D = d, ID = d, IROD = rod, ROD = rod, SD = sd, SL = sl };
            var json = obj.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var reps = new Dictionary<object, object>
            {
                { "A", "1"},
                { "B", "2"}
            };
            var htRep = GetDocumentRepresentationInKeyOrder(d, reps);
            var sdRep = GetDocumentRepresentationInKeyOrder(sd, reps);
            var slRep = GetDocumentRepresentationInKeyOrder(sl, reps);
            var expected = "{ 'D' : #D, 'ID' : #D, 'IROD' : #D, 'ROD' : #D, 'SD' : #SD, 'SL' : #SL }"
                .Replace("#D", htRep)
                .Replace("#SD", sdRep)
                .Replace("#SL", slRep)
                .Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<Dictionary<object, object>>(rehydrated.D);
            Assert.IsType<Dictionary<object, object>>(rehydrated.ID);
            Assert.IsType<ReadOnlyDictionary<object, object>>(rehydrated.IROD);
            Assert.IsType<ReadOnlyDictionary<object, object>>(rehydrated.ROD);
            Assert.IsType<SortedDictionary<object, object>>(rehydrated.SD);
            Assert.IsType<SortedList<object, object>>(rehydrated.SL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestTwoIntsWithIntKeys()
        {
            var d = new Dictionary<object, object> { { 1, 2 }, { 3, 4 } };
            var rod = new ReadOnlyDictionary<object, object>(d);
            var sd = CreateSortedDictionary(d);
            var sl = CreateSortedList(d);
            var obj = new T { D = d, ID = d, IROD = rod, ROD = rod, SD = sd, SL = sl };
            Assert.Throws<BsonSerializationException>(() => obj.ToBson());
        }

        [Fact]
        public void TestTwoStrings()
        {
            var d = new Dictionary<object, object> { { "A", "x" }, { "B", "y" } };
            var rod = new ReadOnlyDictionary<object, object>(d);
            var sd = CreateSortedDictionary(d);
            var sl = CreateSortedList(d);
            var obj = new T { D = d, ID = d, IROD = rod, ROD = rod, SD = sd, SL = sl };
            var json = obj.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var reps = new Dictionary<object, object>
            {
                { "A", "'x'"},
                { "B", "'y'"}
            };
            var htRep = GetDocumentRepresentationInKeyOrder(d, reps);
            var sdRep = GetDocumentRepresentationInKeyOrder(sd, reps);
            var slRep = GetDocumentRepresentationInKeyOrder(sl, reps);
            var expected = "{ 'D' : #D, 'ID' : #D, 'IROD' : #D, 'ROD' : #D, 'SD' : #SD, 'SL' : #SL }"
                .Replace("#D", htRep)
                .Replace("#SD", sdRep)
                .Replace("#SL", slRep)
                .Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<Dictionary<object, object>>(rehydrated.D);
            Assert.IsType<Dictionary<object, object>>(rehydrated.ID);
            Assert.IsType<ReadOnlyDictionary<object, object>>(rehydrated.IROD);
            Assert.IsType<ReadOnlyDictionary<object, object>>(rehydrated.ROD);
            Assert.IsType<SortedDictionary<object, object>>(rehydrated.SD);
            Assert.IsType<SortedList<object, object>>(rehydrated.SL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestTwoStringsWithIntKeys()
        {
            var d = new Dictionary<object, object> { { 1, "x" }, { 2, "y" } };
            var rod = new ReadOnlyDictionary<object, object>(d);
            var sd = CreateSortedDictionary(d);
            var sl = CreateSortedList(d);
            var obj = new T { D = d, ID = d, IROD = rod, ROD = rod, SD = sd, SL = sl };
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
            var d = new Dictionary<object, object>
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
                d.Add("F", guid);
            }
            var rod = new ReadOnlyDictionary<object, object>(d);
            var sd = CreateSortedDictionary(d);
            var sl = CreateSortedList(d);
            var obj = new T { D = d, ID = d, IROD = rod, ROD = rod, SD = sd, SL = sl };
            var json = obj.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var reps = new Dictionary<object, object>
            {
                { "A", "true" },
                { "B", string.Format("ISODate('{0}')", isoDate) },
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
            var htRep = GetDocumentRepresentationInKeyOrder(d, reps);
            var sdRep = GetDocumentRepresentationInKeyOrder(sd, reps);
            var slRep = GetDocumentRepresentationInKeyOrder(sl, reps);
            var expected = "{ 'D' : #D, 'ID' : #D, 'IROD' : #D, 'ROD' : #D, 'SD' : #SD, 'SL' : #SL }"
                .Replace("#D", htRep)
                .Replace("#SD", sdRep)
                .Replace("#SL", slRep)
                .Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson(writerSettings: new BsonBinaryWriterSettings());
            var rehydrated = BsonSerializer.Deserialize<T>(new BsonBinaryReader(new MemoryStream(bson), new BsonBinaryReaderSettings()));
            Assert.IsType<Dictionary<object, object>>(rehydrated.D);
            Assert.IsType<Dictionary<object, object>>(rehydrated.ID);
            Assert.IsType<ReadOnlyDictionary<object, object>>(rehydrated.IROD);
            Assert.IsType<ReadOnlyDictionary<object, object>>(rehydrated.ROD);
            Assert.IsType<SortedDictionary<object, object>>(rehydrated.SD);
            Assert.IsType<SortedList<object, object>>(rehydrated.SL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson(writerSettings: new BsonBinaryWriterSettings())));
        }

        [Fact]
        public void TestMixedPrimitiveTypesWithIntKeys()
        {
            var dateTime = DateTime.SpecifyKind(new DateTime(2010, 1, 1, 11, 22, 33), DateTimeKind.Utc);
            var guid = Guid.Empty;
            var objectId = ObjectId.Empty;
            var d = new Dictionary<object, object>
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
            var rod = new ReadOnlyDictionary<object, object>(d);
            var sd = CreateSortedDictionary(d);
            var sl = CreateSortedList(d);
            var obj = new T { D = d, ID = d, IROD = rod, ROD = rod, SD = sd, SL = sl };
            Assert.Throws<BsonSerializationException>(() => obj.ToBson());
        }

        [Fact]
        public void TestMixedPrimitiveTypesWithMixedKeys()
        {
            // note: no SortedDictionary or SortedList in this test because you can't sort a set of keys that have mixed types
            var dateTime = DateTime.SpecifyKind(new DateTime(2010, 1, 1, 11, 22, 33), DateTimeKind.Utc);
            var guid = Guid.Empty;
            var objectId = ObjectId.Empty;
            var d = new Dictionary<object, object>
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
            var obj = new T { D = d, ID = d, SD = null, SL = null };
            Assert.Throws<BsonSerializationException>(() => obj.ToBson());
        }

        [Fact]
        public void TestImmutablePrivateConstructorDictionaryImplementation()
        {
            var d = new Dictionary<object, object> { { "A", new C { P = "x" } } };
            var id = RO<object, object>.ConstructorReplacement(d);
            var irod = new ReadOnlyDictionary<object, object>(d);
            var rod = new ReadOnlyDictionary<object, object>(d);
            var sd = CreateSortedDictionary(d);
            var sl = CreateSortedList(d);
            var obj = new T { D = d, ID = id, IROD = irod, ROD = rod, SD = sd, SL = sl };
            var json = obj.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var rep = "{ 'A' : { '_t' : 'DictionaryGenericSerializers.C', 'P' : 'x' } }";
            var expected = "{ 'D' : #R, 'ID' : #R, 'IROD' : #R, 'ROD' : #R, 'SD' : #R, 'SL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsType<Dictionary<object, object>>(rehydrated.D);
            Assert.IsType<Dictionary<object, object>>(rehydrated.ID);
            Assert.IsType<ReadOnlyDictionary<object, object>>(rehydrated.IROD);
            Assert.IsType<ReadOnlyDictionary<object, object>>(rehydrated.ROD);
            Assert.IsType<SortedDictionary<object, object>>(rehydrated.SD);
            Assert.IsType<SortedList<object, object>>(rehydrated.SL);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        private SortedDictionary<object, object> CreateSortedDictionary(Dictionary<object, object> d)
        {
            var sd = new SortedDictionary<object, object>();
            foreach (KeyValuePair<object, object> entry in d)
            {
                sd.Add(entry.Key, entry.Value);
            }
            return sd;
        }

        private SortedList<object, object> CreateSortedList(Dictionary<object, object> d)
        {
            var sl = new SortedList<object, object>();
            foreach (KeyValuePair<object, object> entry in d)
            {
                sl.Add(entry.Key, entry.Value);
            }
            return sl;
        }

        private string GetArrayRepresentationInKeyOrder(
            IDictionary<object, object> dictionary,
            IDictionary<object, object> representations)
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

        private string GetDocumentRepresentationInKeyOrder(
            IDictionary<object, object> dictionary,
            IDictionary<object, object> representations)
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
            [BsonRepresentation(BsonType.String)]
            public Dictionary<string, E> Dictionary;
        }

        // required for deterministic tests
        private class D
        {
            [BsonRepresentation(BsonType.String)]
            public SortedList<string, E> Dictionary;
        }

        [Fact]
        public void TestSerializeNull()
        {
            C c = new C { Dictionary = null };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'Dictionary' : null }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeEmpty()
        {
            C c = new C { Dictionary = new Dictionary<string, E>() };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'Dictionary' : { } }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerialize1()
        {
            C c = new C { Dictionary = new Dictionary<string, E> { { "a", E.A } } };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'Dictionary' : { \"a\" : \"A\" } }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerialize2()
        {
            D d = new D { Dictionary = new SortedList<string, E> { { "a", E.A }, { "b", E.B } } };
            var json = d.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'Dictionary' : { \"a\" : \"A\", \"b\" : \"B\" } }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = d.ToBson();
            var rehydrated = BsonSerializer.Deserialize<D>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class StringToObjectIdDictionaryTests
    {
        private class C
        {
            [BsonRepresentation(BsonType.ObjectId)]
            public Dictionary<string, string> Dictionary;
        }

        private static string id1 = "123456789012345678901234";
        private static string id2 = "432109876543210987654321";

        [Fact]
        public void TestSerializeNull()
        {
            C c = new C { Dictionary = null };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'Dictionary' : null }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeEmpty()
        {
            C c = new C { Dictionary = new Dictionary<string, string>() };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'Dictionary' : { } }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerialize1()
        {
            C c = new C { Dictionary = new Dictionary<string, string> { { "a", id1 } } };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'Dictionary' : { \"a\" : ObjectId(\"123456789012345678901234\") } }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerialize2()
        {
            C c = new C { Dictionary = new Dictionary<string, string> { { "a", id1 }, { "b", id2 } } };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected1 = ("{ 'Dictionary' : { \"a\" : ObjectId(\"123456789012345678901234\"), \"b\" : ObjectId(\"432109876543210987654321\") } }").Replace("'", "\"");
            var expected2 = ("{ 'Dictionary' : { \"b\" : ObjectId(\"432109876543210987654321\"), \"a\" : ObjectId(\"123456789012345678901234\") } }").Replace("'", "\"");
            if (json != expected1 && json != expected2)
            {
                Assert.Equal(expected1, json);
            }

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
