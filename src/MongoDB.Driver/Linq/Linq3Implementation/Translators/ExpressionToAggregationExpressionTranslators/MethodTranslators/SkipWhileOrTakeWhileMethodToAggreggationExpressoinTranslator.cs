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
using MongoDB.Bson;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class SkipWhileOrTakeWhileMethodToAggregationExpressionTranslator
    {
        private static MethodInfo[] __skipWhileOrTakeWhileMethods =
        {
            EnumerableMethod.SkipWhile,
            EnumerableMethod.TakeWhile,
            QueryableMethod.SkipWhile,
            QueryableMethod.TakeWhile
        };

        private static MethodInfo[] __skipWhileMethods =
        {
            EnumerableMethod.SkipWhile,
            QueryableMethod.SkipWhile
        };

        private static MethodInfo[] __takeWhileMethods =
        {
            EnumerableMethod.TakeWhile,
            QueryableMethod.TakeWhile
        };

        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__skipWhileOrTakeWhileMethods))
            {
                var sourceExpression = arguments[0];
                var sourceTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, sourceExpression);
                NestedAsQueryableHelper.EnsureQueryableMethodHasNestedAsQueryableSource(expression, sourceTranslation);
                var itemSerializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);

                var predicateExpression = arguments[1];
                var predicateLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, predicateExpression);
                var predicateParameter = predicateLambda.Parameters.Single();
                var thisSymbol = context.CreateSymbol(predicateParameter, "this", itemSerializer);
                var predicateTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, predicateLambda, thisSymbol);

                var (sourceBinding, sourceAst) = AstExpression.UseVarIfNotSimple("source", sourceTranslation.Ast);

                var valueVar = AstExpression.Var("value");
                var reduceAst = AstExpression.Reduce(
                    input: sourceAst,
                    initialValue: new BsonDocument { { "p", true }, { "n", 0 } },
                    @in: AstExpression.Cond(
                        @if: AstExpression.And(AstExpression.GetField(valueVar, "p"), predicateTranslation.Ast),
                        @then: AstExpression.ComputedDocument(
                        [
                            AstExpression.ComputedField("p", true),
                            AstExpression.ComputedField("n", AstExpression.Add(AstExpression.GetField(valueVar, "n"), 1))
                        ]),
                        @else: valueVar));
                var countAst = AstExpression.GetField(reduceAst, "n");

                var sliceAst = method switch
                {
                    _ when method.IsOneOf(__skipWhileMethods) => AstExpression.Slice(sourceTranslation.Ast, countAst, int.MaxValue),
                    _ when method.IsOneOf(__takeWhileMethods) => AstExpression.Slice(sourceTranslation.Ast, countAst),
                    _ => throw new ExpressionNotSupportedException(expression)
                };

                var ast = AstExpression.Let(
                    sourceBinding,
                    sliceAst);

                var resultSerializer = NestedAsQueryableSerializer.CreateIEnumerableOrNestedAsQueryableSerializer(expression.Type, itemSerializer);
                return new TranslatedExpression(expression, ast, resultSerializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
