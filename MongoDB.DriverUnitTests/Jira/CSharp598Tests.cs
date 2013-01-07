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

using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Jira.CSharp598
{
    [TestFixture]
    public class CSharp598Tests
    {
        [Test]
        public void TestVariableWorksForQuery()
        {
            int index = 10;
            IMongoQuery query = null;
            Assert.DoesNotThrow(() =>
            {
                query = Query<TestClass>.EQ(x => x.List[index].Name, "Blah");
            });

            Assert.AreEqual("{ \"List.10.Name\" : \"Blah\" }", query.ToString());
        }

        [Test]
        public void TestVariableWorksForUpdate()
        {
            int index = 10;
            IMongoUpdate update = null;
            Assert.DoesNotThrow(() =>
            {
                update = Update<TestClass>.Set(x => x.List[index].Name, "Blah");
            });

            Assert.AreEqual("{ \"$set\" : { \"List.10.Name\" : \"Blah\" } }", update.ToString());
        }

        [Test]
        public void TestVariableWorksForQueryWithVariableChange()
        {
            int index = 10;
            IMongoQuery query = null;
            var queryBuilder = new QueryBuilder<TestClass>();
            Assert.DoesNotThrow(() =>
            {
                query = queryBuilder.EQ(x => x.List[index].Name, "Blah");
                index = 11;
                query = queryBuilder.EQ(x => x.List[index].Name, "Blah");
            });

            Assert.AreEqual("{ \"List.11.Name\" : \"Blah\" }", query.ToString());
        }

        private class TestClass
        {
            public ObjectId Id { get; set; }

            public List<SubTestClass> List { get; set; }
        }
        private class SubTestClass
        {
            public string Name { get; set; }
        }
    }
}