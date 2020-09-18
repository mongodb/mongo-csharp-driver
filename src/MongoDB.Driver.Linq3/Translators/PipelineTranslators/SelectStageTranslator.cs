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
using MongoDB.Driver.Linq3.Ast.Stages;
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
            if (expression.Method.Is(QueryableMethod.Select))
            {
                var selector = expression.Arguments[1];

                var lambda = ExpressionHelper.Unquote(selector);
                var selectorContext = context.WithSymbolAsCurrent(lambda.Parameters[0], new Symbol("$ROOT", pipeline.OutputSerializer));
                var translatedSelector = ExpressionTranslator.Translate(selectorContext, lambda.Body);
                var translatedSelectorSerializer = translatedSelector.Serializer ?? BsonSerializer.LookupSerializer(lambda.ReturnType);

                if (translatedSelectorSerializer is IBsonDocumentSerializer)
                {
                    var projection = ProjectionHelper.ConvertExpressionToProjection(translatedSelector.Translation);

                    pipeline.AddStages(
                        translatedSelectorSerializer,
                        //new BsonDocument("$project", projection));
                        new AstProjectStage(projection));

                    return pipeline;
                }
                else
                {
                    var valueType = lambda.ReturnType;
                    var wrappedValueSerializer = WrappedValueSerializer.Create(translatedSelectorSerializer);

                    pipeline.AddStages(
                        wrappedValueSerializer,
                        //new BsonDocument("$project", new BsonDocument { { "_id", 0 }, { "_v", translatedSelector.Translation } }));
                        new AstProjectStage(
                            new AstProjectStageExcludeIdSpecification(),
                            new AstProjectStageComputedFieldSpecification(new Ast.AstComputedField("_v", translatedSelector.Translation))));

                    return pipeline;
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
