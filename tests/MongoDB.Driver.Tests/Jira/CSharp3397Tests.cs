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

using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Jira
{
    public class CSharp3397Tests
    {
        [Fact]
        public void Aggregate_out_to_collection_should_work()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("test");
            var collection = database.GetCollection<BsonDocument>("test");
            var outCollection = database.GetCollection<AggregateCountResult>("out");

            var writeConcern = WriteConcern.WMajority;
            if (DriverTestConfiguration.IsReplicaSet(client))
            {
                var n = DriverTestConfiguration.GetReplicaSetNumberOfDataBearingMembers(client);
                writeConcern = new WriteConcern(n);
            }

            database.DropCollection("test");
            database.DropCollection("out");
            collection
                .WithWriteConcern(writeConcern)
                .InsertOne(new BsonDocument("_id", 1));

            var pipeline = new EmptyPipelineDefinition<BsonDocument>()
                .Count()
                .Out(outCollection)
                .As<BsonDocument, AggregateCountResult, AggregateCountResultWithId>();
            var results = collection.WithReadPreference(ReadPreference.SecondaryPreferred).Aggregate(pipeline).ToList();

            results.Single().Count.Should().Be(1);
        }

        [Fact]
        public void Aggregate_out_to_time_series_collection_should_work()
        {
            RequireServer.Check().Supports(Feature.AggregateOutOnSecondaryTimeSeries);
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("test");
            var collection = database.GetCollection<BsonDocument>("test");
            var outCollection = database.GetCollection<AggregateCountResult>("out");

            var writeConcern = WriteConcern.WMajority;
            if (DriverTestConfiguration.IsReplicaSet(client))
            {
                var n = DriverTestConfiguration.GetReplicaSetNumberOfDataBearingMembers(client);
                writeConcern = new WriteConcern(n);
            }

            database.DropCollection("test");
            database.DropCollection("out");
            collection
                .WithWriteConcern(writeConcern)
                .InsertOne(new BsonDocument("_id", 1));

            var pipeline = new EmptyPipelineDefinition<BsonDocument>()
                .Count()
                .Out(outCollection, new TimeSeriesOptions("time", "testing"))
                .As<BsonDocument, AggregateCountResult, AggregateCountResultWithId>();
            var results = collection.WithReadPreference(ReadPreference.SecondaryPreferred).Aggregate(pipeline).ToList();

            results.Single().Count.Should().Be(1);
        }
    }

    public class AggregateCountResultWithId
    {
        public ObjectId Id { get; set; }
        [BsonElement("count")]
        public long Count { get; set; }
    }
}
