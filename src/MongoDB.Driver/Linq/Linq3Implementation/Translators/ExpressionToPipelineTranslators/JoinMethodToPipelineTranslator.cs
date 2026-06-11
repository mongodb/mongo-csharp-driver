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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToPipelineTranslators
{
    internal static class JoinMethodToPipelineTranslator
    {
        // public static methods
        public static TranslatedPipeline Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            var isLeftJoin = method.IsOneOf(MongoQueryableMethod.LeftJoin, QueryableMethod.LeftJoin);
            if (isLeftJoin || method.Is(QueryableMethod.Join))
            {
                var outerExpression = arguments[0];
                var innerExpression = arguments[1];
                var outerKeySelectorLambda = ExpressionHelper.UnquoteLambda(arguments[2]);
                var innerKeySelectorLambda = ExpressionHelper.UnquoteLambda(arguments[3]);
                var resultSelectorLambda = ExpressionHelper.UnquoteLambda(arguments[4]);

                var pipeline = ExpressionToPipelineTranslator.Translate(context, outerExpression);
                ClientSideProjectionHelper.ThrowIfClientSideProjection(expression, pipeline, method);

                AstExpression outerAst;
                IBsonSerializer outerSerializer;
                if (pipeline.OutputSerializer is IWrappedValueSerializer pipelineOutputWrappedSerializer)
                {
                    outerAst = AstExpression.GetField(AstExpression.RootVar, pipelineOutputWrappedSerializer.FieldName);
                    outerSerializer = pipelineOutputWrappedSerializer.ValueSerializer;
                }
                else
                {
                    outerAst = AstExpression.RootVar;
                    outerSerializer = pipeline.OutputSerializer;
                }

                var wrapOuterStage = AstStage.Project(
                    AstProject.Set("_outer", outerAst),
                    AstProject.Exclude("_id"));
                var wrappedOuterSerializer = WrappedValueSerializer.Create("_outer", outerSerializer);

                string innerCollectionName;
                IBsonSerializer innerSerializer;
                AstPipeline innerFilterPipeline = null;

                if (innerExpression is ConstantExpression)
                {
                    (innerCollectionName, innerSerializer) = innerExpression.GetCollectionInfoFromQueryable(containerExpression: expression);
                }
                else
                {
                    var rootInnerExpression = TranslationContext.GetUltimateSource(innerExpression);
                    (innerCollectionName, innerSerializer) = rootInnerExpression.GetCollectionInfoFromQueryable(containerExpression: expression);
                    var innerTranslation = ExpressionToPipelineTranslator.Translate(context, innerExpression);
                    innerSerializer = innerTranslation.OutputSerializer;
                    innerFilterPipeline = innerTranslation.Ast.Stages.Count > 0 ? innerTranslation.Ast : null;
                }

                var localField = outerKeySelectorLambda.TranslateToDottedFieldName(context, wrappedOuterSerializer);
                var foreignField = innerKeySelectorLambda.TranslateToDottedFieldName(context, innerSerializer);

                // When the inner sequence is filtered we emit a $lookup that combines localField/foreignField
                // with a pipeline. That concise syntax requires MongoDB 5.0+ (Feature.LookupConciseSyntax);
                // a bare inner sequence uses the simpler localField/foreignField form supported by all servers.
                var lookupStage = innerFilterPipeline != null
                    ? AstStage.Lookup(innerCollectionName, localField, foreignField, [], innerFilterPipeline, "_inner")
                    : AstStage.Lookup(from: innerCollectionName, localField, foreignField, @as: "_inner");

                var unwindStage = AstStage.Unwind("_inner", preserveNullAndEmptyArrays: isLeftJoin ? true : null);

                var outerParameter = resultSelectorLambda.Parameters[0];
                var outerField = AstExpression.GetField(AstExpression.RootVar, "_outer");
                var outerSymbol = context.CreateSymbol(outerParameter, outerField, outerSerializer);
                var innerParameter = resultSelectorLambda.Parameters[1];
                var innerField = AstExpression.GetField(AstExpression.RootVar, "_inner");
                var innerSymbol = context.CreateSymbol(innerParameter, innerField, innerSerializer);
                var resultSelectorContext = context.WithSymbols(outerSymbol, innerSymbol);
                var resultSelectorTranslation = ExpressionToAggregationExpressionTranslator.Translate(resultSelectorContext, resultSelectorLambda.Body);
                var (projectStage, projectSerializer) = ProjectionHelper.CreateProjectStage(resultSelectorTranslation);

                pipeline = pipeline.AddStages(
                    wrapOuterStage,
                    lookupStage,
                    unwindStage,
                    projectStage,
                    projectSerializer);

                return pipeline;
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
