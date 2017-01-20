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

using MongoDB.Bson.Serialization;
using MongoDB.Driver.GeoJsonObjectModel;
using Xunit;

namespace MongoDB.Driver.Tests.GeoJsonObjectModel
{
    public class GeoJson2DGeographicCoordinatesTests
    {
        [Fact]
        public void TestDeserializeDoubles()
        {
            var json = "[1.0, 2.0]";
            var coordinates = BsonSerializer.Deserialize<GeoJson2DGeographicCoordinates>(json);
            Assert.Equal(1.0, coordinates.Longitude);
            Assert.Equal(2.0, coordinates.Latitude);
        }

        [Fact]
        public void TestDeserializeInts()
        {
            var json = "[1, 2]";
            var coordinates = BsonSerializer.Deserialize<GeoJson2DGeographicCoordinates>(json);
            Assert.Equal(1.0, coordinates.Longitude);
            Assert.Equal(2.0, coordinates.Latitude);
        }
    }
}
