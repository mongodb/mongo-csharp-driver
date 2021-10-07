﻿/* Copyright 2019-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class ConnectionsSurvivePrimaryStepDownTests
    {
        private readonly string _collectionName = "step-down";
        private readonly string _databaseName = "step-down";

        [SkippableTheory]
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
            using (var client = CreateDisposableClient(eventCapturer))
            {
                var database = client.GetDatabase(_databaseName, new MongoDatabaseSettings { WriteConcern = WriteConcern.WMajority });
                database.DropCollection(_databaseName);
                var collection = database.GetCollection<BsonDocument>(_collectionName, new MongoCollectionSettings { WriteConcern = WriteConcern.WMajority });
                eventCapturer.Clear();

                using (ConfigureFailPoint(client, errorCode))
                {
                    var exception = Record.Exception(() => { collection.InsertOne(new BsonDocument("test", 1)); });

                    var e = exception.Should().BeOfType<MongoNodeIsRecoveringException>().Subject;
                    e.Code.Should().Be(errorCode);

                    eventCapturer.Next().Should().BeOfType<ConnectionPoolClearedEvent>();
                    eventCapturer.Events.Should().BeEmpty();

                    collection.InsertOne(new BsonDocument("test", 1));
                    eventCapturer.Next().Should().BeOfType<ConnectionCreatedEvent>();
                    eventCapturer.Events.Should().BeEmpty();
                }
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Connection_pool_should_not_be_cleared_when_replSetStepDown_and_GetMore([Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.KeepConnectionPoolWhenReplSetStepDown).ClusterType(ClusterType.ReplicaSet);

            var eventCapturer = new EventCapturer().Capture<ConnectionPoolClearedEvent>();
            using (var client = CreateDisposableClient(eventCapturer))
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

                replSetStepDownResult.Should().NotBeNull();
                replSetStepDownResult.GetValue("ok", false).ToBoolean().Should().BeTrue();

                cursor.MoveNext();

                eventCapturer.Events.Should().BeEmpty(); // it also means that no new PoolClearedEvent
            }

            void RunOnSecondary(IMongoClient primaryClient, EndPoint secondaryEndpoint, BsonDocument command)
            {
                var secondarySettings = primaryClient.Settings.Clone();
                secondarySettings.ClusterConfigurator = null;
#pragma warning disable CS0618 // Type or member is obsolete
                secondarySettings.ConnectionMode = ConnectionMode.Direct;
#pragma warning restore CS0618 // Type or member is obsolete
                var secondaryDnsEndpoint = (DnsEndPoint)secondaryEndpoint;
                secondarySettings.Server = new MongoServerAddress(secondaryDnsEndpoint.Host, secondaryDnsEndpoint.Port);
                using (var secondaryClient = DriverTestConfiguration.CreateDisposableClient(secondarySettings))
                {
                    var adminDatabase = secondaryClient.GetDatabase(DatabaseNamespace.Admin.DatabaseName);
                    adminDatabase.RunCommand<BsonDocument>(command);
                }
            }
        }

        [SkippableFact]
        public void Connection_pool_should_work_as_expected_when_NonPrimary_exception()
        {
            RequireServer.Check().Supports(Feature.FailPointsFailCommand).ClusterType(ClusterType.ReplicaSet);

            var shouldConnectionPoolBeCleared = !Feature.KeepConnectionPoolWhenNotPrimaryConnectionException.IsSupported(CoreTestConfiguration.ServerVersion);

            var eventCapturer = new EventCapturer()
                .Capture<ConnectionPoolClearedEvent>()
                .Capture<ConnectionCreatedEvent>();
            using (var client = CreateDisposableClient(eventCapturer))
            {
                var database = client.GetDatabase(_databaseName, new MongoDatabaseSettings { WriteConcern = WriteConcern.WMajority });
                database.DropCollection(_databaseName);
                var collection = database.GetCollection<BsonDocument>(_collectionName, new MongoCollectionSettings { WriteConcern = WriteConcern.WMajority });
                eventCapturer.Clear();

                using (ConfigureFailPoint(client, 10107))
                {
                    var exception = Record.Exception(() => { collection.InsertOne(new BsonDocument("test", 1)); });

                    var e = exception.Should().BeOfType<MongoNotPrimaryException>().Subject;
                    e.Code.Should().Be(10107);

                    if (shouldConnectionPoolBeCleared)
                    {
                        eventCapturer.Next().Should().BeOfType<ConnectionPoolClearedEvent>();
                        eventCapturer.Events.Should().BeEmpty();
                    }
                    else
                    {
                        eventCapturer.Events.Should().BeEmpty();
                    }

                    collection.InsertOne(new BsonDocument("test", 1));
                    if (shouldConnectionPoolBeCleared)
                    {
                        eventCapturer.Next().Should().BeOfType<ConnectionCreatedEvent>();
                    }
                    eventCapturer.Events.Should().BeEmpty();
                }
            }
        }

        // private methods
        private FailPoint ConfigureFailPoint(IMongoClient client, int errorCode)
        {
            var session = NoCoreSession.NewHandle();

            var args = BsonDocument.Parse($"{{ mode : {{ times : 1 }}, data : {{ failCommands : [\"insert\"], errorCode : {errorCode} }} }}");
            return FailPoint.Configure(client.Cluster, session, "failCommand", args);
        }

        private DisposableMongoClient CreateDisposableClient(EventCapturer capturedEvents)
        {
            return DriverTestConfiguration.CreateDisposableClient(
                settings =>
                {
                    settings.HeartbeatInterval = TimeSpan.FromMilliseconds(5); // the default value for spec tests
                    settings.RetryWrites = false;
                    settings.ClusterConfigurator = c => { c.Subscribe(capturedEvents); };
                },
                null);
        }
    }
}
