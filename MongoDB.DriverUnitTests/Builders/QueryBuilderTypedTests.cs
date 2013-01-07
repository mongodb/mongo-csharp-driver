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

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Builders;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Builders
{
    [TestFixture]
    public class QueryBuilderTypedTests
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
            var query = Query<A>.All(a => a.J, new [] { 2, 4, 6});
            var expected = "{ \"j\" : { \"$all\" : [2, 4, 6] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestAll_Not()
        {
            var query = Query.Not(Query<A>.All(a => a.J, new[] { 2, 4, 6 }));
            var expected = "{ \"j\" : { \"$not\" : { \"$all\" : [2, 4, 6] } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestAnd()
        {
            var query = Query.And(
                    Query<A>.GTE(a => a.X, 3),
                    Query<A>.Where(a => a.X <= 10));

            var expected = "{ \"x\" : { \"$gte\" : 3, \"$lte\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestAndExpression()
        {
            var query = Query<A>.Where(a => a.X >= 3 && a.X <= 10);
            var expected = "{ \"x\" : { \"$gte\" : 3, \"$lte\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestAnd_Not()
        {
            var query = Query.Not(
                Query.And(
                    Query<A>.GTE(a => a.X, 3),
                    Query<A>.LTE(a => a.X, 10)));

            var expected = "{ \"$nor\" : [{ \"x\" : { \"$gte\" : 3, \"$lte\" : 10 } }] }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestAnd_NotExpression()
        {
            var query = Query<A>.Where(a => !(a.X >= 3 && a.X <= 10));
            var expected = "{ \"$nor\" : [{ \"x\" : { \"$gte\" : 3, \"$lte\" : 10 } }] }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestElemMatch()
        {
            var query = Query<A>.ElemMatch(
                a => a.A_B, 
                qb => qb.And(
                    qb.EQ(ab => ab.A, 1),
                    qb.GT(ab => ab.B, 1)));
                        
            var expected = "{ \"ab\" : { \"$elemMatch\" : { \"a\" : 1, \"b\" : { \"$gt\" : 1 } } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestElemMatch_Not()
        {
            var query = Query.Not(Query<A>.ElemMatch(
                a => a.A_B,
                qb => qb.And(
                    qb.EQ(ab => ab.A, 1),
                    qb.GT(ab => ab.B, 1))));

            var expected = "{ \"ab\" : { \"$not\" : { \"$elemMatch\" : { \"a\" : 1, \"b\" : { \"$gt\" : 1 } } } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestElemMatchExpression()
        {
            var query = Query<A>.Where(a => a.A_B.Any(ab => ab.A == 1 && ab.B > 1));

            var expected = "{ \"ab\" : { \"$elemMatch\" : { \"a\" : 1, \"b\" : { \"$gt\" : 1 } } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestEqual()
        {
            var query = Query<A>.EQ(a => a.X, 3);
            var expected = "{ \"x\" : 3 }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestEqualExpression()
        {
            var query = Query<A>.Where(a => a.X == 3);
            var expected = "{ \"x\" : 3 }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestEqual_NotExpression()
        {
            var query = Query<A>.Where(a => a.X != 3);
            var expected = "{ \"x\" : { \"$ne\" : 3 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestEqual_Array()
        {
            var query = Query<A>.EQ(a => a.J, 3);
            var expected = "{ \"j\" : 3 }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestExists()
        {
            var query = Query<A>.Exists(a => a.X);
            var expected = "{ \"x\" : { \"$exists\" : true } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThan()
        {
            var query = Query<A>.GT(a => a.X, 10);
            var expected = "{ \"x\" : { \"$gt\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanExpression()
        {
            var query = Query<A>.Where(a => a.X > 10);
            var expected = "{ \"x\" : { \"$gt\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThan_Not()
        {
            var query = Query.Not(Query<A>.GT(a => a.X, 10));
            var expected = "{ \"x\" : { \"$not\" : { \"$gt\" : 10 } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThan_NotExpression()
        {
            var query = Query<A>.Where(a => !(a.X > 10));
            var expected = "{ \"x\" : { \"$not\" : { \"$gt\" : 10 } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThan_Array()
        {
            var query = Query<A>.GT(a => a.J, 10);
            var expected = "{ \"j\" : { \"$gt\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanOrEqual()
        {
            var query = Query<A>.GTE(a => a.X, 10);
            var expected = "{ \"x\" : { \"$gte\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanOrEqualExpression()
        {
            var query = Query<A>.Where(a => a.X >= 10);
            var expected = "{ \"x\" : { \"$gte\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanOrEqual_Not()
        {
            var query = Query.Not(Query<A>.GTE(a => a.X, 10));
            var expected = "{ \"x\" : { \"$not\" : { \"$gte\" : 10 } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanOrEqual_NotExpression()
        {
            var query = Query<A>.Where(a => !(a.X >= 10));
            var expected = "{ \"x\" : { \"$not\" : { \"$gte\" : 10 } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanOrEqual_Array()
        {
            var query = Query<A>.GTE(a => a.J, 10);
            var expected = "{ \"j\" : { \"$gte\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestIn()
        {
            var query = Query<A>.In(a => a.X, new[] { 2, 4, 6 });
            var expected = "{ \"x\" : { \"$in\" : [2, 4, 6] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestInExpression()
        {
            var list = new[] { 2, 4, 6 };
            var query = Query<A>.Where(a => list.Contains(a.X));
            var expected = "{ \"x\" : { \"$in\" : [2, 4, 6] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestIn_Not()
        {
            var query = Query.Not(Query<A>.In(a => a.X, new[] { 2, 4, 6 }));
            var expected = "{ \"x\" : { \"$nin\" : [2, 4, 6] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestIn_NotExpression()
        {
            var list = new[] { 2, 4, 6 };
            var query = Query<A>.Where(a => !list.Contains(a.X));
            var expected = "{ \"x\" : { \"$nin\" : [2, 4, 6] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestIn_Array()
        {
            var query = Query<A>.In(a => a.J, new[] { 2, 4, 6 });
            var expected = "{ \"j\" : { \"$in\" : [2, 4, 6] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThan()
        {
            var query = Query<A>.LT(a => a.X, 10);
            var expected = "{ \"x\" : { \"$lt\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThanExpression()
        {
            var query = Query<A>.Where(a => a.X < 10);
            var expected = "{ \"x\" : { \"$lt\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThan_Not()
        {
            var query = Query.Not(Query<A>.LT(a => a.X, 10));
            var expected = "{ \"x\" : { \"$not\" : { \"$lt\" : 10 } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThan_NotExpression()
        {
            var query = Query<A>.Where(a => !(a.X < 10));
            var expected = "{ \"x\" : { \"$not\" : { \"$lt\" : 10 } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThan_Array()
        {
            var query = Query<A>.LT(a => a.J, 10);
            var expected = "{ \"j\" : { \"$lt\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThanOrEqual()
        {
            var query = Query<A>.LTE(a => a.X, 10);
            var expected = "{ \"x\" : { \"$lte\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThanOrEqualExpression()
        {
            var query = Query<A>.Where(a => a.X <= 10);
            var expected = "{ \"x\" : { \"$lte\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThanOrEqual_Not()
        {
            var query = Query.Not(Query<A>.LTE(a => a.X, 10));
            var expected = "{ \"x\" : { \"$not\" : { \"$lte\" : 10 } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThanOrEqual_NotExpression()
        {
            var query = Query<A>.Where(a => !(a.X <= 10));
            var expected = "{ \"x\" : { \"$not\" : { \"$lte\" : 10 } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThanOrEqual_Array()
        {
            var query = Query<A>.LTE(a => a.J, 10);
            var expected = "{ \"j\" : { \"$lte\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestMatchesPattern()
        {
            var query = Query<A>.Matches(a => a.S, "abc");
            var expected = "{ \"s\" : /abc/ }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestMatchesRegex()
        {
            var query = Query<A>.Matches(a => a.S, new Regex("abc", RegexOptions.IgnoreCase));
            var expected = "{ \"s\" : /abc/i }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestMatchesExpressionStringContains()
        {
            var query = Query<A>.Where(a => a.S.Contains("abc"));
            var expected = "{ \"s\" : /abc/s }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestMatchesExpressionRegex()
        {
            var query = Query<A>.Where(a => Regex.IsMatch(a.S, "abc"));
            var expected = "{ \"s\" : /abc/ }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestMatchesExpressionRegexVariable()
        {
            var regex = new Regex("abc", RegexOptions.Singleline);
            var query = Query<A>.Where(a => regex.IsMatch(a.S));
            var expected = "{ \"s\" : /abc/s }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestMatchesExpressionStringEndsWith()
        {
            var query = Query<A>.Where(a => a.S.EndsWith("abc"));
            var expected = "{ \"s\" : /abc$/s }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestMatchesExpressionStringStartsWith()
        {
            var query = Query<A>.Where(a => a.S.StartsWith("abc"));
            var expected = "{ \"s\" : /^abc/s }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestMatches_Not()
        {
            var query = Query.Not(Query<A>.Matches(a => a.S, "abc"));
            var expected = "{ \"s\" : { \"$not\" : /abc/ } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestMatches_NotExpressionStringContains()
        {
            var query = Query<A>.Where(a => !a.S.Contains("abc"));
            var expected = "{ \"s\" : { \"$not\" : /abc/s } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestMatchesPattern_Array()
        {
            var query = Query<A>.Matches(a => a.L, "abc");
            var expected = "{ \"l\" : /abc/ }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestMod()
        {
            var query = Query<A>.Mod(a => a.X, 10, 1);
            var expected = "{ \"x\" : { \"$mod\" : [10, 1] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestModExpression()
        {
            var query = Query<A>.Where(a => a.X % 10 == 1);
            var expected = "{ \"x\" : { \"$mod\" : [10, 1] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestMod_Not()
        {
            var query = Query.Not(Query<A>.Mod(a => a.X, 10, 1));
            var expected = "{ \"x\" : { \"$not\" : { \"$mod\" : [10, 1] } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestMod_NotExpression()
        {
            var query = Query<A>.Where(a => a.X % 10 != 1);
            var expected = "{ \"x\" : { \"$not\" : { \"$mod\" : [10, 1] } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestMod_Array()
        {
            var query = Query<A>.Mod(a => a.J, 10, 1);
            var expected = "{ \"j\" : { \"$mod\" : [10, 1] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestNear()
        {
            var query = Query<A>.Near(a => a.S, 1.1, 2.2);
            var expected = "{ \"s\" : { \"$near\" : [1.1, 2.2] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestNear_Not()
        {
            var query = Query.Not(Query<A>.Near(a => a.S, 1.1, 2.2));
            var expected = "{ \"s\" : { \"$not\" : { \"$near\" : [1.1, 2.2] } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestNearWithMaxDistance()
        {
            var query = Query<A>.Near(a => a.S, 1.1, 2.2, 3.3);
            var expected = "{ \"s\" : { \"$near\" : [1.1, 2.2], \"$maxDistance\" : 3.3 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestNearWithMaxDistance_Not()
        {
            var query = Query.Not(Query<A>.Near(a => a.S, 1.1, 2.2, 3.3));
            var expected = "{ \"$nor\" : [{ \"s\" : { \"$near\" : [1.1, 2.2], \"$maxDistance\" : 3.3 } }] }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestNearWithSphericalTrue()
        {
            var query = Query.Not(Query<A>.Near(a => a.S, 1.1, 2.2, 3.3, true));
            var expected = "{ \"$nor\" : [{ \"s\" : { \"$nearSphere\" : [1.1, 2.2], \"$maxDistance\" : 3.3 } }] }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestNearWithSphericalTrue_Not()
        {
            var query = Query.Not(Query<A>.Near(a => a.S, 1.1, 2.2, 3.3, true));
            var expected = "{ \"$nor\" : [{ \"s\" : { \"$nearSphere\" : [1.1, 2.2], \"$maxDistance\" : 3.3 } }] }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestNotEqual()
        {
            var query = Query<A>.NE(a => a.X, 3);
            var expected = "{ \"x\" : { \"$ne\" : 3 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestNotEqual_Array()
        {
            var query = Query<A>.NE(a => a.J, 3);
            var expected = "{ \"j\" : { \"$ne\" : 3 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestNotExists()
        {
            var query = Query<A>.NotExists(a => a.X);
            var expected = "{ \"x\" : { \"$exists\" : false } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestOr()
        {
            var query = Query.Or(
                Query<A>.GTE(a => a.X, 3),
                Query<A>.LTE(a => a.X, 10));

            var expected = "{ \"$or\" : [{ \"x\" : { \"$gte\" : 3 } }, { \"x\" : { \"$lte\" : 10 } }] }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestOr_Not()
        {
            var query = Query.Not(
                Query.Or(
                    Query<A>.GTE(a => a.X, 3),
                    Query<A>.LTE(a => a.X, 10)));

            var expected = "{ \"$nor\" : [{ \"x\" : { \"$gte\" : 3 } }, { \"x\" : { \"$lte\" : 10 } }] }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestOrExpression()
        {
            var query = Query<A>.Where(a => a.X >= 3 || a.X <= 10);

            var expected = "{ \"$or\" : [{ \"x\" : { \"$gte\" : 3 } }, { \"x\" : { \"$lte\" : 10 } }] }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestOrExpression_Not()
        {
            var query = Query<A>.Where(a => !(a.X >= 3 || a.X <= 10));

            var expected = "{ \"$nor\" : [{ \"x\" : { \"$gte\" : 3 } }, { \"x\" : { \"$lte\" : 10 } }] }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestSize()
        {
            var query = Query<A>.Size(a => a.J, 20);
            var expected = "{ \"j\" : { \"$size\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestSize_Not()
        {
            var query = Query.Not(Query<A>.Size(a => a.J, 20));
            var expected = "{ \"j\" : { \"$not\" : { \"$size\" : 20 } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestSizeExpressionArray()
        {
            var query = Query<A>.Where(a => a.J.Length == 20);
            var expected = "{ \"j\" : { \"$size\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestSizeExpressionArray_Not()
        {
            var query = Query<A>.Where(a => a.J.Length != 20);
            var expected = "{ \"j\" : { \"$not\" : { \"$size\" : 20 } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestSizeExpressionIEnumerable()
        {
            var query = Query<A>.Where(a => a.A_B.Count() == 20);
            var expected = "{ \"ab\" : { \"$size\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestSizeExpressionIEnumerable_Not()
        {
            var query = Query<A>.Where(a => a.A_B.Count() != 20);
            var expected = "{ \"ab\" : { \"$not\" : { \"$size\" : 20 } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestSizeExpressionList()
        {
            var query = Query<A>.Where(a => a.L.Count == 20);
            var expected = "{ \"l\" : { \"$size\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestSizeExpressionList_Not()
        {
            var query = Query<A>.Where(a => a.L.Count != 20);
            var expected = "{ \"l\" : { \"$not\" : { \"$size\" : 20 } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestType()
        {
            var query = Query<A>.Type(a => a.S, BsonType.String);
            var expected = "{ \"s\" : { \"$type\" : 2 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestType_Not()
        {
            var query = Query.Not(Query<A>.Type(a => a.S, BsonType.String));
            var expected = "{ \"s\" : { \"$not\" : { \"$type\" : 2 } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestType_Array()
        {
            var query = Query<A>.Type(a => a.J, BsonType.String);
            var expected = "{ \"j\" : { \"$type\" : 2 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestWhere()
        {
            var query = Query.Where("this.a > 3");
            var expected = "{ \"$where\" : { \"$code\" : \"this.a > 3\" } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestWithinCircle()
        {
            var query = Query<A>.WithinCircle(a => a.X, 1.1, 2.2, 3.3);
            var expected = "{ \"x\" : { \"$within\" : { \"$center\" : [[1.1, 2.2], 3.3] } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestWithinCircle_Not()
        {
            var query = Query.Not(Query<A>.WithinCircle(a => a.X, 1.1, 2.2, 3.3));
            var expected = "{ \"x\" : { \"$not\" : { \"$within\" : { \"$center\" : [[1.1, 2.2], 3.3] } } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestWithinCircleSpherical()
        {
            var query = Query<A>.WithinCircle(a => a.X, 1.1, 2.2, 3.3, true);
            var expected = "{ \"x\" : { \"$within\" : { \"$centerSphere\" : [[1.1, 2.2], 3.3] } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestWithinCircleSpherical_Not()
        {
            var query = Query.Not(Query<A>.WithinCircle(a => a.X, 1.1, 2.2, 3.3, true));
            var expected = "{ \"x\" : { \"$not\" : { \"$within\" : { \"$centerSphere\" : [[1.1, 2.2], 3.3] } } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestWithinPolygon()
        {
            var points = new double[,] { { 1.1, 2.2 }, { 3.3, 4.4 } };
            var query = Query<A>.WithinPolygon(a => a.X, points);
            var expected = "{ \"x\" : { \"$within\" : { \"$polygon\" : [[1.1, 2.2], [3.3, 4.4]] } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestWithinPolygon_Not()
        {
            var points = new double[,] { { 1.1, 2.2 }, { 3.3, 4.4 } };
            var query = Query.Not(Query<A>.WithinPolygon(a => a.X, points));
            var expected = "{ \"x\" : { \"$not\" : { \"$within\" : { \"$polygon\" : [[1.1, 2.2], [3.3, 4.4]] } } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestWithinRectangle()
        {
            var query = Query<A>.WithinRectangle(a => a.X, 1.1, 2.2, 3.3, 4.4);
            var expected = "{ \"x\" : { \"$within\" : { \"$box\" : [[1.1, 2.2], [3.3, 4.4]] } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestWithinRectangle_Not()
        {
            var query = Query.Not(Query<A>.WithinRectangle(a => a.X, 1.1, 2.2, 3.3, 4.4));
            var expected = "{ \"x\" : { \"$not\" : { \"$within\" : { \"$box\" : [[1.1, 2.2], [3.3, 4.4]] } } } }";
            Assert.AreEqual(expected, query.ToJson());
        }
    }
}