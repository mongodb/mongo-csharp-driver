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
    internal class MedianMethodToAggregationExpressionTranslator
    {
        private static readonly MethodInfo[] __medianMethods =
        {
            EnumerableMethod.MedianDecimal,
            EnumerableMethod.MedianDecimalWithSelector,
            EnumerableMethod.MedianDouble,
            EnumerableMethod.MedianDoubleWithSelector,
            EnumerableMethod.MedianInt32,
            EnumerableMethod.MedianInt32WithSelector,
            EnumerableMethod.MedianInt64,
            EnumerableMethod.MedianInt64WithSelector,
            EnumerableMethod.MedianNullableDecimal,
            EnumerableMethod.MedianNullableDecimalWithSelector,
            EnumerableMethod.MedianNullableDouble,
            EnumerableMethod.MedianNullableDoubleWithSelector,
            EnumerableMethod.MedianNullableInt32,
            EnumerableMethod.MedianNullableInt32WithSelector,
            EnumerableMethod.MedianNullableInt64,
            EnumerableMethod.MedianNullableInt64WithSelector,
            EnumerableMethod.MedianNullableSingle,
            EnumerableMethod.MedianNullableSingleWithSelector,
            EnumerableMethod.MedianSingle,
            EnumerableMethod.MedianSingleWithSelector
        };

        private static readonly MethodInfo[] __medianWithSelectorMethods =
        {
            EnumerableMethod.MedianDecimalWithSelector,
            EnumerableMethod.MedianDoubleWithSelector,
            EnumerableMethod.MedianInt32WithSelector,
            EnumerableMethod.MedianInt64WithSelector,
            EnumerableMethod.MedianNullableDecimalWithSelector,
            EnumerableMethod.MedianNullableDoubleWithSelector,
            EnumerableMethod.MedianNullableInt32WithSelector,
            EnumerableMethod.MedianNullableInt64WithSelector,
            EnumerableMethod.MedianNullableSingleWithSelector,
            EnumerableMethod.MedianSingleWithSelector
        };

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
                    var selectorLambda = (LambdaExpression)arguments[1];
                    var selectorParameter = selectorLambda.Parameters[0];
                    var selectorParameterSerializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);
                    var selectorParameterSymbol = context.CreateSymbol(selectorParameter, selectorParameterSerializer);
                    var selectorContext = context.WithSymbol(selectorParameterSymbol);
                    var selectorTranslation = ExpressionToAggregationExpressionTranslator.Translate(selectorContext, selectorLambda.Body);

                    inputAst = AstExpression.Map(
                        input: sourceTranslation.Ast,
                        @as: selectorParameterSymbol.Var,
                        @in: selectorTranslation.Ast);
                }

                var ast = AstExpression.Median(inputAst);
                var serializer = BsonSerializer.LookupSerializer(expression.Type);
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