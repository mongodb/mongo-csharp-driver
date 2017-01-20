/* Copyright 2013-2016 MongoDB Inc.
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
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using Xunit;

namespace MongoDB.Driver
{
    public class MongoCursorNotFoundExceptionTests
    {
        private readonly ConnectionId _connectionId = new ConnectionId(new ServerId(new ClusterId(1), new DnsEndPoint("localhost", 27017)), 1).WithServerValue(2);
        private readonly long _cursorId = 1;
        private readonly BsonDocument _query = new BsonDocument("query", 1);

        [Fact]
        public void constructor_should_initalize_subject()
        {
            var subject = new MongoCursorNotFoundException(_connectionId, _cursorId, _query);

            subject.ConnectionId.Should().BeSameAs(_connectionId);
            subject.CursorId.Should().Be(_cursorId);
            subject.InnerException.Should().BeNull();
            subject.Message.Should().StartWith("Cursor 1 not found");
            subject.Message.Should().Contain("server localhost:27017");
            subject.Message.Should().Contain("connection 2");
            subject.Query.Should().BeSameAs(_query);
            subject.QueryResult.Should().BeNull();
        }

#if NET45
        [Fact]
        public void Serialization_should_work()
        {
            var subject = new MongoCursorNotFoundException(_connectionId, _cursorId, _query);

            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, subject);
                stream.Position = 0;
                var rehydrated = (MongoCursorNotFoundException)formatter.Deserialize(stream);

                rehydrated.ConnectionId.Should().Be(subject.ConnectionId);
                rehydrated.CursorId.Should().Be(subject.CursorId);
                rehydrated.InnerException.Should().BeNull();
                rehydrated.Message.Should().Be(subject.Message);
                rehydrated.Query.Should().Be(subject.Query);
                rehydrated.QueryResult.Should().Be(subject.QueryResult);
            }
        }
#endif
    }
}
