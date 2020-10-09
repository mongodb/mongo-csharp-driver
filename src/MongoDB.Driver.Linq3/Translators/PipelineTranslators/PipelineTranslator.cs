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

namespace MongoDB.Driver.Linq3.Translators.PipelineTranslators
{
    public static class PipelineTranslator
    {
        // public static methods
        public static TranslatedPipeline Translate(TranslationContext context, Expression expression)
        {
            if (expression.NodeType == ExpressionType.Constant)
            {
                var query = (IQueryable)((ConstantExpression)expression).Value;
                var provider = (MongoQueryProvider)query.Provider;
                return new TranslatedPipeline(provider.DocumentSerializer);
            }

            var methodCallExpression = (MethodCallExpression)expression;
            var source = methodCallExpression.Arguments[0];

            var pipeline = Translate(context, source);

            switch (methodCallExpression.Method.Name)
            {
                case "Distinct":
                    return DistinctStageTranslator.Translate(context, methodCallExpression, pipeline);
                case "GroupBy":
                    return GroupByStageTranslator.Translate(context, methodCallExpression, pipeline);
                case "OfType":
                    return OfTypeStageTranslator.Translate(context, methodCallExpression, pipeline);
                case "OrderBy":
                case "OrderByDescending":
                case "ThenBy":
                case "ThenByDescending":
                    return OrderByStageTranslator.Translate(context, methodCallExpression, pipeline);
                case "Select":
                    return SelectStageTranslator.Translate(context, methodCallExpression, pipeline);
                case "Skip":
                    return SkipStageTranslator.Translate(context, methodCallExpression, pipeline);
                case "Take":
                    return TakeStageTranslator.Translate(context, methodCallExpression, pipeline);
                case "Where":
                    return WhereStageTranslator.Translate(context, methodCallExpression, pipeline);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
