/* Copyright 2013-present MongoDB Inc.
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
    public class MongoNodeIsRecoveringExceptionTests
    {
        private readonly ConnectionId _connectionId = new ConnectionId(new ServerId(new ClusterId(1), new DnsEndPoint("localhost", 27017)), 2).WithServerValue(3);
        private readonly BsonDocument _command = new BsonDocument("command", 1);
        private readonly BsonDocument _serverResult = new BsonDocument("result", 1);

        [Fact]
        public void constructor_should_initialize_subject()
        {
            var result = new MongoNodeIsRecoveringException(_connectionId, _command, _serverResult);

            result.ConnectionId.Should().BeSameAs(_connectionId);
            result.InnerException.Should().BeNull();
            result.Message.Should().Be("Server returned node is recovering error (code = -1).");
            result.Command.Should().BeSameAs(_command);
            result.Result.Should().BeSameAs(_serverResult);
        }

        [Fact]
        public void constructor_should_initialize_subject_when_result_contains_code()
        {
            var serverResult = BsonDocument.Parse("{ ok : 0, code : 1234 }");

            var result = new MongoNodeIsRecoveringException(_connectionId, _command, serverResult);

            result.ConnectionId.Should().BeSameAs(_connectionId);
            result.InnerException.Should().BeNull();
            result.Message.Should().Be("Server returned node is recovering error (code = 1234).");
            result.Command.Should().BeSameAs(_command);
            result.Result.Should().BeSameAs(serverResult);
        }

        [Fact]
        public void constructor_should_initialize_subject_when_result_contains_code_and_codeName()
        {
            var serverResult = BsonDocument.Parse("{ ok : 0, code : 1234, codeName : 'some name' }");

            var result = new MongoNodeIsRecoveringException(_connectionId, _command, serverResult);

            result.ConnectionId.Should().BeSameAs(_connectionId);
            result.InnerException.Should().BeNull();
            result.Message.Should().Be("Server returned node is recovering error (code = 1234, codeName = \"some name\").");
            result.Command.Should().BeSameAs(_command);
            result.Result.Should().BeSameAs(serverResult);
        }

#if NET452
        [Fact]
        public void Serialization_should_work()
        {
            var subject = new MongoNodeIsRecoveringException(_connectionId, _command, _serverResult);

            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, subject);
                stream.Position = 0;
                var rehydrated = (MongoNodeIsRecoveringException)formatter.Deserialize(stream);

                rehydrated.ConnectionId.Should().Be(subject.ConnectionId);
                rehydrated.InnerException.Should().BeNull();
                rehydrated.Message.Should().Be(subject.Message);
                rehydrated.Command.Should().Be(subject.Command);
                rehydrated.Result.Should().Be(subject.Result);
            }
        }
#endif
    }
}
