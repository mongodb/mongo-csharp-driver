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
using MongoDB.Driver.Linq3.Ast;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Ast.Stages;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Serializers;
using MongoDB.Driver.Linq3.Translators.ExpressionTranslators;

namespace MongoDB.Driver.Linq3.Translators.PipelineTranslators
{
    public static class GroupByStageTranslator
    {
        // public static methods
        public static TranslatedPipeline Translate(TranslationContext context, MethodCallExpression expression, TranslatedPipeline pipeline)
        {
            if (expression.Method.IsOneOf(QueryableMethod.GroupByWithKeySelector, QueryableMethod.GroupByWithKeySelectorAndElementSelector, QueryableMethod.GroupByWithKeySelectorAndResultSelector))
            {
                var keySelector = expression.Arguments[1];

                var keySelectorLambda = ExpressionHelper.Unquote(keySelector);
                var keySelectorContext = context.WithSymbolAsCurrent(keySelectorLambda.Parameters[0], new Symbol("$ROOT", pipeline.OutputSerializer));
                var translatedKeySelector = ExpressionTranslator.Translate(keySelectorContext, keySelectorLambda.Body);
                var keySerializer = translatedKeySelector.Serializer ?? BsonSerializer.LookupSerializer(keySelectorLambda.ReturnType);

                if (expression.Method.Is(QueryableMethod.GroupByWithKeySelector))
                {
                    var elementSerializer = pipeline.OutputSerializer;
                    var groupingSerializer = IGroupingSerializer.Create(keySerializer, elementSerializer);

                    pipeline.AddStages(
                        groupingSerializer,
                        //new BsonDocument("$group", new BsonDocument
                        //{
                        //    { "_id", translatedKeySelector.Translation },
                        //    { "_elements", new BsonDocument("$push", "$$ROOT") }
                        //}));
                        new AstGroupStage(
                            translatedKeySelector.Translation,
                            new[] { new AstComputedField("_elements", new AstUnaryExpression(AstUnaryOperator.Push, new AstFieldExpression("$$ROOT"))) }));

                    return pipeline;
                }

                if (expression.Method.Is(QueryableMethod.GroupByWithKeySelectorAndElementSelector))
                {
                    var elementSelector = expression.Arguments[2];

                    var elementSelectorLambda = ExpressionHelper.Unquote(elementSelector);
                    var elementSelectorContext = context.WithSymbolAsCurrent(elementSelectorLambda.Parameters[0], new Symbol("$ROOT", pipeline.OutputSerializer));
                    var translatedElementSelector = ExpressionTranslator.Translate(elementSelectorContext, elementSelectorLambda.Body);

                    var elementSerializer = translatedElementSelector.Serializer ?? BsonSerializer.LookupSerializer(elementSelectorLambda.ReturnType);
                    var groupingSerializer = IGroupingSerializer.Create(keySerializer, elementSerializer);

                    pipeline.AddStages(
                        groupingSerializer,
                        //new BsonDocument("$group", new BsonDocument
                        //{
                        //    { "_id", translatedKeySelector.Translation },
                        //    { "_elements", new BsonDocument("$push", translatedElementSelector.Translation) }
                        //}));
                        new AstGroupStage(
                            translatedKeySelector.Translation,
                            new[] { new AstComputedField("_elements", new AstUnaryExpression(AstUnaryOperator.Push, translatedElementSelector.Translation)) }));

                    return pipeline;
                }

                if (expression.Method.Is(QueryableMethod.GroupByWithKeySelectorAndResultSelector))
                {
                    var resultSelector = expression.Arguments[2];

                    var resultSelectorLambda = ExpressionHelper.Unquote(resultSelector);
                    var keyParameter = resultSelectorLambda.Parameters[0];
                    var elementsParameter = resultSelectorLambda.Parameters[1];
                    var elementSerializer = pipeline.OutputSerializer;
                    var enumerableElementSerializer = IEnumerableSerializer.Create(elementSerializer);

                    var resultSelectorContext = context.WithSymbols(
                        ( keyParameter, new Symbol("_id", translatedKeySelector.Serializer) ),
                        ( elementsParameter, new Symbol("_elements", enumerableElementSerializer) )
                    );
                    var translatedResultSelector = ExpressionTranslator.Translate(resultSelectorContext, resultSelectorLambda.Body);
                    var projection = ProjectionHelper.ConvertExpressionToProjection(translatedResultSelector.Translation);

                    pipeline.AddStages(
                        translatedResultSelector.Serializer,
                        //new BsonDocument("$group", new BsonDocument
                        //{
                        //    { "_id", translatedKeySelector.Translation },
                        //    { "_elements", new BsonDocument("$push", "$$ROOT") }
                        //}),
                        //new BsonDocument("$project", projection));
                        new AstGroupStage(
                            translatedKeySelector.Translation,
                            new AstComputedField("_elements", new AstUnaryExpression(AstUnaryOperator.Push, new AstFieldExpression("$$ROOT")))),
                        new AstProjectStage(projection));

                    return pipeline;
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
