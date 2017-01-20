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
    public class GeoJsonLineStringTests
    {
        [Fact]
        public void TestExampleFromSpec()
        {
            var lineString = GeoJson.LineString(
                GeoJson.Position(100.0, 0.0),
                GeoJson.Position(101.0, 1.0));

            var expected = "{ 'type' : 'LineString', 'coordinates' : [[100.0, 0.0], [101.0, 1.0]] }".Replace("'", "\"");
            TestRoundTrip(expected, lineString);
        }

        [Fact]
        public void Test2DLineString()
        {
            var lineString = GeoJson.LineString(
                GeoJson.Position(1.0, 2.0),
                GeoJson.Position(3.0, 4.0));

            var expected = "{ 'type' : 'LineString', 'coordinates' : [[1.0, 2.0], [3.0, 4.0]] }".Replace("'", "\"");
            TestRoundTrip(expected, lineString);
        }

        [Fact]
        public void Test2DLineStringGeographic()
        {
            var lineString = GeoJson. LineString(
                GeoJson.Geographic(1.0, 2.0),
                GeoJson.Geographic(3.0, 4.0));

            var expected = "{ 'type' : 'LineString', 'coordinates' : [[1.0, 2.0], [3.0, 4.0]] }".Replace("'", "\"");
            TestRoundTrip(expected, lineString);
        }

        [Fact]
        public void Test2DLineStringProjected()
        {
            var lineString = GeoJson.LineString(GeoJson.Projected(1.0, 2.0), GeoJson.Projected(3.0, 4.0));

            var expected = "{ 'type' : 'LineString', 'coordinates' : [[1.0, 2.0], [3.0, 4.0]] }".Replace("'", "\"");
            TestRoundTrip(expected, lineString);
        }

        [Fact]
        public void Test2DLineStringWithExtraMembers()
        {
            var lineString = GeoJson.LineString(
                new GeoJsonObjectArgs<GeoJson2DCoordinates> { ExtraMembers = new BsonDocument("x", 1) },
                GeoJson.Position(1.0, 2.0), GeoJson.Position(3.0, 4.0)
            );

            var expected = "{ 'type' : 'LineString', 'coordinates' : [[1.0, 2.0], [3.0, 4.0]], 'x' : 1 }".Replace("'", "\"");
            TestRoundTrip(expected, lineString);
        }

        [Fact]
        public void Test3DLineString()
        {
            var lineString = GeoJson.LineString(
                GeoJson.Position(1.0, 2.0, 3.0),
                GeoJson.Position(4.0, 5.0, 6.0));

            var expected = "{ 'type' : 'LineString', 'coordinates' : [[1.0, 2.0, 3.0], [4.0, 5.0, 6.0]] }".Replace("'", "\"");
            TestRoundTrip(expected, lineString);
        }

        [Fact]
        public void Test3DLineStringGeographic()
        {
            var lineString = GeoJson.LineString(
                GeoJson.Geographic(1.0, 2.0, 3.0),
                GeoJson.Geographic(4.0, 5.0, 6.0));

            var expected = "{ 'type' : 'LineString', 'coordinates' : [[1.0, 2.0, 3.0], [4.0, 5.0, 6.0]] }".Replace("'", "\"");
            TestRoundTrip(expected, lineString);
        }

        [Fact]
        public void Test3DLineStringProjected()
        {
            var lineString = GeoJson.LineString(
                GeoJson.Projected(1.0, 2.0, 3.0),
                GeoJson.Projected(4.0, 5.0, 6.0));

            var expected = "{ 'type' : 'LineString', 'coordinates' : [[1.0, 2.0, 3.0], [4.0, 5.0, 6.0]] }".Replace("'", "\"");
            TestRoundTrip(expected, lineString);
        }

        [Fact]
        public void Test3DLineStringWithExtraMembers()
        {
            var lineString = GeoJson.LineString(
                new GeoJsonObjectArgs<GeoJson3DCoordinates> { ExtraMembers = new BsonDocument("x", 1) },
                GeoJson.Position(1.0, 2.0, 3.0),
                GeoJson.Position(4.0, 5.0, 6.0));

            var expected = "{ 'type' : 'LineString', 'coordinates' : [[1.0, 2.0, 3.0], [4.0, 5.0, 6.0]], 'x' : 1 }".Replace("'", "\"");
            TestRoundTrip(expected, lineString);
        }

        private void TestRoundTrip<TCoordinates>(string expected, GeoJsonLineString<TCoordinates> lineString) where TCoordinates : GeoJsonCoordinates
        {
            var json = lineString.ToJson();
            Assert.Equal(expected, json);

            var rehydrated = BsonSerializer.Deserialize<GeoJsonLineString<TCoordinates>>(json);
            Assert.Equal(expected, rehydrated.ToJson());
        }
    }
}
