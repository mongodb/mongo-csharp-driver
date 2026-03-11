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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToExecutableQueryTranslators.Finalizers;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToPipelineTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToExecutableQueryTranslators
{
    internal static class StandardDeviationMethodToExecutableQueryTranslator<TOutput>
    {
        // private static fields
        private static IExecutableQueryFinalizer<TOutput, TOutput> __singleFinalizer = new SingleFinalizer<TOutput>();
        private static IExecutableQueryFinalizer<TOutput, TOutput> __singleOrDefaultFinalizer = new SingleOrDefaultFinalizer<TOutput>();

        // public static methods
        public static ExecutableQuery<TDocument, TOutput> Translate<TDocument>(MongoQueryProvider<TDocument> provider, TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(MongoQueryableMethod.StandardDeviationOverloads))
            {
                var sourceExpression = arguments[0];
                var pipeline = ExpressionToPipelineTranslator.Translate(context, sourceExpression);
                ClientSideProjectionHelper.ThrowIfClientSideProjection(expression, pipeline, method);

                var sourceSerializer = pipeline.OutputSerializer;
                var stdDevOperator = method.IsOneOf(MongoQueryableMethod.StandardDeviationPopulationOverloads) ? AstUnaryAccumulatorOperator.StdDevPop : AstUnaryAccumulatorOperator.StdDevSamp;
                AstExpression valueAst;
                if (method.IsOneOf(MongoQueryableMethod.StandardDeviationWithSelectorOverloads))
                {
                    var selectorLambda = ExpressionHelper.UnquoteLambda(arguments[1]);
                    var selectorTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, selectorLambda, sourceSerializer, asRoot: true);
                    valueAst = selectorTranslation.Ast;
                }
                else
                {
                    valueAst = AstExpression.GetField(AstExpression.RootVar, "_v");
                }
                var outputValueType = expression.GetResultType();
                var outputValueSerializer = BsonSerializer.LookupSerializer(outputValueType);
                var outputWrappedValueSerializer = WrappedValueSerializer.Create("_v", outputValueSerializer);

                pipeline = pipeline.AddStages(
                    AstStage.Group(
                        id: BsonNull.Value,
                        AstExpression.AccumulatorField("_v", stdDevOperator, valueAst)),
                    AstStage.Project(AstProject.ExcludeId()),
                    outputWrappedValueSerializer);

                var finalizer = method.IsOneOf(MongoQueryableMethod.StandardDeviationNullableOverloads) ? __singleOrDefaultFinalizer : __singleFinalizer;

                return ExecutableQuery.Create(
                    provider,
                    pipeline,
                    finalizer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
