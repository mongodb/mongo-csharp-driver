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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Integration;

public class MqlToHashedIndexKeyTests : LinqIntegrationTest<MqlToHashedIndexKeyTests.ClassFixture>
{
    public MqlToHashedIndexKeyTests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void ToHashedIndexKey_in_select()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => Mql.ToHashedIndexKey(x.Value));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $toHashedIndexKey : '$Value' }, _id : 0 } }");

        var result = queryable.ToList();
        result.Should().HaveCount(3);
        result.Should().OnlyContain(v => v != 0);
    }

    [Fact]
    public void ToHashedIndexKey_in_where()
    {
        var collection = Fixture.Collection;

        var hashedValue = collection.AsQueryable()
            .Where(d => d.Id == 1)
            .Select(d => Mql.ToHashedIndexKey(d.Value))
            .Single();

        var queryable = collection.AsQueryable()
            .Where(d => Mql.ToHashedIndexKey(d.Value) == hashedValue);

        var stages = Translate(collection, queryable);
        AssertStages(stages, $"{{ $match : {{ $expr : {{ $eq : [{{ $toHashedIndexKey : '$Value' }}, {{ $numberLong : '{hashedValue}' }}] }} }} }}");

        var result = queryable.Single();
        result.Id.Should().Be(1);
    }

    [Fact]
    public void ToHashedIndexKey_with_int_field()
    {
        var collection = Fixture.Collection;
        var queryable = collection.AsQueryable()
            .Select(d => Mql.ToHashedIndexKey(d.IntValue));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $toHashedIndexKey : '$IntValue' }, _id : 0 } }");

        var result = queryable.ToList();
        result.Should().HaveCount(3);
        result.Should().OnlyContain(v => v != 0);
    }

    [Fact]
    public async Task ToHashedIndexKey_in_aggregate_pipeline()
    {
        var collection = Fixture.Collection;

        var pipeline = new EmptyPipelineDefinition<C>()
            .Project(d => new { d.Id, Hash = Mql.ToHashedIndexKey(d.Value) });

        var result = await collection.Aggregate(pipeline).ToListAsync();

        var stages = Translate(collection, pipeline, null);
        AssertStages(stages, "{ $project : { _id : '$_id', Hash : { $toHashedIndexKey : '$Value' } } }");

        result.Should().HaveCount(3);
    }

    public class C
    {
        public int Id { get; set; }
        public string Value { get; set; }
        public int IntValue { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new() { Id = 1, Value = "hello", IntValue = 42 },
            new() { Id = 2, Value = "world", IntValue = 99 },
            new() { Id = 3, Value = "test", IntValue = 7 },
        ];
    }
}
