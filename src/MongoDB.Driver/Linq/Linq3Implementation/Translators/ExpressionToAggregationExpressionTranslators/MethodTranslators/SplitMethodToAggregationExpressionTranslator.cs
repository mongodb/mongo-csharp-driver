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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class SplitMethodToAggregationExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(StringMethod.SplitOverloads))
            {
                var stringExpression = expression.Object;
                var stringTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, stringExpression);

                string delimiter;
                if (method.IsOneOf(StringMethod.SplitWithCharsOverloads))
                {
                    var separatorsExpression = arguments[0];
                    var separatorChars = separatorsExpression.GetConstantValue<char[]>(containingExpression: expression);
                    if (separatorChars.Length != 1)
                    {
                        goto notSupported;
                    }
                    delimiter = new string(separatorChars[0], 1);
                }
                else if (method.IsOneOf(StringMethod.SplitWithStringsOverloads))
                {
                    var separatorsExpression = arguments[0];
                    var separatorStrings = separatorsExpression.GetConstantValue<string[]>(containingExpression: expression);
                    if (separatorStrings.Length != 1)
                    {
                        goto notSupported;
                    }
                    delimiter = separatorStrings[0];
                }
                else
                {
                    goto notSupported;
                }

                var ast = AstExpression.Split(stringTranslation.Ast, delimiter);

                var options = StringSplitOptions.None;
                if (method.IsOneOf(StringMethod.SplitWithOptionsOverloads))
                {
                    var optionsExpression = arguments.Last();
                    options = optionsExpression.GetConstantValue<StringSplitOptions>(containingExpression: expression);
                }
                if (options == StringSplitOptions.RemoveEmptyEntries)
                {
                    ast = AstExpression.Filter(
                        input: ast,
                        cond: AstExpression.Ne(AstExpression.Var("item"), ""),
                        @as: "item");
                }

                if (method.IsOneOf(StringMethod.SplitWithCountOverloads))
                {
                    var countExpression = arguments[1];
                    var countTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, countExpression);
                    ast = AstExpression.Slice(ast, countTranslation.Ast);
                }

                var serializer = new ArraySerializer<string>(new StringSerializer());
                return new TranslatedExpression(expression, ast, serializer);
            }

        notSupported:
            throw new ExpressionNotSupportedException(expression);
        }
    }
}
