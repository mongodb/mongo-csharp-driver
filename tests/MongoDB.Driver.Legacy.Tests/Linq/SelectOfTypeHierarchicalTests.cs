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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq
{
    public class SelectOfTypeHierarchicalTests
    {
        [BsonDiscriminator(RootClass = true)]
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

        private static MongoServer __server;
        private static MongoCollection<B> __collection;
        private static Lazy<bool> __lazyOneTimeSetup = new Lazy<bool>(OneTimeSetup);

        public SelectOfTypeHierarchicalTests()
        {
            var _ = __lazyOneTimeSetup.Value;
        }

        private static bool OneTimeSetup()
        {
            __server = LegacyTestConfiguration.Server;
            __collection = LegacyTestConfiguration.GetCollection<B>();

            __collection.Drop();
            __collection.Insert(new B { Id = ObjectId.GenerateNewId(), b = 1 });
            __collection.Insert(new C { Id = ObjectId.GenerateNewId(), b = 2, c = 2 });
            __collection.Insert(new D { Id = ObjectId.GenerateNewId(), b = 3, c = 3, d = 3 });

            return true;
        }

        [Fact]
        public void TestOfTypeB()
        {
            var query = __collection.AsQueryable<B>().OfType<B>();

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(B), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(B x) => LinqToMongo.Inject({ \"_t\" : \"B\" })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Equal(typeof(B), selectQuery.OfType);
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"_t\" : \"B\" }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(3, Consume(query));
        }

        [Fact]
        public void TestOfTypeC()
        {
            var query = __collection.AsQueryable<B>().OfType<C>();

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(B), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(B x) => LinqToMongo.Inject({ \"_t\" : \"C\" })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Equal(typeof(C), selectQuery.OfType);
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"_t\" : \"C\" }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(2, Consume(query));
        }

        [Fact]
        public void TestOfTypeCWhereCGreaterThan0()
        {
            var query = __collection.AsQueryable<B>().OfType<C>().Where(c => c.c > 0);

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(B), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (LinqToMongo.Inject({ \"_t\" : \"C\" }) && (c.c > 0))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Equal(typeof(C), selectQuery.OfType);
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"_t\" : \"C\", \"c\" : { \"$gt\" : 0 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(2, Consume(query));
        }

        [Fact]
        public void TestOfTypeD()
        {
            var query = __collection.AsQueryable<B>().OfType<D>();

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(B), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(B x) => LinqToMongo.Inject({ \"_t\" : \"D\" })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Equal(typeof(D), selectQuery.OfType);
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"_t\" : \"D\" }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereBGreaterThan0OfTypeCWhereCGreaterThan0()
        {
            var query = __collection.AsQueryable<B>().Where(b => b.b > 0).OfType<C>().Where(c => c.c > 0);

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(B), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (((c.b > 0) && LinqToMongo.Inject({ \"_t\" : \"C\" })) && (c.c > 0))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Equal(typeof(C), selectQuery.OfType);
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"b\" : { \"$gt\" : 0 }, \"_t\" : \"C\", \"c\" : { \"$gt\" : 0 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(2, Consume(query));
        }

        [Fact]
        public void TestWhereBIsB()
        {
            var query =
                from b in __collection.AsQueryable<B>()
                where b is B
                select b;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(B), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(B b) => (b is B)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Equal(null, selectQuery.OfType); // OfType ignored because <T> was the same as <TDocument>
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"_t\" : \"B\" }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(3, Consume(query));
        }

        [Fact]
        public void TestWhereBIsC()
        {
            var query =
                from b in __collection.AsQueryable<B>()
                where b is C
                select b;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(B), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(B b) => (b is C)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Equal(null, selectQuery.OfType);
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"_t\" : \"C\" }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(2, Consume(query));
        }

        [Fact]
        public void TestWhereBIsD()
        {
            var query =
                from b in __collection.AsQueryable<B>()
                where b is D
                select b;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(B), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(B b) => (b is D)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Equal(null, selectQuery.OfType);
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"_t\" : \"D\" }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereBTypeEqualsB()
        {
            if (__server.BuildInfo.Version >= new Version(2, 0, 0))
            {
                var query =
                    from b in __collection.AsQueryable<B>()
                    where b.GetType() == typeof(B)
                    select b;

                var translatedQuery = MongoQueryTranslator.Translate(query);
                Assert.IsType<SelectQuery>(translatedQuery);
                Assert.Same(__collection, translatedQuery.Collection);
                Assert.Same(typeof(B), translatedQuery.DocumentType);

                var selectQuery = (SelectQuery)translatedQuery;
                Assert.Equal("(B b) => (b.GetType() == typeof(B))", ExpressionFormatter.ToString(selectQuery.Where));
                Assert.Equal(null, selectQuery.OfType); // OfType ignored because <T> was the same as <TDocument>
                Assert.Null(selectQuery.OrderBy);
                Assert.Null(selectQuery.Projection);
                Assert.Null(selectQuery.Skip);
                Assert.Null(selectQuery.Take);

                Assert.Equal("{ \"_t.0\" : { \"$exists\" : false }, \"_t\" : \"B\" }", selectQuery.BuildQuery().ToJson());
                Assert.Equal(1, Consume(query));
            }
        }

        [Fact]
        public void TestWhereBTypeEqualsC()
        {
            var query =
                from b in __collection.AsQueryable<B>()
                where b.GetType() == typeof(C)
                select b;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(B), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(B b) => (b.GetType() == typeof(C))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Equal(null, selectQuery.OfType);
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"_t\" : { \"$size\" : 2 }, \"_t.0\" : \"B\", \"_t.1\" : \"C\" }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereBTypeEqualsD()
        {
            var query =
                from b in __collection.AsQueryable<B>()
                where b.GetType() == typeof(D)
                select b;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(B), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(B b) => (b.GetType() == typeof(D))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Equal(null, selectQuery.OfType);
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"_t\" : { \"$size\" : 3 }, \"_t.0\" : \"B\", \"_t.1\" : \"C\", \"_t.2\" : \"D\" }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
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
