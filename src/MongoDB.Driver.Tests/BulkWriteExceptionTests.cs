/* Copyright 2010-2014 MongoDB Inc.
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Operations;
using NUnit.Framework;

namespace MongoDB.Driver.Tests
{
    [TestFixture]
    public class BulkWriteExceptionTests
    {
        [Test]
        public void Should_convert_from_core_exception_with_a_write_concern_error_when_original_models_exists()
        {
            var exception = new BulkWriteOperationException(
                result:new BulkWriteOperationResult.Acknowledged(
                    requestCount: 1,
                    matchedCount: 1,
                    deletedCount: 0,
                    insertedCount: 0,
                    modifiedCount: 0,
                    processedRequests: new[] { new InsertRequest(new BsonDocument("b", 1)) { CorrelationId = 1 } },
                    upserts: new List<BulkWriteOperationUpsert>()),
                writeErrors:new [] { new BulkWriteOperationError(10, 1, "blah", new BsonDocument("a", 1)) },
                writeConcernError: new BulkWriteConcernError(11, "funny", new BsonDocument("c", 1)),
                unprocessedRequests: new [] { new InsertRequest(new BsonDocument("a", 1)) { CorrelationId = 0 } });

            var models = new [] 
            {
                new InsertOneModel<BsonDocument>(new BsonDocument("a", 1)),
                new InsertOneModel<BsonDocument>(new BsonDocument("b", 1))
            }; 
            var mapped = BulkWriteException<BsonDocument>.FromCore(exception, models);

            mapped.Result.ProcessedRequests.Count.Should().Be(1);
            mapped.Result.ProcessedRequests[0].Should().BeSameAs(models[1]);
            mapped.WriteConcernError.Should().NotBeNull();
            mapped.WriteErrors.Count.Should().Be(1);
            mapped.WriteErrors[0].Should().NotBeNull();
            mapped.UnprocessedRequests.Count.Should().Be(1);
            mapped.UnprocessedRequests[0].Should().BeSameAs(models[0]);
        }

        [Test]
        public void Should_convert_from_core_exception_with_a_write_concern_error_when_original_models_do_not_exist()
        {
            var exception = new BulkWriteOperationException(
                result: new BulkWriteOperationResult.Acknowledged(
                    requestCount: 1,
                    matchedCount: 1,
                    deletedCount: 0,
                    insertedCount: 0,
                    modifiedCount: 0,
                    processedRequests: new[] { new InsertRequest(new BsonDocumentWrapper(new BsonDocument("b", 1))) { CorrelationId = 1 } },
                    upserts: new List<BulkWriteOperationUpsert>()),
                writeErrors: new[] { new BulkWriteOperationError(10, 1, "blah", new BsonDocument("a", 1)) },
                writeConcernError: new BulkWriteConcernError(11, "funny", new BsonDocument("c", 1)),
                unprocessedRequests: new[] { new InsertRequest(new BsonDocumentWrapper(new BsonDocument("a", 1))) { CorrelationId = 0 } });

            var mapped = BulkWriteException<BsonDocument>.FromCore(exception);

            mapped.Result.ProcessedRequests.Count.Should().Be(1);
            mapped.Result.ProcessedRequests[0].Should().BeOfType<InsertOneModel<BsonDocument>>();
            ((InsertOneModel<BsonDocument>)mapped.Result.ProcessedRequests[0]).Document.Should().Be("{b:1}");
            mapped.WriteConcernError.Should().NotBeNull();
            mapped.WriteErrors.Count.Should().Be(1);
            mapped.WriteErrors[0].Should().NotBeNull();
            mapped.UnprocessedRequests.Count.Should().Be(1);
            mapped.UnprocessedRequests[0].Should().BeOfType<InsertOneModel<BsonDocument>>();
            ((InsertOneModel<BsonDocument>)mapped.UnprocessedRequests[0]).Document.Should().Be("{a:1}");
        }
    }
}
