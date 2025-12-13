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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class CountMethodToAggregationExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(EnumerableOrQueryableMethod.CountOverloads))
            {
                var sourceExpression = arguments[0];
                var sourceTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, sourceExpression);
                NestedAsQueryableHelper.EnsureQueryableMethodHasNestedAsQueryableSource(expression, sourceTranslation);

                AstExpression ast;
                if (method.IsOneOf(EnumerableOrQueryableMethod.CountWithPredicateOverloads))
                {
                    if (sourceExpression.Type == typeof(string))
                    {
                        throw new ExpressionNotSupportedException(expression);
                    }

                    var predicateLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                    var predicateParameter = predicateLambda.Parameters[0];
                    var predicateParameterSerializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);
                    var predicateSymbol = context.CreateSymbol(predicateParameter, predicateParameterSerializer);
                    var predicateContext = context.WithSymbol(predicateSymbol);
                    var predicateTranslation = ExpressionToAggregationExpressionTranslator.Translate(predicateContext, predicateLambda.Body);

                    ast = AstExpression.Sum(
                        AstExpression.Map(
                            input: sourceTranslation.Ast,
                            @as: predicateSymbol.Var,
                            @in: AstExpression.Cond(predicateTranslation.Ast, 1, 0)));
                }
                else
                {
                    if (sourceExpression.Type == typeof(string))
                    {
                        ast = AstExpression.StrLenCP(sourceTranslation.Ast);
                    }
                    else
                    {
                        ast = AstExpression.Size(sourceTranslation.Ast);
                    }
                }
                var serializer = GetSerializer(expression.Type);

                return new TranslatedExpression(expression, ast, serializer);
            }

            if (WindowMethodToAggregationExpressionTranslator.CanTranslate(expression))
            {
                return WindowMethodToAggregationExpressionTranslator.Translate(context, expression);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static IBsonSerializer GetSerializer(Type type)
        {
            if (type == typeof(int))
            {
                return new Int32Serializer();
            }
            else
            {
                return new Int64Serializer();
            }
        }
    }
}
