/* Copyright 2010-2016 MongoDB Inc.
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
using Xunit;

namespace MongoDB.Driver.Tests.CommandResults
{
    public class CommandResultTests
    {
        private MongoDatabase _database;

        public CommandResultTests()
        {
            _database = LegacyTestConfiguration.Database;
        }

        [Fact]
        public void TestCodeMissing()
        {
            var document = new BsonDocument();
            var result = new CommandResult(document);

            Assert.False(result.Code.HasValue);
        }

        [Fact]
        public void TestCode()
        {
            var document = new BsonDocument("code", 18);
            var result = new CommandResult(document);

            Assert.True(result.Code.HasValue);
            Assert.Equal(18, result.Code);
        }

        [Fact]
        public void TestOkMissing()
        {
            var document = new BsonDocument();
            var result = new CommandResult(document);
            Assert.False(result.Ok);
        }

        [Fact]
        public void TestOkFalse()
        {
            var document = new BsonDocument("ok", false);
            var result = new CommandResult(document);
            Assert.False(result.Ok);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public void TestOkTrue()
        {
            var document = new BsonDocument("ok", true);
            var result = new CommandResult(document);
            Assert.True(result.Ok);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void TestOkZero()
        {
            var document = new BsonDocument("ok", 0);
            var result = new CommandResult(document);
            Assert.False(result.Ok);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public void TestOkZeroPointZero()
        {
            var document = new BsonDocument("ok", 0.0);
            var result = new CommandResult(document);
            Assert.False(result.Ok);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public void TestOkOne()
        {
            var document = new BsonDocument("ok", 1);
            var result = new CommandResult(document);
            Assert.True(result.Ok);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void TestOkOnePointZero()
        {
            var document = new BsonDocument("ok", 1.0);
            var result = new CommandResult(document);
            Assert.True(result.Ok);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void TestErrorMessageMissing()
        {
            var document = new BsonDocument();
            var result = new CommandResult(document);
            Assert.Equal("Unknown error", result.ErrorMessage);
        }

        [Fact]
        public void TestErrorMessagePresent()
        {
            var document = new BsonDocument("errmsg", "An error message");
            var result = new CommandResult(document);
            Assert.Equal("An error message", result.ErrorMessage);
        }

        [Fact]
        public void TestErrorMessageNotString()
        {
            var document = new BsonDocument("errmsg", 123);
            var result = new CommandResult(document);
            Assert.Equal("123", result.ErrorMessage);
        }

        [Fact]
        public void TestIsMasterCommand()
        {
            var result = _database.RunCommand("ismaster");
            Assert.True(result.Ok);
        }

        [Fact]
        public void TestInvalidCommand()
        {
            Assert.Throws<MongoCommandException>(() => _database.RunCommand("invalid"));
        }
    }
}
