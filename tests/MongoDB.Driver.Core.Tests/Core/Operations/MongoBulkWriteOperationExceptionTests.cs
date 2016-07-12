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

using System.Collections.Generic;
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

namespace MongoDB.Driver.Core.Operations
{
    public class MongoBulkWriteOperationExceptionTests
    {
        private ConnectionId _connectionId;
        private List<WriteRequest> _processedRequests;
        private BulkWriteOperationResult _result;
        private List<WriteRequest> _unprocessedRequests;
        private List<BulkWriteOperationUpsert> _upserts;
        private List<BulkWriteOperationError> _writeErrors;
        private BulkWriteConcernError _writeConcernError;

        public MongoBulkWriteOperationExceptionTests()
        {
            _connectionId = new ConnectionId(new ServerId(new ClusterId(1), new DnsEndPoint("localhost", 27017)), 2).WithServerValue(3);
            _processedRequests = new List<WriteRequest>();
            _upserts = new List<BulkWriteOperationUpsert>();
            _result = new BulkWriteOperationResult.Acknowledged(1, 2, 3, 4, 5, _processedRequests, _upserts);
            _writeErrors = new List<BulkWriteOperationError>();
            _writeConcernError = new BulkWriteConcernError(1, "message", new BsonDocument("x", 1));
            _unprocessedRequests = new List<WriteRequest>();
        }

        [Fact]
        public void constructor_should_initialize_subject()
        {
            var subject = new MongoBulkWriteOperationException(_connectionId, _result, _writeErrors, _writeConcernError, _unprocessedRequests);

            subject.ConnectionId.Should().BeSameAs(_connectionId);
            subject.Result.Should().BeSameAs(_result);
            subject.UnprocessedRequests.Should().BeSameAs(_unprocessedRequests);
            subject.WriteConcernError.Should().BeSameAs(_writeConcernError);
            subject.WriteErrors.Should().BeSameAs(_writeErrors);
        }

#if NET45
        [Fact]
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
                rehydrated.Result.Should().BeUsing(subject.Result, EqualityComparerRegistry.Default);
                rehydrated.UnprocessedRequests.Should().EqualUsing(subject.UnprocessedRequests, EqualityComparerRegistry.Default);
                rehydrated.WriteConcernError.Should().BeUsing(subject.WriteConcernError, EqualityComparerRegistry.Default);
                rehydrated.WriteErrors.Should().EqualUsing(subject.WriteErrors, EqualityComparerRegistry.Default);
            }
        }
#endif
    }
}
