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
using Xunit;

namespace MongoDB.Driver
{
    public class MongoWaitQueueFullExceptionTests
    {
        [Fact]
        public void constructor_should_initalize_subject()
        {
            var subject = new MongoWaitQueueFullException("message");

            subject.Message.Should().Be("message");
            subject.InnerException.Should().BeNull();
        }

        [Fact]
        public void ForConnectionPool_should_create_expected_message()
        {
            var endPoint = new DnsEndPoint("localhost", 27017);
            var subject = MongoWaitQueueFullException.ForConnectionPool(endPoint);

            subject.Message.Should().Be("The wait queue for acquiring a connection to server localhost:27017 is full.");
        }

        [Fact]
        public void ForServerSelection_should_create_expected_message()
        {
            var subject = MongoWaitQueueFullException.ForServerSelection();

            subject.Message.Should().Be("The wait queue for server selection is full.");
        }

#if NET45
        [Fact]
        public void Serialization_should_work()
        {
            var subject = new MongoWaitQueueFullException("message");

            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, subject);
                stream.Position = 0;
                var rehydrated = (MongoWaitQueueFullException)formatter.Deserialize(stream);

                rehydrated.Message.Should().Be(subject.Message);
                rehydrated.InnerException.Should().BeNull();
            }
        }
#endif
    }
}
