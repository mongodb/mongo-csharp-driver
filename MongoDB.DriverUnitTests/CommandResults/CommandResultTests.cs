/* Copyright 2010-2013 10gen Inc.
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

namespace MongoDB.DriverUnitTests.CommandResults
{
    [TestFixture]
    public class CommandResultTests
    {
        private MongoServer _server;
        private MongoDatabase _database;

        [TestFixtureSetUp]
        public void Setup()
        {
            _server = Configuration.TestServer;
            _database = Configuration.TestDatabase;
        }

        [Test]
        public void TestOkMissing()
        {
            var command = new CommandDocument("invalid", 1);
            var document = new BsonDocument();
            var result = new CommandResult(command, document);
            try
            {
                var dummy = result.Ok;
            }
            catch (MongoCommandException ex)
            {
                Assert.IsTrue(ex.Message.StartsWith("Command 'invalid' failed. Response has no ok element (response was ", StringComparison.Ordinal));
            }
        }

        [Test]
        public void TestOkFalse()
        {
            var document = new BsonDocument("ok", false);
            var result = new CommandResult(null, document);
            Assert.IsFalse(result.Ok);
            Assert.IsNotNull(result.ErrorMessage);
        }

        [Test]
        public void TestOkTrue()
        {
            var document = new BsonDocument("ok", true);
            var result = new CommandResult(null, document);
            Assert.IsTrue(result.Ok);
            Assert.IsNull(result.ErrorMessage);
        }

        [Test]
        public void TestOkZero()
        {
            var document = new BsonDocument("ok", 0);
            var result = new CommandResult(null, document);
            Assert.IsFalse(result.Ok);
            Assert.IsNotNull(result.ErrorMessage);
        }

        [Test]
        public void TestOkZeroPointZero()
        {
            var document = new BsonDocument("ok", 0.0);
            var result = new CommandResult(null, document);
            Assert.IsFalse(result.Ok);
            Assert.IsNotNull(result.ErrorMessage);
        }

        [Test]
        public void TestOkOne()
        {
            var document = new BsonDocument("ok", 1);
            var result = new CommandResult(null, document);
            Assert.IsTrue(result.Ok);
            Assert.IsNull(result.ErrorMessage);
        }

        [Test]
        public void TestOkOnePointZero()
        {
            var document = new BsonDocument("ok", 1.0);
            var result = new CommandResult(null, document);
            Assert.IsTrue(result.Ok);
            Assert.IsNull(result.ErrorMessage);
        }

        [Test]
        public void TestErrorMessageMissing()
        {
            var document = new BsonDocument();
            var result = new CommandResult(null, document);
            Assert.AreEqual("Unknown error", result.ErrorMessage);
        }

        [Test]
        public void TestErrorMessagePresent()
        {
            var document = new BsonDocument("errmsg", "An error message");
            var result = new CommandResult(null, document);
            Assert.AreEqual("An error message", result.ErrorMessage);
        }

        [Test]
        public void TestErrorMessageNotString()
        {
            var document = new BsonDocument("errmsg", 3.14159);
            var result = new CommandResult(null, document);
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
            using (_database.RequestStart())
            {
                try
                {
                    var result = _database.RunCommand("invalid");
                }
                catch (Exception ex)
                {
                    // when connected to mongod a MongoCommandException is thrown
                    // but when connected to mongos a MongoQueryException is thrown
                    // this should be considered a server bug that they don't report the error in the same way

                    var instance = _server.RequestConnection.ServerInstance;
                    if (instance.InstanceType == MongoServerInstanceType.ShardRouter)
                    {
                        Assert.IsInstanceOf<MongoQueryException>(ex);
                        Assert.IsTrue(ex.Message.StartsWith("QueryFailure flag was unrecognized command: ", StringComparison.Ordinal));
                    }
                    else
                    {
                        Assert.IsInstanceOf<MongoCommandException>(ex);
                        Assert.IsTrue(ex.Message.StartsWith("Command 'invalid' failed: no such cmd", StringComparison.Ordinal));
                    }
                }
            }
        }
    }
}
