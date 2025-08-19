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
using System.Reflection;
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
        private static readonly MethodInfo[] __standardDeviationMethods;
        private static readonly MethodInfo[] __standardDeviationNullableMethods;
        private static readonly MethodInfo[] __standardDeviationPopulationMethods;
        private static readonly MethodInfo[] __standardDeviationWithSelectorMethods;

        // static constructor
        static StandardDeviationMethodToExecutableQueryTranslator()
        {
            __standardDeviationMethods = new[]
            {
                MongoQueryableMethod.StandardDeviationPopulationDecimal,
                MongoQueryableMethod.StandardDeviationPopulationDecimalAsync,
                MongoQueryableMethod.StandardDeviationPopulationDecimalWithSelector,
                MongoQueryableMethod.StandardDeviationPopulationDecimalWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationDouble,
                MongoQueryableMethod.StandardDeviationPopulationDoubleAsync,
                MongoQueryableMethod.StandardDeviationPopulationDoubleWithSelector,
                MongoQueryableMethod.StandardDeviationPopulationDoubleWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationInt32,
                MongoQueryableMethod.StandardDeviationPopulationInt32Async,
                MongoQueryableMethod.StandardDeviationPopulationInt32WithSelector,
                MongoQueryableMethod.StandardDeviationPopulationInt32WithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationInt64,
                MongoQueryableMethod.StandardDeviationPopulationInt64Async,
                MongoQueryableMethod.StandardDeviationPopulationInt64WithSelector,
                MongoQueryableMethod.StandardDeviationPopulationInt64WithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationNullableDecimal,
                MongoQueryableMethod.StandardDeviationPopulationNullableDecimalAsync,
                MongoQueryableMethod.StandardDeviationPopulationNullableDecimalWithSelector,
                MongoQueryableMethod.StandardDeviationPopulationNullableDecimalWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationNullableDouble,
                MongoQueryableMethod.StandardDeviationPopulationNullableDoubleAsync,
                MongoQueryableMethod.StandardDeviationPopulationNullableDoubleWithSelector,
                MongoQueryableMethod.StandardDeviationPopulationNullableDoubleWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationNullableInt32,
                MongoQueryableMethod.StandardDeviationPopulationNullableInt32Async,
                MongoQueryableMethod.StandardDeviationPopulationNullableInt32WithSelector,
                MongoQueryableMethod.StandardDeviationPopulationNullableInt32WithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationNullableInt64,
                MongoQueryableMethod.StandardDeviationPopulationNullableInt64Async,
                MongoQueryableMethod.StandardDeviationPopulationNullableInt64WithSelector,
                MongoQueryableMethod.StandardDeviationPopulationNullableInt64WithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationNullableSingle,
                MongoQueryableMethod.StandardDeviationPopulationNullableSingleAsync,
                MongoQueryableMethod.StandardDeviationPopulationNullableSingleWithSelector,
                MongoQueryableMethod.StandardDeviationPopulationNullableSingleWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationSingle,
                MongoQueryableMethod.StandardDeviationPopulationSingleAsync,
                MongoQueryableMethod.StandardDeviationPopulationSingleWithSelector,
                MongoQueryableMethod.StandardDeviationPopulationSingleWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationSampleDecimal,
                MongoQueryableMethod.StandardDeviationSampleDecimalAsync,
                MongoQueryableMethod.StandardDeviationSampleDecimalWithSelector,
                MongoQueryableMethod.StandardDeviationSampleDecimalWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationSampleDouble,
                MongoQueryableMethod.StandardDeviationSampleDoubleAsync,
                MongoQueryableMethod.StandardDeviationSampleDoubleWithSelector,
                MongoQueryableMethod.StandardDeviationSampleDoubleWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationSampleInt32,
                MongoQueryableMethod.StandardDeviationSampleInt32Async,
                MongoQueryableMethod.StandardDeviationSampleInt32WithSelector,
                MongoQueryableMethod.StandardDeviationSampleInt32WithSelectorAsync,
                MongoQueryableMethod.StandardDeviationSampleInt64,
                MongoQueryableMethod.StandardDeviationSampleInt64Async,
                MongoQueryableMethod.StandardDeviationSampleInt64WithSelector,
                MongoQueryableMethod.StandardDeviationSampleInt64WithSelectorAsync,
                MongoQueryableMethod.StandardDeviationSampleNullableDecimal,
                MongoQueryableMethod.StandardDeviationSampleNullableDecimalAsync,
                MongoQueryableMethod.StandardDeviationSampleNullableDecimalWithSelector,
                MongoQueryableMethod.StandardDeviationSampleNullableDecimalWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationSampleNullableDouble,
                MongoQueryableMethod.StandardDeviationSampleNullableDoubleAsync,
                MongoQueryableMethod.StandardDeviationSampleNullableDoubleWithSelector,
                MongoQueryableMethod.StandardDeviationSampleNullableDoubleWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationSampleNullableInt32,
                MongoQueryableMethod.StandardDeviationSampleNullableInt32Async,
                MongoQueryableMethod.StandardDeviationSampleNullableInt32WithSelector,
                MongoQueryableMethod.StandardDeviationSampleNullableInt32WithSelectorAsync,
                MongoQueryableMethod.StandardDeviationSampleNullableInt64,
                MongoQueryableMethod.StandardDeviationSampleNullableInt64Async,
                MongoQueryableMethod.StandardDeviationSampleNullableInt64WithSelector,
                MongoQueryableMethod.StandardDeviationSampleNullableInt64WithSelectorAsync,
                MongoQueryableMethod.StandardDeviationSampleNullableSingle,
                MongoQueryableMethod.StandardDeviationSampleNullableSingleAsync,
                MongoQueryableMethod.StandardDeviationSampleNullableSingleWithSelector,
                MongoQueryableMethod.StandardDeviationSampleNullableSingleWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationSampleSingle,
                MongoQueryableMethod.StandardDeviationSampleSingleAsync,
                MongoQueryableMethod.StandardDeviationSampleSingleWithSelector,
                MongoQueryableMethod.StandardDeviationSampleSingleWithSelectorAsync
            };

            __standardDeviationNullableMethods = new[]
            {
                MongoQueryableMethod.StandardDeviationPopulationNullableDecimal,
                MongoQueryableMethod.StandardDeviationPopulationNullableDecimalAsync,
                MongoQueryableMethod.StandardDeviationPopulationNullableDecimalWithSelector,
                MongoQueryableMethod.StandardDeviationPopulationNullableDecimalWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationNullableDouble,
                MongoQueryableMethod.StandardDeviationPopulationNullableDoubleAsync,
                MongoQueryableMethod.StandardDeviationPopulationNullableDoubleWithSelector,
                MongoQueryableMethod.StandardDeviationPopulationNullableDoubleWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationNullableInt32,
                MongoQueryableMethod.StandardDeviationPopulationNullableInt32Async,
                MongoQueryableMethod.StandardDeviationPopulationNullableInt32WithSelector,
                MongoQueryableMethod.StandardDeviationPopulationNullableInt32WithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationNullableInt64,
                MongoQueryableMethod.StandardDeviationPopulationNullableInt64Async,
                MongoQueryableMethod.StandardDeviationPopulationNullableInt64WithSelector,
                MongoQueryableMethod.StandardDeviationPopulationNullableInt64WithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationNullableSingle,
                MongoQueryableMethod.StandardDeviationPopulationNullableSingleAsync,
                MongoQueryableMethod.StandardDeviationPopulationNullableSingleWithSelector,
                MongoQueryableMethod.StandardDeviationPopulationNullableSingleWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationSampleNullableDecimal,
                MongoQueryableMethod.StandardDeviationSampleNullableDecimalAsync,
                MongoQueryableMethod.StandardDeviationSampleNullableDecimalWithSelector,
                MongoQueryableMethod.StandardDeviationSampleNullableDecimalWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationSampleNullableDouble,
                MongoQueryableMethod.StandardDeviationSampleNullableDoubleAsync,
                MongoQueryableMethod.StandardDeviationSampleNullableDoubleWithSelector,
                MongoQueryableMethod.StandardDeviationSampleNullableDoubleWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationSampleNullableInt32,
                MongoQueryableMethod.StandardDeviationSampleNullableInt32Async,
                MongoQueryableMethod.StandardDeviationSampleNullableInt32WithSelector,
                MongoQueryableMethod.StandardDeviationSampleNullableInt32WithSelectorAsync,
                MongoQueryableMethod.StandardDeviationSampleNullableInt64,
                MongoQueryableMethod.StandardDeviationSampleNullableInt64Async,
                MongoQueryableMethod.StandardDeviationSampleNullableInt64WithSelector,
                MongoQueryableMethod.StandardDeviationSampleNullableInt64WithSelectorAsync,
                MongoQueryableMethod.StandardDeviationSampleNullableSingle,
                MongoQueryableMethod.StandardDeviationSampleNullableSingleAsync,
                MongoQueryableMethod.StandardDeviationSampleNullableSingleWithSelector,
                MongoQueryableMethod.StandardDeviationSampleNullableSingleWithSelectorAsync
            };

            __standardDeviationPopulationMethods = new[]
            {
                MongoQueryableMethod.StandardDeviationPopulationDecimal,
                MongoQueryableMethod.StandardDeviationPopulationDecimalAsync,
                MongoQueryableMethod.StandardDeviationPopulationDecimalWithSelector,
                MongoQueryableMethod.StandardDeviationPopulationDecimalWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationDouble,
                MongoQueryableMethod.StandardDeviationPopulationDoubleAsync,
                MongoQueryableMethod.StandardDeviationPopulationDoubleWithSelector,
                MongoQueryableMethod.StandardDeviationPopulationDoubleWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationInt32,
                MongoQueryableMethod.StandardDeviationPopulationInt32Async,
                MongoQueryableMethod.StandardDeviationPopulationInt32WithSelector,
                MongoQueryableMethod.StandardDeviationPopulationInt32WithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationInt64,
                MongoQueryableMethod.StandardDeviationPopulationInt64Async,
                MongoQueryableMethod.StandardDeviationPopulationInt64WithSelector,
                MongoQueryableMethod.StandardDeviationPopulationInt64WithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationNullableDecimal,
                MongoQueryableMethod.StandardDeviationPopulationNullableDecimalAsync,
                MongoQueryableMethod.StandardDeviationPopulationNullableDecimalWithSelector,
                MongoQueryableMethod.StandardDeviationPopulationNullableDecimalWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationNullableDouble,
                MongoQueryableMethod.StandardDeviationPopulationNullableDoubleAsync,
                MongoQueryableMethod.StandardDeviationPopulationNullableDoubleWithSelector,
                MongoQueryableMethod.StandardDeviationPopulationNullableDoubleWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationNullableInt32,
                MongoQueryableMethod.StandardDeviationPopulationNullableInt32Async,
                MongoQueryableMethod.StandardDeviationPopulationNullableInt32WithSelector,
                MongoQueryableMethod.StandardDeviationPopulationNullableInt32WithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationNullableInt64,
                MongoQueryableMethod.StandardDeviationPopulationNullableInt64Async,
                MongoQueryableMethod.StandardDeviationPopulationNullableInt64WithSelector,
                MongoQueryableMethod.StandardDeviationPopulationNullableInt64WithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationNullableSingle,
                MongoQueryableMethod.StandardDeviationPopulationNullableSingleAsync,
                MongoQueryableMethod.StandardDeviationPopulationNullableSingleWithSelector,
                MongoQueryableMethod.StandardDeviationPopulationNullableSingleWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationSingle,
                MongoQueryableMethod.StandardDeviationPopulationSingleAsync,
                MongoQueryableMethod.StandardDeviationPopulationSingleWithSelector,
                MongoQueryableMethod.StandardDeviationPopulationSingleWithSelectorAsync
            };

            __standardDeviationWithSelectorMethods = new[]
            {
                MongoQueryableMethod.StandardDeviationPopulationDecimalWithSelector,
                MongoQueryableMethod.StandardDeviationPopulationDecimalWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationDoubleWithSelector,
                MongoQueryableMethod.StandardDeviationPopulationDoubleWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationInt32WithSelector,
                MongoQueryableMethod.StandardDeviationPopulationInt32WithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationInt64WithSelector,
                MongoQueryableMethod.StandardDeviationPopulationInt64WithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationNullableDecimalWithSelector,
                MongoQueryableMethod.StandardDeviationPopulationNullableDecimalWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationNullableDoubleWithSelector,
                MongoQueryableMethod.StandardDeviationPopulationNullableDoubleWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationNullableInt32WithSelector,
                MongoQueryableMethod.StandardDeviationPopulationNullableInt32WithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationNullableInt64WithSelector,
                MongoQueryableMethod.StandardDeviationPopulationNullableInt64WithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationNullableSingleWithSelector,
                MongoQueryableMethod.StandardDeviationPopulationNullableSingleWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationPopulationSingleWithSelector,
                MongoQueryableMethod.StandardDeviationPopulationSingleWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationSampleDecimalWithSelector,
                MongoQueryableMethod.StandardDeviationSampleDecimalWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationSampleDoubleWithSelector,
                MongoQueryableMethod.StandardDeviationSampleDoubleWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationSampleInt32WithSelector,
                MongoQueryableMethod.StandardDeviationSampleInt32WithSelectorAsync,
                MongoQueryableMethod.StandardDeviationSampleInt64WithSelector,
                MongoQueryableMethod.StandardDeviationSampleInt64WithSelectorAsync,
                MongoQueryableMethod.StandardDeviationSampleNullableDecimalWithSelector,
                MongoQueryableMethod.StandardDeviationSampleNullableDecimalWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationSampleNullableDoubleWithSelector,
                MongoQueryableMethod.StandardDeviationSampleNullableDoubleWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationSampleNullableInt32WithSelector,
                MongoQueryableMethod.StandardDeviationSampleNullableInt32WithSelectorAsync,
                MongoQueryableMethod.StandardDeviationSampleNullableInt64WithSelector,
                MongoQueryableMethod.StandardDeviationSampleNullableInt64WithSelectorAsync,
                MongoQueryableMethod.StandardDeviationSampleNullableSingleWithSelector,
                MongoQueryableMethod.StandardDeviationSampleNullableSingleWithSelectorAsync,
                MongoQueryableMethod.StandardDeviationSampleSingleWithSelector,
                MongoQueryableMethod.StandardDeviationSampleSingleWithSelectorAsync
            };
        }

        // public static methods
        public static ExecutableQuery<TDocument, TOutput> Translate<TDocument>(MongoQueryProvider<TDocument> provider, TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__standardDeviationMethods))
            {
                var sourceExpression = arguments[0];
                var pipeline = ExpressionToPipelineTranslator.Translate(context, sourceExpression);
                ClientSideProjectionHelper.ThrowIfClientSideProjection(expression, pipeline, method);

                var sourceSerializer = pipeline.OutputSerializer;
                var stdDevOperator = method.IsOneOf(__standardDeviationPopulationMethods) ? AstUnaryAccumulatorOperator.StdDevPop : AstUnaryAccumulatorOperator.StdDevSamp;
                AstExpression valueAst;
                if (method.IsOneOf(__standardDeviationWithSelectorMethods))
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
                var outputValueSerializer = context.SerializationDomain.LookupSerializer(outputValueType);
                var outputWrappedValueSerializer = WrappedValueSerializer.Create("_v", outputValueSerializer);

                pipeline = pipeline.AddStages(
                    AstStage.Group(
                        id: BsonNull.Value,
                        AstExpression.AccumulatorField("_v", stdDevOperator, valueAst)),
                    AstStage.Project(AstProject.ExcludeId()),
                    outputWrappedValueSerializer);

                var finalizer = method.IsOneOf(__standardDeviationNullableMethods) ? __singleOrDefaultFinalizer : __singleFinalizer;

                return ExecutableQuery.Create(
                    provider,
                    pipeline,
                    finalizer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
