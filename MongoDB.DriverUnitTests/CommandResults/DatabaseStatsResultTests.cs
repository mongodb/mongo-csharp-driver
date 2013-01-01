/* Copyright 2010-2013 10gen Inc.
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
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.CommandResults
{
    [TestFixture]
    public class DatabaseStatsResultTests
    {
        private MongoServer _server;
        private MongoDatabase _database;
        private MongoCollection<BsonDocument> _collection;

        [TestFixtureSetUp]
        public void Setup()
        {
            _server = Configuration.TestServer;
            _database = Configuration.TestDatabase;
            _collection = Configuration.TestCollection;
        }

        [Test]
        public void Test()
        {
            using (_database.RequestStart())
            {
                var instance = _server.RequestConnection.ServerInstance;
                if (instance.InstanceType != MongoServerInstanceType.ShardRouter)
                {
                    // make sure collection and database exist
                    _collection.Insert(new BsonDocument());

                    var result = _database.GetStats();
                    Assert.IsTrue(result.Ok);
                    Assert.IsTrue(result.AverageObjectSize > 0);
                    Assert.IsTrue(result.CollectionCount > 0);
                    Assert.IsTrue(result.DataSize > 0);
                    Assert.IsTrue(result.ExtentCount > 0);
                    Assert.IsTrue(result.FileSize > 0);
                    Assert.IsTrue(result.IndexCount > 0);
                    Assert.IsTrue(result.IndexSize > 0);
                    Assert.IsTrue(result.ObjectCount > 0);
                    Assert.IsTrue(result.StorageSize > 0);
                }
            }
        }
    }
}
