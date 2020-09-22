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
using System.Linq.Expressions;

namespace MongoDB.Driver.Linq3.Misc
{
    // adapted from a Microsoft article entitled "Walkthrough: Creating an IQueryable LINQ Provider"
    // https://msdn.microsoft.com/en-us/library/bb546158.aspx

    public static class PartialEvaluator
    {
        #region static
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

            protected override Expression VisitMemberInit(MemberInitExpression node)
            {
                // Bindings must be visited before NewExpression
                foreach (var binding in node.Bindings)
                {
                    switch (binding.BindingType)
                    {
                        case MemberBindingType.Assignment:
                            var memberAssignment = (MemberAssignment)binding;
                            base.Visit(memberAssignment.Expression);
                            break;

                        default:
                            throw new InvalidOperationException($"Unexpected binding type: {binding.BindingType}.");
                    }
                }

                base.Visit(node.NewExpression);

                return node;
            }
        }
    }
}
