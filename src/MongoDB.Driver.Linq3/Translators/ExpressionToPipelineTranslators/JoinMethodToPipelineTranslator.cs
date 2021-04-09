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

using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq3.Ast;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Ast.Stages;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Serializers;
using MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToPipelineTranslators
{
    public static class JoinMethodToPipelineTranslator
    {
        // public static methods
        public static Pipeline Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.Is(QueryableMethod.Join))
            {
                var outerExpression = arguments[0];
                var pipeline = ExpressionToPipelineTranslator.Translate(context, outerExpression);
                var outerSerializer = pipeline.OutputSerializer;

                var wrapOuterStage = AstStage.Project(
                    new AstProjectStageComputedFieldSpecification(AstExpression.ComputedField("_outer", AstExpression.Field("$ROOT"))),
                    new AstProjectStageExcludeIdSpecification());
                var wrappedOuterSerializer = WrappedValueSerializer.Create("_outer", outerSerializer);

                var innerExpression = arguments[1];
                var innerQueryProvider = GetMongoQueryProvider(innerExpression);
                var innerSerializer = innerQueryProvider.DocumentSerializer;

                var outerKeySelectorLambda = ExpressionHelper.Unquote(arguments[2]);
                var localFieldPath = GetLocalFieldPath(context, outerKeySelectorLambda, wrappedOuterSerializer);

                var innerKeySelectorLambda = ExpressionHelper.Unquote(arguments[3]);
                var foreignFieldPath = GetForeignFieldPath(context, innerKeySelectorLambda, innerSerializer);

                var lookupStage = AstStage.Lookup(
                    from: innerQueryProvider.CollectionName,
                    match: new AstLookupStageEqualityMatch(localFieldPath, foreignFieldPath),
                    @as: "_inner");

                var unwindStage = AstStage.Unwind("_inner");

                var resultSelectorLambda = ExpressionHelper.Unquote(arguments[4]);
                var outerParameter = resultSelectorLambda.Parameters[0];
                var outerSymbol = new Symbol("_outer", outerSerializer);
                var innerParameter = resultSelectorLambda.Parameters[1];
                var innerSymbol = new Symbol("_inner", innerSerializer);
                var resultSelectorContext = context
                    .WithSymbol(outerParameter, outerSymbol)
                    .WithSymbol(innerParameter, innerSymbol);
                var resultSelectorTranslation = ExpressionToAggregationExpressionTranslator.Translate(resultSelectorContext, resultSelectorLambda.Body);
                var (projectStage, newOutputSerializer) = ProjectionHelper.CreateProjectStage(pipeline, resultSelectorTranslation);

                pipeline.AddStages(
                    newOutputSerializer,
                    wrapOuterStage,
                    lookupStage,
                    unwindStage,
                    projectStage);

                return pipeline;
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static string GetForeignFieldPath(TranslationContext context, LambdaExpression innerKeySelectorLambda, IBsonSerializer innerSerializer)
        {
            var innerKeySelectorTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, innerKeySelectorLambda, innerSerializer, asCurrentSymbol: true);
            if (innerKeySelectorTranslation.Ast is AstFieldExpression fieldExpression)
            {
                return fieldExpression.Path;
            }

            throw new ExpressionNotSupportedException(innerKeySelectorLambda);
        }

        private static string GetLocalFieldPath(TranslationContext context, LambdaExpression outerKeySelectorLambda, IBsonSerializer outerSerializer)
        {
            var outerKeySelectorTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, outerKeySelectorLambda, outerSerializer, asCurrentSymbol: true);
            if (outerKeySelectorTranslation.Ast is AstFieldExpression fieldExpression)
            {
                return fieldExpression.Path;
            }

            throw new ExpressionNotSupportedException(outerKeySelectorLambda);
        }

        private static MongoQueryProvider GetMongoQueryProvider(Expression expression)
        {
            if (expression is ConstantExpression constantExpression &&
                constantExpression.Value is IQueryable queryable &&
                queryable.Provider is MongoQueryProvider mongoQueryProvider)
            {
                return mongoQueryProvider;
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
