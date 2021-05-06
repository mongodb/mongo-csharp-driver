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

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class IsSubsetOfMethodToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (IsIsSubsetOfMethod(expression, out var sourceExpression, out var otherExpression))
            {
                var sourceTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, sourceExpression);
                var otherTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, otherExpression);
                var ast = AstExpression.SetIsSubset(sourceTranslation.Ast, otherTranslation.Ast);
                return new AggregationExpression(expression, ast, new BooleanSerializer());
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static bool IsIsSubsetOfMethod(MethodCallExpression expression, out Expression sourceExpression, out Expression otherExpression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (!method.IsStatic && method.ReturnType == typeof(bool) && method.Name == "IsSubsetOf" && arguments.Count == 1)
            {
                sourceExpression = expression.Object;
                otherExpression = arguments[0];

                if (sourceExpression.Type.TryGetIEnumerableGenericInterface(out var sourceIEnumerableInterface) &&
                    otherExpression.Type.TryGetIEnumerableGenericInterface(out var otherIEnumerableInterface))
                {
                    var sourceItemType = sourceIEnumerableInterface.GetGenericArguments()[0];
                    var otherItemType = otherIEnumerableInterface.GetGenericArguments()[0];
                    if (sourceItemType == otherItemType)
                    {
                        return true;
                    }
                }
            }

            sourceExpression = null;
            otherExpression = null;
            return false;
        }
    }
}
