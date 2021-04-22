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
using MongoDB.Driver.Linq3.Ast.Stages;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToPipelineTranslators
{
    internal static class ExpressionToPipelineTranslator
    {
        // public static methods
        public static AstPipeline Translate(TranslationContext context, Expression expression)
        {
            if (expression.NodeType == ExpressionType.Constant)
            {
                var query = (IQueryable)((ConstantExpression)expression).Value;
                var provider = (MongoQueryProvider)query.Provider;
                return AstPipeline.Empty(provider.CollectionDocumentSerializer);
            }

            var methodCallExpression = (MethodCallExpression)expression;
            switch (methodCallExpression.Method.Name)
            {
                case "Distinct":
                    return DistinctMethodToPipelineTranslator.Translate(context, methodCallExpression);
                case "GroupBy":
                    return GroupByMethodToPipelineTranslator.Translate(context, methodCallExpression);
                case "GroupJoin":
                    return GroupJoinMethodToPipelineTranslator.Translate(context, methodCallExpression);
                case "Join":
                    return JoinMethodToPipelineTranslator.Translate(context, methodCallExpression);
                case "OfType":
                    return OfTypeMethodToPipelineTranslator.Translate(context, methodCallExpression);
                case "OrderBy":
                case "OrderByDescending":
                case "ThenBy":
                case "ThenByDescending":
                    return OrderByMethodToPipelineTranslator.Translate(context, methodCallExpression);
                case "Sample":
                    return SampleMethodToPipelineTranslator.Translate(context, methodCallExpression);
                case "Select":
                    return SelectMethodToPipelineTranslator.Translate(context, methodCallExpression);
                case "SelectMany":
                    return SelectManyMethodToPipelineTranslator.Translate(context, methodCallExpression);
                case "Skip":
                    return SkipMethodToPipelineTranslator.Translate(context, methodCallExpression);
                case "Take":
                    return TakeMethodToPipelineTranslator.Translate(context, methodCallExpression);
                case "Where":
                    return WhereMethodToPipelineTranslator.Translate(context, methodCallExpression);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
