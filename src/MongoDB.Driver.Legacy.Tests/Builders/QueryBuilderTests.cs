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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.GeoJsonObjectModel;
using NUnit.Framework;

namespace MongoDB.Driver.Tests.Builders
{
    [TestFixture]
    public class QueryBuilderTests
    {
        private MongoServer _server;
        private MongoDatabase _database;
        private MongoServerInstance _primary;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            _server = LegacyTestConfiguration.Server;
            _database = LegacyTestConfiguration.Database;
            _primary = _server.Primary;
        }

        [Test]
        public void TestNear()
        {
            var query = Query.Near("loc", 1.1, 2.2);
            var selector = "{ '$near' : [1.1, 2.2] }";
            Assert.AreEqual(PositiveTest("loc", selector), query.ToJson());

            var collection = LegacyTestConfiguration.Collection;
            collection.Drop();
            collection.CreateIndex(IndexKeys.GeoSpatial("loc"));
            collection.Insert(new BsonDocument { { "_id", 1 }, { "loc", new BsonArray { 1, 1 } } });
            collection.Insert(new BsonDocument { { "_id", 2 }, { "loc", new BsonArray { 2, 2 } } });

            query = Query.Near("loc", 0.0, 0.0);
            var results = collection.Find(query).ToList();
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(1, results[0]["_id"].ToInt32());
            Assert.AreEqual(2, results[1]["_id"].ToInt32());
        }

        [Test]
        public void TestNearWithMaxDistance()
        {
            var query = Query.Near("loc", 1.1, 2.2, 3.3);
            var expected = "{ 'loc' : { '$near' : [1.1, 2.2], '$maxDistance' : 3.3 } }".Replace("'", "\"");
            Assert.AreEqual(expected, query.ToJson());

            var collection = LegacyTestConfiguration.Collection;
            collection.Drop();
            collection.CreateIndex(IndexKeys.GeoSpatial("loc"));
            collection.Insert(new BsonDocument { { "_id", 1 }, { "loc", new BsonArray { 1, 1 } } });
            collection.Insert(new BsonDocument { { "_id", 2 }, { "loc", new BsonArray { 2, 2 } } });

            query = Query.Near("loc", 0.0, 0.0, 2.0);
            var results = collection.Find(query).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0]["_id"].ToInt32());
        }

        [Test]
        public void TestNearWithSphericalTrue()
        {
            var query = Query.Near("loc", 1.1, 2.2, 3.3, true);
            var expected = "{ 'loc' : { '$nearSphere' : [1.1, 2.2], '$maxDistance' : 3.3 } }".Replace("'", "\"");
            Assert.AreEqual(expected, query.ToJson());

            var collection = LegacyTestConfiguration.Collection;
            collection.Drop();
            collection.CreateIndex(IndexKeys.GeoSpatial("loc"));
            collection.Insert(new BsonDocument { { "_id", 1 }, { "loc", new BsonArray { 1, 1 } } });
            collection.Insert(new BsonDocument { { "_id", 2 }, { "loc", new BsonArray { 2, 2 } } });

            var radiansPerDegree = 2 * Math.PI / 360.0;
            query = Query.Near("loc", 0.0, 0.0, 2.0 * radiansPerDegree, true);
            var results = collection.Find(query).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0]["_id"].ToInt32());
        }

        [Test]
        public void TestNearWithGeoJson()
        {
            var point = GeoJson.Point(GeoJson.Geographic(40, 18));
            var query = Query.Near("loc", point);
            var selector = "{ '$near' : { '$geometry' : { 'type' : 'Point', 'coordinates' : [40.0, 18.0] } } }";
            Assert.AreEqual(PositiveTest("loc", selector), query.ToJson());

            var collection = LegacyTestConfiguration.Collection;
            collection.Drop();
            collection.CreateIndex(IndexKeys.GeoSpatial("loc"));
            collection.Insert(new BsonDocument { { "_id", 1 }, { "loc", new BsonArray { 1, 1 } } });
            collection.Insert(new BsonDocument { { "_id", 2 }, { "loc", new BsonArray { 2, 2 } } });

            query = Query.Near("loc", 0.0, 0.0);
            var results = collection.Find(query).ToList();
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(1, results[0]["_id"].ToInt32());
            Assert.AreEqual(2, results[1]["_id"].ToInt32());
        }

        [Test]
        public void TestNearWithGeoJsonWithMaxDistance()
        {
            if (_primary.Supports(FeatureId.GeoJson))
            {
                var point = GeoJson.Point(GeoJson.Geographic(40, 18));
                var query = Query.Near("loc", point, 42);
                var expected = "{ 'loc' : { '$near' : { '$geometry' : { 'type' : 'Point', 'coordinates' : [40.0, 18.0] }, '$maxDistance' : 42.0 } } }".Replace("'", "\"");
                Assert.AreEqual(expected, query.ToJson());

                var collection = LegacyTestConfiguration.Collection;
                collection.Drop();
                collection.CreateIndex(IndexKeys.GeoSpatialSpherical("loc"));
                collection.Insert(new BsonDocument { { "_id", 1 }, { "loc", GeoJson.Point(GeoJson.Geographic(1, 1)).ToBsonDocument() } });
                collection.Insert(new BsonDocument { { "_id", 2 }, { "loc", GeoJson.Point(GeoJson.Geographic(2, 2)).ToBsonDocument() } });

                var circumferenceOfTheEarth = 40075000; // meters at the equator, approx
                var metersPerDegree = circumferenceOfTheEarth / 360.0;
                query = Query.Near("loc", GeoJson.Point(GeoJson.Geographic(0, 0)), 2.0 * metersPerDegree);
                var results = collection.Find(query).ToList();
                Assert.AreEqual(1, results.Count);
                Assert.AreEqual(1, results[0]["_id"].ToInt32());
            }
        }

        [Test]
        public void TestNearWithGeoJsonWithSpherical()
        {
            if (_primary.Supports(FeatureId.GeoJson))
            {
                var point = GeoJson.Point(GeoJson.Geographic(40, 18));
                var query = Query.Near("loc", point, 42, true);
                var expected = "{ 'loc' : { '$nearSphere' : { '$geometry' : { 'type' : 'Point', 'coordinates' : [40.0, 18.0] }, '$maxDistance' : 42.0 } } }".Replace("'", "\"");
                Assert.AreEqual(expected, query.ToJson());

                var collection = LegacyTestConfiguration.Collection;
                collection.Drop();
                collection.CreateIndex(IndexKeys.GeoSpatialSpherical("loc"));
                collection.Insert(new BsonDocument { { "_id", 1 }, { "loc", GeoJson.Point(GeoJson.Geographic(1, 1)).ToBsonDocument() } });
                collection.Insert(new BsonDocument { { "_id", 2 }, { "loc", GeoJson.Point(GeoJson.Geographic(2, 2)).ToBsonDocument() } });

                var circumferenceOfTheEarth = 40075000; // meters at the equator, approx
                var metersPerDegree = circumferenceOfTheEarth / 360.0;
                query = Query.Near("loc", GeoJson.Point(GeoJson.Geographic(0, 0)), 2.0 * metersPerDegree, true);
                var results = collection.Find(query).ToList();
                Assert.AreEqual(1, results.Count);
                Assert.AreEqual(1, results[0]["_id"].ToInt32());
            }
        }

 
        [Test]
        public void TestText()
        {
            if (_primary.Supports(FeatureId.TextSearchQuery))
            {
                var collection = _database.GetCollection<BsonDocument>("test_text");
                collection.Drop();
                collection.CreateIndex(IndexKeys.Text("textfield"));
                collection.Insert(new BsonDocument
                {
                    { "_id", 1 },
                    { "textfield", "The quick brown fox" }
                });
                collection.Insert(new BsonDocument
                {
                    { "_id", 2 },
                    { "textfield", "over the lazy brown dog" }
                });
                var query = Query.Text("fox");
                var results = collection.Find(query).ToArray();
                Assert.AreEqual(1, results.Length);
                Assert.AreEqual(1, results[0]["_id"].AsInt32);
            }
        }

        [Test]
        public void TestTextWithLanguage()
        {
            if (_primary.Supports(FeatureId.TextSearchQuery))
            {
                var collection = _database.GetCollection<BsonDocument>("test_text_spanish");
                collection.Drop();
                collection.CreateIndex(IndexKeys.Text("textfield"), IndexOptions.SetTextDefaultLanguage("spanish"));
                collection.Insert(new BsonDocument
                {
                    { "_id", 1 },
                    { "textfield", "este es mi tercer blog stemmed" }
                });
                collection.Insert(new BsonDocument
                {
                    { "_id", 2 },
                    { "textfield", "This stemmed blog is in english" },
                    { "language", "english" }
                });

                var query = Query.Text("stemmed");
                var results = collection.Find(query).ToArray();
                Assert.AreEqual(1, results.Length);
                Assert.AreEqual(1, results[0]["_id"].AsInt32);

                query = Query.Text("stemmed", "english");
                results = collection.Find(query).ToArray();
                Assert.AreEqual(1, results.Length);
                Assert.AreEqual(2, results[0]["_id"].AsInt32);
            }
        }

        private string PositiveTest(string fieldName, string selector)
        {
            return "{ '#fieldName' : #selector }".Replace("#fieldName", fieldName).Replace("#selector", selector).Replace("'", "\"");
        }
    }
}
