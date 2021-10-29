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
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using Xunit;

namespace MongoDB.Driver.Tests.Jira
{
    public class CSharp3397Tests
    {
        [Fact]
        public void Aggregate_should_use_read_preference()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("test");
            var collection = database.GetCollection<BsonDocument>("test").WithReadPreference(ReadPreference.SecondaryPreferred);

            database.DropCollection("test");
            collection.InsertMany(new[]
            {
                new BsonDocument("_id", 1),
                new BsonDocument("_id", 2)
            });

            var stages = new[]
            {
                "{ $count : 't' }"
            };
            var pipeline = new BsonDocumentStagePipelineDefinition<BsonDocument, BsonDocument>(stages.Select(s => BsonDocument.Parse(s)));
            var result = collection.Aggregate(pipeline).ToList();

            result.Should().HaveCount(1);
            result[0]["t"].AsInt32.Should().Be(2);
        }

        [Fact]
        public void Aggregate_with_out_should_use_read_preference()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("test");
            var collection = database.GetCollection<BsonDocument>("test").WithReadPreference(ReadPreference.SecondaryPreferred);

            database.DropCollection("test");
            collection.InsertMany(new[]
            {
                new BsonDocument("_id", 1),
                new BsonDocument("_id", 2)
            });

            var stages = new[]
            {
                "{ $count : 't' }",
                "{ $out : 'out' }"
            };
            var pipeline = new BsonDocumentStagePipelineDefinition<BsonDocument, BsonDocument>(stages.Select(s => BsonDocument.Parse(s)));
            var result = collection.Aggregate(pipeline).ToList();

            result.Should().HaveCount(1);
            result[0]["t"].AsInt32.Should().Be(2);
        }
    }
}
