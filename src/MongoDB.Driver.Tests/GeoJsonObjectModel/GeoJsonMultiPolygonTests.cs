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
    public class GeoJsonMultiPolygonTests
    {
        [Fact]
        public void TestExampleFromSpec()
        {
            var multiPolygon = GeoJson.MultiPolygon<GeoJson2DCoordinates>(
                GeoJson.PolygonCoordinates(GeoJson.Position(102.0, 2.0), GeoJson.Position(103.0, 2.0), GeoJson.Position(103.0, 3.0), GeoJson.Position(102.0, 3.0), GeoJson.Position(102.0, 2.0)),
                GeoJson.PolygonCoordinates<GeoJson2DCoordinates>(
                    GeoJson.LinearRingCoordinates(GeoJson.Position(102.0, 2.0), GeoJson.Position(103.0, 2.0), GeoJson.Position(103.0, 3.0), GeoJson.Position(102.0, 3.0), GeoJson.Position(102.0, 2.0)),
                    GeoJson.LinearRingCoordinates(GeoJson.Position(102.0, 2.0), GeoJson.Position(103.0, 2.0), GeoJson.Position(103.0, 3.0), GeoJson.Position(102.0, 3.0), GeoJson.Position(102.0, 2.0))));

            var exterior1 = "[[102.0, 2.0], [103.0, 2.0], [103.0, 3.0], [102.0, 3.0], [102.0, 2.0]]";
            var exterior2 = "[[102.0, 2.0], [103.0, 2.0], [103.0, 3.0], [102.0, 3.0], [102.0, 2.0]]";
            var hole2 = "[[102.0, 2.0], [103.0, 2.0], [103.0, 3.0], [102.0, 3.0], [102.0, 2.0]]";
            var expected = "{ 'type' : 'MultiPolygon', 'coordinates' : [[#x1], [#x2, #h2]] }".Replace("#x1", exterior1).Replace("#x2", exterior2).Replace("#h2", hole2).Replace("'", "\"");
            TestRoundTrip(expected, multiPolygon);
        }

        [Fact]
        public void TestMultiPolygon2D()
        {
            var multiPolygon = GeoJson.MultiPolygon(
                GeoJson.PolygonCoordinates(GeoJson.Position(1.0, 2.0), GeoJson.Position(3.0, 4.0), GeoJson.Position(5.0, 6.0), GeoJson.Position(1.0, 2.0)),
                GeoJson.PolygonCoordinates(GeoJson.Position(2.0, 3.0), GeoJson.Position(4.0, 5.0), GeoJson.Position(6.0, 7.0), GeoJson.Position(2.0, 3.0)));

            var expected = "{ 'type' : 'MultiPolygon', 'coordinates' : [[[[1.0, 2.0], [3.0, 4.0], [5.0, 6.0], [1.0, 2.0]]], [[[2.0, 3.0], [4.0, 5.0], [6.0, 7.0], [2.0, 3.0]]]] }".Replace("'", "\"");
            TestRoundTrip(expected, multiPolygon);
        }

        [Fact]
        public void TestMultiPolygon2DGeographic()
        {
            var multiPolygon = GeoJson.MultiPolygon(
                GeoJson.PolygonCoordinates(GeoJson.Geographic(1.0, 2.0), GeoJson.Geographic(3.0, 4.0), GeoJson.Geographic(5.0, 6.0), GeoJson.Geographic(1.0, 2.0)),
                GeoJson.PolygonCoordinates(GeoJson.Geographic(2.0, 3.0), GeoJson.Geographic(4.0, 5.0), GeoJson.Geographic(6.0, 7.0), GeoJson.Geographic(2.0, 3.0)));

            var expected = "{ 'type' : 'MultiPolygon', 'coordinates' : [[[[1.0, 2.0], [3.0, 4.0], [5.0, 6.0], [1.0, 2.0]]], [[[2.0, 3.0], [4.0, 5.0], [6.0, 7.0], [2.0, 3.0]]]] }".Replace("'", "\"");
            TestRoundTrip(expected, multiPolygon);
        }

        [Fact]
        public void TestMultiPolygon2DProjected()
        {
            var multiPolygon = GeoJson.MultiPolygon(
                GeoJson.PolygonCoordinates(GeoJson.Projected(1.0, 2.0), GeoJson.Projected(3.0, 4.0), GeoJson.Projected(5.0, 6.0), GeoJson.Projected(1.0, 2.0)),
                GeoJson.PolygonCoordinates(GeoJson.Projected(2.0, 3.0), GeoJson.Projected(4.0, 5.0), GeoJson.Projected(6.0, 7.0), GeoJson.Projected(2.0, 3.0)));

            var expected = "{ 'type' : 'MultiPolygon', 'coordinates' : [[[[1.0, 2.0], [3.0, 4.0], [5.0, 6.0], [1.0, 2.0]]], [[[2.0, 3.0], [4.0, 5.0], [6.0, 7.0], [2.0, 3.0]]]] }".Replace("'", "\"");
            TestRoundTrip(expected, multiPolygon);
        }

        [Fact]
        public void TestMultiPolygon2DWithExtraMembers()
        {
            var multiPolygon = GeoJson.MultiPolygon(
                new GeoJsonObjectArgs<GeoJson2DCoordinates> { ExtraMembers = new BsonDocument("x", 1) },
                GeoJson.PolygonCoordinates(GeoJson.Position(1.0, 2.0), GeoJson.Position(3.0, 4.0), GeoJson.Position(5.0, 6.0), GeoJson.Position(1.0, 2.0)),
                GeoJson.PolygonCoordinates(GeoJson.Position(2.0, 3.0), GeoJson.Position(4.0, 5.0), GeoJson.Position(6.0, 7.0), GeoJson.Position(2.0, 3.0)));

            var expected = "{ 'type' : 'MultiPolygon', 'coordinates' : [[[[1.0, 2.0], [3.0, 4.0], [5.0, 6.0], [1.0, 2.0]]], [[[2.0, 3.0], [4.0, 5.0], [6.0, 7.0], [2.0, 3.0]]]], 'x' : 1 }".Replace("'", "\"");
            TestRoundTrip(expected, multiPolygon);
        }

        [Fact]
        public void TestMultiPolygon3D()
        {
            var multiPolygon = GeoJson.MultiPolygon(
                GeoJson.PolygonCoordinates(GeoJson.Position(1.0, 2.0, 3.0), GeoJson.Position(4.0, 5.0, 6.0), GeoJson.Position(7.0, 8.0, 9.0), GeoJson.Position(1.0, 2.0, 3.0)),
                GeoJson.PolygonCoordinates(GeoJson.Position(2.0, 3.0, 4.0), GeoJson.Position(5.0, 6.0, 7.0), GeoJson.Position(8.0, 9.0, 10.0), GeoJson.Position(2.0, 3.0, 4.0)));

            var expected = "{ 'type' : 'MultiPolygon', 'coordinates' : [[[[1.0, 2.0, 3.0], [4.0, 5.0, 6.0], [7.0, 8.0, 9.0], [1.0, 2.0, 3.0]]], [[[2.0, 3.0, 4.0], [5.0, 6.0, 7.0], [8.0, 9.0, 10.0], [2.0, 3.0, 4.0]]]] }".Replace("'", "\"");
            TestRoundTrip(expected, multiPolygon);
        }

        [Fact]
        public void TestMultiPolygon3DGeographic()
        {
            var multiPolygon = GeoJson.MultiPolygon(
                GeoJson.PolygonCoordinates(GeoJson.Geographic(1.0, 2.0, 3.0), GeoJson.Geographic(4.0, 5.0, 6.0), GeoJson.Geographic(7.0, 8.0, 9.0), GeoJson.Geographic(1.0, 2.0, 3.0)),
                GeoJson.PolygonCoordinates(GeoJson.Geographic(2.0, 3.0, 4.0), GeoJson.Geographic(5.0, 6.0, 7.0), GeoJson.Geographic(8.0, 9.0, 10.0), GeoJson.Geographic(2.0, 3.0, 4.0)));

            var expected = "{ 'type' : 'MultiPolygon', 'coordinates' : [[[[1.0, 2.0, 3.0], [4.0, 5.0, 6.0], [7.0, 8.0, 9.0], [1.0, 2.0, 3.0]]], [[[2.0, 3.0, 4.0], [5.0, 6.0, 7.0], [8.0, 9.0, 10.0], [2.0, 3.0, 4.0]]]] }".Replace("'", "\"");
            TestRoundTrip(expected, multiPolygon);
        }

        [Fact]
        public void TestMultiPolygon3DProjected()
        {
            var multiPolygon = GeoJson.MultiPolygon(
                GeoJson.PolygonCoordinates(GeoJson.Projected(1.0, 2.0, 3.0), GeoJson.Projected(4.0, 5.0, 6.0), GeoJson.Projected(7.0, 8.0, 9.0), GeoJson.Projected(1.0, 2.0, 3.0)),
                GeoJson.PolygonCoordinates(GeoJson.Projected(2.0, 3.0, 4.0), GeoJson.Projected(5.0, 6.0, 7.0), GeoJson.Projected(8.0, 9.0, 10.0), GeoJson.Projected(2.0, 3.0, 4.0)));

            var expected = "{ 'type' : 'MultiPolygon', 'coordinates' : [[[[1.0, 2.0, 3.0], [4.0, 5.0, 6.0], [7.0, 8.0, 9.0], [1.0, 2.0, 3.0]]], [[[2.0, 3.0, 4.0], [5.0, 6.0, 7.0], [8.0, 9.0, 10.0], [2.0, 3.0, 4.0]]]] }".Replace("'", "\"");
            TestRoundTrip(expected, multiPolygon);
        }

        [Fact]
        public void TestMultiPolygon3DWithExtraMembers()
        {
            var multiPolygon = GeoJson.MultiPolygon(
                new GeoJsonObjectArgs<GeoJson3DCoordinates> { ExtraMembers = new BsonDocument("x", 1) },
                GeoJson.PolygonCoordinates(GeoJson.Position(1.0, 2.0, 3.0), GeoJson.Position(4.0, 5.0, 6.0), GeoJson.Position(7.0, 8.0, 9.0), GeoJson.Position(1.0, 2.0, 3.0)),
                GeoJson.PolygonCoordinates(GeoJson.Position(2.0, 3.0, 4.0), GeoJson.Position(5.0, 6.0, 7.0), GeoJson.Position(8.0, 9.0, 10.0), GeoJson.Position(2.0, 3.0, 4.0)));

            var expected = "{ 'type' : 'MultiPolygon', 'coordinates' : [[[[1.0, 2.0, 3.0], [4.0, 5.0, 6.0], [7.0, 8.0, 9.0], [1.0, 2.0, 3.0]]], [[[2.0, 3.0, 4.0], [5.0, 6.0, 7.0], [8.0, 9.0, 10.0], [2.0, 3.0, 4.0]]]], 'x' : 1 }".Replace("'", "\"");
            TestRoundTrip(expected, multiPolygon);
        }

        private void TestRoundTrip<TCoordinates>(string expected, GeoJsonMultiPolygon<TCoordinates> multiPolygon) where TCoordinates : GeoJsonCoordinates
        {
            var json = multiPolygon.ToJson();
            Assert.Equal(expected, json);

            var rehydrated = BsonSerializer.Deserialize<GeoJsonMultiPolygon<TCoordinates>>(json);
            Assert.Equal(expected, rehydrated.ToJson());
        }
    }
}
