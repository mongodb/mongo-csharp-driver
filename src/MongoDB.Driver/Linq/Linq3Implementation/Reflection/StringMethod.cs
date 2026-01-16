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
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Misc
{
    internal static class StringMethod
    {
        // private static fields
        private static readonly MethodInfo __anyStringInWithEnumerable;
        private static readonly MethodInfo __anyStringInWithParams;
        private static readonly MethodInfo __anyStringNinWithEnumerable;
        private static readonly MethodInfo __anyStringNinWithParams;
        private static readonly MethodInfo __compare;
        private static readonly MethodInfo __compareWithIgnoreCase;
        private static readonly MethodInfo __concatWith1Object;
        private static readonly MethodInfo __concatWith2Objects;
        private static readonly MethodInfo __concatWith3Objects;
        private static readonly MethodInfo __concatWithObjectArray;
        private static readonly MethodInfo __concatWith2Strings;
        private static readonly MethodInfo __concatWith3Strings;
        private static readonly MethodInfo __concatWith4Strings;
        private static readonly MethodInfo __concatWithStringArray;
        private static readonly MethodInfo __containsWithChar;
        private static readonly MethodInfo __containsWithCharAndComparisonType;
        private static readonly MethodInfo __containsWithString;
        private static readonly MethodInfo __containsWithStringAndComparisonType;
        private static readonly MethodInfo __endsWithWithChar;
        private static readonly MethodInfo __endsWithWithString;
        private static readonly MethodInfo __endsWithWithStringAndComparisonType;
        private static readonly MethodInfo __endsWithWithStringAndIgnoreCaseAndCulture;
        private static readonly MethodInfo __equalsWithComparisonType;
        private static readonly MethodInfo __getChars;
        private static readonly MethodInfo __indexOfAny;
        private static readonly MethodInfo __indexOfAnyWithStartIndex;
        private static readonly MethodInfo __indexOfAnyWithStartIndexAndCount;
        private static readonly MethodInfo __indexOfBytesWithValue;
        private static readonly MethodInfo __indexOfBytesWithValueAndStartIndex;
        private static readonly MethodInfo __indexOfBytesWithValueAndStartIndexAndCount;
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
        private static readonly MethodInfo __isNullOrWhiteSpace;
        private static readonly MethodInfo __splitWithChars;
        private static readonly MethodInfo __splitWithCharsAndCount;
        private static readonly MethodInfo __splitWithCharsAndCountAndOptions;
        private static readonly MethodInfo __splitWithCharsAndOptions;
        private static readonly MethodInfo __splitWithStringsAndCountAndOptions;
        private static readonly MethodInfo __splitWithStringsAndOptions;
        private static readonly MethodInfo __startsWithWithChar;
        private static readonly MethodInfo __startsWithWithString;
        private static readonly MethodInfo __startsWithWithStringAndComparisonType;
        private static readonly MethodInfo __startsWithWithStringAndIgnoreCaseAndCulture;
        private static readonly MethodInfo __staticEqualsWithComparisonType;
        private static readonly MethodInfo __stringInWithEnumerable;
        private static readonly MethodInfo __stringInWithParams;
        private static readonly MethodInfo __stringNinWithEnumerable;
        private static readonly MethodInfo __stringNinWithParams;
        private static readonly MethodInfo __strLenBytes;
        private static readonly MethodInfo __substrBytes;
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

        // sets of methods
        private static readonly IReadOnlyMethodInfoSet __anyStringInOverloads;
        private static readonly IReadOnlyMethodInfoSet __anyStringNinOverloads;
        private static readonly IReadOnlyMethodInfoSet __compareOverloads;
        private static readonly IReadOnlyMethodInfoSet __concatOverloads;
        private static readonly IReadOnlyMethodInfoSet __containsOverloads;
        private static readonly IReadOnlyMethodInfoSet __endsWithOrStartsWithOverloads;
        private static readonly IReadOnlyMethodInfoSet __endsWithOverloads;
        private static readonly IReadOnlyMethodInfoSet __indexOfAnyOverloads;
        private static readonly IReadOnlyMethodInfoSet __indexOfOverloads;
        private static readonly IReadOnlyMethodInfoSet __indexOfBytesOverloads;
        private static readonly IReadOnlyMethodInfoSet __indexOfWithCharOverloads;
        private static readonly IReadOnlyMethodInfoSet __indexOfWithComparisonTypeOverloads;
        private static readonly IReadOnlyMethodInfoSet __indexOfWithCountOverloads;
        private static readonly IReadOnlyMethodInfoSet __indexOfWithStartIndexOverloads;
        private static readonly IReadOnlyMethodInfoSet __indexOfWithStringOverloads;
        private static readonly IReadOnlyMethodInfoSet __indexOfWithStringComparisonOverloads;
        private static readonly IReadOnlyMethodInfoSet __splitOverloads;
        private static readonly IReadOnlyMethodInfoSet __splitWithCharsOverloads;
        private static readonly IReadOnlyMethodInfoSet __splitWithCountOverloads;
        private static readonly IReadOnlyMethodInfoSet __splitWithOptionsOverloads;
        private static readonly IReadOnlyMethodInfoSet __splitWithStringsOverloads;
        private static readonly IReadOnlyMethodInfoSet __startsWithOverloads;
        private static readonly IReadOnlyMethodInfoSet __stringInOverloads;
        private static readonly IReadOnlyMethodInfoSet __stringNinOverloads;
        private static readonly IReadOnlyMethodInfoSet __toLowerOrToUpperOverloads;
        private static readonly IReadOnlyMethodInfoSet __toLowerOverloads;
        private static readonly IReadOnlyMethodInfoSet __toUpperOverloads;
        private static readonly IReadOnlyMethodInfoSet __trimOverloads;

        // static constructor
        static StringMethod()
        {
            // initialize methods before sets of methods
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
            __containsWithChar = ReflectionInfo.Method((string s, char value) => s.Contains(value));
            __containsWithCharAndComparisonType = ReflectionInfo.Method((string s, char value, StringComparison comparisonType) => s.Contains(value, comparisonType));
            __containsWithStringAndComparisonType = ReflectionInfo.Method((string s, string value, StringComparison comparisonType) => s.Contains(value, comparisonType));
            __endsWithWithChar = ReflectionInfo.Method((string s, char value) => s.EndsWith(value));
            __startsWithWithChar = ReflectionInfo.Method((string s, char value) => s.StartsWith(value));
#else
            __containsWithChar = null;
            __containsWithCharAndComparisonType = null;
            __containsWithStringAndComparisonType = null;
            __endsWithWithChar = null;
            __startsWithWithChar = null;
#endif

            __anyStringInWithEnumerable = ReflectionInfo.Method((IEnumerable<string> s, IEnumerable<StringOrRegularExpression> values) => s.AnyStringIn(values));
            __anyStringInWithParams = ReflectionInfo.Method((IEnumerable<string> s, StringOrRegularExpression[] values) => s.AnyStringIn(values));
            __anyStringNinWithEnumerable = ReflectionInfo.Method((IEnumerable<string> s, IEnumerable<StringOrRegularExpression> values) => s.AnyStringNin(values));
            __anyStringNinWithParams = ReflectionInfo.Method((IEnumerable<string> s, StringOrRegularExpression[] values) => s.AnyStringNin(values));
            __compare = ReflectionInfo.Method((string strA, string strB) => String.Compare(strA, strB));
            __compareWithIgnoreCase = ReflectionInfo.Method((string strA, string strB, bool ignoreCase) => String.Compare(strA, strB, ignoreCase));
            __concatWith1Object = ReflectionInfo.Method((object arg) => string.Concat(arg));
            __concatWith2Objects = ReflectionInfo.Method((object arg0, object arg1) => string.Concat(arg0, arg1));
            __concatWith3Objects = ReflectionInfo.Method((object arg0, object arg1, object arg2) => string.Concat(arg0, arg1, arg2));
            __concatWithObjectArray = ReflectionInfo.Method((object[] args) => string.Concat(args));
            __concatWith2Strings = ReflectionInfo.Method((string str0, string str1) => string.Concat(str0, str1));
            __concatWith3Strings = ReflectionInfo.Method((string str0, string str1, string str2) => string.Concat(str0, str1, str2));
            __concatWith4Strings = ReflectionInfo.Method((string str0, string str1, string str2, string str3) => string.Concat(str0, str1, str2, str3));
            __concatWithStringArray = ReflectionInfo.Method((string[] s) => string.Concat(s));
            __containsWithString = ReflectionInfo.Method((string s, string value) => s.Contains(value));
            __endsWithWithString = ReflectionInfo.Method((string s, string value) => s.EndsWith(value));
            __endsWithWithStringAndComparisonType = ReflectionInfo.Method((string s, string value, StringComparison comparisonType) => s.EndsWith(value, comparisonType));
            __endsWithWithStringAndIgnoreCaseAndCulture = ReflectionInfo.Method((string s, string value, bool ignoreCase, CultureInfo culture) => s.EndsWith(value, ignoreCase, culture));
            __equalsWithComparisonType = ReflectionInfo.Method((string s, string value, StringComparison comparisonType) => s.Equals(value, comparisonType));
            __getChars = ReflectionInfo.Method((string s, int index) => s[index]);
            __indexOfAny = ReflectionInfo.Method((string s, char[] anyOf) => s.IndexOfAny(anyOf));
            __indexOfAnyWithStartIndex = ReflectionInfo.Method((string s, char[] anyOf, int startIndex) => s.IndexOfAny(anyOf, startIndex));
            __indexOfAnyWithStartIndexAndCount = ReflectionInfo.Method((string s, char[] anyOf, int startIndex, int count) => s.IndexOfAny(anyOf, startIndex, count));
            __indexOfBytesWithValue = ReflectionInfo.Method((string s, string value) => s.IndexOfBytes(value));
            __indexOfBytesWithValueAndStartIndex = ReflectionInfo.Method((string s, string value, int startIndex) => s.IndexOfBytes(value, startIndex));
            __indexOfBytesWithValueAndStartIndexAndCount = ReflectionInfo.Method((string s, string value, int startIndex, int count) => s.IndexOfBytes(value, startIndex, count));
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
            __isNullOrWhiteSpace = ReflectionInfo.Method((string value) => string.IsNullOrWhiteSpace(value));
            __splitWithChars = ReflectionInfo.Method((string s, char[] separator) => s.Split(separator));
            __splitWithCharsAndCount = ReflectionInfo.Method((string s, char[] separator, int count) => s.Split(separator, count));
            __splitWithCharsAndCountAndOptions = ReflectionInfo.Method((string s, char[] separator, int count, StringSplitOptions options) => s.Split(separator, count, options));
            __splitWithCharsAndOptions = ReflectionInfo.Method((string s, char[] separator, StringSplitOptions options) => s.Split(separator, options));
            __splitWithStringsAndCountAndOptions = ReflectionInfo.Method((string s, string[] separator, int count, StringSplitOptions options) => s.Split(separator, count, options));
            __splitWithStringsAndOptions = ReflectionInfo.Method((string s, string[] separator, StringSplitOptions options) => s.Split(separator, options));
            __startsWithWithString = ReflectionInfo.Method((string s, string value) => s.StartsWith(value));
            __startsWithWithStringAndComparisonType = ReflectionInfo.Method((string s, string value, StringComparison comparisonType) => s.StartsWith(value, comparisonType));
            __startsWithWithStringAndIgnoreCaseAndCulture = ReflectionInfo.Method((string s, string value, bool ignoreCase, CultureInfo culture) => s.StartsWith(value, ignoreCase, culture));
            __staticEqualsWithComparisonType = ReflectionInfo.Method((string a, string b, StringComparison comparisonType) => string.Equals(a, b, comparisonType));
            __stringInWithEnumerable = ReflectionInfo.Method((string s, IEnumerable<StringOrRegularExpression> values) => s.StringIn(values));
            __stringInWithParams = ReflectionInfo.Method((string s, StringOrRegularExpression[] values) => s.StringIn(values));
            __stringNinWithEnumerable = ReflectionInfo.Method((string s, IEnumerable<StringOrRegularExpression> values) => s.StringNin(values));
            __stringNinWithParams = ReflectionInfo.Method((string s, StringOrRegularExpression[] values) => s.StringNin(values));
            __strLenBytes = ReflectionInfo.Method((string s) => s.StrLenBytes());
            __substrBytes = ReflectionInfo.Method((string s, int startIndex, int length) => s.SubstrBytes(startIndex, length));
            __substring = ReflectionInfo.Method((string s, int startIndex) => s.Substring(startIndex));
            __substringWithLength = ReflectionInfo.Method((string s, int startIndex, int length) => s.Substring(startIndex, length));
            __toLower = ReflectionInfo.Method((string s) => s.ToLower());
            __toLowerInvariant = ReflectionInfo.Method((string s) => s.ToLowerInvariant());
            __toLowerWithCulture = ReflectionInfo.Method((string s, CultureInfo culture) => s.ToLower(culture));
            __toUpper = ReflectionInfo.Method((string s) => s.ToUpper());
            __toUpperInvariant = ReflectionInfo.Method((string s) => s.ToUpperInvariant());
            __toUpperWithCulture = ReflectionInfo.Method((string s, CultureInfo culture) => s.ToUpper(culture));
            __trim = ReflectionInfo.Method((string s) => s.Trim());
            __trimEnd = ReflectionInfo.Method((string s, char[] trimChars) => s.TrimEnd(trimChars));
            __trimStart = ReflectionInfo.Method((string s, char[] trimChars) => s.TrimStart(trimChars));
            __trimWithChars = ReflectionInfo.Method((string s, char[] trimChars) => s.Trim(trimChars));

                // initialize sets of methods after methods
            __anyStringInOverloads = MethodInfoSet.Create(
            [
                __anyStringInWithEnumerable,
                __anyStringInWithParams
            ]);

            __anyStringNinOverloads = MethodInfoSet.Create(
            [
                __anyStringNinWithEnumerable,
                __anyStringNinWithParams,
            ]);

            __compareOverloads = MethodInfoSet.Create(
            [
                __compare,
                __compareWithIgnoreCase
            ]);

            __concatOverloads = MethodInfoSet.Create(
            [
                __concatWith1Object,
                __concatWith2Objects,
                __concatWith2Strings,
                __concatWith3Objects,
                __concatWith3Strings,
                __concatWith4Strings,
                __concatWithObjectArray,
                __concatWithStringArray
            ]);

            __containsOverloads = MethodInfoSet.Create(
            [
                __containsWithChar,
                __containsWithCharAndComparisonType,
                __containsWithString,
                __containsWithStringAndComparisonType
            ]);

            __endsWithOverloads = MethodInfoSet.Create(
            [
                __endsWithWithChar,
                __endsWithWithString,
                __endsWithWithStringAndComparisonType,
                __endsWithWithStringAndIgnoreCaseAndCulture,
            ]);

            __indexOfAnyOverloads = MethodInfoSet.Create(
            [
                __indexOfAny,
                __indexOfAnyWithStartIndex,
                __indexOfAnyWithStartIndexAndCount,
            ]);

            __indexOfOverloads = MethodInfoSet.Create(
            [
                __indexOfAny,
                __indexOfAnyWithStartIndex,
                __indexOfAnyWithStartIndexAndCount,
                __indexOfBytesWithValue,
                __indexOfBytesWithValueAndStartIndex,
                __indexOfBytesWithValueAndStartIndexAndCount,
                __indexOfWithChar,
                __indexOfWithCharAndStartIndex,
                __indexOfWithCharAndStartIndexAndCount,
                __indexOfWithString,
                __indexOfWithStringAndComparisonType,
                __indexOfWithStringAndStartIndex,
                __indexOfWithStringAndStartIndexAndComparisonType,
                __indexOfWithStringAndStartIndexAndCount,
                __indexOfWithStringAndStartIndexAndCountAndComparisonType,
            ]);

            __indexOfBytesOverloads = MethodInfoSet.Create(
            [
                __indexOfBytesWithValue,
                __indexOfBytesWithValueAndStartIndex,
                __indexOfBytesWithValueAndStartIndexAndCount
            ]);

            __indexOfWithCharOverloads = MethodInfoSet.Create(
            [
                __indexOfWithChar,
                __indexOfWithCharAndStartIndex,
                __indexOfWithCharAndStartIndexAndCount,
            ]);

            __indexOfWithComparisonTypeOverloads = MethodInfoSet.Create(
            [
                __indexOfWithStringAndComparisonType,
                __indexOfWithStringAndStartIndexAndComparisonType,
                __indexOfWithStringAndStartIndexAndCountAndComparisonType
            ]);

            __indexOfWithCountOverloads = MethodInfoSet.Create(
            [
                __indexOfAnyWithStartIndexAndCount,
                __indexOfBytesWithValueAndStartIndexAndCount,
                __indexOfWithCharAndStartIndexAndCount,
                __indexOfWithStringAndStartIndexAndCount,
                __indexOfWithStringAndStartIndexAndCountAndComparisonType
            ]);

            __indexOfWithStartIndexOverloads = MethodInfoSet.Create(
            [
                __indexOfAnyWithStartIndex,
                __indexOfAnyWithStartIndexAndCount,
                __indexOfBytesWithValueAndStartIndex,
                __indexOfBytesWithValueAndStartIndexAndCount,
                __indexOfWithCharAndStartIndex,
                __indexOfWithCharAndStartIndexAndCount,
                __indexOfWithStringAndStartIndex,
                __indexOfWithStringAndStartIndexAndCount,
                __indexOfWithStringAndStartIndexAndComparisonType,
                __indexOfWithStringAndStartIndexAndCountAndComparisonType
            ]);

            __indexOfWithStringOverloads = MethodInfoSet.Create(
            [
                __indexOfWithString,
                __indexOfWithStringAndComparisonType,
                __indexOfWithStringAndStartIndex,
                __indexOfWithStringAndStartIndexAndComparisonType,
                __indexOfWithStringAndStartIndexAndCount,
                __indexOfWithStringAndStartIndexAndCountAndComparisonType
            ]);

            __indexOfWithStringComparisonOverloads = MethodInfoSet.Create(
            [
                __indexOfWithStringAndComparisonType,
                __indexOfWithStringAndStartIndexAndComparisonType,
                __indexOfWithStringAndStartIndexAndCountAndComparisonType
            ]);

            __splitOverloads = MethodInfoSet.Create(
            [
                __splitWithChars,
                __splitWithCharsAndCount,
                __splitWithCharsAndCountAndOptions,
                __splitWithCharsAndOptions,
                __splitWithStringsAndCountAndOptions,
                __splitWithStringsAndOptions
            ]);

            __splitWithCharsOverloads = MethodInfoSet.Create(
            [
                __splitWithChars,
                __splitWithCharsAndCount,
                __splitWithCharsAndCountAndOptions,
                __splitWithCharsAndOptions
            ]);

            __splitWithCountOverloads = MethodInfoSet.Create(
            [
                __splitWithCharsAndCount,
                __splitWithCharsAndCountAndOptions,
                __splitWithStringsAndCountAndOptions
            ]);

            __splitWithOptionsOverloads = MethodInfoSet.Create(
            [
                __splitWithCharsAndCountAndOptions,
                __splitWithCharsAndOptions,
                __splitWithStringsAndCountAndOptions,
                __splitWithStringsAndOptions
            ]);

            __splitWithStringsOverloads = MethodInfoSet.Create(
            [
                __splitWithStringsAndCountAndOptions,
                __splitWithStringsAndOptions
            ]);

            __startsWithOverloads = MethodInfoSet.Create(
            [
                __startsWithWithChar,
                __startsWithWithString,
                __startsWithWithStringAndComparisonType,
                __startsWithWithStringAndIgnoreCaseAndCulture
            ]);

            __stringInOverloads = MethodInfoSet.Create(
            [
                __stringInWithEnumerable,
                __stringInWithParams
            ]);

            __stringNinOverloads = MethodInfoSet.Create(
            [
                __stringNinWithEnumerable,
                __stringNinWithParams
            ]);

            __toLowerOverloads = MethodInfoSet.Create(
            [
                __toLower,
                __toLowerInvariant,
                __toLowerWithCulture,
            ]);

            __toUpperOverloads = MethodInfoSet.Create(
            [
                __toUpper,
                __toUpperInvariant,
                __toUpperWithCulture,
            ]);

            __trimOverloads = MethodInfoSet.Create(
            [
                __trim,
                __trimEnd,
                __trimStart,
                __trimWithChars
            ]);

            // initialize sets of methods after individual methods
            __endsWithOrStartsWithOverloads = MethodInfoSet.Create(
            [
                __endsWithOverloads,
                __startsWithOverloads
            ]);

            __toLowerOrToUpperOverloads = MethodInfoSet.Create(
            [
                __toLowerOverloads,
                __toUpperOverloads
            ]);
        }

        // public properties
        public static MethodInfo AnyStringInWithEnumerable => __anyStringInWithEnumerable;
        public static MethodInfo AnyStringInWithParams => __anyStringInWithParams;
        public static MethodInfo AnyStringNinWithEnumerable => __anyStringNinWithEnumerable;
        public static MethodInfo AnyStringNinWithParams => __anyStringNinWithParams;
        public static MethodInfo Compare => __compare;
        public static MethodInfo CompareWithIgnoreCase => __compareWithIgnoreCase;
        public static MethodInfo ConcatWith1Object => __concatWith1Object;
        public static MethodInfo ConcatWith2Objects => __concatWith2Objects;
        public static MethodInfo ConcatWith3Objects => __concatWith3Objects;
        public static MethodInfo ConcatWithObjectArray => __concatWithObjectArray;
        public static MethodInfo ConcatWith2Strings => __concatWith2Strings;
        public static MethodInfo ConcatWith3Strings => __concatWith3Strings;
        public static MethodInfo ConcatWith4Strings => __concatWith4Strings;
        public static MethodInfo ConcatWithStringArray => __concatWithStringArray;
        public static MethodInfo ContainsWithChar => __containsWithChar;
        public static MethodInfo ContainsWithCharAndComparisonType => __containsWithCharAndComparisonType;
        public static MethodInfo ContainsWithString => __containsWithString;
        public static MethodInfo ContainsWithStringAndComparisonType => __containsWithStringAndComparisonType;
        public static MethodInfo EndsWithWithChar => __endsWithWithChar;
        public static MethodInfo EndsWithWithString => __endsWithWithString;
        public static MethodInfo EndsWithWithStringAndComparisonType => __endsWithWithStringAndComparisonType;
        public static MethodInfo EndsWithWithStringAndIgnoreCaseAndCulture => __endsWithWithStringAndIgnoreCaseAndCulture;
        public static MethodInfo EqualsWithComparisonType => __equalsWithComparisonType;
        public static MethodInfo GetChars => __getChars;
        public static MethodInfo IndexOfAny => __indexOfAny;
        public static MethodInfo IndexOfAnyWithStartIndex => __indexOfAnyWithStartIndex;
        public static MethodInfo IndexOfAnyWithStartIndexAndCount => __indexOfAnyWithStartIndexAndCount;
        public static MethodInfo IndexOfBytesWithValue => __indexOfBytesWithValue;
        public static MethodInfo IndexOfBytesWithValueAndStartIndex => __indexOfBytesWithValueAndStartIndex;
        public static MethodInfo IndexOfBytesWithValueAndStartIndexAndCount => __indexOfBytesWithValueAndStartIndexAndCount;
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
        public static MethodInfo IsNullOrWhiteSpace => __isNullOrWhiteSpace;
        public static MethodInfo SplitWithChars => __splitWithChars;
        public static MethodInfo SplitWithCharsAndCount => __splitWithCharsAndCount;
        public static MethodInfo SplitWithCharsAndCountAndOptions => __splitWithCharsAndCountAndOptions;
        public static MethodInfo SplitWithCharsAndOptions => __splitWithCharsAndOptions;
        public static MethodInfo SplitWithStringsAndCountAndOptions => __splitWithStringsAndCountAndOptions;
        public static MethodInfo SplitWithStringsAndOptions => __splitWithStringsAndOptions;
        public static MethodInfo StartsWithWithChar => __startsWithWithChar;
        public static MethodInfo StartsWithWithString => __startsWithWithString;
        public static MethodInfo StartsWithWithStringAndComparisonType => __startsWithWithStringAndComparisonType;
        public static MethodInfo StartsWithWithStringAndIgnoreCaseAndCulture => __startsWithWithStringAndIgnoreCaseAndCulture;
        public static MethodInfo StaticEqualsWithComparisonType => __staticEqualsWithComparisonType;
        public static MethodInfo StringInWithEnumerable => __stringInWithEnumerable;
        public static MethodInfo StringInWithParams => __stringInWithParams;
        public static MethodInfo StringNinWithEnumerable => __stringNinWithEnumerable;
        public static MethodInfo StringNinWithParams => __stringNinWithParams;
        public static MethodInfo StrLenBytes => __strLenBytes;
        public static MethodInfo SubstrBytes => __substrBytes;
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

        // sets of methods
        public static IReadOnlyMethodInfoSet AnyStringInOverloads => __anyStringInOverloads;
        public static IReadOnlyMethodInfoSet AnyStringNinOverloads => __anyStringNinOverloads;
        public static IReadOnlyMethodInfoSet CompareOverloads => __compareOverloads;
        public static IReadOnlyMethodInfoSet ConcatOverloads => __concatOverloads;
        public static IReadOnlyMethodInfoSet ContainsOverloads => __containsOverloads;
        public static IReadOnlyMethodInfoSet EndsWithOrStartsWithOverloads => __endsWithOrStartsWithOverloads;
        public static IReadOnlyMethodInfoSet EndsWithOverloads => __endsWithOverloads;
        public static IReadOnlyMethodInfoSet IndexOfAnyOverloads => __indexOfAnyOverloads;
        public static IReadOnlyMethodInfoSet IndexOfOverloads => __indexOfOverloads;
        public static IReadOnlyMethodInfoSet IndexOfBytesOverloads => __indexOfBytesOverloads;
        public static IReadOnlyMethodInfoSet IndexOfWithCountOverloads => __indexOfWithCountOverloads;
        public static IReadOnlyMethodInfoSet IndexOfWithCharOverloads => __indexOfWithCharOverloads;
        public static IReadOnlyMethodInfoSet IndexOfWithComparisonTypeOverloads => __indexOfWithComparisonTypeOverloads;
        public static IReadOnlyMethodInfoSet IndexOfWithStartIndexOverloads => __indexOfWithStartIndexOverloads;
        public static IReadOnlyMethodInfoSet IndexOfWithStringOverloads => __indexOfWithStringOverloads;
        public static IReadOnlyMethodInfoSet IndexOfWithStringComparisonOverloads => __indexOfWithStringComparisonOverloads;
        public static IReadOnlyMethodInfoSet SplitOverloads => __splitOverloads;
        public static IReadOnlyMethodInfoSet SplitWithCharsOverloads => __splitWithCharsOverloads;
        public static IReadOnlyMethodInfoSet SplitWithCountOverloads => __splitWithCountOverloads;
        public static IReadOnlyMethodInfoSet SplitWithOptionsOverloads => __splitWithOptionsOverloads;
        public static IReadOnlyMethodInfoSet SplitWithStringsOverloads => __splitWithStringsOverloads;
        public static IReadOnlyMethodInfoSet StartsWithOverloads => __startsWithOverloads;
        public static IReadOnlyMethodInfoSet StringInOverloads => __stringInOverloads;
        public static IReadOnlyMethodInfoSet StringNinOverloads => __stringNinOverloads;
        public static IReadOnlyMethodInfoSet ToLowerOrToUpperOverloads => __toLowerOrToUpperOverloads;
        public static IReadOnlyMethodInfoSet ToLowerOverloads => __toLowerOverloads;
        public static IReadOnlyMethodInfoSet ToUpperOverloads => __toUpperOverloads;
        public static IReadOnlyMethodInfoSet TrimOverloads => __trimOverloads;
    }
}
