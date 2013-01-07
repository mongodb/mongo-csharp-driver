/* Copyright 2010-2013 10gen Inc.
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

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// A static class with methods to partially evaluate an Expression.
    /// </summary>
    public static class PartialEvaluator
    {
        /// <summary>
        /// Performs evaluation and replacement of independent sub-trees.
        /// </summary>
        /// <param name="expression">The root of the expression tree.</param>
        /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
        public static Expression Evaluate(Expression expression)
        {
            return Evaluate(expression, null);
        }

        /// <summary>
        /// Performs evaluation and replacement of independent sub-trees.
        /// </summary>
        /// <param name="expression">The root of the expression tree.</param>
        /// <param name="queryProvider">The query provider when the expression is a LINQ query (can be null).</param>
        /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
        public static Expression Evaluate(Expression expression, IQueryProvider queryProvider)
        {
            return new SubtreeEvaluator(new Nominator(e => CanBeEvaluatedLocally(e, queryProvider)).Nominate(expression)).Evaluate(expression);
        }

        private static bool CanBeEvaluatedLocally(Expression expression, IQueryProvider queryProvider)
        {
            // any operation on a query can't be done locally
            var constantExpression = expression as ConstantExpression;
            if (constantExpression != null)
            {
                var query = constantExpression.Value as IQueryable;
                if (query != null && (queryProvider == null || query.Provider == queryProvider))
                {
                    return false;
                }
            }

            var methodCallExpression = expression as MethodCallExpression;
            if (methodCallExpression != null)
            {
                var declaringType = methodCallExpression.Method.DeclaringType;
                if (declaringType == typeof(Enumerable) || declaringType == typeof(Queryable))
                {
                    return false;
                }
                if (declaringType == typeof(LinqToMongo))
                {
                    return false;
                }
            }

            if (expression.NodeType == ExpressionType.Convert && expression.Type == typeof(object))
            {
                return true;
            }

            if (expression.NodeType == ExpressionType.Parameter || expression.NodeType == ExpressionType.Lambda)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Evaluates and replaces sub-trees when first candidate is reached (top-down)
        /// </summary>
        class SubtreeEvaluator : ExpressionVisitor
        {
            HashSet<Expression> _candidates;

            internal SubtreeEvaluator(HashSet<Expression> candidates)
            {
                _candidates = candidates;
            }

            internal Expression Evaluate(Expression exp)
            {
                return this.Visit(exp);
            }

            protected override Expression Visit(Expression exp)
            {
                if (exp == null)
                {
                    return null;
                }
                if (_candidates.Contains(exp))
                {
                    return this.EvaluateSubtree(exp);
                }
                return base.Visit(exp);
            }

            private Expression EvaluateSubtree(Expression e)
            {
                if (e.NodeType == ExpressionType.Constant)
                {
                    return e;
                }
                LambdaExpression lambda = Expression.Lambda(e);
                Delegate fn = lambda.Compile();
                return Expression.Constant(fn.DynamicInvoke(null), e.Type);
            }
        }
    }
}
