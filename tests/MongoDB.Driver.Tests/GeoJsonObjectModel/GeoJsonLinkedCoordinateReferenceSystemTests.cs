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
    public class GeoJsonLinkedCoordinateReferenceSystemTests
    {
        [Fact]
        public void TestExampleFromSpec()
        {
            var crs = new GeoJsonLinkedCoordinateReferenceSystem("http://example.com/crs/42", "proj4");
            var expected = "{ 'type' : 'link', 'properties' : { 'href' : 'http://example.com/crs/42', 'type' : 'proj4' } }".Replace("'", "\"");

            TestRoundTrip(expected, (GeoJsonCoordinateReferenceSystem)crs);
            TestRoundTrip(expected, (GeoJsonLinkedCoordinateReferenceSystem)crs);
        }

        [Fact]
        public void TestExampleFromSpecWithoutHRefType()
        {
            var crs = new GeoJsonLinkedCoordinateReferenceSystem("http://example.com/crs/42");
            var expected = "{ 'type' : 'link', 'properties' : { 'href' : 'http://example.com/crs/42' } }".Replace("'", "\"");

            TestRoundTrip(expected, (GeoJsonCoordinateReferenceSystem)crs);
            TestRoundTrip(expected, (GeoJsonLinkedCoordinateReferenceSystem)crs);
        }

        private void TestRoundTrip(string expected, GeoJsonCoordinateReferenceSystem crs)
        {
            var json = crs.ToJson();
            Assert.Equal(expected, json);

            var rehydrated = BsonSerializer.Deserialize<GeoJsonCoordinateReferenceSystem>(json);
            Assert.Equal(expected, rehydrated.ToJson());
        }

        private void TestRoundTrip(string expected, GeoJsonLinkedCoordinateReferenceSystem crs)
        {
            var json = crs.ToJson();
            Assert.Equal(expected, json);

            var rehydrated = BsonSerializer.Deserialize<GeoJsonLinkedCoordinateReferenceSystem>(json);
            Assert.Equal(expected, rehydrated.ToJson());
        }
    }
}
