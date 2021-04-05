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
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Serializers;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public static class ExceptMethodToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (expression.Method.Is(EnumerableMethod.Except))
            {
                var firstExpression = expression.Arguments[0];
                var secondExpression = expression.Arguments[1];

                var firstTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, firstExpression);
                var secondTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, secondExpression);
                var ast = AstExpression.SetDifference(firstTranslation.Ast, secondTranslation.Ast);
                var itemSerializer = ArraySerializerHelper.GetItemSerializer(firstTranslation.Serializer);
                var serializer = IEnumerableSerializer.Create(itemSerializer);

                return new AggregationExpression(expression, ast, serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
