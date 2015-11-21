/* Copyright 2010-2015 MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq.Translators;
using NUnit.Framework;

namespace MongoDB.Driver.Tests.Linq.Translators
{
    [TestFixture]
    public class LegacyPredicateTranslatorTests
    {
        private IMongoDatabase _database;
        private IMongoCollection<C> _collection;

        private ObjectId _id1 = ObjectId.GenerateNewId();
        private ObjectId _id2 = ObjectId.GenerateNewId();
        private ObjectId _id3 = ObjectId.GenerateNewId();
        private ObjectId _id4 = ObjectId.GenerateNewId();
        private ObjectId _id5 = ObjectId.GenerateNewId();

        [TestFixtureSetUp]
        public void Setup()
        {
            _database = DriverTestConfiguration.Client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            _collection = _database.GetCollection<C>(DriverTestConfiguration.CollectionNamespace.CollectionName);

            // documents inserted deliberately out of order to test sorting
            _database.DropCollection(_collection.CollectionNamespace.CollectionName);
            _collection.InsertMany(new[]
            {
                new C { Id = _id2, X = 2, LX = 2, Y = 11, Date = new DateTime(2000, 1, 1, 1, 1, 1, 1, DateTimeKind.Utc), D = new D { Z = 22 }, NullableDouble = 2, A = new[] { 2, 3, 4 }, DA = new List<D> { new D { Y = 11, Z = 111 }, new D { Z = 222 } }, L = new List<int> { 2, 3, 4 } },
                new C { Id = _id1, X = 1, LX = 1, Y = 11, Date = new DateTime(2000, 2, 2, 2, 2, 2, 2, DateTimeKind.Utc), D = new D { Z = 11 }, NullableDouble = 2, S = "abc", SA = new string[] { "Tom", "Dick", "Harry" } },
                new C { Id = _id3, X = 3, LX = 3, Y = 33, Date = new DateTime(2001, 1, 1, 1, 1, 1, 1, DateTimeKind.Utc), D = new D { Z = 33 }, NullableDouble = 5, B = true, BA = new bool[] { true }, E = E.A, ENullable = E.A, EA = new E[] { E.A, E.B } },
                new C { Id = _id5, X = 5, LX = 5, Y = 44, Date = new DateTime(2001, 2, 2, 2, 2, 2, 2, DateTimeKind.Utc), D = new D { Z = 55 }, DBRef = new MongoDBRef("db", "c", 1), F = new F { G = new G { H = 10 } } },
                new C { Id = _id4, X = 4, LX = 4, Y = 44, Date = new DateTime(2001, 3, 3, 3, 3, 3, 3, DateTimeKind.Utc), D = new D { Z = 44 }, S = "   xyz   ", DA = new List<D> { new D { Y = 33, Z = 333 }, new D { Y = 44, Z = 444 } } }
            });
        }

        [Test]
        public void TestWhereAAny()
        {
            Assert<C>(c => c.A.Any(), 1, "{ \"a\" : { \"$ne\" : null, \"$not\" : { \"$size\" : 0 } } }");
        }

        [Test]
        public void TestWhereLocalIListContainsX()
        {
            IList<int> local = new[] { 1, 2, 3 };

            Assert<C>(c => local.Contains(c.X), 3, "{ \"x\" : { \"$in\" : [1, 2, 3] } }");
        }

        [Test]
        public void TestWhereLocalListContainsX()
        {
            var local = new List<int> { 1, 2, 3 };

            Assert<C>(c => local.Contains(c.X), 3, "{ \"x\" : { \"$in\" : [1, 2, 3] } }");
        }

        [Test]
        public void TestWhereLocalArrayContainsX()
        {
            var local = new[] { 1, 2, 3 };

            Assert<C>(c => local.Contains(c.X), 3, "{ \"x\" : { \"$in\" : [1, 2, 3] } }");
        }

        [Test]
        public void TestWhereAContains2()
        {
            Assert<C>(c => c.A.Contains(2), 1, "{ \"a\" : 2 }");
        }

        [Test]
        public void TestWhereAContains2Not()
        {
            Assert<C>(c => !c.A.Contains(2), 4, "{ \"a\" : { \"$ne\" : 2 } }");
        }

        [Test]
        public void TestWhereALengthEquals3()
        {
            Assert<C>(c => c.A.Length == 3, 1, "{ \"a\" : { \"$size\" : 3 } }");
        }

        [Test]
        public void TestWhereALengthEquals3Not()
        {
            Assert<C>(c => !(c.A.Length == 3), 4, "{ \"a\" : { \"$not\" : { \"$size\" : 3 } } }");
        }

        [Test]
        public void TestWhereALengthEquals3Reversed()
        {
            Assert<C>(c => 3 == c.A.Length, 1, "{ \"a\" : { \"$size\" : 3 } }");
        }

        [Test]
        public void TestWhereALengthNotEquals3()
        {
            Assert<C>(c => c.A.Length != 3, 4, "{ \"a\" : { \"$not\" : { \"$size\" : 3 } } }");
        }

        [Test]
        public void TestWhereALengthNotEquals3Not()
        {
            Assert<C>(c => !(c.A.Length != 3), 1, "{ \"a\" : { \"$size\" : 3 } }");
        }

        [Test]
        public void TestWhereASub1Equals3()
        {
            Assert<C>(c => c.A[1] == 3, 1, "{ \"a.1\" : 3 }");
        }

        [Test]
        public void TestWhereASub1Equals3Not()
        {
            Assert<C>(c => !(c.A[1] == 3), 4, "{ \"a.1\" : { \"$ne\" : 3 } }");
        }

        [Test]
        public void TestWhereASub1ModTwoEquals1()
        {
            Assert<C>(c => c.A[1] % 2 == 1, 1, "{ \"a.1\" : { \"$mod\" : [NumberLong(2), NumberLong(1)] } }");
        }

        [Test]
        public void TestWhereASub1ModTwoEquals1Not()
        {
            Assert<C>(c => !(c.A[1] % 2 == 1), 4, "{ \"a.1\" : { \"$not\" : { \"$mod\" : [NumberLong(2), NumberLong(1)] } } }");
        }

        [Test]
        public void TestWhereASub1ModTwoNotEquals1()
        {
            Assert<C>(c => c.A[1] % 2 != 1, 4, "{ \"a.1\" : { \"$not\" : { \"$mod\" : [NumberLong(2), NumberLong(1)] } } }");
        }

        [Test]
        public void TestWhereASub1ModTwoNotEquals1Not()
        {
            Assert<C>(c => !(c.A[1] % 2 != 1), 1, "{ \"a.1\" : { \"$mod\" : [NumberLong(2), NumberLong(1)] } }");
        }

        [Test]
        public void TestWhereASub1NotEquals3()
        {
            Assert<C>(c => c.A[1] != 3, 4, "{ \"a.1\" : { \"$ne\" : 3 } }");
        }

        [Test]
        public void TestWhereASub1NotEquals3Not()
        {
            Assert<C>(c => !(c.A[1] != 3), 1, "{ \"a.1\" : 3 }");
        }

        [Test]
        public void TestWhereB()
        {
            Assert<C>(c => c.B, 1, "{ \"b\" : true }");
        }

        [Test]
        public void TestWhereBASub0()
        {
            Assert<C>(c => c.BA[0], 1, "{ \"ba.0\" : true }");
        }

        [Test]
        public void TestWhereBASub0EqualsFalse()
        {
            Assert<C>(c => c.BA[0] == false, 0, "{ \"ba.0\" : false }");
        }

        [Test]
        public void TestWhereBASub0EqualsFalseNot()
        {
            Assert<C>(c => !(c.BA[0] == false), 5, "{ \"ba.0\" : { \"$ne\" : false } }");
        }

        [Test]
        public void TestWhereBASub0EqualsTrue()
        {
            Assert<C>(c => c.BA[0] == true, 1, "{ \"ba.0\" : true }");
        }

        [Test]
        public void TestWhereBASub0EqualsTrueNot()
        {
            Assert<C>(c => !(c.BA[0] == true), 4, "{ \"ba.0\" : { \"$ne\" : true } }");
        }

        [Test]
        public void TestWhereBASub0Not()
        {
            Assert<C>(c => !c.BA[0], 4, "{ \"ba.0\" : { \"$ne\" : true } }");
        }

        [Test]
        public void TestWhereBEqualsFalse()
        {
            Assert<C>(c => c.B == false, 4, "{ \"b\" : false }");
        }

        [Test]
        public void TestWhereBEqualsFalseNot()
        {
            Assert<C>(c => !(c.B == false), 1, "{ \"b\" : { \"$ne\" : false } }");
        }

        [Test]
        public void TestWhereBEqualsTrue()
        {
            Assert<C>(c => c.B == true, 1, "{ \"b\" : true }");
        }

        [Test]
        public void TestWhereBEqualsTrueNot()
        {
            Assert<C>(c => !(c.B == true), 4, "{ \"b\" : { \"$ne\" : true } }");
        }

        [Test]
        public void TestWhereBNot()
        {
            Assert<C>(c => !c.B, 4, "{ \"b\" : { \"$ne\" : true } }");
        }

        [Test]
        public void TestWhereDEquals11()
        {
            Assert<C>(c => c.D == new D { Z = 11 }, 1, "{ \"d\" : { \"z\" : 11 } }");
        }

        [Test]
        public void TestWhereDEquals11Not()
        {
            Assert<C>(c => !(c.D == new D { Z = 11 }), 4, "{ \"d\" : { \"$ne\" : { \"z\" : 11 } } }");
        }

        [Test]
        public void TestWhereDNotEquals11()
        {
            Assert<C>(c => c.D != new D { Z = 11 }, 4, "{ \"d\" : { \"$ne\" : { \"z\" : 11 } } }");
        }

        [Test]
        public void TestWhereDNotEquals11Not()
        {
            Assert<C>(c => !(c.D != new D { Z = 11 }), 1, "{ \"d\" : { \"z\" : 11 } }");
        }

        [Test]
        public void TestWhereDAAnyWithPredicate()
        {
            Assert<C>(c => c.DA.Any(d => d.Z == 333), 1, "{ \"da.z\" : 333 }");

            Assert<C>(c => c.DA.Any(d => d.Z >= 222 && d.Z <= 444), 2, "{ \"da\" : { \"$elemMatch\" : { \"z\" : { \"$gte\" : 222, \"$lte\" : 444 } } } }");
        }

        [Test]
        public void TestWhereEAContainsB()
        {
            Assert<C>(c => c.EA.Contains(E.B), 1, "{ \"ea\" : 2 }");
        }

        [Test]
        public void TestWhereEAContainsBNot()
        {
            Assert<C>(c => !c.EA.Contains(E.B), 4, "{ \"ea\" : { \"$ne\" : 2 } }");
        }

        [Test]
        public void TestWhereEASub0EqualsA()
        {
            Assert<C>(c => c.EA[0] == E.A, 1, "{ \"ea.0\" : 1 }");
        }

        [Test]
        public void TestWhereEASub0EqualsANot()
        {
            Assert<C>(c => !(c.EA[0] == E.A), 4, "{ \"ea.0\" : { \"$ne\" : 1 } }");
        }

        [Test]
        public void TestWhereEASub0NotEqualsA()
        {
            Assert<C>(c => c.EA[0] != E.A, 4, "{ \"ea.0\" : { \"$ne\" : 1 } }");
        }

        [Test]
        public void TestWhereEASub0NotEqualsANot()
        {
            Assert<C>(c => !(c.EA[0] != E.A), 1, "{ \"ea.0\" : 1 }");
        }

        [Test]
        public void TestWhereEEqualsA()
        {
            Assert<C>(c => c.E == E.A, 1, "{ \"e\" : \"A\" }");
        }

        [Test]
        public void TestWhereEEqualsANot()
        {
            Assert<C>(c => !(c.E == E.A), 4, "{ \"e\" : { \"$ne\" : \"A\" } }");
        }

        [Test]
        public void TestWhereEEqualsAReversed()
        {
            Assert<C>(c => E.A == c.E, 1, "{ \"e\" : \"A\" }");
        }

        [Test]
        public void TestWhereENotEqualsA()
        {
            Assert<C>(c => c.E != E.A, 4, "{ \"e\" : { \"$ne\" : \"A\" } }");
        }

        [Test]
        public void TestWhereENotEqualsANot()
        {
            Assert<C>(c => !(c.E != E.A), 1, "{ \"e\" : \"A\" }");
        }

        [Test]
        public void TestWhereENullableEqualsA()
        {
            Assert<C>(c => c.ENullable == E.A, 1, "{ \"en\" : \"A\" }");
        }

        [Test]
        public void TestWhereENullableEqualsNull()
        {
            Assert<C>(c => c.ENullable == null, 4, "{ \"en\" : null }");
        }

        [Test]
        public void TestWhereENullabeEqualsAReversed()
        {
            Assert<C>(c => E.A == c.ENullable, 1, "{ \"en\" : \"A\" }");
        }

        [Test]
        public void TestWhereENullabeEqualsNullReversed()
        {
            Assert<C>(c => null == c.ENullable, 4, "{ \"en\" : null }");
        }

        [Test]
        public void TestWhereLContains2()
        {
            Assert<C>(c => c.L.Contains(2), 1, "{ \"l\" : 2 }");
        }

        [Test]
        public void TestWhereLContains2Not()
        {
            Assert<C>(c => !c.L.Contains(2), 4, "{ \"l\" : { \"$ne\" : 2 } }");
        }

        [Test]
        public void TestWhereLCountMethodEquals3()
        {
            Assert<C>(c => c.L.Count() == 3, 1, "{ \"l\" : { \"$size\" : 3 } }");
        }

        [Test]
        public void TestWhereLCountMethodEquals3Not()
        {
            Assert<C>(c => !(c.L.Count() == 3), 4, "{ \"l\" : { \"$not\" : { \"$size\" : 3 } } }");
        }

        [Test]
        public void TestWhereLCountMethodEquals3Reversed()
        {
            Assert<C>(c => 3 == c.L.Count(), 1, "{ \"l\" : { \"$size\" : 3 } }");
        }

        [Test]
        public void TestWhereLCountPropertyEquals3()
        {
            Assert<C>(c => c.L.Count == 3, 1, "{ \"l\" : { \"$size\" : 3 } }");
        }

        [Test]
        public void TestWhereLCountPropertyEquals3Not()
        {
            Assert<C>(c => !(c.L.Count == 3), 4, "{ \"l\" : { \"$not\" : { \"$size\" : 3 } } }");
        }

        [Test]
        public void TestWhereLCountPropertyEquals3Reversed()
        {
            Assert<C>(c => 3 == c.L.Count, 1, "{ \"l\" : { \"$size\" : 3 } }");
        }

        [Test]
        public void TestWhereLCountPropertyNotEquals3()
        {
            Assert<C>(c => c.L.Count != 3, 4, "{ \"l\" : { \"$not\" : { \"$size\" : 3 } } }");
        }

        [Test]
        public void TestWhereLCountPropertyNotEquals3Not()
        {
            Assert<C>(c => !(c.L.Count != 3), 1, "{ \"l\" : { \"$size\" : 3 } }");
        }

        [Test]
        public void TestWhereLSub1Equals3()
        {
            Assert<C>(c => c.L[1] == 3, 1, "{ \"l.1\" : 3 }");
        }

        [Test]
        public void TestWhereLSub1Equals3Not()
        {
            Assert<C>(c => !(c.L[1] == 3), 4, "{ \"l.1\" : { \"$ne\" : 3 } }");
        }

        [Test]
        public void TestWhereLSub1ModTwoEquals1()
        {
            Assert<C>(c => c.L[1] % 2 == 1, 1, "{ \"l.1\" : { \"$mod\" : [NumberLong(2), NumberLong(1)] } }");
        }

        [Test]
        public void TestWhereLSub1ModTwoEquals1Not()
        {
            Assert<C>(c => !(c.L[1] % 2 == 1), 4, "{ \"l.1\" : { \"$not\" : { \"$mod\" : [NumberLong(2), NumberLong(1)] } } }");
        }

        [Test]
        public void TestWhereLSub1ModTwoNotEquals1()
        {
            Assert<C>(c => c.L[1] % 2 != 1, 4, "{ \"l.1\" : { \"$not\" : { \"$mod\" : [NumberLong(2), NumberLong(1)] } } }");
        }

        [Test]
        public void TestWhereLSub1ModTwoNotEquals1Not()
        {
            Assert<C>(c => !(c.L[1] % 2 != 1), 1, "{ \"l.1\" : { \"$mod\" : [NumberLong(2), NumberLong(1)] } }");
        }

        [Test]
        public void TestWhereLSub1NotEquals3()
        {
            Assert<C>(c => c.L[1] != 3, 4, "{ \"l.1\" : { \"$ne\" : 3 } }");
        }

        [Test]
        public void TestWhereLSub1NotEquals3Not()
        {
            Assert<C>(c => !(c.L[1] != 3), 1, "{ \"l.1\" : 3 }");
        }

        [Test]
        public void TestWhereLXModTwoEquals1()
        {
            Assert<C>(c => c.LX % 2 == 1, 3, "{ \"lx\" : { \"$mod\" : [NumberLong(2), NumberLong(1)] } }");
        }

        [Test]
        public void TestWhereLXModTwoEquals1Not()
        {
            Assert<C>(c => !(c.LX % 2 == 1), 2, "{ \"lx\" : { \"$not\" : { \"$mod\" : [NumberLong(2), NumberLong(1)] } } }");
        }

        [Test]
        public void TestWhereLXModTwoEquals1Reversed()
        {
            Assert<C>(c => 1 == c.LX % 2, 3, "{ \"lx\" : { \"$mod\" : [NumberLong(2), NumberLong(1)] } }");
        }

        [Test]
        public void TestWhereLXModTwoNotEquals1()
        {
            Assert<C>(c => c.LX % 2 != 1, 2, "{ \"lx\" : { \"$not\" : { \"$mod\" : [NumberLong(2), NumberLong(1)] } } }");
        }

        [Test]
        public void TestWhereLXModTwoNotEquals1Not()
        {
            Assert<C>(c => !(c.LX % 2 != 1), 3, "{ \"lx\" : { \"$mod\" : [NumberLong(2), NumberLong(1)] } }");
        }

        [Test]
        public void TestWhereSASub0ContainsO()
        {
            Assert<C>(c => c.SA[0].Contains("o"), 1, "{ \"sa.0\" : /o/s }");
        }

        [Test]
        public void TestWhereSASub0ContainsONot()
        {
            Assert<C>(c => !c.SA[0].Contains("o"), 4, "{ \"sa.0\" : { \"$not\" : /o/s } }");
        }

        [Test]
        public void TestWhereSASub0EndsWithM()
        {
            Assert<C>(c => c.SA[0].EndsWith("m"), 1, "{ \"sa.0\" : /m$/s }");
        }

        [Test]
        public void TestWhereSASub0EndsWithMNot()
        {
            Assert<C>(c => !c.SA[0].EndsWith("m"), 4, "{ \"sa.0\" : { \"$not\" : /m$/s } }");
        }

        [Test]
        public void TestWhereSASub0IsMatch()
        {
            var regex = new Regex(@"^T");
            Assert<C>(c => regex.IsMatch(c.SA[0]), 1, "{ \"sa.0\" : /^T/ }");
        }

        [Test]
        public void TestWhereSASub0IsMatchNot()
        {
            var regex = new Regex(@"^T");
            Assert<C>(c => !regex.IsMatch(c.SA[0]), 4, "{ \"sa.0\" : { \"$not\" : /^T/ } }");
        }

        [Test]
        public void TestWhereSASub0IsMatchStatic()
        {
            Assert<C>(c => Regex.IsMatch(c.SA[0], "^T"), 1, "{ \"sa.0\" : /^T/ }");
        }

        [Test]
        public void TestWhereSASub0IsMatchStaticNot()
        {
            Assert<C>(c => !Regex.IsMatch(c.SA[0], "^T"), 4, "{ \"sa.0\" : { \"$not\" : /^T/ } }");
        }

        [Test]
        public void TestWhereSASub0IsMatchStaticWithOptions()
        {
            Assert<C>(c => Regex.IsMatch(c.SA[0], "^t", RegexOptions.IgnoreCase), 1, "{ \"sa.0\" : /^t/i }");
        }

        [Test]
        public void TestWhereSASub0StartsWithT()
        {
            Assert<C>(c => c.SA[0].StartsWith("T"), 1, "{ \"sa.0\" : /^T/s }");
        }

        [Test]
        public void TestWhereSASub0StartsWithTNot()
        {
            Assert<C>(c => !c.SA[0].StartsWith("T"), 4, "{ \"sa.0\" : { \"$not\" : /^T/s } }");
        }

        [Test]
        public void TestWhereSContainsAbc()
        {
            Assert<C>(c => c.S.Contains("abc"), 1, "{ \"s\" : /abc/s }");
        }

        [Test]
        public void TestWhereSContainsAbcNot()
        {
            Assert<C>(c => !c.S.Contains("abc"), 4, "{ \"s\" : { \"$not\" : /abc/s } }");
        }

        [Test]
        public void TestWhereSContainsDot()
        {
            Assert<C>(c => c.S.Contains("."), 0, "{ \"s\" : /\\./s }");
        }

        [Test]
        public void TestWhereSCountEquals3()
        {
            Assert<C>(c => c.S.Count() == 3, 1, "{ \"s\" : /^.{3}$/s }");
        }

        [Test]
        public void TestWhereSEqualsAbc()
        {
            Assert<C>(c => c.S == "abc", 1, "{ \"s\" : \"abc\" }");
        }

        [Test]
        public void TestWhereSEqualsAbcNot()
        {
            Assert<C>(c => !(c.S == "abc"), 4, "{ \"s\" : { \"$ne\" : \"abc\" } }");
        }

        [Test]
        public void TestWhereSEqualsMethodAbc()
        {
            Assert<C>(c => c.S.Equals("abc"), 1, "{ \"s\" : \"abc\" }");
        }

        [Test]
        public void TestWhereSEqualsMethodAbcNot()
        {
            Assert<C>(c => !(c.S.Equals("abc")), 4, "{ \"s\" : { \"$ne\" : \"abc\" } }");
        }

        [Test]
        public void TestWhereSEqualsStaticMethodAbc()
        {
            Assert<C>(c => string.Equals(c.S, "abc"), 1, "{ \"s\" : \"abc\" }");
        }

        [Test]
        public void TestWhereSEqualsStaticMethodAbcNot()
        {
            Assert<C>(c => !string.Equals(c.S, "abc"), 4, "{ \"s\" : { \"$ne\" : \"abc\" } }");
        }

        [Test]
        public void TestWhereSEndsWithAbc()
        {
            Assert<C>(c => c.S.EndsWith("abc"), 1, "{ \"s\" : /abc$/s }");
        }

        [Test]
        public void TestWhereSEndsWithAbcNot()
        {
            Assert<C>(c => !c.S.EndsWith("abc"), 4, "{ \"s\" : { \"$not\" : /abc$/s } }");
        }

        [Test]
        public void TestWhereSIndexOfAnyBDashCEquals1()
        {
            Assert<C>(c => c.S.IndexOfAny(new char[] { 'b', '-', 'c' }) == 1, 1, "{ \"s\" : /^[^b\\-c]{1}[b\\-c]/s }");
        }

        [Test]
        public void TestWhereSIndexOfAnyBCStartIndex1Equals1()
        {
            Assert<C>(c => c.S.IndexOfAny(new char[] { 'b', '-', 'c' }, 1) == 1, 1, "{ \"s\" : /^.{1}[^b\\-c]{0}[b\\-c]/s }");
        }

        [Test]
        public void TestWhereSIndexOfAnyBCStartIndex1Count2Equals1()
        {
            Assert<C>(c => c.S.IndexOfAny(new char[] { 'b', '-', 'c' }, 1, 2) == 1, 1, "{ \"s\" : /^.{1}(?=.{2})[^b\\-c]{0}[b\\-c]/s }");
        }

        [Test]
        public void TestWhereSIndexOfBEquals1()
        {
            Assert<C>(c => c.S.IndexOf('b') == 1, 1, "{ \"s\" : /^[^b]{1}b/s }");
        }

        [Test]
        public void TestWhereSIndexOfBStartIndex1Equals1()
        {
            Assert<C>(c => c.S.IndexOf('b', 1) == 1, 1, "{ \"s\" : /^.{1}[^b]{0}b/s }");
        }

        [Test]
        public void TestWhereSIndexOfBStartIndex1Count2Equals1()
        {
            Assert<C>(c => c.S.IndexOf('b', 1, 2) == 1, 1, "{ \"s\" : /^.{1}(?=.{2})[^b]{0}b/s }");
        }

        [Test]
        public void TestWhereSIndexOfXyzEquals3()
        {
            Assert<C>(c => c.S.IndexOf("xyz") == 3, 1, "{ \"s\" : /^(?!.{0,2}xyz).{3}xyz/s }");
        }

        [Test]
        public void TestWhereSIndexOfXyzStartIndex1Equals3()
        {
            Assert<C>(c => c.S.IndexOf("xyz", 1) == 3, 1, "{ \"s\" : /^.{1}(?!.{0,1}xyz).{2}xyz/s }");
        }

        [Test]
        public void TestWhereSIndexOfXyzStartIndex1Count5Equals3()
        {
            Assert<C>(c => c.S.IndexOf("xyz", 1, 5) == 3, 1, "{ \"s\" : /^.{1}(?=.{5})(?!.{0,1}xyz).{2}xyz/s }");
        }

        [Test]
        public void TestWhereSIsMatch()
        {
            var regex = new Regex(@"^abc");
            Assert<C>(c => regex.IsMatch(c.S), 1, "{ \"s\" : /^abc/ }");
        }

        [Test]
        public void TestWhereSIsMatchNot()
        {
            var regex = new Regex(@"^abc");
            Assert<C>(c => !regex.IsMatch(c.S), 4, "{ \"s\" : { \"$not\" : /^abc/ } }");
        }

        [Test]
        public void TestWhereSIsMatchStatic()
        {
            Assert<C>(c => Regex.IsMatch(c.S, "^abc"), 1, "{ \"s\" : /^abc/ }");
        }

        [Test]
        public void TestWhereSIsMatchStaticNot()
        {
            Assert<C>(c => !Regex.IsMatch(c.S, "^abc"), 4, "{ \"s\" : { \"$not\" : /^abc/ } }");
        }

        [Test]
        public void TestWhereSIsMatchStaticWithOptions()
        {
            Assert<C>(c => Regex.IsMatch(c.S, "^abc", RegexOptions.IgnoreCase), 1, "{ \"s\" : /^abc/i }");
        }

        [Test]
        public void TestWhereSLengthEquals3()
        {
            Assert<C>(c => c.S.Length == 3, 1, "{ \"s\" : /^.{3}$/s }");
        }

        [Test]
        public void TestWhereSLengthEquals3Not()
        {
            Assert<C>(c => !(c.S.Length == 3), 4, "{ \"s\" : { \"$not\" : /^.{3}$/s } }");
        }

        [Test]
        public void TestWhereSLengthGreaterThan3()
        {
            Assert<C>(c => c.S.Length > 3, 1, "{ \"s\" : /^.{4,}$/s }");
        }

        [Test]
        public void TestWhereSLengthGreaterThanOrEquals3()
        {
            Assert<C>(c => c.S.Length >= 3, 2, "{ \"s\" : /^.{3,}$/s }");
        }

        [Test]
        public void TestWhereSLengthLessThan3()
        {
            Assert<C>(c => c.S.Length < 3, 0, "{ \"s\" : /^.{0,2}$/s }");
        }

        [Test]
        public void TestWhereSLengthLessThanOrEquals3()
        {
            Assert<C>(c => c.S.Length <= 3, 1, "{ \"s\" : /^.{0,3}$/s }");
        }

        [Test]
        public void TestWhereSLengthNotEquals3()
        {
            Assert<C>(c => c.S.Length != 3, 4, "{ \"s\" : { \"$not\" : /^.{3}$/s } }");
        }

        [Test]
        public void TestWhereSLengthNotEquals3Not()
        {
            Assert<C>(c => !(c.S.Length != 3), 1, "{ \"s\" : /^.{3}$/s }");
        }

        [Test]
        public void TestWhereSNotEqualsAbc()
        {
            Assert<C>(c => c.S != "abc", 4, "{ \"s\" : { \"$ne\" : \"abc\" } }");
        }

        [Test]
        public void TestWhereSNotEqualsAbcNot()
        {
            Assert<C>(c => !(c.S != "abc"), 1, "{ \"s\" : \"abc\" }");
        }

        [Test]
        public void TestWhereSStartsWithAbc()
        {
            Assert<C>(c => c.S.StartsWith("abc"), 1, "{ \"s\" : /^abc/s }");
        }

        [Test]
        public void TestWhereSStartsWithAbcNot()
        {
            Assert<C>(c => !c.S.StartsWith("abc"), 4, "{ \"s\" : { \"$not\" : /^abc/s } }");
        }

        [Test]
        public void TestWhereSSub1EqualsB()
        {
            Assert<C>(c => c.S[1] == 'b', 1, "{ \"s\" : /^.{1}b/s }");
        }

        [Test]
        public void TestWhereSSub1EqualsBNot()
        {
            Assert<C>(c => !(c.S[1] == 'b'), 4, "{ \"s\" : { \"$not\" : /^.{1}b/s } }");
        }

        [Test]
        public void TestWhereSSub1NotEqualsB()
        {
            Assert<C>(c => c.S[1] != 'b', 1, "{ \"s\" : /^.{1}[^b]/s }");
        }

        [Test]
        public void TestWhereSSub1NotEqualsBNot()
        {
            Assert<C>(c => !(c.S[1] != 'b'), 4, "{ \"s\" : { \"$not\" : /^.{1}[^b]/s } }");
        }

        [Test]
        public void TestWhereSTrimContainsXyz()
        {
            Assert<C>(c => c.S.Trim().Contains("xyz"), 1, "{ \"s\" : /^\\s*.*xyz.*\\s*$/s }");
        }

        [Test]
        public void TestWhereSTrimContainsXyzNot()
        {
            Assert<C>(c => !c.S.Trim().Contains("xyz"), 4, "{ \"s\" : { \"$not\" : /^\\s*.*xyz.*\\s*$/s } }");
        }

        [Test]
        public void TestWhereSTrimEndsWithXyz()
        {
            Assert<C>(c => c.S.Trim().EndsWith("xyz"), 1, "{ \"s\" : /^\\s*.*xyz\\s*$/s }");
        }

        [Test]
        public void TestWhereSTrimEndsWithXyzNot()
        {
            Assert<C>(c => !c.S.Trim().EndsWith("xyz"), 4, "{ \"s\" : { \"$not\" : /^\\s*.*xyz\\s*$/s } }");
        }

        [Test]
        public void TestWhereSTrimStartsWithXyz()
        {
            Assert<C>(c => c.S.Trim().StartsWith("xyz"), 1, "{ \"s\" : /^\\s*xyz.*\\s*$/s }");
        }

        [Test]
        public void TestWhereSTrimStartsWithXyzNot()
        {
            Assert<C>(c => !c.S.Trim().StartsWith("xyz"), 4, "{ \"s\" : { \"$not\" : /^\\s*xyz.*\\s*$/s } }");
        }

        [Test]
        public void TestWhereSTrimStartTrimEndToLowerContainsXyz()
        {
            Assert<C>(c => c.S.TrimStart(' ', '.', '-', '\t').TrimEnd().ToLower().Contains("xyz"), 1, "{ \"s\" : /^[\\ \\.\\-\\t]*.*xyz.*\\s*$/is }");
        }

        [Test]
        public void TestWhereSToLowerEqualsConstantLowerCaseValue()
        {
            Assert<C>(c => c.S.ToLower() == "abc", 1, "{ \"s\" : /^abc$/i }");
        }

        [Test]
        public void TestWhereSToLowerDoesNotEqualConstantLowerCaseValue()
        {
            Assert<C>(c => c.S.ToLower() != "abc", 4, "{ \"s\" : { \"$not\" : /^abc$/i } }");
        }

        [Test]
        public void TestWhereSToLowerEqualsConstantMixedCaseValue()
        {
            Assert<C>(c => c.S.ToLower() == "Abc", 0, "{ \"_id\" : { \"$type\" : -1 } }");
        }

        [Test]
        public void TestWhereSToLowerDoesNotEqualConstantMixedCaseValue()
        {
            Assert<C>(c => c.S.ToLower() != "Abc", 5, "{ }");
        }

        [Test]
        public void TestWhereSToLowerEqualsNullValue()
        {
            Assert<C>(c => c.S.ToLower() == null, 3, "{ \"s\" : null }");
        }

        [Test]
        public void TestWhereSToLowerDoesNotEqualNullValue()
        {
            Assert<C>(c => c.S.ToLower() != null, 2, "{ \"s\" : { \"$ne\" : null } }");
        }

        [Test]
        public void TestWhereSToUpperEqualsConstantLowerCaseValue()
        {
            Assert<C>(c => c.S.ToUpper() == "abc", 0, "{ \"_id\" : { \"$type\" : -1 } }");
        }

        [Test]
        public void TestWhereSToUpperDoesNotEqualConstantLowerCaseValue()
        {
            Assert<C>(c => c.S.ToUpper() != "abc", 5, "{ }");
        }

        [Test]
        public void TestWhereSToUpperEqualsConstantMixedCaseValue()
        {
            Assert<C>(c => c.S.ToUpper() == "Abc", 0, "{ \"_id\" : { \"$type\" : -1 } }");
        }

        [Test]
        public void TestWhereSToUpperDoesNotEqualConstantMixedCaseValue()
        {
            Assert<C>(c => c.S.ToUpper() != "Abc", 5, "{ }");
        }

        [Test]
        public void TestWhereSToUpperEqualsNullValue()
        {
            Assert<C>(c => c.S.ToUpper() == null, 3, "{ \"s\" : null }");
        }

        [Test]
        public void TestWhereSToUpperDoesNotEqualNullValue()
        {
            Assert<C>(c => c.S.ToUpper() != null, 2, "{ \"s\" : { \"$ne\" : null } }");
        }

        [Test]
        public void TestWhereTripleAnd()
        {
            // the query is a bit odd in order to force the built query to be promoted to $and form
            Assert<C>(c => c.X >= 0 && c.X >= 1 && c.Y == 11, 2, "{ \"$and\" : [{ \"x\" : { \"$gte\" : 0 } }, { \"x\" : { \"$gte\" : 1 } }, { \"y\" : 11 }] }");
        }

        [Test]
        public void TestWhereTripleOr()
        {
            Assert<C>(c => c.X == 1 || c.Y == 33 || c.S == "x is 1", 2, "{ \"$or\" : [{ \"x\" : 1 }, { \"y\" : 33 }, { \"s\" : \"x is 1\" }] }");
        }

        [Test]
        public void TestWhereXEquals1()
        {
            Assert<C>(c => c.X == 1, 1, "{ \"x\" : 1 }");
        }

        [Test]
        public void TestWhereXEquals1AndYEquals11()
        {
            Assert<C>(c => c.X == 1 & c.Y == 11, 1, "{ \"x\" : 1, \"y\" : 11 }");
        }

        [Test]
        public void TestWhereXEquals1AndAlsoYEquals11()
        {
            Assert<C>(c => c.X == 1 && c.Y == 11, 1, "{ \"x\" : 1, \"y\" : 11 }");
        }

        [Test]
        public void TestWhereXEquals1AndYEquals11Not()
        {
            Assert<C>(c => !(c.X == 1 && c.Y == 11), 4, "{ \"$nor\" : [{ \"x\" : 1, \"y\" : 11 }] }");
        }

        [Test]
        public void TestWhereXEquals1AndYEquals11AndZEquals11()
        {
            Assert<C>(c => c.X == 1 && c.Y == 11 && c.D.Z == 11, 1, "{ \"x\" : 1, \"y\" : 11, \"d.z\" : 11 }");
        }

        [Test]
        public void TestWhereXEquals1Not()
        {
            Assert<C>(c => !(c.X == 1), 4, "{ \"x\" : { \"$ne\" : 1 } }");
        }

        [Test]
        public void TestWhereXEquals1OrYEquals33()
        {
            Assert<C>(c => c.X == 1 | c.Y == 33, 2, "{ \"$or\" : [{ \"x\" : 1 }, { \"y\" : 33 }] }");
        }

        [Test]
        public void TestWhereXEquals1OrElseYEquals33()
        {
            Assert<C>(c => c.X == 1 || c.Y == 33, 2, "{ \"$or\" : [{ \"x\" : 1 }, { \"y\" : 33 }] }");
        }

        [Test]
        public void TestWhereXEquals1OrYEquals33Not()
        {
            Assert<C>(c => !(c.X == 1 || c.Y == 33), 3, "{ \"$nor\" : [{ \"x\" : 1 }, { \"y\" : 33 }] }");
        }

        [Test]
        public void TestWhereXEquals1OrYEquals33NotNot()
        {
            Assert<C>(c => !!(c.X == 1 || c.Y == 33), 2, "{ \"$or\" : [{ \"x\" : 1 }, { \"y\" : 33 }] }");
        }

        [Test]
        public void TestWhereXGreaterThan1()
        {
            Assert<C>(c => c.X > 1, 4, "{ \"x\" : { \"$gt\" : 1 } }");
        }

        [Test]
        public void TestWhereXGreaterThan1AndLessThan3()
        {
            Assert<C>(c => c.X > 1 && c.X < 3, 1, "{ \"x\" : { \"$gt\" : 1, \"$lt\" : 3 } }");
        }

        [Test]
        public void TestWhereXGreaterThan1AndLessThan3Not()
        {
            Assert<C>(c => !(c.X > 1 && c.X < 3), 4, "{ \"$nor\" : [{ \"x\" : { \"$gt\" : 1, \"$lt\" : 3 } }] }");
        }

        [Test]
        public void TestWhereXGreaterThan1Not()
        {
            Assert<C>(c => !(c.X > 1), 1, "{ \"x\" : { \"$not\" : { \"$gt\" : 1 } } }");
        }

        [Test]
        public void TestWhereXGreaterThan1Reversed()
        {
            Assert<C>(c => 1 < c.X, 4, "{ \"x\" : { \"$gt\" : 1 } }");
        }

        [Test]
        public void TestWhereXGreaterThanOrEquals1()
        {
            Assert<C>(c => c.X >= 1, 5, "{ \"x\" : { \"$gte\" : 1 } }");
        }

        [Test]
        public void TestWhereXGreaterThanOrEquals1Not()
        {
            Assert<C>(c => !(c.X >= 1), 0, "{ \"x\" : { \"$not\" : { \"$gte\" : 1 } } }");
        }

        [Test]
        public void TestWhereXGreaterThanOrEquals1Reversed()
        {
            Assert<C>(c => 1 <= c.X, 5, "{ \"x\" : { \"$gte\" : 1 } }");
        }

        [Test]
        public void TestWhereXLessThan1()
        {
            Assert<C>(c => c.X < 1, 0, "{ \"x\" : { \"$lt\" : 1 } }");
        }

        [Test]
        public void TestWhereXLessThan1Not()
        {
            Assert<C>(c => !(c.X < 1), 5, "{ \"x\" : { \"$not\" : { \"$lt\" : 1 } } }");
        }

        [Test]
        public void TestWhereXLessThan1Reversed()
        {
            Assert<C>(c => 1 > c.X, 0, "{ \"x\" : { \"$lt\" : 1 } }");
        }

        [Test]
        public void TestWhereXLessThanOrEquals1()
        {
            Assert<C>(c => c.X <= 1, 1, "{ \"x\" : { \"$lte\" : 1 } }");
        }

        [Test]
        public void TestWhereXLessThanOrEquals1Not()
        {
            Assert<C>(c => !(c.X <= 1), 4, "{ \"x\" : { \"$not\" : { \"$lte\" : 1 } } }");
        }

        [Test]
        public void TestWhereXLessThanOrEquals1Reversed()
        {
            Assert<C>(c => 1 >= c.X, 1, "{ \"x\" : { \"$lte\" : 1 } }");
        }

        [Test]
        public void TestWhereXModOneEquals0AndXModTwoEquals0()
        {
            Assert<C>(c => (c.X % 1 == 0) && (c.X % 2 == 0), 2, "{ \"$and\" : [{ \"x\" : { \"$mod\" : [NumberLong(1), NumberLong(0)] } }, { \"x\" : { \"$mod\" : [NumberLong(2), NumberLong(0)] } }] }");
        }

        [Test]
        public void TestWhereXModOneEquals0AndXModTwoEquals0Not()
        {
            Assert<C>(c => !((c.X % 1 == 0) && (c.X % 2 == 0)), 3, "{ \"$nor\" : [{ \"$and\" : [{ \"x\" : { \"$mod\" : [NumberLong(1), NumberLong(0)] } }, { \"x\" : { \"$mod\" : [NumberLong(2), NumberLong(0)] } }] }] }");
        }

        [Test]
        public void TestWhereXModOneEquals0AndXModTwoEquals0NotNot()
        {
            Assert<C>(c => !!((c.X % 1 == 0) && (c.X % 2 == 0)), 2, "{ \"$or\" : [{ \"$and\" : [{ \"x\" : { \"$mod\" : [NumberLong(1), NumberLong(0)] } }, { \"x\" : { \"$mod\" : [NumberLong(2), NumberLong(0)] } }] }] }");
        }

        [Test]
        public void TestWhereXModTwoEquals1()
        {
            Assert<C>(c => c.X % 2 == 1, 3, "{ \"x\" : { \"$mod\" : [NumberLong(2), NumberLong(1)] } }");
        }

        [Test]
        public void TestWhereXModTwoEquals1Not()
        {
            Assert<C>(c => !(c.X % 2 == 1), 2, "{ \"x\" : { \"$not\" : { \"$mod\" : [NumberLong(2), NumberLong(1)] } } }");
        }

        [Test]
        public void TestWhereXModTwoEquals1Reversed()
        {
            Assert<C>(c => 1 == c.X % 2, 3, "{ \"x\" : { \"$mod\" : [NumberLong(2), NumberLong(1)] } }");
        }

        [Test]
        public void TestWhereXModTwoNotEquals1()
        {
            Assert<C>(c => c.X % 2 != 1, 2, "{ \"x\" : { \"$not\" : { \"$mod\" : [NumberLong(2), NumberLong(1)] } } }");
        }

        [Test]
        public void TestWhereXModTwoNotEquals1Not()
        {
            Assert<C>(c => !(c.X % 2 != 1), 3, "{ \"x\" : { \"$mod\" : [NumberLong(2), NumberLong(1)] } }");
        }

        [Test]
        public void TestWhereXNotEquals1()
        {
            Assert<C>(c => c.X != 1, 4, "{ \"x\" : { \"$ne\" : 1 } }");
        }

        [Test]
        public void TestWhereXNotEquals1Not()
        {
            Assert<C>(c => !(c.X != 1), 1, "{ \"x\" : 1 }");
        }

        public void Assert<TDocument>(Expression<Func<TDocument, bool>> filter, int expectedCount, string expectedFilter)
        {
            var serializer = BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
            var filterDocument = PredicateTranslator.Translate(filter, serializer, BsonSerializer.SerializerRegistry);

            var list = _collection.FindSync<TDocument>(filterDocument).ToList();

            filterDocument.Should().Be(expectedFilter);
            list.Count.Should().Be(expectedCount);
        }

        private enum E
        {
            None,
            A,
            B,
            C
        }

        private class C
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
            [BsonElement("en")]
            [BsonRepresentation(BsonType.String)]
            public E? ENullable { get; set; }
            [BsonElement("ea")]
            [BsonIgnoreIfNull]
            public E[] EA { get; set; }
            [BsonElement("f")]
            public F F { get; set; }
            [BsonElement("sa")]
            [BsonIgnoreIfNull]
            public string[] SA { get; set; }
            [BsonElement("ba")]
            [BsonIgnoreIfNull]
            public bool[] BA { get; set; }
            [BsonElement("date")]
            public DateTime Date { get; set; }
            [BsonElement("nuldub")]
            public double? NullableDouble { get; set; }
        }

        private class D
        {
            [BsonElement("y")]
            [BsonIgnoreIfNull]
            [BsonDefaultValue(20)]
            public int? Y;
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
                return string.Format("new D {{ Y = {0}, Z = {1} }}", Y, Z);
            }
        }

        private class F
        {
            [BsonElement("g")]
            public G G { get; set; }
        }

        private class G
        {
            [BsonElement("h")]
            public int H { get; set; }
        }
    }
}
