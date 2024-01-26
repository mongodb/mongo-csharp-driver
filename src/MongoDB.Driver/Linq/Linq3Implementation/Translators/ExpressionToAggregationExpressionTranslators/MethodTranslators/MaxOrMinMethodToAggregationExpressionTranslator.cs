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
    internal static class MaxOrMinMethodToAggregationExpressionTranslator
    {
        private static readonly MethodInfo[] __maxOrMinMethods =
        {
            EnumerableMethod.Max,
            EnumerableMethod.MaxDecimal,
            EnumerableMethod.MaxDecimalWithSelector,
            EnumerableMethod.MaxDouble,
            EnumerableMethod.MaxDoubleWithSelector,
            EnumerableMethod.MaxInt32,
            EnumerableMethod.MaxInt32WithSelector,
            EnumerableMethod.MaxInt64,
            EnumerableMethod.MaxInt64WithSelector,
            EnumerableMethod.MaxNullableDecimal,
            EnumerableMethod.MaxNullableDecimalWithSelector,
            EnumerableMethod.MaxNullableDouble,
            EnumerableMethod.MaxNullableDoubleWithSelector,
            EnumerableMethod.MaxNullableInt32,
            EnumerableMethod.MaxNullableInt32WithSelector,
            EnumerableMethod.MaxNullableInt64,
            EnumerableMethod.MaxNullableInt64WithSelector,
            EnumerableMethod.MaxNullableSingle,
            EnumerableMethod.MaxNullableSingleWithSelector,
            EnumerableMethod.MaxSingle,
            EnumerableMethod.MaxSingleWithSelector,
            EnumerableMethod.MaxWithSelector,
            EnumerableMethod.Min,
            EnumerableMethod.MinDecimal,
            EnumerableMethod.MinDecimalWithSelector,
            EnumerableMethod.MinDouble,
            EnumerableMethod.MinDoubleWithSelector,
            EnumerableMethod.MinInt32,
            EnumerableMethod.MinInt32WithSelector,
            EnumerableMethod.MinInt64,
            EnumerableMethod.MinInt64WithSelector,
            EnumerableMethod.MinNullableDecimal,
            EnumerableMethod.MinNullableDecimalWithSelector,
            EnumerableMethod.MinNullableDouble,
            EnumerableMethod.MinNullableDoubleWithSelector,
            EnumerableMethod.MinNullableInt32,
            EnumerableMethod.MinNullableInt32WithSelector,
            EnumerableMethod.MinNullableInt64,
            EnumerableMethod.MinNullableInt64WithSelector,
            EnumerableMethod.MinNullableSingle,
            EnumerableMethod.MinNullableSingleWithSelector,
            EnumerableMethod.MinSingle,
            EnumerableMethod.MinSingleWithSelector,
            EnumerableMethod.MinWithSelector,
            QueryableMethod.Max,
            QueryableMethod.MaxWithSelector,
            QueryableMethod.Min,
            QueryableMethod.MinWithSelector
        };

        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__maxOrMinMethods))
            {
                var sourceExpression = arguments[0];
                var sourceTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, sourceExpression);
                NestedAsQueryableHelper.EnsureQueryableMethodHasNestedAsQueryableSource(expression, sourceTranslation);

                AstExpression ast;
                IBsonSerializer serializer;
                if (arguments.Count == 1)
                {
                    var array = sourceTranslation.Ast;
                    ast = method.Name == "Max" ? AstExpression.Max(array) : AstExpression.Min(array);
                    serializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);
                }
                else
                {
                    var selectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                    var selectorParameter = selectorLambda.Parameters[0];
                    var selectorParameterSerializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);
                    var selectorParameterSymbol = context.CreateSymbol(selectorParameter, selectorParameterSerializer);
                    var selectorContext = context.WithSymbol(selectorParameterSymbol);
                    var selectorTranslation = ExpressionToAggregationExpressionTranslator.Translate(selectorContext, selectorLambda.Body);
                    var mappedArray =
                        AstExpression.Map(
                            input: sourceTranslation.Ast,
                            @as: selectorParameterSymbol.Var,
                            @in: selectorTranslation.Ast);
                    ast = method.Name == "Max" ? AstExpression.Max(mappedArray) : AstExpression.Min(mappedArray);
                    serializer = selectorTranslation.Serializer;
                }

                return new AggregationExpression(expression, ast, serializer);
            }

            if (WindowMethodToAggregationExpressionTranslator.CanTranslate(expression))
            {
                return WindowMethodToAggregationExpressionTranslator.Translate(context, expression);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
