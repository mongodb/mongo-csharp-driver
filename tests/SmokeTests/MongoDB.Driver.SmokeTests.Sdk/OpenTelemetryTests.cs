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

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Configuration;
using Xunit;

namespace MongoDB.Driver.SmokeTests.Sdk
{
    [Trait("Category", "Integration")]
    public sealed class OpenTelemetryTests
    {

        [Fact]
        public void MongoClient_should_create_activities_when_tracing_enabled()
        {
            using var activityListener = CreateActivityListener(out var capturedActivities);

            var settings = MongoClientSettings.FromConnectionString(InfrastructureUtilities.MongoUri);
            var mongoClient = new MongoClient(settings);

            try
            {
                var database = mongoClient.GetDatabase("test");
                var collection = database.GetCollection<BsonDocument>("smoketest");

                collection.InsertOne(new BsonDocument("name", "test"));
                collection.Find(Builders<BsonDocument>.Filter.Empty).FirstOrDefault();
                collection.DeleteOne(Builders<BsonDocument>.Filter.Eq("name", "test"));
            }
            finally
            {
                ClusterRegistry.Instance.UnregisterAndDisposeCluster(mongoClient.Cluster);
            }

            capturedActivities.Should().HaveCount(6);

            var operationActivities = capturedActivities.Where(a => a.GetTagItem("db.operation.name") != null).ToList();
            var commandActivities = capturedActivities.Where(a => a.GetTagItem("db.command.name") != null).ToList();

            operationActivities.Should().HaveCount(3);
            commandActivities.Should().HaveCount(3);
        }

        [Fact]
        public void MongoClient_should_not_create_activities_when_tracing_disabled()
        {
            using var activityListener = CreateActivityListener(out var capturedActivities);

            var settings = MongoClientSettings.FromConnectionString(InfrastructureUtilities.MongoUri);
            settings.TracingOptions = new TracingOptions { Disabled = true };
            var mongoClient = new MongoClient(settings);

            try
            {
                var database = mongoClient.GetDatabase("test");
                var collection = database.GetCollection<BsonDocument>("smoketest");

                collection.InsertOne(new BsonDocument("name", "test"));
                collection.Find(Builders<BsonDocument>.Filter.Empty).FirstOrDefault();
                collection.DeleteOne(Builders<BsonDocument>.Filter.Eq("name", "test"));
            }
            finally
            {
                ClusterRegistry.Instance.UnregisterAndDisposeCluster(mongoClient.Cluster);
            }

            capturedActivities.Should().BeEmpty();
        }

        private static ActivityListener CreateActivityListener(out List<Activity> capturedActivities)
        {
            var activities = new List<Activity>();
            var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == MongoTelemetry.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity => activities.Add(activity)
            };

            ActivitySource.AddActivityListener(listener);

            capturedActivities = activities;
            return listener;
        }
    }
}
