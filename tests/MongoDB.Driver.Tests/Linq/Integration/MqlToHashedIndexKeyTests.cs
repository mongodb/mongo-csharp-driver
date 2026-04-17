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
        result.Should().Equal(5347277839332858538L, 1955367617033462209L, -3120687751959783844L);
    }

    [Fact]
    public void ToHashedIndexKey_in_where()
    {
        var collection = Fixture.Collection;

        var hashedValue = 5347277839332858538L; // hash of "hello"

        var queryable = collection.AsQueryable()
            .Where(d => Mql.ToHashedIndexKey(d.Value) == hashedValue);

        var stages = Translate(collection, queryable);
        AssertStages(stages, $"{{ $match : {{ $expr : {{ $eq : [{{ $toHashedIndexKey : '$Value' }}, {{ $numberLong : '{hashedValue}' }}] }} }} }}");

        var result = queryable.Single();
        result.Id.Should().Be(1);
    }

    [Fact]
    public async Task ToHashedIndexKey_in_aggregate_pipeline()
    {
        var collection = Fixture.Collection;

        var pipeline = new EmptyPipelineDefinition<C>()
            .Project(d => new { d.Id, Hash = Mql.ToHashedIndexKey(d.Value) });

        var stages = Translate(collection, pipeline, null);
        AssertStages(stages, "{ $project : { _id : '$_id', Hash : { $toHashedIndexKey : '$Value' } } }");

        var result = await collection.Aggregate(pipeline).ToListAsync();
        result.Should().HaveCount(3);
        result.Select(r => r.Hash).Should().Equal(5347277839332858538, 1955367617033462209, -3120687751959783844);
    }

    [Fact]
    public async Task ToHashedIndexKey_in_filter_builder()
    {
        var collection = Fixture.Collection;

        var hashedValue = 5347277839332858538L; // hash of "hello"

        var filter = Builders<C>.Filter.Where(d => Mql.ToHashedIndexKey(d.Value) == hashedValue);

        var renderedFilter = Translate(collection, filter);
        renderedFilter.Should().Be($"{{ $expr : {{ $eq : [{{ $toHashedIndexKey : '$Value' }}, {{ $numberLong : '{hashedValue}' }}] }} }}");

        var result = await collection.Find(filter).SingleAsync();
        result.Id.Should().Be(1);
    }

    [Fact]
    public async Task ToHashedIndexKey_in_projection_builder()
    {
        var collection = Fixture.Collection;

        var projection = Builders<C>.Projection.Expression(c => new { Hash = Mql.ToHashedIndexKey(c.Value) });

        var renderedProjection = TranslateFindProjection(collection, projection, null);
        renderedProjection.Should().Be("{ Hash : { $toHashedIndexKey : '$Value' }, _id : 0 }");

        var result = await collection.Find(Builders<C>.Filter.Empty).Project(projection).ToListAsync();
        result.Select(r => r.Hash).Should().Equal(5347277839332858538L, 1955367617033462209L, -3120687751959783844L);
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
