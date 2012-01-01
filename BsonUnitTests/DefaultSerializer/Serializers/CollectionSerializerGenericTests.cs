/* Copyright 2010-2012 10gen Inc.
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
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.BsonUnitTests.Serialization.CollectionSerializersGeneric
{
    [BsonDiscriminator("CollectionSerializersGeneric.C")] // "C" is an ambiguous discriminator when nominalType is System.Object
    public class C
    {
        public string P { get; set; }
    }

    [TestFixture]
    public class EnumerableSerializerTests
    {
        public class T
        {
            public List<object> L { get; set; }
            public ICollection<object> IC { get; set; }
            public IEnumerable<object> IE { get; set; }
            public IList<object> IL { get; set; }
            public Queue<object> Q { get; set; }
            public Stack<object> S { get; set; }
            public HashSet<object> H { get; set; }
            public LinkedList<object> LL { get; set; }
        }

        [Test]
        public void TestNull()
        {
            var obj = new T { L = null, IC = null, IE = null, IL = null, Q = null, S = null, H = null, LL = null };
            var json = obj.ToJson();
            var rep = "null";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R, 'H' : #R, 'LL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsNull(rehydrated.L);
            Assert.IsNull(rehydrated.Q);
            Assert.IsNull(rehydrated.S);
            Assert.IsNull(rehydrated.IC);
            Assert.IsNull(rehydrated.IE);
            Assert.IsNull(rehydrated.IL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestEmpty()
        {
            var list = new List<object>();
            var obj = new T { L = list, IC = list, IE = list, IL = list, Q = new Queue<object>(list), S = new Stack<object>(list), H = new HashSet<object>(list), LL = new LinkedList<object>(list) };
            var json = obj.ToJson();
            var rep = "[]";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R, 'H' : #R, 'LL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<List<object>>(rehydrated.L);
            Assert.IsInstanceOf<Queue<object>>(rehydrated.Q);
            Assert.IsInstanceOf<Stack<object>>(rehydrated.S);
            Assert.IsInstanceOf<List<object>>(rehydrated.IC);
            Assert.IsInstanceOf<List<object>>(rehydrated.IE);
            Assert.IsInstanceOf<List<object>>(rehydrated.IL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOneC()
        {
            var list = new List<object>(new[] { new C { P = "x" } });
            var obj = new T { L = list, IC = list, IE = list, IL = list, Q = new Queue<object>(list), S = new Stack<object>(list), H = new HashSet<object>(list), LL = new LinkedList<object>(list) };
            var json = obj.ToJson();
            var rep = "[{ '_t' : 'CollectionSerializersGeneric.C', 'P' : 'x' }]";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R, 'H' : #R, 'LL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<List<object>>(rehydrated.L);
            Assert.IsInstanceOf<Queue<object>>(rehydrated.Q);
            Assert.IsInstanceOf<Stack<object>>(rehydrated.S);
            Assert.IsInstanceOf<List<object>>(rehydrated.IC);
            Assert.IsInstanceOf<List<object>>(rehydrated.IE);
            Assert.IsInstanceOf<List<object>>(rehydrated.IL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOneInt()
        {
            var list = new List<object>(new object[] { 1 });
            var obj = new T { L = list, IC = list, IE = list, IL = list, Q = new Queue<object>(list), S = new Stack<object>(list), H = new HashSet<object>(list), LL = new LinkedList<object>(list) };
            var json = obj.ToJson();
            var rep = "[1]";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R, 'H' : #R, 'LL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<List<object>>(rehydrated.L);
            Assert.IsInstanceOf<Queue<object>>(rehydrated.Q);
            Assert.IsInstanceOf<Stack<object>>(rehydrated.S);
            Assert.IsInstanceOf<List<object>>(rehydrated.IC);
            Assert.IsInstanceOf<List<object>>(rehydrated.IE);
            Assert.IsInstanceOf<List<object>>(rehydrated.IL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestOneString()
        {
            var list = new List<object>(new[] { "x" });
            var obj = new T { L = list, IC = list, IE = list, IL = list, Q = new Queue<object>(list), S = new Stack<object>(list), H = new HashSet<object>(list), LL = new LinkedList<object>(list) };
            var json = obj.ToJson();
            var rep = "['x']";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R, 'H' : #R, 'LL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<List<object>>(rehydrated.L);
            Assert.IsInstanceOf<Queue<object>>(rehydrated.Q);
            Assert.IsInstanceOf<Stack<object>>(rehydrated.S);
            Assert.IsInstanceOf<List<object>>(rehydrated.IC);
            Assert.IsInstanceOf<List<object>>(rehydrated.IE);
            Assert.IsInstanceOf<List<object>>(rehydrated.IL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestTwoCs()
        {
            var list = new List<object>(new[] { new C { P = "x" }, new C { P = "y" } });
            var obj = new T { L = list, IC = list, IE = list, IL = list, Q = new Queue<object>(list), S = new Stack<object>(list), H = new HashSet<object>(list), LL = new LinkedList<object>(list) };
            var json = obj.ToJson();
            var rep = "[{ '_t' : 'CollectionSerializersGeneric.C', 'P' : 'x' }, { '_t' : 'CollectionSerializersGeneric.C', 'P' : 'y' }]";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R, 'H' : #R, 'LL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<List<object>>(rehydrated.L);
            Assert.IsInstanceOf<Queue<object>>(rehydrated.Q);
            Assert.IsInstanceOf<Stack<object>>(rehydrated.S);
            Assert.IsInstanceOf<List<object>>(rehydrated.IC);
            Assert.IsInstanceOf<List<object>>(rehydrated.IE);
            Assert.IsInstanceOf<List<object>>(rehydrated.IL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestTwoInts()
        {
            var list = new List<object>(new object[] { 1, 2 });
            var obj = new T { L = list, IC = list, IE = list, IL = list, Q = new Queue<object>(list), S = new Stack<object>(list), H = new HashSet<object>(list), LL = new LinkedList<object>(list) };
            var json = obj.ToJson();
            var rep = "[1, 2]";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R, 'H' : #R, 'LL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<List<object>>(rehydrated.L);
            Assert.IsInstanceOf<Queue<object>>(rehydrated.Q);
            Assert.IsInstanceOf<Stack<object>>(rehydrated.S);
            Assert.IsInstanceOf<List<object>>(rehydrated.IC);
            Assert.IsInstanceOf<List<object>>(rehydrated.IE);
            Assert.IsInstanceOf<List<object>>(rehydrated.IL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestTwoStrings()
        {
            var list = new List<object>(new[] { "x", "y" });
            var obj = new T { L = list, IC = list, IE = list, IL = list, Q = new Queue<object>(list), S = new Stack<object>(list), H = new HashSet<object>(list), LL = new LinkedList<object>(list) };
            var json = obj.ToJson();
            var rep = "['x', 'y']";
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R, 'H' : #R, 'LL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<List<object>>(rehydrated.L);
            Assert.IsInstanceOf<Queue<object>>(rehydrated.Q);
            Assert.IsInstanceOf<Stack<object>>(rehydrated.S);
            Assert.IsInstanceOf<List<object>>(rehydrated.IC);
            Assert.IsInstanceOf<List<object>>(rehydrated.IE);
            Assert.IsInstanceOf<List<object>>(rehydrated.IL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestMixedPrimitiveTypes()
        {
            var dateTime = DateTime.SpecifyKind(new DateTime(2010, 1, 1, 11, 22, 33), DateTimeKind.Utc);
            var isoDate = string.Format("ISODate(\"{0}\")", dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFZ"));
            var guid = Guid.Empty;
            var objectId = ObjectId.Empty;
            var list = new List<object>(new object[] { true, dateTime, 1.5, 1, 2L, guid, objectId, "x" });
            var obj = new T { L = list, IC = list, IE = list, IL = list, Q = new Queue<object>(list), S = new Stack<object>(list), H = new HashSet<object>(list), LL = new LinkedList<object>(list) };
            var json = obj.ToJson();
            var rep = "[true, #Date, 1.5, 1, NumberLong(2), #Guid, #ObjectId, 'x']";
            rep = rep.Replace("#Date", isoDate);
            rep = rep.Replace("#Guid", "CSUUID('00000000-0000-0000-0000-000000000000')");
            rep = rep.Replace("#ObjectId", "ObjectId('000000000000000000000000')");
            var expected = "{ 'L' : #R, 'IC' : #R, 'IE' : #R, 'IL' : #R, 'Q' : #R, 'S' : #R, 'H' : #R, 'LL' : #R }".Replace("#R", rep).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<List<object>>(rehydrated.L);
            Assert.IsInstanceOf<Queue<object>>(rehydrated.Q);
            Assert.IsInstanceOf<Stack<object>>(rehydrated.S);
            Assert.IsInstanceOf<List<object>>(rehydrated.IC);
            Assert.IsInstanceOf<List<object>>(rehydrated.IE);
            Assert.IsInstanceOf<List<object>>(rehydrated.IL);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
