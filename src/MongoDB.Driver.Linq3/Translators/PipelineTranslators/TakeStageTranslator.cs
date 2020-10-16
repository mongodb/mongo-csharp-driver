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
using MongoDB.Driver.Linq3.Ast.Stages;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.PipelineTranslators
{
    public static class TakeStageTranslator
    {
        // public static methods
        public static TranslatedPipeline Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            var source = arguments[0];
            var pipeline = PipelineTranslator.Translate(context, source);

            if (method.Is(QueryableMethod.Take))
            {
                var count = arguments[1];

                if (count.NodeType == ExpressionType.Constant)
                {
                    var countValue = (int)((ConstantExpression)count).Value;

                    pipeline.AddStages(
                        pipeline.OutputSerializer,
                        //new BsonDocument("$limit", countValue));
                        new AstLimitStage(countValue));

                    return pipeline;
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
