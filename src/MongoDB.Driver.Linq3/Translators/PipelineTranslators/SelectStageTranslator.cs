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
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Ast.Stages;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Serializers;
using MongoDB.Driver.Linq3.Translators.ExpressionTranslators;

namespace MongoDB.Driver.Linq3.Translators.PipelineTranslators
{
    public static class SelectStageTranslator
    {
        // public static methods
        public static TranslatedPipeline Translate(TranslationContext context, MethodCallExpression expression, TranslatedPipeline pipeline)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.Is(QueryableMethod.Select))
            {
                var selectorExpression = ExpressionHelper.Unquote(arguments[1]);
                var selectorTranslation = ExpressionTranslator.Translate(context, selectorExpression, parameterSerializer: pipeline.OutputSerializer);

                if (selectorTranslation.Ast is AstComputedDocumentExpression)
                {
                    var projection = ProjectionHelper.ConvertExpressionToProjection(selectorTranslation.Ast);

                    pipeline.AddStages(
                        selectorTranslation.Serializer,
                        new AstProjectStage(projection));
                }
                else
                {
                    var wrappedValueSerializer = WrappedValueSerializer.Create(selectorTranslation.Serializer);

                    pipeline.AddStages(
                        wrappedValueSerializer,
                        new AstProjectStage(
                            new AstProjectStageComputedFieldSpecification(new Ast.AstComputedField("_v", selectorTranslation.Ast)),
                            new AstProjectStageExcludeIdSpecification()));
                }

                return pipeline;
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
