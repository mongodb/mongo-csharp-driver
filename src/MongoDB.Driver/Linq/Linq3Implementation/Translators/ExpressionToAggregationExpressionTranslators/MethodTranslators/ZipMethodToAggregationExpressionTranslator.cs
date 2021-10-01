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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class ZipMethodToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.Is(EnumerableMethod.Zip))
            {
                var firstExpression = arguments[0];
                var firstTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, firstExpression);
                var secondExpression = arguments[1];
                var secondTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, secondExpression);
                var resultSelectorLambda = (LambdaExpression)arguments[2];
                var resultSelectorParameters = resultSelectorLambda.Parameters;
                var resultSelectorParameter1 = resultSelectorParameters[0];
                var resultSelectorParameter2 = resultSelectorParameters[1];
                var resultSelectorSymbol1 = context.CreateSymbol(resultSelectorParameter1, BsonSerializer.LookupSerializer(resultSelectorParameter1.Type));
                var resultSelectorSymbol2 = context.CreateSymbol(resultSelectorParameter2, BsonSerializer.LookupSerializer(resultSelectorParameter2.Type));
                var resultSelectorContext = context.WithSymbols(resultSelectorSymbol1, resultSelectorSymbol2);
                var resultSelectorTranslation = ExpressionToAggregationExpressionTranslator.Translate(resultSelectorContext, resultSelectorLambda.Body);
                var @as = AstExpression.Var("z__");
                var ast = AstExpression.Map(
                    input: AstExpression.Zip(new[] { firstTranslation.Ast, secondTranslation.Ast }),
                    @as: @as,
                    @in: AstExpression.Let(
                        AstExpression.VarBinding(resultSelectorSymbol1.Var, AstExpression.ArrayElemAt(@as, 0)),
                        AstExpression.VarBinding(resultSelectorSymbol2.Var, AstExpression.ArrayElemAt(@as, 1)),
                        @in: resultSelectorTranslation.Ast));
                var serializer = IEnumerableSerializer.Create(resultSelectorTranslation.Serializer);
                return new AggregationExpression(expression, ast, serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
