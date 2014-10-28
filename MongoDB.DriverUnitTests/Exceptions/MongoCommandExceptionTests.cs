/* Copyright 2010-2014 MongoDB Inc.
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

namespace MongoDB.DriverUnitTests.Exceptions
{
    [TestFixture]
    public class MongoCommandExceptionTests
    {
        [Test]
        public void CommandResult_get_should_return_expected_result()
        {
            var response = new BsonDocument("ok", 1);
            var commandResult = new CommandResult(response);
            commandResult.Command = new CommandDocument("commandName", 1);
            var subject = new MongoCommandException(commandResult);

#pragma warning disable 618
            var result = subject.CommandResult;
#pragma warning restore

            Assert.That(result, Is.SameAs(commandResult));
        }

        [Test]
        public void constructor_with_commandResult_should_initialize_subject()
        {
            var response = new BsonDocument("ok", 1);
            var commandResult = new CommandResult(response);
            commandResult.Command = new CommandDocument("commandName", 1);

            var subject = new MongoCommandException(commandResult);

#pragma warning disable 618
            Assert.That(subject.CommandResult, Is.SameAs(commandResult));
#pragma warning restore
        }

        [Test]
        public void constructor_with_message_and_commandResult_should_initialize_subject()
        {
            var message = "abc";
            var response = new BsonDocument("ok", 1);
            var commandResult = new CommandResult(response);

            var subject = new MongoCommandException(message, commandResult);

#pragma warning disable 618
            Assert.That(subject.Message, Is.SameAs(message));
            Assert.That(subject.CommandResult, Is.SameAs(commandResult));
#pragma warning restore
        }

        [Test]
        public void constructor_with_message_and_innerException_should_initialize_subject()
        {
            var message = "abc";
            var innerException = new Exception();

            var subject = new MongoCommandException(message, innerException);

            Assert.That(subject.Message, Is.SameAs(message));
            Assert.That(subject.InnerException, Is.SameAs(innerException));
        }

        [Test]
        public void constructor_with_message_should_initialize_subject()
        {
            var message = "abc";

            var subject = new MongoCommandException(message);

            Assert.That(subject.Message, Is.SameAs(message));
        }

        [Test]
        public void Result_get_should_return_expected_result()
        {
            var response = new BsonDocument("ok", 1);
            var commandResult = new CommandResult(response);
            commandResult.Command = new CommandDocument("commandName", 1);
            var subject = new MongoCommandException(commandResult);

            var result = subject.Result;

            Assert.That(result, Is.SameAs(response));
        }
    }
}