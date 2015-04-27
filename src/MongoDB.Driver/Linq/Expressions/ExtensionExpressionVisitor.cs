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
using System.Linq;
using System.Linq.Expressions;

namespace MongoDB.Driver.Linq.Expressions
{
    internal class ExtensionExpressionVisitor : ExpressionVisitor
    {
        public static LambdaExpression GetLambda(Expression node)
        {
            return (LambdaExpression)StripQuotes(node);
        }

        public static bool IsLambda(Expression node)
        {
            return StripQuotes(node).NodeType == ExpressionType.Lambda;
        }

        public static bool IsLambda(Expression node, int parameterCount)
        {
            var lambda = StripQuotes(node);
            return lambda.NodeType == ExpressionType.Lambda &&
                ((LambdaExpression)lambda).Parameters.Count == parameterCount;
        }

        public static bool IsLinqMethod(MethodCallExpression node, params string[] names)
        {
            if (node == null)
            {
                return false;
            }

            if (node.Method.DeclaringType != typeof(Enumerable) &&
                node.Method.DeclaringType != typeof(Queryable) &&
                node.Method.DeclaringType != typeof(MongoQueryable))
            {
                return false;
            }

            if (names == null || names.Length == 0)
            {
                return true;
            }

            return names.Contains(node.Method.Name);
        }

        private static Expression StripQuotes(Expression expression)
        {
            while (expression.NodeType == ExpressionType.Quote)
            {
                expression = ((UnaryExpression)expression).Operand;
            }
            return expression;
        }

        protected internal virtual Expression VisitAccumulator(AccumulatorExpression node)
        {
            return node.Update(Visit(node.Argument));
        }

        protected internal virtual Expression VisitCorrelatedAccumulator(CorrelatedAccumulatorExpression node)
        {
            return node.Update(VisitAndConvert<AccumulatorExpression>(node.Accumulator, "VisitCorrelatedAccumulator"));
        }

        protected internal virtual Expression VisitCorrelatedGroupBy(CorrelatedGroupByExpression node)
        {
            return node.Update(
                Visit(node.Source),
                Visit(node.Id),
                Visit(node.Accumulators));
        }

        protected internal virtual Expression VisitDistinct(DistinctExpression node)
        {
            return node.Update(
                Visit(node.Source),
                Visit(node.Selector));
        }

        protected internal virtual Expression VisitExtensionExpression(ExtensionExpression node)
        {
            throw new NotSupportedException(string.Format("{0} is an unknown expression.", node.GetType()));
        }

        protected internal virtual Expression VisitGroupByWithResultSelector(GroupByWithResultSelectorExpression node)
        {
            return node.Update(
                Visit(node.Source),
                Visit(node.Selector));
        }

        protected internal virtual Expression VisitGroupId(GroupIdExpression node)
        {
            return node.Update(Visit(node.Expression));
        }

        protected internal virtual Expression VisitOrderBy(OrderByExpression node)
        {
            return node.Update(
                Visit(node.Source),
                Visit(node.Clauses, VisitSortClause));
        }

        protected internal virtual Expression VisitProjection(ProjectionExpression node)
        {
            return node.Update(
                Visit(node.Source),
                Visit(node.Projector),
                VisitAndConvert<LambdaExpression>(node.Aggregator, "VisitPipeline"));
        }

        protected internal virtual Expression VisitRootAccumulator(RootAccumulatorExpression node)
        {
            return node.Update(
                Visit(node.Source),
                Visit(node.Accumulator));
        }

        protected internal virtual Expression VisitSelect(SelectExpression node)
        {
            return node.Update(
                Visit(node.Source),
                Visit(node.Selector));
        }

        protected internal virtual Expression VisitSelectMany(SelectManyExpression node)
        {
            return node.Update(
                Visit(node.Source),
                Visit(node.CollectionSelector),
                Visit(node.ResultSelector));
        }

        protected internal virtual Expression VisitSerialization(SerializationExpression node)
        {
            return node.Update(Visit(node.Expression));
        }

        protected internal virtual Expression VisitSkip(SkipExpression node)
        {
            return node.Update(
                Visit(node.Source),
                node.Count);
        }

        protected internal virtual SortClause VisitSortClause(SortClause clause)
        {
            return clause.Update(Visit(clause.Expression));
        }

        protected internal virtual Expression VisitTake(TakeExpression node)
        {
            return node.Update(
                Visit(node.Source),
                node.Count);
        }

        protected internal virtual Expression VisitWhere(WhereExpression node)
        {
            return node.Update(
                Visit(node.Source),
                Visit(node.Predicate));
        }
    }
}
