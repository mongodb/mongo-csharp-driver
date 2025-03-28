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
using System.Linq.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToExecutableQueryTranslators;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToExecutableQueryTranslators
{
    public class LastMethodToExecutableQueryTranslator : LinqIntegrationTest<LastMethodToExecutableQueryTranslator.ClassFixture>
    {
        public LastMethodToExecutableQueryTranslator(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Last_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable().OrderBy(t => t.Id);
            var provider = (MongoQueryProvider<TestClass>)queryable.Provider;

            var lastMethod = typeof(Queryable).GetMethods()
                .First(m => m.Name == nameof(Queryable.Last) && m.GetParameters().Length == 1)
                .MakeGenericMethod(typeof(TestClass));

            var executableQuery = ExpressionToExecutableQueryTranslator.TranslateScalar<TestClass, TestClass>(
                provider,
                Expression.Call(lastMethod, queryable.Expression),
                translationOptions: null);

            var stages = executableQuery.Pipeline.Ast.Stages.Select(s => s.Render().AsBsonDocument).ToList();

            var expectedStages = new[] {
                """{ "$sort" : { "_id" : 1 } }""",
                """{ "$group" : { "_id" : null, "_last" : { "$last" : "$$ROOT" } } }""",
                """{ "$replaceRoot" : { "newRoot" : "$_last" } }"""
            };

            AssertStages(stages, expectedStages);
            var result = queryable.Last();
            Assert.Equal(3, result.Id);
        }

        [Fact]
        public void Last_with_null_return_should_throw()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable().Where(t => t.Id > 5).OrderBy(t => t.Id);
            var provider = (MongoQueryProvider<TestClass>)queryable.Provider;

            var lastMethod = typeof(Queryable).GetMethods()
                .First(m => m.Name == nameof(Queryable.Last) && m.GetParameters().Length == 1)
                .MakeGenericMethod(typeof(TestClass));

            var executableQuery = ExpressionToExecutableQueryTranslator.TranslateScalar<TestClass, TestClass>(
                provider,
                Expression.Call(lastMethod, queryable.Expression),
                translationOptions: null);

            var stages = executableQuery.Pipeline.Ast.Stages.Select(s => s.Render().AsBsonDocument).ToList();

            var expectedStages = new[] {
                """{ "$match" : { "_id" : { "$gt" : 5 } }}""",
                """{ "$sort" : { "_id" : 1 } }""",
                """{ "$group" : { "_id" : null, "_last" : { "$last" : "$$ROOT" } } }""",
                """{ "$replaceRoot" : { "newRoot" : "$_last" } }"""
            };

            AssertStages(stages, expectedStages);
            var exception = Record.Exception(() => queryable.Last());
            Assert.NotNull(exception);
            Assert.IsType<InvalidOperationException>(exception);
        }

        [Fact]
        public void LastOrDefault_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable().OrderBy(t => t.Id);
            var provider = (MongoQueryProvider<TestClass>)queryable.Provider;

            var lastMethod = typeof(Queryable).GetMethods()
                .First(m => m.Name == nameof(Queryable.LastOrDefault) && m.GetParameters().Length == 1)
                .MakeGenericMethod(typeof(TestClass));

            var executableQuery = ExpressionToExecutableQueryTranslator.TranslateScalar<TestClass, TestClass>(
                provider,
                Expression.Call(lastMethod, queryable.Expression),
                translationOptions: null);

            var stages = executableQuery.Pipeline.Ast.Stages.Select(s => s.Render().AsBsonDocument).ToList();

            var expectedStages = new[] {
                """{ "$sort" : { "_id" : 1 } }""",
                """{ "$group" : { "_id" : null, "_last" : { "$last" : "$$ROOT" } } }""",
                """{ "$replaceRoot" : { "newRoot" : "$_last" } }"""
            };

            AssertStages(stages, expectedStages);
            var result = queryable.LastOrDefault();
            Assert.NotNull(result);
            Assert.Equal(3, result.Id);
        }

        [Fact]
        public void LastOrDefault_with_null_return_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable().Where(t => t.Id > 5).OrderBy(t => t.Id);
            var provider = (MongoQueryProvider<TestClass>)queryable.Provider;

            var lastMethod = typeof(Queryable).GetMethods()
                .First(m => m.Name == nameof(Queryable.LastOrDefault) && m.GetParameters().Length == 1)
                .MakeGenericMethod(typeof(TestClass));

            var executableQuery = ExpressionToExecutableQueryTranslator.TranslateScalar<TestClass, TestClass>(
                provider,
                Expression.Call(lastMethod, queryable.Expression),
                translationOptions: null);

            var stages = executableQuery.Pipeline.Ast.Stages.Select(s => s.Render().AsBsonDocument).ToList();

            var expectedStages = new[] {
                """{ "$match" : { "_id" : { "$gt" : 5 } }}""",
                """{ "$sort" : { "_id" : 1 } }""",
                """{ "$group" : { "_id" : null, "_last" : { "$last" : "$$ROOT" } } }""",
                """{ "$replaceRoot" : { "newRoot" : "$_last" } }"""
            };

            AssertStages(stages, expectedStages);
            var result = queryable.LastOrDefault();
            Assert.Null(result);
        }

        [Fact]
        public void LastOrDefaultWithPredicate_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable().OrderBy(t => t.Id);
            var provider = (MongoQueryProvider<TestClass>)queryable.Provider;

            var lastMethod = typeof(Queryable).GetMethods()
                .First(m => m.Name == nameof(Queryable.Last) && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(TestClass));

            Expression<Func<TestClass, bool>> exp = t => t.Id > 1;

            var executableQuery = ExpressionToExecutableQueryTranslator.TranslateScalar<TestClass, TestClass>(
                provider,
                Expression.Call(lastMethod, queryable.Expression, exp),
                translationOptions: null);

            var stages = executableQuery.Pipeline.Ast.Stages.Select(s => s.Render().AsBsonDocument).ToList();

            var expectedStages = new[] {
                """{ "$sort" : { "_id" : 1 } }""",
                """{ "$match" : { "_id" : { "$gt" : 1 } }}""",
                """{ "$group" : { "_id" : null, "_last" : { "$last" : "$$ROOT" } } }""",
                """{ "$replaceRoot" : { "newRoot" : "$_last" } }"""
            };

            AssertStages(stages, expectedStages);
            var result = queryable.LastOrDefault(exp);
            Assert.NotNull(result);
            Assert.Equal(3, result.Id);
        }

        [Fact]
        public void LastOrDefaultWithPredicate_and_null_return_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable().OrderBy(t => t.Id);
            var provider = (MongoQueryProvider<TestClass>)queryable.Provider;

            var lastMethod = typeof(Queryable).GetMethods()
                .First(m => m.Name == nameof(Queryable.Last) && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(TestClass));

            Expression<Func<TestClass, bool>> exp = t => t.Id > 4;

            var executableQuery = ExpressionToExecutableQueryTranslator.TranslateScalar<TestClass, TestClass>(
                provider,
                Expression.Call(lastMethod, queryable.Expression, exp),
                translationOptions: null);

            var stages = executableQuery.Pipeline.Ast.Stages.Select(s => s.Render().AsBsonDocument).ToList();

            var expectedStages = new[] {
                """{ "$sort" : { "_id" : 1 } }""",
                """{ "$match" : { "_id" : { "$gt" : 4 } }}""",
                """{ "$group" : { "_id" : null, "_last" : { "$last" : "$$ROOT" } } }""",
                """{ "$replaceRoot" : { "newRoot" : "$_last" } }"""
            };

            AssertStages(stages, expectedStages);
            var result = queryable.LastOrDefault(exp);
            Assert.Null(result);
        }

        [Fact]
        public void LastWithPredicate_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable().OrderBy(t => t.Id);
            var provider = (MongoQueryProvider<TestClass>)queryable.Provider;

            var lastMethod = typeof(Queryable).GetMethods()
                .First(m => m.Name == nameof(Queryable.Last) && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(TestClass));

            Expression<Func<TestClass, bool>> exp = t => t.Id > 1;

            var executableQuery = ExpressionToExecutableQueryTranslator.TranslateScalar<TestClass, TestClass>(
                provider,
                Expression.Call(lastMethod, queryable.Expression, exp),
                translationOptions: null);

            var stages = executableQuery.Pipeline.Ast.Stages.Select(s => s.Render().AsBsonDocument).ToList();

            var expectedStages = new[] {
                """{ "$sort" : { "_id" : 1 } }""",
                """{ "$match" : { "_id" : { "$gt" : 1 } }}""",
                """{ "$group" : { "_id" : null, "_last" : { "$last" : "$$ROOT" } } }""",
                """{ "$replaceRoot" : { "newRoot" : "$_last" } }"""
            };

            AssertStages(stages, expectedStages);
            var result = queryable.Last(exp);
            Assert.Equal(3, result.Id);
        }

        [Fact]
        public void LastWithPredicate_and_null_return_should_throw()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable().OrderBy(t => t.Id);
            var provider = (MongoQueryProvider<TestClass>)queryable.Provider;

            var lastMethod = typeof(Queryable).GetMethods()
                .First(m => m.Name == nameof(Queryable.Last) && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(TestClass));

            Expression<Func<TestClass, bool>> exp = t => t.Id > 4;

            var executableQuery = ExpressionToExecutableQueryTranslator.TranslateScalar<TestClass, TestClass>(
                provider,
                Expression.Call(lastMethod, queryable.Expression, exp),
                translationOptions: null);

            var stages = executableQuery.Pipeline.Ast.Stages.Select(s => s.Render().AsBsonDocument).ToList();

            var expectedStages = new[] {
                """{ "$sort" : { "_id" : 1 } }""",
                """{ "$match" : { "_id" : { "$gt" : 4 } }}""",
                """{ "$group" : { "_id" : null, "_last" : { "$last" : "$$ROOT" } } }""",
                """{ "$replaceRoot" : { "newRoot" : "$_last" } }"""
            };

            AssertStages(stages, expectedStages);
            var exception = Record.Exception(() => queryable.Last(exp));
            Assert.NotNull(exception);
            Assert.IsType<InvalidOperationException>(exception);
        }

        public class TestClass
        {
            public int Id { get; set; }
            public string StringProperty { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<TestClass>
        {
            protected override IEnumerable<TestClass> InitialData { get; } =
            [
                new TestClass { Id = 1, StringProperty = "AB" },
                new TestClass { Id = 2, StringProperty = "ABC" },
                new TestClass { Id = 3, StringProperty = "ABCDE" }
            ];
        }
    }
}
