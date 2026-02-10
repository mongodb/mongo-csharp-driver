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
using FluentAssertions;
using MongoDB.Driver.TestHelpers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp2787Tests : LinqIntegrationTest<CSharp2787Tests.ClassFixture>
{
    public CSharp2787Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Project_should_use_the_correct_serializer()
    {
        var collection = Fixture.Collection;

        var aggregate = collection.Aggregate()
            .Project(ewt => new EntityInfo
            {
                Id = ewt.Id,
                Name = ewt.Name,
                Towns = ewt.Towns.Select(t => t.Id)
            })
            .Match(ei => ei.Id != "0102030405060708090a0b0c")
            .Match(ei => ei.Towns.Contains("1102030405060708090a0b0c"));

        var stages = Translate(collection, aggregate);
        AssertStages(
            stages,
            """{ $project : { _id : "$_id", Name : "$Name", Towns : "$Towns._id" } }""",
            """{ $match : { _id : { $ne : { $oid : "0102030405060708090a0b0c" } } } }""",
            """{ $match : { Towns : "1102030405060708090a0b0c" } }""");

        var result = aggregate.Single();
        result.Name.Should().Be("2");
    }

    public class EntityWithTowns
    {
        [BsonId, BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Name { get; set; }

        public IEnumerable<Towns> Towns { get; set; }
    }

    public class EntityInfo
    {
        [BsonId, BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Name { get; set; }

        public IEnumerable<string> Towns { get; set; }
    }

    public class Towns
    {
        public string Id  { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<EntityWithTowns>
    {
        protected override IEnumerable<EntityWithTowns> InitialData =>
        [
            new EntityWithTowns { Id = "0102030405060708090a0b0c", Name = "1", Towns = new[] { new Towns { Id = "0102030405060708090a0b0c" } } },
            new EntityWithTowns { Id = "1102030405060708090a0b0c", Name = "2", Towns = new[] { new Towns { Id = "1102030405060708090a0b0c" } } },
        ];
    }
}
