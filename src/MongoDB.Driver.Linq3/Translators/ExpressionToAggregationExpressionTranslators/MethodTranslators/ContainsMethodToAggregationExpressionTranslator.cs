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

using System.Linq.Expressions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Reflection;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public static class ContainsMethodToAggregationExpressionTranslator
    {
        // public methods
        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (IsEnumerableContainsMethod(expression, out var sourceExpression, out var valueExpression))
            {
                return TranslateEnumerableContains(context, expression, sourceExpression, valueExpression);
            }

            if (expression.Method.Is(StringMethod.Contains))
            {
                return StartsWithContainsOrEndsWithMethodToAggregationExpressionTranslator.Translate(context, expression);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        // private methods
        private static bool IsEnumerableContainsMethod(MethodCallExpression expression, out Expression sourceExpression, out Expression valueExpression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.Is(EnumerableMethod.Contains))
            {
                sourceExpression = arguments[0];
                valueExpression = arguments[1];
                return true;
            }

            if (!method.IsStatic && method.ReturnType == typeof(bool) && arguments.Count == 1)
            {
                sourceExpression = expression.Object;
                valueExpression = arguments[0];
                var ienumerableInterface = sourceExpression.Type.GetIEnumerableGenericInterface();
                var itemType = ienumerableInterface.GetGenericArguments()[0];
                return itemType == valueExpression.Type;
            }

            sourceExpression = null;
            valueExpression = null;
            return false;
        }

        private static AggregationExpression TranslateEnumerableContains(TranslationContext context, Expression expression, Expression sourceExpression, Expression valueExpression)
        {
            var sourceTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, sourceExpression);
            var valueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, valueExpression);
            var ast = AstExpression.In(valueTranslation.Ast, sourceTranslation.Ast);

            return new AggregationExpression(expression, ast, new BooleanSerializer());
        }
    }
}
