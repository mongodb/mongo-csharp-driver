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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.GeoJsonObjectModel;
using Xunit;

namespace MongoDB.Driver.Tests.Builders
{
    public class QueryBuilderGeoJsonIntegrationTests
    {
        private MongoCollection<GeoClass> _collection;

        private class GeoClass
        {
            public int Id { get; set; }

            [BsonElement("loc")]
            public GeoJsonPoint<GeoJson2DGeographicCoordinates> Location { get; set; }

            [BsonElement("sur")]
            public GeoJsonPolygon<GeoJson2DGeographicCoordinates> Surrounding { get; set; }
        }

        public QueryBuilderGeoJsonIntegrationTests()
        {
            var db = LegacyTestConfiguration.Database;
            _collection = db.GetCollection<GeoClass>("geo");

            _collection.Drop();
            _collection.CreateIndex(IndexKeys<GeoClass>.GeoSpatialSpherical(x => x.Location));
            _collection.CreateIndex(IndexKeys<GeoClass>.GeoSpatialSpherical(x => x.Surrounding));

            var doc = new GeoClass
            {
                Id = 1,
                Location = GeoJson.Point(GeoJson.Geographic(40.5, 18.5)),
                Surrounding = GeoJson.Polygon(
                    GeoJson.Geographic(40, 18),
                    GeoJson.Geographic(40, 19),
                    GeoJson.Geographic(41, 19),
                    GeoJson.Geographic(40, 18))
            };

            _collection.Save(doc);
        }

        [Fact]
        public void TestGeoIntersects()
        {
            var server = LegacyTestConfiguration.Server;
            server.Connect();
            if (server.BuildInfo.Version >= new Version(2, 4, 0))
            {
                var point = GeoJson.Point(GeoJson.Geographic(40, 18));

                var query = Query<GeoClass>.GeoIntersects(x => x.Surrounding, point);

                var results = _collection.Count(query);

                Assert.Equal(1, results);
            }
        }

        [Fact]
        public void TestNear()
        {
            var server = LegacyTestConfiguration.Server;
            server.Connect();
            if (server.BuildInfo.Version >= new Version(2, 4, 0))
            {
                var point = GeoJson.Point(GeoJson.Geographic(40, 18));

                var query = Query<GeoClass>.Near(x => x.Location, point);

                var results = _collection.Count(query);

                Assert.Equal(1, results);
            }
        }

        [Fact]
        public void TestWithin()
        {
            var server = LegacyTestConfiguration.Server;
            server.Connect();
            if (server.BuildInfo.Version >= new Version(2, 4, 0))
            {
                var polygon = GeoJson.Polygon(
                        GeoJson.Geographic(40, 18),
                        GeoJson.Geographic(40, 19),
                        GeoJson.Geographic(41, 19),
                        GeoJson.Geographic(41, 18),
                        GeoJson.Geographic(40, 18));

                var query = Query<GeoClass>.Within(x => x.Location, polygon);

                var results = _collection.Count(query);

                Assert.Equal(1, results);
            }
        }

        [Fact]
        public void TestWithinNotFound()
        {
            var server = LegacyTestConfiguration.Server;
            server.Connect();
            if (server.BuildInfo.Version >= new Version(2, 4, 0))
            {
                var polygon = GeoJson.Polygon(
                        GeoJson.Geographic(41, 19),
                        GeoJson.Geographic(41, 20),
                        GeoJson.Geographic(42, 20),
                        GeoJson.Geographic(42, 19),
                        GeoJson.Geographic(41, 19));

                var query = Query<GeoClass>.Within(x => x.Location, polygon);

                var results = _collection.Count(query);

                Assert.Equal(0, results);
            }
        }
    }
}