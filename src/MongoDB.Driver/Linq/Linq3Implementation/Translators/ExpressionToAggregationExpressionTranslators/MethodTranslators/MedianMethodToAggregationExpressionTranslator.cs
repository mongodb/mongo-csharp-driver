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
    internal class MedianMethodToAggregationExpressionTranslator
    {
        private static readonly MethodInfo[] __medianMethods =
        [
            MongoEnumerableMethod.MedianDecimal,
            MongoEnumerableMethod.MedianDecimalWithSelector,
            MongoEnumerableMethod.MedianDouble,
            MongoEnumerableMethod.MedianDoubleWithSelector,
            MongoEnumerableMethod.MedianInt32,
            MongoEnumerableMethod.MedianInt32WithSelector,
            MongoEnumerableMethod.MedianInt64,
            MongoEnumerableMethod.MedianInt64WithSelector,
            MongoEnumerableMethod.MedianNullableDecimal,
            MongoEnumerableMethod.MedianNullableDecimalWithSelector,
            MongoEnumerableMethod.MedianNullableDouble,
            MongoEnumerableMethod.MedianNullableDoubleWithSelector,
            MongoEnumerableMethod.MedianNullableInt32,
            MongoEnumerableMethod.MedianNullableInt32WithSelector,
            MongoEnumerableMethod.MedianNullableInt64,
            MongoEnumerableMethod.MedianNullableInt64WithSelector,
            MongoEnumerableMethod.MedianNullableSingle,
            MongoEnumerableMethod.MedianNullableSingleWithSelector,
            MongoEnumerableMethod.MedianSingle,
            MongoEnumerableMethod.MedianSingleWithSelector
        ];

        private static readonly MethodInfo[] __medianWithSelectorMethods =
        [
            MongoEnumerableMethod.MedianDecimalWithSelector,
            MongoEnumerableMethod.MedianDoubleWithSelector,
            MongoEnumerableMethod.MedianInt32WithSelector,
            MongoEnumerableMethod.MedianInt64WithSelector,
            MongoEnumerableMethod.MedianNullableDecimalWithSelector,
            MongoEnumerableMethod.MedianNullableDoubleWithSelector,
            MongoEnumerableMethod.MedianNullableInt32WithSelector,
            MongoEnumerableMethod.MedianNullableInt64WithSelector,
            MongoEnumerableMethod.MedianNullableSingleWithSelector,
            MongoEnumerableMethod.MedianSingleWithSelector
        ];

        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__medianMethods))
            {
                var sourceExpression = arguments[0];
                var sourceTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, sourceExpression);
                NestedAsQueryableHelper.EnsureQueryableMethodHasNestedAsQueryableSource(expression, sourceTranslation);

                var inputAst = sourceTranslation.Ast;

                if (method.IsOneOf(__medianWithSelectorMethods))
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

                var ast = AstExpression.Median(inputAst);
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