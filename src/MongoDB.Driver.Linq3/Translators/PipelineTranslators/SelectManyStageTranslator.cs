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
        public static TranslatedPipeline Translate(TranslationContext context, MethodCallExpression expression, TranslatedPipeline pipeline)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.Is(QueryableMethod.SelectMany))
            {
                var selectorExpression = ExpressionHelper.Unquote(arguments[1]);
                var selectorTranslation = ExpressionTranslator.Translate(context, selectorExpression, parameterSerializer: pipeline.OutputSerializer);
                if (!(selectorTranslation.Ast is AstFieldExpression selectorFieldAst))
                {
                    goto notSupported;
                }
                var fieldName = selectorFieldAst.Field.Substring(1); // remove leading "$"
                var outputValueType = selectorExpression.ReturnType.GetGenericArguments()[0];
                var outputValueSerializer = BsonSerializer.LookupSerializer(outputValueType); // TODO: use known serializer
                var outputWrappedValueSerializer = WrappedValueSerializer.Create(outputValueSerializer);

                pipeline.AddStages(
                    outputWrappedValueSerializer,
                    new AstProjectStage(
                        new AstProjectStageComputedFieldSpecification(new Ast.AstComputedField("_v", new AstFieldExpression("$" + fieldName))),
                        new AstProjectStageExcludeIdSpecification()),
                    new AstUnwindStage("_v"));

                return pipeline;
            }

        notSupported:
            throw new ExpressionNotSupportedException(expression);
        }
    }
}
