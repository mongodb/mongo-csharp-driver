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
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.GeoJsonObjectModel;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Builders
{
    [TestFixture]
    public class QueryBuilderTests
    {
        private MongoServer _server;
        private MongoDatabase _database;
        private MongoServerInstance _primary;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            _server = Configuration.TestServer;
            _database = Configuration.TestDatabase;
            _primary = _server.Primary;
        }

        [Test]
        public void TestNewSyntax()
        {
            var query = Query.And(Query.GTE("x", 3), Query.LTE("x", 10));
            var expected = "{ \"x\" : { \"$gte\" : 3, \"$lte\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Test]
        public void TestAllBsonArray()
        {
            var array = new BsonArray { 2, 4, null, 6 }; // null will be skipped due to functional construction semantics
            var query = Query.All("j", array);
            var selector = "{ \"$all\" : [2, 4, 6] }";
            Assert.AreEqual(PositiveTest("j", selector), query.ToJson());
            Assert.AreEqual(NegativeTest("j", selector), Query.Not(query).ToJson());
        }

        [Test]
        public void TestAllBsonArrayCastToIEnumerableBsonValue()
        {
            var array = new BsonArray { 2, 4, null, 6 }; // null will be skipped due to functional construction semantics
            var enumerable = (IEnumerable<BsonValue>)array;
            var query = Query.All("j", enumerable);
            var selector = "{ \"$all\" : [2, 4, 6] }";
            Assert.AreEqual(PositiveTest("j", selector), query.ToJson());
            Assert.AreEqual(NegativeTest("j", selector), Query.Not(query).ToJson());
        }

        [Test]
        public void TestAllIEnumerableBsonValue()
        {
            var enumerable = new List<BsonValue> { 2, 4, null, 6 }; // null will be skipped due to functional construction semantics
            var query = Query.All("j", enumerable);
            var selector = "{ \"$all\" : [2, 4, 6] }";
            Assert.AreEqual(PositiveTest("j", selector), query.ToJson());
            Assert.AreEqual(NegativeTest("j", selector), Query.Not(query).ToJson());
        }

        [Test]
        public void TestAnd()
        {
            var query = Query.And(
                Query.EQ("a", 1),
                Query.EQ("b", 2)
            );
            var expected = "{ \"a\" : 1, \"b\" : 2 }";
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Test]
        public void TestAndNoArgs()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => { Query.And(); });
            Assert.IsTrue(ex.Message.StartsWith("And cannot be called with zero queries."));
        }

        [Test]
        public void TestAndNull()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => { Query.And(Query.Null); });
            Assert.IsTrue(ex.Message.StartsWith("One of the queries is null."));
        }

        [Test]
        public void TestAndNullFirst()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Query.And(
                    Query.Null,
                    Query.EQ("x", 1)
                    );
            });
            Assert.IsTrue(ex.Message.StartsWith("One of the queries is null."));
        }

        [Test]
        public void TestAndNullSecond()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Query.And(
                    Query.EQ("x", 1),
                    Query.Null
                    );
            });
            Assert.IsTrue(ex.Message.StartsWith("One of the queries is null."));
        }

        [Test]
        public void TestAndWithEmptyQuery()
        {
            var emptyQuery = new QueryDocument();
            var expected = "{ }";
            var negated = "{ \"$nor\" : [{ }] }";

            var query = Query.And(emptyQuery);
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(negated, Query.Not(query).ToJson());

            query = Query.And(emptyQuery, emptyQuery);
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(negated, Query.Not(query).ToJson());

            expected = "{ \"x\" : 1 }";
            negated = "{ \"x\" : { \"$ne\" : 1 } }";

            query = Query.And(emptyQuery, Query.EQ("x", 1));
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(negated, Query.Not(query).ToJson());

            query = Query.And(Query.EQ("x", 1), emptyQuery);
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(negated, Query.Not(query).ToJson());

            query = Query.And(emptyQuery, Query.EQ("x", 1), emptyQuery);
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(negated, Query.Not(query).ToJson());

            query = Query.And(Query.EQ("x", 1), emptyQuery, Query.EQ("y", 2));
            expected = "{ \"x\" : 1, \"y\" : 2 }";
            negated = "{ \"$nor\" : [{ \"x\" : 1, \"y\" : 2 }] }";
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(negated, Query.Not(query).ToJson());
        }

        [Test]
        public void TestAndXNE1()
        {
            var query = Query.And(Query.NE("x", 1));
            var expected = "{ \"x\" : { \"$ne\" : 1 } }";
            var negated = "{ \"x\" : 1 }";
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(negated, Query.Not(query).ToJson());
        }

        [Test]
        public void TestAndXNE1XNE2()
        {
            var query = Query.And(Query.NE("x", 1), Query.NE("x", 2));
            var expected = "{ \"$and\" : [{ \"x\" : { \"$ne\" : 1 } }, { \"x\" : { \"$ne\" : 2 } }] }";
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Test]
        public void TestAndXNE1YNE2()
        {
            var query = Query.And(Query.NE("x", 1), Query.NE("y", 2));
            var expected = "{ \"x\" : { \"$ne\" : 1 }, \"y\" : { \"$ne\" : 2 } }";
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Test]
        public void TestElementMatch()
        {
            var query = Query.ElemMatch("x",
                Query.And(
                    Query.EQ("a", 1),
                    Query.GT("b", 1)
                )
            );
            var selector = "{ \"$elemMatch\" : { \"a\" : 1, \"b\" : { \"$gt\" : 1 } } }";
            Assert.AreEqual(PositiveTest("x", selector), query.ToJson());
            Assert.AreEqual(NegativeTest("x", selector), Query.Not(query).ToJson());
        }

        [Test]
        public void TestEQ()
        {
            var query = Query.EQ("x", 3);
            var expected = "{ \"x\" : 3 }";
            var negated = "{ \"x\" : { \"$ne\" : 3 } }";
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(negated, Query.Not(query).ToJson());
        }

        [Test]
        public void TestEQDBRef()
        {
            var query = Query.EQ("x", new BsonDocument { { "$ref", "c" }, { "$id", 1 } });
            var expected = "{ \"x\" : { \"$ref\" : \"c\", \"$id\" : 1 } }";
            var negated = "{ \"x\" : { \"$ne\" : { \"$ref\" : \"c\", \"$id\" : 1 } } }";
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(negated, Query.Not(query).ToJson());
        }

        [Test]
        public void TestEQTwoElements()
        {
            var query = Query.And(
                Query.EQ("x", 3),
                Query.EQ("y", "foo")
            );
            var expected = "{ \"x\" : 3, \"y\" : \"foo\" }";
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Test]
        public void TestExists()
        {
            var query = Query.Exists("x");
            var expected = "{ \"x\" : { \"$exists\" : true } }";
            var negated = "{ \"x\" : { \"$exists\" : false } }";
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(negated, Query.Not(query).ToJson());
        }

        [Test]
        public void TestExistsFalse()
        {
            var query = Query.NotExists("x");
            var expected = "{ \"x\" : { \"$exists\" : false } }";
            var negated = "{ \"x\" : { \"$exists\" : true } }";
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(negated, Query.Not(query).ToJson());
        }

        [Test]
        public void TestGeoIntersects()
        {
            var poly = GeoJson.Polygon(
                GeoJson.Geographic(40, 18),
                GeoJson.Geographic(40, 19),
                GeoJson.Geographic(41, 19),
                GeoJson.Geographic(40, 18));

            var query = Query.GeoIntersects("loc", poly);
            var selector = "{ '$geoIntersects' : { '$geometry' : { 'type' : 'Polygon', 'coordinates' : [[[40.0, 18.0], [40.0, 19.0], [41.0, 19.0], [40.0, 18.0]]] } } }";
            Assert.AreEqual(PositiveTest("loc", selector), query.ToJson());
            Assert.AreEqual(NegativeTest("loc", selector), Query.Not(query).ToJson());
        }

        [Test]
        public void TestGreaterThan()
        {
            var query = Query.GT("k", 10);
            var selector = "{ \"$gt\" : 10 }";
            Assert.AreEqual(PositiveTest("k", selector), query.ToJson());
            Assert.AreEqual(NegativeTest("k", selector), Query.Not(query).ToJson());
        }

        [Test]
        public void TestGreaterThanAndLessThan()
        {
            var query = Query.And(Query.GT("k", 10), Query.LT("k", 20));
            var expected = "{ \"k\" : { \"$gt\" : 10, \"$lt\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Test]
        public void TestGreaterThanAndLessThanOrEqual()
        {
            var query = Query.And(Query.GT("k", 10), Query.LTE("k", 20));
            var expected = "{ \"k\" : { \"$gt\" : 10, \"$lte\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Test]
        public void TestGreaterThanAndMod()
        {
            var query = Query.And(Query.GT("k", 10), Query.Mod("k", 10, 1));
            var expected = "{ \"k\" : { \"$gt\" : 10, \"$mod\" : [10, 1] } }";
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(NegateArbitraryQuery(query.ToJson()), Query.Not(query).ToJson());
        }

        [Test]
        public void TestGreaterThanOrEqual()
        {
            var query = Query.GTE("k", 10);
            var selector = "{ \"$gte\" : 10 }";
            Assert.AreEqual(PositiveTest("k", selector), query.ToJson());
            Assert.AreEqual(NegativeTest("k", selector), Query.Not(query).ToJson());
        }

        [Test]
        public void TestGreaterThanOrEqualAndLessThan()
        {
            var query = Query.And(Query.GTE("k", 10), Query.LT("k", 20));
            var expected = "{ \"k\" : { \"$gte\" : 10, \"$lt\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Test]
        public void TestGreaterThanOrEqualAndLessThanOrEqual()
        {
            var query = Query.And(Query.GTE("k", 10), Query.LTE("k", 20));
            var expected = "{ \"k\" : { \"$gte\" : 10, \"$lte\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Test]
        public void TestInBsonArray()
        {
            var array = new BsonArray { 2, 4, null, 6 }; // null will be skipped due to functional construction semantics
            var query = Query.In("j", array);
            var expected = "{ \"j\" : { \"$in\" : [2, 4, 6] } }";
            var negated = "{ \"j\" : { \"$nin\" : [2, 4, 6] } }";
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(negated, Query.Not(query).ToJson());
        }

        [Test]
        public void TestInBsonArrayCastToIEnumerableBsonValue()
        {
            var array = new BsonArray { 2, 4, null, 6 }; // null will be skipped due to functional construction semantics
            var enumerable = (IEnumerable<BsonValue>)array;
            var query = Query.In("j", enumerable);
            var expected = "{ \"j\" : { \"$in\" : [2, 4, 6] } }";
            var negated = "{ \"j\" : { \"$nin\" : [2, 4, 6] } }";
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(negated, Query.Not(query).ToJson());
        }

        [Test]
        public void TestInIEnumerableBsonValue()
        {
            var enumerable = new List<BsonValue> { 2, 4, null, 6 }; // null will be skipped due to functional construction semantics
            var query = Query.In("j", enumerable);
            var expected = "{ \"j\" : { \"$in\" : [2, 4, 6] } }";
            var negated = "{ \"j\" : { \"$nin\" : [2, 4, 6] } }";
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(negated, Query.Not(query).ToJson());
        }

        [Test]
        public void TestLessThan()
        {
            var query = Query.LT("k", 10);
            var selector = "{ \"$lt\" : 10 }";
            Assert.AreEqual(PositiveTest("k", selector), query.ToJson());
            Assert.AreEqual(NegativeTest("k", selector), Query.Not(query).ToJson());
        }

        [Test]
        public void TestLessThanOrEqual()
        {
            var query = Query.LTE("k", 10);
            var selector = "{ \"$lte\" : 10 }";
            Assert.AreEqual(PositiveTest("k", selector), query.ToJson());
            Assert.AreEqual(NegativeTest("k", selector), Query.Not(query).ToJson());
        }

        [Test]
        public void TestLessThanOrEqualAndNotEquals()
        {
            var query = Query.And(Query.LTE("k", 10), Query.NE("k", 5));
            var expected = "{ \"k\" : { \"$lte\" : 10, \"$ne\" : 5 } }";
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Test]
        public void TestLessThanOrEqualAndNotIn()
        {
            var query = Query.And(Query.LTE("k", 20), Query.NotIn("k", new BsonValue[] { 7, 11 }));
            var expected = "{ \"k\" : { \"$lte\" : 20, \"$nin\" : [7, 11] } }";
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Test]
        public void TestMatches()
        {
            var query = Query.Matches("a", "/abc/");
            var selector = "/abc/".Replace("'", "\"");
            Assert.AreEqual(PositiveTest("a", selector), query.ToJson());
            Assert.AreEqual(NegativeTest("a", selector), Query.Not(query).ToJson());
        }

        [Test]
        public void TestMod()
        {
            var query = Query.Mod("a", 10, 1);
            var selector = "{ \"$mod\" : [10, 1] }";
            Assert.AreEqual(PositiveTest("a", selector), query.ToJson());
            Assert.AreEqual(NegativeTest("a", selector), Query.Not(query).ToJson());
        }

        [Test]
        public void TestNear()
        {
            var query = Query.Near("loc", 1.1, 2.2);
            var selector = "{ '$near' : [1.1, 2.2] }";
            Assert.AreEqual(PositiveTest("loc", selector), query.ToJson());
            Assert.AreEqual(NegativeTest("loc", selector), Query.Not(query).ToJson());
        }

        [Test]
        public void TestNearWithMaxDistance()
        {
            var query = Query.Near("loc", 1.1, 2.2, 3.3);
            var expected = "{ 'loc' : { '$near' : [1.1, 2.2], '$maxDistance' : 3.3 } }".Replace("'", "\"");
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Test]
        public void TestNearWithSphericalTrue()
        {
            var query = Query.Near("loc", 1.1, 2.2, 3.3, true);
            var expected = "{ 'loc' : { '$nearSphere' : [1.1, 2.2], '$maxDistance' : 3.3 } }".Replace("'", "\"");
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Test]
        public void TestNearWithGeoJson()
        {
            var point = GeoJson.Point(GeoJson.Geographic(40, 18));
            var query = Query.Near("loc", point);
            var selector = "{ '$near' : { '$geometry' : { 'type' : 'Point', 'coordinates' : [40.0, 18.0] } } }";
            Assert.AreEqual(PositiveTest("loc", selector), query.ToJson());
            Assert.AreEqual(NegativeTest("loc", selector), Query.Not(query).ToJson());
        }

        [Test]
        public void TestNearWithGeoJsonWithMaxDistance()
        {
            var point = GeoJson.Point(GeoJson.Geographic(40, 18));
            var query = Query.Near("loc", point, 42);
            var expected = "{ 'loc' : { '$near' : { '$geometry' : { 'type' : 'Point', 'coordinates' : [40.0, 18.0] } }, '$maxDistance' : 42.0 } }".Replace("'", "\"");
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Test]
        public void TestNearWithGeoJsonWithSpherical()
        {
            var point = GeoJson.Point(GeoJson.Geographic(40, 18));
            var query = Query.Near("loc", point, 42, true);
            var expected = "{ 'loc' : { '$nearSphere' : { '$geometry' : { 'type' : 'Point', 'coordinates' : [40.0, 18.0] } }, '$maxDistance' : 42.0 } }".Replace("'", "\"");
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Test]
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
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Test]
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
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Test]
        public void TestNor()
        {
            var query = Query.Not(Query.Or(
                Query.EQ("a", 1),
                Query.EQ("b", 2)
            ));
            var expected = "{ \"$nor\" : [{ \"a\" : 1 }, { \"b\" : 2 }] }";
            var negated = "{ \"$or\" : [{ \"a\" : 1 }, { \"b\" : 2 }] }";
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(negated, Query.Not(query).ToJson());
        }

        [Test]
        public void TestNE()
        {
            var query = Query.NE("j", 3);
            var expected = "{ \"j\" : { \"$ne\" : 3 } }";
            var negated = "{ \"j\" : 3 }";
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(negated, Query.Not(query).ToJson());
        }

        [Test]
        public void TestNinBsonArray()
        {
            var array = new BsonArray { 2, 4, null, 6 }; // null will be skipped due to functional construction semantics
            var query = Query.NotIn("j", array);
            var expected = "{ \"j\" : { \"$nin\" : [2, 4, 6] } }";
            var negated = "{ \"j\" : { \"$in\" : [2, 4, 6] } }";
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(negated, Query.Not(query).ToJson());
        }

        [Test]
        public void TestNinBsonArrayCastToIEnumerableBsonValue()
        {
            var array = new BsonArray { 2, 4, null, 6 }; // null will be skipped due to functional construction semantics
            var enumerable = (IEnumerable<BsonValue>)array;
            var query = Query.NotIn("j", enumerable);
            var expected = "{ \"j\" : { \"$nin\" : [2, 4, 6] } }";
            var negated = "{ \"j\" : { \"$in\" : [2, 4, 6] } }";
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(negated, Query.Not(query).ToJson());
        }

        [Test]
        public void TestNinIEnumerableBsonValue()
        {
            var enumerable = new List<BsonValue> { 2, 4, null, 6 }; // null will be skipped due to functional construction semantics
            var query = Query.NotIn("j", enumerable);
            var expected = "{ \"j\" : { \"$nin\" : [2, 4, 6] } }";
            var negated = "{ \"j\" : { \"$in\" : [2, 4, 6] } }";
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(negated, Query.Not(query).ToJson());
        }

        [Test]
        public void TestOrXEQ1()
        {
            var query = Query.Or(Query.EQ("x", 1));
            var expected = "{ \"x\" : 1 }";
            var negated = "{ \"x\" : { \"$ne\" : 1 } }";
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(negated, Query.Not(query).ToJson());
        }

        [Test]
        public void TestOrXEQ1XEQ2()
        {
            var query = Query.Or(Query.EQ("x", 1), Query.EQ("x", 2));
            var expected = "{ \"$or\" : [{ \"x\" : 1 }, { \"x\" : 2 }] }";
            var negated = "{ \"$nor\" : [{ \"x\" : 1 }, { \"x\" : 2 }] }";
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(negated, Query.Not(query).ToJson());
        }

        [Test]
        public void TestOrNoArgs()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => { Query.Or(); });
            Assert.IsTrue(ex.Message.StartsWith("Or cannot be called with zero queries."));
        }

        [Test]
        public void TestOrNull()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => { Query.Or(Query.Null); });
            Assert.IsTrue(ex.Message.StartsWith("One of the queries is null."));
        }

        [Test]
        public void TestOrNullFirst()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Query.Or(
                    Query.Null,
                    Query.EQ("x", 1)
                    );
            });
            Assert.IsTrue(ex.Message.StartsWith("One of the queries is null."));
        }

        [Test]
        public void TestOrNullSecond()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Query.Or(
                    Query.EQ("x", 1),
                    Query.Null
                    );
            });
            Assert.IsTrue(ex.Message.StartsWith("One of the queries is null."));
        }

        [Test]
        public void TestOrWithEmptyQuery()
        {
            var emptyQuery = new QueryDocument();
            var expected = "{ }";
            var negated = "{ \"$nor\" : [{ }] }";

            var query = Query.Or(emptyQuery);
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(negated, Query.Not(query).ToJson());

            query = Query.Or(emptyQuery, emptyQuery);
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(negated, Query.Not(query).ToJson());

            query = Query.Or(emptyQuery, Query.EQ("x", 1));
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(negated, Query.Not(query).ToJson());

            query = Query.Or(Query.EQ("x", 1), emptyQuery);
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(negated, Query.Not(query).ToJson());

            query = Query.Or(emptyQuery, Query.EQ("x", 1), emptyQuery);
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(negated, Query.Not(query).ToJson());

            query = Query.Or(Query.EQ("x", 1), emptyQuery, Query.EQ("y", 2));
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(negated, Query.Not(query).ToJson());
        }

        [Test]
        public void TestRegex()
        {
            var query = Query.Matches("name", new BsonRegularExpression("acme.*corp", "i"));
            var selector = "/acme.*corp/i";
            Assert.AreEqual(PositiveTest("name", selector), query.ToJson());
            Assert.AreEqual(NegativeTest("name", selector), Query.Not(query).ToJson());
        }

        [Test]
        public void TestSize()
        {
            var query = Query.Size("k", 20);
            var selector = "{ \"$size\" : 20 }";
            Assert.AreEqual(PositiveTest("k", selector), query.ToJson());
            Assert.AreEqual(NegativeTest("k", selector), Query.Not(query).ToJson());
        }

        [Test]
        public void TestSizeAndAll()
        {
            var query = Query.And(Query.Size("k", 20), Query.All("k", new BsonArray { 7, 11 }));
            var expected = "{ \"k\" : { \"$size\" : 20, \"$all\" : [7, 11] } }";
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Test]
        public void TestSizeGreaterThan()
        {
            var query = Query.SizeGreaterThan("k", 20);
            var expected = "{ \"k.20\" : { \"$exists\" : true } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestSizeGreaterThanOrEqual()
        {
            var query = Query.SizeGreaterThanOrEqual("k", 20);
            var expected = "{ \"k.19\" : { \"$exists\" : true } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestSizeLessThan()
        {
            var query = Query.SizeLessThan("k", 20);
            var expected = "{ \"k.19\" : { \"$exists\" : false } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestSizeLessThanOrEqual()
        {
            var query = Query.SizeLessThanOrEqual("k", 20);
            var expected = "{ \"k.20\" : { \"$exists\" : false } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestSizeNotGreaterThan()
        {
            var query = Query.Not(Query.SizeGreaterThan("k", 20));
            var expected = "{ \"k.20\" : { \"$exists\" : false } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestSizeNotGreaterThanOrEqual()
        {
            var query = Query.Not(Query.SizeGreaterThanOrEqual("k", 20));
            var expected = "{ \"k.19\" : { \"$exists\" : false } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestSizeNotLessThan()
        {
            var query = Query.Not(Query.SizeLessThan("k", 20));
            var expected = "{ \"k.19\" : { \"$exists\" : true } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestSizeNotLessThanOrEqual()
        {
            var query = Query.Not(Query.SizeLessThanOrEqual("k", 20));
            var expected = "{ \"k.20\" : { \"$exists\" : true } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestTextQueryGeneration()
        {
            var query = Query.Text("foo");
            var expected = "{ \"$text\" : { \"$search\" : \"foo\" } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestTextWithLanguageQueryGeneration()
        {
            var query = Query.Text("foo", "norwegian");
            var expected = "{ \"$text\" : { \"$search\" : \"foo\", \"$language\" : \"norwegian\" } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestTextQueryGenerationWithNullLanguage()
        {
            var query = Query.Text("foo", null);
            var expected = "{ \"$text\" : { \"$search\" : \"foo\" } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestText()
        {
            if (_primary.Supports(FeatureId.TextSearchQuery))
            {
                using (_server.RequestStart(null, _primary))
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
                    Assert.AreEqual(1, results.Length);
                    Assert.AreEqual(1, results[0]["_id"].AsInt32);
                }
            }
        }

        [Test]
        public void TestTextWithLanguage()
        {
            if (_primary.Supports(FeatureId.TextSearchQuery))
            {
                using (_server.RequestStart(null, _primary))
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
                    Assert.AreEqual(1, results.Length);
                    Assert.AreEqual(1, results[0]["_id"].AsInt32);

                    query = Query.Text("stemmed", "english");
                    results = collection.Find(query).ToArray();
                    Assert.AreEqual(1, results.Length);
                    Assert.AreEqual(2, results[0]["_id"].AsInt32);
                }
            }
        }

        [Test]
        public void TestType()
        {
            var query = Query.Type("a", BsonType.String);
            var selector = "{ \"$type\" : 2 }";
            Assert.AreEqual(PositiveTest("a", selector), query.ToJson());
            Assert.AreEqual(NegativeTest("a", selector), Query.Not(query).ToJson());
        }

        [Test]
        public void TestWhere()
        {
            var query = Query.Where("this.a > 3");
            var expected = "{ \"$where\" : { \"$code\" : \"this.a > 3\" } }";
            Assert.AreEqual(expected, query.ToJson());
            Assert.AreEqual(NegateArbitraryQuery(expected), Query.Not(query).ToJson());
        }

        [Test]
        public void TestWithin()
        {
            var poly = GeoJson.Polygon(
                GeoJson.Geographic(40, 18),
                GeoJson.Geographic(40, 19),
                GeoJson.Geographic(41, 19),
                GeoJson.Geographic(40, 18));

            var query = Query.Within("loc", poly);
            var selector = "{ '$within' : { '$geometry' : { 'type' : 'Polygon', 'coordinates' : [[[40.0, 18.0], [40.0, 19.0], [41.0, 19.0], [40.0, 18.0]]] } } }";
            Assert.AreEqual(PositiveTest("loc", selector), query.ToJson());
            Assert.AreEqual(NegativeTest("loc", selector), Query.Not(query).ToJson());
        }

        [Test]
        public void TestWithinCircle()
        {
            var query = Query.WithinCircle("loc", 1.1, 2.2, 3.3);
            var selector = "{ '$within' : { '$center' : [[1.1, 2.2], 3.3] } }";
            Assert.AreEqual(PositiveTest("loc", selector), query.ToJson());
            Assert.AreEqual(NegativeTest("loc", selector), Query.Not(query).ToJson());
        }

        [Test]
        public void TestWithinCircleSpherical()
        {
            var query = Query.WithinCircle("loc", 1.1, 2.2, 3.3, true);
            var selector = "{ '$within' : { '$centerSphere' : [[1.1, 2.2], 3.3] } }";
            Assert.AreEqual(PositiveTest("loc", selector), query.ToJson());
            Assert.AreEqual(NegativeTest("loc", selector), Query.Not(query).ToJson());
        }

        [Test]
        public void TestWithinPolygon()
        {
            var points = new double[,] { { 1.1, 2.2 }, { 3.3, 4.4 } };
            var query = Query.WithinPolygon("loc", points);
            var selector = "{ '$within' : { '$polygon' : [[1.1, 2.2], [3.3, 4.4]] } }";
            Assert.AreEqual(PositiveTest("loc", selector), query.ToJson());
            Assert.AreEqual(NegativeTest("loc", selector), Query.Not(query).ToJson());
        }

        [Test]
        public void TestWithinPolygonInvalidSecondDimension()
        {
            var points = new double[,] { { 1, 2, 3 } };
            Assert.Throws<ArgumentOutOfRangeException>(() => Query.WithinPolygon("loc", points));
        }

        [Test]
        public void TestWithinRectangle()
        {
            var query = Query.WithinRectangle("loc", 1.1, 2.2, 3.3, 4.4);
            var selector = "{ '$within' : { '$box' : [[1.1, 2.2], [3.3, 4.4]] } }";
            Assert.AreEqual(PositiveTest("loc", selector), query.ToJson());
            Assert.AreEqual(NegativeTest("loc", selector), Query.Not(query).ToJson());
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
    }
}
