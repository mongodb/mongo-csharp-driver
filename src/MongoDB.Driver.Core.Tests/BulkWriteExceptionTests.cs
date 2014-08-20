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
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver.Core.Exceptions
{
    [TestFixture]
    public class BulkWriteExceptionTests
    {
        [Test]
        public void Constructor_should_work()
        {
            var processedRequests = new WriteRequest[0];
            var upserts = new BulkWriteUpsert[0];
            var result = new AcknowledgedBulkWriteResult(1, 2, 3, 4, 5, processedRequests, upserts);
            var writeErrors = new BulkWriteError[0];
            var writeConcernError = new WriteConcernError(1, "message", new BsonDocument("x", 1));
            var unprocessedRequests = new WriteRequest[0];
            var subject = new BulkWriteException(result, writeErrors, writeConcernError, unprocessedRequests);

            subject.Result.Should().BeSameAs(result);
            subject.UnprocessedRequests.Should().BeSameAs(unprocessedRequests);
            subject.WriteConcernError.Should().BeSameAs(writeConcernError);
            subject.WriteErrors.Should().BeSameAs(writeErrors);
        }

        [Test]
        public void Serialization_should_drop_custom_fields()
        {
            var processedRequests = new WriteRequest[0];
            var upserts = new BulkWriteUpsert[0];
            var result = new AcknowledgedBulkWriteResult(1, 2, 3, 4, 5, processedRequests, upserts);
            var writeErrors = new BulkWriteError[0];
            var writeConcernError = new WriteConcernError(1, "message", new BsonDocument("x", 1));
            var unprocessedRequests = new WriteRequest[0];
            var subject = new BulkWriteException(result, writeErrors, writeConcernError, unprocessedRequests);

            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, subject);
                stream.Position = 0;
                var rehydrated = (BulkWriteException)formatter.Deserialize(stream);

                rehydrated.Result.Should().BeNull();
                rehydrated.UnprocessedRequests.Should().BeNull();
                rehydrated.WriteConcernError.Should().BeNull();
                rehydrated.WriteErrors.Should().BeNull();
            }
        }
    }
}
