/* Copyright 2010-2014 MongoDB Inc.
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

namespace MongoDB.Driver.Linq.Processors
{
    internal class PartialEvaluator : ExpressionVisitor
    {
        public static Expression Evaluate(Expression node)
        {
            var candidates = Nominator.Nominate(node);
            var evaluator = new PartialEvaluator(candidates);
            return evaluator.Visit(node);
        }

        private readonly HashSet<Expression> _candidates;

        private PartialEvaluator(HashSet<Expression> candidates)
        {
            _candidates = candidates;
        }

        public override Expression Visit(Expression node)
        {
            if (node == null)
            {
                return null;
            }
            if (!_candidates.Contains(node))
            {
                return base.Visit(node);
            }

            var evaluated = EvaluateSubtree(node);

            if (evaluated != node)
            {
                return PartialEvaluator.Evaluate(evaluated);
            }

            return evaluated;
        }

        private Expression EvaluateSubtree(Expression subtree)
        {
            if (subtree.NodeType == ExpressionType.Constant)
            {
                var constant = (ConstantExpression)subtree;
                var queryableValue = constant.Value as IQueryable;
                if (queryableValue != null && queryableValue.Expression != constant)
                {
                    return queryableValue.Expression;
                }

                return subtree;
            }

            Expression<Func<object>> lambda = Expression.Lambda<Func<object>>(Expression.Convert(subtree, typeof(object)));
            var compiledLambda = lambda.Compile();
            return Expression.Constant(compiledLambda(), subtree.Type);
        }

        private class Nominator : ExpressionVisitor
        {
            public static HashSet<Expression> Nominate(Expression node)
            {
                var nominator = new Nominator();
                nominator.Visit(node);
                return nominator._candidates;
            }

            private HashSet<Expression> _candidates;
            private bool _isBlocked;

            private Nominator()
            {
                _candidates = new HashSet<Expression>();
            }

            public override Expression Visit(Expression node)
            {
                if (node == null)
                {
                    return base.Visit(node);
                }

                bool _oldIsBlocked = _isBlocked;
                _isBlocked = false;

                var visited = base.Visit(node);

                if (!_isBlocked)
                {
                    _candidates.Add(node);
                }

                _isBlocked |= _oldIsBlocked;
                return visited;
            }

            protected override Expression VisitListInit(ListInitExpression node)
            {
                Visit(node.Initializers, VisitElementInit);

                if (_isBlocked)
                {
                    return node;
                }

                Visit(node.NewExpression);
                return node;
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                if (IsQueryableExpression(node.Expression))
                {
                    _isBlocked = true;
                }

                return base.VisitMember(node);
            }

            protected override Expression VisitMemberInit(MemberInitExpression node)
            {
                Visit(node.Bindings, VisitMemberBinding);

                if (_isBlocked)
                {
                    return node;
                }

                Visit(node.NewExpression);
                return node;
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (IsQueryableExpression(node.Object))
                {
                    _isBlocked = true;
                }

                for (int i = 0; i < node.Arguments.Count && !_isBlocked; i++)
                {
                    if (IsQueryableExpression(node.Arguments[i]))
                    {
                        _isBlocked = true;
                    }
                }

                return base.VisitMethodCall(node);
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                _isBlocked = true;
                return base.VisitParameter(node);
            }

            private static bool IsQueryableExpression(Expression node)
            {
                return node != null && typeof(IQueryable).IsAssignableFrom(node.Type);
            }
        }
    }
}
