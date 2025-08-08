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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal class PercentileMethodToAggregationExpressionTranslator
    {
        private static readonly MethodInfo[] __percentileMethods =
        [
            MongoEnumerableMethod.PercentileDecimal,
            MongoEnumerableMethod.PercentileDecimalWithSelector,
            MongoEnumerableMethod.PercentileDouble,
            MongoEnumerableMethod.PercentileDoubleWithSelector,
            MongoEnumerableMethod.PercentileInt32,
            MongoEnumerableMethod.PercentileInt32WithSelector,
            MongoEnumerableMethod.PercentileInt64,
            MongoEnumerableMethod.PercentileInt64WithSelector,
            MongoEnumerableMethod.PercentileNullableDecimal,
            MongoEnumerableMethod.PercentileNullableDecimalWithSelector,
            MongoEnumerableMethod.PercentileNullableDouble,
            MongoEnumerableMethod.PercentileNullableDoubleWithSelector,
            MongoEnumerableMethod.PercentileNullableInt32,
            MongoEnumerableMethod.PercentileNullableInt32WithSelector,
            MongoEnumerableMethod.PercentileNullableInt64,
            MongoEnumerableMethod.PercentileNullableInt64WithSelector,
            MongoEnumerableMethod.PercentileNullableSingle,
            MongoEnumerableMethod.PercentileNullableSingleWithSelector,
            MongoEnumerableMethod.PercentileSingle,
            MongoEnumerableMethod.PercentileSingleWithSelector
        ];

        private static readonly MethodInfo[] __percentileWithSelectorMethods =
        [
            MongoEnumerableMethod.PercentileDecimalWithSelector,
            MongoEnumerableMethod.PercentileDoubleWithSelector,
            MongoEnumerableMethod.PercentileInt32WithSelector,
            MongoEnumerableMethod.PercentileInt64WithSelector,
            MongoEnumerableMethod.PercentileNullableDecimalWithSelector,
            MongoEnumerableMethod.PercentileNullableDoubleWithSelector,
            MongoEnumerableMethod.PercentileNullableInt32WithSelector,
            MongoEnumerableMethod.PercentileNullableInt64WithSelector,
            MongoEnumerableMethod.PercentileNullableSingleWithSelector,
            MongoEnumerableMethod.PercentileSingleWithSelector
        ];

        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__percentileMethods))
            {
                var sourceExpression = arguments[0];
                var sourceTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, sourceExpression);
                NestedAsQueryableHelper.EnsureQueryableMethodHasNestedAsQueryableSource(expression, sourceTranslation);

                var inputAst = sourceTranslation.Ast;

                if (method.IsOneOf(__percentileWithSelectorMethods))
                {
                    var sourceItemSerializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);

                    var selectorLambda = (LambdaExpression)arguments[1];
                    var selectorParameter = selectorLambda.Parameters[0];
                    var selectorParameterSymbol = context.CreateSymbol(selectorParameter, sourceItemSerializer);
                    var selectorContext = context.WithSymbol(selectorParameterSymbol);
                    var selectorTranslation = ExpressionToAggregationExpressionTranslator.Translate(selectorContext, selectorLambda.Body);

                    inputAst = AstExpression.Map(
                        input: sourceTranslation.Ast,
                        @as: selectorParameterSymbol.Var,
                        @in: selectorTranslation.Ast);
                }

                var percentilesExpression = arguments[arguments.Count - 1];
                var percentilesTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, percentilesExpression);

                var ast = AstExpression.Percentile(inputAst, percentilesTranslation.Ast);
                var serializer = StandardSerializers.GetSerializer(expression.Type);
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