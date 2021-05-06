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
using System.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Misc
{
    internal static class StringMethod
    {
        // private static fields
        private static readonly MethodInfo __contains;
        private static readonly MethodInfo __endsWith;
        private static readonly MethodInfo __endsWithWithComparisonType;
        private static readonly MethodInfo __endsWithWithIgnoreCaseAndCulture;
        private static readonly MethodInfo __getChars;
        private static readonly MethodInfo __indexOfAny;
        private static readonly MethodInfo __indexOfAnyWithStartIndex;
        private static readonly MethodInfo __indexOfAnyWithStartIndexAndCount;
        private static readonly MethodInfo __indexOfWithChar;
        private static readonly MethodInfo __indexOfWithCharAndStartIndex;
        private static readonly MethodInfo __indexOfWithCharAndStartIndexAndCount;
        private static readonly MethodInfo __indexOfWithString;
        private static readonly MethodInfo __indexOfWithStringAndStartIndex;
        private static readonly MethodInfo __indexOfWithStringAndStartIndexAndCount;
        private static readonly MethodInfo __indexOfWithStringAndComparisonType;
        private static readonly MethodInfo __indexOfWithStringAndStartIndexAndComparisonType;
        private static readonly MethodInfo __indexOfWithStringAndStartIndexAndCountAndComparisonType;
        private static readonly MethodInfo __isNullOrEmpty;
        private static readonly MethodInfo __splitWithChars;
        private static readonly MethodInfo __splitWithCharsAndCount;
        private static readonly MethodInfo __splitWithCharsAndCountAndOptions;
        private static readonly MethodInfo __splitWithCharsAndOptions;
        private static readonly MethodInfo __splitWithStringsAndCountAndOptions;
        private static readonly MethodInfo __splitWithStringsAndOptions;
        private static readonly MethodInfo __startsWith;
        private static readonly MethodInfo __startsWithWithComparisonType;
        private static readonly MethodInfo __startsWithWithIgnoreCaseAndCulture;
        private static readonly MethodInfo __substring;
        private static readonly MethodInfo __substringWithLength;
        private static readonly MethodInfo __toLower;
        private static readonly MethodInfo __toLowerInvariant;
        private static readonly MethodInfo __toLowerWithCulture;
        private static readonly MethodInfo __toUpper;
        private static readonly MethodInfo __toUpperInvariant;
        private static readonly MethodInfo __toUpperWithCulture;
        private static readonly MethodInfo __trim;
        private static readonly MethodInfo __trimEnd;
        private static readonly MethodInfo __trimStart;
        private static readonly MethodInfo __trimWithChars;

        // static constructor
        static StringMethod()
        {
            __contains = ReflectionInfo.Method((string s, string value) => s.Contains(value));
            __endsWith = ReflectionInfo.Method((string s, string value) => s.EndsWith(value));
            __endsWithWithComparisonType = ReflectionInfo.Method((string s, string value, StringComparison comparisonType) => s.EndsWith(value, comparisonType));
#if NETSTANDARD1_5
            __endsWithWithIgnoreCaseAndCulture = null;
#else
            __endsWithWithIgnoreCaseAndCulture = ReflectionInfo.Method((string s, string value, bool ignoreCase, CultureInfo culture) => s.EndsWith(value, ignoreCase, culture));
#endif
            __getChars = ReflectionInfo.Method((string s, int index) => s[index]);
            __indexOfAny = ReflectionInfo.Method((string s, char[] anyOf) => s.IndexOfAny(anyOf));
            __indexOfAnyWithStartIndex = ReflectionInfo.Method((string s, char[] anyOf, int startIndex) => s.IndexOfAny(anyOf, startIndex));
            __indexOfAnyWithStartIndexAndCount = ReflectionInfo.Method((string s, char[] anyOf, int startIndex, int count) => s.IndexOfAny(anyOf, startIndex, count));
            __indexOfWithChar = ReflectionInfo.Method((string s, char value) => s.IndexOf(value));
            __indexOfWithCharAndStartIndex = ReflectionInfo.Method((string s, char value, int startIndex) => s.IndexOf(value, startIndex));
            __indexOfWithCharAndStartIndexAndCount = ReflectionInfo.Method((string s, char value, int startIndex, int count) => s.IndexOf(value, startIndex, count));
            __indexOfWithString = ReflectionInfo.Method((string s, string value) => s.IndexOf(value));
            __indexOfWithStringAndStartIndex = ReflectionInfo.Method((string s, string value, int startIndex) => s.IndexOf(value, startIndex));
            __indexOfWithStringAndStartIndexAndCount = ReflectionInfo.Method((string s, string value, int startIndex, int count) => s.IndexOf(value, startIndex, count));
            __indexOfWithStringAndComparisonType = ReflectionInfo.Method((string s, string value, StringComparison comparisonType) => s.IndexOf(value, comparisonType));
            __indexOfWithStringAndStartIndexAndComparisonType = ReflectionInfo.Method((string s, string value, int startIndex, StringComparison comparisonType) => s.IndexOf(value, startIndex, comparisonType));
            __indexOfWithStringAndStartIndexAndCountAndComparisonType = ReflectionInfo.Method((string s, string value, int startIndex, int count, StringComparison comparisonType) => s.IndexOf(value, startIndex, count, comparisonType));
            __isNullOrEmpty = ReflectionInfo.Method((string value) => string.IsNullOrEmpty(value));
            __splitWithChars = ReflectionInfo.Method((string s, char[] separator) => s.Split(separator));
            __splitWithCharsAndCount = ReflectionInfo.Method((string s, char[] separator, int count) => s.Split(separator, count));
            __splitWithCharsAndCountAndOptions = ReflectionInfo.Method((string s, char[] separator, int count, StringSplitOptions options) => s.Split(separator, count, options));
            __splitWithCharsAndOptions = ReflectionInfo.Method((string s, char[] separator, StringSplitOptions options) => s.Split(separator, options));
            __splitWithStringsAndCountAndOptions = ReflectionInfo.Method((string s, string[] separator, int count, StringSplitOptions options) => s.Split(separator, count, options));
            __splitWithStringsAndOptions = ReflectionInfo.Method((string s, string[] separator, StringSplitOptions options) => s.Split(separator, options));
            __startsWith = ReflectionInfo.Method((string s, string value) => s.StartsWith(value));
            __startsWithWithComparisonType = ReflectionInfo.Method((string s, string value, StringComparison comparisonType) => s.StartsWith(value, comparisonType));
#if NETSTANDARD1_5
            __startsWithWithIgnoreCaseAndCulture = null;
#else
            __startsWithWithIgnoreCaseAndCulture = ReflectionInfo.Method((string s, string value, bool ignoreCase, CultureInfo culture) => s.StartsWith(value, ignoreCase, culture));
#endif
            __substring = ReflectionInfo.Method((string s, int startIndex) => s.Substring(startIndex));
            __substringWithLength = ReflectionInfo.Method((string s, int startIndex, int length) => s.Substring(startIndex, length));
            __toLower = ReflectionInfo.Method((string s) => s.ToLower());
            __toLowerInvariant = ReflectionInfo.Method((string s) => s.ToLowerInvariant());
#if NETSTANDARD1_5
            __toLowerWithCulture = null;
#else
            __toLowerWithCulture = ReflectionInfo.Method((string s, CultureInfo culture) => s.ToLower(culture));
#endif
            __toUpper = ReflectionInfo.Method((string s) => s.ToUpper());
            __toUpperInvariant = ReflectionInfo.Method((string s) => s.ToUpperInvariant());
#if NETSTANDARD1_5
            __toUpperWithCulture = null;
#else
            __toUpperWithCulture = ReflectionInfo.Method((string s, CultureInfo culture) => s.ToUpper(culture));
#endif
            __trim = ReflectionInfo.Method((string s) => s.Trim());
            __trimEnd = ReflectionInfo.Method((string s, char[] trimChars) => s.TrimEnd(trimChars));
            __trimStart = ReflectionInfo.Method((string s, char[] trimChars) => s.TrimStart(trimChars));
            __trimWithChars = ReflectionInfo.Method((string s, char[] trimChars) => s.Trim(trimChars));
        }

        // public properties
        public static MethodInfo Contains => __contains;
        public static MethodInfo EndsWith => __endsWith;
        public static MethodInfo EndsWithWithComparisonType => __endsWithWithComparisonType;
        public static MethodInfo EndsWithWithIgnoreCaseAndCulture => __endsWithWithIgnoreCaseAndCulture;
        public static MethodInfo GetChars => __getChars;
        public static MethodInfo IndexOfAny => __indexOfAny;
        public static MethodInfo IndexOfAnyWithStartIndex => __indexOfAnyWithStartIndex;
        public static MethodInfo IndexOfAnyWithStartIndexAndCount => __indexOfAnyWithStartIndexAndCount;
        public static MethodInfo IndexOfWithChar => __indexOfWithChar;
        public static MethodInfo IndexOfWithCharAndStartIndex => __indexOfWithCharAndStartIndex;
        public static MethodInfo IndexOfWithCharAndStartIndexAndCount => __indexOfWithCharAndStartIndexAndCount;
        public static MethodInfo IndexOfWithString => __indexOfWithString;
        public static MethodInfo IndexOfWithStringAndStartIndex => __indexOfWithStringAndStartIndex;
        public static MethodInfo IndexOfWithStringAndStartIndexAndCount => __indexOfWithStringAndStartIndexAndCount;
        public static MethodInfo IndexOfWithStringAndComparisonType => __indexOfWithStringAndComparisonType;
        public static MethodInfo IndexOfWithStringAndStartIndexAndComparisonType => __indexOfWithStringAndStartIndexAndComparisonType;
        public static MethodInfo IndexOfWithStringAndStartIndexAndCountAndComparisonType => __indexOfWithStringAndStartIndexAndCountAndComparisonType;
        public static MethodInfo IsNullOrEmpty => __isNullOrEmpty;
        public static MethodInfo SplitWithChars => __splitWithChars;
        public static MethodInfo SplitWithCharsAndCount => __splitWithCharsAndCount;
        public static MethodInfo SplitWithCharsAndCountAndOptions => __splitWithCharsAndCountAndOptions;
        public static MethodInfo SplitWithCharsAndOptions => __splitWithCharsAndOptions;
        public static MethodInfo SplitWithStringsAndCountAndOptions => __splitWithStringsAndCountAndOptions;
        public static MethodInfo SplitWithStringsAndOptions => __splitWithStringsAndOptions;
        public static MethodInfo StartsWith => __startsWith;
        public static MethodInfo StartsWithWithComparisonType => __startsWithWithComparisonType;
        public static MethodInfo StartsWithWithIgnoreCaseAndCulture => __startsWithWithIgnoreCaseAndCulture;
        public static MethodInfo Substring => __substring;
        public static MethodInfo SubstringWithLength => __substringWithLength;
        public static MethodInfo ToLower => __toLower;
        public static MethodInfo ToLowerInvariant => __toLowerInvariant;
        public static MethodInfo ToLowerWithCulture => __toLowerWithCulture;
        public static MethodInfo ToUpper => __toUpper;
        public static MethodInfo ToUpperInvariant => __toUpperInvariant;
        public static MethodInfo ToUpperWithCulture => __toUpperWithCulture;
        public static MethodInfo Trim => __trim;
        public static MethodInfo TrimEnd => __trimEnd;
        public static MethodInfo TrimStart => __trimStart;
        public static MethodInfo TrimWithChars => __trimWithChars;
    }
}
