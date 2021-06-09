﻿/* Copyright 2010-present MongoDB Inc.
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

using System.Linq.Expressions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class StringGetCharsComparisonExpressionToAggregationExpressionTranslator
    {
        // public static methods
        public static bool CanTranslate(BinaryExpression expression, out MethodCallExpression getCharsExpression)
        {
            if (IsConvertGetCharsExpression(expression.Left, out getCharsExpression))
            {
                return true;
            }

            getCharsExpression = null;
            return false;
        }

        public static AggregationExpression Translate(TranslationContext context, BinaryExpression expression, MethodCallExpression getCharsExpression)
        {
            var method = getCharsExpression.Method;
            var arguments = getCharsExpression.Arguments;

            if (method.Is(StringMethod.GetChars))
            {
                var comparisonOperator = GetComparisonOperator(expression);
                var objectExpression = getCharsExpression.Object;
                var objectTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, objectExpression);
                var indexExpression = arguments[0];
                var indexTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, indexExpression);
                var comparandExpression = expression.Right;
                var c = (char)comparandExpression.GetConstantValue<int>(expression);
                var comparand = new string(c, 1);
                var ast = AstExpression.Comparison(
                    comparisonOperator,
                    AstExpression.SubstrCP(objectTranslation.Ast, indexTranslation.Ast, 1),
                    comparand);
                return new AggregationExpression(expression, ast, new BooleanSerializer());
            }

            throw new ExpressionNotSupportedException(expression);
        }

        // private static methods
        private static bool IsConvertGetCharsExpression(Expression expression, out MethodCallExpression getCharsExpression)
        {
            if (expression is UnaryExpression unaryExpression &&
                unaryExpression.NodeType == ExpressionType.Convert &&
                unaryExpression.Type == typeof(int) &&
                unaryExpression.Operand is MethodCallExpression operandMethodCallExpression &&
                operandMethodCallExpression.Method.Is(StringMethod.GetChars))
            {
                getCharsExpression = operandMethodCallExpression;
                return true;
            }

            getCharsExpression = null;
            return false;
        }

        private static AstBinaryOperator GetComparisonOperator(Expression expression)
        {
            var nodeType = expression.NodeType;
            switch (nodeType)
            {
                case ExpressionType.Equal: return AstBinaryOperator.Eq;
                case ExpressionType.NotEqual: return AstBinaryOperator.Ne;
                case ExpressionType.LessThan: return AstBinaryOperator.Lt;
                case ExpressionType.LessThanOrEqual: return AstBinaryOperator.Lte;
                case ExpressionType.GreaterThan: return AstBinaryOperator.Gt;
                case ExpressionType.GreaterThanOrEqual: return AstBinaryOperator.Gte;
            }

            var message = $"Expression not supported: {nodeType} in {expression}.";
            throw new ExpressionNotSupportedException(message);
        }
    }
}
