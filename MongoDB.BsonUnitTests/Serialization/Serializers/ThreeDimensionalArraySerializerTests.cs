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

namespace MongoDB.BsonUnitTests.Serialization.ArraySerializer
{
    [TestFixture]
    public class ThreeDimensionalArraySerializerTests
    {
        private class C
        {
            public int[, ,] A;
        }

        [Test]
        public void TestNull()
        {
            var c = new C { A = null };
            var json = c.ToJson();
            var expected = "{ 'A' : null }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void Test0x0x0()
        {
            var c = new C { A = new int[0, 0, 0] };
            var json = c.ToJson();
            var expected = "{ 'A' : [] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void Test1x0x0()
        {
            var c = new C { A = new int[1, 0, 0] };
            var json = c.ToJson();
            var expected = "{ 'A' : [[]] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void Test1x1x0()
        {
            var c = new C { A = new int[1, 1, 0] };
            var json = c.ToJson();
            var expected = "{ 'A' : [[[]]] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void Test1x1x1()
        {
            var c = new C { A = new int[1, 1, 1] { { { 1 } } } };
            var json = c.ToJson();
            var expected = "{ 'A' : [[[1]]] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void Test1x1x2()
        {
            var c = new C { A = new int[1, 1, 2] { { { 1, 2 } } } };
            var json = c.ToJson();
            var expected = "{ 'A' : [[[1, 2]]] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void Test1x2x0()
        {
            var c = new C { A = new int[1, 2, 0] };
            var json = c.ToJson();
            var expected = "{ 'A' : [[[], []]] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void Test1x2x1()
        {
            var c = new C { A = new int[1, 2, 1] { { { 1 }, { 2 } } } };
            var json = c.ToJson();
            var expected = "{ 'A' : [[[1], [2]]] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void Test1x2x2()
        {
            var c = new C { A = new int[1, 2, 2] { { { 1, 2 }, { 3, 4 } } } };
            var json = c.ToJson();
            var expected = "{ 'A' : [[[1, 2], [3, 4]]] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void Test2x0x0()
        {
            var c = new C { A = new int[2, 0, 0] };
            var json = c.ToJson();
            var expected = "{ 'A' : [[], []] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void Test2x1x0()
        {
            var c = new C { A = new int[2, 1, 0] };
            var json = c.ToJson();
            var expected = "{ 'A' : [[[]], [[]]] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void Test2x1x1()
        {
            var c = new C { A = new int[2, 1, 1] { { { 1 } }, { { 2 } } } };
            var json = c.ToJson();
            var expected = "{ 'A' : [[[1]], [[2]]] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void Test2x1x2()
        {
            var c = new C { A = new int[2, 1, 2] { { { 1, 2 } }, { { 3, 4 } } } };
            var json = c.ToJson();
            var expected = "{ 'A' : [[[1, 2]], [[3, 4]]] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void Test2x2x0()
        {
            var c = new C { A = new int[2, 2, 0] };
            var json = c.ToJson();
            var expected = "{ 'A' : [[[], []], [[], []]] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void Test2x2x1()
        {
            var c = new C { A = new int[2, 2, 1] { { { 1 }, { 2 } }, { { 3 }, { 4 } } } };
            var json = c.ToJson();
            var expected = "{ 'A' : [[[1], [2]], [[3], [4]]] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void Test2x2x2()
        {
            var c = new C { A = new int[2, 2, 2] { { { 1, 2 }, { 3, 4 } }, { { 5, 6 }, { 7, 8 } } } };
            var json = c.ToJson();
            var expected = "{ 'A' : [[[1, 2], [3, 4]], [[5, 6], [7, 8]]] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
