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
using MongoDB.Driver.Support;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Support
{
    [TestFixture]
    public class IndexMapTests
    {
        [Test]
        public void TestDictionaryBasedWith0()
        {
            var map = new IndexMap.DictionaryBased();
            Assert.Throws<KeyNotFoundException>(() => map.Map(0));
        }

        [Test]
        public void TestDictionaryBasedWith1()
        {
            IndexMap map = new IndexMap.DictionaryBased();
            map = map.Add(0, 1);
            Assert.AreEqual(1, map.Map(0));
            Assert.Throws<KeyNotFoundException>(() => map.Map(1));
        }

        [Test]
        public void TestDictionaryBasedWith2()
        {
            IndexMap map = new IndexMap.DictionaryBased();
            map = map.Add(0, 1);
            map = map.Add(1, 3);
            Assert.AreEqual(1, map.Map(0));
            Assert.AreEqual(3, map.Map(1));
            Assert.Throws<KeyNotFoundException>(() => map.Map(2));
        }

        [Test]
        public void TestMixedWith1Then1()
        {
            IndexMap map = new IndexMap.RangeBased();
            map = map.Add(0, 1);
            Assert.IsInstanceOf<IndexMap.RangeBased>(map);
            map = map.Add(1, 3);
            Assert.IsInstanceOf<IndexMap.DictionaryBased>(map);
            Assert.AreEqual(1, map.Map(0));
            Assert.AreEqual(3, map.Map(1));
            Assert.Throws<KeyNotFoundException>(() => map.Map(2));
        }

        [Test]
        public void TestMixedWith2Then1()
        {
            IndexMap map = new IndexMap.RangeBased();
            map = map.Add(0, 1);
            map = map.Add(1, 2);
            Assert.IsInstanceOf<IndexMap.RangeBased>(map);
            map = map.Add(2, 4);
            Assert.IsInstanceOf<IndexMap.DictionaryBased>(map);
            Assert.AreEqual(1, map.Map(0));
            Assert.AreEqual(2, map.Map(1));
            Assert.AreEqual(4, map.Map(2));
            Assert.Throws<KeyNotFoundException>(() => map.Map(3));
        }

        [Test]
        public void TestRangeBasedAdd1()
        {
            IndexMap map = new IndexMap.RangeBased();
            map = map.Add(0, 1);
            Assert.IsInstanceOf<IndexMap.RangeBased>(map);
            Assert.AreEqual(1, map.Map(0));
            Assert.Throws<KeyNotFoundException>(() => map.Map(1));
        }

        [Test]
        public void TestRangeBasedAdd2()
        {
            IndexMap map = new IndexMap.RangeBased();
            map = map.Add(0, 1);
            map = map.Add(1, 2);
            Assert.IsInstanceOf<IndexMap.RangeBased>(map);
            Assert.AreEqual(1, map.Map(0));
            Assert.AreEqual(2, map.Map(1));
            Assert.Throws<KeyNotFoundException>(() => map.Map(2));
        }

        [Test]
        public void TestRangeBasedWith0()
        {
            var map = new IndexMap.RangeBased();
            Assert.Throws<KeyNotFoundException>(() => map.Map(0));
        }

        [Test]
        public void TestRangeBasedWith1()
        {
            var map = new IndexMap.RangeBased(0, 1, 1);
            Assert.AreEqual(1, map.Map(0));
            Assert.Throws<KeyNotFoundException>(() => map.Map(1));
        }

        [Test]
        public void TestRangeBasedWith2()
        {
            var map = new IndexMap.RangeBased(0, 1, 2);
            Assert.AreEqual(1, map.Map(0));
            Assert.AreEqual(2, map.Map(1));
            Assert.Throws<KeyNotFoundException>(() => map.Map(2));
        }
    }
}
