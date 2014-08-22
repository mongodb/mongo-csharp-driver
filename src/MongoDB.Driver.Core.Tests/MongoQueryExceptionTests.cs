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

namespace MongoDB.Driver.Core.Exceptions
{
    [TestFixture]
    public class QueryExceptionTests
    {
        [Test]
        public void Constructor_with_2_arguments_should_work()
        {
            var query = new BsonDocument("query", 1);
            var exception = new MongoQueryException("message", query);
            exception.Message.Should().Be("message");
            exception.InnerException.Should().BeNull();
            exception.Query.Equals(query).Should().BeTrue();
            exception.QueryResult.Should().BeNull();
        }

        [Test]
        public void Constructor_with_3_arguments_should_work()
        {
            var query = new BsonDocument("query", 1);
            var result = new BsonDocument("result", 2);
            var exception = new MongoQueryException("message", query, result);
            exception.Message.Should().Be("message");
            exception.InnerException.Should().BeNull();
            exception.Query.Equals(query).Should().BeTrue();
            exception.QueryResult.Equals(result).Should().BeTrue();
        }

        [Test]
        public void Constructor_with_4_arguments_should_work()
        {
            var query = new BsonDocument("query", 1);
            var result = new BsonDocument("result", 2);
            var innerException = new Exception("inner");
            var exception = new MongoQueryException("message", query, result, innerException);
            exception.Message.Should().Be("message");
            exception.InnerException.Message.Should().Be("inner");
            exception.Query.Equals(query).Should().BeTrue();
            exception.QueryResult.Equals(result).Should().BeTrue();
        }

        [Test]
        public void Serialization_should_work()
        {
            var query = new BsonDocument("query", 1);
            var result = new BsonDocument("result", 2);
            var innerException = new Exception("inner");
            var exception = new MongoQueryException("message", query, result, innerException);

            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, exception);
                stream.Position = 0;
                var rehydrated = (MongoQueryException)formatter.Deserialize(stream);
                rehydrated.Message.Should().Be("message");
                rehydrated.InnerException.Message.Should().Be("inner");
                rehydrated.Query.Equals(query).Should().BeTrue();
                rehydrated.QueryResult.Equals(result).Should().BeTrue();
            }
        }
    }
}
