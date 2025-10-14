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
using System.Linq.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using static System.Linq.Expressions.Expression;

namespace MongoDB.Driver.Linq.Linq3Implementation.Misc;

/// <summary>
/// This visitor rewrites expressions where new features of .NET CLR or
/// C# compiler interfere with LINQ expression tree translation.
/// </summary>
internal class ClrCompatExpressionPreprocessor : ExpressionVisitor
{
    private static readonly ClrCompatExpressionPreprocessor s_clrCompatExpressionPreprocessor = new();

    public static Expression Preprocess(Expression expression)
        => s_clrCompatExpressionPreprocessor.Visit(expression);

    /// <inheritdoc />
    protected override Expression VisitMethodCall(MethodCallExpression methodCall)
    {
        var method = methodCall.Method;

        // C# 14 compiler changes mean array and list Contains now prefer MemoryExtensions with a ReadOnlySpan<T> cast
        // instead of the traditional Enumerable methods. This aliases them back to their original Enumerable methods.
        if (method.DeclaringType == typeof(MemoryExtensions) && method.IsGenericMethod)
        {
            switch (method.Name)
            {
                // Replace MemoryExtensions.Contains<T>(ReadOnlySpan<T>, T) with Enumerable.Contains<T>(IEnumerable<T>, T)
                case nameof(MemoryExtensions.Contains)
                    when (methodCall.Arguments.Count == 2 || (methodCall.Arguments.Count == 3 &&
                                                              methodCall.Arguments[2] is ConstantExpression { Value: null }))
                         && TryUnwrapSpanImplicitCast(methodCall.Arguments[0], out var unwrappedSpanArg):
                    return Visit(
                        Call(
                            EnumerableMethod.Contains.MakeGenericMethod(method.GetGenericArguments()[0]),
                            unwrappedSpanArg, methodCall.Arguments[1]))!;

                // Replace MemoryExtensions.SequenceEqual<T>(ReadOnlySpan<T>, ReadOnlySpan<T>) with Enumerable.SequenceEqual<T>(IEnumerable<T>, IEnumerable<T>)
                case nameof(MemoryExtensions.SequenceEqual)
                    when methodCall.Arguments.Count == 2
                         && TryUnwrapSpanImplicitCast(methodCall.Arguments[0], out var unwrappedSpanArg)
                         && TryUnwrapSpanImplicitCast(methodCall.Arguments[1], out var unwrappedOtherArg):
                    return Visit(
                        Call(
                            EnumerableMethod.SequenceEqual.MakeGenericMethod(method.GetGenericArguments()[0]),
                            unwrappedSpanArg, unwrappedOtherArg))!;
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

        return base.VisitMethodCall(methodCall);
    }
}
