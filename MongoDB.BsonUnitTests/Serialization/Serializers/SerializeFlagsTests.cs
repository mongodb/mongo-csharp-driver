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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Serialization
{
    [TestFixture]
    public class SerializeFlagsTests
    {
        // TODO: add unit tests for other underlying types
        [Flags]
        private enum F
        {
            A = 1,
            B = 2
        }

        private class C
        {
            [BsonRepresentation(BsonType.Int32)]
            public F IF { get; set; }
            [BsonRepresentation(BsonType.String)]
            public F SF { get; set; }
        }

        [Test]
        public void TestSerializeZero()
        {
            C c = new C { IF = 0, SF = 0 };
            var json = c.ToJson();
            var expected = ("{ 'IF' : 0, 'SF' : '0' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeA()
        {
            C c = new C { IF = F.A, SF = F.A };
            var json = c.ToJson();
            var expected = ("{ 'IF' : 1, 'SF' : 'A' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeB()
        {
            C c = new C { IF = F.B, SF = F.B };
            var json = c.ToJson();
            var expected = ("{ 'IF' : 2, 'SF' : 'B' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeAB()
        {
            C c = new C { IF = F.A | F.B, SF = F.A | F.B };
            var json = c.ToJson();
            var expected = ("{ 'IF' : 3, 'SF' : 'A, B' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeInvalid()
        {
            C c = new C { IF = (F)127, SF = (F)127 };
            var json = c.ToJson();
            var expected = ("{ 'IF' : 127, 'SF' : '127' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
