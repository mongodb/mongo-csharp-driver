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
using MongoDB.Driver.Builders;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class MongoDatabaseTests
    {
        private MongoServer _server;
        private MongoServerInstance _primary;
        private MongoDatabase _database;
        private MongoDatabase _adminDatabase;

        public MongoDatabaseTests()
        {
            _server = LegacyTestConfiguration.Server;
            _primary = LegacyTestConfiguration.Server.Primary;
            _database = LegacyTestConfiguration.Database;
            _adminDatabase = _server.GetDatabase("admin");
            // TODO: DropDatabase
            //_database.Drop();
        }

        // TODO: more tests for MongoDatabase

        [Fact]
        public void TestCollectionExists()
        {
            var collectionName = "testcollectionexists";
            Assert.False(_database.CollectionExists(collectionName));

            _database.GetCollection(collectionName).Insert(new BsonDocument());
            Assert.True(_database.CollectionExists(collectionName));
        }

        [Fact]
        public void TestConstructorArgumentChecking()
        {
            var settings = new MongoDatabaseSettings();
            Assert.Throws<ArgumentNullException>(() => { new MongoDatabase(null, "name", settings); });
            Assert.Throws<ArgumentNullException>(() => { new MongoDatabase(_server, null, settings); });
            Assert.Throws<ArgumentNullException>(() => { new MongoDatabase(_server, "name", null); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { new MongoDatabase(_server, "", settings); });
        }

        [Fact]
        public void TestCreateCollection()
        {
            var collectionName = "testcreatecollection";
            Assert.False(_database.CollectionExists(collectionName));

            _database.CreateCollection(collectionName);
            Assert.True(_database.CollectionExists(collectionName));
        }

        [SkippableFact]
        public void TestCreateCollectionSetIndexOptionDefaults()
        {
            RequireServer.Check().Supports(Feature.IndexOptionsDefaults);
            var collection = _database.GetCollection("testindexoptiondefaults");
            collection.Drop();
            Assert.False(collection.Exists());
            var storageEngineOptions = new BsonDocument("mmapv1", new BsonDocument());
            var indexOptionDefaults = new IndexOptionDefaults { StorageEngine = storageEngineOptions };
            var expectedIndexOptionDefaultsDocument = new BsonDocument("storageEngine", storageEngineOptions);
            var options = CollectionOptions.SetIndexOptionDefaults(indexOptionDefaults);

            _database.CreateCollection(collection.Name, options);

            var commandResult = _database.RunCommand("listCollections");
            var collectionInfo = commandResult.Response["cursor"]["firstBatch"].AsBsonArray.Where(doc => doc["name"] == collection.Name).Single().AsBsonDocument;
            Assert.Equal(expectedIndexOptionDefaultsDocument, collectionInfo["options"]["indexOptionDefaults"]);
        }

        [SkippableFact]
        public void TestCreateCollectionSetStorageEngine()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("2.7.0");
            var collection = _database.GetCollection("storage_engine_collection");
            collection.Drop();
            Assert.False(collection.Exists());
            var storageEngineOptions = new BsonDocument
            {
                { "wiredTiger", new BsonDocument("configString", "block_compressor=zlib") },
                { "mmapv1", new BsonDocument() }
            };
            var options = CollectionOptions.SetStorageEngineOptions(storageEngineOptions);
            _database.CreateCollection(collection.Name, options);

            var result = _database.RunCommand("listCollections");
            var resultCollection = result.Response["cursor"]["firstBatch"].AsBsonArray.Where(doc => doc["name"] == collection.Name).Single();
            Assert.Equal(storageEngineOptions, resultCollection["options"]["storageEngine"]);
        }

        [SkippableFact]
        public void TestCreateCollectionSetValidator()
        {
            RequireServer.Check().Supports(Feature.DocumentValidation);
            var collection = _database.GetCollection("testvalidation");
            collection.Drop();
            Assert.False(collection.Exists());
            var options = CollectionOptions
                .SetValidator(new QueryDocument("_id", new BsonDocument("$exists", true)))
                .SetValidationAction(DocumentValidationAction.Error)
                .SetValidationLevel(DocumentValidationLevel.Strict);

            _database.CreateCollection(collection.Name, options);

            var commandResult = _database.RunCommand("listCollections");
            var collectionInfo = commandResult.Response["cursor"]["firstBatch"].AsBsonArray.Where(c => c["name"] == collection.Name).Single();
            Assert.Equal(new BsonDocument("_id", new BsonDocument("$exists", true)), collectionInfo["options"]["validator"]);
            Assert.Equal("error", collectionInfo["options"]["validationAction"].AsString);
            Assert.Equal("strict", collectionInfo["options"]["validationLevel"].AsString);
        }

        [SkippableFact]
        public void TestCreateCollectionWriteConcern()
        {
            RequireServer.Check().Supports(Feature.CommandsThatWriteAcceptWriteConcern).ClusterType(ClusterType.ReplicaSet);
            var subject = _database;
            var writeConcern = new WriteConcern(9);

            var exception = Record.Exception(() => subject.WithWriteConcern(writeConcern).CreateCollection("collection"));

            exception.Should().BeOfType<MongoWriteConcernException>();
        }

        [SkippableFact]
        public void TestCreateViewWriteConcern()
        {
            RequireServer.Check().Supports(Feature.CommandsThatWriteAcceptWriteConcern).ClusterType(ClusterType.ReplicaSet);
            var subject = _database;
            var writeConcern = new WriteConcern(9);
            var pipeline = new BsonDocument[0];

            var exception = Record.Exception(() => subject.WithWriteConcern(writeConcern).CreateView("viewName", "viewOn", pipeline, null));

            exception.Should().BeOfType<MongoWriteConcernException>();
        }

        [Fact]
        public void TestDropCollection()
        {
            var collectionName = "testdropcollection";
            Assert.False(_database.CollectionExists(collectionName));

            _database.GetCollection(collectionName).Insert(new BsonDocument());
            Assert.True(_database.CollectionExists(collectionName));

            _database.DropCollection(collectionName);
            Assert.False(_database.CollectionExists(collectionName));
        }

        [SkippableFact]
        public void TestDropCollectionWriteConcern()
        {
            RequireServer.Check().Supports(Feature.CommandsThatWriteAcceptWriteConcern).ClusterType(ClusterType.ReplicaSet);
            var subject = _database;
            var writeConcern = new WriteConcern(9);

            var exception = Record.Exception(() => subject.WithWriteConcern(writeConcern).DropCollection("collection"));

            exception.Should().BeOfType<MongoWriteConcernException>();
        }

        [Fact]
        public void TestEvalNoArgs()
        {
            if (!DriverTestConfiguration.Client.Settings.Credentials.Any())
            {
                var code = "function() { return 1; }";
#pragma warning disable 618
                var result = _database.Eval(code);
#pragma warning restore
                Assert.Equal(1, result.ToInt32());
            }
        }

        [Fact]
        public void TestEvalNoArgsNoLock()
        {
            if (!DriverTestConfiguration.Client.Settings.Credentials.Any())
            {
                var code = "function() { return 1; }";
#pragma warning disable 618
                var result = _adminDatabase.Eval(EvalFlags.NoLock, code);
#pragma warning restore
                Assert.Equal(1, result.ToInt32());
            }
        }

        [Fact]
        public void TestEvalWithMaxTime()
        {
            if (!DriverTestConfiguration.Client.Settings.Credentials.Any())
            {
                if (_primary.Supports(FeatureId.MaxTime))
                {
                    using (var failpoint = new FailPoint(FailPointName.MaxTimeAlwaysTimeout, _server, _primary))
                    {
                        if (failpoint.IsSupported())
                        {
                            failpoint.SetAlwaysOn();
                            var args = new EvalArgs
                            {
                                Code = "return 0;",
                                MaxTime = TimeSpan.FromMilliseconds(1)
                            };
                            Assert.Throws<MongoExecutionTimeoutException>(() => _adminDatabase.Eval(args));
                        }
                    }
                }
            }
        }

        [Fact]
        public void TestEvalWithOneArg()
        {
            if (!DriverTestConfiguration.Client.Settings.Credentials.Any())
            {
                var code = "function(x) { return x + 1; }";
#pragma warning disable 618
                var result = _adminDatabase.Eval(code, 1);
#pragma warning restore
                Assert.Equal(2, result.ToInt32());
            }
        }

        [Fact]
        public void TestEvalWithOneArgNoLock()
        {
            if (!DriverTestConfiguration.Client.Settings.Credentials.Any())
            {
                var code = "function(x) { return x + 1; }";
#pragma warning disable 618
                var result = _adminDatabase.Eval(EvalFlags.NoLock, code, 1);
#pragma warning restore
                Assert.Equal(2, result.ToInt32());
            }
        }

        [Fact]
        public void TestEvalWithTwoArgs()
        {
            if (!DriverTestConfiguration.Client.Settings.Credentials.Any())
            {
                var code = "function(x, y) { return x / y; }";
#pragma warning disable 618
                var result = _adminDatabase.Eval(code, 6, 2);
#pragma warning restore
                Assert.Equal(3, result.ToInt32());
            }
        }

        [Fact]
        public void TestEvalWithTwoArgsNoLock()
        {
            if (!DriverTestConfiguration.Client.Settings.Credentials.Any())
            {
                var code = "function(x, y) { return x / y; }";
#pragma warning disable 618
                var result = _adminDatabase.Eval(EvalFlags.NoLock, code, 6, 2);
#pragma warning restore
                Assert.Equal(3, result.ToInt32());
            }
        }

        [Fact]
        public void TestFetchDBRef()
        {
            var collectionName = "testdbref";
            var collection = _database.GetCollection(collectionName);
            var document = new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "P", "x" } };
            collection.Insert(document);

            var dbRef = new MongoDBRef(collectionName, document["_id"].AsObjectId);
            var fetched = _database.FetchDBRef(dbRef);
            Assert.Equal(document, fetched);
            Assert.Equal(document.ToJson(), fetched.ToJson());

            var dbRefWithDatabaseName = new MongoDBRef(_database.Name, collectionName, document["_id"].AsObjectId);
            fetched = _server.FetchDBRef(dbRefWithDatabaseName);
            Assert.Equal(document, fetched);
            Assert.Equal(document.ToJson(), fetched.ToJson());
            Assert.Throws<ArgumentException>(() => { _server.FetchDBRef(dbRef); });
        }

        [Fact]
        public void TestGetCollection()
        {
            var collectionName = LegacyTestConfiguration.Collection.Name;
            var collection = _database.GetCollection(typeof(BsonDocument), collectionName);
            Assert.Same(_database, collection.Database);
            Assert.Equal(_database.Name + "." + collectionName, collection.FullName);
            Assert.Equal(collectionName, collection.Name);
            Assert.Equal(_database.Settings.WriteConcern, collection.Settings.WriteConcern);
        }

        [Fact]
        public void TestGetCollectionGeneric()
        {
            var collectionName = LegacyTestConfiguration.Collection.Name;
            var collection = _database.GetCollection(collectionName);
            Assert.Same(_database, collection.Database);
            Assert.Equal(_database.Name + "." + collectionName, collection.FullName);
            Assert.Equal(collectionName, collection.Name);
            Assert.Equal(_database.Settings.WriteConcern, collection.Settings.WriteConcern);
        }

        [Fact]
        public void TestGetCollectionNames()
        {
            var databaseNamespace = CoreTestConfiguration.GetDatabaseNamespaceForTestClass(typeof(MongoDatabaseTests));
            var database = _server.GetDatabase(databaseNamespace.DatabaseName);
            database.Drop();
            database.GetCollection("a").Insert(new BsonDocument("a", 1));
            database.GetCollection("b").Insert(new BsonDocument("b", 1));
            database.GetCollection("c").Insert(new BsonDocument("c", 1));
            var collectionNames = database.GetCollectionNames();
            Assert.Equal(new[] { "a", "b", "c" }, collectionNames.Where(n => n != "system.indexes"));
        }

        [SkippableFact]
        public void TestGetCurrentOp()
        {
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);
            var adminDatabase = _server.GetDatabase("admin");
            var currentOp = adminDatabase.GetCurrentOp();
            Assert.Equal("inprog", currentOp.GetElement(0).Name);
        }

        [Fact]
        public void TestGetProfilingInfo()
        {
            if (_primary.InstanceType != MongoServerInstanceType.ShardRouter)
            {
                var collection = LegacyTestConfiguration.Collection;
                if (collection.Exists()) { collection.Drop(); }
                collection.Insert(new BsonDocument("x", 1));
                _database.SetProfilingLevel(ProfilingLevel.All);
                collection.Count();
                _database.SetProfilingLevel(ProfilingLevel.None);
                var info = _database.GetProfilingInfo(Query.Null).SetSortOrder(SortBy.Descending("$natural")).SetLimit(1).First();
                Assert.True(info.Timestamp >= new DateTime(2011, 10, 6, 0, 0, 0, DateTimeKind.Utc));
                Assert.True(info.Duration >= TimeSpan.Zero);
            }
        }

        [Fact]
        public void TestIsCollectionNameValid()
        {
            string message;
            Assert.Throws<ArgumentNullException>(() => { _database.IsCollectionNameValid(null, out message); });
            Assert.False(_database.IsCollectionNameValid("", out message));
            Assert.False(_database.IsCollectionNameValid("a\0b", out message));
            Assert.False(_database.IsCollectionNameValid(new string('x', 128), out message));
        }

        [Fact]
        public void TestRenameCollection()
        {
            var collectionName1 = "testrenamecollection1";
            var collectionName2 = "testrenamecollection2";
            Assert.False(_database.CollectionExists(collectionName1));
            Assert.False(_database.CollectionExists(collectionName2));

            _database.GetCollection(collectionName1).Insert(new BsonDocument());
            Assert.True(_database.CollectionExists(collectionName1));
            Assert.False(_database.CollectionExists(collectionName2));

            _database.RenameCollection(collectionName1, collectionName2);
            Assert.False(_database.CollectionExists(collectionName1));
            Assert.True(_database.CollectionExists(collectionName2));
        }

        [Fact]
        public void TestRenameCollectionArgumentChecking()
        {
            Assert.Throws<ArgumentNullException>(() => { _database.RenameCollection(null, "new"); });
            Assert.Throws<ArgumentNullException>(() => { _database.RenameCollection("old", null); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { _database.RenameCollection("old", ""); });
        }

        [Fact]
        public void TestRenameCollectionDropTarget()
        {
            const string collectionName1 = "testrenamecollectiondroptarget1";
            const string collectionName2 = "testrenamecollectiondroptarget2";
            Assert.False(_database.CollectionExists(collectionName1));
            Assert.False(_database.CollectionExists(collectionName2));

            _database.GetCollection(collectionName1).Insert(new BsonDocument());
            _database.GetCollection(collectionName2).Insert(new BsonDocument());
            Assert.True(_database.CollectionExists(collectionName1));
            Assert.True(_database.CollectionExists(collectionName2));

            Assert.Throws<MongoCommandException>(() => _database.RenameCollection(collectionName1, collectionName2));
            _database.RenameCollection(collectionName1, collectionName2, true);
            Assert.False(_database.CollectionExists(collectionName1));
            Assert.True(_database.CollectionExists(collectionName2));
        }

        [SkippableFact]
        public void TestRenameCollectionWriteConcern()
        {
            RequireServer.Check().Supports(Feature.CommandsThatWriteAcceptWriteConcern).ClusterType(ClusterType.ReplicaSet);
            var oldCollectionName = "oldcollectioname";
            var newCollectionName = "newcollectioname";
            EnsureCollectionExists(oldCollectionName);
            EnsureCollectionDoesNotExist(newCollectionName);
            var subject = _database;
            var writeConcern = new WriteConcern(9);

            var exception = Record.Exception(() => subject.WithWriteConcern(writeConcern).RenameCollection(oldCollectionName, newCollectionName));

            exception.Should().BeOfType<MongoWriteConcernException>();
        }

        [Fact]
        public void TestSetProfilingLevel()
        {
            if (_primary.InstanceType != MongoServerInstanceType.ShardRouter)
            {
                _database.SetProfilingLevel(ProfilingLevel.None, TimeSpan.FromMilliseconds(100));
                var result = _database.GetProfilingLevel();
                Assert.Equal(ProfilingLevel.None, result.Level);
                Assert.Equal(TimeSpan.FromMilliseconds(100), result.Slow);

                _database.SetProfilingLevel(ProfilingLevel.Slow);
                result = _database.GetProfilingLevel();
                Assert.Equal(ProfilingLevel.Slow, result.Level);
                Assert.Equal(TimeSpan.FromMilliseconds(100), result.Slow);

                _database.SetProfilingLevel(ProfilingLevel.Slow, TimeSpan.FromMilliseconds(200));
                result = _database.GetProfilingLevel();
                Assert.Equal(ProfilingLevel.Slow, result.Level);
                Assert.Equal(TimeSpan.FromMilliseconds(200), result.Slow);

                _database.SetProfilingLevel(ProfilingLevel.Slow, TimeSpan.FromMilliseconds(100));
                result = _database.GetProfilingLevel();
                Assert.Equal(ProfilingLevel.Slow, result.Level);
                Assert.Equal(TimeSpan.FromMilliseconds(100), result.Slow);

                _database.SetProfilingLevel(ProfilingLevel.All);
                result = _database.GetProfilingLevel();
                Assert.Equal(ProfilingLevel.All, result.Level);
                Assert.Equal(TimeSpan.FromMilliseconds(100), result.Slow);

                _database.SetProfilingLevel(ProfilingLevel.None);
                result = _database.GetProfilingLevel();
                Assert.Equal(ProfilingLevel.None, result.Level);
                Assert.Equal(TimeSpan.FromMilliseconds(100), result.Slow);
            }
        }

        [Theory]
        [InlineData("user1", "pass1", true)]
        [InlineData("user2", "pass2", false)]
        public void TestUserMethods(string username, string password, bool isReadOnly)
        {
#pragma warning disable 618
            bool usesCommands = _primary.Supports(FeatureId.UserManagementCommands);
            if (usesCommands)
            {
                _database.RunCommand("dropAllUsersFromDatabase");
            }
            else
            {
                var collection = _database.GetCollection("system.users");
                collection.RemoveAll();
            }

            _database.AddUser(new MongoUser(username, new PasswordEvidence(password), isReadOnly));

            var user = _database.FindUser(username);
            Assert.NotNull(user);
            Assert.Equal(username, user.Username);
            if (!usesCommands)
            {
                Assert.Equal(MongoUtils.Hash(string.Format("{0}:mongo:{1}", username, password)), user.PasswordHash);
                Assert.Equal(isReadOnly, user.IsReadOnly);
            }

            var users = _database.FindAllUsers();
            Assert.Equal(1, users.Length);
            Assert.Equal(username, users[0].Username);
            if (!usesCommands)
            {
                Assert.Equal(MongoUtils.Hash(string.Format("{0}:mongo:{1}", username, password)), users[0].PasswordHash);
                Assert.Equal(isReadOnly, users[0].IsReadOnly);
            }

            // test updating existing user
            _database.AddUser(new MongoUser(username, new PasswordEvidence("newpassword"), !isReadOnly));
            user = _database.FindUser(username);
            Assert.NotNull(user);
            Assert.Equal(username, user.Username);
            if (!usesCommands)
            {
                Assert.Equal(MongoUtils.Hash(string.Format("{0}:mongo:{1}", username, "newpassword")), user.PasswordHash);
                Assert.Equal(!isReadOnly, user.IsReadOnly);
            }

            _database.RemoveUser(user);
            user = _database.FindUser(username);
            Assert.Null(user);
#pragma warning restore
        }

        [Fact]
        public void TestWithReadConcern()
        {
            var originalReadConcern = new ReadConcern(ReadConcernLevel.Linearizable);
            var subject = _database.WithReadConcern(originalReadConcern);
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
            var subject = _database.WithReadPreference(originalReadPreference);
            var newReadPReference = new ReadPreference(ReadPreferenceMode.SecondaryPreferred);

            var result = subject.WithReadPreference(newReadPReference);

            subject.Settings.ReadPreference.Should().BeSameAs(originalReadPreference);
            result.Settings.ReadPreference.Should().BeSameAs(newReadPReference);
            result.WithReadPreference(originalReadPreference).Settings.Should().Be(subject.Settings);
        }

        [Fact]
        public void TestWithWriteConcern()
        {
            var originalWriteConcern = new WriteConcern(2);
            var subject = _database.WithWriteConcern(originalWriteConcern);
            var newWriteConcern = new WriteConcern(3);

            var result = subject.WithWriteConcern(newWriteConcern);

            subject.Settings.WriteConcern.Should().BeSameAs(originalWriteConcern);
            result.Settings.WriteConcern.Should().BeSameAs(newWriteConcern);
            result.WithWriteConcern(originalWriteConcern).Settings.Should().Be(subject.Settings);
        }

        // private methods
        private void EnsureCollectionDoesNotExist(string collectionName)
        {
            _database.DropCollection(collectionName);
        }

        private void EnsureCollectionExists(string collectionName)
        {
            _database.DropCollection(collectionName);
            _database.CreateCollection(collectionName);
        }
    }
}