/* Copyright 2010 10gen Inc.
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
using System.Linq;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.DefaultSerializer;
using MongoDB.Bson.Serialization;

namespace MongoDB.BsonUnitTests.DefaultSerializer.StackSerializer {
    [BsonDiscriminator("StackSerializer.C")] // "C" is an ambiguous discriminator when nominalType is System.Object
    public class C {
        public string P { get; set; }
    }

    [TestFixture]
    public class StackSerializerTests {
        public class T {
            public Stack S { get; set; }
        }

        [Test]
        public void TestNull() {
            var obj = new T { S = null };
            var json = obj.ToJson();
            var expected = "{ 'S' : null }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<T>(bson);
            Assert.IsNull(rehydrated.S);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestEmpty() {
            var obj = new T { S = new Stack() };
            var json = obj.ToJson();
            var expected = "{ 'S' : [] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<T>(bson);
            Assert.IsInstanceOf<Stack>(rehydrated.S);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOneC() {
            var obj = new T { S = new Stack(new[] { new C { P = "x" } }) };
            var json = obj.ToJson();
            var expected = "{ 'S' : [{ '_t' : 'StackSerializer.C', 'P' : 'x' }] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<T>(bson);
            Assert.IsInstanceOf<Stack>(rehydrated.S);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOneInt() {
            var obj = new T { S = new Stack(new[] { 1 }) };
            var json = obj.ToJson();
            var expected = "{ 'S' : [1] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<T>(bson);
            Assert.IsInstanceOf<Stack>(rehydrated.S);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOneString() {
            var obj = new T { S = new Stack(new[] { "x" }) };
            var json = obj.ToJson();
            var expected = "{ 'S' : ['x'] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<T>(bson);
            Assert.IsInstanceOf<Stack>(rehydrated.S);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestTwoCs() {
            var obj = new T { S = new Stack(new[] { new C { P = "x" }, new C { P = "y" } }) };
            var json = obj.ToJson();
            var expected = "{ 'S' : [{ '_t' : 'StackSerializer.C', 'P' : 'x' }, { '_t' : 'StackSerializer.C', 'P' : 'y' }] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<T>(bson);
            Assert.IsInstanceOf<Stack>(rehydrated.S);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestTwoInts() {
            var obj = new T { S = new Stack(new[] { 1, 2 }) };
            var json = obj.ToJson();
            var expected = "{ 'S' : [1, 2] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<T>(bson);
            Assert.IsInstanceOf<Stack>(rehydrated.S);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestTwoStrings() {
            var obj = new T { S = new Stack(new[] { "x", "y" }) };
            var json = obj.ToJson();
            var expected = "{ 'S' : ['x', 'y'] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<T>(bson);
            Assert.IsInstanceOf<Stack>(rehydrated.S);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMixedPrimitiveTypes() {
            var dateTime = DateTime.SpecifyKind(new DateTime(2010, 1, 1, 11, 22, 33), DateTimeKind.Utc);
            var millis = (long) ((dateTime - BsonConstants.UnixEpoch).TotalMilliseconds);
            var guid = Guid.Empty;
            var objectId = ObjectId.Empty;
            var obj = new T { S = new Stack(new object[] { true, dateTime, 1.5, 1, 2L, guid, objectId, "x" }) };
            var json = obj.ToJson();
            var expected = "{ 'S' : [true, #Date, 1.5, 1, 2, #Guid, #ObjectId, 'x'] }";
            expected = expected.Replace("#Date", "{ '$date' : #ms }".Replace("#ms", millis.ToString()));
            expected = expected.Replace("#Guid", "{ '$binary' : 'AAAAAAAAAAAAAAAAAAAAAA==', '$type' : '03' }");
            expected = expected.Replace("#ObjectId", "{ '$oid' : '000000000000000000000000' }");
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<T>(bson);
            Assert.IsInstanceOf<Stack>(rehydrated.S);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
