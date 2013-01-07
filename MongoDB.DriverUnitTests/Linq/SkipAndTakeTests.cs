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

using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Linq
{
    [TestFixture]
    public class SkipAndTakeTests
    {
        private class C
        {
            public ObjectId Id { get; set; }
            [BsonElement("x")]
            public int? X { get; set; }
        }

        private MongoServer _server;
        private MongoDatabase _database;
        private MongoCollection<C> _collection;

        [TestFixtureSetUp]
        public void Setup()
        {
            _server = Configuration.TestServer;
            _database = Configuration.TestDatabase;
            _collection = Configuration.GetTestCollection<C>();
        }

        [Test]
        public void TestSkip()
        {
            var s = new List<string> { "one", "two", "three" };

            var list = s.Take(3).Take(5).ToList();

            var query = _collection.AsQueryable<C>().Skip(5);

            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.AreEqual(5, selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);
        }

        [Test]
        public void TestSkipThenSkip()
        {
            var query = _collection.AsQueryable<C>().Skip(5).Skip(15);

            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.AreEqual(20, selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);
        }

        [Test]
        public void TestSkipThenTake()
        {
            var query = _collection.AsQueryable<C>().Skip(5).Take(20);

            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.AreEqual(5, selectQuery.Skip);
            Assert.AreEqual(20, selectQuery.Take);
        }

        [Test]
        public void TestSkipThenTakeThenSkip()
        {
            var query = _collection.AsQueryable<C>().Skip(5).Take(20).Skip(10);

            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.AreEqual(15, selectQuery.Skip);
            Assert.AreEqual(10, selectQuery.Take);
        }

        [Test]
        public void TestSkipThenTakeThenSkipWithTooMany()
        {
            var query = _collection.AsQueryable<C>().Skip(5).Take(20).Skip(30);

            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.IsNull(selectQuery.Skip);
            Assert.AreEqual(0, selectQuery.Take);
        }

        [Test]
        public void TestSkipThenWhereThenTake()
        {
            var query = _collection.AsQueryable<C>().Skip(20).Where(c => c.X == 10).Take(30);

            Assert.Throws(typeof(MongoQueryException), () => MongoQueryTranslator.Translate(query));
        }

        [Test]
        public void TestTake()
        {
            var query = _collection.AsQueryable<C>().Take(5);

            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.IsNull(selectQuery.Skip);
            Assert.AreEqual(5, selectQuery.Take);
        }

        [Test]
        public void TestTakeThenSkip()
        {
            var query = _collection.AsQueryable<C>().Take(20).Skip(10);

            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.AreEqual(10, selectQuery.Skip);
            Assert.AreEqual(10, selectQuery.Take);
        }

        [Test]
        public void TestTakeThenSkipThenTake()
        {
            var query = _collection.AsQueryable<C>().Take(20).Skip(10).Take(5);

            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.AreEqual(10, selectQuery.Skip);
            Assert.AreEqual(5, selectQuery.Take);
        }

        [Test]
        public void TestTakeThenSkipThenTakeWithTooMany()
        {
            var query = _collection.AsQueryable<C>().Take(20).Skip(10).Take(15);

            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.AreEqual(10, selectQuery.Skip);
            Assert.AreEqual(10, selectQuery.Take);
        }

        [Test]
        public void TestTakeThenTake()
        {
            var query = _collection.AsQueryable<C>().Take(20).Take(5);

            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.IsNull(selectQuery.Skip);
            Assert.AreEqual(5, selectQuery.Take);
        }

        [Test]
        public void TestTakeThenWhereThenSkip()
        {
            var query = _collection.AsQueryable<C>().Take(20).Where(c => c.X == 10).Skip(30);

            Assert.Throws(typeof(MongoQueryException), () => MongoQueryTranslator.Translate(query));
        }

        [Test]
        public void TestWhereThenSkipThenTake()
        {
            var query = _collection.AsQueryable<C>().Where(c => c.X == 10).Skip(10).Take(5);

            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(query);
            Assert.AreEqual(10, selectQuery.Skip);
            Assert.AreEqual(5, selectQuery.Take);
        }

        [Test]
        public void Test0Take()
        {
            var query = _collection.AsQueryable<C>().Take(0).ToList();
            Assert.AreEqual(0, query.Count);
        }

        [Test]
        public void TestOfTypeCWith0Take()
        {
            var query = _collection.AsQueryable<Uri>().OfType<C>().Take(0).ToList();
            Assert.AreEqual(0, query.Count);
        }
    }
}