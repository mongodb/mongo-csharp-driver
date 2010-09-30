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
    public class MapReduceOptionsBuilderTests {
        [Test]
        public void TestNone() {
            var options = MapReduceOptions.None;
            Assert.IsNull(options);
        }

        [Test]
        public void TestFinalize() {
            var options = MapReduceOptions.Finalize("code");
            var expected = "{ \"finalize\" : { \"$code\" : \"code\" } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestKeepTemp() {
            var options = MapReduceOptions.KeepTemp(true);
            var expected = "{ \"keeptemp\" : true }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestLimit() {
            var options = MapReduceOptions.Limit(123);
            var expected = "{ \"limit\" : 123 }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestOut() {
            var options = MapReduceOptions.Out("name");
            var expected = "{ \"out\" : \"name\" }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestQuery() {
            var options = MapReduceOptions.Query(Query.EQ("x", 1));
            var expected = "{ \"query\" : { \"x\" : 1 } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestScope() {
            var options = MapReduceOptions.Scope(new BsonDocument("x", 1));
            var expected = "{ \"scope\" : { \"x\" : 1 } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestSortWithBuilder() {
            var options = MapReduceOptions.Sort(SortBy.Ascending("a", "b"));
            var expected = "{ \"sort\" : { \"a\" : 1, \"b\" : 1 } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestSortWithKeys() {
            var options = MapReduceOptions.Sort("a", "b");
            var expected = "{ \"sort\" : { \"a\" : 1, \"b\" : 1 } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestVerbose() {
            var options = MapReduceOptions.Verbose(true);
            var expected = "{ \"verbose\" : true }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestQueryAndFinalize() {
            var options = MapReduceOptions.Query(Query.EQ("x", 1)).Finalize("code");
            var expected = "{ \"query\" : { \"x\" : 1 }, \"finalize\" : { \"$code\" : \"code\" } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestQueryAndKeepTemp() {
            var options = MapReduceOptions.Query(Query.EQ("x", 1)).KeepTemp(true);
            var expected = "{ \"query\" : { \"x\" : 1 }, \"keeptemp\" : true }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestQueryAndLimit() {
            var options = MapReduceOptions.Query(Query.EQ("x", 1)).Limit(123);
            var expected = "{ \"query\" : { \"x\" : 1 }, \"limit\" : 123 }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestQueryAndOut() {
            var options = MapReduceOptions.Query(Query.EQ("x", 1)).Out("name");
            var expected = "{ \"query\" : { \"x\" : 1 }, \"out\" : \"name\" }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestQueryAndScope() {
            var options = MapReduceOptions.Query(Query.EQ("x", 1)).Scope(new BsonDocument("x", 1));
            var expected = "{ \"query\" : { \"x\" : 1 }, \"scope\" : { \"x\" : 1 } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestQueryAndSortWithBuilder() {
            var options = MapReduceOptions.Query(Query.EQ("x", 1)).Sort(SortBy.Ascending("a", "b"));
            var expected = "{ \"query\" : { \"x\" : 1 }, \"sort\" : { \"a\" : 1, \"b\" : 1 } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestQueryAndSortWithKeys() {
            var options = MapReduceOptions.Query(Query.EQ("x", 1)).Sort("a", "b");
            var expected = "{ \"query\" : { \"x\" : 1 }, \"sort\" : { \"a\" : 1, \"b\" : 1 } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestQueryAndVerbose() {
            var options = MapReduceOptions.Query(Query.EQ("x", 1)).Verbose(true);
            var expected = "{ \"query\" : { \"x\" : 1 }, \"verbose\" : true }";
            Assert.AreEqual(expected, options.ToJson());
        }
    }
}
