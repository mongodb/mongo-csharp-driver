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
using MongoDB.Driver.Linq3.Ast.Stages;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Serializers;
using MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToPipelineTranslators
{
    public static class SelectManyMethodToPipelineTranslator
    {
        // public static methods
        public static Pipeline Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            var source = arguments[0];
            var pipeline = ExpressionToPipelineTranslator.Translate(context, source);
            var sourceSerializer = pipeline.OutputSerializer;

            if (method.Is(QueryableMethod.SelectMany))
            {
                var selectorLambdaExpression = ExpressionHelper.Unquote(arguments[1]);
                var selectorTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, selectorLambdaExpression, sourceSerializer, asCurrentSymbol: true);
                var resultValueSerializer = ArraySerializerHelper.GetItemSerializer(selectorTranslation.Serializer);
                var resultWrappedValueSerializer = WrappedValueSerializer.Create(resultValueSerializer);

                pipeline.AddStages(
                    resultWrappedValueSerializer,
                    AstStage.Project(
                        new AstProjectStageComputedFieldSpecification(new Ast.AstComputedField("_v", selectorTranslation.Ast)),
                        new AstProjectStageExcludeIdSpecification()),
                    AstStage.Unwind("_v"));

                return pipeline;
            }

            if (method.Is(QueryableMethod.SelectManyWithCollectionSelectorAndResultSelector))
            {
                var collectionSelectorLambdaExpression = ExpressionHelper.Unquote(arguments[1]);
                var collectionSelectorTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, collectionSelectorLambdaExpression, sourceSerializer, asCurrentSymbol: true);
                var collectionItemSerializer = ArraySerializerHelper.GetItemSerializer(collectionSelectorTranslation.Serializer);

                var resultSelectorLambdaExpression = ExpressionHelper.Unquote(arguments[2]);
                var resultSelectorSourceParameterExpression = resultSelectorLambdaExpression.Parameters[0];
                var resultSelectorCollectionItemParameterExpression = resultSelectorLambdaExpression.Parameters[1];

                if (resultSelectorLambdaExpression.Body == resultSelectorCollectionItemParameterExpression)
                {
                    // special case identity resultSelector: (x, y) => y
                    var resultValueSerializer = collectionItemSerializer;
                    var resultWrappedValueSerializer = WrappedValueSerializer.Create(resultValueSerializer);

                    pipeline.AddStages(
                        resultWrappedValueSerializer,
                        AstStage.Project(
                            new AstProjectStageComputedFieldSpecification(new Ast.AstComputedField("_v", collectionSelectorTranslation.Ast)),
                            new AstProjectStageExcludeIdSpecification()),
                        AstStage.Unwind("_v"));
                }
                else
                {
                    var resultSelectorSourceParameterSymbol = new Symbol("$" + resultSelectorSourceParameterExpression.Name, sourceSerializer);
                    var resultSelectorCollectionItemParameterSymbol = new Symbol("$" + resultSelectorCollectionItemParameterExpression.Name, collectionItemSerializer);
                    var resultSelectorContext = context
                        .WithSymbolAsCurrent(resultSelectorSourceParameterExpression, resultSelectorSourceParameterSymbol)
                        .WithSymbol(resultSelectorCollectionItemParameterExpression, resultSelectorCollectionItemParameterSymbol);
                    var resultSelectorTranslation = ExpressionToAggregationExpressionTranslator.Translate(resultSelectorContext, resultSelectorLambdaExpression.Body);
                    var resultValueSerializer = resultSelectorTranslation.Serializer;
                    var resultWrappedValueSerializer = WrappedValueSerializer.Create(resultValueSerializer);

                    pipeline.AddStages(
                        resultWrappedValueSerializer,
                        AstStage.Project(
                            new AstProjectStageComputedFieldSpecification(
                                AstExpression.ComputedField(
                                    "_v",
                                    AstExpression.Map(
                                        input: collectionSelectorTranslation.Ast,
                                        @as: resultSelectorCollectionItemParameterExpression.Name,
                                        @in: resultSelectorTranslation.Ast))),
                            new AstProjectStageExcludeIdSpecification()),
                        AstStage.Unwind("_v"));
                }

                return pipeline;
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
