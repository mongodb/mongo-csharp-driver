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
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver.Linq3.Ast.Filters;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Reflection;
using MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.MethodTranslators
{
    internal static class IsMatchMethodToFilterTranslator
    {
        // public static methods
        public static AstFilter Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (IsMatchMethod(expression, out var inputExpression, out var regularExpression))
            {
                return Translate(context, inputExpression, regularExpression);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        // private static methods
        private static bool IsMatchMethod(MethodCallExpression expression, out Expression inputExpression, out Regex regex)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.Is(RegexMethod.IsMatch))
            {
                var objectExpression = expression.Object;
                if (objectExpression is ConstantExpression objectConstantExpression)
                {
                    regex = (Regex)objectConstantExpression.Value;
                    inputExpression = arguments[0];
                    return true;
                }
            }

            if (method.IsOneOf(RegexMethod.StaticIsMatch, RegexMethod.StaticIsMatchWithOptions))
            {
                inputExpression = arguments[0];
                var patternExpression = arguments[1];
                var optionsExpression = arguments.Count < 3 ? null : arguments[2];

                string pattern;
                if (patternExpression is ConstantExpression patternConstantExpression)
                {
                    pattern = (string)patternConstantExpression.Value;
                }
                else
                {
                    goto returnFalse;
                }

                var options = RegexOptions.None;
                if (optionsExpression != null)
                {
                    if (optionsExpression is ConstantExpression optionsConstantExpression)
                    {
                        options = (RegexOptions)optionsConstantExpression.Value;
                    }
                    else
                    {
                        goto returnFalse;
                    }
                }

                regex = new Regex(pattern, options);
                return true;
            }

        returnFalse:
            inputExpression = null;
            regex = null;
            return false;
        }

        private static AstFilter Translate(TranslationContext context, Expression inputExpression, Regex regex)
        {
            var inputFieldAst = ExpressionToFilterFieldTranslator.Translate(context, inputExpression);
            var regularExpression = new BsonRegularExpression(regex);
            return AstFilter.Regex(inputFieldAst, regularExpression.Pattern, regularExpression.Options);
        }
    }
}
