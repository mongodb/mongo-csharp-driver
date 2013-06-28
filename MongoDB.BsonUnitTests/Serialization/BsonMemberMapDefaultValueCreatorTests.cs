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

using System.Collections.Generic;
using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Serialization
{
    [TestFixture]
    public class BsonMemberMapDefaultValueCreatorTests
    {
        private class C
        {
            public int Id { get; set; }
            public List<int> L { get; set; }
        }

        static BsonMemberMapDefaultValueCreatorTests()
        {
            BsonClassMap.RegisterClassMap<C>(cm =>
            {
                cm.AutoMap();
                cm.GetMemberMap(c => c.L).SetDefaultValue(() => new List<int>());
            });
        }

        [Test]
        public void TestEachDefaultValueIsNewInstance()
        {
            var json = "{ _id : 1 }";
            var c = BsonSerializer.Deserialize<C>(json);
            var d = BsonSerializer.Deserialize<C>(json);
            Assert.IsNotNull(c.L);
            Assert.IsNotNull(d.L);
            Assert.AreNotSame(c.L, d.L);
        }

        [Test]
        public void TestValueMissing()
        {
            var json = "{ _id : 1 }";
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.AreEqual(1, c.Id);
            Assert.IsNotNull(c.L);
            Assert.AreEqual(0, c.L.Count);
        }

        [Test]
        public void TestValuePresent()
        {
            var json = "{ _id : 1, L : [1] }";
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.AreEqual(1, c.Id);
            Assert.IsNotNull(c.L);
            Assert.AreEqual(1, c.L.Count);
            Assert.AreEqual(1, c.L[0]);
        }
    }
}
