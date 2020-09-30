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
using MongoDB.Driver.Linq3.Ast;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators.MethodTranslators
{
    public static class AggregateMethodTranslator
    {
        private static readonly MethodInfo[] __aggregateMethods =
        {
            EnumerableMethod.Aggregate,
            EnumerableMethod.AggregateWithSeedAndFunc,
            EnumerableMethod.AggregateWithSeedFuncAndResultSelector
        };

        public static ExpressionTranslation Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__aggregateMethods))
            {
                var sourceExpression = arguments[0];
                var sourceTranslation = ExpressionTranslator.Translate(context, sourceExpression);
                var itemSerializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);

                if (method.Is(EnumerableMethod.Aggregate))
                {
                    var funcExpression = (LambdaExpression)arguments[1];
                    var funcParameters = funcExpression.Parameters;
                    var accumulatorParameter = funcParameters[0];
                    var itemParameter = funcParameters[1];
                    var accumulatorSymbol = new Symbol("$value", itemSerializer); // note: MQL uses $$value for the accumulator
                    var itemSymbol = new Symbol("$this", itemSerializer);
                    var funcContext = context.WithSymbols((accumulatorParameter, accumulatorSymbol), (itemParameter, itemSymbol));
                    var funcTranslation = ExpressionTranslator.Translate(funcContext, funcExpression.Body);

                    var sourceField = new AstFieldExpression("$$source");
                    var ast = new AstLetExpression(
                        vars: new[] { new AstComputedField("source", sourceTranslation.Ast) },
                        @in: new AstCondExpression(
                            @if: new AstBinaryExpression(AstBinaryOperator.Lte, new AstUnaryExpression(AstUnaryOperator.Size, sourceField), 1),
                            @then: new AstBinaryExpression(AstBinaryOperator.ArrayElemAt, sourceField, 0),
                            @else: new AstReduceExpression(
                                input: new AstSliceExpression(sourceField, 1, int.MaxValue),
                                initialValue: new AstBinaryExpression(AstBinaryOperator.ArrayElemAt, sourceField, 0),
                                @in: funcTranslation.Ast)));

                    return new ExpressionTranslation(expression, ast, itemSerializer);
                }
                else if (method.IsOneOf(EnumerableMethod.AggregateWithSeedAndFunc, EnumerableMethod.AggregateWithSeedFuncAndResultSelector))
                {
                    var seedExpression = arguments[1];
                    var seedTranslation = ExpressionTranslator.Translate(context, seedExpression);

                    var funcExpression = (LambdaExpression)arguments[2];
                    var funcParameters = funcExpression.Parameters;
                    var accumulatorParameter = funcParameters[0];
                    var itemParameter = funcParameters[1];
                    var accumulatorSerializer = BsonSerializer.LookupSerializer(accumulatorParameter.Type); // TODO: use known serializer
                    var accumulatorSymbol = new Symbol("$value", accumulatorSerializer); // note: MQL uses $$value for the accumulator
                    var itemSymbol = new Symbol("$this", itemSerializer);
                    var funcContext = context.WithSymbols((accumulatorParameter, accumulatorSymbol), (itemParameter, itemSymbol));
                    var funcTranslation = ExpressionTranslator.Translate(funcContext, funcExpression.Body);

                    var ast = (AstExpression)new AstReduceExpression(
                        input: sourceTranslation.Ast,
                        initialValue: seedTranslation.Ast,
                        @in: funcTranslation.Ast);
                    var serializer = accumulatorSerializer;

                    if (method.Is(EnumerableMethod.AggregateWithSeedFuncAndResultSelector))
                    {
                        var resultSelectorExpression = (LambdaExpression)arguments[3];
                        var resultSelectorParameter = resultSelectorExpression.Parameters[0];
                        var resultSelectorParameterSerializer = BsonSerializer.LookupSerializer(resultSelectorParameter.Type); // TODO: use known serializer
                        var resultSelectorSymbol = new Symbol("$" + resultSelectorParameter.Name, resultSelectorParameterSerializer);
                        var resultSelectorContext = context.WithSymbol(resultSelectorParameter, resultSelectorSymbol);
                        var resultSelectorTranslation = ExpressionTranslator.Translate(resultSelectorContext, resultSelectorExpression.Body);

                        ast = new AstLetExpression(
                            vars: new[] { new AstComputedField(resultSelectorParameter.Name, ast) },
                            @in: resultSelectorTranslation.Ast);
                        serializer = BsonSerializer.LookupSerializer(resultSelectorExpression.ReturnType); // TODO: use known serializer
                    }

                    return new ExpressionTranslation(expression, ast, serializer);
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
