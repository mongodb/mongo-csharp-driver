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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Serialization.DictionaryGenericSerializers
{
    [BsonDiscriminator("DictionaryGenericSerializers.C")] // "C" is an ambiguous discriminator when nominalType is System.Object
    public class C
    {
        public string P { get; set; }
    }

    [TestFixture]
    public class DictionaryGenericSerializerTests
    {
        public class T
        {
            public Dictionary<object, object> D { get; set; }
            public IDictionary<object, object> ID { get; set; }
            public SortedDictionary<object, object> SD { get; set; }
            public SortedList<object, object> SL { get; set; }
        }

        [Test]
        public void TestNull()
        {
            var obj = new T { D = null, ID = null, SD = null, SL = null };
            var json = obj.ToJson();
            var rep = "null";
            var expected = "{ 'D' : #R, 'ID' : #R, 'SD' : #R, 'SL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsNull(rehydrated.D);
            Assert.IsNull(rehydrated.ID);
            Assert.IsNull(rehydrated.SD);
            Assert.IsNull(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestEmpty()
        {
            var d = new Dictionary<object, object>();
            var sd = CreateSortedDictionary(d);
            var sl = CreateSortedList(d);
            var obj = new T { D = d, ID = d, SD = sd, SL = sl };
            var json = obj.ToJson();
            var rep = "{ }";
            var expected = "{ 'D' : #R, 'ID' : #R, 'SD' : #R, 'SL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<Dictionary<object, object>>(rehydrated.D);
            Assert.IsInstanceOf<Dictionary<object, object>>(rehydrated.ID);
            Assert.IsInstanceOf<SortedDictionary<object, object>>(rehydrated.SD);
            Assert.IsInstanceOf<SortedList<object, object>>(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOneC()
        {
            var d = new Dictionary<object, object> { { "A", new C { P = "x" } } };
            var sd = CreateSortedDictionary(d);
            var sl = CreateSortedList(d);
            var obj = new T { D = d, ID = d, SD = sd, SL = sl };
            var json = obj.ToJson();
            var rep = "{ 'A' : { '_t' : 'DictionaryGenericSerializers.C', 'P' : 'x' } }";
            var expected = "{ 'D' : #R, 'ID' : #R, 'SD' : #R, 'SL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<Dictionary<object, object>>(rehydrated.D);
            Assert.IsInstanceOf<Dictionary<object, object>>(rehydrated.ID);
            Assert.IsInstanceOf<SortedDictionary<object, object>>(rehydrated.SD);
            Assert.IsInstanceOf<SortedList<object, object>>(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOneInt()
        {
            var d = new Dictionary<object, object> { { "A", 1 } };
            var sd = CreateSortedDictionary(d);
            var sl = CreateSortedList(d);
            var obj = new T { D = d, ID = d, SD = sd, SL = sl };
            var json = obj.ToJson();
            var rep = "{ 'A' : 1 }";
            var expected = "{ 'D' : #R, 'ID' : #R, 'SD' : #R, 'SL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<Dictionary<object, object>>(rehydrated.D);
            Assert.IsInstanceOf<Dictionary<object, object>>(rehydrated.ID);
            Assert.IsInstanceOf<SortedDictionary<object, object>>(rehydrated.SD);
            Assert.IsInstanceOf<SortedList<object, object>>(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOneIntWithIntKey()
        {
            var d = new Dictionary<object, object> { { 1, 2 } };
            var sd = CreateSortedDictionary(d);
            var sl = CreateSortedList(d);
            var obj = new T { D = d, ID = d, SD = sd, SL = sl };
            var json = obj.ToJson();
            var rep = "[[1, 2]]";
            var expected = "{ 'D' : #R, 'ID' : #R, 'SD' : #R, 'SL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<Dictionary<object, object>>(rehydrated.D);
            Assert.IsInstanceOf<Dictionary<object, object>>(rehydrated.ID);
            Assert.IsInstanceOf<SortedDictionary<object, object>>(rehydrated.SD);
            Assert.IsInstanceOf<SortedList<object, object>>(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOneString()
        {
            var d = new Dictionary<object, object> { { "A", "x" } };
            var sd = CreateSortedDictionary(d);
            var sl = CreateSortedList(d);
            var obj = new T { D = d, ID = d, SD = sd, SL = sl };
            var json = obj.ToJson();
            var rep = "{ 'A' : 'x' }";
            var expected = "{ 'D' : #R, 'ID' : #R, 'SD' : #R, 'SL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<Dictionary<object, object>>(rehydrated.D);
            Assert.IsInstanceOf<Dictionary<object, object>>(rehydrated.ID);
            Assert.IsInstanceOf<SortedDictionary<object, object>>(rehydrated.SD);
            Assert.IsInstanceOf<SortedList<object, object>>(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOneStringWithIntKey()
        {
            var d = new Dictionary<object, object> { { 1, "x" } };
            var sd = CreateSortedDictionary(d);
            var sl = CreateSortedList(d);
            var obj = new T { D = d, ID = d, SD = sd, SL = sl };
            var json = obj.ToJson();
            var rep = "[[1, 'x']]";
            var expected = "{ 'D' : #R, 'ID' : #R, 'SD' : #R, 'SL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<Dictionary<object, object>>(rehydrated.D);
            Assert.IsInstanceOf<Dictionary<object, object>>(rehydrated.ID);
            Assert.IsInstanceOf<SortedDictionary<object, object>>(rehydrated.SD);
            Assert.IsInstanceOf<SortedList<object, object>>(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestTwoCs()
        {
            var d = new Dictionary<object, object> { { "A", new C { P = "x" } }, { "B", new C { P = "y" } } };
            var sd = CreateSortedDictionary(d);
            var sl = CreateSortedList(d);
            var obj = new T { D = d, ID = d, SD = sd, SL = sl };
            var json = obj.ToJson();
            var reps = new Dictionary<object, object>
            {
                { "A", "{ '_t' : 'DictionaryGenericSerializers.C', 'P' : 'x' }"},
                { "B", "{ '_t' : 'DictionaryGenericSerializers.C', 'P' : 'y' }"}
            };
            var htRep = GetDocumentRepresentationInKeyOrder(d, reps);
            var sdRep = GetDocumentRepresentationInKeyOrder(sd, reps);
            var slRep = GetDocumentRepresentationInKeyOrder(sl, reps);
            var expected = "{ 'D' : #D, 'ID' : #D, 'SD' : #SD, 'SL' : #SL }";
            expected = expected.Replace("#D", htRep);
            expected = expected.Replace("#SD", sdRep);
            expected = expected.Replace("#SL", slRep);
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<Dictionary<object, object>>(rehydrated.D);
            Assert.IsInstanceOf<Dictionary<object, object>>(rehydrated.ID);
            Assert.IsInstanceOf<SortedDictionary<object, object>>(rehydrated.SD);
            Assert.IsInstanceOf<SortedList<object, object>>(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestTwoCsWithIntKeys()
        {
            var d = new Dictionary<object, object> { { 1, new C { P = "x" } }, { 2, new C { P = "y" } } };
            var sd = CreateSortedDictionary(d);
            var sl = CreateSortedList(d);
            var obj = new T { D = d, ID = d, SD = sd, SL = sl };
            var json = obj.ToJson();
            var reps = new Dictionary<object, object>
            {
                { 1, "[1, { '_t' : 'DictionaryGenericSerializers.C', 'P' : 'x' }]"},
                { 2, "[2, { '_t' : 'DictionaryGenericSerializers.C', 'P' : 'y' }]"}
            };
            var htRep = GetArrayRepresentationInKeyOrder(d, reps);
            var sdRep = GetArrayRepresentationInKeyOrder(sd, reps);
            var slRep = GetArrayRepresentationInKeyOrder(sl, reps);
            var expected = "{ 'D' : #D, 'ID' : #D, 'SD' : #SD, 'SL' : #SL }";
            expected = expected.Replace("#D", htRep);
            expected = expected.Replace("#SD", sdRep);
            expected = expected.Replace("#SL", slRep);
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<Dictionary<object, object>>(rehydrated.D);
            Assert.IsInstanceOf<Dictionary<object, object>>(rehydrated.ID);
            Assert.IsInstanceOf<SortedDictionary<object, object>>(rehydrated.SD);
            Assert.IsInstanceOf<SortedList<object, object>>(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestTwoInts()
        {
            var d = new Dictionary<object, object> { { "A", 1 }, { "B", 2 } };
            var sd = CreateSortedDictionary(d);
            var sl = CreateSortedList(d);
            var obj = new T { D = d, ID = d, SD = sd, SL = sl };
            var json = obj.ToJson();
            var reps = new Dictionary<object, object>
            {
                { "A", "1"},
                { "B", "2"}
            };
            var htRep = GetDocumentRepresentationInKeyOrder(d, reps);
            var sdRep = GetDocumentRepresentationInKeyOrder(sd, reps);
            var slRep = GetDocumentRepresentationInKeyOrder(sl, reps);
            var expected = "{ 'D' : #D, 'ID' : #D, 'SD' : #SD, 'SL' : #SL }";
            expected = expected.Replace("#D", htRep);
            expected = expected.Replace("#SD", sdRep);
            expected = expected.Replace("#SL", slRep);
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<Dictionary<object, object>>(rehydrated.D);
            Assert.IsInstanceOf<Dictionary<object, object>>(rehydrated.ID);
            Assert.IsInstanceOf<SortedDictionary<object, object>>(rehydrated.SD);
            Assert.IsInstanceOf<SortedList<object, object>>(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestTwoIntsWithIntKeys()
        {
            var d = new Dictionary<object, object> { { 1, 2 }, { 3, 4 } };
            var sd = CreateSortedDictionary(d);
            var sl = CreateSortedList(d);
            var obj = new T { D = d, ID = d, SD = sd, SL = sl };
            var json = obj.ToJson();
            var reps = new Dictionary<object, object>
            {
                { 1, "[1, 2]"},
                { 3, "[3, 4]"}
            };
            var htRep = GetArrayRepresentationInKeyOrder(d, reps);
            var sdRep = GetArrayRepresentationInKeyOrder(sd, reps);
            var slRep = GetArrayRepresentationInKeyOrder(sl, reps);
            var expected = "{ 'D' : #D, 'ID' : #D, 'SD' : #SD, 'SL' : #SL }";
            expected = expected.Replace("#D", htRep);
            expected = expected.Replace("#SD", sdRep);
            expected = expected.Replace("#SL", slRep);
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<Dictionary<object, object>>(rehydrated.D);
            Assert.IsInstanceOf<Dictionary<object, object>>(rehydrated.ID);
            Assert.IsInstanceOf<SortedDictionary<object, object>>(rehydrated.SD);
            Assert.IsInstanceOf<SortedList<object, object>>(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestTwoStrings()
        {
            var d = new Dictionary<object, object> { { "A", "x" }, { "B", "y" } };
            var sd = CreateSortedDictionary(d);
            var sl = CreateSortedList(d);
            var obj = new T { D = d, ID = d, SD = sd, SL = sl };
            var json = obj.ToJson();
            var reps = new Dictionary<object, object>
            {
                { "A", "'x'"},
                { "B", "'y'"}
            };
            var htRep = GetDocumentRepresentationInKeyOrder(d, reps);
            var sdRep = GetDocumentRepresentationInKeyOrder(sd, reps);
            var slRep = GetDocumentRepresentationInKeyOrder(sl, reps);
            var expected = "{ 'D' : #D, 'ID' : #D, 'SD' : #SD, 'SL' : #SL }";
            expected = expected.Replace("#D", htRep);
            expected = expected.Replace("#SD", sdRep);
            expected = expected.Replace("#SL", slRep);
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<Dictionary<object, object>>(rehydrated.D);
            Assert.IsInstanceOf<Dictionary<object, object>>(rehydrated.ID);
            Assert.IsInstanceOf<SortedDictionary<object, object>>(rehydrated.SD);
            Assert.IsInstanceOf<SortedList<object, object>>(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestTwoStringsWithIntKeys()
        {
            var d = new Dictionary<object, object> { { 1, "x" }, { 2, "y" } };
            var sd = CreateSortedDictionary(d);
            var sl = CreateSortedList(d);
            var obj = new T { D = d, ID = d, SD = sd, SL = sl };
            var json = obj.ToJson();
            var reps = new Dictionary<object, object>
            {
                { 1, "[1, 'x']"},
                { 2, "[2, 'y']"}
            };
            var htRep = GetArrayRepresentationInKeyOrder(d, reps);
            var sdRep = GetArrayRepresentationInKeyOrder(sd, reps);
            var slRep = GetArrayRepresentationInKeyOrder(sl, reps);
            var expected = "{ 'D' : #D, 'ID' : #D, 'SD' : #SD, 'SL' : #SL }";
            expected = expected.Replace("#D", htRep);
            expected = expected.Replace("#SD", sdRep);
            expected = expected.Replace("#SL", slRep);
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<Dictionary<object, object>>(rehydrated.D);
            Assert.IsInstanceOf<Dictionary<object, object>>(rehydrated.ID);
            Assert.IsInstanceOf<SortedDictionary<object, object>>(rehydrated.SD);
            Assert.IsInstanceOf<SortedList<object, object>>(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMixedPrimitiveTypes()
        {
            var dateTime = DateTime.SpecifyKind(new DateTime(2010, 1, 1, 11, 22, 33), DateTimeKind.Utc);
            var isoDate = dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFZ");
            var guid = Guid.Empty;
            var objectId = ObjectId.Empty;
            var d = new Dictionary<object, object>
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
            var sd = CreateSortedDictionary(d);
            var sl = CreateSortedList(d);
            var obj = new T { D = d, ID = d, SD = sd, SL = sl };
            var json = obj.ToJson();
            var reps = new Dictionary<object, object>
            {
                { "A", "true" }, 
                { "B", string.Format("ISODate('{0}')", isoDate) },
                { "C", "1.5" }, 
                { "D", "1" }, 
                { "E", "NumberLong(2)" },
                { "F", "CSUUID('00000000-0000-0000-0000-000000000000')" }, 
                { "G", "ObjectId('000000000000000000000000')" }, 
                { "H", "'x'" }
            };
            var htRep = GetDocumentRepresentationInKeyOrder(d, reps);
            var sdRep = GetDocumentRepresentationInKeyOrder(sd, reps);
            var slRep = GetDocumentRepresentationInKeyOrder(sl, reps);
            var expected = "{ 'D' : #D, 'ID' : #D, 'SD' : #SD, 'SL' : #SL }";
            expected = expected.Replace("#D", htRep);
            expected = expected.Replace("#SD", sdRep);
            expected = expected.Replace("#SL", slRep);
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<Dictionary<object, object>>(rehydrated.D);
            Assert.IsInstanceOf<Dictionary<object, object>>(rehydrated.ID);
            Assert.IsInstanceOf<SortedDictionary<object, object>>(rehydrated.SD);
            Assert.IsInstanceOf<SortedList<object, object>>(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMixedPrimitiveTypesWithIntKeys()
        {
            var dateTime = DateTime.SpecifyKind(new DateTime(2010, 1, 1, 11, 22, 33), DateTimeKind.Utc);
            var isoDate = dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFZ");
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
            var sd = CreateSortedDictionary(d);
            var sl = CreateSortedList(d);
            var obj = new T { D = d, ID = d, SD = sd, SL = sl };
            var json = obj.ToJson();
            var reps = new Dictionary<object, object>
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
            var htRep = GetArrayRepresentationInKeyOrder(d, reps);
            var sdRep = GetArrayRepresentationInKeyOrder(sd, reps);
            var slRep = GetArrayRepresentationInKeyOrder(sl, reps);
            var expected = "{ 'D' : #D, 'ID' : #D, 'SD' : #SD, 'SL' : #SL }";
            expected = expected.Replace("#D", htRep);
            expected = expected.Replace("#SD", sdRep);
            expected = expected.Replace("#SL", slRep);
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<Dictionary<object, object>>(rehydrated.D);
            Assert.IsInstanceOf<Dictionary<object, object>>(rehydrated.ID);
            Assert.IsInstanceOf<SortedDictionary<object, object>>(rehydrated.SD);
            Assert.IsInstanceOf<SortedList<object, object>>(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMixedPrimitiveTypesWithMixedKeys()
        {
            // note: no SortedDictionary or SortedList in this test because you can't sort a set of keys that have mixed types
            var dateTime = DateTime.SpecifyKind(new DateTime(2010, 1, 1, 11, 22, 33), DateTimeKind.Utc);
            var isoDate = dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFZ");
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
            var json = obj.ToJson();
            var reps = new Dictionary<object, object>
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
            var htRep = GetArrayRepresentationInKeyOrder(d, reps);
            var expected = "{ 'D' : #D, 'ID' : #D, 'SD' : null, 'SL' : null }";
            expected = expected.Replace("#D", htRep);
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<Dictionary<object, object>>(rehydrated.D);
            Assert.IsInstanceOf<Dictionary<object, object>>(rehydrated.ID);
            Assert.IsNull(rehydrated.SD);
            Assert.IsNull(rehydrated.SL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
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
            public Dictionary<string, E> Dictionary;
        }

        // required for deterministic tests
        private class D
        {
            [BsonRepresentation(BsonType.String)]
            public SortedList<string, E> Dictionary;
        }

        [Test]
        public void TestSerializeNull()
        {
            C c = new C { Dictionary = null };
            var json = c.ToJson();
            var expected = ("{ 'Dictionary' : null }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeEmpty()
        {
            C c = new C { Dictionary = new Dictionary<string, E>() };
            var json = c.ToJson();
            var expected = ("{ 'Dictionary' : { } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerialize1()
        {
            C c = new C { Dictionary = new Dictionary<string, E> { { "a", E.A } } };
            var json = c.ToJson();
            var expected = ("{ 'Dictionary' : { \"a\" : \"A\" } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerialize2()
        {
            D d = new D { Dictionary = new SortedList<string, E> { { "a", E.A }, { "b", E.B } } };
            var json = d.ToJson();
            var expected = ("{ 'Dictionary' : { \"a\" : \"A\", \"b\" : \"B\" } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = d.ToBson();
            var rehydrated = BsonSerializer.Deserialize<D>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    [TestFixture]
    public class StringToObjectIdDictionaryTests
    {
        private class C
        {
            [BsonRepresentation(BsonType.ObjectId)]
            public Dictionary<string, string> Dictionary;
        }

        private static string id1 = "123456789012345678901234";
        private static string id2 = "432109876543210987654321";

        [Test]
        public void TestSerializeNull()
        {
            C c = new C { Dictionary = null };
            var json = c.ToJson();
            var expected = ("{ 'Dictionary' : null }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeEmpty()
        {
            C c = new C { Dictionary = new Dictionary<string, string>() };
            var json = c.ToJson();
            var expected = ("{ 'Dictionary' : { } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerialize1()
        {
            C c = new C { Dictionary = new Dictionary<string, string> { { "a", id1 } } };
            var json = c.ToJson();
            var expected = ("{ 'Dictionary' : { \"a\" : ObjectId(\"123456789012345678901234\") } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerialize2()
        {
            C c = new C { Dictionary = new Dictionary<string, string> { { "a", id1 }, { "b", id2 } } };
            var json = c.ToJson();
            var expected1 = ("{ 'Dictionary' : { \"a\" : ObjectId(\"123456789012345678901234\"), \"b\" : ObjectId(\"432109876543210987654321\") } }").Replace("'", "\"");
            var expected2 = ("{ 'Dictionary' : { \"b\" : ObjectId(\"432109876543210987654321\"), \"a\" : ObjectId(\"123456789012345678901234\") } }").Replace("'", "\"");
            if (json != expected1 && json != expected2)
            {
                Assert.AreEqual(expected1, json);
            }

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
