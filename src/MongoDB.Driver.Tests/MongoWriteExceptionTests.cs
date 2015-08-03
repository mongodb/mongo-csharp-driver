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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.EqualityComparers;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using NUnit.Framework;

namespace MongoDB.Driver.Tests
{
    [TestFixture]
    public class MongoWriteExceptionTests
    {
        private ConnectionId _connectionId;
        private Exception _innerException;
        private WriteConcernError _writeConcernError;
        private WriteError _writeError;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            _connectionId = new ConnectionId(new ServerId(new ClusterId(1), new DnsEndPoint("localhost", 27017)), 2);
            _innerException = new Exception("inner");
            _writeConcernError = new WriteConcernError(1, "writeConcernError", new BsonDocument("details", "writeConcernError"));
            _writeError = new WriteError(ServerErrorCategory.Uncategorized, 1, "writeError", new BsonDocument("details", "writeError"));
        }

        [Test]
        public void constructor_should_initialize_subject()
        {
            var subject = new MongoWriteException(_connectionId, _writeError, _writeConcernError, _innerException);

            subject.ConnectionId.Should().Be(_connectionId);
            subject.InnerException.Should().Be(_innerException);
            subject.Message.Should().Be("A write operation resulted in an error." + Environment.NewLine + "  writeError" + Environment.NewLine + "  writeConcernError");
            subject.WriteConcernError.Should().Be(_writeConcernError);
            subject.WriteError.Should().Be(_writeError);
        }

        [Test]
        public void FromBulkWriteException_should_return_expected_result()
        {
            var processedRequests = new[] { new InsertOneModel<BsonDocument>(new BsonDocument("_id", 1)) };
            var upserts = new List<BulkWriteUpsert>();
            var bulkWriteResult = new BulkWriteResult<BsonDocument>.Acknowledged(1, 1, 0, 0, 0, processedRequests, upserts);
            var writeErrors = new[] { new BulkWriteError(1, ServerErrorCategory.Uncategorized, 2, "message", new BsonDocument("details", 1)) };
            var writeConcernError = new WriteConcernError(1, "message", new BsonDocument("details", 1));
            var unprocessedRequests = new List<WriteModel<BsonDocument>>();
            var bulkWriteException = new MongoBulkWriteException<BsonDocument>(_connectionId, bulkWriteResult, writeErrors, writeConcernError, unprocessedRequests);

            var result = MongoWriteException.FromBulkWriteException(bulkWriteException);

            result.ConnectionId.Should().Be(_connectionId);
            result.InnerException.Should().BeSameAs(bulkWriteException);
            result.Message.Should().Be("A write operation resulted in an error." + Environment.NewLine + "  message" + Environment.NewLine + "  message");
            result.WriteConcernError.Should().Be(writeConcernError);
            result.WriteError.Should().Be(writeErrors[0]);
        }

        [Test]
        public void Serialization_should_work()
        {
            var subject = new MongoWriteException(_connectionId, _writeError, _writeConcernError, _innerException);

            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, subject);
                stream.Position = 0;
                var rehydrated = (MongoWriteException)formatter.Deserialize(stream);

                rehydrated.ConnectionId.Should().Be(subject.ConnectionId);
                rehydrated.InnerException.Message.Should().Be(subject.InnerException.Message); // Exception does not override Equals
                rehydrated.Message.Should().Be(subject.Message);
                rehydrated.WriteConcernError.Should().BeUsing(subject.WriteConcernError, EqualityComparerRegistry.Default);
                rehydrated.WriteError.Should().BeUsing(subject.WriteError, EqualityComparerRegistry.Default);
            }
        }
    }
}
