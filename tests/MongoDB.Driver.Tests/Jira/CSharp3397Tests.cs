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
        public void Aggregate_out_to_time_series_collection_on_secondary_should_work()
        {
            RequireServer.Check().Supports(Feature.AggregateOutTimeSeries);

            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("test");
            var collection = database.GetCollection<BsonDocument>("testCol");
            var outCollection = database.GetCollection<BsonDocument>("timeCol");

            var writeConcern = WriteConcern.WMajority;
            if (DriverTestConfiguration.IsReplicaSet(client))
            {
                var n = DriverTestConfiguration.GetReplicaSetNumberOfDataBearingMembers(client);
                writeConcern = new WriteConcern(n);
            }

            database.DropCollection("testCol");
            database.DropCollection("timeCol");
            collection
                .WithWriteConcern(writeConcern)
                .InsertOne(new BsonDocument("_id", 1));

            var fields = Builders<BsonDocument>.SetFields.Set("time", DateTime.Now);
            var pipeline = new EmptyPipelineDefinition<BsonDocument>()
                .Match(FilterDefinition<BsonDocument>.Empty)
                .Set(fields)
                .Out(outCollection, new TimeSeriesOptions("time"));

            var results = collection.WithReadPreference(ReadPreference.SecondaryPreferred).Aggregate(pipeline).ToList();
            results.Count.Should().Be(1);

            var listCollectionsCommand = new BsonDocument
            {
                { "listCollections", 1 }, { "filter", new BsonDocument { { "type", "timeseries" } } }
            };
            var output = database.RunCommand<BsonDocument>(listCollectionsCommand);
            output["cursor"]["firstBatch"][0][0].ToString().Should().Be("timeCol"); // checking name of collection
            output["cursor"]["firstBatch"][0][1].ToString().Should().Be("timeseries"); // checking type of collection
        }
    }

    public class AggregateCountResultWithId
    {
        public ObjectId Id { get; set; }
        [BsonElement("count")]
        public long Count { get; set; }
    }
}
