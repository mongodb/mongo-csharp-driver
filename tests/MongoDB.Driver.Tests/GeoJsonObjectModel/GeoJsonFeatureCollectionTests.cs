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
using MongoDB.Driver.GeoJsonObjectModel;
using Xunit;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Tests.GeoJsonObjectModel
{
    public class GeoJsonFeatureCollectionTests
    {
        [Fact]
        public void TestExampleFromSpec()
        {
            var featureCollection = GeoJson.FeatureCollection(
                GeoJson.Feature(
                    new GeoJsonFeatureArgs<GeoJson2DCoordinates> { Properties = new BsonDocument { { "prop0", "value0" } } },
                    GeoJson.Point(GeoJson.Position(102.0, 0.5))),
                GeoJson.Feature(
                    new GeoJsonFeatureArgs<GeoJson2DCoordinates> { Properties = new BsonDocument { { "prop0", "value0" }, { "prop1", 0.0 } } },
                    GeoJson.LineString(GeoJson.Position(102.0, 0.0), GeoJson.Position(103.0, 1.0), GeoJson.Position(104.0, 0), GeoJson.Position(105.0, 1.0))),
                GeoJson.Feature(
                    new GeoJsonFeatureArgs<GeoJson2DCoordinates> { Properties = new BsonDocument { { "prop0", "value0" }, { "prop1", new BsonDocument("this", "that") } } },
                    GeoJson.Polygon(
                        GeoJson.Position(100.0, 0.0), GeoJson.Position(101.0, 0.0), GeoJson.Position(101.0, 1.0),
                        GeoJson.Position(100.0, 1.0), GeoJson.Position(100.0, 0.0))));

            var feature1 = "{ 'type' : 'Feature', 'geometry' : { 'type' : 'Point', 'coordinates' : [102.0, 0.5] }, 'properties' : { 'prop0' : 'value0' } }";
            var feature2 = "{ 'type' : 'Feature', 'geometry' : { 'type' : 'LineString', 'coordinates' : [[102.0, 0.0], [103.0, 1.0], [104.0, 0.0], [105.0, 1.0]] }, 'properties' : { 'prop0' : 'value0', 'prop1' : 0.0 } }";
            var feature3 = "{ 'type' : 'Feature', 'geometry' : { 'type' : 'Polygon', 'coordinates' : [[[100.0, 0.0], [101.0, 0.0], [101.0, 1.0], [100.0, 1.0], [100.0, 0.0]]] }, 'properties' : { 'prop0' : 'value0', 'prop1' : { 'this' : 'that' } } }";
            var expected = "{ 'type' : 'FeatureCollection', 'features' : [#1, #2, #3] }".Replace("#1", feature1).Replace("#2", feature2).Replace("#3", feature3).Replace("'", "\"");
            TestRoundTrip(expected, featureCollection);
        }

        [Fact]
        public void TestFeatureCollectionWithExtraMembers()
        {
            var collection = GeoJson.FeatureCollection(
                new GeoJsonObjectArgs<GeoJson2DCoordinates> { ExtraMembers = new BsonDocument("x", 1) },
                GeoJson.Feature(GeoJson.Point(GeoJson.Position(1.0, 2.0))),
                GeoJson.Feature(GeoJson.Point(GeoJson.Position(3.0, 4.0))));

            var feature1 = "{ 'type' : 'Feature', 'geometry' : { 'type' : 'Point', 'coordinates' : [1.0, 2.0] } }";
            var feature2 = "{ 'type' : 'Feature', 'geometry' : { 'type' : 'Point', 'coordinates' : [3.0, 4.0] } }";
            var expected = "{ 'type' : 'FeatureCollection', 'features' : [#1, #2], 'x' : 1 }".Replace("#1", feature1).Replace("#2", feature2).Replace("'", "\"");
            TestRoundTrip(expected, collection);
        }

        private void TestRoundTrip<TCoordinates>(string expected, GeoJsonFeatureCollection<TCoordinates> featureCollection) where TCoordinates : GeoJsonCoordinates
        {
            var json = featureCollection.ToJson();
            Assert.Equal(expected, json);

            var rehydrated = BsonSerializer.Deserialize<GeoJsonFeatureCollection<TCoordinates>>(json);
            Assert.Equal(expected, rehydrated.ToJson());
        }
    }
}
