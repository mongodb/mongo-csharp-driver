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
using MongoDB.Driver.TestHelpers;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp4819Tests : LinqIntegrationTest<CSharp4819Tests.ClassFixture>
{
    public CSharp4819Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void ReplaceWith_should_use_configured_element_name()
    {
        var collection = Fixture.Collection;
        var stage = PipelineStageDefinitionBuilder
            .ReplaceWith((User u) => new User { UserId = u.UserId });

        var aggregate = collection.Aggregate()
            .AppendStage(stage);

        var stages = Translate(collection, aggregate);
        AssertStages(
            stages,
            "{ $replaceWith : { uuid : '$uuid' } }");

        var result = aggregate.Single();
        result.Id.Should().Be(0);
        result.UserId.Should().Be(Guid.Parse("00112233-4455-6677-8899-aabbccddeeff"));
    }

    public class User
    {
        public int Id { get; set; }
        [BsonElement("uuid")]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid UserId { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<User>
    {
        protected override IEnumerable<User> InitialData =>
        [
            new User { Id = 1, UserId = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff") }
        ];
    }
}
