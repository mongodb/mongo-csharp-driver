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
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Builders
{
    public class IndexKeysBuilderTypedTests
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

        private class Test
        {
            [BsonElement("a")]
            public string A { get; set; }

            [BsonElement("b")]
            public string B { get; set; }

            [BsonElement("c")]
            public int C { get; set; }

            [BsonElement("d")]
            public string[] D { get; set; }

            [BsonElement("e")]
            public List<string> E { get; set; }
        }

        [Test]
        public void TestAscending1()
        {
            var keys = IndexKeys<Test>.Ascending(x => x.A);
            string expected = "{ \"a\" : 1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestAscending2()
        {
            var keys = IndexKeys<Test>.Ascending(x => x.A, x => x.B);
            string expected = "{ \"a\" : 1, \"b\" : 1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestAscendingAscending()
        {
            var keys = IndexKeys<Test>.Ascending(x => x.A).Ascending(x => x.B);
            string expected = "{ \"a\" : 1, \"b\" : 1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestAscendingDescending()
        {
            var keys = IndexKeys<Test>.Ascending(x => x.A).Descending(x => x.B);
            string expected = "{ \"a\" : 1, \"b\" : -1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestDescending1()
        {
            var keys = IndexKeys<Test>.Descending(x => x.A);
            string expected = "{ \"a\" : -1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestDescending2()
        {
            var keys = IndexKeys<Test>.Descending(x => x.A, x => x.B);
            string expected = "{ \"a\" : -1, \"b\" : -1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestDescendingAscending()
        {
            var keys = IndexKeys<Test>.Descending(x => x.A).Ascending(x => x.B);
            string expected = "{ \"a\" : -1, \"b\" : 1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestDescendingDescending()
        {
            var keys = IndexKeys<Test>.Descending(x => x.A).Descending(x => x.B);
            string expected = "{ \"a\" : -1, \"b\" : -1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestGeoSpatial()
        {
            var keys = IndexKeys<Test>.GeoSpatial(x => x.A);
            string expected = "{ \"a\" : \"2d\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestGeoSpatialAscending()
        {
            var keys = IndexKeys<Test>.GeoSpatial(x => x.A).Ascending(x => x.B);
            string expected = "{ \"a\" : \"2d\", \"b\" : 1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestAscendingGeoSpatial()
        {
            var keys = IndexKeys<Test>.Ascending(x => x.A).GeoSpatial(x => x.B);
            string expected = "{ \"a\" : 1, \"b\" : \"2d\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestGeoSpatialSpherical()
        {
            var keys = IndexKeys<Test>.GeoSpatialSpherical(x => x.A);
            string expected = "{ \"a\" : \"2dsphere\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestGeoSpatialSphericalAscending()
        {
            var keys = IndexKeys<Test>.GeoSpatialSpherical(x => x.A).Ascending(x => x.B);
            string expected = "{ \"a\" : \"2dsphere\", \"b\" : 1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestAscendingGeoSpatialSpherical()
        {
            var keys = IndexKeys<Test>.Ascending(x => x.A).GeoSpatialSpherical(x => x.B);
            string expected = "{ \"a\" : 1, \"b\" : \"2dsphere\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestHashed()
        {
            var keys = IndexKeys<Test>.Hashed(x => x.A);
            string expected = "{ \"a\" : \"hashed\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestHashedAscending()
        {
            var keys = IndexKeys<Test>.Hashed(x => x.A).Ascending(x => x.B);
            string expected = "{ \"a\" : \"hashed\", \"b\" : 1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestAscendingHashed()
        {
            var keys = IndexKeys<Test>.Ascending(x => x.A).Hashed(x => x.B);
            string expected = "{ \"a\" : 1, \"b\" : \"hashed\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestText()
        {
            var key = IndexKeys<Test>.Text(x => x.A);
            string expected = "{ \"a\" : \"text\" }";
            Assert.AreEqual(expected, key.ToJson());
        }

        [Test]
        public void TestTextMultiple()
        {
            var keys = IndexKeys<Test>.Text(x => x.A, x => x.B);
            string expected = "{ \"a\" : \"text\", \"b\" : \"text\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestTextAll()
        {
            var key = IndexKeys<Test>.TextAll();
            string expected = "{ \"$**\" : \"text\" }";
            Assert.AreEqual(expected, key.ToJson());
        }
        [Test]
        public void TestTextCombination()
        {
            var key = IndexKeys<Test>.Text(x => x.A).Ascending(x => x.B);
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
                        var collection = _database.GetCollection<Test>("test_text");
                        collection.Drop();
                        collection.CreateIndex(IndexKeys<Test>.Text(x => x.A, x => x.B).Ascending(x => x.C), IndexOptions.SetTextLanguageOverride("idioma").SetName("custom").SetTextDefaultLanguage("spanish"));
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

        [Test]
        public void TestTextArrayField()
        {
            var keys = IndexKeys<Test>.Text(x => x.D);
            string expected = "{ \"d\" : \"text\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestTextArrayFields()
        {
            var keys = IndexKeys<Test>.Text(x => x.D).Text(x => x.E);
            string expected = "{ \"d\" : \"text\", \"e\" : \"text\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestTextArrayNonArrayFields()
        {
            var keys = IndexKeys<Test>.Text(x => x.A, x => x.B).Text(x => x.D, x=> x.E);
            string expected = "{ \"a\" : \"text\", \"b\" : \"text\", \"d\" : \"text\", \"e\" : \"text\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestTextArrayNonArrayFields2()
        {
            var keys = IndexKeys<Test>.Text(x => x.A).Text(x => x.E).Text(x => x.D).Text(x => x.B);
            string expected = "{ \"a\" : \"text\", \"e\" : \"text\", \"d\" : \"text\", \"b\" : \"text\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }
    }
}
