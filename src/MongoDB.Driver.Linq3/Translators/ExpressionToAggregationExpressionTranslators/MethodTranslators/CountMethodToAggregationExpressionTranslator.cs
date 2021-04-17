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
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Reflection;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public static class CountMethodToAggregationExpressionTranslator
    {
        private static MethodInfo[] __countMethods;
        private static MethodInfo[] __countWithPredicateMethods;

        static CountMethodToAggregationExpressionTranslator()
        {
            __countMethods = new[]
            {
                EnumerableMethod.Count,
                EnumerableMethod.CountWithPredicate,
                EnumerableMethod.LongCount,
                EnumerableMethod.LongCountWithPredicate
            };

            __countWithPredicateMethods = new[]
            {
                EnumerableMethod.CountWithPredicate,
                EnumerableMethod.LongCountWithPredicate
            };
        }

        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__countMethods))
            {
                var sourceExpression = arguments[0];
                var sourceTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, sourceExpression);

                AstExpression ast;
                if (method.IsOneOf(__countWithPredicateMethods))
                {
                    if (sourceExpression.Type == typeof(string))
                    {
                        throw new ExpressionNotSupportedException(expression);
                    }

                    var predicateLambda = (LambdaExpression)arguments[1];
                    var sourceItemSerializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);
                    var predicateTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, predicateLambda, sourceItemSerializer);
                    var filteredSourceAst = AstExpression.Filter(
                        input: sourceTranslation.Ast,
                        cond: predicateTranslation.Ast,
                        @as: predicateLambda.Parameters[0].Name);
                    ast = AstExpression.Size(filteredSourceAst);
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

                return new AggregationExpression(expression, ast, serializer);
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
