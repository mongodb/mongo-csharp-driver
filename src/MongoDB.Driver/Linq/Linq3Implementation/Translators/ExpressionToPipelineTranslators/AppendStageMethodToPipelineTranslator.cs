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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToPipelineTranslators
{
    internal static class AppendStageMethodToPipelineTranslator
    {
        // public static methods
        public static TranslatedPipeline Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.Is(MongoQueryableMethod.AppendStage))
            {
                var sourceExpression = arguments[0];
                var pipeline = ExpressionToPipelineTranslator.Translate(context, sourceExpression);
                ClientSideProjectionHelper.ThrowIfClientSideProjection(expression, pipeline, method);

                var sourceSerializer = pipeline.OutputSerializer;
                var stageExpression = arguments[1];
                var renderedStage = TranslateStage(expression, stageExpression, sourceSerializer, context.SerializationDomain.SerializerRegistry, context.TranslationOptions);
                var stage = AstStage.Universal(renderedStage.Document);

                var resultSerializerExpression = arguments[2];
                var resultSerializer = resultSerializerExpression.GetConstantValue<IBsonSerializer>(expression);
                var outputSerializer = resultSerializer ?? renderedStage.OutputSerializer;

                pipeline = pipeline.AddStage(stage, outputSerializer);
                return pipeline;
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static IRenderedPipelineStageDefinition TranslateStage(
            Expression expression,
            Expression stageExpression,
            IBsonSerializer inputSerializer,
            IBsonSerializerRegistry serializerRegistry,
            ExpressionTranslationOptions translationOptions)
        {
            var stageDefinition = stageExpression.GetConstantValue<IPipelineStageDefinition>(stageExpression);
            return stageDefinition.Render(inputSerializer, serializerRegistry, translationOptions);
        }
    }
}
