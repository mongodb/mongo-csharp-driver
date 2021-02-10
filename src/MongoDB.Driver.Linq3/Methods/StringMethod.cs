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
using System.Reflection;

namespace MongoDB.Driver.Linq3.Misc
{
    public static class StringMethod
    {
        // private static fields
        private static readonly MethodInfo __endsWith;
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
        private static readonly MethodInfo __substring;
        private static readonly MethodInfo __substringWithLength;
        private static readonly MethodInfo __toLower;
        private static readonly MethodInfo __toLowerInvariant;
        private static readonly MethodInfo __toUpper;
        private static readonly MethodInfo __toUpperInvariant;

        // static constructor
        static StringMethod()
        {
            __endsWith = new Func<string, bool>("".EndsWith).Method;
            __indexOfAny = new Func<char[], int>("".IndexOfAny).Method;
            __indexOfAnyWithStartIndex = new Func<char[], int, int>("".IndexOfAny).Method;
            __indexOfAnyWithStartIndexAndCount = new Func<char[], int, int, int>("".IndexOfAny).Method;
            __indexOfWithChar = new Func<char, int>("".IndexOf).Method;
            __indexOfWithCharAndStartIndex = new Func<char, int, int>("".IndexOf).Method;
            __indexOfWithCharAndStartIndexAndCount = new Func<char, int, int, int>("".IndexOf).Method;
            __indexOfWithString = new Func<string, int>("".IndexOf).Method;
            __indexOfWithStringAndStartIndex = new Func<string, int, int>("".IndexOf).Method;
            __indexOfWithStringAndStartIndexAndCount = new Func<string, int, int, int>("".IndexOf).Method;
            __indexOfWithStringAndComparisonType = new Func<string, StringComparison, int>("".IndexOf).Method;
            __indexOfWithStringAndStartIndexAndComparisonType = new Func<string, int, StringComparison, int>("".IndexOf).Method;
            __indexOfWithStringAndStartIndexAndCountAndComparisonType = new Func<string, int, int, StringComparison, int>("".IndexOf).Method;
            __isNullOrEmpty = new Func<string, bool>(string.IsNullOrEmpty).Method;
            __splitWithChars = new Func<char[], string[]>("".Split).Method;
            __splitWithCharsAndCount = new Func<char[], int, string[]>("".Split).Method;
            __splitWithCharsAndCountAndOptions = new Func<char[], int, StringSplitOptions, string[]>("".Split).Method;
            __splitWithCharsAndOptions = new Func<char[], StringSplitOptions, string[]>("".Split).Method;
            __splitWithStringsAndCountAndOptions = new Func<string[], int, StringSplitOptions, string[]>("".Split).Method;
            __splitWithStringsAndOptions = new Func<string[], StringSplitOptions, string[]>("".Split).Method;
            __substring = new Func<int, string>("".Substring).Method;
            __substringWithLength = new Func<int, int, string>("".Substring).Method;
            __toLower = new Func<string>("".ToLower).Method;
            __toLowerInvariant = new Func<string>("".ToLowerInvariant).Method;
            __toUpper = new Func<string>("".ToUpper).Method;
            __toUpperInvariant = new Func<string>("".ToUpperInvariant).Method;
        }

        // public properties
        public static MethodInfo EndsWith => __endsWith;
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
        public static MethodInfo Substring => __substring;
        public static MethodInfo SubstringWithLength => __substringWithLength;
        public static MethodInfo ToLower => __toLower;
        public static MethodInfo ToLowerInvariant => __toLowerInvariant;
        public static MethodInfo ToUpper => __toUpper;
        public static MethodInfo ToUpperInvariant => __toUpperInvariant;
    }
}
