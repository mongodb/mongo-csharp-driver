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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class ContainsMethodToAggregationExpressionTranslator
    {
        // public methods
        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (StartsWithContainsOrEndsWithMethodToAggregationExpressionTranslator.CanTranslate(expression))
            {
                return StartsWithContainsOrEndsWithMethodToAggregationExpressionTranslator.Translate(context, expression);
            }

            if (IsEnumerableContainsMethod(expression, out var sourceExpression, out var valueExpression))
            {
                return TranslateEnumerableContains(context, expression, sourceExpression, valueExpression);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        // private methods
        private static bool IsEnumerableContainsMethod(MethodCallExpression expression, out Expression sourceExpression, out Expression valueExpression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(EnumerableMethod.Contains, QueryableMethod.Contains))
            {
                sourceExpression = arguments[0];
                valueExpression = arguments[1];
                return true;
            }

            if (!method.IsStatic && method.ReturnType == typeof(bool) && method.Name == "Contains" && arguments.Count == 1)
            {
                sourceExpression = expression.Object;
                valueExpression = arguments[0];

                if (sourceExpression.Type.TryGetIEnumerableGenericInterface(out var ienumerableInterface))
                {
                    var itemType = ienumerableInterface.GetGenericArguments()[0];
                    if (itemType == valueExpression.Type)
                    {
                        // string.Contains(char) is not translated like other Contains methods because string is not represented as an array
                        return sourceExpression.Type != typeof(string) && valueExpression.Type != typeof(char);
                    }
                }
            }

            sourceExpression = null;
            valueExpression = null;
            return false;
        }

        private static AggregationExpression TranslateEnumerableContains(TranslationContext context, MethodCallExpression expression, Expression sourceExpression, Expression valueExpression)
        {
            var sourceTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, sourceExpression);
            NestedAsQueryableHelper.EnsureQueryableMethodHasNestedAsQueryableSource(expression, sourceTranslation);
            var valueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, valueExpression);
            var ast = AstExpression.In(valueTranslation.Ast, sourceTranslation.Ast);
            return new AggregationExpression(expression, ast, BooleanSerializer.Instance);
        }
    }
}
