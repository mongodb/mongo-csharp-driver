/* Copyright 2010-present MongoDB Inc.
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
#if NET452
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
#endif
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using Xunit;

namespace MongoDB.Driver
{
    public class MongoCommandExceptionTests
    {
        private readonly BsonDocument _command = new BsonDocument("command", 1);
        private readonly BsonDocument _commandResult = new BsonDocument { { "code", 123 }, { "codeName", "abc" }, { "errmsg", "error message" }, { "ok", 0 } };
        private readonly ConnectionId _connectionId = new ConnectionId(new ServerId(new ClusterId(1), new DnsEndPoint("localhost", 27017)), 2).WithServerValue(3);
        private readonly string _message = "message";

        [Fact]
        public void Code_get_returns_expected_result()
        {
            var subject = new MongoCommandException(_connectionId, _message, _command, _commandResult);

            var result = subject.Code;

            result.Should().Be(123);
        }

        [Fact]
        public void CodeName_get_returns_expected_result()
        {
            var subject = new MongoCommandException(_connectionId, _message, _command, _commandResult);

            var result = subject.CodeName;

            result.Should().Be("abc");
        }

        [Fact]
        public void constructor_with_message_command_should_initialize_subject()
        {
            var subject = new MongoCommandException(_connectionId, _message, _command);

            subject.Command.Should().BeSameAs(_command);
            subject.ConnectionId.Should().BeSameAs(_connectionId);
            subject.InnerException.Should().BeNull();
            subject.Message.Should().BeSameAs(_message);
            subject.Result.Should().BeNull();
        }

        [Fact]
        public void constructor_with_message_command_result_should_initialize_subject()
        {
            var subject = new MongoCommandException(_connectionId, _message, _command, _commandResult);

            subject.Command.Should().BeSameAs(_command);
            subject.ConnectionId.Should().BeSameAs(_connectionId);
            subject.InnerException.Should().BeNull();
            subject.Message.Should().BeSameAs(_message);
            subject.Result.Should().BeSameAs(_commandResult);
        }

        [Theory]
        [InlineData(null, new string[0])]
        [InlineData("{ }", new string[0])]
        [InlineData("{ }", new string[0])]
        [InlineData("{ errorLabels : \"not an array\" }", new string[0])]
        [InlineData("{ errorLabels : [ ] }", new string[0])]
        [InlineData("{ errorLabels : [ 1 ] }", new string[0])]
        [InlineData("{ errorLabels : [ null ] }", new string[0])]
        [InlineData("{ errorLabels : [ \"one\" ] }", new[] { "one" })]
        [InlineData("{ errorLabels : [ 1, \"one\" ] }", new[] { "one" })]
        [InlineData("{ errorLabels : [ \"one\", 1 ] }", new[] { "one" })]
        [InlineData("{ errorLabels : [ \"one\", null, \"two\" ] }", new[] { "one", "two" })]
        public void constructor_should_add_error_labels(string jsonResult, string[] expectedErrorLabels)
        {
            var result = jsonResult == null ? null : BsonDocument.Parse(jsonResult);

            var subject = new MongoCommandException(_connectionId, _message, _command, result);

            subject.ErrorLabels.Should().Equal(expectedErrorLabels);
        }

        [Fact]
        public void ErrorMessage_get_returns_expected_result()
        {
            var subject = new MongoCommandException(_connectionId, _message, _command, _commandResult);

            var result = subject.ErrorMessage;

            result.Should().Be("error message");
        }

#if NET452
        [Fact]
        public void Serialization_should_work()
        {
            var subject = new MongoCommandException(_connectionId, _message, _command, _commandResult);
            subject.AddErrorLabel("one");

            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, subject);
                stream.Position = 0;
                var rehydrated = (MongoCommandException)formatter.Deserialize(stream);

                rehydrated.ErrorLabels.Should().Equal(subject.ErrorLabels);
                rehydrated.ConnectionId.Should().Be(subject.ConnectionId);
                rehydrated.Message.Should().Be(_message);
                rehydrated.InnerException.Should().BeNull();
                rehydrated.Command.Should().Be(_command);
                rehydrated.Result.Should().Be(_commandResult);
            }
        }
#endif
    }
}
