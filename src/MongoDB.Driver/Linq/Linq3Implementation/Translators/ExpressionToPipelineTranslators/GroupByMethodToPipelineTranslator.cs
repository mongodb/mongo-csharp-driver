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

using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToPipelineTranslators
{
    internal static class GroupByMethodToPipelineTranslator
    {
        // public static methods
        public static TranslatedPipeline Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(QueryableMethod.GroupByOverloads))
            {
                var sourceExpression = arguments[0];
                var pipeline = ExpressionToPipelineTranslator.Translate(context, sourceExpression);
                ClientSideProjectionHelper.ThrowIfClientSideProjection(expression, pipeline, method);

                var sourceSerializer = pipeline.OutputSerializer;
                var keySelectorLambda = ExpressionHelper.UnquoteLambda(arguments[1]);
                var keySelectorTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, keySelectorLambda, sourceSerializer, asRoot: true);
                var keySerializer = keySelectorTranslation.Serializer;

                var (elementAst, elementSerializer) = TranslateElement(context, method, arguments, sourceSerializer);

                var groupingSerializer = IGroupingSerializer.Create(keySerializer, elementSerializer);
                pipeline = pipeline.AddStage(
                    AstStage.Group(
                        id: keySelectorTranslation.Ast,
                        fields: AstExpression.AccumulatorField("_elements", AstUnaryAccumulatorOperator.Push, elementAst)),
                    groupingSerializer);

                if (method.IsOneOf(QueryableMethod.GroupByWithResultSelectorOverloads))
                {
                    pipeline = TranslateResultSelector(context, pipeline, arguments, keySerializer, elementSerializer);
                }

                return pipeline;
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static (AstExpression, IBsonSerializer) TranslateElement(
            TranslationContext context,
            MethodInfo method,
            ReadOnlyCollection<Expression> arguments,
            IBsonSerializer sourceSerializer)
        {
            AstExpression elementAst;
            IBsonSerializer elementSerializer;
            if (method.IsOneOf(QueryableMethod.GroupByWithElementSelectorOverloads))
            {
                var elementLambda = ExpressionHelper.UnquoteLambda(arguments[2]);
                var elementTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, elementLambda, sourceSerializer, asRoot: true);
                elementAst = elementTranslation.Ast;
                elementSerializer = elementTranslation.Serializer;
            }
            else
            {
                if (sourceSerializer is IWrappedValueSerializer wrappedSerializer)
                {
                    elementAst = AstExpression.GetField(AstExpression.RootVar, wrappedSerializer.FieldName);
                    elementSerializer = wrappedSerializer.ValueSerializer;
                }
                else
                {
                    elementAst = AstExpression.RootVar;
                    elementSerializer = sourceSerializer;
                }
            }

            return (elementAst, elementSerializer);
        }

        private static TranslatedPipeline TranslateResultSelector(
            TranslationContext context,
            TranslatedPipeline pipeline,
            ReadOnlyCollection<Expression> arguments,
            IBsonSerializer keySerializer,
            IBsonSerializer elementSerializer)
        {
            var resultSelectorLambda = ExpressionHelper.UnquoteLambda(arguments.Last());
            var keyParameter = resultSelectorLambda.Parameters[0];
            var keyField = AstExpression.GetField(AstExpression.RootVar, "_id");
            var keySymbol = context.CreateSymbol(keyParameter, keyField, keySerializer);
            var elementsParameter = resultSelectorLambda.Parameters[1];
            var elementsField = AstExpression.GetField(AstExpression.RootVar, "_elements");
            var elementsSerializer = IEnumerableSerializer.Create(elementSerializer);
            var elementsSymbol = context.CreateSymbol(elementsParameter, elementsField, elementsSerializer);
            var resultSelectContext = context.WithSymbols(keySymbol, elementsSymbol);
            var resultSelectorTranslation = ExpressionToAggregationExpressionTranslator.Translate(resultSelectContext, resultSelectorLambda.Body);
            var (projectStage, projectionSerializer) = ProjectionHelper.CreateProjectStage(resultSelectorTranslation);
            return pipeline.AddStage(projectStage, projectionSerializer);
        }
    }
}
