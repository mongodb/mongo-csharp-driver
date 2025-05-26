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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp5286Tests : LinqIntegrationTest<CSharp5286Tests.ClassFixture>
    {
        static CSharp5286Tests()
        {
            var animalDiscriminatorConvention = new AnimalDiscriminatorConvention();
            RegisterClassMap<Animal>(animalDiscriminatorConvention);
            RegisterClassMap<Mammal>(animalDiscriminatorConvention);
            RegisterClassMap<Cat>(animalDiscriminatorConvention);
            RegisterClassMap<Dog>(animalDiscriminatorConvention);
            RegisterClassMap<Reptile>(animalDiscriminatorConvention);
            RegisterClassMap<Snake>(animalDiscriminatorConvention);

            static void RegisterClassMap<TClass>(IDiscriminatorConvention discriminatorConvention)
            {
                BsonClassMap.RegisterClassMap<TClass>(cm =>
                {
                    cm.AutoMap();
                    cm.SetDiscriminatorConvention(discriminatorConvention);
                });
            }
        }

        public CSharp5286Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void OfType_Mammal_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .OfType<Mammal>();

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { _t : { $in : ['Cat', 'Dog'] } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2);
            results.Select(x => x.GetType().Name).Should().Equal("Cat", "Dog");
        }

        [Fact]
        public void OfType_Dog_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .OfType<Dog>();

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { _t : 'Dog' } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2);
            results.Select(x => x.GetType().Name).Should().Equal("Dog");
        }

        [Fact]
        public void OfType_Reptile_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .OfType<Snake>();

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { _t : 'Snake' } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(3);
            results.Select(x => x.GetType().Name).Should().Equal("Snake");
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

        public class Reptile : Animal
        {
        }

        public class Snake : Reptile
        {
        }

        public sealed class ClassFixture : MongoCollectionFixture<Animal>
        {
            protected override IEnumerable<Animal> InitialData =>
            [
                new Cat { Id = 1 },
                new Dog { Id = 2 },
                new Snake { Id = 3 }
            ];
        }

        private class AnimalDiscriminatorConvention : IScalarDiscriminatorConvention
        {
            public string ElementName => "_t";

            public Type GetActualType(IBsonReader bsonReader, Type nominalType)
            {
                var discriminator = ReadDiscriminator(bsonReader);
                return discriminator.AsString switch
                {
                    // abstract types can be omitted because the can't be any documents of that type
                    "Cat" => typeof(Cat),
                    "Dog" => typeof(Dog),
                    "Snake" => typeof(Snake),
                    _ => throw new InvalidOperationException($"Unknown discriminator: {discriminator}")
                };
            }

            public Type GetActualType(IBsonReader bsonReader, Type nominalType, IBsonSerializationDomain domain)
            {
                throw new NotImplementedException();
            }

            public BsonValue GetDiscriminator(Type nominalType, Type actualType) => actualType.Name;
            public BsonValue GetDiscriminator(Type nominalType, Type actualType, IBsonSerializationDomain domain)
            {
                throw new NotImplementedException();
            }

            public BsonValue[] GetDiscriminatorsForTypeAndSubTypes(Type type)
            {
                // note that we are omitting abstract classes from the results because they can't exist
                return type.Name switch
                {
                    "Animal" => new BsonValue[] { "Cat", "Dog", "Snake" },
                    "Mammal" => new BsonValue[] { "Cat", "Dog" },
                    "Cat" => new BsonValue[] { "Cat" },
                    "Dog" => new BsonValue[] { "Dog", },
                    "Reptile" => new BsonValue[] { "Snake" },
                    "Snake" => new BsonValue[] { "Snake" },
                    _ => throw new InvalidOperationException($"Unknown type: {type}")
                };
            }

            private BsonValue ReadDiscriminator(IBsonReader bsonReader)
            {
                // this code peeks ahead to read the _t value
                // the actual serializer needs to know to skip the _t value
                var bookmark = bsonReader.GetBookmark();
                bsonReader.ReadStartDocument();
                BsonValue discriminator = null;
                if (bsonReader.FindElement("_t"))
                {
                    var context = BsonDeserializationContext.CreateRoot(bsonReader);
                    discriminator = BsonValueSerializer.Instance.Deserialize(context);
                }
                bsonReader.ReturnToBookmark(bookmark);
                return discriminator;
            }
        }
    }
}
