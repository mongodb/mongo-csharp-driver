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

using System;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Ast.Stages;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToPipelineTranslators
{
    public static class OrderByMethodToPipelineTranslator
    {
        // public static methods
        public static Pipeline Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            var source = arguments[0];
            var pipeline = ExpressionToPipelineTranslator.Translate(context, source);

            if (method.IsOneOf(QueryableMethod.OrderBy, QueryableMethod.OrderByDescending, QueryableMethod.ThenBy, QueryableMethod.ThenByDescending))
            {
                var keySelector = ExpressionHelper.Unquote(arguments[1]);

                var sortField = CreateSortField(context, method.Name, keySelector, parameterSerializer: pipeline.OutputSerializer);

                switch (method.Name)
                {
                    case "OrderBy":
                    case "OrderByDescending":
                        pipeline.AddStages(
                            pipeline.OutputSerializer,
                            //new BsonDocument("$sort", new BsonDocument(sortElement)));
                            new AstSortStage(new[] { sortField }));
                        break;

                    case "ThenBy":
                    case "ThenByDescending":
                        //var sortStage = pipeline.Stages.Last();
                        //var sortDocument = sortStage["$sort"].AsBsonDocument;
                        //sortDocument.Add(sortElement);
                        var sortStage = (AstSortStage)pipeline.Stages.Last();
                        var newSortStage = sortStage.AddSortField(sortField);
                        pipeline.ReplaceLastStage(pipeline.OutputSerializer, newSortStage);
                        break;
                }

                return pipeline;
            }

            throw new ExpressionNotSupportedException(expression);
        }

        // private static methods
        private static AstSortStageField CreateSortField(TranslationContext context, string methodName, LambdaExpression keySelector, IBsonSerializer parameterSerializer)
        {
            var fieldPath = GetFieldPath(context, keySelector, parameterSerializer);
            switch (methodName)
            {
                case "OrderBy":
                case "ThenBy":
                    return new AstSortStageField(fieldPath, new AstSortStageAscendingSortOrder());
                case "OrderByDescending":
                case "ThenByDescending":
                    return new AstSortStageField(fieldPath, new AstSortStageDescendingSortOrder());
                default:
                    throw new ArgumentException("Unexpected method name.", nameof(methodName));
            }
        }

        private static string GetFieldPath(TranslationContext context, LambdaExpression keySelector, IBsonSerializer parameterSerializer)
        {
            var keySelectorTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, keySelector, parameterSerializer);
            if (keySelectorTranslation.Ast is AstFieldExpression fieldExpressionAst)
            {
                return fieldExpressionAst.Path;
            }

            throw new ExpressionNotSupportedException(keySelector);
        }
    }
}
