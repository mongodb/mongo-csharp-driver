/* Copyright 2010-2016 MongoDB Inc.
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

using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Core;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq
{
    public class WithIndexTests
    {
        private class B
        {
            public ObjectId Id;
            public int a;
            public int b;
            public int c;
        }

        private static MongoCollection<B> __collection;
        private static Lazy<bool> __lazyOneTimeSetup = new Lazy<bool>(OneTimeSetup);

        public WithIndexTests()
        {
            var _ = __lazyOneTimeSetup.Value;
        }

        private static bool OneTimeSetup()
        {
            __collection = LegacyTestConfiguration.GetCollection<B>();

            __collection.Drop();
            __collection.CreateIndex(new IndexKeysBuilder().Ascending("a", "b"), IndexOptions.SetName("i"));
            __collection.CreateIndex(new IndexKeysBuilder().Ascending("a", "b"), IndexOptions.SetName("i"));

            __collection.Insert(new B { Id = ObjectId.GenerateNewId(), a = 1, b = 10, c = 100 });
            __collection.Insert(new B { Id = ObjectId.GenerateNewId(), a = 2, b = 20, c = 200 });
            __collection.Insert(new B { Id = ObjectId.GenerateNewId(), a = 3, b = 30, c = 300 });
            __collection.Insert(new B { Id = ObjectId.GenerateNewId(), a = 4, b = 40, c = 400 });

            return true;
        }

        [Fact]
        public void TestSimpleQueryHasIndexNameHint()
        {
            var query = __collection.AsQueryable().WithIndex("i");
            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.Equal("i", selectQuery.IndexHint.AsString);
        }

        [Fact]
        public void TestQueryWithSkipAndTakeHasIndexNameHint()
        {
            var query = __collection.AsQueryable().WithIndex("i").Skip(2).Take(5);
            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.Equal("i", selectQuery.IndexHint.AsString);

            query = __collection.AsQueryable().Skip(2).Take(5).WithIndex("i");
            selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.Equal("i", selectQuery.IndexHint.AsString);
        }

        [Fact]
        public void TestQueryWithProjectionHasIndexNameHint()
        {
            var query = __collection.AsQueryable().WithIndex("i").Select(o => o.a);
            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.Equal("i", selectQuery.IndexHint.AsString);
            Assert.Equal("(B o) => o.a", ExpressionFormatter.ToString(selectQuery.Projection));
        }

        [Fact]
        public void TestQueryWithConditionHasIndexNameHint()
        {
            var query = __collection.AsQueryable().Where(o => o.a == 1 && o.b == 3).WithIndex("i");
            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.Equal("i", selectQuery.IndexHint.AsString);
            Assert.Equal("{ \"a\" : 1, \"b\" : 3 }", selectQuery.BuildQuery().ToJson());
        }

        [Fact]
        public void TestQueryWithIndexBeforeConditionHasIndexNameHint()
        {
            var query = __collection.AsQueryable().WithIndex("i").Where(o => o.a == 1 && o.b == 3);
            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.Equal("i", selectQuery.IndexHint.AsString);
            Assert.Equal("{ \"a\" : 1, \"b\" : 3 }", selectQuery.BuildQuery().ToJson());
        }

        [Fact]
        public void TestIndexNameHintIsUsedInQuery()
        {
            var query = __collection.AsQueryable().Where(o => o.b == 1);
            var plan = query.Explain();
            if (LegacyTestConfiguration.Server.BuildInfo.Version < new Version(2, 7, 0))
            {
                Assert.Equal("BasicCursor", plan["cursor"].AsString); // Normally this query would use no index
            }
            else
            {
                var winningPlan = plan["queryPlanner"]["winningPlan"].AsBsonDocument;
                var stage = winningPlan["stage"].AsString;
                Assert.NotEqual("IXSCAN", stage);
            }

            // Now check that we can force it to use our index
            query = __collection.AsQueryable().Where(o => o.a == 1).WithIndex("i");
            plan = query.Explain();
            if (LegacyTestConfiguration.Server.BuildInfo.Version < new Version(2, 7, 0))
            {
                Assert.Equal("BtreeCursor i", plan["cursor"].AsString);
            }
            else
            {
                var winningPlan = plan["queryPlanner"]["winningPlan"].AsBsonDocument;
                if (winningPlan.Contains("shards"))
                {
                    winningPlan = winningPlan["shards"][0]["winningPlan"].AsBsonDocument;
                }
                var inputStage = winningPlan["inputStage"].AsBsonDocument;
                var stage = inputStage["stage"].AsString;
                var keyPattern = inputStage["keyPattern"].AsBsonDocument;
                Assert.Equal("IXSCAN", stage);
                Assert.Equal(BsonDocument.Parse("{ a : 1, b : 1 }"), keyPattern);
            }
        }

        [Fact]
        public void TestSimpleQueryHasIndexDocumentHint()
        {
            var query = __collection.AsQueryable().WithIndex(new BsonDocument("x", 1));
            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.Equal(new BsonDocument("x", 1), selectQuery.IndexHint.AsBsonDocument);
        }

        [Fact]
        public void TestQueryWithSkipAndTakeHasIndexDocumentHint()
        {
            var query = __collection.AsQueryable().WithIndex(new BsonDocument("x", 1)).Skip(2).Take(5);
            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.Equal(new BsonDocument("x", 1), selectQuery.IndexHint.AsBsonDocument);

            query = __collection.AsQueryable().Skip(2).Take(5).WithIndex(new BsonDocument("x", 1));
            selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.Equal(new BsonDocument("x", 1), selectQuery.IndexHint.AsBsonDocument);
        }

        [Fact]
        public void TestQueryWithProjectionHasIndexDocumentHint()
        {
            var query = __collection.AsQueryable().WithIndex(new BsonDocument("x", 1)).Select(o => o.a);
            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.Equal(new BsonDocument("x", 1), selectQuery.IndexHint.AsBsonDocument);
            Assert.Equal("(B o) => o.a", ExpressionFormatter.ToString(selectQuery.Projection));
        }

        [Fact]
        public void TestQueryWithConditionHasIndexDocumentHint()
        {
            var query = __collection.AsQueryable().Where(o => o.a == 1 && o.b == 3).WithIndex(new BsonDocument("x", 1));
            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.Equal(new BsonDocument("x", 1), selectQuery.IndexHint.AsBsonDocument);
            Assert.Equal("{ \"a\" : 1, \"b\" : 3 }", selectQuery.BuildQuery().ToJson());
        }

        [Fact]
        public void TestQueryWithIndexBeforeConditionHasIndexDocumentHint()
        {
            var query = __collection.AsQueryable().WithIndex(new BsonDocument("x", 1)).Where(o => o.a == 1 && o.b == 3);
            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.Equal(new BsonDocument("x", 1), selectQuery.IndexHint.AsBsonDocument);
            Assert.Equal("{ \"a\" : 1, \"b\" : 3 }", selectQuery.BuildQuery().ToJson());
        }

        [Fact]
        public void TestIndexDocumentHintIsUsedInQuery()
        {
            var query = __collection.AsQueryable().Where(o => o.b == 1);
            var plan = query.Explain();
            if (LegacyTestConfiguration.Server.BuildInfo.Version < new Version(2, 7, 0))
            {
                Assert.Equal("BasicCursor", plan["cursor"].AsString); // Normally this query would use no index
            }
            else
            {
                var winningPlan = plan["queryPlanner"]["winningPlan"].AsBsonDocument;
                var stage = winningPlan["stage"].AsString;
                Assert.NotEqual("IXSCAN", stage);
            }

            // Now check that we can force it to use our index
            var indexDoc = new BsonDocument()
                .AddRange(new[] { new BsonElement("a", 1), new BsonElement("b", 1) });
            query = __collection.AsQueryable().Where(o => o.a == 1).WithIndex(indexDoc);
            plan = query.Explain();
            if (LegacyTestConfiguration.Server.BuildInfo.Version < new Version(2, 7, 0))
            {
                Assert.Equal("BtreeCursor i", plan["cursor"].AsString);
            }
            else
            {
                var winningPlan = plan["queryPlanner"]["winningPlan"].AsBsonDocument;
                if (winningPlan.Contains("shards"))
                {
                    winningPlan = winningPlan["shards"][0]["winningPlan"].AsBsonDocument;
                }
                var inputStage = winningPlan["inputStage"].AsBsonDocument;
                var stage = inputStage["stage"].AsString;
                var keyPattern = inputStage["keyPattern"].AsBsonDocument;
                Assert.Equal("IXSCAN", stage);
                Assert.Equal(BsonDocument.Parse("{ a : 1, b : 1 }"), keyPattern);
            }
        }

        [Fact]
        public void TestWithIndexCannotBeBeforeDistinct()
        {
            Assert.Throws<NotSupportedException>(
                () => __collection.AsQueryable().Select(o => o.a).WithIndex("i").Distinct().ToList());
        }

        [Fact]
        public void TestWithIndexCannotBeAfterDistinct()
        {
            Assert.Throws<NotSupportedException>(() => __collection.AsQueryable().Select(o => o.a).Distinct().WithIndex("i").ToList());
        }

        [Fact]
        public void TestThereCanOnlyBeOneIndexHint()
        {
            Assert.Throws<NotSupportedException>(() => __collection.AsQueryable().WithIndex("i").WithIndex(new BsonDocument("a", 1)).ToList());
        }

    }
}