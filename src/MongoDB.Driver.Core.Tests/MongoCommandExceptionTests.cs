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
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using NUnit.Framework;

namespace MongoDB.Driver
{
    [TestFixture]
    public class MongoCommandExceptionTests
    {
        private readonly ConnectionId _connectionId = new ConnectionId(new ServerId(new ClusterId(0), new DnsEndPoint("localhost", 27017)), 0);

        [Test]
        public void Code_get_returns_expected_result()
        {
            var command = new BsonDocument("command", 1);
            var code = 123;
            var commandResult = new BsonDocument { { "code", code }, { "ok", 0 } };
            var subject = new MongoCommandException(_connectionId, "message", command, commandResult);

            var result = subject.Code;

            result.Should().Be(code);
        }

        [Test]
        public void Command_get_returns_expected_result()
        {
            var command = new BsonDocument("command", 1);
            var commandResult = new BsonDocument { { "ok", 0 } };
            var subject = new MongoCommandException(_connectionId, "message", command, commandResult);

            var result = subject.Command;

            result.Should().BeSameAs(command);
        }

        [Test]
        public void ConnectionId_get_returns_expected_result()
        {
            var command = new BsonDocument("command", 1);
            var commandResult = new BsonDocument { { "ok", 0 } };
            var subject = new MongoCommandException(_connectionId, "message", command, commandResult);

            var result = subject.ConnectionId;

            result.Should().BeSameAs(_connectionId);
        }

        [Test]
        public void constructor_with_message_command_should_initialize_subject()
        {
            var message = "message";
            var command = new BsonDocument("command", 1);

            var subject = new MongoCommandException(_connectionId, message, command);

            subject.ConnectionId.Should().BeSameAs(_connectionId);
            subject.Message.Should().BeSameAs(message);
            subject.Command.Should().BeSameAs(command);
            subject.Result.Should().BeNull();
            subject.InnerException.Should().BeNull();
        }

        [Test]
        public void constructor_with_message_command_result_should_initialize_subject()
        {
            var message = "message";
            var command = new BsonDocument("command", 1);
            var commandResult = new BsonDocument("ok", 1);

            var subject = new MongoCommandException(_connectionId, message, command, commandResult);

            subject.ConnectionId.Should().BeSameAs(_connectionId);
            subject.Message.Should().BeSameAs(message);
            subject.Command.Should().BeSameAs(command);
            subject.Result.Should().BeSameAs(commandResult);
            subject.InnerException.Should().BeNull();
        }

        [Test]
        public void constructor_with_message_command_result_innerException_should_initialize_subject()
        {
            var message = "message";
            var command = new BsonDocument("command", 1);
            var commandResult = new BsonDocument("ok", 1);
            var innerException = new Exception();

            var subject = new MongoCommandException(_connectionId, message, command, commandResult, innerException);

            subject.ConnectionId.Should().BeSameAs(_connectionId);
            subject.Message.Should().BeSameAs(message);
            subject.Command.Should().BeSameAs(command);
            subject.Result.Should().BeSameAs(commandResult);
            subject.InnerException.Should().BeSameAs(innerException);
        }

        [Test]
        public void constructor_with_info_context_should_initialize_subject()
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                var command = new BsonDocument("command", 1);
                var commandResult = new BsonDocument("result", 2);
                var innerException = new Exception("inner");
                var exception = new MongoCommandException(_connectionId, "message", command, commandResult, innerException);
                formatter.Serialize(stream, exception);
                stream.Position = 0;

                var subject = (MongoCommandException)formatter.Deserialize(stream);

                subject.ConnectionId.Should().BeNull(); // ConnectionId is not serializable
                subject.Message.Should().Be("message");
                subject.InnerException.Message.Should().Be("inner");
                subject.Command.Should().Be(command);
                subject.Result.Should().Be(commandResult);
            }
        }

        [Test]
        public void ErrorMessage_get_returns_expected_result()
        {
            var command = new BsonDocument("command", 1);
            var errorMessage = "errorMessage";
            var commandResult = new BsonDocument { { "errmsg", errorMessage }, { "ok", 0 } };
            var subject = new MongoCommandException(_connectionId, "message", command, commandResult);

            var result = subject.ErrorMessage;

            result.Should().Be(errorMessage);
        }

        [Test]
        public void GetObjectData_should_add_serialized_representation_to_info()
        {
            // implicitly tested by constructor_with_info_context_should_initialize_subject
            Assert.Pass();
        }

        [Test]
        public void Result_get_should_return_expected_result()
        {
            var command = new BsonDocument("command", 1);
            var commandResult = new BsonDocument { { "ok", 0 } };
            var subject = new MongoCommandException(_connectionId, "message", command, commandResult);

            var result = subject.Result;

            result.Should().BeSameAs(commandResult);
        }
    }
}
