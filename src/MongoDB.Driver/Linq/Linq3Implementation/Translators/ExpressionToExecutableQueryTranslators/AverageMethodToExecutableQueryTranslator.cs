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
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
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
    internal static class AverageMethodToExecutableQueryTranslator<TOutput>
    {
        // private static fields
        private static readonly IReadOnlyMethodInfoSet __averageOverloads;
        private static readonly IReadOnlyMethodInfoSet __averageWithSelectorOverloads;
        private static readonly IExecutableQueryFinalizer<TOutput, TOutput> __singleFinalizer = new SingleFinalizer<TOutput>();
        private static readonly IExecutableQueryFinalizer<TOutput, TOutput> __singleOrDefaultFinalizer = new SingleOrDefaultFinalizer<TOutput>();

        // static constructor
        static AverageMethodToExecutableQueryTranslator()
        {
            __averageOverloads = MethodInfoSet.Create(
            [
                QueryableMethod.AverageOverloads,
                MongoQueryableMethod.AverageOverloads
            ]);

            __averageWithSelectorOverloads = MethodInfoSet.Create(
            [
                QueryableMethod.AverageWithSelectorOverloads,
                MongoQueryableMethod.AverageWithSelectorOverloads
           ]);
        }

        // public static methods
        public static ExecutableQuery<TDocument, TOutput> Translate<TDocument>(MongoQueryProvider<TDocument> provider, TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__averageOverloads))
            {
                var sourceExpression = arguments[0];
                var pipeline = ExpressionToPipelineTranslator.Translate(context, sourceExpression);
                ClientSideProjectionHelper.ThrowIfClientSideProjection(expression, pipeline, method);

                var sourceSerializer = pipeline.OutputSerializer;
                AstExpression valueExpression;
                if (method.IsOneOf(__averageWithSelectorOverloads))
                {
                    var selectorLambda = ExpressionHelper.UnquoteLambda(arguments[1]);
                    var selectorTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, selectorLambda, sourceSerializer, asRoot: true);
                    valueExpression = selectorTranslation.Ast;
                }
                else
                {
                    Ensure.That(sourceSerializer is IWrappedValueSerializer, "Expected sourceSerializer to be an IWrappedValueSerializer.", nameof(sourceSerializer));
                    valueExpression = AstExpression.GetField(AstExpression.RootVar, "_v");
                }

                IBsonSerializer outputValueSerializer = expression.GetResultType() switch
                {
                    Type t when t == typeof(int) => Int32Serializer.Instance,
                    Type t when t == typeof(long) => Int64Serializer.Instance,
                    Type t when t == typeof(float) => SingleSerializer.Instance,
                    Type t when t == typeof(double) => DoubleSerializer.Instance,
                    Type t when t == typeof(decimal) => DecimalSerializer.Instance,
                    Type { IsConstructedGenericType: true } t when t.GetGenericTypeDefinition() == typeof(Nullable<>) => (IBsonSerializer)Activator.CreateInstance(typeof(NullableSerializer<>).MakeGenericType(t.GenericTypeArguments[0])),
                    _ => throw new ExpressionNotSupportedException(expression)
                };
                var outputWrappedValueSerializer = WrappedValueSerializer.Create("_v", outputValueSerializer);

                pipeline = pipeline.AddStages(
                    AstStage.Group(
                        id: BsonNull.Value,
                        fields: AstExpression.AccumulatorField("_v", AstUnaryAccumulatorOperator.Avg, valueExpression)),
                    AstStage.Project(AstProject.ExcludeId()),
                    outputWrappedValueSerializer);

                var returnType = expression.Type;

                return ExecutableQuery.Create(
                    provider,
                    pipeline,
                    returnType.IsNullable() // Note: numeric types are never reference types
                        ? __singleOrDefaultFinalizer
                        : __singleFinalizer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
