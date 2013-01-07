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

using System.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Jira
{
    [TestFixture]
    public class CSharp369Tests
    {
        [BsonIgnoreExtraElements]
        private class C
        {
            public int Id { get; set; }
            public int X { get; set; }
        }

        private class D : C
        {
            public int Y { get; set; }
        }

        [BsonIgnoreExtraElements(Inherited = true)]
        private class E
        {
            public int Id { get; set; }
            public int X { get; set; }
        }

        private class F : E
        {
            public int Y { get; set; }
        }

        [Test]
        public void TestCWithExtraFields()
        {
            var json = "{ _id : 1, X : 2, Y : 3, Z : 4 }";
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.IsInstanceOf<C>(c);
            Assert.AreEqual(1, c.Id);
            Assert.AreEqual(2, c.X);
        }

        [Test]
        public void TestDWithExtraFields()
        {
            var json = "{ _id : 1, X : 2, Y : 3, Z : 4 }";
            Assert.Throws<FileFormatException>(() => { var d = BsonSerializer.Deserialize<D>(json); });
        }

        [Test]
        public void TestEWithExtraFields()
        {
            var json = "{ _id : 1, X : 2, Y : 3, Z : 4 }";
            var e = BsonSerializer.Deserialize<E>(json);
            Assert.IsInstanceOf<E>(e);
            Assert.AreEqual(1, e.Id);
            Assert.AreEqual(2, e.X);
        }

        [Test]
        public void TestFWithExtraFields()
        {
            var json = "{ _id : 1, X : 2, Y : 3, Z : 4 }";
            var f = BsonSerializer.Deserialize<F>(json);
            Assert.IsInstanceOf<F>(f);
            Assert.AreEqual(1, f.Id);
            Assert.AreEqual(2, f.X);
            Assert.AreEqual(3, f.Y);
        }
    }
}
