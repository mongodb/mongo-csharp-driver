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
using System.Text.RegularExpressions;
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
        public enum E
        {
            None,
            A
        }

        public class C
        {
            public ObjectId Id { get; set; }
            [BsonElement("x")]
            public int X { get; set; }
            [BsonElement("y")]
            public int Y { get; set; }
            [BsonElement("d")]
            public D D { get; set; }
            [BsonElement("s")]
            [BsonIgnoreIfNull]
            public string S { get; set; }
            [BsonElement("a")]
            [BsonIgnoreIfNull]
            public int[] A { get; set; }
            [BsonElement("b")]
            public bool B { get; set; }
            [BsonElement("l")]
            [BsonIgnoreIfNull]
            public List<int> L { get; set; }
            [BsonElement("dbref")]
            [BsonIgnoreIfNull]
            public MongoDBRef DBRef { get; set; }
            [BsonElement("e")]
            [BsonIgnoreIfDefault]
            [BsonRepresentation(BsonType.String)]
            public E E { get; set; }
        }

        public class D
        {
            [BsonElement("z")]
            public int Z; // use field instead of property to test fields also
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
        private MongoCollection<SystemProfileInfo> _systemProfileCollection;

        [TestFixtureSetUp]
        public void Setup()
        {
            _server = Configuration.TestServer;
            _server.Connect();
            _database = Configuration.TestDatabase;
            _collection = Configuration.GetTestCollection<C>();
            _systemProfileCollection = _database.GetCollection<SystemProfileInfo>("system.profile");

            // documents inserted deliberately out of order to test sorting
            _collection.Drop();
            _collection.Insert(new C { X = 2, Y = 11, D = new D { Z = 22 }, A = new [] { 2, 3, 4 }, L = new List<int> { 2, 3, 4 } });
            _collection.Insert(new C { X = 1, Y = 11, D = new D { Z = 11 }, S = "x is 1" });
            _collection.Insert(new C { X = 3, Y = 33, D = new D { Z = 33 }, B = true, E = E.A });
            _collection.Insert(new C { X = 5, Y = 44, D = new D { Z = 55 }, DBRef = new MongoDBRef("db", "c", 1) });
            _collection.Insert(new C { X = 4, Y = 44, D = new D { Z = 44 } });
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
        public void TestAny()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).Any();
            Assert.IsTrue(result);
        }

        [Test]
        public void TestAnyWhereXEquals1()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          where c.X == 1
                          select c).Any();
            Assert.IsTrue(result);
        }

        [Test]
        public void TestAnyWhereXEquals9()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          where c.X == 9
                          select c).Any();
            Assert.IsFalse(result);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Any with predicate after a projection is not supported.")]
        public void TestAnyWithPredicateAfterProjection()
        {
            var result = _collection.AsQueryable<C>().Select(c => c.Y).Any(y => y == 11);
        }

        [Test]
        public void TestAnyWithPredicateAfterWhere()
        {
            var result = _collection.AsQueryable<C>().Where(c => c.X == 1).Any(c => c.Y == 11);
            Assert.IsTrue(result);
        }

        [Test]
        public void TestAnyWithPredicateFalse()
        {
            var result = _collection.AsQueryable<C>().Any(c => c.X == 9);
            Assert.IsFalse(result);
        }

        [Test]
        public void TestAnyWithPredicateTrue()
        {
            var result = _collection.AsQueryable<C>().Any(c => c.X == 1);
            Assert.IsTrue(result);
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
        public void TestCountEquals2()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          where c.Y == 11
                          select c).Count();

            Assert.AreEqual(2, result);
        }

        [Test]
        public void TestCountEquals5()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).Count();

            Assert.AreEqual(5, result);
        }

        [Test]
        public void TestCountWithPredicate()
        {
            var result = _collection.AsQueryable<C>().Count(c => c.Y == 11);

            Assert.AreEqual(2, result);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Count with predicate after a projection is not supported.")]
        public void TestCountWithPredicateAfterProjection()
        {
            var result = _collection.AsQueryable<C>().Select(c => c.Y).Count(y => y == 11);
        }

        [Test]
        public void TestCountWithPredicateAfterWhere()
        {
            var result = _collection.AsQueryable<C>().Where(c => c.X == 1).Count(c => c.Y == 11);

            Assert.AreEqual(1, result);
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
        [ExpectedException(typeof(InvalidOperationException))]
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
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "FirstOrDefault with predicate after a projection is not supported.")]
        public void TestFirstOrDefaultWithPredicateAfterProjection()
        {
            var result = _collection.AsQueryable<C>().Select(c => c.Y).FirstOrDefault(y => y == 11);
        }

        [Test]
        public void TestFirstOrDefaultWithPredicateAfterWhere()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          where c.X == 1
                          select c).FirstOrDefault(c => c.Y == 11);
            Assert.AreEqual(1, result.X);
            Assert.AreEqual(11, result.Y);
        }

        [Test]
        public void TestFirstOrDefaultWithPredicateNoMatch()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).FirstOrDefault(c => c.X == 9);
            Assert.IsNull(result);
        }

        [Test]
        public void TestFirstOrDefaultWithPredicateOneMatch()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).FirstOrDefault(c => c.X == 3);
            Assert.AreEqual(3, result.X);
            Assert.AreEqual(33, result.Y);
        }

        [Test]
        public void TestFirstOrDefaultWithPredicateTwoMatches()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).FirstOrDefault(c => c.Y == 11);
            Assert.AreEqual(2, result.X);
            Assert.AreEqual(11, result.Y);
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
        [ExpectedException(typeof(InvalidOperationException))]
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
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "First with predicate after a projection is not supported.")]
        public void TestFirstWithPredicateAfterProjection()
        {
            var result = _collection.AsQueryable<C>().Select(c => c.Y).First(y => y == 11);
        }

        [Test]
        public void TestFirstWithPredicateAfterWhere()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          where c.X == 1
                          select c).First(c => c.Y == 11);
            Assert.AreEqual(1, result.X);
            Assert.AreEqual(11, result.Y);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Sequence contains no elements")]
        public void TestFirstWithPredicateNoMatch()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).First(c => c.X == 9);
        }

        [Test]
        public void TestFirstWithPredicateOneMatch()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).First(c => c.X == 3);
            Assert.AreEqual(3, result.X);
            Assert.AreEqual(33, result.Y);
        }

        [Test]
        public void TestFirstWithPredicateTwoMatches()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).First(c => c.Y == 11);
            Assert.AreEqual(2, result.X);
            Assert.AreEqual(11, result.Y);
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
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "LastOrDefault with predicate after a projection is not supported.")]
        public void TestLastOrDefaultWithPredicateAfterProjection()
        {
            var result = _collection.AsQueryable<C>().Select(c => c.Y).LastOrDefault(y => y == 11);
        }

        [Test]
        public void TestLastOrDefaultWithPredicateAfterWhere()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          where c.X == 1
                          select c).LastOrDefault(c => c.Y == 11);
            Assert.AreEqual(1, result.X);
            Assert.AreEqual(11, result.Y);
        }

        [Test]
        public void TestLastOrDefaultWithPredicateNoMatch()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).LastOrDefault(c => c.X == 9);
            Assert.IsNull(result);
        }

        [Test]
        public void TestLastOrDefaultWithPredicateOneMatch()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).LastOrDefault(c => c.X == 3);
            Assert.AreEqual(3, result.X);
            Assert.AreEqual(33, result.Y);
        }

        [Test]
        public void TestLastOrDefaultWithPredicateTwoMatches()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).LastOrDefault(c => c.Y == 11);
            Assert.AreEqual(1, result.X);
            Assert.AreEqual(11, result.Y);
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
        [ExpectedException(typeof(InvalidOperationException))]
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
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Last with predicate after a projection is not supported.")]
        public void TestLastWithPredicateAfterProjection()
        {
            var result = _collection.AsQueryable<C>().Select(c => c.Y).Last(y => y == 11);
        }

        [Test]
        public void TestLastWithPredicateAfterWhere()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          where c.X == 1
                          select c).Last(c => c.Y == 11);
            Assert.AreEqual(1, result.X);
            Assert.AreEqual(11, result.Y);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Sequence contains no elements")]
        public void TestLastWithPredicateNoMatch()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).Last(c => c.X == 9);
        }

        [Test]
        public void TestLastWithPredicateOneMatch()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).Last(c => c.X == 3);
            Assert.AreEqual(3, result.X);
            Assert.AreEqual(33, result.Y);
        }

        [Test]
        public void TestLastWithPredicateTwoMatches()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).Last(c => c.Y == 11);
            Assert.AreEqual(1, result.X);
            Assert.AreEqual(11, result.Y);
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
        public void TestLongCountEquals2()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          where c.Y == 11
                          select c).LongCount();

            Assert.AreEqual(2L, result);
        }

        [Test]
        public void TestLongCountEquals5()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).LongCount();

            Assert.AreEqual(5L, result);
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

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.IsNull(selectQuery.Where);
            Assert.AreEqual(1, selectQuery.OrderBy.Count);
            Assert.AreEqual("(C c) => c.X", ExpressionFormatter.ToString(selectQuery.OrderBy[0].Key));
            Assert.AreEqual(OrderByDirection.Ascending, selectQuery.OrderBy[0].Direction);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.IsNull(selectQuery.BuildQuery());
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

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.IsNull(selectQuery.Where);
            Assert.AreEqual(2, selectQuery.OrderBy.Count);
            Assert.AreEqual("(C c) => c.Y", ExpressionFormatter.ToString(selectQuery.OrderBy[0].Key));
            Assert.AreEqual(OrderByDirection.Ascending, selectQuery.OrderBy[0].Direction);
            Assert.AreEqual("(C c) => c.X", ExpressionFormatter.ToString(selectQuery.OrderBy[1].Key));
            Assert.AreEqual(OrderByDirection.Ascending, selectQuery.OrderBy[1].Direction);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.IsNull(selectQuery.BuildQuery());
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

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.IsNull(selectQuery.Where);
            Assert.AreEqual(2, selectQuery.OrderBy.Count);
            Assert.AreEqual("(C c) => c.Y", ExpressionFormatter.ToString(selectQuery.OrderBy[0].Key));
            Assert.AreEqual(OrderByDirection.Ascending, selectQuery.OrderBy[0].Direction);
            Assert.AreEqual("(C c) => c.X", ExpressionFormatter.ToString(selectQuery.OrderBy[1].Key));
            Assert.AreEqual(OrderByDirection.Descending, selectQuery.OrderBy[1].Direction);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.IsNull(selectQuery.BuildQuery());
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

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.IsNull(selectQuery.Where);
            Assert.AreEqual(1, selectQuery.OrderBy.Count);
            Assert.AreEqual("(C c) => c.X", ExpressionFormatter.ToString(selectQuery.OrderBy[0].Key));
            Assert.AreEqual(OrderByDirection.Descending, selectQuery.OrderBy[0].Direction);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.IsNull(selectQuery.BuildQuery());
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

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.IsNull(selectQuery.Where);
            Assert.AreEqual(2, selectQuery.OrderBy.Count);
            Assert.AreEqual("(C c) => c.Y", ExpressionFormatter.ToString(selectQuery.OrderBy[0].Key));
            Assert.AreEqual(OrderByDirection.Descending, selectQuery.OrderBy[0].Direction);
            Assert.AreEqual("(C c) => c.X", ExpressionFormatter.ToString(selectQuery.OrderBy[1].Key));
            Assert.AreEqual(OrderByDirection.Ascending, selectQuery.OrderBy[1].Direction);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.IsNull(selectQuery.BuildQuery());
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

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.IsNull(selectQuery.Where);
            Assert.AreEqual(2, selectQuery.OrderBy.Count);
            Assert.AreEqual("(C c) => c.Y", ExpressionFormatter.ToString(selectQuery.OrderBy[0].Key));
            Assert.AreEqual(OrderByDirection.Descending, selectQuery.OrderBy[0].Direction);
            Assert.AreEqual("(C c) => c.X", ExpressionFormatter.ToString(selectQuery.OrderBy[1].Key));
            Assert.AreEqual(OrderByDirection.Descending, selectQuery.OrderBy[1].Direction);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.IsNull(selectQuery.BuildQuery());
            var results = query.ToList();
            Assert.AreEqual(5, results.Count);
            Assert.AreEqual(5, results.First().X);
            Assert.AreEqual(1, results.Last().X);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Only one OrderBy or OrderByDescending clause is allowed (use ThenBy or ThenByDescending for multiple order by clauses).")]
        public void TestOrderByDuplicate()
        {
            var query = from c in _collection.AsQueryable<C>()
                        orderby c.X
                        orderby c.Y
                        select c;

            MongoQueryTranslator.Translate(query);
        }

        [Test]
        public void TestProjection()
        {
            var query = from c in _collection.AsQueryable<C>()
                        select c.X;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.IsNull(selectQuery.Where);
            Assert.IsNull(selectQuery.OrderBy);
            Assert.AreEqual("(C c) => c.X", ExpressionFormatter.ToString(selectQuery.Projection));
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.IsNull(selectQuery.BuildQuery());

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

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.IsNull(selectQuery.Where);
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.IsNull(selectQuery.BuildQuery());
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
        [ExpectedException(typeof(InvalidOperationException))]
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
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "SingleOrDefault with predicate after a projection is not supported.")]
        public void TestSingleOrDefaultWithPredicateAfterProjection()
        {
            var result = _collection.AsQueryable<C>().Select(c => c.Y).SingleOrDefault(y => y == 11);
        }

        [Test]
        public void TestSingleOrDefaultWithPredicateAfterWhere()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          where c.X == 1
                          select c).SingleOrDefault(c => c.Y == 11);
            Assert.AreEqual(1, result.X);
            Assert.AreEqual(11, result.Y);
        }

        [Test]
        public void TestSingleOrDefaultWithPredicateNoMatch()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).SingleOrDefault(c => c.X == 9);
            Assert.IsNull(result);
        }

        [Test]
        public void TestSingleOrDefaultWithPredicateOneMatch()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).SingleOrDefault(c => c.X == 3);
            Assert.AreEqual(3, result.X);
            Assert.AreEqual(33, result.Y);
        }

        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Sequence contains more than one element")]
        [Test]
        public void TestSingleOrDefaultWithPredicateTwoMatches()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).SingleOrDefault(c => c.Y == 11);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestSingleOrDefaultWithTwoMatches()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          where c.Y == 11
                          select c).SingleOrDefault();
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestSingleWithManyMatches()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).Single();
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
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
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Single with predicate after a projection is not supported.")]
        public void TestSingleWithPredicateAfterProjection()
        {
            var result = _collection.AsQueryable<C>().Select(c => c.Y).Single(y => y == 11);
        }

        [Test]
        public void TestSingleWithPredicateAfterWhere()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          where c.X == 1
                          select c).Single(c => c.Y == 11);
            Assert.AreEqual(1, result.X);
            Assert.AreEqual(11, result.Y);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Sequence contains no elements")]
        public void TestSingleWithPredicateNoMatch()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).Single(c => c.X == 9);
            Assert.IsNull(result);
        }

        [Test]
        public void TestSingleWithPredicateOneMatch()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).Single(c => c.X == 3);
            Assert.AreEqual(3, result.X);
            Assert.AreEqual(33, result.Y);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Sequence contains more than one element")]
        public void TestSingleWithPredicateTwoMatches()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).Single(c => c.Y == 11);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
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

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.IsNull(selectQuery.Where);
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.AreEqual("2", ExpressionFormatter.ToString(selectQuery.Skip));
            Assert.IsNull(selectQuery.Take);

            Assert.IsNull(selectQuery.BuildQuery());
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
        public void TestSumWithSelectorNullable()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).Sum(c => (double?)1.0);
        }

        [Test]
        public void TestTake2()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c).Take(2);

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.IsNull(selectQuery.Where);
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.AreEqual("2", ExpressionFormatter.ToString(selectQuery.Take));

            Assert.IsNull(selectQuery.BuildQuery());
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

            MongoQueryTranslator.Translate(query);
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
        public void TestWhereAContains2()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.A.Contains(2)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => Enumerable.Contains<Int32>(c.A, 2)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"a\" : 2 }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereAContains2Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !c.A.Contains(2)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !Enumerable.Contains<Int32>(c.A, 2)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"a\" : { \"$ne\" : 2 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereAContainsAll()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.A.ContainsAll(new[] { 2, 3 })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => LinqToMongo.ContainsAll<Int32>(c.A, Int32[]:{ 2, 3 })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"a\" : { \"$all\" : [2, 3] } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereAContainsAllNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !c.A.ContainsAll(new[] { 2, 3 })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !LinqToMongo.ContainsAll<Int32>(c.A, Int32[]:{ 2, 3 })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"a\" : { \"$not\" : { \"$all\" : [2, 3] } } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereAContainsAny()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.A.ContainsAny(new[] { 2, 3 })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => LinqToMongo.ContainsAny<Int32>(c.A, Int32[]:{ 2, 3 })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"a\" : { \"$in\" : [2, 3] } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereAContainsAnyNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !c.A.ContainsAny(new[] { 1, 2 })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !LinqToMongo.ContainsAny<Int32>(c.A, Int32[]:{ 1, 2 })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"a\" : { \"$nin\" : [1, 2] } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereAExistsFalse()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where Query.Exists("a", false).Inject()
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => LinqToMongo.Inject({ \"a\" : { \"$exists\" : false } })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"a\" : { \"$exists\" : false } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereAExistsTrue()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where Query.Exists("a", true).Inject()
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => LinqToMongo.Inject({ \"a\" : { \"$exists\" : true } })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"a\" : { \"$exists\" : true } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereAExistsTrueNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !Query.Exists("a", true).Inject()
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !LinqToMongo.Inject({ \"a\" : { \"$exists\" : true } })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"a\" : { \"$exists\" : false } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereALengthEquals3()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.A.Length == 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.A.Length == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"a\" : { \"$size\" : 3 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereALengthEquals3Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.A.Length == 3)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !(c.A.Length == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"a\" : { \"$not\" : { \"$size\" : 3 } } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereALengthNotEquals3()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.A.Length != 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.A.Length != 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"a\" : { \"$not\" : { \"$size\" : 3 } } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereALengthNotEquals3Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.A.Length != 3)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !(c.A.Length != 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"a\" : { \"$size\" : 3 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereASub1Equals3()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.A[1] == 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.A[1] == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            //Assert.AreEqual("{ \"a.1\" : 3 }", selectQuery.BuildQuery().ToJson());
            //Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereASub1Equals3Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.A[1] == 3)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !(c.A[1] == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            //Assert.AreEqual("{ \"a.1\" : { \"$ne\" : 3 } }", selectQuery.BuildQuery().ToJson());
            //Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereASub1NotEquals3()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.A[1] != 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.A[1] != 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            //Assert.AreEqual("{ \"a.1\" : { \"$ne\" : 3 } }", selectQuery.BuildQuery().ToJson());
            //Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereASub1NotEquals3Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.A[1] != 3)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !(c.A[1] != 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            //Assert.AreEqual("{ \"a.1\" : 3 }", selectQuery.BuildQuery().ToJson());
            //Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereB()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.B
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => c.B", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"b\" : true }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereBNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !c.B
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !c.B", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"b\" : false }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereDBRefCollectionNameEqualsC()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.DBRef.CollectionName == "c"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.DBRef.CollectionName == \"c\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"dbref.$ref\" : \"c\" }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereDBRefDatabaseNameEqualsDb()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.DBRef.DatabaseName == "db"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.DBRef.DatabaseName == \"db\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"dbref.$db\" : \"db\" }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereDBRefIdEquals1()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.DBRef.Id == 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.DBRef.Id == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"dbref.$id\" : 1 }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereEEqualsA()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.E == E.A
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => ((Int32)c.E == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"e\" : \"A\" }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereEEqualsANot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.E == E.A)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !((Int32)c.E == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"e\" : { \"$ne\" : \"A\" } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereENotEqualsA()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.E != E.A
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => ((Int32)c.E != 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"e\" : { \"$ne\" : \"A\" } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereENotEqualsANot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.E != E.A)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !((Int32)c.E != 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"e\" : \"A\" }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereLContains2()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.L.Contains(2)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => c.L.Contains(2)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"l\" : 2 }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereLContains2Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !c.L.Contains(2)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !c.L.Contains(2)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"l\" : { \"$ne\" : 2 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereLContainsAll()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.L.ContainsAll(new[] { 2, 3 })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => LinqToMongo.ContainsAll<Int32>(c.L, Int32[]:{ 2, 3 })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"l\" : { \"$all\" : [2, 3] } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereLContainsAllNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !c.L.ContainsAll(new[] { 2, 3 })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !LinqToMongo.ContainsAll<Int32>(c.L, Int32[]:{ 2, 3 })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"l\" : { \"$not\" : { \"$all\" : [2, 3] } } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereLContainsAny()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.L.ContainsAny(new[] { 2, 3 })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => LinqToMongo.ContainsAny<Int32>(c.L, Int32[]:{ 2, 3 })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"l\" : { \"$in\" : [2, 3] } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereLContainsAnyNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !c.L.ContainsAny(new[] { 1, 2 })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !LinqToMongo.ContainsAny<Int32>(c.L, Int32[]:{ 1, 2 })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"l\" : { \"$nin\" : [1, 2] } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereLExistsFalse()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where Query.Exists("l", false).Inject()
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => LinqToMongo.Inject({ \"l\" : { \"$exists\" : false } })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"l\" : { \"$exists\" : false } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereLExistsTrue()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where Query.Exists("l", true).Inject()
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => LinqToMongo.Inject({ \"l\" : { \"$exists\" : true } })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"l\" : { \"$exists\" : true } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereLExistsTrueNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !Query.Exists("l", true).Inject()
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !LinqToMongo.Inject({ \"l\" : { \"$exists\" : true } })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"l\" : { \"$exists\" : false } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereLLengthEquals3()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.L.Count == 3 // use Count as a property in this test
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.L.Count == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"l\" : { \"$size\" : 3 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereLLengthEquals3Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.L.Count() == 3) // use Count as a method in this test
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !(Enumerable.Count<Int32>(c.L) == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"l\" : { \"$not\" : { \"$size\" : 3 } } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereLLengthNotEquals3()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.L.Count != 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.L.Count != 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"l\" : { \"$not\" : { \"$size\" : 3 } } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereLLengthNotEquals3Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.L.Count != 3)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !(c.L.Count != 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"l\" : { \"$size\" : 3 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereBEqualsFalse()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.B == false
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.B == false)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"b\" : false }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereBEqualsFalseNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.B == false)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !(c.B == false)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"b\" : true }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereBEqualsTrue()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.B == true
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.B == true)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"b\" : true }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereBEqualsTrueNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.B == true)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !(c.B == true)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"b\" : false }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereSContainsX()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.Contains("x")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => c.S.Contains(\"x\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /x/ }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSContainsXNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !c.S.Contains("x")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !c.S.Contains(\"x\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : { \"$not\" : /x/ } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereSEndsWith1()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.EndsWith("1")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => c.S.EndsWith(\"1\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /1$/ }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSEndsWith1Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !c.S.EndsWith("1")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !c.S.EndsWith(\"1\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : { \"$not\" : /1$/ } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereSIsMatch()
        {
            var regex = new Regex(@"^x");
            var query = from c in _collection.AsQueryable<C>()
                        where regex.IsMatch(c.S)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => Regex:(@\"^x\").IsMatch(c.S)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^x/ }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSIsMatchNot()
        {
            var regex = new Regex(@"^x");
            var query = from c in _collection.AsQueryable<C>()
                        where !regex.IsMatch(c.S)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !Regex:(@\"^x\").IsMatch(c.S)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : { \"$not\" : /^x/ } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereSIsMatchStatic()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where Regex.IsMatch(c.S, "^x")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => Regex.IsMatch(c.S, \"^x\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^x/ }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSIsMatchStaticNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !Regex.IsMatch(c.S, "^x")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !Regex.IsMatch(c.S, \"^x\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : { \"$not\" : /^x/ } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereSIsMatchStaticWithOptions()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where Regex.IsMatch(c.S, "^x", RegexOptions.IgnoreCase)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => Regex.IsMatch(c.S, \"^x\", RegexOptions.IgnoreCase)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^x/i }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSStartsWithX()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.StartsWith("x")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => c.S.StartsWith(\"x\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^x/ }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSStartsWithXNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !c.S.StartsWith("x")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !c.S.StartsWith(\"x\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : { \"$not\" : /^x/ } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereSystemProfileInfoDurationGreatherThan10Seconds()
        {
            var query = from pi in _systemProfileCollection.AsQueryable<SystemProfileInfo>()
                        where pi.Duration > TimeSpan.FromSeconds(10)
                        select pi;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_systemProfileCollection, translatedQuery.Collection);
            Assert.AreSame(typeof(SystemProfileInfo), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(SystemProfileInfo pi) => (pi.Duration > TimeSpan:(00:00:10))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"millis\" : { \"$gt\" : 10000.0 } }", selectQuery.BuildQuery().ToJson());
        }

        [Test]
        public void TestWhereSystemProfileInfoNamespaceEqualsNs()
        {
            var query = from pi in _systemProfileCollection.AsQueryable<SystemProfileInfo>()
                        where pi.Namespace == "ns"
                        select pi;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_systemProfileCollection, translatedQuery.Collection);
            Assert.AreSame(typeof(SystemProfileInfo), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(SystemProfileInfo pi) => (pi.Namespace == \"ns\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"ns\" : \"ns\" }", selectQuery.BuildQuery().ToJson());
        }

        [Test]
        public void TestWhereSystemProfileInfoNumberScannedGreaterThan1000()
        {
            var query = from pi in _systemProfileCollection.AsQueryable<SystemProfileInfo>()
                        where pi.NumberScanned > 1000
                        select pi;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_systemProfileCollection, translatedQuery.Collection);
            Assert.AreSame(typeof(SystemProfileInfo), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(SystemProfileInfo pi) => (pi.NumberScanned > 1000)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"nscanned\" : { \"$gt\" : 1000 } }", selectQuery.BuildQuery().ToJson());
        }

        [Test]
        public void TestWhereSystemProfileInfoTimeStampGreatherThanJan12012()
        {
            var query = from pi in _systemProfileCollection.AsQueryable<SystemProfileInfo>()
                        where pi.Timestamp > new DateTime(2012, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                        select pi;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_systemProfileCollection, translatedQuery.Collection);
            Assert.AreSame(typeof(SystemProfileInfo), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(SystemProfileInfo pi) => (pi.Timestamp > DateTime:(2012-01-01T00:00:00Z))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"ts\" : { \"$gt\" : ISODate(\"2012-01-01T00:00:00Z\") } }", selectQuery.BuildQuery().ToJson());
        }

        [Test]
        public void TestWhereTripleAnd()
        {
            // the query is a bit odd in order to force the built query to be promoted to $and form
            var query = from c in _collection.AsQueryable<C>()
                        where c.X >= 0 && c.X >= 1 && c.Y == 11
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (((c.X >= 0) && (c.X >= 1)) && (c.Y == 11))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"$and\" : [{ \"x\" : { \"$gte\" : 0 } }, { \"x\" : { \"$gte\" : 1 } }, { \"y\" : 11 }] }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(2, Consume(query));
        }

        [Test]
        public void TestWhereTripleOr()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.X == 1 || c.Y == 33 || c.S == "x is 1"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (((c.X == 1) || (c.Y == 33)) || (c.S == \"x is 1\"))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"$or\" : [{ \"x\" : 1 }, { \"y\" : 33 }, { \"s\" : \"x is 1\" }] }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(2, Consume(query));
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

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.X == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"x\" : 1 }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereXEquals1AndYEquals11()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.X == 1 && c.Y == 11
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => ((c.X == 1) && (c.Y == 11))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"x\" : 1, \"y\" : 11 }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereXEquals1AndYEquals11UsingTwoWhereClauses()
        {
            // note: using different variable names in the two where clauses to test parameter replacement when combining predicates
            var query = _collection.AsQueryable<C>().Where(c => c.X == 1).Where(d => d.Y == 11);

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => ((c.X == 1) && (c.Y == 11))", ExpressionFormatter.ToString(selectQuery.Where)); // note parameter replacement from c to d in second clause
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"x\" : 1, \"y\" : 11 }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereXEquals1AndYEquals11Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.X == 1 && c.Y == 11)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !((c.X == 1) && (c.Y == 11))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"$nor\" : [{ \"x\" : 1, \"y\" : 11 }] }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereXEquals1AndYEquals11AndZEquals11()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.X == 1 && c.Y == 11 && c.D.Z == 11
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (((c.X == 1) && (c.Y == 11)) && (c.D.Z == 11))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"x\" : 1, \"y\" : 11, \"d.z\" : 11 }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereXEquals1Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.X == 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !(c.X == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"x\" : { \"$ne\" : 1 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereXEquals1OrYEquals33()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.X == 1 || c.Y == 33
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => ((c.X == 1) || (c.Y == 33))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"$or\" : [{ \"x\" : 1 }, { \"y\" : 33 }] }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(2, Consume(query));
        }

        [Test]
        public void TestWhereXEquals1OrYEquals33Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.X == 1 || c.Y == 33)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !((c.X == 1) || (c.Y == 33))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"$nor\" : [{ \"x\" : 1 }, { \"y\" : 33 }] }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(3, Consume(query));
        }

        [Test]
        public void TestWhereXEquals1UsingJavaScript()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.X == 1 && Query.Where("this.x < 9").Inject()
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => ((c.X == 1) && LinqToMongo.Inject({ \"$where\" : { \"$code\" : \"this.x < 9\" } }))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"x\" : 1, \"$where\" : { \"$code\" : \"this.x < 9\" } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereXGreaterThan1()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.X > 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.X > 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"x\" : { \"$gt\" : 1 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereXGreaterThan1Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.X > 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !(c.X > 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"x\" : { \"$lte\" : 1 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereXGreaterThanOrEquals1()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.X >= 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.X >= 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"x\" : { \"$gte\" : 1 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(5, Consume(query));
        }

        [Test]
        public void TestWhereXGreaterThanOrEquals1Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.X >= 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !(c.X >= 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"x\" : { \"$lt\" : 1 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(0, Consume(query));
        }

        [Test]
        public void TestWhereXIn1Or9()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.X.In(new [] { 1, 9 })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => LinqToMongo.In<Int32>(c.X, Int32[]:{ 1, 9 })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"x\" : { \"$in\" : [1, 9] } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereXIn1Or9Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !c.X.In(new[] { 1, 9 })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !LinqToMongo.In<Int32>(c.X, Int32[]:{ 1, 9 })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"x\" : { \"$nin\" : [1, 9] } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereXIsTypeInt32()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where Query.Type("x", BsonType.Int32).Inject()
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => LinqToMongo.Inject({ \"x\" : { \"$type\" : 16 } })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"x\" : { \"$type\" : 16 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(5, Consume(query));
        }

        [Test]
        public void TestWhereXIsTypeInt32Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !Query.Type("x", BsonType.Int32).Inject()
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !LinqToMongo.Inject({ \"x\" : { \"$type\" : 16 } })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"x\" : { \"$not\" : { \"$type\" : 16 } } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(0, Consume(query));
        }

        [Test]
        public void TestWhereXLessThan1()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.X < 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.X < 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"x\" : { \"$lt\" : 1 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(0, Consume(query));
        }

        [Test]
        public void TestWhereXLessThan1Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.X < 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !(c.X < 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"x\" : { \"$gte\" : 1 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(5, Consume(query));
        }

        [Test]
        public void TestWhereXLessThanOrEquals1()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.X <= 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.X <= 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"x\" : { \"$lte\" : 1 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereXLessThanOrEquals1Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.X <= 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !(c.X <= 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"x\" : { \"$gt\" : 1 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereXModTwoEquals1()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.X % 2 == 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => ((c.X % 2) == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"x\" : { \"$mod\" : [2, 1] } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(3, Consume(query));
        }

        [Test]
        public void TestWhereXModTwoEquals1Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.X % 2 == 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !((c.X % 2) == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"x\" : { \"$not\" : { \"$mod\" : [2, 1] } } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(2, Consume(query));
        }

        [Test]
        public void TestWhereXModTwoNotEquals1()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.X % 2 != 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => ((c.X % 2) != 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"x\" : { \"$not\" : { \"$mod\" : [2, 1] } } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(2, Consume(query));
        }

        [Test]
        public void TestWhereXModTwoNotEquals1Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.X % 2 != 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !((c.X % 2) != 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"x\" : { \"$mod\" : [2, 1] } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(3, Consume(query));
        }

        [Test]
        public void TestWhereXNotEquals1()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.X != 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.X != 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"x\" : { \"$ne\" : 1 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereXNotEquals1Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.X != 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !(c.X != 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"x\" : 1 }", selectQuery.BuildQuery().ToJson());
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
