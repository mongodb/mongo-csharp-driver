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
using MongoDB.Driver.Linq3.Ast;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Ast.Stages;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToPipelineTranslators
{
    public static class DistinctMethodToPipelineTranslator
    {
        // public static methods
        public static Pipeline Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            var source = arguments[0];
            var pipeline = ExpressionToPipelineTranslator.Translate(context, source);

            if (method.Is(QueryableMethod.Distinct))
            {
                pipeline.AddStages(
                    pipeline.OutputSerializer,
                    AstStage.Group(AstExpression.Field("$ROOT"), Enumerable.Empty<AstComputedField>()),
                    AstStage.ReplaceRoot(AstExpression.Field("_id")));

                return pipeline;
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
