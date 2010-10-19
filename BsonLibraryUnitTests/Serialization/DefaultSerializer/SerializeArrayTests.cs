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

using MongoDB.BsonLibrary.DefaultSerializer;
using MongoDB.BsonLibrary.IO;
using MongoDB.BsonLibrary.Serialization;

namespace MongoDB.BsonLibrary.UnitTests.Serialization {
    [TestFixture]
    public class SerializeArrayTests {
        private class C {
            public string P { get; set; }
        }

        private class A {
            public int[] IntArray { get; set; }
        }

        private class B {
            public C[] CArray { get; set; }
        }

        [Test]
        public void TestSerializeANull() {
            A a = new A { IntArray = null };
            var json = a.ToJson();
            var expected = ("{ 'IntArray' : null }").Replace("'", "\""); // no discriminator
            Assert.AreEqual(expected, json);

            var bson = a.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<A>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeAEmpty() {
            A a = new A { IntArray = new int[] { } };
            var json = a.ToJson();
            var expected = ("{ 'IntArray' : [] }").Replace("'", "\""); // no discriminator
            Assert.AreEqual(expected, json);

            var bson = a.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<A>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeA1() {
            A a = new A { IntArray = new int[] { 1 } };
            var json = a.ToJson();
            var expected = ("{ 'IntArray' : [1] }").Replace("'", "\""); // no discriminator
            Assert.AreEqual(expected, json);

            var bson = a.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<A>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeA2() {
            A a = new A { IntArray = new int[] { 1, 2 } };
            var json = a.ToJson();
            var expected = ("{ 'IntArray' : [1, 2] }").Replace("'", "\""); // no discriminator
            Assert.AreEqual(expected, json);

            var bson = a.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<A>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeBNull() {
            B b = new B { CArray = null };
            var json = b.ToJson();
            var expected = ("{ 'CArray' : null }").Replace("'", "\""); // no discriminator
            Assert.AreEqual(expected, json);

            var bson = b.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<B>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeBEmpty() {
            B b = new B { CArray = new C[] { } };
            var json = b.ToJson();
            var expected = ("{ 'CArray' : [] }").Replace("'", "\""); // no discriminator
            Assert.AreEqual(expected, json);

            var bson = b.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<B>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeB1() {
            B b = new B { CArray = new C[] { new C { P = "x" } } };
            var json = b.ToJson();
            var expected = ("{ 'CArray' : [{ 'P' : 'x' }] }").Replace("'", "\""); // no discriminator
            Assert.AreEqual(expected, json);

            var bson = b.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<B>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeB2() {
            B b = new B { CArray = new C[] { new C { P = "x" }, new C { P = "y" } } };
            var json = b.ToJson();
            var expected = ("{ 'CArray' : [{ 'P' : 'x' }, { 'P' : 'y' }] }").Replace("'", "\""); // no discriminator
            Assert.AreEqual(expected, json);

            var bson = b.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<B>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
