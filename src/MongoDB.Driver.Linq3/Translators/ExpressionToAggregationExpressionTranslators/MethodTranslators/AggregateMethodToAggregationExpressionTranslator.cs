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
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Reflection;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public static class AggregateMethodToAggregationExpressionTranslator
    {
        private static readonly MethodInfo[] __aggregateMethods =
        {
            EnumerableMethod.Aggregate,
            EnumerableMethod.AggregateWithSeedAndFunc,
            EnumerableMethod.AggregateWithSeedFuncAndResultSelector
        };

        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__aggregateMethods))
            {
                var sourceExpression = arguments[0];
                var sourceTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, sourceExpression);
                var itemSerializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);

                if (method.Is(EnumerableMethod.Aggregate))
                {
                    var funcLambda = (LambdaExpression)arguments[1];
                    var funcParameters = funcLambda.Parameters;
                    var accumulatorParameter = funcParameters[0];
                    var itemParameter = funcParameters[1];
                    var accumulatorSymbol = new Symbol("$value", itemSerializer); // note: MQL uses $$value for the accumulator
                    var itemSymbol = new Symbol("$this", itemSerializer);
                    var funcContext = context.WithSymbols((accumulatorParameter, accumulatorSymbol), (itemParameter, itemSymbol));
                    var funcTranslation = ExpressionToAggregationExpressionTranslator.Translate(funcContext, funcLambda.Body);

                    var sourceField = AstExpression.Field("$source");
                    var ast = AstExpression.Let(
                        var: AstExpression.Var("source", sourceTranslation.Ast),
                        @in: AstExpression.Cond(
                            @if: AstExpression.Lte(AstExpression.Size(sourceField), 1),
                            @then: AstExpression.ArrayElemAt(sourceField, 0),
                            @else: AstExpression.Reduce(
                                input: AstExpression.Slice(sourceField, 1, int.MaxValue),
                                initialValue: AstExpression.ArrayElemAt(sourceField, 0),
                                @in: funcTranslation.Ast)));

                    return new AggregationExpression(expression, ast, itemSerializer);
                }
                else if (method.IsOneOf(EnumerableMethod.AggregateWithSeedAndFunc, EnumerableMethod.AggregateWithSeedFuncAndResultSelector))
                {
                    var seedExpression = arguments[1];
                    var seedTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, seedExpression);

                    var funcLambda = (LambdaExpression)arguments[2];
                    var funcParameters = funcLambda.Parameters;
                    var accumulatorParameter = funcParameters[0];
                    var itemParameter = funcParameters[1];
                    var accumulatorSerializer = BsonSerializer.LookupSerializer(accumulatorParameter.Type); // TODO: use known serializer
                    var accumulatorSymbol = new Symbol("$value", accumulatorSerializer); // note: MQL uses $$value for the accumulator
                    var itemSymbol = new Symbol("$this", itemSerializer);
                    var funcContext = context.WithSymbols((accumulatorParameter, accumulatorSymbol), (itemParameter, itemSymbol));
                    var funcTranslation = ExpressionToAggregationExpressionTranslator.Translate(funcContext, funcLambda.Body);

                    var ast = AstExpression.Reduce(
                        input: sourceTranslation.Ast,
                        initialValue: seedTranslation.Ast,
                        @in: funcTranslation.Ast);
                    var serializer = accumulatorSerializer;

                    if (method.Is(EnumerableMethod.AggregateWithSeedFuncAndResultSelector))
                    {
                        var resultSelectorLambda = (LambdaExpression)arguments[3];
                        var resultSelectorParameter = resultSelectorLambda.Parameters[0];
                        var resultSelectorParameterSerializer = BsonSerializer.LookupSerializer(resultSelectorParameter.Type); // TODO: use known serializer
                        var resultSelectorSymbol = new Symbol("$" + resultSelectorParameter.Name, resultSelectorParameterSerializer);
                        var resultSelectorContext = context.WithSymbol(resultSelectorParameter, resultSelectorSymbol);
                        var resultSelectorTranslation = ExpressionToAggregationExpressionTranslator.Translate(resultSelectorContext, resultSelectorLambda.Body);

                        ast = AstExpression.Let(
                            var: AstExpression.Var(resultSelectorParameter.Name, ast),
                            @in: resultSelectorTranslation.Ast);
                        serializer = BsonSerializer.LookupSerializer(resultSelectorLambda.ReturnType); // TODO: use known serializer
                    }

                    return new AggregationExpression(expression, ast, serializer);
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
