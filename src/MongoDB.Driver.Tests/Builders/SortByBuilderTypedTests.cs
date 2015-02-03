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

using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using NUnit.Framework;

namespace MongoDB.Driver.Tests.Builders
{
    [TestFixture]
    public class SortByBuilderTypedTests
    {
        private class Test
        {
            public int Id { get; set; }

            [BsonElement("a")]
            public string A { get; set; }

            [BsonElement("b")]
            public string B { get; set; }

            [BsonElement("textfield")]
            public string T { get; set; }

            public int z = 0;

            [BsonIgnoreIfDefault]
            [BsonElement("relevance")]
            public double R { get; set; }
        }

        [Test]
        public void TestAscending1()
        {
            var sortBy = SortBy<Test>.Ascending(x => x.A);
            string expected = "{ \"a\" : 1 }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }

        [Test]
        public void TestAscending2()
        {
            var sortBy = SortBy<Test>.Ascending(x => x.A, x => x.B);
            string expected = "{ \"a\" : 1, \"b\" : 1 }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }

        [Test]
        public void TestAscendingAscending()
        {
            var sortBy = SortBy<Test>.Ascending(x => x.A).Ascending(x => x.B);
            string expected = "{ \"a\" : 1, \"b\" : 1 }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }

        [Test]
        public void TestAscendingDescending()
        {
            var sortBy = SortBy<Test>.Ascending(x => x.A).Descending(x => x.B);
            string expected = "{ \"a\" : 1, \"b\" : -1 }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }

        [Test]
        public void TestDescending1()
        {
            var sortBy = SortBy<Test>.Descending(x => x.A);
            string expected = "{ \"a\" : -1 }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }

        [Test]
        public void TestDescending2()
        {
            var sortBy = SortBy<Test>.Descending(x => x.A, x => x.B);
            string expected = "{ \"a\" : -1, \"b\" : -1 }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }

        [Test]
        public void TestDescendingAscending()
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

        [Test]
        public void TestMetaTextGenerate()
        {
            var sortBy = SortBy<Test>.MetaTextScore(y => y.R);
            string expected = "{ \"relevance\" : { \"$meta\" : \"textScore\" } }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }

        [Test]
        public void TestMetaTextAndOtherFields()
        {
            var sortBy = SortBy<Test>.MetaTextScore(y => y.R).Descending(y => y.A).Ascending(y => y.z);
            string expected = "{ \"relevance\" : { \"$meta\" : \"textScore\" }, \"a\" : -1, \"z\" : 1 }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }
    }
}
