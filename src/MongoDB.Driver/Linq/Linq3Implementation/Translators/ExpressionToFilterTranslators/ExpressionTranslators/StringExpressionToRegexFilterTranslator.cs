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
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.MethodTranslators
{
    internal static class StringExpressionToRegexFilterTranslator
    {
        // private static fields
        private static readonly IReadOnlyMethodInfoSet __modifierOverloads;
        private static readonly IReadOnlyMethodInfoSet __translatableOverloads;
        private static readonly IReadOnlyMethodInfoSet __withComparisonTypeOverloads;
        private static readonly IReadOnlyMethodInfoSet __withIgnoreCaseAndCultureOverloads;

        // static constructor
        static StringExpressionToRegexFilterTranslator()
        {
            __modifierOverloads = MethodInfoSet.Create(
            [
                StringMethod.ToLowerOverloads,
                StringMethod.ToUpperOverloads,
                StringMethod.TrimOverloads
            ]);

            __translatableOverloads = MethodInfoSet.Create(
            [
                StringMethod.ContainsOverloads,
                StringMethod.EndsWithOverloads,
                StringMethod.StartsWithOverloads
            ]);

            __withComparisonTypeOverloads = MethodInfoSet.Create(
            [
                StringMethod.ContainsWithCharAndComparisonType,
                StringMethod.ContainsWithStringAndComparisonType,
                StringMethod.EndsWithWithStringAndComparisonType,
                StringMethod.StartsWithWithStringAndComparisonType,
            ]);

            __withIgnoreCaseAndCultureOverloads = MethodInfoSet.Create(
            [
                StringMethod.EndsWithWithStringAndIgnoreCaseAndCulture,
                StringMethod.StartsWithWithStringAndIgnoreCaseAndCulture
            ]);
        }

        // public static methods
        public static bool CanTranslate(Expression expression)
        {
            if (expression is MethodCallExpression methodCallExpression)
            {
                var method = methodCallExpression.Method;

                if (method.IsOneOf(__translatableOverloads))
                {
                    return true;
                }

                // on .NET Framework string.Contains(char) compiles to Enumerable.Contains<char>(string, char)
                // on all frameworks we will translate Enumerable.Contains<char>(string, char) the same as string.Contains(char)
                if (method.Is(EnumerableMethod.Contains) && methodCallExpression.Arguments[0].Type == typeof(string))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool CanTranslateComparisonExpression(Expression leftExpression, AstComparisonFilterOperator comparisonOperator, Expression rightExpression)
        {
            return
                IsGetCharsComparison(leftExpression) ||
                IsStringEqualityComparison(leftExpression, comparisonOperator) ||
                IsStringIndexOfComparison(leftExpression) ||
                IsStringLengthComparison(leftExpression) || IsStringCountComparison(leftExpression);
        }

        public static bool TryTranslate(TranslationContext context, Expression expression, out AstFilter filter)
        {
            if (CanTranslate(expression))
            {
                try
                {
                    filter = Translate(context, expression);
                    return true;
                }
                catch (ExpressionNotSupportedException)
                {
                    // ignore exception and return false
                }
            }

            filter = null;
            return false;
        }

        // caller is responsible for ensuring constant is on the right
        public static bool TryTranslateComparisonExpression(TranslationContext context, Expression expression, Expression leftExpression, AstComparisonFilterOperator comparisonOperator, Expression rightExpression, out AstFilter filter)
        {
            if (CanTranslateComparisonExpression(leftExpression, comparisonOperator, rightExpression))
            {
                try
                {
                    filter = TranslateComparisonExpression(context, expression, leftExpression, comparisonOperator, rightExpression);
                    return true;
                }
                catch (ExpressionNotSupportedException)
                {
                    // ignore exception and return false
                }
            }

            filter = null;
            return false;
        }

        public static AstFilter Translate(TranslationContext context, Expression expression)
        {
            if (CanTranslate(expression))
            {
                if (expression is MethodCallExpression methodCallExpression)
                {
                    var method = methodCallExpression.Method;
                    switch (method.Name)
                    {
                        case "StartsWith":
                        case "Contains":
                        case "EndsWith":
                            return TranslateStartsWithOrContainsOrEndsWith(context, methodCallExpression);
                    }
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }

        // caller is responsible for ensuring constant is on the right
        public static AstFilter TranslateComparisonExpression(TranslationContext context, Expression expression, Expression leftExpression, AstComparisonFilterOperator comparisonOperator, Expression rightExpression)
        {
            if (IsGetCharsComparison(leftExpression))
            {
                return TranslateGetCharsComparison(context, expression, leftExpression, comparisonOperator, rightExpression);
            }

            if (IsStringEqualityComparison(leftExpression, comparisonOperator))
            {
                return TranslateStringEqualityComparison(context, expression, leftExpression, comparisonOperator, rightExpression);
            }

            if (IsStringIndexOfComparison(leftExpression))
            {
                return TranslateStringIndexOfComparison(context, expression, leftExpression, comparisonOperator, rightExpression);
            }

            if (IsStringLengthComparison(leftExpression) || IsStringCountComparison(leftExpression))
            {
                return TranslateStringLengthComparison(context, expression, leftExpression, comparisonOperator, rightExpression);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        // private static methods
        private static AstFilter CreateRegexFilter(AstFilterField field, Modifiers modifiers, string pattern)
        {
            var combinedPattern = "^" + modifiers.LeadingPattern + pattern + modifiers.TrailingPattern + "$";
            if (combinedPattern.StartsWith("^.*"))
            {
                combinedPattern = combinedPattern.Substring(3);
            }
            if (combinedPattern.EndsWith(".*$"))
            {
                combinedPattern = combinedPattern.Substring(0, combinedPattern.Length - 3);
            }
            var options = CreateRegexOptions(modifiers);
            return AstFilter.Regex(field, combinedPattern, options);
        }

        private static string CreateRegexOptions(Modifiers modifiers)
        {
            return (modifiers.IgnoreCase || modifiers.ToLower || modifiers.ToUpper) ? "is" : "s";
        }

        private static string EscapeCharacterSet(params char[] chars)
        {
            var escaped = new StringBuilder();
            foreach (var c in chars)
            {
                switch (c)
                {
                    case ' ': escaped.Append("\\ "); break; // space doesn't really need to be escaped but LINQ2 escaped it
                    case '.': escaped.Append("\\."); break; // dot doesn't really need to be escaped (in a character class) but LINQ2 escaped it
                    case '-': escaped.Append("\\-"); break;
                    case '^': escaped.Append("\\^"); break;
                    case '\t': escaped.Append("\\t"); break;
                    default: escaped.Append(c); break;
                }
            }
            return escaped.ToString();
        }

        private static string GetEscapedTrimChars(MethodCallExpression trimExpression)
        {
            var method = trimExpression.Method;
            var arguments = trimExpression.Arguments;

            if (method.Is(StringMethod.Trim))
            {
                return null;
            }
            else
            {
                var trimCharsExpression = arguments[0];
                var trimChars = trimCharsExpression.GetConstantValue<char[]>(containingExpression: trimExpression);
                if (trimChars == null || trimChars.Length == 0)
                {
                    return null;
                }
                else
                {
                    return EscapeCharacterSet(trimChars);
                }
            }
        }

        private static bool IsGetCharsComparison(Expression leftExpression)
        {
            return
                leftExpression is UnaryExpression leftConvertExpression &&
                leftConvertExpression.NodeType == ExpressionType.Convert &&
                leftConvertExpression.Type == typeof(int) &&
                leftConvertExpression.Operand is MethodCallExpression leftGetCharsExpression &&
                leftGetCharsExpression.Method.Is(StringMethod.GetChars);
        }

        private static bool IsStringEqualityComparison(Expression leftExpression, AstComparisonFilterOperator comparisonOperator)
        {
            return
                leftExpression.Type == typeof(string) &&
                (comparisonOperator == AstComparisonFilterOperator.Eq || comparisonOperator == AstComparisonFilterOperator.Ne);
        }

        private static bool IsStringCountComparison(Expression leftExpression)
        {
            return
                leftExpression is MethodCallExpression leftMethodCallExpression &&
                leftMethodCallExpression.Method.Is(EnumerableMethod.Count) &&
                leftMethodCallExpression.Arguments[0].Type == typeof(string);
        }

        private static bool IsStringIndexOfComparison(Expression leftExpression)
        {
            return
                leftExpression is MethodCallExpression leftMethodCallExpression &&
                leftMethodCallExpression.Method.IsOneOf(StringMethod.IndexOfOverloads);
        }

        private static bool IsStringLengthComparison(Expression leftExpression)
        {
            return
                leftExpression is MemberExpression leftMemberExpression &&
                leftMemberExpression.Member is PropertyInfo propertyInfo &&
                propertyInfo.Is(StringProperty.Length);
        }

        private static bool IsWithComparisonTypeMethod(MethodInfo method)
        {
            if (method.IsOneOf(__withComparisonTypeOverloads))
            {
                return true;
            }

            return false;
        }

        private static Modifiers TranslateComparisonType(Modifiers modifiers, Expression expression, Expression comparisonTypeExpression)
        {
            var comparisonType = comparisonTypeExpression.GetConstantValue<StringComparison>(containingExpression: expression);
            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                    return modifiers;

                case StringComparison.CurrentCultureIgnoreCase:
                    modifiers.IgnoreCase = true;
                    return modifiers;
            }

            throw new ExpressionNotSupportedException(comparisonTypeExpression);
        }

        private static Modifiers TranslateCulture(Modifiers modifiers, Expression expression, Expression cultureExpression)
        {
            var culture = cultureExpression.GetConstantValue<CultureInfo>(containingExpression: expression);
            if (culture.Equals(CultureInfo.CurrentCulture))
            {
                return modifiers;
            }

            throw new ExpressionNotSupportedException(cultureExpression);
        }

        private static AstFilter TranslateGetCharsComparison(TranslationContext context, Expression expression, Expression leftExpression, AstComparisonFilterOperator comparisonOperator, Expression rightExpression)
        {
            var leftConvertExpression = (UnaryExpression)leftExpression;
            var leftGetCharsExpression = (MethodCallExpression)leftConvertExpression.Operand;
            var fieldExpression = leftGetCharsExpression.Object;
            var (field, modifiers) = TranslateField(context, expression, fieldExpression);

            var indexExpression = leftGetCharsExpression.Arguments[0];
            var index = indexExpression.GetConstantValue<int>(containingExpression: expression);

            var comparand = rightExpression.GetConstantValue<int>(containingExpression: expression);
            var comparandChar = (char)comparand;
            var comparandString = new string(comparandChar, 1);

            if (comparisonOperator == AstComparisonFilterOperator.Eq || comparisonOperator == AstComparisonFilterOperator.Ne)
            {
                var pattern = comparisonOperator == AstComparisonFilterOperator.Eq ?
                    $".{{{index}}}{Regex.Escape(comparandString)}.*" :
                    $".{{{index}}}[^{EscapeCharacterSet(comparandChar)}].*";

                return CreateRegexFilter(field, modifiers, pattern);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static (AstFilterField, Modifiers) TranslateField(TranslationContext context, Expression expression, Expression fieldExpression)
        {
            if (fieldExpression is MethodCallExpression fieldMethodCallExpression &&
                fieldMethodCallExpression.Method.IsOneOf(__modifierOverloads))
            {
                var (field, modifiers) = TranslateField(context, expression, fieldMethodCallExpression.Object);
                modifiers = TranslateModifier(modifiers, fieldMethodCallExpression);
                return (field, modifiers);
            }
            else
            {
                var fieldTranslation = ExpressionToFilterFieldTranslator.Translate(context, fieldExpression);

                if (fieldTranslation.Serializer is not IHasRepresentationSerializer hasRepresentationSerializer)
                {
                    throw new ExpressionNotSupportedException(fieldExpression, expression, because: $"it was not possible to determine whether field \"{fieldTranslation.Ast.Path}\" is represented as a string");
                }
                if (hasRepresentationSerializer.Representation != BsonType.String)
                {
                    throw new ExpressionNotSupportedException(fieldExpression, expression, because: $"field \"{fieldTranslation.Ast.Path}\" is not represented as a string");
                }

                return (fieldTranslation.Ast, new Modifiers());
            }
        }

        private static Modifiers TranslateIgnoreCase(Modifiers modifiers, Expression expression, Expression ignoreCaseExpression)
        {
            modifiers.IgnoreCase = ignoreCaseExpression.GetConstantValue<bool>(containingExpression: expression);
            return modifiers;
        }

        private static Modifiers TranslateModifier(Modifiers modifiers, MethodCallExpression modifierExpression)
        {
            switch (modifierExpression.Method.Name)
            {
                case "ToLower": case "ToLowerInvariant": return TranslateToLower(modifiers, modifierExpression);
                case "ToUpper": case "ToUpperInvariant": return TranslateToUpper(modifiers, modifierExpression);
                case "Trim": return TranslateTrim(modifiers, modifierExpression);
                case "TrimEnd": return TranslateTrimEnd(modifiers, modifierExpression);
                case "TrimStart": return TranslateTrimStart(modifiers, modifierExpression);
            }

            throw new ExpressionNotSupportedException(modifierExpression);
        }

        private static AstFilter TranslateStartsWithOrContainsOrEndsWith(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            Expression objectExpression;
            if (method.Is(EnumerableMethod.Contains))
            {
                objectExpression = arguments[0];
                arguments = new ReadOnlyCollection<Expression>(arguments.Skip(1).ToList());

                if (objectExpression.Type != typeof(string))
                {
                    throw new ExpressionNotSupportedException(objectExpression, expression, because: "type implementing IEnumerable<char> is not string");
                }
            }
            else
            {
                objectExpression = expression.Object;
            }

            var (field, modifiers) = TranslateField(context, expression, objectExpression);

            string value;
            var valueExpression = arguments[0];
            if (valueExpression.Type == typeof(char))
            {
                var c = valueExpression.GetConstantValue<char>(containingExpression: expression);
                value = new string(c, 1);
            }
            else
            {
                value = valueExpression.GetConstantValue<string>(containingExpression: expression);
            }

            if (IsWithComparisonTypeMethod(method))
            {
                modifiers = TranslateComparisonType(modifiers, expression, arguments[1]);
            }
            if (method.IsOneOf(__withIgnoreCaseAndCultureOverloads))
            {
                modifiers = TranslateIgnoreCase(modifiers, expression, arguments[1]);
                modifiers = TranslateCulture(modifiers, expression, arguments[2]);
            }

            if (IsImpossibleMatch(modifiers, value))
            {
                return AstFilter.MatchesNothing();
            }
            else
            {
                return CreateRegexFilter(field, modifiers, CreatePattern(method.Name, value));
            }

            string CreatePattern(string methodName, string value)
            {
                return methodName switch
                {
                    "Contains" => ".*" + Regex.Escape(value) + ".*",
                    "EndsWith" => ".*" + Regex.Escape(value),
                    "StartsWith" => Regex.Escape(value) + ".*",
                    _ => throw new InvalidOperationException()
                };
            }

            bool IsImpossibleMatch(Modifiers modifiers, string value)
            {
                return
                    (modifiers.ToLower && value != value.ToLower()) ||
                    (modifiers.ToUpper && value != value.ToUpper());
            }
        }

        private static AstFilter TranslateStringEqualityComparison(TranslationContext context, Expression expression, Expression leftExpression, AstComparisonFilterOperator comparisonOperator, Expression rightExpression)
        {
            var (field, modifiers) = TranslateField(context, expression, leftExpression);
            var comparand = rightExpression.GetConstantValue<string>(containingExpression: expression);

            if (comparisonOperator == AstComparisonFilterOperator.Eq || comparisonOperator == AstComparisonFilterOperator.Ne)
            {
                if (IsImpossibleMatch(modifiers, comparand))
                {
                    return comparisonOperator == AstComparisonFilterOperator.Eq ? AstFilter.MatchesNothing() : AstFilter.MatchesEverything();
                }
                else
                {
                    if (modifiers.IsAllDefaults() || comparand == null)
                    {
                        BsonValue value = comparand == null ? BsonNull.Value : BsonString.Create(comparand);
                        return comparisonOperator == AstComparisonFilterOperator.Eq ? AstFilter.Eq(field, value) : AstFilter.Ne(field, value);
                    }
                    else
                    {
                        var pattern = Regex.Escape(comparand);
                        var filter = CreateRegexFilter(field, modifiers, pattern);
                        if (comparisonOperator == AstComparisonFilterOperator.Ne)
                        {
                            filter = AstFilter.Not(filter);
                        }
                        return filter;
                    }
                }
            }

            throw new ExpressionNotSupportedException(expression);

            static bool IsImpossibleMatch(Modifiers modifiers, string comparand)
            {
                return
                    comparand != null && (
                        (modifiers.ToLower && comparand != comparand.ToLower()) ||
                        (modifiers.ToUpper && comparand != comparand.ToUpper()));
            }
        }

        private static AstFilter TranslateStringIndexOfComparison(TranslationContext context, Expression expression, Expression leftExpression, AstComparisonFilterOperator comparisonOperator, Expression rightExpression)
        {
            var leftMethodCallExpression = (MethodCallExpression)leftExpression;
            var method = leftMethodCallExpression.Method;
            var arguments = leftMethodCallExpression.Arguments;

            var fieldExpression = leftMethodCallExpression.Object;
            var (field, modifiers) = TranslateField(context, expression, fieldExpression);

            var startIndex = 0;
            if (method.IsOneOf(StringMethod.IndexOfWithStartIndexOverloads))
            {
                var startIndexExpression = arguments[1];
                startIndex = startIndexExpression.GetConstantValue<int>(containingExpression: expression);
                if (startIndex < 0)
                {
                    throw new ExpressionNotSupportedException(startIndexExpression);
                }
            }

            var count = (int?)null;
            if (method.IsOneOf(StringMethod.IndexOfWithCountOverloads))
            {
                var countExpression = arguments[2];
                count = countExpression.GetConstantValue<int>(containingExpression: expression);
                if (count < 0)
                {
                    throw new ExpressionNotSupportedException(countExpression);
                }
            }

            var comparand = rightExpression.GetConstantValue<int>(containingExpression: expression);

            if (method.IsOneOf(StringMethod.IndexOfAnyOverloads, StringMethod.IndexOfWithCharOverloads))
            {
                char[] anyOf;
                if (method.IsOneOf(StringMethod.IndexOfAnyOverloads))
                {
                    var anyOfExpression = arguments[0];
                    anyOf = anyOfExpression.GetConstantValue<char[]>(containingExpression: expression);
                }
                else
                if (method.IsOneOf(StringMethod.IndexOfWithCharOverloads))
                {
                    var valueExpression = arguments[0];
                    var value = valueExpression.GetConstantValue<char>(containingExpression: expression);
                    anyOf = new char[] { value };
                }
                else
                {
                    throw new ExpressionNotSupportedException(expression);
                }

                var escapedSet = EscapeCharacterSet(anyOf);
                var pattern = "";
                if (startIndex > 0)
                {
                    pattern += $".{{{startIndex}}}"; // advance to startIndex
                }
                if (count.HasValue)
                {
                    pattern += $"(?=.{{{count.Value}}})"; // verify string is long enough
                }
                var noEarlyMatchCount = comparand - startIndex;
                if (noEarlyMatchCount > 0)
                {
                    pattern += $"[^{escapedSet}]{{{noEarlyMatchCount}}}"; // advance to comparand while verifying there are no earlier matches
                }
                if (anyOf.Length == 1)
                {
                    pattern += escapedSet; // verify presence of [escapedSet] at comparand (no brackets needed for single character)
                }
                else
                {
                    pattern += $"[{escapedSet}]"; // verify presence of [escapedSet] at comparand
                }
                pattern += ".*";

                return CreateFilter(expression, field, modifiers, comparisonOperator, pattern);
            }

            if (method.IsOneOf(StringMethod.IndexOfWithStringOverloads))
            {
                var valueExpression = arguments[0];
                var value = valueExpression.GetConstantValue<string>(containingExpression: expression);
                var escapedValue = Regex.Escape(value);

                if (method.IsOneOf(StringMethod.IndexOfWithComparisonTypeOverloads))
                {
                    var comparisonTypeExpression = arguments.Last();
                    modifiers = TranslateComparisonType(modifiers, expression, comparisonTypeExpression);
                }

                var pattern = "";
                if (startIndex > 0)
                {
                    pattern += $".{{{startIndex}}}"; // advance to startIndex
                }
                if (count.HasValue)
                {
                    pattern += $"(?=.{{{count.Value}}})"; // verify string is long enough
                }
                var noEarlyMatchCount = (comparand - startIndex) - 1;
                if (noEarlyMatchCount > 0)
                {
                    pattern += $"(?!.{{0,{noEarlyMatchCount}}}{escapedValue})"; // verify there are no earlier matches
                }
                var advanceToComparandCount = comparand - startIndex;
                if (advanceToComparandCount > 0)
                {
                    pattern += $".{{{advanceToComparandCount}}}"; // advance to comparand
                }
                pattern += $"{escapedValue}.*"; // verify presence of value at comparand

                return CreateFilter(expression, field, modifiers, comparisonOperator, pattern);
            }

            throw new ExpressionNotSupportedException(expression);

            static AstFilter CreateFilter(Expression expression, AstFilterField field, Modifiers modifiers, AstComparisonFilterOperator comparisonOperator, string pattern)
            {
                var filter = CreateRegexFilter(field, modifiers, pattern);
                return comparisonOperator switch
                {
                    AstComparisonFilterOperator.Eq => filter,
                    AstComparisonFilterOperator.Ne => AstFilter.Not(filter),
                    _ => throw new ExpressionNotSupportedException(expression)
                };
            }
        }

        private static AstFilter TranslateStringLengthComparison(TranslationContext context, Expression expression, Expression leftExpression, AstComparisonFilterOperator comparisonOperator, Expression rightExpression)
        {
            Expression fieldExpression;
            if (IsStringLengthComparison(leftExpression))
            {
                fieldExpression = ((MemberExpression)leftExpression).Expression;
            }
            else if (IsStringCountComparison(leftExpression))
            {
                fieldExpression = ((MethodCallExpression)leftExpression).Arguments[0];
            }
            else
            {
                throw new ExpressionNotSupportedException(expression);
            }

            var (field, modifiers) = TranslateField(context, expression, fieldExpression);

            var comparand = rightExpression.GetConstantValue<int>(containingExpression: expression);
            var pattern = comparisonOperator switch
            {
                AstComparisonFilterOperator.Eq => $".{{{comparand}}}",
                AstComparisonFilterOperator.Ne => $".{{{comparand}}}", // $not will be applied below
                AstComparisonFilterOperator.Lt => $".{{0,{comparand - 1}}}",
                AstComparisonFilterOperator.Lte => $".{{0,{comparand}}}",
                AstComparisonFilterOperator.Gt => $".{{{comparand + 1},}}",
                AstComparisonFilterOperator.Gte => $".{{{comparand},}}",
                _ => throw new ExpressionNotSupportedException(expression)
            };

            var filter = CreateRegexFilter(field, modifiers, pattern);
            if (comparisonOperator == AstComparisonFilterOperator.Ne)
            {
                filter = AstFilter.Not(filter);
            }
            return filter;
        }

        private static Modifiers TranslateToLower(Modifiers modifiers, MethodCallExpression toLowerExpression)
        {
            modifiers.ToLower = true;
            modifiers.ToUpper = false;
            return modifiers;
        }

        private static Modifiers TranslateToUpper(Modifiers modifiers, MethodCallExpression toUpperExpression)
        {
            modifiers.ToUpper = true;
            modifiers.ToLower = false;
            return modifiers;
        }

        private static Modifiers TranslateTrim(Modifiers modifiers, MethodCallExpression trimExpression)
        {
            var trimChars = GetEscapedTrimChars(trimExpression);

            if (trimChars == null)
            {
                modifiers.LeadingPattern = modifiers.LeadingPattern + @"\s*(?!\s)";
                modifiers.TrailingPattern = @"(?<!\s)\s*" + modifiers.TrailingPattern;
            }
            else
            {
                var set = Regex.Escape(trimChars);
                modifiers.LeadingPattern = modifiers.LeadingPattern + @"[" + set + "]*(^[" + set + "])";
                modifiers.TrailingPattern = @"(?<[^" + set + "])[" + set + "]*" + modifiers.TrailingPattern;
            }

            return modifiers;
        }

        private static Modifiers TranslateTrimEnd(Modifiers modifiers, MethodCallExpression trimEndExpression)
        {
            var trimChars = GetEscapedTrimChars(trimEndExpression);

            if (trimChars == null)
            {
                modifiers.TrailingPattern = @"(?<!\s)\s*" + modifiers.TrailingPattern;
            }
            else
            {
                modifiers.TrailingPattern = @"(?<=[^" + trimChars + "])[" + trimChars + "]*" + modifiers.TrailingPattern;
            }

            return modifiers;
        }

        private static Modifiers TranslateTrimStart(Modifiers modifiers, MethodCallExpression trimStartExpression)
        {
            var trimChars = GetEscapedTrimChars(trimStartExpression);

            if (trimChars == null)
            {
                modifiers.LeadingPattern = modifiers.LeadingPattern + @"\s*(?!\s)";
            }
            else
            {
                modifiers.LeadingPattern = modifiers.LeadingPattern + @"[" + trimChars + "]*(?=[^" + trimChars + "])";
            }

            return modifiers;
        }

        // nested types
        private class Modifiers
        {
            public Modifiers()
            {
                IgnoreCase = false;
                ToLower = false;
                ToUpper = false;
                LeadingPattern = "";
                TrailingPattern = "";
            }

            public bool IgnoreCase { get; set; }
            public bool ToLower { get; set; }
            public bool ToUpper { get; set; }
            public string LeadingPattern { get; set; }
            public string TrailingPattern { get; set; }

            public bool IsAllDefaults()
            {
                return
                    IgnoreCase == false &&
                    ToLower == false &&
                    ToUpper == false &&
                    LeadingPattern == "" &&
                    TrailingPattern == "";
            }
        }
    }
}
