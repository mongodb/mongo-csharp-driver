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

namespace MongoDB.CSharpDriver.UnitTests.Builders {
    [TestFixture]
    public class UpdateBuilderTests {
        [Test]
        public void TestAddToSet() {
            var update = Update.addToSet("name", "abc");
            var expected = "{ \"$addToSet\" : { \"name\" : \"abc\" } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestAddToSetEach() {
            var update = Update.addToSetEach("name", "abc", "def");
            var expected = "{ \"$addToSet\" : { \"name\" : { \"$each\" : [\"abc\", \"def\"] } } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestIncDouble() {
            var update = Update.inc("name", 1.1);
            var expected = "{ \"$inc\" : { \"name\" : 1.1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestIncInt() {
            var update = Update.inc("name", 1);
            var expected = "{ \"$inc\" : { \"name\" : 1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestIncLong() {
            var update = Update.inc("name", 1L);
            var expected = "{ \"$inc\" : { \"name\" : 1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPopFirst() {
            var update = Update.popFirst("name");
            var expected = "{ \"$pop\" : { \"name\" : 1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPopLast() {
            var update = Update.popLast("name");
            var expected = "{ \"$pop\" : { \"name\" : -1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPull() {
            var update = Update.pull("name", "abc");
            var expected = "{ \"$pull\" : { \"name\" : \"abc\" } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPullAll() {
            var update = Update.pullAll("name", "abc", "def");
            var expected = "{ \"$pullAll\" : { \"name\" : [\"abc\", \"def\"] } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPush() {
            var update = Update.push("name", "abc");
            var expected = "{ \"$push\" : { \"name\" : \"abc\" } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPushAll() {
            var update = Update.pushAll("name", "abc", "def");
            var expected = "{ \"$pushAll\" : { \"name\" : [\"abc\", \"def\"] } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSet() {
            var update = Update.set("name", "abc");
            var expected = "{ \"$set\" : { \"name\" : \"abc\" } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestUnset() {
            var update = Update.unset("name");
            var expected = "{ \"$unset\" : { \"name\" : 1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestAddToSetTwice() {
            var update = Update.addToSet("a", 1).addToSet("b", 2);
            var expected = "{ \"$addToSet\" : { \"a\" : 1, \"b\" : 2 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestAddToSetEachTwice() {
            var update = Update.addToSetEach("a", 1, 2).addToSetEach("b", 3, 4);
            var expected = "{ \"$addToSet\" : { \"a\" : { \"$each\" : [1, 2] }, \"b\" : { \"$each\" : [3, 4] } } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestIncDoubleTwice() {
            var update = Update.inc("x", 1.1).inc("y", 2.2);
            var expected = "{ \"$inc\" : { \"x\" : 1.1, \"y\" : 2.2 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestIncIntTwice() {
            var update = Update.inc("x", 1).inc("y", 2);
            var expected = "{ \"$inc\" : { \"x\" : 1, \"y\" : 2 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestIncLongTwice() {
            var update = Update.inc("x", 1L).inc("y", 2L);
            var expected = "{ \"$inc\" : { \"x\" : 1, \"y\" : 2 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPopFirstTwice() {
            var update = Update.popFirst("a").popFirst("b");
            var expected = "{ \"$pop\" : { \"a\" : 1, \"b\" : 1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPopLastTwice() {
            var update = Update.popLast("a").popLast("b");
            var expected = "{ \"$pop\" : { \"a\" : -1, \"b\" : -1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPullTwice() {
            var update = Update.pull("a", 1).pull("b", 2);
            var expected = "{ \"$pull\" : { \"a\" : 1, \"b\" : 2 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPullAllTwice() {
            var update = Update.pullAll("a", 1, 2).pullAll("b", 3, 4);
            var expected = "{ \"$pullAll\" : { \"a\" : [1, 2], \"b\" : [3, 4] } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPushTwice() {
            var update = Update.push("a", 1).push("b", 2);
            var expected = "{ \"$push\" : { \"a\" : 1, \"b\" : 2 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPushAllTwice() {
            var update = Update.pushAll("a", 1, 2).pushAll("b", 3, 4);
            var expected = "{ \"$pushAll\" : { \"a\" : [1, 2], \"b\" : [3, 4] } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSetTwice() {
            var update = Update.set("a", 1).set("b", 2);
            var expected = "{ \"$set\" : { \"a\" : 1, \"b\" : 2 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestUnsetTwice() {
            var update = Update.unset("a").unset("b");
            var expected = "{ \"$unset\" : { \"a\" : 1, \"b\" : 1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSetThenAddToSet() {
            var update = Update.set("x", 1).addToSet("name", "abc");
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$addToSet\" : { \"name\" : \"abc\" } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSetThenAddToSetEach() {
            var update = Update.set("x", 1).addToSetEach("name", "abc", "def");
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$addToSet\" : { \"name\" : { \"$each\" : [\"abc\", \"def\"] } } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSetThenIncDouble() {
            var update = Update.set("x", 1).inc("name", 1.1);
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$inc\" : { \"name\" : 1.1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSetThenIncInt() {
            var update = Update.set("x", 1).inc("name", 1);
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$inc\" : { \"name\" : 1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSetThenIncLong() {
            var update = Update.set("x", 1).inc("name", 1L);
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$inc\" : { \"name\" : 1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSetThenPopFirst() {
            var update = Update.set("x", 1).popFirst("name");
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$pop\" : { \"name\" : 1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSetThenPopLast() {
            var update = Update.set("x", 1).popLast("name");
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$pop\" : { \"name\" : -1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSetThenPull() {
            var update = Update.set("x", 1).pull("name", "abc");
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$pull\" : { \"name\" : \"abc\" } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSetThenPullAll() {
            var update = Update.set("x", 1).pullAll("name", "abc", "def");
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$pullAll\" : { \"name\" : [\"abc\", \"def\"] } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSetThenPush() {
            var update = Update.set("x", 1).push("name", "abc");
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$push\" : { \"name\" : \"abc\" } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSetThenPushAll() {
            var update = Update.set("x", 1).pushAll("name", "abc", "def");
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$pushAll\" : { \"name\" : [\"abc\", \"def\"] } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSetThenUnset() {
            var update = Update.set("x", 1).unset("name");
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$unset\" : { \"name\" : 1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }
    }
}
