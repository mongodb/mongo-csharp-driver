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
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MongoDB.DriverOnlineTests.CommandResults {
    [TestFixture]
    public class CommandResultTests {
        private MongoServer server;
        private MongoDatabase database;

        [TestFixtureSetUp]
        public void Setup() {
            server = MongoServer.Create();
            database = server["onlinetests"];
        }

        [Test]
        public void TestOkMissing() {
            var document = new BsonDocument();
            var bson = document.ToBson();
            var result = BsonSerializer.Deserialize<CommandResult>(bson);
            Assert.Throws<MongoCommandException>(() => { var dummy = result.Ok; });
        }

        [Test]
        public void TestOkFalse() {
            var document = new BsonDocument("ok", false);
            var bson = document.ToBson();
            var result = BsonSerializer.Deserialize<CommandResult>(bson);
            Assert.IsFalse(result.Ok);
            Assert.IsNotNull(result.ErrorMessage);
        }

        [Test]
        public void TestOkTrue() {
            var document = new BsonDocument("ok", true);
            var bson = document.ToBson();
            var result = BsonSerializer.Deserialize<CommandResult>(bson);
            Assert.IsTrue(result.Ok);
            Assert.IsNull(result.ErrorMessage);
        }

        [Test]
        public void TestOkZero() {
            var document = new BsonDocument("ok", 0);
            var bson = document.ToBson();
            var result = BsonSerializer.Deserialize<CommandResult>(bson);
            Assert.IsFalse(result.Ok);
            Assert.IsNotNull(result.ErrorMessage);
        }

        [Test]
        public void TestOkZeroPointZero() {
            var document = new BsonDocument("ok", 0.0);
            var bson = document.ToBson();
            var result = BsonSerializer.Deserialize<CommandResult>(bson);
            Assert.IsFalse(result.Ok);
            Assert.IsNotNull(result.ErrorMessage);
        }

        [Test]
        public void TestOkOne() {
            var document = new BsonDocument("ok", 1);
            var bson = document.ToBson();
            var result = BsonSerializer.Deserialize<CommandResult>(bson);
            Assert.IsTrue(result.Ok);
            Assert.IsNull(result.ErrorMessage);
        }

        [Test]
        public void TestOkOnePointZero() {
            var document = new BsonDocument("ok", 1.0);
            var bson = document.ToBson();
            var result = BsonSerializer.Deserialize<CommandResult>(bson);
            Assert.IsTrue(result.Ok);
            Assert.IsNull(result.ErrorMessage);
        }

        [Test]
        public void TestErrorMessageMissing() {
            var document = new BsonDocument();
            var bson = document.ToBson();
            var result = BsonSerializer.Deserialize<CommandResult>(bson);
            Assert.AreEqual("Unknown error", result.ErrorMessage);
        }

        [Test]
        public void TestErrorMessagePresent() {
            var document = new BsonDocument("errmsg", "An error message");
            var bson = document.ToBson();
            var result = BsonSerializer.Deserialize<CommandResult>(bson);
            Assert.AreEqual("An error message", result.ErrorMessage);
        }

        [Test]
        public void TestErrorMessageNotString() {
            var document = new BsonDocument("errmsg", 3.14159);
            var bson = document.ToBson();
            var result = BsonSerializer.Deserialize<CommandResult>(bson);
            Assert.AreEqual("3.14159", result.ErrorMessage);
        }

        [Test]
        public void TestIsMasterCommand() {
            var result = database.RunCommand("ismaster");
            Assert.IsTrue(result.Ok);
        }

        [Test]
        public void TestInvalidCommand() {
            try {
                var result = database.RunCommand("invalidcommand");
            } catch (MongoCommandException ex) {
                Assert.AreEqual("Command failed: no such cmd", ex.Message);
            }
        }
    }
}
