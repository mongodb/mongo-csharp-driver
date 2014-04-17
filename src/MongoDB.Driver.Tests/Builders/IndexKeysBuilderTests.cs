/* Copyright 2010-2014 MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Builders
{
    [TestFixture]
    public class IndexKeysBuilderTests
    {
        private MongoServer _server;
        private MongoDatabase _database;
        private MongoServerInstance _primary;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            _server = Configuration.TestServer;
            _database = Configuration.TestDatabase;
            _primary = _server.Primary;
        }

        [Test]
        public void TestAscending1()
        {
            var keys = IndexKeys.Ascending("a");
            string expected = "{ \"a\" : 1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestAscending2()
        {
            var keys = IndexKeys.Ascending("a", "b");
            string expected = "{ \"a\" : 1, \"b\" : 1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestAscendingAscending()
        {
            var keys = IndexKeys.Ascending("a").Ascending("b");
            string expected = "{ \"a\" : 1, \"b\" : 1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestAscendingDescending()
        {
            var keys = IndexKeys.Ascending("a").Descending("b");
            string expected = "{ \"a\" : 1, \"b\" : -1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestDescending1()
        {
            var keys = IndexKeys.Descending("a");
            string expected = "{ \"a\" : -1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestDescending2()
        {
            var keys = IndexKeys.Descending("a", "b");
            string expected = "{ \"a\" : -1, \"b\" : -1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestDescendingAscending()
        {
            var keys = IndexKeys.Descending("a").Ascending("b");
            string expected = "{ \"a\" : -1, \"b\" : 1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestDescendingDescending()
        {
            var keys = IndexKeys.Descending("a").Descending("b");
            string expected = "{ \"a\" : -1, \"b\" : -1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestGeoSpatial()
        {
            var keys = IndexKeys.GeoSpatial("a");
            string expected = "{ \"a\" : \"2d\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestGeoSpatialAscending()
        {
            var keys = IndexKeys.GeoSpatial("a").Ascending("b");
            string expected = "{ \"a\" : \"2d\", \"b\" : 1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestAscendingGeoSpatial()
        {
            var keys = IndexKeys.Ascending("a").GeoSpatial("b");
            string expected = "{ \"a\" : 1, \"b\" : \"2d\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestGeoSpatialSpherical()
        {
            var keys = IndexKeys.GeoSpatialSpherical("a");
            string expected = "{ \"a\" : \"2dsphere\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestGeoSpatialSphericalAscending()
        {
            var keys = IndexKeys.GeoSpatialSpherical("a").Ascending("b");
            string expected = "{ \"a\" : \"2dsphere\", \"b\" : 1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestAscendingGeoSpatialSpherical()
        {
            var keys = IndexKeys.Ascending("a").GeoSpatialSpherical("b");
            string expected = "{ \"a\" : 1, \"b\" : \"2dsphere\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestHashed()
        {
            var keys = IndexKeys.Hashed("a");
            string expected = "{ \"a\" : \"hashed\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestHashedAscending()
        {
            var keys = IndexKeys.Hashed("a").Ascending("b");
            string expected = "{ \"a\" : \"hashed\", \"b\" : 1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestAscendingHashed()
        {
            var keys = IndexKeys.Ascending("a").Hashed("b");
            string expected = "{ \"a\" : 1, \"b\" : \"hashed\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestText()
        {
            var key = IndexKeys.Text("a");
            string expected = "{ \"a\" : \"text\" }";
            Assert.AreEqual(expected, key.ToJson());
        }

        [Test]
        public void TestTextMultiple()
        {
            var keys = IndexKeys.Text("a", "b");
            string expected = "{ \"a\" : \"text\", \"b\" : \"text\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestTextAll()
        {
            var key = IndexKeys.TextAll();
            string expected = "{ \"$**\" : \"text\" }";
            Assert.AreEqual(expected, key.ToJson());
        }

        [Test]
        public void TestTextCombination()
        {
            var key = IndexKeys.Text("a").Ascending("b");
            string expected = "{ \"a\" : \"text\", \"b\" : 1 }";
            Assert.AreEqual(expected, key.ToJson());
        }

        [Test]
        public void TestTextIndexCreation()
        {
            if (_primary.InstanceType != MongoServerInstanceType.ShardRouter)
            {
                if (_primary.Supports(FeatureId.TextSearchCommand))
                {
                    using (_server.RequestStart(null, _primary))
                    {
                        var collection = _database.GetCollection<BsonDocument>("test_text");
                        collection.Drop();
                        collection.CreateIndex(IndexKeys.Text("a", "b").Ascending("c"), IndexOptions.SetTextLanguageOverride("idioma").SetName("custom").SetTextDefaultLanguage("spanish"));
                        var indexCollection = _database.GetCollection("system.indexes");
                        var result = indexCollection.FindOne(Query.EQ("name", "custom"));
                        Assert.AreEqual("custom", result["name"].AsString);
                        Assert.AreEqual("idioma", result["language_override"].AsString);
                        Assert.AreEqual("spanish", result["default_language"].AsString);
                        Assert.AreEqual(1, result["key"]["c"].AsInt32);
                    }
                }
            }
        }
    }
}