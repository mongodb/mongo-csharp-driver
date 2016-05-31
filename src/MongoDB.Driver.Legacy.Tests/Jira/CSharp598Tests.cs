/* Copyright 2010-2015 MongoDB Inc.
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
using Xunit;

namespace MongoDB.Driver.Tests.Jira.CSharp598
{
    public class CSharp598Tests
    {
        [Fact]
        public void TestVariableWorksForQuery()
        {
            int index = 10;

            IMongoQuery query = Query<TestClass>.EQ(x => x.List[index].Name, "Blah");

            Assert.Equal("{ \"List.10.Name\" : \"Blah\" }", query.ToString());
        }

        [Fact]
        public void TestVariableWorksForUpdate()
        {
            int index = 10;

            IMongoUpdate update = Update<TestClass>.Set(x => x.List[index].Name, "Blah");

            Assert.Equal("{ \"$set\" : { \"List.10.Name\" : \"Blah\" } }", update.ToString());
        }

        [Fact]
        public void TestVariableWorksForQueryWithVariableChange()
        {
            var queryBuilder = new QueryBuilder<TestClass>();

            int index = 10;
            IMongoQuery query = queryBuilder.EQ(x => x.List[index].Name, "Blah");

            index = 11;
            query = queryBuilder.EQ(x => x.List[index].Name, "Blah");

            Assert.Equal("{ \"List.11.Name\" : \"Blah\" }", query.ToString());
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