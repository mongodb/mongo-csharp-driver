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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests;

[Trait("Category", "Integration")]
public class AtClusterTimeTests
{
    [Fact]
    public void AtClusterTime_should_work()
    {
        RequireServer.Check().ClusterType(ClusterType.ReplicaSet).Supports(Feature.SnapshotReads);
        const string collectionName = "atClusterTimeTests";
        const string databaseName = "testDb";

        using var client = DriverTestConfiguration.Client;
        var database = client.GetDatabase(databaseName);
        database.DropCollection(collectionName);
        var collection = database.GetCollection<TestObject>(collectionName);

        var obj1 = new TestObject { Name = "obj1" };
        collection.InsertOne(obj1);

        BsonTimestamp clusterTime1;

        var filterDefinition = Builders<TestObject>.Filter.Empty;
        var sortDefinition = Builders<TestObject>.Sort.Ascending(o => o.Name);

        var sessionOptions1 = new ClientSessionOptions
        {
            Snapshot = true
        };

        using (var session1 = client.StartSession(sessionOptions1))
        {
            var results = collection.Find(session1, filterDefinition).Sort(sortDefinition).ToList();
            AssertOneObj(results);

            clusterTime1 = session1.GetSnapshotTime();
            Assert.NotEqual(null, clusterTime1);
        }

        var obj2 = new TestObject { Name = "obj2" };
        collection.InsertOne(obj2);

        var sessionOptions2 = new ClientSessionOptions
        {
            Snapshot = true,
            SnapshotTime = clusterTime1
        };

        //Snapshot read session at clusterTime1 should not see obj2
        using (var session2 = client.StartSession(sessionOptions2))
        {
            var results = collection.Find(session2, filterDefinition).Sort(sortDefinition).ToList();
            AssertOneObj(results);

            var clusterTime2 = session2.GetSnapshotTime();
            Assert.Equal(clusterTime2, clusterTime1);
        }

        var sessionOptions3 = new ClientSessionOptions
        {
            Snapshot = true,
        };

        //Snapshot read session without cluster time should see obj2
        using (var session3 = client.StartSession(sessionOptions3))
        {
            var results = collection.Find(session3, filterDefinition).Sort(sortDefinition).ToList();
            AssertTwoObjs(results);

            var clusterTime3 = session3.WrappedCoreSession.SnapshotTime;
            Assert.NotEqual(clusterTime3, clusterTime1);
        }

        void AssertOneObj(List<TestObject> objs)
        {
            Assert.Equal(1, objs.Count);
            Assert.Equal("obj1", objs[0].Name);
        }

        void AssertTwoObjs(List<TestObject> objs)
        {
            Assert.Equal(2, objs.Count);
            Assert.Equal("obj1", objs[0].Name);
            Assert.Equal("obj2", objs[1].Name);
        }
    }

    private class TestObject
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string Name { get; set; }
    }
}