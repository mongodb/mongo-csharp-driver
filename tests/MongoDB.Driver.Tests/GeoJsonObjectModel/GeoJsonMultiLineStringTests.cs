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
    public class GeoJsonMultiLineStringTests
    {
        [Fact]
        public void TestExampleFromSpec()
        {
            var multiLineString = GeoJson.MultiLineString(
                GeoJson.LineStringCoordinates(GeoJson.Position(100.0, 0.0), GeoJson.Position(101.0, 1.0)),
                GeoJson.LineStringCoordinates(GeoJson.Position(102.0, 2.0), GeoJson.Position(103.0, 3.0)));

            var expected = "{ 'type' : 'MultiLineString', 'coordinates' : [[[100.0, 0.0], [101.0, 1.0]], [[102.0, 2.0], [103.0, 3.0]]] }".Replace("'", "\"");
            TestRoundTrip(expected, multiLineString);
        }

        [Fact]
        public void TestMultiLineString2D()
        {
            var multiLineString = GeoJson.MultiLineString(
                GeoJson.LineStringCoordinates(GeoJson.Position(1.0, 2.0), GeoJson.Position(3.0, 4.0)),
                GeoJson.LineStringCoordinates(GeoJson.Position(5.0, 6.0), GeoJson.Position(7.0, 8.0)));

            var expected = "{ 'type' : 'MultiLineString', 'coordinates' : [[[1.0, 2.0], [3.0, 4.0]], [[5.0, 6.0], [7.0, 8.0]]] }".Replace("'", "\"");
            TestRoundTrip(expected, multiLineString);
        }

        [Fact]
        public void TestMultiLineString2DGeographic()
        {
            var multiLineString = GeoJson.MultiLineString(
                GeoJson.LineStringCoordinates(GeoJson.Geographic(1.0, 2.0), GeoJson.Geographic(3.0, 4.0)),
                GeoJson.LineStringCoordinates(GeoJson.Geographic(5.0, 6.0), GeoJson.Geographic(7.0, 8.0)));

            var expected = "{ 'type' : 'MultiLineString', 'coordinates' : [[[1.0, 2.0], [3.0, 4.0]], [[5.0, 6.0], [7.0, 8.0]]] }".Replace("'", "\"");
            TestRoundTrip(expected, multiLineString);
        }

        [Fact]
        public void TestMultiLineString2DProjected()
        {
            var multiLineString = GeoJson.MultiLineString(
                GeoJson.LineStringCoordinates(GeoJson.Projected(1.0, 2.0), GeoJson.Projected(3.0, 4.0)),
                GeoJson.LineStringCoordinates(GeoJson.Projected(5.0, 6.0), GeoJson.Projected(7.0, 8.0)));

            var expected = "{ 'type' : 'MultiLineString', 'coordinates' : [[[1.0, 2.0], [3.0, 4.0]], [[5.0, 6.0], [7.0, 8.0]]] }".Replace("'", "\"");
            TestRoundTrip(expected, multiLineString);
        }

        [Fact]
        public void TestMultiLineString2DWithExtraMembers()
        {
            var multiLineString = GeoJson.MultiLineString(
                new GeoJsonObjectArgs<GeoJson2DCoordinates> { ExtraMembers = new BsonDocument("x", 1) },
                GeoJson.LineStringCoordinates(GeoJson.Position(1.0, 2.0), GeoJson.Position(3.0, 4.0)),
                GeoJson.LineStringCoordinates(GeoJson.Position(5.0, 6.0), GeoJson.Position(7.0, 8.0)));

            var expected = "{ 'type' : 'MultiLineString', 'coordinates' : [[[1.0, 2.0], [3.0, 4.0]], [[5.0, 6.0], [7.0, 8.0]]], 'x' : 1 }".Replace("'", "\"");
            TestRoundTrip(expected, multiLineString);
        }

        [Fact]
        public void TestMultiLineString3D()
        {
            var multiLineString = GeoJson.MultiLineString(
                GeoJson.LineStringCoordinates(GeoJson.Position(1.0, 2.0, 3.0), GeoJson.Position(4.0, 5.0, 6.0)),
                GeoJson.LineStringCoordinates(GeoJson.Position(7.0, 8.0, 9.0), GeoJson.Position(10.0, 11.0, 12.0)));

            var expected = "{ 'type' : 'MultiLineString', 'coordinates' : [[[1.0, 2.0, 3.0], [4.0, 5.0, 6.0]], [[7.0, 8.0, 9.0], [10.0, 11.0, 12.0]]] }".Replace("'", "\"");
            TestRoundTrip(expected, multiLineString);
        }

        [Fact]
        public void TestMultiLineString3DGeographic()
        {
            var multiLineString = GeoJson.MultiLineString(
                GeoJson.LineStringCoordinates(GeoJson.Geographic(1.0, 2.0, 3.0), GeoJson.Geographic(4.0, 5.0, 6.0)),
                GeoJson.LineStringCoordinates(GeoJson.Geographic(7.0, 8.0, 9.0), GeoJson.Geographic(10.0, 11.0, 12.0)));

            var expected = "{ 'type' : 'MultiLineString', 'coordinates' : [[[1.0, 2.0, 3.0], [4.0, 5.0, 6.0]], [[7.0, 8.0, 9.0], [10.0, 11.0, 12.0]]] }".Replace("'", "\"");
            TestRoundTrip(expected, multiLineString);
        }

        [Fact]
        public void TestMultiLineString3DProjected()
        {
            var multiLineString = GeoJson.MultiLineString(
                GeoJson.LineStringCoordinates(GeoJson.Projected(1.0, 2.0, 3.0), GeoJson.Projected(4.0, 5.0, 6.0)),
                GeoJson.LineStringCoordinates(GeoJson.Projected(7.0, 8.0, 9.0), GeoJson.Projected(10.0, 11.0, 12.0)));

            var expected = "{ 'type' : 'MultiLineString', 'coordinates' : [[[1.0, 2.0, 3.0], [4.0, 5.0, 6.0]], [[7.0, 8.0, 9.0], [10.0, 11.0, 12.0]]] }".Replace("'", "\"");
            TestRoundTrip(expected, multiLineString);
        }

        [Fact]
        public void TestMultiLineString3DWithExtraMembers()
        {
            var multiLineString = GeoJson.MultiLineString(
                new GeoJsonObjectArgs<GeoJson3DCoordinates> { ExtraMembers = new BsonDocument("x", 1) },
                GeoJson.LineStringCoordinates(GeoJson.Position(1.0, 2.0, 3.0), GeoJson.Position(4.0, 5.0, 6.0)),
                GeoJson.LineStringCoordinates(GeoJson.Position(7.0, 8.0, 9.0), GeoJson.Position(10.0, 11.0, 12.0)));

            var expected = "{ 'type' : 'MultiLineString', 'coordinates' : [[[1.0, 2.0, 3.0], [4.0, 5.0, 6.0]], [[7.0, 8.0, 9.0], [10.0, 11.0, 12.0]]], 'x' : 1 }".Replace("'", "\"");
            TestRoundTrip(expected, multiLineString);
        }

        private void TestRoundTrip<TCoordinates>(string expected, GeoJsonMultiLineString<TCoordinates> multiLineString) where TCoordinates : GeoJsonCoordinates
        {
            var json = multiLineString.ToJson();
            Assert.Equal(expected, json);

            var rehydrated = BsonSerializer.Deserialize<GeoJsonMultiLineString<TCoordinates>>(json);
            Assert.Equal(expected, rehydrated.ToJson());
        }
    }
}
