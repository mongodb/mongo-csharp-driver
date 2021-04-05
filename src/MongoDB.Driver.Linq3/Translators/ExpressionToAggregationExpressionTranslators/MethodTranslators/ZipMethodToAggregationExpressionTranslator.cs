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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq3.Ast;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Serializers;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public static class ZipMethodToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.Is(EnumerableMethod.Zip))
            {
                var firstExpression = arguments[0];
                var secondExpression = arguments[1];
                var resultSelectorExpression = (LambdaExpression)arguments[2];

                var firstTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, firstExpression);
                var secondTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, secondExpression);
                var resultSelectorParameters = resultSelectorExpression.Parameters;
                var resultSelectorParameter1 = resultSelectorParameters[0];
                var resultSelectorParameter2 = resultSelectorParameters[1];
                var resultSelectorSymbol1 = new Symbol("$" + resultSelectorParameter1.Name, BsonSerializer.LookupSerializer(resultSelectorParameter1.Type));
                var resultSelectorSymbol2 = new Symbol("$" + resultSelectorParameter2.Name, BsonSerializer.LookupSerializer(resultSelectorParameter2.Type));
                var resultSelectorContext = context.WithSymbols((resultSelectorParameter1, resultSelectorSymbol1), (resultSelectorParameter2, resultSelectorSymbol2));
                var resultSelectorTranslation = ExpressionToAggregationExpressionTranslator.Translate(resultSelectorContext, resultSelectorExpression.Body);

                var ast = AstExpression.Map(
                    input: AstExpression.Zip(new[] { firstTranslation.Ast, secondTranslation.Ast }),
                    @as: "z__",
                    @in: AstExpression.Let(
                        vars: new[]
                        {
                            new AstComputedField(resultSelectorParameter1.Name, AstExpression.ArrayElemAt(AstExpression.Field("$z__"), 0)),
                            new AstComputedField(resultSelectorParameter2.Name, AstExpression.ArrayElemAt(AstExpression.Field("$z__"), 1))
                        },
                        @in: resultSelectorTranslation.Ast));
                var serializer = IEnumerableSerializer.Create(resultSelectorTranslation.Serializer);

                return new AggregationExpression(expression, ast, serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
