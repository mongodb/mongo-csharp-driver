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
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class SplitMethodToAggregationExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            AstExpression ast = null;
            if (method.IsOneOf(StringMethod.SplitOverloads))
            {
                var stringAst = ExpressionToAggregationExpressionTranslator.TranslateAndEnsureRepresentation(context, expression.Object, BsonType.String);

                AstExpression delimiter;
                if (method.IsOneOf(StringMethod.SplitWithCharOverloads))
                {
                    var separatorsExpression = arguments[0];
                    var separatorChar = separatorsExpression.GetConstantValue<char>(containingExpression: expression);
                    delimiter = new string(separatorChar, 1);
                }
                else if (method.IsOneOf(StringMethod.SplitWithCharsOverloads))
                {
                    var separatorsExpression = arguments[0];
                    var separatorChars = separatorsExpression.GetConstantValue<char[]>(containingExpression: expression);
                    if (separatorChars.Length != 1)
                    {
                        goto notSupported;
                    }
                    delimiter = new string(separatorChars[0], 1);
                }
                else if (method.IsOneOf(StringMethod.SplitWithStringOverloads))
                {
                    var separatorsExpression = arguments[0];
                    var separatorString = separatorsExpression.GetConstantValue<string>(containingExpression: expression);
                    delimiter = separatorString;
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

                ast = AstExpression.Split(stringAst, delimiter);

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
            }
            else if (method.IsOneOf(RegexMethod.SplitOverloads))
            {
                AstExpression delimiterAst = null;
                Expression stringExpression = null;

                if (method == RegexMethod.Split)
                {
                    delimiterAst = ExpressionToAggregationExpressionTranslator.TranslateAndEnsureRepresentation(context, expression.Object, BsonType.RegularExpression);
                    stringExpression = arguments[0];
                }
                else if (method == RegexMethod.StaticSplit || method == RegexMethod.StaticSplitWithOptions)
                {
                    var options = arguments.Count == 3 ? arguments[2].GetConstantValue<RegexOptions>(expression) : RegexOptions.None;
                    var pattern = arguments[1].GetConstantValue<string>(expression);
                    delimiterAst = new BsonRegularExpression(new Regex(pattern, options));
                    stringExpression = arguments[0];
                }

                var stringAst = ExpressionToAggregationExpressionTranslator.TranslateAndEnsureRepresentation(context, stringExpression, BsonType.String);
                ast = AstExpression.Split(stringAst, delimiterAst);
            }

            if (ast != null)
            {
                var serializer = context.GetSerializer(expression);
                return new TranslatedExpression(expression, ast, serializer);
            }

        notSupported:
            throw new ExpressionNotSupportedException(expression);
        }
    }
}
