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
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests;

public class AtClusterTimeTests : IntegrationTest<AtClusterTimeTests.ClassFixture>
{
    public AtClusterTimeTests(ClassFixture fixture)
        : base(fixture, server => server.Supports(Feature.SnapshotReads).ClusterType(ClusterType.ReplicaSet))
    {
    }

    [Fact]
    public void MainTest()
    {
        var client = Fixture.Client;
        var collection = Fixture.Collection;

        BsonTimestamp clusterTime1;

        var sessionOptions1 = new ClientSessionOptions
        {
            Snapshot = true
        };

        using (var session1 = client.StartSession(sessionOptions1))
        {
            var results = GetTestObjects(collection, session1);
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
            var results = GetTestObjects(collection, session2);
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
            var results = GetTestObjects(collection, session3);
            AssertTwoObjs(results);

            var clusterTime3 = session3.GetSnapshotTime();
            Assert.NotEqual(clusterTime3, clusterTime1);
        }
    }

    [Fact]
    public void IncreasedTimestamp()
    {
        var client = Fixture.Client;
        var collection = Fixture.Collection;

        BsonTimestamp clusterTime1;

        var sessionOptions1 = new ClientSessionOptions
        {
            Snapshot = true
        };

        using (var session1 = client.StartSession(sessionOptions1))
        {
            var results = GetTestObjects(collection, session1);
            AssertOneObj(results);

            clusterTime1 = session1.GetSnapshotTime();
            Assert.NotEqual(null, clusterTime1);
        }

        var obj2 = new TestObject { Name = "obj2" };
        collection.InsertOne(obj2);

        var modifiedClusterTime = new BsonTimestamp(clusterTime1.Value + 1);
        var sessionOptions2 = new ClientSessionOptions
        {
            Snapshot = true,
            SnapshotTime = modifiedClusterTime
        };

        //Snapshot read session at clusterTime1+1 should see obj2
        using (var session2 = client.StartSession(sessionOptions2))
        {
            var results = GetTestObjects(collection, session2);
            AssertTwoObjs(results);

            var clusterTime2 = session2.GetSnapshotTime();
            Assert.Equal(modifiedClusterTime, clusterTime2);
        }
    }

    [Fact]
    public void DecreasedTimestamp()
    {
        var client = Fixture.Client;
        var collection = Fixture.Collection;

        BsonTimestamp clusterTime1;

        var sessionOptions1 = new ClientSessionOptions
        {
            Snapshot = true
        };

        using (var session1 = client.StartSession(sessionOptions1))
        {
            var results = GetTestObjects(collection, session1);
            AssertOneObj(results);

            clusterTime1 = session1.GetSnapshotTime();
            Assert.NotEqual(null, clusterTime1);
        }

        var obj2 = new TestObject { Name = "obj2" };
        collection.InsertOne(obj2);

        var modifiedClusterTime = new BsonTimestamp(clusterTime1.Value - 1);
        var sessionOptions2 = new ClientSessionOptions
        {
            Snapshot = true,
            SnapshotTime = modifiedClusterTime
        };

        //Snapshot read session at clusterTime1-1 should not see obj2
        using (var session2 = client.StartSession(sessionOptions2))
        {
            var results = GetTestObjects(collection, session2);
            Assert.Equal(0, results.Count);

            var clusterTime2 = session2.GetSnapshotTime();
            Assert.Equal(modifiedClusterTime, clusterTime2);
        }
    }

    List<TestObject> GetTestObjects(IMongoCollection<TestObject> collection, IClientSessionHandle session)
    {
        var filterDefinition = Builders<TestObject>.Filter.Empty;
        var sortDefinition = Builders<TestObject>.Sort.Ascending(o => o.Name);
        return collection.Find(session, filterDefinition).Sort(sortDefinition).ToList();
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

    public class ClassFixture : MongoCollectionFixture<TestObject>
    {
        public override bool InitializeDataBeforeEachTestCase => true;
        protected override IEnumerable<TestObject> InitialData => [new() { Name = "obj1" }] ;
    }

    public class TestObject
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string Name { get; set; }
    }
}