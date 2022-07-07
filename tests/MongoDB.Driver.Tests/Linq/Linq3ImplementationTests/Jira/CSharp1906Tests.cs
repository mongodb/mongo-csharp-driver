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

using System;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp1906Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Using_ToLower_should_work()
        {
            var collection = CreateCollection();
            var lowerCaseValues = new[] { "abc", "def" }; // ensure all are lower case at compile time
            var queryable = collection.AsQueryable()
                .Where(c => lowerCaseValues.Contains(c.S.ToLower()));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { $expr : { $in : [{ $toLower : '$S' }, ['abc', 'def']] } } }");

            var results = queryable.ToList().OrderBy(x => x.Id).ToList();
            results.Select(x => x.Id).Should().Equal(1, 2);
        }

        [Fact]
        public void Using_regular_expression_should_work()
        {
            var collection = CreateCollection();
            var regularExpresssion = new StringOrRegularExpression[] { new Regex("ABC", RegexOptions.IgnoreCase), new Regex("DEF", RegexOptions.IgnoreCase) };
            var queryable = collection.AsQueryable()
                .Where(c => c.S.StringIn(regularExpresssion));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { S : { $in : [/ABC/i, /DEF/i] } } }");

            var results = queryable.ToList().OrderBy(x => x.Id).ToList();
            results.Select(x => x.Id).Should().Equal(1, 2);
        }

        private IMongoCollection<C> CreateCollection()
        {
            var collection = GetCollection<C>();

            var documents = new[]
            {
                new C { Id = 1, S = "aBc" },
                new C { Id = 2, S = "dEf" },
                new C { Id = 3, S = "gHi" }
            };
            CreateCollection(collection, documents);

            return collection;
        }

        public class C
        {
            public int Id { get; set; }
            public string S { get; set; }
        }
    }
}
