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
    public class OrderByBuilderTests {
        [Test]
        public void TestAscending1() {
            var orderBy = OrderBy.Ascending("a");
            string expected = "{ \"a\" : 1 }";
            Assert.AreEqual(expected, orderBy.ToJson());
        }

        [Test]
        public void TestAscending2() {
            var orderBy = OrderBy.Ascending("a", "b");
            string expected = "{ \"a\" : 1, \"b\" : 1 }";
            Assert.AreEqual(expected, orderBy.ToJson());
        }

        [Test]
        public void TestAscendingAscending() {
            var orderBy = OrderBy.Ascending("a").Ascending("b");
            string expected = "{ \"a\" : 1, \"b\" : 1 }";
            Assert.AreEqual(expected, orderBy.ToJson());
        }

        [Test]
        public void TestAscendingDescending() {
            var orderBy = OrderBy.Ascending("a").Descending("b");
            string expected = "{ \"a\" : 1, \"b\" : -1 }";
            Assert.AreEqual(expected, orderBy.ToJson());
        }

        [Test]
        public void TestDescending1() {
            var orderBy = OrderBy.Descending("a");
            string expected = "{ \"a\" : -1 }";
            Assert.AreEqual(expected, orderBy.ToJson());
        }

        [Test]
        public void TestDescending2() {
            var orderBy = OrderBy.Descending("a", "b");
            string expected = "{ \"a\" : -1, \"b\" : -1 }";
            Assert.AreEqual(expected, orderBy.ToJson());
        }

        [Test]
        public void TestDescendingAscending() {
            var orderBy = OrderBy.Descending("a").Ascending("b");
            string expected = "{ \"a\" : -1, \"b\" : 1 }";
            Assert.AreEqual(expected, orderBy.ToJson());
        }

        [Test]
        public void TestDescendingDescending() {
            var orderBy = OrderBy.Descending("a").Descending("b");
            string expected = "{ \"a\" : -1, \"b\" : -1 }";
            Assert.AreEqual(expected, orderBy.ToJson());
        }
    }
}
