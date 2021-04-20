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
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Reflection;
using MongoDB.Driver.Linq3.Serializers;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public static class UnionMethodToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.Is(EnumerableMethod.Union))
            {
                var firstExpression = arguments[0];
                var firstTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, firstExpression);
                var secondExpression = arguments[1];
                var secondTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, secondExpression);
                var ast = AstExpression.SetUnion(firstTranslation.Ast, secondTranslation.Ast);
                var itemSerializer = ArraySerializerHelper.GetItemSerializer(firstTranslation.Serializer);
                var serializer = IEnumerableSerializer.Create(itemSerializer);
                return new AggregationExpression(expression, ast, serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
