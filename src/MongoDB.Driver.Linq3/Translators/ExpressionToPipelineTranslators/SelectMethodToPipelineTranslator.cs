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
using MongoDB.Driver.Linq3.Ast;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Reflection;
using MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToPipelineTranslators
{
    public static class SelectMethodToPipelineTranslator
    {
        // public static methods
        public static AstPipeline Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.Is(QueryableMethod.Select))
            {
                var sourceExpression = arguments[0];
                var pipeline = ExpressionToPipelineTranslator.Translate(context, sourceExpression);
                var sourceSerializer = pipeline.OutputSerializer;

                var selectorLambda = ExpressionHelper.UnquoteLambda(arguments[1]);
                if (selectorLambda.Body == selectorLambda.Parameters[0])
                {
                    return pipeline; // ignore identity projection: Select(x => x)
                }
                var selectorTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, selectorLambda, sourceSerializer, asCurrentSymbol: true);
                var (projectStage, projectionSerializer) = ProjectionHelper.CreateProjectStage(selectorTranslation);
                pipeline = pipeline.AddStages(projectionSerializer, projectStage);

                return pipeline;
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
