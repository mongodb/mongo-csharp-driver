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
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.retryable_writes.prose_tests
{
    [Trait("Category", "Integration")]
    public class RetryWriteOnOtherMongos
    {
        [Fact]
        public void Sharded_cluster_retryable_writes_are_retried_on_different_mongos_if_available()
        {
            RequireServer.Check()
                .Supports(Feature.FailPointsFailCommandForSharded)
                .ClusterTypes(ClusterType.Sharded)
                .MultipleMongoses(true);

            var failPointCommand = BsonDocument.Parse(
                @"{
                    configureFailPoint: ""failCommand"",
                    mode: { times: 1 },
                    data:
                    {
                        failCommands: [""insert""],
                        errorCode: 6,
                        errorLabels: [""RetryableWriteError""]
                    }
                }");

            var eventCapturer = new EventCapturer().CaptureCommandEvents("insert");

            using var client = DriverTestConfiguration.CreateMongoClient(
                s =>
                {
                    s.RetryWrites = true;
                    s.ClusterConfigurator = b => b.Subscribe(eventCapturer);
                },
                useMultipleShardRouters: true);

            var (failPointServer1, roundTripTime1) = client.GetClusterInternal().SelectServer(OperationContext.NoTimeout, new EndPointServerSelector(client.Cluster.Description.Servers[0].EndPoint));
            var (failPointServer2, roundTripTime2) = client.GetClusterInternal().SelectServer(OperationContext.NoTimeout, new EndPointServerSelector(client.Cluster.Description.Servers[1].EndPoint));

            using var failPoint1 = FailPoint.Configure(failPointServer1, roundTripTime1, NoCoreSession.NewHandle(), failPointCommand);
            using var failPoint2 = FailPoint.Configure(failPointServer2, roundTripTime2, NoCoreSession.NewHandle(), failPointCommand);

            var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

            Assert.Throws<MongoCommandException>(() =>
            {
                collection.InsertOne(new BsonDocument("x", 1));
            });

            var failedEvents = eventCapturer.Events.OfType<CommandFailedEvent>().ToArray();
            failedEvents.Length.Should().Be(2);

            failedEvents[0].CommandName.Should().Be(failedEvents[1].CommandName).And.Be("insert");
            failedEvents[0].ConnectionId.ServerId.Should().NotBe(failedEvents[1].ConnectionId.ServerId);
        }

        [Fact]
        public void Sharded_cluster_retryable_writes_are_retried_on_same_mongo_if_no_other_is_available()
        {
            RequireServer.Check()
                .Supports(Feature.FailPointsFailCommandForSharded)
                .ClusterTypes(ClusterType.Sharded);

            var failPointCommand = BsonDocument.Parse(
                @"{
                    configureFailPoint: ""failCommand"",
                    mode: { times: 1 },
                    data:
                    {
                        failCommands: [""insert""],
                        errorCode: 6,
                        errorLabels: [""RetryableWriteError""]
                    }
                }");

            var eventCapturer = new EventCapturer().CaptureCommandEvents("insert");

            using var client = DriverTestConfiguration.CreateMongoClient(
                s =>
                {
                    s.RetryWrites = true;
                    s.DirectConnection = false;
                    s.ClusterConfigurator = b => b.Subscribe(eventCapturer);
                },
                useMultipleShardRouters: false);

            var (failPointServer, roundTripTime) = client.GetClusterInternal().SelectServer(OperationContext.NoTimeout, new EndPointServerSelector(client.Cluster.Description.Servers[0].EndPoint));

            using var failPoint = FailPoint.Configure(failPointServer, roundTripTime, NoCoreSession.NewHandle(), failPointCommand);

            var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

            collection.InsertOne(new BsonDocument("x", 1));

            var failedEvents = eventCapturer.Events.OfType<CommandFailedEvent>().ToArray();
            var succeededEvents = eventCapturer.Events.OfType<CommandSucceededEvent>().ToArray();

            failedEvents.Length.Should().Be(1);
            succeededEvents.Length.Should().Be(1);

            failedEvents[0].CommandName.Should().Be(succeededEvents[0].CommandName).And.Be("insert");
            failedEvents[0].ConnectionId.ServerId.Should().Be(succeededEvents[0].ConnectionId.ServerId);
        }
    }
}
