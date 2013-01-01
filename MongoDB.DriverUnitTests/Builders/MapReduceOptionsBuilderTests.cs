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
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Builders
{
    [TestFixture]
    public class MapReduceOptionsBuilderTests
    {
        [Test]
        public void TestNone()
        {
            var options = MapReduceOptions.Null;
            Assert.IsNull(options);
        }

        [Test]
        public void TestFinalize()
        {
            var options = MapReduceOptions.SetFinalize("code");
            var expected = "{ \"finalize\" : { \"$code\" : \"code\" } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestJSMode()
        {
            var options = MapReduceOptions.SetJSMode(true);
            var expected = "{ \"jsMode\" : true }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestKeepTemp()
        {
            var options = MapReduceOptions.SetKeepTemp(true);
            var expected = "{ \"keeptemp\" : true }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestLimit()
        {
            var options = MapReduceOptions.SetLimit(123);
            var expected = "{ \"limit\" : 123 }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestOut()
        {
            var options = MapReduceOptions.SetOutput("name");
            var expected = "{ \"out\" : \"name\" }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestOutReplace()
        {
            var options = MapReduceOptions.SetOutput(MapReduceOutput.Replace("name"));
            var expected = "{ \"out\" : \"name\" }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestOutReplaceSharded()
        {
            var options = MapReduceOptions.SetOutput(MapReduceOutput.Replace("name", true));
            var expected = "{ \"out\" : { \"replace\" : \"name\", \"sharded\" : true } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestOutReplaceWithDatabase()
        {
            var options = MapReduceOptions.SetOutput(MapReduceOutput.Replace("database", "name"));
            var expected = "{ \"out\" : { \"replace\" : \"name\", \"db\" : \"database\" } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestOutReplaceWithDatabaseSharded()
        {
            var options = MapReduceOptions.SetOutput(MapReduceOutput.Replace("database", "name", true));
            var expected = "{ \"out\" : { \"replace\" : \"name\", \"db\" : \"database\", \"sharded\" : true } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestOutMerge()
        {
            var options = MapReduceOptions.SetOutput(MapReduceOutput.Merge("name"));
            var expected = "{ \"out\" : { \"merge\" : \"name\" } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestOutMergeSharded()
        {
            var options = MapReduceOptions.SetOutput(MapReduceOutput.Merge("name", true));
            var expected = "{ \"out\" : { \"merge\" : \"name\", \"sharded\" : true } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestOutMergeWithDatabase()
        {
            var options = MapReduceOptions.SetOutput(MapReduceOutput.Merge("database", "name"));
            var expected = "{ \"out\" : { \"merge\" : \"name\", \"db\" : \"database\" } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestOutMergeWithDatabaseSharded()
        {
            var options = MapReduceOptions.SetOutput(MapReduceOutput.Merge("database", "name", true));
            var expected = "{ \"out\" : { \"merge\" : \"name\", \"db\" : \"database\", \"sharded\" : true } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestOutReduce()
        {
            var options = MapReduceOptions.SetOutput(MapReduceOutput.Reduce("name"));
            var expected = "{ \"out\" : { \"reduce\" : \"name\" } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestOutReduceSharded()
        {
            var options = MapReduceOptions.SetOutput(MapReduceOutput.Reduce("name", true));
            var expected = "{ \"out\" : { \"reduce\" : \"name\", \"sharded\" : true } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestOutReduceWithDatabase()
        {
            var options = MapReduceOptions.SetOutput(MapReduceOutput.Reduce("database", "name"));
            var expected = "{ \"out\" : { \"reduce\" : \"name\", \"db\" : \"database\" } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestOutReduceWithDatabaseSharded()
        {
            var options = MapReduceOptions.SetOutput(MapReduceOutput.Reduce("database", "name", true));
            var expected = "{ \"out\" : { \"reduce\" : \"name\", \"db\" : \"database\", \"sharded\" : true } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestOutInline()
        {
            var options = MapReduceOptions.SetOutput(MapReduceOutput.Inline);
            var expected = "{ \"out\" : { \"inline\" : 1 } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestQuery()
        {
            var options = MapReduceOptions.SetQuery(Query.EQ("x", 1));
            var expected = "{ \"query\" : { \"x\" : 1 } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestScope()
        {
            var options = MapReduceOptions.SetScope(new ScopeDocument("x", 1));
            var expected = "{ \"scope\" : { \"x\" : 1 } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestSortWithBuilder()
        {
            var options = MapReduceOptions.SetSortOrder(SortBy.Ascending("a", "b"));
            var expected = "{ \"sort\" : { \"a\" : 1, \"b\" : 1 } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestSortWithKeys()
        {
            var options = MapReduceOptions.SetSortOrder("a", "b");
            var expected = "{ \"sort\" : { \"a\" : 1, \"b\" : 1 } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestVerbose()
        {
            var options = MapReduceOptions.SetVerbose(true);
            var expected = "{ \"verbose\" : true }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestQueryAndFinalize()
        {
            var options = MapReduceOptions.SetQuery(Query.EQ("x", 1)).SetFinalize("code");
            var expected = "{ \"query\" : { \"x\" : 1 }, \"finalize\" : { \"$code\" : \"code\" } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestQueryAndJSMode()
        {
            var options = MapReduceOptions.SetQuery(Query.EQ("x", 1)).SetJSMode(true);
            var expected = "{ \"query\" : { \"x\" : 1 }, \"jsMode\" : true }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestQueryAndKeepTemp()
        {
            var options = MapReduceOptions.SetQuery(Query.EQ("x", 1)).SetKeepTemp(true);
            var expected = "{ \"query\" : { \"x\" : 1 }, \"keeptemp\" : true }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestQueryAndLimit()
        {
            var options = MapReduceOptions.SetQuery(Query.EQ("x", 1)).SetLimit(123);
            var expected = "{ \"query\" : { \"x\" : 1 }, \"limit\" : 123 }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestQueryAndOut()
        {
            var options = MapReduceOptions.SetQuery(Query.EQ("x", 1)).SetOutput("name");
            var expected = "{ \"query\" : { \"x\" : 1 }, \"out\" : \"name\" }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestQueryAndScope()
        {
            var options = MapReduceOptions.SetQuery(Query.EQ("x", 1)).SetScope(new ScopeDocument("x", 1));
            var expected = "{ \"query\" : { \"x\" : 1 }, \"scope\" : { \"x\" : 1 } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestQueryAndSortWithBuilder()
        {
            var options = MapReduceOptions.SetQuery(Query.EQ("x", 1)).SetSortOrder(SortBy.Ascending("a", "b"));
            var expected = "{ \"query\" : { \"x\" : 1 }, \"sort\" : { \"a\" : 1, \"b\" : 1 } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestQueryAndSortWithKeys()
        {
            var options = MapReduceOptions.SetQuery(Query.EQ("x", 1)).SetSortOrder("a", "b");
            var expected = "{ \"query\" : { \"x\" : 1 }, \"sort\" : { \"a\" : 1, \"b\" : 1 } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestQueryAndVerbose()
        {
            var options = MapReduceOptions.SetQuery(Query.EQ("x", 1)).SetVerbose(true);
            var expected = "{ \"query\" : { \"x\" : 1 }, \"verbose\" : true }";
            Assert.AreEqual(expected, options.ToJson());
        }
    }
}
