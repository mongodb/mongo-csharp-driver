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
using MongoDB.Bson;
using MongoDB.Driver.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp5190Tests : LinqIntegrationTest<CSharp5190Tests.TestDataFixture>
    {
        public CSharp5190Tests(ITestOutputHelper testOutputHelper, TestDataFixture fixture)
            : base(testOutputHelper, fixture)
        {
        }

        [Fact]
        public void Select_new_BsonDocument_with_computed_value_and_empty_initializers_should_work()
        {
            var collection = Fixture.GetCollection<MyModel>();

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument("a", x.A) { });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { a : '$A', _id : 0 } }");

            var result = queryable.First();
            result.Should().Be("{ a : 2 }");
        }

        [Fact]
        public void Select_new_BsonDocument_with_computed_value_and_no_initializers_should_work()
        {
            var collection = Fixture.GetCollection<MyModel>();

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument("a", x.A));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { a : '$A', _id : 0 } }");

            var result = queryable.First();
            result.Should().Be("{ a : 2 }");
        }

        [Fact]
        public void Select_new_BsonDocument_with_computed_value_and_one_initializer_with_computed_value_should_work()
        {
            var collection = Fixture.GetCollection<MyModel>();

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument("a", x.A) { { "b", x.B } });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { a : '$A', b : '$B', _id : 0 } }");

            var result = queryable.First();
            result.Should().Be("{ a : 2, b : 3 }");
        }

        [Fact]
        public void Select_new_BsonDocument_with_computed_value_and_one_initializer_with_constant_value_should_work()
        {
            var collection = Fixture.GetCollection<MyModel>();

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument("a", x.A) { { "b", 5 } });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { a : '$A', b : { $literal : 5 }, _id : 0 } }");

            var result = queryable.First();
            result.Should().Be("{ a : 2, b : 5 }");
        }

        [Fact]
        public void Select_new_BsonDocument_with_constant_value_and_empty_initializers_should_work()
        {
            var collection = Fixture.GetCollection<MyModel>();

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument("a", 4) { }); // new BsonDocument is evaluated by the PartialEvaluator

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $literal : { a : 4 } }, _id : 0 } }");

            var result = queryable.First();
            result.Should().Be("{ a : 4 }");
        }

        [Fact]
        public void Select_new_BsonDocument_with_constant_value_and_no_initializers_should_work()
        {
            var collection = Fixture.GetCollection<MyModel>();

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument("a", 4)); // new BsonDocument is evaluated by the PartialEvaluator

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $literal : { a : 4 } }, _id : 0 } }");

            var result = queryable.First();
            result.Should().Be("{ a : 4 }");
        }

        [Fact]
        public void Select_new_BsonDocument_with_constant_value_and_one_initializer_with_computed_value_should_work()
        {
            var collection = Fixture.GetCollection<MyModel>();

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument("a", 4) { { "b", x.B } });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { a : { $literal : 4 }, b : '$B', _id : 0 } }");

            var result = queryable.First();
            result.Should().Be("{ a : 4, b : 3 }");
        }

        [Fact]
        public void Select_new_BsonDocument_with_constant_value_and_one_initializer_with_constant_value_should_work()
        {
            var collection = Fixture.GetCollection<MyModel>();

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument("a", 4) { { "b", 5 } }); // new BsonDocument is evaluated by the PartialEvaluator

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $literal : { a : 4, b : 5 } }, _id : 0 } }");

            var result = queryable.First();
            result.Should().Be("{ a : 4, b : 5 }");
        }

        [Fact]
        public void Select_new_BsonDocument_with_no_arguments_and_empty_initializers_should_work()
        {
            var collection = Fixture.GetCollection<MyModel>();

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument() { }); // new BsonDocument is evaluated by the PartialEvaluator

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $literal : { } }, _id : 0 } }");

            var result = queryable.First();
            result.Should().Be("{ }");
        }

        [Fact]
        public void Select_new_BsonDocument_with_no_arguments_and_no_initializers_should_work()
        {
            var collection = Fixture.GetCollection<MyModel>();

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument()); // new BsonDocument is evaluated by the PartialEvaluator

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $literal : { } }, _id : 0 } }");

            var result = queryable.First();
            result.Should().Be("{ }");
        }

        [Fact]
        public void Select_new_BsonDocument_with_no_arguments_and_one_initializer_with_computed_value_should_work()
        {
            var collection = Fixture.GetCollection<MyModel>();

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument { { "a", x.A } });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { a : '$A', _id : 0 } }");

            var result = queryable.First();
            result.Should().Be("{ a : 2 }");
        }

        [Fact]
        public void Select_new_BsonDocument_with_no_arguments_and_one_initializer_with_constant_value_should_work()
        {
            var collection = Fixture.GetCollection<MyModel>();

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument { { "a", 4 } }); // new BsonDocument is evaluated by the PartialEvaluator

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $literal : { a : 4 } }, _id : 0 } }");

            var result = queryable.First();
            result.Should().Be("{ a : 4 }");
        }

        [Fact]
        public void Select_new_BsonDocument_with_no_arguments_and_two_initializers_with_computed_and_computed_values_should_work()
        {
            var collection = Fixture.GetCollection<MyModel>();

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument { { "a", x.A }, { "b", x.B } });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { a : '$A', b : '$B', _id : 0 } }");

            var result = queryable.First();
            result.Should().Be("{ a : 2, b : 3 }");
        }

        [Fact]
        public void Select_new_BsonDocument_with_no_arguments_and_two_initializers_with_conmputed_and_constant_values_should_work()
        {
            var collection = Fixture.GetCollection<MyModel>();

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument { { "a", x.A }, { "b", 5 } });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { a : '$A', b : { $literal : 5 }, _id : 0 } }");

            var result = queryable.First();
            result.Should().Be("{ a : 2, b : 5 }");
        }

        [Fact]
        public void Select_new_BsonDocument_with_no_arguments_and_two_initializers_with_constant_and_computed_values_should_work()
        {
            var collection = Fixture.GetCollection<MyModel>();

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument { { "a", 4 }, { "b", x.B } });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { a : { $literal : 4 }, b : '$B', _id : 0 } }");

            var result = queryable.First();
            result.Should().Be("{ a : 4, b : 3 }");
        }

        [Fact]
        public void Select_new_BsonDocument_with_no_arguments_and_two_initializers_with_constant_and_constant_values_should_work()
        {
            var collection = Fixture.GetCollection<MyModel>();

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument { { "a", 4 }, { "b", 5 } });

            var stages = Translate(collection, queryable);
            var result = queryable.First();

            AssertStages(stages, "{ $project : { _v : { $literal : { a : 4, b : 5 } }, _id : 0 } }");
            result.Should().Be("{ a : 4, b : 5 }");
        }

        public class MyModel
        {
            public int Id { get; set; }
            public int A { get; set; }
            public int B { get; set; }
        }

        public sealed class TestDataFixture : MongoCollectionFixture<MyModel>
        {
            protected override IEnumerable<MyModel> InitialData =>
            [
                new MyModel { Id = 1, A = 2, B = 3 }
            ];
        }
    }
}
