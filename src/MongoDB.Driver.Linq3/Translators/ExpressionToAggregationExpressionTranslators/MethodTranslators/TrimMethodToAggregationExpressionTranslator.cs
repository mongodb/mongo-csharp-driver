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

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public static class TrimMethodToAggregationExpressionTranslator
    {
        private static MethodInfo[] __trimMethods;

        static TrimMethodToAggregationExpressionTranslator()
        {
            __trimMethods = new[]
            {
                StringMethod.Trim,
                StringMethod.TrimEnd,
                StringMethod.TrimStart,
                StringMethod.TrimWithChars
            };
        }

        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__trimMethods))
            {
                var objectExpression = expression.Object;
                var objectTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, objectExpression);

                var trimCharsExpression = arguments.FirstOrDefault();
                var trimCharsValue = GetTrimCharsValue(trimCharsExpression);

                AstExpression ast =
                    method.Name switch
                    {
                        "Trim" => new AstTrimExpression(objectTranslation.Ast, trimCharsValue),
                        "TrimEnd" => new AstRTrimExpression(objectTranslation.Ast, trimCharsValue),
                        "TrimStart" => new AstLTrimExpression(objectTranslation.Ast, trimCharsValue),
                        _ => throw new InvalidOperationException()
                    };

                return new AggregationExpression(expression, ast, objectTranslation.Serializer);
            }

            throw new ExpressionNotSupportedException(expression);

            AstExpression GetTrimCharsValue(Expression trimCharsExpression)
            {
                if (trimCharsExpression == null)
                {
                    return null;
                }

                if (trimCharsExpression is ConstantExpression trimCharsConstantExpression)
                {
                    var trimChars = (char[])trimCharsConstantExpression.Value;
                    return trimChars.Length == 0 ? null : new AstConstantExpression(new string(trimChars));
                }

                throw new ExpressionNotSupportedException(trimCharsExpression);
            }
        }
    }
}
