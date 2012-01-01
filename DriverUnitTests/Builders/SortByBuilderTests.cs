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
    public class SortByBuilderTests
    {
        [Test]
        public void TestAscending1()
        {
            var sortBy = SortBy.Ascending("a");
            string expected = "{ \"a\" : 1 }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }

        [Test]
        public void TestAscending2()
        {
            var sortBy = SortBy.Ascending("a", "b");
            string expected = "{ \"a\" : 1, \"b\" : 1 }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }

        [Test]
        public void TestAscendingAscending()
        {
            var sortBy = SortBy.Ascending("a").Ascending("b");
            string expected = "{ \"a\" : 1, \"b\" : 1 }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }

        [Test]
        public void TestAscendingDescending()
        {
            var sortBy = SortBy.Ascending("a").Descending("b");
            string expected = "{ \"a\" : 1, \"b\" : -1 }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }

        [Test]
        public void TestDescending1()
        {
            var sortBy = SortBy.Descending("a");
            string expected = "{ \"a\" : -1 }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }

        [Test]
        public void TestDescending2()
        {
            var sortBy = SortBy.Descending("a", "b");
            string expected = "{ \"a\" : -1, \"b\" : -1 }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }

        [Test]
        public void TestDescendingAscending()
        {
            var sortBy = SortBy.Descending("a").Ascending("b");
            string expected = "{ \"a\" : -1, \"b\" : 1 }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }

        [Test]
        public void TestDescendingDescending()
        {
            var sortBy = SortBy.Descending("a").Descending("b");
            string expected = "{ \"a\" : -1, \"b\" : -1 }";
            Assert.AreEqual(expected, sortBy.ToJson());
        }
    }
}
