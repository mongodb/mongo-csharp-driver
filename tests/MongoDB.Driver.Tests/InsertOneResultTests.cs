/* Copyright 2010-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class InsertOneResultTests
    {
        private class Person
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [Fact]
        public void Acknowledged_should_expose_the_inserted_id()
        {
            var result = new InsertOneResult.Acknowledged(insertedId: 42);

            result.IsAcknowledged.Should().BeTrue();
            result.InsertedId.Should().Be(42);
        }

        [Fact]
        public void Unacknowledged_should_report_not_acknowledged()
        {
            var result = InsertOneResult.Unacknowledged.Instance;

            result.IsAcknowledged.Should().BeFalse();
        }

        [Fact]
        public void Unacknowledged_InsertedId_should_throw()
        {
            var result = InsertOneResult.Unacknowledged.Instance;

            var exception = Record.Exception(() => { _ = result.InsertedId; });

            exception.Should().BeOfType<NotSupportedException>();
        }

        [Fact]
        public void FromBulkWriteResult_should_return_acknowledged_with_id_from_processed_request()
        {
            var document = new BsonDocument("_id", 7).Add("a", 1);
            var bulkWriteResult = new BulkWriteResult<BsonDocument>.Acknowledged(
                requestCount: 1,
                matchedCount: 0,
                deletedCount: 0,
                insertedCount: 1,
                modifiedCount: 0,
                processedRequests: new[] { new InsertOneModel<BsonDocument>(document) },
                upserts: Array.Empty<BulkWriteUpsert>());

            var result = InsertOneResult.FromBulkWriteResult(bulkWriteResult, BsonDocumentSerializer.Instance);

            result.IsAcknowledged.Should().BeTrue();
            result.InsertedId.Should().Be((BsonValue)7);
        }

        [Fact]
        public void FromBulkWriteResult_should_return_the_id_for_POCO()
        {
            var serializer = BsonSerializer.LookupSerializer<Person>();
            var person = new Person { Id = 5, Name = "Jo" };
            var bulkWriteResult = new BulkWriteResult<Person>.Acknowledged(
                requestCount: 1,
                matchedCount: 0,
                deletedCount: 0,
                insertedCount: 1,
                modifiedCount: 0,
                processedRequests: new[] { new InsertOneModel<Person>(person) },
                upserts: Array.Empty<BulkWriteUpsert>());

            var result = InsertOneResult.FromBulkWriteResult(bulkWriteResult, serializer);

            result.InsertedId.Should().Be(5);
        }

        [Fact]
        public void FromBulkWriteResult_should_return_unacknowledged_when_bulk_result_is_unacknowledged()
        {
            var document = new BsonDocument("_id", 7);
            var bulkWriteResult = new BulkWriteResult<BsonDocument>.Unacknowledged(
                requestCount: 1,
                processedRequests: new[] { new InsertOneModel<BsonDocument>(document) });

            var result = InsertOneResult.FromBulkWriteResult(bulkWriteResult, BsonDocumentSerializer.Instance);

            result.IsAcknowledged.Should().BeFalse();
        }
    }
}
