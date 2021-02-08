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
using MongoDB.Driver.Linq3.Ast.Filters;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.MethodTranslators
{
    public static class EqualsMethodToFilterTranslator
    {
        public static AstFilter Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.Name == "Equals" & method.ReturnType == typeof(bool))
            {
                if (method.IsStatic)
                {
                    if (arguments.Count == 2)
                    {
                        var expression1 = arguments[0];
                        var expression2 = arguments[1];
                        return Translate(context, expression1, expression2);
                    }
                }
                else
                {
                    if (arguments.Count == 1)
                    {
                        var expression1 = expression.Object;
                        var expression2 = arguments[0];
                        return Translate(context, expression1, expression2);
                    }
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static AstFilter Translate(TranslationContext context, Expression expression1, Expression expression2)
        {
            if (expression1 is ConstantExpression constantExpression1 && expression2.NodeType != ExpressionType.Constant)
            {
                expression1 = expression2;
                expression2 = constantExpression1;
            }

            var field = ExpressionToFilterFieldTranslator.Translate(context, expression1);
            if (expression2 is ConstantExpression constantExpression2)
            {
                var value = constantExpression2.Value;
                var serializedValue = SerializationHelper.SerializeValue(field.Serializer, value);
                return new AstComparisonFilter(AstComparisonFilterOperator.Eq, field, serializedValue);
            }
            else
            {
                throw new ExpressionNotSupportedException(expression2);
            }
        }
    }
}
