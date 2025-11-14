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

using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class FirstOrLastMethodToAggregationExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(EnumerableOrQueryableMethod.FirstOrLastOverloads))
            {
                var sourceExpression = arguments[0];
                var sourceTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, sourceExpression);
                NestedAsQueryableHelper.EnsureQueryableMethodHasNestedAsQueryableSource(expression, sourceTranslation);

                var sourceAst = sourceTranslation.Ast;
                var itemSerializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);

                var isFirstMethod = method.IsOneOf(EnumerableOrQueryableMethod.FirstOverloads);

                if (method.IsOneOf(EnumerableOrQueryableMethod.FirstOrLastWithPredicateOverloads))
                {
                    var predicateLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                    var parameterExpression = predicateLambda.Parameters.Single();
                    var parameterSymbol = context.CreateSymbol(parameterExpression, itemSerializer, isCurrent: false);
                    var predicateTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, predicateLambda, parameterSymbol);

                    AstExpression limit = null;
                    if (isFirstMethod)
                    {
                        var compatibilityLevel = context.TranslationOptions.CompatibilityLevel;
                        if (Feature.FilterLimit.IsSupported(compatibilityLevel.ToWireVersion()))
                        {
                            limit = AstExpression.Constant(1);
                        }
                    }

                    sourceAst = AstExpression.Filter(
                        input: sourceAst,
                        cond: predicateTranslation.Ast,
                        @as: parameterSymbol.Var.Name,
                        limit: limit);
                }

                AstExpression ast;
                if (method.IsOneOf(EnumerableOrQueryableMethod.FirstOrDefaultOverloads, EnumerableOrQueryableMethod.LastOrDefaultOverloads))
                {
                    var defaultValue = itemSerializer.ValueType.GetDefaultValue();
                    var serializedDefaultValue = SerializationHelper.SerializeValue(itemSerializer, defaultValue);

                    var (valuesVarBinding, valuesAst) = AstExpression.UseVarIfNotSimple("values", sourceAst);
                    ast = AstExpression.Let(
                        var: valuesVarBinding,
                        @in: AstExpression.Cond(
                            @if: AstExpression.Eq(AstExpression.Size(valuesAst), 0),
                            then: serializedDefaultValue,
                            @else: isFirstMethod ? AstExpression.First(valuesAst) : AstExpression.Last(valuesAst)));
                }
                else
                {
                    ast = isFirstMethod ? AstExpression.First(sourceAst) : AstExpression.Last(sourceAst);
                }

                return new TranslatedExpression(expression, ast, itemSerializer);
            }

            if (WindowMethodToAggregationExpressionTranslator.CanTranslate(expression))
            {
                return WindowMethodToAggregationExpressionTranslator.Translate(context, expression);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
