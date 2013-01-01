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

using System;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Builders
{
    [TestFixture]
    public class IndexOptionsBuilderTests
    {
        [Test]
        public void TestBackground()
        {
            var options = IndexOptions.SetBackground(true);
            string expected = "{ \"background\" : true }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestDropDups()
        {
            var options = IndexOptions.SetDropDups(true);
            string expected = "{ \"dropDups\" : true }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestGeoSpatialRange()
        {
            var options = IndexOptions.SetGeoSpatialRange(1.1, 2.2);
            string expected = "{ \"min\" : 1.1, \"max\" : 2.2 }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestName()
        {
            var options = IndexOptions.SetName("custom");
            string expected = "{ \"name\" : \"custom\" }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestSparse()
        {
            var options = IndexOptions.SetSparse(true);
            string expected = "{ \"sparse\" : true }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestTimeToLive()
        {
            var options = IndexOptions.SetTimeToLive(TimeSpan.FromHours(1));
            string expected = "{ \"expireAfterSeconds\" : 3600 }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestUnique()
        {
            var options = IndexOptions.SetUnique(true);
            string expected = "{ \"unique\" : true }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestNameBackground()
        {
            var options = IndexOptions.SetName("custom").SetBackground(true);
            string expected = "{ \"name\" : \"custom\", \"background\" : true }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestNameDropDups()
        {
            var options = IndexOptions.SetName("custom").SetDropDups(true);
            string expected = "{ \"name\" : \"custom\", \"dropDups\" : true }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestNameGeoSpatialRange()
        {
            var options = IndexOptions.SetName("custom").SetGeoSpatialRange(1.1, 2.2);
            string expected = "{ \"name\" : \"custom\", \"min\" : 1.1, \"max\" : 2.2 }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestNameUnique()
        {
            var options = IndexOptions.SetName("custom").SetUnique(true);
            string expected = "{ \"name\" : \"custom\", \"unique\" : true }";
            Assert.AreEqual(expected, options.ToJson());
        }
    }
}
