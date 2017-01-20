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

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Xunit;

namespace MongoDB.Driver.Tests.Builders
{
    public class FieldsBuilderTests
    {
        [Fact]
        public void TestElemMatch()
        {
            var fields = Fields.ElemMatch("a2", Query.EQ("b", 10));
            string expected = "{ \"a2\" : { \"$elemMatch\" : { \"b\" : 10 } } }";
            Assert.Equal(expected, fields.ToJson());
        }

        [Fact]
        public void TestIncludeElemMatch()
        {
            var fields = Fields.Include("x").ElemMatch("a2", Query.EQ("b", 10));
            string expected = "{ \"x\" : 1, \"a2\" : { \"$elemMatch\" : { \"b\" : 10 } } }";
            Assert.Equal(expected, fields.ToJson());
        }

        [Fact]
        public void TestInclude()
        {
            var fields = Fields.Include("a");
            string expected = "{ \"a\" : 1 }";
            Assert.Equal(expected, fields.ToJson());
        }

        [Fact]
        public void TestExclude()
        {
            var fields = Fields.Exclude("a");
            string expected = "{ \"a\" : 0 }";
            Assert.Equal(expected, fields.ToJson());
        }

        [Fact]
        public void TestSliceNameSize()
        {
            var fields = Fields.Slice("a", 10);
            string expected = "{ \"a\" : { \"$slice\" : 10 } }";
            Assert.Equal(expected, fields.ToJson());
        }

        [Fact]
        public void TestSliceNameSkipLimit()
        {
            var fields = Fields.Slice("a", 10, 20);
            string expected = "{ \"a\" : { \"$slice\" : [10, 20] } }";
            Assert.Equal(expected, fields.ToJson());
        }
        [Fact]
        public void TestIncludeInclude()
        {
            var fields = Fields.Include("x").Include("a");
            string expected = "{ \"x\" : 1, \"a\" : 1 }";
            Assert.Equal(expected, fields.ToJson());
        }

        [Fact]
        public void TestIncludeExclude()
        {
            var fields = Fields.Include("x").Exclude("a");
            string expected = "{ \"x\" : 1, \"a\" : 0 }";
            Assert.Equal(expected, fields.ToJson());
        }

        [Fact]
        public void TestIncludeSliceNameSize()
        {
            var fields = Fields.Include("x").Slice("a", 10);
            string expected = "{ \"x\" : 1, \"a\" : { \"$slice\" : 10 } }";
            Assert.Equal(expected, fields.ToJson());
        }

        [Fact]
        public void TestIncludeSliceNameSkipLimit()
        {
            var fields = Fields.Include("x").Slice("a", 10, 20);
            string expected = "{ \"x\" : 1, \"a\" : { \"$slice\" : [10, 20] } }";
            Assert.Equal(expected, fields.ToJson());
        }

        [Fact]
        public void TestMetaTextGenerate()
        {
            var fields = Fields.MetaTextScore("score");
            string expected = "{ \"score\" : { \"$meta\" : \"textScore\" } }";
            Assert.Equal(expected, fields.ToJson());
        }

        [Fact]
        public void TestMetaTextIncludeExcludeGenerate()
        {
            var fields = Fields.MetaTextScore("searchrelevancescore").Include("x").Exclude("_id");
            string expected = "{ \"searchrelevancescore\" : { \"$meta\" : \"textScore\" }, \"x\" : 1, \"_id\" : 0 }";
            Assert.Equal(expected, fields.ToJson());
        }

        [Fact]
        public void TestMetaText()
        {
            if (LegacyTestConfiguration.Server.Primary.Supports(FeatureId.TextSearchQuery))
            {
                var collection = LegacyTestConfiguration.Database.GetCollection<BsonDocument>("test_meta_text");
                collection.Drop();
                collection.CreateIndex(IndexKeys.Text("textfield"));
                collection.Insert(new BsonDocument
                {
                    { "_id", 1 },
                    { "textfield", "The quick brown fox jumped" }
                });
                collection.Insert(new BsonDocument
                {
                    { "_id", 2 },
                    { "textfield", "over the lazy brown dog" }
                });
                var query = Query.Text("fox");
                var result = collection.FindOneAs<BsonDocument>(query);
                Assert.Equal(2, result.ElementCount);
                Assert.False(result.Contains("relevance"));

                var fields = Fields.MetaTextScore("relevance");
                result = collection.FindOneAs<BsonDocument>(new FindOneArgs { Query = query, Fields = fields });
                Assert.Equal(3, result.ElementCount);
                Assert.True(result.Contains("relevance"));
            }
        }
    }
}