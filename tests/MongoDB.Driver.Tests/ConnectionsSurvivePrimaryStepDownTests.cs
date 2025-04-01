/* Copyright 2019-present MongoDB Inc.
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
using System.Net;
using Shouldly;
using MongoDB.Bson;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class ConnectionsSurvivePrimaryStepDownTests
    {
        private readonly string _collectionName = "step-down";
        private readonly string _databaseName = "step-down";

        [Theory]
        [ParameterAttributeData]
        public void Connection_pool_should_be_cleared_when_Shutdown_exceptions(
            [Values(
                ServerErrorCode.ShutdownInProgress, // 91
                ServerErrorCode.InterruptedAtShutdown)] // 11600
            int errorCode)
        {
            RequireServer.Check().Supports(Feature.FailPointsFailCommand).ClusterType(ClusterType.ReplicaSet);

            var eventCapturer = new EventCapturer()
                .Capture<ConnectionPoolClearedEvent>()
                .Capture<ConnectionCreatedEvent>();
            using (var client = CreateMongoClient(eventCapturer))
            {
                var database = client.GetDatabase(_databaseName, new MongoDatabaseSettings { WriteConcern = WriteConcern.WMajority });
                database.DropCollection(_databaseName);
                var collection = database.GetCollection<BsonDocument>(_collectionName, new MongoCollectionSettings { WriteConcern = WriteConcern.WMajority });
                eventCapturer.Clear();

                using (ConfigureFailPoint(client, errorCode))
                {
                    var exception = Record.Exception(() => { collection.InsertOne(new BsonDocument("test", 1)); });

                    var e = exception.ShouldBeOfType<MongoNodeIsRecoveringException>();
                    e.Code.ShouldBe(errorCode);

                    eventCapturer.Next().ShouldBeOfType<ConnectionPoolClearedEvent>();
                    eventCapturer.Events.ShouldBeEmpty();

                    collection.InsertOne(new BsonDocument("test", 1));
                    eventCapturer.Next().ShouldBeOfType<ConnectionCreatedEvent>();
                    eventCapturer.Events.ShouldBeEmpty();
                }
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Connection_pool_should_not_be_cleared_when_replSetStepDown_and_GetMore([Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.KeepConnectionPoolWhenReplSetStepDown).ClusterType(ClusterType.ReplicaSet);

            var eventCapturer = new EventCapturer().Capture<ConnectionPoolClearedEvent>();
            using (var client = CreateMongoClient(eventCapturer))
            {
                var database = client.GetDatabase(_databaseName, new MongoDatabaseSettings { WriteConcern = WriteConcern.WMajority });
                database.DropCollection(_databaseName);
                var collection = database.GetCollection<BsonDocument>(_collectionName, new MongoCollectionSettings { WriteConcern = WriteConcern.WMajority });
                var adminDatabase = client.GetDatabase("admin").WithWriteConcern(WriteConcern.W1);

                collection.InsertMany(
                    new[]
                    {
                        new BsonDocument("x", 1),
                        new BsonDocument("x", 2),
                        new BsonDocument("x", 3),
                        new BsonDocument("x", 4),
                        new BsonDocument("x", 5),
                    });
                eventCapturer.Clear();

                var cursor = collection.FindSync(FilterDefinition<BsonDocument>.Empty, new FindOptions<BsonDocument> { BatchSize = 2 });
                cursor.MoveNext();

                foreach (var secondary in client.Cluster.Description.Servers.Where(c => c.Type == ServerType.ReplicaSetSecondary))
                {
                    RunOnSecondary(client, secondary.EndPoint, BsonDocument.Parse("{ replSetFreeze : 0 }"));
                }

                var replSetStepDownCommand = BsonDocument.Parse("{ replSetStepDown : 30, force : true }");
                BsonDocument replSetStepDownResult;
                if (async)
                {
                    replSetStepDownResult = adminDatabase.RunCommandAsync<BsonDocument>(replSetStepDownCommand).GetAwaiter().GetResult();
                }
                else
                {
                    replSetStepDownResult = adminDatabase.RunCommand<BsonDocument>(replSetStepDownCommand);
                }

                replSetStepDownResult.ShouldNotBeNull();
                replSetStepDownResult.GetValue("ok", false).ToBoolean().ShouldBeTrue();

                cursor.MoveNext();

                eventCapturer.Events.ShouldBeEmpty(); // it also means that no new PoolClearedEvent
            }

            void RunOnSecondary(IMongoClient primaryClient, EndPoint secondaryEndpoint, BsonDocument command)
            {
                var secondarySettings = primaryClient.Settings.Clone();
                secondarySettings.ClusterConfigurator = null;
                secondarySettings.DirectConnection = true;
                var secondaryDnsEndpoint = (DnsEndPoint)secondaryEndpoint;
                secondarySettings.Server = new MongoServerAddress(secondaryDnsEndpoint.Host, secondaryDnsEndpoint.Port);
                using (var secondaryClient = DriverTestConfiguration.CreateMongoClient(secondarySettings))
                {
                    var adminDatabase = secondaryClient.GetDatabase(DatabaseNamespace.Admin.DatabaseName);
                    adminDatabase.RunCommand<BsonDocument>(command);
                }
            }
        }

        [Fact]
        public void Connection_pool_should_work_as_expected_when_NonPrimary_exception()
        {
            RequireServer.Check().Supports(Feature.FailPointsFailCommand).ClusterType(ClusterType.ReplicaSet);

            var shouldConnectionPoolBeCleared = !Feature.KeepConnectionPoolWhenNotPrimaryConnectionException.IsSupported(CoreTestConfiguration.MaxWireVersion);

            var eventCapturer = new EventCapturer()
                .Capture<ConnectionPoolClearedEvent>()
                .Capture<ConnectionCreatedEvent>();
            using (var client = CreateMongoClient(eventCapturer))
            {
                var database = client.GetDatabase(_databaseName, new MongoDatabaseSettings { WriteConcern = WriteConcern.WMajority });
                database.DropCollection(_databaseName);
                var collection = database.GetCollection<BsonDocument>(_collectionName, new MongoCollectionSettings { WriteConcern = WriteConcern.WMajority });
                eventCapturer.Clear();

                using (ConfigureFailPoint(client, 10107))
                {
                    var exception = Record.Exception(() => { collection.InsertOne(new BsonDocument("test", 1)); });

                    var e = exception.ShouldBeOfType<MongoNotPrimaryException>();
                    e.Code.ShouldBe(10107);

                    if (shouldConnectionPoolBeCleared)
                    {
                        eventCapturer.Next().ShouldBeOfType<ConnectionPoolClearedEvent>();
                        eventCapturer.Events.ShouldBeEmpty();
                    }
                    else
                    {
                        eventCapturer.Events.ShouldBeEmpty();
                    }

                    collection.InsertOne(new BsonDocument("test", 1));
                    if (shouldConnectionPoolBeCleared)
                    {
                        eventCapturer.Next().ShouldBeOfType<ConnectionCreatedEvent>();
                    }
                    eventCapturer.Events.ShouldBeEmpty();
                }
            }
        }

        // private methods
        private FailPoint ConfigureFailPoint(IMongoClient client, int errorCode)
        {
            var session = NoCoreSession.NewHandle();

            var args = BsonDocument.Parse($"{{ mode : {{ times : 1 }}, data : {{ failCommands : [\"insert\"], errorCode : {errorCode} }} }}");
            return FailPoint.Configure(client.GetClusterInternal(), session, "failCommand", args);
        }

        private IMongoClient CreateMongoClient(EventCapturer capturedEvents) =>
            DriverTestConfiguration.CreateMongoClient(
                settings =>
                {
                    settings.HeartbeatInterval = TimeSpan.FromMilliseconds(5); // the default value for spec tests
                    settings.RetryWrites = false;
                    settings.ClusterConfigurator = c => { c.Subscribe(capturedEvents); };
                });
    }
}
