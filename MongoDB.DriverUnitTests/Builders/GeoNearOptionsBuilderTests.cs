/* Copyright 2010-2013 10gen Inc.
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
using MongoDB.Driver.Builders;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Builders
{
    [TestFixture]
    public class GeoNearOptionsBuilderTests
    {
        [Test]
        public void TestSetAll()
        {
            var options = GeoNearOptions
                .SetDistanceMultiplier(1.5)
                .SetMaxDistance(2.5)
                .SetSpherical(true);
            var expected = "{ 'distanceMultiplier' : 1.5, 'maxDistance' : 2.5, 'spherical' : true }".Replace("'", "\"");
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestSetDistanceMultiplier()
        {
            var options = GeoNearOptions.SetDistanceMultiplier(1.5);
            var expected = "{ 'distanceMultiplier' : 1.5 }".Replace("'", "\"");
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestSetMaxDistance()
        {
            var options = GeoNearOptions.SetMaxDistance(1.5);
            var expected = "{ 'maxDistance' : 1.5 }".Replace("'", "\"");
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestSetSphericalFalse()
        {
            var options = GeoNearOptions.SetSpherical(false);
            var expected = "{ }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestSetSphericalTrue()
        {
            var options = GeoNearOptions.SetSpherical(true);
            var expected = "{ 'spherical' : true }".Replace("'", "\"");
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestSetSphericalTrueThenFalse()
        {
            var options = GeoNearOptions.SetSpherical(true);
            var expected = "{ 'spherical' : true }".Replace("'", "\"");
            Assert.AreEqual(expected, options.ToJson());

            options = GeoNearOptions.SetSpherical(false);
            expected = "{ }";
            Assert.AreEqual(expected, options.ToJson());
        }
    }
}
