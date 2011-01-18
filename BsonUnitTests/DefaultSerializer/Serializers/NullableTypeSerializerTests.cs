/* Copyright 2010-2011 10gen Inc.
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
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.DefaultSerializer;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.BsonUnitTests.DefaultSerializer {
    [TestFixture]
    public class NullableTypeSerializerTests {
        private class C {
            public bool? Boolean { get; set; }
            public DateTime? DateTime { get; set; }
            [BsonDateTimeOptions(DateOnly = true)]
            public DateTime? DateOnly { get; set; }
            public double? Double { get; set; }
            public Guid? Guid { get; set; }
            public int? Int32 { get; set; }
            public long? Int64 { get; set; }
            public ObjectId? ObjectId { get; set; }
            // public Struct? Struct { get; set; }
        }

        //private struct Struct {
        //    public string StructP { get; set; }
        //}

        private const string template =
            "{ " +
            "'Boolean' : null, " +
            "'DateTime' : null, " +
            "'DateOnly' : null, " +
            "'Double' : null, " +
            "'Guid' : null, " +
            "'Int32' : null, " +
            "'Int64' : null, " +
            "'ObjectId' : null" +
            // "'Struct' : null" +
            " }";

        [Test]
        public void TestAllNulls() {
            C c = new C();
            var json = c.ToJson();
            var expected = template.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestBoolean() {
            C c = new C { Boolean = true };
            var json = c.ToJson();
            var expected = template.Replace("'Boolean' : null", "'Boolean' : true").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestDateTime() {
            C c = new C { DateTime = BsonConstants.UnixEpoch };
            var json = c.ToJson();
            var expected = template.Replace("'DateTime' : null", "'DateTime' : { '$date' : 0 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestDateOnly() {
            C c = new C { DateOnly = BsonConstants.UnixEpoch };
            var json = c.ToJson();
            var expected = template.Replace("'DateOnly' : null", "'DateOnly' : { '$date' : 0 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestDouble() {
            C c = new C { Double = 1.5 };
            var json = c.ToJson();
            var expected = template.Replace("'Double' : null", "'Double' : 1.5").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestGuid() {
            C c = new C { Guid = Guid.Empty };
            var json = c.ToJson();
            var expected = template.Replace("'Guid' : null", "'Guid' : { '$binary' : 'AAAAAAAAAAAAAAAAAAAAAA==', '$type' : '03' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestInt32() {
            C c = new C { Int32 = 1 };
            var json = c.ToJson();
            var expected = template.Replace("'Int32' : null", "'Int32' : 1").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestInt64() {
            C c = new C { Int64 = 2 };
            var json = c.ToJson();
            var expected = template.Replace("'Int64' : null", "'Int64' : 2").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestObjectId() {
            C c = new C { ObjectId = ObjectId.Empty };
            var json = c.ToJson();
            var expected = template.Replace("'ObjectId' : null", "'ObjectId' : { '$oid' : '000000000000000000000000' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        //[Test]
        //public void TestStruct() {
        //    C c = new C { Struct = new Struct { StructP = "x" } };
        //    var json = c.ToJson();
        //    var expected = template.Replace("'Struct' : null", "'Struct' : { 'StructP' : 'x' }").Replace("'", "\"");
        //    Assert.AreEqual(expected, json);

        //    var bson = c.ToBson();
        //    var rehydrated = BsonSerializer.Deserialize<C>(bson);
        //    Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        //}
    }
}
