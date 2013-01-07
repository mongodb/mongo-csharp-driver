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

using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.CommandResults
{
    [TestFixture]
    public class CollectionStatsResultTests
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
#pragma warning disable 618
            Assert.AreEqual(1, result.Flags);
#pragma warning restore
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
            Assert.AreEqual(CollectionUserFlags.None, result.UserFlags);
        }
    }
}
