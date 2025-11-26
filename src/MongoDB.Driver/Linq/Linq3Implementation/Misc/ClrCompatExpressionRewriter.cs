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
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Misc;

/// <summary>
/// This visitor rewrites expressions where features of .NET CLR or
/// C# compiler interfere with LINQ expression tree translation.
/// </summary>
internal class ClrCompatExpressionRewriter : ExpressionVisitor
{
    private static readonly ClrCompatExpressionRewriter __instance = new();

    private static readonly MethodInfo[] __memoryExtensionsContainsMethods =
    [
        MemoryExtensionsMethod.ContainsWithReadOnlySpanAndValue,
        MemoryExtensionsMethod.ContainsWithSpanAndValue
    ];

    private static readonly MethodInfo[] __memoryExtensionsContainsWithComparerMethods =
    [
        MemoryExtensionsMethod.ContainsWithReadOnlySpanAndValueAndComparer
    ];

    private static readonly MethodInfo[] __memoryExtensionsSequenceEqualMethods =
    [
        MemoryExtensionsMethod.SequenceEqualWithReadOnlySpanAndReadOnlySpan,
        MemoryExtensionsMethod.SequenceEqualWithSpanAndReadOnlySpan
    ];

    private static readonly MethodInfo[] __memoryExtensionsSequenceEqualWithComparerMethods =
    [
        MemoryExtensionsMethod.SequenceEqualWithReadOnlySpanAndReadOnlySpanAndComparer,
        MemoryExtensionsMethod.SequenceEqualWithSpanAndReadOnlySpanAndComparer
    ];

    public static Expression Rewrite(Expression expression)
        => __instance.Visit(expression);

    /// <inheritdoc />
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
            if (method.IsOneOf(__memoryExtensionsContainsMethods))
            {
                var itemType = method.GetGenericArguments().Single();
                var span = arguments[0];
                var value = arguments[1];

                if (TryUnwrapSpanImplicitCast(span, out var unwrappedSpan) &&
                    unwrappedSpan.Type.ImplementsIEnumerableOf(itemType))
                {
                    return
                        Expression.Call(
                            EnumerableMethod.Contains.MakeGenericMethod(itemType),
                            [unwrappedSpan, value]);
                }
            }
            else if (method.IsOneOf(__memoryExtensionsContainsWithComparerMethods))
            {
                var itemType = method.GetGenericArguments().Single();
                var span = arguments[0];
                var value = arguments[1];
                var comparer = arguments[2];

                if (TryUnwrapSpanImplicitCast(span, out var unwrappedSpan) &&
                    unwrappedSpan.Type.ImplementsIEnumerableOf(itemType))
                {
                    return
                        Expression.Call(
                            EnumerableMethod.ContainsWithComparer.MakeGenericMethod(itemType),
                            [unwrappedSpan, value, comparer]);
                }
            }

            return node;
        }

        static Expression VisitSequenceEqualMethod(MethodCallExpression node, MethodInfo method, ReadOnlyCollection<Expression> arguments)
        {
            if (method.IsOneOf(__memoryExtensionsSequenceEqualMethods))
            {
                var itemType = method.GetGenericArguments().Single();
                var span = arguments[0];
                var other = arguments[1];

                if (TryUnwrapSpanImplicitCast(span, out var unwrappedSpan) &&
                    TryUnwrapSpanImplicitCast(other, out var unwrappedOther) &&
                    unwrappedSpan.Type.ImplementsIEnumerableOf(itemType) &&
                    unwrappedOther.Type.ImplementsIEnumerableOf(itemType))
                {
                    return
                        Expression.Call(
                            EnumerableMethod.SequenceEqual.MakeGenericMethod(itemType),
                            [unwrappedSpan, unwrappedOther]);
                }
            }
            else if (method.IsOneOf(__memoryExtensionsSequenceEqualWithComparerMethods))
            {
                var itemType = method.GetGenericArguments().Single();
                var span = arguments[0];
                var other = arguments[1];
                var comparer = arguments[2];

                if (TryUnwrapSpanImplicitCast(span, out var unwrappedSpan) &&
                    TryUnwrapSpanImplicitCast(other, out var unwrappedOther) &&
                    unwrappedSpan.Type.ImplementsIEnumerableOf(itemType) &&
                    unwrappedOther.Type.ImplementsIEnumerableOf(itemType))
                {
                    return
                        Expression.Call(
                            EnumerableMethod.SequenceEqualWithComparer.MakeGenericMethod(itemType),
                            [unwrappedSpan, unwrappedOther, comparer]);
                }
            }

            return node;
        }

        // Erase implicit casts to ReadOnlySpan<T> and Span<T>
        static bool TryUnwrapSpanImplicitCast(Expression expression, out Expression result)
        {
            if (expression is MethodCallExpression
                {
                    Method:
                    {
                        Name: "op_Implicit", DeclaringType: { IsGenericType: true } implicitCastDeclaringType
                    }
                } methodCallExpression
                && implicitCastDeclaringType.GetGenericTypeDefinition() is var genericTypeDefinition
                && (genericTypeDefinition == typeof(Span<>) || genericTypeDefinition == typeof(ReadOnlySpan<>)))
            {
                result = methodCallExpression.Arguments[0];
                return true;
            }

            result = null;
            return false;
        }
    }
}
