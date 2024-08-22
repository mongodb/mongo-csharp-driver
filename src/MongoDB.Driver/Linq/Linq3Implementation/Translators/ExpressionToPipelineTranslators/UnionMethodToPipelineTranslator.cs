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
        public static AstPipeline Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.Is(QueryableMethod.Union))
            {
                var firstExpression = arguments[0];
                var pipeline = ExpressionToPipelineTranslator.Translate(context, firstExpression);

                var secondExpression = arguments[1];
                var secondValue = secondExpression.Evaluate(); // TODO: Evaluate might not be necessary once IMongoQueryable is removed
                if (secondValue is IMongoQueryable secondQueryable)
                {
                    var secondProvider = (IMongoQueryProviderInternal)secondQueryable.Provider;
                    var secondCollectionNamespace = secondProvider.CollectionNamespace;
                    if (secondCollectionNamespace == null)
                    {
                        throw new ExpressionNotSupportedException(expression, because: "second argument must be an IMongoQueryable against a collection");
                    }

                    var secondCollectionName = secondCollectionNamespace.CollectionName;
                    var secondPipelineInputSerializer = secondProvider.PipelineInputSerializer;
                    var secondContext = TranslationContext.Create(secondQueryable.Expression, secondPipelineInputSerializer);
                    var secondPipeline = ExpressionToPipelineTranslator.Translate(secondContext, secondQueryable.Expression);
                    if (secondPipeline.Stages.Count == 0)
                    {
                        secondPipeline = null;
                    }

                    pipeline = pipeline.AddStages(
                        pipeline.OutputSerializer,
                        AstStage.UnionWith(secondCollectionName, secondPipeline),
                        AstStage.Group(AstExpression.RootVar, fields: Array.Empty<AstAccumulatorField>()),
                        AstStage.ReplaceRoot(AstExpression.GetField(AstExpression.RootVar, "_id")));

                    return pipeline;
                }

                throw new ExpressionNotSupportedException(expression, because: "second argument must be IMongoQueryable");
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
