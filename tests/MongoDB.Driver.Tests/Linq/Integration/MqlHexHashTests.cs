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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Integration;

public class MqlHexHashTests : LinqIntegrationTest<MqlHexHashTests.ClassFixture>
{
    public MqlHexHashTests(ClassFixture fixture)
        : base(fixture, server => server.Supports(Feature.HashOperator))
    {
    }

    [Theory]
    [InlineData("A12871FEE210FB8619291EAEA194581CBD2531E4B23759D225F6806923F63222", 1)]
    [InlineData(null, 3)]
    public void MqlHexHash_in_where(string hexHash, int expectedId)
    {
        var collection = Fixture.Collection;
        var queryable = collection.AsQueryable()
            .Where(d => Mql.HexHash(d.Data, MqlHashAlgorithm.SHA256) == hexHash);

        var renderedStages = Translate(collection, queryable);
        AssertStages(renderedStages, $"{{ $match : {{ $expr : {{ $eq : [{{ $hexHash : {{ input : '$Data', algorithm : 'sha256' }} }}, {(hexHash == null ? "null" : "'" + hexHash + "'")}] }} }} }}");

        var result = queryable.Single();
        result.Id.Should().Be(expectedId);
    }

    [Fact]
    public void MqlHexHash_in_select()
    {
        var collection = Fixture.Collection;
        var queryable = collection.AsQueryable()
            .Select(d => new { Hash = Mql.HexHash(d.Data, MqlHashAlgorithm.SHA256) });

        var renderedStages = Translate(collection, queryable);
        AssertStages(renderedStages, "{ $project : { Hash : { $hexHash : { input : '$Data', algorithm : 'sha256' } }, _id : 0 } }");

        var result = queryable.ToList();
        result.Select(d => d.Hash).Should().BeEquivalentTo("A12871FEE210FB8619291EAEA194581CBD2531E4B23759D225F6806923F63222", "C0EED9296A02FB06CDAC7FBB88C3579B8C4C803D32CF1B29A2D3794A3877BC3C", null);
    }

    [Fact]
    public async Task MqlHexHash_in_filter_builder()
    {
        var collection = Fixture.Collection;

        var filter = Builders<C>.Filter.Where(d => Mql.HexHash(d.Data, MqlHashAlgorithm.SHA256) == "A12871FEE210FB8619291EAEA194581CBD2531E4B23759D225F6806923F63222");
        var result = await collection.Find(filter).SingleAsync();

        var renderedFilter = Translate(collection, filter);
        renderedFilter.Should().Be("{ $expr : { $eq : [{ $hexHash : { input : '$Data', algorithm : 'sha256' } }, 'A12871FEE210FB8619291EAEA194581CBD2531E4B23759D225F6806923F63222' ]} }");
        result.Id.Should().Be(1);
    }

    [Fact]
    public async Task MqlHexHash_in_projection_builder()
    {
        var collection = Fixture.Collection;

        var projection = Builders<C>.Projection.Expression(c => new { Hash = Mql.HexHash(c.Data, MqlHashAlgorithm.SHA256) });
        var result = await collection.Find(Builders<C>.Filter.Empty).Project(projection).ToListAsync();

        var renderedProjection = TranslateFindProjection(collection, projection, null);
        renderedProjection.Should().Be("{ Hash : { $hexHash : { input : '$Data', algorithm : 'sha256' } }, _id : 0 }");

        result.Select(d => d.Hash).Should().BeEquivalentTo("A12871FEE210FB8619291EAEA194581CBD2531E4B23759D225F6806923F63222", "C0EED9296A02FB06CDAC7FBB88C3579B8C4C803D32CF1B29A2D3794A3877BC3C", null);
    }

    [Fact]
    public async Task MqlHexHash_in_aggregate()
    {
        var collection = Fixture.Collection;

        var pipeline = new EmptyPipelineDefinition<C>()
            .Match(d => Mql.HexHash(d.Data, MqlHashAlgorithm.SHA256) == "A12871FEE210FB8619291EAEA194581CBD2531E4B23759D225F6806923F63222")
            .Project(d => new { d.Id, Hash = Mql.HexHash(d.Data, MqlHashAlgorithm.SHA256) });

        var result = await collection.Aggregate(pipeline).SingleAsync();

        var renderedPipeline = Translate(collection, pipeline, null);
        AssertStages(
            renderedPipeline,
            "{ $match : { $expr : { $eq : [ { $hexHash : { input : '$Data', algorithm : 'sha256' } }, 'A12871FEE210FB8619291EAEA194581CBD2531E4B23759D225F6806923F63222' ] } } }",
            "{ $project : { _id : '$_id', Hash : { $hexHash : { input : '$Data', algorithm : 'sha256' } } } }");

        result.Hash.Should().Be("A12871FEE210FB8619291EAEA194581CBD2531E4B23759D225F6806923F63222");
    }

    public class C
    {
        public int Id { get; set; }

        [BsonIgnoreIfNull]
        public BsonBinaryData Data { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new() { Id = 1, Data = new BsonBinaryData([0x01, 0x02]) },
            new() { Id = 2, Data = new BsonBinaryData(Guid.Parse("E4A10FB8-7A83-494C-9710-29BBFFB1C262"), GuidRepresentation.Standard) },
            new() { Id = 3 },
        ];
    }
}
