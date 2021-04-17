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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq3.Ast;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Ast.Stages;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Reflection;
using MongoDB.Driver.Linq3.Serializers;
using MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToPipelineTranslators
{
    public static class GroupByMethodToPipelineTranslator
    {
        // private static fields
        private static readonly MethodInfo[] __groupByMethods;
        private static readonly MethodInfo[] __groupByMethodsWithElementSelector;
        private static readonly MethodInfo[] __groupByMethodsWithResultSelector;

        // static constructor
        static GroupByMethodToPipelineTranslator()
        {
            __groupByMethods = new[]
            {
                QueryableMethod.GroupByWithKeySelector,
                QueryableMethod.GroupByWithKeySelectorAndElementSelector,
                QueryableMethod.GroupByWithKeySelectorAndResultSelector,
                QueryableMethod.GroupByWithKeySelectorElementSelectorAndResultSelector
            };

            __groupByMethodsWithElementSelector = new[]
            {
                QueryableMethod.GroupByWithKeySelectorAndElementSelector,
                QueryableMethod.GroupByWithKeySelectorElementSelectorAndResultSelector
            };

            __groupByMethodsWithResultSelector = new[]
            {
                QueryableMethod.GroupByWithKeySelectorAndResultSelector,
                QueryableMethod.GroupByWithKeySelectorElementSelectorAndResultSelector
            };
        }

        // public static methods
        public static AstPipeline Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__groupByMethods))
            {
                var sourceExpression = arguments[0];
                var pipeline = ExpressionToPipelineTranslator.Translate(context, sourceExpression);
                var sourceSerializer = pipeline.OutputSerializer;

                var keySelectorLambda = ExpressionHelper.UnquoteLambda(arguments[1]);
                var keySelectorTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, keySelectorLambda, sourceSerializer, asCurrentSymbol: true);
                var keySerializer = keySelectorTranslation.Serializer;

                AstExpression elementAst;
                IBsonSerializer elementSerializer;
                if (method.IsOneOf(__groupByMethodsWithElementSelector))
                {
                    var elementLambda = ExpressionHelper.UnquoteLambda(arguments[2]);
                    var elementTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, elementLambda, sourceSerializer, asCurrentSymbol: true);
                    elementAst = elementTranslation.Ast;
                    elementSerializer = elementTranslation.Serializer;
                }
                else
                {
                    if (sourceSerializer is IWrappedValueSerializer wrappedSerializer)
                    {
                        elementAst = AstExpression.Field("_v");
                        elementSerializer = wrappedSerializer.ValueSerializer;

                    }
                    else
                    {
                        elementAst = AstExpression.Field("$ROOT");
                        elementSerializer = sourceSerializer;
                    }
                }

                var groupingSerializer = IGroupingSerializer.Create(keySerializer, elementSerializer);
                pipeline = pipeline.AddStages(
                    groupingSerializer,
                    AstStage.Group(
                        id: keySelectorTranslation.Ast,
                        fields: AstExpression.ComputedField("_elements", AstExpression.Push(elementAst))));

                if (method.IsOneOf(__groupByMethodsWithResultSelector))
                {
                    var resultSelectorLambda = ExpressionHelper.UnquoteLambda(arguments.Last());
                    var keyParameter = resultSelectorLambda.Parameters[0];
                    var keySymbol = new Symbol("_id", keySerializer);
                    var elementsParameter = resultSelectorLambda.Parameters[1];
                    var elementsSerializer = IEnumerableSerializer.Create(elementSerializer);
                    var elementsSymbol = new Symbol("_elements", elementsSerializer);
                    var resultSelectContext = context
                        .WithSymbol(keyParameter, keySymbol)
                        .WithSymbol(elementsParameter, elementsSymbol);
                    var resultSelectorTranslation = ExpressionToAggregationExpressionTranslator.Translate(resultSelectContext, resultSelectorLambda.Body);
                    var (projectStage, projectionSerializer) = ProjectionHelper.CreateProjectStage(resultSelectorTranslation);
                    pipeline = pipeline.AddStages(projectionSerializer, projectStage);
                }

                return pipeline;
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
