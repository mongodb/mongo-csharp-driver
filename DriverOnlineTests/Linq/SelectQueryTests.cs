﻿/* Copyright 2010-2012 10gen Inc.
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

        // used to test some query operators that have an IEqualityComparer parameter
        private class CEqualityComparer : IEqualityComparer<C>
        {
            public bool Equals(C x, C y)
            {
                return x.Id.Equals(y.Id) && x.X.Equals(y.X) && x.Y.Equals(y.Y);
            }

            public int GetHashCode(C obj)
            {
                return obj.GetHashCode();
            }
        }

        // used to test some query operators that have an IEqualityComparer parameter
        private class Int32EqualityComparer : IEqualityComparer<int>
        {
            public bool Equals(int x, int y)
            {
                return x == y;
            }

            public int GetHashCode(int obj)
            {
                return obj.GetHashCode();
            }
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
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Aggregate query operator is not supported.")]
        public void TestAggregate()
        {
            var result = (from c in _collection.AsQueryable<C>()
                             select c).Aggregate((a, b) => null);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Aggregate query operator is not supported.")]
        public void TestAggregateWithAccumulator()
        {
            var result = (from c in _collection.AsQueryable<C>()
                             select c).Aggregate<C, int>(0, (a, c) => 0);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Aggregate query operator is not supported.")]
        public void TestAggregateWithAccumulatorAndSelector()
        {
            var result = (from c in _collection.AsQueryable<C>()
                             select c).Aggregate<C, int, int>(0, (a, c) => 0, a => a);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The All query operator is not supported.")]
        public void TestAll()
        {
            var result = (from c in _collection.AsQueryable<C>()
                             select c).All(c => true);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Any query operator is not supported.")]
        public void TestAny()
        {
            var result = (from c in _collection.AsQueryable<C>()
                             select c).Any();
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Any query operator is not supported.")]
        public void TestAnyWithPredicate()
        {
            var result = (from c in _collection.AsQueryable<C>()
                             select c).Any(c => true);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Average query operator is not supported.")]
        public void TestAverage()
        {
            var result = (from c in _collection.AsQueryable<C>()
                             select 1.0).Average();
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Average query operator is not supported.")]
        public void TestAverageNullable()
        {
            var result = (from c in _collection.AsQueryable<C>()
                             select (double?)1.0).Average();
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Average query operator is not supported.")]
        public void TestAverageWithSelector()
        {
            var result = (from c in _collection.AsQueryable<C>()
                             select c).Average(c => 1.0);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Average query operator is not supported.")]
        public void TestAverageWithSelectorNullable()
        {
            var result = (from c in _collection.AsQueryable<C>()
                             select c).Average(c => (double?)1.0);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Cast query operator is not supported.")]
        public void TestCast()
        {
            var query = (from c in _collection.AsQueryable<C>()
                             select c).Cast<C>();
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Concat query operator is not supported.")]
        public void TestConcat()
        {
            var source2 = new C[0];
            var query = (from c in _collection.AsQueryable<C>()
                          select c).Concat(source2);
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Contains query operator is not supported.")]
        public void TestContains()
        {
            var item = new C();
            var result = (from c in _collection.AsQueryable<C>()
                          select c).Contains(item);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Contains query operator is not supported.")]
        public void TestContainsWithEqualityComparer()
        {
            var item = new C();
            var result = (from c in _collection.AsQueryable<C>()
                          select c).Contains(item, new CEqualityComparer());
        }

        [Test]
        public void TestCount2()
        {
            var result = (from c in _collection.AsQueryable<C>()
                         where c.Y == 11
                         select c).Count();

            Assert.AreEqual(2, result);
        }

        [Test]
        public void TestCount5()
        {
            var result = (from c in _collection.AsQueryable<C>()
                         select c).Count();

            Assert.AreEqual(5, result);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Count with predicate query operator is not supported.")]
        public void TestCountWithPredicate()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).Count(c => true);
        }

        [Test]
        public void TestCountWithSkipAndTake()
        {
            var result = (from c in _collection.AsQueryable<C>()
                         select c).Skip(2).Take(2).Count();

            Assert.AreEqual(2, result);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The DefaultIfEmpty query operator is not supported.")]
        public void TestDefaultIfEmpty()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c).DefaultIfEmpty();
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The DefaultIfEmpty query operator is not supported.")]
        public void TestDefaultIfEmptyWithDefaultValue()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c).DefaultIfEmpty(null);
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Distinct query operator is not supported.")]
        public void TestDistinct()
        {
            var query = _collection.AsQueryable<C>().Distinct();
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Distinct query operator is not supported.")]
        public void TestDistinctWithEqualityComparer()
        {
            var query = _collection.AsQueryable<C>().Distinct(new CEqualityComparer());
            query.ToList(); // execute query
        }

        [Test]
        public void TestElementAtOrDefaultWithManyMatches()
        {
            var result = (from c in _collection.AsQueryable<C>()
                         select c).ElementAtOrDefault(2);

            Assert.AreEqual(3, result.X);
            Assert.AreEqual(33, result.Y);
        }

        [Test]
        public void TestElementAtOrDefaultWithNoMatch()
        {
            var result = (from c in _collection.AsQueryable<C>()
                         where c.X == 9
                         select c).ElementAtOrDefault(0);
            Assert.IsNull(result);
        }

        [Test]
        public void TestElementAtOrDefaultWithOneMatch()
        {
            var result = (from c in _collection.AsQueryable<C>()
                         where c.X == 3
                         select c).ElementAtOrDefault(0);

            Assert.AreEqual(3, result.X);
            Assert.AreEqual(33, result.Y);
        }

        [Test]
        public void TestElementAtOrDefaultWithTwoMatches()
        {
            var result = (from c in _collection.AsQueryable<C>()
                         where c.Y == 11
                         select c).ElementAtOrDefault(1);

            Assert.AreEqual(1, result.X);
            Assert.AreEqual(11, result.Y);
        }

        [Test]
        public void TestElementAtWithManyMatches()
        {
            var result = (from c in _collection.AsQueryable<C>()
                         select c).ElementAt(2);

            Assert.AreEqual(3, result.X);
            Assert.AreEqual(33, result.Y);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Sequence contains no elements")]
        public void TestElementAtWithNoMatch()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          where c.X == 9
                          select c).ElementAt(0);
        }

        [Test]
        public void TestElementAtWithOneMatch()
        {
            var result = (from c in _collection.AsQueryable<C>()
                         where c.X == 3
                         select c).ElementAt(0);

            Assert.AreEqual(3, result.X);
            Assert.AreEqual(33, result.Y);
        }

        [Test]
        public void TestElementAtWithTwoMatches()
        {
            var result = (from c in _collection.AsQueryable<C>()
                         where c.Y == 11
                         select c).ElementAt(1);

            Assert.AreEqual(1, result.X);
            Assert.AreEqual(11, result.Y);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Except query operator is not supported.")]
        public void TestExcept()
        {
            var source2 = new C[0];
            var query = (from c in _collection.AsQueryable<C>()
                         select c).Except(source2);
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Except query operator is not supported.")]
        public void TestExceptWithEqualityComparer()
        {
            var source2 = new C[0];
            var query = (from c in _collection.AsQueryable<C>()
                         select c).Except(source2, new CEqualityComparer());
            query.ToList(); // execute query
        }

        [Test]
        public void TestFirstOrDefaultWithManyMatches()
        {
            var result = (from c in _collection.AsQueryable<C>()
                         select c).FirstOrDefault();

            Assert.AreEqual(2, result.X);
            Assert.AreEqual(11, result.Y);
        }

        [Test]
        public void TestFirstOrDefaultWithNoMatch()
        {
            var result = (from c in _collection.AsQueryable<C>()
                         where c.X == 9
                         select c).FirstOrDefault();
            Assert.IsNull(result);
        }

        [Test]
        public void TestFirstOrDefaultWithOneMatch()
        {
            var result = (from c in _collection.AsQueryable<C>()
                         where c.X == 3
                         select c).FirstOrDefault();

            Assert.AreEqual(3, result.X);
            Assert.AreEqual(33, result.Y);
        }

        [Test]
        public void TestFirstOrDefaultWithTwoMatches()
        {
            var result = (from c in _collection.AsQueryable<C>()
                         where c.Y == 11
                         select c).FirstOrDefault();

            Assert.AreEqual(2, result.X);
            Assert.AreEqual(11, result.Y);
        }

        [Test]
        public void TestFirstWithManyMatches()
        {
            var result = (from c in _collection.AsQueryable<C>()
                         select c).First();

            Assert.AreEqual(2, result.X);
            Assert.AreEqual(11, result.Y);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Sequence contains no elements")]
        public void TestFirstWithNoMatch()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          where c.X == 9
                          select c).First();
        }

        [Test]
        public void TestFirstWithOneMatch()
        {
            var result = (from c in _collection.AsQueryable<C>()
                         where c.X == 3
                         select c).First();

            Assert.AreEqual(3, result.X);
            Assert.AreEqual(33, result.Y);
        }

        [Test]
        public void TestFirstWithTwoMatches()
        {
            var result = (from c in _collection.AsQueryable<C>()
                         where c.Y == 11
                         select c).First();

            Assert.AreEqual(2, result.X);
            Assert.AreEqual(11, result.Y);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The GroupBy query operator is not supported.")]
        public void TestGroupByWithKeySelector()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c).GroupBy(c => c);
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The GroupBy query operator is not supported.")]
        public void TestGroupByWithKeySelectorAndElementSelector()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c).GroupBy(c => c, c => c);
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The GroupBy query operator is not supported.")]
        public void TestGroupByWithKeySelectorAndElementSelectorAndEqualityComparer()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c).GroupBy(c => c, c => c, new CEqualityComparer());
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The GroupBy query operator is not supported.")]
        public void TestGroupByWithKeySelectorAndElementSelectorAndResultSelector()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c).GroupBy(c => c, c => c, (c, e) => 1.0);
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The GroupBy query operator is not supported.")]
        public void TestGroupByWithKeySelectorAndElementSelectorAndResultSelectorAndEqualityComparer()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c).GroupBy(c => c, c => c, (c, e) => e.First(), new CEqualityComparer());
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The GroupBy query operator is not supported.")]
        public void TestGroupByWithKeySelectorAndEqualityComparer()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c).GroupBy(c => c, new CEqualityComparer());
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The GroupBy query operator is not supported.")]
        public void TestGroupByWithKeySelectorAndResultSelector()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c).GroupBy(c => c, (k, e) => 1.0);
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The GroupBy query operator is not supported.")]
        public void TestGroupByWithKeySelectorAndResultSelectorAndEqualityComparer()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c).GroupBy(c => c, (k, e) => e.First(), new CEqualityComparer());
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The GroupJoin query operator is not supported.")]
        public void TestGroupJoin()
        {
            var inner = new C[0];
            var query = _collection.AsQueryable<C>().GroupJoin(inner, c => c, c => c, (c, e) => c);
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The GroupJoin query operator is not supported.")]
        public void TestGroupJoinWithEqualityComparer()
        {
            var inner = new C[0];
            var query = _collection.AsQueryable<C>().GroupJoin(inner, c => c, c => c, (c, e) => c, new CEqualityComparer());
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Intersect query operator is not supported.")]
        public void TestIntersect()
        {
            var source2 = new C[0];
            var query = (from c in _collection.AsQueryable<C>()
                         select c).Intersect(source2);
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Intersect query operator is not supported.")]
        public void TestIntersectWithEqualityComparer()
        {
            var source2 = new C[0];
            var query = (from c in _collection.AsQueryable<C>()
                         select c).Intersect(source2, new CEqualityComparer());
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Join query operator is not supported.")]
        public void TestJoin()
        {
            var query = _collection.AsQueryable<C>().Join(_collection.AsQueryable<C>(), c => c.X, c => c.X, (x, y) => x);
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Join query operator is not supported.")]
        public void TestJoinWithEqualityComparer()
        {
            var query = _collection.AsQueryable<C>().Join(_collection.AsQueryable<C>(), c => c.X, c => c.X, (x, y) => x, new Int32EqualityComparer());
            query.ToList(); // execute query
        }

        [Test]
        public void TestLastOrDefaultWithManyMatches()
        {
            var result = (from c in _collection.AsQueryable<C>()
                         select c).LastOrDefault();

            Assert.AreEqual(4, result.X);
            Assert.AreEqual(44, result.Y);
        }

        [Test]
        public void TestLastOrDefaultWithNoMatch()
        {
            var result = (from c in _collection.AsQueryable<C>()
                         where c.X == 9
                         select c).LastOrDefault();
            Assert.IsNull(result);
        }

        [Test]
        public void TestLastOrDefaultWithOneMatch()
        {
            var result = (from c in _collection.AsQueryable<C>()
                         where c.X == 3
                         select c).LastOrDefault();

            Assert.AreEqual(3, result.X);
            Assert.AreEqual(33, result.Y);
        }

        [Test]
        public void TestLastOrDefaultWithOrderBy()
        {
            var result = (from c in _collection.AsQueryable<C>()
                        orderby c.X
                        select c).LastOrDefault();

            Assert.AreEqual(5, result.X);
            Assert.AreEqual(44, result.Y);
        }

        [Test]
        public void TestLastOrDefaultWithTwoMatches()
        {
            var result = (from c in _collection.AsQueryable<C>()
                         where c.Y == 11
                         select c).LastOrDefault();

            Assert.AreEqual(1, result.X);
            Assert.AreEqual(11, result.Y);
        }

        [Test]
        public void TestLastWithManyMatches()
        {
            var result = (from c in _collection.AsQueryable<C>()
                         select c).Last();

            Assert.AreEqual(4, result.X);
            Assert.AreEqual(44, result.Y);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Sequence contains no elements")]
        public void TestLastWithNoMatch()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          where c.X == 9
                          select c).Last();
        }

        [Test]
        public void TestLastWithOneMatch()
        {
            var result = (from c in _collection.AsQueryable<C>()
                         where c.X == 3
                         select c).Last();

            Assert.AreEqual(3, result.X);
            Assert.AreEqual(33, result.Y);
        }

        [Test]
        public void TestLastWithOrderBy()
        {
            var result = (from c in _collection.AsQueryable<C>()
                        orderby c.X
                        select c).Last();

            Assert.AreEqual(5, result.X);
            Assert.AreEqual(44, result.Y);
        }

        [Test]
        public void TestLastWithTwoMatches()
        {
            var result = (from c in _collection.AsQueryable<C>()
                         where c.Y == 11
                         select c).Last();

            Assert.AreEqual(1, result.X);
            Assert.AreEqual(11, result.Y);
        }

        [Test]
        public void TestLongCountAll()
        {
            var result = (from c in _collection.AsQueryable<C>()
                         select c).LongCount();

            Assert.AreEqual(5L, result);
        }

        [Test]
        public void TestLongCountTwo()
        {
            var result = (from c in _collection.AsQueryable<C>()
                         where c.Y == 11
                         select c).LongCount();

            Assert.AreEqual(2L, result);
        }

        [Test]
        public void TestLongCountWithSkipAndTake()
        {
            var result = (from c in _collection.AsQueryable<C>()
                         select c).Skip(2).Take(2).LongCount();

            Assert.AreEqual(2L, result);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Max query operator is not supported.")]
        public void TestMax()
        {
            var result = (from c in _collection.AsQueryable<C>()
                           select c).Max();
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Max query operator is not supported.")]
        public void TestMaxWithSelector()
        {
            var result = (from c in _collection.AsQueryable<C>()
                           select c).Max(c => 1.0);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Min query operator is not supported.")]
        public void TestMin()
        {
            var result = (from c in _collection.AsQueryable<C>()
                           select c).Min();
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Min query operator is not supported.")]
        public void TestMinWithSelector()
        {
            var result = (from c in _collection.AsQueryable<C>()
                           select c).Min(c => 1.0);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The OfType query operator is not supported.")]
        public void TestOfType()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c).OfType<C>();
            query.ToList(); // execute query
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
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Only one OrderBy or OrderByDescending clause is allowed (use ThenBy or ThenByDescending for multiple order by clauses).")]
        public void TestOrderByDuplicateNotAllowed()
        {
            var query = from c in _collection.AsQueryable<C>()
                        orderby c.X
                        orderby c.Y
                        select c;

            MongoQueryTranslator.Translate(_collection, query.Expression);
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
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Reverse query operator is not supported.")]
        public void TestReverse()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c).Reverse();
            query.ToList(); // execute query
        }

        [Test]
        public void TestSelect()
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
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The SelectMany query operator is not supported.")]
        public void TestSelectMany()
        {
            var query = _collection.AsQueryable<C>().SelectMany(c => new C[] { c });
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The SelectMany query operator is not supported.")]
        public void TestSelectManyWithIndex()
        {
            var query = _collection.AsQueryable<C>().SelectMany((c, index) => new C[] { c });
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The SelectMany query operator is not supported.")]
        public void TestSelectManyWithIntermediateResults()
        {
            var query = _collection.AsQueryable<C>().SelectMany(c => new C[] { c }, (c, i) => i);
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The SelectMany query operator is not supported.")]
        public void TestSelectManyWithIndexAndIntermediateResults()
        {
            var query = _collection.AsQueryable<C>().SelectMany((c, index) => new C[] { c }, (c, i) => i);
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The indexed version of the Select query operator is not supported.")]
        public void TestSelectWithIndex()
        {
            var query = _collection.AsQueryable<C>().Select((c, index) => c);
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The SequenceEqual query operator is not supported.")]
        public void TestSequenceEqual()
        {
            var source2 = new C[0];
            var result = (from c in _collection.AsQueryable<C>()
                         select c).SequenceEqual(source2);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The SequenceEqual query operator is not supported.")]
        public void TestSequenceEqualtWithEqualityComparer()
        {
            var source2 = new C[0];
            var result = (from c in _collection.AsQueryable<C>()
                         select c).SequenceEqual(source2, new CEqualityComparer());
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Sequence contains more than one element")]
        public void TestSingleOrDefaultWithManyMatches()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).SingleOrDefault();
        }

        [Test]
        public void TestSingleOrDefaultWithNoMatch()
        {
            var result = (from c in _collection.AsQueryable<C>()
                         where c.X == 9
                         select c).SingleOrDefault();
            Assert.IsNull(result);
        }

        [Test]
        public void TestSingleOrDefaultWithOneMatch()
        {
            var result = (from c in _collection.AsQueryable<C>()
                         where c.X == 3
                         select c).SingleOrDefault();

            Assert.AreEqual(3, result.X);
            Assert.AreEqual(33, result.Y);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Sequence contains more than one element")]
        public void TestSingleOrDefaultWithTwoMatches()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          where c.Y == 11
                          select c).SingleOrDefault();
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Sequence contains more than one element")]
        public void TestSingleWithManyMatches()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).Single();
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Sequence contains no elements")]
        public void TestSingleWithNoMatch()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          where c.X == 9
                          select c).Single();
        }

        [Test]
        public void TestSingleWithOneMatch()
        {
            var result = (from c in _collection.AsQueryable<C>()
                         where c.X == 3
                         select c).Single();

            Assert.AreEqual(3, result.X);
            Assert.AreEqual(33, result.Y);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Sequence contains more than one element")]
        public void TestSingleWithTwoMatches()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          where c.Y == 11
                          select c).Single();
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
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The SkipWhile query operator is not supported.")]
        public void TestSkipWhile()
        {
            var query = _collection.AsQueryable<C>().SkipWhile(c => true);
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Sum query operator is not supported.")]
        public void TestSum()
        {
            var result = (from c in _collection.AsQueryable<C>()
                           select 1.0).Sum();
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Sum query operator is not supported.")]
        public void TestSumNullable()
        {
            var result = (from c in _collection.AsQueryable<C>()
                           select (double?)1.0).Sum();
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Sum query operator is not supported.")]
        public void TestSumWithSelector()
        {
            var result = (from c in _collection.AsQueryable<C>()
                           select c).Sum(c => 1.0);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Sum query operator is not supported.")]
        public void TestSumeWithSelectorNullable()
        {
            var result = (from c in _collection.AsQueryable<C>()
                           select c).Sum(c => (double?)1.0);
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
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The TakeWhile query operator is not supported.")]
        public void TestTakeWhile()
        {
            var query = _collection.AsQueryable<C>().TakeWhile(c => true);
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "ThenBy or ThenByDescending can only be used after OrderBy or OrderByDescending.")]
        public void TestThenByWithMissingOrderBy()
        {
            // not sure this could ever happen in real life without deliberate sabotaging like with this cast
            var query = ((IOrderedQueryable<C>)_collection.AsQueryable<C>())
                .ThenBy(c => c.X);

            MongoQueryTranslator.Translate(_collection, query.Expression);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Union query operator is not supported.")]
        public void TestUnion()
        {
            var source2 = new C[0];
            var query = (from c in _collection.AsQueryable<C>()
                         select c).Union(source2);
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The Union query operator is not supported.")]
        public void TestUnionWithEqualityComparer()
        {
            var source2 = new C[0];
            var query = (from c in _collection.AsQueryable<C>()
                         select c).Union(source2, new CEqualityComparer());
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "The indexed version of the Where query operator is not supported.")]
        public void TestWhereWithIndex()
        {
            var query = _collection.AsQueryable<C>().Where((c, i) => true);
            query.ToList(); // execute query
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
