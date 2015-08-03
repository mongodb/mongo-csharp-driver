/* Copyright 2013-2015 MongoDB Inc.
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
using System.Runtime.Serialization.Formatters.Binary;
using FluentAssertions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.GridFS;
using NUnit.Framework;

namespace MongoDB.Driver.Tests.GridFS
{
    [TestFixture]
    public class MongoGridFSExceptionTests
    {
        private readonly ConnectionId _connectionId = new ConnectionId(new ServerId(new ClusterId(1), new DnsEndPoint("localhost", 27017)), 2).WithServerValue(3);
        private Exception _innerException = new Exception("inner");
        private string _message = "message";

        [Test]
        public void constructor_should_initialize_subject()
        {
            var subject = new MongoGridFSException(_connectionId, _message);

            subject.ConnectionId.Should().BeSameAs(_connectionId);
            subject.InnerException.Should().BeNull();
            subject.Message.Should().BeSameAs(_message);
        }

        [Test]
        public void constructor_with_innerException_should_initialize_subject()
        {
            var subject = new MongoGridFSException(_connectionId, _message, _innerException);

            subject.ConnectionId.Should().BeSameAs(_connectionId);
            subject.InnerException.Should().BeSameAs(_innerException);
            subject.Message.Should().BeSameAs(_message);
        }

        [Test]
        public void Serialization_should_work()
        {
            var subject = new MongoGridFSException(_connectionId, _message, _innerException);

            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, subject);
                stream.Position = 0;
                var rehydrated = (MongoGridFSException)formatter.Deserialize(stream);

                rehydrated.ConnectionId.Should().Be(subject.ConnectionId);
                rehydrated.Message.Should().Be(subject.Message);
                rehydrated.InnerException.Message.Should().Be(subject.InnerException.Message); // Exception does not override Equals
            }
        }
    }
}
