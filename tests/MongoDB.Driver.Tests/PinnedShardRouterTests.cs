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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class PinnedShardRouterTests
    {
        private static readonly HashSet<string> __commandsToNotCapture = new HashSet<string>
        {
            "isMaster",
            "buildInfo",
            "getLastError",
            "authenticate",
            "saslStart",
            "saslContinue",
            "getnonce"
        };
        
        private string _collectionName = "test";
        private string _databaseName = "test";
        
        /// <summary>
        /// Test that starting a new transaction on a pinned ClientSession unpins the
        /// session and normal server selection is performed for the next operation.
        /// </summary>
        [SkippableTheory]
        [ParameterAttributeData]
        public async void Test_Unpin_For_Next_Transaction([Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ShardedTransactions).ClusterType(ClusterType.Sharded);
            RequireMultipleShardRouters();
            
            DropCollection();
            var eventCapturer = CreateEventCapturer();
            var listOfFindResults = new List<List<BsonDocument>>();
            using (var client = CreateDisposableClient(eventCapturer, useMultipleShardRouters:true))
            using (var session = client.StartSession())
            {
                // Session is pinned to mongos.
                session.StartTransaction();
                var database = client.GetDatabase(_databaseName);
                CreateCollection();
                var collection = database.GetCollection<BsonDocument>(_collectionName)
                    .WithWriteConcern(WriteConcern.WMajority);

                if (async)
                {
                    await collection.InsertOneAsync(new BsonDocument()).ConfigureAwait(false);
                    await session.CommitTransactionAsync().ConfigureAwait(false);
                    for (var i = 0; i < 50; i++)
                    {
                        session.StartTransaction();
                        var cursor = await collection.FindAsync(session, filter: new BsonDocument()).ConfigureAwait(false);
                        listOfFindResults.Add(cursor.ToList());
                        await session.CommitTransactionAsync().ConfigureAwait(false);
                    }
                }
                else
                {
                    collection.InsertOne(new BsonDocument());
                    session.CommitTransaction();

                    for (var i = 0; i < 50; i++)
                    {
                        session.StartTransaction();
                        var cursor = collection.Find(session, filter: new BsonDocument());
                        listOfFindResults.Add(cursor.ToList());
                        session.CommitTransaction();
                    }
                }
            }
            
            listOfFindResults.Should().OnlyContain(findResult => findResult.Count > 0);
            var servers = new HashSet<ServerId>(eventCapturer.Events.Select(e => ((CommandStartedEvent) e).ConnectionId.ServerId));
            servers.Count.Should().BeGreaterThan(1);

        }

        /// <summary>
        /// Test non-transaction operations using a pinned ClientSession unpins the
        /// session and normal server selection is performed.
        /// </summary>
        [SkippableTheory]
        [ParameterAttributeData]
        public async void Test_Unpin_For_Non_Transaction_Operation([Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ShardedTransactions).ClusterType(ClusterType.Sharded);
            RequireMultipleShardRouters();
            
            DropCollection();
            var eventCapturer = CreateEventCapturer();
            var listOfFindResults = new List<List<BsonDocument>>();
            using (var client = CreateDisposableClient(eventCapturer, useMultipleShardRouters:true))
            using (var session = client.StartSession())
            {
                CreateCollection();
                // Session is pinned to mongos.
                session.StartTransaction();
                var database = client.GetDatabase(_databaseName);
                var collection = database.GetCollection<BsonDocument>(_collectionName)
                    .WithWriteConcern(WriteConcern.WMajority);

                if (async)
                {
                    await collection.InsertOneAsync(new BsonDocument()).ConfigureAwait(false);
                    await session.CommitTransactionAsync().ConfigureAwait(false);
                    for (var i = 0; i < 50; i++)
                    {
                        var cursor = await collection.FindAsync(session, filter: new BsonDocument()).ConfigureAwait(false);
                        listOfFindResults.Add(cursor.ToList());
                    }    
                }
                else
                { 
                    session.CommitTransaction();
                    for (var i = 0; i < 50; i++)
                    {
                        listOfFindResults.Add(collection.Find(session, filter: new BsonDocument()).ToList());
                    }
                }
            }

            listOfFindResults.Should().OnlyContain(findResult => findResult.Count > 0);
            var servers = new HashSet<ServerId>(eventCapturer.Events.Select(e => ((CommandStartedEvent) e).ConnectionId.ServerId));
            servers.Count.Should().BeGreaterThan(1);
        }

        private EventCapturer CreateEventCapturer()
        {
            return new EventCapturer()
                .Capture<CommandStartedEvent>(e => !__commandsToNotCapture.Contains(e.CommandName));
        }

        private void CreateCollection()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase(_databaseName).WithWriteConcern(WriteConcern.WMajority);
            // Collection must be created outside of a transaction
            database.GetCollection<BsonDocument>(_collectionName).InsertOne(new BsonDocument());
        }

        private DisposableMongoClient CreateDisposableClient(EventCapturer eventCapturer, bool useMultipleShardRouters)
        {
            // Increase localThresholdMS and wait until all nodes are discovered to avoid false positives.
            var client = DriverTestConfiguration.CreateDisposableClient((MongoClientSettings settings) =>
                {
                    settings.ClusterConfigurator = c => c.Subscribe(eventCapturer);
                    settings.LocalThreshold = TimeSpan.FromMilliseconds(1000);
                }, 
                useMultipleShardRouters);
            var timeOut = TimeSpan.FromSeconds(60);
            SpinWait.SpinUntil(() => client.Cluster.Description.Type != ClusterType.Unknown, timeOut).Should().BeTrue();
            return client;
        }

        private void DropCollection()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase(_databaseName).WithWriteConcern(WriteConcern.WMajority);
            database.DropCollection(_collectionName);
        }
        
        private void RequireMultipleShardRouters()
        {
            var connectionString = CoreTestConfiguration.ConnectionStringWithMultipleShardRouters.ToString();
            var numberOfShardRouters = MongoClientSettings.FromUrl(new MongoUrl(connectionString)).Servers.Count();
            if (numberOfShardRouters < 2)
            {
                throw new SkipException("Two or more shard routers are required.");
            }
        }
    }
}
