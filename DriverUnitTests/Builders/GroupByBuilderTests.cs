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
    public class GroupByBuilderTests
    {
        [Test]
        public void Test1Key()
        {
            var groupBy = GroupBy.Keys("a");
            string expected = "{ \"a\" : 1 }";
            Assert.AreEqual(expected, groupBy.ToJson());
        }

        [Test]
        public void Test2Keys()
        {
            var groupBy = GroupBy.Keys("a", "b");
            string expected = "{ \"a\" : 1, \"b\" : 1 }";
            Assert.AreEqual(expected, groupBy.ToJson());
        }

        [Test]
        public void Test3Keys()
        {
            var groupBy = GroupBy.Keys("a", "b", "c");
            string expected = "{ \"a\" : 1, \"b\" : 1, \"c\" : 1 }";
            Assert.AreEqual(expected, groupBy.ToJson());
        }

        [Test]
        public void TestFunction()
        {
            var groupBy = GroupBy.Function("this.age >= 21");
            string expected = "new BsonJavaScript(\"this.age >= 21\")";
            Assert.AreEqual(expected, groupBy.ToString());
        }
    }
}
