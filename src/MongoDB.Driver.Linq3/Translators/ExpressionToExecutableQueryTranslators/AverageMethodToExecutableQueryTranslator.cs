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
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Ast.Stages;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Reflection;
using MongoDB.Driver.Linq3.Serializers;
using MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators;
using MongoDB.Driver.Linq3.Translators.ExpressionToExecutableQueryTranslators.Finalizers;
using MongoDB.Driver.Linq3.Translators.ExpressionToPipelineTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToExecutableQueryTranslators
{
    public static class AverageMethodToExecutableQueryTranslator<TOutput>
    {
        // private static fields
        private static readonly MethodInfo[] __averageMethods;
        private static readonly MethodInfo[] __averageWithSelectorMethods;
        private static readonly IExecutableQueryFinalizer<TOutput, TOutput> __finalizer = new SingleFinalizer<TOutput>();

        // static constructor
        static AverageMethodToExecutableQueryTranslator()
        {
            __averageMethods = new[]
            {
                QueryableMethod.AverageDecimal,
                QueryableMethod.AverageDecimalWithSelector,
                QueryableMethod.AverageDouble,
                QueryableMethod.AverageDoubleWithSelector,
                QueryableMethod.AverageInt32,
                QueryableMethod.AverageInt32WithSelector,
                QueryableMethod.AverageInt64,
                QueryableMethod.AverageInt64WithSelector,
                QueryableMethod.AverageNullableDecimal,
                QueryableMethod.AverageNullableDecimalWithSelector,
                QueryableMethod.AverageNullableDouble,
                QueryableMethod.AverageNullableDoubleWithSelector,
                QueryableMethod.AverageNullableInt32,
                QueryableMethod.AverageNullableInt32WithSelector,
                QueryableMethod.AverageNullableInt64,
                QueryableMethod.AverageNullableInt64WithSelector,
                QueryableMethod.AverageNullableSingle,
                QueryableMethod.AverageNullableSingleWithSelector,
                QueryableMethod.AverageSingle,
                QueryableMethod.AverageSingleWithSelector,
                MongoQueryableMethod.AverageDecimalAsync,
                MongoQueryableMethod.AverageDecimalWithSelectorAsync,
                MongoQueryableMethod.AverageDoubleAsync,
                MongoQueryableMethod.AverageDoubleWithSelectorAsync,
                MongoQueryableMethod.AverageInt32Async,
                MongoQueryableMethod.AverageInt32WithSelectorAsync,
                MongoQueryableMethod.AverageInt64Async,
                MongoQueryableMethod.AverageInt64WithSelectorAsync,
                MongoQueryableMethod.AverageNullableDecimalAsync,
                MongoQueryableMethod.AverageNullableDecimalWithSelectorAsync,
                MongoQueryableMethod.AverageNullableDoubleAsync,
                MongoQueryableMethod.AverageNullableDoubleWithSelectorAsync,
                MongoQueryableMethod.AverageNullableInt32Async,
                MongoQueryableMethod.AverageNullableInt32WithSelectorAsync,
                MongoQueryableMethod.AverageNullableInt64Async,
                MongoQueryableMethod.AverageNullableInt64WithSelectorAsync,
                MongoQueryableMethod.AverageNullableSingleAsync,
                MongoQueryableMethod.AverageNullableSingleWithSelectorAsync,
                MongoQueryableMethod.AverageSingleAsync,
                MongoQueryableMethod.AverageSingleWithSelectorAsync
            };

            __averageWithSelectorMethods = new[]
            {
                QueryableMethod.AverageDecimalWithSelector,
                QueryableMethod.AverageDoubleWithSelector,
                QueryableMethod.AverageInt32WithSelector,
                QueryableMethod.AverageInt64WithSelector,
                QueryableMethod.AverageNullableDecimalWithSelector,
                QueryableMethod.AverageNullableDoubleWithSelector,
                QueryableMethod.AverageNullableInt32WithSelector,
                QueryableMethod.AverageNullableInt64WithSelector,
                QueryableMethod.AverageNullableSingleWithSelector,
                QueryableMethod.AverageSingleWithSelector,
                MongoQueryableMethod.AverageDecimalWithSelectorAsync,
                MongoQueryableMethod.AverageDoubleWithSelectorAsync,
                MongoQueryableMethod.AverageInt32WithSelectorAsync,
                MongoQueryableMethod.AverageInt64WithSelectorAsync,
                MongoQueryableMethod.AverageNullableDecimalWithSelectorAsync,
                MongoQueryableMethod.AverageNullableDoubleWithSelectorAsync,
                MongoQueryableMethod.AverageNullableInt32WithSelectorAsync,
                MongoQueryableMethod.AverageNullableInt64WithSelectorAsync,
                MongoQueryableMethod.AverageNullableSingleWithSelectorAsync,
                MongoQueryableMethod.AverageSingleWithSelectorAsync
           };
        }

        // public static methods
        public static ExecutableQuery<TDocument, TOutput> Translate<TDocument>(MongoQueryProvider<TDocument> provider, TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__averageMethods))
            {
                var sourceExpression = arguments[0];
                var pipeline = ExpressionToPipelineTranslator.Translate(context, sourceExpression);
                var sourceSerializer = pipeline.OutputSerializer;

                AstExpression valueExpression;
                if (method.IsOneOf(__averageWithSelectorMethods))
                {
                    var selectorLambda = ExpressionHelper.UnquoteLambda(arguments[1]);
                    var selectorTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, selectorLambda, sourceSerializer, asCurrentSymbol: true);
                    valueExpression = selectorTranslation.Ast;
                }
                else
                {
                    Ensure.That(sourceSerializer is IWrappedValueSerializer, "Expected sourceSerializer to be an IWrappedValueSerializer.", nameof(sourceSerializer));
                    valueExpression = AstExpression.Field("_v");
                }

                var outputValueType = GetValueType(expression);
                var outputValueSerializer = BsonSerializer.LookupSerializer(outputValueType); // TODO: use known serializer
                var outputWrappedValueSerializer = WrappedValueSerializer.Create(outputValueSerializer);

                pipeline = pipeline.AddStages(
                    outputWrappedValueSerializer,
                    AstStage.Group(
                        id: BsonNull.Value,
                        fields: AstExpression.ComputedField("_v", AstUnaryExpression.Avg(valueExpression))),
                    AstStage.Project(AstProject.ExcludeId()));

                return new ExecutableQuery<TDocument, TOutput, TOutput>(
                    provider.Collection,
                    provider.Options,
                    pipeline,
                    __finalizer);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static Type GetValueType(Expression expression)
        {
            var expressionType = expression.Type;
            if (expressionType.IsConstructedGenericType && expressionType.GetGenericTypeDefinition() == typeof (Task<>))
            {
                return expressionType.GetGenericArguments()[0];
            }
            else
            {
                return expressionType;
            }
        }
    }
}
