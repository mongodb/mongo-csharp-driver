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
using MongoDB.Driver.Linq3.Ast.Stages;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.PipelineTranslators
{
    public static class OrderByStageTranslator
    {
        // public static methods
        public static TranslatedPipeline Translate(TranslationContext context, MethodCallExpression expression, TranslatedPipeline pipeline)
        {
            if (expression.Method.IsOneOf(QueryableMethod.OrderBy, QueryableMethod.OrderByDescending, QueryableMethod.ThenBy, QueryableMethod.ThenByDescending))
            {
                var keySelector = expression.Arguments[1];

                var sortField = CreateSortField(expression.Method.Name, keySelector, pipeline.OutputSerializer);

                switch (expression.Method.Name)
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
        private static AstSortStageField CreateSortField(string methodName, Expression keySelector, IBsonSerializer outputSerializer)
        {
            var dottedFieldName = GetDottedFieldName(keySelector, outputSerializer);
            //return new BsonElement(dottedFieldName, direction);
            switch (methodName)
            {
                case "OrderBy":
                case "ThenBy":
                    return new AstSortStageField(dottedFieldName, new AstSortStageAscendingSortOrder());
                case "OrderByDescending":
                case "ThenByDescending":
                    return new AstSortStageField(dottedFieldName, new AstSortStageDescendingSortOrder());
                default:
                    throw new ArgumentException("Unexpected method name.", nameof(methodName));
            }
        }

        private static string GetDottedFieldName(Expression keySelector, IBsonSerializer outputSerializer)
        {
            var lambda = ExpressionHelper.Unquote(keySelector);
            var symbolTable = new SymbolTable(lambda.Parameters[0], new Symbol("$CURRENT", outputSerializer));
            var keyField = FieldResolver.ResolveField(lambda.Body, symbolTable);
            return keyField.DottedFieldName;
        }
    }
}
