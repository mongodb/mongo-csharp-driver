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
using System.Reflection;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

#if NET6_0_OR_GREATER || NETCOREAPP3_1_OR_GREATER
public class CSharp5749Tests : LinqIntegrationTest<CSharp5749Tests.ClassFixture>
{
    public CSharp5749Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void MemoryExtensions_Contains_in_Where_should_work()
    {
        var collection = Fixture.Collection;
        var names = new[] { "Two", "Three" };

        var queryable = collection.AsQueryable().Where(Rewrite((C x) => names.Contains(x.Name)));

        var results = queryable.ToArray();
        results.Select(x => x.Id).Should().Equal(2, 3);
    }

    [Fact]
    public void MemoryExtensions_Contains_in_Where_should_work_with_enum()
    {
        var collection = Fixture.Collection;
        var daysOfWeek = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday };

        // Can't actually rewrite/fake these with MemoryExtensions.Contains overload with 3 args from .NET 10
        // This test will activate correctly on .NET 10+
        var queryable = collection.AsQueryable().Where(x => daysOfWeek.Contains(x.Day));

        var results = queryable.ToArray();
        results.Select(x => x.Id).Should().Equal(2, 3);
    }

    [Fact]
    public void MemoryExtensions_Contains_in_Single_should_work()
    {
        var collection = Fixture.Collection;
        var names = new[] { "Two" };

        var result = collection.AsQueryable().Single(Rewrite((C x) => names.Contains(x.Name)));

        result.Id.Should().Be(2);
    }

    [Fact]
    public void MemoryExtensions_Contains_in_Any_should_work()
    {
        var collection = Fixture.Collection;
        var ids = new[] { 2 };

        var result = collection.AsQueryable().Any(Rewrite((C x) => ids.Contains(x.Id)));

        result.Should().BeTrue();
    }

    [Fact]
    public void MemoryExtensions_Contains_in_Count_should_work()
    {
        var collection = Fixture.Collection;
        var ids = new[] { 2 };

        var result = collection.AsQueryable().Count(Rewrite((C x) => ids.Contains(x.Id)));

        result.Should().Be(1);
    }

    [Fact]
    public void MemoryExtensions_SequenceEqual_in_Where_should_work()
    {
        var collection = Fixture.Collection;
        var ratings = new[] { 1, 9, 6 };

        var queryable = collection.AsQueryable().Where(Rewrite((C x) => ratings.SequenceEqual(x.Ratings)));

        var results = queryable.ToArray();
        results.Select(x => x.Id).Should().Equal(3);
    }

    [Fact]
    public void MemoryExtensions_SequenceEqual_in_Where_should_work_with_enum()
    {
        var collection = Fixture.Collection;
        var daysOfWeek = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday };

        // Can't actually rewrite/fake these with MemoryExtensions.SequenceEqual overload with 3 args from .NET 10
        // This test will activate correctly on .NET 10+
        var queryable = collection.AsQueryable().Where(x => daysOfWeek.SequenceEqual(x.Days));

        var results = queryable.ToArray();
        results.Select(x => x.Id).Should().Equal(1);
    }

    [Fact]
    public void MemoryExtensions_SequenceEqual_in_Single_should_work()
    {
        var collection = Fixture.Collection;
        var ratings = new[] { 1, 9, 6 };

        var result = collection.AsQueryable().Single(Rewrite((C x) => ratings.SequenceEqual(x.Ratings)));

        result.Id.Should().Be(3);
    }

    [Fact]
    public void MemoryExtensions_SequenceEqual_in_Any_should_work()
    {
        var collection = Fixture.Collection;
        var ratings = new[] { 1, 2, 3, 4, 5 };

        var result = collection.AsQueryable().Any(Rewrite((C x) => ratings.SequenceEqual(x.Ratings)));

        result.Should().BeTrue();
    }

    [Fact]
    public void MemoryExtensions_SequenceEqual_in_Count_should_work()
    {
        var collection = Fixture.Collection;
        var ratings = new[] { 3, 4, 5, 6, 7 };

        var result = collection.AsQueryable().Count(Rewrite((C x) => ratings.SequenceEqual(x.Ratings)));

        result.Should().Be(1);
    }

    [Fact]
    public void Enumerable_Contains_with_null_comparer_should_work()
    {
        var collection = Fixture.Collection;
        var names = new[] { "Two", "Three" };

        var queryable = collection.AsQueryable().Where(x => names.Contains(x.Name, null));

        var results = queryable.ToArray();
        results.Select(x => x.Id).Should().Equal(2, 3);
    }

    [Fact]
    public void Enumerable_SequenceEqual_with_null_comparer_should_work()
    {
        var collection = Fixture.Collection;
        var ratings = new[] { 1, 9, 6 };

        var queryable = collection.AsQueryable().Where(x => ratings.SequenceEqual(x.Ratings, null));

        var results = queryable.ToArray();
        results.Select(x => x.Id).Should().Equal(3);
    }

    [Fact]
    public void Queryable_Contains_with_null_comparer_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable().Where(x => x.Days.AsQueryable().Contains(x.Day, null));

        var results = queryable.ToArray();
        results.Select(x => x.Id).Should().Equal(2, 3);
    }

    [Fact]
    public void Queryable_SequenceEqual_with_null_comparer_should_work()
    {
        var collection = Fixture.Collection;

        var result = collection.AsQueryable().Count(x => x.Ratings.SequenceEqual(x.Ratings, null));

        result.Should().Be(3);
    }

    public class C
    {
        public int Id { get; set; }
        public DayOfWeek Day { get; set; }
        public string Name { get; set; }
        public int[] Ratings { get; set; }
        public DayOfWeek[] Days { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C, BsonDocument>
    {
        protected override IEnumerable<BsonDocument> InitialData =>
        [
            BsonDocument.Parse("{ _id : 1, Name : \"One\", Day : 0, Ratings : [1, 2, 3, 4, 5], Days : [1, 2] }"),
            BsonDocument.Parse("{ _id : 2, Name : \"Two\", Day : 1, Ratings : [3, 4, 5, 6, 7], Days: [1, 2, 3] }"),
            BsonDocument.Parse("{ _id : 3, Name : \"Three\", Day : 2, Ratings : [1, 9, 6], Days: [2, 3, 4] }")
        ];
    }

    private Expression<Func<T, bool>> Rewrite<T>(Expression<Func<T, bool>> predicate)
    {
        return (Expression<Func<T,bool>>)new EnumerableToMemoryExtensionsRewriter().Visit(predicate);
    }

    public class EnumerableToMemoryExtensionsRewriter : ExpressionVisitor
    {
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            node = (MethodCallExpression)base.VisitMethodCall(node);

            var method = node.Method;
            var arguments = node.Arguments;

            return method.Name switch
            {
                "Contains" => VisitContainsMethod(node, method, arguments),
                "SequenceEqual" => VisitSequenceEqualMethod(node, method, arguments),
                _ => node
            };

            static Expression VisitContainsMethod(MethodCallExpression node, MethodInfo method, ReadOnlyCollection<Expression> arguments)
            {
                if (method.Is(EnumerableMethod.Contains))
                {
                    var itemType = method.GetGenericArguments().Single();
                    var source = arguments[0];
                    var value = arguments[1];

                    if (source.Type.IsArray)
                    {
                        var readOnlySpan = ImplicitCastArrayToSpan(source, typeof(ReadOnlySpan<>), itemType);

                        // Not worth checking for IEquatable<T> and generating 3 args overload as that requires .NET 10
                        // which if we had we could run the tests on natively without this visitor.

                        return Expression.Call(
                            MemoryExtensionsMethod.ContainsWithReadOnlySpanAndValue.MakeGenericMethod(itemType),
                            [readOnlySpan, value]);
                    }
                }
                else if (method.Is(EnumerableMethod.ContainsWithComparer))
                {
                    var itemType = method.GetGenericArguments().Single();
                    var source = arguments[0];
                    var value = arguments[1];
                    var comparer = arguments[2];

                    if (source.Type.IsArray)
                    {
                        var readOnlySpan = ImplicitCastArrayToSpan(source, typeof(ReadOnlySpan<>), itemType);
                        return
                            Expression.Call(
                                MemoryExtensionsMethod.ContainsWithReadOnlySpanAndValueAndComparer.MakeGenericMethod(itemType),
                                [readOnlySpan, value, comparer]);
                    }
                }


                return node;
            }

            static Expression VisitSequenceEqualMethod(MethodCallExpression node, MethodInfo method, ReadOnlyCollection<Expression> arguments)
            {
                if (method.Is(EnumerableMethod.SequenceEqual))
                {
                    var itemType = method.GetGenericArguments().Single();
                    var first = arguments[0];
                    var second = arguments[1];

                    if (first.Type.IsArray && second.Type.IsArray)
                    {
                        var firstReadOnlySpan = ImplicitCastArrayToSpan(first, typeof(ReadOnlySpan<>), itemType);
                        var secondReadOnlySpan = ImplicitCastArrayToSpan(second, typeof(ReadOnlySpan<>), itemType);
                        return
                            Expression.Call(
                                MemoryExtensionsMethod.SequenceEqualWithReadOnlySpanAndReadOnlySpan.MakeGenericMethod(itemType),
                                [firstReadOnlySpan, secondReadOnlySpan]);
                    }
                }
                else if (method.Is(EnumerableMethod.SequenceEqualWithComparer))
                {
                    var itemType = method.GetGenericArguments().Single();
                    var first = arguments[0];
                    var second = arguments[1];
                    var comparer = arguments[2];

                    if (first.Type.IsArray && second.Type.IsArray)
                    {
                        var firstReadOnlySpan = ImplicitCastArrayToSpan(first, typeof(ReadOnlySpan<>), itemType);
                        var secondReadOnlySpan = ImplicitCastArrayToSpan(second, typeof(ReadOnlySpan<>), itemType);
                        return
                            Expression.Call(
                                MemoryExtensionsMethod.SequenceEqualWithReadOnlySpanAndReadOnlySpan.MakeGenericMethod(itemType),
                                [firstReadOnlySpan, secondReadOnlySpan, comparer]);
                    }
                }

                return node;
            }

            static Expression ImplicitCastArrayToSpan(Expression value, Type spanType, Type itemType)
            {
                var opImplicitMethod = spanType.MakeGenericType(itemType).GetMethod(
                    "op_Implicit",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [itemType.MakeArrayType()],
                    null);
                return Expression.Call(opImplicitMethod, value);
            }
        }
    }
}
#endif
