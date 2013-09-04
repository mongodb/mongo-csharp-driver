﻿/* Copyright 2010-2013 10gen Inc.
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
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.GeoJsonObjectModel
{
    public class GeoJson2DProjectedCoordinatesTests
    {
        [Test]
        public void TestDeserializeDoubles()
        {
            var json = "[1.0, 2.0]";
            var coordinates = BsonSerializer.Deserialize<GeoJson2DProjectedCoordinates>(json);
            Assert.AreEqual(1.0, coordinates.Easting);
            Assert.AreEqual(2.0, coordinates.Northing);
        }

        [Test]
        public void TestDeserializeInts()
        {
            var json = "[1, 2]";
            var coordinates = BsonSerializer.Deserialize<GeoJson2DProjectedCoordinates>(json);
            Assert.AreEqual(1.0, coordinates.Easting);
            Assert.AreEqual(2.0, coordinates.Northing);
        }
    }
}
