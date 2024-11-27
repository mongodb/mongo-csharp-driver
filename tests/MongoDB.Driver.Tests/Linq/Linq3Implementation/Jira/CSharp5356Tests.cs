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

using System.Linq;
using FluentAssertions;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp5356Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Documents_should_be_serialized_as_expected()
        {
            var collection = GetCollection();

            var documents = collection.AsQueryable().As(BsonDocumentSerializer.Instance).ToList();

            documents.Count.Should().Be(3);
            documents[0].Should().Be("{ _id : 1, _t : 'Cat' }");
            documents[1].Should().Be("{ _id : 2, _t : 'Dog' }");
            documents[2].Should().Be("{ _id : 3, _t : 'Snake' }");
        }

        [Fact]
        public void OfType_Animal_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .OfType<Animal>();

            var stages = Translate(collection, queryable);
            stages.Count.Should().Be(0);

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2, 3);
        }

        [Fact]
        public void OfType_Mammal_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .OfType<Mammal>();

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { _t : { $in : ['Cat', 'Dog', 'Mammal'] } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2);
        }

        [Fact]
        public void OfType_Cat_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .OfType<Cat>();

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { _t : 'Cat' } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void OfType_Reptile_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .OfType<Reptile>();

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { _t : { $in : ['Reptile', 'Snake'] } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(3);
        }

        [Fact]
        public void OfType_Snake_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .OfType<Snake>();

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { _t : 'Snake' } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(3);
        }

        private IMongoCollection<Animal> GetCollection()
        {
            var collection = GetCollection<Animal>("test");
            CreateCollection(
                collection,
                new Cat { Id = 1 },
                new Dog { Id = 2 },
                new Snake { Id = 3 });
            return collection;
        }

        [BsonKnownTypes(typeof(Mammal))]
        [BsonKnownTypes(typeof(Cat))]
        [BsonKnownTypes(typeof(Dog))]
        [BsonKnownTypes(typeof(Reptile))]
        [BsonKnownTypes(typeof(Snake))]
        private abstract class Animal
        {
            public int Id { get; set; }
        }

        private abstract class Mammal : Animal
        {
        }

        private class Cat : Mammal
        {
        }

        private class Dog : Mammal
        {
        }

        private class Reptile : Animal
        {
        }

        private class Snake : Reptile
        {
        }
    }
}
