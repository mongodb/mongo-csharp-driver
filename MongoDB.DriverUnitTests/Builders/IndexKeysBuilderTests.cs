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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Builders;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Builders
{
    [TestFixture]
    public class IndexKeysBuilderTests
    {
        private class Test
        {
            [BsonElement("a")]
            public string A { get; set; }

            [BsonElement("b")]
            public string B { get; set; }
        }

        [Test]
        public void TestAscending1()
        {
            var keys = IndexKeys.Ascending("a");
            string expected = "{ \"a\" : 1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestAscending1_Typed()
        {
            var keys = IndexKeys<Test>.Ascending(x => x.A);
            string expected = "{ \"a\" : 1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestAscending2()
        {
            var keys = IndexKeys.Ascending("a", "b");
            string expected = "{ \"a\" : 1, \"b\" : 1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestAscending2_Typed()
        {
            var keys = IndexKeys<Test>.Ascending(x => x.A, x => x.B);
            string expected = "{ \"a\" : 1, \"b\" : 1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestAscendingAscending()
        {
            var keys = IndexKeys.Ascending("a").Ascending("b");
            string expected = "{ \"a\" : 1, \"b\" : 1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestAscendingAscending_Typed()
        {
            var keys = IndexKeys<Test>.Ascending(x => x.A).Ascending(x => x.B);
            string expected = "{ \"a\" : 1, \"b\" : 1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestAscendingDescending()
        {
            var keys = IndexKeys.Ascending("a").Descending("b");
            string expected = "{ \"a\" : 1, \"b\" : -1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestAscendingDescending_Typed()
        {
            var keys = IndexKeys<Test>.Ascending(x => x.A).Descending(x => x.B);
            string expected = "{ \"a\" : 1, \"b\" : -1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestDescending1()
        {
            var keys = IndexKeys.Descending("a");
            string expected = "{ \"a\" : -1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestDescending1_Typed()
        {
            var keys = IndexKeys<Test>.Descending(x => x.A);
            string expected = "{ \"a\" : -1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestDescending2()
        {
            var keys = IndexKeys.Descending("a", "b");
            string expected = "{ \"a\" : -1, \"b\" : -1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestDescending2_Typed()
        {
            var keys = IndexKeys<Test>.Descending(x => x.A, x => x.B);
            string expected = "{ \"a\" : -1, \"b\" : -1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestDescendingAscending()
        {
            var keys = IndexKeys.Descending("a").Ascending("b");
            string expected = "{ \"a\" : -1, \"b\" : 1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestDescendingAscending_Typed()
        {
            var keys = IndexKeys<Test>.Descending(x => x.A).Ascending(x => x.B);
            string expected = "{ \"a\" : -1, \"b\" : 1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestDescendingDescending()
        {
            var keys = IndexKeys.Descending("a").Descending("b");
            string expected = "{ \"a\" : -1, \"b\" : -1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestDescendingDescending_Typed()
        {
            var keys = IndexKeys<Test>.Descending(x => x.A).Descending(x => x.B);
            string expected = "{ \"a\" : -1, \"b\" : -1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestGeoSpatial()
        {
            var keys = IndexKeys.GeoSpatial("a");
            string expected = "{ \"a\" : \"2d\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestGeoSpatial_Typed()
        {
            var keys = IndexKeys<Test>.GeoSpatial(x => x.A);
            string expected = "{ \"a\" : \"2d\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestGeoSpatialAscending()
        {
            var keys = IndexKeys.GeoSpatial("a").Ascending("b");
            string expected = "{ \"a\" : \"2d\", \"b\" : 1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestGeoSpatialAscending_Typed()
        {
            var keys = IndexKeys<Test>.GeoSpatial(x => x.A).Ascending(x => x.B);
            string expected = "{ \"a\" : \"2d\", \"b\" : 1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestAscendingGeoSpatial()
        {
            var keys = IndexKeys.Ascending("a").GeoSpatial("b");
            string expected = "{ \"a\" : 1, \"b\" : \"2d\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestAscendingGeoSpatial_Typed()
        {
            var keys = IndexKeys<Test>.Ascending(x => x.A).GeoSpatial(x => x.B);
            string expected = "{ \"a\" : 1, \"b\" : \"2d\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestGeoSpatialSpherical()
        {
            var keys = IndexKeys.GeoSpatialSpherical("a");
            string expected = "{ \"a\" : \"2dsphere\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestGeoSpatialSpherical_Typed()
        {
            var keys = IndexKeys<Test>.GeoSpatialSpherical(x => x.A);
            string expected = "{ \"a\" : \"2dsphere\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestGeoSpatialSphericalAscending()
        {
            var keys = IndexKeys.GeoSpatialSpherical("a").Ascending("b");
            string expected = "{ \"a\" : \"2dsphere\", \"b\" : 1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestGeoSpatialSphericalAscending_Typed()
        {
            var keys = IndexKeys<Test>.GeoSpatialSpherical(x => x.A).Ascending(x => x.B);
            string expected = "{ \"a\" : \"2dsphere\", \"b\" : 1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestAscendingGeoSpatialSpherical()
        {
            var keys = IndexKeys.Ascending("a").GeoSpatialSpherical("b");
            string expected = "{ \"a\" : 1, \"b\" : \"2dsphere\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestAscendingGeoSpatialSpherical_Typed()
        {
            var keys = IndexKeys<Test>.Ascending(x => x.A).GeoSpatialSpherical(x => x.B);
            string expected = "{ \"a\" : 1, \"b\" : \"2dsphere\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestHashed()
        {
            var keys = IndexKeys.Hashed("a");
            string expected = "{ \"a\" : \"hashed\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestHashed_Typed()
        {
            var keys = IndexKeys<Test>.Hashed(x => x.A);
            string expected = "{ \"a\" : \"hashed\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestHashedAscending()
        {
            var keys = IndexKeys.Hashed("a").Ascending("b");
            string expected = "{ \"a\" : \"hashed\", \"b\" : 1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestHashedAscending_Typed()
        {
            var keys = IndexKeys<Test>.Hashed(x => x.A).Ascending(x => x.B);
            string expected = "{ \"a\" : \"hashed\", \"b\" : 1 }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestAscendingHashed()
        {
            var keys = IndexKeys.Ascending("a").Hashed("b");
            string expected = "{ \"a\" : 1, \"b\" : \"hashed\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }

        [Test]
        public void TestAscendingHashed_Typed()
        {
            var keys = IndexKeys<Test>.Ascending(x => x.A).Hashed(x => x.B);
            string expected = "{ \"a\" : 1, \"b\" : \"hashed\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }
    }
}