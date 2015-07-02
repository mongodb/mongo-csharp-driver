/* Copyright 2010-2015 MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.Driver.Tests.CommandResults
{
    [TestFixture]
    public class CommandResultTests
    {
        private MongoDatabase _database;

        [TestFixtureSetUp]
        public void Setup()
        {
            _database = LegacyTestConfiguration.Database;
        }

        [Test]
        public void TestCodeMissing()
        {
            var document = new BsonDocument();
            var result = new CommandResult(document);

            Assert.IsFalse(result.Code.HasValue);
        }

        [Test]
        public void TestCode()
        {
            var document = new BsonDocument("code", 18);
            var result = new CommandResult(document);

            Assert.IsTrue(result.Code.HasValue);
            Assert.AreEqual(18, result.Code);
        }

        [Test]
        public void TestOkMissing()
        {
            var document = new BsonDocument();
            var result = new CommandResult(document);
            Assert.That(result.Ok, Is.False);
        }

        [Test]
        public void TestOkFalse()
        {
            var document = new BsonDocument("ok", false);
            var result = new CommandResult(document);
            Assert.IsFalse(result.Ok);
            Assert.IsNotNull(result.ErrorMessage);
        }

        [Test]
        public void TestOkTrue()
        {
            var document = new BsonDocument("ok", true);
            var result = new CommandResult(document);
            Assert.IsTrue(result.Ok);
            Assert.IsNull(result.ErrorMessage);
        }

        [Test]
        public void TestOkZero()
        {
            var document = new BsonDocument("ok", 0);
            var result = new CommandResult(document);
            Assert.IsFalse(result.Ok);
            Assert.IsNotNull(result.ErrorMessage);
        }

        [Test]
        public void TestOkZeroPointZero()
        {
            var document = new BsonDocument("ok", 0.0);
            var result = new CommandResult(document);
            Assert.IsFalse(result.Ok);
            Assert.IsNotNull(result.ErrorMessage);
        }

        [Test]
        public void TestOkOne()
        {
            var document = new BsonDocument("ok", 1);
            var result = new CommandResult(document);
            Assert.IsTrue(result.Ok);
            Assert.IsNull(result.ErrorMessage);
        }

        [Test]
        public void TestOkOnePointZero()
        {
            var document = new BsonDocument("ok", 1.0);
            var result = new CommandResult(document);
            Assert.IsTrue(result.Ok);
            Assert.IsNull(result.ErrorMessage);
        }

        [Test]
        public void TestErrorMessageMissing()
        {
            var document = new BsonDocument();
            var result = new CommandResult(document);
            Assert.AreEqual("Unknown error", result.ErrorMessage);
        }

        [Test]
        public void TestErrorMessagePresent()
        {
            var document = new BsonDocument("errmsg", "An error message");
            var result = new CommandResult(document);
            Assert.AreEqual("An error message", result.ErrorMessage);
        }

        [Test]
        public void TestErrorMessageNotString()
        {
            var document = new BsonDocument("errmsg", 3.14159);
            var result = new CommandResult(document);
            Assert.AreEqual("3.14159", result.ErrorMessage);
        }

        [Test]
        public void TestIsMasterCommand()
        {
            var result = _database.RunCommand("ismaster");
            Assert.IsTrue(result.Ok);
        }

        [Test]
        public void TestInvalidCommand()
        {
            Assert.Throws<MongoCommandException>(() => _database.RunCommand("invalid"));
        }
    }
}
