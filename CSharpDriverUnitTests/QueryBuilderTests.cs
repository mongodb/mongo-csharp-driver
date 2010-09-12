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
        public void TestAll() {
            //BsonDocument query = Query.In("j", new BsonArray { 2, 4, 6 });
            BsonDocument query = Query.All("j", 2, 4, 6);
            var expected = "{ \"j\" : { \"$all\" : [2, 4, 6] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestEquals() {
            BsonDocument query = Query.Eq("x", 3);
            var expected = "{ \"x\" : 3 }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestEqualsTwoElements() {
            BsonDocument query = Query.And(
                Query.Eq("x", 3),
                Query.Eq("y", "foo")
            );
            var expected = "{ \"x\" : 3, \"y\" : \"foo\" }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThan() {
            BsonDocument query = Query.GT("k", 10);
            var expected = "{ \"k\" : { \"$gt\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanAndLessThan() {
            BsonDocument query = Query.GT("k", 10).LT(20);
            var expected = "{ \"k\" : { \"$gt\" : 10, \"$lt\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanAndLessThanOrEqual() {
            BsonDocument query = Query.GT("k", 10).LE(20);
            var expected = "{ \"k\" : { \"$gt\" : 10, \"$lte\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanAndMod() {
            BsonDocument query = Query.GT("k", 10).Mod(10, 1);
            var expected = "{ \"k\" : { \"$gt\" : 10, \"$mod\" : [10, 1] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanOrEqual() {
            BsonDocument query = Query.GTE("k", 10);
            var expected = "{ \"k\" : { \"$gte\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanOrEqualAndLessThan() {
            BsonDocument query = Query.GTE("k", 10).LT(20);
            var expected = "{ \"k\" : { \"$gte\" : 10, \"$lt\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestGreaterThanOrEqualAndLessThanOrEqual() {
            BsonDocument query = Query.GTE("k", 10).LE(20);
            var expected = "{ \"k\" : { \"$gte\" : 10, \"$lte\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestIn() {
            //BsonDocument query = Query.In("j", new BsonArray { 2, 4, 6 });
            BsonDocument query = Query.In("j", 2, 4, 6);
            var expected = "{ \"j\" : { \"$in\" : [2, 4, 6] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThan() {
            BsonDocument query = Query.LT("k", 10);
            var expected = "{ \"k\" : { \"$lt\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThanOrEqual() {
            BsonDocument query = Query.LTE("k", 10);
            var expected = "{ \"k\" : { \"$lte\" : 10 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThanOrEqualAndNotEquals() {
            BsonDocument query = Query.LTE("k", 10).NE(5);
            var expected = "{ \"k\" : { \"$lte\" : 10, \"$ne\" : 5 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestLessThanOrEqualAndNotIn() {
            BsonDocument query = Query.LTE("k", 20).NotIn(7, 11);
            var expected = "{ \"k\" : { \"$lte\" : 20, \"$nin\" : [7, 11] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestMod() {
            BsonDocument query = Query.Mod("a", 10, 1);
            var expected = "{ \"a\" : { \"$mod\" : [10, 1] } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestNotEquals() {
            BsonDocument query = Query.NE("j", 3);
            var expected = "{ \"j\" : { \"$ne\" : 3 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestSize() {
            BsonDocument query = Query.Size("k", 20);
            var expected = "{ \"k\" : { \"$size\" : 20 } }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestSizeAndAll() {
            BsonDocument query = Query.Size("k", 20).All(7, 11);
            var expected = "{ \"k\" : { \"$size\" : 20, \"$all\" : [7, 11] } }";
            Assert.AreEqual(expected, query.ToJson());
        }
    }
}
