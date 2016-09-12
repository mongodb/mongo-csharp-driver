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

using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.CommandResults
{
    public class DatabaseStatsResultTests
    {
        private MongoServer _server;
        private MongoDatabase _database;
        private MongoCollection<BsonDocument> _collection;

        public DatabaseStatsResultTests()
        {
            _server = LegacyTestConfiguration.Server;
            _database = LegacyTestConfiguration.Database;
            _collection = LegacyTestConfiguration.Collection;
        }

        [SkippableFact]
        public void Test()
        {
            RequireServer.Check().StorageEngine("mmapv1");
            if (_server.Primary.InstanceType != MongoServerInstanceType.ShardRouter)
            {
                // make sure collection and database exist
                _collection.Insert(new BsonDocument());

                var result = _database.GetStats();
                Assert.True(result.Ok);
                Assert.True(result.AverageObjectSize > 0);
                Assert.True(result.CollectionCount > 0);
                Assert.True(result.DataSize > 0);
                Assert.True(result.ExtentCount > 0);
                Assert.True(result.FileSize > 0);
                Assert.True(result.IndexCount > 0);
                Assert.True(result.IndexSize > 0);
                Assert.True(result.ObjectCount > 0);
                Assert.True(result.StorageSize > 0);
            }
        }
    }
}
