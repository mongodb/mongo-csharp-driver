/* Copyright 2010-2012 10gen Inc.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;

namespace MongoDB.DriverOnlineTests.Linq
{
    [TestFixture]
    public class SelectQueryTests
    {
        private class C
        {
            public ObjectId Id { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
        }

        private MongoServer _server;
        private MongoDatabase _database;
        private MongoCollection<C> _collection;

        [TestFixtureSetUp]
        public void Setup()
        {
            _server = Configuration.TestServer;
            _server.Connect();
            _database = Configuration.TestDatabase;
            _collection = Configuration.GetTestCollection<C>();

            _collection.Drop();
            _collection.Insert(new C { X = 1, Y = 11 });
            _collection.Insert(new C { X = 2, Y = 22 });
            _collection.Insert(new C { X = 3, Y = 33 });
            _collection.Insert(new C { X = 4, Y = 44 });
            _collection.Insert(new C { X = 5, Y = 55 });
        }

        [Test]
        public void TestSelectAll()
        {
            var query = from c in _collection.AsQueryable<C>()
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(_collection, query.Expression);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.IsNull(selectQuery.Where);
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.IsNull(selectQuery.CreateMongoQuery());
            Assert.AreEqual(5, Consume(query));
        }

        [Test]
        public void TestSkip2()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c).Skip(2);

            var translatedQuery = MongoQueryTranslator.Translate(_collection, query.Expression);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.IsNull(selectQuery.Where);
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.AreEqual("2", selectQuery.Skip.ToString());
            Assert.IsNull(selectQuery.Take);

            Assert.IsNull(selectQuery.CreateMongoQuery());
            Assert.AreEqual(3, Consume(query));
        }

        [Test]
        public void TestTake2()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c).Take(2);

            var translatedQuery = MongoQueryTranslator.Translate(_collection, query.Expression);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.IsNull(selectQuery.Where);
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.AreEqual("2", selectQuery.Take.ToString());

            Assert.IsNull(selectQuery.CreateMongoQuery());
            Assert.AreEqual(2, Consume(query));
        }

        [Test]
        public void TestWhereXEquals1()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.X == 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(_collection, query.Expression);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("c => (c.X = 1)", selectQuery.Where.ToString());
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"X\" : 1 }", selectQuery.CreateMongoQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereXEquals1AndYEquals11()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.X == 1 && c.Y == 11
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(_collection, query.Expression);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("c => ((c.X = 1) && (c.Y = 11))", selectQuery.Where.ToString());
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"X\" : 1, \"Y\" : 11 }", selectQuery.CreateMongoQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereXEquals1OrYEquals22()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.X == 1 || c.Y == 22
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(_collection, query.Expression);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("c => ((c.X = 1) || (c.Y = 22))", selectQuery.Where.ToString());
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"$or\" : [{ \"X\" : 1 }, { \"Y\" : 22 }] }", selectQuery.CreateMongoQuery().ToJson());
            Assert.AreEqual(2, Consume(query));
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
