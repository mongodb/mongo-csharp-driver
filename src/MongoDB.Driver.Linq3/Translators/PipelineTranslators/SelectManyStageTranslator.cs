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
using MongoDB.Driver.Linq3.Translators.ExpressionTranslators;

namespace MongoDB.Driver.Linq3.Translators.PipelineTranslators
{
    public static class SelectManyStageTranslator
    {
        // public static methods
        public static TranslatedPipeline Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            var source = arguments[0];
            var pipeline = PipelineTranslator.Translate(context, source);

            if (method.Is(QueryableMethod.SelectMany))
            {
                var selectorLambdaExpression = ExpressionHelper.Unquote(arguments[1]);
                var selectorTranslation = ExpressionTranslator.Translate(context, selectorLambdaExpression, parameterSerializer: pipeline.OutputSerializer);
                var resultValueSerializer = ArraySerializerHelper.GetItemSerializer(selectorTranslation.Serializer);
                var resultWrappedValueSerializer = WrappedValueSerializer.Create(resultValueSerializer);

                pipeline.AddStages(
                    resultWrappedValueSerializer,
                    new AstProjectStage(
                        new AstProjectStageComputedFieldSpecification(new Ast.AstComputedField("_v", selectorTranslation.Ast)),
                        new AstProjectStageExcludeIdSpecification()),
                    new AstUnwindStage("_v"));

                return pipeline;
            }

            if (method.Is(QueryableMethod.SelectManyWithCollectionSelectorAndResultSelector))
            {
                var sourceSerializer = pipeline.OutputSerializer;

                var collectionSelectorLambdaExpression = ExpressionHelper.Unquote(arguments[1]);
                var collectionSelectorTranslation = ExpressionTranslator.Translate(context, collectionSelectorLambdaExpression, parameterSerializer: sourceSerializer);
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
                        new AstProjectStage(
                            new AstProjectStageComputedFieldSpecification(new Ast.AstComputedField("_v", collectionSelectorTranslation.Ast)),
                            new AstProjectStageExcludeIdSpecification()),
                        new AstUnwindStage("_v"));
                }
                else
                {
                    var resultSelectorSourceParameterSymbol = new Symbol("$" + resultSelectorSourceParameterExpression.Name, sourceSerializer);
                    var resultSelectorCollectionItemParameterSymbol = new Symbol("$" + resultSelectorCollectionItemParameterExpression.Name, collectionItemSerializer);
                    var resultSelectorContext = context
                        .WithSymbolAsCurrent(resultSelectorSourceParameterExpression, resultSelectorSourceParameterSymbol)
                        .WithSymbol(resultSelectorCollectionItemParameterExpression, resultSelectorCollectionItemParameterSymbol);
                    var resultSelectorTranslation = ExpressionTranslator.Translate(resultSelectorContext, resultSelectorLambdaExpression.Body);
                    var resultValueSerializer = resultSelectorTranslation.Serializer;
                    var resultWrappedValueSerializer = WrappedValueSerializer.Create(resultValueSerializer);

                    pipeline.AddStages(
                        resultWrappedValueSerializer,
                        new AstProjectStage(
                            new AstProjectStageComputedFieldSpecification(
                                new AstComputedField(
                                    "_v",
                                    new AstMapExpression(
                                        input: collectionSelectorTranslation.Ast,
                                        @as: resultSelectorCollectionItemParameterExpression.Name,
                                        @in: resultSelectorTranslation.Ast))),
                            new AstProjectStageExcludeIdSpecification()),
                        new AstUnwindStage("_v"));
                }

                return pipeline;
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
