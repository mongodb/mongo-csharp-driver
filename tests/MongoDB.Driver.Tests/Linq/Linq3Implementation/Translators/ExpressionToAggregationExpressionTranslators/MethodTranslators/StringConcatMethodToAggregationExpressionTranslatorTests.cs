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

using FluentAssertions;
using System.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public class StringConcatMethodToAggregationExpressionTranslatorTests : Linq3IntegrationTest
    {
        [Fact]
        public void Filter_using_string_concat_with_two_strings_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Where(i => string.Concat(i.A, ";") == "A1;");

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { $expr : { $eq : [{ $concat : ['$A', ';'] }, 'A1;'] } } }");

            var result = queryable.Single();
            result.Id.Should().Be(1);
        }

        [Fact]
        public void Projection_using_string_concat_with_two_strings_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Where(i => i.Id == 1)
                .Select(i => new { T = string.Concat(i.A, ";") });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { _id : 1 } }",
                "{ $project : { T : { $concat : ['$A', ';'] }, _id : 0 } }");

            var result = queryable.Single();
            result.T.Should().Be("A1;");
        }

        [Fact]
        public void Filter_using_string_concat_with_three_strings_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Where(i => string.Concat(i.A, ";", i.B) == "A1;B1");

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { $expr : { $eq : [{ $concat : ['$A', ';', '$B'] }, 'A1;B1'] } } }");

            var result = queryable.Single();
            result.Id.Should().Be(1);
        }

        [Fact]
        public void Projection_using_string_concat_with_three_strings_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Where(i => i.Id == 1)
                .Select(i => new { T = string.Concat(i.A, ";", i.B) });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { _id : 1 } }",
                "{ $project : { T : { $concat : ['$A', ';', '$B'] }, _id : 0 } }");

            var result = queryable.Single();
            result.T.Should().Be("A1;B1");
        }

        [Fact]
        public void Filter_using_string_concat_with_four_strings_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Where(i => string.Concat(i.A, ";", i.B, i.C) == "A1;B1C1");

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { $expr : { $eq : [{ $concat : ['$A', ';', '$B', '$C'] }, 'A1;B1C1'] } } }");

            var result = queryable.Single();
            result.Id.Should().Be(1);
        }

        [Fact]
        public void Projection_using_string_concat_with_four_strings_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Where(i => i.Id == 1)
                .Select(i => new { T = string.Concat(i.A, ";", i.B, i.C) });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { _id : 1 } }",
                "{ $project : { T : { $concat : ['$A', ';', '$B', '$C'] }, _id : 0 } }");

            var result = queryable.Single();
            result.T.Should().Be("A1;B1C1");
        }

        [Fact]
        public void Filter_using_string_concat_with_params_array_should_work()
        {
            var collection = CreateCollection();
            var queryable = collection.AsQueryable()
                .Where(i => string.Concat(i.A, ";", i.B, ";", i.C) == "A1;B1;C1");

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { $expr : { $eq : [{ $concat : ['$A', ';', '$B', ';', '$C'] }, 'A1;B1;C1'] } } }");

            var result = queryable.Single();
            result.Id.Should().Be(1);
        }

        [Fact]
        public void Projection_using_string_concat_with_params_array_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Where(i => i.Id == 1)
                .Select(i => new { T = string.Concat(i.A, ";", i.B, ";", i.C) });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { '_id' : 1 } }",
                "{ $project : { T : { $concat : ['$A', ';', '$B', ';', '$C'] }, _id : 0 } }");

            var result = queryable.Single();
            result.T.Should().Be("A1;B1;C1");
        }

        private IMongoCollection<Data> CreateCollection()
        {
            var collection = GetCollection<Data>("test");
            CreateCollection(
                collection,
                new Data { Id = 1, A = "A1", B = "B1", C = "C1", D="D1" },
                new Data { Id = 2, A = "A2", B = "B2", C = "C2", D="D2" });
            return collection;
        }

        private class Data
        {
            public int Id { get; set; }
            public string A { get; set; }
            public string B { get; set; }
            public string C { get; set; }
            public string D { get; set; }
        }
    }
}
