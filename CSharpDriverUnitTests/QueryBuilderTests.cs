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

using MongoDB.BsonLibrary;
using MongoDB.CSharpDriver;
using MongoDB.CSharpDriver.Builders;

namespace MongoDB.CSharpDriver.Tests {
    [TestFixture]
    public class QueryBuilderTests {
        [Test]
        public void TestNewSyntax() {
            BsonDocument query = Query.Element("x").gte(3).lte(10);
            var expected = "{ \"x\" : { \"$gte\" : 3, \"$lte\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestAll() {
            //BsonDocument query = Query.Element("j").all(new BsonArray { 2, 4, 6 });
            BsonDocument query = Query.Element("j").all(2, 4, 6);
            var expected = "{ \"j\" : { \"$all\" : [2, 4, 6] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestElementMatch() {
            BsonDocument query = Query.Element("x").elemMatch(
                Query.and(
                    Query.Element("a").eq(1),
                    Query.Element("b").gt(1)
                )
            );
            var expected = "{ \"x\" : { \"$elemMatch\" : { \"a\" : 1, \"b\" : { \"$gt\" : 1 } } } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestEquals() {
            BsonDocument query = Query.Element("x").eq(3);
            var expected = "{ \"x\" : 3 }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestEqualsTwoElements() {
            BsonDocument query = Query.and(
                Query.Element("x").eq(3),
                Query.Element("y").eq("foo")
            );
            var expected = "{ \"x\" : 3, \"y\" : \"foo\" }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestExists() {
            BsonDocument query = Query.Element("x").exists(true);
            var expected = "{ \"x\" : { \"$exists\" : true } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestExistsFalse() {
            BsonDocument query = Query.Element("x").exists(false);
            var expected = "{ \"x\" : { \"$exists\" : false } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThan() {
            BsonDocument query = Query.Element("k").gt(10);
            var expected = "{ \"k\" : { \"$gt\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanAndLessThan() {
            BsonDocument query = Query.Element("k").gt(10).lt(20);
            var expected = "{ \"k\" : { \"$gt\" : 10, \"$lt\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanAndLessThanOrEqual() {
            BsonDocument query = Query.Element("k").gt(10).lte(20);
            var expected = "{ \"k\" : { \"$gt\" : 10, \"$lte\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanAndMod() {
            BsonDocument query = Query.Element("k").gt(10).mod(10, 1);
            var expected = "{ \"k\" : { \"$gt\" : 10, \"$mod\" : [10, 1] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanOrEqual() {
            BsonDocument query = Query.Element("k").gte(10);
            var expected = "{ \"k\" : { \"$gte\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanOrEqualAndLessThan() {
            BsonDocument query = Query.Element("k").gte(10).lt(20);
            var expected = "{ \"k\" : { \"$gte\" : 10, \"$lt\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanOrEqualAndLessThanOrEqual() {
            BsonDocument query = Query.Element("k").gte(10).lte(20);
            var expected = "{ \"k\" : { \"$gte\" : 10, \"$lte\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestIn() {
            //BsonDocument query = Query.Element("j").In(new BsonArray { 2, 4, 6 });
            BsonDocument query = Query.Element("j").@in(2, 4, 6);
            var expected = "{ \"j\" : { \"$in\" : [2, 4, 6] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThan() {
            BsonDocument query = Query.Element("k").lt(10);
            var expected = "{ \"k\" : { \"$lt\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThanOrEqual() {
            BsonDocument query = Query.Element("k").lte(10);
            var expected = "{ \"k\" : { \"$lte\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThanOrEqualAndNotEquals() {
            BsonDocument query = Query.Element("k").lte(10).ne(5);
            var expected = "{ \"k\" : { \"$lte\" : 10, \"$ne\" : 5 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThanOrEqualAndNotIn() {
            BsonDocument query = Query.Element("k").lte(20).nin(7, 11);
            var expected = "{ \"k\" : { \"$lte\" : 20, \"$nin\" : [7, 11] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestMod() {
            BsonDocument query = Query.Element("a").mod(10, 1);
            var expected = "{ \"a\" : { \"$mod\" : [10, 1] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestNestedOr() {
            BsonDocument query = Query.and(
                Query.Element("name").eq("bob"),
                Query.or(
                    Query.Element("a").eq(1),
                    Query.Element("b").eq(2)
                )
            );
            var expected = "{ \"name\" : \"bob\", \"$or\" : [{ \"a\" : 1 }, { \"b\" : 2 }] }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestNotEquals() {
            BsonDocument query = Query.Element("j").ne(3);
            var expected = "{ \"j\" : { \"$ne\" : 3 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestOr() {
            BsonDocument query = Query.or(
                Query.Element("a").eq(1),
                Query.Element("b").eq(2)
            );
            var expected = "{ \"$or\" : [{ \"a\" : 1 }, { \"b\" : 2 }] }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestRegex() {
            BsonDocument query = Query.Element("name").regex(new BsonRegularExpression("acme.*corp", "i"));
            var expected = "{ \"name\" : /acme.*corp/i }";
            BsonJsonWriterSettings settings = new BsonJsonWriterSettings { OutputMode = BsonJsonOutputMode.JavaScript };
            var actual = query.ToJson(settings);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestSize() {
            BsonDocument query = Query.Element("k").size(20);
            var expected = "{ \"k\" : { \"$size\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestSizeAndAll() {
            BsonDocument query = Query.Element("k").size(20).all(7, 11);
            var expected = "{ \"k\" : { \"$size\" : 20, \"$all\" : [7, 11] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestType() {
            BsonDocument query = Query.Element("a").type(BsonType.String);
            var expected = "{ \"a\" : { \"$type\" : 2 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestWhere() {
            BsonDocument query = Query.where("this.a > 3");
            var expected = "{ \"$where\" : { \"$code\" : \"this.a > 3\" } }";
            Assert.AreEqual(expected, query.ToJson());
        }
    }
}
