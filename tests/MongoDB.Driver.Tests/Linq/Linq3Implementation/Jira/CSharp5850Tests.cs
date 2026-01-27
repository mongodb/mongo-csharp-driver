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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp5850Tests : LinqIntegrationTest<CSharp5850Tests.ClassFixture>
{
    static CSharp5850Tests()
    {
        var csharpLegacyGuidSerializer = new GuidSerializer(GuidRepresentation.CSharpLegacy);

        BsonClassMap.RegisterClassMap<UserDbo>(map =>
        {
            map.AutoMap();
            map.MapMember(x => x.Id).SetSerializer(csharpLegacyGuidSerializer);
        });
    }

    public CSharp5850Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void UserDbo_should_serialize_as_expected()
    {
        var collection = Fixture.Collection;

        var serializedDocument = collection.AsQueryable().As<UserDbo, BsonDocument>(BsonDocumentSerializer.Instance).Single();
        serializedDocument.Should().Be(
            """
            {
                _id : CSUUID("01020304-0506-0708-090a-0b0c0d0e0f10"),
                emails : null
            }
            """);
    }

    [Fact]
    public void Find_using_implicit_client_side_projection_should_work()
    {
        var collection = Fixture.Collection;
        var findOptions = new FindOptions { TranslationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = true } };

        var find = collection
            .Find(Builders<UserDbo>.Filter.Empty, findOptions)
            .Project(x => new UserDto(x.Id, x.MainEmail));

        var projection = TranslateFindProjection(collection, find);
        projection.Should().BeNull(); // because the whole document is needed

        var result = find.Single();
        result.Id.Should().Be(Guid.Parse("0102030405060708090a0b0c0d0e0f10"));
        result.MainEmail.Should().BeNull();
    }

    [Fact]
    public void Find_using_explicit_client_side_projection_should_work()
    {
        var collection = Fixture.Collection;

        var result = collection
            .Find(Builders<UserDbo>.Filter.Empty)
            .ToEnumerable() // execute the rest of the query client side
            .Select(x => new UserDto(x.Id, x.MainEmail))
            .Single();

        result.Id.Should().Be(Guid.Parse("0102030405060708090a0b0c0d0e0f10"));
        result.MainEmail.Should().BeNull();
    }

    [Fact]
    public void Find_using_supported_server_side_projection_should_work()
    {
        RequireServer.Check().Supports(Feature.FindProjectionExpressions);
        var collection = Fixture.Collection;
        var findOptions = new FindOptions { TranslationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = true } };

        var find = collection
            .Find(Builders<UserDbo>.Filter.Empty, findOptions)
            .Project(x => new UserDto(x.Id, x.Emails == null ? null : x.Emails.FirstOrDefault()));

        var projection = TranslateFindProjection(collection, find);
        projection.Should().Be(
            """
            {
                _id : 1,
                MainEmail :
                {
                    $cond :
                    {
                        if : { $eq : ["$emails", null] },
                        then : null,
                        else : { $cond : { if : { $eq : [{ $size : "$emails" }, 0] }, then : null, else : { $arrayElemAt : ["$emails", 0] } } }
                    }
                }
            }
            """);

        var result = find.Single();
        result.Id.Should().Be(Guid.Parse("0102030405060708090a0b0c0d0e0f10"));
        result.MainEmail.Should().BeNull();
    }

    public class UserDbo
    {
        [BsonId]
        public Guid Id { get; set; }
        [BsonElement("emails"), BsonRequired]
        public IEnumerable<string> Emails { get; set; }

        public string MainEmail => Emails?.FirstOrDefault(); // not serialized
    }

    public class UserDto
    {
        public UserDto(Guid id, string mainEmail)
        {
            Id = id;
            MainEmail = mainEmail;
        }
        public Guid Id { get; }
        public string MainEmail { get; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<UserDbo>
    {
        protected override IEnumerable<UserDbo> InitialData =>
        [
            new UserDbo
            {
                Id = Guid.Parse("0102030405060708090a0b0c0d0e0f10"),
                Emails = null
            }
        ];
    }
}
