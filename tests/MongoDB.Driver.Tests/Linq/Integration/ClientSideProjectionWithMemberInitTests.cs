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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Integration;

// Regression tests for CSHARP-6223.
public class ClientSideProjectionWithMemberInitTests : LinqIntegrationTest<ClientSideProjectionWithMemberInitTests.ClassFixture>
{
    private static readonly DateTime __createdOn = new(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);
    private static readonly string __createdOnFormatted = __createdOn.ToLocalTime().ToString("yyyy-MM-dd");

    public ClientSideProjectionWithMemberInitTests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Find_with_member_init_projection_when_client_side_projections_are_disabled_should_throw()
    {
        var collection = Fixture.Collection;
        var findOptions = new FindOptions { TranslationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = false } };

        var find = collection
            .Find(Builders<UserDbo>.Filter.Empty, findOptions)
            .Project(MemberInitProjectionExpression());

        var exception = Record.Exception(() => TranslateFindProjection(collection, find));

        exception.Should().BeOfType<ExpressionNotSupportedException>();
        exception.Message.Should().Contain("not supported");
    }

    [Fact]
    public void Find_with_member_init_projection_when_client_side_projections_are_enabled_should_work()
    {
        var collection = Fixture.Collection;
        var findOptions = new FindOptions { TranslationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = true } };

        var find = collection
            .Find(Builders<UserDbo>.Filter.Empty, findOptions)
            .Project(MemberInitProjectionExpression());

        var projection = TranslateFindProjection(collection, find);
        projection.Should().Be("{ _snippets : ['$name', '$createdOn'], _id : 0 }");

        var result = find.Single();
        result.Name.Should().Be("Jane");
        result.CreatedOn.Should().Be(__createdOnFormatted);
    }

    [Fact]
    public void Find_with_nested_member_init_projection_when_client_side_projections_are_enabled_should_work()
    {
        var collection = Fixture.Collection;
        var findOptions = new FindOptions { TranslationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = true } };

        var find = collection
            .Find(Builders<UserDbo>.Filter.Empty, findOptions)
            .Project(x => new OuterDto
            {
                Name = x.Name,
                Inner = new InnerDto { Label = x.Name, CreatedOn = FormatCreatedOn(x.CreatedOn) }
            });

        var projection = TranslateFindProjection(collection, find);
        projection.Should().Be("{ _snippets : ['$name', '$name', '$createdOn'], _id : 0 }");

        var result = find.Single();
        result.Name.Should().Be("Jane");
        result.Inner.Label.Should().Be("Jane");
        result.Inner.CreatedOn.Should().Be(__createdOnFormatted);
    }

    [Fact]
    public void Find_with_collection_initializer_inside_member_init_when_client_side_projections_are_enabled_should_work()
    {
        var collection = Fixture.Collection;
        var findOptions = new FindOptions { TranslationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = true } };

        var find = collection
            .Find(Builders<UserDbo>.Filter.Empty, findOptions)
            .Project(x => new DtoWithList
            {
                Name = x.Name,
                Tags = new List<string> { x.Name, FormatCreatedOn(x.CreatedOn) }
            });

        var projection = TranslateFindProjection(collection, find);
        projection.Should().Be("{ _snippets : ['$name', '$name', '$createdOn'], _id : 0 }");

        var result = find.Single();
        result.Name.Should().Be("Jane");
        result.Tags.Should().Equal("Jane", __createdOnFormatted);
    }

    [Fact]
    public void Queryable_Select_with_member_init_projection_when_client_side_projections_are_enabled_should_work()
    {
        var collection = Fixture.Collection;
        var aggregateOptions = new AggregateOptions { TranslationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = true } };

        var queryable = collection.AsQueryable(aggregateOptions)
            .Select(x => new UserDto
            {
                Name = x.Name,
                CreatedOn = FormatCreatedOn(x.CreatedOn)
            });

        var stages = Translate(collection, queryable, out var outputSerializer);
        AssertStages(stages, "{ $project : { _snippets : ['$name', '$createdOn'], _id : 0 } }");
        outputSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

        var result = queryable.Single();
        result.Name.Should().Be("Jane");
        result.CreatedOn.Should().Be(__createdOnFormatted);
    }

    private static System.Linq.Expressions.Expression<Func<UserDbo, UserDto>> MemberInitProjectionExpression() =>
        x => new UserDto
        {
            Name = x.Name,
            CreatedOn = FormatCreatedOn(x.CreatedOn)
        };

    private static string FormatCreatedOn(BsonDateTime createdOn) =>
        createdOn != null && createdOn.ToNullableLocalTime() != null
            ? createdOn.ToNullableLocalTime().GetValueOrDefault().ToString("yyyy-MM-dd")
            : string.Empty;

    public class UserDbo
    {
        public ObjectId Id { get; set; }
        [BsonElement("name")]
        public string Name { get; set; }
        [BsonElement("createdOn")]
        public BsonDateTime CreatedOn { get; set; }
    }

    public class UserDto
    {
        public string Name { get; set; }
        public string CreatedOn { get; set; }
    }

    public class OuterDto
    {
        public string Name { get; set; }
        public InnerDto Inner { get; set; }
    }

    public class InnerDto
    {
        public string Label { get; set; }
        public string CreatedOn { get; set; }
    }

    public class DtoWithList
    {
        public string Name { get; set; }
        public List<string> Tags { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<UserDbo>
    {
        protected override IEnumerable<UserDbo> InitialData =>
        [
            new UserDbo
            {
                Id = ObjectId.Parse("0102030405060708090a0b0c"),
                Name = "Jane",
                CreatedOn = new BsonDateTime(__createdOn)
            }
        ];
    }
}
