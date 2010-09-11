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
using MongoDB.MongoDBClient;
using MongoDB.MongoDBClient.Builders;

namespace MongoDB.MongoDBClient.Tests {
    [TestFixture]
    public class QueryBuilderTests {
        [Test]
        public void TestAll() {
            //var query = Query.In("j", new BsonArray { 2, 4, 6 });
            var query = Query.All("j", 2, 4, 6);
            var document = (BsonDocument) query;
            var expected = "{ \"j\" : { \"$all\" : [2, 4, 6] } }";
            Assert.AreEqual(expected, document.ToJson());
        }

        [Test]
        public void TestEquals() {
            var query = Query.Eq("x", 3);
            var document = (BsonDocument) query;
            var expected = "{ \"x\" : 3 }";
            Assert.AreEqual(expected, document.ToJson());
        }

        [Test]
        public void TestEqualsTwoElements() {
            var query = Query.And(
                Query.Eq("x", 3),
                Query.Eq("y", "foo")
            );
            var document = (BsonDocument) query;
            var expected = "{ \"x\" : 3, \"y\" : \"foo\" }";
            Assert.AreEqual(expected, document.ToJson());
        }

        [Test]
        public void TestGreaterThan() {
            var query = Query.GT("k", 10);
            var document = (BsonDocument) query;
            var expected = "{ \"k\" : { \"$gt\" : 10 } }";
            Assert.AreEqual(expected, document.ToJson());
        }

        [Test]
        public void TestGreaterThanAndLessThan() {
            var query = Query.GT("k", 10).LT(20);
            var document = (BsonDocument) query;
            var expected = "{ \"k\" : { \"$gt\" : 10, \"$lt\" : 20 } }";
            Assert.AreEqual(expected, document.ToJson());
        }

        [Test]
        public void TestGreaterThanAndLessThanOrEqual() {
            var query = Query.GT("k", 10).LE(20);
            var document = (BsonDocument) query;
            var expected = "{ \"k\" : { \"$gt\" : 10, \"$lte\" : 20 } }";
            Assert.AreEqual(expected, document.ToJson());
        }

        [Test]
        public void TestGreaterThanAndMod() {
            var query = Query.GT("k", 10).Mod(10, 1);
            var document = (BsonDocument) query;
            var expected = "{ \"k\" : { \"$gt\" : 10, \"$mod\" : [10, 1] } }";
            Assert.AreEqual(expected, document.ToJson());
        }

        [Test]
        public void TestGreaterThanOrEqual() {
            var query = Query.GTE("k", 10);
            var document = (BsonDocument) query;
            var expected = "{ \"k\" : { \"$gte\" : 10 } }";
            Assert.AreEqual(expected, document.ToJson());
        }

        [Test]
        public void TestGreaterThanOrEqualAndLessThan() {
            var query = Query.GTE("k", 10).LT(20);
            var document = (BsonDocument) query;
            var expected = "{ \"k\" : { \"$gte\" : 10, \"$lt\" : 20 } }";
            Assert.AreEqual(expected, document.ToJson());
        }

        [Test]
        public void TestGreaterThanOrEqualAndLessThanOrEqual() {
            var query = Query.GTE("k", 10).LE(20);
            var document = (BsonDocument) query;
            var expected = "{ \"k\" : { \"$gte\" : 10, \"$lte\" : 20 } }";
            Assert.AreEqual(expected, document.ToJson());
        }

        [Test]
        public void TestIn() {
            //var query = Query.In("j", new BsonArray { 2, 4, 6 });
            var query = Query.In("j", 2, 4, 6);
            var document = (BsonDocument) query;
            var expected = "{ \"j\" : { \"$in\" : [2, 4, 6] } }";
            Assert.AreEqual(expected, document.ToJson());
        }

        [Test]
        public void TestLessThan() {
            var query = Query.LT("k", 10);
            var document = (BsonDocument) query;
            var expected = "{ \"k\" : { \"$lt\" : 10 } }";
            Assert.AreEqual(expected, document.ToJson());
        }

        [Test]
        public void TestLessThanOrEqual() {
            var query = Query.LTE("k", 10);
            var document = (BsonDocument) query;
            var expected = "{ \"k\" : { \"$lte\" : 10 } }";
            Assert.AreEqual(expected, document.ToJson());
        }

        [Test]
        public void TestLessThanOrEqualAndNotEquals() {
            var query = Query.LTE("k", 10).NE(5);
            var document = (BsonDocument) query;
            var expected = "{ \"k\" : { \"$lte\" : 10, \"$ne\" : 5 } }";
            Assert.AreEqual(expected, document.ToJson());
        }

        [Test]
        public void TestLessThanOrEqualAndNotIn() {
            var query = Query.LTE("k", 20).NotIn(7, 11);
            var document = (BsonDocument) query;
            var expected = "{ \"k\" : { \"$lte\" : 20, \"$nin\" : [7, 11] } }";
            Assert.AreEqual(expected, document.ToJson());
        }

        [Test]
        public void TestMod() {
            var query = Query.Mod("a", 10, 1);
            var document = (BsonDocument) query;
            var expected = "{ \"a\" : { \"$mod\" : [10, 1] } }";
            Assert.AreEqual(expected, document.ToJson());
        }

        [Test]
        public void TestNotEquals() {
            var query = Query.NE("j", 3);
            var document = (BsonDocument) query;
            var expected = "{ \"j\" : { \"$ne\" : 3 } }";
            Assert.AreEqual(expected, document.ToJson());
        }

        [Test]
        public void TestSize() {
            var query = Query.Size("k", 20);
            var document = (BsonDocument) query;
            var expected = "{ \"k\" : { \"$size\" : 20 } }";
            Assert.AreEqual(expected, document.ToJson());
        }

        [Test]
        public void TestSizeAndAll() {
            var query = Query.Size("k", 20).All(7, 11);
            var document = (BsonDocument) query;
            var expected = "{ \"k\" : { \"$size\" : 20, \"$all\" : [7, 11] } }";
            Assert.AreEqual(expected, document.ToJson());
        }
    }
}
