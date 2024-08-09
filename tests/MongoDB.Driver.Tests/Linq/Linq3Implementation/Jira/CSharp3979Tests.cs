/* Copyright 2010-present MongoDB Inc.
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

using System.Linq;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp3979Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Inject_should_work()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("test");
            var collection = database.GetCollection<C>("test");

            var filter = Builders<C>.Filter.Eq(c => c.X, 1);
            var queryable = collection.AsQueryable().Where(x => filter.Inject());

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $match : { X : 1 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Inject_embedded_in_an_expression_should_work()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("test");
            var collection = database.GetCollection<C>("test");

            var filter = Builders<C>.Filter.Lte(c => c.X, 10);
            var queryable = collection.AsQueryable().Where(x => x.X >= 1 && filter.Inject());

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $match : { $and : [{ X : { $gte : 1 } }, { X : { $lte : 10 } }] } }"
            };
            AssertStages(stages, expectedStages);
        }

        private class C
        {
            public int Id { get; set; }
            public int X { get; set; }
        }
    }
}
