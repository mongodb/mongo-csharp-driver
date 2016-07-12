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

using System.IO;
using System.Net;
#if NET45
using System.Runtime.Serialization.Formatters.Binary;
#endif
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.EqualityComparers;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using Xunit;

namespace MongoDB.Driver
{
    public class MongoWriteConcernExceptionTests
    {
        private readonly ConnectionId _connectionId = new ConnectionId(new ServerId(new ClusterId(1), new DnsEndPoint("localhost", 27017)), 2).WithServerValue(3);
        private readonly string _message = "message";
        private readonly WriteConcernResult _writeConcernResult = new WriteConcernResult(new BsonDocument("result", 1));

        [Fact]
        public void constructor_should_initialize_subject()
        {
            var subject = new MongoWriteConcernException(_connectionId, _message, _writeConcernResult);

            subject.Command.Should().BeNull();
            subject.ConnectionId.Should().BeSameAs(_connectionId);
            subject.InnerException.Should().BeNull();
            subject.Message.Should().BeSameAs(_message);
            subject.Result.Should().Be(_writeConcernResult.Response);
            subject.WriteConcernResult.Should().Be(_writeConcernResult);
        }

#if NET45
        [Fact]
        public void Serialization_should_work()
        {
            var subject = new MongoWriteConcernException(_connectionId, _message, _writeConcernResult);

            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, subject);
                stream.Position = 0;
                var rehydrated = (MongoWriteConcernException)formatter.Deserialize(stream);

                rehydrated.Command.Should().BeNull();
                rehydrated.ConnectionId.Should().Be(subject.ConnectionId);
                rehydrated.InnerException.Should().BeNull();
                rehydrated.Message.Should().Be(subject.Message);
                rehydrated.Result.Should().Be(subject.Result);
                rehydrated.WriteConcernResult.Should().BeUsing(subject.WriteConcernResult, EqualityComparerRegistry.Default);
            }
        }
#endif
    }
}
