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

using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Linq
{
    [TestFixture]
    public class SelectOfTypeTests
    {
        private class B
        {
            public ObjectId Id;
            public int b;
        }

        private class C : B
        {
            public int c;
        }

        private class D : C
        {
            public int d;
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
            _collection.Insert(new B { Id = ObjectId.GenerateNewId(), b = 1 });
            _collection.Insert(new C { Id = ObjectId.GenerateNewId(), b = 2, c = 2 });
            _collection.Insert(new D { Id = ObjectId.GenerateNewId(), b = 3, c = 3, d = 3 });
        }

        [Test]
        public void TestOfTypeB()
        {
            var query = _collection.AsQueryable<B>().OfType<B>();

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(B), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.IsNull(selectQuery.Where);
            Assert.AreEqual(null, selectQuery.OfType); // OfType ignored because <T> was the same as <TDocument>
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.IsNull(selectQuery.BuildQuery());
            Assert.AreEqual(3, Consume(query));
        }

        [Test]
        public void TestOfTypeC()
        {
            var query = _collection.AsQueryable<B>().OfType<C>();

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(B), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(B x) => LinqToMongo.Inject({ \"_t\" : \"C\" })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.AreEqual(typeof(C), selectQuery.OfType);
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"_t\" : \"C\" }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query)); // should match 2 but for that you need to use the hierarchical discriminator
        }

        [Test]
        public void TestOfTypeCWhereCGreaterThan0()
        {
            var query = _collection.AsQueryable<B>().OfType<C>().Where(c => c.c > 0);

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(B), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (LinqToMongo.Inject({ \"_t\" : \"C\" }) && (c.c > 0))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.AreEqual(typeof(C), selectQuery.OfType);
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"_t\" : \"C\", \"c\" : { \"$gt\" : 0 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query)); // should match 2 but for that you need to use the hierarchical discriminator
        }

        [Test]
        public void TestOfTypeD()
        {
            var query = _collection.AsQueryable<B>().OfType<D>();

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(B), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(B x) => LinqToMongo.Inject({ \"_t\" : \"D\" })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.AreEqual(typeof(D), selectQuery.OfType);
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"_t\" : \"D\" }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestOfTypeDWithProjection()
        {
            var query = _collection.AsQueryable<B>().OfType<D>().Select(x => new { A = x.d });

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(B), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(B x) => LinqToMongo.Inject({ \"_t\" : \"D\" })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.AreEqual(typeof(D), selectQuery.OfType);
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNotNull(selectQuery.Projection);
            Assert.AreEqual("(D x) => new __AnonymousType<Int32>(x.d)", ExpressionFormatter.ToString(selectQuery.Projection));
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"_t\" : \"D\" }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereBGreaterThan0OfTypeCWhereCGreaterThan0()
        {
            var query = _collection.AsQueryable<B>().Where(b => b.b > 0).OfType<C>().Where(c => c.c > 0);

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(B), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (((c.b > 0) && LinqToMongo.Inject({ \"_t\" : \"C\" })) && (c.c > 0))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.AreEqual(typeof(C), selectQuery.OfType);
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"b\" : { \"$gt\" : 0 }, \"_t\" : \"C\", \"c\" : { \"$gt\" : 0 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query)); // should match 2 but for that you need to use the hierarchical discriminator
        }

        [Test]
        public void TestWhereBIsB()
        {
            var query =
                from b in _collection.AsQueryable<B>()
                where b is B
                select b;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(B), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(B b) => (b is B)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.AreEqual(null, selectQuery.OfType); // OfType ignored because <T> was the same as <TDocument>
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(3, Consume(query));
        }

        [Test]
        public void TestWhereBIsC()
        {
            var query =
                from b in _collection.AsQueryable<B>()
                where b is C
                select b;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(B), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(B b) => (b is C)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.AreEqual(null, selectQuery.OfType);
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"_t\" : \"C\" }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query)); // should match 2 but for that you need to use the hierarchical discriminator
        }

        [Test]
        public void TestWhereBIsD()
        {
            var query =
                from b in _collection.AsQueryable<B>()
                where b is D
                select b;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(B), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(B b) => (b is D)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.AreEqual(null, selectQuery.OfType);
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"_t\" : \"D\" }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereBTypeEqualsB()
        {
            var query =
                from b in _collection.AsQueryable<B>()
                where b.GetType() == typeof(B)
                select b;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(B), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(B b) => (b.GetType() == typeof(B))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.AreEqual(null, selectQuery.OfType); // OfType ignored because <T> was the same as <TDocument>
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(3, Consume(query));
        }

        [Test]
        public void TestWhereBTypeEqualsC()
        {
            var query =
                from b in _collection.AsQueryable<B>()
                where b.GetType() == typeof(C)
                select b;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(B), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(B b) => (b.GetType() == typeof(C))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.AreEqual(null, selectQuery.OfType);
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"_t.0\" : { \"$exists\" : false }, \"_t\" : \"C\" }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereBTypeEqualsD()
        {
            var query =
                from b in _collection.AsQueryable<B>()
                where b.GetType() == typeof(D)
                select b;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(B), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(B b) => (b.GetType() == typeof(D))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.AreEqual(null, selectQuery.OfType);
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"_t.0\" : { \"$exists\" : false }, \"_t\" : \"D\" }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        private int Consume<T>(IQueryable<T> query)
        {
            var count = 0;
            foreach (var c in query)
            {
                count++;
            }
            return count;
        }
    }
}
