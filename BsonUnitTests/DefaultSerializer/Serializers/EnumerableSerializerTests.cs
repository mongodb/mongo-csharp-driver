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

namespace MongoDB.BsonUnitTests.DefaultSerializer.EnumerableSerializer {
    [BsonDiscriminator("EnumerableSerializer.C")] // "C" is an ambiguous discriminator when nominalType is System.Object
    public class C {
        public string P { get; set; }
    }

    [TestFixture]
    public class EnumerableSerializerTests {
        public class T {
            public ArrayList AL { get; set; }
            public ICollection IC { get; set; }
            public IEnumerable IE { get; set; }
            public IList IL { get; set; }
        }

        [Test]
        public void TestNull() {
            ArrayList list = null;
            var obj = new T { AL = list, IC = list, IE = list, IL = list };
            var json = obj.ToJson();
            var rep = "null";
            var expected = "{ 'AL' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<T>(bson);
            Assert.IsNull(rehydrated.AL);
            Assert.IsNull(rehydrated.IC);
            Assert.IsNull(rehydrated.IE);
            Assert.IsNull(rehydrated.IL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestEmpty() {
            var list = new ArrayList();
            var obj = new T { AL = list, IC = list, IE = list, IL = list };
            var json = obj.ToJson();
            var rep = "[]";
            var expected = "{ 'AL' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<T>(bson);
            Assert.IsInstanceOf<ArrayList>(rehydrated.AL);
            Assert.IsInstanceOf<ArrayList>(rehydrated.IC);
            Assert.IsInstanceOf<ArrayList>(rehydrated.IE);
            Assert.IsInstanceOf<ArrayList>(rehydrated.IL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOneC() {
            var list = new ArrayList(new [] { new C { P = "x" } });
            var obj = new T { AL = list, IC = list, IE = list, IL = list };
            var json = obj.ToJson();
            var rep = "[{ '_t' : 'EnumerableSerializer.C', 'P' : 'x' }]";
            var expected = "{ 'AL' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<T>(bson);
            Assert.IsInstanceOf<ArrayList>(rehydrated.AL);
            Assert.IsInstanceOf<ArrayList>(rehydrated.IC);
            Assert.IsInstanceOf<ArrayList>(rehydrated.IE);
            Assert.IsInstanceOf<ArrayList>(rehydrated.IL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOneInt() {
            var list = new ArrayList(new[] { 1 });
            var obj = new T { AL = list, IC = list, IE = list, IL = list };
            var json = obj.ToJson();
            var rep = "[1]";
            var expected = "{ 'AL' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<T>(bson);
            Assert.IsInstanceOf<ArrayList>(rehydrated.AL);
            Assert.IsInstanceOf<ArrayList>(rehydrated.IC);
            Assert.IsInstanceOf<ArrayList>(rehydrated.IE);
            Assert.IsInstanceOf<ArrayList>(rehydrated.IL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOneString() {
            var list = new ArrayList(new[] { "x" });
            var obj = new T { AL = list, IC = list, IE = list, IL = list };
            var json = obj.ToJson();
            var rep = "['x']";
            var expected = "{ 'AL' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<T>(bson);
            Assert.IsInstanceOf<ArrayList>(rehydrated.AL);
            Assert.IsInstanceOf<ArrayList>(rehydrated.IC);
            Assert.IsInstanceOf<ArrayList>(rehydrated.IE);
            Assert.IsInstanceOf<ArrayList>(rehydrated.IL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestTwoCs() {
            var list = new ArrayList(new[] { new C { P = "x" }, new C { P = "y" } });
            var obj = new T { AL = list, IC = list, IE = list, IL = list };
            var json = obj.ToJson();
            var rep = "[{ '_t' : 'EnumerableSerializer.C', 'P' : 'x' }, { '_t' : 'EnumerableSerializer.C', 'P' : 'y' }]";
            var expected = "{ 'AL' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<T>(bson);
            Assert.IsInstanceOf<ArrayList>(rehydrated.AL);
            Assert.IsInstanceOf<ArrayList>(rehydrated.IC);
            Assert.IsInstanceOf<ArrayList>(rehydrated.IE);
            Assert.IsInstanceOf<ArrayList>(rehydrated.IL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestTwoInts() {
            var list = new ArrayList(new[] { 1, 2 });
            var obj = new T { AL = list, IC = list, IE = list, IL = list };
            var json = obj.ToJson();
            var rep = "[1, 2]";
            var expected = "{ 'AL' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<T>(bson);
            Assert.IsInstanceOf<ArrayList>(rehydrated.AL);
            Assert.IsInstanceOf<ArrayList>(rehydrated.IC);
            Assert.IsInstanceOf<ArrayList>(rehydrated.IE);
            Assert.IsInstanceOf<ArrayList>(rehydrated.IL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestTwoStrings() {
            var list = new ArrayList(new[] { "x", "y" });
            var obj = new T { AL = list, IC = list, IE = list, IL = list };
            var json = obj.ToJson();
            var rep = "['x', 'y']";
            var expected = "{ 'AL' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<T>(bson);
            Assert.IsInstanceOf<ArrayList>(rehydrated.AL);
            Assert.IsInstanceOf<ArrayList>(rehydrated.IC);
            Assert.IsInstanceOf<ArrayList>(rehydrated.IE);
            Assert.IsInstanceOf<ArrayList>(rehydrated.IL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMixedPrimitiveTypes() {
            var dateTime = DateTime.SpecifyKind(new DateTime(2010, 1, 1, 11, 22, 33), DateTimeKind.Utc);
            var millis = (long) ((dateTime - BsonConstants.UnixEpoch).TotalMilliseconds);
            var guid = Guid.Empty;
            var objectId = ObjectId.Empty;
            var list = new ArrayList(new object[] { true, dateTime, 1.5, 1, 2L, guid, objectId, "x" });
            var obj = new T { AL = list, IC = list, IE = list, IL = list };
            var json = obj.ToJson();
            var rep = "[true, #Date, 1.5, 1, 2, #Guid, #ObjectId, 'x']";
            rep = rep.Replace("#Date", "{ '$date' : #ms }".Replace("#ms", millis.ToString()));
            rep = rep.Replace("#Guid", "{ '$binary' : 'AAAAAAAAAAAAAAAAAAAAAA==', '$type' : '03' }");
            rep = rep.Replace("#ObjectId", "{ '$oid' : '000000000000000000000000' }");
            var expected = "{ 'AL' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<T>(bson);
            Assert.IsInstanceOf<ArrayList>(rehydrated.AL);
            Assert.IsInstanceOf<ArrayList>(rehydrated.IC);
            Assert.IsInstanceOf<ArrayList>(rehydrated.IE);
            Assert.IsInstanceOf<ArrayList>(rehydrated.IL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
