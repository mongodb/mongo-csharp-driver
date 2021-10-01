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
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class SumMethodToAggregationExpressionTranslator
    {
        private static readonly MethodInfo[] __sumMethods =
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

                AstExpression ast;
                IBsonSerializer serializer;
                if (arguments.Count == 1)
                {
                    ast = AstExpression.Sum(sourceTranslation.Ast);
                    serializer = sourceItemSerializer;
                }
                else
                {
                    var selectorLambda = (LambdaExpression)arguments[1];
                    var selectorParameter = selectorLambda.Parameters[0];
                    var selectorSymbol = context.CreateSymbol(selectorParameter, sourceItemSerializer);
                    var selectorContext = context.WithSymbol(selectorSymbol);
                    var selectorTranslation = ExpressionToAggregationExpressionTranslator.Translate(selectorContext, selectorLambda.Body);
                    ast = AstExpression.Sum(
                        AstExpression.Map(
                            input: sourceTranslation.Ast,
                            @as: selectorSymbol.Var,
                            @in: selectorTranslation.Ast));
                    serializer = selectorTranslation.Serializer;
                }

                return new AggregationExpression(expression, ast, serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
