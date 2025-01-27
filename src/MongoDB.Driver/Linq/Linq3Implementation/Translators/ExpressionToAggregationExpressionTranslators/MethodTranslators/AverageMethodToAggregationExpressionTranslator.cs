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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class AverageMethodToAggregationExpressionTranslator
    {
        private static readonly MethodInfo[] __averageMethods =
        {
            EnumerableMethod.AverageDecimal,
            EnumerableMethod.AverageDecimalWithSelector,
            EnumerableMethod.AverageDouble,
            EnumerableMethod.AverageDoubleWithSelector,
            EnumerableMethod.AverageInt32,
            EnumerableMethod.AverageInt32WithSelector,
            EnumerableMethod.AverageInt64,
            EnumerableMethod.AverageInt64WithSelector,
            EnumerableMethod.AverageNullableDecimal,
            EnumerableMethod.AverageNullableDecimalWithSelector,
            EnumerableMethod.AverageNullableDouble,
            EnumerableMethod.AverageNullableDoubleWithSelector,
            EnumerableMethod.AverageNullableInt32,
            EnumerableMethod.AverageNullableInt32WithSelector,
            EnumerableMethod.AverageNullableInt64,
            EnumerableMethod.AverageNullableInt64WithSelector,
            EnumerableMethod.AverageNullableSingle,
            EnumerableMethod.AverageNullableSingleWithSelector,
            EnumerableMethod.AverageSingle,
            EnumerableMethod.AverageSingleWithSelector,
            QueryableMethod.AverageDecimal,
            QueryableMethod.AverageDecimalWithSelector,
            QueryableMethod.AverageDouble,
            QueryableMethod.AverageDoubleWithSelector,
            QueryableMethod.AverageInt32,
            QueryableMethod.AverageInt32WithSelector,
            QueryableMethod.AverageInt64,
            QueryableMethod.AverageInt64WithSelector,
            QueryableMethod.AverageNullableDecimal,
            QueryableMethod.AverageNullableDecimalWithSelector,
            QueryableMethod.AverageNullableDouble,
            QueryableMethod.AverageNullableDoubleWithSelector,
            QueryableMethod.AverageNullableInt32,
            QueryableMethod.AverageNullableInt32WithSelector,
            QueryableMethod.AverageNullableInt64,
            QueryableMethod.AverageNullableInt64WithSelector,
            QueryableMethod.AverageNullableSingle,
            QueryableMethod.AverageNullableSingleWithSelector,
            QueryableMethod.AverageSingle,
            QueryableMethod.AverageSingleWithSelector
        };

        private static readonly MethodInfo[] __averageWithSelectorMethods =
        {
            EnumerableMethod.AverageDecimalWithSelector,
            EnumerableMethod.AverageDoubleWithSelector,
            EnumerableMethod.AverageInt32WithSelector,
            EnumerableMethod.AverageInt64WithSelector,
            EnumerableMethod.AverageNullableDecimalWithSelector,
            EnumerableMethod.AverageNullableDoubleWithSelector,
            EnumerableMethod.AverageNullableInt32WithSelector,
            EnumerableMethod.AverageNullableInt64WithSelector,
            EnumerableMethod.AverageNullableSingleWithSelector,
            EnumerableMethod.AverageSingleWithSelector,
            QueryableMethod.AverageDecimalWithSelector,
            QueryableMethod.AverageDoubleWithSelector,
            QueryableMethod.AverageInt32WithSelector,
            QueryableMethod.AverageInt64WithSelector,
            QueryableMethod.AverageNullableDecimalWithSelector,
            QueryableMethod.AverageNullableDoubleWithSelector,
            QueryableMethod.AverageNullableInt32WithSelector,
            QueryableMethod.AverageNullableInt64WithSelector,
            QueryableMethod.AverageNullableSingleWithSelector,
            QueryableMethod.AverageSingleWithSelector
        };

        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__averageMethods))
            {
                var sourceExpression = arguments[0];
                var sourceTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, sourceExpression);
                NestedAsQueryableHelper.EnsureQueryableMethodHasNestedAsQueryableSource(expression, sourceTranslation);

                AstExpression ast;
                if (method.IsOneOf(__averageWithSelectorMethods))
                {
                    var selectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                    var selectorParameter = selectorLambda.Parameters[0];
                    var sourceItemSerializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);
                    var selectorSymbol = context.CreateSymbol(selectorParameter, sourceItemSerializer);
                    var selectorContext = context.WithSymbol(selectorSymbol);
                    var selectorTranslation = ExpressionToAggregationExpressionTranslator.Translate(selectorContext, selectorLambda.Body);

                    ast = AstExpression.Avg(
                        AstExpression.Map(
                            input: sourceTranslation.Ast,
                            @as: selectorSymbol.Var,
                            @in: selectorTranslation.Ast));
                }
                else
                {
                    ast = AstExpression.Avg(sourceTranslation.Ast);
                }
                IBsonSerializer serializer = expression.Type switch
                {
                    Type t when t == typeof(int) => new Int32Serializer(),
                    Type t when t == typeof(long) => new Int64Serializer(),
                    Type t when t == typeof(float) => new SingleSerializer(),
                    Type t when t == typeof(double) => new DoubleSerializer(),
                    Type t when t == typeof(decimal) => new DecimalSerializer(),
                    Type { IsConstructedGenericType: true } t when t.GetGenericTypeDefinition() == typeof(Nullable<>) => (IBsonSerializer)Activator.CreateInstance(typeof(NullableSerializer<>).MakeGenericType(t.GenericTypeArguments[0])),
                    _ => throw new ExpressionNotSupportedException(expression)
                };

                return new TranslatedExpression(expression, ast, serializer);
            }

            if (WindowMethodToAggregationExpressionTranslator.CanTranslate(expression))
            {
                return WindowMethodToAggregationExpressionTranslator.Translate(context, expression);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
