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
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Integration;

public class ObjectIdGenerateNewIdTests : LinqIntegrationTest<ObjectIdGenerateNewIdTests.ClassFixture>
{
    public ObjectIdGenerateNewIdTests(ClassFixture fixture)
        : base(fixture, server => server.Supports(Feature.CreateObjectIdExpression))
    {
    }

    [Fact]
    public void ObjectIdGenerateNewId_in_where()
    {
        var collection = Fixture.Collection;
        var queryable = collection.AsQueryable()
            .Where(d => d.Id == ObjectId.GenerateNewId());

        var renderedStages = Translate(collection, queryable);
        AssertStages(renderedStages, "{ $match : { $expr : { $eq : ['$_id', { $createObjectId : {} }] } } }");

        var result = queryable.ToList();
        result.Should().BeEmpty();
    }

    [Fact]
    public void ObjectIdGenerateNewId_in_select()
    {
        var collection = Fixture.Collection;
        var queryable = collection.AsQueryable()
            .Select(d => new { NewId = ObjectId.GenerateNewId() });

        var renderedStages = Translate(collection, queryable);
        AssertStages(renderedStages, "{ $project : { 'NewId' : { $createObjectId : {} }, '_id' : 0 } }");

        var result = queryable.ToList();
        var firstId = result.First().NewId;
        var secondId = result.Skip(1).First().NewId;

        firstId.Should().NotBe(default);
        secondId.Should().NotBe(default);
        firstId.Should().NotBe(secondId);
    }

    [Fact]
    public async Task ObjectIdGenerateNewId_in_filter_builder()
    {
        var collection = Fixture.Collection;

        var filter = Builders<C>.Filter.Where(d => d.Id == ObjectId.GenerateNewId());
        var result = await collection.Find(filter).ToListAsync();

        var renderedFilter = Translate(collection, filter);
        renderedFilter.Should().Be("{ $expr : { $eq : ['$_id', { $createObjectId : {} }] } }");
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ObjectIdGenerateNewId_in_projection_builder()
    {
        var collection = Fixture.Collection;

        var projection = Builders<C>.Projection.Expression(c => new { NewId = ObjectId.GenerateNewId() });
        var result = await collection.Find(Builders<C>.Filter.Empty).Project(projection).ToListAsync();

        var renderedProjection = TranslateFindProjection(collection, projection, null);
        renderedProjection.Should().Be("{ 'NewId' : { $createObjectId : {} }, '_id' : 0 }");

        var firstId = result.First().NewId;
        var secondId = result.Skip(1).First().NewId;

        firstId.Should().NotBe(default);
        secondId.Should().NotBe(default);
        firstId.Should().NotBe(secondId);
    }

    [Fact]
    public async Task ObjectIdGenerateNewId_in_aggregate()
    {
        var collection = Fixture.Collection;

        var pipeline = new EmptyPipelineDefinition<C>()
            .Match(d => d.Id != ObjectId.GenerateNewId())
            .Project(d => new { NewId = ObjectId.GenerateNewId() });

        var result = await collection.Aggregate(pipeline).ToListAsync();

        var renderedPipeline = Translate(collection, pipeline, null);
        AssertStages(
            renderedPipeline,
            "{ '$match' : { '$expr' : { '$ne' : [ '$_id', { $createObjectId : {} }] } } }",
            "{ '$project' : { 'NewId' : { $createObjectId : {} }, '_id' : 0 } }");

        var firstId = result.First().NewId;
        var secondId = result.Skip(1).First().NewId;

        firstId.Should().NotBe(default);
        secondId.Should().NotBe(default);
        firstId.Should().NotBe(secondId);
    }

    public class C
    {
        public ObjectId Id { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new(),
            new(),
        ];
    }
}
