/* Copyright 2010 10gen Inc.
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

using MongoDB.BsonLibrary;
using MongoDB.CSharpDriver;
using MongoDB.CSharpDriver.Builders;

namespace MongoDB.CSharpDriver.UnitTests.Builders {
    [TestFixture]
    public class IndexOptionsBuilderTests {
        [Test]
        public void TestBackground() {
            var options = IndexOptions.Background(true);
            string expected = "{ \"background\" : true }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestDropDups() {
            var options = IndexOptions.DropDups(true);
            string expected = "{ \"dropDups\" : true }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestGeoSpatialRange() {
            var options = IndexOptions.GeoSpatialRange(1.1, 2.2);
            string expected = "{ \"min\" : 1.1, \"max\" : 2.2 }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestName() {
            var options = IndexOptions.Name("custom");
            string expected = "{ \"name\" : \"custom\" }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestUnique() {
            var options = IndexOptions.Unique(true);
            string expected = "{ \"unique\" : true }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestNameBackground() {
            var options = IndexOptions.Name("custom").SetBackground(true);
            string expected = "{ \"name\" : \"custom\", \"background\" : true }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestNameDropDups() {
            var options = IndexOptions.Name("custom").SetDropDups(true);
            string expected = "{ \"name\" : \"custom\", \"dropDups\" : true }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestNameGeoSpatialRange() {
            var options = IndexOptions.Name("custom").SetGeoSpatialRange(1.1, 2.2);
            string expected = "{ \"name\" : \"custom\", \"min\" : 1.1, \"max\" : 2.2 }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestNameUnique() {
            var options = IndexOptions.Name("custom").SetUnique(true);
            string expected = "{ \"name\" : \"custom\", \"unique\" : true }";
            Assert.AreEqual(expected, options.ToJson());
        }
    }
}
