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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq
{
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

        private static MongoServer __server;
        private static MongoDatabase __database;
        private static MongoCollection<C> __collection;
        private static MongoCollection<SystemProfileInfo> __systemProfileCollection;
        private static Lazy<bool> __lazyOneTimeSetup = new Lazy<bool>(OneTimeSetup);

        private static ObjectId __id1 = ObjectId.GenerateNewId();
        private static ObjectId __id2 = ObjectId.GenerateNewId();
        private static ObjectId __id3 = ObjectId.GenerateNewId();
        private static ObjectId __id4 = ObjectId.GenerateNewId();
        private static ObjectId __id5 = ObjectId.GenerateNewId();

        public SelectQueryTests()
        {
            var _ = __lazyOneTimeSetup.Value;
        }

        private static bool OneTimeSetup()
        {
            __server = LegacyTestConfiguration.Server;
            __server.Connect();
            __database = LegacyTestConfiguration.Database;
            __collection = LegacyTestConfiguration.GetCollection<C>();
            __systemProfileCollection = __database.GetCollection<SystemProfileInfo>("system.profile");

            // documents inserted deliberately out of order to test sorting
            __collection.Drop();
            __collection.Insert(new C { Id = __id2, X = 2, LX = 2, Y = 11, D = new D { Z = 22 }, A = new[] { 2, 3, 4 }, DA = new List<D> { new D { Z = 111 }, new D { Z = 222 } }, L = new List<int> { 2, 3, 4 } });
            __collection.Insert(new C { Id = __id1, X = 1, LX = 1, Y = 11, D = new D { Z = 11 }, S = "abc", SA = new string[] { "Tom", "Dick", "Harry" } });
            __collection.Insert(new C { Id = __id3, X = 3, LX = 3, Y = 33, D = new D { Z = 33 }, B = true, BA = new bool[] { true }, E = E.A, EA = new E[] { E.A, E.B } });
            __collection.Insert(new C { Id = __id5, X = 5, LX = 5, Y = 44, D = new D { Z = 55 }, DBRef = new MongoDBRef("db", "c", 1) });
            __collection.Insert(new C { Id = __id4, X = 4, LX = 4, Y = 44, D = new D { Z = 44 }, S = "   xyz   ", DA = new List<D> { new D { Z = 333 }, new D { Z = 444 } } });

            return true;
        }

        [Fact]
        public void TestAggregate()
        {
            var exception = Record.Exception(() =>
                (from c in __collection.AsQueryable<C>()
                 select c).Aggregate((a, b) => null));

            var expectedMessage = "The Aggregate query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestAggregateWithAccumulator()
        {
            var exception = Record.Exception(() => 
                (from c in __collection.AsQueryable<C>()
                 select c).Aggregate<C, int>(0, (a, c) => 0));

            var expectedMessage = "The Aggregate query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestAggregateWithAccumulatorAndSelector()
        {
            var exception = Record.Exception(() =>
                (from c in __collection.AsQueryable<C>()
                 select c).Aggregate<C, int, int>(0, (a, c) => 0, a => a));

            var expectedMessage = "The Aggregate query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestAll()
        {
            var exception = Record.Exception(() =>
                (from c in __collection.AsQueryable<C>()
                 select c).All(c => true));

            var expectedMessage = "The All query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestAny()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c).Any();
            Assert.True(result);
        }

        [Fact]
        public void TestAnyWhereXEquals1()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          where c.X == 1
                          select c).Any();
            Assert.True(result);
        }

        [Fact]
        public void TestAnyWhereXEquals9()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          where c.X == 9
                          select c).Any();
            Assert.False(result);
        }

        [Fact]
        public void TestAnyWithPredicateAfterProjection()
        {
            var exception = Record.Exception(() => __collection.AsQueryable<C>().Select(c => c.Y).Any(y => y == 11));

            var expectedMessage = "Any with predicate after a projection is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestAnyWithPredicateAfterWhere()
        {
            var result = __collection.AsQueryable<C>().Where(c => c.X == 1).Any(c => c.Y == 11);
            Assert.True(result);
        }

        [Fact]
        public void TestAnyWithPredicateFalse()
        {
            var result = __collection.AsQueryable<C>().Any(c => c.X == 9);
            Assert.False(result);
        }

        [Fact]
        public void TestAnyWithPredicateTrue()
        {
            var result = __collection.AsQueryable<C>().Any(c => c.X == 1);
            Assert.True(result);
        }

        [Fact]
        public void TestAsQueryableWithNothingElse()
        {
            var query = __collection.AsQueryable<C>();
            var result = query.ToList();
            Assert.Equal(5, result.Count);
        }

        [Fact]
        public void TestAverage()
        {
            var exception = Record.Exception(() =>
                (from c in __collection.AsQueryable<C>()
                 select 1.0).Average());

            var expectedMessage = "The Average query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestAverageNullable()
        {
            var exception = Record.Exception(() =>
                (from c in __collection.AsQueryable<C>()
                 select (double?)1.0).Average());

            var expectedMessage = "The Average query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestAverageWithSelector()
        {
            var exception = Record.Exception(() =>
                (from c in __collection.AsQueryable<C>()
                 select c).Average(c => 1.0));

            var expectedMessage = "The Average query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestAverageWithSelectorNullable()
        {
            var exception = Record.Exception(() =>
                (from c in __collection.AsQueryable<C>()
                 select c).Average(c => (double?)1.0));

            var expectedMessage = "The Average query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestCast()
        {
            var query = (from c in __collection.AsQueryable<C>()
                         select c).Cast<C>();
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The Cast query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestConcat()
        {
            var source2 = new C[0];
            var query = (from c in __collection.AsQueryable<C>()
                         select c).Concat(source2);
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The Concat query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestContains()
        {
            var item = new C();
            var exception = Record.Exception(() =>
                (from c in __collection.AsQueryable<C>()
                 select c).Contains(item));

            var expectedMessage = "The Contains query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestContainsWithEqualityComparer()
        {
            var item = new C();
            var exception = Record.Exception(() =>
                (from c in __collection.AsQueryable<C>()
                 select c).Contains(item, new CEqualityComparer()));

            var expectedMessage = "The Contains query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestCountEquals2()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          where c.Y == 11
                          select c).Count();

            Assert.Equal(2, result);
        }

        [Fact]
        public void TestCountEquals5()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c).Count();

            Assert.Equal(5, result);
        }

        [Fact]
        public void TestCountWithPredicate()
        {
            var result = __collection.AsQueryable<C>().Count(c => c.Y == 11);

            Assert.Equal(2, result);
        }

        [Fact]
        public void TestCountWithPredicateAfterProjection()
        {
            var exception = Record.Exception(() => __collection.AsQueryable<C>().Select(c => c.Y).Count(y => y == 11));

            var expectedMessage = "Count with predicate after a projection is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestCountWithPredicateAfterWhere()
        {
            var result = __collection.AsQueryable<C>().Where(c => c.X == 1).Count(c => c.Y == 11);

            Assert.Equal(1, result);
        }

        [Fact]
        public void TestCountWithSkipAndTake()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c).Skip(2).Take(2).Count();

            Assert.Equal(2, result);
        }

        [Fact]
        public void TestDefaultIfEmpty()
        {
            var query = (from c in __collection.AsQueryable<C>()
                         select c).DefaultIfEmpty();
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The DefaultIfEmpty query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestDefaultIfEmptyWithDefaultValue()
        {
            var query = (from c in __collection.AsQueryable<C>()
                         select c).DefaultIfEmpty(null);
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The DefaultIfEmpty query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestDistinctASub0()
        {
            if (__server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var query = (from c in __collection.AsQueryable<C>()
                             select c.A[0]).Distinct();
                var results = query.ToList();
                Assert.Equal(1, results.Count);
                Assert.True(results.Contains(2));
            }
        }

        [Fact]
        public void TestDistinctB()
        {
            var query = (from c in __collection.AsQueryable<C>()
                         select c.B).Distinct();
            var results = query.ToList();
            Assert.Equal(2, results.Count);
            Assert.True(results.Contains(false));
            Assert.True(results.Contains(true));
        }

        [Fact]
        public void TestDistinctBASub0()
        {
            if (__server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var query = (from c in __collection.AsQueryable<C>()
                             select c.BA[0]).Distinct();
                var results = query.ToList();
                Assert.Equal(1, results.Count);
                Assert.True(results.Contains(true));
            }
        }

        [Fact]
        public void TestDistinctD()
        {
            var query = (from c in __collection.AsQueryable<C>()
                         select c.D).Distinct();
            var results = query.ToList(); // execute query
            Assert.Equal(5, results.Count);
            Assert.True(results.Contains(new D { Z = 11 }));
            Assert.True(results.Contains(new D { Z = 22 }));
            Assert.True(results.Contains(new D { Z = 33 }));
            Assert.True(results.Contains(new D { Z = 44 }));
            Assert.True(results.Contains(new D { Z = 55 }));
        }

        [Fact]
        public void TestDistinctDBRef()
        {
            var query = (from c in __collection.AsQueryable<C>()
                         select c.DBRef).Distinct();
            var results = query.ToList();
            Assert.Equal(1, results.Count);
            Assert.True(results.Contains(new MongoDBRef("db", "c", 1)));
        }

        [Fact]
        public void TestDistinctDBRefDatabase()
        {
            var query = (from c in __collection.AsQueryable<C>()
                         select c.DBRef.DatabaseName).Distinct();
            var results = query.ToList();
            Assert.Equal(1, results.Count);
            Assert.True(results.Contains("db"));
        }

        [Fact]
        public void TestDistinctDZ()
        {
            var query = (from c in __collection.AsQueryable<C>()
                         select c.D.Z).Distinct();
            var results = query.ToList();
            Assert.Equal(5, results.Count);
            Assert.True(results.Contains(11));
            Assert.True(results.Contains(22));
            Assert.True(results.Contains(33));
            Assert.True(results.Contains(44));
            Assert.True(results.Contains(55));
        }

        [Fact]
        public void TestDistinctE()
        {
            var query = (from c in __collection.AsQueryable<C>()
                         select c.E).Distinct();
            var results = query.ToList();
            Assert.Equal(1, results.Count);
            Assert.True(results.Contains(E.A));
        }

        [Fact]
        public void TestDistinctEASub0()
        {
            if (__server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var query = (from c in __collection.AsQueryable<C>()
                             select c.EA[0]).Distinct();
                var results = query.ToList();
                Assert.Equal(1, results.Count);
                Assert.True(results.Contains(E.A));
            }
        }

        [Fact]
        public void TestDistinctId()
        {
            var query = (from c in __collection.AsQueryable<C>()
                         select c.Id).Distinct();
            var results = query.ToList();
            Assert.Equal(5, results.Count);
            Assert.True(results.Contains(__id1));
            Assert.True(results.Contains(__id2));
            Assert.True(results.Contains(__id3));
            Assert.True(results.Contains(__id4));
            Assert.True(results.Contains(__id5));
        }

        [Fact]
        public void TestDistinctLSub0()
        {
            if (__server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var query = (from c in __collection.AsQueryable<C>()
                             select c.L[0]).Distinct();
                var results = query.ToList();
                Assert.Equal(1, results.Count);
                Assert.True(results.Contains(2));
            }
        }

        [Fact]
        public void TestDistinctS()
        {
            var query = (from c in __collection.AsQueryable<C>()
                         select c.S).Distinct();
            var results = query.ToList();
            Assert.Equal(2, results.Count);
            Assert.True(results.Contains("abc"));
            Assert.True(results.Contains("   xyz   "));
        }

        [Fact]
        public void TestDistinctSASub0()
        {
            if (__server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var query = (from c in __collection.AsQueryable<C>()
                             select c.SA[0]).Distinct();
                var results = query.ToList();
                Assert.Equal(1, results.Count);
                Assert.True(results.Contains("Tom"));
            }
        }

        [Fact]
        public void TestDistinctX()
        {
            var query = (from c in __collection.AsQueryable<C>()
                         select c.X).Distinct();
            var results = query.ToList();
            Assert.Equal(5, results.Count);
            Assert.True(results.Contains(1));
            Assert.True(results.Contains(2));
            Assert.True(results.Contains(3));
            Assert.True(results.Contains(4));
            Assert.True(results.Contains(5));
        }

        [Fact]
        public void TestDistinctXWithQuery()
        {
            var query = (from c in __collection.AsQueryable<C>()
                         where c.X > 3
                         select c.X).Distinct();
            var results = query.ToList();
            Assert.Equal(2, results.Count);
            Assert.True(results.Contains(4));
            Assert.True(results.Contains(5));
        }

        [Fact]
        public void TestDistinctY()
        {
            var query = (from c in __collection.AsQueryable<C>()
                         select c.Y).Distinct();
            var results = query.ToList();
            Assert.Equal(3, results.Count);
            Assert.True(results.Contains(11));
            Assert.True(results.Contains(33));
            Assert.True(results.Contains(44));
        }

        [Fact]
        public void TestDistinctWithEqualityComparer()
        {
            var query = __collection.AsQueryable<C>().Distinct(new CEqualityComparer());
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The version of the Distinct query operator with an equality comparer is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestElementAtOrDefaultWithManyMatches()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c).ElementAtOrDefault(2);

            Assert.Equal(3, result.X);
            Assert.Equal(33, result.Y);
        }

        [Fact]
        public void TestElementAtOrDefaultWithNoMatch()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          where c.X == 9
                          select c).ElementAtOrDefault(0);
            Assert.Null(result);
        }

        [Fact]
        public void TestElementAtOrDefaultWithOneMatch()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          where c.X == 3
                          select c).ElementAtOrDefault(0);

            Assert.Equal(3, result.X);
            Assert.Equal(33, result.Y);
        }

        [Fact]
        public void TestElementAtOrDefaultWithTwoMatches()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          where c.Y == 11
                          select c).ElementAtOrDefault(1);

            Assert.Equal(1, result.X);
            Assert.Equal(11, result.Y);
        }

        [Fact]
        public void TestElementAtWithManyMatches()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c).ElementAt(2);

            Assert.Equal(3, result.X);
            Assert.Equal(33, result.Y);
        }

        [Fact]
        public void TestElementAtWithNoMatch()
        {
            var exception = Record.Exception(() =>
                (from c in __collection.AsQueryable<C>()
                 where c.X == 9
                 select c).ElementAt(0));

            Assert.IsType<InvalidOperationException>(exception);
        }

        [Fact]
        public void TestElementAtWithOneMatch()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          where c.X == 3
                          select c).ElementAt(0);

            Assert.Equal(3, result.X);
            Assert.Equal(33, result.Y);
        }

        [Fact]
        public void TestElementAtWithTwoMatches()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          where c.Y == 11
                          select c).ElementAt(1);

            Assert.Equal(1, result.X);
            Assert.Equal(11, result.Y);
        }

        [Fact]
        public void TestExcept()
        {
            var source2 = new C[0];
            var query = (from c in __collection.AsQueryable<C>()
                         select c).Except(source2);
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The Except query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestExceptWithEqualityComparer()
        {
            var source2 = new C[0];
            var query = (from c in __collection.AsQueryable<C>()
                         select c).Except(source2, new CEqualityComparer());
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The Except query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestFirstOrDefaultWithManyMatches()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c).FirstOrDefault();

            Assert.Equal(2, result.X);
            Assert.Equal(11, result.Y);
        }

        [Fact]
        public void TestFirstOrDefaultWithNoMatch()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          where c.X == 9
                          select c).FirstOrDefault();
            Assert.Null(result);
        }

        [Fact]
        public void TestFirstOrDefaultWithNoMatchAndProjectionToStruct()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          where c.X == 9
                          select c.X).FirstOrDefault();
            Assert.Equal(0, result);
        }

        [Fact]
        public void TestFirstOrDefaultWithOneMatch()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          where c.X == 3
                          select c).FirstOrDefault();

            Assert.Equal(3, result.X);
            Assert.Equal(33, result.Y);
        }

        [Fact]
        public void TestFirstOrDefaultWithPredicateAfterProjection()
        {
            var exception = Record.Exception(() => __collection.AsQueryable<C>().Select(c => c.Y).FirstOrDefault(y => y == 11));

            var expectedMessage = "FirstOrDefault with predicate after a projection is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestFirstOrDefaultWithPredicateAfterWhere()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          where c.X == 1
                          select c).FirstOrDefault(c => c.Y == 11);
            Assert.Equal(1, result.X);
            Assert.Equal(11, result.Y);
        }

        [Fact]
        public void TestFirstOrDefaultWithPredicateNoMatch()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c).FirstOrDefault(c => c.X == 9);
            Assert.Null(result);
        }

        [Fact]
        public void TestFirstOrDefaultWithPredicateOneMatch()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c).FirstOrDefault(c => c.X == 3);
            Assert.Equal(3, result.X);
            Assert.Equal(33, result.Y);
        }

        [Fact]
        public void TestFirstOrDefaultWithPredicateTwoMatches()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c).FirstOrDefault(c => c.Y == 11);
            Assert.Equal(2, result.X);
            Assert.Equal(11, result.Y);
        }

        [Fact]
        public void TestFirstOrDefaultWithTwoMatches()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          where c.Y == 11
                          select c).FirstOrDefault();

            Assert.Equal(2, result.X);
            Assert.Equal(11, result.Y);
        }

        [Fact]
        public void TestFirstWithManyMatches()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c).First();

            Assert.Equal(2, result.X);
            Assert.Equal(11, result.Y);
        }

        [Fact]
        public void TestFirstWithNoMatch()
        {
            var exception = Record.Exception(() =>
                (from c in __collection.AsQueryable<C>()
                 where c.X == 9
                 select c).First());

            Assert.IsType<InvalidOperationException>(exception);
        }

        [Fact]
        public void TestFirstWithOneMatch()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          where c.X == 3
                          select c).First();

            Assert.Equal(3, result.X);
            Assert.Equal(33, result.Y);
        }

        [Fact]
        public void TestFirstWithPredicateAfterProjection()
        {
            var exception = Record.Exception(() => __collection.AsQueryable<C>().Select(c => c.Y).First(y => y == 11));

            var expectedMessage = "First with predicate after a projection is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestFirstWithPredicateAfterWhere()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          where c.X == 1
                          select c).First(c => c.Y == 11);
            Assert.Equal(1, result.X);
            Assert.Equal(11, result.Y);
        }

        [Fact]
        public void TestFirstWithPredicateNoMatch()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                (from c in __collection.AsQueryable<C>()
                 select c).First(c => c.X == 9);
            });
            Assert.Equal(ExpectedErrorMessage.FirstEmptySequence, ex.Message);
        }

        [Fact]
        public void TestFirstWithPredicateOneMatch()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c).First(c => c.X == 3);
            Assert.Equal(3, result.X);
            Assert.Equal(33, result.Y);
        }

        [Fact]
        public void TestFirstWithPredicateTwoMatches()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c).First(c => c.Y == 11);
            Assert.Equal(2, result.X);
            Assert.Equal(11, result.Y);
        }

        [Fact]
        public void TestFirstWithTwoMatches()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          where c.Y == 11
                          select c).First();

            Assert.Equal(2, result.X);
            Assert.Equal(11, result.Y);
        }

        [Fact]
        public void TestGroupByWithKeySelector()
        {
            var query = (from c in __collection.AsQueryable<C>()
                         select c).GroupBy(c => c);
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The GroupBy query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestGroupByWithKeySelectorAndElementSelector()
        {
            var query = (from c in __collection.AsQueryable<C>()
                         select c).GroupBy(c => c, c => c);
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The GroupBy query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestGroupByWithKeySelectorAndElementSelectorAndEqualityComparer()
        {
            var query = (from c in __collection.AsQueryable<C>()
                         select c).GroupBy(c => c, c => c, new CEqualityComparer());
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The GroupBy query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestGroupByWithKeySelectorAndElementSelectorAndResultSelector()
        {
            var query = (from c in __collection.AsQueryable<C>()
                         select c).GroupBy(c => c, c => c, (c, e) => 1.0);
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The GroupBy query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestGroupByWithKeySelectorAndElementSelectorAndResultSelectorAndEqualityComparer()
        {
            var query = (from c in __collection.AsQueryable<C>()
                         select c).GroupBy(c => c, c => c, (c, e) => e.First(), new CEqualityComparer());
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The GroupBy query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestGroupByWithKeySelectorAndEqualityComparer()
        {
            var query = (from c in __collection.AsQueryable<C>()
                         select c).GroupBy(c => c, new CEqualityComparer());
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The GroupBy query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestGroupByWithKeySelectorAndResultSelector()
        {
            var query = (from c in __collection.AsQueryable<C>()
                         select c).GroupBy(c => c, (k, e) => 1.0);
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The GroupBy query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestGroupByWithKeySelectorAndResultSelectorAndEqualityComparer()
        {
            var query = (from c in __collection.AsQueryable<C>()
                         select c).GroupBy(c => c, (k, e) => e.First(), new CEqualityComparer());
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The GroupBy query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestGroupJoin()
        {
            var inner = new C[0];
            var query = __collection.AsQueryable<C>().GroupJoin(inner, c => c, c => c, (c, e) => c);
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The GroupJoin query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestGroupJoinWithEqualityComparer()
        {
            var inner = new C[0];
            var query = __collection.AsQueryable<C>().GroupJoin(inner, c => c, c => c, (c, e) => c, new CEqualityComparer());
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The GroupJoin query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestIntersect()
        {
            var source2 = new C[0];
            var query = (from c in __collection.AsQueryable<C>()
                         select c).Intersect(source2);
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The Intersect query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestIntersectWithEqualityComparer()
        {
            var source2 = new C[0];
            var query = (from c in __collection.AsQueryable<C>()
                         select c).Intersect(source2, new CEqualityComparer());
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The Intersect query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestJoin()
        {
            var query = __collection.AsQueryable<C>().Join(__collection.AsQueryable<C>(), c => c.X, c => c.X, (x, y) => x);
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The Join query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestJoinWithEqualityComparer()
        {
            var query = __collection.AsQueryable<C>().Join(__collection.AsQueryable<C>(), c => c.X, c => c.X, (x, y) => x, new Int32EqualityComparer());
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The Join query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestLastOrDefaultWithManyMatches()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c).LastOrDefault();

            Assert.Equal(4, result.X);
            Assert.Equal(44, result.Y);
        }

        [Fact]
        public void TestLastOrDefaultWithNoMatch()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          where c.X == 9
                          select c).LastOrDefault();
            Assert.Null(result);
        }

        [Fact]
        public void TestLastOrDefaultWithOneMatch()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          where c.X == 3
                          select c).LastOrDefault();

            Assert.Equal(3, result.X);
            Assert.Equal(33, result.Y);
        }

        [Fact]
        public void TestLastOrDefaultWithOrderBy()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          orderby c.X
                          select c).LastOrDefault();

            Assert.Equal(5, result.X);
            Assert.Equal(44, result.Y);
        }

        [Fact]
        public void TestLastOrDefaultWithPredicateAfterProjection()
        {
            var exception = Record.Exception(() => __collection.AsQueryable<C>().Select(c => c.Y).LastOrDefault(y => y == 11));

            var expectedMessage = "LastOrDefault with predicate after a projection is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestLastOrDefaultWithPredicateAfterWhere()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          where c.X == 1
                          select c).LastOrDefault(c => c.Y == 11);
            Assert.Equal(1, result.X);
            Assert.Equal(11, result.Y);
        }

        [Fact]
        public void TestLastOrDefaultWithPredicateNoMatch()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c).LastOrDefault(c => c.X == 9);
            Assert.Null(result);
        }

        [Fact]
        public void TestLastOrDefaultWithPredicateOneMatch()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c).LastOrDefault(c => c.X == 3);
            Assert.Equal(3, result.X);
            Assert.Equal(33, result.Y);
        }

        [Fact]
        public void TestLastOrDefaultWithPredicateTwoMatches()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c).LastOrDefault(c => c.Y == 11);
            Assert.Equal(1, result.X);
            Assert.Equal(11, result.Y);
        }

        [Fact]
        public void TestLastOrDefaultWithTwoMatches()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          where c.Y == 11
                          select c).LastOrDefault();

            Assert.Equal(1, result.X);
            Assert.Equal(11, result.Y);
        }

        [Fact]
        public void TestLastWithManyMatches()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c).Last();

            Assert.Equal(4, result.X);
            Assert.Equal(44, result.Y);
        }

        [Fact]
        public void TestLastWithNoMatch()
        {
            var exception = Record.Exception(() =>
                (from c in __collection.AsQueryable<C>()
                 where c.X == 9
                 select c).Last());

            Assert.IsType<InvalidOperationException>(exception);
        }

        [Fact]
        public void TestLastWithOneMatch()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          where c.X == 3
                          select c).Last();

            Assert.Equal(3, result.X);
            Assert.Equal(33, result.Y);
        }

        [Fact]
        public void TestLastWithPredicateAfterProjection()
        {
            var exception = Record.Exception(() => __collection.AsQueryable<C>().Select(c => c.Y).Last(y => y == 11));

            var expectedMessage = "Last with predicate after a projection is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestLastWithPredicateAfterWhere()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          where c.X == 1
                          select c).Last(c => c.Y == 11);
            Assert.Equal(1, result.X);
            Assert.Equal(11, result.Y);
        }

        [Fact]
        public void TestLastWithPredicateNoMatch()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                (from c in __collection.AsQueryable<C>()
                 select c).Last(c => c.X == 9);
            });
        }

        [Fact]
        public void TestLastWithPredicateOneMatch()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c).Last(c => c.X == 3);
            Assert.Equal(3, result.X);
            Assert.Equal(33, result.Y);
        }

        [Fact]
        public void TestLastWithPredicateTwoMatches()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c).Last(c => c.Y == 11);
            Assert.Equal(1, result.X);
            Assert.Equal(11, result.Y);
        }

        [Fact]
        public void TestLastWithOrderBy()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          orderby c.X
                          select c).Last();

            Assert.Equal(5, result.X);
            Assert.Equal(44, result.Y);
        }

        [Fact]
        public void TestLastWithTwoMatches()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          where c.Y == 11
                          select c).Last();

            Assert.Equal(1, result.X);
            Assert.Equal(11, result.Y);
        }

        [Fact]
        public void TestLongCountEquals2()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          where c.Y == 11
                          select c).LongCount();

            Assert.Equal(2L, result);
        }

        [Fact]
        public void TestLongCountEquals5()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c).LongCount();

            Assert.Equal(5L, result);
        }

        [Fact]
        public void TestLongCountWithSkipAndTake()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c).Skip(2).Take(2).LongCount();

            Assert.Equal(2L, result);
        }

        [Fact]
        public void TestMaxDZWithProjection()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c.D.Z).Max();
            Assert.Equal(55, result);
        }

        [Fact]
        public void TestMaxDZWithSelector()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c).Max(c => c.D.Z);
            Assert.Equal(55, result);
        }

        [Fact]
        public void TestMaxWithProjectionAndSelector()
        {
            var exception = Record.Exception(() =>
                (from c in __collection.AsQueryable<C>()
                 select c.D).Max(d => d.Z));

            var expectedMessage = "Max must be used with either Select or a selector argument, but not both.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestMaxXWithProjection()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c.X).Max();
            Assert.Equal(5, result);
        }

        [Fact]
        public void TestMaxXWithSelector()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c).Max(c => c.X);
            Assert.Equal(5, result);
        }

        [Fact]
        public void TestMaxXYWithProjection()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select new { c.X, c.Y }).Max();
            Assert.Equal(5, result.X);
            Assert.Equal(44, result.Y);
        }

        [Fact]
        public void TestMaxXYWithSelector()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c).Max(c => new { c.X, c.Y });
            Assert.Equal(5, result.X);
            Assert.Equal(44, result.Y);
        }

        [Fact]
        public void TestMinDZWithProjection()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c.D.Z).Min();
            Assert.Equal(11, result);
        }

        [Fact]
        public void TestMinDZWithSelector()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c).Min(c => c.D.Z);
            Assert.Equal(11, result);
        }

        [Fact]
        public void TestMinWithProjectionAndSelector()
        {
            var exception = Record.Exception(() =>
                (from c in __collection.AsQueryable<C>()
                 select c.D).Min(d => d.Z));

            var expectedMessage = "Min must be used with either Select or a selector argument, but not both.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestMinXWithProjection()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c.X).Min();
            Assert.Equal(1, result);
        }

        [Fact]
        public void TestMinXWithSelector()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c).Min(c => c.X);
            Assert.Equal(1, result);
        }

        [Fact]
        public void TestMinXYWithProjection()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select new { c.X, c.Y }).Min();
            Assert.Equal(1, result.X);
            Assert.Equal(11, result.Y);
        }

        [Fact]
        public void TestMinXYWithSelector()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c).Min(c => new { c.X, c.Y });
            Assert.Equal(1, result.X);
            Assert.Equal(11, result.Y);
        }

        [Fact]
        public void TestOrderByValueTypeWithObjectReturnType()
        {
            Expression<Func<C, object>> orderByClause = c => c.LX;
            var query = __collection.AsQueryable<C>().OrderBy(orderByClause);

            RunTestOrderByValueTypeWithMismatchingType(query, "(C c) => (Object)c.LX");
        }

        [Fact]
        public void TestOrderByValueTypeWithIComparableReturnType()
        {
            Expression<Func<C, IComparable>> orderByClause = c => c.LX;
            var query = __collection.AsQueryable<C>().OrderBy(orderByClause);

            RunTestOrderByValueTypeWithMismatchingType(query, "(C c) => (IComparable)c.LX");
        }

        private void RunTestOrderByValueTypeWithMismatchingType(IOrderedQueryable query, string orderByString)
        {
            var mongoQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(mongoQuery);
            var selectQuery = (SelectQuery)mongoQuery;
            Assert.Equal(orderByString, ExpressionFormatter.ToString(selectQuery.OrderBy[0].Key));
        }

        [Fact]
        public void TestOrderByAscending()
        {
            var query = from c in __collection.AsQueryable<C>()
                        orderby c.X
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Null(selectQuery.Where);
            Assert.Equal(1, selectQuery.OrderBy.Count);
            Assert.Equal("(C c) => c.X", ExpressionFormatter.ToString(selectQuery.OrderBy[0].Key));
            Assert.Equal(OrderByDirection.Ascending, selectQuery.OrderBy[0].Direction);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Null(selectQuery.BuildQuery());
            var results = query.ToList();
            Assert.Equal(5, results.Count);
            Assert.Equal(1, results.First().X);
            Assert.Equal(5, results.Last().X);
        }

        [Fact]
        public void TestOrderByAscendingThenByAscending()
        {
            var query = from c in __collection.AsQueryable<C>()
                        orderby c.Y, c.X
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Null(selectQuery.Where);
            Assert.Equal(2, selectQuery.OrderBy.Count);
            Assert.Equal("(C c) => c.Y", ExpressionFormatter.ToString(selectQuery.OrderBy[0].Key));
            Assert.Equal(OrderByDirection.Ascending, selectQuery.OrderBy[0].Direction);
            Assert.Equal("(C c) => c.X", ExpressionFormatter.ToString(selectQuery.OrderBy[1].Key));
            Assert.Equal(OrderByDirection.Ascending, selectQuery.OrderBy[1].Direction);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Null(selectQuery.BuildQuery());
            var results = query.ToList();
            Assert.Equal(5, results.Count);
            Assert.Equal(1, results.First().X);
            Assert.Equal(5, results.Last().X);
        }

        [Fact]
        public void TestOrderByAscendingThenByDescending()
        {
            var query = from c in __collection.AsQueryable<C>()
                        orderby c.Y, c.X descending
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Null(selectQuery.Where);
            Assert.Equal(2, selectQuery.OrderBy.Count);
            Assert.Equal("(C c) => c.Y", ExpressionFormatter.ToString(selectQuery.OrderBy[0].Key));
            Assert.Equal(OrderByDirection.Ascending, selectQuery.OrderBy[0].Direction);
            Assert.Equal("(C c) => c.X", ExpressionFormatter.ToString(selectQuery.OrderBy[1].Key));
            Assert.Equal(OrderByDirection.Descending, selectQuery.OrderBy[1].Direction);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Null(selectQuery.BuildQuery());
            var results = query.ToList();
            Assert.Equal(5, results.Count);
            Assert.Equal(2, results.First().X);
            Assert.Equal(4, results.Last().X);
        }

        [Fact]
        public void TestOrderByDescending()
        {
            var query = from c in __collection.AsQueryable<C>()
                        orderby c.X descending
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Null(selectQuery.Where);
            Assert.Equal(1, selectQuery.OrderBy.Count);
            Assert.Equal("(C c) => c.X", ExpressionFormatter.ToString(selectQuery.OrderBy[0].Key));
            Assert.Equal(OrderByDirection.Descending, selectQuery.OrderBy[0].Direction);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Null(selectQuery.BuildQuery());
            var results = query.ToList();
            Assert.Equal(5, results.Count);
            Assert.Equal(5, results.First().X);
            Assert.Equal(1, results.Last().X);
        }

        [Fact]
        public void TestOrderByDescendingThenByAscending()
        {
            var query = from c in __collection.AsQueryable<C>()
                        orderby c.Y descending, c.X
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Null(selectQuery.Where);
            Assert.Equal(2, selectQuery.OrderBy.Count);
            Assert.Equal("(C c) => c.Y", ExpressionFormatter.ToString(selectQuery.OrderBy[0].Key));
            Assert.Equal(OrderByDirection.Descending, selectQuery.OrderBy[0].Direction);
            Assert.Equal("(C c) => c.X", ExpressionFormatter.ToString(selectQuery.OrderBy[1].Key));
            Assert.Equal(OrderByDirection.Ascending, selectQuery.OrderBy[1].Direction);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Null(selectQuery.BuildQuery());
            var results = query.ToList();
            Assert.Equal(5, results.Count);
            Assert.Equal(4, results.First().X);
            Assert.Equal(2, results.Last().X);
        }

        [Fact]
        public void TestOrderByDescendingThenByDescending()
        {
            var query = from c in __collection.AsQueryable<C>()
                        orderby c.Y descending, c.X descending
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Null(selectQuery.Where);
            Assert.Equal(2, selectQuery.OrderBy.Count);
            Assert.Equal("(C c) => c.Y", ExpressionFormatter.ToString(selectQuery.OrderBy[0].Key));
            Assert.Equal(OrderByDirection.Descending, selectQuery.OrderBy[0].Direction);
            Assert.Equal("(C c) => c.X", ExpressionFormatter.ToString(selectQuery.OrderBy[1].Key));
            Assert.Equal(OrderByDirection.Descending, selectQuery.OrderBy[1].Direction);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Null(selectQuery.BuildQuery());
            var results = query.ToList();
            Assert.Equal(5, results.Count);
            Assert.Equal(5, results.First().X);
            Assert.Equal(1, results.Last().X);
        }

        [Fact]
        public void TestOrderByDuplicate()
        {
            var query = from c in __collection.AsQueryable<C>()
                        orderby c.X
                        orderby c.Y
                        select c;

            var exception = Record.Exception(() => MongoQueryTranslator.Translate(query));

            var expectedMessage = "Only one OrderBy or OrderByDescending clause is allowed (use ThenBy or ThenByDescending for multiple order by clauses).";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestProjection()
        {
            var query = from c in __collection.AsQueryable<C>()
                        select c.X;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Null(selectQuery.Where);
            Assert.Null(selectQuery.OrderBy);
            Assert.Equal("(C c) => c.X", ExpressionFormatter.ToString(selectQuery.Projection));
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Null(selectQuery.BuildQuery());

            var results = query.ToList();
            Assert.Equal(5, results.Count);
            Assert.Equal(2, results.First());
            Assert.Equal(4, results.Last());
        }

        [Fact]
        public void TestReverse()
        {
            var query = (from c in __collection.AsQueryable<C>()
                         select c).Reverse();
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The Reverse query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestSelect()
        {
            var query = from c in __collection.AsQueryable<C>()
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Null(selectQuery.Where);
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Null(selectQuery.BuildQuery());
            Assert.Equal(5, Consume(query));
        }

        [Fact]
        public void TestSelectMany()
        {
            var query = __collection.AsQueryable<C>().SelectMany(c => new C[] { c });
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The SelectMany query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestSelectManyWithIndex()
        {
            var query = __collection.AsQueryable<C>().SelectMany((c, index) => new C[] { c });
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The SelectMany query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestSelectManyWithIntermediateResults()
        {
            var query = __collection.AsQueryable<C>().SelectMany(c => new C[] { c }, (c, i) => i);
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The SelectMany query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestSelectManyWithIndexAndIntermediateResults()
        {
            var query = __collection.AsQueryable<C>().SelectMany((c, index) => new C[] { c }, (c, i) => i);
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The SelectMany query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestSelectWithIndex()
        {
            var query = __collection.AsQueryable<C>().Select((c, index) => c);
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The indexed version of the Select query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestSelectWithNothingElse()
        {
            var query = from c in __collection.AsQueryable<C>() select c;
            var result = query.ToList();
            Assert.Equal(5, result.Count);
        }

        [Fact]
        public void TestSequenceEqual()
        {
            var source2 = new C[0];
            var exception = Record.Exception(() =>
                (from c in __collection.AsQueryable<C>()
                 select c).SequenceEqual(source2));

            var expectedMessage = "The SequenceEqual query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestSequenceEqualtWithEqualityComparer()
        {
            var source2 = new C[0];
            var exception = Record.Exception(() =>
                (from c in __collection.AsQueryable<C>()
                 select c).SequenceEqual(source2, new CEqualityComparer()));

            var expectedMessage = "The SequenceEqual query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestSingleOrDefaultWithManyMatches()
        {
            var exception = Record.Exception(() =>
                (from c in __collection.AsQueryable<C>()
                 select c).SingleOrDefault());

            Assert.IsType<InvalidOperationException>(exception);
        }

        [Fact]
        public void TestSingleOrDefaultWithNoMatch()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          where c.X == 9
                          select c).SingleOrDefault();
            Assert.Null(result);
        }

        [Fact]
        public void TestSingleOrDefaultWithOneMatch()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          where c.X == 3
                          select c).SingleOrDefault();

            Assert.Equal(3, result.X);
            Assert.Equal(33, result.Y);
        }

        [Fact]
        public void TestSingleOrDefaultWithPredicateAfterProjection()
        {
            var exception = Record.Exception(() => __collection.AsQueryable<C>().Select(c => c.Y).SingleOrDefault(y => y == 11));

            var expectedMessage = "SingleOrDefault with predicate after a projection is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestSingleOrDefaultWithPredicateAfterWhere()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          where c.X == 1
                          select c).SingleOrDefault(c => c.Y == 11);
            Assert.Equal(1, result.X);
            Assert.Equal(11, result.Y);
        }

        [Fact]
        public void TestSingleOrDefaultWithPredicateNoMatch()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c).SingleOrDefault(c => c.X == 9);
            Assert.Null(result);
        }

        [Fact]
        public void TestSingleOrDefaultWithPredicateOneMatch()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c).SingleOrDefault(c => c.X == 3);
            Assert.Equal(3, result.X);
            Assert.Equal(33, result.Y);
        }

        [Fact]
        public void TestSingleOrDefaultWithPredicateTwoMatches()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                (from c in __collection.AsQueryable<C>()
                 select c).SingleOrDefault(c => c.Y == 11);
            });
            Assert.Equal(ExpectedErrorMessage.SingleLongSequence, ex.Message);
        }

        [Fact]
        public void TestSingleOrDefaultWithTwoMatches()
        {
            var exception = Record.Exception(() =>
                (from c in __collection.AsQueryable<C>()
                 where c.Y == 11
                 select c).SingleOrDefault());

            Assert.IsType<InvalidOperationException>(exception);
        }

        [Fact]
        public void TestSingleWithManyMatches()
        {
            var exception = Record.Exception(() =>
                (from c in __collection.AsQueryable<C>()
                 select c).Single());

            Assert.IsType<InvalidOperationException>(exception);
        }

        [Fact]
        public void TestSingleWithNoMatch()
        {
            var exception = Record.Exception(() =>
                (from c in __collection.AsQueryable<C>()
                 where c.X == 9
                 select c).Single());

            Assert.IsType<InvalidOperationException>(exception);
        }

        [Fact]
        public void TestSingleWithOneMatch()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          where c.X == 3
                          select c).Single();

            Assert.Equal(3, result.X);
            Assert.Equal(33, result.Y);
        }

        [Fact]
        public void TestSingleWithPredicateAfterProjection()
        {
            var exception = Record.Exception(() => __collection.AsQueryable<C>().Select(c => c.Y).Single(y => y == 11));

            var expectedMessage = "Single with predicate after a projection is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestSingleWithPredicateAfterWhere()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          where c.X == 1
                          select c).Single(c => c.Y == 11);
            Assert.Equal(1, result.X);
            Assert.Equal(11, result.Y);
        }

        [Fact]
        public void TestSingleWithPredicateNoMatch()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                (from c in __collection.AsQueryable<C>()
                 select c).Single(c => c.X == 9);
            });
            Assert.Equal(ExpectedErrorMessage.SingleEmptySequence, ex.Message);
        }

        [Fact]
        public void TestSingleWithPredicateOneMatch()
        {
            var result = (from c in __collection.AsQueryable<C>()
                          select c).Single(c => c.X == 3);
            Assert.Equal(3, result.X);
            Assert.Equal(33, result.Y);
        }

        [Fact]
        public void TestSingleWithPredicateTwoMatches()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                (from c in __collection.AsQueryable<C>()
                 select c).Single(c => c.Y == 11);
            });
            Assert.Equal(ExpectedErrorMessage.SingleLongSequence, ex.Message);
        }

        [Fact]
        public void TestSingleWithTwoMatches()
        {
            var exception = Record.Exception(() =>
                (from c in __collection.AsQueryable<C>()
                 where c.Y == 11
                 select c).Single());

            Assert.IsType<InvalidOperationException>(exception);
        }

        [Fact]
        public void TestSkip2()
        {
            var query = (from c in __collection.AsQueryable<C>()
                         select c).Skip(2);

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Null(selectQuery.Where);
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Equal(2, selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Null(selectQuery.BuildQuery());
            Assert.Equal(3, Consume(query));
        }

        [Fact]
        public void TestSkipWhile()
        {
            var query = __collection.AsQueryable<C>().SkipWhile(c => true);
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The SkipWhile query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestSum()
        {
            var exception = Record.Exception(() =>
                (from c in __collection.AsQueryable<C>()
                 select 1.0).Sum());

            var expectedMessage = "The Sum query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestSumNullable()
        {
            var exception = Record.Exception(() =>
                (from c in __collection.AsQueryable<C>()
                 select (double?)1.0).Sum());

            var expectedMessage = "The Sum query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestSumWithSelector()
        {
            var exception = Record.Exception(() =>
                (from c in __collection.AsQueryable<C>()
                 select c).Sum(c => 1.0));

            var expectedMessage = "The Sum query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestSumWithSelectorNullable()
        {
            var exception = Record.Exception(() =>
                (from c in __collection.AsQueryable<C>()
                 select c).Sum(c => (double?)1.0));

            var expectedMessage = "The Sum query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestTake2()
        {
            var query = (from c in __collection.AsQueryable<C>()
                         select c).Take(2);

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Null(selectQuery.Where);
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Equal(2, selectQuery.Take);

            Assert.Null(selectQuery.BuildQuery());
            Assert.Equal(2, Consume(query));
        }

        [Fact]
        public void TestTakeWhile()
        {
            var query = __collection.AsQueryable<C>().TakeWhile(c => true);
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The TakeWhile query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestThenByWithMissingOrderBy()
        {
            // not sure this could ever happen in real life without deliberate sabotaging like with this cast
            var query = ((IOrderedQueryable<C>)__collection.AsQueryable<C>())
                .ThenBy(c => c.X);

            var exception = Record.Exception(() => MongoQueryTranslator.Translate(query));

            var expectedMessage = "ThenBy or ThenByDescending can only be used after OrderBy or OrderByDescending.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestUnion()
        {
            var source2 = new C[0];
            var query = (from c in __collection.AsQueryable<C>()
                         select c).Union(source2);
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The Union query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestUnionWithEqualityComparer()
        {
            var source2 = new C[0];
            var query = (from c in __collection.AsQueryable<C>()
                         select c).Union(source2, new CEqualityComparer());
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The Union query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestWhereAAny()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.A.Any()
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => Enumerable.Any<Int32>(c.A)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"a\" : { \"$ne\" : null, \"$not\" : { \"$size\" : 0 } } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereAAnyWithPredicate()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.A.Any(a => a > 3)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => Enumerable.Any<Int32>(c.A, (Int32 a) => (a > 3))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            var exception = Record.Exception(() => selectQuery.BuildQuery());

            var expectedMessage = "Any is only support for items that serialize into documents. The current serializer is Int32Serializer and must implement IBsonDocumentSerializer for participation in Any queries.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestWhereLocalListContainsX()
        {
            var local = new List<int> { 1, 2, 3 };

            var query = from c in __collection.AsQueryable<C>()
                        where local.Contains(c.X)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => System.Collections.Generic.List`1[System.Int32].Contains(c.X)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : { \"$in\" : [1, 2, 3] } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(3, Consume(query));
        }

        [Fact]
        public void TestWhereLocalArrayContainsX()
        {
            var local = new[] { 1, 2, 3 };

            var query = from c in __collection.AsQueryable<C>()
                        where local.Contains(c.X)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => Enumerable.Contains<Int32>(Int32[]:{ 1, 2, 3 }, c.X)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : { \"$in\" : [1, 2, 3] } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(3, Consume(query));
        }

        [Fact]
        public void TestWhereLocalIListContainsX()
        {
            // this will generate a non-list, non-array.
            IList<int> local = new[] { 1, 2, 3 };

            var query = from c in __collection.AsQueryable<C>()
                        where local.Contains(c.X)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => Int32[]:{ 1, 2, 3 }.Contains(c.X)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : { \"$in\" : [1, 2, 3] } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(3, Consume(query));
        }

        [Fact]
        public void TestWhereAContains2()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.A.Contains(2)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => Enumerable.Contains<Int32>(c.A, 2)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"a\" : 2 }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereAContains2Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !c.A.Contains(2)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !Enumerable.Contains<Int32>(c.A, 2)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"a\" : { \"$ne\" : 2 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereAContainsAll()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.A.ContainsAll(new[] { 2, 3 })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => LinqToMongo.ContainsAll<Int32>(c.A, Int32[]:{ 2, 3 })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"a\" : { \"$all\" : [2, 3] } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereAContainsAllNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !c.A.ContainsAll(new[] { 2, 3 })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !LinqToMongo.ContainsAll<Int32>(c.A, Int32[]:{ 2, 3 })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"a\" : { \"$not\" : { \"$all\" : [2, 3] } } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereAContainsAny()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.A.ContainsAny(new[] { 2, 3 })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => LinqToMongo.ContainsAny<Int32>(c.A, Int32[]:{ 2, 3 })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"a\" : { \"$in\" : [2, 3] } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereAContainsAnyNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !c.A.ContainsAny(new[] { 1, 2 })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !LinqToMongo.ContainsAny<Int32>(c.A, Int32[]:{ 1, 2 })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"a\" : { \"$nin\" : [1, 2] } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereAExistsFalse()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where Query.NotExists("a").Inject()
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => LinqToMongo.Inject({ \"a\" : { \"$exists\" : false } })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"a\" : { \"$exists\" : false } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereAExistsTrue()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where Query.Exists("a").Inject()
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => LinqToMongo.Inject({ \"a\" : { \"$exists\" : true } })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"a\" : { \"$exists\" : true } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereAExistsTrueNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !Query.Exists("a").Inject()
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !LinqToMongo.Inject({ \"a\" : { \"$exists\" : true } })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"a\" : { \"$exists\" : false } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereALengthEquals3()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.A.Length == 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.A.Length == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"a\" : { \"$size\" : 3 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereALengthEquals3Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.A.Length == 3)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !(c.A.Length == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"a\" : { \"$not\" : { \"$size\" : 3 } } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereALengthEquals3Reversed()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where 3 == c.A.Length
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.A.Length == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"a\" : { \"$size\" : 3 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereALengthNotEquals3()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.A.Length != 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.A.Length != 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"a\" : { \"$not\" : { \"$size\" : 3 } } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereALengthNotEquals3Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.A.Length != 3)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !(c.A.Length != 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"a\" : { \"$size\" : 3 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereASub1Equals3()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.A[1] == 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.A[1] == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"a.1\" : 3 }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereASub1Equals3Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.A[1] == 3)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !(c.A[1] == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"a.1\" : { \"$ne\" : 3 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereASub1ModTwoEquals1()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.A[1] % 2 == 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => ((c.A[1] % 2) == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"a.1\" : { \"$mod\" : [2, 1] } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereASub1ModTwoEquals1Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.A[1] % 2 == 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !((c.A[1] % 2) == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"a.1\" : { \"$not\" : { \"$mod\" : [2, 1] } } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereASub1ModTwoNotEquals1()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.A[1] % 2 != 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => ((c.A[1] % 2) != 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"a.1\" : { \"$not\" : { \"$mod\" : [2, 1] } } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereASub1ModTwoNotEquals1Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.A[1] % 2 != 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !((c.A[1] % 2) != 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"a.1\" : { \"$mod\" : [2, 1] } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereASub1NotEquals3()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.A[1] != 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.A[1] != 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"a.1\" : { \"$ne\" : 3 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereASub1NotEquals3Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.A[1] != 3)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !(c.A[1] != 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"a.1\" : 3 }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereB()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.B
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => c.B", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"b\" : true }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereBASub0()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.BA[0]
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => c.BA[0]", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"ba.0\" : true }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereBASub0EqualsFalse()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.BA[0] == false
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.BA[0] == false)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"ba.0\" : false }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(0, Consume(query));
        }

        [Fact]
        public void TestWhereBASub0EqualsFalseNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.BA[0] == false)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !(c.BA[0] == false)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"ba.0\" : { \"$ne\" : false } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(5, Consume(query));
        }

        [Fact]
        public void TestWhereBASub0EqualsTrue()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.BA[0] == true
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.BA[0] == true)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"ba.0\" : true }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereBASub0EqualsTrueNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.BA[0] == true)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !(c.BA[0] == true)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"ba.0\" : { \"$ne\" : true } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereBASub0Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !c.BA[0]
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !c.BA[0]", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"ba.0\" : { \"$ne\" : true } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereBEqualsFalse()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.B == false
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.B == false)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"b\" : false }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereBEqualsFalseNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.B == false)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !(c.B == false)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"b\" : { \"$ne\" : false } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereBEqualsTrue()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.B == true
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.B == true)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"b\" : true }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereBEqualsTrueNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.B == true)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !(c.B == true)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"b\" : { \"$ne\" : true } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereBNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !c.B
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !c.B", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"b\" : { \"$ne\" : true } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereDBRefCollectionNameEqualsC()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.DBRef.CollectionName == "c"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.DBRef.CollectionName == \"c\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"dbref.$ref\" : \"c\" }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereDBRefDatabaseNameEqualsDb()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.DBRef.DatabaseName == "db"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.DBRef.DatabaseName == \"db\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"dbref.$db\" : \"db\" }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereDBRefEquals()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.DBRef == new MongoDBRef("db", "c", 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.DBRef == new MongoDBRef(\"db\", \"c\", 1))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"dbref\" : { \"$ref\" : \"c\", \"$id\" : 1, \"$db\" : \"db\" } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereDBRefEqualsNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.DBRef == new MongoDBRef("db", "c", 1))
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !(c.DBRef == new MongoDBRef(\"db\", \"c\", 1))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"dbref\" : { \"$ne\" : { \"$ref\" : \"c\", \"$id\" : 1, \"$db\" : \"db\" } } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereDBRefNotEquals()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.DBRef != new MongoDBRef("db", "c", 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.DBRef != new MongoDBRef(\"db\", \"c\", 1))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"dbref\" : { \"$ne\" : { \"$ref\" : \"c\", \"$id\" : 1, \"$db\" : \"db\" } } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereDBRefNotEqualsNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.DBRef != new MongoDBRef("db", "c", 1))
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !(c.DBRef != new MongoDBRef(\"db\", \"c\", 1))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"dbref\" : { \"$ref\" : \"c\", \"$id\" : 1, \"$db\" : \"db\" } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereDBRefIdEquals1()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.DBRef.Id == 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.DBRef.Id == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"dbref.$id\" : 1 }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereDEquals11()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.D == new D { Z = 11 }
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.D == new D { Z = 11 })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"d\" : { \"z\" : 11 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereDEquals11Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.D == new D { Z = 11 })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !(c.D == new D { Z = 11 })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"d\" : { \"$ne\" : { \"z\" : 11 } } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereDNotEquals11()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.D != new D { Z = 11 }
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.D != new D { Z = 11 })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"d\" : { \"$ne\" : { \"z\" : 11 } } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereDNotEquals11Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.D != new D { Z = 11 })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !(c.D != new D { Z = 11 })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"d\" : { \"z\" : 11 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereDAAnyWithPredicate()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.DA.Any(d => d.Z == 333)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => Enumerable.Any<D>(c.DA, (D d) => (d.Z == 333))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"da\" : { \"$elemMatch\" : { \"z\" : 333 } } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereEAContainsAll()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.EA.ContainsAll(new E[] { E.A, E.B })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => LinqToMongo.ContainsAll<E>(c.EA, E[]:{ E.A, E.B })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"ea\" : { \"$all\" : [1, 2] } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereEAContainsAllNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !c.EA.ContainsAll(new E[] { E.A, E.B })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !LinqToMongo.ContainsAll<E>(c.EA, E[]:{ E.A, E.B })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"ea\" : { \"$not\" : { \"$all\" : [1, 2] } } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereEAContainsAny()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.EA.ContainsAny(new[] { E.A, E.B })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => LinqToMongo.ContainsAny<E>(c.EA, E[]:{ E.A, E.B })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"ea\" : { \"$in\" : [1, 2] } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereEAContainsAnyNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !c.EA.ContainsAny(new[] { E.A, E.B })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !LinqToMongo.ContainsAny<E>(c.EA, E[]:{ E.A, E.B })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"ea\" : { \"$nin\" : [1, 2] } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereEAContainsB()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.EA.Contains(E.B)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => Enumerable.Contains<E>(c.EA, E.B)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"ea\" : 2 }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereEAContainsBNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !c.EA.Contains(E.B)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !Enumerable.Contains<E>(c.EA, E.B)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"ea\" : { \"$ne\" : 2 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereEASub0EqualsA()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.EA[0] == E.A
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => ((Int32)c.EA[0] == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"ea.0\" : 1 }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereEASub0EqualsANot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.EA[0] == E.A)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !((Int32)c.EA[0] == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"ea.0\" : { \"$ne\" : 1 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereEASub0NotEqualsA()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.EA[0] != E.A
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => ((Int32)c.EA[0] != 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"ea.0\" : { \"$ne\" : 1 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereEASub0NotEqualsANot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.EA[0] != E.A)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !((Int32)c.EA[0] != 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"ea.0\" : 1 }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereEEqualsA()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.E == E.A
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => ((Int32)c.E == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"e\" : \"A\" }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereEEqualsANot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.E == E.A)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !((Int32)c.E == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"e\" : { \"$ne\" : \"A\" } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereEEqualsAReversed()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where E.A == c.E
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => ((Int32)c.E == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"e\" : \"A\" }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereEInAOrB()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.E.In(new[] { E.A, E.B })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => LinqToMongo.In<E>(c.E, E[]:{ E.A, E.B })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"e\" : { \"$in\" : [\"A\", \"B\"] } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereEInAOrBNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !c.E.In(new[] { E.A, E.B })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !LinqToMongo.In<E>(c.E, E[]:{ E.A, E.B })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"e\" : { \"$nin\" : [\"A\", \"B\"] } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereENotEqualsA()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.E != E.A
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => ((Int32)c.E != 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"e\" : { \"$ne\" : \"A\" } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereENotEqualsANot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.E != E.A)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !((Int32)c.E != 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"e\" : \"A\" }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereLContains2()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.L.Contains(2)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => c.L.Contains(2)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"l\" : 2 }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereLContains2Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !c.L.Contains(2)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !c.L.Contains(2)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"l\" : { \"$ne\" : 2 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereLContainsAll()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.L.ContainsAll(new[] { 2, 3 })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => LinqToMongo.ContainsAll<Int32>(c.L, Int32[]:{ 2, 3 })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"l\" : { \"$all\" : [2, 3] } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereLContainsAllNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !c.L.ContainsAll(new[] { 2, 3 })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !LinqToMongo.ContainsAll<Int32>(c.L, Int32[]:{ 2, 3 })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"l\" : { \"$not\" : { \"$all\" : [2, 3] } } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereLContainsAny()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.L.ContainsAny(new[] { 2, 3 })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => LinqToMongo.ContainsAny<Int32>(c.L, Int32[]:{ 2, 3 })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"l\" : { \"$in\" : [2, 3] } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereLContainsAnyNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !c.L.ContainsAny(new[] { 1, 2 })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !LinqToMongo.ContainsAny<Int32>(c.L, Int32[]:{ 1, 2 })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"l\" : { \"$nin\" : [1, 2] } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereLExistsFalse()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where Query.NotExists("l").Inject()
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => LinqToMongo.Inject({ \"l\" : { \"$exists\" : false } })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"l\" : { \"$exists\" : false } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereLExistsTrue()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where Query.Exists("l").Inject()
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => LinqToMongo.Inject({ \"l\" : { \"$exists\" : true } })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"l\" : { \"$exists\" : true } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereLExistsTrueNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !Query.Exists("l").Inject()
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !LinqToMongo.Inject({ \"l\" : { \"$exists\" : true } })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"l\" : { \"$exists\" : false } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereLCountMethodEquals3()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.L.Count() == 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (Enumerable.Count<Int32>(c.L) == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"l\" : { \"$size\" : 3 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereLCountMethodEquals3Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.L.Count() == 3)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !(Enumerable.Count<Int32>(c.L) == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"l\" : { \"$not\" : { \"$size\" : 3 } } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereLCountMethodEquals3Reversed()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where 3 == c.L.Count()
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (Enumerable.Count<Int32>(c.L) == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"l\" : { \"$size\" : 3 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereLCountPropertyEquals3()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.L.Count == 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.L.Count == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"l\" : { \"$size\" : 3 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereLCountPropertyEquals3Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.L.Count == 3)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !(c.L.Count == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"l\" : { \"$not\" : { \"$size\" : 3 } } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereLCountPropertyEquals3Reversed()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where 3 == c.L.Count
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.L.Count == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"l\" : { \"$size\" : 3 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereLCountPropertyNotEquals3()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.L.Count != 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.L.Count != 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"l\" : { \"$not\" : { \"$size\" : 3 } } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereLCountPropertyNotEquals3Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.L.Count != 3)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !(c.L.Count != 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"l\" : { \"$size\" : 3 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereLSub1Equals3()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.L[1] == 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.L.get_Item(1) == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"l.1\" : 3 }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereLSub1Equals3Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.L[1] == 3)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !(c.L.get_Item(1) == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"l.1\" : { \"$ne\" : 3 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereLSub1ModTwoEquals1()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.L[1] % 2 == 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => ((c.L.get_Item(1) % 2) == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"l.1\" : { \"$mod\" : [2, 1] } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereLSub1ModTwoEquals1Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.L[1] % 2 == 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !((c.L.get_Item(1) % 2) == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"l.1\" : { \"$not\" : { \"$mod\" : [2, 1] } } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereLSub1ModTwoNotEquals1()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.L[1] % 2 != 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => ((c.L.get_Item(1) % 2) != 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"l.1\" : { \"$not\" : { \"$mod\" : [2, 1] } } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereLSub1ModTwoNotEquals1Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.L[1] % 2 != 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !((c.L.get_Item(1) % 2) != 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"l.1\" : { \"$mod\" : [2, 1] } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereLSub1NotEquals3()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.L[1] != 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.L.get_Item(1) != 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"l.1\" : { \"$ne\" : 3 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereLSub1NotEquals3Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.L[1] != 3)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !(c.L.get_Item(1) != 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"l.1\" : 3 }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereLXModTwoEquals1()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.LX % 2 == 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => ((c.LX % 2) == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"lx\" : { \"$mod\" : [2, 1] } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(3, Consume(query));
        }

        [Fact]
        public void TestWhereLXModTwoEquals1Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.LX % 2 == 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !((c.LX % 2) == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"lx\" : { \"$not\" : { \"$mod\" : [2, 1] } } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(2, Consume(query));
        }

        [Fact]
        public void TestWhereLXModTwoEquals1Reversed()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where 1 == c.LX % 2
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => ((c.LX % 2) == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"lx\" : { \"$mod\" : [2, 1] } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(3, Consume(query));
        }

        [Fact]
        public void TestWhereLXModTwoNotEquals1()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.LX % 2 != 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => ((c.LX % 2) != 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"lx\" : { \"$not\" : { \"$mod\" : [2, 1] } } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(2, Consume(query));
        }

        [Fact]
        public void TestWhereLXModTwoNotEquals1Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.LX % 2 != 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !((c.LX % 2) != 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"lx\" : { \"$mod\" : [2, 1] } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(3, Consume(query));
        }

        [Fact]
        public void TestWhereSASub0ContainsO()
        {
            if (__server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var query = from c in __collection.AsQueryable<C>()
                            where c.SA[0].Contains("o")
                            select c;

                var translatedQuery = MongoQueryTranslator.Translate(query);
                Assert.IsType<SelectQuery>(translatedQuery);
                Assert.Same(__collection, translatedQuery.Collection);
                Assert.Same(typeof(C), translatedQuery.DocumentType);

                var selectQuery = (SelectQuery)translatedQuery;
                Assert.Equal("(C c) => c.SA[0].Contains(\"o\")", ExpressionFormatter.ToString(selectQuery.Where));
                Assert.Null(selectQuery.OrderBy);
                Assert.Null(selectQuery.Projection);
                Assert.Null(selectQuery.Skip);
                Assert.Null(selectQuery.Take);

                Assert.Equal("{ \"sa.0\" : /o/s }", selectQuery.BuildQuery().ToJson());
                Assert.Equal(1, Consume(query));
            }
        }

        [Fact]
        public void TestWhereSASub0ContainsONot()
        {
            if (__server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var query = from c in __collection.AsQueryable<C>()
                            where !c.SA[0].Contains("o")
                            select c;

                var translatedQuery = MongoQueryTranslator.Translate(query);
                Assert.IsType<SelectQuery>(translatedQuery);
                Assert.Same(__collection, translatedQuery.Collection);
                Assert.Same(typeof(C), translatedQuery.DocumentType);

                var selectQuery = (SelectQuery)translatedQuery;
                Assert.Equal("(C c) => !c.SA[0].Contains(\"o\")", ExpressionFormatter.ToString(selectQuery.Where));
                Assert.Null(selectQuery.OrderBy);
                Assert.Null(selectQuery.Projection);
                Assert.Null(selectQuery.Skip);
                Assert.Null(selectQuery.Take);

                Assert.Equal("{ \"sa.0\" : { \"$not\" : /o/s } }", selectQuery.BuildQuery().ToJson());
                Assert.Equal(4, Consume(query));
            }
        }

        [Fact]
        public void TestWhereSASub0EndsWithM()
        {
            if (__server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var query = from c in __collection.AsQueryable<C>()
                            where c.SA[0].EndsWith("m")
                            select c;

                var translatedQuery = MongoQueryTranslator.Translate(query);
                Assert.IsType<SelectQuery>(translatedQuery);
                Assert.Same(__collection, translatedQuery.Collection);
                Assert.Same(typeof(C), translatedQuery.DocumentType);

                var selectQuery = (SelectQuery)translatedQuery;
                Assert.Equal("(C c) => c.SA[0].EndsWith(\"m\")", ExpressionFormatter.ToString(selectQuery.Where));
                Assert.Null(selectQuery.OrderBy);
                Assert.Null(selectQuery.Projection);
                Assert.Null(selectQuery.Skip);
                Assert.Null(selectQuery.Take);

                Assert.Equal("{ \"sa.0\" : /m$/s }", selectQuery.BuildQuery().ToJson());
                Assert.Equal(1, Consume(query));
            }
        }

        [Fact]
        public void TestWhereSASub0EndsWithMNot()
        {
            if (__server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var query = from c in __collection.AsQueryable<C>()
                            where !c.SA[0].EndsWith("m")
                            select c;

                var translatedQuery = MongoQueryTranslator.Translate(query);
                Assert.IsType<SelectQuery>(translatedQuery);
                Assert.Same(__collection, translatedQuery.Collection);
                Assert.Same(typeof(C), translatedQuery.DocumentType);

                var selectQuery = (SelectQuery)translatedQuery;
                Assert.Equal("(C c) => !c.SA[0].EndsWith(\"m\")", ExpressionFormatter.ToString(selectQuery.Where));
                Assert.Null(selectQuery.OrderBy);
                Assert.Null(selectQuery.Projection);
                Assert.Null(selectQuery.Skip);
                Assert.Null(selectQuery.Take);

                Assert.Equal("{ \"sa.0\" : { \"$not\" : /m$/s } }", selectQuery.BuildQuery().ToJson());
                Assert.Equal(4, Consume(query));
            }
        }

        [Fact]
        public void TestWhereSASub0IsMatch()
        {
            if (__server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var regex = new Regex(@"^T");
                var query = from c in __collection.AsQueryable<C>()
                            where regex.IsMatch(c.SA[0])
                            select c;

                var translatedQuery = MongoQueryTranslator.Translate(query);
                Assert.IsType<SelectQuery>(translatedQuery);
                Assert.Same(__collection, translatedQuery.Collection);
                Assert.Same(typeof(C), translatedQuery.DocumentType);

                var selectQuery = (SelectQuery)translatedQuery;
                Assert.Equal("(C c) => Regex:(@\"^T\").IsMatch(c.SA[0])", ExpressionFormatter.ToString(selectQuery.Where));
                Assert.Null(selectQuery.OrderBy);
                Assert.Null(selectQuery.Projection);
                Assert.Null(selectQuery.Skip);
                Assert.Null(selectQuery.Take);

                Assert.Equal("{ \"sa.0\" : /^T/ }", selectQuery.BuildQuery().ToJson());
                Assert.Equal(1, Consume(query));
            }
        }

        [Fact]
        public void TestWhereSASub0IsMatchNot()
        {
            if (__server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var regex = new Regex(@"^T");
                var query = from c in __collection.AsQueryable<C>()
                            where !regex.IsMatch(c.SA[0])
                            select c;

                var translatedQuery = MongoQueryTranslator.Translate(query);
                Assert.IsType<SelectQuery>(translatedQuery);
                Assert.Same(__collection, translatedQuery.Collection);
                Assert.Same(typeof(C), translatedQuery.DocumentType);

                var selectQuery = (SelectQuery)translatedQuery;
                Assert.Equal("(C c) => !Regex:(@\"^T\").IsMatch(c.SA[0])", ExpressionFormatter.ToString(selectQuery.Where));
                Assert.Null(selectQuery.OrderBy);
                Assert.Null(selectQuery.Projection);
                Assert.Null(selectQuery.Skip);
                Assert.Null(selectQuery.Take);

                Assert.Equal("{ \"sa.0\" : { \"$not\" : /^T/ } }", selectQuery.BuildQuery().ToJson());
                Assert.Equal(4, Consume(query));
            }
        }

        [Fact]
        public void TestWhereSASub0IsMatchStatic()
        {
            if (__server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var query = from c in __collection.AsQueryable<C>()
                            where Regex.IsMatch(c.SA[0], "^T")
                            select c;

                var translatedQuery = MongoQueryTranslator.Translate(query);
                Assert.IsType<SelectQuery>(translatedQuery);
                Assert.Same(__collection, translatedQuery.Collection);
                Assert.Same(typeof(C), translatedQuery.DocumentType);

                var selectQuery = (SelectQuery)translatedQuery;
                Assert.Equal("(C c) => Regex.IsMatch(c.SA[0], \"^T\")", ExpressionFormatter.ToString(selectQuery.Where));
                Assert.Null(selectQuery.OrderBy);
                Assert.Null(selectQuery.Projection);
                Assert.Null(selectQuery.Skip);
                Assert.Null(selectQuery.Take);

                Assert.Equal("{ \"sa.0\" : /^T/ }", selectQuery.BuildQuery().ToJson());
                Assert.Equal(1, Consume(query));
            }
        }

        [Fact]
        public void TestWhereSASub0IsMatchStaticNot()
        {
            if (__server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var query = from c in __collection.AsQueryable<C>()
                            where !Regex.IsMatch(c.SA[0], "^T")
                            select c;

                var translatedQuery = MongoQueryTranslator.Translate(query);
                Assert.IsType<SelectQuery>(translatedQuery);
                Assert.Same(__collection, translatedQuery.Collection);
                Assert.Same(typeof(C), translatedQuery.DocumentType);

                var selectQuery = (SelectQuery)translatedQuery;
                Assert.Equal("(C c) => !Regex.IsMatch(c.SA[0], \"^T\")", ExpressionFormatter.ToString(selectQuery.Where));
                Assert.Null(selectQuery.OrderBy);
                Assert.Null(selectQuery.Projection);
                Assert.Null(selectQuery.Skip);
                Assert.Null(selectQuery.Take);

                Assert.Equal("{ \"sa.0\" : { \"$not\" : /^T/ } }", selectQuery.BuildQuery().ToJson());
                Assert.Equal(4, Consume(query));
            }
        }

        [Fact]
        public void TestWhereSASub0IsMatchStaticWithOptions()
        {
            if (__server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var query = from c in __collection.AsQueryable<C>()
                            where Regex.IsMatch(c.SA[0], "^t", RegexOptions.IgnoreCase)
                            select c;

                var translatedQuery = MongoQueryTranslator.Translate(query);
                Assert.IsType<SelectQuery>(translatedQuery);
                Assert.Same(__collection, translatedQuery.Collection);
                Assert.Same(typeof(C), translatedQuery.DocumentType);

                var selectQuery = (SelectQuery)translatedQuery;
                Assert.Equal("(C c) => Regex.IsMatch(c.SA[0], \"^t\", RegexOptions.IgnoreCase)", ExpressionFormatter.ToString(selectQuery.Where));
                Assert.Null(selectQuery.OrderBy);
                Assert.Null(selectQuery.Projection);
                Assert.Null(selectQuery.Skip);
                Assert.Null(selectQuery.Take);

                Assert.Equal("{ \"sa.0\" : /^t/i }", selectQuery.BuildQuery().ToJson());
                Assert.Equal(1, Consume(query));
            }
        }

        [Fact]
        public void TestWhereSASub0StartsWithT()
        {
            if (__server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var query = from c in __collection.AsQueryable<C>()
                            where c.SA[0].StartsWith("T")
                            select c;

                var translatedQuery = MongoQueryTranslator.Translate(query);
                Assert.IsType<SelectQuery>(translatedQuery);
                Assert.Same(__collection, translatedQuery.Collection);
                Assert.Same(typeof(C), translatedQuery.DocumentType);

                var selectQuery = (SelectQuery)translatedQuery;
                Assert.Equal("(C c) => c.SA[0].StartsWith(\"T\")", ExpressionFormatter.ToString(selectQuery.Where));
                Assert.Null(selectQuery.OrderBy);
                Assert.Null(selectQuery.Projection);
                Assert.Null(selectQuery.Skip);
                Assert.Null(selectQuery.Take);

                Assert.Equal("{ \"sa.0\" : /^T/s }", selectQuery.BuildQuery().ToJson());
                Assert.Equal(1, Consume(query));
            }
        }

        [Fact]
        public void TestWhereSASub0StartsWithTNot()
        {
            if (__server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var query = from c in __collection.AsQueryable<C>()
                            where !c.SA[0].StartsWith("T")
                            select c;

                var translatedQuery = MongoQueryTranslator.Translate(query);
                Assert.IsType<SelectQuery>(translatedQuery);
                Assert.Same(__collection, translatedQuery.Collection);
                Assert.Same(typeof(C), translatedQuery.DocumentType);

                var selectQuery = (SelectQuery)translatedQuery;
                Assert.Equal("(C c) => !c.SA[0].StartsWith(\"T\")", ExpressionFormatter.ToString(selectQuery.Where));
                Assert.Null(selectQuery.OrderBy);
                Assert.Null(selectQuery.Projection);
                Assert.Null(selectQuery.Skip);
                Assert.Null(selectQuery.Take);

                Assert.Equal("{ \"sa.0\" : { \"$not\" : /^T/s } }", selectQuery.BuildQuery().ToJson());
                Assert.Equal(4, Consume(query));
            }
        }

        [Fact]
        public void TestWhereSContainsAbc()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.Contains("abc")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => c.S.Contains(\"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /abc/s }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSContainsAbcNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !c.S.Contains("abc")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !c.S.Contains(\"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : { \"$not\" : /abc/s } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereSContainsDot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.Contains(".")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => c.S.Contains(\".\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /\\./s }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(0, Consume(query));
        }

        [Fact]
        public void TestWhereSCountEquals3()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.Count() == 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (Enumerable.Count<Char>(c.S) == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /^.{3}$/s }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSEqualsAbc()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S == "abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S == \"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : \"abc\" }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSEqualsAbcNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.S == "abc")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !(c.S == \"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : { \"$ne\" : \"abc\" } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereSEqualsMethodAbc()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.Equals("abc")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => c.S.Equals(\"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : \"abc\" }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSEqualsMethodAbcNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.S.Equals("abc"))
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !c.S.Equals(\"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : { \"$ne\" : \"abc\" } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereSEqualsStaticMethodAbc()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where string.Equals(c.S, "abc")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => String.Equals(c.S, \"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : \"abc\" }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSEqualsStaticMethodAbcNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !string.Equals(c.S, "abc")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !String.Equals(c.S, \"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : { \"$ne\" : \"abc\" } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereSEndsWithAbc()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.EndsWith("abc")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => c.S.EndsWith(\"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /abc$/s }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSEndsWithAbcNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !c.S.EndsWith("abc")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !c.S.EndsWith(\"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : { \"$not\" : /abc$/s } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereSIndexOfAnyBC()
        {
            var tempCollection = __database.GetCollection("temp");
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
            Assert.Equal(2, Consume(query1));

            var query2 =
                from c in tempCollection.AsQueryable<C>()
                where c.S.IndexOfAny(new char[] { 'b', 'c' }, 1) == 2
                select c;
            Assert.Equal(3, Consume(query2));

            var query3 =
                from c in tempCollection.AsQueryable<C>()
                where c.S.IndexOfAny(new char[] { 'b', 'c' }, 1, 1) == 2
                select c;
            Assert.Equal(0, Consume(query3));

            var query4 =
                from c in tempCollection.AsQueryable<C>()
                where c.S.IndexOfAny(new char[] { 'b', 'c' }, 1, 2) == 2
                select c;
            Assert.Equal(3, Consume(query4));
        }

        [Fact]
        public void TestWhereSIndexOfAnyBDashCEquals1()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.IndexOfAny(new char[] { 'b', '-', 'c' }) == 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.IndexOfAny(Char[]:{ 'b', '-', 'c' }) == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /^[^b\\-c]{1}[b\\-c]/s }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSIndexOfAnyBCStartIndex1Equals1()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.IndexOfAny(new char[] { 'b', '-', 'c' }, 1) == 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.IndexOfAny(Char[]:{ 'b', '-', 'c' }, 1) == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /^.{1}[^b\\-c]{0}[b\\-c]/s }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSIndexOfAnyBCStartIndex1Count2Equals1()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.IndexOfAny(new char[] { 'b', '-', 'c' }, 1, 2) == 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.IndexOfAny(Char[]:{ 'b', '-', 'c' }, 1, 2) == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /^.{1}(?=.{2})[^b\\-c]{0}[b\\-c]/s }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSIndexOfB()
        {
            var tempCollection = __database.GetCollection("temp");
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
            Assert.Equal(2, Consume(query1));

            var query2 =
                from c in tempCollection.AsQueryable<C>()
                where c.S.IndexOf('b', 1) == 2
                select c;
            Assert.Equal(3, Consume(query2));

            var query3 =
                from c in tempCollection.AsQueryable<C>()
                where c.S.IndexOf('b', 1, 1) == 2
                select c;
            Assert.Equal(0, Consume(query3));

            var query4 =
                from c in tempCollection.AsQueryable<C>()
                where c.S.IndexOf('b', 1, 2) == 2
                select c;
            Assert.Equal(3, Consume(query4));
        }

        [Fact]
        public void TestWhereSIndexOfBEquals1()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.IndexOf('b') == 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.IndexOf('b') == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /^[^b]{1}b/s }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSIndexOfBStartIndex1Equals1()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.IndexOf('b', 1) == 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.IndexOf('b', 1) == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /^.{1}[^b]{0}b/s }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSIndexOfBStartIndex1Count2Equals1()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.IndexOf('b', 1, 2) == 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.IndexOf('b', 1, 2) == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /^.{1}(?=.{2})[^b]{0}b/s }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSIndexOfXyz()
        {
            var tempCollection = __database.GetCollection("temp");
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
            Assert.Equal(1, Consume(query1));

            var query2 =
                from c in tempCollection.AsQueryable<C>()
                where c.S.IndexOf("xyz", 1) == 3
                select c;
            Assert.Equal(2, Consume(query2));

            var query3 =
                from c in tempCollection.AsQueryable<C>()
                where c.S.IndexOf("xyz", 1, 4) == 3
                select c;
            Assert.Equal(0, Consume(query3)); // substring isn't long enough to match

            var query4 =
                from c in tempCollection.AsQueryable<C>()
                where c.S.IndexOf("xyz", 1, 5) == 3
                select c;
            Assert.Equal(2, Consume(query4));
        }

        [Fact]
        public void TestWhereSIndexOfXyzEquals3()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.IndexOf("xyz") == 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.IndexOf(\"xyz\") == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /^(?!.{0,2}xyz).{3}xyz/s }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSIndexOfXyzStartIndex1Equals3()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.IndexOf("xyz", 1) == 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.IndexOf(\"xyz\", 1) == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /^.{1}(?!.{0,1}xyz).{2}xyz/s }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSIndexOfXyzStartIndex1Count5Equals3()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.IndexOf("xyz", 1, 5) == 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.IndexOf(\"xyz\", 1, 5) == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /^.{1}(?=.{5})(?!.{0,1}xyz).{2}xyz/s }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSIsMatch()
        {
            var regex = new Regex(@"^abc");
            var query = from c in __collection.AsQueryable<C>()
                        where regex.IsMatch(c.S)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => Regex:(@\"^abc\").IsMatch(c.S)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /^abc/ }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSIsMatchNot()
        {
            var regex = new Regex(@"^abc");
            var query = from c in __collection.AsQueryable<C>()
                        where !regex.IsMatch(c.S)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !Regex:(@\"^abc\").IsMatch(c.S)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : { \"$not\" : /^abc/ } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereSIsMatchStatic()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where Regex.IsMatch(c.S, "^abc")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => Regex.IsMatch(c.S, \"^abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /^abc/ }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSIsMatchStaticNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !Regex.IsMatch(c.S, "^abc")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !Regex.IsMatch(c.S, \"^abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : { \"$not\" : /^abc/ } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereSIsMatchStaticWithOptions()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where Regex.IsMatch(c.S, "^abc", RegexOptions.IgnoreCase)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => Regex.IsMatch(c.S, \"^abc\", RegexOptions.IgnoreCase)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /^abc/i }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSIsNullOrEmpty()
        {
            var tempCollection = __database.GetCollection("temp");
            tempCollection.Drop();
            tempCollection.Insert(new C()); // serialized document will have no "s" field
            tempCollection.Insert(new BsonDocument("s", BsonNull.Value)); // work around [BsonIgnoreIfNull] on S
            tempCollection.Insert(new C { S = "" });
            tempCollection.Insert(new C { S = "x" });

            var query = from c in tempCollection.AsQueryable<C>()
                        where string.IsNullOrEmpty(c.S)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(tempCollection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => String.IsNullOrEmpty(c.S)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"$or\" : [{ \"s\" : { \"$type\" : 10 } }, { \"s\" : \"\" }] }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(2, Consume(query));
        }

        [Fact]
        public void TestWhereSLengthEquals3()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.Length == 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.Length == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /^.{3}$/s }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSLengthEquals3Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.S.Length == 3)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !(c.S.Length == 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : { \"$not\" : /^.{3}$/s } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereSLengthGreaterThan3()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.Length > 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.Length > 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /^.{4,}$/s }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSLengthGreaterThanOrEquals3()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.Length >= 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.Length >= 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /^.{3,}$/s }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(2, Consume(query));
        }

        [Fact]
        public void TestWhereSLengthLessThan3()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.Length < 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.Length < 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /^.{0,2}$/s }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(0, Consume(query));
        }

        [Fact]
        public void TestWhereSLengthLessThanOrEquals3()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.Length <= 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.Length <= 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /^.{0,3}$/s }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSLengthNotEquals3()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.Length != 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.Length != 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : { \"$not\" : /^.{3}$/s } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereSLengthNotEquals3Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.S.Length != 3)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !(c.S.Length != 3)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /^.{3}$/s }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSNotEqualsAbc()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S != "abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S != \"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : { \"$ne\" : \"abc\" } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereSNotEqualsAbcNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.S != "abc")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !(c.S != \"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : \"abc\" }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSStartsWithAbc()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.StartsWith("abc")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => c.S.StartsWith(\"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /^abc/s }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSStartsWithAbcNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !c.S.StartsWith("abc")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !c.S.StartsWith(\"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : { \"$not\" : /^abc/s } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereSSub1EqualsB()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S[1] == 'b'
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => ((Int32)c.S.get_Chars(1) == 98)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /^.{1}b/s }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSSub1EqualsBNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.S[1] == 'b')
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !((Int32)c.S.get_Chars(1) == 98)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : { \"$not\" : /^.{1}b/s } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereSSub1NotEqualsB()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S[1] != 'b'
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => ((Int32)c.S.get_Chars(1) != 98)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /^.{1}[^b]/s }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSSub1NotEqualsBNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.S[1] != 'b')
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !((Int32)c.S.get_Chars(1) != 98)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : { \"$not\" : /^.{1}[^b]/s } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereSTrimContainsXyz()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.Trim().Contains("xyz")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => c.S.Trim().Contains(\"xyz\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /^\\s*.*xyz.*\\s*$/s }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSTrimContainsXyzNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !c.S.Trim().Contains("xyz")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !c.S.Trim().Contains(\"xyz\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : { \"$not\" : /^\\s*.*xyz.*\\s*$/s } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereSTrimEndsWithXyz()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.Trim().EndsWith("xyz")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => c.S.Trim().EndsWith(\"xyz\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /^\\s*.*xyz\\s*$/s }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSTrimEndsWithXyzNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !c.S.Trim().EndsWith("xyz")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !c.S.Trim().EndsWith(\"xyz\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : { \"$not\" : /^\\s*.*xyz\\s*$/s } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereSTrimStartsWithXyz()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.Trim().StartsWith("xyz")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => c.S.Trim().StartsWith(\"xyz\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /^\\s*xyz.*\\s*$/s }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSTrimStartsWithXyzNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !c.S.Trim().StartsWith("xyz")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !c.S.Trim().StartsWith(\"xyz\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : { \"$not\" : /^\\s*xyz.*\\s*$/s } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereSTrimStartTrimEndToLowerContainsXyz()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.TrimStart(' ', '.', '-', '\t').TrimEnd().ToLower().Contains("xyz")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => c.S.TrimStart(Char[]:{ ' ', '.', '-', '\t' }).TrimEnd(Char[]:{ }).ToLower().Contains(\"xyz\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /^[\\ \\.\\-\\t]*.*xyz.*\\s*$/is }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSToLowerEqualsConstantLowerCaseValue()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.ToLower() == "abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.ToLower() == \"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /^abc$/i }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSToLowerDoesNotEqualConstantLowerCaseValue()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.ToLower() != "abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.ToLower() != \"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : { \"$not\" : /^abc$/i } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereSToLowerEqualsConstantMixedCaseValue()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.ToLower() == "Abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.ToLower() == \"Abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"_id\" : { \"$type\" : -1 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(0, Consume(query));
        }

        [Fact]
        public void TestWhereSToLowerDoesNotEqualConstantMixedCaseValue()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.ToLower() != "Abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.ToLower() != \"Abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(5, Consume(query));
        }

        [Fact]
        public void TestWhereSToLowerEqualsNullValue()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.ToLower() == null
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.ToLower() == null)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : null }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(3, Consume(query));
        }

        [Fact]
        public void TestWhereSToLowerDoesNotEqualNullValue()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.ToLower() != null
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.ToLower() != null)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : { \"$ne\" : null } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(2, Consume(query));
        }

        [Fact]
        public void TestWhereSToUpperEqualsConstantLowerCaseValue()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.ToUpper() == "abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.ToUpper() == \"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"_id\" : { \"$type\" : -1 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(0, Consume(query));
        }

        [Fact]
        public void TestWhereSToUpperDoesNotEqualConstantLowerCaseValue()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.ToUpper() != "abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.ToUpper() != \"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(5, Consume(query));
        }

        [Fact]
        public void TestWhereSToUpperEqualsConstantMixedCaseValue()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.ToUpper() == "Abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.ToUpper() == \"Abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"_id\" : { \"$type\" : -1 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(0, Consume(query));
        }

        [Fact]
        public void TestWhereSToUpperDoesNotEqualConstantMixedCaseValue()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.ToUpper() != "Abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.ToUpper() != \"Abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(5, Consume(query));
        }

        [Fact]
        public void TestWhereSToUpperEqualsNullValue()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.ToUpper() == null
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.ToUpper() == null)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : null }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(3, Consume(query));
        }

        [Fact]
        public void TestWhereSToUpperDoesNotEqualNullValue()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.ToUpper() != null
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.ToUpper() != null)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : { \"$ne\" : null } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(2, Consume(query));
        }

        [Fact]
        public void TestWhereSTrimStartTrimEndToLowerInvariantContainsXyz()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.TrimStart(' ', '.', '-', '\t').TrimEnd().ToLowerInvariant().Contains("xyz")
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => c.S.TrimStart(Char[]:{ ' ', '.', '-', '\t' }).TrimEnd(Char[]:{ }).ToLowerInvariant().Contains(\"xyz\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /^[\\ \\.\\-\\t]*.*xyz.*\\s*$/is }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSToLowerInvariantEqualsConstantLowerCaseValue()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.ToLowerInvariant() == "abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.ToLowerInvariant() == \"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : /^abc$/i }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereSToLowerInvariantDoesNotEqualConstantLowerCaseValue()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.ToLowerInvariant() != "abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.ToLowerInvariant() != \"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : { \"$not\" : /^abc$/i } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereSToLowerInvariantEqualsConstantMixedCaseValue()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.ToLowerInvariant() == "Abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.ToLowerInvariant() == \"Abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"_id\" : { \"$type\" : -1 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(0, Consume(query));
        }

        [Fact]
        public void TestWhereSToLowerInvariantDoesNotEqualConstantMixedCaseValue()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.ToLowerInvariant() != "Abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.ToLowerInvariant() != \"Abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(5, Consume(query));
        }

        [Fact]
        public void TestWhereSToLowerInvariantEqualsNullValue()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.ToLowerInvariant() == null
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.ToLowerInvariant() == null)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : null }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(3, Consume(query));
        }

        [Fact]
        public void TestWhereSToLowerInvariantDoesNotEqualNullValue()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.ToLowerInvariant() != null
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.ToLowerInvariant() != null)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : { \"$ne\" : null } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(2, Consume(query));
        }

        [Fact]
        public void TestWhereSToUpperInvariantEqualsConstantLowerCaseValue()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.ToUpperInvariant() == "abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.ToUpperInvariant() == \"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"_id\" : { \"$type\" : -1 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(0, Consume(query));
        }

        [Fact]
        public void TestWhereSToUpperInvariantDoesNotEqualConstantLowerCaseValue()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.ToUpperInvariant() != "abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.ToUpperInvariant() != \"abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(5, Consume(query));
        }

        [Fact]
        public void TestWhereSToUpperInvariantEqualsConstantMixedCaseValue()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.ToUpperInvariant() == "Abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.ToUpperInvariant() == \"Abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"_id\" : { \"$type\" : -1 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(0, Consume(query));
        }

        [Fact]
        public void TestWhereSToUpperInvariantDoesNotEqualConstantMixedCaseValue()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.ToUpperInvariant() != "Abc"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.ToUpperInvariant() != \"Abc\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(5, Consume(query));
        }

        [Fact]
        public void TestWhereSToUpperInvariantEqualsNullValue()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.ToUpperInvariant() == null
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.ToUpperInvariant() == null)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : null }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(3, Consume(query));
        }

        [Fact]
        public void TestWhereSToUpperInvariantDoesNotEqualNullValue()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.S.ToUpperInvariant() != null
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.S.ToUpperInvariant() != null)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"s\" : { \"$ne\" : null } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(2, Consume(query));
        }

        [Fact]
        public void TestWhereSystemProfileInfoDurationGreatherThan10Seconds()
        {
            var query = from pi in __systemProfileCollection.AsQueryable<SystemProfileInfo>()
                        where pi.Duration > TimeSpan.FromSeconds(10)
                        select pi;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__systemProfileCollection, translatedQuery.Collection);
            Assert.Same(typeof(SystemProfileInfo), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(SystemProfileInfo pi) => (pi.Duration > TimeSpan:(00:00:10))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"millis\" : { \"$gt\" : 10000.0 } }", selectQuery.BuildQuery().ToJson());
        }

        [Fact]
        public void TestWhereSystemProfileInfoNamespaceEqualsNs()
        {
            var query = from pi in __systemProfileCollection.AsQueryable<SystemProfileInfo>()
                        where pi.Namespace == "ns"
                        select pi;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__systemProfileCollection, translatedQuery.Collection);
            Assert.Same(typeof(SystemProfileInfo), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(SystemProfileInfo pi) => (pi.Namespace == \"ns\")", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"ns\" : \"ns\" }", selectQuery.BuildQuery().ToJson());
        }

        [Fact]
        public void TestWhereSystemProfileInfoNumberScannedGreaterThan1000()
        {
            var query = from pi in __systemProfileCollection.AsQueryable<SystemProfileInfo>()
                        where pi.NumberScanned > 1000
                        select pi;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__systemProfileCollection, translatedQuery.Collection);
            Assert.Same(typeof(SystemProfileInfo), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(SystemProfileInfo pi) => (pi.NumberScanned > 1000)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"nscanned\" : { \"$gt\" : 1000 } }", selectQuery.BuildQuery().ToJson());
        }

        [Fact]
        public void TestWhereSystemProfileInfoTimeStampGreatherThanJan12012()
        {
            var query = from pi in __systemProfileCollection.AsQueryable<SystemProfileInfo>()
                        where pi.Timestamp > new DateTime(2012, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                        select pi;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__systemProfileCollection, translatedQuery.Collection);
            Assert.Same(typeof(SystemProfileInfo), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(SystemProfileInfo pi) => (pi.Timestamp > DateTime:(2012-01-01T00:00:00Z))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"ts\" : { \"$gt\" : ISODate(\"2012-01-01T00:00:00Z\") } }", selectQuery.BuildQuery().ToJson());
        }

        [Fact]
        public void TestWhereTripleAnd()
        {
            if (__server.BuildInfo.Version >= new Version(2, 0, 0))
            {
                // the query is a bit odd in order to force the built query to be promoted to $and form
                var query = from c in __collection.AsQueryable<C>()
                            where c.X >= 0 && c.X >= 1 && c.Y == 11
                            select c;

                var translatedQuery = MongoQueryTranslator.Translate(query);
                Assert.IsType<SelectQuery>(translatedQuery);
                Assert.Same(__collection, translatedQuery.Collection);
                Assert.Same(typeof(C), translatedQuery.DocumentType);

                var selectQuery = (SelectQuery)translatedQuery;
                Assert.Equal("(C c) => (((c.X >= 0) && (c.X >= 1)) && (c.Y == 11))", ExpressionFormatter.ToString(selectQuery.Where));
                Assert.Null(selectQuery.OrderBy);
                Assert.Null(selectQuery.Projection);
                Assert.Null(selectQuery.Skip);
                Assert.Null(selectQuery.Take);

                Assert.Equal("{ \"$and\" : [{ \"x\" : { \"$gte\" : 0 } }, { \"x\" : { \"$gte\" : 1 } }, { \"y\" : 11 }] }", selectQuery.BuildQuery().ToJson());
                Assert.Equal(2, Consume(query));
            }
        }

        [Fact]
        public void TestWhereTripleOr()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.X == 1 || c.Y == 33 || c.S == "x is 1"
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (((c.X == 1) || (c.Y == 33)) || (c.S == \"x is 1\"))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"$or\" : [{ \"x\" : 1 }, { \"y\" : 33 }, { \"s\" : \"x is 1\" }] }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(2, Consume(query));
        }

        [Fact]
        public void TestWhereWithIndex()
        {
            var query = __collection.AsQueryable<C>().Where((c, i) => true);
            var exception = Record.Exception(() => query.ToList()); // execute query

            var expectedMessage = "The indexed version of the Where query operator is not supported.";
            Assert.IsType<NotSupportedException>(exception);
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TestWhereXEquals1()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.X == 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.X == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : 1 }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereXEquals1AndYEquals11()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.X == 1 & c.Y == 11
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => ((c.X == 1) & (c.Y == 11))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : 1, \"y\" : 11 }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereXEquals1AndAlsoYEquals11()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.X == 1 && c.Y == 11
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => ((c.X == 1) && (c.Y == 11))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : 1, \"y\" : 11 }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereXEquals1AndYEquals11UsingTwoWhereClauses()
        {
            // note: using different variable names in the two where clauses to test parameter replacement when combining predicates
            var query = __collection.AsQueryable<C>().Where(c => c.X == 1).Where(d => d.Y == 11);

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => ((c.X == 1) && (c.Y == 11))", ExpressionFormatter.ToString(selectQuery.Where)); // note parameter replacement from c to d in second clause
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : 1, \"y\" : 11 }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereXEquals1AndYEquals11Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.X == 1 && c.Y == 11)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !((c.X == 1) && (c.Y == 11))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"$nor\" : [{ \"x\" : 1, \"y\" : 11 }] }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereXEquals1AndYEquals11AndZEquals11()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.X == 1 && c.Y == 11 && c.D.Z == 11
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (((c.X == 1) && (c.Y == 11)) && (c.D.Z == 11))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : 1, \"y\" : 11, \"d.z\" : 11 }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereXEquals1Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.X == 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !(c.X == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : { \"$ne\" : 1 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereXEquals1OrYEquals33()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.X == 1 | c.Y == 33
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => ((c.X == 1) | (c.Y == 33))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"$or\" : [{ \"x\" : 1 }, { \"y\" : 33 }] }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(2, Consume(query));
        }

        [Fact]
        public void TestWhereXEquals1OrElseYEquals33()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.X == 1 || c.Y == 33
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => ((c.X == 1) || (c.Y == 33))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"$or\" : [{ \"x\" : 1 }, { \"y\" : 33 }] }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(2, Consume(query));
        }

        [Fact]
        public void TestWhereXEquals1OrYEquals33Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.X == 1 || c.Y == 33)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !((c.X == 1) || (c.Y == 33))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"$nor\" : [{ \"x\" : 1 }, { \"y\" : 33 }] }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(3, Consume(query));
        }

        [Fact]
        public void TestWhereXEquals1OrYEquals33NotNot()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !!(c.X == 1 || c.Y == 33)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !!((c.X == 1) || (c.Y == 33))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"$or\" : [{ \"x\" : 1 }, { \"y\" : 33 }] }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(2, Consume(query));
        }

        [Fact]
        public void TestWhereXEquals1UsingJavaScript()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.X == 1 && Query.Where("this.x < 9").Inject()
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => ((c.X == 1) && LinqToMongo.Inject({ \"$where\" : { \"$code\" : \"this.x < 9\" } }))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : 1, \"$where\" : { \"$code\" : \"this.x < 9\" } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereXGreaterThan1()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.X > 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.X > 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : { \"$gt\" : 1 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereXGreaterThan1AndLessThan3()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.X > 1 && c.X < 3
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => ((c.X > 1) && (c.X < 3))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : { \"$gt\" : 1, \"$lt\" : 3 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereXGreaterThan1AndLessThan3Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.X > 1 && c.X < 3)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !((c.X > 1) && (c.X < 3))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"$nor\" : [{ \"x\" : { \"$gt\" : 1, \"$lt\" : 3 } }] }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereXGreaterThan1Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.X > 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !(c.X > 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : { \"$not\" : { \"$gt\" : 1 } } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereXGreaterThan1Reversed()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where 1 < c.X
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.X > 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : { \"$gt\" : 1 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereXGreaterThanOrEquals1()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.X >= 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.X >= 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : { \"$gte\" : 1 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(5, Consume(query));
        }

        [Fact]
        public void TestWhereXGreaterThanOrEquals1Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.X >= 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !(c.X >= 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : { \"$not\" : { \"$gte\" : 1 } } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(0, Consume(query));
        }

        [Fact]
        public void TestWhereXGreaterThanOrEquals1Reversed()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where 1 <= c.X
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.X >= 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : { \"$gte\" : 1 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(5, Consume(query));
        }

        [Fact]
        public void TestWhereXIn1Or9()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.X.In(new[] { 1, 9 })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => LinqToMongo.In<Int32>(c.X, Int32[]:{ 1, 9 })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : { \"$in\" : [1, 9] } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereXIn1Or9Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !c.X.In(new[] { 1, 9 })
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !LinqToMongo.In<Int32>(c.X, Int32[]:{ 1, 9 })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : { \"$nin\" : [1, 9] } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereXIsTypeInt32()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where Query.Type("x", BsonType.Int32).Inject()
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => LinqToMongo.Inject({ \"x\" : { \"$type\" : 16 } })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : { \"$type\" : 16 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(5, Consume(query));
        }

        [Fact]
        public void TestWhereXIsTypeInt32Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !Query.Type("x", BsonType.Int32).Inject()
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !LinqToMongo.Inject({ \"x\" : { \"$type\" : 16 } })", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : { \"$not\" : { \"$type\" : 16 } } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(0, Consume(query));
        }

        [Fact]
        public void TestWhereXLessThan1()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.X < 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.X < 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : { \"$lt\" : 1 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(0, Consume(query));
        }

        [Fact]
        public void TestWhereXLessThan1Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.X < 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !(c.X < 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : { \"$not\" : { \"$lt\" : 1 } } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(5, Consume(query));
        }

        [Fact]
        public void TestWhereXLessThan1Reversed()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where 1 > c.X
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.X < 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : { \"$lt\" : 1 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(0, Consume(query));
        }

        [Fact]
        public void TestWhereXLessThanOrEquals1()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.X <= 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.X <= 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : { \"$lte\" : 1 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereXLessThanOrEquals1Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.X <= 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !(c.X <= 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : { \"$not\" : { \"$lte\" : 1 } } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereXLessThanOrEquals1Reversed()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where 1 >= c.X
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.X <= 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : { \"$lte\" : 1 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, Consume(query));
        }

        [Fact]
        public void TestWhereXModOneEquals0AndXModTwoEquals0()
        {
            if (__server.BuildInfo.Version >= new Version(2, 0, 0))
            {
                var query = from c in __collection.AsQueryable<C>()
                            where (c.X % 1 == 0) && (c.X % 2 == 0)
                            select c;

                var translatedQuery = MongoQueryTranslator.Translate(query);
                Assert.IsType<SelectQuery>(translatedQuery);
                Assert.Same(__collection, translatedQuery.Collection);
                Assert.Same(typeof(C), translatedQuery.DocumentType);

                var selectQuery = (SelectQuery)translatedQuery;
                Assert.Equal("(C c) => (((c.X % 1) == 0) && ((c.X % 2) == 0))", ExpressionFormatter.ToString(selectQuery.Where));
                Assert.Null(selectQuery.OrderBy);
                Assert.Null(selectQuery.Projection);
                Assert.Null(selectQuery.Skip);
                Assert.Null(selectQuery.Take);

                Assert.Equal("{ \"$and\" : [{ \"x\" : { \"$mod\" : [1, 0] } }, { \"x\" : { \"$mod\" : [2, 0] } }] }", selectQuery.BuildQuery().ToJson());
                Assert.Equal(2, Consume(query));
            }
        }

        [Fact]
        public void TestWhereXModOneEquals0AndXModTwoEquals0Not()
        {
            if (__server.BuildInfo.Version >= new Version(2, 0, 0))
            {
                var query = from c in __collection.AsQueryable<C>()
                            where !((c.X % 1 == 0) && (c.X % 2 == 0))
                            select c;

                var translatedQuery = MongoQueryTranslator.Translate(query);
                Assert.IsType<SelectQuery>(translatedQuery);
                Assert.Same(__collection, translatedQuery.Collection);
                Assert.Same(typeof(C), translatedQuery.DocumentType);

                var selectQuery = (SelectQuery)translatedQuery;
                Assert.Equal("(C c) => !(((c.X % 1) == 0) && ((c.X % 2) == 0))", ExpressionFormatter.ToString(selectQuery.Where));
                Assert.Null(selectQuery.OrderBy);
                Assert.Null(selectQuery.Projection);
                Assert.Null(selectQuery.Skip);
                Assert.Null(selectQuery.Take);

                Assert.Equal("{ \"$nor\" : [{ \"$and\" : [{ \"x\" : { \"$mod\" : [1, 0] } }, { \"x\" : { \"$mod\" : [2, 0] } }] }] }", selectQuery.BuildQuery().ToJson());
                Assert.Equal(3, Consume(query));
            }
        }

        [Fact]
        public void TestWhereXModOneEquals0AndXModTwoEquals0NotNot()
        {
            if (__server.BuildInfo.Version >= new Version(2, 0, 0))
            {
                var query = from c in __collection.AsQueryable<C>()
                            where !!((c.X % 1 == 0) && (c.X % 2 == 0))
                            select c;

                var translatedQuery = MongoQueryTranslator.Translate(query);
                Assert.IsType<SelectQuery>(translatedQuery);
                Assert.Same(__collection, translatedQuery.Collection);
                Assert.Same(typeof(C), translatedQuery.DocumentType);

                var selectQuery = (SelectQuery)translatedQuery;
                Assert.Equal("(C c) => !!(((c.X % 1) == 0) && ((c.X % 2) == 0))", ExpressionFormatter.ToString(selectQuery.Where));
                Assert.Null(selectQuery.OrderBy);
                Assert.Null(selectQuery.Projection);
                Assert.Null(selectQuery.Skip);
                Assert.Null(selectQuery.Take);

                Assert.Equal("{ \"$or\" : [{ \"$and\" : [{ \"x\" : { \"$mod\" : [1, 0] } }, { \"x\" : { \"$mod\" : [2, 0] } }] }] }", selectQuery.BuildQuery().ToJson());
                Assert.Equal(2, Consume(query));
            }
        }

        [Fact]
        public void TestWhereXModTwoEquals1()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.X % 2 == 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => ((c.X % 2) == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : { \"$mod\" : [2, 1] } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(3, Consume(query));
        }

        [Fact]
        public void TestWhereXModTwoEquals1Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.X % 2 == 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !((c.X % 2) == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : { \"$not\" : { \"$mod\" : [2, 1] } } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(2, Consume(query));
        }

        [Fact]
        public void TestWhereXModTwoEquals1Reversed()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where 1 == c.X % 2
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => ((c.X % 2) == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : { \"$mod\" : [2, 1] } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(3, Consume(query));
        }

        [Fact]
        public void TestWhereXModTwoNotEquals1()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.X % 2 != 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => ((c.X % 2) != 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : { \"$not\" : { \"$mod\" : [2, 1] } } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(2, Consume(query));
        }

        [Fact]
        public void TestWhereXModTwoNotEquals1Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.X % 2 != 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !((c.X % 2) != 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : { \"$mod\" : [2, 1] } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(3, Consume(query));
        }

        [Fact]
        public void TestWhereXNotEquals1()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where c.X != 1
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => (c.X != 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : { \"$ne\" : 1 } }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(4, Consume(query));
        }

        [Fact]
        public void TestWhereXNotEquals1Not()
        {
            var query = from c in __collection.AsQueryable<C>()
                        where !(c.X != 1)
                        select c;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(__collection, translatedQuery.Collection);
            Assert.Same(typeof(C), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(C c) => !(c.X != 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"x\" : 1 }", selectQuery.BuildQuery().ToJson());
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
