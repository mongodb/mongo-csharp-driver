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

namespace MongoDB.Driver.Linq.Linq3Implementation.Misc
{
    // adapted from a Microsoft article entitled "Walkthrough: Creating an IQueryable LINQ Provider"
    // https://msdn.microsoft.com/en-us/library/bb546158.aspx

    internal static class PartialEvaluator
    {
        #region static
        private static Type[] __customLinqExtensionMethodClasses = new[]
        {
            typeof(DateTimeExtensions),
            typeof(LinqExtensions),
            typeof(MongoEnumerable),
            typeof(Mql),
            typeof(StringExtensions)
        };

        public static Expression EvaluatePartially(Expression expression)
        {
            var nominator = new Nominator();
            nominator.Visit(expression);
            var candidates = nominator.Candidates;
            var evaluator = new SubtreeEvaluator(candidates);
            return evaluator.Visit(expression);
        }
        #endregion

        // nested types
        private class SubtreeEvaluator : ExpressionVisitor
        {
            // private fields
            private readonly HashSet<Expression> _candidates;

            // constructors
            internal SubtreeEvaluator(HashSet<Expression> candidates)
            {
                _candidates = candidates;
            }

            // public methods
            public override Expression Visit(Expression expression)
            {
                if (expression == null)
                {
                    return null;
                }
                if (_candidates.Contains(expression))
                {
                    return Evaluate(expression);
                }
                return base.Visit(expression);
            }

            protected override Expression VisitBinary(BinaryExpression node)
            {
                if (node.NodeType == ExpressionType.AndAlso)
                {
                    var leftExpression = Visit(node.Left);
                    if (leftExpression is ConstantExpression constantLeftExpression )
                    {
                        var value = (bool)constantLeftExpression.Value;
                        return value ? Visit(node.Right) : Expression.Constant(false);
                    }

                    var rightExpression = Visit(node.Right);
                    if (rightExpression is ConstantExpression constantRightExpression)
                    {
                        var value = (bool)constantRightExpression.Value;
                        return value ? leftExpression : Expression.Constant(false);
                    }

                    return node.Update(leftExpression, conversion: null, rightExpression);
                }

                if (node.NodeType == ExpressionType.OrElse)
                {
                    var leftExpression = Visit(node.Left);
                    if (leftExpression is ConstantExpression constantLeftExpression)
                    {
                        var value = (bool)constantLeftExpression.Value;
                        return value ? Expression.Constant(true) : Visit(node.Right);
                    }

                    var rightExpression = Visit(node.Right);
                    if (rightExpression is ConstantExpression constantRightExpression)
                    {
                        var value = (bool)constantRightExpression.Value;
                        return value ? Expression.Constant(true) : leftExpression;
                    }

                    return node.Update(leftExpression, conversion: null, rightExpression);
                }

                return base.VisitBinary(node);
            }

            protected override Expression VisitConditional(ConditionalExpression node)
            {
                var test = Visit(node.Test);
                if (test is ConstantExpression constantTestExpression)
                {
                    var value = (bool)constantTestExpression.Value;
                    return value ? Visit(node.IfTrue) : Visit(node.IfFalse);
                }

                return node.Update(test, Visit(node.IfTrue), Visit(node.IfFalse));
            }

            // private methods
            private Expression Evaluate(Expression expression)
            {
                if (expression.NodeType == ExpressionType.Constant)
                {
                    return expression;
                }
                LambdaExpression lambda = Expression.Lambda(expression);
                Delegate fn = lambda.Compile();
                return Expression.Constant(fn.DynamicInvoke(null), expression.Type);
            }
        }

        private class Nominator : ExpressionVisitor
        {
            #region static
            private static bool CanBeEvaluatedLocally(Expression expression)
            {
                if (expression is ConstantExpression constantExpression)
                {
                    if (constantExpression.Value is IQueryable queryable && object.ReferenceEquals(queryable.Expression, expression))
                    {
                        return false;
                    }
                }

                return expression.NodeType != ExpressionType.Parameter;
            }
            #endregion

            // private fields
            private readonly HashSet<Expression> _candidates = new HashSet<Expression>();
            private bool _cannotBeEvaluated;

            // public properties
            public HashSet<Expression> Candidates => _candidates;

            // public methods
            public override Expression Visit(Expression expression)
            {
                if (expression != null)
                {
                    bool saveCannotBeEvaluated = _cannotBeEvaluated;
                    _cannotBeEvaluated = false;
                    base.Visit(expression);
                    if (!_cannotBeEvaluated)
                    {
                        if (CanBeEvaluatedLocally(expression))
                        {
                            _candidates.Add(expression);
                        }
                        else
                        {
                            _cannotBeEvaluated = true;
                        }
                    }
                    _cannotBeEvaluated |= saveCannotBeEvaluated;
                }
                return expression;
            }

            protected override Expression VisitListInit(ListInitExpression node)
            {
                // Initializers must be visited before NewExpression
                Visit(node.Initializers, VisitElementInit);

                if (_cannotBeEvaluated)
                {
                    // visit only the arguments if any Initializers cannot be partially evaluated
                    Visit(node.NewExpression.Arguments);
                }
                else
                {
                    Visit(node.NewExpression);
                }

                return node;
            }

            protected override Expression VisitMemberInit(MemberInitExpression node)
            {
                // Bindings must be visited before NewExpression
                Visit(node.Bindings, VisitMemberBinding);

                if (_cannotBeEvaluated)
                {
                    // visit only the arguments if any MemberBindings cannot be partially evaluated
                    Visit(node.NewExpression.Arguments);
                }
                else
                {
                    Visit(node.NewExpression);
                }

                return node;
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                var result = base.VisitMethodCall(node);

                var method = node.Method;
                if (IsCustomLinqExtensionMethod(method))
                {
                    _cannotBeEvaluated = true;
                }

                return result;
            }

            private bool IsCustomLinqExtensionMethod(MethodInfo method)
            {
                return __customLinqExtensionMethodClasses.Contains(method.DeclaringType);
            }
        }
    }
}
