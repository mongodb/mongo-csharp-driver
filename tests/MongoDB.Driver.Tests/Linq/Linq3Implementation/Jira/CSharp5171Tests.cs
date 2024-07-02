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
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp5171Tests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void Select_ReadOnlyDictionary_item_with_string_using_compiler_generated_expression_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)]
            LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.Dictionary["a"]);

            var stages = Translate(collection, queryable);
            var expectedStage = linqProvider == LinqProvider.V2 ? "{ $project : { a : '$Dictionary.a', _id : 0 } }" : "{ $project : { _v : '$Dictionary.a', _id : 0 } }";
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Should().Equal(1, 0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_ReadOnlyDictionary_item_with_string_using_call_to_get_item_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)]
            LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);
            var x = Expression.Parameter(typeof(C), "x");
            var body = Expression.Call(
                Expression.Property(x, typeof(C).GetProperty("Dictionary")!),
                typeof(IReadOnlyDictionary<string, int>).GetProperty("Item")!.GetGetMethod(),
                Expression.Constant("a"));
            var parameters = new[] {x};
            var selector = Expression.Lambda<Func<C, int>>(body, parameters);

            var queryable = collection.AsQueryable()
                .Select(selector);

            var stages = Translate(collection, queryable);
            var expectedStage = linqProvider == LinqProvider.V2 ? "{ $project : { a : '$Dictionary.a', _id : 0 } }" : "{ $project : { _v : '$Dictionary.a', _id : 0 } }";
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Should().Equal(1, 0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_ReadOnlyDictionary_item_with_string_using_MakeIndex_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)]
            LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);
            var x = Expression.Parameter(typeof(C), "x");
            var body = Expression.MakeIndex(
                Expression.Property(x, typeof(C).GetProperty("Dictionary")!),
                typeof(IReadOnlyDictionary<string, int>).GetProperty("Item"),
                new Expression[] {Expression.Constant("a")});
            var parameters = new[] {x};
            var selector = Expression.Lambda<Func<C, int>>(body, parameters);

            var queryable = collection.AsQueryable()
                .Select(selector);

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<NotSupportedException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $project : { _v : '$Dictionary.a', _id : 0 } }");

                var results = queryable.ToList();
                results.Should().Equal(1, 0);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Where_ReadOnlyDictionary_item_with_string_using_compiler_generated_expression_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)]
            LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Where(x => x.Dictionary["a"] == 1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'Dictionary.a' : 1 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Where_ReadOnlyDictionary_item_with_string_using_call_to_get_item_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)]
            LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);
            var x = Expression.Parameter(typeof(C), "x");
            var body = Expression.Equal(
                Expression.Call(
                    Expression.Property(x, typeof(C).GetProperty("Dictionary")!),
                    typeof(IReadOnlyDictionary<string, int>).GetProperty("Item")!.GetGetMethod(),
                    Expression.Constant("a")),
                Expression.Constant(1));
            var parameters = new[] {x};
            var predicate = Expression.Lambda<Func<C, bool>>(body, parameters);

            var queryable = collection.AsQueryable()
                .Where(predicate);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'Dictionary.a' : 1 } }");

            var results = queryable.ToList();
            results.Select(r => r.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Where_ReadOnlyDictionary_item_with_string_using_MakeIndex_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)]
            LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);
            var x = Expression.Parameter(typeof(C), "x");
            var body = Expression.Equal(
                Expression.MakeIndex(
                    Expression.Property(x, typeof(C).GetProperty("Dictionary")!),
                    typeof(IReadOnlyDictionary<string, int>).GetProperty("Item")!,
                    new Expression[] {Expression.Constant("a")}),
                Expression.Constant(1));
            var parameters = new[] {x};
            var predicate = Expression.Lambda<Func<C, bool>>(body, parameters);

            var queryable = collection.AsQueryable()
                .Where(predicate);

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<InvalidOperationException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $match : { 'Dictionary.a' : 1 } }");

                var results = queryable.ToList();
                results.Select(r => r.Id).Should().Equal(1);
            }
        }

        private IMongoCollection<C> GetCollection(LinqProvider linqProvider)
        {
            var collection = GetCollection<C>("test", linqProvider);
            CreateCollection(
                collection,
                new C {Id = 1, Dictionary = new ReadOnlyDictionary<string, int>(new Dictionary<string, int> {["a"] = 1})},
                new C {Id = 2, Dictionary = new ReadOnlyDictionary<string, int>(new Dictionary<string, int> {["b"] = 2})});
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public IReadOnlyDictionary<string, int> Dictionary { get; set; }
        }
    }
}
