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

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal class PercentileMethodToAggregationExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (IsPercentileMethod(method))
            {
                if (arguments.Count is 2 or 3)
                {
                    var sourceExpression = arguments[0];
                    var sourceTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, sourceExpression);
                    NestedAsQueryableHelper.EnsureQueryableMethodHasNestedAsQueryableSource(expression, sourceTranslation);

                    AstExpression inputAst = sourceTranslation.Ast;

                    // handle selector
                    if (arguments.Count == 3)
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

                    var percentilesExpression = arguments[arguments.Count - 1];
                    var percentilesTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, percentilesExpression);

                    var ast = AstExpression.Percentile(inputAst, percentilesTranslation.Ast);
                    var serializer = BsonSerializer.LookupSerializer(expression.Type);
                    return new TranslatedExpression(expression, ast, serializer);
                }
            }

            if (WindowMethodToAggregationExpressionTranslator.CanTranslate(expression))
            {
                return WindowMethodToAggregationExpressionTranslator.Translate(context, expression);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static bool IsPercentileMethod(MethodInfo methodInfo)
        {
            return methodInfo.DeclaringType == typeof(MongoEnumerable) && methodInfo.Name == "Percentile";
        }
    }
}