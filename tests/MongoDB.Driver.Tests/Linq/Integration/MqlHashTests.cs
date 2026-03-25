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

public class MqlHashTests : LinqIntegrationTest<MqlHashTests.ClassFixture>
{
    public MqlHashTests(ClassFixture fixture)
        : base(fixture, server => server.Supports(Feature.HashOperator))
    {
    }

    [Fact]
    public void MqlHash_in_where()
    {
        var collection = Fixture.Collection;
        var queryable = collection.AsQueryable()
            .Where(d => Mql.Hash(d.Data, MqlHashAlgorithm.SHA256) == new BsonBinaryData(Convert.FromBase64String("oShx/uIQ+4YZKR6uoZRYHL0lMeSyN1nSJfaAaSP2MiI="), BsonBinarySubType.Binary));

        var renderedStages = Translate(collection, queryable);
        AssertStages(renderedStages, "{ $match : { $expr : { $eq : [{ $hash : { input : '$Data', algorithm : 'sha256' } }, { $binary : { base64 : 'oShx/uIQ+4YZKR6uoZRYHL0lMeSyN1nSJfaAaSP2MiI=', subType : '00' } } ]} } }");

        var result = queryable.Single();
        result.Id.Should().Be(1);
    }

    [Fact]
    public void MqlHash_in_select()
    {
        var collection = Fixture.Collection;
        var queryable = collection.AsQueryable()
            .Select(d => new { Hash = Mql.Hash(d.Data, MqlHashAlgorithm.SHA256) });

        var renderedStages = Translate(collection, queryable);
        AssertStages(renderedStages, "{ $project : { Hash : { $hash : { input : '$Data', algorithm : 'sha256' } }, _id : 0 } }");

        var result = queryable.ToList();
        result.Select(d => d.Hash.ToString()).Should().BeEquivalentTo("Binary:0xa12871fee210fb8619291eaea194581cbd2531e4b23759d225f6806923f63222", "Binary:0xc0eed9296a02fb06cdac7fbb88c3579b8c4c803d32cf1b29a2d3794a3877bc3c");
    }

    [Fact]
    public async Task MqlHash_in_filter_builder()
    {
        var collection = Fixture.Collection;

        var filter = Builders<C>.Filter.Where(d => Mql.Hash(d.Data, MqlHashAlgorithm.SHA256) == new BsonBinaryData(Convert.FromBase64String("oShx/uIQ+4YZKR6uoZRYHL0lMeSyN1nSJfaAaSP2MiI="), BsonBinarySubType.Binary));
        var result = await collection.Find(filter).SingleAsync();

        var renderedFilter = Translate(collection, filter);
        renderedFilter.Should().Be("{ $expr : { $eq : [{ $hash : { input : '$Data', algorithm : 'sha256' } }, { $binary : { base64 : 'oShx/uIQ+4YZKR6uoZRYHL0lMeSyN1nSJfaAaSP2MiI=', subType : '00' } } ]} }");
        result.Id.Should().Be(1);
    }

    [Fact]
    public async Task MqlHash_in_projection_builder()
    {
        var collection = Fixture.Collection;

        var projection = Builders<C>.Projection.Expression(c => new { Hash = Mql.Hash(c.Data, MqlHashAlgorithm.SHA256) });
        var result = await collection.Find(Builders<C>.Filter.Empty).Project(projection).ToListAsync();

        var renderedProjection = TranslateFindProjection(collection, projection, null);
        renderedProjection.Should().Be("{ Hash : { $hash : { input : '$Data', algorithm : 'sha256' } }, _id : 0 }");

        result.Select(d => d.Hash.ToString()).Should().BeEquivalentTo("Binary:0xa12871fee210fb8619291eaea194581cbd2531e4b23759d225f6806923f63222", "Binary:0xc0eed9296a02fb06cdac7fbb88c3579b8c4c803d32cf1b29a2d3794a3877bc3c");
    }

    [Fact]
    public async Task MqlHash_in_aggregate()
    {
        var collection = Fixture.Collection;

        var pipeline = new EmptyPipelineDefinition<C>()
            .Match(d => Mql.Hash(d.Data, MqlHashAlgorithm.SHA256) == new BsonBinaryData(Convert.FromBase64String("oShx/uIQ+4YZKR6uoZRYHL0lMeSyN1nSJfaAaSP2MiI="), BsonBinarySubType.Binary))
            .Project(d => new { Hash = Mql.Hash(d.Data, MqlHashAlgorithm.SHA256) });

        var result = await collection.Aggregate(pipeline).SingleAsync();

        var renderedPipeline = Translate(collection, pipeline, null);
        AssertStages(
            renderedPipeline,
            "{ $match : { $expr : { $eq : [{ $hash : { input : '$Data', algorithm : 'sha256' } }, { $binary : { base64 : 'oShx/uIQ+4YZKR6uoZRYHL0lMeSyN1nSJfaAaSP2MiI=', subType : '00' } } ]} } }",
            "{ $project : { Hash : { $hash : { input : '$Data', algorithm : 'sha256' } }, _id : 0 } }");

        result.Hash.ToString().Should().Be("Binary:0xa12871fee210fb8619291eaea194581cbd2531e4b23759d225f6806923f63222");
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
            //new() { Id = 4 }, TODO: investigate why BsonIgnoreIfNull is not working on deserialization
        ];
    }
}
