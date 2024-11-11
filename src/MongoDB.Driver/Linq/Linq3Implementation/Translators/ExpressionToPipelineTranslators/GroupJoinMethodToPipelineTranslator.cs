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
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToPipelineTranslators
{
    internal static class GroupJoinMethodToPipelineTranslator
    {
        // public static methods
        public static AstPipeline Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.Is(QueryableMethod.GroupJoin))
            {
                var outerExpression = arguments[0];
                var pipeline = ExpressionToPipelineTranslator.Translate(context, outerExpression);
                ClientSideProjectionHelper.ThrowIfClientSideProjection(expression, pipeline, method);

                AstExpression outerAst;
                var rootVar = AstExpression.Var("ROOT", isCurrent: true);
                var outerSerializer = pipeline.OutputSerializer;
                if (outerSerializer is IWrappedValueSerializer wrappedSerializer)
                {
                    outerAst = AstExpression.GetField(rootVar, wrappedSerializer.FieldName);
                    outerSerializer = wrappedSerializer.ValueSerializer;
                }
                else
                {
                    outerAst = rootVar;
                }

                var wrapOuterStage = AstStage.Project(
                    AstProject.Set("_outer", outerAst),
                    AstProject.ExcludeId());
                var wrappedOuterSerializer = WrappedValueSerializer.Create("_outer", outerSerializer);

                var innerExpression = arguments[1];
                var (queryProvider, isRawCollectionExpression) = innerExpression.FindMongoQueryProvider(containerExpression: expression);

                var outerKeySelectorLambda = ExpressionHelper.UnquoteLambda(arguments[2]);
                var localField = outerKeySelectorLambda.TranslateToDottedFieldName(context, wrappedOuterSerializer);

                var innerKeySelectorLambda = ExpressionHelper.UnquoteLambda(arguments[3]);
                var foreignField = innerKeySelectorLambda.TranslateToDottedFieldName(context, queryProvider.PipelineInputSerializer);

                AstStage lookupStage;

                if (isRawCollectionExpression)
                {
                    lookupStage = AstStage.Lookup(
                        from: queryProvider.CollectionNamespace.CollectionName,
                        localField,
                        foreignField,
                        @as: "_inner");
                }
                else
                {
                    var lookupPipeline = ExpressionToPipelineTranslator.Translate(context, innerExpression);

                    lookupStage = AstStage.Lookup(
                        from: queryProvider.CollectionNamespace.CollectionName,
                        localField,
                        foreignField,
                        Array.Empty<AstComputedField>(),
                        lookupPipeline,
                        @as: "_inner");
                }

                var resultSelectorLambda = ExpressionHelper.UnquoteLambda(arguments[4]);
                var root = AstExpression.Var("ROOT", isCurrent: true);
                var outerParameter = resultSelectorLambda.Parameters[0];
                var outerField = AstExpression.GetField(root, "_outer");
                var outerSymbol = context.CreateSymbol(outerParameter, outerField, outerSerializer);
                var innerParameter = resultSelectorLambda.Parameters[1];
                var innerField = AstExpression.GetField(root, "_inner");
                var ienumerableInnerSerializer = IEnumerableSerializer.Create(queryProvider.PipelineInputSerializer);
                var innerSymbol = context.CreateSymbol(innerParameter, innerField, ienumerableInnerSerializer);
                var resultSelectorContext = context.WithSymbols(outerSymbol, innerSymbol);
                var resultSelectorTranslation = ExpressionToAggregationExpressionTranslator.Translate(resultSelectorContext, resultSelectorLambda.Body);
                var (projectStage, newOutputSerializer) = ProjectionHelper.CreateProjectStage(resultSelectorTranslation);

                pipeline = pipeline.AddStages(
                    newOutputSerializer,
                    wrapOuterStage,
                    lookupStage,
                    projectStage);

                return pipeline;
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
