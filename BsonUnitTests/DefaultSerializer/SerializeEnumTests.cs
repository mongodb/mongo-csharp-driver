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
    public class SerializeEnumTests {
        // TODO: add unit tests for other underlying types
        private enum E {
            A = 1,
            B = 2
        }

        private class C {
            [BsonRepresentation(BsonType.Int32)]
            public E I { get; set; }
            [BsonRepresentation(BsonType.String)]
            public E S { get; set; }
        }

        [Test]
        public void TestSerializeZero() {
            C c = new C { I = 0, S = 0 };
            var json = c.ToJson();
            var expected = ("{ 'I' : 0, 'S' : '0' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeA() {
            C c = new C { I = E.A, S = E.A };
            var json = c.ToJson();
            var expected = ("{ 'I' : 1, 'S' : 'A' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeB() {
            C c = new C { I = E.B, S = E.B };
            var json = c.ToJson();
            var expected = ("{ 'I' : 2, 'S' : 'B' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeInvalid() {
            C c = new C { I = (E) 123, S = (E) 123 };
            var json = c.ToJson();
            var expected = ("{ 'I' : 123, 'S' : '123' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
