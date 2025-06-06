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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp5587Tests : LinqIntegrationTest<CSharp5587Tests.ClassFixture>
{
    public CSharp5587Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void FindOneAndUpdate_should_use_correct_discriminator()
    {
        var collection = Fixture.Collection;

        var lion1 = new Lion { Id = 1, Name = "Lion1" };
        var updateDefinition1 = Builders<Lion>.Update
            .SetOnInsert(l => l.Id, 1)
            .Set(l => l.Name, lion1.Name);
        collection.OfType<Lion>().FindOneAndUpdate(
            f => f.Name == lion1.Name,
            updateDefinition1,
            new FindOneAndUpdateOptions<Lion> { IsUpsert = true });

        var result = collection.AsQueryable().As(BsonDocumentSerializer.Instance).Single();
        result.Should().BeEquivalentTo(
            """
            {
                _id : 1,
                _t : ["Animal", "Cat", "Lion"],
                Name : "Lion1"
            }
            """);
    }

    [Fact]
    public void UpdateOne_should_use_correct_discriminator()
    {
        var collection = Fixture.Collection;

        var lion2 = new Lion { Id = 2, Name = "Lion2" };
        var updateDefinition2 = Builders<Lion>.Update
            .SetOnInsert(l => l.Id, lion2.Id)
            .Set(l => l.Name, lion2.Name);
        collection.OfType<Lion>().UpdateOne(
            f => f.Name == lion2.Name,
            updateDefinition2,
            new UpdateOptions<Lion> { IsUpsert = true });

        var result = collection.AsQueryable().As(BsonDocumentSerializer.Instance).Single();
        result.Should().BeEquivalentTo(
            """
            {
                _id : 2,
                _t : ["Animal", "Cat", "Lion"],
                Name : "Lion2"
            }
            """);
    }

    [BsonDiscriminator(RootClass = true)]
    [BsonKnownTypes(typeof(Cat), typeof(Dog))]
    public class Animal
    {
        public int Id { get; set; }
    }

    [BsonKnownTypes(typeof(Lion), typeof(Tiger))]
    public class Cat : Animal
    {
    }

    public class Dog : Animal
    {
    }

    public class Lion : Cat
    {
        public string Name { get; set; }
    }
    public class Tiger : Cat
    {
    }

    public sealed class ClassFixture : MongoCollectionFixture<Animal>
    {
        public override bool InitializeDataBeforeEachTestCase => true;

        protected override IEnumerable<Animal> InitialData => null;
     }
}
