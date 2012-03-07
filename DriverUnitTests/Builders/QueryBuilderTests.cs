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
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace MongoDB.DriverUnitTests.Builders
{
    [TestFixture]
    public class QueryBuilderTests
    {
        [Test]
        public void TestNewSyntax()
        {
            var query = Query.GTE("x", 3).LTE(10);
            var expected = "{ \"x\" : { \"$gte\" : 3, \"$lte\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestAll()
        {
            //var query = Query.All("j", new BsonArray { 2, 4, 6 });
            var query = Query.All("j", 2, 4, 6);
            var expected = "{ \"j\" : { \"$all\" : [2, 4, 6] } }";
            Assert.AreEqual(expected, query.ToJson());
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
        }

        [Test]
        public void TestAndNoArgs()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => { Query.And(); });
            Assert.IsTrue(ex.Message.StartsWith("Query.And cannot be called with zero queries."));
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
        public void TestElementMatch()
        {
            var query = Query.ElemMatch("x", 
                Query.And(
                    Query.EQ("a", 1),
                    Query.GT("b", 1)
                )
            );
            var expected = "{ \"x\" : { \"$elemMatch\" : { \"a\" : 1, \"b\" : { \"$gt\" : 1 } } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestEquals()
        {
            var query = Query.EQ("x", 3);
            var expected = "{ \"x\" : 3 }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestEqualsTwoElements()
        {
            var query = Query.And(
                Query.EQ("x", 3),
                Query.EQ("y", "foo")
            );
            var expected = "{ \"x\" : 3, \"y\" : \"foo\" }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestExists()
        {
            var query = Query.Exists("x", true);
            var expected = "{ \"x\" : { \"$exists\" : true } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestExistsFalse()
        {
            var query = Query.Exists("x", false);
            var expected = "{ \"x\" : { \"$exists\" : false } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThan()
        {
            var query = Query.GT("k", 10);
            var expected = "{ \"k\" : { \"$gt\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanAndLessThan()
        {
            var query = Query.GT("k", 10).LT(20);
            var expected = "{ \"k\" : { \"$gt\" : 10, \"$lt\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanAndLessThanOrEqual()
        {
            var query = Query.GT("k", 10).LTE(20);
            var expected = "{ \"k\" : { \"$gt\" : 10, \"$lte\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanAndMod()
        {
            var query = Query.GT("k", 10).Mod(10, 1);
            var expected = "{ \"k\" : { \"$gt\" : 10, \"$mod\" : [10, 1] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanOrEqual()
        {
            var query = Query.GTE("k", 10);
            var expected = "{ \"k\" : { \"$gte\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanOrEqualAndLessThan()
        {
            var query = Query.GTE("k", 10).LT(20);
            var expected = "{ \"k\" : { \"$gte\" : 10, \"$lt\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanOrEqualAndLessThanOrEqual()
        {
            var query = Query.GTE("k", 10).LTE(20);
            var expected = "{ \"k\" : { \"$gte\" : 10, \"$lte\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestIn()
        {
            var query = Query.In("j", 2, 4, 6);
            var expected = "{ \"j\" : { \"$in\" : [2, 4, 6] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThan()
        {
            var query = Query.LT("k", 10);
            var expected = "{ \"k\" : { \"$lt\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThanOrEqual()
        {
            var query = Query.LTE("k", 10);
            var expected = "{ \"k\" : { \"$lte\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThanOrEqualAndNotEquals()
        {
            var query = Query.LTE("k", 10).NE(5);
            var expected = "{ \"k\" : { \"$lte\" : 10, \"$ne\" : 5 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThanOrEqualAndNotIn()
        {
            var query = Query.LTE("k", 20).NotIn(7, 11);
            var expected = "{ \"k\" : { \"$lte\" : 20, \"$nin\" : [7, 11] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestMatches()
        {
            var query = Query.Matches("a", "/abc/");
            var expected = "{ 'a' : /abc/ }".Replace("'", "\"");
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestMod()
        {
            var query = Query.Mod("a", 10, 1);
            var expected = "{ \"a\" : { \"$mod\" : [10, 1] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestNear()
        {
            var query = Query.Near("loc", 1.1, 2.2);
            var expected = "{ 'loc' : { '$near' : [1.1, 2.2] } }".Replace("'", "\"");
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestNearWithMaxDistance()
        {
            var query = Query.Near("loc", 1.1, 2.2, 3.3);
            var expected = "{ 'loc' : { '$near' : [1.1, 2.2], '$maxDistance' : 3.3 } }".Replace("'", "\"");
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestNearWithSphericalTrue()
        {
            var query = Query.Near("loc", 1.1, 2.2, 3.3, true);
            var expected = "{ 'loc' : { '$nearSphere' : [1.1, 2.2], '$maxDistance' : 3.3 } }".Replace("'", "\"");
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestNestedNor()
        {
            var query = Query.And(
                Query.EQ("name", "bob"),
                Query.Nor(
                    Query.EQ("a", 1),
                    Query.EQ("b", 2)
                )
            );
            var expected = "{ \"name\" : \"bob\", \"$nor\" : [{ \"a\" : 1 }, { \"b\" : 2 }] }";
            Assert.AreEqual(expected, query.ToJson());
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
        }

        [Test]
        public void TestNor()
        {
            var query = Query.Nor(
                Query.EQ("a", 1),
                Query.EQ("b", 2)
            );
            var expected = "{ \"$nor\" : [{ \"a\" : 1 }, { \"b\" : 2 }] }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestNotEquals()
        {
            var query = Query.NE("j", 3);
            var expected = "{ \"j\" : { \"$ne\" : 3 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestNin()
        {
            var query = Query.NotIn("j", 2, 4, 6);
            var expected = "{ \"j\" : { \"$nin\" : [2, 4, 6] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestOr()
        {
            var query = Query.Or(
                Query.EQ("a", 1),
                Query.EQ("b", 2)
            );
            var expected = "{ \"$or\" : [{ \"a\" : 1 }, { \"b\" : 2 }] }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestOrNoArgs()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => { Query.Or(); });
            Assert.IsTrue(ex.Message.StartsWith("Query.Or cannot be called with zero queries."));
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
        public void TestRegex()
        {
            var query = Query.Matches("name", new BsonRegularExpression("acme.*corp", "i"));
            var expected = "{ \"name\" : /acme.*corp/i }";
            JsonWriterSettings settings = new JsonWriterSettings { OutputMode = JsonOutputMode.JavaScript };
            var actual = query.ToJson(settings);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestNotAll()
        {
            var query = Query.Not("name").All(1, 2, 3);
            var expected = "{ \"name\" : { \"$not\" : { \"$all\" : [1, 2, 3] } } }";
            JsonWriterSettings settings = new JsonWriterSettings { OutputMode = JsonOutputMode.JavaScript };
            var actual = query.ToJson(settings);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestNotElemMatch()
        {
            var query = Query.Not("name").ElemMatch(Query.GT("x", 1));
            var expected = "{ \"name\" : { \"$not\" : { \"$elemMatch\" : { \"x\" : { \"$gt\" : 1 } } } } }";
            JsonWriterSettings settings = new JsonWriterSettings { OutputMode = JsonOutputMode.JavaScript };
            var actual = query.ToJson(settings);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestNotExists()
        {
            var query = Query.Not("name").Exists(false);
            var expected = "{ \"name\" : { \"$not\" : { \"$exists\" : false } } }";
            JsonWriterSettings settings = new JsonWriterSettings { OutputMode = JsonOutputMode.JavaScript };
            var actual = query.ToJson(settings);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestNotGT()
        {
            var query = Query.Not("name").GT(1);
            var expected = "{ \"name\" : { \"$not\" : { \"$gt\" : 1 } } }";
            JsonWriterSettings settings = new JsonWriterSettings { OutputMode = JsonOutputMode.JavaScript };
            var actual = query.ToJson(settings);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestNotIn()
        {
            var query = Query.Not("name").In(1, 2, 3);
            var expected = "{ \"name\" : { \"$not\" : { \"$in\" : [1, 2, 3] } } }";
            JsonWriterSettings settings = new JsonWriterSettings { OutputMode = JsonOutputMode.JavaScript };
            var actual = query.ToJson(settings);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestNotNin()
        {
            var query = Query.Not("name").NotIn(1, 2, 3);
            var expected = "{ \"name\" : { \"$not\" : { \"$nin\" : [1, 2, 3] } } }";
            JsonWriterSettings settings = new JsonWriterSettings { OutputMode = JsonOutputMode.JavaScript };
            var actual = query.ToJson(settings);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestNotRegex()
        {
            var query = Query.Not("name").Matches(new BsonRegularExpression("acme.*corp", "i"));
            var expected = "{ \"name\" : { \"$not\" : /acme.*corp/i } }";
            JsonWriterSettings settings = new JsonWriterSettings { OutputMode = JsonOutputMode.JavaScript };
            var actual = query.ToJson(settings);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestNotSize()
        {
            var query = Query.Not("name").Size(1);
            var expected = "{ \"name\" : { \"$not\" : { \"$size\" : 1 } } }";
            JsonWriterSettings settings = new JsonWriterSettings { OutputMode = JsonOutputMode.JavaScript };
            var actual = query.ToJson(settings);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestSize()
        {
            var query = Query.Size("k", 20);
            var expected = "{ \"k\" : { \"$size\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestSizeAndAll()
        {
            var query = Query.Size("k", 20).All(7, 11);
            var expected = "{ \"k\" : { \"$size\" : 20, \"$all\" : [7, 11] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestType()
        {
            var query = Query.Type("a", BsonType.String);
            var expected = "{ \"a\" : { \"$type\" : 2 } }";
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
            var query = Query.WithinCircle("loc", 1.1, 2.2, 3.3);
            var expected = "{ 'loc' : { '$within' : { '$center' : [[1.1, 2.2], 3.3] } } }".Replace("'", "\"");
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestWithinCircleSpherical()
        {
            var query = Query.WithinCircle("loc", 1.1, 2.2, 3.3, true);
            var expected = "{ 'loc' : { '$within' : { '$centerSphere' : [[1.1, 2.2], 3.3] } } }".Replace("'", "\"");
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestWithinPolygon()
        {
            var points = new double[,] { { 1.1, 2.2 }, { 3.3, 4.4 } };
            var query = Query.WithinPolygon("loc", points);
            var expected = "{ 'loc' : { '$within' : { '$polygon' : [[1.1, 2.2], [3.3, 4.4]] } } }".Replace("'", "\"");
            Assert.AreEqual(expected, query.ToJson());
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
            var expected = "{ 'loc' : { '$within' : { '$box' : [[1.1, 2.2], [3.3, 4.4]] } } }".Replace("'", "\"");
            Assert.AreEqual(expected, query.ToJson());
        }
    }
}
