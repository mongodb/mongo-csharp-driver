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
using MongoDB.Driver.Linq3.Ast;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Ast.Stages;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Serializers;
using MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators;
using MongoDB.Driver.Linq3.Translators.ExpressionToPipelineTranslators;
using MongoDB.Driver.Linq3.Translators.ExpressionToExecutableQueryTranslators.Finalizers;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToExecutableQueryTranslators
{
    public static class MaxMethodToExecutableQueryTranslator<TOutput>
    {
        // private static fields
        private static readonly IExecutableQueryFinalizer<TOutput, TOutput> __finalizer = new SingleFinalizer<TOutput>();
        private static readonly MethodInfo[] __maxMethods;
        private static readonly MethodInfo[] __maxWithSelectorMethods;

        // static constructor
        static MaxMethodToExecutableQueryTranslator()
        {
            __maxMethods = new[]
            {
                QueryableMethod.Max,
                QueryableMethod.MaxWithSelector,
                MongoQueryableMethod.MaxAsync,
                MongoQueryableMethod.MaxWithSelectorAsync,
            };

            __maxWithSelectorMethods = new[]
            {
                QueryableMethod.MaxWithSelector,
                MongoQueryableMethod.MaxWithSelectorAsync,
            };
        }

        // public static methods
        public static ExecutableQuery<TDocument, TOutput> Translate<TDocument>(MongoQueryProvider<TDocument> provider, TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__maxMethods))
            {
                var source = arguments[0];
                var pipeline = ExpressionToPipelineTranslator.Translate(context, source);
                var sourceSerializer = pipeline.OutputSerializer;

                AstExpression maxArgument;
                IBsonSerializer maxSerializer;
                if (method.IsOneOf(__maxWithSelectorMethods))
                {
                    var selectorExpression = ExpressionHelper.Unquote(arguments[1]);
                    var selectorTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, selectorExpression, sourceSerializer, asCurrentSymbol: true);
                    if (selectorTranslation.Serializer is IBsonDocumentSerializer)
                    {
                        maxArgument = selectorTranslation.Ast;
                        maxSerializer = selectorTranslation.Serializer;
                    }
                    else
                    {
                        maxArgument = AstExpression.ComputedDocument(new[] { AstExpression.ComputedField("_v", selectorTranslation.Ast) });
                        maxSerializer = WrappedValueSerializer.Create(selectorTranslation.Serializer);
                    }
                }
                else
                {
                    maxArgument = AstExpression.Field("$ROOT");
                    maxSerializer = pipeline.OutputSerializer;
                }

                pipeline.AddStages(
                    maxSerializer,
                    AstStage.Group(
                        id: BsonNull.Value,
                        fields: AstExpression.ComputedField("_max", AstExpression.Max(maxArgument))),
                    AstStage.ReplaceRoot(AstExpression.Field("_max")));

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
