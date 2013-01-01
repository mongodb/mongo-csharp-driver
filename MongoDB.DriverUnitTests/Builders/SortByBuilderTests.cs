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

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Builders;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Builders
{
    [TestFixture]
    public class SortByBuilderTests
    {
        private class Test
        {
            [BsonElement("a")]
            public string A { get; set; }

            [BsonElement("b")]
            public string B { get; set; }
        }

        [Test]
        public void TestAscending1()
        {
            var sortBy = SortBy.Ascending("a");
            string expected = "{ \"a\" : 1 }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }

        [Test]
        public void TestAscending1_Typed()
        {
            var sortBy = SortBy<Test>.Ascending(x => x.A);
            string expected = "{ \"a\" : 1 }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }

        [Test]
        public void TestAscending2()
        {
            var sortBy = SortBy.Ascending("a", "b");
            string expected = "{ \"a\" : 1, \"b\" : 1 }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }

        [Test]
        public void TestAscending2_Typed()
        {
            var sortBy = SortBy<Test>.Ascending(x => x.A, x => x.B);
            string expected = "{ \"a\" : 1, \"b\" : 1 }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }

        [Test]
        public void TestAscendingAscending()
        {
            var sortBy = SortBy.Ascending("a").Ascending("b");
            string expected = "{ \"a\" : 1, \"b\" : 1 }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }

        [Test]
        public void TestAscendingAscending_Typed()
        {
            var sortBy = SortBy<Test>.Ascending(x => x.A).Ascending(x => x.B);
            string expected = "{ \"a\" : 1, \"b\" : 1 }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }

        [Test]
        public void TestAscendingDescending()
        {
            var sortBy = SortBy.Ascending("a").Descending("b");
            string expected = "{ \"a\" : 1, \"b\" : -1 }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }

        [Test]
        public void TestAscendingDescending_Typed()
        {
            var sortBy = SortBy<Test>.Ascending(x => x.A).Descending(x => x.B);
            string expected = "{ \"a\" : 1, \"b\" : -1 }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }

        [Test]
        public void TestDescending1()
        {
            var sortBy = SortBy.Descending("a");
            string expected = "{ \"a\" : -1 }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }

        [Test]
        public void TestDescending1_Typed()
        {
            var sortBy = SortBy<Test>.Descending(x => x.A);
            string expected = "{ \"a\" : -1 }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }

        [Test]
        public void TestDescending2()
        {
            var sortBy = SortBy.Descending("a", "b");
            string expected = "{ \"a\" : -1, \"b\" : -1 }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }

        [Test]
        public void TestDescending2_Typed()
        {
            var sortBy = SortBy<Test>.Descending(x => x.A, x => x.B);
            string expected = "{ \"a\" : -1, \"b\" : -1 }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }

        [Test]
        public void TestDescendingAscending()
        {
            var sortBy = SortBy.Descending("a").Ascending("b");
            string expected = "{ \"a\" : -1, \"b\" : 1 }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }

        [Test]
        public void TestDescendingAscending_Typed()
        {
            var sortBy = SortBy<Test>.Descending(x => x.A).Ascending(x => x.B);
            string expected = "{ \"a\" : -1, \"b\" : 1 }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }

        [Test]
        public void TestDescendingDescending()
        {
            var sortBy = SortBy<Test>.Descending(x => x.A).Descending(x => x.B);
            string expected = "{ \"a\" : -1, \"b\" : -1 }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }
    }
}
