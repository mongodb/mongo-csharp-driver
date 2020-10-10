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
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq3.Ast;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Ast.Stages;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Serializers;
using MongoDB.Driver.Linq3.Translators.ExpressionTranslators;
using MongoDB.Driver.Linq3.Translators.PipelineTranslators;
using MongoDB.Driver.Linq3.Translators.QueryTranslators.Finalizers;

namespace MongoDB.Driver.Linq3.Translators.QueryTranslators
{
    public static class SumQueryTranslator<TOutput>
    {
        // private static fields
        private static readonly IExecutableQueryFinalizer<TOutput, TOutput> __finalizer = new SingleOrDefaultFinalizer<TOutput>();
        private static readonly MethodInfo[] __sumAsyncMethods;
        private static readonly MethodInfo[] __sumMethods;
        private static readonly MethodInfo[] __sumWithSelectorMethods;

        // static constructor
        static SumQueryTranslator()
        {
            __sumMethods = new[]
            {
                QueryableMethod.SumDecimal,
                QueryableMethod.SumDecimalWithSelector,
                QueryableMethod.SumDouble,
                QueryableMethod.SumDoubleWithSelector,
                QueryableMethod.SumInt32,
                QueryableMethod.SumInt32WithSelector,
                QueryableMethod.SumInt64,
                QueryableMethod.SumInt64WithSelector,
                QueryableMethod.SumNullableDecimal,
                QueryableMethod.SumNullableDecimalWithSelector,
                QueryableMethod.SumNullableDouble,
                QueryableMethod.SumNullableDoubleWithSelector,
                QueryableMethod.SumNullableInt32,
                QueryableMethod.SumNullableInt32WithSelector,
                QueryableMethod.SumNullableInt64,
                QueryableMethod.SumNullableInt64WithSelector,
                QueryableMethod.SumNullableSingle,
                QueryableMethod.SumNullableSingleWithSelector,
                QueryableMethod.SumSingle,
                QueryableMethod.SumSingleWithSelector,
                MongoQueryableMethod.SumDecimalAsync,
                MongoQueryableMethod.SumDecimalWithSelectorAsync,
                MongoQueryableMethod.SumDoubleAsync,
                MongoQueryableMethod.SumDoubleWithSelectorAsync,
                MongoQueryableMethod.SumInt32Async,
                MongoQueryableMethod.SumInt32WithSelectorAsync,
                MongoQueryableMethod.SumInt64Async,
                MongoQueryableMethod.SumInt64WithSelectorAsync,
                MongoQueryableMethod.SumNullableDecimalAsync,
                MongoQueryableMethod.SumNullableDecimalWithSelectorAsync,
                MongoQueryableMethod.SumNullableDoubleAsync,
                MongoQueryableMethod.SumNullableDoubleWithSelectorAsync,
                MongoQueryableMethod.SumNullableInt32Async,
                MongoQueryableMethod.SumNullableInt32WithSelectorAsync,
                MongoQueryableMethod.SumNullableInt64Async,
                MongoQueryableMethod.SumNullableInt64WithSelectorAsync,
                MongoQueryableMethod.SumNullableSingleAsync,
                MongoQueryableMethod.SumNullableSingleWithSelectorAsync,
                MongoQueryableMethod.SumSingleAsync,
                MongoQueryableMethod.SumSingleWithSelectorAsync
            };

            __sumWithSelectorMethods = new[]
            {
                QueryableMethod.SumDecimalWithSelector,
                QueryableMethod.SumDoubleWithSelector,
                QueryableMethod.SumInt32WithSelector,
                QueryableMethod.SumInt64WithSelector,
                QueryableMethod.SumNullableDecimalWithSelector,
                QueryableMethod.SumNullableDoubleWithSelector,
                QueryableMethod.SumNullableInt32WithSelector,
                QueryableMethod.SumNullableInt64WithSelector,
                QueryableMethod.SumNullableSingleWithSelector,
                QueryableMethod.SumSingleWithSelector,
                MongoQueryableMethod.SumDecimalWithSelectorAsync,
                MongoQueryableMethod.SumDoubleWithSelectorAsync,
                MongoQueryableMethod.SumInt32WithSelectorAsync,
                MongoQueryableMethod.SumInt64WithSelectorAsync,
                MongoQueryableMethod.SumNullableDecimalWithSelectorAsync,
                MongoQueryableMethod.SumNullableDoubleWithSelectorAsync,
                MongoQueryableMethod.SumNullableInt32WithSelectorAsync,
                MongoQueryableMethod.SumNullableInt64WithSelectorAsync,
                MongoQueryableMethod.SumNullableSingleWithSelectorAsync,
                MongoQueryableMethod.SumSingleWithSelectorAsync
            };

            __sumAsyncMethods = new[]
            {
                MongoQueryableMethod.SumDecimalAsync,
                MongoQueryableMethod.SumDecimalWithSelectorAsync,
                MongoQueryableMethod.SumDoubleAsync,
                MongoQueryableMethod.SumDoubleWithSelectorAsync,
                MongoQueryableMethod.SumInt32Async,
                MongoQueryableMethod.SumInt32WithSelectorAsync,
                MongoQueryableMethod.SumInt64Async,
                MongoQueryableMethod.SumInt64WithSelectorAsync,
                MongoQueryableMethod.SumNullableDecimalAsync,
                MongoQueryableMethod.SumNullableDecimalWithSelectorAsync,
                MongoQueryableMethod.SumNullableDoubleAsync,
                MongoQueryableMethod.SumNullableDoubleWithSelectorAsync,
                MongoQueryableMethod.SumNullableInt32Async,
                MongoQueryableMethod.SumNullableInt32WithSelectorAsync,
                MongoQueryableMethod.SumNullableInt64Async,
                MongoQueryableMethod.SumNullableInt64WithSelectorAsync,
                MongoQueryableMethod.SumNullableSingleAsync,
                MongoQueryableMethod.SumNullableSingleWithSelectorAsync,
                MongoQueryableMethod.SumSingleAsync,
                MongoQueryableMethod.SumSingleWithSelectorAsync
            };
        }

        // public static methods
        public static ExecutableQuery<TDocument, TOutput> Translate<TDocument>(MongoQueryProvider<TDocument> provider, TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__sumMethods))
            { 
                var source = arguments[0];
                var pipeline = PipelineTranslator.Translate(context, source);

                AstExpression arg;
                if (method.IsOneOf(__sumWithSelectorMethods))
                {
                    var selectorLambda = ExpressionHelper.Unquote(arguments[1]);
                    var selectorTranslation = ExpressionTranslator.Translate(context, selectorLambda, pipeline.OutputSerializer);
                    arg = selectorTranslation.Ast;
                }
                else
                {
                    Throw.If(!(pipeline.OutputSerializer is IWrappedValueSerializer), "Expected pipeline.OutputSerializer to be an IWrappedValueSerializer.", nameof(pipeline));
                    arg = new AstFieldExpression("$_v");
                }

                var outputValueType = method.IsOneOf(__sumAsyncMethods) ? expression.Type.GetGenericArguments()[0] : expression.Type;
                var outputValueSerializer = BsonSerializer.LookupSerializer(outputValueType);
                var outputWrappedValueSerializer = WrappedValueSerializer.Create(outputValueSerializer);

                pipeline.AddStages(
                    outputWrappedValueSerializer,
                    new AstGroupStage(
                        id: BsonNull.Value,
                        fields: new AstComputedField("_v", new AstUnaryExpression(AstUnaryOperator.Sum, arg))),
                    new AstProjectStage(new AstProjectStageExcludeIdSpecification()));

                return new ExecutableQuery<TDocument, TOutput, TOutput>(
                    provider.Collection,
                    provider.Options,
                    pipeline.ToPipelineDefinition<TDocument, TOutput>(),
                    __finalizer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
