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
using MongoDB.Driver.TestHelpers;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Driver.Tests.Jira;

public class CSharp5757Tests : LinqIntegrationTest<CSharp5757Tests.ClassFixture>
{
    static CSharp5757Tests()
    {
        var scalarDiscriminatorConvention = new AnimalDiscriminatorConvention();
        var hierarchicalDiscriminatorConvention = new PersonDiscriminatorConvention();
        BsonSerializer.RegisterDiscriminatorConvention(typeof(Animal), scalarDiscriminatorConvention);
        BsonSerializer.RegisterDiscriminatorConvention(typeof(Person), hierarchicalDiscriminatorConvention);
    }

    public CSharp5757Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void HierarchicalDiscriminator_with_Filter_OfType_HealthCareWorker_should_throw()
    {
        var filter = Builders<Person>.Filter.OfType<HealthCareWorker>();

        var renderedArgs =
            new RenderArgs<Person>(BsonSerializer.LookupSerializer<Person>(), BsonSerializer.SerializerRegistry);

        var exception = Record.Exception(() => filter.Render(renderedArgs));
        exception.Should().BeOfType<NotSupportedException>();
        exception.Message.Should().Be("Hierarchical discriminator convention requires that documents of type HealthCareWorker have a discriminator value.");
    }

    [Fact]
    public void HierarchicalDiscriminator_with_Queryable_OfType_HealthCareWorker_should_throw()
    {
        var collection = Fixture.Database.GetCollection<Person>("person");
        var queryable = collection.AsQueryable()
            .OfType<HealthCareWorker>();


        var exception = Record.Exception(() => Translate(collection, queryable));
        exception.Should().BeOfType<NotSupportedException>();
        exception.Message.Should().Be("Hierarchical discriminator convention requires that documents of type HealthCareWorker have a discriminator value.");
    }

    [Fact]
    public void ScalarDiscriminator_with_Filter_OfType_Mammal_should_work()
    {
        var collection = Fixture.Collection;
        var filter = Builders<Animal>.Filter.OfType<Mammal>();

        var renderedFilter = filter.Render(new RenderArgs<Animal>(collection.DocumentSerializer, BsonSerializer.SerializerRegistry));
        renderedFilter.Should().Be("{ _t : { $in : ['Cat', 'Dog'] } }");

        var results = collection.FindSync(filter).ToList();
        results.Select(x => x.Id).Should().Equal(1, 2);
    }

    [Fact]
    public void ScalarDiscriminator_with_Queryable_OfType_Mammal_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .OfType<Mammal>();

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { _t : { $in : ['Cat', 'Dog'] } } }");

        var results = queryable.ToList();
        results.Select(x => x.Id).Should().Equal(1, 2);
    }

    public abstract class Person
    {
    }

    public abstract class HealthCareWorker : Person
    {
    }

    public class Doctor : HealthCareWorker
    {
    }

    public class Nurse : HealthCareWorker
    {
    }

    public class PersonDiscriminatorConvention : IHierarchicalDiscriminatorConvention
    {
        public string ElementName => "_t";

        public Type GetActualType(IBsonReader bsonReader, Type nominalType)
        {
            throw new NotImplementedException();
        }

        public BsonValue GetDiscriminator(Type nominalType, Type actualType)
            => actualType.IsAbstract ? null : actualType.Name;
    }

    public abstract class Animal
    {
        public int Id { get; set; }
    }

    public abstract class Mammal : Animal
    {
    }

    public class Cat : Mammal
    {
    }

    public class Dog : Mammal
    {
    }

    public class AnimalDiscriminatorConvention : IScalarDiscriminatorConvention
    {
        public string ElementName => "_t";

        public Type GetActualType(IBsonReader bsonReader, Type nominalType)
        {
            var discriminatorValue = ReadDiscriminatorValue(bsonReader);
            return discriminatorValue switch
            {
                "Cat" => typeof(Cat),
                "Dog" => typeof(Dog),
                _ => throw new Exception($"Invalid discriminator value: {discriminatorValue}.")
            };
        }

        public BsonValue GetDiscriminator(Type nominalType, Type actualType)
            => actualType.IsAbstract ? null : actualType.Name;

        public BsonValue[] GetDiscriminatorsForTypeAndSubTypes(Type type)
            => type.Name switch
            {
                "Animal" => ["Cat", "Dog"],
                "Mammal" => ["Cat", "Dog"],
                "Cat" => ["Cat"],
                "Dog" => ["Dog"],
                _ => throw new ArgumentException($"Invalid type: {type.Name}.")
            };

        private string ReadDiscriminatorValue(IBsonReader bsonReader)
        {
            string discriminatorValue = null;

            var bsonType = bsonReader.GetCurrentBsonType();
            if (bsonType == BsonType.Document)
            {
                var bookmark = bsonReader.GetBookmark();
                bsonReader.ReadStartDocument();
                if (bsonReader.FindElement("_t"))
                {
                    var context = BsonDeserializationContext.CreateRoot(bsonReader);
                    if (BsonValueSerializer.Instance.Deserialize(context) is BsonString bsonString)
                    {
                        discriminatorValue = bsonString.Value;
                    }
                }
                bsonReader.ReturnToBookmark(bookmark);
            }

            return discriminatorValue;
        }
    }

    public sealed class ClassFixture : MongoCollectionFixture<Animal>
    {
        protected override IEnumerable<Animal> InitialData =>
        [
            new Cat { Id = 1 },
            new Dog { Id = 2 }
        ];
    }
}