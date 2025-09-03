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
using System.Text.RegularExpressions;
using FluentAssertions;
using MongoDB.Driver.Linq;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp1906Tests : LinqIntegrationTest<CSharp1906Tests.ClassFixture>
    {
        public CSharp1906Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Using_ToLower_should_work()
        {
            var collection = Fixture.Collection;
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
            var collection = Fixture.Collection;
            var regularExpresssion = new StringOrRegularExpression[] { new Regex("ABC", RegexOptions.IgnoreCase), new Regex("DEF", RegexOptions.IgnoreCase) };
            var queryable = collection.AsQueryable()
                .Where(c => c.S.StringIn(regularExpresssion));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { S : { $in : [/ABC/i, /DEF/i] } } }");

            var results = queryable.ToList().OrderBy(x => x.Id).ToList();
            results.Select(x => x.Id).Should().Equal(1, 2);
        }

        public class C
        {
            public int Id { get; set; }
            public string S { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData =>
            [
                new C { Id = 1, S = "aBc" },
                new C { Id = 2, S = "dEf" },
                new C { Id = 3, S = "gHi" }
            ];
        }
    }
}
