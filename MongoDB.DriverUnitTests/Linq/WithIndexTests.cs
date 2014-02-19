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

using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Linq
{
    [TestFixture]
    public class WithIndexTests
    {
        private class B
        {
            public ObjectId Id;
            public int a;
            public int b;
            public int c;
        }

        private MongoServer _server;
        private MongoDatabase _database;
        private MongoCollection<B> _collection;

        [TestFixtureSetUp]
        public void Setup()
        {
            _server = Configuration.TestServer;
            _database = Configuration.TestDatabase;
            _collection = Configuration.GetTestCollection<B>();

            _collection.Drop();
            _collection.CreateIndex(new IndexKeysBuilder().Ascending("a", "b"), IndexOptions.SetName("i"));
            _collection.CreateIndex(new IndexKeysBuilder().Ascending("a", "b"), IndexOptions.SetName("i"));

            _collection.Insert(new B { Id = ObjectId.GenerateNewId(), a = 1, b = 10, c = 100 });
            _collection.Insert(new B { Id = ObjectId.GenerateNewId(), a = 2, b = 20, c = 200 });
            _collection.Insert(new B { Id = ObjectId.GenerateNewId(), a = 3, b = 30, c = 300 });
            _collection.Insert(new B { Id = ObjectId.GenerateNewId(), a = 4, b = 40, c = 400 });
        }

        [Test]
        public void TestSimpleQueryHasIndexNameHint()
        {
            var query = _collection.AsQueryable().WithIndex("i");
            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.AreEqual("i", selectQuery.IndexHint.AsString);
        }

        [Test]
        public void TestQueryWithSkipAndTakeHasIndexNameHint()
        {
            var query = _collection.AsQueryable().WithIndex("i").Skip(2).Take(5);
            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.AreEqual("i", selectQuery.IndexHint.AsString);

            query = _collection.AsQueryable().Skip(2).Take(5).WithIndex("i");
            selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.AreEqual("i", selectQuery.IndexHint.AsString);
        }

        [Test]
        public void TestQueryWithProjectionHasIndexNameHint()
        {
            var query = _collection.AsQueryable().WithIndex("i").Select(o => o.a);
            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.AreEqual("i", selectQuery.IndexHint.AsString);
            Assert.AreEqual("(B o) => o.a", ExpressionFormatter.ToString(selectQuery.Projection));
        }

        [Test]
        public void TestQueryWithConditionHasIndexNameHint()
        {
            var query = _collection.AsQueryable().Where(o=>o.a == 1 && o.b == 3).WithIndex("i");
            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.AreEqual("i", selectQuery.IndexHint.AsString);
            Assert.AreEqual("{ \"a\" : 1, \"b\" : 3 }", selectQuery.BuildQuery().ToJson());
        }

        [Test]
        public void TestQueryWithIndexBeforeConditionHasIndexNameHint()
        {
            var query = _collection.AsQueryable().WithIndex("i").Where(o => o.a == 1 && o.b == 3);
            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.AreEqual("i", selectQuery.IndexHint.AsString);
            Assert.AreEqual("{ \"a\" : 1, \"b\" : 3 }", selectQuery.BuildQuery().ToJson());
        }

        [Test]
        public void TestIndexNameHintIsUsedInQuery()
        {
            var query = _collection.AsQueryable().Where(o => o.b == 1);
            var plan = query.Explain();
            Assert.AreEqual("BasicCursor", plan["cursor"].AsString); //Normally this query would use no index

            //Now check that we can force it to use our index
            query = _collection.AsQueryable().Where(o => o.a == 1).WithIndex("i");
            plan = query.Explain();
            Assert.AreEqual("BtreeCursor i", plan["cursor"].AsString);
        }

        [Test]
        public void TestSimpleQueryHasIndexDocumentHint()
        {
            var query = _collection.AsQueryable().WithIndex(new BsonDocument("x", 1));
            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.AreEqual(new BsonDocument("x", 1), selectQuery.IndexHint.AsBsonDocument);
        }

        [Test]
        public void TestQueryWithSkipAndTakeHasIndexDocumentHint()
        {
            var query = _collection.AsQueryable().WithIndex(new BsonDocument("x", 1)).Skip(2).Take(5);
            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.AreEqual(new BsonDocument("x", 1), selectQuery.IndexHint.AsBsonDocument);

            query = _collection.AsQueryable().Skip(2).Take(5).WithIndex(new BsonDocument("x", 1));
            selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.AreEqual(new BsonDocument("x", 1), selectQuery.IndexHint.AsBsonDocument);
        }

        [Test]
        public void TestQueryWithProjectionHasIndexDocumentHint()
        {
            var query = _collection.AsQueryable().WithIndex(new BsonDocument("x", 1)).Select(o => o.a);
            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.AreEqual(new BsonDocument("x", 1), selectQuery.IndexHint.AsBsonDocument);
            Assert.AreEqual("(B o) => o.a", ExpressionFormatter.ToString(selectQuery.Projection));
        }

        [Test]
        public void TestQueryWithConditionHasIndexDocumentHint()
        {
            var query = _collection.AsQueryable().Where(o => o.a == 1 && o.b == 3).WithIndex(new BsonDocument("x", 1));
            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.AreEqual(new BsonDocument("x", 1), selectQuery.IndexHint.AsBsonDocument);
            Assert.AreEqual("{ \"a\" : 1, \"b\" : 3 }", selectQuery.BuildQuery().ToJson());
        }

        [Test]
        public void TestQueryWithIndexBeforeConditionHasIndexDocumentHint()
        {
            var query = _collection.AsQueryable().WithIndex(new BsonDocument("x", 1)).Where(o => o.a == 1 && o.b == 3);
            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.AreEqual(new BsonDocument("x", 1), selectQuery.IndexHint.AsBsonDocument);
            Assert.AreEqual("{ \"a\" : 1, \"b\" : 3 }", selectQuery.BuildQuery().ToJson());
        }

        [Test]
        public void TestIndexDocumentHintIsUsedInQuery()
        {
            var query = _collection.AsQueryable().Where(o => o.b == 1);
            var plan = query.Explain();
            Assert.AreEqual("BasicCursor", plan["cursor"].AsString); //Normally this query would use no index

            //Now check that we can force it to use our index
            var indexDoc = new BsonDocument()
                .AddRange(new[] { new BsonElement("a", 1), new BsonElement("b", 1) });
            query = _collection.AsQueryable().Where(o => o.a == 1).WithIndex(indexDoc);
            plan = query.Explain();
            Assert.AreEqual("BtreeCursor i", plan["cursor"].AsString);
        }

        [Test]
        public void TestWithIndexCannotBeBeforeDistinct()
        {
            Assert.Throws<NotSupportedException>(
                () => _collection.AsQueryable().Select(o => o.a).WithIndex("i").Distinct().ToList());
        }

        [Test]
        public void TestWithIndexCannotBeAfterDistinct()
        {
            Assert.Throws<NotSupportedException>(()=> _collection.AsQueryable().Select(o => o.a).Distinct().WithIndex("i").ToList());
        }

        [Test]
        public void TestThereCanOnlyBeOneIndexHint()
        {
            Assert.Throws<NotSupportedException>(() => _collection.AsQueryable().WithIndex("i").WithIndex(new BsonDocument("a",1)).ToList());
        }

    }
}