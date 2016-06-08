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

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.GeoJsonObjectModel;
using Xunit;

namespace MongoDB.Driver.Tests.GeoJsonObjectModel
{
    public class GeoJsonFeatureTests
    {
        [Fact]
        public void TestExampleFromSpec()
        {
            var feature = GeoJson.Feature(
                new GeoJsonFeatureArgs<GeoJson2DCoordinates> { BoundingBox = GeoJson.BoundingBox(GeoJson.Position(-180.0, -90.0), GeoJson.Position(180.0, 90.0)) },
                GeoJson.Polygon(
                    GeoJson.Position(-180.0, 10.0),
                    GeoJson.Position(20.0, 90.0),
                    GeoJson.Position(180.0, -5.0),
                    GeoJson.Position(-30.0, -90.0),
                    GeoJson.Position(-180.0, 10.0)));

            var boundingBox = "[-180.0, -90.0, 180.0, 90.0]";
            var polygon = "{ 'type' : 'Polygon', 'coordinates' : [[[-180.0, 10.0], [20.0, 90.0], [180.0, -5.0], [-30.0, -90.0], [-180.0, 10.0]]] }";
            var expected = "{ 'type' : 'Feature', 'bbox' : #bbox, 'geometry' : #polygon }".Replace("#bbox", boundingBox).Replace("#polygon", polygon).Replace("'", "\"");
            TestRoundTrip(expected, feature);
        }

        [Fact]
        public void TestFeatureWithId()
        {
            var feature = GeoJson.Feature(
                new GeoJsonFeatureArgs<GeoJson2DCoordinates> { Id = 1 },
                GeoJson.Point(GeoJson.Position(1.0, 2.0)));

            var expected = "{ 'type' : 'Feature', 'geometry' : { 'type' : 'Point', 'coordinates' : [1.0, 2.0] }, 'id' : 1 }".Replace("'", "\"");
            TestRoundTrip(expected, feature);
        }

        [Fact]
        public void TestFeatureWithProperties()
        {
            var feature = GeoJson.Feature(
                new GeoJsonFeatureArgs<GeoJson2DCoordinates> { Properties = new BsonDocument("x", 1) },
                GeoJson.Point(GeoJson.Position(1.0, 2.0)));

            var expected = "{ 'type' : 'Feature', 'geometry' : { 'type' : 'Point', 'coordinates' : [1.0, 2.0] }, 'properties' : { 'x' : 1 } }".Replace("'", "\"");
            TestRoundTrip(expected, feature);
        }

        private void TestRoundTrip<TCoordinates>(string expected, GeoJsonFeature<TCoordinates> feature) where TCoordinates : GeoJsonCoordinates
        {
            var json = feature.ToJson();
            Assert.Equal(expected, json);

            var rehydrated = BsonSerializer.Deserialize<GeoJsonFeature<TCoordinates>>(json);
            Assert.Equal(expected, rehydrated.ToJson());
        }
    }
}
