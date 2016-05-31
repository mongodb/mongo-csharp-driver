/* Copyright 2010-2015 MongoDB Inc.
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
using Xunit;

namespace MongoDB.Driver.Tests.Builders
{
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

            public int z;

            [BsonIgnoreIfDefault]
            [BsonElement("relevance")]
            public double R { get; set; }
        }

        [Fact]
        public void TestAscending1()
        {
            var sortBy = SortBy<Test>.Ascending(x => x.A);
            string expected = "{ \"a\" : 1 }";
            Assert.Equal(expected, sortBy.ToJson());
        }

        [Fact]
        public void TestAscending2()
        {
            var sortBy = SortBy<Test>.Ascending(x => x.A, x => x.B);
            string expected = "{ \"a\" : 1, \"b\" : 1 }";
            Assert.Equal(expected, sortBy.ToJson());
        }

        [Fact]
        public void TestAscendingAscending()
        {
            var sortBy = SortBy<Test>.Ascending(x => x.A).Ascending(x => x.B);
            string expected = "{ \"a\" : 1, \"b\" : 1 }";
            Assert.Equal(expected, sortBy.ToJson());
        }

        [Fact]
        public void TestAscendingDescending()
        {
            var sortBy = SortBy<Test>.Ascending(x => x.A).Descending(x => x.B);
            string expected = "{ \"a\" : 1, \"b\" : -1 }";
            Assert.Equal(expected, sortBy.ToJson());
        }

        [Fact]
        public void TestDescending1()
        {
            var sortBy = SortBy<Test>.Descending(x => x.A);
            string expected = "{ \"a\" : -1 }";
            Assert.Equal(expected, sortBy.ToJson());
        }

        [Fact]
        public void TestDescending2()
        {
            var sortBy = SortBy<Test>.Descending(x => x.A, x => x.B);
            string expected = "{ \"a\" : -1, \"b\" : -1 }";
            Assert.Equal(expected, sortBy.ToJson());
        }

        [Fact]
        public void TestDescendingAscending()
        {
            var sortBy = SortBy<Test>.Descending(x => x.A).Ascending(x => x.B);
            string expected = "{ \"a\" : -1, \"b\" : 1 }";
            Assert.Equal(expected, sortBy.ToJson());
        }

        [Fact]
        public void TestDescendingDescending()
        {
            var sortBy = SortBy<Test>.Descending(x => x.A).Descending(x => x.B);
            string expected = "{ \"a\" : -1, \"b\" : -1 }";
            Assert.Equal(expected, sortBy.ToJson());
        }

        [Fact]
        public void TestMetaTextGenerate()
        {
            var sortBy = SortBy<Test>.MetaTextScore(y => y.R);
            string expected = "{ \"relevance\" : { \"$meta\" : \"textScore\" } }";
            Assert.Equal(expected, sortBy.ToJson());
        }

        [Fact]
        public void TestMetaTextAndOtherFields()
        {
            var sortBy = SortBy<Test>.MetaTextScore(y => y.R).Descending(y => y.A).Ascending(y => y.z);
            string expected = "{ \"relevance\" : { \"$meta\" : \"textScore\" }, \"a\" : -1, \"z\" : 1 }";
            Assert.Equal(expected, sortBy.ToJson());
        }

        [Fact]
        public void TestMetaText()
        {
            if (LegacyTestConfiguration.Server.Primary.Supports(FeatureId.TextSearchQuery))
            {
                var collection = LegacyTestConfiguration.Database.GetCollection<Test>("test_meta_text_sort");
                collection.Drop();
                collection.CreateIndex(IndexKeys<Test>.Text(x => x.T));
                collection.Insert(new Test
                {
                    Id = 1,
                    T = "The quick brown fox jumped",
                    z = 1
                });
                collection.Insert(new Test
                {
                    Id = 2,
                    T = "over the lazy brown dog and brown cat",
                    z = 2
                });
                collection.Insert(new Test
                {
                    Id = 3,
                    T = "over the lazy brown dog and brown cat",
                    z = 4
                });
                collection.Insert(new Test
                {
                    Id = 4,
                    T = "over the lazy brown dog and brown cat",
                    z = 3
                });

                var query = Query.Text("brown");
                var fields = Fields<Test>.MetaTextScore(y => y.R);
                var sortBy = SortBy<Test>.MetaTextScore(y => y.R).Descending(y => y.z);
                var cursor = collection.FindAs<BsonDocument>(query).SetFields(fields).SetSortOrder(sortBy);
                var result = cursor.ToArray();
                Assert.Equal(4, result.Length);
                Assert.Equal(3, result[0]["_id"].AsInt32);
                Assert.Equal(4, result[1]["_id"].AsInt32);
                Assert.Equal(2, result[2]["_id"].AsInt32);
                Assert.Equal(1, result[3]["_id"].AsInt32);
            }
        }
    }
}
