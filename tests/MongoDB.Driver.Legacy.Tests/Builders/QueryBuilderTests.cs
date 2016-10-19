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
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.GeoJsonObjectModel;
using Xunit;

namespace MongoDB.Driver.Tests.Builders
{
    public class QueryBuilderTests
    {
        private MongoServer _server;
        private MongoDatabase _database;
        private MongoServerInstance _primary;

        public QueryBuilderTests()
        {
            _server = LegacyTestConfiguration.Server;
            _database = LegacyTestConfiguration.Database;
            _primary = _server.Primary;
        }

        [Fact]
        public void TestNewSyntax()
        {
            var query = Query.And(Query.GTE("x", 3), Query.LTE("x", 10));
            var expected = "{ \"x\" : { \"$gte\" : 3, \"$lte\" : 10 } }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestAllBsonArray()
        {
            var array = new BsonArray { 2, 4, 6 };
            var query = Query.All("j", array);
            var selector = "{ \"$all\" : [2, 4, 6] }";
            Assert.Equal(PositiveTest("j", selector), query.ToJson());
            Assert.Equal(NegativeTest("j", selector), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestAllBsonArrayCastToIEnumerableBsonValue()
        {
            var array = new BsonArray { 2, 4, 6 };
            var enumerable = (IEnumerable<BsonValue>)array;
            var query = Query.All("j", enumerable);
            var selector = "{ \"$all\" : [2, 4, 6] }";
            Assert.Equal(PositiveTest("j", selector), query.ToJson());
            Assert.Equal(NegativeTest("j", selector), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestAllIEnumerableBsonValue()
        {
            var enumerable = new List<BsonValue> { 2, 4, 6 };
            var query = Query.All("j", enumerable);
            var selector = "{ \"$all\" : [2, 4, 6] }";
            Assert.Equal(PositiveTest("j", selector), query.ToJson());
            Assert.Equal(NegativeTest("j", selector), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestAnd()
        {
            var query = Query.And(
                Query.EQ("a", 1),
                Query.EQ("b", 2)
            );
            var expected = "{ \"a\" : 1, \"b\" : 2 }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestAndNoArgs()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => { Query.And(); });
            Assert.True(ex.Message.StartsWith("And cannot be called with zero queries."));
        }

        [Fact]
        public void TestAndNull()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => { Query.And(Query.Null); });
            Assert.True(ex.Message.StartsWith("One of the queries is null."));
        }

        [Fact]
        public void TestAndNullFirst()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Query.And(
                    Query.Null,
                    Query.EQ("x", 1)
                    );
            });
            Assert.True(ex.Message.StartsWith("One of the queries is null."));
        }

        [Fact]
        public void TestAndNullSecond()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Query.And(
                    Query.EQ("x", 1),
                    Query.Null
                    );
            });
            Assert.True(ex.Message.StartsWith("One of the queries is null."));
        }

        [Fact]
        public void TestAndWithEmptyQuery()
        {
            var emptyQuery = Query.Empty;
            var expected = "{ }";
            var negated = "{ \"$nor\" : [{ }] }";

            var query = Query.And(emptyQuery);
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());

            query = Query.And(emptyQuery, emptyQuery);
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());

            expected = "{ \"x\" : 1 }";
            negated = "{ \"x\" : { \"$ne\" : 1 } }";

            query = Query.And(emptyQuery, Query.EQ("x", 1));
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());

            query = Query.And(Query.EQ("x", 1), emptyQuery);
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());

            query = Query.And(emptyQuery, Query.EQ("x", 1), emptyQuery);
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());

            query = Query.And(Query.EQ("x", 1), emptyQuery, Query.EQ("y", 2));
            expected = "{ \"x\" : 1, \"y\" : 2 }";
            negated = "{ \"$nor\" : [{ \"x\" : 1, \"y\" : 2 }] }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());
        }

        [Fact]
        public void TestAndXNE1()
        {
            var query = Query.And(Query.NE("x", 1));
            var expected = "{ \"x\" : { \"$ne\" : 1 } }";
            var negated = "{ \"x\" : 1 }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());
        }

        [Fact]
        public void TestAndXNE1XNE2()
        {
            var query = Query.And(Query.NE("x", 1), Query.NE("x", 2));
            var expected = "{ \"$and\" : [{ \"x\" : { \"$ne\" : 1 } }, { \"x\" : { \"$ne\" : 2 } }] }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestAndXNE1YNE2()
        {
            var query = Query.And(Query.NE("x", 1), Query.NE("y", 2));
            var expected = "{ \"x\" : { \"$ne\" : 1 }, \"y\" : { \"$ne\" : 2 } }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestBitsAllClear()
        {
            var query = Query.BitsAllClear("x", 3);
            var expected = "{ \"x\" : { \"$bitsAllClear\" : NumberLong(3) } }";
            var negated = "{ \"x\" : { \"$not\" : { \"$bitsAllClear\" : NumberLong(3) } } }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());
        }

        [Fact]
        public void TestBitsAllSet()
        {
            var query = Query.BitsAllSet("x", 3);
            var expected = "{ \"x\" : { \"$bitsAllSet\" : NumberLong(3) } }";
            var negated = "{ \"x\" : { \"$not\" : { \"$bitsAllSet\" : NumberLong(3) } } }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());
        }

        [Fact]
        public void TestBitsAnyClear()
        {
            var query = Query.BitsAnyClear("x", 3);
            var expected = "{ \"x\" : { \"$bitsAnyClear\" : NumberLong(3) } }";
            var negated = "{ \"x\" : { \"$not\" : { \"$bitsAnyClear\" : NumberLong(3) } } }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());
        }

        [Fact]
        public void TestBitsAnySet()
        {
            var query = Query.BitsAnySet("x", 3);
            var expected = "{ \"x\" : { \"$bitsAnySet\" : NumberLong(3) } }";
            var negated = "{ \"x\" : { \"$not\" : { \"$bitsAnySet\" : NumberLong(3) } } }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());
        }

        [Fact]
        public void TestElementMatch()
        {
            var query = Query.ElemMatch("x",
                Query.And(
                    Query.EQ("a", 1),
                    Query.GT("b", 1)
                )
            );
            var selector = "{ \"$elemMatch\" : { \"a\" : 1, \"b\" : { \"$gt\" : 1 } } }";
            Assert.Equal(PositiveTest("x", selector), query.ToJson());
            Assert.Equal(NegativeTest("x", selector), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestEQ()
        {
            var query = Query.EQ("x", 3);
            var expected = "{ \"x\" : 3 }";
            var negated = "{ \"x\" : { \"$ne\" : 3 } }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());
        }

        [Fact]
        public void TestEQDBRef()
        {
            var query = Query.EQ("x", new BsonDocument { { "$ref", "c" }, { "$id", 1 } });
            var expected = "{ \"x\" : { \"$ref\" : \"c\", \"$id\" : 1 } }";
            var negated = "{ \"x\" : { \"$ne\" : { \"$ref\" : \"c\", \"$id\" : 1 } } }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());
        }

        [Fact]
        public void TestEQTwoElements()
        {
            var query = Query.And(
                Query.EQ("x", 3),
                Query.EQ("y", "foo")
            );
            var expected = "{ \"x\" : 3, \"y\" : \"foo\" }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestExists()
        {
            var query = Query.Exists("x");
            var expected = "{ \"x\" : { \"$exists\" : true } }";
            var negated = "{ \"x\" : { \"$exists\" : false } }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());
        }

        [Fact]
        public void TestExistsFalse()
        {
            var query = Query.NotExists("x");
            var expected = "{ \"x\" : { \"$exists\" : false } }";
            var negated = "{ \"x\" : { \"$exists\" : true } }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());
        }

        [Fact]
        public void TestGeoIntersects()
        {
            var poly = GeoJson.Polygon(
                GeoJson.Geographic(40, 18),
                GeoJson.Geographic(40, 19),
                GeoJson.Geographic(41, 19),
                GeoJson.Geographic(40, 18));

            var query = Query.GeoIntersects("loc", poly);
            var selector = "{ '$geoIntersects' : { '$geometry' : { 'type' : 'Polygon', 'coordinates' : [[[40.0, 18.0], [40.0, 19.0], [41.0, 19.0], [40.0, 18.0]]] } } }";
            Assert.Equal(PositiveTest("loc", selector), query.ToJson());
            Assert.Equal(NegativeTest("loc", selector), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestGreaterThan()
        {
            var query = Query.GT("k", 10);
            var selector = "{ \"$gt\" : 10 }";
            Assert.Equal(PositiveTest("k", selector), query.ToJson());
            Assert.Equal(NegativeTest("k", selector), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestGreaterThanAndLessThan()
        {
            var query = Query.And(Query.GT("k", 10), Query.LT("k", 20));
            var expected = "{ \"k\" : { \"$gt\" : 10, \"$lt\" : 20 } }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestGreaterThanAndLessThanOrEqual()
        {
            var query = Query.And(Query.GT("k", 10), Query.LTE("k", 20));
            var expected = "{ \"k\" : { \"$gt\" : 10, \"$lte\" : 20 } }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestGreaterThanAndMod()
        {
            var query = Query.And(Query.GT("k", 10), Query.Mod("k", 10, 1));
            var expected = "{ \"k\" : { \"$gt\" : 10, \"$mod\" : [10, 1] } }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(NegateArbitraryQuery(query.ToJson()), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestGreaterThanOrEqual()
        {
            var query = Query.GTE("k", 10);
            var selector = "{ \"$gte\" : 10 }";
            Assert.Equal(PositiveTest("k", selector), query.ToJson());
            Assert.Equal(NegativeTest("k", selector), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestGreaterThanOrEqualAndLessThan()
        {
            var query = Query.And(Query.GTE("k", 10), Query.LT("k", 20));
            var expected = "{ \"k\" : { \"$gte\" : 10, \"$lt\" : 20 } }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestGreaterThanOrEqualAndLessThanOrEqual()
        {
            var query = Query.And(Query.GTE("k", 10), Query.LTE("k", 20));
            var expected = "{ \"k\" : { \"$gte\" : 10, \"$lte\" : 20 } }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestInBsonArray()
        {
            var array = new BsonArray { 2, 4, 6 };
            var query = Query.In("j", array);
            var expected = "{ \"j\" : { \"$in\" : [2, 4, 6] } }";
            var negated = "{ \"j\" : { \"$nin\" : [2, 4, 6] } }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());
        }

        [Fact]
        public void TestInBsonArrayCastToIEnumerableBsonValue()
        {
            var array = new BsonArray { 2, 4, 6 };
            var enumerable = (IEnumerable<BsonValue>)array;
            var query = Query.In("j", enumerable);
            var expected = "{ \"j\" : { \"$in\" : [2, 4, 6] } }";
            var negated = "{ \"j\" : { \"$nin\" : [2, 4, 6] } }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());
        }

        [Fact]
        public void TestInIEnumerableBsonValue()
        {
            var enumerable = new List<BsonValue> { 2, 4, 6 };
            var query = Query.In("j", enumerable);
            var expected = "{ \"j\" : { \"$in\" : [2, 4, 6] } }";
            var negated = "{ \"j\" : { \"$nin\" : [2, 4, 6] } }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());
        }

        [Fact]
        public void TestLessThan()
        {
            var query = Query.LT("k", 10);
            var selector = "{ \"$lt\" : 10 }";
            Assert.Equal(PositiveTest("k", selector), query.ToJson());
            Assert.Equal(NegativeTest("k", selector), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestLessThanOrEqual()
        {
            var query = Query.LTE("k", 10);
            var selector = "{ \"$lte\" : 10 }";
            Assert.Equal(PositiveTest("k", selector), query.ToJson());
            Assert.Equal(NegativeTest("k", selector), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestLessThanOrEqualAndNotEquals()
        {
            var query = Query.And(Query.LTE("k", 10), Query.NE("k", 5));
            var expected = "{ \"k\" : { \"$lte\" : 10, \"$ne\" : 5 } }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestLessThanOrEqualAndNotIn()
        {
            var query = Query.And(Query.LTE("k", 20), Query.NotIn("k", new BsonValue[] { 7, 11 }));
            var expected = "{ \"k\" : { \"$lte\" : 20, \"$nin\" : [7, 11] } }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestMatches()
        {
            var query = Query.Matches("a", "/abc/");
            var selector = "/abc/".Replace("'", "\"");
            Assert.Equal(PositiveTest("a", selector), query.ToJson());
            Assert.Equal(NegativeTest("a", selector), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestMod()
        {
            var query = Query.Mod("a", 10, 1);
            var selector = "{ \"$mod\" : [10, 1] }";
            Assert.Equal(PositiveTest("a", selector), query.ToJson());
            Assert.Equal(NegativeTest("a", selector), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestNestedNor()
        {
            var query = Query.And(
                Query.EQ("name", "bob"),
                Query.Not(Query.Or(
                    Query.EQ("a", 1),
                    Query.EQ("b", 2)
                ))
            );
            var expected = "{ \"name\" : \"bob\", \"$nor\" : [{ \"a\" : 1 }, { \"b\" : 2 }] }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestNestedOr()
        {
            var query = Query.And(
                Query.EQ("name", "bob"),
                Query.Or(
                    Query.EQ("a", 1),
                    Query.EQ("b", 2)
                )
            );
            var expected = "{ \"name\" : \"bob\", \"$or\" : [{ \"a\" : 1 }, { \"b\" : 2 }] }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestNor()
        {
            var query = Query.Not(Query.Or(
                Query.EQ("a", 1),
                Query.EQ("b", 2)
            ));
            var expected = "{ \"$nor\" : [{ \"a\" : 1 }, { \"b\" : 2 }] }";
            var negated = "{ \"$or\" : [{ \"a\" : 1 }, { \"b\" : 2 }] }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());
        }

        [Fact]
        public void TestNE()
        {
            var query = Query.NE("j", 3);
            var expected = "{ \"j\" : { \"$ne\" : 3 } }";
            var negated = "{ \"j\" : 3 }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());
        }

        [Fact]
        public void TestNinBsonArray()
        {
            var array = new BsonArray { 2, 4, 6 };
            var query = Query.NotIn("j", array);
            var expected = "{ \"j\" : { \"$nin\" : [2, 4, 6] } }";
            var negated = "{ \"j\" : { \"$in\" : [2, 4, 6] } }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());
        }

        [Fact]
        public void TestNinBsonArrayCastToIEnumerableBsonValue()
        {
            var array = new BsonArray { 2, 4, 6 };
            var enumerable = (IEnumerable<BsonValue>)array;
            var query = Query.NotIn("j", enumerable);
            var expected = "{ \"j\" : { \"$nin\" : [2, 4, 6] } }";
            var negated = "{ \"j\" : { \"$in\" : [2, 4, 6] } }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());
        }

        [Fact]
        public void TestNinIEnumerableBsonValue()
        {
            var enumerable = new List<BsonValue> { 2, 4, 6 };
            var query = Query.NotIn("j", enumerable);
            var expected = "{ \"j\" : { \"$nin\" : [2, 4, 6] } }";
            var negated = "{ \"j\" : { \"$in\" : [2, 4, 6] } }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());
        }

        [Fact]
        public void TestOrXEQ1()
        {
            var query = Query.Or(Query.EQ("x", 1));
            var expected = "{ \"x\" : 1 }";
            var negated = "{ \"x\" : { \"$ne\" : 1 } }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());
        }

        [Fact]
        public void TestOrXEQ1XEQ2()
        {
            var query = Query.Or(Query.EQ("x", 1), Query.EQ("x", 2));
            var expected = "{ \"$or\" : [{ \"x\" : 1 }, { \"x\" : 2 }] }";
            var negated = "{ \"$nor\" : [{ \"x\" : 1 }, { \"x\" : 2 }] }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());
        }

        [Fact]
        public void TestOrNoArgs()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => { Query.Or(); });
            Assert.True(ex.Message.StartsWith("Or cannot be called with zero queries."));
        }

        [Fact]
        public void TestOrNull()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => { Query.Or(Query.Null); });
            Assert.True(ex.Message.StartsWith("One of the queries is null."));
        }

        [Fact]
        public void TestOrNullFirst()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Query.Or(
                    Query.Null,
                    Query.EQ("x", 1)
                    );
            });
            Assert.True(ex.Message.StartsWith("One of the queries is null."));
        }

        [Fact]
        public void TestOrNullSecond()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Query.Or(
                    Query.EQ("x", 1),
                    Query.Null
                    );
            });
            Assert.True(ex.Message.StartsWith("One of the queries is null."));
        }

        [Fact]
        public void TestOrWithEmptyQuery()
        {
            var emptyQuery = Query.Empty;
            var expected = "{ }";
            var negated = "{ \"$nor\" : [{ }] }";

            var query = Query.Or(emptyQuery);
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());

            query = Query.Or(emptyQuery, emptyQuery);
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());

            query = Query.Or(emptyQuery, Query.EQ("x", 1));
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());

            query = Query.Or(Query.EQ("x", 1), emptyQuery);
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());

            query = Query.Or(emptyQuery, Query.EQ("x", 1), emptyQuery);
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());

            query = Query.Or(Query.EQ("x", 1), emptyQuery, Query.EQ("y", 2));
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(negated, Query.Not(query).ToJson());
        }

        [Fact]
        public void TestRegex()
        {
            var query = Query.Matches("name", new BsonRegularExpression("acme.*corp", "i"));
            var selector = "/acme.*corp/i";
            Assert.Equal(PositiveTest("name", selector), query.ToJson());
            Assert.Equal(NegativeTest("name", selector), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestSize()
        {
            var query = Query.Size("k", 20);
            var selector = "{ \"$size\" : 20 }";
            Assert.Equal(PositiveTest("k", selector), query.ToJson());
            Assert.Equal(NegativeTest("k", selector), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestSizeAndAll()
        {
            var query = Query.And(Query.Size("k", 20), Query.All("k", new BsonArray { 7, 11 }));
            var expected = "{ \"k\" : { \"$size\" : 20, \"$all\" : [7, 11] } }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestSizeGreaterThan()
        {
            var query = Query.SizeGreaterThan("k", 20);
            var expected = "{ \"k.20\" : { \"$exists\" : true } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestSizeGreaterThanOrEqual()
        {
            var query = Query.SizeGreaterThanOrEqual("k", 20);
            var expected = "{ \"k.19\" : { \"$exists\" : true } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestSizeLessThan()
        {
            var query = Query.SizeLessThan("k", 20);
            var expected = "{ \"k.19\" : { \"$exists\" : false } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestSizeLessThanOrEqual()
        {
            var query = Query.SizeLessThanOrEqual("k", 20);
            var expected = "{ \"k.20\" : { \"$exists\" : false } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestSizeNotGreaterThan()
        {
            var query = Query.Not(Query.SizeGreaterThan("k", 20));
            var expected = "{ \"k.20\" : { \"$exists\" : false } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestSizeNotGreaterThanOrEqual()
        {
            var query = Query.Not(Query.SizeGreaterThanOrEqual("k", 20));
            var expected = "{ \"k.19\" : { \"$exists\" : false } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestSizeNotLessThan()
        {
            var query = Query.Not(Query.SizeLessThan("k", 20));
            var expected = "{ \"k.19\" : { \"$exists\" : true } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestSizeNotLessThanOrEqual()
        {
            var query = Query.Not(Query.SizeLessThanOrEqual("k", 20));
            var expected = "{ \"k.20\" : { \"$exists\" : true } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestTextQueryGeneration()
        {
            var query = Query.Text("foo");
            var expected = "{ \"$text\" : { \"$search\" : \"foo\" } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestTextWithLanguageQueryGeneration()
        {
            var query = Query.Text("foo", "norwegian");
            var expected = "{ \"$text\" : { \"$search\" : \"foo\", \"$language\" : \"norwegian\" } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestTextQueryGenerationWithNullLanguage()
        {
            var query = Query.Text("foo", (string)null);
            var expected = "{ \"$text\" : { \"$search\" : \"foo\" } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestTextWithOptionsQueryGeneration()
        {
            var options = new TextSearchOptions
            {
                Language = "norwegian",
                CaseSensitive = true,
                DiacriticSensitive = true
            };
            var query = Query.Text("foo", options);
            var expected = "{ \"$text\" : { \"$search\" : \"foo\", \"$language\" : \"norwegian\", \"$caseSensitive\" : true, \"$diacriticSensitive\" : true } }";
            Assert.Equal(expected, query.ToJson());
        }

        [Fact]
        public void TestType()
        {
            var query = Query.Type("a", BsonType.String);
            var selector = "{ \"$type\" : 2 }";
            Assert.Equal(PositiveTest("a", selector), query.ToJson());
            Assert.Equal(NegativeTest("a", selector), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestType_number()
        {
            var query = Query.Type("a", "number");
            var selector = "{ \"$type\" : \"number\" }";
            Assert.Equal(PositiveTest("a", selector), query.ToJson());
            Assert.Equal(NegativeTest("a", selector), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestWhere()
        {
            var query = Query.Where("this.a > 3");
            var expected = "{ \"$where\" : { \"$code\" : \"this.a > 3\" } }";
            Assert.Equal(expected, query.ToJson());
            Assert.Equal(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestWithin()
        {
            var poly = GeoJson.Polygon(
                GeoJson.Geographic(40, 18),
                GeoJson.Geographic(40, 19),
                GeoJson.Geographic(41, 19),
                GeoJson.Geographic(40, 18));

            var query = Query.Within("loc", poly);
            var selector = "{ '$within' : { '$geometry' : { 'type' : 'Polygon', 'coordinates' : [[[40.0, 18.0], [40.0, 19.0], [41.0, 19.0], [40.0, 18.0]]] } } }";
            Assert.Equal(PositiveTest("loc", selector), query.ToJson());
            Assert.Equal(NegativeTest("loc", selector), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestWithinCircle()
        {
            var query = Query.WithinCircle("loc", 1.5, 2.5, 3.5);
            var selector = "{ '$within' : { '$center' : [[1.5, 2.5], 3.5] } }";
            Assert.Equal(PositiveTest("loc", selector), query.ToJson());
            Assert.Equal(NegativeTest("loc", selector), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestWithinCircleSpherical()
        {
            var query = Query.WithinCircle("loc", 1.5, 2.5, 3.5, true);
            var selector = "{ '$within' : { '$centerSphere' : [[1.5, 2.5], 3.5] } }";
            Assert.Equal(PositiveTest("loc", selector), query.ToJson());
            Assert.Equal(NegativeTest("loc", selector), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestWithinPolygon()
        {
            var points = new double[,] { { 1.5, 2.5 }, { 3.5, 4.5 } };
            var query = Query.WithinPolygon("loc", points);
            var selector = "{ '$within' : { '$polygon' : [[1.5, 2.5], [3.5, 4.5]] } }";
            Assert.Equal(PositiveTest("loc", selector), query.ToJson());
            Assert.Equal(NegativeTest("loc", selector), Query.Not(query).ToJson());
        }

        [Fact]
        public void TestWithinPolygonInvalidSecondDimension()
        {
            var points = new double[,] { { 1, 2, 3 } };
            Assert.Throws<ArgumentOutOfRangeException>(() => Query.WithinPolygon("loc", points));
        }

        [Fact]
        public void TestWithinRectangle()
        {
            var query = Query.WithinRectangle("loc", 1.5, 2.5, 3.5, 4.5);
            var selector = "{ '$within' : { '$box' : [[1.5, 2.5], [3.5, 4.5]] } }";
            Assert.Equal(PositiveTest("loc", selector), query.ToJson());
            Assert.Equal(NegativeTest("loc", selector), Query.Not(query).ToJson());
        }

        private string NegateArbitraryQuery(string query)
        {
            return "{ \"$nor\" : [#Q] }".Replace("#Q", query);
        }

        private string NegativeTest(string fieldName, string selector)
        {
            return "{ '#fieldName' : { '$not' : #selector } }".Replace("#fieldName", fieldName).Replace("#selector", selector).Replace("'", "\"");
        }

        private string PositiveTest(string fieldName, string selector)
        {
            return "{ '#fieldName' : #selector }".Replace("#fieldName", fieldName).Replace("#selector", selector).Replace("'", "\"");
        }

        [Fact]
        public void TestNear()
        {
            var query = Query.Near("loc", 1.5, 2.5);
            var selector = "{ '$near' : [1.5, 2.5] }";
            Assert.Equal(PositiveTest("loc", selector), query.ToJson());

            var collection = LegacyTestConfiguration.Collection;
            collection.Drop();
            collection.CreateIndex(IndexKeys.GeoSpatial("loc"));
            collection.Insert(new BsonDocument { { "_id", 1 }, { "loc", new BsonArray { 1, 1 } } });
            collection.Insert(new BsonDocument { { "_id", 2 }, { "loc", new BsonArray { 2, 2 } } });

            query = Query.Near("loc", 0.0, 0.0);
            var results = collection.Find(query).ToList();
            Assert.Equal(2, results.Count);
            Assert.Equal(1, results[0]["_id"].ToInt32());
            Assert.Equal(2, results[1]["_id"].ToInt32());
        }

        [Fact]
        public void TestNearWithMaxDistance()
        {
            var query = Query.Near("loc", 1.5, 2.5, 3.5);
            var expected = "{ 'loc' : { '$near' : [1.5, 2.5], '$maxDistance' : 3.5 } }".Replace("'", "\"");
            Assert.Equal(expected, query.ToJson());

            var collection = LegacyTestConfiguration.Collection;
            collection.Drop();
            collection.CreateIndex(IndexKeys.GeoSpatial("loc"));
            collection.Insert(new BsonDocument { { "_id", 1 }, { "loc", new BsonArray { 1, 1 } } });
            collection.Insert(new BsonDocument { { "_id", 2 }, { "loc", new BsonArray { 2, 2 } } });

            query = Query.Near("loc", 0.0, 0.0, 2.0);
            var results = collection.Find(query).ToList();
            Assert.Equal(1, results.Count);
            Assert.Equal(1, results[0]["_id"].ToInt32());
        }

        [Fact]
        public void TestNearWithSphericalTrue()
        {
            var query = Query.Near("loc", 1.5, 2.5, 3.5, true);
            var expected = "{ 'loc' : { '$nearSphere' : [1.5, 2.5], '$maxDistance' : 3.5 } }".Replace("'", "\"");
            Assert.Equal(expected, query.ToJson());

            var collection = LegacyTestConfiguration.Collection;
            collection.Drop();
            collection.CreateIndex(IndexKeys.GeoSpatial("loc"));
            collection.Insert(new BsonDocument { { "_id", 1 }, { "loc", new BsonArray { 1, 1 } } });
            collection.Insert(new BsonDocument { { "_id", 2 }, { "loc", new BsonArray { 2, 2 } } });

            var radiansPerDegree = 2 * Math.PI / 360.0;
            query = Query.Near("loc", 0.0, 0.0, 2.0 * radiansPerDegree, true);
            var results = collection.Find(query).ToList();
            Assert.Equal(1, results.Count);
            Assert.Equal(1, results[0]["_id"].ToInt32());
        }

        [Fact]
        public void TestNearWithGeoJson()
        {
            var point = GeoJson.Point(GeoJson.Geographic(40, 18));
            var query = Query.Near("loc", point);
            var selector = "{ '$near' : { '$geometry' : { 'type' : 'Point', 'coordinates' : [40.0, 18.0] } } }";
            Assert.Equal(PositiveTest("loc", selector), query.ToJson());

            var collection = LegacyTestConfiguration.Collection;
            collection.Drop();
            collection.CreateIndex(IndexKeys.GeoSpatial("loc"));
            collection.Insert(new BsonDocument { { "_id", 1 }, { "loc", new BsonArray { 1, 1 } } });
            collection.Insert(new BsonDocument { { "_id", 2 }, { "loc", new BsonArray { 2, 2 } } });

            query = Query.Near("loc", 0.0, 0.0);
            var results = collection.Find(query).ToList();
            Assert.Equal(2, results.Count);
            Assert.Equal(1, results[0]["_id"].ToInt32());
            Assert.Equal(2, results[1]["_id"].ToInt32());
        }

        [Fact]
        public void TestNearWithGeoJsonWithMaxDistance()
        {
            if (_primary.Supports(FeatureId.GeoJson))
            {
                var point = GeoJson.Point(GeoJson.Geographic(40, 18));
                var query = Query.Near("loc", point, 42);
                var expected = "{ 'loc' : { '$near' : { '$geometry' : { 'type' : 'Point', 'coordinates' : [40.0, 18.0] }, '$maxDistance' : 42.0 } } }".Replace("'", "\"");
                Assert.Equal(expected, query.ToJson());

                var collection = LegacyTestConfiguration.Collection;
                collection.Drop();
                collection.CreateIndex(IndexKeys.GeoSpatialSpherical("loc"));
                collection.Insert(new BsonDocument { { "_id", 1 }, { "loc", GeoJson.Point(GeoJson.Geographic(1, 1)).ToBsonDocument() } });
                collection.Insert(new BsonDocument { { "_id", 2 }, { "loc", GeoJson.Point(GeoJson.Geographic(2, 2)).ToBsonDocument() } });

                var circumferenceOfTheEarth = 40075000; // meters at the equator, approx
                var metersPerDegree = circumferenceOfTheEarth / 360.0;
                query = Query.Near("loc", GeoJson.Point(GeoJson.Geographic(0, 0)), 2.0 * metersPerDegree);
                var results = collection.Find(query).ToList();
                Assert.Equal(1, results.Count);
                Assert.Equal(1, results[0]["_id"].ToInt32());
            }
        }

        [Fact]
        public void TestNearWithGeoJsonWithSpherical()
        {
            if (_primary.Supports(FeatureId.GeoJson))
            {
                var point = GeoJson.Point(GeoJson.Geographic(40, 18));
                var query = Query.Near("loc", point, 42, true);
                var expected = "{ 'loc' : { '$nearSphere' : { '$geometry' : { 'type' : 'Point', 'coordinates' : [40.0, 18.0] }, '$maxDistance' : 42.0 } } }".Replace("'", "\"");
                Assert.Equal(expected, query.ToJson());

                var collection = LegacyTestConfiguration.Collection;
                collection.Drop();
                collection.CreateIndex(IndexKeys.GeoSpatialSpherical("loc"));
                collection.Insert(new BsonDocument { { "_id", 1 }, { "loc", GeoJson.Point(GeoJson.Geographic(1, 1)).ToBsonDocument() } });
                collection.Insert(new BsonDocument { { "_id", 2 }, { "loc", GeoJson.Point(GeoJson.Geographic(2, 2)).ToBsonDocument() } });

                var circumferenceOfTheEarth = 40075000; // meters at the equator, approx
                var metersPerDegree = circumferenceOfTheEarth / 360.0;
                query = Query.Near("loc", GeoJson.Point(GeoJson.Geographic(0, 0)), 2.0 * metersPerDegree, true);
                var results = collection.Find(query).ToList();
                Assert.Equal(1, results.Count);
                Assert.Equal(1, results[0]["_id"].ToInt32());
            }
        }


        [Fact]
        public void TestText()
        {
            if (_primary.Supports(FeatureId.TextSearchQuery))
            {
                var collection = _database.GetCollection<BsonDocument>("test_text");
                collection.Drop();
                collection.CreateIndex(IndexKeys.Text("textfield"));
                collection.Insert(new BsonDocument
                {
                    { "_id", 1 },
                    { "textfield", "The quick brown fox" }
                });
                collection.Insert(new BsonDocument
                {
                    { "_id", 2 },
                    { "textfield", "over the lazy brown dog" }
                });
                var query = Query.Text("fox");
                var results = collection.Find(query).ToArray();
                Assert.Equal(1, results.Length);
                Assert.Equal(1, results[0]["_id"].AsInt32);
            }
        }

        [Fact]
        public void TestTextWithLanguage()
        {
            if (_primary.Supports(FeatureId.TextSearchQuery))
            {
                var collection = _database.GetCollection<BsonDocument>("test_text_spanish");
                collection.Drop();
                collection.CreateIndex(IndexKeys.Text("textfield"), IndexOptions.SetTextDefaultLanguage("spanish"));
                collection.Insert(new BsonDocument
                {
                    { "_id", 1 },
                    { "textfield", "este es mi tercer blog stemmed" }
                });
                collection.Insert(new BsonDocument
                {
                    { "_id", 2 },
                    { "textfield", "This stemmed blog is in english" },
                    { "language", "english" }
                });

                var query = Query.Text("stemmed");
                var results = collection.Find(query).ToArray();
                Assert.Equal(1, results.Length);
                Assert.Equal(1, results[0]["_id"].AsInt32);

                query = Query.Text("stemmed", "english");
                results = collection.Find(query).ToArray();
                Assert.Equal(1, results.Length);
                Assert.Equal(2, results[0]["_id"].AsInt32);
            }
        }
    }
}
