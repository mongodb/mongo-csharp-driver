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
    public class GeoJsonPolygonTests
    {
        [Fact]
        public void TestExampleFromSpecNoHoles()
        {
            var polygon = GeoJson.Polygon(
                GeoJson.Position(100.0, 0.0),
                GeoJson.Position(101.0, 0.0),
                GeoJson.Position(101.0, 1.0),
                GeoJson.Position(100.0, 0.0));

            var expected = "{ 'type' : 'Polygon', 'coordinates' : [[[100.0, 0.0], [101.0, 0.0], [101.0, 1.0], [100.0, 0.0]]] }".Replace("'", "\"");
            TestRoundTrip(expected, polygon);
        }

        [Fact]
        public void TestExampleFromSpecWithHoles()
        {
            var polygon = GeoJson.Polygon(
                GeoJson.PolygonCoordinates<GeoJson2DCoordinates>(
                    // exterior
                    GeoJson.LinearRingCoordinates(
                        GeoJson.Position(100.0, 0.0),
                        GeoJson.Position(101.0, 0.0),
                        GeoJson.Position(101.0, 1.0),
                        GeoJson.Position(100.0, 1.0),
                        GeoJson.Position(100.0, 0.0)
                    ),
                    // holes
                    GeoJson.LinearRingCoordinates(
                        GeoJson.Position(100.25, 0.25),
                        GeoJson.Position(100.75, 0.25),
                        GeoJson.Position(100.75, 0.75),
                        GeoJson.Position(100.25, 0.75),
                        GeoJson.Position(100.25, 0.25)
                    )
                )
            );

            var exterior = "[[100.0, 0.0], [101.0, 0.0], [101.0, 1.0], [100.0, 1.0], [100.0, 0.0]]";
            var hole = "[[100.25, 0.25], [100.75, 0.25], [100.75, 0.75], [100.25, 0.75], [100.25, 0.25]]";
            var expected = "{ 'type' : 'Polygon', 'coordinates' : [#exterior, #hole] }".Replace("#exterior", exterior).Replace("#hole", hole).Replace("'", "\"");
            TestRoundTrip(expected, polygon);
        }

        [Fact]
        public void TestPolygon2D()
        {
            var polygon = GeoJson.Polygon(
                GeoJson.Position(1.0, 2.0),
                GeoJson.Position(3.0, 4.0),
                GeoJson.Position(5.0, 6.0),
                GeoJson.Position(1.0, 2.0));

            var expected = "{ 'type' : 'Polygon', 'coordinates' : [[[1.0, 2.0], [3.0, 4.0], [5.0, 6.0], [1.0, 2.0]]] }".Replace("'", "\"");
            TestRoundTrip(expected, polygon);
        }

        [Fact]
        public void TestPolygon2DGeographic()
        {
            var polygon = GeoJson.Polygon(
                GeoJson.Geographic(1.0, 2.0),
                GeoJson.Geographic(3.0, 4.0),
                GeoJson.Geographic(5.0, 6.0),
                GeoJson.Geographic(1.0, 2.0));

            var expected = "{ 'type' : 'Polygon', 'coordinates' : [[[1.0, 2.0], [3.0, 4.0], [5.0, 6.0], [1.0, 2.0]]] }".Replace("'", "\"");
            TestRoundTrip(expected, polygon);
        }

        [Fact]
        public void TestPolygon2DProjected()
        {
            var polygon = GeoJson.Polygon(
                GeoJson.Projected(1.0, 2.0),
                GeoJson.Projected(3.0, 4.0),
                GeoJson.Projected(5.0, 6.0),
                GeoJson.Projected(1.0, 2.0));

            var expected = "{ 'type' : 'Polygon', 'coordinates' : [[[1.0, 2.0], [3.0, 4.0], [5.0, 6.0], [1.0, 2.0]]] }".Replace("'", "\"");
            TestRoundTrip(expected, polygon);
        }

        [Fact]
        public void TestPolygon2DWithExtraMembers()
        {
            var polygon = GeoJson.Polygon(
                new GeoJsonObjectArgs<GeoJson2DCoordinates> { ExtraMembers = new BsonDocument("x", 1) },
                GeoJson.Position(1.0, 2.0),
                GeoJson.Position(3.0, 4.0),
                GeoJson.Position(5.0, 6.0),
                GeoJson.Position(1.0, 2.0));

            var expected = "{ 'type' : 'Polygon', 'coordinates' : [[[1.0, 2.0], [3.0, 4.0], [5.0, 6.0], [1.0, 2.0]]], 'x' : 1 }".Replace("'", "\"");
            TestRoundTrip(expected, polygon);
        }

        [Fact]
        public void TestPolygon3D()
        {
            var polygon = GeoJson.Polygon(
                GeoJson.Position(1.0, 2.0, 3.0),
                GeoJson.Position(4.0, 5.0, 6.0),
                GeoJson.Position(7.0, 8.0, 9.0),
                GeoJson.Position(1.0, 2.0, 3.0));

            var expected = "{ 'type' : 'Polygon', 'coordinates' : [[[1.0, 2.0, 3.0], [4.0, 5.0, 6.0], [7.0, 8.0, 9.0], [1.0, 2.0, 3.0]]] }".Replace("'", "\"");
            TestRoundTrip(expected, polygon);
        }

        [Fact]
        public void TestPolygon3DGeographic()
        {
            var polygon = GeoJson.Polygon(
                GeoJson.Geographic(1.0, 2.0, 3.0),
                GeoJson.Geographic(4.0, 5.0, 6.0),
                GeoJson.Geographic(7.0, 8.0, 9.0),
                GeoJson.Geographic(1.0, 2.0, 3.0));

            var expected = "{ 'type' : 'Polygon', 'coordinates' : [[[1.0, 2.0, 3.0], [4.0, 5.0, 6.0], [7.0, 8.0, 9.0], [1.0, 2.0, 3.0]]] }".Replace("'", "\"");
            TestRoundTrip(expected, polygon);
        }

        [Fact]
        public void TestPolygon3DProjected()
        {
            var polygon = GeoJson.Polygon(
                GeoJson.Projected(1.0, 2.0, 3.0),
                GeoJson.Projected(4.0, 5.0, 6.0),
                GeoJson.Projected(7.0, 8.0, 9.0),
                GeoJson.Projected(1.0, 2.0, 3.0));

            var expected = "{ 'type' : 'Polygon', 'coordinates' : [[[1.0, 2.0, 3.0], [4.0, 5.0, 6.0], [7.0, 8.0, 9.0], [1.0, 2.0, 3.0]]] }".Replace("'", "\"");
            TestRoundTrip(expected, polygon);
        }

        [Fact]
        public void TestPolygon3DWithExtraMembers()
        {
            var polygon = GeoJson.Polygon(
                new GeoJsonObjectArgs<GeoJson3DCoordinates> { ExtraMembers = new BsonDocument("x", 1) },
                GeoJson.Position(1.0, 2.0, 3.0),
                GeoJson.Position(4.0, 5.0, 6.0),
                GeoJson.Position(7.0, 8.0, 9.0),
                GeoJson.Position(1.0, 2.0, 3.0));

            var expected = "{ 'type' : 'Polygon', 'coordinates' : [[[1.0, 2.0, 3.0], [4.0, 5.0, 6.0], [7.0, 8.0, 9.0], [1.0, 2.0, 3.0]]], 'x' : 1 }".Replace("'", "\"");
            TestRoundTrip(expected, polygon);
        }

        private void TestRoundTrip<TCoordinates>(string expected, GeoJsonPolygon<TCoordinates> polygon) where TCoordinates : GeoJsonCoordinates
        {
            var json = polygon.ToJson();
            Assert.Equal(expected, json);

            var rehydrated = BsonSerializer.Deserialize<GeoJsonPolygon<TCoordinates>>(json);
            Assert.Equal(expected, rehydrated.ToJson());
        }
    }
}
