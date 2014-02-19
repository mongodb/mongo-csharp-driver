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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Linq
{
    [TestFixture]
    public class SelectQueryTests
    {
        public enum E
        {
            None,
            A,
            B,
            C
        }

        public class C
        {
            public ObjectId Id { get; set; }
            [BsonElement("x")]
            public int X { get; set; }
            [BsonElement("lx")]
            public long LX { get; set; }
            [BsonElement("y")]
            public int Y { get; set; }
            [BsonElement("d")]
            public D D { get; set; }
            [BsonElement("da")]
            public List<D> DA { get; set; }
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
            [BsonElement("ea")]
            [BsonIgnoreIfNull]
            public E[] EA { get; set; }
            [BsonElement("sa")]
            [BsonIgnoreIfNull]
            public string[] SA { get; set; }
            [BsonElement("ba")]
            [BsonIgnoreIfNull]
            public bool[] BA { get; set; }
        }

        public class D
        {
            [BsonElement("z")]
            public int Z; // use field instead of property to test fields also

            public override bool Equals(object obj)
            {
                if (obj == null || GetType() != obj.GetType()) { return false; }
                return Z == ((D)obj).Z;
            }

            public override int GetHashCode()
            {
                return Z.GetHashCode();
            }

            public override string ToString()
            {
                return string.Format("new D {{ Z = {0} }}", Z);
            }
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

        private ObjectId _id1 = ObjectId.GenerateNewId();
        private ObjectId _id2 = ObjectId.GenerateNewId();
        private ObjectId _id3 = ObjectId.GenerateNewId();
        private ObjectId _id4 = ObjectId.GenerateNewId();
        private ObjectId _id5 = ObjectId.GenerateNewId();

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
            _collection.Insert(new C { Id = _id2, X = 2, LX = 2, Y = 11, D = new D { Z = 22 }, A = new[] { 2, 3, 4 }, DA = new List<D> { new D { Z = 111 }, new D { Z = 222 } }, L = new List<int> { 2, 3, 4 } });
            _collection.Insert(new C { Id = _id1, X = 1, LX = 1, Y = 11, D = new D { Z = 11 }, S = "abc", SA = new string[] { "Tom", "Dick", "Harry" } });
            _collection.Insert(new C { Id = _id3, X = 3, LX = 3, Y = 33, D = new D { Z = 33 }, B = true, BA = new bool[] { true }, E = E.A, EA = new E[] { E.A, E.B } });
            _collection.Insert(new C { Id = _id5, X = 5, LX = 5, Y = 44, D = new D { Z = 55 }, DBRef = new MongoDBRef("db", "c", 1) });
            _collection.Insert(new C { Id = _id4, X = 4, LX = 4, Y = 44, D = new D { Z = 44 }, S = "   xyz   ", DA = new List<D> { new D { Z = 333 }, new D { Z = 444 } } });
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The Aggregate query operator is not supported.")]
        public void TestAggregate()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).Aggregate((a, b) => null);
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The Aggregate query operator is not supported.")]
        public void TestAggregateWithAccumulator()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).Aggregate<C, int>(0, (a, c) => 0);
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The Aggregate query operator is not supported.")]
        public void TestAggregateWithAccumulatorAndSelector()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).Aggregate<C, int, int>(0, (a, c) => 0, a => a);
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The All query operator is not supported.")]
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
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "Any with predicate after a projection is not supported.")]
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
        public void TestAsQueryableWithNothingElse()
        {
            var query = _collection.AsQueryable<C>();
            var result = query.ToList();
            Assert.AreEqual(5, result.Count);
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The Average query operator is not supported.")]
        public void TestAverage()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select 1.0).Average();
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The Average query operator is not supported.")]
        public void TestAverageNullable()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select (double?)1.0).Average();
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The Average query operator is not supported.")]
        public void TestAverageWithSelector()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).Average(c => 1.0);
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The Average query operator is not supported.")]
        public void TestAverageWithSelectorNullable()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).Average(c => (double?)1.0);
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The Cast query operator is not supported.")]
        public void TestCast()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c).Cast<C>();
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The Concat query operator is not supported.")]
        public void TestConcat()
        {
            var source2 = new C[0];
            var query = (from c in _collection.AsQueryable<C>()
                         select c).Concat(source2);
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The Contains query operator is not supported.")]
        public void TestContains()
        {
            var item = new C();
            var result = (from c in _collection.AsQueryable<C>()
                          select c).Contains(item);
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The Contains query operator is not supported.")]
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
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "Count with predicate after a projection is not supported.")]
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
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The DefaultIfEmpty query operator is not supported.")]
        public void TestDefaultIfEmpty()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c).DefaultIfEmpty();
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The DefaultIfEmpty query operator is not supported.")]
        public void TestDefaultIfEmptyWithDefaultValue()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c).DefaultIfEmpty(null);
            query.ToList(); // execute query
        }

        [Test]
        public void TestDistinctASub0()
        {
            if (_server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var query = (from c in _collection.AsQueryable<C>()
                             select c.A[0]).Distinct();
                var results = query.ToList();
                Assert.AreEqual(1, results.Count);
                Assert.IsTrue(results.Contains(2));
            }
        }

        [Test]
        public void TestDistinctB()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c.B).Distinct();
            var results = query.ToList();
            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results.Contains(false));
            Assert.IsTrue(results.Contains(true));
        }

        [Test]
        public void TestDistinctBASub0()
        {
            if (_server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var query = (from c in _collection.AsQueryable<C>()
                             select c.BA[0]).Distinct();
                var results = query.ToList();
                Assert.AreEqual(1, results.Count);
                Assert.IsTrue(results.Contains(true));
            }
        }

        [Test]
        public void TestDistinctD()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c.D).Distinct();
            var results = query.ToList(); // execute query
            Assert.AreEqual(5, results.Count);
            Assert.IsTrue(results.Contains(new D { Z = 11 }));
            Assert.IsTrue(results.Contains(new D { Z = 22 }));
            Assert.IsTrue(results.Contains(new D { Z = 33 }));
            Assert.IsTrue(results.Contains(new D { Z = 44 }));
            Assert.IsTrue(results.Contains(new D { Z = 55 }));
        }

        [Test]
        public void TestDistinctDBRef()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c.DBRef).Distinct();
            var results = query.ToList();
            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(results.Contains(new MongoDBRef("db", "c", 1)));
        }

        [Test]
        public void TestDistinctDBRefDatabase()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c.DBRef.DatabaseName).Distinct();
            var results = query.ToList();
            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(results.Contains("db"));
        }

        [Test]
        public void TestDistinctDZ()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c.D.Z).Distinct();
            var results = query.ToList();
            Assert.AreEqual(5, results.Count);
            Assert.IsTrue(results.Contains(11));
            Assert.IsTrue(results.Contains(22));
            Assert.IsTrue(results.Contains(33));
            Assert.IsTrue(results.Contains(44));
            Assert.IsTrue(results.Contains(55));
        }

        [Test]
        public void TestDistinctE()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c.E).Distinct();
            var results = query.ToList();
            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(results.Contains(E.A));
        }

        [Test]
        public void TestDistinctEASub0()
        {
            if (_server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var query = (from c in _collection.AsQueryable<C>()
                             select c.EA[0]).Distinct();
                var results = query.ToList();
                Assert.AreEqual(1, results.Count);
                Assert.IsTrue(results.Contains(E.A));
            }
        }

        [Test]
        public void TestDistinctId()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c.Id).Distinct();
            var results = query.ToList();
            Assert.AreEqual(5, results.Count);
            Assert.IsTrue(results.Contains(_id1));
            Assert.IsTrue(results.Contains(_id2));
            Assert.IsTrue(results.Contains(_id3));
            Assert.IsTrue(results.Contains(_id4));
            Assert.IsTrue(results.Contains(_id5));
        }

        [Test]
        public void TestDistinctLSub0()
        {
            if (_server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var query = (from c in _collection.AsQueryable<C>()
                             select c.L[0]).Distinct();
                var results = query.ToList();
                Assert.AreEqual(1, results.Count);
                Assert.IsTrue(results.Contains(2));
            }
        }

        [Test]
        public void TestDistinctS()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c.S).Distinct();
            var results = query.ToList();
            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results.Contains("abc"));
            Assert.IsTrue(results.Contains("   xyz   "));
        }

        [Test]
        public void TestDistinctSASub0()
        {
            if (_server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var query = (from c in _collection.AsQueryable<C>()
                             select c.SA[0]).Distinct();
                var results = query.ToList();
                Assert.AreEqual(1, results.Count);
                Assert.IsTrue(results.Contains("Tom"));
            }
        }

        [Test]
        public void TestDistinctX()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c.X).Distinct();
            var results = query.ToList();
            Assert.AreEqual(5, results.Count);
            Assert.IsTrue(results.Contains(1));
            Assert.IsTrue(results.Contains(2));
            Assert.IsTrue(results.Contains(3));
            Assert.IsTrue(results.Contains(4));
            Assert.IsTrue(results.Contains(5));
        }

        [Test]
        public void TestDistinctXWithQuery()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         where c.X >3
                         select c.X).Distinct();
            var results = query.ToList();
            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results.Contains(4));
            Assert.IsTrue(results.Contains(5));
        }

        [Test]
        public void TestDistinctY()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c.Y).Distinct();
            var results = query.ToList();
            Assert.AreEqual(3, results.Count);
            Assert.IsTrue(results.Contains(11));
            Assert.IsTrue(results.Contains(33));
            Assert.IsTrue(results.Contains(44));
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The version of the Distinct query operator with an equality comparer is not supported.")]
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
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The Except query operator is not supported.")]
        public void TestExcept()
        {
            var source2 = new C[0];
            var query = (from c in _collection.AsQueryable<C>()
                         select c).Except(source2);
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The Except query operator is not supported.")]
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
        public void TestFirstOrDefaultWithNoMatchAndProjectionToStruct()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          where c.X == 9
                          select c.X).FirstOrDefault();
            Assert.AreEqual(0, result);
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
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "FirstOrDefault with predicate after a projection is not supported.")]
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
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "First with predicate after a projection is not supported.")]
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
        public void TestFirstWithPredicateNoMatch()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                var result = (from c in _collection.AsQueryable<C>()
                              select c).First(c => c.X == 9);
            });
            Assert.AreEqual(ExpectedErrorMessage.FirstEmptySequence, ex.Message);
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
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The GroupBy query operator is not supported.")]
        public void TestGroupByWithKeySelector()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c).GroupBy(c => c);
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The GroupBy query operator is not supported.")]
        public void TestGroupByWithKeySelectorAndElementSelector()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c).GroupBy(c => c, c => c);
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The GroupBy query operator is not supported.")]
        public void TestGroupByWithKeySelectorAndElementSelectorAndEqualityComparer()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c).GroupBy(c => c, c => c, new CEqualityComparer());
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The GroupBy query operator is not supported.")]
        public void TestGroupByWithKeySelectorAndElementSelectorAndResultSelector()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c).GroupBy(c => c, c => c, (c, e) => 1.0);
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The GroupBy query operator is not supported.")]
        public void TestGroupByWithKeySelectorAndElementSelectorAndResultSelectorAndEqualityComparer()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c).GroupBy(c => c, c => c, (c, e) => e.First(), new CEqualityComparer());
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The GroupBy query operator is not supported.")]
        public void TestGroupByWithKeySelectorAndEqualityComparer()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c).GroupBy(c => c, new CEqualityComparer());
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The GroupBy query operator is not supported.")]
        public void TestGroupByWithKeySelectorAndResultSelector()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c).GroupBy(c => c, (k, e) => 1.0);
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The GroupBy query operator is not supported.")]
        public void TestGroupByWithKeySelectorAndResultSelectorAndEqualityComparer()
        {
            var query = (from c in _collection.AsQueryable<C>()
                         select c).GroupBy(c => c, (k, e) => e.First(), new CEqualityComparer());
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The GroupJoin query operator is not supported.")]
        public void TestGroupJoin()
        {
            var inner = new C[0];
            var query = _collection.AsQueryable<C>().GroupJoin(inner, c => c, c => c, (c, e) => c);
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The GroupJoin query operator is not supported.")]
        public void TestGroupJoinWithEqualityComparer()
        {
            var inner = new C[0];
            var query = _collection.AsQueryable<C>().GroupJoin(inner, c => c, c => c, (c, e) => c, new CEqualityComparer());
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The Intersect query operator is not supported.")]
        public void TestIntersect()
        {
            var source2 = new C[0];
            var query = (from c in _collection.AsQueryable<C>()
                         select c).Intersect(source2);
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The Intersect query operator is not supported.")]
        public void TestIntersectWithEqualityComparer()
        {
            var source2 = new C[0];
            var query = (from c in _collection.AsQueryable<C>()
                         select c).Intersect(source2, new CEqualityComparer());
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The Join query operator is not supported.")]
        public void TestJoin()
        {
            var query = _collection.AsQueryable<C>().Join(_collection.AsQueryable<C>(), c => c.X, c => c.X, (x, y) => x);
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The Join query operator is not supported.")]
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
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "LastOrDefault with predicate after a projection is not supported.")]
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
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "Last with predicate after a projection is not supported.")]
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
        public void TestLastWithPredicateNoMatch()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                var result = (from c in _collection.AsQueryable<C>()
                              select c).Last(c => c.X == 9);
            });
            Assert.AreEqual(ExpectedErrorMessage.LastEmptySequence, ex.Message);
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
        public void TestMaxDZWithProjection()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c.D.Z).Max();
            Assert.AreEqual(55, result);
        }

        [Test]
        public void TestMaxDZWithSelector()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).Max(c => c.D.Z);
            Assert.AreEqual(55, result);
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "Max must be used with either Select or a selector argument, but not both.")]
        public void TestMaxWithProjectionAndSelector()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c.D).Max(d => d.Z);
        }

        [Test]
        public void TestMaxXWithProjection()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c.X).Max();
            Assert.AreEqual(5, result);
        }

        [Test]
        public void TestMaxXWithSelector()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).Max(c => c.X);
            Assert.AreEqual(5, result);
        }

        [Test]
        public void TestMaxXYWithProjection()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select new { c.X, c.Y }).Max();
            Assert.AreEqual(5, result.X);
            Assert.AreEqual(44, result.Y);
        }

        [Test]
        public void TestMaxXYWithSelector()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).Max(c => new { c.X, c.Y });
            Assert.AreEqual(5, result.X);
            Assert.AreEqual(44, result.Y);
        }

        [Test]
        public void TestMinDZWithProjection()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c.D.Z).Min();
            Assert.AreEqual(11, result);
        }

        [Test]
        public void TestMinDZWithSelector()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).Min(c => c.D.Z);
            Assert.AreEqual(11, result);
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "Min must be used with either Select or a selector argument, but not both.")]
        public void TestMinWithProjectionAndSelector()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c.D).Min(d => d.Z);
        }

        [Test]
        public void TestMinXWithProjection()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c.X).Min();
            Assert.AreEqual(1, result);
        }

        [Test]
        public void TestMinXWithSelector()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).Min(c => c.X);
            Assert.AreEqual(1, result);
        }

        [Test]
        public void TestMinXYWithProjection()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select new { c.X, c.Y }).Min();
            Assert.AreEqual(1, result.X);
            Assert.AreEqual(11, result.Y);
        }

        [Test]
        public void TestMinXYWithSelector()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).Min(c => new { c.X, c.Y });
            Assert.AreEqual(1, result.X);
            Assert.AreEqual(11, result.Y);
        }

        [Test]
        public void TestOrderByValueTypeWithObjectReturnType()
        {
            Expression<Func<C, object>> orderByClause = c => c.LX;
            var query = _collection.AsQueryable<C>().OrderBy(orderByClause);

            RunTestOrderByValueTypeWithMismatchingType(query, "(C c) => (Object)c.LX");
        }

        [Test]
        public void TestOrderByValueTypeWithIComparableReturnType()
        {
            Expression<Func<C, IComparable>> orderByClause = c => c.LX;
            var query = _collection.AsQueryable<C>().OrderBy(orderByClause);

            RunTestOrderByValueTypeWithMismatchingType(query, "(C c) => (IComparable)c.LX");
        }

        private void RunTestOrderByValueTypeWithMismatchingType(IOrderedQueryable query, string orderByString)
        {
            var mongoQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(mongoQuery);
            var selectQuery = (SelectQuery) mongoQuery;
            Assert.AreEqual(orderByString, ExpressionFormatter.ToString(selectQuery.OrderBy[0].Key));
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
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "Only one OrderBy or OrderByDescending clause is allowed (use ThenBy or ThenByDescending for multiple order by clauses).")]
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

            var results = query.ToList();
            Assert.AreEqual(5, results.Count);
            Assert.AreEqual(2, results.First());
            Assert.AreEqual(4, results.Last());
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The Reverse query operator is not supported.")]
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
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The SelectMany query operator is not supported.")]
        public void TestSelectMany()
        {
            var query = _collection.AsQueryable<C>().SelectMany(c => new C[] { c });
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The SelectMany query operator is not supported.")]
        public void TestSelectManyWithIndex()
        {
            var query = _collection.AsQueryable<C>().SelectMany((c, index) => new C[] { c });
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The SelectMany query operator is not supported.")]
        public void TestSelectManyWithIntermediateResults()
        {
            var query = _collection.AsQueryable<C>().SelectMany(c => new C[] { c }, (c, i) => i);
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The SelectMany query operator is not supported.")]
        public void TestSelectManyWithIndexAndIntermediateResults()
        {
            var query = _collection.AsQueryable<C>().SelectMany((c, index) => new C[] { c }, (c, i) => i);
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The indexed version of the Select query operator is not supported.")]
        public void TestSelectWithIndex()
        {
            var query = _collection.AsQueryable<C>().Select((c, index) => c);
            query.ToList(); // execute query
        }

        [Test]
        public void TestSelectWithNothingElse()
        {
            var query = from c in _collection.AsQueryable<C>() select c;
            var result = query.ToList();
            Assert.AreEqual(5, result.Count);
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The SequenceEqual query operator is not supported.")]
        public void TestSequenceEqual()
        {
            var source2 = new C[0];
            var result = (from c in _collection.AsQueryable<C>()
                          select c).SequenceEqual(source2);
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The SequenceEqual query operator is not supported.")]
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
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "SingleOrDefault with predicate after a projection is not supported.")]
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

        [Test]
        public void TestSingleOrDefaultWithPredicateTwoMatches()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                var result = (from c in _collection.AsQueryable<C>()
                              select c).SingleOrDefault(c => c.Y == 11);
            });
            Assert.AreEqual(ExpectedErrorMessage.SingleLongSequence, ex.Message);
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
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "Single with predicate after a projection is not supported.")]
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
        public void TestSingleWithPredicateNoMatch()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                var result = (from c in _collection.AsQueryable<C>()
                              select c).Single(c => c.X == 9);
            });
            Assert.AreEqual(ExpectedErrorMessage.SingleEmptySequence, ex.Message);
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
        public void TestSingleWithPredicateTwoMatches()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                var result = (from c in _collection.AsQueryable<C>()
                              select c).Single(c => c.Y == 11);
            });
            Assert.AreEqual(ExpectedErrorMessage.SingleLongSequence, ex.Message);
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
            Assert.AreEqual(2, selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.IsNull(selectQuery.BuildQuery());
            Assert.AreEqual(3, Consume(query));
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The SkipWhile query operator is not supported.")]
        public void TestSkipWhile()
        {
            var query = _collection.AsQueryable<C>().SkipWhile(c => true);
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The Sum query operator is not supported.")]
        public void TestSum()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select 1.0).Sum();
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The Sum query operator is not supported.")]
        public void TestSumNullable()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select (double?)1.0).Sum();
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The Sum query operator is not supported.")]
        public void TestSumWithSelector()
        {
            var result = (from c in _collection.AsQueryable<C>()
                          select c).Sum(c => 1.0);
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The Sum query operator is not supported.")]
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
            Assert.AreEqual(2, selectQuery.Take);

            Assert.IsNull(selectQuery.BuildQuery());
            Assert.AreEqual(2, Consume(query));
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The TakeWhile query operator is not supported.")]
        public void TestTakeWhile()
        {
            var query = _collection.AsQueryable<C>().TakeWhile(c => true);
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "ThenBy or ThenByDescending can only be used after OrderBy or OrderByDescending.")]
        public void TestThenByWithMissingOrderBy()
        {
            // not sure this could ever happen in real life without deliberate sabotaging like with this cast
            var query = ((IOrderedQueryable<C>)_collection.AsQueryable<C>())
                .ThenBy(c => c.X);

            MongoQueryTranslator.Translate(query);
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The Union query operator is not supported.")]
        public void TestUnion()
        {
            var source2 = new C[0];
            var query = (from c in _collection.AsQueryable<C>()
                         select c).Union(source2);
            query.ToList(); // execute query
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The Union query operator is not supported.")]
        public void TestUnionWithEqualityComparer()
        {
            var source2 = new C[0];
            var query = (from c in _collection.AsQueryable<C>()
                         select c).Union(source2, new CEqualityComparer());
            query.ToList(); // execute query
        }

        [Test]
        public void TestWhereAAny()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.A.Any()
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => Enumerable.Any<Int32>(c.A)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"a\" : { \"$ne\" : null, \"$not\" : { \"$size\" : 0 } } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "Any is only support for items that serialize into documents. The current serializer is Int32Serializer and must implement IBsonDocumentSerializer for participation in Any queries.")]
        public void TestWhereAAnyWithPredicate()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.A.Any(a => a > 3)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => Enumerable.Any<Int32>(c.A, (Int32 a) => (a > 3))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"a\" : 2 }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereLocalListContainsX()
        {
            var local = new List<int> { 1, 2, 3 };
            
            var query = from c in _collection.AsQueryable<C>()
                        where local.Contains(c.X)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => System.Collections.Generic.List`1[System.Int32].Contains(c.X)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"x\" : { \"$in\" : [1, 2, 3] } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(3, Consume(query));
        }

        [Test]
        public void TestWhereLocalArrayContainsX()
        {
            var local = new [] { 1, 2, 3 };

            var query = from c in _collection.AsQueryable<C>()
                        where local.Contains(c.X)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => Enumerable.Contains<Int32>(Int32[]:{ 1, 2, 3 }, c.X)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"x\" : { \"$in\" : [1, 2, 3] } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(3, Consume(query));
        }

        [Test]
        public void TestWhereLocalIListContainsX()
        {
            // this will generate a non-list, non-array.
            IList<int> local = new[] { 1, 2, 3 };

            var query = from c in _collection.AsQueryable<C>()
                        where local.Contains(c.X)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => Int32[]:{ 1, 2, 3 }.Contains(c.X)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"x\" : { \"$in\" : [1, 2, 3] } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(3, Consume(query));
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
                        where Query.NotExists("a").Inject()
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
                        where Query.Exists("a").Inject()
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
                        where !Query.Exists("a").Inject()
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
        public void TestWhereALengthEquals3Reversed()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where 3 == c.A.Length
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

            Assert.AreEqual("{ \"a.1\" : 3 }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
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

            Assert.AreEqual("{ \"a.1\" : { \"$ne\" : 3 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereASub1ModTwoEquals1()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.A[1] % 2 == 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => ((c.A[1] % 2) == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"a.1\" : { \"$mod\" : [2, 1] } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereASub1ModTwoEquals1Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.A[1] % 2 == 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !((c.A[1] % 2) == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"a.1\" : { \"$not\" : { \"$mod\" : [2, 1] } } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereASub1ModTwoNotEquals1()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.A[1] % 2 != 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => ((c.A[1] % 2) != 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"a.1\" : { \"$not\" : { \"$mod\" : [2, 1] } } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereASub1ModTwoNotEquals1Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.A[1] % 2 != 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !((c.A[1] % 2) != 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"a.1\" : { \"$mod\" : [2, 1] } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
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

            Assert.AreEqual("{ \"a.1\" : { \"$ne\" : 3 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
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

            Assert.AreEqual("{ \"a.1\" : 3 }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
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
        public void TestWhereBASub0()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.BA[0]
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => c.BA[0]", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"ba.0\" : true }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereBASub0EqualsFalse()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.BA[0] == false
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.BA[0] == false)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"ba.0\" : false }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(0, Consume(query));
        }

        [Test]
        public void TestWhereBASub0EqualsFalseNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.BA[0] == false)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !(c.BA[0] == false)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"ba.0\" : { \"$ne\" : false } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(5, Consume(query));
        }

        [Test]
        public void TestWhereBASub0EqualsTrue()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.BA[0] == true
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.BA[0] == true)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"ba.0\" : true }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereBASub0EqualsTrueNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.BA[0] == true)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !(c.BA[0] == true)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"ba.0\" : { \"$ne\" : true } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereBASub0Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !c.BA[0]
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !c.BA[0]", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"ba.0\" : { \"$ne\" : true } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
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

            Assert.AreEqual("{ \"b\" : { \"$ne\" : false } }", selectQuery.BuildQuery().ToJson());
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

            Assert.AreEqual("{ \"b\" : { \"$ne\" : true } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
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

            Assert.AreEqual("{ \"b\" : { \"$ne\" : true } }", selectQuery.BuildQuery().ToJson());
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
        public void TestWhereDBRefEquals()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.DBRef == new MongoDBRef("db", "c", 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.DBRef == new MongoDBRef(\"db\", \"c\", 1))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"dbref\" : { \"$ref\" : \"c\", \"$id\" : 1, \"$db\" : \"db\" } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereDBRefEqualsNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.DBRef == new MongoDBRef("db", "c", 1))
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !(c.DBRef == new MongoDBRef(\"db\", \"c\", 1))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"dbref\" : { \"$ne\" : { \"$ref\" : \"c\", \"$id\" : 1, \"$db\" : \"db\" } } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereDBRefNotEquals()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.DBRef != new MongoDBRef("db", "c", 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.DBRef != new MongoDBRef(\"db\", \"c\", 1))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"dbref\" : { \"$ne\" : { \"$ref\" : \"c\", \"$id\" : 1, \"$db\" : \"db\" } } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereDBRefNotEqualsNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.DBRef != new MongoDBRef("db", "c", 1))
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !(c.DBRef != new MongoDBRef(\"db\", \"c\", 1))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"dbref\" : { \"$ref\" : \"c\", \"$id\" : 1, \"$db\" : \"db\" } }", selectQuery.BuildQuery().ToJson());
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
        public void TestWhereDEquals11()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.D == new D { Z = 11 }
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.D == new D { Z = 11 })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"d\" : { \"z\" : 11 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereDEquals11Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.D == new D { Z = 11 })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !(c.D == new D { Z = 11 })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"d\" : { \"$ne\" : { \"z\" : 11 } } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereDNotEquals11()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.D != new D { Z = 11 }
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.D != new D { Z = 11 })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"d\" : { \"$ne\" : { \"z\" : 11 } } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereDNotEquals11Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.D != new D { Z = 11 })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !(c.D != new D { Z = 11 })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"d\" : { \"z\" : 11 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereDAAnyWithPredicate()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.DA.Any(d => d.Z == 333)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => Enumerable.Any<D>(c.DA, (D d) => (d.Z == 333))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"da\" : { \"$elemMatch\" : { \"z\" : 333 } } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereEAContainsAll()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.EA.ContainsAll(new E[] { E.A, E.B})
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => LinqToMongo.ContainsAll<E>(c.EA, E[]:{ E.A, E.B })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"ea\" : { \"$all\" : [1, 2] } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereEAContainsAllNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !c.EA.ContainsAll(new E[] { E.A, E.B })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !LinqToMongo.ContainsAll<E>(c.EA, E[]:{ E.A, E.B })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"ea\" : { \"$not\" : { \"$all\" : [1, 2] } } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereEAContainsAny()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.EA.ContainsAny(new[] { E.A, E.B })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => LinqToMongo.ContainsAny<E>(c.EA, E[]:{ E.A, E.B })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"ea\" : { \"$in\" : [1, 2] } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereEAContainsAnyNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !c.EA.ContainsAny(new[] { E.A, E.B })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !LinqToMongo.ContainsAny<E>(c.EA, E[]:{ E.A, E.B })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"ea\" : { \"$nin\" : [1, 2] } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereEAContainsB()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.EA.Contains(E.B)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => Enumerable.Contains<E>(c.EA, E.B)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"ea\" : 2 }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereEAContainsBNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !c.EA.Contains(E.B)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !Enumerable.Contains<E>(c.EA, E.B)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"ea\" : { \"$ne\" : 2 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereEASub0EqualsA()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.EA[0] == E.A
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => ((Int32)c.EA[0] == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"ea.0\" : 1 }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereEASub0EqualsANot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.EA[0] == E.A)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !((Int32)c.EA[0] == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"ea.0\" : { \"$ne\" : 1 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereEASub0NotEqualsA()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.EA[0] != E.A
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => ((Int32)c.EA[0] != 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"ea.0\" : { \"$ne\" : 1 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereEASub0NotEqualsANot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.EA[0] != E.A)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !((Int32)c.EA[0] != 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"ea.0\" : 1 }", selectQuery.BuildQuery().ToJson());
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
        public void TestWhereEEqualsAReversed()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where E.A == c.E
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
        public void TestWhereEInAOrB()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.E.In(new[] { E.A, E.B })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => LinqToMongo.In<E>(c.E, E[]:{ E.A, E.B })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"e\" : { \"$in\" : [\"A\", \"B\"] } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereEInAOrBNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !c.E.In(new[] { E.A, E.B })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !LinqToMongo.In<E>(c.E, E[]:{ E.A, E.B })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"e\" : { \"$nin\" : [\"A\", \"B\"] } }", selectQuery.BuildQuery().ToJson());
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
                        where Query.NotExists("l").Inject()
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
                        where Query.Exists("l").Inject()
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
                        where !Query.Exists("l").Inject()
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
        public void TestWhereLCountMethodEquals3()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.L.Count() == 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (Enumerable.Count<Int32>(c.L) == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"l\" : { \"$size\" : 3 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereLCountMethodEquals3Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.L.Count() == 3)
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
        public void TestWhereLCountMethodEquals3Reversed()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where 3 == c.L.Count()
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (Enumerable.Count<Int32>(c.L) == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"l\" : { \"$size\" : 3 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereLCountPropertyEquals3()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.L.Count == 3
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
        public void TestWhereLCountPropertyEquals3Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.L.Count == 3)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !(c.L.Count == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"l\" : { \"$not\" : { \"$size\" : 3 } } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereLCountPropertyEquals3Reversed()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where 3 == c.L.Count
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
        public void TestWhereLCountPropertyNotEquals3()
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
        public void TestWhereLCountPropertyNotEquals3Not()
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
        public void TestWhereLSub1Equals3()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.L[1] == 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.L.get_Item(1) == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"l.1\" : 3 }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereLSub1Equals3Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.L[1] == 3)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !(c.L.get_Item(1) == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"l.1\" : { \"$ne\" : 3 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereLSub1ModTwoEquals1()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.L[1] % 2 == 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => ((c.L.get_Item(1) % 2) == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"l.1\" : { \"$mod\" : [2, 1] } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereLSub1ModTwoEquals1Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.L[1] % 2 == 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !((c.L.get_Item(1) % 2) == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"l.1\" : { \"$not\" : { \"$mod\" : [2, 1] } } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereLSub1ModTwoNotEquals1()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.L[1] % 2 != 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => ((c.L.get_Item(1) % 2) != 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"l.1\" : { \"$not\" : { \"$mod\" : [2, 1] } } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereLSub1ModTwoNotEquals1Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.L[1] % 2 != 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !((c.L.get_Item(1) % 2) != 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"l.1\" : { \"$mod\" : [2, 1] } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereLSub1NotEquals3()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.L[1] != 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.L.get_Item(1) != 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"l.1\" : { \"$ne\" : 3 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereLSub1NotEquals3Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.L[1] != 3)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !(c.L.get_Item(1) != 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"l.1\" : 3 }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereLXModTwoEquals1()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.LX % 2 == 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => ((c.LX % 2) == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"lx\" : { \"$mod\" : [2, 1] } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(3, Consume(query));
        }

        [Test]
        public void TestWhereLXModTwoEquals1Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.LX % 2 == 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !((c.LX % 2) == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"lx\" : { \"$not\" : { \"$mod\" : [2, 1] } } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(2, Consume(query));
        }

        [Test]
        public void TestWhereLXModTwoEquals1Reversed()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where 1 == c.LX % 2
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => ((c.LX % 2) == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"lx\" : { \"$mod\" : [2, 1] } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(3, Consume(query));
        }

        [Test]
        public void TestWhereLXModTwoNotEquals1()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.LX % 2 != 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => ((c.LX % 2) != 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"lx\" : { \"$not\" : { \"$mod\" : [2, 1] } } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(2, Consume(query));
        }

        [Test]
        public void TestWhereLXModTwoNotEquals1Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.LX % 2 != 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !((c.LX % 2) != 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"lx\" : { \"$mod\" : [2, 1] } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(3, Consume(query));
        }

        [Test]
        public void TestWhereSASub0ContainsO()
        {
            if (_server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var query = from c in _collection.AsQueryable<C>()
                            where c.SA[0].Contains("o")
                            select c;

                var translatedQuery = MongoQueryTranslator.Translate(query);
                Assert.IsInstanceOf<SelectQuery>(translatedQuery);
                Assert.AreSame(_collection, translatedQuery.Collection);
                Assert.AreSame(typeof(C), translatedQuery.DocumentType);

                var selectQuery = (SelectQuery)translatedQuery;
                Assert.AreEqual("(C c) => c.SA[0].Contains(\"o\")", ExpressionFormatter.ToString(selectQuery.Where));
                Assert.IsNull(selectQuery.OrderBy);
                Assert.IsNull(selectQuery.Projection);
                Assert.IsNull(selectQuery.Skip);
                Assert.IsNull(selectQuery.Take);

                Assert.AreEqual("{ \"sa.0\" : /o/s }", selectQuery.BuildQuery().ToJson());
                Assert.AreEqual(1, Consume(query));
            }
        }

        [Test]
        public void TestWhereSASub0ContainsONot()
        {
            if (_server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var query = from c in _collection.AsQueryable<C>()
                            where !c.SA[0].Contains("o")
                            select c;

                var translatedQuery = MongoQueryTranslator.Translate(query);
                Assert.IsInstanceOf<SelectQuery>(translatedQuery);
                Assert.AreSame(_collection, translatedQuery.Collection);
                Assert.AreSame(typeof(C), translatedQuery.DocumentType);

                var selectQuery = (SelectQuery)translatedQuery;
                Assert.AreEqual("(C c) => !c.SA[0].Contains(\"o\")", ExpressionFormatter.ToString(selectQuery.Where));
                Assert.IsNull(selectQuery.OrderBy);
                Assert.IsNull(selectQuery.Projection);
                Assert.IsNull(selectQuery.Skip);
                Assert.IsNull(selectQuery.Take);

                Assert.AreEqual("{ \"sa.0\" : { \"$not\" : /o/s } }", selectQuery.BuildQuery().ToJson());
                Assert.AreEqual(4, Consume(query));
            }
        }

        [Test]
        public void TestWhereSASub0EndsWithM()
        {
            if (_server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var query = from c in _collection.AsQueryable<C>()
                            where c.SA[0].EndsWith("m")
                            select c;

                var translatedQuery = MongoQueryTranslator.Translate(query);
                Assert.IsInstanceOf<SelectQuery>(translatedQuery);
                Assert.AreSame(_collection, translatedQuery.Collection);
                Assert.AreSame(typeof(C), translatedQuery.DocumentType);

                var selectQuery = (SelectQuery)translatedQuery;
                Assert.AreEqual("(C c) => c.SA[0].EndsWith(\"m\")", ExpressionFormatter.ToString(selectQuery.Where));
                Assert.IsNull(selectQuery.OrderBy);
                Assert.IsNull(selectQuery.Projection);
                Assert.IsNull(selectQuery.Skip);
                Assert.IsNull(selectQuery.Take);

                Assert.AreEqual("{ \"sa.0\" : /m$/s }", selectQuery.BuildQuery().ToJson());
                Assert.AreEqual(1, Consume(query));
            }
        }

        [Test]
        public void TestWhereSASub0EndsWithMNot()
        {
            if (_server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var query = from c in _collection.AsQueryable<C>()
                            where !c.SA[0].EndsWith("m")
                            select c;

                var translatedQuery = MongoQueryTranslator.Translate(query);
                Assert.IsInstanceOf<SelectQuery>(translatedQuery);
                Assert.AreSame(_collection, translatedQuery.Collection);
                Assert.AreSame(typeof(C), translatedQuery.DocumentType);

                var selectQuery = (SelectQuery)translatedQuery;
                Assert.AreEqual("(C c) => !c.SA[0].EndsWith(\"m\")", ExpressionFormatter.ToString(selectQuery.Where));
                Assert.IsNull(selectQuery.OrderBy);
                Assert.IsNull(selectQuery.Projection);
                Assert.IsNull(selectQuery.Skip);
                Assert.IsNull(selectQuery.Take);

                Assert.AreEqual("{ \"sa.0\" : { \"$not\" : /m$/s } }", selectQuery.BuildQuery().ToJson());
                Assert.AreEqual(4, Consume(query));
            }
        }

        [Test]
        public void TestWhereSASub0IsMatch()
        {
            if (_server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var regex = new Regex(@"^T");
                var query = from c in _collection.AsQueryable<C>()
                            where regex.IsMatch(c.SA[0])
                            select c;

                var translatedQuery = MongoQueryTranslator.Translate(query);
                Assert.IsInstanceOf<SelectQuery>(translatedQuery);
                Assert.AreSame(_collection, translatedQuery.Collection);
                Assert.AreSame(typeof(C), translatedQuery.DocumentType);

                var selectQuery = (SelectQuery)translatedQuery;
                Assert.AreEqual("(C c) => Regex:(@\"^T\").IsMatch(c.SA[0])", ExpressionFormatter.ToString(selectQuery.Where));
                Assert.IsNull(selectQuery.OrderBy);
                Assert.IsNull(selectQuery.Projection);
                Assert.IsNull(selectQuery.Skip);
                Assert.IsNull(selectQuery.Take);

                Assert.AreEqual("{ \"sa.0\" : /^T/ }", selectQuery.BuildQuery().ToJson());
                Assert.AreEqual(1, Consume(query));
            }
        }

        [Test]
        public void TestWhereSASub0IsMatchNot()
        {
            if (_server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var regex = new Regex(@"^T");
                var query = from c in _collection.AsQueryable<C>()
                            where !regex.IsMatch(c.SA[0])
                            select c;

                var translatedQuery = MongoQueryTranslator.Translate(query);
                Assert.IsInstanceOf<SelectQuery>(translatedQuery);
                Assert.AreSame(_collection, translatedQuery.Collection);
                Assert.AreSame(typeof(C), translatedQuery.DocumentType);

                var selectQuery = (SelectQuery)translatedQuery;
                Assert.AreEqual("(C c) => !Regex:(@\"^T\").IsMatch(c.SA[0])", ExpressionFormatter.ToString(selectQuery.Where));
                Assert.IsNull(selectQuery.OrderBy);
                Assert.IsNull(selectQuery.Projection);
                Assert.IsNull(selectQuery.Skip);
                Assert.IsNull(selectQuery.Take);

                Assert.AreEqual("{ \"sa.0\" : { \"$not\" : /^T/ } }", selectQuery.BuildQuery().ToJson());
                Assert.AreEqual(4, Consume(query));
            }
        }

        [Test]
        public void TestWhereSASub0IsMatchStatic()
        {
            if (_server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var query = from c in _collection.AsQueryable<C>()
                            where Regex.IsMatch(c.SA[0], "^T")
                            select c;

                var translatedQuery = MongoQueryTranslator.Translate(query);
                Assert.IsInstanceOf<SelectQuery>(translatedQuery);
                Assert.AreSame(_collection, translatedQuery.Collection);
                Assert.AreSame(typeof(C), translatedQuery.DocumentType);

                var selectQuery = (SelectQuery)translatedQuery;
                Assert.AreEqual("(C c) => Regex.IsMatch(c.SA[0], \"^T\")", ExpressionFormatter.ToString(selectQuery.Where));
                Assert.IsNull(selectQuery.OrderBy);
                Assert.IsNull(selectQuery.Projection);
                Assert.IsNull(selectQuery.Skip);
                Assert.IsNull(selectQuery.Take);

                Assert.AreEqual("{ \"sa.0\" : /^T/ }", selectQuery.BuildQuery().ToJson());
                Assert.AreEqual(1, Consume(query));
            }
        }

        [Test]
        public void TestWhereSASub0IsMatchStaticNot()
        {
            if (_server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var query = from c in _collection.AsQueryable<C>()
                            where !Regex.IsMatch(c.SA[0], "^T")
                            select c;

                var translatedQuery = MongoQueryTranslator.Translate(query);
                Assert.IsInstanceOf<SelectQuery>(translatedQuery);
                Assert.AreSame(_collection, translatedQuery.Collection);
                Assert.AreSame(typeof(C), translatedQuery.DocumentType);

                var selectQuery = (SelectQuery)translatedQuery;
                Assert.AreEqual("(C c) => !Regex.IsMatch(c.SA[0], \"^T\")", ExpressionFormatter.ToString(selectQuery.Where));
                Assert.IsNull(selectQuery.OrderBy);
                Assert.IsNull(selectQuery.Projection);
                Assert.IsNull(selectQuery.Skip);
                Assert.IsNull(selectQuery.Take);

                Assert.AreEqual("{ \"sa.0\" : { \"$not\" : /^T/ } }", selectQuery.BuildQuery().ToJson());
                Assert.AreEqual(4, Consume(query));
            }
        }

        [Test]
        public void TestWhereSASub0IsMatchStaticWithOptions()
        {
            if (_server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var query = from c in _collection.AsQueryable<C>()
                            where Regex.IsMatch(c.SA[0], "^t", RegexOptions.IgnoreCase)
                            select c;

                var translatedQuery = MongoQueryTranslator.Translate(query);
                Assert.IsInstanceOf<SelectQuery>(translatedQuery);
                Assert.AreSame(_collection, translatedQuery.Collection);
                Assert.AreSame(typeof(C), translatedQuery.DocumentType);

                var selectQuery = (SelectQuery)translatedQuery;
                Assert.AreEqual("(C c) => Regex.IsMatch(c.SA[0], \"^t\", RegexOptions.IgnoreCase)", ExpressionFormatter.ToString(selectQuery.Where));
                Assert.IsNull(selectQuery.OrderBy);
                Assert.IsNull(selectQuery.Projection);
                Assert.IsNull(selectQuery.Skip);
                Assert.IsNull(selectQuery.Take);

                Assert.AreEqual("{ \"sa.0\" : /^t/i }", selectQuery.BuildQuery().ToJson());
                Assert.AreEqual(1, Consume(query));
            }
        }

        [Test]
        public void TestWhereSASub0StartsWithT()
        {
            if (_server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var query = from c in _collection.AsQueryable<C>()
                            where c.SA[0].StartsWith("T")
                            select c;

                var translatedQuery = MongoQueryTranslator.Translate(query);
                Assert.IsInstanceOf<SelectQuery>(translatedQuery);
                Assert.AreSame(_collection, translatedQuery.Collection);
                Assert.AreSame(typeof(C), translatedQuery.DocumentType);

                var selectQuery = (SelectQuery)translatedQuery;
                Assert.AreEqual("(C c) => c.SA[0].StartsWith(\"T\")", ExpressionFormatter.ToString(selectQuery.Where));
                Assert.IsNull(selectQuery.OrderBy);
                Assert.IsNull(selectQuery.Projection);
                Assert.IsNull(selectQuery.Skip);
                Assert.IsNull(selectQuery.Take);

                Assert.AreEqual("{ \"sa.0\" : /^T/s }", selectQuery.BuildQuery().ToJson());
                Assert.AreEqual(1, Consume(query));
            }
        }

        [Test]
        public void TestWhereSASub0StartsWithTNot()
        {
            if (_server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var query = from c in _collection.AsQueryable<C>()
                            where !c.SA[0].StartsWith("T")
                            select c;

                var translatedQuery = MongoQueryTranslator.Translate(query);
                Assert.IsInstanceOf<SelectQuery>(translatedQuery);
                Assert.AreSame(_collection, translatedQuery.Collection);
                Assert.AreSame(typeof(C), translatedQuery.DocumentType);

                var selectQuery = (SelectQuery)translatedQuery;
                Assert.AreEqual("(C c) => !c.SA[0].StartsWith(\"T\")", ExpressionFormatter.ToString(selectQuery.Where));
                Assert.IsNull(selectQuery.OrderBy);
                Assert.IsNull(selectQuery.Projection);
                Assert.IsNull(selectQuery.Skip);
                Assert.IsNull(selectQuery.Take);

                Assert.AreEqual("{ \"sa.0\" : { \"$not\" : /^T/s } }", selectQuery.BuildQuery().ToJson());
                Assert.AreEqual(4, Consume(query));
            }
        }

        [Test]
        public void TestWhereSContainsAbc()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.Contains("abc")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => c.S.Contains(\"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /abc/s }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSContainsAbcNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !c.S.Contains("abc")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !c.S.Contains(\"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : { \"$not\" : /abc/s } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereSContainsDot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.Contains(".")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => c.S.Contains(\".\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /\\./s }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(0, Consume(query));
        }

        [Test]
        public void TestWhereSCountEquals3()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.Count() == 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (Enumerable.Count<Char>(c.S) == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^.{3}$/s }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSEqualsAbc()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S == "abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S == \"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : \"abc\" }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSEqualsAbcNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.S == "abc")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !(c.S == \"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : { \"$ne\" : \"abc\" } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereSEqualsMethodAbc()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.Equals("abc")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => c.S.Equals(\"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : \"abc\" }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSEqualsMethodAbcNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.S.Equals("abc"))
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !c.S.Equals(\"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : { \"$ne\" : \"abc\" } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereSEqualsStaticMethodAbc()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where string.Equals(c.S, "abc")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => String.Equals(c.S, \"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : \"abc\" }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSEqualsStaticMethodAbcNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !string.Equals(c.S, "abc")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !String.Equals(c.S, \"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : { \"$ne\" : \"abc\" } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereSEndsWithAbc()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.EndsWith("abc")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => c.S.EndsWith(\"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /abc$/s }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSEndsWithAbcNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !c.S.EndsWith("abc")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !c.S.EndsWith(\"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : { \"$not\" : /abc$/s } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereSIndexOfAnyBC()
        {
            var tempCollection = _database.GetCollection("temp");
            tempCollection.Drop();
            tempCollection.Insert(new C { S = "bxxx" });
            tempCollection.Insert(new C { S = "xbxx" });
            tempCollection.Insert(new C { S = "xxbx" });
            tempCollection.Insert(new C { S = "xxxb" });
            tempCollection.Insert(new C { S = "bxbx" });
            tempCollection.Insert(new C { S = "xbbx" });
            tempCollection.Insert(new C { S = "xxbb" });

            var query1 =
                from c in tempCollection.AsQueryable<C>()
                where c.S.IndexOfAny(new char[] { 'b', 'c' }) == 2
                select c;
            Assert.AreEqual(2, Consume(query1));

            var query2 =
                from c in tempCollection.AsQueryable<C>()
                where c.S.IndexOfAny(new char[] { 'b', 'c' }, 1) == 2
                select c;
            Assert.AreEqual(3, Consume(query2));

            var query3 =
                from c in tempCollection.AsQueryable<C>()
                where c.S.IndexOfAny(new char[] { 'b', 'c' }, 1, 1) == 2
                select c;
            Assert.AreEqual(0, Consume(query3));

            var query4 =
                from c in tempCollection.AsQueryable<C>()
                where c.S.IndexOfAny(new char[] { 'b', 'c' }, 1, 2) == 2
                select c;
            Assert.AreEqual(3, Consume(query4));
        }

        [Test]
        public void TestWhereSIndexOfAnyBDashCEquals1()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.IndexOfAny(new char[] { 'b', '-', 'c' }) == 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.IndexOfAny(Char[]:{ 'b', '-', 'c' }) == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^[^b\\-c]{1}[b\\-c]/s }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSIndexOfAnyBCStartIndex1Equals1()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.IndexOfAny(new char[] { 'b', '-', 'c' }, 1) == 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.IndexOfAny(Char[]:{ 'b', '-', 'c' }, 1) == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^.{1}[^b\\-c]{0}[b\\-c]/s }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSIndexOfAnyBCStartIndex1Count2Equals1()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.IndexOfAny(new char[] { 'b', '-', 'c' }, 1, 2) == 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.IndexOfAny(Char[]:{ 'b', '-', 'c' }, 1, 2) == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^.{1}(?=.{2})[^b\\-c]{0}[b\\-c]/s }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSIndexOfB()
        {
            var tempCollection = _database.GetCollection("temp");
            tempCollection.Drop();
            tempCollection.Insert(new C { S = "bxxx" });
            tempCollection.Insert(new C { S = "xbxx" });
            tempCollection.Insert(new C { S = "xxbx" });
            tempCollection.Insert(new C { S = "xxxb" });
            tempCollection.Insert(new C { S = "bxbx" });
            tempCollection.Insert(new C { S = "xbbx" });
            tempCollection.Insert(new C { S = "xxbb" });

            var query1 =
                from c in tempCollection.AsQueryable<C>()
                where c.S.IndexOf('b') == 2
                select c;
            Assert.AreEqual(2, Consume(query1));

            var query2 =
                from c in tempCollection.AsQueryable<C>()
                where c.S.IndexOf('b', 1) == 2
                select c;
            Assert.AreEqual(3, Consume(query2));

            var query3 =
                from c in tempCollection.AsQueryable<C>()
                where c.S.IndexOf('b', 1, 1) == 2
                select c;
            Assert.AreEqual(0, Consume(query3));

            var query4 =
                from c in tempCollection.AsQueryable<C>()
                where c.S.IndexOf('b', 1, 2) == 2
                select c;
            Assert.AreEqual(3, Consume(query4));
        }

        [Test]
        public void TestWhereSIndexOfBEquals1()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.IndexOf('b') == 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.IndexOf('b') == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^[^b]{1}b/s }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSIndexOfBStartIndex1Equals1()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.IndexOf('b', 1) == 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.IndexOf('b', 1) == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^.{1}[^b]{0}b/s }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSIndexOfBStartIndex1Count2Equals1()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.IndexOf('b', 1, 2) == 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.IndexOf('b', 1, 2) == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^.{1}(?=.{2})[^b]{0}b/s }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSIndexOfXyz()
        {
            var tempCollection = _database.GetCollection("temp");
            tempCollection.Drop();
            tempCollection.Insert(new C { S = "xyzaaa" });
            tempCollection.Insert(new C { S = "axyzaa" });
            tempCollection.Insert(new C { S = "aaxyza" });
            tempCollection.Insert(new C { S = "aaaxyz" });
            tempCollection.Insert(new C { S = "aaaaxy" });
            tempCollection.Insert(new C { S = "xyzxyz" });

            var query1 =
                from c in tempCollection.AsQueryable<C>()
                where c.S.IndexOf("xyz") == 3
                select c;
            Assert.AreEqual(1, Consume(query1));

            var query2 =
                from c in tempCollection.AsQueryable<C>()
                where c.S.IndexOf("xyz", 1) == 3
                select c;
            Assert.AreEqual(2, Consume(query2));

            var query3 =
                from c in tempCollection.AsQueryable<C>()
                where c.S.IndexOf("xyz", 1, 4) == 3
                select c;
            Assert.AreEqual(0, Consume(query3)); // substring isn't long enough to match

            var query4 =
                from c in tempCollection.AsQueryable<C>()
                where c.S.IndexOf("xyz", 1, 5) == 3
                select c;
            Assert.AreEqual(2, Consume(query4));
        }

        [Test]
        public void TestWhereSIndexOfXyzEquals3()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.IndexOf("xyz") == 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.IndexOf(\"xyz\") == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^(?!.{0,2}xyz).{3}xyz/s }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSIndexOfXyzStartIndex1Equals3()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.IndexOf("xyz", 1) == 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.IndexOf(\"xyz\", 1) == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^.{1}(?!.{0,1}xyz).{2}xyz/s }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSIndexOfXyzStartIndex1Count5Equals3()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.IndexOf("xyz", 1, 5) == 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.IndexOf(\"xyz\", 1, 5) == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^.{1}(?=.{5})(?!.{0,1}xyz).{2}xyz/s }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSIsMatch()
        {
            var regex = new Regex(@"^abc");
            var query = from c in _collection.AsQueryable<C>()
                        where regex.IsMatch(c.S)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => Regex:(@\"^abc\").IsMatch(c.S)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^abc/ }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSIsMatchNot()
        {
            var regex = new Regex(@"^abc");
            var query = from c in _collection.AsQueryable<C>()
                        where !regex.IsMatch(c.S)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !Regex:(@\"^abc\").IsMatch(c.S)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : { \"$not\" : /^abc/ } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereSIsMatchStatic()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where Regex.IsMatch(c.S, "^abc")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => Regex.IsMatch(c.S, \"^abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^abc/ }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSIsMatchStaticNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !Regex.IsMatch(c.S, "^abc")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !Regex.IsMatch(c.S, \"^abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : { \"$not\" : /^abc/ } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereSIsMatchStaticWithOptions()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where Regex.IsMatch(c.S, "^abc", RegexOptions.IgnoreCase)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => Regex.IsMatch(c.S, \"^abc\", RegexOptions.IgnoreCase)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^abc/i }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSIsNullOrEmpty()
        {
            var tempCollection = _database.GetCollection("temp");
            tempCollection.Drop();
            tempCollection.Insert(new C()); // serialized document will have no "s" field
            tempCollection.Insert(new BsonDocument("s", BsonNull.Value)); // work around [BsonIgnoreIfNull] on S
            tempCollection.Insert(new C { S = "" });
            tempCollection.Insert(new C { S = "x" });

            var query = from c in tempCollection.AsQueryable<C>()
                        where string.IsNullOrEmpty(c.S)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(tempCollection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => String.IsNullOrEmpty(c.S)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"$or\" : [{ \"s\" : { \"$type\" : 10 } }, { \"s\" : \"\" }] }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(2, Consume(query));
        }

        [Test]
        public void TestWhereSLengthEquals3()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.Length == 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.Length == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^.{3}$/s }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSLengthEquals3Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.S.Length == 3)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !(c.S.Length == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : { \"$not\" : /^.{3}$/s } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereSLengthGreaterThan3()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.Length > 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.Length > 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^.{4,}$/s }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSLengthGreaterThanOrEquals3()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.Length >= 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.Length >= 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^.{3,}$/s }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(2, Consume(query));
        }

        [Test]
        public void TestWhereSLengthLessThan3()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.Length < 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.Length < 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^.{0,2}$/s }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(0, Consume(query));
        }

        [Test]
        public void TestWhereSLengthLessThanOrEquals3()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.Length <= 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.Length <= 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^.{0,3}$/s }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSLengthNotEquals3()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.Length != 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.Length != 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : { \"$not\" : /^.{3}$/s } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereSLengthNotEquals3Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.S.Length != 3)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !(c.S.Length != 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^.{3}$/s }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSNotEqualsAbc()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S != "abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S != \"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : { \"$ne\" : \"abc\" } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereSNotEqualsAbcNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.S != "abc")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !(c.S != \"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : \"abc\" }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSStartsWithAbc()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.StartsWith("abc")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => c.S.StartsWith(\"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^abc/s }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSStartsWithAbcNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !c.S.StartsWith("abc")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !c.S.StartsWith(\"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : { \"$not\" : /^abc/s } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereSSub1EqualsB()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S[1] == 'b'
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => ((Int32)c.S.get_Chars(1) == 98)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^.{1}b/s }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSSub1EqualsBNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.S[1] == 'b')
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !((Int32)c.S.get_Chars(1) == 98)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : { \"$not\" : /^.{1}b/s } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereSSub1NotEqualsB()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S[1] != 'b'
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => ((Int32)c.S.get_Chars(1) != 98)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^.{1}[^b]/s }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSSub1NotEqualsBNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.S[1] != 'b')
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !((Int32)c.S.get_Chars(1) != 98)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : { \"$not\" : /^.{1}[^b]/s } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereSTrimContainsXyz()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.Trim().Contains("xyz")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => c.S.Trim().Contains(\"xyz\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^\\s*.*xyz.*\\s*$/s }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSTrimContainsXyzNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !c.S.Trim().Contains("xyz")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !c.S.Trim().Contains(\"xyz\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : { \"$not\" : /^\\s*.*xyz.*\\s*$/s } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereSTrimEndsWithXyz()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.Trim().EndsWith("xyz")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => c.S.Trim().EndsWith(\"xyz\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^\\s*.*xyz\\s*$/s }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSTrimEndsWithXyzNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !c.S.Trim().EndsWith("xyz")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !c.S.Trim().EndsWith(\"xyz\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : { \"$not\" : /^\\s*.*xyz\\s*$/s } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereSTrimStartsWithXyz()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.Trim().StartsWith("xyz")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => c.S.Trim().StartsWith(\"xyz\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^\\s*xyz.*\\s*$/s }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSTrimStartsWithXyzNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !c.S.Trim().StartsWith("xyz")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !c.S.Trim().StartsWith(\"xyz\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : { \"$not\" : /^\\s*xyz.*\\s*$/s } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereSTrimStartTrimEndToLowerContainsXyz()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.TrimStart(' ', '.', '-', '\t').TrimEnd().ToLower().Contains("xyz")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => c.S.TrimStart(Char[]:{ ' ', '.', '-', '\t' }).TrimEnd(Char[]:{ }).ToLower().Contains(\"xyz\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^[\\ \\.\\-\\t]*.*xyz.*\\s*$/is }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSToLowerEqualsConstantLowerCaseValue()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.ToLower() == "abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.ToLower() == \"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^abc$/i }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSToLowerDoesNotEqualConstantLowerCaseValue()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.ToLower() != "abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.ToLower() != \"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : { \"$not\" : /^abc$/i } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereSToLowerEqualsConstantMixedCaseValue()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.ToLower() == "Abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.ToLower() == \"Abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"_id\" : { \"$type\" : -1 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(0, Consume(query));
        }

        [Test]
        public void TestWhereSToLowerDoesNotEqualConstantMixedCaseValue()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.ToLower() != "Abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.ToLower() != \"Abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(5, Consume(query));
        }

        [Test]
        public void TestWhereSToLowerEqualsNullValue()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.ToLower() == null
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.ToLower() == null)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : null }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(3, Consume(query));
        }

        [Test]
        public void TestWhereSToLowerDoesNotEqualNullValue()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.ToLower() != null
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.ToLower() != null)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : { \"$ne\" : null } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(2, Consume(query));
        }

        [Test]
        public void TestWhereSToUpperEqualsConstantLowerCaseValue()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.ToUpper() == "abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.ToUpper() == \"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"_id\" : { \"$type\" : -1 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(0, Consume(query));
        }

        [Test]
        public void TestWhereSToUpperDoesNotEqualConstantLowerCaseValue()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.ToUpper() != "abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.ToUpper() != \"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(5, Consume(query));
        }

        [Test]
        public void TestWhereSToUpperEqualsConstantMixedCaseValue()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.ToUpper() == "Abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.ToUpper() == \"Abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"_id\" : { \"$type\" : -1 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(0, Consume(query));
        }

        [Test]
        public void TestWhereSToUpperDoesNotEqualConstantMixedCaseValue()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.ToUpper() != "Abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.ToUpper() != \"Abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(5, Consume(query));
        }

        [Test]
        public void TestWhereSToUpperEqualsNullValue()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.ToUpper() == null
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.ToUpper() == null)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : null }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(3, Consume(query));
        }

        [Test]
        public void TestWhereSToUpperDoesNotEqualNullValue()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.ToUpper() != null
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.ToUpper() != null)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : { \"$ne\" : null } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(2, Consume(query));
        }

        [Test]
        public void TestWhereSTrimStartTrimEndToLowerInvariantContainsXyz()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.TrimStart(' ', '.', '-', '\t').TrimEnd().ToLowerInvariant().Contains("xyz")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => c.S.TrimStart(Char[]:{ ' ', '.', '-', '\t' }).TrimEnd(Char[]:{ }).ToLowerInvariant().Contains(\"xyz\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^[\\ \\.\\-\\t]*.*xyz.*\\s*$/is }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSToLowerInvariantEqualsConstantLowerCaseValue()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.ToLowerInvariant() == "abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.ToLowerInvariant() == \"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : /^abc$/i }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereSToLowerInvariantDoesNotEqualConstantLowerCaseValue()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.ToLowerInvariant() != "abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.ToLowerInvariant() != \"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : { \"$not\" : /^abc$/i } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereSToLowerInvariantEqualsConstantMixedCaseValue()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.ToLowerInvariant() == "Abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.ToLowerInvariant() == \"Abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"_id\" : { \"$type\" : -1 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(0, Consume(query));
        }

        [Test]
        public void TestWhereSToLowerInvariantDoesNotEqualConstantMixedCaseValue()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.ToLowerInvariant() != "Abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.ToLowerInvariant() != \"Abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(5, Consume(query));
        }

        [Test]
        public void TestWhereSToLowerInvariantEqualsNullValue()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.ToLowerInvariant() == null
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.ToLowerInvariant() == null)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : null }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(3, Consume(query));
        }

        [Test]
        public void TestWhereSToLowerInvariantDoesNotEqualNullValue()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.ToLowerInvariant() != null
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.ToLowerInvariant() != null)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : { \"$ne\" : null } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(2, Consume(query));
        }

        [Test]
        public void TestWhereSToUpperInvariantEqualsConstantLowerCaseValue()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.ToUpperInvariant() == "abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.ToUpperInvariant() == \"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"_id\" : { \"$type\" : -1 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(0, Consume(query));
        }

        [Test]
        public void TestWhereSToUpperInvariantDoesNotEqualConstantLowerCaseValue()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.ToUpperInvariant() != "abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.ToUpperInvariant() != \"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(5, Consume(query));
        }

        [Test]
        public void TestWhereSToUpperInvariantEqualsConstantMixedCaseValue()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.ToUpperInvariant() == "Abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.ToUpperInvariant() == \"Abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"_id\" : { \"$type\" : -1 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(0, Consume(query));
        }

        [Test]
        public void TestWhereSToUpperInvariantDoesNotEqualConstantMixedCaseValue()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.ToUpperInvariant() != "Abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.ToUpperInvariant() != \"Abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(5, Consume(query));
        }

        [Test]
        public void TestWhereSToUpperInvariantEqualsNullValue()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.ToUpperInvariant() == null
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.ToUpperInvariant() == null)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : null }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(3, Consume(query));
        }

        [Test]
        public void TestWhereSToUpperInvariantDoesNotEqualNullValue()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.S.ToUpperInvariant() != null
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => (c.S.ToUpperInvariant() != null)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"s\" : { \"$ne\" : null } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(2, Consume(query));
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
            if (_server.BuildInfo.Version >= new Version(2, 0))
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
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "The indexed version of the Where query operator is not supported.")]
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
                        where c.X == 1 & c.Y == 11
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => ((c.X == 1) & (c.Y == 11))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"x\" : 1, \"y\" : 11 }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereXEquals1AndAlsoYEquals11()
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
                        where c.X == 1 | c.Y == 33
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => ((c.X == 1) | (c.Y == 33))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"$or\" : [{ \"x\" : 1 }, { \"y\" : 33 }] }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(2, Consume(query));
        }

        [Test]
        public void TestWhereXEquals1OrElseYEquals33()
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
        public void TestWhereXEquals1OrYEquals33NotNot()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !!(c.X == 1 || c.Y == 33)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !!((c.X == 1) || (c.Y == 33))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"$or\" : [{ \"x\" : 1 }, { \"y\" : 33 }] }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(2, Consume(query));
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
        public void TestWhereXGreaterThan1AndLessThan3()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where c.X > 1 && c.X < 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => ((c.X > 1) && (c.X < 3))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"x\" : { \"$gt\" : 1, \"$lt\" : 3 } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereXGreaterThan1AndLessThan3Not()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where !(c.X > 1 && c.X < 3)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsInstanceOf<SelectQuery>(translatedQuery);
            Assert.AreSame(_collection, translatedQuery.Collection);
            Assert.AreSame(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.AreEqual("(C c) => !((c.X > 1) && (c.X < 3))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.IsNull(selectQuery.OrderBy);
            Assert.IsNull(selectQuery.Projection);
            Assert.IsNull(selectQuery.Skip);
            Assert.IsNull(selectQuery.Take);

            Assert.AreEqual("{ \"$nor\" : [{ \"x\" : { \"$gt\" : 1, \"$lt\" : 3 } }] }", selectQuery.BuildQuery().ToJson());
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

            Assert.AreEqual("{ \"x\" : { \"$not\" : { \"$gt\" : 1 } } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(1, Consume(query));
        }

        [Test]
        public void TestWhereXGreaterThan1Reversed()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where 1 < c.X
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

            Assert.AreEqual("{ \"x\" : { \"$not\" : { \"$gte\" : 1 } } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(0, Consume(query));
        }

        [Test]
        public void TestWhereXGreaterThanOrEquals1Reversed()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where 1 <= c.X
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

            Assert.AreEqual("{ \"x\" : { \"$not\" : { \"$lt\" : 1 } } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(5, Consume(query));
        }

        [Test]
        public void TestWhereXLessThan1Reversed()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where 1 > c.X
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

            Assert.AreEqual("{ \"x\" : { \"$not\" : { \"$lte\" : 1 } } }", selectQuery.BuildQuery().ToJson());
            Assert.AreEqual(4, Consume(query));
        }

        [Test]
        public void TestWhereXLessThanOrEquals1Reversed()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where 1 >= c.X
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
        public void TestWhereXModOneEquals0AndXModTwoEquals0()
        {
            if (_server.BuildInfo.Version >= new Version(2, 0))
            {
                var query = from c in _collection.AsQueryable<C>()
                            where (c.X % 1 == 0) && (c.X % 2 == 0)
                            select c;

                var translatedQuery = MongoQueryTranslator.Translate(query);
                Assert.IsInstanceOf<SelectQuery>(translatedQuery);
                Assert.AreSame(_collection, translatedQuery.Collection);
                Assert.AreSame(typeof(C), translatedQuery.DocumentType);

                var selectQuery = (SelectQuery)translatedQuery;
                Assert.AreEqual("(C c) => (((c.X % 1) == 0) && ((c.X % 2) == 0))", ExpressionFormatter.ToString(selectQuery.Where));
                Assert.IsNull(selectQuery.OrderBy);
                Assert.IsNull(selectQuery.Projection);
                Assert.IsNull(selectQuery.Skip);
                Assert.IsNull(selectQuery.Take);

                Assert.AreEqual("{ \"$and\" : [{ \"x\" : { \"$mod\" : [1, 0] } }, { \"x\" : { \"$mod\" : [2, 0] } }] }", selectQuery.BuildQuery().ToJson());
                Assert.AreEqual(2, Consume(query));
            }
        }

        [Test]
        public void TestWhereXModOneEquals0AndXModTwoEquals0Not()
        {
            if (_server.BuildInfo.Version >= new Version(2, 0))
            {
                var query = from c in _collection.AsQueryable<C>()
                            where !((c.X % 1 == 0) && (c.X % 2 == 0))
                            select c;

                var translatedQuery = MongoQueryTranslator.Translate(query);
                Assert.IsInstanceOf<SelectQuery>(translatedQuery);
                Assert.AreSame(_collection, translatedQuery.Collection);
                Assert.AreSame(typeof(C), translatedQuery.DocumentType);

                var selectQuery = (SelectQuery)translatedQuery;
                Assert.AreEqual("(C c) => !(((c.X % 1) == 0) && ((c.X % 2) == 0))", ExpressionFormatter.ToString(selectQuery.Where));
                Assert.IsNull(selectQuery.OrderBy);
                Assert.IsNull(selectQuery.Projection);
                Assert.IsNull(selectQuery.Skip);
                Assert.IsNull(selectQuery.Take);

                Assert.AreEqual("{ \"$nor\" : [{ \"$and\" : [{ \"x\" : { \"$mod\" : [1, 0] } }, { \"x\" : { \"$mod\" : [2, 0] } }] }] }", selectQuery.BuildQuery().ToJson());
                Assert.AreEqual(3, Consume(query));
            }
        }

        [Test]
        public void TestWhereXModOneEquals0AndXModTwoEquals0NotNot()
        {
            if (_server.BuildInfo.Version >= new Version(2, 0))
            {
                var query = from c in _collection.AsQueryable<C>()
                            where !!((c.X % 1 == 0) && (c.X % 2 == 0))
                            select c;

                var translatedQuery = MongoQueryTranslator.Translate(query);
                Assert.IsInstanceOf<SelectQuery>(translatedQuery);
                Assert.AreSame(_collection, translatedQuery.Collection);
                Assert.AreSame(typeof(C), translatedQuery.DocumentType);

                var selectQuery = (SelectQuery)translatedQuery;
                Assert.AreEqual("(C c) => !!(((c.X % 1) == 0) && ((c.X % 2) == 0))", ExpressionFormatter.ToString(selectQuery.Where));
                Assert.IsNull(selectQuery.OrderBy);
                Assert.IsNull(selectQuery.Projection);
                Assert.IsNull(selectQuery.Skip);
                Assert.IsNull(selectQuery.Take);

                Assert.AreEqual("{ \"$or\" : [{ \"$and\" : [{ \"x\" : { \"$mod\" : [1, 0] } }, { \"x\" : { \"$mod\" : [2, 0] } }] }] }", selectQuery.BuildQuery().ToJson());
                Assert.AreEqual(2, Consume(query));
            }
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
        public void TestWhereXModTwoEquals1Reversed()
        {
            var query = from c in _collection.AsQueryable<C>()
                        where 1 == c.X % 2
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
