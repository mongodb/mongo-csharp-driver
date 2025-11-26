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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class SequenceEqualMethodToAggregationExpressionTranslator
    {
        private static readonly MethodInfo[] __sequenceEqualMethods =
        [
            EnumerableMethod.SequenceEqual,
            QueryableMethod.SequenceEqual
        ];

        private static readonly MethodInfo[] __sequenceEqualWithComparerMethods =
        [
            EnumerableMethod.SequenceEqualWithComparer,
            QueryableMethod.SequenceEqualWithComparer
        ];

        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__sequenceEqualMethods, __sequenceEqualWithComparerMethods))
            {
                var firstExpression = arguments[0];
                var secondExpression = arguments[1];
                var comparerExpression = method.IsOneOf(__sequenceEqualWithComparerMethods) ? arguments[2] : null;

                var firstTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, firstExpression);
                var secondTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, secondExpression);
                if (comparerExpression != null && comparerExpression is not ConstantExpression { Value : null })
                {
                    throw new ExpressionNotSupportedException(expression, because: "comparer value must be null");
                }

                var (firstVarBinding, firstAst) = AstExpression.UseVarIfNotSimple("first", firstTranslation.Ast);
                var (secondVarBinding, secondAst) = AstExpression.UseVarIfNotSimple("second", secondTranslation.Ast);
                var pairVar = AstExpression.Var("pair");

                var ast = AstExpression.Let(
                    firstVarBinding,
                    secondVarBinding,
                    @in : AstExpression.And(
                        AstExpression.Eq(AstExpression.Type(firstAst), "array"),
                        AstExpression.Eq(AstExpression.Type(secondAst), "array"),
                        AstExpression.Eq(AstExpression.Size(firstAst), AstExpression.Size(secondAst)),
                        AstExpression.AllElementsTrue(
                            AstExpression.Map(
                                input: AstExpression.Zip([firstAst, secondAst]),
                                @as: pairVar,
                                @in : AstExpression.Eq(AstExpression.ArrayElemAt(pairVar, 0), AstExpression.ArrayElemAt(pairVar, 1)))))
                );

                return new TranslatedExpression(expression, ast, new BooleanSerializer());
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
