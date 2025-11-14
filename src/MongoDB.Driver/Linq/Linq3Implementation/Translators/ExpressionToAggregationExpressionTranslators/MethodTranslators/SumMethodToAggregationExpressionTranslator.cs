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
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(EnumerableOrQueryableMethod.SumOverloads))
            {
                var sourceExpression = arguments[0];
                var sourceTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, sourceExpression);
                NestedAsQueryableHelper.EnsureQueryableMethodHasNestedAsQueryableSource(expression, sourceTranslation);
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
                    var selectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
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
