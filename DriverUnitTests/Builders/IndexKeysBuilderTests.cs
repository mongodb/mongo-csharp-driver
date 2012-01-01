/* Copyright 2010-2012 10gen Inc.
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace MongoDB.DriverUnitTests.Builders
{
    [TestFixture]
    public class IndexKeysBuilderTests
    {
        [Test]
        public void TestAscending1()
        {
            var keys = IndexKeys.Ascending("a");
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
        public void TestAscendingAscending()
        {
            var keys = IndexKeys.Ascending("a").Ascending("b");
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
        public void TestDescending1()
        {
            var keys = IndexKeys.Descending("a");
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
        public void TestDescendingAscending()
        {
            var keys = IndexKeys.Descending("a").Ascending("b");
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
        public void TestGeoSpatial()
        {
            var keys = IndexKeys.GeoSpatial("a");
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
        public void TestAscendingGeoSpatial()
        {
            var keys = IndexKeys.Ascending("a").GeoSpatial("b");
            string expected = "{ \"a\" : 1, \"b\" : \"2d\" }";
            Assert.AreEqual(expected, keys.ToJson());
        }
    }
}
