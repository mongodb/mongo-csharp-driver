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

using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
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

            var eventCapturer = new EventCapturer().Capture<ConnectionPoolRemovedConnectionEvent>();
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
                }
                eventCapturer.Events.Count.Should().Be(1);
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Connection_pool_should_not_be_cleared_when_replSetStepDown_and_GetMore([Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.KeepConnectionPoolWhenReplSetStepDown).ClusterType(ClusterType.ReplicaSet);

            var eventCapturer = new EventCapturer().Capture<ConnectionPoolRemovedConnectionEvent>();
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

                BsonDocument replSetStepDownResult;
                if (async)
                {
                    replSetStepDownResult = adminDatabase.RunCommandAsync<BsonDocument>("{ replSetStepDown : 5, force : true }").GetAwaiter().GetResult();
                }
                else
                {
                    replSetStepDownResult = adminDatabase.RunCommand<BsonDocument>("{ replSetStepDown : 5, force : true }");
                }

                replSetStepDownResult.GetValue("ok", false).ToBoolean().Should().BeTrue();

                cursor.MoveNext();

                eventCapturer.Events.Should().BeEmpty();
            }
        }

        [SkippableFact]
        public void Connection_pool_should_work_as_expected_when_NonMaster_exception()
        {
            RequireServer.Check().Supports(Feature.FailPointsFailCommand).ClusterType(ClusterType.ReplicaSet);

            var shouldConnectionPoolBeCleared = !Feature.KeepConnectionPoolWhenNotMasterConnectionException.IsSupported(CoreTestConfiguration.ServerVersion);

            var eventCapturer = new EventCapturer().Capture<ConnectionPoolRemovedConnectionEvent>();
            using (var client = CreateDisposableClient(eventCapturer))
            {
                var database = client.GetDatabase(_databaseName, new MongoDatabaseSettings { WriteConcern = WriteConcern.WMajority });
                database.DropCollection(_databaseName);
                var collection = database.GetCollection<BsonDocument>(_collectionName, new MongoCollectionSettings { WriteConcern = WriteConcern.WMajority });
                eventCapturer.Clear();

                using (ConfigureFailPoint(client, 10107))
                {
                    var document = new BsonDocument("test", 1);
                    var exception = Record.Exception(() => { collection.InsertOne(document); });

                    var e = exception.Should().BeOfType<MongoNotPrimaryException>().Subject;
                    e.Code.Should().Be(10107);

                    if (!shouldConnectionPoolBeCleared)
                    {
                        collection.InsertOne(document);
                    }
                }
                eventCapturer.Events.Count.Should().Be(shouldConnectionPoolBeCleared ? 1 : 0);
            }
        }

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
                    settings.RetryWrites = false;
                    settings.ClusterConfigurator = c => { c.Subscribe(capturedEvents); };
                });
        }
    }
}
