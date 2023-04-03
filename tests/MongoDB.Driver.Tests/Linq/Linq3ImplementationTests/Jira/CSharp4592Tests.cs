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
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4592Tests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void Project_dictionary_value_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.Dict["test"]);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { test : '$Dict.test', _id : 0 } }"); // LINQ2 translates slightly differently
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : '$Dict.test', _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(3, 0); // missing values deserialize as 0
        }

        private IMongoCollection<ExampleClass> CreateCollection(LinqProvider linqProvider)
        {
            var collection = GetCollection<ExampleClass>("test", linqProvider);
            CreateCollection(
                collection,
                new ExampleClass { Id = 1, Dict = new Dictionary<string, int> { ["test"] = 3 } },
                new ExampleClass { Id = 2, Dict = new Dictionary<string, int> { ["a"] = 4 } });
            return collection;
        }

        private class ExampleClass
        {
            public int Id { get; set; }
            public IDictionary<string, int> Dict { get; set; }
        }
    }
}
