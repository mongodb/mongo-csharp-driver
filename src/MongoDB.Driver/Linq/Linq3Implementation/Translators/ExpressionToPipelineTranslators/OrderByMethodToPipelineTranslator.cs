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
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToPipelineTranslators
{
    internal static class OrderByMethodToPipelineTranslator
    {
        // public static methods
        public static AstPipeline Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(QueryableMethod.OrderBy, QueryableMethod.OrderByDescending, QueryableMethod.ThenBy, QueryableMethod.ThenByDescending))
            {
                var sourceExpression = arguments[0];
                var pipeline = ExpressionToPipelineTranslator.Translate(context, sourceExpression);
                ClientSideProjectionHelper.ThrowIfClientSideProjection(expression, pipeline, method);

                var keySelectorLambda = ExpressionHelper.UnquoteLambda(arguments[1]);
                var keySelectorTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, keySelectorLambda, pipeline.OutputSerializer, asRoot: true);

                var newSortStages = CreateSortStages(method.Name, keySelectorTranslation);
                switch (method.Name)
                {
                    case "OrderBy":
                    case "OrderByDescending":
                        pipeline = AppendSortStages(pipeline, newSortStages);
                        break;

                    case "ThenBy":
                    case "ThenByDescending":
                        pipeline = CombineSortStages(pipeline, newSortStages);
                        break;
                }

                return pipeline;
            }

            throw new ExpressionNotSupportedException(expression);
        }

        // private static methods
        private static AstPipeline AppendSortStages(AstPipeline pipeline, AstStage[] newSortStages)
        {
            return pipeline.AddStages(pipeline.OutputSerializer, newSortStages);
        }

        private static AstPipeline CombineSortStages(AstPipeline pipeline, AstStage[] newSortStages)
        {
            var oldSortStages = FindOldSortStages(pipeline);

            // 1 stage => $sort
            // 3 stages => $project/$sort/$replaceRoot
            return (oldSortStages.Length, newSortStages.Length) switch
            {
                (1, 1) => Combine1And1(pipeline, oldSortStages, newSortStages),
                (1, 3) => Combine1And3(pipeline, oldSortStages, newSortStages),
                (3, 1) => Combine3And1(pipeline, oldSortStages, newSortStages),
                (3, 3) => Combine3And3(pipeline, oldSortStages, newSortStages),
                _ => throw new Exception("Unexpected number of old and new sort stages.")
            };

            static AstStage[] FindOldSortStages(AstPipeline pipeline)
            {
                var stages = pipeline.Stages;
                var count = stages.Count;

                if (count >= 1)
                {
                    if (stages.Last() is AstSortStage oldSortStage)
                    {
                        return new AstStage[] { oldSortStage };
                    }
                }

                if (count >= 3)
                {
                    if (stages[count - 3] is AstProjectStage oldProjectStage &&
                        stages[count - 2] is AstSortStage oldSortStage &&
                        stages[count - 1] is AstReplaceRootStage oldReplaceRootStage)
                    {
                        return new AstStage[] { oldProjectStage, oldSortStage, oldReplaceRootStage };
                    }
                }

                throw new Exception("Unexpected failure to find old sort stages.");
            }

            static AstPipeline Combine1And1(AstPipeline pipeline, AstStage[] oldSortStages, AstStage[] newSortStages)
            {
                // old:
                // { $sort : { f1 : d1, ..., fj : dj } }
                // new:
                // { $sort : { fj+1 : dj+1 } }
                // combined:
                // { $sort : { f1 : d1, ..., fj : dj, fj+1 : dj+1 } }

                var oldSortStage = (AstSortStage)oldSortStages.Single();
                var newSortStage = (AstSortStage)newSortStages.Single();
                var combinedSortStage = oldSortStage.AddSortField(newSortStage.Fields.Single());

                return pipeline.ReplaceLastStage(pipeline.OutputSerializer, combinedSortStage);
            }

            static AstPipeline Combine1And3(AstPipeline pipeline, AstStage[] oldSortStages, AstStage[] newSortStages)
            {
                // old:
                // { $sort : { f1 : d1, ..., fj : dj } }
                // new:
                // { $project : { _id : 0, _document : "$$ROOT", _key1 : expr1 } }
                // { $sort : { _key1 : dj+1 } }
                // { $replaceRoot : { newRoot : "$_document" } }
                // combined:
                // { $project : { _id : 0, _document : "$$ROOT", _key1 : expr1 } }
                // { $sort : { '_document.f1' : d1, ..., '_document.fj' : dj, _key1 : dj+1 } }
                // { $replaceRoot : { newRoot : "$_document" } }

                var oldSortStage = (AstSortStage)oldSortStages.Single();
                var newProjectStage = (AstProjectStage)newSortStages[0];
                var newSortStage = (AstSortStage)newSortStages[1];
                var newReplaceRootStage = (AstReplaceRootStage)newSortStages[2];
                var combinedSortStage = AstStage.Sort(
                    oldSortStage.Fields.Select(f => AstSort.Field("_document." + f.Path, f.Order))
                    .Append(newSortStage.Fields.Single()));

                return pipeline.ReplaceStagesAtEnd(pipeline.OutputSerializer, numberOfStagesToReplace: 1, newProjectStage, combinedSortStage, newReplaceRootStage);
            }

            static AstPipeline Combine3And1(AstPipeline pipeline, AstStage[] oldSortStages, AstStage[] newSortStages)
            {
                // old:
                // { $project : { _id : 0, _document : "$$ROOT", _key1 : expr1, ..., _keyj : exprj } }
                // { $sort : { f1 : d1, ..., fk : dk } }
                // { $replaceRoot : { newRoot : "$_document" } }
                // new:
                // { $sort : { fk+1 : dk+1 } }
                // combined:
                // { $project : { _id : 0, _document : "$$ROOT", _key1 : expr1, ..., _keyj : exprj } }
                // { $sort : { f1 : d1, ..., fk : dk, '_document.fk+1' : dk+1 } }
                // { $replaceRoot : { newRoot : "$_document" } }

                var oldProjectStage = (AstProjectStage)oldSortStages[0];
                var oldSortStage = (AstSortStage)oldSortStages[1];
                var oldReplaceRootStage = oldSortStages[2];
                var newSortStage = (AstSortStage)newSortStages.Single();
                var combinedSortStage = AstStage.Sort(
                    oldSortStage.Fields
                    .Append(newSortStage.Fields.Select(f => AstSort.Field("_document." + f.Path, f.Order)).Single()));
                    
                return pipeline.ReplaceStagesAtEnd(pipeline.OutputSerializer, numberOfStagesToReplace: 3, oldProjectStage, combinedSortStage, oldReplaceRootStage);
            }

            static AstPipeline Combine3And3(AstPipeline pipeline, AstStage[] oldSortStages, AstStage[] newSortStages)
            {
                // old:
                // { $project : { _id : 0, _document : "$$ROOT", _key1 : oldExpr1, ..., _keyj : oldExprj } }
                // { $sort : { f1 : d1, ..., fk : dk } }
                // { $replaceRoot : { newRoot : "$_document" } }
                // new:
                // { $project : { _id : 0, _document : "$$ROOT", _key1 : newExpr } }
                // { $sort : { _key1 : dnew } }
                // { $replaceRoot : { newRoot : "$_document" } }
                // combined:
                // { $project : { _id : 0, _document : "$$ROOT", _key1 : oldExpr1, ..., _keyj : oldExprj, _keyj+1 : newExpr } }
                // { $sort : { f1 : d1, ..., fk : dk, _keyj+1 : dnew } }
                // { $replaceRoot : { newRoot : "$_document" } }

                var oldProjectStage = (AstProjectStage)oldSortStages[0];
                var oldSortStage = (AstSortStage)oldSortStages[1];
                var oldReplaceRootStage = (AstReplaceRootStage)oldSortStages[2];
                var newProjectStage = (AstProjectStage)newSortStages[0];
                var newSortStage = (AstSortStage)newSortStages[1];

                var j = oldProjectStage.Specifications.Count - 2;
                var newKey = $"_key{j + 1}";
                var newExpr = ((AstProjectStageSetFieldSpecification)newProjectStage.Specifications[2]).Value;
                var dNew = newSortStage.Fields.Single().Order;

                var combinedProjectStage = AstStage.Project(
                    oldProjectStage.Specifications
                    .Append(AstProject.Set(newKey, newExpr)));
                var combinedSortStage = AstStage.Sort(
                    oldSortStage.Fields
                    .Append(AstSort.Field(newKey, dNew)));

                return pipeline.ReplaceStagesAtEnd(pipeline.OutputSerializer, numberOfStagesToReplace: 3, combinedProjectStage, combinedSortStage, oldReplaceRootStage);
            }
        }

        private static AstStage[] CreateSortStages(string methodName, AggregationExpression keySelectorTranslation)
        {
            var sortOrder = ToSortOrder(methodName);

            if (TryConvertKeySelectorTranslationToFieldPath(keySelectorTranslation, out var path))
            {
                var sortField = AstSort.Field(path, sortOrder);
                var sortStage = AstStage.Sort(sortField);
                return new[] { sortStage };
            }
            else
            {
                var projectStage = AstStage.Project(
                    AstProject.Exclude("_id"),
                    AstProject.Set("_document", AstExpression.Var("ROOT")),
                    AstProject.Set("_key1", keySelectorTranslation.Ast));
                var sortStage = AstStage.Sort(AstSort.Field("_key1", sortOrder));
                var replaceRootStage = AstStage.ReplaceRoot(AstExpression.FieldPath("$_document"));
                return new[] { projectStage, sortStage, replaceRootStage };
            }
        }

        private static AstSortOrder ToSortOrder(string methodName)
        {
            return methodName switch
            {
                "OrderBy" or "ThenBy" => AstSortOrder.Ascending,
                "OrderByDescending" or "ThenByDescending" => AstSortOrder.Descending,
                _ => throw new ArgumentException($"Unexpected method name: {methodName}.", nameof(methodName))
            };
        }

        private static bool TryConvertKeySelectorTranslationToFieldPath(AggregationExpression keySelectorTranslation, out string path)
        {
            if (keySelectorTranslation.Ast is AstGetFieldExpression getFieldExpression &&
                getFieldExpression.CanBeConvertedToFieldPath())
            {
                path = getFieldExpression.ConvertToFieldPath();
                if (!path.StartsWith("$$"))
                {
                    path = path.Substring(1);
                    return true;
                }
            }

            path = null;
            return false;
        }
    }
}
