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

            // documents inserted deliberately out of order to test sorting
            _collection.Insert(new C { X = 2, Y = 11 });
            _collection.Insert(new C { X = 1, Y = 11 });
            _collection.Insert(new C { X = 3, Y = 33 });
            _collection.Insert(new C { X = 5, Y = 44 });
            _collection.Insert(new C { X = 4, Y = 44 });
        }

        [Test]
        public void TestCountAll()
        {
            var count = (from c in _collection.AsQueryable<C>()
                         select c).Count();

            Assert.AreEqual(5, count);
        }

        [Test]
        public void TestCountTwo()
        {
            var count = (from c in _collection.AsQueryable<C>()
                         where c.Y == 11
                         select c).Count();

            Assert.AreEqual(2, count);
        }

        [Test]
        public void TestCountWithSkipAndTake()
        {
            var count = (from c in _collection.AsQueryable<C>()
                         select c).Skip(2).Take(2).Count();

            Assert.AreEqual(2, count);
        }

        [Test]
        public void TestElementAtOrDefaultWithManyMatches()
        {
            var element = (from c in _collection.AsQueryable<C>()
                         select c).ElementAtOrDefault(2);

            Assert.AreEqual(3, element.X);
            Assert.AreEqual(33, element.Y);
        }

        [Test]
        public void TestElementAtOrDefaultWithNoMatch()
        {
            var element = (from c in _collection.AsQueryable<C>()
                         where c.X == 9
                         select c).ElementAtOrDefault(0);
            Assert.IsNull(element);
        }

        [Test]
        public void TestElementAtOrDefaultWithOneMatch()
        {
            var element = (from c in _collection.AsQueryable<C>()
                         where c.X == 3
                         select c).ElementAtOrDefault(0);

            Assert.AreEqual(3, element.X);
            Assert.AreEqual(33, element.Y);
        }

        [Test]
        public void TestElementAtOrDefaultWithTwoMatches()
        {
            var element = (from c in _collection.AsQueryable<C>()
                         where c.Y == 11
                         select c).ElementAtOrDefault(1);

            Assert.AreEqual(1, element.X);
            Assert.AreEqual(11, element.Y);
        }

        [Test]
        public void TestElementAtWithManyMatches()
        {
            var element = (from c in _collection.AsQueryable<C>()
                         select c).ElementAt(2);

            Assert.AreEqual(3, element.X);
            Assert.AreEqual(33, element.Y);
        }

        [Test]
        public void TestElementAtWithNoMatch()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var element = (from c in _collection.AsQueryable<C>()
                             where c.X == 9
                             select c).ElementAt(0);
            });
        }

        [Test]
        public void TestElementAtWithOneMatch()
        {
            var element = (from c in _collection.AsQueryable<C>()
                         where c.X == 3
                         select c).ElementAt(0);

            Assert.AreEqual(3, element.X);
            Assert.AreEqual(33, element.Y);
        }

        [Test]
        public void TestElementAtWithTwoMatches()
        {
            var element = (from c in _collection.AsQueryable<C>()
                         where c.Y == 11
                         select c).ElementAt(1);

            Assert.AreEqual(1, element.X);
            Assert.AreEqual(11, element.Y);
        }

        [Test]
        public void TestFirstOrDefaultWithManyMatches()
        {
            var first = (from c in _collection.AsQueryable<C>()
                         select c).FirstOrDefault();

            Assert.AreEqual(2, first.X);
            Assert.AreEqual(11, first.Y);
        }

        [Test]
        public void TestFirstOrDefaultWithNoMatch()
        {
            var first = (from c in _collection.AsQueryable<C>()
                         where c.X == 9
                         select c).FirstOrDefault();
            Assert.IsNull(first);
        }

        [Test]
        public void TestFirstOrDefaultWithOneMatch()
        {
            var first = (from c in _collection.AsQueryable<C>()
                         where c.X == 3
                         select c).FirstOrDefault();

            Assert.AreEqual(3, first.X);
            Assert.AreEqual(33, first.Y);
        }

        [Test]
        public void TestFirstOrDefaultWithTwoMatches()
        {
            var first = (from c in _collection.AsQueryable<C>()
                         where c.Y == 11
                         select c).FirstOrDefault();

            Assert.AreEqual(2, first.X);
            Assert.AreEqual(11, first.Y);
        }

        [Test]
        public void TestFirstWithManyMatches()
        {
            var first = (from c in _collection.AsQueryable<C>()
                         select c).First();

            Assert.AreEqual(2, first.X);
            Assert.AreEqual(11, first.Y);
        }

        [Test]
        public void TestFirstWithNoMatch()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var first = (from c in _collection.AsQueryable<C>()
                             where c.X == 9
                             select c).First();
            });
        }

        [Test]
        public void TestFirstWithOneMatch()
        {
            var first = (from c in _collection.AsQueryable<C>()
                         where c.X == 3
                         select c).First();

            Assert.AreEqual(3, first.X);
            Assert.AreEqual(33, first.Y);
        }

        [Test]
        public void TestFirstWithTwoMatches()
        {
            var first = (from c in _collection.AsQueryable<C>()
                         where c.Y == 11
                         select c).First();

            Assert.AreEqual(2, first.X);
            Assert.AreEqual(11, first.Y);
        }

        [Test]
        public void TestLastOrDefaultWithManyMatches()
        {
            var last = (from c in _collection.AsQueryable<C>()
                         select c).LastOrDefault();

            Assert.AreEqual(4, last.X);
            Assert.AreEqual(44, last.Y);
        }

        [Test]
        public void TestLastOrDefaultWithNoMatch()
        {
            var last = (from c in _collection.AsQueryable<C>()
                         where c.X == 9
                         select c).LastOrDefault();
            Assert.IsNull(last);
        }

        [Test]
        public void TestLastOrDefaultWithOneMatch()
        {
            var last = (from c in _collection.AsQueryable<C>()
                         where c.X == 3
                         select c).LastOrDefault();

            Assert.AreEqual(3, last.X);
            Assert.AreEqual(33, last.Y);
        }

        [Test]
        public void TestLastOrDefaultWithOrderBy()
        {
            var last = (from c in _collection.AsQueryable<C>()
                        orderby c.X
                        select c).LastOrDefault();

            Assert.AreEqual(5, last.X);
            Assert.AreEqual(44, last.Y);
        }

        [Test]
        public void TestLastOrDefaultWithTwoMatches()
        {
            var last = (from c in _collection.AsQueryable<C>()
                         where c.Y == 11
                         select c).LastOrDefault();

            Assert.AreEqual(1, last.X);
            Assert.AreEqual(11, last.Y);
        }

        [Test]
        public void TestLastWithManyMatches()
        {
            var last = (from c in _collection.AsQueryable<C>()
                         select c).Last();

            Assert.AreEqual(4, last.X);
            Assert.AreEqual(44, last.Y);
        }

        [Test]
        public void TestLastWithNoMatch()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var last = (from c in _collection.AsQueryable<C>()
                             where c.X == 9
                             select c).Last();
            });
        }

        [Test]
        public void TestLastWithOneMatch()
        {
            var last = (from c in _collection.AsQueryable<C>()
                         where c.X == 3
                         select c).Last();

            Assert.AreEqual(3, last.X);
            Assert.AreEqual(33, last.Y);
        }

        [Test]
        public void TestLastWithOrderBy()
        {
            var last = (from c in _collection.AsQueryable<C>()
                        orderby c.X
                        select c).Last();

            Assert.AreEqual(5, last.X);
            Assert.AreEqual(44, last.Y);
        }

        [Test]
        public void TestLastWithTwoMatches()
        {
            var last = (from c in _collection.AsQueryable<C>()
                         where c.Y == 11
                         select c).Last();

            Assert.AreEqual(1, last.X);
            Assert.AreEqual(11, last.Y);
        }

        [Test]
        public void TestLongCountAll()
        {
            var longCount = (from c in _collection.AsQueryable<C>()
                         select c).LongCount();

            Assert.AreEqual(5L, longCount);
        }

        [Test]
        public void TestLongCountTwo()
        {
            var longCount = (from c in _collection.AsQueryable<C>()
                         where c.Y == 11
                         select c).LongCount();

            Assert.AreEqual(2L, longCount);
        }

        [Test]
        public void TestLongCountWithSkipAndTake()
        {
            var longCount = (from c in _collection.AsQueryable<C>()
                         select c).Skip(2).Take(2).LongCount();

            Assert.AreEqual(2L, longCount);
        }

        [Test]
        public void TestOrderByAscending()
        {
            var query = from c in _collection.AsQueryable<C>()
                        orderby c.X
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(_collection, query.Expression);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.IsNull(selectQuery.Where);
            Assert.AreEqual(1, selectQuery.OrderBy.Count);
            Assert.AreEqual("c => c.X", selectQuery.OrderBy[0].Key.ToString());
            Assert.AreEqual(OrderByDirection.Ascending, selectQuery.OrderBy[0].Direction);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.IsNull(selectQuery.CreateMongoQuery());
            var results = query.ToList();
            Assert.AreEqual(5, results.Count);
            Assert.AreEqual(1, results.First().X);
            Assert.AreEqual(5, results.Last().X);
        }

        [Test]
        public void TestOrderByAscendingThenByAscending()
        {
            var query = from c in _collection.AsQueryable<C>()
                        orderby c.Y, c.X
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(_collection, query.Expression);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.IsNull(selectQuery.Where);
            Assert.AreEqual(2, selectQuery.OrderBy.Count);
            Assert.AreEqual("c => c.Y", selectQuery.OrderBy[0].Key.ToString());
            Assert.AreEqual(OrderByDirection.Ascending, selectQuery.OrderBy[0].Direction);
            Assert.AreEqual("c => c.X", selectQuery.OrderBy[1].Key.ToString());
            Assert.AreEqual(OrderByDirection.Ascending, selectQuery.OrderBy[1].Direction);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.IsNull(selectQuery.CreateMongoQuery());
            var results = query.ToList();
            Assert.AreEqual(5, results.Count);
            Assert.AreEqual(1, results.First().X);
            Assert.AreEqual(5, results.Last().X);
        }

        [Test]
        public void TestOrderByAscendingThenByDescending()
        {
            var query = from c in _collection.AsQueryable<C>()
                        orderby c.Y, c.X descending
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(_collection, query.Expression);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.IsNull(selectQuery.Where);
            Assert.AreEqual(2, selectQuery.OrderBy.Count);
            Assert.AreEqual("c => c.Y", selectQuery.OrderBy[0].Key.ToString());
            Assert.AreEqual(OrderByDirection.Ascending, selectQuery.OrderBy[0].Direction);
            Assert.AreEqual("c => c.X", selectQuery.OrderBy[1].Key.ToString());
            Assert.AreEqual(OrderByDirection.Descending, selectQuery.OrderBy[1].Direction);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.IsNull(selectQuery.CreateMongoQuery());
            var results = query.ToList();
            Assert.AreEqual(5, results.Count);
            Assert.AreEqual(2, results.First().X);
            Assert.AreEqual(4, results.Last().X);
        }

        [Test]
        public void TestOrderByDescending()
        {
            var query = from c in _collection.AsQueryable<C>()
                        orderby c.X descending
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(_collection, query.Expression);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.IsNull(selectQuery.Where);
            Assert.AreEqual(1, selectQuery.OrderBy.Count);
            Assert.AreEqual("c => c.X", selectQuery.OrderBy[0].Key.ToString());
            Assert.AreEqual(OrderByDirection.Descending, selectQuery.OrderBy[0].Direction);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.IsNull(selectQuery.CreateMongoQuery());
            var results = query.ToList();
            Assert.AreEqual(5, results.Count);
            Assert.AreEqual(5, results.First().X);
            Assert.AreEqual(1, results.Last().X);
        }

        [Test]
        public void TestOrderByDescendingThenByAscending()
        {
            var query = from c in _collection.AsQueryable<C>()
                        orderby c.Y descending, c.X
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(_collection, query.Expression);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.IsNull(selectQuery.Where);
            Assert.AreEqual(2, selectQuery.OrderBy.Count);
            Assert.AreEqual("c => c.Y", selectQuery.OrderBy[0].Key.ToString());
            Assert.AreEqual(OrderByDirection.Descending, selectQuery.OrderBy[0].Direction);
            Assert.AreEqual("c => c.X", selectQuery.OrderBy[1].Key.ToString());
            Assert.AreEqual(OrderByDirection.Ascending, selectQuery.OrderBy[1].Direction);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.IsNull(selectQuery.CreateMongoQuery());
            var results = query.ToList();
            Assert.AreEqual(5, results.Count);
            Assert.AreEqual(4, results.First().X);
            Assert.AreEqual(2, results.Last().X);
        }

        [Test]
        public void TestOrderByDescendingThenByDescending()
        {
            var query = from c in _collection.AsQueryable<C>()
                        orderby c.Y descending, c.X descending
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(_collection, query.Expression);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.IsNull(selectQuery.Where);
            Assert.AreEqual(2, selectQuery.OrderBy.Count);
            Assert.AreEqual("c => c.Y", selectQuery.OrderBy[0].Key.ToString());
            Assert.AreEqual(OrderByDirection.Descending, selectQuery.OrderBy[0].Direction);
            Assert.AreEqual("c => c.X", selectQuery.OrderBy[1].Key.ToString());
            Assert.AreEqual(OrderByDirection.Descending, selectQuery.OrderBy[1].Direction);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.IsNull(selectQuery.CreateMongoQuery());
            var results = query.ToList();
            Assert.AreEqual(5, results.Count);
            Assert.AreEqual(5, results.First().X);
            Assert.AreEqual(1, results.Last().X);
        }

        [Test]
        public void TestOrderByDuplicateNotAllowed()
        {
            var query = from c in _collection.AsQueryable<C>()
                        orderby c.X
                        orderby c.Y
                        select c;

            Assert.Throws<InvalidOperationException>(() => { MongoQueryTranslator.Translate(_collection, query.Expression); });
        }

        [Test]
        public void TestProjection()
        {
            var query = from c in _collection.AsQueryable<C>()
                        select c.X;

            var translatedQuery = MongoQueryTranslator.Translate(_collection, query.Expression);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.IsNull(selectQuery.Where);
            Assert.IsNull(selectQuery.OrderBy);
            Assert.AreEqual("c => c.X", selectQuery.Projection.ToString());
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.IsNull(selectQuery.CreateMongoQuery());

            var result = query.ToList();
            Assert.AreEqual(5, result.Count);
            Assert.AreEqual(2, result.First());
            Assert.AreEqual(4, result.Last());
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
        public void TestSingleOrDefaultWithManyMatches()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var single = (from c in _collection.AsQueryable<C>()
                             select c).SingleOrDefault();
            });
        }

        [Test]
        public void TestSingleOrDefaultWithNoMatch()
        {
            var single = (from c in _collection.AsQueryable<C>()
                         where c.X == 9
                         select c).SingleOrDefault();
            Assert.IsNull(single);
        }

        [Test]
        public void TestSingleOrDefaultWithOneMatch()
        {
            var single = (from c in _collection.AsQueryable<C>()
                         where c.X == 3
                         select c).SingleOrDefault();

            Assert.AreEqual(3, single.X);
            Assert.AreEqual(33, single.Y);
        }

        [Test]
        public void TestSingleOrDefaultWithTwoMatches()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var single = (from c in _collection.AsQueryable<C>()
                              where c.Y == 11
                              select c).SingleOrDefault();
            });
        }

        [Test]
        public void TestSingleWithManyMatches()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var single = (from c in _collection.AsQueryable<C>()
                              select c).Single();
            });
        }

        [Test]
        public void TestSingleWithNoMatch()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var single = (from c in _collection.AsQueryable<C>()
                             where c.X == 9
                             select c).Single();
            });
        }

        [Test]
        public void TestSingleWithOneMatch()
        {
            var single = (from c in _collection.AsQueryable<C>()
                         where c.X == 3
                         select c).Single();

            Assert.AreEqual(3, single.X);
            Assert.AreEqual(33, single.Y);
        }

        [Test]
        public void TestSingleWithTwoMatches()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var single = (from c in _collection.AsQueryable<C>()
                              where c.Y == 11
                              select c).Single();
            });
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
        public void TestThenByWithMissingOrderBy()
        {
            // not sure this could ever happen in real life without deliberate sabotaging like with this cast
            var query = ((IOrderedQueryable<C>)_collection.AsQueryable<C>())
                .ThenBy(c => c.X);

            Assert.Throws<InvalidOperationException>(() => { MongoQueryTranslator.Translate(_collection, query.Expression); });
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
        public void TestWhereXEquals1OrYEquals33()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.X == 1 || c.Y == 33
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(_collection, query.Expression);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("c => ((c.X = 1) || (c.Y = 33))", selectQuery.Where.ToString());
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"$or\" : [{ \"X\" : 1 }, { \"Y\" : 33 }] }", selectQuery.CreateMongoQuery().ToJson());
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
