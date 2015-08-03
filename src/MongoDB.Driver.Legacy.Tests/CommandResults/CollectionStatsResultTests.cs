/* Copyright 2010-2015 MongoDB Inc.
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
using MongoDB.Driver;
using MongoDB.Driver.Core;
using NUnit.Framework;

namespace MongoDB.Driver.Tests.CommandResults
{
    [TestFixture]
    public class CollectionStatsResultTests
    {
        private MongoServer _server;
        private MongoCollection<BsonDocument> _collection;

        [TestFixtureSetUp]
        public void Setup()
        {
            _server = LegacyTestConfiguration.Server;
            _collection = LegacyTestConfiguration.Collection;
        }

        [Test]
        [RequiresServer(StorageEngines = "mmapv1", ClusterTypes = ClusterTypes.StandaloneOrReplicaSet)]
        public void Test()
        {
            // make sure collection exists and has exactly one document
            _collection.Drop();
            _collection.Insert(new BsonDocument());

            var result = _collection.GetStats();
            Assert.IsTrue(result.Ok);
            Assert.AreEqual(_collection.FullName, result.Namespace);
            Assert.AreEqual(1, result.ObjectCount);
            Assert.IsTrue(result.AverageObjectSize > 0.0);
            Assert.IsTrue(result.DataSize > 0);
            Assert.IsTrue(result.ExtentCount > 0);
            Assert.IsTrue(result.IndexCount > 0);
            Assert.IsTrue(result.IndexSizes["_id_"] > 0);
            Assert.IsTrue(result.IndexSizes.ContainsKey("_id_"));
            Assert.IsTrue(result.IndexSizes.Count > 0);
            Assert.IsTrue(result.IndexSizes.Keys.Contains("_id_"));
            Assert.IsTrue(result.IndexSizes.Values.Count() > 0);
            Assert.IsTrue(result.IndexSizes.Values.First() > 0);
            Assert.IsFalse(result.IsCapped);
            Assert.IsTrue(result.LastExtentSize > 0);
            Assert.IsFalse(result.Response.Contains("max"));
            Assert.AreEqual(_collection.FullName, result.Namespace);
            Assert.AreEqual(1, result.ObjectCount);
            Assert.IsTrue(result.PaddingFactor > 0.0);
            Assert.IsTrue(result.StorageSize > 0);
            Assert.AreEqual(CollectionSystemFlags.HasIdIndex, result.SystemFlags);
            Assert.IsTrue(result.TotalIndexSize > 0);
            if (_server.BuildInfo.Version < new Version(2, 6, 0))
            {
                Assert.AreEqual(CollectionUserFlags.None, result.UserFlags);
            }
            else
            {
                Assert.AreEqual(CollectionUserFlags.UsePowerOf2Sizes, result.UserFlags);
            }
        }
    }
}