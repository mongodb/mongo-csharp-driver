/* Copyright 2010 10gen Inc.
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

namespace MongoDB.DriverUnitTests.Builders {
    [TestFixture]
    public class QueryBuilderTests {
        [Test]
        public void TestNewSyntax() {
            var query = Query.GTE("x", 3).LTE(10);
            var expected = "{ \"x\" : { \"$gte\" : 3, \"$lte\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestAll() {
            //var query = Query.All("j", new BsonArray { 2, 4, 6 });
            var query = Query.All("j", 2, 4, 6);
            var expected = "{ \"j\" : { \"$all\" : [2, 4, 6] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestElementMatch() {
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
        public void TestEquals() {
            var query = Query.EQ("x", 3);
            var expected = "{ \"x\" : 3 }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestEqualsTwoElements() {
            var query = Query.And(
                Query.EQ("x", 3),
                Query.EQ("y", "foo")
            );
            var expected = "{ \"x\" : 3, \"y\" : \"foo\" }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestExists() {
            var query = Query.Exists("x", true);
            var expected = "{ \"x\" : { \"$exists\" : true } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestExistsFalse() {
            var query = Query.Exists("x", false);
            var expected = "{ \"x\" : { \"$exists\" : false } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThan() {
            var query = Query.GT("k", 10);
            var expected = "{ \"k\" : { \"$gt\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanAndLessThan() {
            var query = Query.GT("k", 10).LT(20);
            var expected = "{ \"k\" : { \"$gt\" : 10, \"$lt\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanAndLessThanOrEqual() {
            var query = Query.GT("k", 10).LTE(20);
            var expected = "{ \"k\" : { \"$gt\" : 10, \"$lte\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanAndMod() {
            var query = Query.GT("k", 10).Mod(10, 1);
            var expected = "{ \"k\" : { \"$gt\" : 10, \"$mod\" : [10, 1] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanOrEqual() {
            var query = Query.GTE("k", 10);
            var expected = "{ \"k\" : { \"$gte\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanOrEqualAndLessThan() {
            var query = Query.GTE("k", 10).LT(20);
            var expected = "{ \"k\" : { \"$gte\" : 10, \"$lt\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanOrEqualAndLessThanOrEqual() {
            var query = Query.GTE("k", 10).LTE(20);
            var expected = "{ \"k\" : { \"$gte\" : 10, \"$lte\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestIn() {
            //var query = Query.@in("j", new BsonArray { 2, 4, 6 });
            var query = Query.In("j", 2, 4, 6);
            var expected = "{ \"j\" : { \"$in\" : [2, 4, 6] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThan() {
            var query = Query.LT("k", 10);
            var expected = "{ \"k\" : { \"$lt\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThanOrEqual() {
            var query = Query.LTE("k", 10);
            var expected = "{ \"k\" : { \"$lte\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThanOrEqualAndNotEquals() {
            var query = Query.LTE("k", 10).NE(5);
            var expected = "{ \"k\" : { \"$lte\" : 10, \"$ne\" : 5 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThanOrEqualAndNotIn() {
            var query = Query.LTE("k", 20).NotIn(7, 11);
            var expected = "{ \"k\" : { \"$lte\" : 20, \"$nin\" : [7, 11] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestMod() {
            var query = Query.Mod("a", 10, 1);
            var expected = "{ \"a\" : { \"$mod\" : [10, 1] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestNestedOr() {
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
        public void TestNotEquals() {
            var query = Query.NE("j", 3);
            var expected = "{ \"j\" : { \"$ne\" : 3 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestOr() {
            var query = Query.Or(
                Query.EQ("a", 1),
                Query.EQ("b", 2)
            );
            var expected = "{ \"$or\" : [{ \"a\" : 1 }, { \"b\" : 2 }] }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestRegex() {
            var query = Query.Matches("name", new BsonRegularExpression("acme.*corp", "i"));
            var expected = "{ \"name\" : /acme.*corp/i }";
            JsonWriterSettings settings = new JsonWriterSettings { OutputMode = JsonOutputMode.JavaScript };
            var actual = query.ToJson(settings);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestNotRegex() {
            var query = Query.Not("name").Matches(new BsonRegularExpression("acme.*corp", "i"));
            var expected = "{ \"name\" : { \"$not\" : /acme.*corp/i } }";
            JsonWriterSettings settings = new JsonWriterSettings { OutputMode = JsonOutputMode.JavaScript };
            var actual = query.ToJson(settings);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestSize() {
            var query = Query.Size("k", 20);
            var expected = "{ \"k\" : { \"$size\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestSizeAndAll() {
            var query = Query.Size("k", 20).All(7, 11);
            var expected = "{ \"k\" : { \"$size\" : 20, \"$all\" : [7, 11] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestType() {
            var query = Query.Type("a", BsonType.String);
            var expected = "{ \"a\" : { \"$type\" : 2 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestWhere() {
            var query = Query.Where("this.a > 3");
            var expected = "{ \"$where\" : { \"$code\" : \"this.a > 3\" } }";
            Assert.AreEqual(expected, query.ToJson());
        }
    }
}
