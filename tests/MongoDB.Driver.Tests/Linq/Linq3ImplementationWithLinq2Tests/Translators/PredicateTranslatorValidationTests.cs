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
using MongoDB.Driver;
using MongoDB.Driver.Linq.Linq3Implementation;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToExecutableQueryTranslators;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationWithLinq2Tests.Translators
{
    public class PredicateTranslatorValidationTests
    {
        private IMongoCollection<TestObject> _collection;

        private void Setup()
        {
            var client = DriverTestConfiguration.Client;
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
            AssertWhere(
                expr,
                @"{ $match : { Collection2 : { $in : [1, 2] } } }");
        }

        [Fact]
        public void Should_not_throw_the_exception_when_a_child_expression_is_called_from_nested_locals()
        {
            Setup();

            var local = new List<int> { 1, 2 };
            var local2 = new List<bool> { true, false };

            Expression<Func<TestObject, bool>> expr = (a) => a.Collection1.Any(b => local.Any(c => b.Collection2.Contains(c)));
            AssertWhere(expr, "{ $match : { Collection1 : { $elemMatch : { Collection2 : { $in : [1, 2] } } } } }");

            expr = (a) => a.Collection1.Any(b => b.Collection1.Any(c => local.Any(d => c.Collection2.Contains(d))));
            AssertWhere(expr, "{ $match : { Collection1 : { $elemMatch : { Collection1 : { $elemMatch : { Collection2 : { $in : [1, 2] } } } } } } }");

            expr = (a) => a.Collection1.Any(b => b.Collection2 != null && b.Collection1.Any(c => local.Any(d => c.Collection2.Contains(d))));
            AssertWhere(expr, "{ $match : { Collection1 : { $elemMatch : { Collection2 : { $ne : null }, Collection1 : { $elemMatch : { Collection2 : { $in : [1, 2 ] } } } } } } }");

            expr = (a) => a.Collection1.Any(
                b =>
                    b.Collection1 != null && b.Collection1.Any(
                        c =>
                            local.Any(d => c.Collection2.Contains(d)) &&
                            local2.Any(e => c.Collection3.Contains(e))
                        ));
            AssertWhere(
                expr,
                @"
                {
                    $match : {
                        Collection1 : {
                            $elemMatch : {
                                Collection1 : {
                                    $ne : null,
                                    $elemMatch : {
                                        Collection2 : { $in : [1, 2] },
                                        Collection3 : { $in : [true, false] }
                                    }
                                }
                            }
                        }
                    }
                }");

            expr = (a) => a.Collection1.Any(
                b =>
                    b.Collection1 != null && b.Collection1.Any(
                        c =>
                            c.Collection1.Any(d => d.Value1 == 2) &&
                            local2.Any(e => c.Collection3.Contains(e))
                        ));
            AssertWhere(
                expr,
                @"
                {
                    $match : {
                        Collection1 : {
                            $elemMatch : {
                                Collection1 : {
                                    $ne : null,
                                    $elemMatch : {
                                        Collection1 : { $elemMatch : { Value1 : 2 } },
                                        Collection3 : { $in : [true, false] }
                                    }
                                }
                            }
                        }
                    }
                }");
        }

        [Fact]
        public void Should_not_throw_the_exception_when_a_predicate_has_only_parameter_expressions()
        {
            Setup();

            Expression<Func<TestObject, bool>> expr = (a) => a.Collection3.Any(b => b);
            AssertWhere(
                expr,
                @"{ $match : { Collection3 : true } }");

            expr = (a) => a.Collection3.Any(b => b && b);
            AssertWhere(
                expr,
                @"{ $match : { Collection3 : { $elemMatch : { $and : [{ $eq : true }, { $eq : true }] } } } }");

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

            AssertWhere(
                expr,
                @"{ $match : { Collection1 : { $elemMatch : { Collection1 : { $elemMatch : { Value1 : { $ne : 2 } } }, Value1 : 3 } } } }");
        }

        [Fact]
        public void Should_not_throw_the_exception_when_there_are_several_conditions_and_there_are_no_parent_parameters_in_children()
        {
            Setup();

            Expression<Func<TestObject, bool>> expr =
                (a) =>
                    a.Collection1.Any(c => c.Value1 == 3) &&
                    a.Collection1.Any(d => d.Value1 != 4);

            AssertWhere(
                expr,
                @"{ $match : { $and : [{ Collection1 : { $elemMatch : { Value1 : 3 } } }, { Collection1 : { $elemMatch : { Value1 : { $ne : 4 } } } } ] } }");

            expr =
                (a) => a.Collection1
                    .Any(
                        c => c.Value1 != 3 && c.Collection1.Any(d => d.Value1 != 5));

            AssertWhere(
                expr,
                @"{ $match : { Collection1 : { $elemMatch : { Value1 : { $ne : 3 }, Collection1 : { $elemMatch : { Value1 : { $ne : 5 } } } } } } }");
        }


        [Fact]
        public void Should_not_throw_the_exception_when_top_method_is_called_from_other_linq_method_and_there_is_no_any_using_of_cross_levels_parameters()
        {
            Setup();

            var query = CreateQuery().Select(x => x.Collection1).Where(x => x.Any(y => y.Value1 > 1));

            AssertQuery(
                query,
                @"{ $project : { _v : '$Collection1', _id : 0 } }",
                @"{ $match : { _v : { $elemMatch : { Value1 : { $gt : 1 } } } } }");
        }

        [Fact]
        public void Should_not_throw_the_exception_when_top_method_is_not_where_predicate()
        {
            Setup();

            var query1 = CreateQuery().Select(x => x.Collection1.Where(y => y.Value1 > x.Value1));
            AssertQuery(
                query1,
                @"{ $project : { _v : { $filter : { input : '$Collection1', as : 'y', cond : { $gt : ['$$y.Value1', '$Value1'] } } }, _id : 0 } }");

            var query2 = CreateQuery().GroupBy(x => x.Collection1.Where(y => y.Value1 > x.Value1));
            AssertQuery(
                query2,
                @"{ $group : { _id : { $filter : { input : '$Collection1', as : 'y', cond : { $gt : ['$$y.Value1', '$Value1'] } } }, _elements : { $push : '$$ROOT' } } }");
        }

        [Fact]
        public void Should_not_throw_the_exception_when_a_child_expression_uses_parameters_from_grandparents()
        {
            Setup();

            Expression<Func<TestObject, bool>> expr = (a) => a.Collection1.Any(b => b.Collection1.Any(c => a.Value1 == 2));

            AssertWhere(expr, "{ $match : { $expr : { $anyElementTrue : { $map : { input : '$Collection1', as : 'b', in : { $anyElementTrue : { $map : { input : '$$b.Collection1', as : 'c', in : { $eq : ['$Value1', 2] } } } } } } } } }");
        }

        [Fact]
        public void Should_not_throw_the_exception_when_a_child_expression_uses_parameters_from_parents()
        {
            Setup();

            Expression<Func<TestObject, bool>> expr = (a) => a.Collection1.Any(b => a.Value1 == 2);
            AssertWhere(expr, "{ $match : { $expr : { $anyElementTrue : { $map : { input : '$Collection1', as : 'b', in : { $eq : ['$Value1', 2] } } } } } }");

            expr = (a) => a.Collection1.Any(b => b.Value1 == 2 && a.Value1 == 3);
            AssertWhere(expr, "{ $match : { $expr : { $anyElementTrue : { $map : { input : '$Collection1', as : 'b', in : { $and : [{ $eq : ['$$b.Value1', 2] }, { $eq : ['$Value1', 3] }] } } } } } }");

            expr = (a) => a.Collection3.Any(b => a.Value2);
            AssertWhere(expr, "{ $match : { $expr : { $anyElementTrue : { $map : { input : '$Collection3', as : 'b', in : '$Value2' } } } } }");

            expr = (a) => a.Collection1.Where(b => a.Value1 == 2).Any();
            AssertWhere(expr, "{ $match : { $expr : { $gt : [{ $size : { $filter : { input : '$Collection1', as : 'b', cond : { $eq : ['$Value1', 2] } } } }, 0] } } }");
        }

        [Fact]
        public void Should_not_throw_the_exception_when_there_is_the_parent_parameter_in_the_child_expression_and_there_are_several_conditions()
        {
            Setup();

            Expression<Func<TestObject, bool>> expr =
                (a) =>
                    a.Collection1.Any(c => c.Value1 == 3) &&
                    a.Collection1.Any(d => a.Value1 != 4);
            AssertWhere(expr, "{ $match : { $and : [{ Collection1 : { $elemMatch : { 'Value1' : 3 } } }, { $expr : { $anyElementTrue : { $map : { input : '$Collection1', as : 'd', in : { $ne : ['$Value1', 4] } } } } }] } }");

            expr =
                (a) => a.Collection1
                    .Any(
                        c => c.Value1 != 3 && c.Collection1.Any(d => a.Value1 != 5));
            AssertWhere(expr, "{ $match : { $expr : { $anyElementTrue : { $map : { input : '$Collection1', as : 'c', in : { $and : [{ $ne : ['$$c.Value1', 3] }, { $anyElementTrue : { $map : { input : '$$c.Collection1', as : 'd', in : { $ne : ['$Value1', 5] } } } }] } } } }  } }");
        }

        // private methods
        private void AssertQuery<TResult>(IQueryable<TResult> query, params string[] expectedStages)
        {
            var actualStages = Translate(query).ToList();

            actualStages.Should().HaveCount(expectedStages.Length);
            for (var i = 0; i < actualStages.Count; i++)
            {
                var actualStage = actualStages[i];
                var expectedStage = expectedStages[i];
                actualStage.Should().Be(expectedStage);
            }
        }

        private void AssertWhere(Expression<Func<TestObject, bool>> expression, params string[] expectedStages)
        {
            var query = CreateWhereQuery(expression);
            AssertQuery(query, expectedStages);
        }

        private IEnumerable<BsonDocument> Translate<T>(IQueryable<T> queryable)
        {
            var provider = (MongoQueryProvider<TestObject>)queryable.Provider;
            var executableQuery = ExpressionToExecutableQueryTranslator.Translate<TestObject, T>(provider, queryable.Expression, translationOptions: null);
            return executableQuery.Pipeline.AstStages.Select(s => (BsonDocument)s.Render()).ToArray();
        }

        private IQueryable<TestObject> CreateWhereQuery(Expression<Func<TestObject, bool>> expression)
        {
            return CreateQuery().Where(expression);
        }

        private IQueryable<TestObject> CreateQuery()
        {
            return _collection.AsQueryable();
        }
    }
}
