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

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Builders;
using MongoDB.Driver.GeoJsonObjectModel;
using Xunit;

namespace MongoDB.Driver.Tests.Builders
{
    public class QueryBuilderTypedTests
    {
        private class A
        {
            [BsonElementAttribute("ab")]
            public IEnumerable<A_B> A_B { get; set; }

            [BsonElementAttribute("b")]
            public bool B { get; set; }

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

        [Fact]
        public void TestAll()
        {
            var query = Query<A>.All(a => a.J, new[] { 2, 4, 6 });
            var expected = "{ \"j\" : { \"$all\" : [2, 4, 6] } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestAll_Not()
        {
            var query = Query.Not(Query<A>.All(a => a.J, new[] { 2, 4, 6 }));
            var expected = "{ \"j\" : { \"$not\" : { \"$all\" : [2, 4, 6] } } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestAnd()
        {
            var query = Query.And(
                    Query<A>.GTE(a => a.X, 3),
                    Query<A>.Where(a => a.X <= 10));

            var expected = "{ \"x\" : { \"$gte\" : 3, \"$lte\" : 10 } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestAndExpression()
        {
            var query = Query<A>.Where(a => a.X >= 3 && a.X <= 10);
            var expected = "{ \"x\" : { \"$gte\" : 3, \"$lte\" : 10 } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestAnd_Not()
        {
            var query = Query.Not(
                Query.And(
                    Query<A>.GTE(a => a.X, 3),
                    Query<A>.LTE(a => a.X, 10)));

            var expected = "{ \"$nor\" : [{ \"x\" : { \"$gte\" : 3, \"$lte\" : 10 } }] }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestAnd_NotExpression()
        {
            var query = Query<A>.Where(a => !(a.X >= 3 && a.X <= 10));
            var expected = "{ \"$nor\" : [{ \"x\" : { \"$gte\" : 3, \"$lte\" : 10 } }] }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestBitsAllClear()
        {
            var query = Query<A>.BitsAllClear(x => x.X, 3);
            var expected = "{ \"x\" : { \"$bitsAllClear\" : NumberLong(3) } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestBitsAllSet()
        {
            var query = Query<A>.BitsAllSet(x => x.X, 3);
            var expected = "{ \"x\" : { \"$bitsAllSet\" : NumberLong(3) } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestBitsAnyClear()
        {
            var query = Query<A>.BitsAnyClear(x => x.X, 3);
            var expected = "{ \"x\" : { \"$bitsAnyClear\" : NumberLong(3) } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestBitsAnySet()
        {
            var query = Query<A>.BitsAnySet(x => x.X, 3);
            var expected = "{ \"x\" : { \"$bitsAnySet\" : NumberLong(3) } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestBooleanExpressionTrueForMethods()
        {
            var query = Query<A>.Where(a => a.S.StartsWith("k"));
            var expected = "{ \"s\" : /^k/s }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestBooleanExpressionFalseForMethods()
        {
            var query = Query<A>.Where(a => !a.S.StartsWith("k"));
            var expected = "{ \"s\" : { \"$not\" : /^k/s } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestBooleanExpressionTrueForProperties()
        {
            var query = Query<A>.Where(a => a.B);
            var expected = "{ \"b\" : true }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestBooleanExpressionFalseForProperties()
        {
            var query = Query<A>.Where(a => !a.B);
            var expected = "{ \"b\" : { \"$ne\" : true } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestBooleanEqualityExpressionForMethodsWithExplicitComparisonToTrue()
        {
            var query = Query<A>.Where(a => a.S.StartsWith("k") == true);
            var expected = "{ \"s\" : /^k/s }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestBooleanEqualityExpressionForMethodsWithExplicitComparisonToFalse()
        {
            var query = Query<A>.Where(a => a.S.StartsWith("k") == false);
            var expected = "{ \"s\" : { \"$not\" : /^k/s } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestBooleanInequalityExpressionForMethodsWithExplicitComparisonToTrue()
        {
            var query = Query<A>.Where(a => a.S.StartsWith("k") != true);
            var expected = "{ \"s\" : { \"$not\" : /^k/s } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestBooleanInequalityExpressionForMethodsWithExplicitComparisonToFalse()
        {
            var query = Query<A>.Where(a => a.S.StartsWith("k") != false);
            var expected = "{ \"s\" : /^k/s }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestBooleanEqualityExpressionForPropertiesWithExplicitComparisonToTrue()
        {
            var query = Query<A>.Where(a => a.B == true);
            var expected = "{ \"b\" : true }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestBooleanEqualityExpressionForPropertiesWithExplicitComparisonToFalse()
        {
            var query = Query<A>.Where(a => a.B == false);
            var expected = "{ \"b\" : false }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestBooleanInequalityExpressionForPropertiesWithExplicitComparisonToTrue()
        {
            var query = Query<A>.Where(a => a.B != true);
            var expected = "{ \"b\" : { \"$ne\" : true } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestBooleanInequalityExpressionForPropertiesWithExplicitComparisonToFalse()
        {
            var query = Query<A>.Where(a => a.B != false);
            var expected = "{ \"b\" : { \"$ne\" : false } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestElemMatch()
        {
            var query = Query<A>.ElemMatch(
                a => a.A_B,
                qb => qb.And(
                    qb.EQ(ab => ab.A, 1),
                    qb.GT(ab => ab.B, 1)));

            var expected = "{ \"ab\" : { \"$elemMatch\" : { \"a\" : 1, \"b\" : { \"$gt\" : 1 } } } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestElemMatch_Not()
        {
            var query = Query.Not(Query<A>.ElemMatch(
                a => a.A_B,
                qb => qb.And(
                    qb.EQ(ab => ab.A, 1),
                    qb.GT(ab => ab.B, 1))));

            var expected = "{ \"ab\" : { \"$not\" : { \"$elemMatch\" : { \"a\" : 1, \"b\" : { \"$gt\" : 1 } } } } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestElemMatchExpression()
        {
            var query = Query<A>.Where(a => a.A_B.Any(ab => ab.A == 1 && ab.B > 1));

            var expected = "{ \"ab\" : { \"$elemMatch\" : { \"a\" : 1, \"b\" : { \"$gt\" : 1 } } } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestEqual()
        {
            var query = Query<A>.EQ(a => a.X, 3);
            var expected = "{ \"x\" : 3 }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestEqualExpression()
        {
            var query = Query<A>.Where(a => a.X == 3);
            var expected = "{ \"x\" : 3 }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestEqual_NotExpression()
        {
            var query = Query<A>.Where(a => a.X != 3);
            var expected = "{ \"x\" : { \"$ne\" : 3 } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestEqual_Array()
        {
            var query = Query<A>.EQ(a => a.J, 3);
            var expected = "{ \"j\" : 3 }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestExists()
        {
            var query = Query<A>.Exists(a => a.X);
            var expected = "{ \"x\" : { \"$exists\" : true } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestGeoIntersects()
        {
            var poly = GeoJson.Polygon(
                GeoJson.Geographic(40, 18),
                GeoJson.Geographic(40, 19),
                GeoJson.Geographic(41, 19),
                GeoJson.Geographic(40, 18));

            var query = Query<A>.GeoIntersects(a => a.X, poly);
            var expected = "{ 'x' : { '$geoIntersects' : { '$geometry' : { 'type' : 'Polygon', 'coordinates' : [[[40.0, 18.0], [40.0, 19.0], [41.0, 19.0], [40.0, 18.0]]] } } } }".Replace("'", "\"");
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestGreaterThan()
        {
            var query = Query<A>.GT(a => a.X, 10);
            var expected = "{ \"x\" : { \"$gt\" : 10 } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestGreaterThanExpression()
        {
            var query = Query<A>.Where(a => a.X > 10);
            var expected = "{ \"x\" : { \"$gt\" : 10 } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestGreaterThan_Not()
        {
            var query = Query.Not(Query<A>.GT(a => a.X, 10));
            var expected = "{ \"x\" : { \"$not\" : { \"$gt\" : 10 } } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestGreaterThan_NotExpression()
        {
            var query = Query<A>.Where(a => !(a.X > 10));
            var expected = "{ \"x\" : { \"$not\" : { \"$gt\" : 10 } } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestGreaterThan_Array()
        {
            var query = Query<A>.GT(a => a.J, 10);
            var expected = "{ \"j\" : { \"$gt\" : 10 } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestGreaterThanOrEqual()
        {
            var query = Query<A>.GTE(a => a.X, 10);
            var expected = "{ \"x\" : { \"$gte\" : 10 } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestGreaterThanOrEqualExpression()
        {
            var query = Query<A>.Where(a => a.X >= 10);
            var expected = "{ \"x\" : { \"$gte\" : 10 } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestGreaterThanOrEqual_Not()
        {
            var query = Query.Not(Query<A>.GTE(a => a.X, 10));
            var expected = "{ \"x\" : { \"$not\" : { \"$gte\" : 10 } } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestGreaterThanOrEqual_NotExpression()
        {
            var query = Query<A>.Where(a => !(a.X >= 10));
            var expected = "{ \"x\" : { \"$not\" : { \"$gte\" : 10 } } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestGreaterThanOrEqual_Array()
        {
            var query = Query<A>.GTE(a => a.J, 10);
            var expected = "{ \"j\" : { \"$gte\" : 10 } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestIn()
        {
            var query = Query<A>.In(a => a.X, new[] { 2, 4, 6 });
            var expected = "{ \"x\" : { \"$in\" : [2, 4, 6] } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestInExpression()
        {
            var list = new[] { 2, 4, 6 };
            var query = Query<A>.Where(a => list.Contains(a.X));
            var expected = "{ \"x\" : { \"$in\" : [2, 4, 6] } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestIn_Not()
        {
            var query = Query.Not(Query<A>.In(a => a.X, new[] { 2, 4, 6 }));
            var expected = "{ \"x\" : { \"$nin\" : [2, 4, 6] } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestIn_NotExpression()
        {
            var list = new[] { 2, 4, 6 };
            var query = Query<A>.Where(a => !list.Contains(a.X));
            var expected = "{ \"x\" : { \"$nin\" : [2, 4, 6] } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestIn_Array()
        {
            var query = Query<A>.In(a => a.J, new[] { 2, 4, 6 });
            var expected = "{ \"j\" : { \"$in\" : [2, 4, 6] } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestLessThan()
        {
            var query = Query<A>.LT(a => a.X, 10);
            var expected = "{ \"x\" : { \"$lt\" : 10 } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestLessThanExpression()
        {
            var query = Query<A>.Where(a => a.X < 10);
            var expected = "{ \"x\" : { \"$lt\" : 10 } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestLessThan_Not()
        {
            var query = Query.Not(Query<A>.LT(a => a.X, 10));
            var expected = "{ \"x\" : { \"$not\" : { \"$lt\" : 10 } } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestLessThan_NotExpression()
        {
            var query = Query<A>.Where(a => !(a.X < 10));
            var expected = "{ \"x\" : { \"$not\" : { \"$lt\" : 10 } } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestLessThan_Array()
        {
            var query = Query<A>.LT(a => a.J, 10);
            var expected = "{ \"j\" : { \"$lt\" : 10 } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestLessThanOrEqual()
        {
            var query = Query<A>.LTE(a => a.X, 10);
            var expected = "{ \"x\" : { \"$lte\" : 10 } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestLessThanOrEqualExpression()
        {
            var query = Query<A>.Where(a => a.X <= 10);
            var expected = "{ \"x\" : { \"$lte\" : 10 } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestLessThanOrEqual_Not()
        {
            var query = Query.Not(Query<A>.LTE(a => a.X, 10));
            var expected = "{ \"x\" : { \"$not\" : { \"$lte\" : 10 } } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestLessThanOrEqual_NotExpression()
        {
            var query = Query<A>.Where(a => !(a.X <= 10));
            var expected = "{ \"x\" : { \"$not\" : { \"$lte\" : 10 } } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestLessThanOrEqual_Array()
        {
            var query = Query<A>.LTE(a => a.J, 10);
            var expected = "{ \"j\" : { \"$lte\" : 10 } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestMatchesPattern()
        {
            var query = Query<A>.Matches(a => a.S, "abc");
            var expected = "{ \"s\" : /abc/ }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestMatchesRegex()
        {
            var query = Query<A>.Matches(a => a.S, new Regex("abc", RegexOptions.IgnoreCase));
            var expected = "{ \"s\" : /abc/i }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestMatchesExpressionStringContains()
        {
            var query = Query<A>.Where(a => a.S.Contains("abc"));
            var expected = "{ \"s\" : /abc/s }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestMatchesExpressionRegex()
        {
            var query = Query<A>.Where(a => Regex.IsMatch(a.S, "abc"));
            var expected = "{ \"s\" : /abc/ }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestMatchesExpressionRegexVariable()
        {
            var regex = new Regex("abc", RegexOptions.Singleline);
            var query = Query<A>.Where(a => regex.IsMatch(a.S));
            var expected = "{ \"s\" : /abc/s }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestMatchesExpressionStringEndsWith()
        {
            var query = Query<A>.Where(a => a.S.EndsWith("abc"));
            var expected = "{ \"s\" : /abc$/s }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestMatchesExpressionStringStartsWith()
        {
            var query = Query<A>.Where(a => a.S.StartsWith("abc"));
            var expected = "{ \"s\" : /^abc/s }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestMatches_Not()
        {
            var query = Query.Not(Query<A>.Matches(a => a.S, "abc"));
            var expected = "{ \"s\" : { \"$not\" : /abc/ } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestMatches_NotExpressionStringContains()
        {
            var query = Query<A>.Where(a => !a.S.Contains("abc"));
            var expected = "{ \"s\" : { \"$not\" : /abc/s } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestMatchesPattern_Array()
        {
            var query = Query<A>.Matches(a => a.L, "abc");
            var expected = "{ \"l\" : /abc/ }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestMod()
        {
            var query = Query<A>.Mod(a => a.X, 10, 1);
            var expected = "{ \"x\" : { \"$mod\" : [10, 1] } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestModExpression()
        {
            var query = Query<A>.Where(a => a.X % 10 == 1);
            var expected = "{ \"x\" : { \"$mod\" : [10, 1] } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestMod_Not()
        {
            var query = Query.Not(Query<A>.Mod(a => a.X, 10, 1));
            var expected = "{ \"x\" : { \"$not\" : { \"$mod\" : [10, 1] } } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestMod_NotExpression()
        {
            var query = Query<A>.Where(a => a.X % 10 != 1);
            var expected = "{ \"x\" : { \"$not\" : { \"$mod\" : [10, 1] } } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestMod_Array()
        {
            var query = Query<A>.Mod(a => a.J, 10, 1);
            var expected = "{ \"j\" : { \"$mod\" : [10, 1] } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestNear()
        {
            var query = Query<A>.Near(a => a.S, 1.5, 2.5);
            var expected = "{ \"s\" : { \"$near\" : [1.5, 2.5] } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestNear_Not()
        {
            var query = Query.Not(Query<A>.Near(a => a.S, 1.5, 2.5));
            var expected = "{ \"s\" : { \"$not\" : { \"$near\" : [1.5, 2.5] } } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestNearWithMaxDistance()
        {
            var query = Query<A>.Near(a => a.S, 1.5, 2.5, 3.5);
            var expected = "{ \"s\" : { \"$near\" : [1.5, 2.5], \"$maxDistance\" : 3.5 } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestNearWithMaxDistance_Not()
        {
            var query = Query.Not(Query<A>.Near(a => a.S, 1.5, 2.5, 3.5));
            var expected = "{ \"$nor\" : [{ \"s\" : { \"$near\" : [1.5, 2.5], \"$maxDistance\" : 3.5 } }] }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestNearWithSphericalTrue()
        {
            var query = Query.Not(Query<A>.Near(a => a.S, 1.5, 2.5, 3.5, true));
            var expected = "{ \"$nor\" : [{ \"s\" : { \"$nearSphere\" : [1.5, 2.5], \"$maxDistance\" : 3.5 } }] }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestNearWithSphericalTrue_Not()
        {
            var query = Query.Not(Query<A>.Near(a => a.S, 1.5, 2.5, 3.5, true));
            var expected = "{ \"$nor\" : [{ \"s\" : { \"$nearSphere\" : [1.5, 2.5], \"$maxDistance\" : 3.5 } }] }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestNearWithGeoJson()
        {
            var point = GeoJson.Point(GeoJson.Geographic(40, 18));
            var query = Query<A>.Near(a => a.X, point);
            var expected = "{ 'x' : { '$near' : { '$geometry' : { 'type' : 'Point', 'coordinates' : [40.0, 18.0] } } } }".Replace("'", "\"");
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestNearWithGeoJsonWithMaxDistance()
        {
            var point = GeoJson.Point(GeoJson.Geographic(40, 18));
            var query = Query<A>.Near(a => a.X, point, 42);
            var expected = "{ 'x' : { '$near' : { '$geometry' : { 'type' : 'Point', 'coordinates' : [40.0, 18.0] }, '$maxDistance' : 42.0 } } }".Replace("'", "\"");
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestNearWithGeoJsonWithSpherical()
        {
            var point = GeoJson.Point(GeoJson.Geographic(40, 18));
            var query = Query<A>.Near(a => a.X, point, 42, true);
            var expected = "{ 'x' : { '$nearSphere' : { '$geometry' : { 'type' : 'Point', 'coordinates' : [40.0, 18.0] }, '$maxDistance' : 42.0 } } }".Replace("'", "\"");
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestNotEqual()
        {
            var query = Query<A>.NE(a => a.X, 3);
            var expected = "{ \"x\" : { \"$ne\" : 3 } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestNotEqual_Array()
        {
            var query = Query<A>.NE(a => a.J, 3);
            var expected = "{ \"j\" : { \"$ne\" : 3 } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestNotExists()
        {
            var query = Query<A>.NotExists(a => a.X);
            var expected = "{ \"x\" : { \"$exists\" : false } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestOr()
        {
            var query = Query.Or(
                Query<A>.GTE(a => a.X, 3),
                Query<A>.LTE(a => a.X, 10));

            var expected = "{ \"$or\" : [{ \"x\" : { \"$gte\" : 3 } }, { \"x\" : { \"$lte\" : 10 } }] }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestOr_Not()
        {
            var query = Query.Not(
                Query.Or(
                    Query<A>.GTE(a => a.X, 3),
                    Query<A>.LTE(a => a.X, 10)));

            var expected = "{ \"$nor\" : [{ \"x\" : { \"$gte\" : 3 } }, { \"x\" : { \"$lte\" : 10 } }] }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestOrExpression()
        {
            var query = Query<A>.Where(a => a.X >= 3 || a.X <= 10);

            var expected = "{ \"$or\" : [{ \"x\" : { \"$gte\" : 3 } }, { \"x\" : { \"$lte\" : 10 } }] }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestOrExpression_Not()
        {
            var query = Query<A>.Where(a => !(a.X >= 3 || a.X <= 10));

            var expected = "{ \"$nor\" : [{ \"x\" : { \"$gte\" : 3 } }, { \"x\" : { \"$lte\" : 10 } }] }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestSize()
        {
            var query = Query<A>.Size(a => a.J, 20);
            var expected = "{ \"j\" : { \"$size\" : 20 } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestSize_Not()
        {
            var query = Query.Not(Query<A>.Size(a => a.J, 20));
            var expected = "{ \"j\" : { \"$not\" : { \"$size\" : 20 } } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestSizeExpressionArray()
        {
            var query = Query<A>.Where(a => a.J.Length == 20);
            var expected = "{ \"j\" : { \"$size\" : 20 } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestSizeExpressionArray_GreaterThan()
        {
            var query = Query<A>.Where(a => a.J.Length > 20);
            var expected = "{ \"j.20\" : { \"$exists\" : true } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestSizeExpressionArray_GreaterThanOrEqual()
        {
            var query = Query<A>.Where(a => a.J.Length >= 20);
            var expected = "{ \"j.19\" : { \"$exists\" : true } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestSizeExpressionArray_LessThan()
        {
            var query = Query<A>.Where(a => a.J.Length < 20);
            var expected = "{ \"j.19\" : { \"$exists\" : false } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestSizeExpressionArray_LessThanOrEqual()
        {
            var query = Query<A>.Where(a => a.J.Length <= 20);
            var expected = "{ \"j.20\" : { \"$exists\" : false } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestSizeExpressionArray_Not()
        {
            var query = Query<A>.Where(a => a.J.Length != 20);
            var expected = "{ \"j\" : { \"$not\" : { \"$size\" : 20 } } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestSizeExpressionIEnumerable()
        {
            var query = Query<A>.Where(a => a.A_B.Count() == 20);
            var expected = "{ \"ab\" : { \"$size\" : 20 } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestSizeExpressionIEnumerable_GreaterThan()
        {
            var query = Query<A>.Where(a => a.A_B.Count() > 20);
            var expected = "{ \"ab.20\" : { \"$exists\" : true } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestSizeExpressionIEnumerable_GreaterThanOrEqual()
        {
            var query = Query<A>.Where(a => a.A_B.Count() >= 20);
            var expected = "{ \"ab.19\" : { \"$exists\" : true } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestSizeExpressionIEnumerable_LessThan()
        {
            var query = Query<A>.Where(a => a.A_B.Count() < 20);
            var expected = "{ \"ab.19\" : { \"$exists\" : false } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestSizeExpressionIEnumerable_LessThanOrEqual()
        {
            var query = Query<A>.Where(a => a.A_B.Count() <= 20);
            var expected = "{ \"ab.20\" : { \"$exists\" : false } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestSizeExpressionIEnumerable_Not()
        {
            var query = Query<A>.Where(a => a.A_B.Count() != 20);
            var expected = "{ \"ab\" : { \"$not\" : { \"$size\" : 20 } } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestSizeExpressionList()
        {
            var query = Query<A>.Where(a => a.L.Count == 20);
            var expected = "{ \"l\" : { \"$size\" : 20 } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestSizeExpressionList_GreaterThan()
        {
            var query = Query<A>.Where(a => a.L.Count > 20);
            var expected = "{ \"l.20\" : { \"$exists\" : true } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestSizeExpressionList_GreaterThanOrEqual()
        {
            var query = Query<A>.Where(a => a.L.Count >= 20);
            var expected = "{ \"l.19\" : { \"$exists\" : true } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestSizeExpressionList_LessThan()
        {
            var query = Query<A>.Where(a => a.L.Count < 20);
            var expected = "{ \"l.19\" : { \"$exists\" : false } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestSizeExpressionList_LessThanOrEqual()
        {
            var query = Query<A>.Where(a => a.L.Count <= 20);
            var expected = "{ \"l.20\" : { \"$exists\" : false } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestSizeExpressionList_Not()
        {
            var query = Query<A>.Where(a => a.L.Count != 20);
            var expected = "{ \"l\" : { \"$not\" : { \"$size\" : 20 } } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestType()
        {
            var query = Query<A>.Type(a => a.S, BsonType.String);
            var expected = "{ \"s\" : { \"$type\" : 2 } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestType_number()
        {
            var query = Query<A>.Type(a => a.S, "number");
            var expected = "{ \"s\" : { \"$type\" : \"number\" } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestType_Not()
        {
            var query = Query.Not(Query<A>.Type(a => a.S, BsonType.String));
            var expected = "{ \"s\" : { \"$not\" : { \"$type\" : 2 } } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestType_Not_number()
        {
            var query = Query.Not(Query<A>.Type(a => a.S, "number"));
            var expected = "{ \"s\" : { \"$not\" : { \"$type\" : \"number\" } } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestType_Array()
        {
            var query = Query<A>.Type(a => a.J, BsonType.String);
            var expected = "{ \"j\" : { \"$type\" : 2 } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestType_Array_number()
        {
            var query = Query<A>.Type(a => a.J, "number");
            var expected = "{ \"j\" : { \"$type\" : \"number\" } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestWhere()
        {
            var query = Query.Where("this.a > 3");
            var expected = "{ \"$where\" : { \"$code\" : \"this.a > 3\" } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestWithin()
        {
            var poly = GeoJson.Polygon(
                GeoJson.Geographic(40, 18),
                GeoJson.Geographic(40, 19),
                GeoJson.Geographic(41, 19),
                GeoJson.Geographic(40, 18));

            var query = Query<A>.Within(a => a.X, poly);
            var expected = "{ 'x' : { '$within' : { '$geometry' : { 'type' : 'Polygon', 'coordinates' : [[[40.0, 18.0], [40.0, 19.0], [41.0, 19.0], [40.0, 18.0]]] } } } }".Replace("'", "\"");
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestWithinCircle()
        {
            var query = Query<A>.WithinCircle(a => a.X, 1.5, 2.5, 3.5);
            var expected = "{ \"x\" : { \"$within\" : { \"$center\" : [[1.5, 2.5], 3.5] } } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestWithinCircle_Not()
        {
            var query = Query.Not(Query<A>.WithinCircle(a => a.X, 1.5, 2.5, 3.5));
            var expected = "{ \"x\" : { \"$not\" : { \"$within\" : { \"$center\" : [[1.5, 2.5], 3.5] } } } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestWithinCircleSpherical()
        {
            var query = Query<A>.WithinCircle(a => a.X, 1.5, 2.5, 3.5, true);
            var expected = "{ \"x\" : { \"$within\" : { \"$centerSphere\" : [[1.5, 2.5], 3.5] } } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestWithinCircleSpherical_Not()
        {
            var query = Query.Not(Query<A>.WithinCircle(a => a.X, 1.5, 2.5, 3.5, true));
            var expected = "{ \"x\" : { \"$not\" : { \"$within\" : { \"$centerSphere\" : [[1.5, 2.5], 3.5] } } } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestWithinPolygon()
        {
            var points = new double[,] { { 1.5, 2.5 }, { 3.5, 4.5 } };
            var query = Query<A>.WithinPolygon(a => a.X, points);
            var expected = "{ \"x\" : { \"$within\" : { \"$polygon\" : [[1.5, 2.5], [3.5, 4.5]] } } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestWithinPolygon_Not()
        {
            var points = new double[,] { { 1.5, 2.5 }, { 3.5, 4.5 } };
            var query = Query.Not(Query<A>.WithinPolygon(a => a.X, points));
            var expected = "{ \"x\" : { \"$not\" : { \"$within\" : { \"$polygon\" : [[1.5, 2.5], [3.5, 4.5]] } } } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestWithinRectangle()
        {
            var query = Query<A>.WithinRectangle(a => a.X, 1.5, 2.5, 3.5, 4.5);
            var expected = "{ \"x\" : { \"$within\" : { \"$box\" : [[1.5, 2.5], [3.5, 4.5]] } } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestWithinRectangle_Not()
        {
            var query = Query.Not(Query<A>.WithinRectangle(a => a.X, 1.5, 2.5, 3.5, 4.5));
            var expected = "{ \"x\" : { \"$not\" : { \"$within\" : { \"$box\" : [[1.5, 2.5], [3.5, 4.5]] } } } }";
            Assert.Equal(expected, query.ToJson());
        }
    }
}