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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp5190Tests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void Select_new_BsonDocument_with_computed_value_and_empty_initializers_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument("a", x.A) { });

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<NotSupportedException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $project : { a : '$A', _id : 0 } }");

                var result = queryable.First();
                result.Should().Be("{ a : 2 }");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_new_BsonDocument_with_computed_value_and_no_initializers_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument("a", x.A));

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<NotSupportedException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $project : { a : '$A', _id : 0 } }");

                var result = queryable.First();
                result.Should().Be("{ a : 2 }");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_new_BsonDocument_with_computed_value_and_one_initializer_with_computed_value_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument("a", x.A) { { "b", x.B } });

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<NotSupportedException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $project : { a : '$A', b : '$B', _id : 0 } }");

                var result = queryable.First();
                result.Should().Be("{ a : 2, b : 3 }");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_new_BsonDocument_with_computed_value_and_one_initializer_with_constant_value_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument("a", x.A) { { "b", 5 } });

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<NotSupportedException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $project : { a : '$A', b : { $literal : 5 }, _id : 0 } }");

                var result = queryable.First();
                result.Should().Be("{ a : 2, b : 5 }");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_new_BsonDocument_with_constant_value_and_empty_initializers_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument("a", 4) { }); // new BsonDocument is evaluated by the PartialEvaluator

            var stages = Translate(collection, queryable);
            var result = queryable.First();
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { a : 4 }, _id : 0 } }"); // LINQ2 translation is missing $literal
                result.Should().BeNull(); // LINQ2 results is wrong
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $literal : { a : 4 } }, _id : 0 } }");
                result.Should().Be("{ a : 4 }");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_new_BsonDocument_with_constant_value_and_no_initializers_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument("a", 4)); // new BsonDocument is evaluated by the PartialEvaluator

            var stages = Translate(collection, queryable);
            var result = queryable.First();
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { a : 4 }, _id : 0 } }"); // LINQ2 translation is missing $literal
                result.Should().BeNull(); // LINQ2 results is wrong
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $literal : { a : 4 } }, _id : 0 } }");
                result.Should().Be("{ a : 4 }");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_new_BsonDocument_with_constant_value_and_one_initializer_with_computed_value_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument("a", 4) { { "b", x.B } });

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<NotSupportedException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $project : { a : { $literal : 4 }, b : '$B', _id : 0 } }");

                var result = queryable.First();
                result.Should().Be("{ a : 4, b : 3 }");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_new_BsonDocument_with_constant_value_and_one_initializer_with_constant_value_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument("a", 4) { { "b", 5 } }); // new BsonDocument is evaluated by the PartialEvaluator

            var stages = Translate(collection, queryable);
            var result = queryable.First();
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { a : 4, b : 5 }, _id : 0 } }"); // LINQ2 translation is missing $literal
                result.Should().BeNull(); // LINQ2 results is wrong
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $literal : { a : 4, b : 5 } }, _id : 0 } }");
                result.Should().Be("{ a : 4, b : 5 }");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_new_BsonDocument_with_no_arguments_and_empty_initializers_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument() { }); // new BsonDocument is evaluated by the PartialEvaluator

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { }, _id : 0 } }"); // LINQ2 translation is missing $literal

                var exception = Record.Exception(() => queryable.First());
                exception.Should().BeOfType<MongoCommandException>();
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $literal : { } }, _id : 0 } }");

                var result = queryable.First();
                result.Should().Be("{ }");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_new_BsonDocument_with_no_arguments_and_no_initializers_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument()); // new BsonDocument is evaluated by the PartialEvaluator

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { }, _id : 0 } }"); // LINQ2 translation is missing $literal

                var exception = Record.Exception(() => queryable.First());
                exception.Should().BeOfType<MongoCommandException>();
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $literal : { } }, _id : 0 } }");

                var result = queryable.First();
                result.Should().Be("{ }");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_new_BsonDocument_with_no_arguments_and_one_initializer_with_computed_value_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument { { "a", x.A } });

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<NotSupportedException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $project : { a : '$A', _id : 0 } }");

                var result = queryable.First();
                result.Should().Be("{ a : 2 }");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_new_BsonDocument_with_no_arguments_and_one_initializer_with_constant_value_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument { { "a", 4 } }); // new BsonDocument is evaluated by the PartialEvaluator

            var stages = Translate(collection, queryable);
            var result = queryable.First();
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { a : 4 }, _id : 0 } }"); // LINQ2 translation is wrong
                result.Should().BeNull(); // LINQ2 result is wrong
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $literal : { a : 4 } }, _id : 0 } }");
                result.Should().Be("{ a : 4 }");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_new_BsonDocument_with_no_arguments_and_two_initializers_with_computed_and_computed_values_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument { { "a", x.A }, { "b", x.B } });

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<NotSupportedException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $project : { a : '$A', b : '$B', _id : 0 } }");

                var result = queryable.First();
                result.Should().Be("{ a : 2, b : 3 }");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_new_BsonDocument_with_no_arguments_and_two_initializers_with_conmputed_and_constant_values_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument { { "a", x.A }, { "b", 5 } });

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<NotSupportedException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $project : { a : '$A', b : { $literal : 5 }, _id : 0 } }");

                var result = queryable.First();
                result.Should().Be("{ a : 2, b : 5 }");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_new_BsonDocument_with_no_arguments_and_two_initializers_with_constant_and_computed_values_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument { { "a", 4 }, { "b", x.B } });

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<NotSupportedException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $project : { a : { $literal : 4 }, b : '$B', _id : 0 } }");

                var result = queryable.First();
                result.Should().Be("{ a : 4, b : 3 }");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_new_BsonDocument_with_no_arguments_and_two_initializers_with_constant_and_constant_values_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => new BsonDocument { { "a", 4 }, { "b", 5 } });

            var stages = Translate(collection, queryable);
            var result = queryable.First();

            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { a : 4, b : 5 }, _id : 0 } }"); // LINQ2 translation is wrong
                result.Should().BeNull(); // LINQ2 result is wrong
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $literal : { a : 4, b : 5 } }, _id : 0 } }");
                result.Should().Be("{ a : 4, b : 5 }");
            }
        }

        private IMongoCollection<C> GetCollection(LinqProvider linqProvider)
        {
            var collection = GetCollection<C>("test", linqProvider);
            CreateCollection(
                collection,
                new C { Id = 1, A = 2, B = 3 });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public int A { get; set; }
            public int B { get; set; }
        }
    }
}
