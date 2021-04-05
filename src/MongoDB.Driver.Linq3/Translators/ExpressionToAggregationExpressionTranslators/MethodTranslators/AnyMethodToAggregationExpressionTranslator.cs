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
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public static class AnyMethodToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var sourceExpression = expression.Arguments[0];

            var sourceTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, sourceExpression);
            if (expression.Method.Is(EnumerableMethod.Any))
            {
                var ast = AstExpression.Gt(AstExpression.Size(sourceTranslation.Ast), 0);

                return new AggregationExpression(expression, ast, new BooleanSerializer());
            }

            if (expression.Method.Is(EnumerableMethod.AnyWithPredicate))
            {
                var predicateExpression = (LambdaExpression)expression.Arguments[1];
                var predicateParameter = predicateExpression.Parameters[0];
                var predicateParameterSerializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);
                var predicateContext = context.WithSymbol(predicateParameter, new Symbol("$" + predicateParameter.Name, predicateParameterSerializer));
                var predicateTranslation = ExpressionToAggregationExpressionTranslator.Translate(predicateContext, predicateExpression.Body);

                var ast = AstExpression.AnyElementTrue(
                    AstExpression.Map(
                        input: sourceTranslation.Ast,
                        @as: predicateParameter.Name,
                        @in: predicateTranslation.Ast));

                return new AggregationExpression(expression, ast, new BooleanSerializer());
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
