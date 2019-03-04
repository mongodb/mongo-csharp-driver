/* Copyright 2019-present MongoDB Inc.
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
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Translators
{
    public class PredicateTranslatorValidationTests
    {
        private IMongoCollection<TestObject> _collection;

        public void Setup()
        {
            var connectionString = CoreTestConfiguration.ConnectionString.ToString();
            IMongoClient client = new MongoClient(connectionString);
            var database = client.GetDatabase("test");
            _collection = database.GetCollection<TestObject>("testObject");
            database.DropCollection("testObject");
        }

        private class TestObject
        {
            public int Value1 { get; set; }
            public bool Value2 { get; set; }
            public IEnumerable<TestObject> Collection1 { get; set; }
            public IEnumerable<int> Collection2 { get; set; }
            public IEnumerable<bool> Collection3 { get; set; }
        }

        private const string NotSupportErrorMessageTemplate = "The LINQ expression: {0} has the member \"{1}\" which can not be used to build a correct MongoDB query.";

        [Fact]
        public void Should_not_throw_the_exception_when_a_child_expression_is_called_from_locals()
        {
            Setup();

            var local = new List<int> { 1, 2 };

            Expression<Func<TestObject, bool>> expr = (a) => local.Any(b => a.Collection2.Contains(b));
            Execute(CreateWhereQuery(expr));
        }

        [Fact]
        public void Should_not_throw_the_exception_when_a_predicate_has_only_parameter_expressions()
        {
            Setup();

            Expression<Func<TestObject, bool>> expr = (a) => a.Collection3.Any(b => b);
            Execute(CreateWhereQuery(expr));

            expr = (a) => a.Collection3.Any(b => b && b);
            Execute(CreateWhereQuery(expr));
        }

        [Fact]
        public void Should_not_throw_the_exception_when_there_are_no_parent_parameters_in_child_expressions()
        {
            Setup();

            Expression<Func<TestObject, bool>> expr =
                (a) =>
                    a.Collection1
                        .Where(b => b.Collection1.Any(d => d.Value1 != 2))
                        .Any(c => c.Value1 == 3);

            Execute(CreateWhereQuery(expr));
        }

        [Fact]
        public void Should_not_throw_the_exception_when_there_are_several_conditions_and_there_are_no_parent_parameters_in_children()
        {
            Setup();

            Expression<Func<TestObject, bool>> expr =
                (a) =>
                    a.Collection1.Any(c => c.Value1 == 3) &&
                    a.Collection1.Any(d => d.Value1 != 4);
            Execute(CreateWhereQuery(expr));

            expr =
                (a) => a.Collection1
                    .Any(
                        c => c.Value1 != 3 && c.Collection1.Any(d => d.Value1 != 5));
            Execute(CreateWhereQuery(expr));
        }


        [Fact]
        public void Should_not_throw_the_exception_when_top_method_is_called_from_other_linq_method_and_there_is_no_any_using_of_cross_levels_parameters()
        {
            Setup();

            Execute(CreateQuery().Select(x => x.Collection1).Where(x => x.Any(y => y.Value1 > 1)));
        }

        [Fact]
        public void Should_not_throw_the_exception_when_top_method_is_not_where_predicate()
        {
            Setup();

            Execute(CreateQuery().Select(x => x.Collection1.Where(y => y.Value1 > x.Value1)));

            Execute(CreateQuery().GroupBy(x => x.Collection1.Where(y => y.Value1 > x.Value1)));
        }

        [Fact]
        public void Should_throw_the_exception_when_a_child_expression_uses_parameters_from_grandparents()
        {
            Setup();

            Expression<Func<TestObject, bool>> expr = (a) => a.Collection1.Any(b => b.Collection1.Any(c => a.Value1 == 2));

            var exception = Assert.Throws<NotSupportedException>(() => Execute(CreateWhereQuery(expr)));
            exception.Message.Should().Be(string.Format(NotSupportErrorMessageTemplate, "{document}{Collection1}.Where(Any({document}{Collection1}.Where(({document}{Value1} == 2))))", "a"));
        }

        [Fact]
        public void Should_throw_the_exception_when_a_child_expression_uses_parameters_from_parents()
        {
            Setup();

            Expression<Func<TestObject, bool>> expr = (a) => a.Collection1.Any(b => a.Value1 == 2);
            var exception = Assert.Throws<NotSupportedException>(() => Execute(CreateWhereQuery(expr)));
            exception.Message.Should().Be(string.Format(NotSupportErrorMessageTemplate, "{document}{Collection1}.Where(({document}{Value1} == 2))", "a"));

            expr = (a) => a.Collection1.Any(b => b.Value1 == 2 && a.Value1 == 3);
            exception = Assert.Throws<NotSupportedException>(() => Execute(CreateWhereQuery(expr)));
            exception.Message.Should().Be(string.Format(NotSupportErrorMessageTemplate, "{document}{Collection1}.Where((({document}{Value1} == 2) AndAlso ({document}{Value1} == 3)))", "a"));

            expr = (a) => a.Collection3.Any(b => a.Value2);
            exception = Assert.Throws<NotSupportedException>(() => Execute(CreateWhereQuery(expr)));
            exception.Message.Should().Be(string.Format(NotSupportErrorMessageTemplate, "{document}{Collection3}.Where({document}{Value2})", "a"));

            expr = (a) => a.Collection1.Where(b => a.Value1 == 2).Any();
            exception = Assert.Throws<NotSupportedException>(() => Execute(CreateWhereQuery(expr)));
            exception.Message.Should().Be(string.Format(NotSupportErrorMessageTemplate, "{document}{Collection1}.Where(({document}{Value1} == 2))", "a"));
        }

        [Fact]
        public void Should_throw_the_exception_when_there_is_the_parent_parameter_in_the_child_expression_and_there_are_several_conditions()
        {
            Setup();

            Expression<Func<TestObject, bool>> expr =
                (a) =>
                    a.Collection1.Any(c => c.Value1 == 3) &&
                    a.Collection1.Any(d => a.Value1 != 4);
            var exception = Assert.Throws<NotSupportedException>(() => Execute(CreateWhereQuery(expr)));
            exception.Message.Should().Be(string.Format(NotSupportErrorMessageTemplate, "{document}{Collection1}.Where(({document}{Value1} != 4))", "a"));

            expr =
                (a) => a.Collection1
                    .Any(
                        c => c.Value1 != 3 && c.Collection1.Any(d => a.Value1 != 5));
            exception = Assert.Throws<NotSupportedException>(() => Execute(CreateWhereQuery(expr)));
            exception.Message.Should().Be(string.Format(NotSupportErrorMessageTemplate, "{document}{Collection1}.Where((({document}{Value1} != 3) AndAlso Any({document}{Collection1}.Where(({document}{Value1} != 5)))))", "a"));
        }

        private IEnumerable<BsonDocument> Execute<T>(IMongoQueryable<T> queryable)
        {
            var result = (AggregateQueryableExecutionModel<T>)queryable.GetExecutionModel();
            return result.Stages;
        }

        private IMongoQueryable<TestObject> CreateWhereQuery(Expression<Func<TestObject, bool>> expression)
        {
            return CreateQuery().Where(expression);
        }

        private IMongoQueryable<TestObject> CreateQuery()
        {
            return _collection.AsQueryable();
        }
    }
}