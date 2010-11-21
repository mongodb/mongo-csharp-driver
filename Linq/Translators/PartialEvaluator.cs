using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using MongoDB.Linq.Expressions;

namespace MongoDB.Linq.Translators
{
    internal static class PartialEvaluator
    {
        /// <summary>
        /// Performs evaluation and replacement of independent sub-trees
        /// </summary>
        /// <param name="expression">The root of the expression tree.</param>
        /// <param name="canBeEvaluated">A function that decides whether a given expression node can be part of the local function.</param>
        /// <returns>
        /// A new tree with sub-trees evaluated and replaced.
        /// </returns>
        public static Expression Evaluate(Expression expression, Func<Expression, bool> canBeEvaluated)
        {
            return new SubtreeEvaluator(new Nominator(canBeEvaluated).Nominate(expression)).Eval(expression);
        }

        /// <summary>
        /// 
        /// </summary>
        private class SubtreeEvaluator : ExpressionVisitor
        {
            private readonly HashSet<Expression> _candidates;

            internal SubtreeEvaluator(HashSet<Expression> candidates)
            {
                _candidates = candidates;
            }

            internal Expression Eval(Expression exp)
            {
                return Visit(exp);
            }

            protected override Expression Visit(Expression exp)
            {
                if (exp == null)
                    return null;
                if (_candidates.Contains(exp))
                {
                    return Evaluate(exp);
                }

                return base.Visit(exp);
            }

            private Expression Evaluate(Expression e)
            {
                if (e.NodeType == ExpressionType.Constant)
                    return e;

                var lambda = Expression.Lambda(e);
                var fn = lambda.Compile();
                return Expression.Constant(fn.DynamicInvoke(null), e.Type);
            }
        }
    }
}