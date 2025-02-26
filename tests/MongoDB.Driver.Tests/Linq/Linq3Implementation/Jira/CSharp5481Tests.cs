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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp5481Tests : LinqIntegrationTest<CSharp5481Tests.ClassFixture>
    {
        public CSharp5481Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Documents_should_be_serialized_as_expected()
        {
            var collection = Fixture.Collection;

            var documents = collection.AsQueryable().As(BsonDocumentSerializer.Instance).ToList();

            documents.Count.Should().Be(3);
            documents[0].Should().Be("{ _id : 1, _t : 'Cat' }");
            documents[1].Should().Be("{ _id : 2, _t : 'Dog' }");
            documents[2].Should().Be("{ _id : 3, _t : 'Snake' }");
        }

        [Fact]
        public void OfType_Animal_should_work()
        {
            var collection = Fixture.Collection;

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
            var collection = Fixture.Collection;

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
            var collection = Fixture.Collection;

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
            var collection = Fixture.Collection;

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
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .OfType<Snake>();

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { _t : 'Snake' } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(3);
        }

        [Fact]
        public void Where_is_Animal_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Where(x => x is Animal);

            var stages = Translate(collection, queryable);
            AssertStages(stages);

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2, 3);
        }

        [Fact]
        public void Where_is_Mammal_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Where(x => x is Mammal);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { _t : { $in : ['Cat', 'Dog', 'Mammal'] } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2);
        }

        [Fact]
        public void Where_is_Cat_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Where(x => x is Cat);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { _t : 'Cat' } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Where_is_Reptile_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Where(x => x is Reptile);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { _t : { $in : ['Reptile', 'Snake'] } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(3);
        }

        [Fact]
        public void Where_is_Snake_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Where(x => x is Snake);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { _t : 'Snake' } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(3);
        }

        [Fact]
        public void Where_not_is_Animal_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Where(x => !(x is Animal));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { _id : { $type : -1 } } }");

            var results = queryable.ToList();
            results.Count.Should().Be(0);
        }

        [Fact]
        public void Where_not_is_Mammal_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Where(x => !(x is Mammal));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { _t : { $nin : ['Cat', 'Dog', 'Mammal'] } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(3);
        }

        [Fact]
        public void Where_not_is_Cat_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Where(x => !(x is Cat));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { _t : { $ne : 'Cat' } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2, 3);
        }

        [Fact]
        public void Where_not_is_Reptile_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Where(x => !(x is Reptile));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { _t : { $nin : ['Reptile', 'Snake'] } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2);
        }

        [Fact]
        public void Where_not_is_Snake_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Where(x => !(x is Snake));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { _t : { $ne : 'Snake' } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2);
        }

        [Fact]
        public void Array_OfType_Animal_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .GroupBy(x => 1, (key, grouping) => grouping.ToArray())
                .Select(x => x.OfType<Animal>().ToArray());

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : 1, _elements : { $push : '$$ROOT' } } }",
                "{ $project : { _v : '$_elements', _id : 0 } }",
                "{ $project : { _v : '$_v', _id : 0 } }");

            var result = queryable.Single();
            result.Select(x => x.Id).Should().Equal(1, 2, 3);
        }

        [Fact]
        public void Array_OfType_Mammal_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .GroupBy(x => 1, (key, grouping) => grouping.ToArray())
                .Select(x => x.OfType<Mammal>().ToArray());

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : 1, _elements : { $push : '$$ROOT' } } }",
                "{ $project : { _v : '$_elements', _id : 0 } }",
                "{ $project : { _v : { $filter : { input : '$_v', as : 'item', cond : { $in : ['$$item._t', ['Cat', 'Dog', 'Mammal']] } } }, _id : 0 } }");

            var result = queryable.Single();
            result.Select(x => x.Id).Should().Equal(1, 2);
        }

        [Fact]
        public void Array_OfType_Cat_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .GroupBy(x => 1, (key, grouping) => grouping.ToArray())
                .Select(x => x.OfType<Cat>().ToArray());

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : 1, _elements : { $push : '$$ROOT' } } }",
                "{ $project : { _v : '$_elements', _id : 0 } }",
                "{ $project : { _v : { $filter : { input : '$_v', as : 'item', cond : { $eq : ['$$item._t', 'Cat'] } } }, _id : 0 } }");

            var result = queryable.Single();
            result.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Array_OfType_Reptile_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .GroupBy(x => 1, (key, grouping) => grouping.ToArray())
                .Select(x => x.OfType<Reptile>().ToArray());

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : 1, _elements : { $push : '$$ROOT' } } }",
                "{ $project : { _v : '$_elements', _id : 0 } }",
                "{ $project : { _v : { $filter : { input : '$_v', as : 'item', cond : { $in : ['$$item._t', ['Reptile', 'Snake']] } } }, _id : 0 } }");

            var result = queryable.Single();
            result.Select(x => x.Id).Should().Equal(3);
        }

        [Fact]
        public void Array_OfType_Snake_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .GroupBy(x => 1, (key, grouping) => grouping.ToArray())
                .Select(x => x.OfType<Snake>().ToArray());

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : 1, _elements : { $push : '$$ROOT' } } }",
                "{ $project : { _v : '$_elements', _id : 0 } }",
                "{ $project : { _v : { $filter : { input : '$_v', as : 'item', cond : { $eq : ['$$item._t', 'Snake'] } } }, _id : 0 } }");

            var result = queryable.Single();
            result.Select(x => x.Id).Should().Equal(3);
        }

        [Fact]
        public void Array_Where_is_Animal_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .GroupBy(x => 1, (key, grouping) => grouping.ToArray())
                .Select(x => x.Where(x => x is Animal).ToArray());

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : 1, _elements : { $push : '$$ROOT' } } }",
                "{ $project : { _v : '$_elements', _id : 0 } }",
                "{ $project : { _v : '$_v', _id : 0 } }");

            var result = queryable.Single();
            result.Select(x => x.Id).Should().Equal(1, 2, 3);
        }

        [Fact]
        public void Array_Where_is_Mammal_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .GroupBy(x => 1, (key, grouping) => grouping.ToArray())
                .Select(x => x.Where(x => x is Mammal).ToArray());

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : 1, _elements : { $push : '$$ROOT' } } }",
                "{ $project : { _v : '$_elements', _id : 0 } }",
                "{ $project : { _v: { $filter : { input : '$_v', as : 'x', cond : { $in : ['$$x._t', ['Cat', 'Dog', 'Mammal']] } } }, _id : 0 } }");

            var result = queryable.Single();
            result.Select(x => x.Id).Should().Equal(1, 2);
        }

        [Fact]
        public void Array_Where_is_Cat_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .GroupBy(x => 1, (key, grouping) => grouping.ToArray())
                .Select(x => x.Where(x => x is Cat).ToArray());

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : 1, _elements : { $push : '$$ROOT' } } }",
                "{ $project : { _v : '$_elements', _id : 0 } }",
                "{ $project : { _v : { $filter : { input : '$_v', as : 'x', cond : { $eq : ['$$x._t', 'Cat'] } } }, _id : 0 } }");

            var result = queryable.Single();
            result.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Array_Where_is_Reptile_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .GroupBy(x => 1, (key, grouping) => grouping.ToArray())
                .Select(x => x.Where(x => x is Reptile).ToArray());

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : 1, _elements : { $push : '$$ROOT' } } }",
                "{ $project : { _v : '$_elements', _id : 0 } }",
                "{ $project : { _v : { $filter : { input : '$_v', as : 'x', cond : { $in : ['$$x._t', ['Reptile', 'Snake']] } } }, _id : 0 } }");

            var result = queryable.Single();
            result.Select(x => x.Id).Should().Equal(3);
        }

        [Fact]
        public void Array_Where_is_Snake_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .GroupBy(x => 1, (key, grouping) => grouping.ToArray())
                .Select(x => x.Where(x => x is Snake).ToArray());

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : 1, _elements : { $push : '$$ROOT' } } }",
                "{ $project : { _v : '$_elements', _id : 0 } }",
                "{ $project : { _v : { $filter : { input : '$_v', as : 'x', cond : { $eq : ['$$x._t', 'Snake'] } } }, _id : 0 } }");

            var result = queryable.Single();
            result.Select(x => x.Id).Should().Equal(3);
        }

        [Fact]
        public void Array_Where_not_is_Animal_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .GroupBy(x => 1, (key, grouping) => grouping.ToArray())
                .Select(x => x.Where(x => !(x is Animal)).ToArray());

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : 1, _elements : { $push : '$$ROOT' } } }",
                "{ $project : { _v : '$_elements', _id : 0 } }",
                "{ $project : { _v : [], _id : 0 } }");

            var result = queryable.Single();
            result.Select(x => x.Id).Should().Equal();
        }

        [Fact]
        public void Array_Where_not_is_Mammal_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .GroupBy(x => 1, (key, grouping) => grouping.ToArray())
                .Select(x => x.Where(x => !(x is Mammal)).ToArray());

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : 1, _elements : { $push : '$$ROOT' } } }",
                "{ $project : { _v : '$_elements', _id : 0 } }",
                "{ $project : { _v : { $filter : { input : '$_v', as : 'x', cond : { $not : { $in : ['$$x._t', ['Cat', 'Dog', 'Mammal']] } } } }, _id : 0 } }");

            var result = queryable.Single();
            result.Select(x => x.Id).Should().Equal(3);
        }

        [Fact]
        public void Array_Where_not_is_Cat_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .GroupBy(x => 1, (key, grouping) => grouping.ToArray())
                .Select(x => x.Where(x => !(x is Cat)).ToArray());

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : 1, _elements : { $push : '$$ROOT' } } }",
                "{ $project : { _v : '$_elements', _id : 0 } }",
                "{ $project : { _v : { $filter : { input : '$_v', as : 'x', cond : { $ne : ['$$x._t', 'Cat'] } } } , _id : 0 } }");

            var result = queryable.Single();
            result.Select(x => x.Id).Should().Equal(2, 3);
        }

        [Fact]
        public void Array_Where_not_is_Reptile_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .GroupBy(x => 1, (key, grouping) => grouping.ToArray())
                .Select(x => x.Where(x => !(x is Reptile)).ToArray());

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : 1, _elements : { $push : '$$ROOT' } } }",
                "{ $project : { _v : '$_elements', _id : 0 } }",
                "{ $project : { _v : { $filter : { input : '$_v', as : 'x', cond : { $not : { $in : ['$$x._t', ['Reptile', 'Snake']] } } } }, _id : 0 } }");

            var result = queryable.Single();
            result.Select(x => x.Id).Should().Equal(1, 2);
        }

        [Fact]
        public void Array_Where_not_is_Snake_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .GroupBy(x => 1, (key, grouping) => grouping.ToArray())
                .Select(x => x.Where(x => !(x is Snake)).ToArray());

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : 1, _elements : { $push : '$$ROOT' } } }",
                "{ $project : { _v : '$_elements', _id : 0 } }",
                "{ $project : { _v : { $filter : { input : '$_v', as : 'x', cond : { $ne : ['$$x._t', 'Snake'] } } } , _id : 0 } }");

            var result = queryable.Single();
            result.Select(x => x.Id).Should().Equal(1, 2);
        }

        [BsonKnownTypes(typeof(Mammal))]
        [BsonKnownTypes(typeof(Cat))]
        [BsonKnownTypes(typeof(Dog))]
        [BsonKnownTypes(typeof(Reptile))]
        [BsonKnownTypes(typeof(Snake))]
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
            protected override IEnumerable<Animal> InitialData
                =>
                [
                    new Cat { Id = 1 },
                    new Dog { Id = 2 },
                    new Snake { Id = 3 }
                ];
        }    }
}
