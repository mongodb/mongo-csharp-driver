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

using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToPipelineTranslators
{
    internal static class DensifyMethodToPipelineTranslator
    {
        // public static methods
        public static TranslatedPipeline Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.Is(MongoQueryableMethod.DensifyWithArrayPartitionByFields))
            {
                var sourceExpression = arguments[0];
                var pipeline = ExpressionToPipelineTranslator.Translate(context, sourceExpression);
                ClientSideProjectionHelper.ThrowIfClientSideProjection(expression, pipeline, method);

                var sourceSerializer = pipeline.OutputSerializer;
                var fieldLambda = ExpressionHelper.UnquoteLambda(arguments[1]);
                var fieldPath = fieldLambda.TranslateToDottedFieldName(context, sourceSerializer);

                var rangeExpression = arguments[2];
                var range = rangeExpression.GetConstantValue<DensifyRange>(expression);

                List<string> partitionByFieldPaths;
                var partitionByFieldExpressions = arguments[3];
                if (partitionByFieldExpressions is NewArrayExpression newArrayExpression)
                {
                    partitionByFieldPaths = new List<string>();
                    foreach (var partitionByFieldExpression in newArrayExpression.Expressions)
                    {
                        var partitionByFieldLambda = ExpressionHelper.UnquoteLambda(partitionByFieldExpression);
                        var partitionByFieldPath = partitionByFieldLambda.TranslateToDottedFieldName(context, sourceSerializer);
                        partitionByFieldPaths.Add(partitionByFieldPath);
                    }
                }
                else if (
                    partitionByFieldExpressions is ConstantExpression constantExpression &&
                    constantExpression.Value == null)
                {
                    partitionByFieldPaths = null;
                }
                else
                {
                    throw new ExpressionNotSupportedException(partitionByFieldExpressions, expression);
                }

                var stage = AstStage.Densify(fieldPath, range, partitionByFieldPaths);

                pipeline = pipeline.AddStages(sourceSerializer, stage);
                return pipeline;
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
