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
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToPipelineTranslators
{
    internal static class UnionMethodToPipelineTranslator
    {
        public static TranslatedPipeline Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.Is(QueryableMethod.Union))
            {
                var firstExpression = arguments[0];
                var pipeline = ExpressionToPipelineTranslator.Translate(context, firstExpression);
                ClientSideProjectionHelper.ThrowIfClientSideProjection(expression, pipeline, method);

                var secondExpression = arguments[1];
                var secondValue = secondExpression.Evaluate();
                if (secondValue is IQueryable secondQueryable &&
                    secondQueryable.Provider is IMongoQueryProviderInternal secondProvider &&
                    secondProvider.CollectionNamespace is var secondCollectionNamespace &&
                    secondCollectionNamespace != null)
                {
                    var secondCollectionName = secondCollectionNamespace.CollectionName;
                    var secondContext = TranslationContext.Create(context.TranslationOptions, context.SerializationDomain);
                    var secondPipeline = ExpressionToPipelineTranslator.Translate(secondContext, secondQueryable.Expression);
                    if (secondPipeline.Ast.Stages.Count == 0)
                    {
                        secondPipeline = null;
                    }

                    pipeline = pipeline.AddStages(
                        AstStage.UnionWith(secondCollectionName, secondPipeline?.Ast),
                        AstStage.Group(AstExpression.RootVar, fields: Array.Empty<AstAccumulatorField>()),
                        AstStage.ReplaceRoot(AstExpression.GetField(AstExpression.RootVar, "_id")),
                        pipeline.OutputSerializer);

                    return pipeline;
                }

                throw new ExpressionNotSupportedException(expression, because: "second argument must be a MongoDB IQueryable against a collection");
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
