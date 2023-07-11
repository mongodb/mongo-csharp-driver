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
using FluentAssertions;
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4700Tests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void OrderBy_Count_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .GroupBy(x => x.Name)
                .OrderBy(x => x.Count());

            var stages = Translate(collection, queryable);
            var results = queryable.ToList();

            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(
                    stages,
                    "{ $group : { _id : '$Name', __agg0 : { $sum : 1 } } }",
                    "{ $sort : { __agg0 : 1 } }");

                results.Should().HaveCount(2);
                results[0].Key.Should().Be("Jane");
                results[0].Count().Should().Be(0); // this result is incorrect in LINQ2
                results[1].Key.Should().Be("John");
                results[1].Count().Should().Be(0); // this result is incorrect in LINQ2
            }
            else
            {
                AssertStages(
                    stages,
                    "{ $group : { _id : '$Name', _elements : { $push : '$$ROOT' } } }",
                    "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $size : '$_elements' } } }",
                    "{ $sort : { _key1 : 1 } }",
                    "{ $replaceRoot : { newRoot : '$_document' } }");

                results.Should().HaveCount(2);
                results[0].Key.Should().Be("Jane");
                results[0].Count().Should().Be(1);
                results[1].Key.Should().Be("John");
                results[1].Count().Should().Be(2);
            }
        }

        private IMongoCollection<C> GetCollection(LinqProvider linqProvider)
        {
            var collection = GetCollection<C>("test", linqProvider);
            CreateCollection(
                collection,
                new C { Id = 1, Name = "John" },
                new C { Id = 2, Name = "John" },
                new C { Id = 6, Name = "Jane" });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

    }
}
