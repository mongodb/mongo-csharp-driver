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

using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Serialization
{
    [TestFixture]
    public class SerializeInterfaceTests
    {
        private interface IX
        {
            string FX { get; set; }
        }

        private class A : IX
        {
            public string FX { get; set; }
        }

        private class B : IX
        {
            public string FX { get; set; }
        }

        [Test]
        public void TestSerializeAasA()
        {
            A a = new A { FX = "a" };
            var json = a.ToJson();
            var expected = ("{ 'FX' : 'a' }").Replace("'", "\""); // no discriminator
            Assert.AreEqual(expected, json);

            var bson = a.ToBson();
            var rehydrated = BsonSerializer.Deserialize<A>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeAasIX()
        {
            IX a = new A { FX = "a" };
            var json = a.ToJson();
            var expected = ("{ '_t' : 'A', 'FX' : 'a' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = a.ToBson();
            var rehydrated = BsonSerializer.Deserialize<IX>(bson);
            Assert.IsInstanceOf<A>(rehydrated);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeBasB()
        {
            B b = new B { FX = "b" };
            var json = b.ToJson();
            var expected = ("{ 'FX' : 'b' }").Replace("'", "\""); // no discriminator
            Assert.AreEqual(expected, json);

            var bson = b.ToBson();
            var rehydrated = BsonSerializer.Deserialize<B>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeBasIX()
        {
            IX b = new B { FX = "b" };
            var json = b.ToJson();
            var expected = ("{ '_t' : 'B', 'FX' : 'b' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = b.ToBson();
            var rehydrated = BsonSerializer.Deserialize<IX>(bson);
            Assert.IsInstanceOf<B>(rehydrated);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
