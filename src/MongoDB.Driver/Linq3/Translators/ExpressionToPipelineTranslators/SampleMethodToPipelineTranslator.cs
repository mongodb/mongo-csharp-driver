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
using MongoDB.Driver.Linq3.Ast.Stages;
using MongoDB.Driver.Linq3.ExtensionMethods;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Reflection;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToPipelineTranslators
{
    internal static class SampleMethodToPipelineTranslator
    {
        // public static methods
        public static AstPipeline Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.Is(MongoQueryableMethod.Sample))
            {
                var sourceExpression = ConvertHelper.RemoveConvertToMongoQueryable(arguments[0]);
                var pipeline = ExpressionToPipelineTranslator.Translate(context, sourceExpression);

                var sizeExpression = arguments[1];
                var size = sizeExpression.GetConstantValue<long>(containingExpression: expression);

                pipeline = pipeline.AddStages(
                    pipeline.OutputSerializer,
                    AstStage.Sample(size));

                return pipeline;
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
