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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class SetEqualsMethodToAggregationExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (ISetMethod.IsSetEqualsMethod(expression.Method))
            {
                var objectExpression = expression.Object;
                var otherExpression = expression.Arguments[0];

                var objectTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, objectExpression);
                var otherTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, otherExpression);
                var ast = AstExpression.SetEquals(objectTranslation.Ast, otherTranslation.Ast);

                return new TranslatedExpression(expression, ast, new BooleanSerializer());
            }
            throw new ExpressionNotSupportedException(expression);
        }
    }
}
