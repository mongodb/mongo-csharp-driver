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
using MongoDB.Driver.TestHelpers;
using FluentAssertions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp5463Tests : LinqIntegrationTest<CSharp5463Tests.ClassFixture>
{
    public CSharp5463Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Nested_AsQueryable_Contains_constant_should_be_partially_evaluated()
    {
        var collection = Fixture.Collection;
        var enumerable = new int[] { 1, 2, 3 };

        var queryable = collection.AsQueryable()
            .Select(x => enumerable.AsQueryable().Contains(1));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $in : [1, [1, 2, 3]] }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Be(true);
    }

    [Fact]
    public void NestedQueryable_Contains_constant_should_be_partially_evaluated()
    {
        var collection = Fixture.Collection;
        var nestedQueryable = new int[] { 1, 2, 3 }.AsQueryable();

        var queryable = collection.AsQueryable()
            .Select(x => nestedQueryable.Contains(1)); // PartialEvaluator turns this into Select(x => true)

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $literal : true }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Be(true);
    }

    [Fact]
    public void NestedQueryable_Contains_field_should_throw()
    {
        var collection = Fixture.Collection;
        var nestedQueryable = new int[] { 1, 2, 3 }.AsQueryable();

        var queryable = collection.AsQueryable()
            .Select(x => nestedQueryable.Contains(x.Id));

        var exception = Record.Exception(() => Translate(collection, queryable));
        exception.Should().BeOfType<ExpressionNotSupportedException>();
        exception.Message.Should().Contain("source argument is an unsupported IQueryable type");
    }

    public class C
    {
        public int Id { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new C { Id = 1 }
        ];
    }
}
