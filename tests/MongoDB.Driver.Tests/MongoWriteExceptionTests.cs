/* Copyright 2013-present MongoDB Inc.
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
#if NET472
using System.Runtime.Serialization.Formatters.Binary;
#endif
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.EqualityComparers;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class MongoWriteExceptionTests
    {
        private static ConnectionId __connectionId;
        private static Exception __innerException;
        private static WriteConcernError __writeConcernError;
        private static WriteError __writeError;
        private static bool __oneTimeSetupHasRun = false;
        private static object __oneTimeSetupLock = new object();

        public MongoWriteExceptionTests()
        {
            lock (__oneTimeSetupLock)
            {
                __oneTimeSetupHasRun = __oneTimeSetupHasRun || OneTimeSetup();
            }
        }

        public bool OneTimeSetup()
        {
            __connectionId = new ConnectionId(new ServerId(new ClusterId(1), new DnsEndPoint("localhost", 27017)), 2);
            __innerException = new Exception("inner");
            __writeConcernError = new WriteConcernError(1, null, "writeConcernError", new BsonDocument("details", "writeConcernError"), new[] { "RetryableWriteError" });
            __writeError = new WriteError(ServerErrorCategory.Uncategorized, 1, "writeError", new BsonDocument("details", "writeError"));
            return true;
        }

        [Fact]
        public void constructor_should_initialize_subject()
        {
            var subject = new MongoWriteException(__connectionId, __writeError, __writeConcernError, __innerException);

            subject.ConnectionId.Should().Be(__connectionId);
            subject.ErrorLabels.Should().BeEquivalentTo(__writeConcernError.ErrorLabels);
            subject.InnerException.Should().Be(__innerException);
            subject.Message.Should().Be("A write operation resulted in an error. WriteError: { Category : \"Uncategorized\", Code : 1, Message : \"writeError\", Details : \"{ \"details\" : \"writeError\" }\" }. WriteConcernError: { Code : \"1\", Message : \"writeConcernError\", Details : \"{ \"details\" : \"writeConcernError\" }\", ErrorLabels : [ \"RetryableWriteError\" ] }.");
            subject.WriteConcernError.Should().Be(__writeConcernError);
            subject.WriteError.Should().Be(__writeError);
        }

        [Fact]
        public void FromBulkWriteException_should_return_expected_result()
        {
            var processedRequests = new[] { new InsertOneModel<BsonDocument>(new BsonDocument("_id", 1)) };
            var upserts = new List<BulkWriteUpsert>();
            var bulkWriteResult = new BulkWriteResult<BsonDocument>.Acknowledged(1, 1, 0, 0, 0, processedRequests, upserts);
            var writeErrors = new[] { new BulkWriteError(1, ServerErrorCategory.Uncategorized, 2, "message", new BsonDocument("details", 1)) };
            var writeConcernError = new WriteConcernError(1, null, "message", new BsonDocument("details", 1), new[] { "RetryableWriteError" });
            var unprocessedRequests = new List<WriteModel<BsonDocument>>();
            var bulkWriteException = new MongoBulkWriteException<BsonDocument>(__connectionId, bulkWriteResult, writeErrors, writeConcernError, unprocessedRequests);

            var result = MongoWriteException.FromBulkWriteException(bulkWriteException);

            result.ConnectionId.Should().Be(__connectionId);
            result.ErrorLabels.Should().BeEquivalentTo(writeConcernError.ErrorLabels);
            result.InnerException.Should().BeSameAs(bulkWriteException);
            result.Message.Should().Be("A write operation resulted in an error. WriteError: { Category : \"Uncategorized\", Code : 2, Message : \"message\", Details : \"{ \"details\" : 1 }\" }. WriteConcernError: { Code : \"1\", Message : \"message\", Details : \"{ \"details\" : 1 }\", ErrorLabels : [ \"RetryableWriteError\" ] }.");
            result.WriteConcernError.Should().Be(writeConcernError);
            result.WriteError.Should().Be(writeErrors[0]);
        }
    }
}
