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
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public static class SumMethodToAggregationExpressionTranslator
    {
        private static MethodInfo[] __sumMethods =
        {
            EnumerableMethod.SumDecimal,
            EnumerableMethod.SumDecimalWithSelector,
            EnumerableMethod.SumDouble,
            EnumerableMethod.SumDoubleWithSelector,
            EnumerableMethod.SumInt32,
            EnumerableMethod.SumInt32WithSelector,
            EnumerableMethod.SumInt64,
            EnumerableMethod.SumInt64WithSelector,
            EnumerableMethod.SumNullableDecimal,
            EnumerableMethod.SumNullableDecimalWithSelector,
            EnumerableMethod.SumNullableDouble,
            EnumerableMethod.SumNullableDoubleWithSelector,
            EnumerableMethod.SumNullableInt32,
            EnumerableMethod.SumNullableInt32WithSelector,
            EnumerableMethod.SumNullableInt64,
            EnumerableMethod.SumNullableInt64WithSelector,
            EnumerableMethod.SumNullableSingle,
            EnumerableMethod.SumNullableSingleWithSelector,
            EnumerableMethod.SumSingle,
            EnumerableMethod.SumSingleWithSelector
        };

        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__sumMethods))
            {
                var sourceExpression = arguments[0];
                var sourceTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, sourceExpression);
                var sourceItemSerializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);
                var selectorLambda = arguments.Count == 2 ? (LambdaExpression)arguments[1] : null;
                AstExpression ast;
                IBsonSerializer serializer;
                if (selectorLambda == null)
                {
                    ast = AstExpression.Sum(sourceTranslation.Ast);
                    serializer = sourceItemSerializer;
                }
                else
                {
                    var selectorParameter = selectorLambda.Parameters[0];
                    var selectorSymbol = new Symbol("$" + selectorParameter.Name, sourceItemSerializer);
                    var selectorContext = context.WithSymbol(selectorParameter, selectorSymbol);
                    var selectorTranslation = ExpressionToAggregationExpressionTranslator.Translate(selectorContext, selectorLambda.Body);
                    ast = AstExpression.Sum(
                        AstExpression.Map(
                            input: sourceTranslation.Ast,
                            @as: selectorParameter.Name,
                            @in: selectorTranslation.Ast));
                    serializer = selectorTranslation.Serializer;
                }
                return new AggregationExpression(expression, ast, serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
