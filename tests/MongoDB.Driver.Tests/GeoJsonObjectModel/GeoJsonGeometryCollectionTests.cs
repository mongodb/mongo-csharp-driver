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
    public class GeoJsonGeometryCollectionTests
    {
        [Fact]
        public void TestExampleFromSpec()
        {
            var geometryCollection = GeoJson.GeometryCollection(
                GeoJson.Point(GeoJson.Position(100.0, 0.0)),
                GeoJson.LineString(GeoJson.Position(101.0, 0.0), GeoJson.Position(102.0, 1.0)));

            var geometry1 = "{ 'type' : 'Point', 'coordinates' : [100.0, 0.0] }";
            var geometry2 = "{ 'type' : 'LineString', 'coordinates' : [[101.0, 0.0], [102.0, 1.0]] }";
            var expected = "{ 'type' : 'GeometryCollection', 'geometries' : [#1, #2] }".Replace("#1", geometry1).Replace("#2", geometry2).Replace("'", "\"");
            TestRoundTrip(expected, geometryCollection);
        }

        [Fact]
        public void TestGeometryCollectionWithExtraMembers()
        {
            var geometryCollection = GeoJson.GeometryCollection(
                new GeoJsonObjectArgs<GeoJson2DCoordinates> { ExtraMembers = new BsonDocument("x", 1) },
                GeoJson.Point(GeoJson.Position(1.0, 2.0)),
                GeoJson.Point(GeoJson.Position(3.0, 4.0)));

            var geometry1 = "{ 'type' : 'Point', 'coordinates' : [1.0, 2.0] }";
            var geometry2 = "{ 'type' : 'Point', 'coordinates' : [3.0, 4.0] }";
            var expected = "{ 'type' : 'GeometryCollection', 'geometries' : [#1, #2], 'x' : 1 }".Replace("#1", geometry1).Replace("#2", geometry2).Replace("'", "\"");
            TestRoundTrip(expected, geometryCollection);
        }

        private void TestRoundTrip<TCoordinates>(string expected, GeoJsonGeometryCollection<TCoordinates> geometryCollection) where TCoordinates : GeoJsonCoordinates
        {
            var json = geometryCollection.ToJson();
            Assert.Equal(expected, json);

            var rehydrated = BsonSerializer.Deserialize<GeoJsonGeometryCollection<TCoordinates>>(json);
            Assert.Equal(expected, rehydrated.ToJson());
        }
    }
}
