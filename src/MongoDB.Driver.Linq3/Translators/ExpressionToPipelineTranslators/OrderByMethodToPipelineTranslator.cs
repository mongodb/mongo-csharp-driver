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
                var keySelector = arguments[1];

                var sortField = CreateSortField(method.Name, keySelector, pipeline.OutputSerializer);

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
            var keySelectorLambdaExpression = ExpressionHelper.Unquote(keySelector);
            var symbolTable = new SymbolTable(keySelectorLambdaExpression.Parameters[0], new Symbol("$$CURRENT", outputSerializer));
            var keyField = FieldResolver.ResolveField(keySelectorLambdaExpression.Body, symbolTable);
            return keyField.DottedFieldName;
        }
    }
}
