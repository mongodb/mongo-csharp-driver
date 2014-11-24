/* Copyright 2013-2014 MongoDB Inc.
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
using MongoDB.Driver.Core.Clusters;
using FluentAssertions;
using NUnit.Framework;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using MongoDB.Bson;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using System.Net;

namespace MongoDB.Driver
{
    [TestFixture]
    public class WriteExceptionTests
    {
        private readonly ConnectionId _connectionId = new ConnectionId(new ServerId(new ClusterId(0), new DnsEndPoint("localhost", 27017)), 0);

        [Test]
        public void Constructor_with_1_arguments_should_work()
        {
            var exception = new WriteProtocolException(_connectionId, "message");
            exception.ConnectionId.Should().BeSameAs(_connectionId);
            exception.Message.Should().Be("message");
            exception.InnerException.Should().BeNull();
            exception.Result.Should().BeNull();
        }

        [Test]
        public void Constructor_with_2_arguments_should_work()
        {
            var result = new BsonDocument("result", 1);
            var exception = new WriteProtocolException(_connectionId, "message", result);
            exception.ConnectionId.Should().BeSameAs(_connectionId);
            exception.Message.Should().Be("message");
            exception.InnerException.Should().BeNull();
            exception.Result.Equals(result).Should().BeTrue();
        }

        [Test]
        public void Serialization_should_work()
        {
            var result = new BsonDocument("result", 1);
            var exception = new WriteProtocolException(_connectionId, "message", result);

            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, exception);
                stream.Position = 0;
                var rehydrated = (WriteProtocolException)formatter.Deserialize(stream);
                rehydrated.ConnectionId.Should().BeNull(); // ConnectionId is not serializable
                rehydrated.Message.Should().Be("message");
                rehydrated.InnerException.Should().BeNull();
                rehydrated.Result.Equals(result).Should().BeTrue();
            }
        }
    }
}
