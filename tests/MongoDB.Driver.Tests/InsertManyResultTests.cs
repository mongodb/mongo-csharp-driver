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
using System.Collections.Generic;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Driver.Tests;

public class InsertManyResultTests
{
    private class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [Fact]
    public void Acknowledged_should_expose_the_inserted_ids()
    {
        var insertedIds = new Dictionary<int, object> { { 0, 1 }, { 1, 2 } };

        var result = new InsertManyResult.Acknowledged(insertedIds);

        result.IsAcknowledged.Should().BeTrue();
        result.InsertedIds.Should().Equal(insertedIds);
    }

    [Fact]
    public void FromBulkWriteResult_should_map_index_to_id_from_processed_requests()
    {
        var documents = new[]
        {
            new BsonDocument("_id", 10),
            new BsonDocument("_id", 20)
        };
        var bulkWriteResult = new BulkWriteResult<BsonDocument>.Acknowledged(
            requestCount: 2,
            matchedCount: 0,
            deletedCount: 0,
            insertedCount: 2,
            modifiedCount: 0,
            processedRequests: new[]
            {
                new InsertOneModel<BsonDocument>(documents[0]),
                new InsertOneModel<BsonDocument>(documents[1])
            },
            upserts: Array.Empty<BulkWriteUpsert>());

        var result = InsertManyResult.FromBulkWriteResult(bulkWriteResult, BsonDocumentSerializer.Instance);

        result.IsAcknowledged.Should().BeTrue();
        result.InsertedIds.Should().Equal(new Dictionary<int, object>
        {
            { 0, (BsonValue)10 },
            { 1, (BsonValue)20 }
        });
    }

    [Fact]
    public void FromBulkWriteResult_should_return_the_ids_for_POCOs()
    {
        var serializer = BsonSerializer.LookupSerializer<Person>();
        var people = new[]
        {
            new Person { Id = 5, Name = "Jo" },
            new Person { Id = 6, Name = "Al" }
        };
        var bulkWriteResult = new BulkWriteResult<Person>.Acknowledged(
            requestCount: 2,
            matchedCount: 0,
            deletedCount: 0,
            insertedCount: 2,
            modifiedCount: 0,
            processedRequests: new[]
            {
                new InsertOneModel<Person>(people[0]),
                new InsertOneModel<Person>(people[1])
            },
            upserts: Array.Empty<BulkWriteUpsert>());

        var result = InsertManyResult.FromBulkWriteResult(bulkWriteResult, serializer);

        result.InsertedIds.Should().Equal(new Dictionary<int, object> { { 0, 5 }, { 1, 6 } });
    }

    [Fact]
    public void FromBulkWriteResult_should_return_unacknowledged_when_bulk_result_is_unacknowledged()
    {
        var bulkWriteResult = new BulkWriteResult<BsonDocument>.Unacknowledged(
            requestCount: 1,
            processedRequests: new[] { new InsertOneModel<BsonDocument>(new BsonDocument("_id", 1)) });

        var result = InsertManyResult.FromBulkWriteResult(bulkWriteResult, BsonDocumentSerializer.Instance);

        result.IsAcknowledged.Should().BeFalse();
    }

    [Fact]
    public void Unacknowledged_InsertedIds_should_throw()
    {
        var result = InsertManyResult.Unacknowledged.Instance;

        var exception = Record.Exception(() => { _ = result.InsertedIds; });

        exception.Should().BeOfType<NotSupportedException>();
    }

    [Fact]
    public void Unacknowledged_should_report_not_acknowledged()
    {
        var result = InsertManyResult.Unacknowledged.Instance;

        result.IsAcknowledged.Should().BeFalse();
    }
}
