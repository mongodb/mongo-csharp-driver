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
using MongoDB.Driver.Core.Exceptions;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace MongoDB.Driver.Core.Tests.Exceptions
{
    [TestFixture]
    public class MongoDBExceptionTests
    {
        [Test]
        public void Constructor_should_work()
        {
            var innerException = new Exception("inner");
            var exception = new MongoDBException("message", innerException);
            exception.Message.Should().Be("message");
            exception.InnerException.Message.Should().Be("inner");
        }

        [Test]
        public void Serialization_should_work()
        {
            var innerException = new Exception("inner");
            var exception = new MongoDBException("message", innerException);
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, exception);
                stream.Position = 0;
                var rehydrated = (MongoDBException)formatter.Deserialize(stream);
                rehydrated.Message.Should().Be("message");
                rehydrated.InnerException.Message.Should().Be("inner");
            }
        }
    }
}
