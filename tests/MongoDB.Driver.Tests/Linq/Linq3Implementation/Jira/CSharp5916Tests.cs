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
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp5916Tests : LinqIntegrationTest<CSharp5916Tests.ClassFixture>
    {
        public CSharp5916Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Pipeline_update_with_conditional_nested_object_assignment_should_work()
        {
            var collection = Fixture.Collection;
            var newHealth = new Health { Timestamp = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), Value = 90 };
            var filter = Builders<Device>.Filter.Empty;

            var pipeline = new EmptyPipelineDefinition<Device>()
                .Set(x => new Device
                {
                    Health = x.Health == null || x.Health.Timestamp < newHealth.Timestamp
                        ? newHealth
                        : x.Health
                });

            var update = Builders<Device>.Update.Pipeline(pipeline);

            var updateStages =
                update.Render(new(collection.DocumentSerializer, BsonSerializer.SerializerRegistry))
                    .AsBsonArray
                    .Cast<BsonDocument>();
            AssertStages(
                updateStages,
                "{ $set : { Health : { $cond : { if : { $or : [{ $eq : ['$Health', null] }, { $lt : ['$Health.Timestamp', { $date : '2025-01-01T00:00:00Z' }] }] }, then : { Timestamp : { $date : '2025-01-01T00:00:00Z' }, Value : 90 }, else : '$Health' } } } }");

            collection.UpdateMany(filter, update, new UpdateOptions { IsUpsert = true });

            var items = collection.AsQueryable().ToList();
            items.Select(i => i.Health.Value).Should().BeEquivalentTo(90, 90, 90, 75);
        }

        [BsonIgnoreExtraElements]
        public class Device
        {
            public int Id { get; set; }
            public Health Health { get; set; }
            public string Note { get; set; }
        }

        [BsonIgnoreExtraElements]
        public class Health
        {
            public DateTime Timestamp { get; set; }
            public int Value { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<Device, BsonDocument>
        {
            protected override IEnumerable<BsonDocument> InitialData =>
            [
                BsonDocument.Parse("{ _id : 1 }"),
                BsonDocument.Parse("{ _id : 2, Health : null }"),
                BsonDocument.Parse("{ _id : 3, Health : { Timestamp : ISODate('2024-01-01T00:00:00Z'), Value : 50 } }"),
                BsonDocument.Parse("{ _id : 4, Health : { Timestamp : ISODate('2026-01-01T00:00:00Z'), Value : 75 } }")
            ];
        }
    }
}
