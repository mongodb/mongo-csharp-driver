/* Copyright 2010-2014 MongoDB Inc.
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

using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using NUnit.Framework;

namespace MongoDB.Bson.Tests.Jira.CSharp783
{
    [TestFixture]
    public class CSharp783DiscriminatedInterfaceTests
    {
        // nested types
        private class C
        {
            [BsonSerializer(typeof(DiscriminatedInterfaceSerializer<ISet<int>>))]
            public ISet<int> S { get; set; }
        }

        // public methods
        [Test]
        public void TestEmptyHashSet()
        {
            var c = new C { S = new HashSet<int>() };
            var json = c.ToJson();
            var expected = "{ 'S' : { '_t' : 'System.Collections.Generic.HashSet`1[System.Int32]', '_v' : [] } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.IsNotNull(r.S);
            Assert.IsInstanceOf<HashSet<int>>(r.S);
            Assert.AreEqual(0, r.S.Count);
        }

        [Test]
        public void TestEmptySortedSet()
        {
            var c = new C { S = new SortedSet<int>() };
            var json = c.ToJson();
            var expected = "{ 'S' : { '_t' : 'System.Collections.Generic.SortedSet`1[System.Int32]', '_v' : [] } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.IsNotNull(r.S);
            Assert.IsInstanceOf<SortedSet<int>>(r.S);
            Assert.AreEqual(0, r.S.Count);
        }

        [Test]
        public void TestNull()
        {
            var c = new C { S = null };
            var json = c.ToJson();
            var expected = "{ 'S' : null }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.AreEqual(null, r.S);
        }

        [Test]
        public void TestHashSetOneInt()
        {
            var c = new C { S = new HashSet<int> { 1 } };
            var json = c.ToJson();
            var expected = "{ 'S' : { '_t' : 'System.Collections.Generic.HashSet`1[System.Int32]', '_v' : [1] } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.IsNotNull(r.S);
            Assert.IsInstanceOf<HashSet<int>>(r.S);
            Assert.AreEqual(1, r.S.Count);
            Assert.AreEqual(1, r.S.ElementAt(0));
        }

        [TestCase(1, 2)]
        [TestCase(2, 1)]
        public void TestHashSetTwoInts(int x, int y)
        {
            var c = new C { S = new HashSet<int> { x, y } };
            var json = c.ToJson();
            var expected = new[] 
            {
                "{ 'S' : { '_t' : 'System.Collections.Generic.HashSet`1[System.Int32]', '_v' : [1, 2] } }",
                "{ 'S' : { '_t' : 'System.Collections.Generic.HashSet`1[System.Int32]', '_v' : [2, 1] } }"
            };
            Assert.That(expected.Select(e => e.Replace("'", "\"")).Contains(json));

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.IsNotNull(r.S);
            Assert.IsInstanceOf<HashSet<int>>(r.S);
            Assert.AreEqual(2, r.S.Count);
            Assert.That(r.S, Is.EquivalentTo(c.S));
        }

        [Test]
        public void TestSortedSetOneInt()
        {
            var c = new C { S = new SortedSet<int> { 1 } };
            var json = c.ToJson();
            var expected = "{ 'S' : { '_t' : 'System.Collections.Generic.SortedSet`1[System.Int32]', '_v' : [1] } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.IsNotNull(r.S);
            Assert.IsInstanceOf<SortedSet<int>>(r.S);
            Assert.AreEqual(1, r.S.Count);
            Assert.AreEqual(1, r.S.ElementAt(0));
        }

        [TestCase(1, 2)]
        [TestCase(2, 1)]
        public void TestSortedSetTwoInts(int x, int y)
        {
            var c = new C { S = new SortedSet<int> { x, y } };
            var json = c.ToJson();
            var expected = "{ 'S' : { '_t' : 'System.Collections.Generic.SortedSet`1[System.Int32]', '_v' : [1, 2] } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.IsNotNull(r.S);
            Assert.IsInstanceOf<SortedSet<int>>(r.S);
            Assert.AreEqual(2, r.S.Count);
            Assert.That(r.S, Is.EquivalentTo(c.S));
        }
    }

    [TestFixture]
    public class CSharp783ImpliedHashSetImplementationTests
    {
        // nested types
        private class C
        {
            public ISet<int> S { get; set; }
        }

        // public methods
        [Test]
        public void TestEmptyHashSet()
        {
            var c = new C { S = new HashSet<int>() };
            var json = c.ToJson();
            var expected = "{ 'S' : [] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.IsNotNull(r.S);
            Assert.IsInstanceOf<HashSet<int>>(r.S);
            Assert.AreEqual(0, r.S.Count);
        }

        [Test]
        public void TestEmptySortedSet()
        {
            var c = new C { S = new SortedSet<int>() };
            var json = c.ToJson();
            var expected = "{ 'S' : [] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.IsNotNull(r.S);
            Assert.IsInstanceOf<HashSet<int>>(r.S);
            Assert.AreEqual(0, r.S.Count);
        }

        [Test]
        public void TestNull()
        {
            var c = new C { S = null };
            var json = c.ToJson();
            var expected = "{ 'S' : null }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.AreEqual(null, r.S);
        }

        [Test]
        public void TestHashSetOneInt()
        {
            var c = new C { S = new HashSet<int> { 1 } };
            var json = c.ToJson();
            var expected = "{ 'S' : [1] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.IsNotNull(r.S);
            Assert.IsInstanceOf<HashSet<int>>(r.S);
            Assert.AreEqual(1, r.S.Count);
            Assert.AreEqual(1, r.S.ElementAt(0));
        }

        [TestCase(1, 2)]
        [TestCase(2, 1)]
        public void TestHashSetTwoInts(int x, int y)
        {
            var c = new C { S = new HashSet<int> { x, y } };
            var json = c.ToJson();
            var expected = new[] 
            {
                "{ 'S' : [1, 2] }",
                "{ 'S' : [2, 1] }"
            };
            Assert.That(expected.Select(e => e.Replace("'", "\"")).Contains(json));

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.IsNotNull(r.S);
            Assert.IsInstanceOf<HashSet<int>>(r.S);
            Assert.AreEqual(2, r.S.Count);
            Assert.That(r.S, Is.EquivalentTo(c.S));
        }

        [Test]
        public void TestSortedSetOneInt()
        {
            var c = new C { S = new SortedSet<int> { 1 } };
            var json = c.ToJson();
            var expected = "{ 'S' : [1] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.IsNotNull(r.S);
            Assert.IsInstanceOf<HashSet<int>>(r.S);
            Assert.AreEqual(1, r.S.Count);
            Assert.AreEqual(1, r.S.ElementAt(0));
        }

        [TestCase(1, 2)]
        [TestCase(2, 1)]
        public void TestSortedSetTwoInts(int x, int y)
        {
            var c = new C { S = new SortedSet<int> { x, y } };
            var json = c.ToJson();
            var expected = string.Format("{{ 'S' : [1, 2] }}", x, y).Replace("'", "\""); // always sorted
            Assert.AreEqual(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.IsNotNull(r.S);
            Assert.IsInstanceOf<HashSet<int>>(r.S);
            Assert.AreEqual(2, r.S.Count);
            Assert.That(r.S, Is.EquivalentTo(c.S));
        }
    }

    [TestFixture]
    public class CSharp783ImpliedSortedSetImplementationTests
    {
        // nested types
        private class C
        {
            [BsonSerializer(typeof(ImpliedImplementationInterfaceSerializer<ISet<int>, SortedSet<int>>))]
            public ISet<int> S { get; set; }
        }

        // public methods
        [Test]
        public void TestEmptyHashSet()
        {
            var c = new C { S = new HashSet<int>() };
            var json = c.ToJson();
            var expected = "{ 'S' : [] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.IsNotNull(r.S);
            Assert.IsInstanceOf<SortedSet<int>>(r.S);
            Assert.AreEqual(0, r.S.Count);
        }

        [Test]
        public void TestEmptySortedSet()
        {
            var c = new C { S = new SortedSet<int>() };
            var json = c.ToJson();
            var expected = "{ 'S' : [] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.IsNotNull(r.S);
            Assert.IsInstanceOf<SortedSet<int>>(r.S);
            Assert.AreEqual(0, r.S.Count);
        }

        [Test]
        public void TestNull()
        {
            var c = new C { S = null };
            var json = c.ToJson();
            var expected = "{ 'S' : null }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.AreEqual(null, r.S);
        }

        [Test]
        public void TestHashSetOneInt()
        {
            var c = new C { S = new HashSet<int> { 1 } };
            var json = c.ToJson();
            var expected = "{ 'S' : [1] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.IsNotNull(r.S);
            Assert.IsInstanceOf<SortedSet<int>>(r.S);
            Assert.AreEqual(1, r.S.Count);
            Assert.AreEqual(1, r.S.ElementAt(0));
        }

        [TestCase(1, 2)]
        [TestCase(2, 1)]
        public void TestHashSetTwoInts(int x, int y)
        {
            var c = new C { S = new HashSet<int> { x, y } };
            var json = c.ToJson();
            var expected = new[] 
            {
                "{ 'S' : [1, 2] }",
                "{ 'S' : [2, 1] }"
            };
            Assert.That(expected.Select(e => e.Replace("'", "\"")).Contains(json));

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.IsNotNull(r.S);
            Assert.IsInstanceOf<SortedSet<int>>(r.S);
            Assert.AreEqual(2, r.S.Count);
            Assert.That(r.S, Is.EquivalentTo(c.S));
        }

        [Test]
        public void TestSortedSetOneInt()
        {
            var c = new C { S = new SortedSet<int> { 1 } };
            var json = c.ToJson();
            var expected = "{ 'S' : [1] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.IsNotNull(r.S);
            Assert.IsInstanceOf<SortedSet<int>>(r.S);
            Assert.AreEqual(1, r.S.Count);
            Assert.AreEqual(1, r.S.ElementAt(0));
        }

        [TestCase(1, 2)]
        [TestCase(2, 1)]
        public void TestSortedSetTwoInts(int x, int y)
        {
            var c = new C { S = new SortedSet<int> { x, y } };
            var json = c.ToJson();
            var expected = string.Format("{{ 'S' : [1, 2] }}", x, y).Replace("'", "\""); // always sorted
            Assert.AreEqual(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.IsNotNull(r.S);
            Assert.IsInstanceOf<SortedSet<int>>(r.S);
            Assert.AreEqual(2, r.S.Count);
            Assert.That(r.S, Is.EquivalentTo(c.S));
        }
    }
}