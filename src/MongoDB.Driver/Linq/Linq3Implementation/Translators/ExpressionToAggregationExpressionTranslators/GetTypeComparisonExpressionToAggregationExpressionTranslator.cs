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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class GetTypeComparisonExpressionToAggregationExpressionTranslator
    {
        // public static methods
        public static bool CanTranslate(BinaryExpression expression)
        {
            return CanTranslate(expression, out _, out _);
        }

        public static AggregationExpression Translate(TranslationContext context, BinaryExpression expression)
        {
            if (CanTranslate(expression, out var getTypeMethodCallExpression, out var comparandType))
            {
                var objectExpression = getTypeMethodCallExpression.Object;
                var objectTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, objectExpression);
                var nominalType = objectExpression.Type;
                var actualType = comparandType;

                var discriminatorConvention = objectTranslation.Serializer.GetDiscriminatorConvention();
                var discriminatorField = AstExpression.GetField(objectTranslation.Ast, discriminatorConvention.ElementName);
                var ast = DiscriminatorAstExpression.TypeEquals(discriminatorField, discriminatorConvention, nominalType, actualType);

                return new AggregationExpression(expression, ast, BooleanSerializer.Instance);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        // private static methods
        private static bool CanTranslate(BinaryExpression expression, out MethodCallExpression getTypeMethodCallExpression, out Type comparandType)
        {
            var leftExpression = expression.Left;
            var rightExpression = expression.Right;

            if (leftExpression is MethodCallExpression methodCallExpression &&
                methodCallExpression.Method.Is(ObjectMethod.GetType) &&
                expression.NodeType == ExpressionType.Equal &&
                rightExpression is ConstantExpression constantExpression)
            {
                getTypeMethodCallExpression = methodCallExpression;
                comparandType = (Type)constantExpression.Value;
                return true;
            }

            getTypeMethodCallExpression = null;
            comparandType = null;
            return false;
        }

    }
}
