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

using System.Linq;
using System.Linq.Expressions;

namespace MongoDB.Driver.Linq.Expressions
{
    internal class MongoExpressionVisitor : System.Linq.Expressions.ExpressionVisitor
    {
        public static LambdaExpression GetLambda(Expression node)
        {
            return (LambdaExpression)StripQuotes(node);
        }

        public static bool IsLinqMethod(MethodCallExpression node, params string[] names)
        {
            if(node == null)
            {
                return false;
            }

            if (node.Method.DeclaringType != typeof(Enumerable) && node.Method.DeclaringType != typeof(Queryable))
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

        protected override Expression VisitExtension(Expression node)
        {
            var mongoNode = node as MongoExpression;
            if (mongoNode != null)
            {
                switch (mongoNode.MongoNodeType)
                {
                    case MongoExpressionType.Aggregation:
                        return VisitAggregation((AggregationExpression)node);
                    case MongoExpressionType.Serialization:
                        return VisitSerialization((SerializationExpression)node);
                }
            }

            return base.VisitExtension(node);
        }

        private Expression VisitAggregation(AggregationExpression node)
        {
            return node;
        }

        protected virtual Expression VisitSerialization(SerializationExpression node)
        {
            return node;
        }
    }
}
