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
    public static class MinMethodToExecutableQueryTranslator<TOutput>
    {
        // private static fields
        private static readonly IExecutableQueryFinalizer<TOutput, TOutput> __finalizer = new SingleFinalizer<TOutput>();
        private static readonly MethodInfo[] __minMethods;
        private static readonly MethodInfo[] __minWithSelectorMethods;

        // static constructor
        static MinMethodToExecutableQueryTranslator()
        {
            __minMethods = new[]
            {
                QueryableMethod.Min,
                QueryableMethod.MinWithSelector,
                MongoQueryableMethod.MinAsync,
                MongoQueryableMethod.MinWithSelectorAsync,
            };

            __minWithSelectorMethods = new[]
            {
                QueryableMethod.MinWithSelector,
                MongoQueryableMethod.MinWithSelectorAsync,
            };
        }

        // public static methods
        public static ExecutableQuery<TDocument, TOutput> Translate<TDocument>(MongoQueryProvider<TDocument> provider, TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__minMethods))
            {
                var source = arguments[0];
                var pipeline = ExpressionToPipelineTranslator.Translate(context, source);
                var sourceSerializer = pipeline.OutputSerializer;

                AstExpression minArgument;
                IBsonSerializer minSerializer;
                if (method.IsOneOf(__minWithSelectorMethods))
                {
                    var selectorExpression = ExpressionHelper.Unquote(arguments[1]);
                    var selectorTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, selectorExpression, sourceSerializer, asCurrentSymbol: true);
                    if (selectorTranslation.Serializer is IBsonDocumentSerializer)
                    {
                        minArgument = selectorTranslation.Ast;
                        minSerializer = selectorTranslation.Serializer;
                    }
                    else
                    {
                        minArgument = AstExpression.ComputedDocument(new[] { AstExpression.ComputedField("_v", selectorTranslation.Ast) });
                        minSerializer = WrappedValueSerializer.Create(selectorTranslation.Serializer);
                    }
                }
                else
                {
                    minArgument = AstExpression.Field("$ROOT");
                    minSerializer = pipeline.OutputSerializer;
                }

                pipeline = pipeline.AddStages(
                    minSerializer,
                    AstStage.Group(
                        id: BsonNull.Value,
                        fields: AstExpression.ComputedField("_min", AstExpression.Min(minArgument))),
                    AstStage.ReplaceRoot(AstExpression.Field("_min")));

                return new ExecutableQuery<TDocument, TOutput, TOutput>(
                    provider.Collection,
                    provider.Options,
                    pipeline,
                    __finalizer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
