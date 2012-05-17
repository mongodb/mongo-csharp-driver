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
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.RegularExpressions;

namespace MongoDB.DriverUnitTests.Builders
{
    [TestFixture]
    public class TypedQueryBuilderTests
    {
        private class A
        {
            [BsonElementAttribute("ab")]
            public IEnumerable<A_B> A_B { get; set; }

            [BsonElementAttribute("j")]
            public int[] J { get; set; }

            [BsonElementAttribute("x")]
            public int X { get; set; }

            [BsonElementAttribute("s")]
            public string S { get; set; }

            [BsonElementAttribute("l")]
            public List<string> L { get; set; }
        }

        private class A_B
        {
            [BsonElementAttribute("a")]
            public int A { get; set; }

            [BsonElementAttribute("b")]
            public int B { get; set; }
        }

        [Test]
        public void TestAll()
        {
            var query = Query.Build<A>(qb => qb.All(a => a.J, new [] { 2, 4, 6}));
            var expected = "{ \"j\" : { \"$all\" : [2, 4, 6] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestAll_Not()
        {
            var query = Query.Build<A>(qb => qb.Not(qb.All(a => a.J, new[] { 2, 4, 6 })));
            var expected = "{ \"j\" : { \"$not\" : { \"$all\" : [2, 4, 6] } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestAnd()
        {
            var query = Query.Build<A>(qb =>
                qb.And(
                    qb.GTE(a => a.X, 3),
                    qb.Where(a => a.X <= 10)));

            var expected = "{ \"x\" : { \"$gte\" : 3, \"$lte\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestAndExpression()
        {
            var query = Query.Where<A>(a => a.X >= 3 && a.X <= 10);
            var expected = "{ \"x\" : { \"$gte\" : 3, \"$lte\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestAnd_Not()
        {
            var query = Query.Build<A>(qb =>
                qb.Not(qb.And(
                    qb.GTE(a => a.X, 3),
                    qb.LTE(a => a.X, 10))));

            var expected = "{ \"$nor\" : [{ \"x\" : { \"$gte\" : 3, \"$lte\" : 10 } }] }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestAnd_NotExpression()
        {
            var query = Query.Where<A>(a => !(a.X >= 3 && a.X <= 10));
            var expected = "{ \"$nor\" : [{ \"x\" : { \"$gte\" : 3, \"$lte\" : 10 } }] }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestElemMatch()
        {
            var query = Query.Build<A>(qb =>
                qb.ElemMatch(a => a.A_B, eqb =>
                    eqb.And(
                        eqb.EQ(ab => ab.A, 1),
                        eqb.GT(ab => ab.B, 1))));
                        
            var expected = "{ \"ab\" : { \"$elemMatch\" : { \"a\" : 1, \"b\" : { \"$gt\" : 1 } } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestElemMatch_Not()
        {
            var query = Query.Build<A>(qb =>
                qb.Not(qb.ElemMatch(a => a.A_B, eqb =>
                    eqb.And(
                        eqb.EQ(ab => ab.A, 1),
                        eqb.GT(ab => ab.B, 1)))));

            var expected = "{ \"ab\" : { \"$not\" : { \"$elemMatch\" : { \"a\" : 1, \"b\" : { \"$gt\" : 1 } } } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestElemMatchExpression()
        {
            var query = Query.Where<A>(a => a.A_B.Any(ab => ab.A == 1 && ab.B > 1));

            var expected = "{ \"ab\" : { \"$elemMatch\" : { \"a\" : 1, \"b\" : { \"$gt\" : 1 } } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestEqual()
        {
            var query = Query.Build<A>(qb => qb.EQ(a => a.X, 3));
            var expected = "{ \"x\" : 3 }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestEqualExpression()
        {
            var query = Query.Where<A>(a => a.X == 3);
            var expected = "{ \"x\" : 3 }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestEqual_NotExpression()
        {
            var query = Query.Where<A>(a => a.X != 3);
            var expected = "{ \"x\" : { \"$ne\" : 3 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestExists()
        {
            var query = Query.Build<A>(qb => qb.Exists(a => a.X));
            var expected = "{ \"x\" : { \"$exists\" : true } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThan()
        {
            var query = Query.Build<A>(qb => qb.GT(a => a.X, 10));
            var expected = "{ \"x\" : { \"$gt\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanExpression()
        {
            var query = Query.Where<A>(a => a.X > 10);
            var expected = "{ \"x\" : { \"$gt\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThan_Not()
        {
            var query = Query.Build<A>(qb => qb.Not(qb.GT(a => a.X, 10)));
            var expected = "{ \"x\" : { \"$not\" : { \"$gt\" : 10 } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThan_NotExpression()
        {
            var query = Query.Where<A>(a => !(a.X > 10));
            var expected = "{ \"x\" : { \"$not\" : { \"$gt\" : 10 } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanOrEqual()
        {
            var query = Query.Build<A>(qb => qb.GTE(a => a.X, 10));
            var expected = "{ \"x\" : { \"$gte\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanOrEqualExpression()
        {
            var query = Query.Where<A>(a => a.X >= 10);
            var expected = "{ \"x\" : { \"$gte\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanOrEqual_Not()
        {
            var query = Query.Build<A>(qb => qb.Not(qb.GTE(a => a.X, 10)));
            var expected = "{ \"x\" : { \"$not\" : { \"$gte\" : 10 } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanOrEqual_NotExpression()
        {
            var query = Query.Where<A>(a => !(a.X >= 10));
            var expected = "{ \"x\" : { \"$not\" : { \"$gte\" : 10 } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestIn()
        {
            var query = Query.Build<A>(qb => qb.In(a => a.X, new[] { 2, 4, 6 }));
            var expected = "{ \"x\" : { \"$in\" : [2, 4, 6] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestInExpression()
        {
            var list = new[] { 2, 4, 6 };
            var query = Query.Where<A>(a => list.Contains(a.X));
            var expected = "{ \"x\" : { \"$in\" : [2, 4, 6] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestIn_Not()
        {
            var query = Query.Build<A>(qb => qb.Not(qb.In(a => a.X, new[] { 2, 4, 6 })));
            var expected = "{ \"x\" : { \"$nin\" : [2, 4, 6] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestIn_NotExpression()
        {
            var list = new[] { 2, 4, 6 };
            var query = Query.Where<A>(a => !list.Contains(a.X));
            var expected = "{ \"x\" : { \"$nin\" : [2, 4, 6] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThan()
        {
            var query = Query.Build<A>(qb => qb.LT(a => a.X, 10));
            var expected = "{ \"x\" : { \"$lt\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThanExpression()
        {
            var query = Query.Where<A>(a => a.X < 10);
            var expected = "{ \"x\" : { \"$lt\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThan_Not()
        {
            var query = Query.Build<A>(qb => qb.Not(qb.LT(a => a.X, 10)));
            var expected = "{ \"x\" : { \"$not\" : { \"$lt\" : 10 } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThan_NotExpression()
        {
            var query = Query.Where<A>(a => !(a.X < 10));
            var expected = "{ \"x\" : { \"$not\" : { \"$lt\" : 10 } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThanOrEqual()
        {
            var query = Query.Build<A>(qb => qb.LTE(a => a.X, 10));
            var expected = "{ \"x\" : { \"$lte\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThanOrEqualExpression()
        {
            var query = Query.Where<A>(a => a.X <= 10);
            var expected = "{ \"x\" : { \"$lte\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThanOrEqual_Not()
        {
            var query = Query.Build<A>(qb => qb.Not(qb.LTE(a => a.X, 10)));
            var expected = "{ \"x\" : { \"$not\" : { \"$lte\" : 10 } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThanOrEqual_NotExpression()
        {
            var query = Query.Where<A>(a => !(a.X <= 10));
            var expected = "{ \"x\" : { \"$not\" : { \"$lte\" : 10 } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestMatches()
        {
            var query = Query.Build<A>(qb => qb.Matches(a => a.S, "abc"));
            var expected = "{ \"s\" : /abc/ }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestMatchesExpressionStringContains()
        {
            var query = Query.Where<A>(a => a.S.Contains("abc"));
            var expected = "{ \"s\" : /abc/s }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestMatchesExpressionRegex()
        {
            var query = Query.Where<A>(a => Regex.IsMatch(a.S, "abc"));
            var expected = "{ \"s\" : /abc/ }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestMatchesExpressionRegexVariable()
        {
            var regex = new Regex("abc", RegexOptions.Singleline);
            var query = Query.Where<A>(a => regex.IsMatch(a.S));
            var expected = "{ \"s\" : /abc/s }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestMatchesExpressionStringEndsWith()
        {
            var query = Query.Where<A>(a => a.S.EndsWith("abc"));
            var expected = "{ \"s\" : /abc$/s }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestMatchesExpressionStringStartsWith()
        {
            var query = Query.Where<A>(a => a.S.StartsWith("abc"));
            var expected = "{ \"s\" : /^abc/s }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestMatches_Not()
        {
            var query = Query.Build<A>(qb => qb.Not(qb.Matches(a => a.S, "abc")));
            var expected = "{ \"s\" : { \"$not\" : /abc/ } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestMatches_NotExpressionStringContains()
        {
            var query = Query.Where<A>(a => !a.S.Contains("abc"));
            var expected = "{ \"s\" : { \"$not\" : /abc/s } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestMod()
        {
            var query = Query.Build<A>(qb => qb.Mod(a => a.X, 10, 1));
            var expected = "{ \"x\" : { \"$mod\" : [10, 1] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestModExpression()
        {
            var query = Query.Where<A>(a => a.X % 10 == 1);
            var expected = "{ \"x\" : { \"$mod\" : [10, 1] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestMod_Not()
        {
            var query = Query.Build<A>(qb => qb.Not(qb.Mod(a => a.X, 10, 1)));
            var expected = "{ \"x\" : { \"$not\" : { \"$mod\" : [10, 1] } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestMod_NotExpression()
        {
            var query = Query.Where<A>(a => a.X % 10 != 1);
            var expected = "{ \"x\" : { \"$not\" : { \"$mod\" : [10, 1] } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestNear()
        {
            var query = Query.Build<A>(qb => qb.Near(a => a.S, 1.1, 2.2));
            var expected = "{ \"s\" : { \"$near\" : [1.1, 2.2] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestNear_Not()
        {
            var query = Query.Build<A>(qb => qb.Not(qb.Near(a => a.S, 1.1, 2.2)));
            var expected = "{ \"s\" : { \"$not\" : { \"$near\" : [1.1, 2.2] } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestNearWithMaxDistance()
        {
            var query = Query.Build<A>(qb => qb.Near(a => a.S, 1.1, 2.2, 3.3));
            var expected = "{ \"s\" : { \"$near\" : [1.1, 2.2], \"$maxDistance\" : 3.3 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestNearWithMaxDistance_Not()
        {
            var query = Query.Build<A>(qb => qb.Not(qb.Near(a => a.S, 1.1, 2.2, 3.3)));
            var expected = "{ \"$nor\" : [{ \"s\" : { \"$near\" : [1.1, 2.2], \"$maxDistance\" : 3.3 } }] }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestNearWithSphericalTrue()
        {
            var query = Query.Build<A>(qb => qb.Not(qb.Near(a => a.S, 1.1, 2.2, 3.3, true)));
            var expected = "{ \"$nor\" : [{ \"s\" : { \"$nearSphere\" : [1.1, 2.2], \"$maxDistance\" : 3.3 } }] }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestNearWithSphericalTrue_Not()
        {
            var query = Query.Build<A>(qb => qb.Not(qb.Near(a => a.S, 1.1, 2.2, 3.3, true)));
            var expected = "{ \"$nor\" : [{ \"s\" : { \"$nearSphere\" : [1.1, 2.2], \"$maxDistance\" : 3.3 } }] }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestNotEqual()
        {
            var query = Query.Build<A>(qb => qb.NE(a => a.X, 3));
            var expected = "{ \"x\" : { \"$ne\" : 3 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestNotExists()
        {
            var query = Query.Build<A>(qb => qb.NotExists(a => a.X));
            var expected = "{ \"x\" : { \"$exists\" : false } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestOr()
        {
            var query = Query.Build<A>(qb =>
                qb.Or(
                    qb.GTE(a => a.X, 3),
                    qb.LTE(a => a.X, 10)));

            var expected = "{ \"$or\" : [{ \"x\" : { \"$gte\" : 3 } }, { \"x\" : { \"$lte\" : 10 } }] }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestOr_Not()
        {
            var query = Query.Build<A>(qb =>
                qb.Not(qb.Or(
                    qb.GTE(a => a.X, 3),
                    qb.LTE(a => a.X, 10))));

            var expected = "{ \"$nor\" : [{ \"x\" : { \"$gte\" : 3 } }, { \"x\" : { \"$lte\" : 10 } }] }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestOrExpression()
        {
            var query = Query.Where<A>(a => a.X >= 3 || a.X <= 10);

            var expected = "{ \"$or\" : [{ \"x\" : { \"$gte\" : 3 } }, { \"x\" : { \"$lte\" : 10 } }] }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestOrExpression_Not()
        {
            var query = Query.Where<A>(a => !(a.X >= 3 || a.X <= 10));

            var expected = "{ \"$nor\" : [{ \"x\" : { \"$gte\" : 3 } }, { \"x\" : { \"$lte\" : 10 } }] }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestSize()
        {
            var query = Query.Build<A>(qb => qb.Size(a => a.J, 20));
            var expected = "{ \"j\" : { \"$size\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestSize_Not()
        {
            var query = Query.Build<A>(qb => qb.Not(qb.Size(a => a.J, 20)));
            var expected = "{ \"j\" : { \"$not\" : { \"$size\" : 20 } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestSizeExpressionArray()
        {
            var query = Query.Where<A>(a => a.J.Length == 20);
            var expected = "{ \"j\" : { \"$size\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestSizeExpressionArray_Not()
        {
            var query = Query.Where<A>(a => a.J.Length != 20);
            var expected = "{ \"j\" : { \"$not\" : { \"$size\" : 20 } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestSizeExpressionIEnumerable()
        {
            var query = Query.Where<A>(a => a.A_B.Count() == 20);
            var expected = "{ \"ab\" : { \"$size\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestSizeExpressionIEnumerable_Not()
        {
            var query = Query.Where<A>(a => a.A_B.Count() != 20);
            var expected = "{ \"ab\" : { \"$not\" : { \"$size\" : 20 } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestSizeExpressionList()
        {
            var query = Query.Where<A>(a => a.L.Count == 20);
            var expected = "{ \"l\" : { \"$size\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestSizeExpressionList_Not()
        {
            var query = Query.Where<A>(a => a.L.Count != 20);
            var expected = "{ \"l\" : { \"$not\" : { \"$size\" : 20 } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestType()
        {
            var query = Query.Build<A>(qb => qb.Type(a => a.S, BsonType.String));
            var expected = "{ \"s\" : { \"$type\" : 2 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestType_Not()
        {
            var query = Query.Build<A>(qb => qb.Not(qb.Type(a => a.S, BsonType.String)));
            var expected = "{ \"s\" : { \"$not\" : { \"$type\" : 2 } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestWhere()
        {
            var query = Query.Build<A>(qb => qb.Where("this.a > 3"));
            var expected = "{ \"$where\" : { \"$code\" : \"this.a > 3\" } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestWithinCircle()
        {
            var query = Query.Build<A>(qb => qb.WithinCircle(a => a.X, 1.1, 2.2, 3.3));
            var expected = "{ \"x\" : { \"$within\" : { \"$center\" : [[1.1, 2.2], 3.3] } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestWithinCircle_Not()
        {
            var query = Query.Build<A>(qb => qb.Not(qb.WithinCircle(a => a.X, 1.1, 2.2, 3.3)));
            var expected = "{ \"x\" : { \"$not\" : { \"$within\" : { \"$center\" : [[1.1, 2.2], 3.3] } } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestWithinCircleSpherical()
        {
            var query = Query.Build<A>(qb => qb.WithinCircle(a => a.X, 1.1, 2.2, 3.3, true));
            var expected = "{ \"x\" : { \"$within\" : { \"$centerSphere\" : [[1.1, 2.2], 3.3] } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestWithinCircleSpherical_Not()
        {
            var query = Query.Build<A>(qb => qb.Not(qb.WithinCircle(a => a.X, 1.1, 2.2, 3.3, true)));
            var expected = "{ \"x\" : { \"$not\" : { \"$within\" : { \"$centerSphere\" : [[1.1, 2.2], 3.3] } } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestWithinPolygon()
        {
            var points = new double[,] { { 1.1, 2.2 }, { 3.3, 4.4 } };
            var query = Query.Build<A>(qb => qb.WithinPolygon(a => a.X, points));
            var expected = "{ \"x\" : { \"$within\" : { \"$polygon\" : [[1.1, 2.2], [3.3, 4.4]] } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestWithinPolygon_Not()
        {
            var points = new double[,] { { 1.1, 2.2 }, { 3.3, 4.4 } };
            var query = Query.Build<A>(qb => qb.Not(qb.WithinPolygon(a => a.X, points)));
            var expected = "{ \"x\" : { \"$not\" : { \"$within\" : { \"$polygon\" : [[1.1, 2.2], [3.3, 4.4]] } } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestWithinRectangle()
        {
            var query = Query.Build<A>(qb => qb.WithinRectangle(a => a.X, 1.1, 2.2, 3.3, 4.4));
            var expected = "{ \"x\" : { \"$within\" : { \"$box\" : [[1.1, 2.2], [3.3, 4.4]] } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestWithinRectangle_Not()
        {
            var query = Query.Build<A>(qb => qb.Not(qb.WithinRectangle(a => a.X, 1.1, 2.2, 3.3, 4.4)));
            var expected = "{ \"x\" : { \"$not\" : { \"$within\" : { \"$box\" : [[1.1, 2.2], [3.3, 4.4]] } } } }";
            Assert.AreEqual(expected, query.ToJson());
        }
    }
}