/* Copyright 2010-2016 MongoDB Inc.
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
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class MongoServerTests
    {
        private static MongoServer __server;
        private static MongoDatabase __database;
        private static MongoCollection<BsonDocument> __collection;
        private static bool __isMasterSlavePair;
        private static bool __isReplicaSet;
        private static Lazy<bool> __lazyOneTimeSetup = new Lazy<bool>(OneTimeSetup);

        public MongoServerTests()
        {
            var _ = __lazyOneTimeSetup.Value;
        }

        private static bool OneTimeSetup()
        {
            __server = LegacyTestConfiguration.Server;
            __database = LegacyTestConfiguration.Database;
            __collection = LegacyTestConfiguration.Collection;
            __isReplicaSet = LegacyTestConfiguration.IsReplicaSet;

            var adminDatabase = __server.GetDatabase("admin");
            var commandResult = adminDatabase.RunCommand("getCmdLineOpts");
            var argv = commandResult.Response["argv"].AsBsonArray;
            __isMasterSlavePair = argv.Contains("--master") || argv.Contains("--slave");

            return true;
        }

        [Fact]
        public void TestArbiters()
        {
            if (__isReplicaSet)
            {
                var isMasterResult = __database.RunCommand("isMaster").Response;
                BsonValue arbiters;
                int arbiterCount = 0;
                if (isMasterResult.TryGetValue("arbiters", out arbiters))
                {
                    arbiterCount = arbiters.AsBsonArray.Count;
                }
                Assert.Equal(arbiterCount, __server.Arbiters.Length);
            }
        }

        [Fact]
        public void TestBuildInfo()
        {
            var versionZero = new Version(0, 0, 0);
            var buildInfo = __server.BuildInfo;
            Assert.NotEqual(versionZero, buildInfo.Version);
        }

        [Fact]
        public void TestCreateMongoServerSettings()
        {
            var settings = new MongoServerSettings
            {
                Server = new MongoServerAddress("localhost"),
            };
#pragma warning disable 618
            var server1 = MongoServer.Create(settings);
            var server2 = MongoServer.Create(settings);
#pragma warning restore 618
            Assert.Same(server1, server2);
            Assert.Equal(settings, server1.Settings);
        }

        [Fact]
        public void TestDatabaseExists()
        {
            if (!__isMasterSlavePair)
            {
                var databaseNamespace = CoreTestConfiguration.GetDatabaseNamespaceForTestClass(typeof(MongoServerTests));
                var database = __server.GetDatabase(databaseNamespace.DatabaseName);
                var collection = database.GetCollection("test");

                database.Drop();
                Assert.False(__server.DatabaseExists(database.Name));
                collection.Insert(new BsonDocument("x", 1));
                Assert.True(__server.DatabaseExists(database.Name));
            }
        }

        [Fact]
        public void TestDropDatabase()
        {
            if (!__isMasterSlavePair)
            {
                var databaseNamespace = CoreTestConfiguration.GetDatabaseNamespaceForTestClass(typeof(MongoServerTests));
                var database = __server.GetDatabase(databaseNamespace.DatabaseName);
                var collection = database.GetCollection("test");

                collection.Insert(new BsonDocument());
                var databaseNames = __server.GetDatabaseNames();
                Assert.True(databaseNames.Contains(database.Name));

                __server.DropDatabase(database.Name);
                databaseNames = __server.GetDatabaseNames();
                Assert.False(databaseNames.Contains(database.Name));
            }
        }

        [SkippableFact]
        public void TestDropDatabaseWriteConcern()
        {
            RequireServer.Check().Supports(Feature.CommandsThatWriteAcceptWriteConcern).ClusterType(ClusterType.ReplicaSet);
            var subject = __server;
            var writeConcern = new WriteConcern(9);

            var exception = Record.Exception(() => subject.WithWriteConcern(writeConcern).DropDatabase("database"));

            exception.Should().BeOfType<MongoWriteConcernException>();
        }

        [Fact]
        public void TestFetchDBRef()
        {
            __collection.Drop();
            __collection.Insert(new BsonDocument { { "_id", 1 }, { "x", 2 } });
            var dbRef = new MongoDBRef(__database.Name, __collection.Name, 1);
            var document = __server.FetchDBRef(dbRef);
            Assert.Equal(2, document.ElementCount);
            Assert.Equal(1, document["_id"].AsInt32);
            Assert.Equal(2, document["x"].AsInt32);
        }

        [Fact]
        public void TestGetAllServers()
        {
            var snapshot1 = MongoServer.GetAllServers();
#pragma warning disable 618
            var server = MongoServer.Create("mongodb://newhostnamethathasnotbeenusedbefore");
#pragma warning restore
            var snapshot2 = MongoServer.GetAllServers();
            Assert.Equal(snapshot1.Length + 1, snapshot2.Length);
            Assert.False(snapshot1.Contains(server));
            Assert.True(snapshot2.Contains(server));
            MongoServer.UnregisterServer(server);
            var snapshot3 = MongoServer.GetAllServers();
            Assert.Equal(snapshot1.Length, snapshot3.Length);
            Assert.False(snapshot3.Contains(server));
        }

        [Fact]
        public void TestGetDatabase()
        {
            var settings = new MongoDatabaseSettings { ReadPreference = ReadPreference.Primary };
            var database = __server.GetDatabase("test", settings);
            Assert.Equal("test", database.Name);
            Assert.Equal(ReadPreference.Primary, database.Settings.ReadPreference);
        }

        [Fact]
        public void TestGetDatabaseNames()
        {
            var names = __server.GetDatabaseNames();

            Assert.Equal(names.OrderBy(n => n), names);
        }

        [Fact]
        public void TestInstance()
        {
            if (__server.Instances.Length == 1)
            {
                var instance = __server.Instance;
                Assert.NotNull(instance);
                Assert.True(instance.IsPrimary);
            }
        }

        [Fact]
        public void TestInstances()
        {
            var instances = __server.Instances;
            Assert.NotNull(instances);
            Assert.True(instances.Length >= 1);
        }

        [Fact]
        public void TestIsDatabaseNameValid()
        {
            string message;
            Assert.Throws<ArgumentNullException>(() => { __server.IsDatabaseNameValid(null, out message); });
            Assert.False(__server.IsDatabaseNameValid("", out message));
            Assert.False(__server.IsDatabaseNameValid("/", out message));
            Assert.False(__server.IsDatabaseNameValid(new string('x', 128), out message));
            Assert.True(__server.IsDatabaseNameValid("$external", out message));
        }

        [Fact]
        public void TestPing()
        {
            __server.Ping();
        }

        [Fact]
        public void TestPrimary()
        {
            var instance = __server.Primary;
            Assert.NotNull(instance);
            Assert.True(instance.IsPrimary);
        }

        [SkippableFact]
        public void TestReconnect()
        {
            RequireEnvironment.Check().EnvironmentVariable("EXPLICIT");
            __server.Reconnect();
            Assert.True(__server.State == MongoServerState.Connected || __server.State == MongoServerState.ConnectedToSubset);
        }

        [Fact]
        public void TestReplicaSetName()
        {
            if (__isReplicaSet)
            {
                Assert.NotNull(__server.ReplicaSetName);
            }
            else
            {
                Assert.Null(__server.ReplicaSetName);
            }
        }

        [Fact]
        public void TestRequestStart()
        {
            Assert.Equal(0, __server.RequestNestingLevel);
            using (__server.RequestStart())
            {
                Assert.Equal(1, __server.RequestNestingLevel);
            }
            Assert.Equal(0, __server.RequestNestingLevel);
        }

        [Fact]
        public void TestRequestStartPrimary()
        {
            Assert.Equal(0, __server.RequestNestingLevel);
            using (__server.RequestStart(__server.Primary))
            {
                Assert.Equal(1, __server.RequestNestingLevel);
            }
            Assert.Equal(0, __server.RequestNestingLevel);
        }

        [Fact]
        public void TestRequestStartPrimaryNested()
        {
            Assert.Equal(0, __server.RequestNestingLevel);
            using (__server.RequestStart(__server.Primary))
            {
                Assert.Equal(1, __server.RequestNestingLevel);
                using (__server.RequestStart(__server.Primary))
                {
                    Assert.Equal(2, __server.RequestNestingLevel);
                }
                Assert.Equal(1, __server.RequestNestingLevel);
            }
            Assert.Equal(0, __server.RequestNestingLevel);
        }

        [Fact]
        public void TestSecondaries()
        {
            Assert.True(__server.Secondaries.Length < __server.Instances.Length);
        }

        [Fact]
        public void TestVersion()
        {
            var versionZero = new Version(0, 0, 0);
            Assert.NotEqual(versionZero, __server.BuildInfo.Version);
        }

        [Fact]
        public void TestWithReadConcern()
        {
            var originalReadConcern = new ReadConcern(ReadConcernLevel.Linearizable);
            var subject = __server.WithReadConcern(originalReadConcern);
            var newReadConcern = new ReadConcern(ReadConcernLevel.Majority);

            var result = subject.WithReadConcern(newReadConcern);

            subject.Settings.ReadConcern.Should().BeSameAs(originalReadConcern);
            result.Settings.ReadConcern.Should().BeSameAs(newReadConcern);
            result.WithReadConcern(originalReadConcern).Settings.Should().Be(subject.Settings);
        }

        [Fact]
        public void TestWithReadPreference()
        {
            var originalReadPreference = new ReadPreference(ReadPreferenceMode.Secondary);
            var subject = __server.WithReadPreference(originalReadPreference);
            var newReadPreference = new ReadPreference(ReadPreferenceMode.SecondaryPreferred);

            var result = subject.WithReadPreference(newReadPreference);

            subject.Settings.ReadPreference.Should().BeSameAs(originalReadPreference);
            result.Settings.ReadPreference.Should().BeSameAs(newReadPreference);
            result.WithReadPreference(originalReadPreference).Settings.Should().Be(subject.Settings);
        }

        [Fact]
        public void TestWithWriteConcern()
        {
            var originalWriteConcern = new WriteConcern(2);
            var subject = __server.WithWriteConcern(originalWriteConcern);
            var newWriteConcern = new WriteConcern(3);

            var result = subject.WithWriteConcern(newWriteConcern);

            subject.Settings.WriteConcern.Should().BeSameAs(originalWriteConcern);
            result.Settings.WriteConcern.Should().BeSameAs(newWriteConcern);
            result.WithWriteConcern(originalWriteConcern).Settings.Should().Be(subject.Settings);
        }
    }
}
