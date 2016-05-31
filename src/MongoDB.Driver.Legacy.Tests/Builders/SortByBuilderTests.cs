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
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Xunit;

namespace MongoDB.Driver.Tests.Builders
{
    public class SortByBuilderTests
    {
        [Fact]
        public void TestAscending1()
        {
            var sortBy = SortBy.Ascending("a");
            string expected = "{ \"a\" : 1 }";
            Assert.Equal(expected, sortBy.ToJson());
        }

        [Fact]
        public void TestAscending2()
        {
            var sortBy = SortBy.Ascending("a", "b");
            string expected = "{ \"a\" : 1, \"b\" : 1 }";
            Assert.Equal(expected, sortBy.ToJson());
        }

        [Fact]
        public void TestAscendingAscending()
        {
            var sortBy = SortBy.Ascending("a").Ascending("b");
            string expected = "{ \"a\" : 1, \"b\" : 1 }";
            Assert.Equal(expected, sortBy.ToJson());
        }

        [Fact]
        public void TestAscendingDescending()
        {
            var sortBy = SortBy.Ascending("a").Descending("b");
            string expected = "{ \"a\" : 1, \"b\" : -1 }";
            Assert.Equal(expected, sortBy.ToJson());
        }

        [Fact]
        public void TestDescending1()
        {
            var sortBy = SortBy.Descending("a");
            string expected = "{ \"a\" : -1 }";
            Assert.Equal(expected, sortBy.ToJson());
        }

        [Fact]
        public void TestDescending2()
        {
            var sortBy = SortBy.Descending("a", "b");
            string expected = "{ \"a\" : -1, \"b\" : -1 }";
            Assert.Equal(expected, sortBy.ToJson());
        }

        [Fact]
        public void TestDescendingAscending()
        {
            var sortBy = SortBy.Descending("a").Ascending("b");
            string expected = "{ \"a\" : -1, \"b\" : 1 }";
            Assert.Equal(expected, sortBy.ToJson());
        }

        [Fact]
        public void TestMetaTextGenerate()
        {
            var sortBy = SortBy.MetaTextScore("score");
            string expected = "{ \"score\" : { \"$meta\" : \"textScore\" } }";
            Assert.Equal(expected, sortBy.ToJson());
        }

        [Fact]
        public void TestMetaTextAndOtherFields()
        {
            var sortBy = SortBy.MetaTextScore("searchrelevancescore").Descending("y").Ascending("z");
            string expected = "{ \"searchrelevancescore\" : { \"$meta\" : \"textScore\" }, \"y\" : -1, \"z\" : 1 }";
            Assert.Equal(expected, sortBy.ToJson());
        }

        [Fact]
        public void TestMetaText()
        {
            if (LegacyTestConfiguration.Server.Primary.Supports(FeatureId.TextSearchQuery))
            {
                var collection = LegacyTestConfiguration.Database.GetCollection<BsonDocument>("test_meta_text_sort");
                collection.Drop();
                collection.CreateIndex(IndexKeys.Text("textfield"));
                collection.Insert(new BsonDocument
                {
                    { "_id", 1 },
                    { "textfield", "The quick brown fox jumped" },
                    { "z", 1 }
                });
                collection.Insert(new BsonDocument
                {
                    { "_id", 2 },
                    { "textfield", "over the lazy brown dog and brown cat" },
                    { "z", 2 }
                });
                collection.Insert(new BsonDocument
                {
                    { "_id", 3 },
                    { "textfield", "over the lazy brown dog and brown cat" },
                    { "z", 4 }
                });
                collection.Insert(new BsonDocument
                {
                    { "_id", 4 },
                    { "textfield", "over the lazy brown dog and brown cat" },
                    { "z", 3 }
                });

                var query = Query.Text("brown");
                var fields = Fields.MetaTextScore("relevance");
                var sortBy = SortBy.MetaTextScore("relevance").Descending("z");
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
