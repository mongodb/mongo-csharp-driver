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
    internal static class MaxMethodToExecutableQueryTranslator<TOutput>
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
                var sourceExpression = arguments[0];
                var pipeline = ExpressionToPipelineTranslator.Translate(context, sourceExpression);
                var sourceSerializer = pipeline.OutputSerializer;
                var root = AstExpression.Var("ROOT", isCurrent: true);

                AstExpression valueAst;
                IBsonSerializer valueSerializer;
                if (method.IsOneOf(__maxWithSelectorMethods))
                {
                    var selectorLambda = ExpressionHelper.UnquoteLambda(arguments[1]);
                    var selectorTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, selectorLambda, sourceSerializer, asRoot: true);
                    if (selectorTranslation.Serializer is IBsonDocumentSerializer)
                    {
                        valueAst = selectorTranslation.Ast;
                        valueSerializer = selectorTranslation.Serializer;
                    }
                    else
                    {
                        valueAst = AstExpression.ComputedDocument(new[] { AstExpression.ComputedField("_v", selectorTranslation.Ast) });
                        valueSerializer = WrappedValueSerializer.Create("_v", selectorTranslation.Serializer);
                    }
                }
                else
                {
                    valueAst = root;
                    valueSerializer = pipeline.OutputSerializer;
                }

                pipeline = pipeline.AddStages(
                    valueSerializer,
                    AstStage.Group(
                        id: BsonNull.Value,
                        fields: AstExpression.AccumulatorField("_max", AstAccumulatorOperator.Max, valueAst)),
                    AstStage.ReplaceRoot(AstExpression.GetField(root, "_max")));

                return ExecutableQuery.Create(
                    provider.Collection,
                    provider.Options,
                    pipeline,
                    __finalizer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
