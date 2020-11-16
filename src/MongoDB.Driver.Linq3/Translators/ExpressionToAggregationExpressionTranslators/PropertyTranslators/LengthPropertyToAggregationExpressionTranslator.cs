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
using System.Reflection;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq3.Ast.Expressions;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators.PropertyTranslators
{
    public static class LengthPropertyToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, MemberExpression expression)
        {
            if (IsStringLengthProperty(expression))
            {
                var stringExpression = expression.Expression;

                var stringTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, stringExpression);
                var ast = new AstUnaryExpression(AstUnaryOperator.StrLenCP, stringTranslation.Ast);

                return new AggregationExpression(expression, ast, new Int32Serializer());
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static bool IsStringLengthProperty(MemberExpression expression)
        {
            return
                expression.Member is PropertyInfo propertyMember &&
                propertyMember.DeclaringType == typeof(string) &&
                propertyMember.PropertyType == typeof(int) &&
                propertyMember.Name == "Length";
        }
    }
}
