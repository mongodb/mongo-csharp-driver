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
using System.Collections.Generic;
using MongoDB.Driver.Core.Helpers;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class MongoBulkWriteOperationExceptionTests
    {
        private ConnectionId _connectionId;
        private List<WriteRequest> _processedRequests;
        private BulkWriteOperationResult _result;
        private List<WriteRequest> _unprocessedRequests;
        private List<BulkWriteOperationUpsert> _upserts;
        private List<BulkWriteOperationError> _writeErrors;
        private BulkWriteConcernError _writeConcernError;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            _connectionId = new ConnectionId(new ServerId(new ClusterId(1), new DnsEndPoint("localhost", 27017)), 2).WithServerValue(3);
            _processedRequests = new List<WriteRequest>();
            _upserts = new List<BulkWriteOperationUpsert>();
            _result = new BulkWriteOperationResult.Acknowledged(1, 2, 3, 4, 5, _processedRequests, _upserts);
            _writeErrors = new List<BulkWriteOperationError>();
            _writeConcernError = new BulkWriteConcernError(1, "message", new BsonDocument("x", 1));
            _unprocessedRequests = new List<WriteRequest>();
        }

        [Test]
        public void constructor_should_initialize_subject()
        {
            var subject = new MongoBulkWriteOperationException(_connectionId, _result, _writeErrors, _writeConcernError, _unprocessedRequests);

            subject.ConnectionId.Should().BeSameAs(_connectionId);
            subject.Result.Should().BeSameAs(_result);
            subject.UnprocessedRequests.Should().BeSameAs(_unprocessedRequests);
            subject.WriteConcernError.Should().BeSameAs(_writeConcernError);
            subject.WriteErrors.Should().BeSameAs(_writeErrors);
        }

        [Test]
        public void Serialization_should_work()
        {
            var subject = new MongoBulkWriteOperationException(_connectionId, _result, _writeErrors, _writeConcernError, _unprocessedRequests);

            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, subject);
                stream.Position = 0;
                var rehydrated = (MongoBulkWriteOperationException)formatter.Deserialize(stream);

                rehydrated.ConnectionId.Should().Be(subject.ConnectionId);
                rehydrated.Message.Should().Be(subject.Message);
                rehydrated.Result.Should().Match<BulkWriteOperationResult>(x => new BulkWriteOperationResultEqualityComparer().Equals(x, subject.Result));
                rehydrated.UnprocessedRequests.Should().Equal(subject.UnprocessedRequests);
                rehydrated.WriteConcernError.Should().Match<BulkWriteConcernError>(x => new BulkWriteConcernErrorEqualityComparer().Equals(x, subject.WriteConcernError));
                rehydrated.WriteErrors.Should().Equal(subject.WriteErrors);
            }
        }
    }
}
