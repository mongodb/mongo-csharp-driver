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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp2308Tests
    {
        [Fact]
        public void Nested_Select_should_work()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("FooBar");
            var collection = database.GetCollection<FooNest>("Foos");

            database.DropCollection("Foos");

            collection.InsertOne(
                new FooNest
                {
                    Name = "Parent",
                    NestedCollection = new[] {
                        new FooNest {
                            Name = "Child"
                        }
                    }
                });

            var queryable = collection.AsQueryable()
                .Select(top => top.NestedCollection.Select(child => new { ParentName = top.Name, child.Name }));

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $map : { input : '$NestedCollection', as : 'child', in : { ParentName : '$Name', Name : '$$child.Name' } } }, _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);

            var pipelineDefinition = new BsonDocumentStagePipelineDefinition<FooNest, BsonDocument>(stages);
            var resultAsDocument = collection.Aggregate(pipelineDefinition).ToList().Single();
            resultAsDocument.Should().Be("{ _v : [{ ParentName : 'Parent', Name : 'Child' }] }");

            var result = queryable.ToList().Single().ToList();
            result.Should().HaveCount(1);
            result[0].ParentName.Should().Be("Parent");
            result[0].Name.Should().Be("Child");
        }

        public class FooNest
        {
            public string Name;
            public IEnumerable<FooNest> NestedCollection;
        }
    }
}
