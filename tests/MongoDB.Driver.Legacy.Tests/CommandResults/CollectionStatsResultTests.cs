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
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.CommandResults
{
    public class CollectionStatsResultTests
    {
        private MongoServer _server;
        private MongoCollection<BsonDocument> _collection;

        public CollectionStatsResultTests()
        {
            _server = LegacyTestConfiguration.Server;
            _collection = LegacyTestConfiguration.Collection;
        }

        [SkippableFact]
        public void Test()
        {
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet).StorageEngine("mmapv1");
            // make sure collection exists and has exactly one document
            _collection.Drop();
            _collection.Insert(new BsonDocument());

            var result = _collection.GetStats();
            Assert.True(result.Ok);
            Assert.Equal(_collection.FullName, result.Namespace);
            Assert.Equal(1, result.ObjectCount);
            Assert.True(result.AverageObjectSize > 0.0);
            Assert.True(result.DataSize > 0);
            Assert.True(result.ExtentCount > 0);
            Assert.True(result.IndexCount > 0);
            Assert.True(result.IndexSizes["_id_"] > 0);
            Assert.True(result.IndexSizes.ContainsKey("_id_"));
            Assert.True(result.IndexSizes.Count > 0);
            Assert.True(result.IndexSizes.Keys.Contains("_id_"));
            Assert.True(result.IndexSizes.Values.Count() > 0);
            Assert.True(result.IndexSizes.Values.First() > 0);
            Assert.False(result.IsCapped);
            Assert.True(result.LastExtentSize > 0);
            Assert.False(result.Response.Contains("max"));
            Assert.Equal(_collection.FullName, result.Namespace);
            Assert.Equal(1, result.ObjectCount);
            Assert.True(result.PaddingFactor > 0.0);
            Assert.True(result.StorageSize > 0);
            Assert.Equal(CollectionSystemFlags.HasIdIndex, result.SystemFlags);
            Assert.True(result.TotalIndexSize > 0);
            if (_server.BuildInfo.Version < new Version(2, 6, 0))
            {
                Assert.Equal(CollectionUserFlags.None, result.UserFlags);
            }
            else
            {
                Assert.Equal(CollectionUserFlags.UsePowerOf2Sizes, result.UserFlags);
            }
        }
    }
}