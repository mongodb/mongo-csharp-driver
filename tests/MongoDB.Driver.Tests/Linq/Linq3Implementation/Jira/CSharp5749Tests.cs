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
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp5749Tests : LinqIntegrationTest<CSharp5749Tests.ClassFixture>
{
    public CSharp5749Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

#if NET6_0_OR_GREATER

    [Fact]
    public void MemoryExtension_contains_in_where_should_work()
    {
        var names = new[] { "Two", "Three" };

        var queryable = MangledQueryable().Where(x => names.Contains(x.Name));

        Assert.Equal(2, queryable.Count());
    }

    [Fact]
    public void MemoryExtension_contains_in_single_should_work()
    {
        var names = new[] { "Two" };

        var actual = MangledQueryable().Single(x => names.Contains(x.Name));

        Assert.Equal("Two", actual.Name);
    }

    [Fact]
    public void MemoryExtension_contains_in_any_should_work()
    {
        var ids = new[] { 2 };

        var actual = MangledQueryable().Any(x => ids.Contains(x.Id));

        Assert.True(actual);
    }

    [Fact]
    public void MemoryExtension_contains_in_count_should_work()
    {
        var ids = new[] { 2 };

        var actual = MangledQueryable().Count(x => ids.Contains(x.Id));

        Assert.Equal(1, actual);
    }

    [Fact]
    public void MemoryExtension_sequenceequal_in_where_should_work()
    {
        var ratings = new[] { 1, 9, 6 };

        var queryable = MangledQueryable().Where(x => ratings.SequenceEqual(x.Ratings));

        Assert.Equal(1, queryable.Count());
    }

    [Fact]
    public void MemoryExtension_sequenceequal_in_single_should_work()
    {
        var ratings = new[] { 1, 9, 6 };

        var actual = MangledQueryable().Single(x => ratings.SequenceEqual(x.Ratings));

        Assert.Equal("Three", actual.Name);
    }

    [Fact]
    public void MemoryExtension_sequenceequas_in_any_should_work()
    {
        var ratings = new[] { 1, 2, 3, 4, 5 };

        var actual = MangledQueryable().Any(x => ratings.SequenceEqual(x.Ratings));

        Assert.True(actual);
    }

    [Fact]
    public void MemoryExtension_sequenceequas_in_count_should_work()
    {
        var ratings = new[] { 3, 4, 5, 6, 7 };

        var actual = MangledQueryable().Count(x => ratings.SequenceEqual(x.Ratings));

        Assert.Equal(1, actual);
    }

#endif

    public class C
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int[] Ratings { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C, BsonDocument>
    {
        protected override IEnumerable<BsonDocument> InitialData =>
        [
            BsonDocument.Parse("{ _id : 1, Name: \"One\", Ratings: [1,2,3,4,5] }"),
            BsonDocument.Parse("{ _id : 2, Name: \"Two\", Ratings: [3,4,5,6,7] }"),
            BsonDocument.Parse("{ _id : 3, Name: \"Three\", Ratings: [1,9,6] }")
        ];
    }

    public IQueryable<C> MangledQueryable()
        => new ManglingQueryable<C>(Fixture.Collection.AsQueryable());

    public class ManglingQueryProvider(IQueryProvider innerProvider) : IQueryProvider
    {
        public IQueryable CreateQuery(Expression expression)
            => innerProvider.CreateQuery(EnumerableToMemoryExtensionsMangler.Mangle(expression));

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            var mangledExpression = EnumerableToMemoryExtensionsMangler.Mangle(expression);
            return innerProvider.CreateQuery<TElement>(mangledExpression);
        }

        public object Execute(Expression expression)
        {
            var mangledExpression = EnumerableToMemoryExtensionsMangler.Mangle(expression);
            return innerProvider.Execute(mangledExpression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            var mangledExpression = EnumerableToMemoryExtensionsMangler.Mangle(expression);
            return innerProvider.Execute<TResult>(mangledExpression);
        }
    }

    public class ManglingQueryable<T>(IQueryable<T> innerQueryable) : IOrderedQueryable<T>
    {
        private readonly ManglingQueryProvider _provider = new(innerQueryable.Provider);

        public Type ElementType => innerQueryable.ElementType;
        public Expression Expression => innerQueryable.Expression;
        public IQueryProvider Provider => _provider;

        public IEnumerator<T> GetEnumerator()
        {
            var mangledExpression = EnumerableToMemoryExtensionsMangler.Mangle(Expression);
            var query = innerQueryable.Provider.CreateQuery<T>(mangledExpression);
            return query.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            => GetEnumerator();
    }

    public class EnumerableToMemoryExtensionsMangler : ExpressionVisitor
    {
        private static readonly EnumerableToMemoryExtensionsMangler s_instance = new();

        public static Expression Mangle(Expression expression)
            => s_instance.Visit(expression);

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            // Check if this is Enumerable.Contains or Enumerable.SequenceEqual
            if (node.Method.DeclaringType == typeof(Enumerable) &&
                node.Method.IsGenericMethod &&
                (node.Method.Name == nameof(Enumerable.Contains) ||
                 node.Method.Name == nameof(Enumerable.SequenceEqual)))
            {
                var elementType = node.Method.GetGenericArguments()[0];

                // Get ReadOnlySpan<T>.op_Implicit method
                var spanType = typeof(ReadOnlySpan<>).MakeGenericType(elementType);
                var opImplicitMethod = spanType.GetMethod("op_Implicit",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [elementType.MakeArrayType()],
                    null);

                if (opImplicitMethod == null)
                    return base.VisitMethodCall(node);

                switch (node.Method.Name)
                {
                    case nameof(Enumerable.Contains):
                        {
                            var source = node.Arguments[0];
                            if (!source.Type.IsArray)
                                return base.VisitMethodCall(node);

                            var containsMethod = typeof(MemoryExtensions)
                                                     .GetMethods(BindingFlags.Public | BindingFlags.Static)
                                                     .FirstOrDefault(m =>
                                                         m.Name == nameof(MemoryExtensions.Contains) &&
                                                         m.IsGenericMethod &&
                                                         m.GetParameters().Length == 2 &&
                                                         m.GetParameters()[0].ParameterType.IsGenericType &&
                                                         m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() ==
                                                         typeof(ReadOnlySpan<>))
                                                     ?.MakeGenericMethod(elementType)
                                                 ?? throw new InvalidOperationException(
                                                     "Could not find MemoryExtensions.Contains method.");

                            return Expression.Call(containsMethod,
                                Expression.Call(opImplicitMethod, Visit(source)),
                                Visit(node.Arguments[1]));
                        }

                    case nameof(Enumerable.SequenceEqual):
                        {
                            var first = node.Arguments[0];
                            var second = node.Arguments[1];
                            if (!first.Type.IsArray || !second.Type.IsArray)
                                return base.VisitMethodCall(node);

                            var sequenceEqualMethod = typeof(MemoryExtensions)
                                                          .GetMethods(BindingFlags.Public | BindingFlags.Static)
                                                          .FirstOrDefault(m =>
                                                              m.Name == nameof(MemoryExtensions.SequenceEqual) &&
                                                              m.IsGenericMethod &&
                                                              m.GetParameters().Length == 2 &&
                                                              m.GetParameters()[0].ParameterType.IsGenericType &&
                                                              m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() ==
                                                              typeof(ReadOnlySpan<>) &&
                                                              m.GetParameters()[1].ParameterType.IsGenericType &&
                                                              m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() ==
                                                              typeof(ReadOnlySpan<>))
                                                          ?.MakeGenericMethod(elementType)
                                                      ?? throw new InvalidOperationException(
                                                          "Could not find MemoryExtensions.SequenceEquals method.");

                            return Expression.Call(sequenceEqualMethod,
                                Expression.Call(opImplicitMethod, Visit(first)),
                                Expression.Call(opImplicitMethod, Visit(second)));
                        }
                }
            }

            return base.VisitMethodCall(node);
        }
    }
}
