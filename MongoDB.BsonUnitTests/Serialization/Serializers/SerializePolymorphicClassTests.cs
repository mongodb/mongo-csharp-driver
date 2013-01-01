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
using MongoDB.Bson.Serialization.Attributes;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Serialization
{
    [TestFixture]
    public class SerializePolymorphicClassTests
    {
        private abstract class A
        {
            public string FA { get; set; }
        }

        [BsonDiscriminator(Required = true)]
        private abstract class B : A
        {
            public string FB { get; set; }
        }

        private class C : A
        {
            public string FC { get; set; }
        }

        private class D : B
        {
            public string FD { get; set; }
        }

        private class E : B
        {
            public string FE { get; set; }
        }

        private class T
        {
            public A FT { get; set; }
        }

        [Test]
        public void TestSerializeCasA()
        {
            A a = new C { FA = "a", FC = "c" };
            var json = a.ToJson();
            var expected = ("{ '_t' : 'C', 'FA' : 'a', 'FC' : 'c' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = a.ToBson();
            var rehydrated = BsonSerializer.Deserialize<A>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeCasC()
        {
            C c = new C { FA = "a", FC = "c" };
            var json = c.ToJson();
            var expected = ("{ 'FA' : 'a', 'FC' : 'c' }").Replace("'", "\""); // no discriminator
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeDasA()
        {
            A a = new D { FA = "a", FB = "b", FD = "d" };
            var json = a.ToJson();
            var expected = ("{ '_t' : 'D', 'FA' : 'a', 'FB' : 'b', 'FD' : 'd' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = a.ToBson();
            var rehydrated = BsonSerializer.Deserialize<A>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeDasB()
        {
            B b = new D { FA = "a", FB = "b", FD = "d" };
            var json = b.ToJson();
            var expected = ("{ '_t' : 'D', 'FA' : 'a', 'FB' : 'b', 'FD' : 'd' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = b.ToBson();
            var rehydrated = BsonSerializer.Deserialize<B>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeDasD()
        {
            D d = new D { FA = "a", FB = "b", FD = "d" };
            var json = d.ToJson();
            var expected = ("{ '_t' : 'D', 'FA' : 'a', 'FB' : 'b', 'FD' : 'd' }").Replace("'", "\""); // has discriminator because B has DiscriminatorIsRequired true
            Assert.AreEqual(expected, json);

            var bson = d.ToBson();
            var rehydrated = BsonSerializer.Deserialize<D>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeEasA()
        {
            A a = new E { FA = "a", FB = "b", FE = "e" };
            var json = a.ToJson();
            var expected = ("{ '_t' : 'E', 'FA' : 'a', 'FB' : 'b', 'FE' : 'e' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = a.ToBson();
            var rehydrated = BsonSerializer.Deserialize<A>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeEasB()
        {
            B b = new E { FA = "a", FB = "b", FE = "e" };
            var json = b.ToJson();
            var expected = ("{ '_t' : 'E', 'FA' : 'a', 'FB' : 'b', 'FE' : 'e' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = b.ToBson();
            var rehydrated = BsonSerializer.Deserialize<B>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeEasE()
        {
            E e = new E { FA = "a", FB = "b", FE = "e" };
            var json = e.ToJson();
            var expected = ("{ '_t' : 'E', 'FA' : 'a', 'FB' : 'b', 'FE' : 'e' }").Replace("'", "\""); // has discriminator because B has DiscriminatorIsRequired true
            Assert.AreEqual(expected, json);

            var bson = e.ToBson();
            var rehydrated = BsonSerializer.Deserialize<E>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeTNull()
        {
            T t = new T { FT = null };
            var json = t.ToJson();
            var expected = ("{ 'FT' : null }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = t.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsNull(rehydrated.FT);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeTC()
        {
            T t = new T { FT = new C { FA = "a", FC = "c" } };
            var json = t.ToJson();
            var expected = ("{ 'FT' : { '_t' : 'C', 'FA' : 'a', 'FC' : 'c' } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = t.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<C>(rehydrated.FT);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeTD()
        {
            T t = new T { FT = new D { FA = "a", FB = "b", FD = "d" } };
            var json = t.ToJson();
            var expected = ("{ 'FT' : { '_t' : 'D', 'FA' : 'a', 'FB' : 'b', 'FD' : 'd' } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = t.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<D>(rehydrated.FT);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeTE()
        {
            T t = new T { FT = new E { FA = "a", FB = "b", FE = "e" } };
            var json = t.ToJson();
            var expected = ("{ 'FT' : { '_t' : 'E', 'FA' : 'a', 'FB' : 'b', 'FE' : 'e' } }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = t.ToBson();
            var rehydrated = BsonSerializer.Deserialize<T>(bson);
            Assert.IsInstanceOf<E>(rehydrated.FT);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
