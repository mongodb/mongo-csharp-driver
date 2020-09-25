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

namespace MongoDB.Driver.Linq3.Translators.PipelineTranslators
{
    public static class DistinctStageTranslator
    {
        // public static methods
        public static TranslatedPipeline Translate(TranslationContext context, MethodCallExpression expression, TranslatedPipeline pipeline)
        {
            if (expression.Method.Is(QueryableMethod.Distinct))
            {
                pipeline.AddStages(
                    pipeline.OutputSerializer,
                    //new BsonDocument("$group", new BsonDocument("_id", "$$ROOT")),
                    //new BsonDocument("$replaceRoot", new BsonDocument("newRoot", "$_id")));
                    new AstGroupStage(new AstFieldExpression("$$ROOT"), Enumerable.Empty<AstComputedField>()),
                    new AstReplaceRootStage(new AstFieldExpression("$_id")));

                return pipeline;
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
