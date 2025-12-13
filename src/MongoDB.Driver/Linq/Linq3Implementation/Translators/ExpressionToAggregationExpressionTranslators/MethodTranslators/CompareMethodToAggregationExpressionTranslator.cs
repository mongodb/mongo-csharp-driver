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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class CompareMethodToAggregationExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsStaticCompareMethod() || method.IsInstanceCompareToMethod() || method.IsOneOf(StringMethod.CompareOverloads))
            {
                Expression value1Expression;
                Expression value2Expression;
                if (method.IsStatic)
                {
                    value1Expression = arguments[0];
                    value2Expression = arguments[1];
                }
                else
                {
                    value1Expression = expression.Object;
                    value2Expression = arguments[0];
                }

                var value1Translation = ExpressionToAggregationExpressionTranslator.Translate(context, value1Expression);
                var value2Translation = ExpressionToAggregationExpressionTranslator.Translate(context, value2Expression);

                AstExpression ast;
                if (method.Is(StringMethod.CompareWithIgnoreCase))
                {
                    var ignoreCaseExpression = arguments[2];
                    var ignoreCase = ignoreCaseExpression.GetConstantValue<bool>(containingExpression: expression);
                    ast = ignoreCase
                        ? AstExpression.StrCaseCmp(value1Translation.Ast, value2Translation.Ast)
                        : AstExpression.Cmp(value1Translation.Ast, value2Translation.Ast);
                }
                else
                {
                    ast = AstExpression.Cmp(value1Translation.Ast, value2Translation.Ast);
                }

                return new TranslatedExpression(expression, ast, Int32Serializer.Instance);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
