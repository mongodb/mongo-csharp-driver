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
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using MongoDB.Driver.Linq3.Ast.Filters;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ExpressionTranslators
{
    public static class StringExpressionToRegexFilterTranslator
    {
        // private static fields
        private static MethodInfo[] __containsMethods;
        private static MethodInfo[] __endsWithMethods;
        private static MethodInfo[] __modifierMethods;
        private static MethodInfo[] __startsWithMethods;
        private static MethodInfo[] __translatableMethods;

        // static constructor
        static StringExpressionToRegexFilterTranslator()
        {
            __containsMethods = new[]
            {
                StringMethod.Contains
            };

            __endsWithMethods = new[]
            {
                StringMethod.EndsWith,
                StringMethod.EndsWithWithComparisonType,
                StringMethod.EndsWithWithIgnoreCaseAndCulture
            };

            __modifierMethods = new[]
            {
                StringMethod.ToLower,
                StringMethod.ToLowerInvariant,
                StringMethod.ToLowerWithCulture,
                StringMethod.ToUpper,
                StringMethod.ToUpperInvariant,
                StringMethod.ToUpperWithCulture,
                StringMethod.Trim,
                StringMethod.TrimEnd,
                StringMethod.TrimStart,
                StringMethod.TrimWithChars
            };

            __startsWithMethods = new[]
            {
                StringMethod.StartsWith,
                StringMethod.StartsWithWithComparisonType,
                StringMethod.StartsWithWithIgnoreCaseAndCulture
            };

            __translatableMethods = new[]
            {
                StringMethod.Contains,
                StringMethod.EndsWith,
                StringMethod.EndsWithWithComparisonType,
                StringMethod.EndsWithWithIgnoreCaseAndCulture,
                StringMethod.StartsWith,
                StringMethod.StartsWithWithComparisonType,
                StringMethod.StartsWithWithIgnoreCaseAndCulture
            };
        }

        // public static methods
        public static bool CanTranslate(Expression expression)
        {
            if (expression is MethodCallExpression methodCallExpression &&
                methodCallExpression.Method.IsOneOf(__translatableMethods))
            {
                return true;
            }

            return false;
        }

        public static AstFilter Translate(TranslationContext context, Expression expression)
        {
            if (expression is MethodCallExpression methodCallExpression)
            {
                var method = methodCallExpression.Method;
                if (method.IsOneOf(__translatableMethods))
                {
                    switch (method.Name)
                    {
                        case "Contains":
                        case "EndsWith":
                        case "StartsWith":
                            return TranslateStartsWithOrContainsOrEndsWithMethod(context, methodCallExpression);
                    }
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }

        // private static methods
        private static string AddComparisonTypeOptions(string options, Expression comparisonTypeExpression)
        {
            if (comparisonTypeExpression is ConstantExpression comparisonTypeConstantExpression)
            {
                var comparisonType = (StringComparison)comparisonTypeConstantExpression.Value;
                switch (comparisonType)
                {
                    case StringComparison.CurrentCulture:
                        return options;

                    case StringComparison.CurrentCultureIgnoreCase:
                        return AddOption(options, "i");
                }
            }

            throw new ExpressionNotSupportedException(comparisonTypeExpression);
        }

        private static string AddCultureOptions(string options, Expression cultureExpression)
        {
            if (cultureExpression is ConstantExpression cultureConstantExpression)
            {
                var culture = (CultureInfo)cultureConstantExpression.Value;
                if (culture == CultureInfo.CurrentCulture)
                {
                    return options;
                }
            }

            throw new ExpressionNotSupportedException(cultureExpression);
        }

        private static string AddIgnoreCaseOptions(string options, Expression ignoreCaseExpression)
        {
            if (ignoreCaseExpression is ConstantExpression ignoreCaseConstantExpression)
            {
                var ignoreCase = (bool)ignoreCaseConstantExpression.Value;
                if (ignoreCase)
                {
                    return AddOption(options, "i");
                }
                else
                {
                    return options;
                }
            }

            throw new ExpressionNotSupportedException(ignoreCaseExpression);
        }

        private static string AddOption(string options, string option)
        {
            if (options.Contains(option))
            {
                return options;
            }
            else
            {
                return options + option;
            }
        }

        private static T GetConstantValue<T>(Expression expression)
        {
            if (expression is ConstantExpression constantExpression)
            {
                return (T)constantExpression.Value;
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static (string Pattern, string Options) ProcessModifierMethod(string pattern, string options, MethodCallExpression modifierExpression)
        {
            var method = modifierExpression.Method;
            var arguments = modifierExpression.Arguments;

            if (method.Is(StringMethod.ToLower))
            {
                options = AddOption(options, "i");
                return (pattern, options);
            }

            if (method.Is(StringMethod.Trim))
            {
                pattern = @"\s*(?!\s)" + pattern + @"(?<!\s)\s*";
                return (pattern, options);
            }

            if (method.Is(StringMethod.TrimEnd))
            {
                var chars = GetConstantValue<char[]>(arguments[0]);
                pattern = ProcessTrimEnd(pattern, chars);
                return (pattern, options);
            }

            if (method.Is(StringMethod.TrimStart))
            {
                var chars = GetConstantValue<char[]>(arguments[0]);
                pattern = ProcessTrimStart(pattern, chars);
                return (pattern, options);
            }

            if (method.Is(StringMethod.TrimWithChars))
            {
                var chars = GetConstantValue<char[]>(arguments[0]);
                pattern = ProcessTrimWithChars(pattern, chars);
                return (pattern, options);
            }

            throw new ExpressionNotSupportedException(modifierExpression);

        }

        private static (string Pattern, string Options) ProcessModifierMethods(string pattern, string options, Expression objectExpression)
        {
            var modifierExpression = objectExpression;

            while (
                modifierExpression is MethodCallExpression modifierMethodCallExpression &&
                modifierMethodCallExpression.Method.IsOneOf(__modifierMethods))
            {
                (pattern, options) = ProcessModifierMethod(pattern, options, modifierMethodCallExpression);
                modifierExpression = modifierMethodCallExpression.Object;
            }

            if (pattern.StartsWith(".*"))
            {
                pattern = pattern.Remove(0, 2);
            }
            else
            {
                pattern = "^" + pattern;
            }

            if (pattern.EndsWith(".*"))
            {
                pattern = pattern.Remove(pattern.Length - 2, 2);
            }
            else
            {
                pattern = pattern + "$";
            }

            return (pattern, options);
        }

        private static string ProcessTrimEnd(string pattern, char[] chars)
        {
            if (chars == null || chars.Length == 0)
            {
                return $"{pattern}(?<!\\s)\\s*";
            }
            else
            {
                var set = ToCharacterSet(chars);
                return $"{pattern}(?<![{set}])[{set}]*)";
            }
        }

        private static string ProcessTrimStart(string pattern, char[] chars)
        {
            if (chars == null || chars.Length == 0)
            {
                return $"\\s*(?!\\s){pattern}";
            }
            else
            {
                var set = ToCharacterSet(chars);
                return $"[{set}]*(?![{set}]){pattern}";
            }
        }

        private static string ProcessTrimWithChars(string pattern, char[] chars)
        {
            if (chars == null || chars.Length == 0)
            {
                return $"\\s*(?!\\s){pattern}(?<!\\s)\\s*";
            }
            else
            {
                var set = ToCharacterSet(chars);
                return $"[{set}]*(?![{set}]){pattern}(?<![{set}])[{set}]*)";
            }
        }

        private static string ToCharacterSet(char[] chars)
        {
            var set = new StringBuilder();

            foreach (var c in chars)
            {
                switch (c)
                {
                    case ' ':
                    case '^':
                    case '-':
                    case '.':
                    case '\\':
                        set.Append('\\');
                        set.Append(c);
                        break;

                    case '\n': set.Append("\\n"); break;
                    case '\r': set.Append("\\r"); break;
                    case '\t': set.Append("\\t"); break;

                    default:
                        set.Append(c);
                        break;
                }
            }

            return set.ToString();
        }

        private static AstFilter TranslateStartsWithOrContainsOrEndsWithMethod(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__startsWithMethods) || method.IsOneOf(__containsMethods) || method.IsOneOf(__endsWithMethods))
            {
                var objectExpression = expression.Object;
                var field = TranslateRootField(context, objectExpression);

                var valueExpression = arguments[0];
                var value = GetConstantValue<string>(valueExpression);
                var pattern = Regex.Escape(value);
                if (method.IsOneOf(__startsWithMethods) || method.IsOneOf(__containsMethods))
                {
                    pattern = $"{pattern}.*";
                }
                if (method.IsOneOf(__containsMethods) || method.IsOneOf(__endsWithMethods))
                {
                    pattern = $".*{pattern}";
                }
                var options = "s";

                if (method.IsOneOf(StringMethod.StartsWithWithComparisonType, StringMethod.EndsWithWithComparisonType))
                {
                    var comparisonTypeExpression = arguments[1];
                    options = AddComparisonTypeOptions(options, comparisonTypeExpression);
                }

                if (method.IsOneOf(StringMethod.StartsWithWithIgnoreCaseAndCulture, StringMethod.EndsWithWithIgnoreCaseAndCulture))
                {
                    var ignoreCaseExpression = arguments[1];
                    options = AddIgnoreCaseOptions(options, ignoreCaseExpression);

                    var cultureExpression = arguments[2];
                    options = AddCultureOptions(options, cultureExpression);
                }

                (pattern, options) = ProcessModifierMethods(pattern, options, objectExpression);
                return new AstRegexFilter(field, pattern, options);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static AstFilterField TranslateRootField(TranslationContext context, Expression expression)
        {
            while (
                expression is MethodCallExpression methodCallExpression &&
                methodCallExpression.Method.IsOneOf(__modifierMethods))
            {
                expression = methodCallExpression.Object;
            }

            return ExpressionToFilterFieldTranslator.Translate(context, expression);
        }
    }
}
