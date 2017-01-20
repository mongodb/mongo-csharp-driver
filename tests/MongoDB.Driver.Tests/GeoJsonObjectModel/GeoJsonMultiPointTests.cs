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
    public class GeoJsonMultiPointTests
    {
        [Fact]
        public void TestExampleFromSpec()
        {
            var multiPoint = GeoJson.MultiPoint(
                GeoJson.Position(100.0, 0.0),
                GeoJson.Position(101.0, 1.0));

            var expected = "{ 'type' : 'MultiPoint', 'coordinates' : [[100.0, 0.0], [101.0, 1.0]] }".Replace("'", "\"");
            TestRoundTrip(expected, multiPoint);
        }

        [Fact]
        public void TestMultiPoint2D()
        {
            var multiPoint = GeoJson.MultiPoint(
                GeoJson.Position(1.0, 2.0),
                GeoJson.Position(3.0, 4.0));

            var expected = "{ 'type' : 'MultiPoint', 'coordinates' : [[1.0, 2.0], [3.0, 4.0]] }".Replace("'", "\"");
            TestRoundTrip(expected, multiPoint);
        }

        [Fact]
        public void TestMultiPoint2DGeographic()
        {
            var multiPoint = GeoJson.MultiPoint(
                GeoJson.Geographic(1.0, 2.0),
                GeoJson.Geographic(3.0, 4.0));

            var expected = "{ 'type' : 'MultiPoint', 'coordinates' : [[1.0, 2.0], [3.0, 4.0]] }".Replace("'", "\"");
            TestRoundTrip(expected, multiPoint);
        }

        [Fact]
        public void TestMultiPoint2DProjected()
        {
            var multiPoint = GeoJson.MultiPoint(
                GeoJson.Projected(1.0, 2.0),
                GeoJson.Projected(3.0, 4.0));

            var expected = "{ 'type' : 'MultiPoint', 'coordinates' : [[1.0, 2.0], [3.0, 4.0]] }".Replace("'", "\"");
            TestRoundTrip(expected, multiPoint);
        }

        [Fact]
        public void TestMultiPoint2DWithExtraMembers()
        {
            var multiPoint = GeoJson.MultiPoint(
                new GeoJsonObjectArgs<GeoJson2DCoordinates> { ExtraMembers = new BsonDocument("x", 1) },
                GeoJson.Position(1.0, 2.0),
                GeoJson.Position(3.0, 4.0));

            var expected = "{ 'type' : 'MultiPoint', 'coordinates' : [[1.0, 2.0], [3.0, 4.0]], 'x' : 1 }".Replace("'", "\"");
            TestRoundTrip(expected, multiPoint);
        }

        [Fact]
        public void TestMultiPoint3D()
        {
            var multiPoint = GeoJson.MultiPoint(
                GeoJson.Position(1.0, 2.0, 3.0),
                GeoJson.Position(4.0, 5.0, 6.0));

            var expected = "{ 'type' : 'MultiPoint', 'coordinates' : [[1.0, 2.0, 3.0], [4.0, 5.0, 6.0]] }".Replace("'", "\"");
            TestRoundTrip(expected, multiPoint);
        }

        [Fact]
        public void TestMultiPoint3DGeographic()
        {
            var multiPoint = GeoJson.MultiPoint(
                GeoJson.Geographic(1.0, 2.0, 3.0),
                GeoJson.Geographic(4.0, 5.0, 6.0));

            var expected = "{ 'type' : 'MultiPoint', 'coordinates' : [[1.0, 2.0, 3.0], [4.0, 5.0, 6.0]] }".Replace("'", "\"");
            TestRoundTrip(expected, multiPoint);
        }

        [Fact]
        public void TestMultiPoint3DProjected()
        {
            var multiPoint = GeoJson.MultiPoint(
                GeoJson.Projected(1.0, 2.0, 3.0),
                GeoJson.Projected(4.0, 5.0, 6.0));

            var expected = "{ 'type' : 'MultiPoint', 'coordinates' : [[1.0, 2.0, 3.0], [4.0, 5.0, 6.0]] }".Replace("'", "\"");
            TestRoundTrip(expected, multiPoint);
        }

        [Fact]
        public void TestMultiPoint3DWithExtraMembers()
        {
            var multiPoint = GeoJson.MultiPoint(
                new GeoJsonObjectArgs<GeoJson3DCoordinates> { ExtraMembers = new BsonDocument("x", 1) },
                GeoJson.Position(1.0, 2.0, 3.0),
                GeoJson.Position(4.0, 5.0, 6.0));

            var expected = "{ 'type' : 'MultiPoint', 'coordinates' : [[1.0, 2.0, 3.0], [4.0, 5.0, 6.0]], 'x' : 1 }".Replace("'", "\"");
            TestRoundTrip(expected, multiPoint);
        }

        private void TestRoundTrip<TCoordinates>(string expected, GeoJsonMultiPoint<TCoordinates> multiPoint) where TCoordinates : GeoJsonCoordinates
        {
            var json = multiPoint.ToJson();
            Assert.Equal(expected, json);

            var rehydrated = BsonSerializer.Deserialize<GeoJsonMultiPoint<TCoordinates>>(json);
            Assert.Equal(expected, rehydrated.ToJson());
        }
    }
}
