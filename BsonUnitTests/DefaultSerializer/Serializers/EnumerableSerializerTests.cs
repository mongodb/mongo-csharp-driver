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
using MongoDB.Bson.Serialization;

namespace MongoDB.BsonUnitTests.DefaultSerializer.EnumerableSerializer {
    public class C {
        public string P { get; set; }
    }

    [TestFixture]
    public class IListSerializerTests {
        public class T {
            public IList E { get; set; }
        }

        [Test]
        public void TestNull() {
            var obj = new T { E = null };
            var json = obj.ToJson();
            var expected = "{ 'E' : null }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<T>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestEmpty() {
            var obj = new T { E = new ArrayList() };
            var json = obj.ToJson();
            var expected = "{ 'E' : [] }";
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<T>(bson);
            Assert.IsInstanceOf<ArrayList>(rehydrated.E);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOneC() {
            var obj = new T { E = new ArrayList(new [] { new C { P = "x" } }) };
            var json = obj.ToJson();
            var expected = "{ 'E' : [#C1] }";
            expected = expected.Replace("#C1", "{ '_t' : 'C', 'P' : 'x' }");
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<T>(bson);
            Assert.IsInstanceOf<ArrayList>(rehydrated.E);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOneInt() {
            var obj = new T { E = new ArrayList(new[] { 1 }) };
            var json = obj.ToJson();
            var expected = "{ 'E' : [1] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<T>(bson);
            Assert.IsInstanceOf<ArrayList>(rehydrated.E);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOneString() {
            var obj = new T { E = new ArrayList(new[] { "x" }) };
            var json = obj.ToJson();
            var expected = "{ 'E' : ['x'] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<T>(bson);
            Assert.IsInstanceOf<ArrayList>(rehydrated.E);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestTwoCs() {
            var obj = new T { E = new ArrayList(new[] { new C { P = "x" }, new C { P = "y" } }) };
            var json = obj.ToJson();
            var expected = "{ 'E' : [#C1, #C2] }";
            expected = expected.Replace("#C1", "{ '_t' : 'C', 'P' : 'x' }");
            expected = expected.Replace("#C2", "{ '_t' : 'C', 'P' : 'y' }");
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<T>(bson);
            Assert.IsInstanceOf<ArrayList>(rehydrated.E);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestTwoInts() {
            var obj = new T { E = new ArrayList(new[] { 1, 2 }) };
            var json = obj.ToJson();
            var expected = "{ 'E' : [1, 2] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<T>(bson);
            Assert.IsInstanceOf<ArrayList>(rehydrated.E);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestTwoStrings() {
            var obj = new T { E = new ArrayList(new[] { "x", "y" }) };
            var json = obj.ToJson();
            var expected = "{ 'E' : ['x', 'y'] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<T>(bson);
            Assert.IsInstanceOf<ArrayList>(rehydrated.E);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMixedPrimitiveTypes() {
            var dateTime = DateTime.SpecifyKind(new DateTime(2010, 1, 1, 11, 22, 33), DateTimeKind.Utc);
            var millis = (long) ((dateTime - BsonConstants.UnixEpoch).TotalMilliseconds);
            var objectId = ObjectId.Empty;
            var obj = new T { E = new ArrayList(new object[] { true, dateTime, 1.5, 1, 2L, objectId, "x" }) };
            var json = obj.ToJson();
            var expected = "{ 'E' : [true, { '$date' : #ms }, 1.5, 1, 2, { '$oid' : '000000000000000000000000' }, 'x'] }";
            expected = expected.Replace("#ms", millis.ToString());
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<T>(bson);
            Assert.IsInstanceOf<ArrayList>(rehydrated.E);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
