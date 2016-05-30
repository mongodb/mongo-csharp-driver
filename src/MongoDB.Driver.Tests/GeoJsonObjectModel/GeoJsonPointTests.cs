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

using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.GeoJsonObjectModel;
using Xunit;

namespace MongoDB.Driver.Tests.GeoJsonObjectModel
{
    public class GeoJsonPointTests
    {
        [Fact]
        public void TestExampleFromSpec()
        {
            var point = GeoJson.Point(GeoJson.Position(100.0, 0.0));

            Assert.IsType<GeoJsonPoint<GeoJson2DCoordinates>>(point);
            Assert.Equal(null, point.CoordinateReferenceSystem);
            Assert.Equal(100.0, point.Coordinates.X);
            Assert.Equal(0.0, point.Coordinates.Y);
            Assert.True(new[] { 100.0, 0.0 }.SequenceEqual(point.Coordinates.Values));

            var expected = "{ 'type' : 'Point', 'coordinates' : [100.0, 0.0] }".Replace("'", "\"");
            TestRoundTrip(expected, point);
        }

        [Fact]
        public void TestPoint2D()
        {
            var point = GeoJson.Point(GeoJson.Position(1.0, 2.0));
            var expected = "{ 'type' : 'Point', 'coordinates' : [1.0, 2.0] }".Replace("'", "\"");
            TestRoundTrip(expected, point);
        }

        [Fact]
        public void TestPoint2DGeographic()
        {
            var point = GeoJson.Point(GeoJson.Geographic(1.0, 2.0));
            var expected = "{ 'type' : 'Point', 'coordinates' : [1.0, 2.0] }".Replace("'", "\"");
            TestRoundTrip(expected, point);
        }

        [Fact]
        public void TestPoint2DProjected()
        {
            var point = GeoJson.Point(GeoJson.Projected(1.0, 2.0));
            var expected = "{ 'type' : 'Point', 'coordinates' : [1.0, 2.0] }".Replace("'", "\"");
            TestRoundTrip(expected, point);
        }

        [Fact]
        public void TestPoint2DWithBoundingBox()
        {
            var point = GeoJson.Point(
                new GeoJsonObjectArgs<GeoJson2DCoordinates> { BoundingBox = GeoJson.BoundingBox(GeoJson.Position(1.0, 2.0), GeoJson.Position(3.0, 4.0)) },
                GeoJson.Position(100.0, 0.0));

            var expected = "{ 'type' : 'Point', 'bbox' : [1.0, 2.0, 3.0, 4.0], 'coordinates' : [100.0, 0.0] }".Replace("'", "\"");
            TestRoundTrip(expected, point);
        }

        [Fact]
        public void TestPoint2DWithExtraMembers()
        {
            var point = GeoJson.Point(
                new GeoJsonObjectArgs<GeoJson2DCoordinates> { ExtraMembers = new BsonDocument { { "x", 1 }, { "y", 2 } } },
                GeoJson.Position(100.0, 0.0));

            var expected = "{ 'type' : 'Point', 'coordinates' : [100.0, 0.0], 'x' : 1, 'y' : 2 }".Replace("'", "\"");
            TestRoundTrip(expected, point);
        }

        [Fact]
        public void TestPoint2DWithLinkedCoordinateReferenceSystem()
        {
            var point = GeoJson.Point(
                new GeoJsonObjectArgs<GeoJson2DCoordinates> { CoordinateReferenceSystem = new GeoJsonLinkedCoordinateReferenceSystem("abc") },
                GeoJson.Position(100.0, 0.0));

            var expected = "{ 'type' : 'Point', 'crs' : { 'type' : 'link', 'properties' : { 'href' : 'abc' } }, 'coordinates' : [100.0, 0.0] }".Replace("'", "\"");
            TestRoundTrip(expected, point);
        }

        [Fact]
        public void TestPoint2DWithLinkedCoordinateReferenceSystemWithType()
        {
            var point = GeoJson.Point(
                new GeoJsonObjectArgs<GeoJson2DCoordinates> { CoordinateReferenceSystem = new GeoJsonLinkedCoordinateReferenceSystem("abc", "def") },
                GeoJson.Position(100.0, 0.0));

            var expected = "{ 'type' : 'Point', 'crs' : { 'type' : 'link', 'properties' : { 'href' : 'abc', 'type' : 'def' } }, 'coordinates' : [100.0, 0.0] }".Replace("'", "\"");
            TestRoundTrip(expected, point);
        }

        [Fact]
        public void TestPoint2DWithNamedCoordinateReferenceSystem()
        {
            var point = GeoJson.Point(
                new GeoJsonObjectArgs<GeoJson2DCoordinates> { CoordinateReferenceSystem = new GeoJsonNamedCoordinateReferenceSystem("abc") },
                GeoJson.Position(100.0, 0.0));

            var expected = "{ 'type' : 'Point', 'crs' : { 'type' : 'name', 'properties' : { 'name' : 'abc' } }, 'coordinates' : [100.0, 0.0] }".Replace("'", "\"");
            TestRoundTrip(expected, point);
        }

        [Fact]
        public void TestPoint3D()
        {
            var point = GeoJson.Point(GeoJson.Position(1.0, 2.0, 3.0));
            var expected = "{ 'type' : 'Point', 'coordinates' : [1.0, 2.0, 3.0] }".Replace("'", "\"");
            TestRoundTrip(expected, point);
        }

        [Fact]
        public void TestPoint3DGeographic()
        {
            var point = GeoJson.Point(GeoJson.Geographic(1.0, 2.0, 3.0));
            var expected = "{ 'type' : 'Point', 'coordinates' : [1.0, 2.0, 3.0] }".Replace("'", "\"");
            TestRoundTrip(expected, point);
        }

        [Fact]
        public void TestPoint3DProjected()
        {
            var point = GeoJson.Point(GeoJson.Projected(1.0, 2.0, 3.0));
            var expected = "{ 'type' : 'Point', 'coordinates' : [1.0, 2.0, 3.0] }".Replace("'", "\"");
            TestRoundTrip(expected, point);
        }

        [Fact]
        public void TestPoint3DWithBoundingBox()
        {
            var point = GeoJson.Point(
                new GeoJsonObjectArgs<GeoJson3DCoordinates> { BoundingBox = GeoJson.BoundingBox(GeoJson.Position(1.0, 2.0, 3.0), GeoJson.Position(4.0, 5.0, 6.0)) },
                GeoJson.Position(1.0, 2.0, 3.0));

            var expected = "{ 'type' : 'Point', 'bbox' : [1.0, 2.0, 3.0, 4.0, 5.0, 6.0], 'coordinates' : [1.0, 2.0, 3.0] }".Replace("'", "\"");
            TestRoundTrip(expected, point);
        }

        private void TestRoundTrip<TCoordinates>(string expected, GeoJsonPoint<TCoordinates> point) where TCoordinates : GeoJsonCoordinates
        {
            var json = point.ToJson();
            Assert.Equal(expected, json);

            var rehydrated = BsonSerializer.Deserialize<GeoJsonPoint<TCoordinates>>(json);
            Assert.Equal(expected, rehydrated.ToJson());
        }
    }
}
