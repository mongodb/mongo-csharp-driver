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
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class CompareToMethodToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (IComparableMethod.IsCompareToMethod(method))
            {
                var objectExpression = expression.Object;
                var otherExpression = arguments[0];

                AggregationExpression objectTranslation;
                AggregationExpression otherTranslation;
                if (objectExpression is ConstantExpression && otherExpression is not ConstantExpression)
                {
                    otherTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, otherExpression);
                    objectTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, objectExpression, otherTranslation.Serializer);
                }
                else
                {
                    objectTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, objectExpression);
                    otherTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, otherExpression, objectTranslation.Serializer);
                }
                var ast = AstExpression.Cmp(objectTranslation.Ast, otherTranslation.Ast);

                return new AggregationExpression(expression, ast, Int32Serializer.Instance);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
