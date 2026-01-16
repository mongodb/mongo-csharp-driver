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
    internal static class AggregateMethodToAggregationExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(EnumerableOrQueryableMethod.AggregateOverloads))
            {
                var sourceExpression = arguments[0];
                var sourceTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, sourceExpression);
                NestedAsQueryableHelper.EnsureQueryableMethodHasNestedAsQueryableSource(expression, sourceTranslation);
                var itemSerializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);

                if (method.IsOneOf(EnumerableOrQueryableMethod.AggregateWithFunc))
                {
                    var funcLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                    var funcParameters = funcLambda.Parameters;
                    var accumulatorParameter = funcParameters[0];
                    var accumulatorSymbol = context.CreateSymbolWithVarName(accumulatorParameter, varName: "value", itemSerializer); // note: MQL uses $$value for the accumulator
                    var itemParameter = funcParameters[1];
                    var itemSymbol = context.CreateSymbolWithVarName(itemParameter, varName: "this", itemSerializer); // note: MQL uses $$this for the item being processed
                    var funcContext = context.WithSymbols(accumulatorSymbol, itemSymbol);
                    var funcTranslation = ExpressionToAggregationExpressionTranslator.Translate(funcContext, funcLambda.Body);

                    var (sourceVarBinding, sourceAst) = AstExpression.UseVarIfNotSimple("source", sourceTranslation.Ast);
                    var seedVar = AstExpression.Var("seed");
                    var restVar = AstExpression.Var("rest");
                    var ast = AstExpression.Let(
                        var: sourceVarBinding,
                        @in: AstExpression.Let(
                            var1: AstExpression.VarBinding(seedVar, AstExpression.ArrayElemAt(sourceAst, 0)),
                            var2: AstExpression.VarBinding(restVar, AstExpression.Slice(sourceAst, 1, int.MaxValue)),
                            @in: AstExpression.Cond(
                                @if: AstExpression.Eq(AstExpression.Size(restVar), 0),
                                @then: seedVar,
                                @else: AstExpression.Reduce(
                                    input: restVar,
                                    initialValue: seedVar,
                                    @in: funcTranslation.Ast))));

                    return new TranslatedExpression(expression, ast, itemSerializer);
                }
                else if (method.IsOneOf(EnumerableOrQueryableMethod.AggregateWithSeedOverloads))
                {
                    var seedExpression = arguments[1];
                    var seedTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, seedExpression);

                    var funcLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[2]);
                    var funcParameters = funcLambda.Parameters;
                    var accumulatorParameter = funcParameters[0];
                    var accumulatorSerializer = seedTranslation.Serializer;
                    var accumulatorSymbol = context.CreateSymbolWithVarName(accumulatorParameter, varName: "value", accumulatorSerializer); // note: MQL uses $$value for the accumulator
                    var itemParameter = funcParameters[1];
                    var itemSymbol = context.CreateSymbolWithVarName(itemParameter, varName: "this", itemSerializer); // note: MQL uses $$this for the item being processed
                    var funcContext = context.WithSymbols(accumulatorSymbol, itemSymbol);
                    var funcTranslation = ExpressionToAggregationExpressionTranslator.Translate(funcContext, funcLambda.Body);

                    var ast = AstExpression.Reduce(
                        input: sourceTranslation.Ast,
                        initialValue: seedTranslation.Ast,
                        @in: funcTranslation.Ast);
                    var serializer = accumulatorSerializer;

                    if (method.IsOneOf(EnumerableOrQueryableMethod.AggregateWithSeedFuncAndResultSelector))
                    {
                        var resultSelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[3]);
                        var resultSelectorAccumulatorParameter = resultSelectorLambda.Parameters[0];
                        var resultSelectorAccumulatorSymbol = context.CreateSymbol(resultSelectorAccumulatorParameter, accumulatorSerializer);
                        var resultSelectorContext = context.WithSymbol(resultSelectorAccumulatorSymbol);
                        var resultSelectorTranslation = ExpressionToAggregationExpressionTranslator.Translate(resultSelectorContext, resultSelectorLambda.Body);

                        ast = AstExpression.Let(
                            var: AstExpression.VarBinding(resultSelectorAccumulatorSymbol.Var, ast),
                            @in: resultSelectorTranslation.Ast);
                        serializer = resultSelectorTranslation.Serializer;
                    }

                    return new TranslatedExpression(expression, ast, serializer);
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
